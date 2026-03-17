import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Cable, Search, CheckCircle, AlertTriangle, XCircle, Clock,
  ArrowRight, Activity, Plug2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection } from '../../../components/shell';

type ConnectorStatus = 'Active' | 'Degraded' | 'Failed' | 'Disabled';
type ConnectorHealth = 'Healthy' | 'Degraded' | 'Failed' | 'Stale';
type ConnectorType = 'Observability' | 'ITSM' | 'CI/CD' | 'CloudProvider' | 'SCM' | 'APM' | 'LogAggregator' | 'Alerting' | 'Wiki' | 'ContractRegistry';

interface Connector {
  connectorId: string;
  name: string;
  type: ConnectorType;
  provider: string;
  status: ConnectorStatus;
  health: ConnectorHealth;
  lastSuccess: string;
  freshnessLag: string;
  itemsSynced: number;
}

const mockConnectors: Connector[] = [
  { connectorId: 'conn-001', name: 'Datadog APM', type: 'APM', provider: 'Datadog', status: 'Active', health: 'Healthy', lastSuccess: '2024-01-15T10:30:00Z', freshnessLag: '2m', itemsSynced: 14520 },
  { connectorId: 'conn-002', name: 'PagerDuty Incidents', type: 'ITSM', provider: 'PagerDuty', status: 'Active', health: 'Healthy', lastSuccess: '2024-01-15T10:28:00Z', freshnessLag: '4m', itemsSynced: 2340 },
  { connectorId: 'conn-003', name: 'GitHub Actions', type: 'CI/CD', provider: 'GitHub', status: 'Active', health: 'Healthy', lastSuccess: '2024-01-15T10:25:00Z', freshnessLag: '7m', itemsSynced: 8710 },
  { connectorId: 'conn-004', name: 'AWS CloudWatch', type: 'CloudProvider', provider: 'AWS', status: 'Active', health: 'Degraded', lastSuccess: '2024-01-15T09:45:00Z', freshnessLag: '47m', itemsSynced: 31200 },
  { connectorId: 'conn-005', name: 'Jira Service Desk', type: 'ITSM', provider: 'Atlassian', status: 'Active', health: 'Healthy', lastSuccess: '2024-01-15T10:20:00Z', freshnessLag: '12m', itemsSynced: 5640 },
  { connectorId: 'conn-006', name: 'Splunk Logs', type: 'LogAggregator', provider: 'Splunk', status: 'Degraded', health: 'Degraded', lastSuccess: '2024-01-15T08:10:00Z', freshnessLag: '2h 22m', itemsSynced: 102400 },
  { connectorId: 'conn-007', name: 'Prometheus Metrics', type: 'Observability', provider: 'Prometheus', status: 'Active', health: 'Healthy', lastSuccess: '2024-01-15T10:29:00Z', freshnessLag: '3m', itemsSynced: 47800 },
  { connectorId: 'conn-008', name: 'Confluence Wiki', type: 'Wiki', provider: 'Atlassian', status: 'Failed', health: 'Failed', lastSuccess: '2024-01-14T18:00:00Z', freshnessLag: '16h 32m', itemsSynced: 1230 },
  { connectorId: 'conn-009', name: 'OpsGenie Alerts', type: 'Alerting', provider: 'Atlassian', status: 'Active', health: 'Stale', lastSuccess: '2024-01-15T06:00:00Z', freshnessLag: '4h 32m', itemsSynced: 890 },
  { connectorId: 'conn-010', name: 'Swagger Hub', type: 'ContractRegistry', provider: 'SmartBear', status: 'Disabled', health: 'Stale', lastSuccess: '2024-01-10T12:00:00Z', freshnessLag: '5d 22h', itemsSynced: 320 },
];

const statusBadge = (s: ConnectorStatus): 'success' | 'warning' | 'danger' | 'default' => {
  switch (s) {
    case 'Active': return 'success';
    case 'Degraded': return 'warning';
    case 'Failed': return 'danger';
    case 'Disabled': return 'default';
  }
};

const healthBadge = (h: ConnectorHealth): 'success' | 'warning' | 'danger' | 'info' => {
  switch (h) {
    case 'Healthy': return 'success';
    case 'Degraded': return 'warning';
    case 'Failed': return 'danger';
    case 'Stale': return 'info';
  }
};

type TypeFilter = 'all' | ConnectorType;
type StatusFilter = 'all' | ConnectorStatus;

export function IntegrationHubPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<TypeFilter>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  const data = mockConnectors;

  const healthyCount = data.filter(c => c.health === 'Healthy').length;
  const degradedFailedCount = data.filter(c => c.health === 'Degraded' || c.health === 'Failed').length;
  const staleCount = data.filter(c => c.health === 'Stale').length;

  const types = Array.from(new Set(data.map(c => c.type)));

  const filtered = data.filter(c => {
    if (typeFilter !== 'all' && c.type !== typeFilter) return false;
    if (statusFilter !== 'all' && c.status !== statusFilter) return false;
    if (search) {
      const q = search.toLowerCase();
      return c.name.toLowerCase().includes(q)
        || c.provider.toLowerCase().includes(q)
        || c.type.toLowerCase().includes(q);
    }
    return true;
  });

  const formatDate = (iso: string) => {
    try { return new Date(iso).toLocaleString(); }
    catch { return iso; }
  };

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('integrations.hubTitle')}</h1>
        <p className="text-muted mt-1">{t('integrations.hubSubtitle')}</p>
      </div>

      {/* Stats */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          <StatCard title={t('integrations.totalConnectors')} value={data.length} icon={<Cable size={20} />} color="text-accent" />
          <StatCard title={t('integrations.healthyConnectors')} value={healthyCount} icon={<CheckCircle size={20} />} color="text-success" />
          <StatCard title={t('integrations.degradedFailed')} value={degradedFailedCount} icon={<AlertTriangle size={20} />} color="text-critical" />
          <StatCard title={t('integrations.staleFeeds')} value={staleCount} icon={<Clock size={20} />} color="text-warning" />
        </div>
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
          onChange={e => setTypeFilter(e.target.value as TypeFilter)}
          className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
        >
          <option value="all">{t('integrations.filterType')}</option>
          {types.map(tp => (
            <option key={tp} value={tp}>{tp}</option>
          ))}
        </select>
        <select
          value={statusFilter}
          onChange={e => setStatusFilter(e.target.value as StatusFilter)}
          className="px-3 py-2 text-xs rounded-md bg-elevated border border-edge text-body focus:outline-none focus:ring-1 focus:ring-accent"
        >
          <option value="all">{t('integrations.filterStatus')}</option>
          {(['Active', 'Degraded', 'Failed', 'Disabled'] as ConnectorStatus[]).map(s => (
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
          <div className="hidden md:grid grid-cols-8 gap-2 px-4 py-2 text-xs font-semibold text-muted uppercase tracking-wider border-b border-edge">
            <span>{t('integrations.columnName')}</span>
            <span>{t('integrations.columnType')}</span>
            <span>{t('integrations.columnProvider')}</span>
            <span>{t('integrations.columnStatus')}</span>
            <span>{t('integrations.columnHealth')}</span>
            <span>{t('integrations.columnLastSuccess')}</span>
            <span>{t('integrations.columnFreshness')}</span>
            <span className="text-right">{t('integrations.columnItemsSynced')}</span>
          </div>
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('integrations.noConnectors')}</div>
            ) : (
              filtered.map(c => (
                <Link
                  key={c.connectorId}
                  to={`/integrations/connectors/${c.connectorId}`}
                  className="grid grid-cols-1 md:grid-cols-8 gap-2 px-4 py-3 hover:bg-hover transition-colors items-center"
                >
                  <span className="text-sm font-medium text-heading truncate flex items-center gap-2">
                    <Activity size={14} className="text-accent shrink-0 hidden md:inline" />
                    {c.name}
                  </span>
                  <span className="text-xs text-muted">{c.type}</span>
                  <span className="text-xs text-muted">{c.provider}</span>
                  <span><Badge variant={statusBadge(c.status)}>{t(`integrations.${c.status.toLowerCase()}`)}</Badge></span>
                  <span><Badge variant={healthBadge(c.health)}>{t(`integrations.${c.health.toLowerCase()}`)}</Badge></span>
                  <span className="text-xs text-muted">{formatDate(c.lastSuccess)}</span>
                  <span className={`text-xs font-mono ${c.health === 'Stale' || c.health === 'Failed' ? 'text-critical' : 'text-muted'}`}>{c.freshnessLag}</span>
                  <span className="text-xs font-mono text-heading text-right flex items-center justify-end gap-1">
                    {c.itemsSynced.toLocaleString()}
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
