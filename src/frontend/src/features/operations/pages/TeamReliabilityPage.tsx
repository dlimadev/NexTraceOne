import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import {
  Activity, CheckCircle, AlertTriangle, XCircle,
  TrendingUp, TrendingDown, Minus, Shield, Search,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

// Simulated data matching backend ListServiceReliability response
const mockServices = [
  { serviceId: 'svc-order-api', displayName: 'Order API', serviceType: 'RestApi', domain: 'Orders', teamName: 'order-squad', criticality: 'Critical', status: 'Healthy', summary: 'reliability.mock.orderApiSummary', trend: 'Stable', flags: 0, incidents: 0, changeImpact: false },
  { serviceId: 'svc-payment-gateway', displayName: 'Payment Gateway', serviceType: 'RestApi', domain: 'Payments', teamName: 'payment-squad', criticality: 'Critical', status: 'Degraded', summary: 'reliability.mock.paymentGwSummary', trend: 'Declining', flags: 5, incidents: 0, changeImpact: true },
  { serviceId: 'svc-notification-worker', displayName: 'Notification Worker', serviceType: 'BackgroundService', domain: 'Notifications', teamName: 'platform-squad', criticality: 'High', status: 'Healthy', summary: 'reliability.mock.notificationSummary', trend: 'Improving', flags: 0, incidents: 0, changeImpact: false },
  { serviceId: 'svc-inventory-consumer', displayName: 'Inventory Consumer', serviceType: 'KafkaConsumer', domain: 'Inventory', teamName: 'order-squad', criticality: 'High', status: 'NeedsAttention', summary: 'reliability.mock.inventorySummary', trend: 'Declining', flags: 8, incidents: 0, changeImpact: false },
  { serviceId: 'svc-user-service', displayName: 'User Service', serviceType: 'RestApi', domain: 'Identity', teamName: 'identity-squad', criticality: 'Critical', status: 'Healthy', summary: 'reliability.mock.userSvcSummary', trend: 'Stable', flags: 0, incidents: 0, changeImpact: false },
  { serviceId: 'svc-catalog-sync', displayName: 'Catalog Sync', serviceType: 'IntegrationComponent', domain: 'Catalog', teamName: 'platform-squad', criticality: 'Medium', status: 'Unavailable', summary: 'reliability.mock.catalogSyncSummary', trend: 'Declining', flags: 10, incidents: 1, changeImpact: false },
  { serviceId: 'svc-report-scheduler', displayName: 'Report Scheduler', serviceType: 'ScheduledProcess', domain: 'Analytics', teamName: 'data-squad', criticality: 'Low', status: 'NeedsAttention', summary: 'reliability.mock.reportSchedulerSummary', trend: 'Stable', flags: 16, incidents: 0, changeImpact: false },
  { serviceId: 'svc-auth-gateway', displayName: 'Auth Gateway', serviceType: 'SharedPlatformService', domain: 'Security', teamName: 'identity-squad', criticality: 'Critical', status: 'Healthy', summary: 'reliability.mock.authGwSummary', trend: 'Stable', flags: 0, incidents: 0, changeImpact: false },
];

type StatusFilter = 'all' | 'Healthy' | 'Degraded' | 'Unavailable' | 'NeedsAttention';

const statusBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default'; icon: React.ReactNode } => {
  switch (status) {
    case 'Healthy': return { variant: 'success', icon: <CheckCircle size={14} /> };
    case 'Degraded': return { variant: 'warning', icon: <AlertTriangle size={14} /> };
    case 'Unavailable': return { variant: 'danger', icon: <XCircle size={14} /> };
    case 'NeedsAttention': return { variant: 'default', icon: <Shield size={14} /> };
    default: return { variant: 'default', icon: <Minus size={14} /> };
  }
};

const trendIcon = (trend: string) => {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-emerald-500" />;
    case 'Declining': return <TrendingDown size={14} className="text-red-500" />;
    default: return <Minus size={14} className="text-muted" />;
  }
};

export function TeamReliabilityPage() {
  const { t } = useTranslation();
  const [filter, setFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');

  const filtered = mockServices.filter(s => {
    if (filter !== 'all' && s.status !== filter) return false;
    if (search && !s.displayName.toLowerCase().includes(search.toLowerCase()) && !s.serviceId.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const stats = {
    total: mockServices.length,
    healthy: mockServices.filter(s => s.status === 'Healthy').length,
    degraded: mockServices.filter(s => s.status === 'Degraded').length,
    unavailable: mockServices.filter(s => s.status === 'Unavailable').length,
    needsAttention: mockServices.filter(s => s.status === 'NeedsAttention').length,
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('reliability.title')}
        subtitle={t('reliability.subtitle')}
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('reliability.totalServices')} value={stats.total} icon={<Activity size={20} />} />
        <StatCard title={t('reliability.healthyServices')} value={stats.healthy} icon={<CheckCircle size={20} />} color="text-emerald-500" />
        <StatCard title={t('reliability.degradedServices')} value={stats.degraded} icon={<AlertTriangle size={20} />} color="text-amber-500" />
        <StatCard title={t('reliability.needsAttention')} value={stats.needsAttention + stats.unavailable} icon={<Shield size={20} />} color="text-red-500" />
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('reliability.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Healthy', 'Degraded', 'Unavailable', 'NeedsAttention'] as StatusFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              filter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(`reliability.filter.${f}`)}
          </button>
        ))}
      </div>

      {/* Service list */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Activity size={16} className="text-accent" />
            {t('reliability.serviceList')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(svc => {
                const badge = statusBadge(svc.status);
                return (
                  <NavLink
                    key={svc.serviceId}
                    to={`/operations/reliability/${svc.serviceId}`}
                    className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors"
                  >
                    <div className="flex items-center gap-2 min-w-0 flex-1">
                      <Badge variant={badge.variant} className="flex items-center gap-1">
                        {badge.icon}
                        {t(`reliability.status.${svc.status}`)}
                      </Badge>
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-heading truncate">{svc.displayName}</p>
                        <p className="text-xs text-muted truncate">{svc.serviceId}</p>
                      </div>
                    </div>
                    <div className="hidden md:flex items-center gap-4 text-xs text-muted shrink-0">
                      <span className="w-20">{svc.domain}</span>
                      <span className="w-24">{svc.teamName}</span>
                      <span className="w-16">{svc.criticality}</span>
                      <span className="w-6 flex justify-center">{trendIcon(svc.trend)}</span>
                      {svc.changeImpact && (
                        <Badge variant="warning" className="text-[10px]">{t('reliability.changeImpact')}</Badge>
                      )}
                      {svc.incidents > 0 && (
                        <Badge variant="danger" className="text-[10px]">{t('reliability.incidentActive')}</Badge>
                      )}
                    </div>
                  </NavLink>
                );
              })
            )}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
