import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import {
  DollarSign, TrendingUp, TrendingDown, Minus, AlertTriangle,
  CheckCircle, AlertCircle, XCircle, Activity, ArrowLeft,
  Zap, GitCommit, Target, Gauge,
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

const mockServiceFinOps = {
  serviceId: 'svc-payment-api',
  serviceName: 'Payment API',
  domain: 'Payments',
  team: 'Team Payments',
  monthlyCost: 12500,
  previousMonthCost: 11200,
  costTrend: 'Declining' as const,
  efficiency: 'Inefficient' as CostEfficiencyType,
  totalWaste: 5300,
  reliabilityScore: 72.5,
  recentIncidents: 3,
  reliabilityTrend: 'Declining' as const,
  totalPotentialSavings: 4700,
  wasteSignals: [
    { signalId: 'ws-001', description: 'Excessive retries on timeout', pattern: 'retry-pattern', type: 'ExcessiveRetries', estimatedWaste: 3200, severity: 'High', detectedAt: '2026-03-10T08:00:00Z', correlatedCause: 'Upstream latency causing retry storms' },
    { signalId: 'ws-002', description: 'Over-provisioned compute instances', pattern: 'over-provisioned', type: 'OverProvisioned', estimatedWaste: 2100, severity: 'Medium', detectedAt: '2026-03-12T14:00:00Z', correlatedCause: null },
  ],
  efficiencyIndicators: [
    { name: 'CPU Utilization', category: 'ResourceUtilization', currentValue: 42.5, targetValue: 75.0, unit: '%', assessment: 'Below optimal range' },
    { name: 'Cost per Request', category: 'CostPerTransaction', currentValue: 0.032, targetValue: 0.015, unit: 'USD', assessment: 'Above target threshold' },
    { name: 'Error Rate Impact', category: 'ErrorRate', currentValue: 4.2, targetValue: 1.0, unit: '%', assessment: 'Errors adding operational cost' },
    { name: 'Throughput Efficiency', category: 'ThroughputOptimization', currentValue: 68.0, targetValue: 85.0, unit: '%', assessment: 'Room for improvement' },
  ],
  changeImpacts: [
    { changeId: 'chg-2026-0312', description: 'Deploy v3.2.1 — Payment retry logic', appliedAt: '2026-03-12T10:00:00Z', costImpact: 1200, explanation: 'Cost increase after deployment due to retry amplification' },
    { changeId: 'chg-2026-0305', description: 'Scale-up instance tier', appliedAt: '2026-03-05T16:00:00Z', costImpact: 2800, explanation: 'Planned capacity increase for peak traffic' },
  ],
  optimizations: [
    { recommendation: 'Reduce retry backoff threshold', potentialSavings: 1800, priority: 'High', rationale: 'Excessive retries are adding $1,800/mo in wasted compute' },
    { recommendation: 'Right-size compute instances', potentialSavings: 2100, priority: 'Medium', rationale: 'CPU utilization consistently below 45% — downsize recommended' },
    { recommendation: 'Implement circuit breaker', potentialSavings: 800, priority: 'Medium', rationale: 'Reduce cascading failure cost from upstream timeouts' },
  ],
};

export function ServiceFinOpsPage() {
  const { t, i18n } = useTranslation();
  const { serviceId: _serviceId } = useParams<{ serviceId: string }>();
  const fmt = (v: number) => formatCurrency(v, i18n.language);
  const d = mockServiceFinOps;
  const costChange = d.monthlyCost - d.previousMonthCost;
  const costChangePct = d.previousMonthCost > 0 ? ((costChange / d.previousMonthCost) * 100).toFixed(1) : '0';

  return (
    <PageContainer>
      {/* Back + Header */}
      <div className="mb-6">
        <Link to="/governance/finops" className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent-hover mb-3">
          <ArrowLeft size={14} />
          {t('governance.finops.backToOverview')}
        </Link>
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-heading">{d.serviceName}</h1>
          <Badge variant={efficiencyBadgeVariant(d.efficiency)}>{t(`governance.finops.efficiency.${d.efficiency}`)}</Badge>
        </div>
        <p className="text-muted mt-1">{d.domain} · {d.team} · {t('governance.finops.serviceFinOpsProfile')}</p>
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">{t('governance.preview.finopsReason')}</span>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.monthlyCost')} value={fmt(d.monthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(d.totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('governance.finops.reliability')} value={`${d.reliabilityScore}%`} icon={<Activity size={20} />} color={d.reliabilityScore >= 90 ? 'text-success' : d.reliabilityScore >= 70 ? 'text-warning' : 'text-critical'} />
        <StatCard title={t('governance.finops.potentialSavings')} value={fmt(d.totalPotentialSavings)} icon={<Target size={20} />} color="text-success" />
      </div>

      {/* Cost trend summary */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex items-center gap-4 text-sm">
            {trendIcon(d.costTrend)}
            <span className="text-body">
              {t('governance.finops.costTrendDetail', { change: `${costChange >= 0 ? '+' : ''}${fmt(costChange)}`, percent: costChangePct })}
            </span>
            <span className="text-muted">·</span>
            <span className="text-muted">{t('governance.finops.incidents')}: {d.recentIncidents}</span>
            <span className="text-muted">·</span>
            <span className="text-muted">{t('governance.finops.reliabilityTrend')}: {t(`governance.finops.trend.${d.reliabilityTrend}`)}</span>
          </div>
        </CardBody>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Waste Signals */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-critical" />
              {t('governance.finops.wasteSignals')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.wasteSignals.map(ws => (
                <div key={ws.signalId} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <XCircle size={14} className="text-critical shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{ws.description}</p>
                      <p className="text-xs text-muted mt-0.5">{ws.pattern} · {t('governance.finops.severity')}: <Badge variant={ws.severity === 'High' ? 'danger' : 'warning'} className="text-[10px]">{ws.severity}</Badge></p>
                      {ws.correlatedCause && <p className="text-xs text-muted mt-0.5">{t('governance.finops.correlatedCause')}: {ws.correlatedCause}</p>}
                    </div>
                    <span className="text-sm font-mono font-medium text-critical shrink-0">{fmt(ws.estimatedWaste)}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>

        {/* Efficiency Indicators */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Gauge size={16} className="text-accent" />
              {t('governance.finops.efficiencyIndicators')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.efficiencyIndicators.map((ind, i) => {
                const ratio = ind.targetValue > 0 ? ind.currentValue / ind.targetValue : 0;
                const isGood = ind.category === 'ErrorRate' ? ratio < 1 : ratio >= 0.9;
                return (
                  <div key={i} className="px-4 py-3">
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-sm text-heading">{ind.name}</span>
                      <span className={`text-sm font-mono font-medium ${isGood ? 'text-success' : 'text-warning'}`}>
                        {ind.currentValue}{ind.unit === '%' ? '%' : ` ${ind.unit}`}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="flex-1 h-1.5 bg-elevated rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full ${isGood ? 'bg-success' : 'bg-warning'}`}
                          style={{ width: `${Math.min(100, (ratio * 100))}%` }}
                        />
                      </div>
                      <span className="text-[10px] text-muted">{t('governance.finops.target')}: {ind.targetValue}{ind.unit === '%' ? '%' : ` ${ind.unit}`}</span>
                    </div>
                    <p className="text-xs text-muted mt-0.5">{ind.assessment}</p>
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Change Impacts */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <GitCommit size={16} className="text-info" />
              {t('governance.finops.changeImpacts')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.changeImpacts.map(ci => (
                <div key={ci.changeId} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <GitCommit size={14} className="text-info shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{ci.description}</p>
                      <p className="text-xs text-muted mt-0.5">{ci.explanation}</p>
                    </div>
                    <span className="text-sm font-mono font-medium text-warning shrink-0">+{fmt(ci.costImpact)}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>

        {/* Optimization Opportunities */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Target size={16} className="text-success" />
              {t('governance.finops.optimizationOpportunities')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.optimizations.map((opt, i) => (
                <div key={i} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <Zap size={14} className={opt.priority === 'High' ? 'text-critical' : 'text-warning'} />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{opt.recommendation}</p>
                      <p className="text-xs text-muted mt-0.5">{opt.rationale}</p>
                    </div>
                    <Badge variant={opt.priority === 'High' ? 'danger' : 'warning'}>{opt.priority}</Badge>
                    <span className="text-sm font-mono font-medium text-success shrink-0">{fmt(opt.potentialSavings)}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>
    </PageContainer>
  );
}
