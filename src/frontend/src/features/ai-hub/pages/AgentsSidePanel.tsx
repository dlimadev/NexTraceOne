import { useTranslation } from 'react-i18next';
import {
  ChevronUp,
  Zap,
  Users,
  User,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import type { AgentItem } from './AiAssistantTypes';

interface Props {
  agents: AgentItem[];
  isOpen: boolean;
  onClose: () => void;
}

export function AgentsSidePanel({ agents, isOpen, onClose }: Props) {
  const { t } = useTranslation();

  if (!isOpen || agents.length === 0) {
    return null;
  }

  return (
    <div className="w-[280px] shrink-0 bg-card rounded-lg border border-edge flex flex-col">
      <div className="px-4 py-3 border-b border-edge flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Users size={16} className="text-accent" />
          <h2 className="text-sm font-semibold text-heading">{t('aiHub.agentsPanel')}</h2>
        </div>
        <button
          onClick={onClose}
          className="text-muted hover:text-body transition-colors"
        >
          <ChevronUp size={14} />
        </button>
      </div>
      <div className="flex-1 overflow-y-auto">
        {agents.map(agent => (
          <div
            key={agent.agentId}
            className="px-4 py-3 border-b border-edge hover:bg-hover transition-colors"
          >
            <div className="flex items-start gap-2.5">
              <span className="text-lg leading-none mt-0.5">{agent.icon || '🤖'}</span>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1.5">
                  <span className="text-sm font-medium text-heading truncate">{agent.displayName}</span>
                  {agent.isOfficial && (
                    <Badge variant="info">
                      <Zap size={8} className="mr-0.5" />
                      {t('aiHub.officialAgent')}
                    </Badge>
                  )}
                </div>
                <p className="text-[11px] text-muted mt-0.5 line-clamp-2">{agent.description}</p>
                <div className="flex items-center gap-1.5 mt-1.5 flex-wrap">
                  <Badge variant="default">
                    {agent.category}
                  </Badge>
                  {agent.targetPersona && (
                    <span className="text-[10px] text-muted flex items-center gap-0.5">
                      <User size={8} />
                      {agent.targetPersona}
                    </span>
                  )}
                </div>
                {agent.capabilities && (
                  <div className="flex items-center gap-1 mt-1.5 flex-wrap">
                    {agent.capabilities.split(',').slice(0, 3).map(cap => (
                      <span key={cap} className="inline-flex items-center px-1.5 py-0.5 rounded text-[9px] bg-elevated text-muted">
                        {cap.trim()}
                      </span>
                    ))}
                    {agent.capabilities.split(',').length > 3 && (
                      <span className="text-[9px] text-muted">+{agent.capabilities.split(',').length - 3}</span>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
      <div className="px-4 py-2 border-t border-edge">
        <p className="text-[10px] text-muted text-center">{t('aiHub.agentsHint')}</p>
      </div>
    </div>
  );
}
