import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import {
  DollarSign, TrendingUp, TrendingDown, Minus, AlertTriangle,
  CheckCircle, AlertCircle, XCircle, Activity, ArrowLeft,
  Layers, Users, ArrowRight,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type { CostEfficiencyType } from '../../../types';
import { PageContainer } from '../../../components/shell';

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

const mockDomainFinOps = {
  domainId: 'domain-commerce',
  domainName: 'Commerce',
  totalMonthlyCost: 64800,
  previousMonthCost: 63500,
  costTrend: 'Declining' as const,
  overallEfficiency: 'Inefficient' as CostEfficiencyType,
  totalWaste: 23700,
  teamCount: 2,
  serviceCount: 5,
  avgReliabilityScore: 75.4,
  teams: [
    { teamId: 'team-commerce', teamName: 'Team Commerce', serviceCount: 3, monthlyCost: 42800, wasteAmount: 17000, efficiency: 'Inefficient' as CostEfficiencyType, avgReliabilityScore: 68.2 },
    { teamId: 'team-platform', teamName: 'Team Platform', serviceCount: 2, monthlyCost: 22000, wasteAmount: 6700, efficiency: 'Acceptable' as CostEfficiencyType, avgReliabilityScore: 82.5 },
  ],
  topWasteServices: [
    { serviceId: 'svc-order-processor', serviceName: 'Order Processor', team: 'Team Commerce', wasteAmount: 7500, efficiency: 'Wasteful' as CostEfficiencyType },
    { serviceId: 'svc-catalog-sync', serviceName: 'Catalog Sync', team: 'Team Platform', wasteAmount: 6700, efficiency: 'Wasteful' as CostEfficiencyType },
    { serviceId: 'svc-inventory-sync', serviceName: 'Inventory Sync', team: 'Team Commerce', wasteAmount: 2800, efficiency: 'Inefficient' as CostEfficiencyType },
  ],
  trendSeries: [
    { period: '2025-10', cost: 58200 }, { period: '2025-11', cost: 60100 }, { period: '2025-12', cost: 61500 },
    { period: '2026-01', cost: 62800 }, { period: '2026-02', cost: 63500 }, { period: '2026-03', cost: 64800 },
  ],
};

export function DomainFinOpsPage() {
  const { t, i18n } = useTranslation();
  const { domainId: _domainId } = useParams<{ domainId: string }>();
  const fmt = (v: number) => formatCurrency(v, i18n.language);
  const d = mockDomainFinOps;
  const costChange = d.totalMonthlyCost - d.previousMonthCost;

  return (
    <PageContainer>
      {/* Back + Header */}
      <div className="mb-6">
        <Link to="/governance/finops" className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent-hover mb-3">
          <ArrowLeft size={14} />
          {t('governance.finops.backToOverview')}
        </Link>
        <div className="flex items-center gap-3">
          <Layers size={24} className="text-accent" />
          <h1 className="text-2xl font-bold text-heading">{d.domainName}</h1>
          <Badge variant={efficiencyBadgeVariant(d.overallEfficiency)}>{t(`governance.finops.efficiency.${d.overallEfficiency}`)}</Badge>
        </div>
        <p className="text-muted mt-1">{d.teamCount} {t('governance.finops.teams')} · {d.serviceCount} {t('governance.finops.services')} · {t('governance.finops.domainFinOpsProfile')}</p>
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">{t('governance.preview.finopsReason')}</span>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(d.totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('governance.finops.avgReliability')} value={`${d.avgReliabilityScore}%`} icon={<Activity size={20} />} color="text-warning" />
        <StatCard title={t('governance.finops.services')} value={String(d.serviceCount)} icon={<Layers size={20} />} color="text-info" />
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

      {/* Teams */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Users size={16} className="text-accent" />
            {t('governance.finops.teamBreakdown')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.teams.map(team => (
              <Link key={team.teamId} to={`/governance/finops/teams/${team.teamId}`} className="px-4 py-3 hover:bg-hover transition-colors flex items-center gap-3">
                <Users size={14} className="text-accent shrink-0" />
                <span className="text-sm font-medium text-heading flex-1 truncate">{team.teamName}</span>
                <Badge variant={efficiencyBadgeVariant(team.efficiency)}>{t(`governance.finops.efficiency.${team.efficiency}`)}</Badge>
                <span className="text-xs text-muted">{team.serviceCount} {t('governance.finops.services')}</span>
                <div className="hidden md:flex items-center gap-1 text-xs text-muted">
                  <Activity size={12} className={team.avgReliabilityScore >= 90 ? 'text-success' : team.avgReliabilityScore >= 70 ? 'text-warning' : 'text-critical'} />
                  {team.avgReliabilityScore}%
                </div>
                {team.wasteAmount > 0 && (
                  <Badge variant="danger" className="text-[10px]">{t('governance.finops.wasteAmount')}: {fmt(team.wasteAmount)}</Badge>
                )}
                <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(team.monthlyCost)}</span>
                <ArrowRight size={14} className="text-muted" />
              </Link>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Top waste services */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <AlertTriangle size={16} className="text-critical" />
            {t('governance.finops.topWasteServices')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.topWasteServices.map(svc => (
              <Link key={svc.serviceId} to={`/governance/finops/services/${svc.serviceId}`} className="px-4 py-3 hover:bg-hover transition-colors flex items-center gap-3">
                {efficiencyIcon(svc.efficiency)}
                <span className="text-sm font-medium text-heading flex-1 truncate">{svc.serviceName}</span>
                <span className="text-xs text-muted">{svc.team}</span>
                <Badge variant={efficiencyBadgeVariant(svc.efficiency)}>{t(`governance.finops.efficiency.${svc.efficiency}`)}</Badge>
                <span className="text-sm font-mono font-medium text-critical w-24 text-right">{fmt(svc.wasteAmount)}</span>
                <ArrowRight size={14} className="text-muted" />
              </Link>
            ))}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
