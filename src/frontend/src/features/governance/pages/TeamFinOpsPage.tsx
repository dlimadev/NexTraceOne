import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import {
  DollarSign, TrendingUp, TrendingDown, Minus, AlertTriangle,
  CheckCircle, AlertCircle, XCircle, Activity, ArrowLeft,
  Users, ArrowRight, Target,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type { CostEfficiencyType } from '../../../types';

function formatCurrency(value: number, locale = 'en-US'): string {
  return new Intl.NumberFormat(locale, { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value);
}

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

const mockTeamFinOps = {
  teamId: 'team-commerce',
  teamName: 'Team Commerce',
  domain: 'Commerce',
  totalMonthlyCost: 42800,
  previousMonthCost: 40500,
  costTrend: 'Declining' as const,
  overallEfficiency: 'Inefficient' as CostEfficiencyType,
  totalWaste: 17000,
  serviceCount: 3,
  avgReliabilityScore: 68.2,
  totalRecentIncidents: 11,
  topOptimizationFocus: 'Reduce reprocessing waste in Order Processor and Catalog Sync',
  services: [
    { serviceId: 'svc-order-processor', serviceName: 'Order Processor', efficiency: 'Wasteful' as CostEfficiencyType, monthlyCost: 18700, trend: 'Declining' as const, wasteAmount: 7500, reliabilityScore: 58.3 },
    { serviceId: 'svc-catalog-sync', serviceName: 'Catalog Sync', efficiency: 'Wasteful' as CostEfficiencyType, monthlyCost: 15200, trend: 'Declining' as const, wasteAmount: 6700, reliabilityScore: 65.4 },
    { serviceId: 'svc-inventory-sync', serviceName: 'Inventory Sync', efficiency: 'Inefficient' as CostEfficiencyType, monthlyCost: 8900, trend: 'Declining' as const, wasteAmount: 2800, reliabilityScore: 81.0 },
  ],
  trendSeries: [
    { period: '2025-10', cost: 38200 }, { period: '2025-11', cost: 39800 }, { period: '2025-12', cost: 40500 },
    { period: '2026-01', cost: 41200 }, { period: '2026-02', cost: 42100 }, { period: '2026-03', cost: 42800 },
  ],
};

export function TeamFinOpsPage() {
  const { t, i18n } = useTranslation();
  const { teamId: _teamId } = useParams<{ teamId: string }>();
  const fmt = (v: number) => formatCurrency(v, i18n.language);
  const d = mockTeamFinOps;
  const costChange = d.totalMonthlyCost - d.previousMonthCost;

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Back + Header */}
      <div className="mb-6">
        <Link to="/governance/finops" className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent-hover mb-3">
          <ArrowLeft size={14} />
          {t('governance.finops.backToOverview')}
        </Link>
        <div className="flex items-center gap-3">
          <Users size={24} className="text-accent" />
          <h1 className="text-2xl font-bold text-heading">{d.teamName}</h1>
          <Badge variant={efficiencyBadgeVariant(d.overallEfficiency)}>{t(`governance.finops.efficiency.${d.overallEfficiency}`)}</Badge>
        </div>
        <p className="text-muted mt-1">{d.domain} · {d.serviceCount} {t('governance.finops.services')} · {t('governance.finops.teamFinOpsProfile')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(d.totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('governance.finops.avgReliability')} value={`${d.avgReliabilityScore}%`} icon={<Activity size={20} />} color={d.avgReliabilityScore >= 90 ? 'text-success' : d.avgReliabilityScore >= 70 ? 'text-warning' : 'text-critical'} />
        <StatCard title={t('governance.finops.incidents')} value={String(d.totalRecentIncidents)} icon={<AlertTriangle size={20} />} color="text-warning" />
      </div>

      {/* Cost trend */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            {trendIcon(d.costTrend)}
            {t('governance.finops.costTrend')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-end gap-1 h-24">
            {d.trendSeries.map((point, i) => {
              const max = Math.max(...d.trendSeries.map(p => p.cost));
              const height = max > 0 ? (point.cost / max) * 100 : 0;
              return (
                <div key={i} className="flex-1 flex flex-col items-center gap-1">
                  <div className="w-full bg-accent/20 rounded-t relative" style={{ height: `${height}%` }}>
                    <div className="absolute inset-0 bg-accent/40 rounded-t" />
                  </div>
                  <span className="text-[9px] text-muted">{point.period.slice(5)}</span>
                </div>
              );
            })}
          </div>
          <p className="text-xs text-muted mt-2">{t('governance.finops.costChange')}: {costChange >= 0 ? '+' : ''}{fmt(costChange)} {t('governance.finops.vsLastMonth')}</p>
        </CardBody>
      </Card>

      {/* Optimization focus */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex items-center gap-3">
            <Target size={16} className="text-success shrink-0" />
            <div>
              <p className="text-sm font-medium text-heading">{t('governance.finops.topOptimizationFocus')}</p>
              <p className="text-sm text-muted mt-0.5">{d.topOptimizationFocus}</p>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Services */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <DollarSign size={16} className="text-accent" />
            {t('governance.finops.serviceCosts')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.services.map(svc => (
              <Link key={svc.serviceId} to={`/governance/finops/services/${svc.serviceId}`} className="px-4 py-3 hover:bg-hover transition-colors flex items-center gap-3">
                {efficiencyIcon(svc.efficiency)}
                <span className="text-sm font-medium text-heading flex-1 truncate">{svc.serviceName}</span>
                <Badge variant={efficiencyBadgeVariant(svc.efficiency)}>{t(`governance.finops.efficiency.${svc.efficiency}`)}</Badge>
                <div className="hidden md:flex items-center gap-1 text-xs text-muted">
                  <Activity size={12} className={svc.reliabilityScore >= 90 ? 'text-success' : svc.reliabilityScore >= 70 ? 'text-warning' : 'text-critical'} />
                  {svc.reliabilityScore}%
                </div>
                <div className="hidden md:flex items-center gap-1 text-xs text-muted">
                  {trendIcon(svc.trend)}
                  {t(`governance.finops.trend.${svc.trend}`)}
                </div>
                {svc.wasteAmount > 0 && (
                  <Badge variant="danger" className="text-[10px]">{t('governance.finops.wasteAmount')}: {fmt(svc.wasteAmount)}</Badge>
                )}
                <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(svc.monthlyCost)}</span>
                <ArrowRight size={14} className="text-muted" />
              </Link>
            ))}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
