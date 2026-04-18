/**
 * IncidentSummaryWidget — resumo de incidentes abertos e recentes.
 * Dados via GET /operations/incidents.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface IncidentsResponse {
  items: { id: string; title: string; severity: string; status: string }[];
  totalCount: number;
}

const SEVERITY_COLOR: Record<string, string> = {
  critical: 'text-red-500',
  high: 'text-orange-500',
  medium: 'text-yellow-500',
  low: 'text-blue-400',
};

export function IncidentSummaryWidget({ config, timeRange, title, environmentId }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.incidentSummary');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-incidents', config.serviceId, environmentId, timeRange],
    queryFn: () =>
      client
        .get<IncidentsResponse>('/operations/incidents', {
          params: {
            serviceId: config.serviceId ?? undefined,
            environmentId: environmentId ?? undefined,
            status: 'open',
            page: 1,
            pageSize: 5,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <AlertTriangle size={14} className="text-red-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs font-semibold text-red-500">{data.totalCount} open</span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.items.length === 0 ? (
          <p className="text-xs text-gray-400 text-center mt-4">
            {t('governance.dashboardView.noIncidents', 'No open incidents')}
          </p>
        ) : (
          data.items.slice(0, 5).map((inc) => (
            <div key={inc.id} className="flex items-center justify-between py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
              <span className="text-xs text-gray-700 dark:text-gray-300 truncate flex-1 mr-2">{inc.title}</span>
              <span className={`text-xs font-semibold ${SEVERITY_COLOR[inc.severity] ?? 'text-gray-400'}`}>
                {inc.severity}
              </span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
