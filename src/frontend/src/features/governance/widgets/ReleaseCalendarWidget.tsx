/**
 * ReleaseCalendarWidget — calendário horizontal de mudanças/releases num horizonte de 7 dias.
 * Reforça o pilar de Change Intelligence & Production Change Confidence do NexTraceOne.
 * Dados via GET /governance/changes/calendar.
 */
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CalendarDays } from 'lucide-react';
import { WidgetSkeleton, WidgetError } from './DoraMetricsWidget';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

type ChangeType = 'deploy' | 'release' | 'patch' | 'rollback' | 'config';
type ChangeStatus = 'planned' | 'in-progress' | 'completed' | 'cancelled';

interface CalendarChange {
  changeId: string;
  serviceName: string;
  type: ChangeType;
  environment: string;
  scheduledAt: string;
  status: ChangeStatus;
}

interface ReleaseCalendarResponse {
  items: CalendarChange[];
  fromDate: string;
  toDate: string;
}

// ── Colour helpers ─────────────────────────────────────────────────────────

const CHANGE_TYPE_COLOR: Record<string, string> = {
  deploy:   'bg-blue-500',
  release:  'bg-emerald-500',
  patch:    'bg-amber-500',
  rollback: 'bg-red-500',
  config:   'bg-violet-500',
};

const STATUS_CLASSES: Record<string, string> = {
  planned:      'opacity-60',
  'in-progress': 'opacity-100',
  completed:    'opacity-40',
  cancelled:    'opacity-20 line-through',
};

function formatDayLabel(dateStr: string): string {
  const d = new Date(dateStr + 'T00:00:00');
  return d.toLocaleDateString(undefined, { weekday: 'short', day: 'numeric' });
}

// ── Component ──────────────────────────────────────────────────────────────

export function ReleaseCalendarWidget({ config, environmentId, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const displayTitle =
    title ?? t('governance.customDashboards.widgets.releaseCalendar', 'Release Calendar');

  const days = timeRange === '30d' ? 14 : 7;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['widget-release-calendar', config.serviceId, config.teamId, environmentId, days],
    queryFn: () =>
      client
        .get<ReleaseCalendarResponse>('/governance/changes/calendar', {
          params: {
            days,
            serviceId: config.serviceId ?? undefined,
            teamId: config.teamId ?? undefined,
            environmentId: environmentId ?? undefined,
          },
        })
        .then((r) => r.data),
  });

  if (isLoading) return <WidgetSkeleton title={displayTitle} />;
  if (isError || !data) return <WidgetError title={displayTitle} />;

  // Group changes by calendar day (ISO date prefix)
  const byDay: Record<string, CalendarChange[]> = {};
  data.items.forEach((c) => {
    const day = c.scheduledAt.substring(0, 10);
    if (!byDay[day]) byDay[day] = [];
    byDay[day].push(c);
  });

  const sortedDays = Object.keys(byDay).sort();

  return (
    <div className="h-full flex flex-col gap-2 p-1">
      {/* Header */}
      <div className="flex items-center gap-2">
        <CalendarDays size={14} className="text-accent shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        <span className="ml-auto text-[10px] text-gray-400 shrink-0 tabular-nums">
          {data.items.length}{' '}
          {t('governance.customDashboards.releaseCalendar.changes', 'changes')}
        </span>
      </div>

      {data.items.length === 0 ? (
        <div className="flex-1 flex items-center justify-center text-xs text-gray-400">
          {t('governance.customDashboards.releaseCalendar.noChanges', 'No changes scheduled')}
        </div>
      ) : (
        <div className="flex-1 overflow-auto flex flex-col gap-1.5">
          {sortedDays.map((day) => (
            <div key={day} className="flex gap-2 items-start min-h-[20px]">
              {/* Day label */}
              <span className="text-[9px] text-gray-400 font-mono w-14 shrink-0 pt-0.5 tabular-nums">
                {formatDayLabel(day)}
              </span>

              {/* Change tags */}
              <div className="flex flex-wrap gap-1 flex-1">
                {byDay[day].map((c) => {
                  const colorClass = CHANGE_TYPE_COLOR[c.type] ?? 'bg-gray-400';
                  const statusClass = STATUS_CLASSES[c.status] ?? '';
                  return (
                    <span
                      key={c.changeId}
                      title={`${c.serviceName} · ${c.type} · ${c.environment} · ${c.status}`}
                      className={`inline-flex items-center rounded px-1.5 py-0.5 text-[9px] font-medium text-white ${colorClass} ${statusClass}`}
                    >
                      {c.serviceName}
                    </span>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Legend */}
      <div className="flex flex-wrap gap-2 pt-0.5 border-t border-gray-100 dark:border-gray-800">
        {(['deploy', 'release', 'patch', 'rollback'] as ChangeType[]).map((type) => (
          <span key={type} className="flex items-center gap-1">
            <span className={`inline-block w-2 h-2 rounded-full ${CHANGE_TYPE_COLOR[type]}`} />
            <span className="text-[9px] text-gray-400 capitalize">{type}</span>
          </span>
        ))}
      </div>
    </div>
  );
}
