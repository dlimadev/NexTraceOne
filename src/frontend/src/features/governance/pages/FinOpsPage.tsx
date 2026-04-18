import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  DollarSign, Search,
  AlertTriangle, XCircle,
  Activity, Zap, ArrowRight, Target, PieChart, TrendingUp,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import type { CostEfficiencyType } from '../../../types';
import { finOpsApi } from '../api/finOps';
import { queryKeys } from '../../../shared/api/queryKeys';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import {
  formatCurrency,
  efficiencyBadgeVariant,
  efficiencyIcon,
  trendIcon,
} from '../utils/finOpsFormatters';
import { useFinOpsCurrency } from '../hooks/useFinOpsConfig';


type EfficiencyFilter = 'all' | CostEfficiencyType;

export function FinOpsPage() {
  const { t, i18n } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [filter, setFilter] = useState<EfficiencyFilter>('all');
  const [search, setSearch] = useState('');
  const currency = useFinOpsCurrency();
  const fmt = (v: number) => formatCurrency(v, i18n.language, currency);

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.finops.summary(undefined, activeEnvironmentId),
    queryFn: () => finOpsApi.getSummary(),
    staleTime: 30_000,
  });

  const { data: trendsData } = useQuery({
    queryKey: queryKeys.governance.finops.trends(undefined, activeEnvironmentId),
    queryFn: () => finOpsApi.getTrends(),
    staleTime: 60_000,
    enabled: !!d,
  });

  if (isLoading) return (<PageContainer><PageLoadingState /></PageContainer>);
  if (isError || !d) return (<PageContainer><PageErrorState action={<button onClick={() => refetch()} className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">{t('common.retry')}</button>} /></PageContainer>);

  const filtered = d.services.filter(svc => {
    if (filter !== 'all' && svc.efficiency !== filter) return false;
    if (search) {
      const q = search.toLowerCase();
      return svc.serviceName.toLowerCase().includes(q)
        || svc.domain.toLowerCase().includes(q)
        || svc.team.toLowerCase().includes(q);
    }
    return true;
  });

  const allWasteSignals = d.services.flatMap(svc =>
    svc.wasteSignals.map(ws => ({ ...ws, serviceName: svc.serviceName, serviceId: svc.serviceId })),
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.finopsTitle')}
        subtitle={t('governance.finopsSubtitle')}
      />


      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(d.totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard
          title={t('governance.finops.overallEfficiency')}
          value={t(`governance.finops.efficiency.${d.overallEfficiency}`)}
          icon={efficiencyIcon(d.overallEfficiency) ?? <CheckCircle size={20} />}
          color="text-warning"
        />
        <StatCard
          title={t('governance.finops.costTrend')}
          value={t(`governance.finops.trend.${d.costTrend}`)}
          icon={trendIcon(d.costTrend)}
          color="text-info"
        />
      </div>

      {/* Top Cost Drivers + Optimization Opportunities */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <PieChart size={16} className="text-accent" />
              {t('governance.finops.topCostDrivers')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.topCostDrivers.map(drv => (
                <Link key={drv.serviceId} to={`/governance/finops/services/${drv.serviceId}`} className="px-4 py-3 flex items-center gap-3 hover:bg-hover transition-colors">
                  {efficiencyIcon(drv.efficiency)}
                  <span className="text-sm font-medium text-heading flex-1 truncate">{drv.serviceName}</span>
                  <Badge variant={efficiencyBadgeVariant(drv.efficiency)}>{t(`governance.finops.efficiency.${drv.efficiency}`)}</Badge>
                  <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(drv.monthlyCost)}</span>
                  <ArrowRight size={14} className="text-muted" />
                </Link>
              ))}
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Target size={16} className="text-success" />
              {t('governance.finops.optimizationOpportunities')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.optimizationOpportunities.map(opt => (
                <Link key={opt.serviceId} to={`/governance/finops/services/${opt.serviceId}`} className="px-4 py-3 hover:bg-hover transition-colors block">
                  <div className="flex items-center gap-3">
                    <Zap size={14} className={opt.priority === 'High' ? 'text-critical' : 'text-warning'} />
                    <span className="text-sm font-medium text-heading flex-1 truncate">{opt.serviceName}</span>
                    <Badge variant={opt.priority === 'High' ? 'danger' : 'warning'}>{opt.priority}</Badge>
                    <span className="text-sm font-mono font-medium text-success w-24 text-right">{fmt(opt.potentialSavings)}</span>
                  </div>
                  <p className="text-xs text-muted mt-1 ml-7">{opt.recommendation}</p>
                </Link>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={t('governance.finops.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-elevated border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        {(['all', 'Wasteful', 'Inefficient', 'Acceptable', 'Efficient'] as EfficiencyFilter[]).map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              filter === f
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-elevated text-muted border-edge hover:text-body'
            }`}
          >
            {f === 'all' ? t('governance.finops.filterAll') : t(`governance.finops.filter${f}`)}
          </button>
        ))}
      </div>

      {/* Service cost list */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <DollarSign size={16} className="text-accent" />
            {t('governance.finops.serviceCosts')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {filtered.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              filtered.map(svc => (
                <Link key={svc.serviceId} to={`/governance/finops/services/${svc.serviceId}`} className="px-4 py-3 hover:bg-hover transition-colors block">
                  <div className="flex items-center gap-3">
                    {efficiencyIcon(svc.efficiency)}
                    <span className="text-sm font-medium text-heading flex-1 min-w-0 truncate">{svc.serviceName}</span>
                    <Badge variant={efficiencyBadgeVariant(svc.efficiency)}>
                      {t(`governance.finops.efficiency.${svc.efficiency}`)}
                    </Badge>
                    <div className="hidden md:flex items-center gap-1 text-xs text-muted">
                      {trendIcon(svc.trend)}
                      {t(`governance.finops.trend.${svc.trend}`)}
                    </div>
                    <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(svc.monthlyCost)}</span>
                    <ArrowRight size={14} className="text-muted" />
                  </div>
                  <div className="hidden md:flex items-center gap-3 ml-7 mt-1 text-xs text-muted">
                    <span>{svc.domain}</span>
                    <span>·</span>
                    <span>{svc.team}</span>
                    {svc.reliabilityCorrelation && (
                      <>
                        <span>·</span>
                        <Activity size={12} className={svc.reliabilityCorrelation.reliabilityScore >= 90 ? 'text-success' : svc.reliabilityCorrelation.reliabilityScore >= 70 ? 'text-warning' : 'text-critical'} />
                        <span>{t('governance.finops.reliability')}: {svc.reliabilityCorrelation.reliabilityScore}%</span>
                      </>
                    )}
                    {svc.wasteSignals.length > 0 && (
                      <>
                        <span>·</span>
                        <Badge variant="danger" className="text-[10px]">
                          {t('governance.finops.wasteAmount')}: {fmt(svc.wasteSignals.reduce((sum, ws) => sum + ws.estimatedWaste, 0))}
                        </Badge>
                      </>
                    )}
                  </div>
                </Link>
              ))
            )}
          </div>
        </CardBody>
      </Card>

      {/* Cost Trends */}
      {trendsData && trendsData.aggregatedTrend.length > 0 && (
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <TrendingUp size={16} className="text-accent" />
              {t('governance.finops.costTrends')}
              <span className="ml-auto text-xs font-normal text-muted">
                {t(`governance.finops.trend.${trendsData.overallDirection}`)}
                {' '}
                {trendsData.overallChangePercent > 0 ? '+' : ''}
                {trendsData.overallChangePercent.toFixed(1)}%
              </span>
            </h2>
          </CardHeader>
          <CardBody>
            <div className="flex items-end gap-1 h-20">
              {trendsData.aggregatedTrend.map((pt, i) => {
                const max = Math.max(...trendsData.aggregatedTrend.map(p => p.cost));
                const heightPct = max > 0 ? (pt.cost / max) * 100 : 0;
                return (
                  <div key={i} className="flex-1 flex flex-col items-center gap-1 group">
                    <div
                      className="w-full bg-accent/60 rounded-sm hover:bg-accent transition-colors"
                      style={{ height: `${heightPct}%`, minHeight: 4 }}
                      title={`${pt.period}: ${fmt(pt.cost)}`}
                    />
                    <span className="text-[9px] text-muted rotate-45 origin-left hidden md:block">
                      {pt.period}
                    </span>
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Waste signals */}
      {allWasteSignals.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-critical" />
              {t('governance.finops.wasteSignals')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {allWasteSignals.map((ws, i) => (
                <Link key={i} to={`/governance/finops/services/${ws.serviceId}`} className="px-4 py-3 hover:bg-hover transition-colors block">
                  <div className="flex items-center gap-3">
                    <XCircle size={14} className="text-critical shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{ws.description}</p>
                      <p className="text-xs text-muted">{ws.serviceName} — {ws.pattern}</p>
                    </div>
                    <span className="text-sm font-mono font-medium text-critical shrink-0">{fmt(ws.estimatedWaste)}</span>
                  </div>
                </Link>
              ))}
            </div>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
