import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  DollarSign, TrendingUp, TrendingDown, Minus, AlertTriangle,
  CheckCircle, AlertCircle, XCircle, Activity, ArrowLeft,
  Zap, Target,
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
export function ServiceFinOpsPage() {
  const { t, i18n } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string }>();
  const fmt = (v: number) => formatCurrency(v, i18n.language);

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.finops.service(serviceId!),
    queryFn: () => finOpsApi.getServiceFinOps(serviceId!),
    staleTime: 30_000,
    enabled: !!serviceId,
  });

  if (isLoading) return (<PageContainer><PageLoadingState /></PageContainer>);
  if (isError || !d) return (<PageContainer><PageErrorState action={<button onClick={() => refetch()} className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">{t('common.retry')}</button>} /></PageContainer>);

  const totalWaste = d.wasteSignals.reduce((sum, ws) => sum + ws.estimatedWaste, 0);
  const totalPotentialSavings = d.optimizationOpportunities.reduce((sum, opt) => sum + opt.potentialSavings, 0);

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
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('governance.finops.reliability')} value={d.reliabilityCorrelation ? `${d.reliabilityCorrelation.reliabilityScore}%` : 'N/A'} icon={<Activity size={20} />} color={d.reliabilityCorrelation ? (d.reliabilityCorrelation.reliabilityScore >= 90 ? 'text-success' : d.reliabilityCorrelation.reliabilityScore >= 70 ? 'text-warning' : 'text-critical') : 'text-muted'} />
        <StatCard title={t('governance.finops.potentialSavings')} value={fmt(totalPotentialSavings)} icon={<Target size={20} />} color="text-success" />
      </div>

      {/* Cost trend summary */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex items-center gap-4 text-sm">
            {trendIcon(d.trend)}
            <span className="text-body">{t(`governance.finops.trend.${d.trend}`)}</span>
            {d.reliabilityCorrelation && (
              <>
                <span className="text-muted">·</span>
                <span className="text-muted">{t('governance.finops.incidents')}: {d.reliabilityCorrelation.recentIncidents}</span>
                <span className="text-muted">·</span>
                <span className="text-muted">{t('governance.finops.reliabilityTrend')}: {t(`governance.finops.trend.${d.reliabilityCorrelation.reliabilityTrend}`)}</span>
              </>
            )}
          </div>
        </CardBody>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
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
              {d.wasteSignals.map((ws, i) => (
                <div key={i} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <XCircle size={14} className="text-critical shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{ws.description}</p>
                      <p className="text-xs text-muted mt-0.5">{ws.pattern}</p>
                    </div>
                    <span className="text-sm font-mono font-medium text-critical shrink-0">{fmt(ws.estimatedWaste)}</span>
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
              {d.optimizationOpportunities.map((opt, i) => (
                <div key={i} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <Zap size={14} className={opt.priority === 'High' ? 'text-critical' : 'text-warning'} />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{opt.recommendation}</p>
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
