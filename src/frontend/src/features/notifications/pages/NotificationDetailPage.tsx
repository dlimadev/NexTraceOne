import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  AlertTriangle,
  ExternalLink,
  CheckCircle2,
  Archive,
  X,
  Mail,
  MessageSquare,
  Bell,
  Clock,
  RefreshCw,
  AlertCircle,
  CheckCheck,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { PageContainer } from '../../../components/shell/PageContainer';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { InlineMessage } from '../../../components/InlineMessage';
import {
  useNotificationById,
  useNotificationTrail,
} from '../hooks/useNotificationConfiguration';
import {
  useAcknowledge,
  useArchive,
  useDismiss,
} from '../hooks/useNotifications';
import { getCategoryKey, getSeverityDotColor, getSeverityKey } from '../hooks/useNotificationHelpers';
import type { DeliveryTrailEntryDto } from '../types';

// ── Severity badge ────────────────────────────────────────────────────────────

function SeverityBadge({ severity }: { severity: string }) {
  const { t } = useTranslation();
  const variantMap: Record<string, 'critical' | 'warning' | 'info' | 'success'> = {
    Critical: 'critical',
    Warning: 'warning',
    ActionRequired: 'warning',
    Info: 'info',
  };
  return (
    <Badge variant={variantMap[severity] ?? 'info'}>
      {t(getSeverityKey(severity))}
    </Badge>
  );
}

// ── Status badge ──────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
  const variantMap: Record<string, 'success' | 'info' | 'warning' | 'muted'> = {
    Read: 'success',
    Unread: 'info',
    Acknowledged: 'success',
    Archived: 'muted',
    Dismissed: 'muted',
    Snoozed: 'warning',
  };
  return <Badge variant={variantMap[status] ?? 'info'}>{status}</Badge>;
}

// ── Delivery status chip ──────────────────────────────────────────────────────

function DeliveryStatusChip({ status }: { status: string }) {
  const colorMap: Record<string, string> = {
    Delivered: 'text-success bg-success/10',
    Failed: 'text-critical bg-critical/10',
    RetryScheduled: 'text-warning bg-warning/10',
    Pending: 'text-muted bg-edge',
  };
  const cls = colorMap[status] ?? 'text-muted bg-edge';
  return (
    <span className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-medium ${cls}`}>
      {status}
    </span>
  );
}

// ── Channel icon ──────────────────────────────────────────────────────────────

function ChannelIcon({ channel }: { channel: string }) {
  if (channel === 'Email') return <Mail className="h-3.5 w-3.5" />;
  if (channel === 'MicrosoftTeams') return <MessageSquare className="h-3.5 w-3.5" />;
  return <Bell className="h-3.5 w-3.5" />;
}

// ── Delivery trail row ────────────────────────────────────────────────────────

function DeliveryRow({ entry }: { entry: DeliveryTrailEntryDto }) {
  const { t } = useTranslation();
  return (
    <tr className="border-b border-edge/50 last:border-0 hover:bg-hover/30 transition-colors">
      <td className="py-3 pr-4">
        <div className="flex items-center gap-2 text-sm text-body">
          <ChannelIcon channel={entry.channel} />
          {entry.channel}
        </div>
      </td>
      <td className="py-3 pr-4">
        <DeliveryStatusChip status={entry.status} />
      </td>
      <td className="py-3 pr-4 text-sm tabular-nums text-muted">
        {entry.retryCount}
      </td>
      <td className="py-3 pr-4 text-sm text-muted">
        {entry.deliveredAt
          ? new Date(entry.deliveredAt).toLocaleString()
          : entry.lastAttemptAt
            ? new Date(entry.lastAttemptAt).toLocaleString()
            : '—'}
      </td>
      <td className="py-3 pr-4 text-sm text-muted">
        {entry.nextRetryAt ? new Date(entry.nextRetryAt).toLocaleString() : '—'}
      </td>
      <td className="py-3 text-sm text-muted max-w-[200px] truncate" title={entry.errorMessage ?? undefined}>
        {entry.errorMessage ? (
          <span className="text-critical">{entry.errorMessage}</span>
        ) : (
          <span className="text-muted/50">—</span>
        )}
      </td>
    </tr>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

type ActionFeedback = { type: 'success' | 'error'; message: string } | null;

export function NotificationDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { notificationId } = useParams<{ notificationId: string }>();

  const [feedback, setFeedback] = useState<ActionFeedback>(null);

  const { data, isLoading, isError, refetch } = useNotificationById(notificationId ?? '', !!notificationId);
  const trailQuery = useNotificationTrail(notificationId ?? '', !!notificationId);

  const acknowledge = useAcknowledge();
  const archive = useArchive();
  const dismiss = useDismiss();

  const notification = data?.notification;
  const trail = trailQuery.data;

  const handleAcknowledge = () => {
    if (!notificationId) return;
    setFeedback(null);
    acknowledge.mutate(
      { id: notificationId },
      {
        onSuccess: () => {
          setFeedback({ type: 'success', message: t('notifications.acknowledged') });
          void refetch();
        },
        onError: () => setFeedback({ type: 'error', message: t('notifications.detail.actionFailed') }),
      },
    );
  };

  const handleArchive = () => {
    if (!notificationId) return;
    setFeedback(null);
    archive.mutate(notificationId, {
      onSuccess: () => {
        setFeedback({ type: 'success', message: t('notifications.archive') });
        void refetch();
      },
      onError: () => setFeedback({ type: 'error', message: t('notifications.detail.actionFailed') }),
    });
  };

  const handleDismiss = () => {
    if (!notificationId) return;
    setFeedback(null);
    dismiss.mutate(notificationId, {
      onSuccess: () => navigate('/notifications'),
      onError: () => setFeedback({ type: 'error', message: t('notifications.detail.actionFailed') }),
    });
  };

  const isPending = acknowledge.isPending || archive.isPending || dismiss.isPending;

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('notifications.detail.loading')} />
      </PageContainer>
    );
  }

  if (isError || !notification) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <Button variant="secondary" size="sm" onClick={() => refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      </PageContainer>
    );
  }

  const canAcknowledge =
    notification.requiresAction &&
    notification.status !== 'Acknowledged' &&
    notification.status !== 'Archived' &&
    notification.status !== 'Dismissed';

  const canArchive = notification.status !== 'Archived' && notification.status !== 'Dismissed';
  const canDismiss = notification.status !== 'Dismissed';

  return (
    <PageContainer>
      <PageHeader
        title={notification.title}
        subtitle={t(getCategoryKey(notification.category))}
        actions={
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate('/notifications')}
          >
            <ArrowLeft className="h-4 w-4 mr-1.5" />
            {t('notifications.detail.backToInbox')}
          </Button>
        }
      />

      {/* Severity / Status meta row */}
      <div className="mt-4 flex flex-wrap items-center gap-3">
        <div className={`h-2 w-2 rounded-full ${getSeverityDotColor(notification.severity)}`} />
        <SeverityBadge severity={notification.severity} />
        <StatusBadge status={notification.status} />
        {notification.isEscalated && (
          <Badge variant="critical">{t('notifications.escalated')}</Badge>
        )}
        <span className="text-sm text-muted">
          {new Date(notification.createdAt).toLocaleString()}
        </span>
      </div>

      {/* RequiresAction banner */}
      {canAcknowledge && (
        <div className="mt-4">
          <InlineMessage severity="warning" icon={<AlertTriangle className="h-4 w-4" />}>
            {t('notifications.detail.requiresActionBanner')}
          </InlineMessage>
        </div>
      )}

      {/* Action feedback */}
      {feedback?.type === 'success' && (
        <div className="mt-4">
          <InlineMessage severity="success" icon={<CheckCircle2 className="h-4 w-4" />}>
            {feedback.message}
          </InlineMessage>
        </div>
      )}
      {feedback?.type === 'error' && (
        <div className="mt-4">
          <InlineMessage severity="danger" icon={<AlertCircle className="h-4 w-4" />}>
            {feedback.message}
          </InlineMessage>
        </div>
      )}

      <div className="mt-6 grid grid-cols-1 gap-4 xl:grid-cols-3">
        {/* ── Left: Notification Details ─────────────────────────────────── */}
        <div className="xl:col-span-2 space-y-4">
          {/* Message card */}
          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{notification.title}</span>
            </CardHeader>
            <CardBody>
              <p className="text-sm text-body leading-relaxed whitespace-pre-wrap">
                {notification.message}
              </p>

              {notification.actionUrl && (
                <div className="mt-4">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => notification.actionUrl && navigate(notification.actionUrl)}
                  >
                    <ExternalLink className="h-4 w-4 mr-1.5" />
                    {t('notifications.detail.viewSourceAction')}
                  </Button>
                </div>
              )}
            </CardBody>
          </Card>

          {/* Source metadata */}
          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.detail.sourceModule')}</span>
            </CardHeader>
            <CardBody>
              <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.eventType')}</dt>
                  <dd className="mt-0.5 text-sm font-medium text-heading">{notification.eventType}</dd>
                </div>
                <div>
                  <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.sourceModule')}</dt>
                  <dd className="mt-0.5 text-sm font-medium text-heading">{notification.sourceModule}</dd>
                </div>
                {notification.sourceEntityType && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.sourceEntity')}</dt>
                    <dd className="mt-0.5 text-sm font-medium text-heading">{notification.sourceEntityType}</dd>
                  </div>
                )}
                {notification.sourceEntityId && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.sourceEntityId')}</dt>
                    <dd className="mt-0.5 text-sm font-mono text-heading truncate">{notification.sourceEntityId}</dd>
                  </div>
                )}
                {notification.sourceEventId && (
                  <div className="sm:col-span-2">
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.sourceEventId')}</dt>
                    <dd className="mt-0.5 text-sm font-mono text-heading break-all">{notification.sourceEventId}</dd>
                  </div>
                )}
                {notification.environmentId && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.environment')}</dt>
                    <dd className="mt-0.5 text-sm font-mono text-heading truncate">{notification.environmentId}</dd>
                  </div>
                )}
                {notification.occurrenceCount > 1 && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.occurrences')}</dt>
                    <dd className="mt-0.5 text-sm font-semibold text-heading">{notification.occurrenceCount}</dd>
                  </div>
                )}
              </dl>
            </CardBody>
          </Card>

          {/* Delivery trail */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <span className="font-semibold text-heading">{t('notifications.detail.deliveryTrail')}</span>
                {trailQuery.isLoading && (
                  <RefreshCw className="h-4 w-4 animate-spin text-muted" />
                )}
              </div>
            </CardHeader>
            <CardBody>
              {trailQuery.isError && (
                <p className="text-sm text-muted">{t('notifications.detail.noDeliveries')}</p>
              )}
              {!trailQuery.isLoading && !trailQuery.isError && (
                trail && trail.deliveries.length > 0 ? (
                  <div className="overflow-x-auto">
                    <table className="w-full">
                      <thead>
                        <tr className="border-b border-edge">
                          <th className="pb-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-muted">
                            {t('notifications.detail.deliveryChannel')}
                          </th>
                          <th className="pb-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-muted">
                            {t('notifications.detail.deliveryStatus')}
                          </th>
                          <th className="pb-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-muted">
                            {t('notifications.detail.retries')}
                          </th>
                          <th className="pb-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-muted">
                            {t('notifications.detail.deliveredAt')}
                          </th>
                          <th className="pb-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-muted">
                            {t('notifications.detail.nextRetry')}
                          </th>
                          <th className="pb-2 text-left text-xs font-medium uppercase tracking-wide text-muted">
                            {t('notifications.detail.errorMessage')}
                          </th>
                        </tr>
                      </thead>
                      <tbody>
                        {trail.deliveries.map((entry) => (
                          <DeliveryRow key={entry.deliveryId} entry={entry} />
                        ))}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <p className="text-sm text-muted">{t('notifications.detail.noDeliveries')}</p>
                )
              )}
            </CardBody>
          </Card>
        </div>

        {/* ── Right: Actions + Status timeline ──────────────────────────── */}
        <div className="space-y-4">
          {/* Actions */}
          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.detail.actions')}</span>
            </CardHeader>
            <CardBody>
              <div className="space-y-2">
                {canAcknowledge && (
                  <Button
                    variant="primary"
                    size="sm"
                    className="w-full"
                    onClick={handleAcknowledge}
                    disabled={isPending}
                  >
                    <CheckCheck className="h-4 w-4 mr-1.5" />
                    {t('notifications.acknowledge')}
                  </Button>
                )}
                {canArchive && (
                  <Button
                    variant="secondary"
                    size="sm"
                    className="w-full"
                    onClick={handleArchive}
                    disabled={isPending}
                  >
                    <Archive className="h-4 w-4 mr-1.5" />
                    {t('notifications.archive')}
                  </Button>
                )}
                {canDismiss && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="w-full"
                    onClick={handleDismiss}
                    disabled={isPending}
                  >
                    <X className="h-4 w-4 mr-1.5" />
                    {t('notifications.dismiss')}
                  </Button>
                )}
              </div>
            </CardBody>
          </Card>

          {/* Status timeline */}
          <Card>
            <CardHeader>
              <span className="font-semibold text-heading">{t('notifications.detail.deliveries')}</span>
            </CardHeader>
            <CardBody>
              <dl className="space-y-3">
                <div>
                  <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.readAt')}</dt>
                  <dd className="mt-0.5 text-sm text-heading">
                    {notification.readAt
                      ? new Date(notification.readAt).toLocaleString()
                      : <span className="text-muted">—</span>}
                  </dd>
                </div>
                {notification.acknowledgedAt && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.acknowledgedAt')}</dt>
                    <dd className="mt-0.5 text-sm text-heading">
                      {new Date(notification.acknowledgedAt).toLocaleString()}
                    </dd>
                  </div>
                )}
                {notification.archivedAt && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.archivedAt')}</dt>
                    <dd className="mt-0.5 text-sm text-heading">
                      {new Date(notification.archivedAt).toLocaleString()}
                    </dd>
                  </div>
                )}
                {notification.snoozedUntil && (
                  <div>
                    <dt className="text-xs text-muted uppercase tracking-wide">{t('notifications.detail.snoozedUntil')}</dt>
                    <dd className="mt-0.5 flex items-center gap-1.5 text-sm text-heading">
                      <Clock className="h-3.5 w-3.5 text-warning" />
                      {new Date(notification.snoozedUntil).toLocaleString()}
                    </dd>
                  </div>
                )}
              </dl>
            </CardBody>
          </Card>
        </div>
      </div>
    </PageContainer>
  );
}
