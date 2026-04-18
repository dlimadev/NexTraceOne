/**
 * TopServicesWidget — lista dos top N serviços por incidentes em aberto.
 * Reforça o pilar de Service Governance e Operational Reliability do NexTraceOne.
 * Dados via GET /governance/services com sortBy=incidents.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ListOrdered } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import { timeRangeToDays } from './WidgetRegistry';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface ServiceIncidentRow {
  serviceId: string;
  serviceName: string;
  openIncidents: number;
  healthStatus: 'healthy' | 'degraded' | 'critical';
}

interface TopServicesResponse {
  items: ServiceIncidentRow[];
}

// ── Health colour helpers ──────────────────────────────────────────────────

const HEALTH_CLASS: Record<string, string> = {
  healthy:  'text-emerald-600 dark:text-emerald-400',
  degraded: 'text-amber-600 dark:text-amber-400',
  critical: 'text-red-600 dark:text-red-400',
};

// ── Component ──────────────────────────────────────────────────────────────

export function TopServicesWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.topServices', 'Top Services');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-top-services', config.teamId, config.serviceId, timeRange],
    queryFn: () =>
      client
        .get<TopServicesResponse>('/governance/services', {
          params: {
            sortBy: 'incidents',
            pageSize: 5,
            periodDays: timeRangeToDays(timeRange),
            teamId: config.teamId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <ListOrdered size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        <span className="ml-auto text-[10px] text-gray-400 shrink-0">
          {t('governance.customDashboards.topServices.header', 'incidents')}
        </span>
      </div>

      {data.items.length === 0 ? (
        <div className="flex-1 flex items-center justify-center text-xs text-gray-400">
          {t('governance.customDashboards.topServices.noData', 'No service data available')}
        </div>
      ) : (
        <div className="flex-1 overflow-auto">
          {data.items.map((svc, idx) => {
            const healthCls = HEALTH_CLASS[svc.healthStatus] ?? HEALTH_CLASS.degraded;
            return (
              <div
                key={svc.serviceId}
                className="flex items-center gap-2 py-1 border-b border-gray-100 dark:border-gray-800 last:border-0"
              >
                <span className="text-[10px] font-mono text-gray-400 w-4 shrink-0 text-right">
                  {idx + 1}
                </span>
                <span className="text-xs text-gray-700 dark:text-gray-300 truncate flex-1">
                  {svc.serviceName}
                </span>
                <span className={`text-xs font-bold tabular-nums ${healthCls}`}>
                  {svc.openIncidents}
                </span>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
