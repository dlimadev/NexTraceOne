import { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Bot,
  Send,
  Shield,
  Database,
  Link2,
  Info,
  ChevronDown,
  ChevronUp,
  Eye,
  CheckCircle2,
  AlertCircle,
  Sparkles,
  Server,
  FileText,
  AlertTriangle,
  GitBranch,
  Lightbulb,
  ExternalLink,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { aiGovernanceApi } from '../api/aiGovernance';

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

interface ChatMessage {
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
  timestamp: string;
}

interface SuggestedAction {
  label: string;
  type: 'navigate' | 'query' | 'external';
  target: string;
}

// ── Context-specific ground truth sources ───────────────────────────

const contextGroundingSources: Record<AssistantContextType, string[]> = {
  service: ['Service Catalog', 'Contract Registry', 'Dependency Graph', 'Change Intelligence'],
  contract: ['Contract Registry', 'Service Catalog', 'Version History', 'Compatibility Checks'],
  change: ['Change Intelligence', 'Incident History', 'Service Catalog', 'Blast Radius Analysis'],
  incident: ['Incident History', 'Change Intelligence', 'Runbook Library', 'Service Catalog'],
};

// ── Context-specific suggested actions per use case ─────────────────

function buildSuggestedActions(
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

const contextScopeMap: Record<AssistantContextType, string> = {
  service: 'Services',
  contract: 'Contracts',
  change: 'Changes',
  incident: 'Incidents',
};

const contextScopeIcons: Record<AssistantContextType, React.ReactNode> = {
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

function assessContextStrength(contextData?: ContextData): string {
  if (!contextData) return 'none';
  const propCount = Object.keys(contextData.properties ?? {}).length;
  const relCount = (contextData.relations ?? []).length;
  const hasCaveats = (contextData.caveats ?? []).length > 0;
  if (propCount >= CONTEXT_STRONG_MIN_PROPS && relCount >= CONTEXT_STRONG_MIN_RELS && !hasCaveats) return 'strong';
  if (propCount >= CONTEXT_GOOD_MIN_PROPS && relCount >= CONTEXT_GOOD_MIN_RELS) return 'good';
  if (propCount >= 1 || relCount >= 1) return 'partial';
  return 'weak';
}

function buildGroundedContent(
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

function generateContextualResponse(
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

/**
 * Painel de assistente IA contextualizado, integrável em qualquer detail page.
 * Recebe contexto (tipo + id + resumo) e oferece prompts sugeridos,
 * respostas grounded, referências de fonte e ações de follow-up.
 *
 * @see docs/AI-ASSISTED-OPERATIONS.md
 */
export function AssistantPanel({ contextType, contextId, contextSummary, contextData, activeEnvironmentName, isNonProductionEnvironment }: AssistantPanelProps) {
  const { t } = useTranslation();
  const { persona } = usePersona();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [expandedMeta, setExpandedMeta] = useState<string | null>(null);
  const [isOpen, setIsOpen] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [messages, scrollToBottom]);

  // Reset when context changes
  useEffect(() => {
    setMessages([]);
    setIsOpen(false);
    setExpandedMeta(null);
    setInputValue('');
  }, [contextType, contextId]);

  const suggestedPrompts = getSuggestedPrompts(contextType, contextSummary, t);
  const suggestedActions = buildSuggestedActions(contextType, contextId, t);

  const handleOpen = () => {
    if (!isOpen) {
      setIsOpen(true);
      if (messages.length === 0) {
        const welcomeMsg: ChatMessage = {
          id: `w-${Date.now()}`,
          role: 'assistant',
          content: t(`assistantPanel.welcome.${contextType}`, { name: contextSummary.name }),
          modelName: 'NexTrace-Internal-v1',
          provider: 'Internal',
          isInternalModel: true,
          groundingSources: contextGroundingSources[contextType],
          contextReferences: [`${contextType}:${contextSummary.name}`],
          correlationId: `ctx-${contextId}-init`,
          useCaseType: contextType === 'service' ? 'ServiceLookup' : contextType === 'contract' ? 'ContractExplanation' : contextType === 'change' ? 'ChangeAnalysis' : 'IncidentExplanation',
          routingPath: 'InternalOnly',
          confidenceLevel: 'High',
          costClass: 'low',
          routingRationale: t('assistantPanel.meta.contextualRouting'),
          sourceWeightingSummary: '',
          escalationReason: 'None',
          suggestedActions: suggestedActions,
          timestamp: new Date().toISOString(),
        };
        setMessages([welcomeMsg]);
      }
    } else {
      setIsOpen(false);
    }
  };

  const handleSendMessage = (messageText?: string) => {
    const text = messageText || inputValue.trim();
    if (!text) return;

    const userMsg: ChatMessage = {
      id: `u-${Date.now()}`,
      role: 'user',
      content: text,
      timestamp: new Date().toISOString(),
    };
    setMessages(prev => [...prev, userMsg]);
    setInputValue('');
    setIsTyping(true);

    // stopTyping defers to the next macrotask so React can render the typing indicator first.
    const stopTyping = () => new Promise<void>(resolve => setTimeout(resolve, 0));

    // Fire API call (non-blocking, falls back to contextual local response)
    const contextPayload: Record<string, string> = {};
    if (contextType === 'service') contextPayload.serviceId = contextId;
    if (contextType === 'contract') contextPayload.contractId = contextId;
    if (contextType === 'incident') contextPayload.incidentId = contextId;
    if (contextType === 'change') contextPayload.changeId = contextId;

    aiGovernanceApi
      .sendMessage({
        message: text,
        contextScope: contextScopeMap[contextType],
        persona,
        clientType: 'Web',
        ...contextPayload,
        contextBundle: contextData ? JSON.stringify(contextData) : undefined,
      })
      .then(async response => {
        const rawContent: string = response.assistantResponse || response.message || response.content || '';
        const isFallback = rawContent.startsWith('[FALLBACK_PROVIDER_UNAVAILABLE]');
        const displayContent = isFallback
          ? rawContent.replace('[FALLBACK_PROVIDER_UNAVAILABLE]', '').trimStart()
          : rawContent;

        const baseCaveats: string[] = response.contextCaveats || [];
        if (isFallback) baseCaveats.push(t('assistantPanel.fallbackCaveat'));

        const assistantMsg: ChatMessage = {
          id: response.correlationId || `a-${Date.now()}`,
          role: 'assistant',
          content: displayContent,
          modelName: response.modelUsed || response.modelName,
          provider: response.provider,
          isInternalModel: response.isInternalModel ?? true,
          promptTokens: response.promptTokens,
          completionTokens: response.completionTokens,
          appliedPolicyName: response.appliedPolicyName,
          groundingSources: response.groundingSources || [],
          contextReferences: response.contextReferences || [],
          correlationId: response.correlationId,
          useCaseType: response.useCaseType,
          routingPath: isFallback ? 'ProviderFallback' : response.routingPath,
          confidenceLevel: isFallback ? 'Low' : response.confidenceLevel,
          costClass: isFallback ? 'none' : response.costClass,
          routingRationale: response.routingRationale,
          sourceWeightingSummary: response.sourceWeightingSummary,
          escalationReason: isFallback ? 'ProviderUnavailableFallback' : response.escalationReason,
          suggestedActions: suggestedActions,
          suggestedSteps: response.suggestedNextSteps || [],
          caveats: baseCaveats,
          contextSummaryText: response.contextSummary,
          contextStrength: response.contextStrength,
          timestamp: new Date().toISOString(),
        };
        setMessages(prev => [...prev, assistantMsg]);
        await stopTyping();
        setIsTyping(false);
      })
      .catch(async () => {
        // When contextData is available, generate a local grounded response.
        // This ensures entity-specific data is shown even when the provider is unavailable,
        // without silent mock — the fallback indicator is always explicit.
        let content: string;
        let caveats: string[];
        let contextRefs: string[];
        let contextStrength: string;
        let catchSuggestedSteps: string[] = [];
        let catchContextSummaryText = '';
        let confidence = 'Unknown';

        if (contextData) {
          const localResponse = generateContextualResponse(contextType, contextSummary, text, t, contextData);
          content = localResponse.content;
          caveats = [t('assistantPanel.fallbackCaveat'), ...localResponse.caveats];
          contextRefs = localResponse.refs;
          contextStrength = localResponse.contextStrength;
          catchSuggestedSteps = localResponse.suggestedSteps;
          catchContextSummaryText = localResponse.contextSummaryText;
          confidence = localResponse.confidence;
        } else {
          const unavailableText = t('common.errorLoading');
          content = t('assistantPanel.fallbackCaveat');
          caveats = [unavailableText];
          contextRefs = [`${contextType}:${contextSummary.name}`];
          contextStrength = 'weak';
        }

        const assistantMsg: ChatMessage = {
          id: `a-${Date.now()}`,
          role: 'assistant',
          content,
          modelName: null,
          provider: null,
          isInternalModel: true,
          promptTokens: 0,
          completionTokens: 0,
          appliedPolicyName: null,
          groundingSources: contextGroundingSources[contextType],
          contextReferences: contextRefs,
          correlationId: `provider-unavailable-${Date.now()}`,
          useCaseType: contextType === 'service' ? 'ServiceLookup' : contextType === 'contract' ? 'ContractExplanation' : contextType === 'change' ? 'ChangeAnalysis' : 'IncidentExplanation',
          routingPath: 'LocalFallback',
          confidenceLevel: confidence,
          costClass: 'none',
          routingRationale: 'Provider unavailable. Local grounded response used.',
          sourceWeightingSummary: '',
          escalationReason: 'ProviderUnavailable',
          suggestedActions: suggestedActions,
          suggestedSteps: catchSuggestedSteps,
          caveats,
          contextSummaryText: catchContextSummaryText,
          contextStrength,
          timestamp: new Date().toISOString(),
        };
        setMessages(prev => [...prev, assistantMsg]);
        await stopTyping();
        setIsTyping(false);
      });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleSuggestedAction = (action: SuggestedAction) => {
    if (action.type === 'query') {
      handleSendMessage(action.target);
    }
    // 'navigate' and 'external' are handled via the link rendering
  };

  const toggleMeta = (msgId: string) => {
    setExpandedMeta(prev => (prev === msgId ? null : msgId));
  };

  const formatTime = (ts: string) => {
    try {
      return new Date(ts).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return '';
    }
  };

  // ── Collapsed state (button) ──────────────────────────────────────

  if (!isOpen) {
    return (
      <div className="bg-card rounded-lg border border-edge p-4">
        <button
          onClick={handleOpen}
          className="w-full flex items-center gap-3 text-left hover:bg-hover rounded-lg p-2 transition-colors"
          data-testid="assistant-panel-toggle"
        >
          <div className="w-10 h-10 rounded-full bg-accent/20 flex items-center justify-center shrink-0">
            <Bot size={20} className="text-accent" />
          </div>
          <div className="flex-1 min-w-0">
            <h3 className="text-sm font-semibold text-heading">{t('assistantPanel.title')}</h3>
            <p className="text-xs text-muted truncate">
              {t(`assistantPanel.contextHint.${contextType}`, { name: contextSummary.name })}
            </p>
          </div>
          <div className="flex items-center gap-1.5">
            {contextScopeIcons[contextType]}
            <Badge variant="info">
              <div className="flex items-center gap-1">
                <Shield size={10} />
                {t('aiHub.internalAi')}
              </div>
            </Badge>
          </div>
        </button>

        {/* ── Suggested prompts (collapsed preview) ───────────────────── */}
        <div className="mt-3 space-y-1.5">
          <div className="flex items-center gap-1.5">
            <Sparkles size={12} className="text-accent" />
            <span className="text-xs font-medium text-muted">{t('assistantPanel.quickQuestions')}</span>
          </div>
          {suggestedPrompts.slice(0, 3).map((prompt, idx) => (
            <button
              key={idx}
              onClick={() => {
                setIsOpen(true);
                if (messages.length === 0) {
                  const welcomeMsg: ChatMessage = {
                    id: `w-${Date.now()}`,
                    role: 'assistant',
                    content: t(`assistantPanel.welcome.${contextType}`, { name: contextSummary.name }),
                    modelName: 'NexTrace-Internal-v1',
                    provider: 'Internal',
                    isInternalModel: true,
                    groundingSources: contextGroundingSources[contextType],
                    contextReferences: [`${contextType}:${contextSummary.name}`],
                    correlationId: `ctx-${contextId}-init`,
                    timestamp: new Date().toISOString(),
                  };
                  setMessages([welcomeMsg]);
                }
                setTimeout(() => handleSendMessage(prompt), 100);
              }}
              className="w-full text-left px-3 py-1.5 rounded border border-edge text-xs text-body hover:bg-hover hover:border-accent/30 transition-colors"
              data-testid={`suggested-prompt-${idx}`}
            >
              {prompt}
            </button>
          ))}
        </div>
      </div>
    );
  }

  // ── Expanded state (chat panel) ───────────────────────────────────

  return (
    <div className="bg-card rounded-lg border border-edge flex flex-col" style={{ maxHeight: '600px' }} data-testid="assistant-panel-expanded">
      {/* ── Header ────────────────────────────────────────────────────── */}
      <div className="px-4 py-3 border-b border-edge flex items-center justify-between shrink-0">
        <div className="flex items-center gap-2">
          <Bot size={18} className="text-accent" />
          <div>
            <h3 className="text-sm font-semibold text-heading">{t('assistantPanel.title')}</h3>
            <p className="text-[10px] text-muted">
              {t(`assistantPanel.contextLabel.${contextType}`)}: {contextSummary.name}
            </p>
            {isNonProductionEnvironment && activeEnvironmentName && (
              <div className="flex items-center gap-1.5 px-2 py-1 rounded text-xs bg-yellow-500/10 text-yellow-300 border border-yellow-500/20 mt-2">
                <AlertTriangle size={11} />
                <span>{t('assistantPanel.analyzingNonProd', { environment: activeEnvironmentName })}</span>
              </div>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="info">
            {contextScopeIcons[contextType]}
            <span className="ml-1">{t(`aiHub.context${contextScopeMap[contextType]}`)}</span>
          </Badge>
          <button onClick={() => setIsOpen(false)} className="text-muted hover:text-body transition-colors p-1" data-testid="assistant-panel-close">
            <ChevronDown size={16} />
          </button>
        </div>
      </div>

      {/* ── Messages ──────────────────────────────────────────────────── */}
      <div className="flex-1 overflow-y-auto px-4 py-3 space-y-3">
        {messages.map(msg => (
          <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            <div
              className={`max-w-[85%] rounded-lg px-3 py-2.5 ${
                msg.role === 'assistant' ? 'bg-elevated' : 'bg-accent/20'
              }`}
            >
              {/* Header */}
              {msg.role === 'assistant' && (
                <div className="flex items-center gap-1.5 mb-1.5 flex-wrap">
                  <Bot size={12} className="text-accent" />
                  <span className="text-[10px] font-medium text-accent">{t('aiHub.assistant')}</span>
                  {msg.confidenceLevel && (
                    <Badge
                      variant={
                        msg.confidenceLevel === 'High' ? 'success' : msg.confidenceLevel === 'Medium' ? 'warning' : 'default'
                      }
                    >
                      <div className="flex items-center gap-0.5">
                        <CheckCircle2 size={8} aria-hidden="true" />
                        {msg.confidenceLevel === 'High'
                          ? t('aiHub.trustGrounded')
                          : msg.confidenceLevel === 'Medium'
                            ? t('aiHub.trustPartialContext')
                            : t('aiHub.trustLimitedContext')}
                      </div>
                    </Badge>
                  )}
                  {msg.isInternalModel && (
                    <Badge variant="info">
                      <div className="flex items-center gap-0.5">
                        <Shield size={8} />
                        {t('aiHub.trustInternalOnly')}
                      </div>
                    </Badge>
                  )}
                  {msg.escalationReason && msg.escalationReason !== 'None' && (
                    <Badge variant="warning">
                      <div className="flex items-center gap-0.5">
                        <AlertCircle size={8} aria-hidden="true" />
                        {t('aiHub.trustExternalUsed')}
                      </div>
                    </Badge>
                  )}
                  {msg.contextStrength && (
                    <Badge
                      variant={
                        msg.contextStrength === 'strong'
                          ? 'success'
                          : msg.contextStrength === 'good'
                            ? 'info'
                            : msg.contextStrength === 'partial'
                              ? 'warning'
                              : 'default'
                      }
                    >
                      {t(`assistantPanel.contextStrength.${msg.contextStrength}`)}
                    </Badge>
                  )}
                  <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
                </div>
              )}
              {msg.role === 'user' && (
                <div className="flex items-center gap-1.5 mb-1 justify-end">
                  <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
                  <span className="text-[10px] font-medium text-body">{t('aiHub.you')}</span>
                </div>
              )}

              {/* Content */}
              <p className="text-xs text-body whitespace-pre-wrap leading-relaxed">{msg.content}</p>

              {/* Grounding Sources */}
              {msg.groundingSources && msg.groundingSources.length > 0 && (
                <div className="mt-2 flex items-center gap-1 flex-wrap">
                  <Database size={10} className="text-muted shrink-0" />
                  <span className="text-[10px] text-muted">{t('aiHub.groundingSources')}:</span>
                  {msg.groundingSources.map(src => (
                    <Badge key={src} variant="default">
                      {src}
                    </Badge>
                  ))}
                </div>
              )}

              {/* Context References */}
              {msg.contextReferences && msg.contextReferences.length > 0 && (
                <div className="mt-1 flex items-center gap-1 flex-wrap">
                  <Link2 size={10} className="text-muted shrink-0" />
                  <span className="text-[10px] text-muted">{t('aiHub.contextRefs')}:</span>
                  {msg.contextReferences.map(ref => (
                    <span key={ref} className="text-[10px] text-accent bg-accent/10 px-1.5 py-0.5 rounded">
                      {ref}
                    </span>
                  ))}
                </div>
              )}

              {/* Context Summary */}
              {msg.contextSummaryText && (
                <div className="mt-1.5">
                  <span className="text-[10px] text-muted italic">
                    {t('assistantPanel.contextSummaryLabel')}: {msg.contextSummaryText}
                  </span>
                </div>
              )}

              {/* Caveats */}
              {msg.caveats && msg.caveats.length > 0 && (
                <div className="mt-2 p-2 rounded bg-amber-900/20 border border-amber-700/30">
                  <span className="text-[10px] font-medium text-amber-300">{t('assistantPanel.caveatsLabel')}:</span>
                  <ul className="mt-0.5 space-y-0.5">
                    {msg.caveats.map((caveat, idx) => (
                      <li key={idx} className="text-[10px] text-amber-200/80">• {caveat}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Suggested Actions */}
              {msg.role === 'assistant' && msg.suggestedActions && msg.suggestedActions.length > 0 && (
                <div className="mt-2 pt-2 border-t border-edge">
                  <div className="flex items-center gap-1 mb-1.5">
                    <Lightbulb size={10} className="text-accent" />
                    <span className="text-[10px] font-medium text-muted">{t('assistantPanel.suggestedActions')}</span>
                  </div>
                  <div className="space-y-1">
                    {msg.suggestedActions.map((action, idx) => (
                      <button
                        key={idx}
                        onClick={() => handleSuggestedAction(action)}
                        className="flex items-center gap-1.5 w-full text-left px-2 py-1 rounded text-[10px] text-accent hover:bg-accent/10 transition-colors"
                        data-testid={`action-${idx}`}
                      >
                        {action.type === 'navigate' ? (
                          <ExternalLink size={10} />
                        ) : (
                          <Send size={10} />
                        )}
                        {action.label}
                      </button>
                    ))}
                  </div>
                </div>
              )}

              {/* Suggested Steps (from grounded response) */}
              {msg.role === 'assistant' && msg.suggestedSteps && msg.suggestedSteps.length > 0 && (
                <div className="mt-2 pt-2 border-t border-edge">
                  <div className="flex items-center gap-1 mb-1">
                    <CheckCircle2 size={10} className="text-emerald-400" />
                    <span className="text-[10px] font-medium text-muted">{t('assistantPanel.suggestedSteps')}</span>
                  </div>
                  <ul className="space-y-0.5">
                    {msg.suggestedSteps.map((step, idx) => (
                      <li key={idx} className="text-[10px] text-body pl-3">• {step}</li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Metadata toggle */}
              {msg.role === 'assistant' && msg.correlationId && (
                <div className="mt-1.5">
                  <button
                    onClick={() => toggleMeta(msg.id)}
                    className="flex items-center gap-1 text-[10px] text-muted hover:text-body transition-colors"
                    data-testid={`meta-toggle-${msg.id}`}
                  >
                    <Eye size={10} />
                    {t('aiHub.responseMetadata')}
                    {expandedMeta === msg.id ? <ChevronUp size={10} /> : <ChevronDown size={10} />}
                  </button>

                  {expandedMeta === msg.id && (
                    <div className="mt-1.5 p-2 rounded bg-canvas border border-edge text-[10px] space-y-1" data-testid="response-metadata">
                      <div className="grid grid-cols-2 gap-x-3 gap-y-0.5">
                        <span className="text-muted">{t('aiHub.metaModel')}:</span>
                        <span className="text-body">{msg.modelName ?? '—'}</span>
                        <span className="text-muted">{t('aiHub.metaProvider')}:</span>
                        <span className="text-body">{msg.provider ?? '—'}</span>
                        <span className="text-muted">{t('aiHub.metaModelType')}:</span>
                        <span className="text-body">
                          {msg.isInternalModel ? (
                            <span className="text-success">{t('aiHub.internalLabel')}</span>
                          ) : (
                            <span className="text-warning">{t('aiHub.externalLabel')}</span>
                          )}
                        </span>
                        <span className="text-muted">{t('aiHub.metaUseCase')}:</span>
                        <span className="text-body">{msg.useCaseType ?? '—'}</span>
                        <span className="text-muted">{t('aiHub.metaConfidence')}:</span>
                        <span className="text-body">{msg.confidenceLevel ?? '—'}</span>
                        <span className="text-muted">{t('aiHub.metaSourceWeights')}:</span>
                        <span className="text-body">{msg.sourceWeightingSummary ?? '—'}</span>
                        <span className="text-muted">{t('aiHub.metaCorrelation')}:</span>
                        <span className="text-body font-mono">{msg.correlationId}</span>
                      </div>
                      {msg.routingRationale && (
                        <div className="mt-1 pt-1 border-t border-edge">
                          <span className="text-muted">{t('aiHub.metaRoutingRationale')}:</span>
                          <p className="text-body mt-0.5">{msg.routingRationale}</p>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}

        {/* Typing indicator */}
        {isTyping && (
          <div className="flex justify-start">
            <div className="bg-elevated rounded-lg px-3 py-2 flex items-center gap-2" data-testid="typing-indicator">
              <Bot size={12} className="text-accent" />
              <span className="text-[10px] text-muted animate-pulse">{t('aiHub.typing')}</span>
              <span className="flex gap-0.5">
                <span className="w-1 h-1 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                <span className="w-1 h-1 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                <span className="w-1 h-1 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
              </span>
            </div>
          </div>
        )}

        {/* Suggested prompts (shown when few messages) */}
        {messages.length <= 2 && (
          <div className="pt-2">
            <div className="flex items-center gap-1.5 mb-1.5">
              <Sparkles size={12} className="text-accent" />
              <p className="text-[10px] font-medium text-heading">{t('assistantPanel.tryAsking')}</p>
            </div>
            <div className="space-y-1">
              {suggestedPrompts.map((prompt, idx) => (
                <button
                  key={idx}
                  onClick={() => handleSendMessage(prompt)}
                  className="w-full text-left px-2.5 py-1.5 rounded border border-edge text-[10px] text-body hover:bg-hover hover:border-accent/30 transition-colors"
                  data-testid={`chat-prompt-${idx}`}
                >
                  {prompt}
                </button>
              ))}
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* ── Input ─────────────────────────────────────────────────────── */}
      <div className="px-4 py-3 border-t border-edge shrink-0">
        <div className="flex items-center gap-2">
          <input
            type="text"
            value={inputValue}
            onChange={e => setInputValue(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={t(`assistantPanel.placeholder.${contextType}`)}
            className="flex-1 bg-elevated border border-edge rounded-lg px-3 py-2 text-xs text-body placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
            disabled={isTyping}
            data-testid="assistant-input"
          />
          <Button variant="primary" size="sm" disabled={!inputValue.trim() || isTyping} onClick={() => handleSendMessage()}>
            <Send size={14} />
          </Button>
        </div>
        <div className="flex items-center gap-1.5 mt-1.5 text-[10px] text-faded">
          <Info size={8} />
          <span>{t('aiHub.governanceNotice')}</span>
        </div>
      </div>
    </div>
  );
}

// ── Suggested prompts per context type ──────────────────────────────

function getSuggestedPrompts(
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
