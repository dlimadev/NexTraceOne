import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ScanSearch,
  RefreshCw,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Info,
  Ban,
  Sparkles,
  ChevronDown,
  ChevronRight,
  Filter,
} from 'lucide-react';
import type { DraftValidationState, ValidationPhase } from '../../hooks/useDraftValidation';
import type { ValidationIssue, ValidationSeverity, ContractProtocol } from '../../types';

// ── Constants ─────────────────────────────────────────────────────────────────

const SEVERITY_ICON: Record<ValidationSeverity, React.ComponentType<{ size?: number; className?: string }>> = {
  Error: XCircle,
  Warning: AlertTriangle,
  Info: Info,
  Hint: Sparkles,
  Blocked: Ban,
};

const SEVERITY_ORDER: ValidationSeverity[] = ['Blocked', 'Error', 'Warning', 'Info', 'Hint'];

const SOURCE_LABELS: Record<string, string> = {
  schema: 'Syntax',
  internal: 'Rules & Design',
  canonical: 'Canonical',
  spectral: 'Spectral',
};

const PHASE_LABELS: Record<ValidationPhase, string> = {
  syntax: 'contracts.draftValidation.phaseSyntax',
  rules: 'contracts.draftValidation.phaseRules',
  canonical: 'contracts.draftValidation.phaseCanonical',
};

// ── Props ─────────────────────────────────────────────────────────────────────

interface DraftValidationPanelProps {
  state: DraftValidationState;
  isRunning: boolean;
  protocol: ContractProtocol;
  onRunValidation: () => void;
  className?: string;
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Painel de validação multi-fase para drafts de contrato.
 * Apresenta issues unificados de todas as fontes (syntax, rules, design, canonical),
 * com filtros por severidade e fonte. Integra-se no DraftStudioPage.
 */
export function DraftValidationPanel({
  state,
  isRunning,
  protocol,
  onRunValidation,
  className = '',
}: DraftValidationPanelProps) {
  const { t } = useTranslation();
  const [expandedSeverity, setExpandedSeverity] = useState<string | null>(null);
  const [sourceFilter, setSourceFilter] = useState<string | null>(null);

  const { issues, summary, completedPhases } = state;

  const filteredIssues = sourceFilter
    ? issues.filter((i) => i.source === sourceFilter)
    : issues;

  const severityCounts: Record<ValidationSeverity, number> = {
    Blocked: filteredIssues.filter((i) => i.severity === 'Blocked').length,
    Error: filteredIssues.filter((i) => i.severity === 'Error').length,
    Warning: filteredIssues.filter((i) => i.severity === 'Warning').length,
    Info: filteredIssues.filter((i) => i.severity === 'Info').length,
    Hint: filteredIssues.filter((i) => i.severity === 'Hint').length,
  };

  const toggleSeverity = (severity: string) => {
    setExpandedSeverity(expandedSeverity === severity ? null : severity);
  };

  const hasResults = issues.length > 0 || completedPhases.length > 0;

  return (
    <div className={`flex flex-col gap-4 h-full overflow-y-auto p-4 ${className}`}>
      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <ScanSearch size={16} className="text-accent" />
          <h3 className="text-sm font-semibold text-heading">
            {t('contracts.draftValidation.title', 'Draft Validation')}
          </h3>
          {completedPhases.length > 0 && (
            <span className="text-[10px] text-muted">
              ({completedPhases.length}/3 {t('contracts.draftValidation.phases', 'phases')})
            </span>
          )}
        </div>
        <button
          onClick={onRunValidation}
          disabled={isRunning}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md bg-accent/15 text-accent border border-accent/25 hover:bg-accent/25 transition-colors disabled:opacity-50"
        >
          <RefreshCw size={12} className={isRunning ? 'animate-spin' : ''} />
          {isRunning
            ? t('contracts.draftValidation.running', 'Validating...')
            : t('contracts.draftValidation.runAll', 'Validate All')}
        </button>
      </div>

      {/* ── Phase indicators ── */}
      <div className="flex items-center gap-2">
        {(['syntax', 'rules', 'canonical'] as ValidationPhase[]).map((phase) => {
          const isCompleted = completedPhases.includes(phase);
          const isCurrent = state.runningPhase === phase;
          return (
            <span
              key={phase}
              className={`inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-medium rounded-full border ${
                isCurrent
                  ? 'bg-accent/15 text-accent border-accent/25 animate-pulse'
                  : isCompleted
                    ? 'bg-mint/15 text-mint border-mint/25'
                    : 'bg-muted/10 text-muted border-edge'
              }`}
            >
              {isCompleted && <CheckCircle2 size={10} />}
              {isCurrent && <RefreshCw size={10} className="animate-spin" />}
              {t(PHASE_LABELS[phase], phase)}
            </span>
          );
        })}
      </div>

      {/* ── Overall status ── */}
      {hasResults && (
        <div className="flex items-center justify-between p-3 rounded-lg bg-elevated border border-edge">
          <div className="flex items-center gap-2">
            {summary.isValid ? (
              <CheckCircle2 size={16} className="text-mint" />
            ) : (
              <XCircle size={16} className="text-danger" />
            )}
            <div>
              <p className="text-xs font-medium text-heading">
                {summary.isValid
                  ? t('contracts.draftValidation.noErrors', 'No errors found')
                  : t('contracts.draftValidation.errorsFound', '{{count}} error(s) found', { count: summary.errorCount })}
              </p>
              <p className="text-[10px] text-muted">
                {t('contracts.draftValidation.totalIssues', '{{count}} total issue(s)', { count: summary.totalIssues })}
                {summary.sources.length > 0 && ` · ${summary.sources.map((s) => SOURCE_LABELS[s] ?? s).join(', ')}`}
              </p>
            </div>
          </div>
          {state.fingerprint && (
            <code className="text-[9px] font-mono text-muted/60">{state.fingerprint}</code>
          )}
        </div>
      )}

      {/* ── Source filter ── */}
      {summary.sources.length > 1 && (
        <div className="flex items-center gap-1">
          <Filter size={11} className="text-muted" />
          <button
            onClick={() => setSourceFilter(null)}
            className={`px-2 py-0.5 text-[10px] rounded-full transition-colors ${
              sourceFilter === null
                ? 'bg-accent/15 text-accent border border-accent/25'
                : 'text-muted hover:text-heading border border-transparent'
            }`}
          >
            {t('contracts.draftValidation.filterAll', 'All')}
          </button>
          {summary.sources.map((src) => (
            <button
              key={src}
              onClick={() => setSourceFilter(sourceFilter === src ? null : src)}
              className={`px-2 py-0.5 text-[10px] rounded-full transition-colors ${
                sourceFilter === src
                  ? 'bg-accent/15 text-accent border border-accent/25'
                  : 'text-muted hover:text-heading border border-transparent'
              }`}
            >
              {SOURCE_LABELS[src] ?? src}
            </button>
          ))}
        </div>
      )}

      {/* ── Severity counters ── */}
      {hasResults && (
        <div className="grid grid-cols-5 gap-2">
          {SEVERITY_ORDER.map((severity) => {
            const count = severityCounts[severity];
            const Icon = SEVERITY_ICON[severity];
            return (
              <button
                key={severity}
                onClick={() => toggleSeverity(severity)}
                className={`flex items-center gap-1.5 p-2 rounded-lg text-left transition-all border ${
                  count > 0
                    ? severity === 'Error' || severity === 'Blocked'
                      ? 'bg-danger/10 text-danger border-danger/20'
                      : severity === 'Warning'
                        ? 'bg-warning/10 text-warning border-warning/20'
                        : 'bg-muted/10 text-muted border-edge'
                    : 'bg-elevated/30 text-muted/40 border-edge/50'
                } ${expandedSeverity === severity ? 'ring-1 ring-accent/40' : ''}`}
              >
                <Icon size={12} />
                <div>
                  <p className="text-sm font-bold leading-none">{count}</p>
                  <p className="text-[9px] opacity-70">{severity}</p>
                </div>
              </button>
            );
          })}
        </div>
      )}

      {/* ── Issues list ── */}
      {filteredIssues.length > 0 && (
        <div className="flex-1 min-h-0">
          <div className="space-y-1 max-h-[500px] overflow-y-auto pr-1">
            {SEVERITY_ORDER.map((severity) => {
              const filtered = filteredIssues.filter((i) => i.severity === severity);
              if (filtered.length === 0) return null;
              const isExpanded = expandedSeverity === severity || expandedSeverity === null;
              const Icon = SEVERITY_ICON[severity];

              return (
                <div key={severity}>
                  <button
                    onClick={() => toggleSeverity(severity)}
                    className="flex items-center gap-2 w-full px-2 py-1.5 text-xs font-medium text-heading hover:bg-elevated/50 rounded transition-colors"
                  >
                    {isExpanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
                    <Icon size={12} />
                    <span>{severity}</span>
                    <span className="text-muted">({filtered.length})</span>
                  </button>
                  {isExpanded && (
                    <div className="ml-6 space-y-1 mb-2">
                      {filtered.map((issue, idx) => (
                        <IssueRow key={`${issue.ruleId}-${idx}`} issue={issue} />
                      ))}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ── Empty state ── */}
      {!hasResults && !isRunning && (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <ScanSearch size={32} className="text-muted/30 mb-3" />
          <p className="text-sm font-medium text-heading">
            {t('contracts.draftValidation.emptyTitle', 'No validation results yet')}
          </p>
          <p className="text-xs text-muted mt-1 max-w-xs">
            {t('contracts.draftValidation.emptyDescription', 'Click "Validate All" to check your contract against syntax rules, design guidelines, and canonical conformance.')}
          </p>
        </div>
      )}
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function IssueRow({ issue }: { issue: ValidationIssue }) {
  const { t } = useTranslation();
  const displayMessage = issue.messageKey
    ? t(issue.messageKey, { ...issue.messageParams, defaultValue: issue.message })
    : issue.message;
  const displayFix = issue.suggestedFixKey
    ? t(issue.suggestedFixKey, { defaultValue: issue.suggestedFix })
    : issue.suggestedFix;

  return (
    <div className="flex items-start gap-2 px-2 py-1.5 text-xs rounded bg-elevated/30 border border-edge/10">
      <div className="flex-1 min-w-0">
        <p className="text-heading font-medium leading-snug">{displayMessage}</p>
        <div className="flex items-center gap-2 mt-0.5 text-muted flex-wrap">
          <span className={`px-1 py-px rounded text-[9px] font-medium ${
            issue.source === 'schema'
              ? 'bg-blue-500/15 text-blue-400'
              : issue.source === 'internal'
                ? 'bg-purple-500/15 text-purple-400'
                : issue.source === 'canonical'
                  ? 'bg-amber-500/15 text-amber-400'
                  : 'bg-muted/15 text-muted'
          }`}>
            {SOURCE_LABELS[issue.source] ?? issue.source}
          </span>
          {issue.ruleName && <span className="truncate">{issue.ruleName}</span>}
          {issue.path && issue.path !== '#' && <span className="font-mono text-[9px]">· {issue.path}</span>}
          {issue.line != null && <span>· L{issue.line}</span>}
        </div>
        {displayFix && (
          <p className="mt-1 text-mint/80 italic text-[10px]">{displayFix}</p>
        )}
      </div>
    </div>
  );
}
