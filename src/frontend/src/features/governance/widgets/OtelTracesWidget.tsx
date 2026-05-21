/**
 * OtelTracesWidget — exibe lista de traces distribuídos OpenTelemetry.
 * Dados via GET /api/v1/telemetry/traces e GET /api/v1/telemetry/traces/{traceId}.
 * Clicar num trace expande a vista de detalhe com spans.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitBranch, ArrowLeft, CheckCircle, XCircle } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface TraceEntry {
  traceId: string;
  serviceName: string;
  operationName: string;
  durationMs: number;
  hasErrors: boolean;
  startTime: string;
}

type TracesResponse = TraceEntry[];

interface SpanEntry {
  spanId: string;
  parentSpanId?: string | null;
  operationName: string;
  serviceName: string;
  durationMs: number;
  hasError: boolean;
  startTime: string;
}

interface TraceDetailResponse {
  traceId: string;
  spans: SpanEntry[];
}

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

// ── Duration colour ────────────────────────────────────────────────────────

function durationColour(ms: number): string {
  if (ms > 1000) return 'text-red-600 dark:text-red-400';
  if (ms > 200)  return 'text-yellow-600 dark:text-yellow-400';
  return 'text-emerald-600 dark:text-emerald-400';
}

// ── Trace Detail sub-view ──────────────────────────────────────────────────

function TraceDetail({
  traceId,
  onBack,
}: {
  traceId: string;
  onBack: () => void;
}): React.ReactElement {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-otel-trace-detail', traceId],
    queryFn: (): Promise<TraceDetailResponse> =>
      client
        .get<TraceDetailResponse>(`/telemetry/traces/${traceId}`)
        .then((r) => r.data),
  });

  return (
    <div className="h-full flex flex-col gap-1 p-2">
      {/* Back header */}
      <button
        type="button"
        onClick={onBack}
        className="flex items-center gap-1 text-[10px] text-blue-500 hover:text-blue-400 transition-colors shrink-0"
        aria-label={t('governance.otelTraces.backToList', 'Back to traces')}
      >
        <ArrowLeft size={11} />
        {t('governance.otelTraces.backToList', 'Back')}
      </button>
      <span className="text-[10px] font-mono text-gray-500 dark:text-gray-400 truncate shrink-0">
        {traceId}
      </span>

      {isLoading ? (
        <div className="flex-1 flex flex-col gap-1.5">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} variant="text" className="h-4 w-full" />
          ))}
        </div>
      ) : isError ? (
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
      ) : (
        <div className="flex-1 overflow-y-auto min-h-0">
          <table className="w-full text-[10px] border-collapse">
            <thead className="sticky top-0 bg-white dark:bg-gray-900 z-10">
              <tr className="text-gray-500 dark:text-gray-400">
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('governance.otelTraces.colOperation', 'Operation')}
                </th>
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('governance.otelTraces.colService', 'Service')}
                </th>
                <th className="text-right pb-1 font-medium">
                  {t('governance.otelTraces.colDuration', 'ms')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(data?.spans ?? []).map((span) => (
                <tr
                  key={span.spanId}
                  className="border-t border-gray-100 dark:border-gray-800"
                >
                  <td className="py-0.5 pr-1 truncate max-w-[140px] text-gray-700 dark:text-gray-300">
                    {span.hasError && (
                      <XCircle
                        size={9}
                        className="inline-block mr-0.5 text-red-500 shrink-0"
                        aria-label={t('governance.otelTraces.errorSpan', 'Error')}
                      />
                    )}
                    {span.operationName}
                  </td>
                  <td className="py-0.5 pr-1 text-gray-500 dark:text-gray-400 truncate max-w-[80px]">
                    {span.serviceName}
                  </td>
                  <td className={`py-0.5 text-right tabular-nums ${durationColour(span.durationMs)}`}>
                    {span.durationMs}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

// ── Main component ─────────────────────────────────────────────────────────

export function OtelTracesWidget({
  config,
  environmentId,
  timeRange,
  title,
}: WidgetProps): React.ReactElement {
  const { t } = useTranslation();
  const [selectedTraceId, setSelectedTraceId] = useState<string | null>(null);

  const serviceName = config.serviceId ?? undefined;
  const environment = config.otelEnvironment ?? environmentId ?? undefined;
  const minDurationMs = config.minDurationMs ?? undefined;
  const { from, until } = resolveTimeRange(timeRange);

  const displayTitle =
    title ?? t('governance.customDashboards.widgets.otelTraces', 'OTel Traces');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-otel-traces', serviceName, environment, minDurationMs, timeRange],
    queryFn: (): Promise<TracesResponse> =>
      client
        .get<TracesResponse>('/telemetry/traces', {
          params: {
            serviceName,
            environment,
            from,
            until,
            minDurationMs,
            limit: 20,
          },
        })
        .then((r) => r.data),
  });

  // ── Detail mode ────────────────────────────────────────────────────────
  if (selectedTraceId !== null) {
    return (
      <TraceDetail
        traceId={selectedTraceId}
        onBack={() => setSelectedTraceId(null)}
      />
    );
  }

  // ── List mode ──────────────────────────────────────────────────────────
  return (
    <div className="h-full flex flex-col gap-1 p-2">
      {/* Header */}
      <div className="flex items-center gap-1.5 shrink-0">
        <GitBranch size={13} className="text-purple-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
      </div>

      {/* Body */}
      {isLoading ? (
        <div className="flex-1 flex flex-col gap-1.5">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} variant="text" className="h-4 w-full" />
          ))}
        </div>
      ) : isError ? (
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
      ) : (data ?? []).length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {t('governance.otelTraces.noData', 'No traces')}
          </span>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto min-h-0">
          <table className="w-full text-[10px] border-collapse">
            <thead className="sticky top-0 bg-white dark:bg-gray-900 z-10">
              <tr className="text-gray-500 dark:text-gray-400">
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('governance.otelTraces.colId', 'Trace ID')}
                </th>
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('governance.otelTraces.colService', 'Service')}
                </th>
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('governance.otelTraces.colOperation', 'Operation')}
                </th>
                <th className="text-right pb-1 pr-1 font-medium">
                  {t('governance.otelTraces.colDuration', 'ms')}
                </th>
                <th className="text-center pb-1 font-medium">
                  {t('governance.otelTraces.colStatus', 'OK')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(data ?? []).map((trace) => (
                <tr
                  key={trace.traceId}
                  className="border-t border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer transition-colors"
                  onClick={() => setSelectedTraceId(trace.traceId)}
                  aria-label={`${t('governance.otelTraces.openTrace', 'Open trace')} ${trace.traceId}`}
                >
                  <td className="py-0.5 pr-1 font-mono text-gray-500 dark:text-gray-400 whitespace-nowrap">
                    {trace.traceId.slice(0, 8)}
                  </td>
                  <td className="py-0.5 pr-1 text-gray-600 dark:text-gray-300 truncate max-w-[80px]">
                    {trace.serviceName}
                  </td>
                  <td className="py-0.5 pr-1 text-gray-700 dark:text-gray-300 truncate max-w-[120px]">
                    {trace.operationName}
                  </td>
                  <td className={`py-0.5 pr-1 text-right tabular-nums ${durationColour(trace.durationMs)}`}>
                    {trace.durationMs}
                  </td>
                  <td className="py-0.5 text-center">
                    {trace.hasErrors ? (
                      <XCircle
                        size={10}
                        className="inline-block text-red-500"
                        aria-label={t('governance.otelTraces.hasErrors', 'Has errors')}
                      />
                    ) : (
                      <CheckCircle
                        size={10}
                        className="inline-block text-emerald-500"
                        aria-label={t('governance.otelTraces.ok', 'OK')}
                      />
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
