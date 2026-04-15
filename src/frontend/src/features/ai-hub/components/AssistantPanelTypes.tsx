import {
  Server,
  FileText,
  GitBranch,
  AlertTriangle,
} from 'lucide-react';

// ── Types ───────────────────────────────────────────────────────────────

/** Tipo de contexto do painel assistente. */
export type AssistantContextType = 'service' | 'contract' | 'change' | 'incident';

/** Resumo contextual passado pelo host (detail page). */
export interface ContextSummary {
  name: string;
  description?: string;
  status?: string;
  additionalInfo?: Record<string, string>;
}

/** Relação contextual entre entidades para grounding. */
export interface ContextDataRelation {
  relationType: string;
  entityType: string;
  name: string;
  status?: string;
  properties?: Record<string, string>;
}

/** Dados ricos da entidade para fundamentar respostas da IA. */
export interface ContextData {
  entityType: string;
  entityName: string;
  entityStatus?: string;
  entityDescription?: string;
  properties?: Record<string, string>;
  relations?: ContextDataRelation[];
  caveats?: string[];
}

/** Propriedades do AssistantPanel. */
export interface AssistantPanelProps {
  contextType: AssistantContextType;
  contextId: string;
  contextSummary: ContextSummary;
  contextData?: ContextData;
  /** Contexto de ambiente ativo — passado opcionalmente pelo host para grounding da IA */
  activeEnvironmentId?: string;
  /** Nome do ambiente ativo para exibição */
  activeEnvironmentName?: string;
  /** Indica se o ambiente é não produtivo — exibe aviso no painel */
  isNonProductionEnvironment?: boolean;
}

export interface ChatMessage {
  id: string;
  role: 'assistant' | 'user';
  content: string;
  modelName?: string | null;
  provider?: string | null;
  isInternalModel?: boolean;
  promptTokens?: number;
  completionTokens?: number;
  appliedPolicyName?: string | null;
  groundingSources?: string[];
  contextReferences?: string[];
  correlationId?: string;
  useCaseType?: string;
  routingPath?: string;
  confidenceLevel?: string;
  costClass?: string;
  routingRationale?: string;
  sourceWeightingSummary?: string;
  escalationReason?: string;
  suggestedActions?: SuggestedAction[];
  contextStrength?: string;
  suggestedSteps?: string[];
  caveats?: string[];
  contextSummaryText?: string;
  /** Hints de explicabilidade para o painel "View Sources" (E-M05). */
  explainabilityHints?: ExplainabilityHint[];
  timestamp: string;
}

/** Detalhe de uma fonte individual para o painel de explicabilidade (E-M05). */
export interface ExplainabilityHint {
  /** Identificador ou nome da fonte. */
  sourceId: string;
  /** Título ou nome amigável. */
  title: string;
  /** Tipo de fonte (ex: 'ServiceCatalog', 'KnowledgeHub', 'ContractRegistry'). */
  sourceType: string;
  /** Score de relevância [0-1]. */
  relevanceScore: number;
  /** Trecho ou resumo relevante. */
  snippet?: string;
}

export interface SuggestedAction {
  label: string;
  type: 'navigate' | 'query' | 'external';
  target: string;
}

// ── Context-specific ground truth sources ───────────────────────────

export const contextGroundingSources: Record<AssistantContextType, string[]> = {
  service: ['Service Catalog', 'Contract Registry', 'Dependency Graph', 'Change Intelligence'],
  contract: ['Contract Registry', 'Service Catalog', 'Version History', 'Compatibility Checks'],
  change: ['Change Intelligence', 'Incident History', 'Service Catalog', 'Blast Radius Analysis'],
  incident: ['Incident History', 'Change Intelligence', 'Runbook Library', 'Service Catalog'],
};

// ── Context-specific suggested actions per use case ─────────────────

export function buildSuggestedActions(
  contextType: AssistantContextType,
  contextId: string,
  t: (key: string) => string,
): SuggestedAction[] {
  switch (contextType) {
    case 'service':
      return [
        { label: t('assistantPanel.actions.viewContracts'), type: 'navigate', target: `/services/${contextId}` },
        { label: t('assistantPanel.actions.viewDependencies'), type: 'navigate', target: `/services/graph` },
        { label: t('assistantPanel.actions.viewRecentChanges'), type: 'navigate', target: `/changes` },
      ];
    case 'contract':
      return [
        { label: t('assistantPanel.actions.viewVersionHistory'), type: 'navigate', target: `/contracts/${contextId}` },
        { label: t('assistantPanel.actions.checkCompatibility'), type: 'query', target: 'Check compatibility of this contract version with consumers' },
        { label: t('assistantPanel.actions.viewOwnerService'), type: 'navigate', target: `/services` },
      ];
    case 'change':
      return [
        { label: t('assistantPanel.actions.viewBlastRadius'), type: 'navigate', target: `/changes/${contextId}` },
        { label: t('assistantPanel.actions.correlateIncidents'), type: 'query', target: 'Are there any incidents correlated with this change?' },
        { label: t('assistantPanel.actions.checkRollbackReadiness'), type: 'query', target: 'What is the rollback readiness for this change?' },
      ];
    case 'incident':
      return [
        { label: t('assistantPanel.actions.findRunbook'), type: 'query', target: 'Is there a runbook for this type of incident?' },
        { label: t('assistantPanel.actions.correlateChanges'), type: 'query', target: 'What recent changes could have caused this incident?' },
        { label: t('assistantPanel.actions.suggestMitigation'), type: 'query', target: 'What mitigation steps should I take for this incident?' },
      ];
  }
}

// ── Context-specific scope mapping ──────────────────────────────────

export const contextScopeMap: Record<AssistantContextType, string> = {
  service: 'Services',
  contract: 'Contracts',
  change: 'Changes',
  incident: 'Incidents',
};

export const contextScopeIcons: Record<AssistantContextType, React.ReactNode> = {
  service: <Server size={14} />,
  contract: <FileText size={14} />,
  change: <GitBranch size={14} />,
  incident: <AlertTriangle size={14} />,
};

// ── Context assessment constants ─────────────────────────────────────
// Min property and relation counts to classify context richness.
// These thresholds align with typical entity data: detail pages usually
// provide 3-8 properties and 0-5 relation groups.
const CONTEXT_STRONG_MIN_PROPS = 3;
const CONTEXT_STRONG_MIN_RELS = 2;
const CONTEXT_GOOD_MIN_PROPS = 3;
const CONTEXT_GOOD_MIN_RELS = 1;
const MAX_DISPLAYED_RELATIONS = 5;

// ── Mock contextual response generator ──────────────────────────────

export function assessContextStrength(contextData?: ContextData): string {
  if (!contextData) return 'none';
  const propCount = Object.keys(contextData.properties ?? {}).length;
  const relCount = (contextData.relations ?? []).length;
  const hasCaveats = (contextData.caveats ?? []).length > 0;
  if (propCount >= CONTEXT_STRONG_MIN_PROPS && relCount >= CONTEXT_STRONG_MIN_RELS && !hasCaveats) return 'strong';
  if (propCount >= CONTEXT_GOOD_MIN_PROPS && relCount >= CONTEXT_GOOD_MIN_RELS) return 'good';
  if (propCount >= 1 || relCount >= 1) return 'partial';
  return 'weak';
}

export function buildGroundedContent(
  contextType: AssistantContextType,
  contextData: ContextData,
  t: (key: string) => string,
): { content: string; suggestedSteps: string[]; contextSummaryText: string } {
  const props = contextData.properties ?? {};
  const relations = contextData.relations ?? [];
  const lines: string[] = [];
  let suggestedSteps: string[] = [];

  lines.push(`${t(`assistantPanel.response.${contextType}Analysis`)} **${contextData.entityName}**${contextData.entityStatus ? ` (${contextData.entityStatus})` : ''}.`);

  if (contextData.entityDescription) {
    lines.push(`\n${contextData.entityDescription}`);
  }

  // Properties section
  const propEntries = Object.entries(props);
  if (propEntries.length > 0) {
    lines.push(`\n**${t('assistantPanel.response.additionalContext')}:**`);
    propEntries.forEach(([k, v]) => lines.push(`• ${k}: ${v}`));
  }

  // Relations section grouped by relationType
  const relationGroups = new Map<string, ContextDataRelation[]>();
  relations.forEach(r => {
    const group = relationGroups.get(r.relationType) ?? [];
    group.push(r);
    relationGroups.set(r.relationType, group);
  });

  relationGroups.forEach((rels, groupName) => {
    lines.push(`\n**${groupName}** (${rels.length}):`);
    rels.slice(0, MAX_DISPLAYED_RELATIONS).forEach(r => {
      const relProps = r.properties ? Object.entries(r.properties).map(([k, v]) => `${k}: ${v}`).join(', ') : '';
      lines.push(`• ${r.name}${r.status ? ` [${r.status}]` : ''}${relProps ? ` — ${relProps}` : ''}`);
    });
    if (rels.length > MAX_DISPLAYED_RELATIONS) {
      lines.push(`  … +${rels.length - MAX_DISPLAYED_RELATIONS} more`);
    }
  });

  lines.push(`\n${t(`assistantPanel.response.${contextType}GroundingNote`)}`);

  // Entity-specific suggested steps
  switch (contextType) {
    case 'service':
      suggestedSteps = [
        ...(relations.some(r => r.relationType === 'Contracts') ? [] : ['Review and register service contracts']),
        ...(!props.team ? ['Assign team ownership'] : []),
        ...(!props.criticality ? ['Define service criticality'] : []),
      ];
      break;
    case 'contract':
      suggestedSteps = [
        ...(relations.some(r => r.relationType === 'Violations') ? ['Address contract violations'] : []),
        ...(!props.version ? ['Set semantic version'] : []),
        ...(contextData.entityStatus === 'Draft' ? ['Submit contract for review'] : []),
      ];
      break;
    case 'change':
      suggestedSteps = [
        ...(props.validationStatus !== 'Validated' ? ['Complete validation checks'] : []),
        ...(!props.advisory ? ['Wait for advisory recommendation'] : []),
        ...(props.advisory === 'Reject' ? ['Address rejection factors before resubmission'] : []),
      ];
      break;
    case 'incident':
      suggestedSteps = [
        ...(props.mitigationStatus !== 'Verified' ? ['Execute mitigation actions'] : []),
        ...(!relations.some(r => r.relationType === 'Runbooks') ? ['Associate applicable runbooks'] : []),
        ...(!relations.some(r => r.relationType === 'Correlated Changes') ? ['Investigate potential correlated changes'] : []),
      ];
      break;
  }

  const sourceLabels = contextGroundingSources[contextType];
  const contextSummaryText = `${t('assistantPanel.groundedIn')} ${contextData.entityType}: ${contextData.entityName} (${sourceLabels.join(', ')})`;

  return {
    content: lines.join('\n'),
    suggestedSteps: suggestedSteps.filter(Boolean),
    contextSummaryText,
  };
}

export function generateContextualResponse(
  contextType: AssistantContextType,
  contextSummary: ContextSummary,
  _userMessage: string,
  t: (key: string) => string,
  contextData?: ContextData,
): { content: string; useCaseType: string; sources: string[]; refs: string[]; confidence: string; weightSummary: string; contextStrength: string; suggestedSteps: string[]; caveats: string[]; contextSummaryText: string } {
  const sources = contextGroundingSources[contextType];
  const contextStrength = assessContextStrength(contextData);

  // Weight summary based on context type
  const weightMap: Record<AssistantContextType, string> = {
    service: 'ServiceCatalog:40%,ContractRegistry:25%,DependencyGraph:20%,ChangeIntelligence:15%',
    contract: 'ContractRegistry:45%,VersionHistory:25%,ServiceCatalog:20%,Compatibility:10%',
    change: 'ChangeIntelligence:40%,IncidentHistory:25%,ServiceCatalog:20%,BlastRadius:15%',
    incident: 'IncidentHistory:35%,ChangeIntelligence:25%,RunbookLibrary:25%,ServiceCatalog:15%',
  };

  const useCaseMap: Record<AssistantContextType, string> = {
    service: 'ServiceLookup',
    contract: 'ContractExplanation',
    change: 'ChangeAnalysis',
    incident: 'IncidentExplanation',
  };

  // Grounded response when contextData is available
  if (contextData && contextStrength !== 'none') {
    const { content, suggestedSteps, contextSummaryText } = buildGroundedContent(contextType, contextData, t);

    const refs: string[] = [`${contextType}:${contextData.entityName}`];
    (contextData.relations ?? []).forEach(r => {
      refs.push(`${r.entityType}:${r.name}`);
    });

    const confidence = contextStrength === 'strong' || contextStrength === 'good' ? 'High' : 'Medium';

    return {
      content,
      useCaseType: useCaseMap[contextType],
      sources,
      refs: refs.slice(0, 8),
      confidence,
      weightSummary: weightMap[contextType],
      contextStrength,
      suggestedSteps,
      caveats: contextData.caveats ?? [],
      contextSummaryText,
    };
  }

  // Fallback: template-based response without rich context
  // Low confidence + weak contextStrength: no entity data available, response is template-only
  const contextLabel = contextSummary.name;
  const statusInfo = contextSummary.status ? ` (${contextSummary.status})` : '';
  const descInfo = contextSummary.description ? ` — ${contextSummary.description}` : '';
  const baseInfo = contextSummary.additionalInfo
    ? Object.entries(contextSummary.additionalInfo)
        .map(([k, v]) => `${k}: ${v}`)
        .join(', ')
    : '';

  const content = `${t(`assistantPanel.response.${contextType}Analysis`)} **${contextLabel}**${statusInfo}${descInfo}. ${baseInfo ? `${t('assistantPanel.response.additionalContext')}: ${baseInfo}. ` : ''}${t(`assistantPanel.response.${contextType}GroundingNote`)}`;

  return {
    content,
    useCaseType: useCaseMap[contextType],
    sources,
    refs: [`${contextType}:${contextLabel}`],
    confidence: 'Low',
    weightSummary: weightMap[contextType],
    contextStrength: 'weak',
    suggestedSteps: [],
    caveats: [t('assistantPanel.noContextWarning')],
    contextSummaryText: '',
  };
}

// ── Suggested prompts per context type ──────────────────────────────

export function getSuggestedPrompts(
  contextType: AssistantContextType,
  contextSummary: ContextSummary,
  t: (key: string, opts?: Record<string, string>) => string,
): string[] {
  const name = contextSummary.name;
  switch (contextType) {
    case 'service':
      return [
        t('assistantPanel.prompts.service.overview', { name }),
        t('assistantPanel.prompts.service.contracts', { name }),
        t('assistantPanel.prompts.service.dependencies', { name }),
        t('assistantPanel.prompts.service.recentChanges', { name }),
      ];
    case 'contract':
      return [
        t('assistantPanel.prompts.contract.explain', { name }),
        t('assistantPanel.prompts.contract.compatibility', { name }),
        t('assistantPanel.prompts.contract.versionDiff', { name }),
        t('assistantPanel.prompts.contract.consumers', { name }),
      ];
    case 'change':
      return [
        t('assistantPanel.prompts.change.riskAnalysis', { name }),
        t('assistantPanel.prompts.change.blastRadius', { name }),
        t('assistantPanel.prompts.change.rollback', { name }),
        t('assistantPanel.prompts.change.correlatedIncidents', { name }),
      ];
    case 'incident':
      return [
        t('assistantPanel.prompts.incident.rootCause', { name }),
        t('assistantPanel.prompts.incident.mitigation', { name }),
        t('assistantPanel.prompts.incident.relatedChanges', { name }),
        t('assistantPanel.prompts.incident.runbook', { name }),
      ];
  }
}
