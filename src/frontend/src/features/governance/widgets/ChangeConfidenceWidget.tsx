/**
 * ChangeConfidenceWidget — scores de confiança em mudanças recentes.
 * Dados via GET /changes/confidence.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ShieldCheck } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface ConfidenceItem {
  changeId: string;
  serviceName: string;
  confidenceScore: number;
  riskLevel: string;
}

interface ConfidenceResponse {
  items: ConfidenceItem[];
  averageConfidence: number;
}

const RISK_COLOR: Record<string, string> = {
  Low: 'text-green-500',
  Medium: 'text-yellow-500',
  High: 'text-orange-500',
  Critical: 'text-red-500',
};

export function ChangeConfidenceWidget({ config, environmentId, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.changeConfidence');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-change-confidence', config.serviceId, environmentId, timeRange],
    queryFn: () =>
      client
        .get<ConfidenceResponse>('/changes/confidence', {
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
        <ShieldCheck size={14} className="text-green-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs font-semibold text-green-500">
          {t('governance.dashboardView.avgScore', 'Avg')}: {(data.averageConfidence ?? 0).toFixed(0)}%
        </span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.items.slice(0, 5).map((item) => (
          <div key={item.changeId} className="flex items-center justify-between py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
            <span className="text-xs text-gray-700 dark:text-gray-300 truncate flex-1 mr-2">{item.serviceName}</span>
            <span className={`text-xs font-semibold tabular-nums ${RISK_COLOR[item.riskLevel] ?? 'text-gray-400'}`}>
              {item.confidenceScore}%
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
