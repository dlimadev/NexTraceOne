/**
 * DoraMetricsWidget — exibe métricas DORA resumidas.
 * Dados via GET /executive/dora-metrics.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Activity } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import client from '../../../api/client';
import { timeRangeToDays } from './WidgetRegistry';
import type { WidgetProps } from './WidgetRegistry';

interface DoraMetric {
  name: string;
  value: number;
  unit: string;
  rating: string;
}

interface DoraMetricsResponse {
  overallRating: string;
  deploymentFrequency: DoraMetric;
  leadTimeForChanges: DoraMetric;
  changeFailureRate: DoraMetric;
  meanTimeToRestore: DoraMetric;
}

export function DoraMetricsWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-dora-metrics', config.serviceId, timeRange],
    queryFn: () =>
      client
        .get<DoraMetricsResponse>('/executive/dora-metrics', {
          params: { periodDays: timeRangeToDays(timeRange) },
        })
        .then((r) => r.data),
  });

  const displayTitle = title ?? t('governance.customDashboards.widgets.doraMetrics');

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const metrics = [
    { label: t('governance.dora.deployFreq'), value: data.deploymentFrequency.value, unit: data.deploymentFrequency.unit },
    { label: t('governance.dora.leadTime'), value: data.leadTimeForChanges.value, unit: data.leadTimeForChanges.unit },
    { label: t('governance.dora.cfr'), value: data.changeFailureRate.value, unit: data.changeFailureRate.unit },
    { label: t('governance.dora.mttr'), value: data.meanTimeToRestore.value, unit: data.meanTimeToRestore.unit },
  ];

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <Activity size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs font-medium text-accent">{data.overallRating}</span>
      </div>
      <div className="grid grid-cols-2 gap-2 flex-1">
        {metrics.map((m) => (
          <div key={m.label} className="rounded bg-gray-50 dark:bg-gray-800/50 p-2">
            <div className="text-xs text-gray-500 dark:text-gray-400 truncate">{m.label}</div>
            <div className="text-sm font-bold text-gray-900 dark:text-white tabular-nums">
              {m.value} <span className="text-xs font-normal">{m.unit}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ── Shared mini helpers ────────────────────────────────────────────────────

export function WidgetSkeleton({ title }: { title: string }) {
  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <Skeleton variant="text" height="h-4" width="w-32" />
      <div className="flex-1 grid grid-cols-2 gap-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} variant="rectangular" className="h-14 w-full" />
        ))}
      </div>
      <span className="sr-only">{title}</span>
    </div>
  );
}

export function WidgetError({ title }: { title: string }) {
  const { t } = useTranslation();
  return (
    <div className="h-full flex flex-col items-center justify-center gap-2 p-2">
      <span className="text-xs text-gray-500 dark:text-gray-400 text-center">
        {title} — {t('governance.dashboardView.widgetError', 'Could not load data')}
      </span>
    </div>
  );
}
