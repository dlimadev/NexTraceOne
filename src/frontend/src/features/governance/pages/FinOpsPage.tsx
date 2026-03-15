import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  DollarSign, Search, TrendingUp, TrendingDown, Minus,
  AlertTriangle, CheckCircle, AlertCircle, XCircle,
  Activity, Zap, ArrowRight, Target, BarChart3,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type { CostEfficiencyType } from '../../../types';

function formatCurrency(value: number, locale = 'en-US'): string {
  return new Intl.NumberFormat(locale, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value);
}

const mockFinOps = {
  totalMonthlyCost: 61300,
  totalWaste: 20200,
  overallEfficiency: 'Acceptable' as CostEfficiencyType,
  costTrend: 'Stable' as const,
  services: [
    {
      serviceId: 'svc-order-processor', serviceName: 'Order Processor', domain: 'Commerce', team: 'Team Commerce',
      efficiency: 'Wasteful' as CostEfficiencyType, monthlyCost: 18700, trend: 'Declining' as const,
      wasteSignals: [
        { description: 'Frequent rollbacks causing reprocessing', pattern: 'rollback-waste', type: 'RepeatedReprocessing', estimatedWaste: 5400 },
        { description: 'Idle compute during off-peak', pattern: 'idle-compute', type: 'IdleCostlyResource', estimatedWaste: 2100 },
      ],
      reliabilityCorrelation: { reliabilityScore: 58.3, recentIncidents: 5, reliabilityTrend: 'Declining' as const },
    },
    {
      serviceId: 'svc-catalog-sync', serviceName: 'Catalog Sync', domain: 'Catalog', team: 'Team Platform',
      efficiency: 'Wasteful' as CostEfficiencyType, monthlyCost: 15200, trend: 'Declining' as const,
      wasteSignals: [
        { description: 'Duplicate data processing pipelines', pattern: 'duplicate-etl', type: 'RepeatedReprocessing', estimatedWaste: 4200 },
        { description: 'Idle staging environment', pattern: 'idle-staging', type: 'IdleCostlyResource', estimatedWaste: 2500 },
      ],
      reliabilityCorrelation: { reliabilityScore: 65.4, recentIncidents: 4, reliabilityTrend: 'Declining' as const },
    },
    {
      serviceId: 'svc-payment-api', serviceName: 'Payment API', domain: 'Payments', team: 'Team Payments',
      efficiency: 'Inefficient' as CostEfficiencyType, monthlyCost: 12500, trend: 'Declining' as const,
      wasteSignals: [
        { description: 'Excessive retries on timeout', pattern: 'retry-pattern', type: 'ExcessiveRetries', estimatedWaste: 3200 },
      ],
      reliabilityCorrelation: { reliabilityScore: 72.5, recentIncidents: 3, reliabilityTrend: 'Declining' as const },
    },
    {
      serviceId: 'svc-inventory-sync', serviceName: 'Inventory Sync', domain: 'Commerce', team: 'Team Commerce',
      efficiency: 'Inefficient' as CostEfficiencyType, monthlyCost: 8900, trend: 'Declining' as const,
      wasteSignals: [
        { description: 'Redundant sync cycles', pattern: 'redundant-sync', type: 'RepeatedReprocessing', estimatedWaste: 2800 },
      ],
      reliabilityCorrelation: { reliabilityScore: 81.0, recentIncidents: 2, reliabilityTrend: 'Declining' as const },
    },
    {
      serviceId: 'svc-user-service', serviceName: 'User Service', domain: 'Identity', team: 'Team Identity',
      efficiency: 'Acceptable' as CostEfficiencyType, monthlyCost: 4200, trend: 'Stable' as const,
      wasteSignals: [],
      reliabilityCorrelation: { reliabilityScore: 95.1, recentIncidents: 0, reliabilityTrend: 'Stable' as const },
    },
    {
      serviceId: 'svc-notification-hub', serviceName: 'Notification Hub', domain: 'Messaging', team: 'Team Messaging',
      efficiency: 'Efficient' as CostEfficiencyType, monthlyCost: 1800, trend: 'Improving' as const,
      wasteSignals: [],
      reliabilityCorrelation: { reliabilityScore: 99.2, recentIncidents: 0, reliabilityTrend: 'Improving' as const },
    },
  ],
  topCostDrivers: [
    { serviceId: 'svc-order-processor', serviceName: 'Order Processor', monthlyCost: 18700, efficiency: 'Wasteful' as CostEfficiencyType },
    { serviceId: 'svc-catalog-sync', serviceName: 'Catalog Sync', monthlyCost: 15200, efficiency: 'Wasteful' as CostEfficiencyType },
    { serviceId: 'svc-payment-api', serviceName: 'Payment API', monthlyCost: 12500, efficiency: 'Inefficient' as CostEfficiencyType },
  ],
  optimizationOpportunities: [
    { serviceId: 'svc-order-processor', serviceName: 'Order Processor', potentialSavings: 7500, priority: 'High', recommendation: 'Address rollback waste and idle compute' },
    { serviceId: 'svc-catalog-sync', serviceName: 'Catalog Sync', potentialSavings: 6700, priority: 'High', recommendation: 'Consolidate duplicate ETL pipelines' },
    { serviceId: 'svc-payment-api', serviceName: 'Payment API', potentialSavings: 3200, priority: 'Medium', recommendation: 'Reduce retry storms with circuit breaker' },
  ],
  generatedAt: new Date().toISOString(),
};

type EfficiencyFilter = 'all' | CostEfficiencyType;

const efficiencyBadgeVariant = (eff: CostEfficiencyType): 'success' | 'warning' | 'danger' | 'default' => {
  switch (eff) {
    case 'Efficient': return 'success';
    case 'Acceptable': return 'default';
    case 'Inefficient': return 'warning';
    case 'Wasteful': return 'danger';
    default: return 'default';
  }
};

const efficiencyIcon = (eff: CostEfficiencyType) => {
  switch (eff) {
    case 'Efficient': return <CheckCircle size={14} className="text-emerald-400" />;
    case 'Acceptable': return <AlertCircle size={14} className="text-muted" />;
    case 'Inefficient': return <AlertTriangle size={14} className="text-orange-400" />;
    case 'Wasteful': return <XCircle size={14} className="text-critical" />;
    default: return null;
  }
};

const trendIcon = (dir: string) => {
  switch (dir) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    default: return <Minus size={14} className="text-muted" />;
  }
};

export function FinOpsPage() {
  const { t, i18n } = useTranslation();
  const [filter, setFilter] = useState<EfficiencyFilter>('all');
  const [search, setSearch] = useState('');
  const fmt = (v: number) => formatCurrency(v, i18n.language);

  const d = mockFinOps;

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
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.finopsTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.finopsSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(d.totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard
          title={t('governance.finops.overallEfficiency')}
          value={t(`governance.finops.efficiency.${d.overallEfficiency}`)}
          icon={efficiencyIcon(d.overallEfficiency) ?? <CheckCircle size={20} />}
          color="text-amber-500"
        />
        <StatCard
          title={t('governance.finops.costTrend')}
          value={t(`governance.finops.trend.${d.costTrend}`)}
          icon={trendIcon(d.costTrend)}
          color="text-blue-500"
        />
      </div>

      {/* Top Cost Drivers + Optimization Opportunities */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <BarChart3 size={16} className="text-accent" />
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
    </div>
  );
}
