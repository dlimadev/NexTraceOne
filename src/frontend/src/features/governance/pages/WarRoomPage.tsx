import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Users, MessageSquare, CheckCircle2, AlertTriangle, Plus, Send, X } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

interface PresenceUser {
  userId: string;
  displayName: string;
  avatarColor: string;
  lastSeenAt: string;
}

interface DashboardComment {
  id: string;
  dashboardId: string;
  widgetId?: string;
  authorUserId: string;
  content: string;
  parentCommentId?: string;
  isResolved: boolean;
  resolvedByUserId?: string;
  createdAt: string;
  editedAt?: string;
}

// ── API hooks ──────────────────────────────────────────────────────────────────

const usePresence = (resourceType: string, resourceId: string, tenantId: string) =>
  useQuery({
    queryKey: ['presence', resourceType, resourceId],
    queryFn: () =>
      client
        .get<{ activeUsers: PresenceUser[]; count: number }>('/api/v1/governance/collaboration/presence', {
          params: { resourceType, resourceId, tenantId },
        })
        .then((r) => r.data),
    refetchInterval: 10_000,
  });

const useComments = (dashboardId: string, tenantId: string, includeResolved: boolean) =>
  useQuery({
    queryKey: ['dashboard-comments', dashboardId, includeResolved],
    queryFn: () =>
      client
        .get<{ items: DashboardComment[]; count: number }>('/api/v1/governance/collaboration/comments', {
          params: { dashboardId, tenantId, includeResolved },
        })
        .then((r) => r.data),
  });

const useAddComment = (dashboardId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { content: string; widgetId?: string; tenantId: string; authorUserId: string }) =>
      client.post('/api/v1/governance/collaboration/comments', { dashboardId, ...data }).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dashboard-comments', dashboardId] }),
  });
};

const useResolveComment = (dashboardId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ commentId, tenantId, userId }: { commentId: string; tenantId: string; userId: string }) =>
      client.post(`/api/v1/governance/collaboration/comments/${commentId}/resolve`, null, {
        params: { tenantId, userId },
      }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dashboard-comments', dashboardId] }),
  });
};

// ── Presence Bar ───────────────────────────────────────────────────────────────

function PresenceBar({ users }: { users: PresenceUser[] }) {
  const { t } = useTranslation();
  if (!users.length) return null;

  return (
    <div className="flex items-center gap-2 px-4 py-2 bg-surface-elevated rounded-lg border border-border">
      <Users size={14} className="text-muted-foreground" />
      <span className="text-xs text-muted-foreground">{t('warRoom.activeNow', { count: users.length })}</span>
      <div className="flex -space-x-2">
        {users.slice(0, 8).map((u) => (
          <div
            key={u.userId}
            title={u.displayName}
            className="w-7 h-7 rounded-full border-2 border-background flex items-center justify-center text-xs font-semibold text-white"
            style={{ backgroundColor: u.avatarColor }}
          >
            {u.displayName[0]?.toUpperCase()}
          </div>
        ))}
        {users.length > 8 && (
          <div className="w-7 h-7 rounded-full border-2 border-background bg-muted flex items-center justify-center text-xs text-muted-foreground">
            +{users.length - 8}
          </div>
        )}
      </div>
    </div>
  );
}

// ── Comment Thread ─────────────────────────────────────────────────────────────

function CommentItem({
  comment,
  onResolve,
}: {
  comment: DashboardComment;
  onResolve: (id: string) => void;
}) {
  const { t } = useTranslation();
  return (
    <div className={`p-3 rounded-lg border ${comment.isResolved ? 'opacity-60 bg-muted/30' : 'bg-surface-elevated'}`}>
      <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          <div className="w-6 h-6 rounded-full bg-accent flex items-center justify-center text-xs font-semibold text-white shrink-0">
            {comment.authorUserId[0]?.toUpperCase()}
          </div>
          <span className="text-xs font-medium truncate">{comment.authorUserId}</span>
          {comment.widgetId && (
            <Badge variant="secondary" className="text-xs">
              {t('warRoom.anchoredWidget', { widget: comment.widgetId })}
            </Badge>
          )}
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {comment.isResolved ? (
            <Badge variant="success" className="text-xs">
              <CheckCircle2 size={10} className="mr-1" />
              {t('warRoom.resolved')}
            </Badge>
          ) : (
            <Button size="sm" variant="ghost" onClick={() => onResolve(comment.id)}>
              <CheckCircle2 size={12} className="mr-1" />
              {t('warRoom.resolve')}
            </Button>
          )}
        </div>
      </div>
      <p className="mt-2 text-sm text-foreground">{comment.content}</p>
      <p className="mt-1 text-xs text-muted-foreground">
        {new Date(comment.createdAt).toLocaleString()}
        {comment.editedAt && ` · ${t('warRoom.edited')}`}
      </p>
    </div>
  );
}

// ── Main Page ──────────────────────────────────────────────────────────────────

export function WarRoomPage() {
  const { t } = useTranslation();
  const { dashboardId = '' } = useParams<{ dashboardId: string }>();
  const tenantId = 'default';
  const userId = 'current-user';

  const [showResolved, setShowResolved] = useState(false);
  const [newComment, setNewComment] = useState('');

  const presenceQuery = usePresence('dashboard', dashboardId, tenantId);
  const commentsQuery = useComments(dashboardId, tenantId, showResolved);
  const addComment = useAddComment(dashboardId);
  const resolveComment = useResolveComment(dashboardId);

  const handleSend = () => {
    if (!newComment.trim()) return;
    addComment.mutate({ content: newComment.trim(), tenantId, authorUserId: userId });
    setNewComment('');
  };

  const openComments = (commentsQuery.data?.items ?? []).filter((c) => !c.isResolved);
  const resolvedComments = (commentsQuery.data?.items ?? []).filter((c) => c.isResolved);

  return (
    <PageContainer>
      <PageHeader
        title={t('warRoom.title')}
        subtitle={t('warRoom.subtitle')}
        actions={
          <PresenceBar users={presenceQuery.data?.activeUsers ?? []} />
        }
      />

      <PageSection>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* War Room Status */}
          <div className="lg:col-span-2 space-y-4">
            <Card>
              <CardBody>
                <div className="flex items-center gap-2 mb-4">
                  <AlertTriangle size={16} className="text-warning" />
                  <h3 className="text-sm font-semibold">{t('warRoom.activeContext')}</h3>
                  <Badge variant="warning">{t('warRoom.live')}</Badge>
                </div>
                <p className="text-sm text-muted-foreground">
                  {t('warRoom.contextHint')}
                </p>
                <div className="mt-4 p-3 rounded bg-muted/40 text-xs text-muted-foreground">
                  {t('warRoom.simulatedBanner')}
                </div>
              </CardBody>
            </Card>

            {/* Open Comments */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-semibold flex items-center gap-2">
                  <MessageSquare size={14} />
                  {t('warRoom.openThreads', { count: openComments.length })}
                </h3>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => setShowResolved((v) => !v)}
                >
                  {showResolved ? t('warRoom.hideResolved') : t('warRoom.showResolved')}
                </Button>
              </div>

              {commentsQuery.isLoading ? (
                <PageLoadingState />
              ) : openComments.length === 0 ? (
                <EmptyState
                  icon={<MessageSquare size={24} />}
                  title={t('warRoom.noComments')}
                  description={t('warRoom.noCommentsHint')}
                />
              ) : (
                <div className="space-y-2">
                  {openComments.map((c) => (
                    <CommentItem
                      key={c.id}
                      comment={c}
                      onResolve={(id) => resolveComment.mutate({ commentId: id, tenantId, userId })}
                    />
                  ))}
                </div>
              )}

              {showResolved && resolvedComments.length > 0 && (
                <div className="mt-4 space-y-2">
                  <h4 className="text-xs font-medium text-muted-foreground">{t('warRoom.resolvedThreads', { count: resolvedComments.length })}</h4>
                  {resolvedComments.map((c) => (
                    <CommentItem
                      key={c.id}
                      comment={c}
                      onResolve={(id) => resolveComment.mutate({ commentId: id, tenantId, userId })}
                    />
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Add Comment Panel */}
          <div className="space-y-4">
            <Card>
              <CardBody>
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <Plus size={14} />
                  {t('warRoom.addComment')}
                </h3>
                <textarea
                  className="w-full rounded border border-border bg-background text-sm p-2 resize-none focus:outline-none focus:ring-1 focus:ring-accent"
                  rows={4}
                  placeholder={t('warRoom.commentPlaceholder')}
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                />
                <div className="flex justify-end mt-2">
                  <Button
                    size="sm"
                    onClick={handleSend}
                    disabled={!newComment.trim() || addComment.isPending}
                  >
                    <Send size={12} className="mr-1" />
                    {t('warRoom.send')}
                  </Button>
                </div>
              </CardBody>
            </Card>

            {/* Participants */}
            <Card>
              <CardBody>
                <h3 className="text-sm font-semibold mb-3 flex items-center gap-2">
                  <Users size={14} />
                  {t('warRoom.participants', { count: presenceQuery.data?.count ?? 0 })}
                </h3>
                {presenceQuery.isLoading ? (
                  <PageLoadingState />
                ) : (
                  <div className="space-y-2">
                    {(presenceQuery.data?.activeUsers ?? []).map((u) => (
                      <div key={u.userId} className="flex items-center gap-2 text-sm">
                        <div
                          className="w-6 h-6 rounded-full flex items-center justify-center text-xs font-semibold text-white"
                          style={{ backgroundColor: u.avatarColor }}
                        >
                          {u.displayName[0]?.toUpperCase()}
                        </div>
                        <span className="truncate">{u.displayName}</span>
                        <div className="w-2 h-2 rounded-full bg-success ml-auto" />
                      </div>
                    ))}
                    {(presenceQuery.data?.count ?? 0) === 0 && (
                      <p className="text-xs text-muted-foreground">{t('warRoom.noParticipants')}</p>
                    )}
                  </div>
                )}
              </CardBody>
            </Card>
          </div>
        </div>
      </PageSection>
    </PageContainer>
  );
}
