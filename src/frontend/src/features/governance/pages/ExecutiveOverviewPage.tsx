import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  TrendingUp, ShieldAlert, AlertTriangle, BarChart3, Activity,
  CheckCircle, RefreshCw, Clock, Target, AlertCircle,
  Zap,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import type {
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
 * Página principal do dashboard executivo — redesenhada com hero KPIs,
 * painel "Immediate Action" e hierarquia visual por urgência.
 *
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F3-04
 */
export function ExecutiveOverviewPage() {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['executive-overview'],
    queryFn: () => organizationGovernanceApi.getExecutiveOverview(),
    staleTime: 60_000,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState size="lg" />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <PageErrorState message={t('common.errorLoading')} />
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

  const criticalFocusAreas = d.criticalFocusAreas.filter(a => a.severity === 'Critical' || a.severity === 'High');

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.executive.overviewTitle')}
        subtitle={t('governance.executive.overviewSubtitle')}
      />

      {/* ── Hero KPIs ─────────────────────────────────────────────────────── */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          {/* Overall Risk — primary KPI, biggest visual weight */}
          <div className="col-span-2 md:col-span-1 bg-card border border-edge rounded-xl p-5 flex flex-col gap-2">
            <p className="text-xs font-medium text-muted uppercase tracking-wider">
              {t('governance.executive.overallRisk')}
            </p>
            <div className="flex items-end gap-2">
              <Badge variant={riskBadgeVariant(d.riskSummary.overallRisk)} size="md">
                {t(`governance.risk.level.${d.riskSummary.overallRisk}`)}
              </Badge>
            </div>
            <p className="text-xs text-muted">
              {d.riskSummary.criticalDomains} {t('governance.executive.criticalDomains').toLowerCase()}
            </p>
          </div>

          <StatCard
            title={t('governance.executive.openIncidents')}
            value={d.incidentTrendSummary.openIncidents}
            icon={<AlertCircle size={20} />}
            color="text-critical"
            href="/operations/incidents"
            ariaLabel={t('governance.executive.openIncidents')}
          />
          <StatCard
            title={t('governance.executive.highRiskServices')}
            value={d.riskSummary.highRiskServices}
            icon={<AlertTriangle size={20} />}
            color="text-warning"
            href="/governance/risk"
            ariaLabel={t('governance.executive.highRiskServices')}
          />
          <StatCard
            title={t('governance.executive.stabilityTrend')}
            value={t(`governance.trend.${d.operationalTrend.stabilityTrend}`)}
            icon={<Activity size={20} />}
            color="text-accent"
          />
        </div>
      </PageSection>

      {/* ── Immediate Action Panel (critical focus areas) ──────────────────── */}
      {criticalFocusAreas.length > 0 && (
        <PageSection>
          <div className="mb-6 rounded-xl border border-critical/30 bg-critical/5 overflow-hidden">
            <div className="flex items-center gap-2 px-4 py-3 border-b border-critical/20 bg-critical/10">
              <Zap size={15} className="text-critical" aria-hidden="true" />
              <h2 className="text-sm font-semibold text-critical">
                {t('governance.executive.immediateAction', 'Immediate Action Required')}
              </h2>
              <Badge variant="danger" size="sm" className="ml-auto">
                {criticalFocusAreas.length}
              </Badge>
            </div>
            <div className="divide-y divide-critical/10">
              {criticalFocusAreas.map((area, idx) => (
                <div key={idx} className="flex items-start gap-4 px-4 py-3">
                  <AlertTriangle size={14} className="text-critical mt-0.5 shrink-0" aria-hidden="true" />
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 mb-0.5">
                      <span className="text-sm font-medium text-heading">{area.areaName}</span>
                      <Badge variant={riskBadgeVariant(area.severity)} size="sm">
                        {t(`governance.risk.level.${area.severity}`)}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted">{area.description}</p>
                  </div>
                  <span className="text-xs text-muted shrink-0 mt-0.5">
                    {area.affectedServices} {t('governance.executive.affectedServices')}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </PageSection>
      )}

      {/* ── Operational Trend + Risk Summary ──────────────────────────────── */}
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
          {/* Operational Trend */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <TrendingUp size={16} className="text-accent" aria-hidden="true" />
                {t('governance.executive.operationalTrend')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="flex flex-col gap-4">
                <StatCard
                  title={t('governance.executive.incidentRateChange')}
                  value={`${d.operationalTrend.incidentRateChange}%`}
                  icon={<AlertTriangle size={20} />}
                  color={d.operationalTrend.incidentRateChange < 0 ? 'text-success' : 'text-critical'}
                  trend={
                    d.operationalTrend.incidentRateChange < 0
                      ? { direction: 'down', label: t('governance.trend.Improving') }
                      : { direction: 'up', label: t('governance.trend.Declining') }
                  }
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
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <ShieldAlert size={16} className="text-accent" aria-hidden="true" />
                {t('governance.executive.riskSummary')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 gap-4">
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
                  color="text-warning"
                />
                <div className="col-span-2 flex items-center gap-2">
                  <p className="text-xs text-muted">{t('governance.executive.riskTrend')}:</p>
                  <Badge variant={trendBadgeVariant(d.riskSummary.riskTrend)}>
                    {t(`governance.trend.${d.riskSummary.riskTrend}`)}
                  </Badge>
                </div>
              </div>
            </CardBody>
          </Card>
        </div>
      </PageSection>

      {/* ── Maturity Summary ──────────────────────────────────────────────── */}
      <PageSection>
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <BarChart3 size={16} className="text-accent" aria-hidden="true" />
              {t('governance.executive.maturitySummary')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {maturityItems.map(item => {
                const barColor = item.value >= 80 ? 'bg-success' : item.value >= 60 ? 'bg-warning' : 'bg-critical';
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
      </PageSection>

      {/* ── Change Safety + Incident Trend ────────────────────────────────── */}
      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
          {/* Change Safety */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <RefreshCw size={16} className="text-accent" aria-hidden="true" />
                {t('governance.executive.changeSafety')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 gap-4">
                <StatCard
                  title={t('governance.executive.safeChanges')}
                  value={d.changeSafetySummary.safeChanges}
                  icon={<CheckCircle size={20} />}
                  color="text-success"
                />
                <StatCard
                  title={t('governance.executive.riskyChanges')}
                  value={d.changeSafetySummary.riskyChanges}
                  icon={<AlertTriangle size={20} />}
                  color="text-warning"
                />
                <StatCard
                  title={t('governance.executive.rollbacks')}
                  value={d.changeSafetySummary.rollbacks}
                  icon={<RefreshCw size={20} />}
                  color="text-critical"
                />
                <div className="flex flex-col justify-center">
                  <p className="text-xs text-muted mb-1">{t('governance.executive.confidenceTrend')}</p>
                  <Badge variant={trendBadgeVariant(d.changeSafetySummary.confidenceTrend)}>
                    {t(`governance.trend.${d.changeSafetySummary.confidenceTrend}`)}
                  </Badge>
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Incident Trend */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Activity size={16} className="text-accent" aria-hidden="true" />
                {t('governance.executive.incidentTrend')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 gap-4">
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
                  color="text-success"
                />
                <StatCard
                  title={t('governance.executive.recurrenceRate')}
                  value={`${d.incidentTrendSummary.recurrenceRate}%`}
                  icon={<RefreshCw size={20} />}
                  color="text-warning"
                />
                <div className="flex flex-col justify-center">
                  <p className="text-xs text-muted mb-1">{t('governance.executive.incidentTrend')}</p>
                  <Badge variant={trendBadgeVariant(d.incidentTrendSummary.trend)}>
                    {t(`governance.trend.${d.incidentTrendSummary.trend}`)}
                  </Badge>
                </div>
              </div>
            </CardBody>
          </Card>
        </div>
      </PageSection>

      {/* ── All Focus Areas + Top Domains ─────────────────────────────────── */}
      <PageSection>
        {/* Critical Focus Areas (full list) */}
        <Card className="mb-6">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Target size={16} className="text-accent" aria-hidden="true" />
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
              <ShieldAlert size={16} className="text-accent" aria-hidden="true" />
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
