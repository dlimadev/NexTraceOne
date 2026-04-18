/**
 * AlertStatusWidget — exibe contagem de alertas activos por severidade.
 * Dados via GET /governance/alerts/summary.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle } from 'lucide-react';
import { timeRangeToDays } from './WidgetRegistry';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

interface AlertSummaryResponse {
  critical: number;
  high: number;
  medium: number;
  low: number;
  total: number;
}

const SEV_STYLES: Record<string, { label: string; cls: string }> = {
  critical: { label: 'Critical', cls: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' },
  high:     { label: 'High',     cls: 'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300' },
  medium:   { label: 'Medium',   cls: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
  low:      { label: 'Low',      cls: 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300' },
};

export function AlertStatusWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-alert-status', config.serviceId, config.teamId, timeRange],
    queryFn: () =>
      client
        .get<AlertSummaryResponse>('/governance/alerts/summary', {
          params: {
            periodDays: timeRangeToDays(timeRange),
            serviceId: config.serviceId ?? undefined,
            teamId: config.teamId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  const displayTitle = title ?? t('governance.customDashboards.widgets.alertStatus', 'Alert Status');

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  const severities = ['critical', 'high', 'medium', 'low'] as const;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <AlertTriangle size={14} className="text-orange-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs font-medium text-gray-500 dark:text-gray-400">
          {t('governance.dashboardView.alertTotal', 'Total')}: {data.total}
        </span>
      </div>
      <div className="grid grid-cols-2 gap-2 flex-1">
        {severities.map((sev) => {
          const st = SEV_STYLES[sev];
          return (
            <div key={sev} className={`rounded p-2 flex flex-col items-center justify-center ${st.cls}`}>
              <span className="text-lg font-bold tabular-nums">{data[sev]}</span>
              <span className="text-xs font-medium">{t(`governance.alertSeverity.${sev}`, st.label)}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
