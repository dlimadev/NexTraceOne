import { useTranslation } from 'react-i18next';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  DollarSign, AlertTriangle,
  XCircle, ArrowLeft,
  Layers, ArrowRight,
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
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import {
  formatCurrency,
  efficiencyBadgeVariant,
  efficiencyIcon,
  trendIcon,
} from '../utils/finOpsFormatters';
import { useFinOpsCurrency } from '../hooks/useFinOpsConfig';

export function DomainFinOpsPage() {
  const { t, i18n } = useTranslation();
  const { domainId } = useParams<{ domainId: string }>();
  const { activeEnvironmentId } = useEnvironment();
  const currency = useFinOpsCurrency();
  const fmt = (v: number) => formatCurrency(v, i18n.language, currency);

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.finops.domain(domainId!, activeEnvironmentId),
    queryFn: () => finOpsApi.getDomainFinOps(domainId!),
    staleTime: 30_000,
    enabled: !!domainId,
  });

  if (isLoading) return (<PageContainer><PageLoadingState /></PageContainer>);
  if (isError || !d) return (<PageContainer><PageErrorState action={<button onClick={() => refetch()} className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">{t('common.retry')}</button>} /></PageContainer>);

  const totalWaste = d.topWasteSignals.reduce((sum, ws) => sum + ws.estimatedWaste, 0);

  return (
    <PageContainer>
      <div className="mb-6">
        <Link to="/governance/finops" className="inline-flex items-center gap-1 text-sm text-accent hover:text-accent-hover mb-3">
          <ArrowLeft size={14} />
          {t('governance.finops.backToOverview')}
        </Link>
        <div className="flex items-center gap-3">
          <Layers size={24} className="text-accent" />
          <h1 className="text-2xl font-bold text-heading">{d.domainName}</h1>
          <Badge variant={efficiencyBadgeVariant(d.efficiency)}>{t(`governance.finops.efficiency.${d.efficiency}`)}</Badge>
        </div>
        <p className="text-muted mt-1">{d.services.length} {t('governance.finops.services')} · {t('governance.finops.domainFinOpsProfile')}</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('governance.finops.totalMonthlyCost')} value={fmt(d.totalCost)} icon={<DollarSign size={20} />} color="text-accent" />
        <StatCard title={t('governance.finops.totalWaste')} value={fmt(totalWaste)} icon={<AlertTriangle size={20} />} color="text-critical" />
        <StatCard title={t('governance.finops.overallEfficiency')} value={t(`governance.finops.efficiency.${d.efficiency}`)} icon={<Layers size={20} />} color="text-warning" />
        <StatCard title={t('governance.finops.services')} value={String(d.services.length)} icon={<Layers size={20} />} color="text-info" />
      </div>

      {/* Services */}
      <Card className="mb-6">
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
                  {trendIcon(svc.trend)}
                  {t(`governance.finops.trend.${svc.trend}`)}
                </div>
                <span className="text-sm font-mono font-medium text-heading w-24 text-right">{fmt(svc.monthlyCost)}</span>
                <ArrowRight size={14} className="text-muted" />
              </Link>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Waste Signals */}
      {d.topWasteSignals.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-critical" />
              {t('governance.finops.wasteSignals')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-edge">
              {d.topWasteSignals.map((ws, i) => (
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
      )}
    </PageContainer>
  );
}
