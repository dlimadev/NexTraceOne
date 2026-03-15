import { useTranslation } from 'react-i18next';
import {
  TrendingUp, ShieldAlert, AlertTriangle, BarChart3, Activity,
  CheckCircle, RefreshCw, Clock, Target, AlertCircle,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import type {
  ExecutiveOverviewResponse,
  RiskLevel,
  GovernanceTrendDirection,
} from '../../../types';

/**
 * Dados simulados do overview executivo — alinhados com o backend GetExecutiveOverview.
 * Em produção, virão da API /api/v1/governance/executive/overview.
 */
const mockOverview: ExecutiveOverviewResponse = {
  operationalTrend: {
    stabilityTrend: 'Improving',
    incidentRateChange: -12.5,
    avgResolutionHours: 4.2,
  },
  riskSummary: {
    overallRisk: 'Medium',
    criticalDomains: 2,
    highRiskServices: 5,
    riskTrend: 'Stable',
  },
  maturitySummary: {
    overallMaturity: 'Defined',
    ownershipCoverage: 88,
    contractCoverage: 72,
    documentationCoverage: 64,
    runbookCoverage: 45,
  },
  changeSafetySummary: {
    safeChanges: 184,
    riskyChanges: 23,
    rollbacks: 7,
    confidenceTrend: 'Improving',
  },
  incidentTrendSummary: {
    openIncidents: 8,
    resolvedLast30Days: 34,
    avgResolutionHours: 4.2,
    recurrenceRate: 14,
    trend: 'Improving',
  },
  complianceCoverageSummary: {
    overallScore: 74,
    compliantPct: 68,
    gapCount: 12,
    trend: 'Stable',
  },
  criticalFocusAreas: [
    { areaName: 'Payment Processing Stability', severity: 'Critical', description: 'Recurring incidents in payment gateway affecting SLA', affectedServices: 4 },
    { areaName: 'Contract Coverage Gaps', severity: 'High', description: 'Multiple services without versioned contracts in Payments and Orders domains', affectedServices: 8 },
    { areaName: 'Runbook Deficiency', severity: 'High', description: 'Critical services operating without runbooks', affectedServices: 12 },
    { areaName: 'Dependency Mapping', severity: 'Medium', description: 'Incomplete dependency topology for platform services', affectedServices: 6 },
  ],
  topDomainsRequiringAttention: [
    { domainId: 'dom-payments', domainName: 'Payments', riskLevel: 'Critical', reason: 'Recurring production incidents and SLA breaches' },
    { domainId: 'dom-orders', domainName: 'Orders', riskLevel: 'High', reason: 'Breaking contract changes without validation' },
    { domainId: 'dom-inventory', domainName: 'Inventory', riskLevel: 'High', reason: 'Consumer lag and missing ownership definitions' },
    { domainId: 'dom-platform', domainName: 'Platform', riskLevel: 'Medium', reason: 'Documentation and runbook gaps across shared services' },
    { domainId: 'dom-identity', domainName: 'Identity', riskLevel: 'Low', reason: 'Minor schema mismatch detected in staging' },
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

/**
 * Página principal do dashboard executivo — visão holística do estado operacional,
 * risco, maturidade, mudanças e incidentes.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function ExecutiveOverviewPage() {
  const { t } = useTranslation();
  const d = mockOverview;

  const maturityItems = [
    { key: 'ownershipCoverage', value: d.maturitySummary.ownershipCoverage },
    { key: 'contractCoverage', value: d.maturitySummary.contractCoverage },
    { key: 'documentationCoverage', value: d.maturitySummary.documentationCoverage },
    { key: 'runbookCoverage', value: d.maturitySummary.runbookCoverage },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.executive.overviewTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.executive.overviewSubtitle')}</p>
      </div>

      {/* Operational Trend */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <TrendingUp size={16} className="text-accent" />
            {t('governance.executive.operationalTrend')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <StatCard
              title={t('governance.executive.stabilityTrend')}
              value={t(`governance.trend.${d.operationalTrend.stabilityTrend}`)}
              icon={<Activity size={20} />}
              color="text-accent"
            />
            <StatCard
              title={t('governance.executive.incidentRateChange')}
              value={`${d.operationalTrend.incidentRateChange}%`}
              icon={<AlertTriangle size={20} />}
              color={d.operationalTrend.incidentRateChange < 0 ? 'text-emerald-500' : 'text-critical'}
            />
            <StatCard
              title={t('governance.executive.avgResolutionHours')}
              value={`${d.operationalTrend.avgResolutionHours}h`}
              icon={<Clock size={20} />}
              color="text-accent"
            />
          </div>
        </CardBody>
      </Card>

      {/* Risk Summary */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <ShieldAlert size={16} className="text-accent" />
            {t('governance.executive.riskSummary')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center">
              <p className="text-xs text-muted mb-1">{t('governance.executive.overallRisk')}</p>
              <Badge variant={riskBadgeVariant(d.riskSummary.overallRisk)}>
                {t(`governance.risk.level.${d.riskSummary.overallRisk}`)}
              </Badge>
            </div>
            <StatCard
              title={t('governance.executive.criticalDomains')}
              value={d.riskSummary.criticalDomains}
              icon={<AlertCircle size={20} />}
              color="text-critical"
            />
            <StatCard
              title={t('governance.executive.highRiskServices')}
              value={d.riskSummary.highRiskServices}
              icon={<AlertTriangle size={20} />}
              color="text-orange-500"
            />
            <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center">
              <p className="text-xs text-muted mb-1">{t('governance.executive.riskTrend')}</p>
              <Badge variant={trendBadgeVariant(d.riskSummary.riskTrend)}>
                {t(`governance.trend.${d.riskSummary.riskTrend}`)}
              </Badge>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Maturity Summary */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <BarChart3 size={16} className="text-accent" />
            {t('governance.executive.maturitySummary')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {maturityItems.map(item => {
              const barColor = item.value >= 80 ? 'bg-emerald-500' : item.value >= 60 ? 'bg-amber-500' : 'bg-critical';
              return (
                <div key={item.key}>
                  <div className="flex items-center justify-between mb-1">
                    <p className="text-xs text-muted">{t(`governance.executive.${item.key}`)}</p>
                    <p className="text-xs font-medium text-heading">{item.value}%</p>
                  </div>
                  <div className="w-full bg-surface rounded-full h-2">
                    <div
                      className={`${barColor} rounded-full h-2 transition-all`}
                      style={{ width: `${item.value}%` }}
                    />
                  </div>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* Change Safety */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <RefreshCw size={16} className="text-accent" />
            {t('governance.executive.changeSafety')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatCard
              title={t('governance.executive.safeChanges')}
              value={d.changeSafetySummary.safeChanges}
              icon={<CheckCircle size={20} />}
              color="text-emerald-500"
            />
            <StatCard
              title={t('governance.executive.riskyChanges')}
              value={d.changeSafetySummary.riskyChanges}
              icon={<AlertTriangle size={20} />}
              color="text-orange-500"
            />
            <StatCard
              title={t('governance.executive.rollbacks')}
              value={d.changeSafetySummary.rollbacks}
              icon={<RefreshCw size={20} />}
              color="text-critical"
            />
            <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center">
              <p className="text-xs text-muted mb-1">{t('governance.executive.confidenceTrend')}</p>
              <Badge variant={trendBadgeVariant(d.changeSafetySummary.confidenceTrend)}>
                {t(`governance.trend.${d.changeSafetySummary.confidenceTrend}`)}
              </Badge>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Incident Trend */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Activity size={16} className="text-accent" />
            {t('governance.executive.incidentTrend')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            <StatCard
              title={t('governance.executive.openIncidents')}
              value={d.incidentTrendSummary.openIncidents}
              icon={<AlertCircle size={20} />}
              color="text-critical"
            />
            <StatCard
              title={t('governance.executive.resolvedLast30Days')}
              value={d.incidentTrendSummary.resolvedLast30Days}
              icon={<CheckCircle size={20} />}
              color="text-emerald-500"
            />
            <StatCard
              title={t('governance.executive.avgResolutionHours')}
              value={`${d.incidentTrendSummary.avgResolutionHours}h`}
              icon={<Clock size={20} />}
              color="text-accent"
            />
            <StatCard
              title={t('governance.executive.recurrenceRate')}
              value={`${d.incidentTrendSummary.recurrenceRate}%`}
              icon={<RefreshCw size={20} />}
              color="text-amber-500"
            />
            <div className="bg-card rounded-lg shadow-sm border border-edge p-5 flex flex-col items-center justify-center">
              <p className="text-xs text-muted mb-1">{t('governance.executive.incidentTrend')}</p>
              <Badge variant={trendBadgeVariant(d.incidentTrendSummary.trend)}>
                {t(`governance.trend.${d.incidentTrendSummary.trend}`)}
              </Badge>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Critical Focus Areas */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Target size={16} className="text-accent" />
            {t('governance.executive.criticalFocusAreas')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.criticalFocusAreas.map((area, idx) => (
              <div key={idx} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-sm font-medium text-heading">{area.areaName}</span>
                    <Badge variant={riskBadgeVariant(area.severity)}>
                      {t(`governance.risk.level.${area.severity}`)}
                    </Badge>
                  </div>
                  <p className="text-xs text-muted">{area.description}</p>
                </div>
                <div className="text-xs text-muted shrink-0">
                  {area.affectedServices} {t('governance.executive.affectedServices')}
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Top Domains Requiring Attention */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <ShieldAlert size={16} className="text-accent" />
            {t('governance.executive.topDomainsAttention')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {d.topDomainsRequiringAttention.map(domain => (
              <div key={domain.domainId} className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors">
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-sm font-medium text-heading">{domain.domainName}</span>
                    <Badge variant={riskBadgeVariant(domain.riskLevel)}>
                      {t(`governance.risk.level.${domain.riskLevel}`)}
                    </Badge>
                  </div>
                  <p className="text-xs text-muted">{domain.reason}</p>
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
