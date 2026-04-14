import { useState, useCallback, useRef } from 'react';
import { useMutation } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import {
  validateRestBuilder,
  validateSoapBuilder,
  validateEventBuilder,
  validateWorkserviceBuilder,
  validateSharedSchemaBuilder,
  validateWebhookBuilder,
  validateLegacyContractBuilder,
} from '../workspace/builders/shared/builderValidation';
import type { BuilderValidationResult } from '../workspace/builders/shared/builderTypes';
import type { ValidationIssue, ValidationSummary, ContractProtocol } from '../types';

// ── Types ─────────────────────────────────────────────────────────────────────

/** Fases de validação disponíveis. */
export type ValidationPhase = 'syntax' | 'rules' | 'canonical';

/** Estado unificado da validação de draft. */
export interface DraftValidationState {
  /** Issues de todas as fases combinadas. */
  issues: ValidationIssue[];
  /** Resumo calculado. */
  summary: DraftValidationSummary;
  /** Fases que já foram executadas. */
  completedPhases: ValidationPhase[];
  /** Fase actualmente em execução (null se idle). */
  runningPhase: ValidationPhase | null;
  /** Último fingerprint do conteúdo validado. */
  fingerprint: string | null;
}

export interface DraftValidationSummary {
  totalIssues: number;
  errorCount: number;
  warningCount: number;
  infoCount: number;
  hintCount: number;
  isValid: boolean;
  sources: string[];
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Converte BuilderValidationError em ValidationIssue. */
function builderErrorsToIssues(result: BuilderValidationResult): ValidationIssue[] {
  return result.errors.map((err) => ({
    ruleId: err.field,
    ruleName: err.messageKey,
    severity: 'Error' as const,
    messageKey: err.messageKey,
    message: err.fallback,
    path: err.field,
    source: 'schema' as const,
  }));
}

/** Valida sintaxe do conteúdo textual (YAML/JSON/XML). */
function validateSyntax(content: string, format: string): ValidationIssue[] {
  if (!content.trim()) return [];
  const issues: ValidationIssue[] = [];

  if (format === 'json') {
    try {
      JSON.parse(content);
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Invalid JSON';
      // Tenta extrair posição do erro do JSON.parse
      const lineMatch = msg.match(/position (\d+)/i);
      let line: number | undefined;
      if (lineMatch) {
        const pos = parseInt(lineMatch[1], 10);
        line = content.substring(0, pos).split('\n').length;
      }
      issues.push({
        ruleId: 'json-syntax',
        ruleName: 'JSON Syntax',
        severity: 'Error',
        message: msg,
        path: '#',
        line,
        source: 'schema',
      });
    }
  }

  if (format === 'yaml') {
    // Detecções básicas de YAML malformado
    const lines = content.split('\n');
    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      if (line === undefined) continue;
      // Tabs em YAML são inválidos
      if (line.includes('\t') && !line.trimStart().startsWith('#')) {
        issues.push({
          ruleId: 'yaml-no-tabs',
          ruleName: 'YAML No Tabs',
          severity: 'Error',
          message: 'YAML does not allow tabs for indentation.',
          path: `#/line/${i + 1}`,
          line: i + 1,
          source: 'schema',
        });
      }
    }
  }

  if (format === 'xml') {
    // Validação básica de XML: verifica tags abertas sem fecho
    const openTags = (content.match(/<[a-zA-Z][^/]*?>/g) ?? []).length;
    const closeTags = (content.match(/<\/[a-zA-Z][^>]*>/g) ?? []).length;
    const selfClose = (content.match(/<[^>]+\/>/g) ?? []).length;
    if (openTags - selfClose > closeTags) {
      issues.push({
        ruleId: 'xml-unclosed-tag',
        ruleName: 'XML Syntax',
        severity: 'Error',
        message: 'Unclosed XML tag detected.',
        path: '#',
        source: 'schema',
      });
    }
  }

  return issues;
}

/** Obtém validador correcto para o protocolo do visual builder. */
function validateBuilderByProtocol(
  protocol: string,
  builderState: unknown,
): BuilderValidationResult | null {
  switch (protocol.toLowerCase()) {
    case 'openapi':
    case 'swagger':
      return validateRestBuilder(builderState as Parameters<typeof validateRestBuilder>[0]);
    case 'wsdl':
      return validateSoapBuilder(builderState as Parameters<typeof validateSoapBuilder>[0]);
    case 'asyncapi':
      return validateEventBuilder(builderState as Parameters<typeof validateEventBuilder>[0]);
    case 'workerservice':
      return validateWorkserviceBuilder(builderState as Parameters<typeof validateWorkserviceBuilder>[0]);
    case 'sharedschema':
    case 'jsonschema':
      return validateSharedSchemaBuilder(builderState as Parameters<typeof validateSharedSchemaBuilder>[0]);
    case 'webhook':
      return validateWebhookBuilder(builderState as Parameters<typeof validateWebhookBuilder>[0]);
    case 'legacy':
      return validateLegacyContractBuilder(builderState as Parameters<typeof validateLegacyContractBuilder>[0]);
    default:
      return null;
  }
}

function computeSummary(issues: ValidationIssue[]): DraftValidationSummary {
  const errorCount = issues.filter((i) => i.severity === 'Error' || i.severity === 'Blocked').length;
  const warningCount = issues.filter((i) => i.severity === 'Warning').length;
  const infoCount = issues.filter((i) => i.severity === 'Info').length;
  const hintCount = issues.filter((i) => i.severity === 'Hint').length;
  const sources = [...new Set(issues.map((i) => i.source))];

  return {
    totalIssues: issues.length,
    errorCount,
    warningCount,
    infoCount,
    hintCount,
    isValid: errorCount === 0,
    sources,
  };
}

const EMPTY_STATE: DraftValidationState = {
  issues: [],
  summary: { totalIssues: 0, errorCount: 0, warningCount: 0, infoCount: 0, hintCount: 0, isValid: true, sources: [] },
  completedPhases: [],
  runningPhase: null,
  fingerprint: null,
};

// ── Hook ──────────────────────────────────────────────────────────────────────

/**
 * Hook para validação multi-fase de drafts de contrato.
 * Fase 1 (syntax): executa localmente, em tempo real.
 * Fase 2 (rules + design + canonical): executa no backend via /contracts/validate-spec.
 * Retorna issues unificados, resumo e estado de execução.
 */
export function useDraftValidation() {
  const [state, setState] = useState<DraftValidationState>(EMPTY_STATE);
  const abortRef = useRef<AbortController | null>(null);

  // Backend mutation
  const backendMutation = useMutation({
    mutationFn: (data: { specContent: string; protocol: ContractProtocol; rulesetIds?: string[] }) =>
      contractsApi.validateSpecContent(data),
  });

  /**
   * Executa validação de sintaxe (Fase 1) — local, síncrono.
   * Pode ser chamado a cada mudança de conteúdo (com debounce no componente).
   */
  const validateSyntaxPhase = useCallback((specContent: string, format: string): ValidationIssue[] => {
    const syntaxIssues = validateSyntax(specContent, format);

    setState((prev) => {
      // Remove issues anteriores de 'schema' e substitui pelas novas
      const nonSchemaIssues = prev.issues.filter((i) => i.source !== 'schema');
      const allIssues = [...syntaxIssues, ...nonSchemaIssues];

      return {
        ...prev,
        issues: allIssues,
        summary: computeSummary(allIssues),
        completedPhases: prev.completedPhases.includes('syntax')
          ? prev.completedPhases
          : [...prev.completedPhases, 'syntax'],
      };
    });

    return syntaxIssues;
  }, []);

  /**
   * Executa validação de visual builder (Fase 1b) — local, síncrono.
   * Complementa a validação de sintaxe com checks estruturais do builder.
   */
  const validateBuilderPhase = useCallback((protocol: string, builderState: unknown): ValidationIssue[] => {
    const result = validateBuilderByProtocol(protocol, builderState);
    if (!result) return [];

    const builderIssues = builderErrorsToIssues(result);

    setState((prev) => {
      const nonSchemaIssues = prev.issues.filter((i) => i.source !== 'schema');
      const allIssues = [...builderIssues, ...nonSchemaIssues];

      return {
        ...prev,
        issues: allIssues,
        summary: computeSummary(allIssues),
        completedPhases: prev.completedPhases.includes('syntax')
          ? prev.completedPhases
          : [...prev.completedPhases, 'syntax'],
      };
    });

    return builderIssues;
  }, []);

  /**
   * Executa validação completa no backend (Fase 2 + 3) — assíncrono.
   * Aplica regras determinísticas, directrizes de design e conformidade canónica.
   */
  const validateBackend = useCallback(async (specContent: string, protocol: ContractProtocol) => {
    // Abort previous request
    if (abortRef.current) {
      abortRef.current.abort();
    }
    abortRef.current = new AbortController();

    setState((prev) => ({
      ...prev,
      runningPhase: 'rules',
    }));

    try {
      const result = await backendMutation.mutateAsync({
        specContent,
        protocol,
      });

      const backendIssues: ValidationIssue[] = result.issues.map((i) => ({
        ruleId: i.ruleId,
        ruleName: i.ruleName,
        severity: i.severity,
        messageKey: i.messageKey,
        messageParams: i.messageParams,
        message: i.message,
        path: i.path,
        line: i.line,
        column: i.column,
        source: i.source as ValidationIssue['source'],
        suggestedFixKey: i.suggestedFixKey,
        suggestedFix: i.suggestedFix,
      }));

      setState((prev) => {
        // Mantém issues de 'schema' (local), substitui internal + canonical (backend)
        const localIssues = prev.issues.filter((i) => i.source === 'schema');
        const allIssues = [...localIssues, ...backendIssues];

        return {
          issues: allIssues,
          summary: computeSummary(allIssues),
          completedPhases: ['syntax', 'rules', 'canonical'],
          runningPhase: null,
          fingerprint: result.fingerprint ?? null,
        };
      });
    } catch {
      setState((prev) => ({
        ...prev,
        runningPhase: null,
      }));
    }
  }, [backendMutation]);

  /**
   * Executa todas as fases de validação.
   * Fase 1: sintaxe local + builder.
   * Fase 2+3: backend (regras, design, canonical).
   */
  const validateAll = useCallback(async (
    specContent: string,
    format: string,
    protocol: ContractProtocol,
    builderState?: unknown,
  ) => {
    // Fase 1: local
    validateSyntaxPhase(specContent, format);
    if (builderState) {
      validateBuilderPhase(protocol, builderState);
    }

    // Fases 2+3: backend
    if (specContent.trim()) {
      await validateBackend(specContent, protocol);
    }
  }, [validateSyntaxPhase, validateBuilderPhase, validateBackend]);

  /** Limpa todo o estado de validação. */
  const reset = useCallback(() => {
    setState(EMPTY_STATE);
    if (abortRef.current) {
      abortRef.current.abort();
      abortRef.current = null;
    }
  }, []);

  return {
    state,
    isRunning: state.runningPhase !== null || backendMutation.isPending,
    validateSyntaxPhase,
    validateBuilderPhase,
    validateBackend,
    validateAll,
    reset,
  };
}
