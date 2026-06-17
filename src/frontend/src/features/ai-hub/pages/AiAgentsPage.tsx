import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { EmptyState } from '../../../components/EmptyState';
import { Bot, Plus, Shield, Sparkles, Inbox } from 'lucide-react';
import { Button } from '../../../components/Button';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { SearchInput } from '../../../components/SearchInput';
import { Tabs } from '../../../components/Tabs';
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

  /* Itens de filtro para o DS Tabs (variante pill) */
  const filterTabs = [
    { id: 'all', label: t('agents.filter.all') },
    { id: 'official', label: t('agents.filter.official') },
    { id: 'custom', label: t('agents.filter.custom') },
  ] as const;

  if (agentsQuery.isError) {
    return (
      <PageContainer>
        <PageErrorState onRetry={() => agentsQuery.refetch()} />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      {/* Cabeçalho padronizado Betterstack */}
      <PageHeader
        title={t('agents.title')}
        subtitle={t('agents.subtitle')}
        icon={<Bot size={22} className="text-accent" />}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={() => setIsCreateOpen(true)}
          >
            {t('agents.createAgent')}
          </Button>
        }
      />

      {/* Barra de filtros — SearchInput DS + Tabs pill */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3 mb-6">
        <SearchInput
          size="sm"
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          placeholder={t('agents.searchPlaceholder')}
          aria-label={t('agents.searchPlaceholder')}
          className="w-full max-w-xs"
        />
        <Tabs
          variant="pill"
          size="sm"
          items={filterTabs as unknown as { id: string; label: string }[]}
          activeId={filter}
          onChange={(id) => setFilter(id as typeof filter)}
        />
      </div>

      {/* Estado de carregamento */}
      {agentsQuery.isLoading && <PageLoadingState />}

      {/* Estado vazio */}
      {!agentsQuery.isLoading && !agentsQuery.isError && filteredAgents.length === 0 && (
        <EmptyState
          icon={<Inbox size={20} />}
          title={t('agents.emptyTitle', 'No agents found')}
          description={t('agents.emptyDescription', 'Try adjusting your filters or create a new agent.')}
        />
      )}

      {/* Secção: Agents Oficiais */}
      {!agentsQuery.isLoading && !agentsQuery.isError && officialAgents.length > 0 && (
        <PageSection
          title={`${t('agents.officialSection')} (${officialAgents.length})`}
          icon={<Shield size={14} className="text-accent" />}
        >
          <ContentGrid columns={3}>
            {officialAgents.map(agent => (
              <AgentCard
                key={agent.agentId}
                agent={agent}
                onView={() => navigate(`/ai/agents/${agent.agentId}`)}
                onExecute={() => setExecuteAgent(agent)}
                t={t}
              />
            ))}
          </ContentGrid>
        </PageSection>
      )}

      {/* Secção: Agents Personalizados */}
      {!agentsQuery.isLoading && !agentsQuery.isError && customAgents.length > 0 && (
        <PageSection
          title={`${t('agents.customSection')} (${customAgents.length})`}
          icon={<Sparkles size={14} className="text-accent" />}
        >
          <ContentGrid columns={3}>
            {customAgents.map(agent => (
              <AgentCard
                key={agent.agentId}
                agent={agent}
                onView={() => navigate(`/ai/agents/${agent.agentId}`)}
                onExecute={() => setExecuteAgent(agent)}
                t={t}
              />
            ))}
          </ContentGrid>
        </PageSection>
      )}

      {/* Aviso de governança */}
      {!agentsQuery.isLoading && (
        <div className="px-4 py-3 rounded-md bg-accent/5 border border-accent/20 text-xs text-muted flex items-center gap-2">
          <Shield size={14} className="text-accent shrink-0" />
          {t('agents.governanceNotice')}
        </div>
      )}

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
