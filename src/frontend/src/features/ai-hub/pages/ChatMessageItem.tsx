import { useTranslation } from 'react-i18next';
import {
  Bot,
  Shield,
  Cpu,
  Eye,
  ChevronDown,
  ChevronUp,
  Database,
  Link2,
  CheckCircle2,
  AlertCircle,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import type { ChatMessage } from './AiAssistantTypes';

interface Props {
  message: ChatMessage;
  isExpanded: boolean;
  onToggleMeta: (msgId: string) => void;
  formatTime: (ts: string) => string;
}

export function ChatMessageItem({ message: msg, isExpanded, onToggleMeta, formatTime }: Props) {
  const { t } = useTranslation();

  return (
    <div className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[75%] rounded-lg px-4 py-3 ${
          msg.role === 'assistant' ? 'bg-elevated' : 'bg-accent/20'
        }`}
      >
        {/* ── Header ────────────────────────────────────────── */}
        {msg.role === 'assistant' && (
          <div className="flex items-center gap-2 mb-2 flex-wrap">
            <Bot size={14} className="text-accent" />
            <span className="text-xs font-medium text-accent">{t('aiHub.assistant')}</span>
            {msg.responseState === 'Degraded' && (
              <Badge variant="warning">
                <div className="flex items-center gap-1">
                  <AlertCircle size={10} aria-hidden="true" />
                  {t('aiHub.responseStateDegraded')}
                </div>
              </Badge>
            )}
            {msg.degradedReason === 'ProviderUnavailable' && (
              <Badge variant="warning">{t('aiHub.providerUnavailable')}</Badge>
            )}
            {msg.modelName && (
              <Badge variant={msg.isDegraded ? 'warning' : msg.isInternalModel ? 'info' : 'warning'}>
                <div className="flex items-center gap-1">
                  <Cpu size={10} />
                  {msg.modelName}
                </div>
              </Badge>
            )}
            {msg.confidenceLevel && (
              <Badge
                variant={
                  msg.confidenceLevel === 'High'
                    ? 'success'
                    : msg.confidenceLevel === 'Medium'
                      ? 'warning'
                      : 'default'
                }
              >
                <div className="flex items-center gap-1">
                  <CheckCircle2 size={10} aria-hidden="true" />
                  {msg.confidenceLevel === 'High'
                    ? t('aiHub.trustGrounded')
                    : msg.confidenceLevel === 'Medium'
                      ? t('aiHub.trustPartialContext')
                      : t('aiHub.trustLimitedContext')}
                </div>
              </Badge>
            )}
            {msg.isInternalModel && !msg.isDegraded && (
              <Badge variant="info">
                <div className="flex items-center gap-1">
                  <Shield size={10} />
                  {t('aiHub.trustInternalOnly')}
                </div>
              </Badge>
            )}
            {msg.escalationReason && msg.escalationReason !== 'None' && !msg.isDegraded && (
              <Badge variant="warning">
                <div className="flex items-center gap-1">
                  <AlertCircle size={10} aria-hidden="true" />
                  {t('aiHub.trustExternalUsed')}
                </div>
              </Badge>
            )}
            <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
          </div>
        )}
        {msg.role === 'user' && (
          <div className="flex items-center gap-2 mb-1 justify-end">
            <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
            <span className="text-xs font-medium text-body">{t('aiHub.you')}</span>
          </div>
        )}

        {/* ── Content ──────────────────────────────────────── */}
        <p className="text-sm text-body whitespace-pre-wrap">{msg.content}</p>

        {/* ── Grounding Sources ────────────────────────────── */}
        {msg.groundingSources && msg.groundingSources.length > 0 && (
          <div className="mt-2 flex items-center gap-1 flex-wrap">
            <Database size={12} className="text-muted shrink-0" />
            <span className="text-xs text-muted">{t('aiHub.groundingSources')}:</span>
            {msg.groundingSources.map(src => (
              <Badge key={src} variant="default">
                {src}
              </Badge>
            ))}
          </div>
        )}

        {/* ── Context References ───────────────────────────── */}
        {msg.contextReferences && msg.contextReferences.length > 0 && (
          <div className="mt-1.5 flex items-center gap-1 flex-wrap">
            <Link2 size={12} className="text-muted shrink-0" />
            <span className="text-xs text-muted">{t('aiHub.contextRefs')}:</span>
            {msg.contextReferences.map(ref => (
              <span key={ref} className="text-xs text-accent bg-accent/10 px-1.5 py-0.5 rounded">
                {ref}
              </span>
            ))}
          </div>
        )}

        {/* ── Metadata toggle ──────────────────────────────── */}
        {msg.role === 'assistant' && msg.correlationId && (
          <div className="mt-2">
            <button
              onClick={() => onToggleMeta(msg.id)}
              className="flex items-center gap-1 text-xs text-muted hover:text-body transition-colors"
            >
              <Eye size={12} />
              {t('aiHub.responseMetadata')}
              {isExpanded ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
            </button>

            {isExpanded && (
              <div className="mt-2 p-3 rounded-md bg-canvas border border-edge text-xs space-y-1.5">
                <div className="grid grid-cols-2 gap-x-4 gap-y-1">
                  <span className="text-muted">{t('aiHub.metaResponseState')}:</span>
                  <span className="text-body">{msg.responseState ?? t('aiHub.responseStateCompleted')}</span>
                  <span className="text-muted">{t('aiHub.metaDegradedReason')}:</span>
                  <span className="text-body">{msg.degradedReason ? t(`aiHub.degradedReason${msg.degradedReason}`) : '—'}</span>
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
                  <span className="text-muted">{t('aiHub.metaPromptTokens')}:</span>
                  <span className="text-body">{msg.promptTokens ?? 0}</span>
                  <span className="text-muted">{t('aiHub.metaCompletionTokens')}:</span>
                  <span className="text-body">{msg.completionTokens ?? 0}</span>
                  <span className="text-muted">{t('aiHub.metaPolicy')}:</span>
                  <span className="text-body">{msg.appliedPolicyName ?? t('aiHub.metaNoneApplied')}</span>
                  <span className="text-muted">{t('aiHub.metaCorrelation')}:</span>
                  <span className="text-body font-mono text-[10px]">{msg.correlationId}</span>
                  <span className="text-muted">{t('aiHub.metaUseCase')}:</span>
                  <span className="text-body">{msg.useCaseType ?? '—'}</span>
                  <span className="text-muted">{t('aiHub.metaRoutingPath')}:</span>
                  <span className="text-body">{msg.routingPath ?? '—'}</span>
                  <span className="text-muted">{t('aiHub.metaConfidence')}:</span>
                  <span className="text-body">{msg.confidenceLevel ?? '—'}</span>
                  <span className="text-muted">{t('aiHub.metaCostClass')}:</span>
                  <span className="text-body">{msg.costClass ?? '—'}</span>
                  <span className="text-muted">{t('aiHub.metaSourceWeights')}:</span>
                  <span className="text-body">{msg.sourceWeightingSummary ?? '—'}</span>
                  <span className="text-muted">{t('aiHub.metaEscalation')}:</span>
                  <span className="text-body">{msg.escalationReason ?? '—'}</span>
                </div>
                {msg.routingRationale && (
                  <div className="mt-2 pt-2 border-t border-edge">
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
