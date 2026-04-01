import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  GitCommit, ArrowUpCircle, Lock, FileSignature,
  AlertTriangle, CheckCircle, XCircle, Clock, RefreshCw,
} from 'lucide-react';
import { Card, CardBody } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { contractsApi } from '../../api/contracts';
import type { ContractVersion } from '../../types';

interface ChangelogSectionProps {
  apiAssetId: string;
  currentVersionId: string;
  className?: string;
}

// ── Event type mapping ────────────────────────────────────────────────────────

interface TimelineEvent {
  id: string;
  type: 'version' | 'lifecycle' | 'lock' | 'sign' | 'deprecation';
  title: string;
  description: string;
  date: string;
  icon: React.ReactNode;
  iconColor: string;
  metadata: Record<string, string>;
  isCurrent: boolean;
}

/**
 * Secção de Changelog do workspace.
 * Mostra a timeline de eventos do contrato — versões, transições de lifecycle,
 * locks, assinaturas e deprecações em formato cronológico.
 */
export function ChangelogSection({ apiAssetId, currentVersionId, className = '' }: ChangelogSectionProps) {
  const { t } = useTranslation();

  const historyQuery = useQuery({
    queryKey: ['contract-history', apiAssetId],
    queryFn: () => contractsApi.getHistory(apiAssetId),
    enabled: !!apiAssetId,
  });

  const tFn = (key: string, fallback?: string) => t(key, fallback ?? key);

  const events = useMemo(
    () => buildTimeline(historyQuery.data ?? [], currentVersionId, tFn),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [historyQuery.data, currentVersionId],
  );

  if (historyQuery.isLoading) {
    return (
      <div className={`flex items-center justify-center py-16 ${className}`}>
        <RefreshCw size={16} className="animate-spin text-muted" />
      </div>
    );
  }

  if (events.length === 0) {
    return (
      <div className={className}>
        <EmptyState
          title={t('contracts.changelog.emptyTitle', 'No history yet')}
          description={t('contracts.changelog.emptyDescription', 'Version history and lifecycle events will appear here as the contract evolves.')}
          icon={<Clock size={24} />}
        />
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold text-heading">
          {t('contracts.changelog.title', 'Changelog & History')}
        </h3>
        <span className="text-[10px] text-muted px-2 py-0.5 rounded-full bg-elevated border border-edge">
          {events.length} {t('contracts.changelog.events', 'events')}
        </span>
      </div>

      {/* Timeline */}
      <div className="relative pl-6">
        {/* Vertical line */}
        <div className="absolute left-[11px] top-2 bottom-2 w-px bg-edge" />

        <div className="space-y-1">
          {events.map((event, idx) => (
            <TimelineItem key={event.id} event={event} isFirst={idx === 0} isLast={idx === events.length - 1} />
          ))}
        </div>
      </div>
    </div>
  );
}

// ── Timeline Item ─────────────────────────────────────────────────────────────

function TimelineItem({
  event,
  isFirst,
  isLast,
}: {
  event: TimelineEvent;
  isFirst: boolean;
  isLast: boolean;
}) {
  return (
    <div className={`relative flex gap-3 ${isFirst ? '' : ''} ${isLast ? '' : ''}`}>
      {/* Icon dot */}
      <div className={`relative z-10 flex-shrink-0 flex items-center justify-center w-[22px] h-[22px] rounded-full border ${event.iconColor} ${event.isCurrent ? 'ring-2 ring-accent/40' : ''}`}>
        {event.icon}
      </div>

      {/* Content */}
      <Card className={`flex-1 ${event.isCurrent ? 'border-accent/40' : ''}`}>
        <CardBody className="py-2.5 px-3">
          <div className="flex items-start justify-between gap-2">
            <div>
              <p className="text-xs font-medium text-heading">{event.title}</p>
              {event.description && (
                <p className="text-[10px] text-muted mt-0.5">{event.description}</p>
              )}
            </div>
            <time className="text-[10px] text-muted flex-shrink-0 whitespace-nowrap">
              {formatDate(event.date)}
            </time>
          </div>

          {/* Metadata pills */}
          {Object.keys(event.metadata).length > 0 && (
            <div className="flex items-center gap-2 mt-2 flex-wrap">
              {Object.entries(event.metadata).map(([key, value]) => (
                <span key={key} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated border border-edge text-muted">
                  <span className="text-muted/60">{key}:</span> {value}
                </span>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function buildTimeline(
  versions: ContractVersion[],
  currentVersionId: string,
  t: (key: string, fallback?: string) => string,
): TimelineEvent[] {
  const events: TimelineEvent[] = [];

  // Sort versions by date descending (most recent first)
  const sorted = [...versions].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );

  for (const v of sorted) {
    const isCurrent = v.id === currentVersionId;

    // Version creation event
    events.push({
      id: `version-${v.id}`,
      type: 'version',
      title: `${t('contracts.changelog.versionCreated', 'Version')} ${v.version}`,
      description: isCurrent
        ? t('contracts.changelog.currentVersion', 'Current version')
        : '',
      date: v.createdAt,
      icon: <GitCommit size={11} />,
      iconColor: 'bg-info/15 text-info border-info/25',
      metadata: {
        [t('contracts.protocol', 'Protocol')]: v.protocol,
        [t('contracts.format', 'Format')]: v.format?.toUpperCase(),
      },
      isCurrent,
    });

    // Lifecycle state event (if not Draft)
    if (v.lifecycleState !== 'Draft') {
      events.push({
        id: `lifecycle-${v.id}`,
        type: 'lifecycle',
        title: `${t('contracts.changelog.stateChanged', 'State changed to')} ${v.lifecycleState}`,
        description: '',
        date: v.createdAt,
        icon: stateIcon(v.lifecycleState),
        iconColor: stateColor(v.lifecycleState),
        metadata: {},
        isCurrent: false,
      });
    }

    // Lock event
    if (v.isLocked && v.lockedAt) {
      events.push({
        id: `lock-${v.id}`,
        type: 'lock',
        title: t('contracts.changelog.versionLocked', 'Version locked'),
        description: v.lockedBy ? `${t('contracts.changelog.by', 'by')} ${v.lockedBy}` : '',
        date: v.lockedAt,
        icon: <Lock size={11} />,
        iconColor: 'bg-info/15 text-info border-info/25',
        metadata: {},
        isCurrent: false,
      });
    }

    // Signature event
    if (v.signedBy && v.signedAt) {
      events.push({
        id: `sign-${v.id}`,
        type: 'sign',
        title: t('contracts.changelog.versionSigned', 'Version signed'),
        description: `${t('contracts.changelog.by', 'by')} ${v.signedBy}`,
        date: v.signedAt,
        icon: <FileSignature size={11} />,
        iconColor: 'bg-success/15 text-success border-success/25',
        metadata: {},
        isCurrent: false,
      });
    }

    // Deprecation event
    if (v.deprecationNotice) {
      events.push({
        id: `deprecation-${v.id}`,
        type: 'deprecation',
        title: t('contracts.changelog.deprecated', 'Deprecated'),
        description: v.deprecationNotice,
        date: v.sunsetDate ?? v.createdAt,
        icon: <AlertTriangle size={11} />,
        iconColor: 'bg-warning/15 text-warning border-warning/25',
        metadata: v.sunsetDate ? { [t('contracts.changelog.sunsetDate', 'Sunset')]: v.sunsetDate } : {},
        isCurrent: false,
      });
    }
  }

  // Sort all events by date descending
  events.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

  return events;
}

function stateIcon(state: string): React.ReactNode {
  switch (state) {
    case 'Approved':
      return <CheckCircle size={11} />;
    case 'InReview':
      return <ArrowUpCircle size={11} />;
    case 'Deprecated':
    case 'Sunset':
      return <AlertTriangle size={11} />;
    case 'Retired':
      return <XCircle size={11} />;
    default:
      return <GitCommit size={11} />;
  }
}

function stateColor(state: string): string {
  switch (state) {
    case 'Approved':
      return 'bg-success/15 text-success border-success/25';
    case 'InReview':
      return 'bg-info/15 text-info border-info/25';
    case 'Locked':
      return 'bg-info/15 text-info border-info/25';
    case 'Deprecated':
    case 'Sunset':
      return 'bg-warning/15 text-warning border-warning/25';
    case 'Retired':
      return 'bg-critical/15 text-critical border-critical/25';
    default:
      return 'bg-elevated text-muted border-edge';
  }
}

function formatDate(dateStr: string): string {
  try {
    const d = new Date(dateStr);
    return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  } catch {
    return dateStr;
  }
}
