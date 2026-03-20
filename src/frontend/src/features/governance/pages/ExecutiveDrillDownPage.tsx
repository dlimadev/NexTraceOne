import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import {
  Crosshair, ShieldAlert, Award, BarChart3, AlertTriangle,
  AlertCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type {
  ExecutiveDrillDownResponse,
  RiskLevel,
  GovernanceTrendDirection,
  MaturityLevelType,
} from '../../../types';

/**
 * Dados simulados do drill-down executivo — alinhados com o backend GetExecutiveDrillDown.
 * Em produção, virão da API /api/v1/governance/executive/drilldown/:entityType/:entityId.
 */
const mockDrillDown: ExecutiveDrillDownResponse = {
  entityType: 'domain',
  entityId: 'dom-payments',
  entityName: 'Payments',
  riskLevel: 'Critical',
  maturityLevel: 'Managed',
  keyIndicators: [
    { name: 'Service Count', value: '8', trend: 'Stable', explanation: 'Number of active services in this domain' },
    { name: 'Open Incidents', value: '5', trend: 'Declining', explanation: 'Active incidents impacting production availability' },
    { name: 'Change Confidence', value: '68%', trend: 'Improving', explanation: 'Percentage of changes deployed safely in last 30 days' },
    { name: 'Contract Coverage', value: '72%', trend: 'Improving', explanation: 'Services with versioned and validated contracts' },
    { name: 'Runbook Coverage', value: '38%', trend: 'Stable', explanation: 'Services with operational runbooks available' },
    { name: 'Avg Resolution Time', value: '4.5h', trend: 'Improving', explanation: 'Average time to resolve production incidents' },
  ],
  criticalServices: [
    { serviceId: 'svc-payment-gateway', serviceName: 'Payment Gateway', riskLevel: 'Critical', mainIssue: 'Recurring production incidents with SLA breach risk' },
    { serviceId: 'svc-payment-processor', serviceName: 'Payment Processor', riskLevel: 'Critical', mainIssue: 'High dependency fragility and cascading failure potential' },
    { serviceId: 'svc-refund-engine', serviceName: 'Refund Engine', riskLevel: 'High', mainIssue: 'Missing runbook and incomplete dependency mapping' },
    { serviceId: 'svc-payment-reconciliation', serviceName: 'Payment Reconciliation', riskLevel: 'High', mainIssue: 'Contract versioning not enforced, schema drift detected' },
    { serviceId: 'svc-fraud-detection', serviceName: 'Fraud Detection', riskLevel: 'Medium', mainIssue: 'Documentation gaps and limited change validation' },
  ],
  topGaps: [
    { area: 'Runbook Coverage', severity: 'Critical', description: 'Only 3 of 8 services have operational runbooks', recommendation: 'Prioritize runbook creation for Payment Gateway and Payment Processor' },
    { area: 'Incident Recurrence', severity: 'Critical', description: '22% incident recurrence rate in the last 30 days', recommendation: 'Conduct root cause analysis for top recurring incidents and create prevention measures' },
    { area: 'Contract Validation', severity: 'High', description: 'Schema drift detected between Payment Reconciliation and downstream consumers', recommendation: 'Enforce contract validation in CI/CD pipeline for all payment services' },
    { area: 'Dependency Mapping', severity: 'High', description: 'Incomplete topology mapping for cascading failure analysis', recommendation: 'Complete dependency registration for all services in the domain' },
    { area: 'AI Governance', severity: 'Medium', description: 'No AI governance policies defined for the domain', recommendation: 'Define AI usage policies aligned with platform AI governance framework' },
  ],
  recommendedFocus: [
    'Stabilize Payment Gateway with dedicated incident response',
    'Create operational runbooks for all critical-path services',
    'Enforce contract versioning and validation in deployment pipeline',
    'Complete dependency topology for blast radius analysis',
    'Reduce incident recurrence through systematic root cause analysis',
  ],
  generatedAt: new Date().toISOString(),
};

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
  const { entityType } = useParams<{ entityType: string; entityId: string }>();

  // Usa dados simulados independente dos parâmetros da rota
  const d = mockDrillDown;

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
        <div className="flex items-center gap-2 mt-2">
          <Badge variant="warning">{t('governance.preview.badge')}</Badge>
          <span className="text-xs text-muted">{t('governance.preview.drilldownReason')}</span>
        </div>
        <div className="flex flex-wrap items-center gap-3 mt-3">
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
