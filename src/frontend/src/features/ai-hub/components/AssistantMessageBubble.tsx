import { useTranslation } from 'react-i18next';
import {
  Bot,
  Shield,
  Database,
  Link2,
  Eye,
  ChevronDown,
  ChevronUp,
  CheckCircle2,
  AlertCircle,
  Lightbulb,
  ExternalLink,
  Send,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import type { ChatMessage, SuggestedAction } from './AssistantPanelTypes';

interface AssistantMessageBubbleProps {
  message: ChatMessage;
  isExpanded: boolean;
  onToggleMeta: (id: string) => void;
  formatTime: (ts: string) => string;
  onSuggestedAction: (action: SuggestedAction) => void;
}

export function AssistantMessageBubble({ message: msg, isExpanded, onToggleMeta, formatTime, onSuggestedAction }: AssistantMessageBubbleProps) {
  const { t } = useTranslation();

  return (
    <div className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
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
          <div className="mt-2 p-2 rounded bg-warning/15 border border-warning/25">
            <span className="text-[10px] font-medium text-warning">{t('assistantPanel.caveatsLabel')}:</span>
            <ul className="mt-0.5 space-y-0.5">
              {msg.caveats.map((caveat, idx) => (
                <li key={idx} className="text-[10px] text-warning">• {caveat}</li>
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
                  onClick={() => onSuggestedAction(action)}
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
              <CheckCircle2 size={10} className="text-success" />
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
              onClick={() => onToggleMeta(msg.id)}
              className="flex items-center gap-1 text-[10px] text-muted hover:text-body transition-colors"
              data-testid={`meta-toggle-${msg.id}`}
            >
              <Eye size={10} />
              {t('aiHub.responseMetadata')}
              {isExpanded ? <ChevronUp size={10} /> : <ChevronDown size={10} />}
            </button>

            {isExpanded && (
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
  );
}
