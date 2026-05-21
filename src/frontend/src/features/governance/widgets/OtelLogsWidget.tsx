/**
 * OtelLogsWidget — exibe stream de logs estruturados OpenTelemetry.
 * Dados via GET /api/v1/telemetry/logs.
 * Clicar numa linha de log aplica cross-filter pelo serviço do log.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ScrollText } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface LogEntry {
  id: string;
  timestamp: string;
  severity: string;
  message: string;
  serviceName?: string;
  environment?: string;
  attributes?: Record<string, unknown>;
}

interface LogsResponseWrapped {
  items?: LogEntry[];
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
      return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400';
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
  const level = config.logSeverity ?? undefined;
  const environment = config.otelEnvironment ?? environmentId ?? undefined;
  const { from, until } = resolveTimeRange(timeRange);

  const displayTitle =
    title ?? t('governance.customDashboards.widgets.otelLogs', 'OTel Logs');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-otel-logs', serviceName, level, environment, timeRange],
    queryFn: (): Promise<LogEntry[]> =>
      client
        .get<LogsResponseWrapped | LogEntry[]>('/telemetry/logs', {
          params: {
            serviceName,
            level,
            environment,
            from,
            until,
            limit: 50,
          },
        })
        .then((r) => {
          // API may return array directly or wrapped in { items: [...] }
          const raw = r.data;
          if (Array.isArray(raw)) return raw;
          return raw.items ?? [];
        }),
  });

  return (
    <div className="h-full flex flex-col gap-1 p-2">
      {/* Header */}
      <div className="flex items-center gap-1.5 shrink-0">
        <ScrollText size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        {level && (
          <span className={`ml-auto text-[9px] px-1 py-0.5 rounded font-medium ${severityBadgeClass(level)}`}>
            {level}
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
      ) : (data ?? []).length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {t('governance.otelLogs.noData', 'No log entries')}
          </span>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto min-h-0">
          <table className="w-full text-[10px] border-collapse">
            <thead className="sticky top-0 bg-white dark:bg-gray-900 z-10">
              <tr className="text-gray-500 dark:text-gray-400">
                <th className="text-left pb-1 pr-1.5 font-medium whitespace-nowrap">
                  {t('governance.otelLogs.colTime', 'Time')}
                </th>
                <th className="text-left pb-1 pr-1.5 font-medium">
                  {t('governance.otelLogs.colLevel', 'Level')}
                </th>
                <th className="text-left pb-1 pr-1.5 font-medium">
                  {t('governance.otelLogs.colMessage', 'Message')}
                </th>
                <th className="text-left pb-1 font-medium">
                  {t('governance.otelLogs.colService', 'Service')}
                </th>
              </tr>
            </thead>
            <tbody>
              {(data ?? []).map((entry) => (
                <tr
                  key={entry.id}
                  className="border-t border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50 cursor-pointer transition-colors"
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
                  <td className="py-0.5 pr-1.5 max-w-[160px] truncate text-gray-700 dark:text-gray-300">
                    {entry.message}
                  </td>
                  <td className="py-0.5 text-gray-500 dark:text-gray-400 truncate max-w-[80px]">
                    {entry.serviceName ?? '—'}
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
