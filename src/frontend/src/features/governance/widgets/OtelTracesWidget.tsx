/**
 * ObsTracesWidget — exibe lista de traces distribuídos.
 * Dados via GET /api/v1/governance/observability/traces
 * e GET /api/v1/telemetry/traces/{traceId} para detalhe de spans.
 * Clicar num trace expande a vista de detalhe com spans.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Waypoints, ArrowLeft, CheckCircle2, XCircle, Settings } from 'lucide-react';
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
  spanCount: number;
}

interface DashboardTracesResult {
  traces: TraceEntry[];
  isBackendAvailable: boolean;
}

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

// ── Duration colour ────────────────────────────────────────────────────────

function durationColour(ms: number): string {
  if (ms > 1000) return 'text-danger';
  if (ms > 200)  return 'text-warning';
  return 'text-success';
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
    queryKey: ['widget-obs-trace-detail', traceId],
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
        className="flex items-center gap-1 text-[10px] text-accent hover:text-accent-hover transition-colors shrink-0"
        aria-label={t('obs.traces.backToList', 'Back to traces')}
      >
        <ArrowLeft size={11} />
        {t('obs.traces.backToList', 'Back')}
      </button>
      <span className="text-[10px] font-mono text-muted truncate shrink-0">
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
          <span className="text-xs text-danger text-center">
            {t('governance.dashboardView.widgetError', 'Could not load data')}
          </span>
          <button
            type="button"
            onClick={() => refetch()}
            className="text-xs text-accent underline hover:no-underline"
          >
            {t('common.retry', 'Retry')}
          </button>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto min-h-0">
          <table className="w-full text-[10px] border-collapse">
            <thead className="sticky top-0 bg-card z-10">
              <tr className="text-muted">
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('obs.traces.colOperation', 'Operation')}
                </th>
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('obs.traces.colService', 'Service')}
                </th>
                <th className="text-right pb-1 font-medium">
                  {t('obs.traces.colDuration', 'ms')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(data?.spans ?? []).map((span) => (
                <tr
                  key={span.spanId}
                  className="border-t border-edge"
                >
                  <td className="py-0.5 pr-1 truncate max-w-[140px] text-body">
                    {span.hasError && (
                      <XCircle
                        size={9}
                        className="inline-block mr-0.5 text-danger shrink-0"
                        aria-label={t('obs.traces.errorSpan', 'Error')}
                      />
                    )}
                    {span.operationName}
                  </td>
                  <td className="py-0.5 pr-1 text-muted truncate max-w-[80px]">
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
  const environment = environmentId ?? config.serviceId ?? undefined;
  const minDurationMs = config.minDurationMs ?? undefined;

  const displayTitle =
    title ??
    config.customTitle ??
    t('governance.customDashboards.widgets.obsTraces', 'Distributed Traces');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-obs-traces', serviceName, environment, minDurationMs, timeRange],
    queryFn: (): Promise<DashboardTracesResult> =>
      client
        .get<DashboardTracesResult>('/governance/observability/traces', {
          params: {
            serviceName,
            environment,
            timeRange,
            minDurationMs,
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
        <Waypoints size={13} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-heading truncate">
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
          <span className="text-xs text-danger text-center">
            {t('governance.dashboardView.widgetError', 'Could not load data')}
          </span>
          <button
            type="button"
            onClick={() => refetch()}
            className="text-xs text-accent underline hover:no-underline"
          >
            {t('common.retry', 'Retry')}
          </button>
        </div>
      ) : data && !data.isBackendAvailable ? (
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <Settings size={18} className="text-faded" />
          <span className="text-xs text-muted text-center">
            {t('obs.backendNotConfigured', 'Backend not configured')}
          </span>
        </div>
      ) : (data?.traces ?? []).length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-faded">
            {t('obs.traces.noData', 'No traces')}
          </span>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto min-h-0">
          <table className="w-full text-[10px] border-collapse">
            <thead className="sticky top-0 bg-card z-10">
              <tr className="text-muted">
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('obs.traces.colId', 'Trace ID')}
                </th>
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('obs.traces.colService', 'Service')}
                </th>
                <th className="text-left pb-1 pr-1 font-medium">
                  {t('obs.traces.colOperation', 'Operation')}
                </th>
                <th className="text-right pb-1 pr-1 font-medium">
                  {t('obs.traces.colDuration', 'ms')}
                </th>
                <th className="text-center pb-1 font-medium">
                  {t('obs.traces.colStatus', 'OK')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(data?.traces ?? []).map((trace) => (
                <tr
                  key={trace.traceId}
                  className="border-t border-edge hover:bg-hover dark:hover:bg-elevated/50 cursor-pointer transition-colors"
                  onClick={() => setSelectedTraceId(trace.traceId)}
                  aria-label={`${t('obs.traces.openTrace', 'Open trace')} ${trace.traceId}`}
                >
                  <td className="py-0.5 pr-1 font-mono text-muted whitespace-nowrap">
                    {trace.traceId.slice(0, 8)}
                  </td>
                  <td className="py-0.5 pr-1 text-muted truncate max-w-[80px]">
                    {trace.serviceName}
                  </td>
                  <td className="py-0.5 pr-1 text-body truncate max-w-[120px]">
                    {trace.operationName}
                  </td>
                  <td className={`py-0.5 pr-1 text-right tabular-nums ${durationColour(trace.durationMs)}`}>
                    {trace.durationMs}
                  </td>
                  <td className="py-0.5 text-center">
                    {trace.hasErrors ? (
                      <XCircle
                        size={10}
                        className="inline-block text-danger"
                        aria-label={t('obs.traces.hasErrors', 'Has errors')}
                      />
                    ) : (
                      <CheckCircle2
                        size={10}
                        className="inline-block text-success"
                        aria-label={t('obs.traces.ok', 'OK')}
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
