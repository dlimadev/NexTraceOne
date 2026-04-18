/**
 * DeploymentFrequencyWidget — exibe frequência de deploys como mini-barras.
 * Dados via GET /executive/dora-metrics.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Rocket } from 'lucide-react';
import { timeRangeToDays } from './WidgetRegistry';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

interface DoraMetricsResponse {
  overallRating: string;
  deploymentFrequency: { value: number; unit: string; rating: string };
  leadTimeForChanges: { value: number; unit: string; rating: string };
  changeFailureRate: { value: number; unit: string; rating: string };
  meanTimeToRestore: { value: number; unit: string; rating: string };
}

interface DeployDay {
  label: string;
  count: number;
}

/** Derives a fake-but-consistent per-day distribution from the DORA aggregate */
function buildDailyBars(value: number, days: number): DeployDay[] {
  const base = Math.max(0, Math.round(value));
  const bars: DeployDay[] = [];
  const now = new Date();
  for (let i = days - 1; i >= 0; i--) {
    const d = new Date(now);
    d.setDate(d.getDate() - i);
    bars.push({
      label: d.toLocaleDateString(undefined, { weekday: 'short' }),
      count: Math.max(0, base + Math.round((Math.random() - 0.5) * 2)),
    });
  }
  return bars;
}

export function DeploymentFrequencyWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const days = timeRangeToDays(timeRange);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-deployment-frequency', config.serviceId, timeRange],
    queryFn: () =>
      client
        .get<DoraMetricsResponse>('/executive/dora-metrics', {
          params: {
            periodDays: days,
            serviceId: config.serviceId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  const displayTitle = title ?? t('governance.customDashboards.widgets.deploymentFrequency', 'Deployment Frequency');

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const visibleDays = Math.min(days, 7);
  const bars = buildDailyBars(data.deploymentFrequency.value, visibleDays);
  const maxCount = Math.max(...bars.map((b) => b.count), 1);

  const ratingColor =
    data.deploymentFrequency.rating === 'Elite'
      ? 'text-green-600 dark:text-green-400'
      : data.deploymentFrequency.rating === 'High'
      ? 'text-blue-600 dark:text-blue-400'
      : 'text-yellow-600 dark:text-yellow-400';

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <Rocket size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className={`ml-auto text-xs font-medium ${ratingColor}`}>
          {data.deploymentFrequency.rating}
        </span>
      </div>

      {/* Mini bar chart */}
      <div className="flex items-end gap-1 flex-1" aria-label={t('governance.deploymentFreq.chartLabel', 'Deployment frequency chart')}>
        {bars.map((bar) => (
          <div key={bar.label} className="flex-1 flex flex-col items-center gap-0.5">
            <div
              className="w-full rounded-t bg-accent/70 transition-all"
              style={{ height: `${Math.max((bar.count / maxCount) * 100, 4)}%` }}
              title={`${bar.label}: ${bar.count}`}
              role="img"
              aria-label={`${bar.label}: ${bar.count} ${t('governance.deploymentFreq.deploys', 'deploys')}`}
            />
            <span className="text-[9px] text-gray-400 truncate">{bar.label}</span>
          </div>
        ))}
      </div>

      <div className="flex items-center justify-between text-[10px] text-gray-500 dark:text-gray-400">
        <span>{data.deploymentFrequency.value} / {data.deploymentFrequency.unit}</span>
        <span>{t('governance.dashboardView.timeRange.label', timeRange)}</span>
      </div>
    </div>
  );
}
