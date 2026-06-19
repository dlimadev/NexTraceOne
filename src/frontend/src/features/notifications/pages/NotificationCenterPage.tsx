import React, { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  LineChart,
  Bell,
  CheckCheck,
  Eye,
  EyeOff,
  ExternalLink,
  Settings,
  CheckCircle2,
  Archive,
  X,
  Clock,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell/PageContainer';
import { PageHeader } from '../../../components/PageHeader';
import { EmptyState } from '../../../components/EmptyState';
import { ErrorState } from '../../../components/ErrorState';
import { FilterChip } from '../../../components/FilterChip';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { IconButton } from '../../../components/IconButton';
import {
  useNotificationList,
  useMarkAsRead,
  useMarkAsUnread,
  useMarkAllAsRead,
  useAcknowledge,
  useArchive,
  useDismiss,
  useSnooze,
} from '../hooks/useNotifications';
import {
  getCategoryKey,
  getSeverityDotColor,
  getSeverityKey,
  isUnread,
  useTimeAgo,
} from '../hooks/useNotificationHelpers';
import type { NotificationListParams } from '../types';

type StatusFilter = 'all' | 'Unread' | 'Read';
type SeverityFilter = 'all' | 'Critical' | 'Warning' | 'ActionRequired' | 'Info';

export function NotificationCenterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const timeAgo = useTimeAgo();

  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [severityFilter, setSeverityFilter] = useState<SeverityFilter>('all');
  const [page, setPage] = useState(1);

  const params: NotificationListParams = {
    page,
    pageSize: 20,
    ...(statusFilter !== 'all' && { status: statusFilter }),
    ...(severityFilter !== 'all' && { severity: severityFilter }),
  };

  const { data, isLoading, isError, refetch } = useNotificationList(params);
  const markAsRead = useMarkAsRead();
  const markAsUnread = useMarkAsUnread();
  const markAllAsRead = useMarkAllAsRead();
  const acknowledge = useAcknowledge();
  const archive = useArchive();
  const dismiss = useDismiss();
  const snooze = useSnooze();

  const notifications = data?.items ?? [];
  const hasMore = data?.hasMore ?? false;

  const handleStatusFilter = (filter: StatusFilter) => {
    setStatusFilter(filter);
    setPage(1);
  };

  const handleSeverityFilter = (filter: SeverityFilter) => {
    setSeverityFilter(filter);
    setPage(1);
  };

  const handleSnooze1h = useCallback((id: string) => {
    const until = new Date(Date.now() + 60 * 60 * 1000).toISOString();
    snooze.mutate({ id, snoozedUntil: until });
  }, [snooze]);

  return (
    <PageContainer>
      <PageHeader
        title={t('notifications.title')}
        subtitle={t('notifications.subtitle')}
        actions={
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/notifications/preferences')}
            >
              <Settings className="h-4 w-4 mr-1.5" />
              {t('notifications.preferences.title')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate('/notifications/analytics')}
            >
              <LineChart className="h-4 w-4 mr-1.5" />
              {t('notifications.analytics.title')}
            </Button>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => markAllAsRead.mutate()}
              loading={markAllAsRead.isPending}
            >
              <CheckCheck className="h-4 w-4 mr-1.5" />
              {t('notifications.markAllRead')}
            </Button>
          </div>
        }
      />

      {/* Filters */}
      <div className="mt-6 flex flex-wrap items-center gap-2">
        {/* Status filters */}
        <FilterChip
          label={t('notifications.filter.all')}
          active={statusFilter === 'all'}
          onClick={() => handleStatusFilter('all')}
        />
        <FilterChip
          label={t('notifications.filter.unread')}
          active={statusFilter === 'Unread'}
          onClick={() => handleStatusFilter('Unread')}
        />
        <FilterChip
          label={t('notifications.filter.read')}
          active={statusFilter === 'Read'}
          onClick={() => handleStatusFilter('Read')}
        />

        <span className="mx-1 h-5 w-px bg-edge" aria-hidden="true" />

        {/* Severity filters */}
        <FilterChip
          label={t('notifications.severity.critical')}
          active={severityFilter === 'Critical'}
          onClick={() =>
            handleSeverityFilter(
              severityFilter === 'Critical' ? 'all' : 'Critical',
            )
          }
        />
        <FilterChip
          label={t('notifications.severity.warning')}
          active={severityFilter === 'Warning'}
          onClick={() =>
            handleSeverityFilter(
              severityFilter === 'Warning' ? 'all' : 'Warning',
            )
          }
        />
        <FilterChip
          label={t('notifications.severity.actionRequired')}
          active={severityFilter === 'ActionRequired'}
          onClick={() =>
            handleSeverityFilter(
              severityFilter === 'ActionRequired' ? 'all' : 'ActionRequired',
            )
          }
        />
        <FilterChip
          label={t('notifications.severity.info')}
          active={severityFilter === 'Info'}
          onClick={() =>
            handleSeverityFilter(
              severityFilter === 'Info' ? 'all' : 'Info',
            )
          }
        />
      </div>

      {/* Content */}
      <div className="mt-6 space-y-2">
        {/* Spinner de carregamento com token semântico */}
        {isLoading && (
          <div className="flex items-center justify-center py-16">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-edge border-t-accent" />
          </div>
        )}

        {/* Error */}
        {isError && (
          <ErrorState
            title={t('notifications.errorLoading')}
            action={
              <Button variant="secondary" size="sm" onClick={() => refetch()}>
                {t('common.retry', 'Retry')}
              </Button>
            }
          />
        )}

        {/* Empty */}
        {!isLoading && !isError && notifications.length === 0 && (
          <EmptyState
            icon={<Bell className="h-10 w-10" />}
            title={t('notifications.noNotifications')}
            description={t('notifications.noNotificationsDescription')}
          />
        )}

        {/* Notification list */}
        {!isLoading &&
          !isError &&
          notifications.map((n) => {
            const unread = isUnread(n);
            const isAcknowledged = n.status === 'Acknowledged';
            const isArchived = n.status === 'Archived';
            const isDismissed = n.status === 'Dismissed';
            const isSnoozed = n.snoozedUntil != null && new Date(n.snoozedUntil) > new Date();
            const isClosed = isArchived || isDismissed;
            const isEscalated = n.isEscalated;

            return (
              <div
                key={n.id}
                className={`flex items-start gap-4 rounded-lg border p-4 transition-all hover:border-edge-strong ${
                  isClosed
                    ? 'opacity-60 bg-card border-edge/30'
                    : unread
                    ? 'bg-elevated border-edge shadow-surface'
                    : 'bg-card border-edge/50'
                }`}
              >
                {/* Severity dot with escalation indicator */}
                <span
                  className={`mt-1 h-2.5 w-2.5 shrink-0 rounded-full ${getSeverityDotColor(n.severity)} ${
                    isEscalated ? 'ring-2 ring-red-500/60' : ''
                  }`}
                  aria-hidden="true"
                />

                {/* Content */}
                <div className="min-w-0 flex-1">
                  <div className="flex items-start justify-between gap-3">
                    <p
                      className={`text-sm leading-snug ${
                        unread ? 'font-semibold text-heading' : 'text-body'
                      }`}
                    >
                      {n.title}
                      {/* Badge DS substitui spans artesanais com cores hardcoded */}
                      {isEscalated && (
                        <Badge variant="danger" className="ml-2 uppercase tracking-wide text-[10px]">
                          {t('notifications.escalated')}
                        </Badge>
                      )}
                      {isSnoozed && (
                        <Badge variant="warning" className="ml-2 uppercase tracking-wide text-[10px]">
                          {t('notifications.snoozed')}
                        </Badge>
                      )}
                      {n.occurrenceCount > 1 && (
                        <span className="ml-2 inline-flex items-center rounded bg-panel px-1.5 py-0.5 text-[10px] font-medium text-muted border border-edge">
                          ×{n.occurrenceCount}
                        </span>
                      )}
                    </p>
                    <span className="shrink-0 text-xs text-muted">
                      {timeAgo(n.createdAt)}
                    </span>
                  </div>

                  <p className="mt-1 text-xs text-muted line-clamp-2">
                    {n.message}
                  </p>

                  {/* Tags */}
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <span className="inline-flex items-center rounded bg-panel px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide text-muted border border-edge">
                      {t(getCategoryKey(n.category))}
                    </span>
                    <span className="inline-flex items-center rounded bg-panel px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide text-muted border border-edge">
                      {t(getSeverityKey(n.severity))}
                    </span>
                    {n.sourceModule && (
                      <span className="text-[10px] text-muted/60">
                        {n.sourceModule}
                      </span>
                    )}
                    {isAcknowledged && (
                      <span className="text-[10px] text-success">
                        {t('notifications.acknowledged')}
                      </span>
                    )}
                  </div>
                </div>

                {/* Ações de notificação — raw <button> substituídos por IconButton DS */}
                <div className="flex shrink-0 items-center gap-1">
                  {n.actionUrl && !isClosed && (
                    <IconButton
                      size="sm"
                      icon={<ExternalLink className="h-3.5 w-3.5" />}
                      label={t('common.open', 'Open')}
                      onClick={() => navigate(n.actionUrl!)}
                    />
                  )}

                  {/* Confirmar — apenas para não-lidas/lidas com ActionRequired */}
                  {!isClosed && !isAcknowledged && n.requiresAction && (
                    <IconButton
                      size="sm"
                      icon={<CheckCircle2 className="h-3.5 w-3.5" />}
                      label={t('notifications.acknowledge')}
                      onClick={() => acknowledge.mutate({ id: n.id })}
                      disabled={acknowledge.isPending}
                    />
                  )}

                  {/* Snooze 1h */}
                  {!isClosed && !isSnoozed && (
                    <IconButton
                      size="sm"
                      icon={<Clock className="h-3.5 w-3.5" />}
                      label={t('notifications.snooze1h')}
                      onClick={() => handleSnooze1h(n.id)}
                      disabled={snooze.isPending}
                    />
                  )}

                  {/* Arquivar */}
                  {!isClosed && !isDismissed && (
                    <IconButton
                      size="sm"
                      icon={<Archive className="h-3.5 w-3.5" />}
                      label={t('notifications.archive')}
                      onClick={() => archive.mutate(n.id)}
                      disabled={archive.isPending}
                    />
                  )}

                  {/* Descartar — apenas não-lidas/lidas */}
                  {(unread || n.status === 'Read') && (
                    <IconButton
                      size="sm"
                      icon={<X className="h-3.5 w-3.5" />}
                      label={t('notifications.dismiss')}
                      onClick={() => dismiss.mutate(n.id)}
                      disabled={dismiss.isPending}
                    />
                  )}

                  {/* Alternar lido/não-lido */}
                  {unread && !isClosed ? (
                    <IconButton
                      size="sm"
                      icon={<Eye className="h-3.5 w-3.5" />}
                      label={t('notifications.markRead')}
                      onClick={() => markAsRead.mutate(n.id)}
                      disabled={markAsRead.isPending}
                    />
                  ) : !isClosed && !isAcknowledged ? (
                    <IconButton
                      size="sm"
                      icon={<EyeOff className="h-3.5 w-3.5" />}
                      label={t('notifications.markUnread')}
                      onClick={() => markAsUnread.mutate(n.id)}
                      disabled={markAsUnread.isPending}
                    />
                  ) : null}
                </div>
              </div>
            );
          })}

        {/* Load more */}
        {hasMore && !isLoading && (
          <div className="flex justify-center pt-4">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setPage((p) => p + 1)}
            >
              {t('notifications.loadMore')}
            </Button>
          </div>
        )}
      </div>
    </PageContainer>
  );
}

