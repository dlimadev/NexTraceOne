/**
 * DashboardHistoryDrawer — gaveta lateral com o histórico de revisões de um dashboard.
 * V3.1 — Dashboard Intelligence Foundation.
 * Permite ver e reverter para revisões anteriores.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { X, RotateCcw, Clock, User, FileText, ChevronRight, AlertTriangle } from 'lucide-react';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import client from '../../../api/client';
import { formatDistanceToNow } from '../../../utils/dateUtils';

// ── Types ──────────────────────────────────────────────────────────────────

interface RevisionDto {
  revisionNumber: number;
  name: string;
  description?: string | null;
  layout: string;
  authorUserId: string;
  changeNote?: string | null;
  createdAt: string;
  widgetCount: number;
}

interface DashboardHistoryResponse {
  dashboardId: string;
  totalRevisions: number;
  revisions: RevisionDto[];
}

interface RevertResponse {
  dashboardId: string;
  revertedFromRevision: number;
  newRevisionNumber: number;
  message: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────

function useDashboardHistory(dashboardId: string, tenantId: string, enabled: boolean) {
  return useQuery({
    queryKey: ['dashboard-history', dashboardId, tenantId],
    queryFn: () =>
      client
        .get<DashboardHistoryResponse>(`/governance/dashboards/${dashboardId}/history`, {
          params: { tenantId, maxResults: 50 },
        })
        .then((r) => r.data),
    enabled: enabled && Boolean(dashboardId),
  });
}

function useRevertDashboard(dashboardId: string, tenantId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: { targetRevisionNumber: number; revertNote?: string }) =>
      client
        .post<RevertResponse>(`/governance/dashboards/${dashboardId}/revert`, {
          dashboardId,
          tenantId,
          userId: 'current-user',
          ...payload,
        })
        .then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['dashboard-render-data', dashboardId] });
      qc.invalidateQueries({ queryKey: ['dashboard-history', dashboardId] });
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
    },
  });
}

// ── Component ──────────────────────────────────────────────────────────────

interface DashboardHistoryDrawerProps {
  dashboardId: string;
  tenantId: string;
  currentRevisionNumber: number;
  isOpen: boolean;
  onClose: () => void;
}

export function DashboardHistoryDrawer({
  dashboardId,
  tenantId,
  currentRevisionNumber,
  isOpen,
  onClose,
}: DashboardHistoryDrawerProps) {
  const { t } = useTranslation();
  const [confirmRevertNumber, setConfirmRevertNumber] = useState<number | null>(null);
  const [revertSuccess, setRevertSuccess] = useState<string | null>(null);

  const { data, isLoading } = useDashboardHistory(dashboardId, tenantId, isOpen);
  const revertMutation = useRevertDashboard(dashboardId, tenantId);

  const handleRevert = async (revisionNumber: number) => {
    const result = await revertMutation.mutateAsync({
      targetRevisionNumber: revisionNumber,
      revertNote: t('dashboardHistory.revert', 'Reverted to this revision'),
    });
    setConfirmRevertNumber(null);
    setRevertSuccess(
      t('dashboardHistory.revertSuccess', 'Dashboard reverted to revision #{{number}}', {
        number: revisionNumber,
      })
    );
    setTimeout(() => setRevertSuccess(null), 4000);
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Overlay */}
      <div
        className="fixed inset-0 bg-black/40 z-40"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Drawer */}
      <aside
        className="fixed right-0 top-0 h-full w-96 bg-white dark:bg-gray-900 shadow-2xl z-50 flex flex-col"
        role="complementary"
        aria-label={t('dashboardHistory.title', 'Dashboard History')}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-2">
            <Clock size={18} className="text-accent" />
            <h2 className="font-semibold text-gray-900 dark:text-white">
              {t('dashboardHistory.title', 'Dashboard History')}
            </h2>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} aria-label={t('dashboardHistory.close', 'Close')}>
            <X size={16} />
          </Button>
        </div>

        {/* Subtitle + count */}
        {data && (
          <div className="px-5 py-2 bg-gray-50 dark:bg-gray-800 border-b border-gray-100 dark:border-gray-700">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('dashboardHistory.subtitle', 'Revision history for this dashboard')}
              {' · '}
              <span className="font-medium">{data.totalRevisions}</span>
              {' '}
              {t('dashboardRevision.autoSaveNote', 'revisions')}
            </p>
          </div>
        )}

        {/* Success banner */}
        {revertSuccess && (
          <div className="mx-4 mt-3 px-4 py-2 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 text-sm text-green-700 dark:text-green-300">
            {revertSuccess}
          </div>
        )}

        {/* List */}
        <div className="flex-1 overflow-y-auto px-4 py-3 space-y-2">
          {isLoading && (
            <div className="space-y-3 animate-pulse">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-20 bg-gray-100 dark:bg-gray-800 rounded-lg" />
              ))}
            </div>
          )}

          {!isLoading && data?.revisions.length === 0 && (
            <div className="text-center py-10 text-gray-400 dark:text-gray-500">
              <Clock size={32} className="mx-auto mb-2 opacity-40" />
              <p className="text-sm font-medium">
                {t('dashboardHistory.noHistory', 'No revision history yet')}
              </p>
              <p className="text-xs mt-1">
                {t('dashboardHistory.noHistorySubtitle', 'Revisions are recorded automatically on every save')}
              </p>
            </div>
          )}

          {data?.revisions.map((rev) => {
            const isCurrent = rev.revisionNumber === currentRevisionNumber;
            const isConfirming = confirmRevertNumber === rev.revisionNumber;

            return (
              <div
                key={rev.revisionNumber}
                className={`rounded-lg border px-4 py-3 transition-colors ${
                  isCurrent
                    ? 'border-accent/40 bg-accent/5'
                    : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-gray-300 dark:hover:border-gray-600'
                }`}
              >
                {/* Row 1: number + badge + timestamp */}
                <div className="flex items-center justify-between gap-2 mb-1">
                  <span className="text-sm font-semibold text-gray-800 dark:text-gray-100">
                    {t('dashboardHistory.revisionNumber', 'Revision #{{number}}', {
                      number: rev.revisionNumber,
                    })}
                  </span>
                  <div className="flex items-center gap-1.5">
                    {isCurrent && (
                      <Badge variant="success" size="xs">
                        current
                      </Badge>
                    )}
                    <span className="text-xs text-gray-400 dark:text-gray-500 whitespace-nowrap">
                      {formatDistanceToNow(new Date(rev.createdAt))}
                    </span>
                  </div>
                </div>

                {/* Row 2: name + widget count */}
                <p className="text-xs text-gray-600 dark:text-gray-300 truncate mb-1">
                  {rev.name}
                  <span className="ml-2 text-gray-400 dark:text-gray-500">
                    · {t('dashboardHistory.widgetCount', '{{count}} widget(s)', { count: rev.widgetCount })}
                  </span>
                </p>

                {/* Row 3: author + change note */}
                <div className="flex items-center gap-3 text-xs text-gray-400 dark:text-gray-500 mb-2">
                  <span className="flex items-center gap-1">
                    <User size={11} />
                    {t('dashboardHistory.author', 'by {{user}}', { user: rev.authorUserId })}
                  </span>
                  {rev.changeNote && (
                    <span className="flex items-center gap-1 truncate max-w-[140px]">
                      <FileText size={11} />
                      {rev.changeNote}
                    </span>
                  )}
                </div>

                {/* Revert action */}
                {!isCurrent && (
                  <>
                    {isConfirming ? (
                      <div className="space-y-2">
                        <div className="flex items-center gap-1.5 text-xs text-amber-600 dark:text-amber-400">
                          <AlertTriangle size={12} />
                          {t('dashboardHistory.revertConfirmMessage', 'Revert to revision #{{number}}?', {
                            number: rev.revisionNumber,
                          })}
                        </div>
                        <div className="flex gap-2">
                          <Button
                            size="xs"
                            variant="danger"
                            onClick={() => handleRevert(rev.revisionNumber)}
                            loading={revertMutation.isPending}
                          >
                            {t('dashboardHistory.revert', 'Revert')}
                          </Button>
                          <Button
                            size="xs"
                            variant="ghost"
                            onClick={() => setConfirmRevertNumber(null)}
                          >
                            {t('common.cancel', 'Cancel')}
                          </Button>
                        </div>
                      </div>
                    ) : (
                      <button
                        className="flex items-center gap-1 text-xs text-accent hover:underline"
                        onClick={() => setConfirmRevertNumber(rev.revisionNumber)}
                      >
                        <RotateCcw size={11} />
                        {t('dashboardHistory.revert', 'Revert to this revision')}
                        <ChevronRight size={11} />
                      </button>
                    )}
                  </>
                )}
              </div>
            );
          })}
        </div>

        {/* Footer */}
        <div className="px-5 py-3 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
          <p className="text-xs text-gray-400 dark:text-gray-500">
            {t('dashboardRevision.autoSaveNote', 'Revisions are saved automatically on every update')}
          </p>
        </div>
      </aside>
    </>
  );
}
