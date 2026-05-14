import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { EmptyState } from '../../../components/EmptyState';
import {
  Bot, Plus, Search, Shield, Sparkles, AlertCircle, Inbox,
} from 'lucide-react';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageErrorState } from '../../../components/PageErrorState';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { useToast } from '../../../components/Toast';
import { aiGovernanceApi } from '../api/aiGovernance';
import { FALLBACK_AGENT_CATEGORIES } from './AiAgentTypes';
import { AgentCard } from './AiAgentCard';
import { CreateAgentDialog, ExecuteAgentDialog } from './AiAgentDialogs';
import type { AgentListItem, AgentsResponse } from './AiAgentTypes';

/**
 * Página de gestão de AI Agents — catálogo oficial, agents de utilizador,
 * execução governada e revisão de artefactos.
 *
 * @see docs/AI-ARCHITECTURE.md
 */
export function AiAgentsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { toastSuccess } = useToast();

  const [searchTerm, setSearchTerm] = useState('');
  const [filter, setFilter] = useState<'all' | 'official' | 'custom'>('all');
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [executeAgent, setExecuteAgent] = useState<AgentListItem | null>(null);

  const isOfficial = filter === 'official' ? true : filter === 'custom' ? false : undefined;

  const agentsQuery = useQuery({
    queryKey: ['ai-agents', isOfficial],
    queryFn: async () => aiGovernanceApi.listAgents({ isOfficial }),
    staleTime: 20_000,
  }) as { data?: AgentsResponse; isLoading: boolean; isError: boolean; refetch: () => void };

  const agentCategoriesQuery = useQuery({
    queryKey: ['ai-agent-categories'],
    queryFn: () => aiGovernanceApi.listAgentCategories(),
    staleTime: 5 * 60_000,
  });

  const agents = useMemo(() => agentsQuery.data?.items ?? [], [agentsQuery.data?.items]);

  const availableCategories = useMemo(() => {
    const fromEndpoint = agentCategoriesQuery.data?.items ?? [];
    const fromAgents = agents.map((agent) => agent.category);
    const merged = [...fromEndpoint, ...fromAgents, ...FALLBACK_AGENT_CATEGORIES]
      .filter((value): value is string => Boolean(value && value.trim()));
    return Array.from(new Set(merged));
  }, [agentCategoriesQuery.data?.items, agents]);

  const defaultCategory = availableCategories[0] ?? 'General';

  const handleAgentCreated = async () => {
    await agentsQuery.refetch();
    toastSuccess(t('agents.createSuccess'));
  };

  const filteredAgents = agents.filter(a => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      a.displayName.toLowerCase().includes(term) ||
      a.name.toLowerCase().includes(term) ||
      a.description.toLowerCase().includes(term) ||
      a.category.toLowerCase().includes(term)
    );
  });

  const officialAgents = filteredAgents.filter(a => a.ownershipType === 'System');
  const customAgents = filteredAgents.filter(a => a.ownershipType !== 'System');

  if (agentsQuery.isError) {
    return <PageContainer><PageErrorState /></PageContainer>;
  }

  return (
    <PageContainer>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
              <Bot size={24} className="text-accent" />
              {t('agents.title')}
            </h1>
            <p className="text-sm text-muted mt-1">{t('agents.subtitle')}</p>
          </div>
          <Button variant="primary" size="sm" onClick={() => setIsCreateOpen(true)}>
            <Plus size={14} className="mr-1" />
            {t('agents.createAgent')}
          </Button>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3">
          <div className="relative flex-1 max-w-sm">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
              placeholder={t('agents.searchPlaceholder')}
              aria-label={t('agents.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 rounded-md border border-edge bg-elevated text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          <div className="flex items-center gap-1 rounded-md border border-edge bg-elevated p-0.5" role="group" aria-label={t('aiHub.filterByStatus')}>
            {(['all', 'official', 'custom'] as const).map(f => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                aria-pressed={filter === f}
                className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                  filter === f ? 'bg-accent text-white' : 'text-muted hover:text-body'
                }`}
              >
                {t(`agents.filter.${f}`)}
              </button>
            ))}
          </div>
        </div>

        {/* Loading */}
        {agentsQuery.isLoading && (
          <CardListSkeleton count={4} showStats={false} />
        )}

        {/* Error */}
        {agentsQuery.isError && !agentsQuery.isLoading && (
          <div className="text-center py-12">
            <AlertCircle size={32} className="text-warning mx-auto mb-3" />
            <p className="text-sm text-muted">{t('agents.loadError')}</p>
            <Button variant="ghost" size="sm" className="mt-3" onClick={() => agentsQuery.refetch()}>
              {t('common.retry')}
            </Button>
          </div>
        )}

        {/* Empty */}
        {!agentsQuery.isLoading && !agentsQuery.isError && filteredAgents.length === 0 && (
          <EmptyState
            icon={<Inbox size={20} />}
            title={t('agents.emptyTitle', 'No agents found')}
            description={t('agents.emptyDescription', 'Try adjusting your filters or create a new agent.')}
          />
        )}

        {/* Official Agents */}
        {!agentsQuery.isLoading && !agentsQuery.isError && officialAgents.length > 0 && (
          <div>
            <h2 className="text-sm font-semibold text-heading mb-3 flex items-center gap-2">
              <Shield size={14} className="text-accent" />
              {t('agents.officialSection')} ({officialAgents.length})
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {officialAgents.map(agent => (
                <AgentCard
                  key={agent.agentId}
                  agent={agent}
                  onView={() => navigate(`/ai/agents/${agent.agentId}`)}
                  onExecute={() => setExecuteAgent(agent)}
                  t={t}
                />
              ))}
            </div>
          </div>
        )}

        {/* Custom Agents */}
        {!agentsQuery.isLoading && !agentsQuery.isError && customAgents.length > 0 && (
          <div>
            <h2 className="text-sm font-semibold text-heading mb-3 flex items-center gap-2">
              <Sparkles size={14} className="text-info" />
              {t('agents.customSection')} ({customAgents.length})
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {customAgents.map(agent => (
                <AgentCard
                  key={agent.agentId}
                  agent={agent}
                  onView={() => navigate(`/ai/agents/${agent.agentId}`)}
                  onExecute={() => setExecuteAgent(agent)}
                  t={t}
                />
              ))}
            </div>
          </div>
        )}

        {/* Governance Notice */}
        <div className="mt-6 px-4 py-3 rounded-md bg-accent/5 border border-accent/20 text-xs text-muted flex items-center gap-2">
          <Shield size={14} className="text-accent shrink-0" />
          {t('agents.governanceNotice')}
        </div>
      </div>

      {/* Dialogs */}
      <CreateAgentDialog
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={handleAgentCreated}
        categories={availableCategories}
        defaultCategory={defaultCategory}
      />
      <ExecuteAgentDialog
        isOpen={!!executeAgent}
        agent={executeAgent}
        onClose={() => setExecuteAgent(null)}
      />
    </PageContainer>
  );
}
