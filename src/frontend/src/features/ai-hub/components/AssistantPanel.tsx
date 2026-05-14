import { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Bot,
  Send,
  Shield,
  Info,
  ChevronDown,
  Sparkles,
  AlertTriangle,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { aiGovernanceApi } from '../api/aiGovernance';
import { mapAiError } from '../../../utils/apiErrors';
import { AssistantMessageBubble } from './AssistantMessageBubble';
import {
  contextGroundingSources,
  buildSuggestedActions,
  contextScopeMap,
  contextScopeIcons,
  generateContextualResponse,
  getSuggestedPrompts,
} from './AssistantPanelTypes';
import type {
  AssistantPanelProps,
  ChatMessage,
  SuggestedAction,
} from './AssistantPanelTypes';

// Re-export types for backward compatibility
export type { AssistantContextType, ContextSummary, ContextData, ContextDataRelation, AssistantPanelProps } from './AssistantPanelTypes';

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
          id: `w-${crypto.randomUUID()}`,
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
      id: `u-${crypto.randomUUID()}`,
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
          // E-M05: mapear document search hits para explainability hints
          explainabilityHints: Array.isArray(response.documentSearchHits)
            ? response.documentSearchHits.map((h: {documentId?: string; sourceId?: string; title?: string; sourceType?: string; classification?: string; relevanceScore?: number; snippet?: string}) => ({
                sourceId: h.documentId ?? h.sourceId ?? '',
                title: h.title ?? '',
                sourceType: h.classification ?? h.sourceType ?? '',
                relevanceScore: h.relevanceScore ?? 0,
                snippet: h.snippet,
              }))
            : undefined,
          timestamp: new Date().toISOString(),
        };
        setMessages(prev => [...prev, assistantMsg]);
        await stopTyping();
        setIsTyping(false);
      })
      .catch(async (err: unknown) => {
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
          const unavailableText = mapAiError(err, 'chat');
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
              <div className="flex items-center gap-1.5 px-2 py-1 rounded text-xs bg-warning/15 text-warning border border-warning/25 mt-2">
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
          <AssistantMessageBubble
            key={msg.id}
            message={msg}
            isExpanded={expandedMeta === msg.id}
            onToggleMeta={toggleMeta}
            formatTime={formatTime}
            onSuggestedAction={handleSuggestedAction}
            onExplainResponse={(msgId) => {
              const explainPrompt = t('aiHub.explainPrompt');
              handleSendMessage(explainPrompt);
              void msgId;
            }}
          />
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

