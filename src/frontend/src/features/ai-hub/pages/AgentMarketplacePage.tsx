import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Search, Bot, Sparkles, Play, Star } from 'lucide-react';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ────────────────────────────────────────────────────────────────

interface MarketplaceAgentDto {
  agentId: string;
  name: string;
  displayName: string;
  slug: string;
  description: string;
  category: string;
  isOfficial: boolean;
  isActive: boolean;
  capabilities: string;
  targetPersona: string;
  icon: string;
  version: number;
  executionCount: number;
  publicationStatus: string;
  ownershipType: string;
  tags: string[];
}

interface MarketplaceResponse {
  items: MarketplaceAgentDto[];
  totalCount: number;
  categories: string[];
}

// ── API ──────────────────────────────────────────────────────────────────

const fetchMarketplace = (params: {
  category?: string;
  search?: string;
  isOfficial?: boolean;
  page?: number;
  pageSize?: number;
}) =>
  client
    .get<MarketplaceResponse>('/v1/aiorchestration/marketplace', { params })
    .then((r) => r.data);

// ── Sub-components ───────────────────────────────────────────────────────

interface AgentCardProps {
  agent: MarketplaceAgentDto;
  t: (key: string, opts?: Record<string, unknown>) => string;
}

function AgentCard({ agent, t }: AgentCardProps) {
  return (
    <div className="bg-white dark:bg-neutral-900 border border-neutral-200 dark:border-neutral-700 rounded-lg p-5 flex flex-col gap-3 hover:border-indigo-400 dark:hover:border-indigo-500 transition-colors">
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-3 min-w-0">
          <div className="text-2xl flex-shrink-0" aria-hidden="true">
            {agent.icon || '🤖'}
          </div>
          <div className="min-w-0">
            <h3 className="text-sm font-semibold text-neutral-900 dark:text-neutral-100 truncate">
              {agent.displayName}
            </h3>
            <span className="text-xs text-neutral-500 dark:text-neutral-400">
              {t('agentMarketplace.category')}: {agent.category}
            </span>
          </div>
        </div>
        {agent.isOfficial && (
          <Badge variant="info" size="sm" className="flex-shrink-0 flex items-center gap-1">
            <Star className="w-3 h-3" />
            {t('agentMarketplace.official')}
          </Badge>
        )}
      </div>

      <p className="text-xs text-neutral-600 dark:text-neutral-400 line-clamp-3 flex-1">
        {agent.description}
      </p>

      {agent.tags.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {agent.tags.slice(0, 3).map((tag) => (
            <span
              key={tag}
              className="inline-block px-2 py-0.5 text-xs rounded bg-neutral-100 dark:bg-neutral-800 text-neutral-600 dark:text-neutral-400"
            >
              {tag}
            </span>
          ))}
        </div>
      )}

      <div className="flex items-center justify-between mt-auto pt-2 border-t border-neutral-100 dark:border-neutral-800">
        <span className="text-xs text-neutral-500 dark:text-neutral-400">
          <Play className="w-3 h-3 inline mr-1" />
          {t('agentMarketplace.executions', { count: agent.executionCount })}
        </span>
        <Button variant="primary" size="sm">
          {t('agentMarketplace.useAgent')}
        </Button>
      </div>
    </div>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────

export function AgentMarketplacePage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | undefined>(undefined);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['agent-marketplace', search, selectedCategory],
    queryFn: () =>
      fetchMarketplace({
        search: search || undefined,
        category: selectedCategory,
        pageSize: 50,
      }),
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('agentMarketplace.title')}
        subtitle={t('agentMarketplace.subtitle')}
        icon={<Sparkles className="w-6 h-6 text-indigo-500" />}
      />

      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-neutral-400 pointer-events-none" />
          <input
            type="text"
            className="w-full pl-9 pr-4 py-2 text-sm border border-neutral-200 dark:border-neutral-700 rounded-md bg-white dark:bg-neutral-900 text-neutral-900 dark:text-neutral-100 placeholder-neutral-400 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            placeholder={t('agentMarketplace.searchPlaceholder')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="flex flex-wrap gap-2 items-center">
          <button
            className={`px-3 py-1.5 text-xs rounded-full border transition-colors ${
              !selectedCategory
                ? 'bg-indigo-600 border-indigo-600 text-white'
                : 'bg-white dark:bg-neutral-900 border-neutral-200 dark:border-neutral-700 text-neutral-600 dark:text-neutral-400 hover:border-indigo-400'
            }`}
            onClick={() => setSelectedCategory(undefined)}
          >
            {t('agentMarketplace.filterAll')}
          </button>
          {(data?.categories ?? []).map((cat) => (
            <button
              key={cat}
              className={`px-3 py-1.5 text-xs rounded-full border transition-colors ${
                selectedCategory === cat
                  ? 'bg-indigo-600 border-indigo-600 text-white'
                  : 'bg-white dark:bg-neutral-900 border-neutral-200 dark:border-neutral-700 text-neutral-600 dark:text-neutral-400 hover:border-indigo-400'
              }`}
              onClick={() => setSelectedCategory(cat)}
            >
              {cat}
            </button>
          ))}
        </div>
      </div>

      {isLoading && (
        <PageLoadingState message={t('agentMarketplace.loading')} />
      )}

      {isError && (
        <PageErrorState message={t('agentMarketplace.error')} />
      )}

      {!isLoading && !isError && data && data.items.length === 0 && (
        <EmptyState
          icon={<Bot className="w-5 h-5" />}
          title={t('agentMarketplace.noAgents', 'No agents available')}
          description={t('agentMarketplace.noAgentsHint', 'Agents will appear here once published to the marketplace.')}
        />
      )}

      {!isLoading && !isError && data && data.items.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {data.items.map((agent) => (
            <AgentCard key={agent.agentId} agent={agent} t={t} />
          ))}
        </div>
      )}
    </PageContainer>
  );
}
