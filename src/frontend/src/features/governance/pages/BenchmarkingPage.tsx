import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart3, TrendingUp, Shield, Activity, RefreshCw,
  Award, DollarSign,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { executiveApi } from '../api/executive';
import type {
  CostEfficiencyType,
} from '../../../types';

type BenchmarkDimension = 'teams' | 'domains';

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

  const { data: d, isLoading, isError, refetch } = useQuery({
    queryKey: ['executive-benchmarking', dimension],
    queryFn: () => executiveApi.getBenchmarking(dimension),
    staleTime: 30_000,
  });

  const dimensionOptions: BenchmarkDimension[] = ['teams', 'domains'];

  if (isLoading) return <PageLoadingState />;
  if (isError || !d) return <PageErrorState action={<button onClick={() => refetch()} className="btn btn-sm btn-primary">{t('common.retry')}</button>} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.executive.benchmarkingTitle')}
        subtitle={t('governance.executive.benchmarkingSubtitle')}
      />

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
                  <Badge variant={criticalityBadgeVariant(comp.criticality ?? 'default')}>
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
                  value={comp.reliabilityScore ?? '—'}
                  icon={<Shield size={16} />}
                  color="text-accent"
                  trend={comp.reliabilityTrend ? {
                    direction: comp.reliabilityTrend === 'Declining' ? 'down' as const : 'up' as const,
                    label: t(`governance.trend.${comp.reliabilityTrend}`),
                  } : undefined}
                />
                <StatCard
                  title={t('governance.executive.benchmarkingChangeSafety')}
                  value={comp.changeSafetyScore ?? '—'}
                  icon={<Activity size={16} />}
                  color="text-accent"
                />
                <StatCard
                  title={t('governance.executive.benchmarkingIncidentRecurrence')}
                  value={comp.incidentRecurrenceRate != null ? `${comp.incidentRecurrenceRate}%` : '—'}
                  icon={<RefreshCw size={16} />}
                  color={comp.incidentRecurrenceRate != null && comp.incidentRecurrenceRate > 15 ? 'text-critical' : 'text-warning'}
                />
                <StatCard
                  title={t('governance.executive.benchmarkingMaturity')}
                  value={comp.maturityScore ?? '—'}
                  icon={<Award size={16} />}
                  color="text-accent"
                />
                <StatCard
                  title={t('governance.executive.benchmarkingRisk')}
                  value={comp.riskScore ?? '—'}
                  icon={<TrendingUp size={16} />}
                  color={comp.riskScore != null && comp.riskScore > 60 ? 'text-critical' : 'text-warning'}
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
