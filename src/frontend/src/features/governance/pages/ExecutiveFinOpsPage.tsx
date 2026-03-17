import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  DollarSign, TrendingUp, TrendingDown, Minus, AlertTriangle,
  CheckCircle, AlertCircle, XCircle, Layers, Users,
  ArrowRight, Target, BarChart3,
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

const trendIcon = (dir: string) => {
  switch (dir) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    default: return <Minus size={14} className="text-muted" />;
  }
};

const mockExecutiveFinOps = {
  totalMonthlyCost: 128500,
  totalWaste: 32400,
  wastePercent: 25.2,
  overallEfficiency: 'Acceptable' as CostEfficiencyType,
  costTrend: 'Declining' as const,
  totalPotentialSavings: 28900,
  aggregatedTrend: [
    { period: '2025-10', cost: 115200 }, { period: '2025-11', cost: 118400 }, { period: '2025-12', cost: 121000 },
    { period: '2026-01', cost: 124200 }, { period: '2026-02', cost: 126800 }, { period: '2026-03', cost: 128500 },
  ],
  domainBreakdown: [
    { domainId: 'domain-commerce', domainName: 'Commerce', monthlyCost: 64800, wasteAmount: 23700, efficiency: 'Inefficient' as CostEfficiencyType, serviceCount: 5, trendDirection: 'Declining' as const },
    { domainId: 'domain-payments', domainName: 'Payments', monthlyCost: 28500, wasteAmount: 5300, efficiency: 'Inefficient' as CostEfficiencyType, serviceCount: 3, trendDirection: 'Declining' as const },
    { domainId: 'domain-identity', domainName: 'Identity', monthlyCost: 18200, wasteAmount: 2100, efficiency: 'Acceptable' as CostEfficiencyType, serviceCount: 4, trendDirection: 'Stable' as const },
    { domainId: 'domain-messaging', domainName: 'Messaging', monthlyCost: 9800, wasteAmount: 800, efficiency: 'Efficient' as CostEfficiencyType, serviceCount: 3, trendDirection: 'Improving' as const },
    { domainId: 'domain-analytics', domainName: 'Analytics', monthlyCost: 7200, wasteAmount: 500, efficiency: 'Efficient' as CostEfficiencyType, serviceCount: 2, trendDirection: 'Stable' as const },
  ],
  wastePressureAreas: [
    { area: 'Commerce — Order Processor', wasteAmount: 7500, reason: 'Rollback reprocessing and idle compute' },
    { area: 'Commerce — Catalog Sync', wasteAmount: 6700, reason: 'Duplicate ETL and idle staging' },
    { area: 'Payments — Payment API', wasteAmount: 5300, reason: 'Retry storms and over-provisioning' },
  ],
  optimizationHighlights: [
    { description: 'Consolidate ETL pipelines in Commerce', potentialSavings: 8200, impactDomains: 'Commerce' },
    { description: 'Right-size Payments infrastructure', potentialSavings: 6400, impactDomains: 'Payments' },
    { description: 'Reduce retry amplification across services', potentialSavings: 5100, impactDomains: 'Commerce, Payments' },
  ],
};

export function ExecutiveFinOpsPage() {
  const { t, i18n } = useTranslation();
  const fmt = (v: number) => formatCurrency(v, i18n.language);
  const d = mockExecutiveFinOps;

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.finops.executiveTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.finops.executiveSubtitle')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={`${fmt(d.totalWaste)} (${d.wastePercent}%)`} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard
          title={t('governance.finops.overallEfficiency')}
          value={t(`governance.finops.efficiency.${d.overallEfficiency}`)}
          icon={<BarChart3 size={20} />}
          color="text-amber-500"
        />
        <StatCard title={t('governance.finops.potentialSavings')} value={fmt(d.totalPotentialSavings)} icon={<Target size={20} />} color="text-success" />
      </div>

      {/* Cost trend */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            {trendIcon(d.costTrend)}
            {t('governance.finops.costTrendOverview')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-end gap-1 h-32">
            {d.aggregatedTrend.map((point, i) => {
              const max = Math.max(...d.aggregatedTrend.map(p => p.cost));
              const min = Math.min(...d.aggregatedTrend.map(p => p.cost));
              const range = max - min || 1;
              const height = 30 + ((point.cost - min) / range) * 70;
              return (
                <div key={i} className="flex-1 flex flex-col items-center gap-1">
                  <span className="text-[9px] text-muted font-mono">{fmt(point.cost)}</span>
                  <div className="w-full bg-accent/20 rounded-t relative" style={{ height: `${height}%` }}>
                    <div className="absolute inset-0 bg-accent/40 rounded-t" />
                  </div>
                  <span className="text-[9px] text-muted">{point.period.slice(5)}</span>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* Domain breakdown + Waste pressure */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Layers size={16} className="text-accent" />
              {t('governance.finops.domainBreakdown')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.domainBreakdown.map(dom => (
                <Link key={dom.domainId} to={`/governance/finops/domains/${dom.domainId}`} className="px-4 py-3 hover:bg-hover transition-colors flex items-center gap-3">
                  <Layers size={14} className="text-accent shrink-0" />
                  <span className="text-sm font-medium text-heading flex-1 truncate">{dom.domainName}</span>
                  <Badge variant={efficiencyBadgeVariant(dom.efficiency)}>{t(`governance.finops.efficiency.${dom.efficiency}`)}</Badge>
                  <div className="hidden md:flex items-center gap-1 text-xs text-muted">
                    {trendIcon(dom.trendDirection)}
                  </div>
                  <span className="text-xs text-muted">{dom.serviceCount} {t('governance.finops.services')}</span>
                  <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(dom.monthlyCost)}</span>
                  <ArrowRight size={14} className="text-muted" />
                </Link>
              ))}
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-critical" />
              {t('governance.finops.wastePressureAreas')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.wastePressureAreas.map((area, i) => (
                <div key={i} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <XCircle size={14} className="text-critical shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{area.area}</p>
                      <p className="text-xs text-muted mt-0.5">{area.reason}</p>
                    </div>
                    <span className="text-sm font-mono font-medium text-critical shrink-0">{fmt(area.wasteAmount)}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Optimization highlights */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Target size={16} className="text-success" />
            {t('governance.finops.optimizationHighlights')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.optimizationHighlights.map((opt, i) => (
              <div key={i} className="px-4 py-3">
                <div className="flex items-center gap-3">
                  <Target size={14} className="text-success shrink-0" />
                  <div className="min-w-0 flex-1">
                    <p className="text-sm text-heading">{opt.description}</p>
                    <p className="text-xs text-muted mt-0.5">{t('governance.finops.impactDomains')}: {opt.impactDomains}</p>
                  </div>
                  <span className="text-sm font-mono font-medium text-success shrink-0">{fmt(opt.potentialSavings)}</span>
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
