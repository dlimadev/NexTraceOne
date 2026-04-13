import { memo } from 'react';
import { Eye, Play } from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import {
  humanizeEnumValue, ownershipIcon, visibilityIcon, statusVariant,
} from './AiAgentTypes';
import type { AgentCardProps } from './AiAgentTypes';

/**
 * Card de visualização de um AI Agent — exibe metadados, status e acções.
 */
export const AgentCard = memo(function AgentCard({ agent, onView, onExecute, t }: AgentCardProps) {
  return (
    <div className="bg-card border border-edge rounded-lg p-4 hover:border-accent/30 transition-colors">
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center text-sm">
            {agent.icon || <span className="text-accent">🤖</span>}
          </div>
          <div>
            <h3 className="text-sm font-semibold text-heading leading-tight">{agent.displayName}</h3>
            <span className="text-[10px] text-muted">{agent.slug}</span>
          </div>
        </div>
        <Badge variant={statusVariant(agent.publicationStatus)}>
          {t(`agents.status.${agent.publicationStatus}`)}
        </Badge>
      </div>

      <p className="text-xs text-muted line-clamp-2 mb-3">{agent.description}</p>

      <div className="flex items-center gap-2 flex-wrap mb-3">
        <Badge variant="default">
          {ownershipIcon(agent.ownershipType)}
          <span className="ml-0.5">{t(`agents.ownership.${agent.ownershipType}`)}</span>
        </Badge>
        <Badge variant="default">
          {visibilityIcon(agent.visibility)}
          <span className="ml-0.5">{t(`agents.visibility.${agent.visibility}`)}</span>
        </Badge>
        <Badge variant="info">{t(`agents.category.${agent.category}`) || humanizeEnumValue(agent.category)}</Badge>
      </div>

      <div className="flex items-center justify-between text-[10px] text-muted mb-3">
        <span>v{agent.version}</span>
        <span>{agent.executionCount} {t('agents.executions')}</span>
      </div>

      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" onClick={onView} className="flex-1">
          <Eye size={12} className="mr-1" /> {t('agents.view')}
        </Button>
        {agent.isActive && (agent.publicationStatus === 'Published' || agent.publicationStatus === 'Active') && (
          <Button variant="primary" size="sm" onClick={onExecute} className="flex-1">
            <Play size={12} className="mr-1" /> {t('agents.execute')}
          </Button>
        )}
      </div>
    </div>
  );
});
