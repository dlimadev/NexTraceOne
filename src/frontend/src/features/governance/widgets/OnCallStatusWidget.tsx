/**
 * OnCallStatusWidget — estado do serviço de on-call.
 * Dados via GET /operations/on-call. Fallback gracioso enquanto o módulo não está disponível.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Phone } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface OnCallEntry {
  teamName: string;
  oncallName: string;
  until: string;
}

interface OnCallResponse {
  entries: OnCallEntry[];
}

export function OnCallStatusWidget({ config, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.onCallStatus');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-on-call', config.teamId],
    queryFn: () =>
      client
        .get<OnCallResponse>('/operations/on-call', {
          params: { teamId: config.teamId ?? undefined },
        })
        .then((r) => r.data),
    retry: false,
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <Phone size={14} className="text-indigo-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.entries.map((e) => (
          <div key={e.teamName} className="flex items-center justify-between py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
            <div>
              <div className="text-xs font-medium text-gray-900 dark:text-white">{e.teamName}</div>
              <div className="text-xs text-gray-500 dark:text-gray-400">{e.oncallName}</div>
            </div>
            <span className="text-xs text-gray-400">{e.until}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
