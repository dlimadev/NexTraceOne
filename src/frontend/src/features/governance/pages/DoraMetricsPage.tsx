import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Activity, TrendingUp, TrendingDown, BarChart3, RefreshCw, Minus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, StatsGrid, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

interface DoraMetric {
  name: string;
  value: number;
  unit: string;
  rating: string;
  description: string;
}

interface IncidentContext {
  openIncidents: number;
  resolvedInPeriod: number;
  trend: string;
  recurrenceRate: number;
}

interface DoraMetricsResponse {
  serviceName: string | null;
  teamName: string | null;
  periodDays: number;
  computedAt: string;
  deploymentFrequency: DoraMetric;
  leadTimeForChanges: DoraMetric;
  changeFailureRate: DoraMetric;
  meanTimeToRestore: DoraMetric;
  overallRating: string;
  incidentContext: IncidentContext;
}

interface DoraTrendPoint {
  periodStart: string;
  periodEnd: string;
  deploymentFrequency: number;
  leadTimeHours: number;
  changeFailureRatePct: number;
  mttrHours: number;
}

interface DoraMetricsTrendResponse {
  serviceName: string | null;
  periodDays: number;
  bucketDays: number;
  generatedAt: string;
  dataPoints: DoraTrendPoint[];
  summary: {
    deploymentFrequencyTrend: string;
    mttrTrend: string;
    changeFailureRateTrend: string;
    overallImproving: boolean;
  };
}

const useDoraMetrics = (periodDays: number) =>
  useQuery({
    queryKey: ['dora-metrics', periodDays],
    queryFn: () =>
      client
        .get<DoraMetricsResponse>('/executive/dora-metrics', { params: { periodDays } })
        .then((r) => r.data),
  });

const useDoraMetricsTrend = (periodDays: number) =>
  useQuery({
    queryKey: ['dora-metrics-trend', periodDays],
    queryFn: () =>
      client
        .get<DoraMetricsTrendResponse>('/executive/dora-metrics/trend', { params: { periodDays } })
        .then((r) => r.data),
  });

const RATING_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Elite: 'success',
  High: 'success',
  Medium: 'warning',
  Low: 'danger',
};

const RATING_COLOR: Record<string, string> = {
  Elite: 'text-emerald-600 dark:text-emerald-400',
  High: 'text-green-600 dark:text-green-400',
  Medium: 'text-amber-600 dark:text-amber-400',
  Low: 'text-red-600 dark:text-red-400',
};

const TrendIcon = ({ trend }: { trend: string }) => {
  if (trend === 'Improving') return <TrendingUp size={14} className="text-green-500" />;
  if (trend === 'Degrading') return <TrendingDown size={14} className="text-red-500" />;
  return <Minus size={14} className="text-gray-400" />;
};

export function DoraMetricsPage() {
  const { t } = useTranslation();
  const [periodDays, setPeriodDays] = useState(30);
  const { data, isLoading, isError, refetch } = useDoraMetrics(periodDays);
  const { data: trendData } = useDoraMetricsTrend(periodDays);

  if (isLoading) return <PageLoadingState message={t('governance.dora.loading')} />;
  if (isError) return <PageErrorState message={t('governance.dora.error')} onRetry={() => refetch()} />;

  const metrics = data
    ? [
        data.deploymentFrequency,
        data.leadTimeForChanges,
        data.changeFailureRate,
        data.meanTimeToRestore,
      ]
    : [];

  const stats = [
    {
      label: t('governance.dora.overallRating'),
      value: data?.overallRating ?? '-',
    },
    {
      label: t('governance.dora.openIncidents'),
      value: data?.incidentContext.openIncidents ?? 0,
    },
    {
      label: t('governance.dora.resolvedInPeriod'),
      value: data?.incidentContext.resolvedInPeriod ?? 0,
    },
    {
      label: t('governance.dora.recurrenceRate'),
      value: `${data?.incidentContext.recurrenceRate?.toFixed(1) ?? 0}%`,
    },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.dora.title')}
        subtitle={t('governance.dora.subtitle')}
        icon={<Activity size={24} />}
        actions={
          <div className="flex items-center gap-2">
            <label className="text-sm text-gray-600 dark:text-gray-400">
              {t('governance.dora.period')}:
            </label>
            <select
              value={periodDays}
              onChange={(e) => setPeriodDays(Number(e.target.value))}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-2 py-1"
            >
              {[7, 30, 60, 90].map((d) => (
                <option key={d} value={d}>{t('common.daysN', { count: d })}</option>
              ))}
            </select>
            <Button size="sm" onClick={() => refetch()}>
              <RefreshCw size={14} className="mr-1" />
              {t('common.refresh')}
            </Button>
          </div>
        }
      />

      <StatsGrid stats={stats} />

      {/* DORA 4 Metrics */}
      <PageSection title={t('governance.dora.metricsSection')}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          {metrics.map((metric) => (
            <Card key={metric.name}>
              <CardHeader className="pb-0">
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300">{metric.name}</h3>
                  <Badge variant={RATING_VARIANT[metric.rating] ?? 'secondary'}>{metric.rating}</Badge>
                </div>
              </CardHeader>
              <CardBody className="pt-2">
                <p className={`text-3xl font-bold ${RATING_COLOR[metric.rating] ?? 'text-gray-900 dark:text-white'}`}>
                  {metric.value} <span className="text-sm font-normal text-gray-400">{metric.unit}</span>
                </p>
                <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{metric.description}</p>
              </CardBody>
            </Card>
          ))}
        </div>
      </PageSection>

      {/* Trend Summary */}
      {trendData && (
        <PageSection title={t('governance.dora.trendSummary')}>
          <Card>
            <CardBody className="p-4">
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                <div className="flex items-center gap-2">
                  <TrendIcon trend={trendData.summary.deploymentFrequencyTrend} />
                  <div>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{t('governance.dora.dfTrend')}</p>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {trendData.summary.deploymentFrequencyTrend}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <TrendIcon trend={trendData.summary.mttrTrend} />
                  <div>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{t('governance.dora.mttrTrend')}</p>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {trendData.summary.mttrTrend}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <TrendIcon trend={trendData.summary.changeFailureRateTrend} />
                  <div>
                    <p className="text-xs text-gray-500 dark:text-gray-400">{t('governance.dora.cfrTrend')}</p>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {trendData.summary.changeFailureRateTrend}
                    </p>
                  </div>
                </div>
              </div>
              {trendData.summary.overallImproving && (
                <div className="mt-3 flex items-center gap-2 rounded bg-green-50 dark:bg-green-900/20 p-2">
                  <TrendingUp size={14} className="text-green-600" />
                  <span className="text-xs text-green-700 dark:text-green-300">
                    {t('governance.dora.overallImproving')}
                  </span>
                </div>
              )}
            </CardBody>
          </Card>
        </PageSection>
      )}

      {/* Trend data points */}
      {trendData && trendData.dataPoints.length > 0 && (
        <PageSection title={t('governance.dora.trendDataPoints')}>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="py-2 px-3 text-left text-xs text-gray-500">{t('governance.dora.period')}</th>
                  <th className="py-2 px-3 text-right text-xs text-gray-500">{t('governance.dora.deployFreq')}</th>
                  <th className="py-2 px-3 text-right text-xs text-gray-500">{t('governance.dora.leadTime')}</th>
                  <th className="py-2 px-3 text-right text-xs text-gray-500">{t('governance.dora.cfr')}</th>
                  <th className="py-2 px-3 text-right text-xs text-gray-500">{t('governance.dora.mttr')}</th>
                </tr>
              </thead>
              <tbody>
                {trendData.dataPoints.map((point, idx) => (
                  <tr key={idx} className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-900/20">
                    <td className="py-2 px-3 text-gray-700 dark:text-gray-300">
                      {new Date(point.periodStart).toLocaleDateString()}
                    </td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">
                      {point.deploymentFrequency}
                    </td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">
                      {point.leadTimeHours}h
                    </td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">
                      {point.changeFailureRatePct}%
                    </td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">
                      {point.mttrHours}h
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </PageSection>
      )}
    </PageContainer>
  );
}
