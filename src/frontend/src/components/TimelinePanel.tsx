import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

export type TimelineEventType = 'event' | 'change' | 'incident' | 'approval';

export interface TimelineEntry {
  timestamp: string;
  description: string;
  /** Visual marker type — determines dot color. */
  type?: TimelineEventType;
  /** Optional extra content rendered below the description. */
  detail?: ReactNode;
}

interface TimelinePanelProps {
  entries: TimelineEntry[];
  /** Custom date formatter; defaults to locale short date+time. */
  formatDate?: (iso: string) => string;
  /** Show empty state message when entries is empty. */
  emptyMessage?: string;
  className?: string;
}

const dotColor: Record<TimelineEventType, string> = {
  event: 'bg-accent',
  change: 'bg-warning',
  incident: 'bg-critical',
  approval: 'bg-success',
};

function defaultFormatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

/**
 * TimelinePanel — renderiza uma linha do tempo vertical de eventos.
 * Extraído do IncidentDetailPage; reutilizável em ServiceDetailPage,
 * ChangeDetailPage, etc.
 *
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F3-02
 */
export function TimelinePanel({
  entries,
  formatDate = defaultFormatDate,
  emptyMessage = 'No events recorded.',
  className,
}: TimelinePanelProps) {
  if (entries.length === 0) {
    return (
      <p className={cn('text-sm text-muted', className)}>{emptyMessage}</p>
    );
  }

  return (
    <div className={cn('space-y-3', className)}>
      {entries.map((entry, idx) => {
        const type = entry.type ?? 'event';
        const isFirst = idx === 0;
        const isLast = idx === entries.length - 1;

        return (
          <div key={idx} className="flex gap-3">
            {/* Connector column */}
            <div className="flex flex-col items-center">
              <div
                className={cn(
                  'w-2 h-2 rounded-full mt-1.5 shrink-0',
                  isFirst ? dotColor[type] : 'bg-edge-strong',
                )}
              />
              {!isLast && <div className="w-px flex-1 bg-edge" />}
            </div>

            {/* Content */}
            <div className="pb-3 min-w-0">
              <p className="text-xs text-muted">{formatDate(entry.timestamp)}</p>
              <p className="text-sm text-body">{entry.description}</p>
              {entry.detail && (
                <div className="mt-1">{entry.detail}</div>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
