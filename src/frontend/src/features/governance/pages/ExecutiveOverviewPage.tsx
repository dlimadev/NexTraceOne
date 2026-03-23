import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  TrendingUp, ShieldAlert, AlertTriangle, BarChart3, Activity,
  CheckCircle, RefreshCw, Clock, Target, AlertCircle, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type {
  ExecutiveOverviewResponse,
  RiskLevel,
  GovernanceTrendDirection,
} from '../../../types';
import { organizationGovernanceApi } from '../api/organizationGovernance';



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
  const [data, setData] = useState<ExecutiveOverviewResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setLoading(true);
    // eslint-disable-next-line react-hooks/set-state-in-effect -- synchronous setState before async fetch is intentional
    setError(null);
    organizationGovernanceApi.getExecutiveOverview()
      .then((d) => { if (!cancelled) { setData(d); setLoading(false); } })
      .catch((err) => { if (!cancelled) { setError(err.message || t('common.errorLoading')); setLoading(false); } });
    return () => { cancelled = true; };
  }, [t]);

  if (loading) {
    return (
      <PageContainer>
        <div className="flex items-center justify-center py-20">
          <Loader2 size={32} className="animate-spin text-accent" />
        </div>
      </PageContainer>
    );
  }

  if (error || !data) {
    return (
      <PageContainer>
        <div className="flex flex-col items-center justify-center py-20 gap-4">
          <AlertTriangle size={48} className="text-critical" />
          <p className="text-sm text-muted">{error ?? t('common.errorLoading')}</p>
        </div>
      </PageContainer>
    );
  }

  const d = data;

  const maturityItems = [
    { key: 'ownershipCoverage', value: d.maturitySummary.ownershipCoverage },
    { key: 'contractCoverage', value: d.maturitySummary.contractCoverage },
    { key: 'documentationCoverage', value: d.maturitySummary.documentationCoverage },
    { key: 'runbookCoverage', value: d.maturitySummary.runbookCoverage },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.executive.overviewTitle')}
        subtitle={t('governance.executive.overviewSubtitle')}
      />

      {/* Stat Cards */}
      <PageSection>
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
      </PageSection>

      {/* Critical Focus Areas & Domains */}
      <PageSection>
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
      </PageSection>
    </PageContainer>
  );
}
