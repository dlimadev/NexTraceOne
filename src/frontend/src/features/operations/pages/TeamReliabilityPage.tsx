import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Activity, CheckCircle, AlertTriangle, XCircle,
  TrendingUp, TrendingDown, Minus, Shield, Search,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { reliabilityApi } from '../api/reliability';

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

  const { data, isLoading, isError } = useQuery({
    queryKey: ['reliability-services', filter, search],
    queryFn: () => reliabilityApi.listServices({ status: filter !== 'all' ? filter : undefined, search: search || undefined, page: 1, pageSize: 100 }),
    staleTime: 30_000,
  });

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState message={t('reliability.loadError')} />;

  const services = data?.items ?? [];

  const stats = {
    total: data?.totalCount ?? 0,
    healthy: services.filter(s => s.reliabilityStatus === 'Healthy').length,
    degraded: services.filter(s => s.reliabilityStatus === 'Degraded').length,
    unavailable: services.filter(s => s.reliabilityStatus === 'Unavailable').length,
    needsAttention: services.filter(s => s.reliabilityStatus === 'NeedsAttention').length,
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
            id="reliability-search"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('reliability.searchPlaceholder')}
            aria-label={t('reliability.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Healthy', 'Degraded', 'Unavailable', 'NeedsAttention'] as StatusFilter[]).map(f => (
          <button
            key={f}
            type="button"
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
            {services.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              services.map(svc => {
                const badge = statusBadge(svc.reliabilityStatus);
                return (
                  <NavLink
                    key={svc.serviceName}
                    to={`/operations/reliability/${svc.serviceName}`}
                    className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors"
                  >
                    <div className="flex items-center gap-2 min-w-0 flex-1">
                      <Badge variant={badge.variant} className="flex items-center gap-1">
                        {badge.icon}
                        {t(`reliability.status.${svc.reliabilityStatus}`)}
                      </Badge>
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-heading truncate">{svc.displayName}</p>
                        <p className="text-xs text-muted truncate">{svc.serviceName}</p>
                      </div>
                    </div>
                    <div className="hidden md:flex items-center gap-4 text-xs text-muted shrink-0">
                      <span className="w-20">{svc.domain}</span>
                      <span className="w-24">{svc.teamName}</span>
                      <span className="w-16">{svc.criticality}</span>
                      <span className="w-6 flex justify-center">{trendIcon(svc.trend)}</span>
                      {svc.recentChangeImpact && (
                        <Badge variant="warning" className="text-[10px]">{t('reliability.changeImpact')}</Badge>
                      )}
                      {svc.openIncidents > 0 && (
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
