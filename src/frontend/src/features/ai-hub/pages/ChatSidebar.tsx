import { useTranslation } from 'react-i18next';
import {
  MessageSquare,
  Plus,
  Cpu,
  Tag,
  AlertCircle,
  Loader2,
  Inbox,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import type { Conversation } from './AiAssistantTypes';

interface Props {
  conversations: Conversation[];
  selectedConversation: string | null;
  isLoadingConversations: boolean;
  conversationsError: string | null;
  onNewConversation: () => void;
  onSelectConversation: (id: string) => void;
  onRetry: () => void;
}

export function ChatSidebar({
  conversations,
  selectedConversation,
  isLoadingConversations,
  conversationsError,
  onNewConversation,
  onSelectConversation,
  onRetry,
}: Props) {
  const { t } = useTranslation();

  return (
    <div className="w-[300px] shrink-0 bg-card rounded-lg border border-edge flex flex-col">
      <div className="px-4 py-3 border-b border-edge flex items-center justify-between">
        <h2 className="text-sm font-semibold text-heading">{t('aiHub.conversations')}</h2>
        <Button variant="ghost" size="sm" onClick={onNewConversation}>
          <Plus size={16} />
        </Button>
      </div>
      <div className="flex-1 overflow-y-auto">
        {/* ── Loading state ─────────────────────────────────────────── */}
        {isLoadingConversations && (
          <div className="flex items-center justify-center py-8">
            <Loader2 size={20} className="animate-spin text-muted" />
          </div>
        )}

        {/* ── Error state ───────────────────────────────────────────── */}
        {conversationsError && !isLoadingConversations && (
          <div className="px-4 py-6 text-center">
            <AlertCircle size={24} className="text-warning mx-auto mb-2" />
            <p className="text-sm text-muted">{conversationsError}</p>
            <Button variant="ghost" size="sm" className="mt-2" onClick={onRetry}>
              {t('common.retry')}
            </Button>
          </div>
        )}

        {/* ── Empty state ───────────────────────────────────────────── */}
        {!isLoadingConversations && !conversationsError && conversations.length === 0 && (
          <div className="px-4 py-8 text-center">
            <Inbox size={32} className="text-muted mx-auto mb-3" />
            <p className="text-sm text-heading mb-1">{t('aiHub.noConversations')}</p>
            <p className="text-xs text-muted mb-4">{t('aiHub.startNewConversation')}</p>
            <Button variant="primary" size="sm" onClick={onNewConversation}>
              <Plus size={14} className="mr-1" />
              {t('aiHub.newConversation')}
            </Button>
          </div>
        )}

        {/* ── Conversations list ────────────────────────────────────── */}
        {!isLoadingConversations && !conversationsError && conversations.map(conv => (
          <button
            key={conv.id}
            onClick={() => onSelectConversation(conv.id)}
            className={`w-full text-left px-4 py-3 border-b border-edge transition-colors ${
              selectedConversation === conv.id ? 'bg-hover' : 'hover:bg-hover'
            }`}
          >
            <div className="flex items-center gap-2 mb-1">
              <MessageSquare size={14} className="text-muted shrink-0" />
              <span className="text-sm font-medium text-heading truncate">{conv.title}</span>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted mt-1">
              <span>
                {conv.messageCount} {t('aiHub.messages')}
              </span>
              <span>·</span>
              <span>{conv.persona}</span>
            </div>
            <div className="mt-1.5 flex items-center gap-1.5 flex-wrap">
              <Badge variant={conv.isActive ? 'success' : 'default'}>
                {conv.isActive ? t('aiHub.statusActive') : t('aiHub.statusArchived')}
              </Badge>
              {conv.lastModelUsed && (
                <Badge variant="info">
                  <Cpu size={10} className="mr-0.5" />
                  {t('aiHub.internalLabel')}
                </Badge>
              )}
            </div>
            {conv.tags && (
              <div className="mt-1.5 flex items-center gap-1 flex-wrap">
                {conv.tags.split(',').map(tag => (
                  <span key={tag} className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[10px] bg-elevated text-muted">
                    <Tag size={8} />
                    {tag.trim()}
                  </span>
                ))}
              </div>
            )}
          </button>
        ))}
      </div>
    </div>
  );
}
