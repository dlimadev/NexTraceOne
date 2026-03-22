import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Crosshair, ShieldAlert, Award, BarChart3, AlertTriangle,
  AlertCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import type {
  RiskLevel,
  GovernanceTrendDirection,
  MaturityLevelType,
} from '../../../types';
import { executiveApi } from '../api/executive';
import { queryKeys } from '../../../shared/api/queryKeys';
/** Mapeia RiskLevel para variante do Badge. */
const riskBadgeVariant = (level: RiskLevel): 'success' | 'warning' | 'danger' | 'default' => {
  switch (level) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'warning';
    case 'Low': return 'success';
    default: return 'default';
  }
};

/** Mapeia GovernanceTrendDirection para variante do Badge. */
const trendBadgeVariant = (dir: GovernanceTrendDirection): 'success' | 'info' | 'danger' => {
  switch (dir) {
    case 'Improving': return 'success';
    case 'Stable': return 'info';
    case 'Declining': return 'danger';
    default: return 'info';
  }
};

/** Mapeia MaturityLevelType para variante do Badge. */
const maturityBadgeVariant = (level: MaturityLevelType): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (level) {
    case 'Optimizing': return 'success';
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
};

/**
 * Página de Drill-Down Executivo — visão detalhada de entidade com indicadores,
 * serviços críticos, gaps e recomendações de foco.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function ExecutiveDrillDownPage() {
  const { t } = useTranslation();
  const { entityType, entityId } = useParams<{ entityType: string; entityId: string }>();

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.executive.drillDown(entityType ?? '', entityId ?? ''),
    queryFn: () => executiveApi.getDrillDown(entityType!, entityId!),
    staleTime: 30_000,
    enabled: !!entityType && !!entityId,
  });

  if (isLoading) return (<PageContainer><PageLoadingState /></PageContainer>);
  if (isError || !d) return (<PageContainer><PageErrorState action={<button onClick={() => refetch()} className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">{t('common.retry')}</button>} /></PageContainer>);

  return (
    <PageContainer>
      {/* Header */}
      <PageHeader
        title={t('governance.executive.drillDownTitle')}
        subtitle={t('governance.executive.drillDownSubtitle', {
          entityType: entityType ?? d.entityType,
          entityName: d.entityName,
        })}
      >
        <div className="flex flex-wrap items-center gap-3 mt-2">
          <div className="flex items-center gap-2">
            <ShieldAlert size={14} className="text-muted" />
            <span className="text-xs text-muted">{t('governance.executive.drillDownRiskLevel')}</span>
            <Badge variant={riskBadgeVariant(d.riskLevel)}>
              {t(`governance.risk.level.${d.riskLevel}`)}
            </Badge>
          </div>
          <div className="flex items-center gap-2">
            <Award size={14} className="text-muted" />
            <span className="text-xs text-muted">{t('governance.executive.drillDownMaturity')}</span>
            <Badge variant={maturityBadgeVariant(d.maturityLevel)}>
              {t(`governance.maturity.${d.maturityLevel}`)}
            </Badge>
          </div>
        </div>
      </PageHeader>

      {/* Key Indicators */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <BarChart3 size={16} className="text-accent" />
            {t('governance.executive.drillDownKeyIndicators')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {d.keyIndicators.map((ind, idx) => (
              <div key={idx} className="bg-surface/50 rounded-md p-4 border border-edge/50">
                <div className="flex items-center justify-between mb-1">
                  <p className="text-xs font-medium text-heading">{ind.name}</p>
                  <Badge variant={trendBadgeVariant(ind.trend)}>
                    {t(`governance.trend.${ind.trend}`)}
                  </Badge>
                </div>
                <p className="text-xl font-bold text-heading mb-1">{ind.value}</p>
                <p className="text-xs text-muted">{ind.explanation}</p>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Critical Services */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <AlertTriangle size={16} className="text-accent" />
            {t('governance.executive.drillDownCriticalServices')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.criticalServices.map(svc => (
              <div key={svc.serviceId} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-sm font-medium text-heading">{svc.serviceName}</span>
                    <Badge variant={riskBadgeVariant(svc.riskLevel)}>
                      {t(`governance.risk.level.${svc.riskLevel}`)}
                    </Badge>
                  </div>
                  <p className="text-xs text-muted">{svc.mainIssue}</p>
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Top Gaps */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <AlertCircle size={16} className="text-accent" />
            {t('governance.executive.drillDownTopGaps')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {d.topGaps.map((gap, idx) => (
              <div key={idx} className="bg-surface/50 rounded-md p-4 border border-edge/50">
                <div className="flex items-center gap-2 mb-2">
                  <span className="text-sm font-medium text-heading">{gap.area}</span>
                  <Badge variant={riskBadgeVariant(gap.severity)}>
                    {t(`governance.risk.level.${gap.severity}`)}
                  </Badge>
                </div>
                <p className="text-xs text-body mb-2">{gap.description}</p>
                <p className="text-xs text-muted italic">{gap.recommendation}</p>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Recommended Focus */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Crosshair size={16} className="text-accent" />
            {t('governance.executive.drillDownRecommendedFocus')}
          </h2>
        </CardHeader>
        <CardBody>
          <ol className="space-y-2">
            {d.recommendedFocus.map((item, idx) => (
              <li key={idx} className="flex items-start gap-3">
                <span className="shrink-0 w-6 h-6 rounded-full bg-accent/10 text-accent text-xs font-medium flex items-center justify-center">
                  {idx + 1}
                </span>
                <span className="text-sm text-body">{item}</span>
              </li>
            ))}
          </ol>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
