/**
 * ObsLogsWidget — exibe stream de logs estruturados.
 * Dados via GET /api/v1/governance/observability/logs.
 * Clicar numa linha de log aplica cross-filter pelo serviço do log.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ScrollText, Settings } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface LogEntry {
  timestamp: string;
  severity: string;
  serviceName: string;
  message: string;
  traceId?: string;
  environment?: string;
}

interface DashboardLogsResult {
  entries: LogEntry[];
  totalCount: number;
  isBackendAvailable: boolean;
}

// ── Severity styling ───────────────────────────────────────────────────────

function severityBadgeClass(severity: string): string {
  switch (severity.toUpperCase()) {
    case 'ERROR':
    case 'FATAL':
      return 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300';
    case 'WARN':
    case 'WARNING':
      return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300';
    case 'INFO':
      return 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300';
    default:
      return 'bg-elevated text-muted';
  }
}

// ── Component ──────────────────────────────────────────────────────────────

export function OtelLogsWidget({
  config,
  environmentId,
  timeRange,
  title,
  onCrossFilter,
}: WidgetProps): React.ReactElement {
  const { t } = useTranslation();

  const serviceName = config.serviceId ?? undefined;
  const severity = config.logSeverity ?? undefined;
  const environment = environmentId ?? config.serviceId ?? undefined;

  const displayTitle =
    title ??
    config.customTitle ??
    t('governance.customDashboards.widgets.obsLogs', 'Log Stream');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-obs-logs', serviceName, severity, environment, timeRange],
    queryFn: (): Promise<DashboardLogsResult> =>
      client
        .get<DashboardLogsResult>('/governance/observability/logs', {
          params: {
            serviceName,
            severity,
            environment,
            timeRange,
          },
        })
        .then((r) => r.data),
  });

  return (
    <div className="h-full flex flex-col gap-1 p-2">
      {/* Header */}
      <div className="flex items-center gap-1.5 shrink-0">
        <ScrollText size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-heading truncate">
          {displayTitle}
        </span>
        {severity && (
          <span className={`ml-auto text-[9px] px-1 py-0.5 rounded font-medium ${severityBadgeClass(severity)}`}>
            {severity}
          </span>
        )}
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
      ) : data && !data.isBackendAvailable ? (
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <Settings size={18} className="text-faded" />
          <span className="text-xs text-muted text-center">
            {t('obs.backendNotConfigured', 'Backend not configured')}
          </span>
        </div>
      ) : (data?.entries ?? []).length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-faded">
            {t('obs.logs.noData', 'No log entries')}
          </span>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto min-h-0">
          <table className="w-full text-[10px] border-collapse">
            <thead className="sticky top-0 bg-card z-10">
              <tr className="text-muted">
                <th className="text-left pb-1 pr-1.5 font-medium whitespace-nowrap">
                  {t('obs.logs.colTime', 'Timestamp')}
                </th>
                <th className="text-left pb-1 pr-1.5 font-medium">
                  {t('obs.logs.colLevel', 'Level')}
                </th>
                <th className="text-left pb-1 pr-1.5 font-medium">
                  {t('obs.logs.colService', 'Service')}
                </th>
                <th className="text-left pb-1 font-medium">
                  {t('obs.logs.colMessage', 'Message')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(data?.entries ?? []).map((entry, idx) => (
                <tr
                  key={`${entry.timestamp}-${idx}`}
                  className="border-t border-edge hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer transition-colors"
                  onClick={() => {
                    if (entry.serviceName && onCrossFilter) {
                      onCrossFilter({ serviceId: entry.serviceName });
                    }
                  }}
                  aria-label={`${entry.severity}: ${entry.message}`}
                >
                  <td className="py-0.5 pr-1.5 text-gray-400 whitespace-nowrap">
                    {new Date(entry.timestamp).toLocaleTimeString(undefined, {
                      hour: '2-digit',
                      minute: '2-digit',
                      second: '2-digit',
                    })}
                  </td>
                  <td className="py-0.5 pr-1.5">
                    <span
                      className={`inline-block px-1 rounded text-[9px] font-medium ${severityBadgeClass(entry.severity)}`}
                    >
                      {entry.severity.toUpperCase().slice(0, 5)}
                    </span>
                  </td>
                  <td className="py-0.5 pr-1.5 text-muted truncate max-w-[80px]">
                    {entry.serviceName ?? '—'}
                  </td>
                  <td className="py-0.5 max-w-[160px] truncate text-body">
                    {entry.message}
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
