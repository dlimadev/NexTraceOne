import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Bot, Sparkles, Play, Star, Plus } from 'lucide-react';
import { EmptyState } from '../../../components/EmptyState';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Card, CardBody, CardFooter, CardHeader } from '../../../components/Card';
import { Tabs } from '../../../components/Tabs';
import { PageContainer, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { SearchInput } from '../../../shared/ui';
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
    <Card variant="interactive">
      <CardHeader>
        <div className="flex items-start justify-between gap-2 w-full">
          {/* Ícone + nome + categoria */}
          <div className="flex items-center gap-3 min-w-0">
            <div className="text-2xl flex-shrink-0" aria-hidden="true">
              {agent.icon || '🤖'}
            </div>
            <div className="min-w-0">
              <h3 className="text-sm font-semibold text-heading truncate">
                {agent.displayName}
              </h3>
              <span className="text-xs text-muted">
                {t('agentMarketplace.category')}: {agent.category}
              </span>
            </div>
          </div>

          {/* Badge "Official" alinhado à direita */}
          {agent.isOfficial && (
            <Badge variant="info" size="sm" className="flex-shrink-0 flex items-center gap-1">
              <Star className="w-3 h-3" />
              {t('agentMarketplace.official')}
            </Badge>
          )}
        </div>
      </CardHeader>

      <CardBody>
        {/* Descrição do agente */}
        <p className="text-xs text-muted line-clamp-3">
          {agent.description}
        </p>

        {/* Tags do agente */}
        {agent.tags.length > 0 && (
          <div className="flex flex-wrap gap-1 mt-3">
            {agent.tags.slice(0, 3).map((tag) => (
              <Badge key={tag} variant="default" size="sm">
                {tag}
              </Badge>
            ))}
          </div>
        )}
      </CardBody>

      <CardFooter>
        <div className="flex items-center justify-between">
          <span className="text-xs text-muted flex items-center gap-1">
            <Play className="w-3 h-3" />
            {t('agentMarketplace.executions', { count: agent.executionCount })}
          </span>
          <Button variant="primary" size="sm">
            {t('agentMarketplace.useAgent')}
          </Button>
        </div>
      </CardFooter>
    </Card>
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

  // Monta a lista de tabs a partir das categorias retornadas pela API.
  // A primeira tab representa "Todos" (sem filtro de categoria).
  const categoryTabs = [
    { id: '__all__', label: t('agentMarketplace.filterAll') },
    ...(data?.categories ?? []).map((cat) => ({ id: cat, label: cat })),
  ];

  const activeCategoryTab = selectedCategory ?? '__all__';

  return (
    <PageContainer>
      {/* Cabeçalho da página com CTA de publicação */}
      <PageHeader
        title={t('agentMarketplace.title')}
        subtitle={t('agentMarketplace.subtitle')}
        icon={<Sparkles className="w-6 h-6 text-accent" />}
        actions={
          <Button variant="primary" size="sm" icon={<Plus className="w-4 h-4" />}>
            {t('agentMarketplace.publishAgent')}
          </Button>
        }
      />

      {/* Barra de filtros: busca textual + filtro de categoria via Tabs pill */}
      <div className="flex flex-col gap-4 mb-6">
        <SearchInput
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('agentMarketplace.searchPlaceholder')}
          size="sm"
          aria-label={t('agentMarketplace.searchPlaceholder')}
          className="max-w-sm"
        />

        {/* Tabs pill para filtro de categoria — renderizadas somente após dados carregarem */}
        {data && (
          <Tabs
            variant="pill"
            size="sm"
            items={categoryTabs}
            activeId={activeCategoryTab}
            onChange={(id) => setSelectedCategory(id === '__all__' ? undefined : id)}
          />
        )}
      </div>

      {/* Estado de carregamento */}
      {isLoading && (
        <PageLoadingState message={t('agentMarketplace.loading')} />
      )}

      {/* Estado de erro */}
      {isError && (
        <PageErrorState message={t('agentMarketplace.error')} />
      )}

      {/* Estado vazio */}
      {!isLoading && !isError && data && data.items.length === 0 && (
        <EmptyState
          icon={<Bot className="w-5 h-5" />}
          title={t('agentMarketplace.noAgents', 'No agents available')}
          description={t('agentMarketplace.noAgentsHint', 'Agents will appear here once published to the marketplace.')}
        />
      )}

      {/* Grade de cards dos agentes */}
      {!isLoading && !isError && data && data.items.length > 0 && (
        <ContentGrid columns={3}>
          {data.items.map((agent) => (
            <AgentCard key={agent.agentId} agent={agent} t={t} />
          ))}
        </ContentGrid>
      )}
    </PageContainer>
  );
}
