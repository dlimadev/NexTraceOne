/**
 * ObsMetricsWidget — exibe série temporal de uma métrica de observabilidade via AreaChart.
 * Dados via GET /api/v1/governance/observability/metrics.
 * Suporta filtro por metricName, serviceName e environment derivados do config do widget.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  ReferenceLine,
} from 'recharts';
import { Activity, Settings } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';
import { useAnnotations, type ChartAnnotation } from '../components/ChartAnnotations';

// ── Types ──────────────────────────────────────────────────────────────────

interface MetricDataPoint {
  timestamp: string;
  value: number;
  metricName: string;
  serviceName?: string;
}

interface DashboardMetricsResult {
  points: MetricDataPoint[];
  metricName: string;
  isBackendAvailable: boolean;
}

// ── Chart label helper ────────────────────────────────────────────────────

function formatTimestamp(ts: string, timeRange: string): string {
  const d = new Date(ts);
  if (timeRange === '1h' || timeRange === '6h') {
    return d.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' });
  }
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

// ── Shared loading / error UI ──────────────────────────────────────────────

function WidgetShell({ title, children }: { title: string; children: React.ReactNode }): React.ReactElement {
  return (
    <div className="h-full flex flex-col gap-1 p-2">
      <div className="flex items-center gap-1.5 shrink-0">
        <Activity size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{title}</span>
      </div>
      {children}
    </div>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function OtelMetricsWidget({
  config,
  environmentId,
  timeRange,
  title,
}: WidgetProps): React.ReactElement {
  const TENANT_ID = 'default';
  const { t } = useTranslation();

  const metricName = config.metricName ?? 'http.server.duration';
  const serviceName = config.serviceId ?? undefined;
  const environment = environmentId ?? config.serviceId ?? 'production';

  const displayTitle =
    title ??
    config.customTitle ??
    t('governance.customDashboards.widgets.obsMetrics', 'Metrics');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-obs-metrics', metricName, serviceName, environment, timeRange],
    queryFn: (): Promise<DashboardMetricsResult> =>
      client
        .get<DashboardMetricsResult>('/governance/observability/metrics', {
          params: {
            metricName,
            serviceName,
            environment,
            timeRange,
          },
        })
        .then((r) => r.data),
  });

  // Annotations time range — computed here (before early returns) so useAnnotations
  // is always called unconditionally (Rule of Hooks compliance).
  const { from: annotFrom, to: annotTo } = (() => {
    const now = new Date();
    const match = timeRange.match(/^(\d+)([hd])$/);
    if (match) {
      const amount = parseInt(match[1], 10);
      const unit = match[2];
      const ms = unit === 'h' ? amount * 60 * 60 * 1000 : amount * 24 * 60 * 60 * 1000;
      return { from: new Date(now.getTime() - ms).toISOString(), to: now.toISOString() };
    }
    return { from: new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString(), to: now.toISOString() };
  })();

  const { annotations } = useAnnotations(TENANT_ID, annotFrom, annotTo, config.serviceId ? [config.serviceId] : undefined);

  if (isLoading) {
    return (
      <WidgetShell title={displayTitle}>
        <div className="flex-1 flex flex-col gap-2 justify-center">
          <Skeleton variant="rectangular" className="h-full w-full" />
        </div>
      </WidgetShell>
    );
  }

  if (isError) {
    return (
      <WidgetShell title={displayTitle}>
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <span className="text-xs text-red-500 dark:text-red-400 text-center">
            {t('governance.dashboardView.widgetError', 'Could not load data')}
          </span>
          <button
            type="button"
            onClick={() => refetch()}
            className="text-xs text-blue-500 underline hover:no-underline"
          >
            {t('common.retry', 'Retry')}
          </button>
        </div>
      </WidgetShell>
    );
  }

  // Backend not configured — neutral info state
  if (data && !data.isBackendAvailable) {
    return (
      <WidgetShell title={displayTitle}>
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <Settings size={18} className="text-gray-400 dark:text-gray-500" />
          <span className="text-xs text-gray-500 dark:text-gray-400 text-center">
            {t('obs.backendNotConfigured', 'Backend not configured')}
          </span>
        </div>
      </WidgetShell>
    );
  }

  const points = data?.points ?? [];

  if (points.length === 0) {
    return (
      <WidgetShell title={displayTitle}>
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {t('obs.metrics.noData', 'No metric data')}
          </span>
        </div>
      </WidgetShell>
    );
  }

  const chartData = points.map((p) => ({
    time: formatTimestamp(p.timestamp, timeRange),
    value: p.value,
  }));

  const annotationLines = annotations.map((ann: ChartAnnotation) => ({
    x: formatTimestamp(ann.timestamp, timeRange),
    color: ann.severity === 'critical' ? '#ef4444' : ann.severity === 'warning' ? '#f59e0b' : '#3b82f6',
    label: ann.label,
  }));

  const subtitle = serviceName ?? undefined;

  return (
    <WidgetShell title={displayTitle}>
      {subtitle && (
        <span className="text-[10px] text-gray-500 dark:text-gray-400 truncate shrink-0">
          {subtitle}
        </span>
      )}
      <div className="flex-1 min-h-0">
        <ResponsiveContainer width="100%" height="100%">
          <AreaChart data={chartData} margin={{ top: 4, right: 4, left: -20, bottom: 0 }}>
            <defs>
              <linearGradient id="obsMetricFill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3} />
                <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
              </linearGradient>
            </defs>
            <XAxis
              dataKey="time"
              tick={{ fontSize: 9, fill: '#9ca3af' }}
              tickLine={false}
              axisLine={false}
              interval="preserveStartEnd"
            />
            <YAxis
              tick={{ fontSize: 9, fill: '#9ca3af' }}
              tickLine={false}
              axisLine={false}
              width={36}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: '#1f2937',
                border: '1px solid #374151',
                borderRadius: '6px',
                fontSize: '11px',
                color: '#f9fafb',
              }}
              itemStyle={{ color: '#93c5fd' }}
            />
            {annotationLines.map((line) => (
              <ReferenceLine
                key={line.x}
                x={line.x}
                stroke={line.color}
                strokeWidth={1.5}
                strokeDasharray="4 3"
                opacity={0.7}
                ifOverflow="extendDomain"
              />
            ))}
            <Area
              type="monotone"
              dataKey="value"
              stroke="#3b82f6"
              strokeWidth={1.5}
              fill="url(#obsMetricFill)"
              dot={false}
              activeDot={{ r: 3 }}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </WidgetShell>
  );
}
