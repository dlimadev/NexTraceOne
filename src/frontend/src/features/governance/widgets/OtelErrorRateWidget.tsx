/**
 * ObsErrorRateWidget — exibe contagem de erros e top erros de observabilidade.
 * Dados via GET /api/v1/governance/observability/errors.
 * Estado verde "All Clear" quando sem erros; vermelho/laranja com erros.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, CheckCircle2, Settings } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface ErrorEntry {
  message: string;
  serviceName: string;
  count: number;
  severity: string;
  lastSeen: string;
}

interface DashboardErrorsResult {
  errors: ErrorEntry[];
  totalErrorCount: number;
  isBackendAvailable: boolean;
}

// ── Count bar helpers ──────────────────────────────────────────────────────

function countBarWidth(count: number, max: number): string {
  if (max === 0) return '0%';
  return `${Math.max((count / max) * 100, 4)}%`;
}

// ── Component ──────────────────────────────────────────────────────────────

export function OtelErrorRateWidget({
  config,
  environmentId,
  timeRange,
  title,
}: WidgetProps): React.ReactElement {
  const { t } = useTranslation();

  const environment = environmentId ?? config.serviceId ?? 'production';

  const displayTitle =
    title ??
    config.customTitle ??
    t('governance.customDashboards.widgets.obsErrorRate', 'Error Rate');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-obs-error-rate', environment, timeRange],
    queryFn: (): Promise<DashboardErrorsResult> =>
      client
        .get<DashboardErrorsResult>('/governance/observability/errors', {
          params: {
            environment,
            timeRange,
            top: 5,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) {
    return (
      <div className="h-full flex flex-col gap-2 p-2">
        <div className="flex items-center gap-1.5 shrink-0">
          <AlertTriangle size={13} className="text-orange-500 shrink-0" />
          <span className="text-xs font-semibold text-heading truncate">
            {displayTitle}
          </span>
        </div>
        <div className="flex-1 flex flex-col gap-1.5">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} variant="text" className="h-4 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="h-full flex flex-col items-center justify-center gap-2 p-2">
        <AlertTriangle size={18} className="text-orange-400" />
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
    );
  }

  // Backend not configured — neutral info state
  if (data && !data.isBackendAvailable) {
    return (
      <div className="h-full flex flex-col gap-2 p-2">
        <div className="flex items-center gap-1.5 shrink-0">
          <AlertTriangle size={13} className="text-orange-500 shrink-0" />
          <span className="text-xs font-semibold text-heading truncate">
            {displayTitle}
          </span>
        </div>
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <Settings size={18} className="text-faded" />
          <span className="text-xs text-muted text-center">
            {t('obs.backendNotConfigured', 'Backend not configured')}
          </span>
        </div>
      </div>
    );
  }

  const errors = data?.errors ?? [];
  const totalErrors = data?.totalErrorCount ?? errors.reduce((sum, e) => sum + e.count, 0);
  const maxCount = Math.max(...errors.map((e) => e.count), 1);
  const hasErrors = totalErrors > 0;

  return (
    <div className="h-full flex flex-col gap-1.5 p-2">
      {/* Header */}
      <div className="flex items-center gap-1.5 shrink-0">
        <AlertTriangle
          size={13}
          className={hasErrors ? 'text-red-500 shrink-0' : 'text-emerald-500 shrink-0'}
        />
        <span className="text-xs font-semibold text-heading truncate">
          {displayTitle}
        </span>
      </div>

      {/* Total stat */}
      <div
        className={`shrink-0 rounded-md px-2 py-1.5 flex items-center gap-2 ${
          hasErrors
            ? 'bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800'
            : 'bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800'
        }`}
      >
        {hasErrors ? (
          <>
            <AlertTriangle size={14} className="text-red-500 shrink-0" />
            <span
              className="text-2xl font-bold tabular-nums text-red-600 dark:text-red-400 leading-none"
              aria-label={t('obs.errors.totalCount', 'Total error count')}
            >
              {totalErrors.toLocaleString()}
            </span>
            <span className="text-xs text-red-500 dark:text-red-400 ml-auto">
              {t('obs.errors.errorsLabel', 'errors')}
            </span>
          </>
        ) : (
          <>
            <CheckCircle2 size={14} className="text-emerald-500 shrink-0" />
            <span className="text-sm font-semibold text-emerald-600 dark:text-emerald-400">
              {t('obs.errors.allClear', 'All Clear')}
            </span>
          </>
        )}
      </div>

      {/* Top errors list */}
      {hasErrors && (
        <div className="flex-1 flex flex-col gap-1 overflow-y-auto min-h-0">
          {errors.map((error, idx) => (
            <div key={idx} className="flex flex-col gap-0.5">
              <div className="flex items-center justify-between gap-1">
                <span
                  className="text-[10px] text-body truncate flex-1"
                  title={error.message}
                >
                  {error.message}
                </span>
                <span
                  className="text-[10px] font-semibold tabular-nums text-red-600 dark:text-red-400 shrink-0"
                  aria-label={`${error.count} ${t('obs.errors.occurrences', 'occurrences')}`}
                >
                  ×{error.count}
                </span>
              </div>
              {/* Proportional count bar */}
              <div className="h-1 w-full bg-elevated rounded-full overflow-hidden">
                <div
                  className="h-full bg-red-400 dark:bg-red-500 rounded-full transition-all"
                  style={{ width: countBarWidth(error.count, maxCount) }}
                  role="meter"
                  aria-valuenow={error.count}
                  aria-valuemax={maxCount}
                  aria-label={error.message}
                />
              </div>
              <span className="text-[9px] text-faded">
                {error.serviceName}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* Footer */}
      <div className="shrink-0 text-[9px] text-faded text-right">
        {environment} · {timeRange}
      </div>
    </div>
  );
}
