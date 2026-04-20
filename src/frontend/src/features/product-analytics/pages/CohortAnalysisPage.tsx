import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Users, TrendingUp } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';
import type { CohortRow } from '../api/productAnalyticsApi';

/**
 * Página de análise de cohorts do produto.
 *
 * Agrupa utilizadores pelo seu primeiro evento e apresenta curvas de retenção
 * ou ativação por semana/mês. Alimentada pelo endpoint /product-analytics/cohorts.
 *
 * @see docs/analysis/PRODUCT-ANALYTICS-IMPROVEMENT-PLAN.md — FEAT-05
 */

type Granularity = 'week' | 'month';
type Metric = 'retention' | 'activation';

function rateColor(rate: number): string {
  if (rate >= 70) return 'bg-success/80 text-success';
  if (rate >= 40) return 'bg-accent/60 text-accent';
  if (rate >= 20) return 'bg-warning/60 text-warning';
  if (rate > 0) return 'bg-critical/40 text-critical';
  return 'bg-panel text-muted';
}

function rateLabel(rate: number): string {
  if (rate === 0) return '—';
  return `${rate.toFixed(1)}%`;
}

function CohortHeatmapRow({ cohort, maxPeriods }: { cohort: CohortRow; maxPeriods: number }) {
  const periodMap = new Map(cohort.periods.map((p) => [p.period, p]));

  return (
    <tr>
      <td className="py-2 pr-4 text-xs text-heading font-medium whitespace-nowrap">
        {cohort.cohortLabel}
      </td>
      <td className="py-2 pr-4 text-xs text-muted">{cohort.totalUsers}</td>
      {Array.from({ length: maxPeriods }, (_, i) => i).map((period) => {
        const entry = periodMap.get(period);
        const rate = entry?.rate ?? 0;
        return (
          <td key={period} className="py-1 px-0.5">
            <div
              className={`rounded text-center text-xs py-1 px-1 min-w-[3rem] ${rateColor(rate)}`}
              title={entry ? `${cohort.cohortLabel} P${period}: ${rate.toFixed(1)}% (${entry.count} users)` : '—'}
            >
              {rateLabel(rate)}
            </div>
          </td>
        );
      })}
    </tr>
  );
}

export function CohortAnalysisPage() {
  const { t } = useTranslation();
  const [granularity, setGranularity] = useState<Granularity>('week');
  const [metric, setMetric] = useState<Metric>('retention');
  const [periods, setPeriods] = useState<number>(12);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-cohorts', granularity, metric, periods],
    queryFn: () =>
      productAnalyticsApi.getCohortAnalysis({ granularity, metric, periods }),
    staleTime: 30_000,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('common.loading')} />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <button
              type="button"
              onClick={() => refetch()}
              className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
            >
              {t('common.retry')}
            </button>
          }
        />
      </PageContainer>
    );
  }

  const { cohorts, maxPeriods } = data;

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.cohort.title')}
        subtitle={t('analytics.cohort.subtitle')}
      />

      {/* Controls */}
      <div className="flex flex-wrap gap-3 mb-6">
        {/* Granularity */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted">{t('analytics.cohort.granularity')}:</span>
          {(['week', 'month'] as Granularity[]).map((g) => (
            <button
              key={g}
              type="button"
              onClick={() => setGranularity(g)}
              className={`px-3 py-1.5 rounded text-xs border transition-colors ${
                granularity === g
                  ? 'bg-accent text-white border-accent'
                  : 'bg-panel text-muted border-edge hover:border-accent/50'
              }`}
            >
              {t(`analytics.cohort.${g}`)}
            </button>
          ))}
        </div>

        {/* Metric */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted">{t('analytics.cohort.metric')}:</span>
          {(['retention', 'activation'] as Metric[]).map((m) => (
            <button
              key={m}
              type="button"
              onClick={() => setMetric(m)}
              className={`px-3 py-1.5 rounded text-xs border transition-colors ${
                metric === m
                  ? 'bg-accent text-white border-accent'
                  : 'bg-panel text-muted border-edge hover:border-accent/50'
              }`}
            >
              {t(`analytics.cohort.${m}`)}
            </button>
          ))}
        </div>

        {/* Periods */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted">{t('analytics.cohort.periods')}:</span>
          {[4, 8, 12, 24].map((p) => (
            <button
              key={p}
              type="button"
              onClick={() => setPeriods(p)}
              className={`px-3 py-1.5 rounded text-xs border transition-colors ${
                periods === p
                  ? 'bg-accent text-white border-accent'
                  : 'bg-panel text-muted border-edge hover:border-accent/50'
              }`}
            >
              {p}
            </button>
          ))}
        </div>
      </div>

      {cohorts.length === 0 ? (
        <Card>
          <CardBody>
            <div className="flex flex-col items-center justify-center py-16 text-center gap-3">
              <Users className="w-10 h-10 text-muted opacity-50" />
              <p className="text-muted text-sm">{t('analytics.cohort.empty')}</p>
            </div>
          </CardBody>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <TrendingUp className="w-4 h-4 text-accent" />
              <span className="text-sm font-medium text-heading">
                {t(`analytics.cohort.${metric}`)} — {t(`analytics.cohort.${granularity}`)}
              </span>
            </div>
          </CardHeader>
          <CardBody>
            <div className="overflow-x-auto">
              <table className="w-full text-xs">
                <thead>
                  <tr>
                    <th className="text-left py-2 pr-4 text-muted font-medium">
                      {t('analytics.cohort.cohortLabel')}
                    </th>
                    <th className="text-left py-2 pr-4 text-muted font-medium">
                      {t('analytics.cohort.users')}
                    </th>
                    {Array.from({ length: maxPeriods }, (_, i) => i).map((p) => (
                      <th key={p} className="py-2 px-0.5 text-center text-muted font-medium min-w-[3rem]">
                        P{p}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {cohorts.map((cohort) => (
                    <CohortHeatmapRow key={cohort.cohortLabel} cohort={cohort} maxPeriods={maxPeriods} />
                  ))}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
