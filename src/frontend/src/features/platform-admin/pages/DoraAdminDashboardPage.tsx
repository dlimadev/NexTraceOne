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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BarChart3 size={24} className="text-violet-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-4 items-center">
        <div className="flex items-center gap-1">
          {TIME_RANGES.map((d) => (
            <button
              key={d}
              onClick={() => setTimeDays(d)}
              className={`px-3 py-1.5 text-xs rounded border font-medium transition-colors ${
                timeDays === d
                  ? 'bg-violet-600 text-white border-violet-600'
                  : 'border-slate-300 text-slate-600 hover:bg-slate-50'
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
                  ? 'bg-slate-800 text-white border-slate-800'
                  : 'border-slate-300 text-slate-600 hover:bg-slate-50'
              }`}
            >
              {t(`env.${env.replace('-', '')}`)}
            </button>
          ))}
        </div>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
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
          <div className="flex items-center gap-3 p-4 bg-slate-50 border border-slate-200 rounded-lg text-sm text-slate-600">
            <Clock size={16} className="text-slate-400 shrink-0" />
            <span>
              {t('dataSource')}: <span className="font-medium">{data.dataSource}</span>
              {' · '}
              {t('lastUpdated')}:{' '}
              <span className="font-medium">
                {new Date(data.lastUpdatedAt).toLocaleTimeString()}
              </span>
            </span>
          </div>

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
        </>
      )}
    </div>
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
    Elite: 'text-emerald-700 bg-emerald-50 border-emerald-200',
    High: 'text-violet-700 bg-violet-50 border-violet-200',
    Medium: 'text-amber-700 bg-amber-50 border-amber-200',
    Low: 'text-red-700 bg-red-50 border-red-200',
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
    <div className="border border-slate-200 rounded-lg p-5 bg-white space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-slate-500 uppercase tracking-wide font-medium">{t(labelKey)}</p>
        <span className={`px-2 py-0.5 text-xs font-medium rounded border ${ratingStyle[metric.rating]}`}>
          {t(`rating.${metric.rating}`)}
        </span>
      </div>
      <div className="flex items-end gap-2">
        <p className="text-3xl font-semibold text-slate-900">{metric.value}</p>
        <p className="text-sm text-slate-500 pb-1">{metric.unit}</p>
      </div>
      <div
        className={`flex items-center gap-1 text-xs font-medium ${trendPositive ? 'text-emerald-600' : 'text-red-500'}`}
      >
        <TrendIcon size={13} />
        <span>
          {Math.abs(metric.trend)}% {t('vsPrev')}
        </span>
      </div>
    </div>
  );
}
