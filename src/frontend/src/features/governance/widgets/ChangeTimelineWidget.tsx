/**
 * ChangeTimelineWidget — exibe timeline dos últimos deploys/mudanças.
 * Dados via GET /governance/changes com filtro de período.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { GitCommit, CheckCircle2, XCircle, Clock } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import { timeRangeToDays } from './WidgetRegistry';
import { WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

interface ChangeEvent {
  changeId: string;
  title: string;
  serviceName: string;
  status: 'completed' | 'failed' | 'in-progress' | string;
  environment: string;
  occurredAt: string;
}

interface ChangeListResponse {
  items: ChangeEvent[];
  totalCount: number;
}

const STATUS_ICON: Record<string, React.ReactNode> = {
  completed:   <CheckCircle2 size={12} className="text-green-500 shrink-0" />,
  failed:      <XCircle size={12} className="text-red-500 shrink-0" />,
  'in-progress': <Clock size={12} className="text-yellow-500 shrink-0" />,
};

function defaultIcon() {
  return <GitCommit size={12} className="text-gray-400 shrink-0" />;
}

/** Formats a timestamp as a human-readable relative time string (e.g. "5m ago") */
function formatRelativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60_000);
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function ChangeTimelineWidget({ config, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-change-timeline', config.serviceId, config.teamId, timeRange],
    queryFn: () =>
      client
        .get<ChangeListResponse>('/governance/changes', {
          params: {
            periodDays: timeRangeToDays(timeRange),
            serviceId: config.serviceId ?? undefined,
            teamId: config.teamId ?? undefined,
            pageSize: 6,
            page: 1,
          },
        })
        .then((r) => r.data),
  });

  const displayTitle = title ?? t('governance.customDashboards.widgets.changeTimeline', 'Change Timeline');

  if (isLoading) {
    return (
      <div className="h-full flex flex-col gap-2 p-1">
        <Skeleton variant="text" height="h-4" width="w-32" />
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} variant="text" height="h-8" width="w-full" />
        ))}
      </div>
    );
  }

  if (isError || !data) return <WidgetError title={displayTitle} />;

  const items = data.items.slice(0, 6);

  return (
    <div className="h-full flex flex-col gap-1 p-1 overflow-hidden">
      <div className="flex items-center gap-2 mb-1">
        <GitCommit size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        <span className="ml-auto text-xs text-gray-400">{data.totalCount} total</span>
      </div>

      {items.length === 0 ? (
        <div className="flex-1 flex items-center justify-center text-xs text-gray-400">
          {t('governance.dashboardView.noChanges', 'No recent changes')}
        </div>
      ) : (
        <div className="flex flex-col gap-1 overflow-hidden flex-1">
          {items.map((ev) => (
            <div
              key={ev.changeId}
              className="flex items-start gap-1.5 rounded bg-gray-50 dark:bg-gray-800/50 px-2 py-1"
            >
              {STATUS_ICON[ev.status] ?? defaultIcon()}
              <div className="flex flex-col min-w-0 flex-1">
                <span className="text-xs font-medium text-gray-900 dark:text-white truncate leading-tight">
                  {ev.title}
                </span>
                <span className="text-[10px] text-gray-400 truncate leading-tight">
                  {ev.serviceName} · {ev.environment}
                </span>
              </div>
              <span className="text-[10px] text-gray-400 shrink-0 tabular-nums whitespace-nowrap">
                {formatRelativeTime(ev.occurredAt)}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
