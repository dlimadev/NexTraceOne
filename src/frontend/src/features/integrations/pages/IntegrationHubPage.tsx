import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Cable, Search, CheckCircle, AlertTriangle, XCircle, Clock,
  ArrowRight, Activity, Plug2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageEmptyState } from '../../../components/PageEmptyState';
import { integrationsApi } from '../api/integrations';
import type { IntegrationConnectorDto } from '../../../types';

const statusBadge = (s: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (s) {
    case 'Active': return 'success';
    case 'Degraded': return 'warning';
    case 'Failed': return 'danger';
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

  const connectors = response?.connectors ?? [];
  const totalCount = response?.totalCount ?? 0;

  const types = Array.from(new Set(connectors.map((c: IntegrationConnectorDto) => c.connectorType)));

  const formatDate = (iso: string | null) => {
    if (!iso) return '—';
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState action={<button onClick={() => refetch()} className="btn btn-sm btn-primary">{t('common.retry')}</button>} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('integrations.hubTitle')}
        subtitle={t('integrations.hubSubtitle')}
      />

      {/* Stats */}
      <PageSection>
        <StatsGrid columns={4}>
          <StatCard title={t('integrations.totalConnectors')} value={health?.totalConnectors ?? totalCount} icon={<Cable size={20} />} color="text-accent" />
          <StatCard title={t('integrations.healthyConnectors')} value={health?.activeConnectors ?? 0} icon={<CheckCircle size={20} />} color="text-success" />
          <StatCard title={t('integrations.degradedFailed')} value={health?.failedExecutions24h ?? 0} icon={<AlertTriangle size={20} />} color="text-critical" />
          <StatCard title={t('integrations.staleFeeds')} value={connectors.filter((c: IntegrationConnectorDto) => c.healthScore < 50 && c.healthScore > 0).length} icon={<Clock size={20} />} color="text-warning" />
        </StatsGrid>
      </PageSection>

      {/* Connectors */}
      <PageSection>
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('integrations.search')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        <select
          value={typeFilter}
          onChange={e => setTypeFilter(e.target.value)}
          className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
        >
          <option value="all">{t('integrations.filterType')}</option>
          {types.map(tp => (
            <option key={tp} value={tp}>{tp}</option>
          ))}
        </select>
        <select
          value={statusFilter}
          onChange={e => setStatusFilter(e.target.value)}
          className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
        >
          <option value="all">{t('integrations.filterStatus')}</option>
          {(['Active', 'Degraded', 'Failed', 'Disabled']).map(s => (
            <option key={s} value={s}>{t(`integrations.${s.toLowerCase()}`)}</option>
          ))}
        </select>
      </div>

      {/* Connector Table */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Plug2 size={16} className="text-accent" />
            {t('integrations.totalConnectors')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          {/* Table header */}
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
              <div className="p-8 text-center text-muted text-sm">{t('integrations.noConnectors')}</div>
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
                  <span><Badge variant={statusBadge(c.status)}>{t(`integrations.${c.status.toLowerCase()}`)}</Badge></span>
                  <span><Badge variant={healthBadge(c.healthScore)}>{healthLabel(c.healthScore)}</Badge></span>
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
