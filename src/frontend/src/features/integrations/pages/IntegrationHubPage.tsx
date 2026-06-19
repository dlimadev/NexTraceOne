import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Cable, CheckCircle2, AlertTriangle, Clock,
  ArrowRight, Activity, Plug2, Plus,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Button } from '../../../components/Button';
import { SearchInput } from '../../../components/SearchInput';
import { Select } from '../../../components/Select';
import { integrationsApi } from '../api/integrations';
import type { IntegrationConnectorDto } from '../../../types';

const statusBadge = (s: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (s) {
    case 'Active':
    case 'Healthy':
      return 'success';
    case 'Pending':
    case 'Paused':
    case 'Degraded':
      return 'warning';
    case 'Failed':
    case 'Critical':
    case 'Unhealthy':
      return 'danger';
    default: return 'default';
  }
};

const healthBadge = (score: number): 'success' | 'warning' | 'danger' | 'info' => {
  if (score >= 80) return 'success';
  if (score >= 50) return 'warning';
  if (score > 0) return 'danger';
  return 'info';
};

const healthLabel = (score: number): string => {
  if (score >= 80) return 'Healthy';
  if (score >= 50) return 'Degraded';
  if (score > 0) return 'Failed';
  return 'Unknown';
};

export function IntegrationHubPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const translateState = (value: string) => t(`integrations.${value.toLowerCase()}`, { defaultValue: value });

  const { data: response, isLoading, isError, refetch } = useQuery({
    queryKey: ['integrations', 'connectors', { search, connectorType: typeFilter === 'all' ? undefined : typeFilter, status: statusFilter === 'all' ? undefined : statusFilter }],
    queryFn: () => integrationsApi.listConnectors({
      search: search || undefined,
      connectorType: typeFilter === 'all' ? undefined : typeFilter,
      status: statusFilter === 'all' ? undefined : statusFilter,
    }),
    staleTime: 30_000,
  });

  const { data: health } = useQuery({
    queryKey: ['integrations', 'health'],
    queryFn: () => integrationsApi.getHealth(),
    staleTime: 30_000,
  });

  const { data: filterOptions } = useQuery({
    queryKey: ['integrations', 'filter-options'],
    queryFn: () => integrationsApi.getFilterOptions(),
    staleTime: 300_000,
  });

  const connectors = response?.connectors ?? [];
  const totalCount = response?.totalCount ?? 0;
  const typeOptions = filterOptions?.connectorTypes ?? [];
  const statusOptions = Array.from(new Set([
    ...(filterOptions?.connectorStatuses ?? []),
    ...(filterOptions?.connectorHealthStatuses ?? []),
  ]));

  const formatDate = (iso: string | null) => {
    if (!iso) return '—';
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  /* Opções normalizadas para o componente DS Select */
  const typeSelectOptions = [
    { value: 'all', label: t('integrations.filterType', 'All Types') },
    ...typeOptions.map(tp => ({ value: tp, label: tp })),
  ];

  const statusSelectOptions = [
    { value: 'all', label: t('integrations.filterStatus', 'All Statuses') },
    ...statusOptions.map(s => ({ value: s, label: translateState(s) })),
  ];

  if (isLoading) return <PageLoadingState />;
  if (isError) return (
    <PageErrorState onRetry={() => refetch()} />
  );

  return (
    <PageContainer>
      {/* Cabeçalho padronizado Betterstack com CTA primário */}
      <PageHeader
        title={t('integrations.hubTitle')}
        subtitle={t('integrations.hubSubtitle')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
          >
            {t('integrations.addConnector', 'Add Connector')}
          </Button>
        }
      />

      {/* KPIs — cards de métrica de saúde */}
      <PageSection>
        <StatsGrid columns={4}>
          <StatCard
            title={t('integrations.totalConnectors')}
            value={health?.totalConnectors ?? totalCount}
            icon={<Cable size={20} />}
            color="text-accent"
          />
          <StatCard
            title={t('integrations.healthyConnectors')}
            value={health?.activeConnectors ?? 0}
            icon={<CheckCircle2 size={20} />}
            color="text-success"
          />
          <StatCard
            title={t('integrations.degradedFailed')}
            value={health?.failedExecutions24h ?? 0}
            icon={<AlertTriangle size={20} />}
            color="text-critical"
          />
          <StatCard
            title={t('integrations.staleFeeds')}
            value={connectors.filter((c: IntegrationConnectorDto) => c.healthScore < 50 && c.healthScore > 0).length}
            icon={<Clock size={20} />}
            color="text-warning"
          />
        </StatsGrid>
      </PageSection>

      {/* Lista de conectores com filtros DS */}
      <PageSection>
        {/* Barra de filtros usando componentes do design system */}
        <div className="flex flex-wrap items-end gap-3 mb-4">
          <SearchInput
            size="sm"
            className="flex-1 min-w-[180px] max-w-xs"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('integrations.search')}
            aria-label={t('integrations.search')}
          />
          <Select
            size="sm"
            value={typeFilter}
            options={typeSelectOptions}
            onChange={e => setTypeFilter(e.target.value)}
            className="min-w-[140px]"
          />
          <Select
            size="sm"
            value={statusFilter}
            options={statusSelectOptions}
            onChange={e => setStatusFilter(e.target.value)}
            className="min-w-[140px]"
          />
        </div>

        {/* Tabela de conectores */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Plug2 size={16} className="text-accent" />
              {t('integrations.totalConnectors')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {/* Cabeçalho das colunas */}
            <div className="hidden md:grid grid-cols-7 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
              <span>{t('integrations.columnName')}</span>
              <span>{t('integrations.columnType')}</span>
              <span>{t('integrations.columnProvider')}</span>
              <span>{t('integrations.columnStatus')}</span>
              <span>{t('integrations.columnHealth')}</span>
              <span>{t('integrations.columnLastSuccess')}</span>
              <span className="text-right">{t('integrations.columnItemsSynced')}</span>
            </div>

            <div className="divide-y divide-edge">
              {connectors.length === 0 ? (
                /* Estado vazio padronizado com EmptyState DS */
                <EmptyState
                  size="compact"
                  variant="default"
                  icon={<Plug2 size={18} />}
                  title={t('integrations.noConnectors')}
                  description={t('integrations.noConnectorsDescription', 'Adicione um conector para começar a sincronizar dados.')}
                  action={
                    <Button variant="subtle" size="sm" icon={<Plus size={14} />}>
                      {t('integrations.addConnector', 'Add Connector')}
                    </Button>
                  }
                />
              ) : (
                connectors.map((c: IntegrationConnectorDto) => (
                  <Link
                    key={c.connectorId}
                    to={`/integrations/connectors/${c.connectorId}`}
                    className="grid grid-cols-1 md:grid-cols-7 gap-2 px-4 py-3 hover:bg-hover transition-colors items-center"
                  >
                    <span className="text-sm font-medium text-heading truncate flex items-center gap-2">
                      <Activity size={14} className="text-accent shrink-0 hidden md:inline" />
                      {c.name}
                    </span>
                    <span className="text-xs text-muted">{c.connectorType}</span>
                    <span className="text-xs text-muted">{c.provider}</span>
                    <span><Badge variant={statusBadge(c.status)}>{translateState(c.status)}</Badge></span>
                    <span><Badge variant={healthBadge(c.healthScore)}>{translateState(healthLabel(c.healthScore))}</Badge></span>
                    <span className="text-xs text-muted">{formatDate(c.lastSyncAt)}</span>
                    <span className="text-xs font-mono text-heading text-right flex items-center justify-end gap-1">
                      {c.sourcesCount}
                      <ArrowRight size={12} className="text-muted" />
                    </span>
                  </Link>
                ))
              )}
            </div>
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
