import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  BarChart3, TrendingUp, Shield, Activity, RefreshCw,
  Award, DollarSign,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import type {
  BenchmarkingResponse,
  CostEfficiencyType,
} from '../../../types';

type BenchmarkDimension = 'teams' | 'domains';

/**
 * Dados simulados de benchmarking — alinhados com o backend GetBenchmarking.
 * Em produção, virão da API /api/v1/governance/executive/benchmarking.
 */
const mockBenchmarking: BenchmarkingResponse = {
  dimension: 'teams',
  comparisons: [
    {
      groupId: 'team-payment-squad',
      groupName: 'Payment Squad',
      serviceCount: 8,
      criticality: 'Critical',
      reliabilityScore: 72,
      reliabilityTrend: 'Declining',
      changeSafetyScore: 68,
      incidentRecurrenceRate: 22,
      maturityScore: 74,
      riskScore: 82,
      finopsEfficiency: 'Acceptable',
      strengths: ['Ownership maturity', 'Contract versioning', 'Change validation pipeline'],
      gaps: ['Runbook coverage', 'Incident recurrence', 'AI governance'],
      context: 'Handles highest-criticality payment flows with strong ownership but operational stability challenges due to incident recurrence',
    },
    {
      groupId: 'team-order-squad',
      groupName: 'Order Squad',
      serviceCount: 6,
      criticality: 'High',
      reliabilityScore: 65,
      reliabilityTrend: 'Stable',
      changeSafetyScore: 58,
      incidentRecurrenceRate: 18,
      maturityScore: 55,
      riskScore: 71,
      finopsEfficiency: 'Inefficient',
      strengths: ['Ownership definition', 'Dependency awareness'],
      gaps: ['Contract versioning', 'Runbook coverage', 'Change validation', 'FinOps optimization'],
      context: 'Core order processing team with good ownership but contract governance and change safety need improvement',
    },
    {
      groupId: 'team-platform-squad',
      groupName: 'Platform Squad',
      serviceCount: 12,
      criticality: 'High',
      reliabilityScore: 78,
      reliabilityTrend: 'Improving',
      changeSafetyScore: 82,
      incidentRecurrenceRate: 8,
      maturityScore: 62,
      riskScore: 48,
      finopsEfficiency: 'Efficient',
      strengths: ['Change validation', 'Low incident recurrence', 'FinOps efficiency', 'Reliability trend'],
      gaps: ['Documentation', 'Runbook coverage', 'Shared ownership clarity'],
      context: 'Infrastructure team with strong operational practices but shared service ownership and documentation need attention',
    },
    {
      groupId: 'team-identity-squad',
      groupName: 'Identity Squad',
      serviceCount: 4,
      criticality: 'Critical',
      reliabilityScore: 94,
      reliabilityTrend: 'Improving',
      changeSafetyScore: 92,
      incidentRecurrenceRate: 3,
      maturityScore: 88,
      riskScore: 18,
      finopsEfficiency: 'Efficient',
      strengths: ['Overall maturity', 'Reliability', 'Change safety', 'Contract governance', 'Low risk'],
      gaps: ['AI governance adoption'],
      context: 'Exemplary team with highest maturity and reliability scores, serving as benchmark reference for other teams',
    },
    {
      groupId: 'team-data-squad',
      groupName: 'Data Squad',
      serviceCount: 5,
      criticality: 'Medium',
      reliabilityScore: 80,
      reliabilityTrend: 'Stable',
      changeSafetyScore: 55,
      incidentRecurrenceRate: 12,
      maturityScore: 65,
      riskScore: 40,
      finopsEfficiency: 'Acceptable',
      strengths: ['Documentation quality', 'AI governance integration', 'Pipeline monitoring'],
      gaps: ['Event contract coverage', 'Change validation', 'Schema migration safety'],
      context: 'Data-focused team with good documentation practices but event contracts and change validation require strengthening',
    },
  ],
  generatedAt: new Date().toISOString(),
};

/** Mapeia CostEfficiencyType para variante do Badge. */
const finopsBadgeVariant = (eff: CostEfficiencyType): 'success' | 'warning' | 'danger' | 'default' => {
  switch (eff) {
    case 'Efficient': return 'success';
    case 'Acceptable': return 'warning';
    case 'Inefficient': return 'danger';
    case 'Wasteful': return 'danger';
    default: return 'default';
  }
};

/** Mapeia criticality para variante do Badge. */
const criticalityBadgeVariant = (crit: string): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (crit) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'info';
    case 'Low': return 'success';
    default: return 'default';
  }
};

/**
 * Página de Benchmarking Contextual — comparação governada entre grupos sem ranking simplista.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function BenchmarkingPage() {
  const { t } = useTranslation();
  const [dimension, setDimension] = useState<BenchmarkDimension>('teams');

  const d = mockBenchmarking;
  const dimensionOptions: BenchmarkDimension[] = ['teams', 'domains'];

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.executive.benchmarkingTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.executive.benchmarkingSubtitle')}</p>
      </div>

      {/* Dimension Selector */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        {dimensionOptions.map(dim => (
          <button
            key={dim}
            onClick={() => setDimension(dim)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              dimension === dim
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(`governance.executive.benchmarkingDimension${dim.charAt(0).toUpperCase()}${dim.slice(1)}`)}
          </button>
        ))}
      </div>

      {/* Comparisons */}
      <div className="space-y-6">
        {d.comparisons.map(comp => (
          <Card key={comp.groupId}>
            <CardHeader>
              <div className="flex items-center justify-between flex-wrap gap-2">
                <div className="flex items-center gap-3">
                  <BarChart3 size={16} className="text-accent" />
                  <span className="text-sm font-semibold text-heading">{comp.groupName}</span>
                  <Badge variant={criticalityBadgeVariant(comp.criticality)}>
                    {comp.criticality}
                  </Badge>
                </div>
                <span className="text-xs text-muted">
                  {comp.serviceCount} {t('governance.executive.benchmarkingServiceCount')}
                </span>
              </div>
            </CardHeader>
            <CardBody>
              {/* Metrics Grid */}
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3 mb-4">
                <StatCard
                  title={t('governance.executive.benchmarkingReliability')}
                  value={comp.reliabilityScore}
                  icon={<Shield size={16} />}
                  color="text-accent"
                  trend={{
                    direction: comp.reliabilityTrend === 'Declining' ? 'down' as const : 'up' as const,
                    label: t(`governance.trend.${comp.reliabilityTrend}`),
                  }}
                />
                <StatCard
                  title={t('governance.executive.benchmarkingChangeSafety')}
                  value={comp.changeSafetyScore}
                  icon={<Activity size={16} />}
                  color="text-accent"
                />
                <StatCard
                  title={t('governance.executive.benchmarkingIncidentRecurrence')}
                  value={`${comp.incidentRecurrenceRate}%`}
                  icon={<RefreshCw size={16} />}
                  color={comp.incidentRecurrenceRate > 15 ? 'text-critical' : 'text-amber-500'}
                />
                <StatCard
                  title={t('governance.executive.benchmarkingMaturity')}
                  value={comp.maturityScore}
                  icon={<Award size={16} />}
                  color="text-accent"
                />
                <StatCard
                  title={t('governance.executive.benchmarkingRisk')}
                  value={comp.riskScore}
                  icon={<TrendingUp size={16} />}
                  color={comp.riskScore > 60 ? 'text-critical' : 'text-amber-500'}
                />
                <div className="bg-card rounded-lg shadow-sm border border-edge p-3 flex flex-col items-center justify-center">
                  <DollarSign size={16} className="text-accent mb-1" />
                  <p className="text-xs text-muted mb-1">{t('governance.executive.benchmarkingFinOps')}</p>
                  <Badge variant={finopsBadgeVariant(comp.finopsEfficiency)}>
                    {t(`governance.finops.efficiency.${comp.finopsEfficiency}`)}
                  </Badge>
                </div>
              </div>

              {/* Strengths & Gaps */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-3">
                <div>
                  <p className="text-xs font-medium text-heading mb-2">{t('governance.executive.benchmarkingStrengths')}</p>
                  <div className="flex flex-wrap gap-1.5">
                    {comp.strengths.map((s, i) => (
                      <Badge key={i} variant="success">{s}</Badge>
                    ))}
                  </div>
                </div>
                <div>
                  <p className="text-xs font-medium text-heading mb-2">{t('governance.executive.benchmarkingGaps')}</p>
                  <div className="flex flex-wrap gap-1.5">
                    {comp.gaps.map((g, i) => (
                      <Badge key={i} variant="warning">{g}</Badge>
                    ))}
                  </div>
                </div>
              </div>

              {/* Context */}
              <p className="text-xs text-muted italic">{comp.context}</p>
            </CardBody>
          </Card>
        ))}
      </div>
    </PageContainer>
  );
}
