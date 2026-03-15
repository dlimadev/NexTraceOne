import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import {
  AlertTriangle, AlertCircle, ShieldAlert, Eye,
  Search, CheckCircle, XCircle, Clock, Shield,
  GitBranch, Wrench,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';

/**
 * Dados simulados de incidentes — alinhados com o backend ListIncidents.
 * Em produção, estes dados virão da API /api/v1/incidents.
 */
const mockIncidents = [
  {
    incidentId: 'a1b2c3d4-0001-0000-0000-000000000001',
    reference: 'INC-2026-0042',
    title: 'Payment Gateway — elevated error rate',
    incidentType: 'ServiceDegradation',
    severity: 'Critical',
    status: 'Mitigating',
    serviceId: 'svc-payment-gateway',
    serviceDisplayName: 'Payment Gateway',
    ownerTeam: 'payment-squad',
    environment: 'Production',
    createdAt: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
    hasCorrelatedChanges: true,
    correlationConfidence: 'High',
    mitigationStatus: 'InProgress',
  },
  {
    incidentId: 'a1b2c3d4-0002-0000-0000-000000000002',
    reference: 'INC-2026-0041',
    title: 'Catalog Sync — integration partner unreachable',
    incidentType: 'DependencyFailure',
    severity: 'Major',
    status: 'Investigating',
    serviceId: 'svc-catalog-sync',
    serviceDisplayName: 'Catalog Sync',
    ownerTeam: 'platform-squad',
    environment: 'Production',
    createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
    hasCorrelatedChanges: false,
    correlationConfidence: 'Low',
    mitigationStatus: 'NotStarted',
  },
  {
    incidentId: 'a1b2c3d4-0003-0000-0000-000000000003',
    reference: 'INC-2026-0040',
    title: 'Inventory Consumer — consumer lag spike',
    incidentType: 'MessagingIssue',
    severity: 'Major',
    status: 'Monitoring',
    serviceId: 'svc-inventory-consumer',
    serviceDisplayName: 'Inventory Consumer',
    ownerTeam: 'order-squad',
    environment: 'Production',
    createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(),
    hasCorrelatedChanges: true,
    correlationConfidence: 'Medium',
    mitigationStatus: 'Applied',
  },
  {
    incidentId: 'a1b2c3d4-0004-0000-0000-000000000004',
    reference: 'INC-2026-0039',
    title: 'Order API — latency regression after deploy',
    incidentType: 'OperationalRegression',
    severity: 'Minor',
    status: 'Resolved',
    serviceId: 'svc-order-api',
    serviceDisplayName: 'Order API',
    ownerTeam: 'order-squad',
    environment: 'Production',
    createdAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000).toISOString(),
    hasCorrelatedChanges: true,
    correlationConfidence: 'Confirmed',
    mitigationStatus: 'Verified',
  },
  {
    incidentId: 'a1b2c3d4-0005-0000-0000-000000000005',
    reference: 'INC-2026-0038',
    title: 'Notification Worker — background job failures',
    incidentType: 'BackgroundProcessingIssue',
    severity: 'Warning',
    status: 'Closed',
    serviceId: 'svc-notification-worker',
    serviceDisplayName: 'Notification Worker',
    ownerTeam: 'platform-squad',
    environment: 'Production',
    createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
    hasCorrelatedChanges: false,
    correlationConfidence: 'NotAssessed',
    mitigationStatus: 'Verified',
  },
  {
    incidentId: 'a1b2c3d4-0006-0000-0000-000000000006',
    reference: 'INC-2026-0037',
    title: 'Auth Gateway — contract schema mismatch',
    incidentType: 'ContractImpact',
    severity: 'Major',
    status: 'Resolved',
    serviceId: 'svc-auth-gateway',
    serviceDisplayName: 'Auth Gateway',
    ownerTeam: 'identity-squad',
    environment: 'Staging',
    createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    hasCorrelatedChanges: true,
    correlationConfidence: 'High',
    mitigationStatus: 'Verified',
  },
];

type StatusFilter = 'all' | 'Open' | 'Investigating' | 'Mitigating' | 'Monitoring' | 'Resolved' | 'Closed';

const severityBadge = (severity: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info'; icon: React.ReactNode } => {
  switch (severity) {
    case 'Critical': return { variant: 'danger', icon: <ShieldAlert size={14} /> };
    case 'Major': return { variant: 'warning', icon: <AlertCircle size={14} /> };
    case 'Minor': return { variant: 'info', icon: <AlertTriangle size={14} /> };
    case 'Warning': return { variant: 'default', icon: <Eye size={14} /> };
    default: return { variant: 'default', icon: <AlertTriangle size={14} /> };
  }
};

const statusIcon = (status: string) => {
  switch (status) {
    case 'Open': return <AlertCircle size={14} className="text-red-400" />;
    case 'Investigating': return <Search size={14} className="text-amber-400" />;
    case 'Mitigating': return <Wrench size={14} className="text-orange-400" />;
    case 'Monitoring': return <Eye size={14} className="text-blue-400" />;
    case 'Resolved': return <CheckCircle size={14} className="text-emerald-400" />;
    case 'Closed': return <XCircle size={14} className="text-gray-400" />;
    default: return <Clock size={14} className="text-muted" />;
  }
};

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours < 1) return '< 1h ago';
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

/**
 * Página de Incidentes — visão consolidada de incidentes com correlação contextualizada.
 * Correlaciona incidentes com serviços, mudanças, contratos, ownership e mitigação.
 * Persona-aware: Engineer vê por serviço, Tech Lead vê por equipa.
 */
export function IncidentsPage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');

  const filtered = mockIncidents.filter(inc => {
    if (filter !== 'all' && inc.status !== filter) return false;
    if (search && !inc.title.toLowerCase().includes(search.toLowerCase())
      && !inc.reference.toLowerCase().includes(search.toLowerCase())
      && !inc.serviceDisplayName.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const stats = {
    totalOpen: mockIncidents.filter(i => !['Resolved', 'Closed'].includes(i.status)).length,
    critical: mockIncidents.filter(i => i.severity === 'Critical').length,
    withCorrelation: mockIncidents.filter(i => i.hasCorrelatedChanges).length,
    withMitigation: mockIncidents.filter(i => i.mitigationStatus !== 'NotStarted').length,
    servicesImpacted: new Set(mockIncidents.filter(i => !['Resolved', 'Closed'].includes(i.status)).map(i => i.serviceId)).size,
  };

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('incidents.title')}</h1>
        <p className="text-muted mt-1">{t('incidents.subtitle')}</p>
      </div>

      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="operations" />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6">
        <StatCard title={t('incidents.totalOpen')} value={stats.totalOpen} icon={<AlertTriangle size={20} />} color="text-red-500" />
        <StatCard title={t('incidents.critical')} value={stats.critical} icon={<ShieldAlert size={20} />} color="text-critical" />
        <StatCard title={t('incidents.withCorrelation')} value={stats.withCorrelation} icon={<GitBranch size={20} />} color="text-amber-500" />
        <StatCard title={t('incidents.withMitigation')} value={stats.withMitigation} icon={<Wrench size={20} />} color="text-blue-500" />
        <StatCard title={t('incidents.servicesImpacted')} value={stats.servicesImpacted} icon={<Shield size={20} />} color="text-accent" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('incidents.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Open', 'Investigating', 'Mitigating', 'Monitoring', 'Resolved', 'Closed'] as StatusFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              filter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(`incidents.filter.${f}`)}
          </button>
        ))}
      </div>

      {/* Incident list */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <AlertTriangle size={16} className="text-accent" />
            {t('incidents.list.title')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(inc => {
                const badge = severityBadge(inc.severity);
                return (
                  <NavLink
                    key={inc.incidentId}
                    to={`/operations/incidents/${inc.incidentId}`}
                    className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors"
                  >
                    <div className="flex items-center gap-2 min-w-0 flex-1">
                      <Badge variant={badge.variant} className="flex items-center gap-1 shrink-0">
                        {badge.icon}
                        {t(`incidents.severity.${inc.severity}`)}
                      </Badge>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-muted font-mono">{inc.reference}</span>
                          <span className="flex items-center gap-1 text-xs text-muted">
                            {statusIcon(inc.status)}
                            {t(`incidents.status.${inc.status}`)}
                          </span>
                        </div>
                        <p className="text-sm font-medium text-heading truncate">{inc.title}</p>
                      </div>
                    </div>
                    <div className="hidden md:flex items-center gap-3 text-xs text-muted shrink-0">
                      <span className="w-28 truncate">{inc.serviceDisplayName}</span>
                      <span className="w-24 truncate">{inc.ownerTeam}</span>
                      <span className="w-16 text-right">{timeAgo(inc.createdAt)}</span>
                      {inc.hasCorrelatedChanges && (
                        <Badge variant="warning" className="text-[10px] flex items-center gap-1">
                          <GitBranch size={10} />
                          {t('incidents.list.correlationIndicator')}
                        </Badge>
                      )}
                      {inc.mitigationStatus !== 'NotStarted' && inc.mitigationStatus !== 'Verified' && (
                        <Badge variant="info" className="text-[10px] flex items-center gap-1">
                          <Wrench size={10} />
                          {t('incidents.list.mitigationIndicator')}
                        </Badge>
                      )}
                    </div>
                  </NavLink>
                );
              })
            )}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
