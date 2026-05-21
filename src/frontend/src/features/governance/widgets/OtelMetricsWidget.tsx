/**
 * OtelMetricsWidget — exibe série temporal de uma métrica OpenTelemetry via AreaChart.
 * Dados via GET /api/v1/telemetry/metrics.
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
} from 'recharts';
import { Activity } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface MetricDataPoint {
  timestamp: string;
  value: number;
  serviceName: string;
  metricName: string;
}

type MetricsResponse = MetricDataPoint[];

// ── Time range helper ──────────────────────────────────────────────────────

function resolveTimeRange(timeRange: string): { from: string; until: string } {
  const until = new Date();
  const from = new Date(until);
  switch (timeRange) {
    case '1h':  from.setHours(from.getHours() - 1); break;
    case '6h':  from.setHours(from.getHours() - 6); break;
    case '24h': from.setHours(from.getHours() - 24); break;
    case '7d':  from.setDate(from.getDate() - 7); break;
    case '30d': from.setDate(from.getDate() - 30); break;
    default:    from.setHours(from.getHours() - 24);
  }
  return { from: from.toISOString(), until: until.toISOString() };
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
  const { t } = useTranslation();

  const metricName = config.metricName ?? 'http.server.duration';
  const serviceName = config.serviceId ?? undefined;
  const environment = config.otelEnvironment ?? environmentId ?? undefined;
  const { from, until } = resolveTimeRange(timeRange);

  const displayTitle =
    title ??
    t('governance.customDashboards.widgets.otelMetrics', 'OTel Metrics');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-otel-metrics', metricName, serviceName, environment, timeRange],
    queryFn: (): Promise<MetricsResponse> =>
      client
        .get<MetricsResponse>('/telemetry/metrics', {
          params: {
            metricName,
            serviceName,
            environment,
            from,
            until,
          },
        })
        .then((r) => r.data),
  });

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

  const points = data ?? [];

  if (points.length === 0) {
    return (
      <WidgetShell title={displayTitle}>
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {t('governance.otelMetrics.noData', 'No metric data')}
          </span>
        </div>
      </WidgetShell>
    );
  }

  const chartData = points.map((p) => ({
    time: formatTimestamp(p.timestamp, timeRange),
    value: p.value,
  }));

  const subtitle = [metricName, serviceName].filter(Boolean).join(' · ');

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
              <linearGradient id="otelMetricFill" x1="0" y1="0" x2="0" y2="1">
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
            <Area
              type="monotone"
              dataKey="value"
              stroke="#3b82f6"
              strokeWidth={1.5}
              fill="url(#otelMetricFill)"
              dot={false}
              activeDot={{ r: 3 }}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </WidgetShell>
  );
}
