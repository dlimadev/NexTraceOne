/**
 * KnowledgeGraphWidget — grafo de relações de conhecimento operacional.
 * Dados via GET /knowledge. Fallback gracioso enquanto o módulo não está disponível.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { BookOpen } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import client from '../../../api/client';
import type { WidgetProps } from './WidgetRegistry';

interface KnowledgeItem {
  id: string;
  title: string;
  type: string;
  relevanceScore: number;
}

interface KnowledgeResponse {
  items: KnowledgeItem[];
}

export function KnowledgeGraphWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle = title ?? t('governance.customDashboards.widgets.knowledgeGraph');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-knowledge', config.serviceId, config.teamId, timeRange],
    queryFn: () =>
      client
        .get<KnowledgeResponse>('/knowledge', {
          params: {
            serviceId: config.serviceId ?? undefined,
            teamId: config.teamId ?? undefined,
            page: 1,
            pageSize: 5,
          },
        })
        .then((r) => r.data),
    retry: false,
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  // Graceful fallback — knowledge module may not yet be available
  if (isError || !data) return <WidgetError title={displayTitle} />;

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      <div className="flex items-center gap-2">
        <BookOpen size={14} className="text-purple-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>
      <div className="flex-1 overflow-auto">
        {data.items.slice(0, 5).map((item) => (
          <div key={item.id} className="flex items-center justify-between py-1 border-b border-gray-100 dark:border-gray-800 last:border-0">
            <span className="text-xs text-gray-700 dark:text-gray-300 truncate flex-1 mr-2">{item.title}</span>
            <span className="text-xs text-gray-400">{item.type}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
