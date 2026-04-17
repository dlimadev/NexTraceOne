/**
 * ReliabilitySloWidget — SLO de confiabilidade por serviço.
 * Dados via GET /operations/reliability.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CheckCircle } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface SloItem {
  serviceName: string;
  sloTarget: number;
  sloActual: number;
  errorBudgetRemaining: number;
}

interface SloResponse {
  items: SloItem[];
}

export function ReliabilitySloWidget({ config, environmentId, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.reliabilitySlo');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-reliability-slo', config.serviceId, environmentId, timeRange],
    queryFn: () =>
      client
        .get<SloResponse>('/operations/reliability', {
          params: {
            serviceId: config.serviceId ?? undefined,
            environmentId: environmentId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <CheckCircle size={14} className="text-green-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.items.slice(0, 5).map((item) => (
          <div key={item.serviceName} className="py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
            <div className="flex items-center justify-between">
              <span className="text-xs text-gray-700 dark:text-gray-300 truncate flex-1 mr-2">{item.serviceName}</span>
              <span className={`text-xs font-semibold tabular-nums ${item.sloActual >= item.sloTarget ? 'text-green-500' : 'text-red-500'}`}>
                {item.sloActual?.toFixed(2)}%
              </span>
            </div>
            <div className="mt-1 h-1 rounded-full bg-gray-200 dark:bg-gray-700 overflow-hidden">
              <div
                className={`h-full rounded-full ${item.sloActual >= item.sloTarget ? 'bg-green-500' : 'bg-red-500'}`}
                style={{ width: `${Math.min(100, item.sloActual ?? 0)}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
