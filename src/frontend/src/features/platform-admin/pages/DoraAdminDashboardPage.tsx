import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  BarChart3,
  RefreshCw,
  XCircle,
  TrendingUp,
  TrendingDown,
  Minus,
  Clock,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type DoraMetric, type DoraRating } from '../api/platformAdmin';

const TIME_RANGES = [7, 30, 90] as const;
const ENVIRONMENTS = ['production', 'pre-production'] as const;

export function DoraAdminDashboardPage() {
  const { t } = useTranslation('doraAdminDashboard');
  const [timeDays, setTimeDays] = useState<number>(30);
  const [environment, setEnvironment] = useState<string>('production');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['dora-admin-metrics', environment, timeDays],
    queryFn: () => platformAdminApi.getDoraAdminMetrics(environment, timeDays),
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<BarChart3 size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Filters */}
        <div className="flex flex-wrap gap-4 items-center">
          <div className="flex items-center gap-1">
            {TIME_RANGES.map((d) => (
              <button
                key={d}
                onClick={() => setTimeDays(d)}
                className={`px-3 py-1.5 text-xs rounded border font-medium transition-colors ${
                  timeDays === d
                    ? 'bg-accent text-white border-accent'
                    : 'border-edge text-muted hover:bg-elevated'
                }`}
              >
                {d}d
              </button>
            ))}
          </div>
          <div className="flex items-center gap-1">
            {ENVIRONMENTS.map((env) => (
              <button
                key={env}
                onClick={() => setEnvironment(env)}
                className={`px-3 py-1.5 text-xs rounded border font-medium transition-colors ${
                  environment === env
                    ? 'bg-heading text-white border-heading'
                    : 'border-edge text-muted hover:bg-elevated'
                }`}
              >
                {t(`env.${env.replace('-', '')}`)}
              </button>
            ))}
          </div>
        </div>

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && (
          <>
            {/* DORA Metric Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
              <DoraCard metric={data.deploymentFrequency} labelKey="metricDeployFreq" t={t} />
              <DoraCard metric={data.leadTime} labelKey="metricLeadTime" t={t} />
              <DoraCard metric={data.mttr} labelKey="metricMttr" t={t} />
              <DoraCard metric={data.changeFailureRate} labelKey="metricCfr" t={t} />
            </div>

            {/* Data Freshness */}
            <div className="flex items-center gap-3 p-4 bg-elevated border border-edge rounded-lg text-sm text-muted">
              <Clock size={16} className="text-faded shrink-0" />
              <span>
                {t('dataSource')}: <span className="font-medium text-body">{data.dataSource}</span>
                {' · '}
                {t('lastUpdated')}:{' '}
                <span className="font-medium text-body">
                  {new Date(data.lastUpdatedAt).toLocaleTimeString()}
                </span>
              </span>
            </div>

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function DoraCard({
  metric,
  labelKey,
  t,
}: {
  metric: DoraMetric;
  labelKey: string;
  t: (key: string) => string;
}) {
  const ratingStyle: Record<DoraRating, string> = {
    Elite: 'text-success bg-success/10 border-success/20',
    High: 'text-accent bg-accent/10 border-accent/20',
    Medium: 'text-warning bg-warning/10 border-warning/20',
    Low: 'text-critical bg-critical/10 border-critical/20',
  };

  const TrendIcon =
    metric.trendDirection === 'up'
      ? TrendingUp
      : metric.trendDirection === 'down'
        ? TrendingDown
        : Minus;

  const trendPositive =
    (labelKey === 'metricDeployFreq' && metric.trendDirection === 'up') ||
    (labelKey !== 'metricDeployFreq' && metric.trendDirection === 'down');

  return (
    <div className="border border-edge rounded-lg p-5 bg-card space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-muted uppercase tracking-wide font-medium">{t(labelKey)}</p>
        <span className={`px-2 py-0.5 text-xs font-medium rounded border ${ratingStyle[metric.rating]}`}>
          {t(`rating.${metric.rating}`)}
        </span>
      </div>
      <div className="flex items-end gap-2">
        <p className="text-3xl font-semibold text-heading">{metric.value}</p>
        <p className="text-sm text-muted pb-1">{metric.unit}</p>
      </div>
      <div
        className={`flex items-center gap-1 text-xs font-medium ${trendPositive ? 'text-success' : 'text-critical'}`}
      >
        <TrendIcon size={13} />
        <span>
          {Math.abs(metric.trend)}% {t('vsPrev')}
        </span>
      </div>
    </div>
  );
}
