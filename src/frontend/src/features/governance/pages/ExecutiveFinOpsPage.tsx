import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  DollarSign, AlertTriangle,
  Layers,
  Target, Gauge,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { StatCard } from '../../../components/StatCard';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { finOpsApi } from '../api/finOps';
import { queryKeys } from '../../../shared/api/queryKeys';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { formatCurrency } from '../utils/finOpsFormatters';

export function ExecutiveFinOpsPage() {
  const { t, i18n } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const fmt = (v: number) => formatCurrency(v, i18n.language);

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.finops.summary(undefined, activeEnvironmentId),
    queryFn: () => finOpsApi.getSummary(),
    staleTime: 30_000,
  });

  if (isLoading) return (<PageContainer><PageLoadingState /></PageContainer>);
  if (isError || !d) return (<PageContainer><PageErrorState action={<button onClick={() => refetch()} className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">{t('common.retry')}</button>} /></PageContainer>);

  const totalPotentialSavings = d.optimizationOpportunities.reduce((sum, opt) => sum + opt.potentialSavings, 0);
  const wastePercent = d.totalMonthlyCost > 0 ? ((d.totalWaste / d.totalMonthlyCost) * 100).toFixed(1) : '0';

  const domainMap: Record<string, { domainName: string; monthlyCost: number; serviceCount: number }> = {};
  for (const svc of d.services) {
    if (!domainMap[svc.domain]) {
      domainMap[svc.domain] = { domainName: svc.domain, monthlyCost: 0, serviceCount: 0 };
    }
    domainMap[svc.domain]!.monthlyCost += svc.monthlyCost;
    domainMap[svc.domain]!.serviceCount += 1;
  }
  const domainBreakdown = Object.values(domainMap);

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.finops.executiveTitle')}
        subtitle={t('governance.finops.executiveSubtitle')}
      />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalMonthlyCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={`${fmt(d.totalWaste)} (${wastePercent}%)`} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard
          title={t('governance.finops.overallEfficiency')}
          value={t(`governance.finops.efficiency.${d.overallEfficiency}`)}
          icon={<Gauge size={20} />}
          color="text-warning"
        />
        <StatCard title={t('governance.finops.potentialSavings')} value={fmt(totalPotentialSavings)} icon={<Target size={20} />} color="text-success" />
      </div>

      {/* Domain breakdown + Optimization highlights */}
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
              {domainBreakdown.map(dom => (
                <div key={dom.domainName} className="px-4 py-3 flex items-center gap-3">
                  <Layers size={14} className="text-accent shrink-0" />
                  <span className="text-sm font-medium text-heading flex-1 truncate">{dom.domainName}</span>
                  <span className="text-xs text-muted">{dom.serviceCount} {t('governance.finops.services')}</span>
                  <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(dom.monthlyCost)}</span>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Target size={16} className="text-success" />
              {t('governance.finops.optimizationHighlights')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.optimizationOpportunities.map((opt, i) => (
                <div key={i} className="px-4 py-3">
                  <div className="flex items-center gap-3">
                    <Target size={14} className="text-success shrink-0" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-heading">{opt.recommendation}</p>
                      <p className="text-xs text-muted mt-0.5">{opt.serviceName}</p>
                    </div>
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
