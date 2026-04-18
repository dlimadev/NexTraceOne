/**
 * CostTrendWidget — tendência de custo operacional por serviço/equipa.
 * Dados via GET /governance/finops.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface FinOpsResponse {
  items: { service: string; cost: number; trend: string }[];
  totalCost: number;
}

export function CostTrendWidget({ config, environmentId, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.costTrend');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-finops', config.serviceId, config.teamId, environmentId, timeRange],
    queryFn: () =>
      client
        .get<FinOpsResponse>('/governance/finops', {
          params: {
            serviceId: config.serviceId ?? undefined,
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
        <TrendingUp size={14} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs font-medium text-gray-500 dark:text-gray-400">
          ${(data.totalCost ?? 0).toLocaleString()}
        </span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.items.slice(0, 5).map((item, i) => (
          <div key={i} className="flex items-center justify-between py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
            <span className="text-xs text-gray-700 dark:text-gray-300 truncate flex-1 mr-2">{item.service}</span>
            <span className="text-xs font-semibold tabular-nums text-gray-900 dark:text-white">
              ${item.cost.toLocaleString()}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
