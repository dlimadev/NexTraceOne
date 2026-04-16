import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Save,
  Play,
  RefreshCw,
  CheckCircle2,
  XCircle,
  Clock,
  HardDrive,
  AlertTriangle,
  Loader2,
  DatabaseBackup,
  Timer,
} from 'lucide-react';
import { platformAdminApi } from '../api/platformAdmin';
import type { BackupRecord, BackupScheduleConfig, BackupStatus } from '../api/platformAdmin';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function statusIcon(status: BackupStatus) {
  switch (status) {
    case 'Success':
      return <CheckCircle2 size={14} className="text-success shrink-0" />;
    case 'Failed':
      return <XCircle size={14} className="text-critical shrink-0" />;
    case 'InProgress':
      return <Loader2 size={14} className="text-accent animate-spin shrink-0" />;
    case 'Scheduled':
      return <Clock size={14} className="text-muted shrink-0" />;
  }
}

function statusBadgeClass(status: BackupStatus): string {
  switch (status) {
    case 'Success':
      return 'bg-success/10 text-success';
    case 'Failed':
      return 'bg-critical/10 text-critical';
    case 'InProgress':
      return 'bg-accent/10 text-accent';
    case 'Scheduled':
      return 'bg-muted/10 text-muted';
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatSizeGb(gb: number | null): string {
  if (gb === null) return '—';
  if (gb < 0.1) return `${Math.round(gb * 1000)} MB`;
  return `${gb.toFixed(2)} GB`;
}

// ─── Backup row ───────────────────────────────────────────────────────────────

function BackupRow({ backup }: { backup: BackupRecord }) {
  const { t } = useTranslation();
  return (
    <div className="flex items-center gap-3 py-3 border-b border-border last:border-0">
      {statusIcon(backup.status)}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="text-sm font-medium text-heading">
            {formatDate(backup.startedAt)}
          </span>
          <span className={`text-xs px-1.5 py-0.5 rounded font-medium ${statusBadgeClass(backup.status)}`}>
            {t(`backup.status.${backup.status.toLowerCase()}`)}
          </span>
        </div>
        <div className="flex items-center gap-3 mt-0.5 text-xs text-muted flex-wrap">
          <span className="flex items-center gap-1">
            <HardDrive size={11} />
            {formatSizeGb(backup.fileSizeGb)}
          </span>
          {backup.durationSeconds !== null && (
            <span className="flex items-center gap-1">
              <Timer size={11} />
              {t('backup.duration', { seconds: backup.durationSeconds })}
            </span>
          )}
          <span className="truncate max-w-xs">{backup.destination}</span>
        </div>
        {backup.errorMessage && (
          <p className="text-xs text-critical mt-1">{backup.errorMessage}</p>
        )}
      </div>
    </div>
  );
}

// ─── Schedule form ────────────────────────────────────────────────────────────

function ScheduleForm({ initial }: { initial: BackupScheduleConfig }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<BackupScheduleConfig>(initial);

  const saveMutation = useMutation({
    mutationFn: platformAdminApi.updateBackupSchedule,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['backup-status'] }),
  });

  const field = (key: keyof BackupScheduleConfig) => ({
    value: String(form[key] ?? ''),
    onChange: (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((prev) => ({
        ...prev,
        [key]: e.target.type === 'checkbox' ? e.target.checked : e.target.type === 'number' ? Number(e.target.value) : e.target.value,
      })),
  });

  return (
    <div className="bg-card border border-border rounded-xl p-5 space-y-4">
      <h2 className="text-base font-semibold text-heading">{t('backup.scheduleTitle')}</h2>

      <div className="flex items-center gap-3">
        <input
          type="checkbox"
          id="backup-enabled"
          checked={form.enabled}
          onChange={(e) => setForm((p) => ({ ...p, enabled: e.target.checked }))}
          className="w-4 h-4 accent-accent"
        />
        <label htmlFor="backup-enabled" className="text-sm text-heading">
          {t('backup.enabledLabel')}
        </label>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div>
          <label className="block text-xs text-muted mb-1">{t('backup.cronLabel')}</label>
          <input
            type="text"
            className="w-full px-3 py-2 text-sm bg-surface border border-border rounded-lg text-heading placeholder:text-muted focus:outline-none focus:border-accent"
            placeholder="0 3 * * *"
            {...field('cronExpression')}
          />
          <p className="text-xs text-muted mt-1">{t('backup.cronHint')}</p>
        </div>
        <div>
          <label className="block text-xs text-muted mb-1">{t('backup.retentionLabel')}</label>
          <input
            type="number"
            min={1}
            max={365}
            className="w-full px-3 py-2 text-sm bg-surface border border-border rounded-lg text-heading focus:outline-none focus:border-accent"
            {...field('retentionDays')}
          />
          <p className="text-xs text-muted mt-1">{t('backup.retentionHint')}</p>
        </div>
        <div className="sm:col-span-2">
          <label className="block text-xs text-muted mb-1">{t('backup.destinationLabel')}</label>
          <input
            type="text"
            className="w-full px-3 py-2 text-sm bg-surface border border-border rounded-lg text-heading placeholder:text-muted focus:outline-none focus:border-accent"
            placeholder="/data/backups"
            {...field('destination')}
          />
          <p className="text-xs text-muted mt-1">{t('backup.destinationHint')}</p>
        </div>
        <div className="flex items-center gap-3">
          <input
            type="checkbox"
            id="backup-compression"
            checked={form.compressionEnabled}
            onChange={(e) => setForm((p) => ({ ...p, compressionEnabled: e.target.checked }))}
            className="w-4 h-4 accent-accent"
          />
          <label htmlFor="backup-compression" className="text-sm text-heading">
            {t('backup.compressionLabel')}
          </label>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <button
          onClick={() => saveMutation.mutate(form)}
          disabled={saveMutation.isPending}
          className="flex items-center gap-2 px-4 py-2 bg-accent text-white rounded-lg text-sm font-medium hover:bg-accent/90 transition-colors disabled:opacity-60"
        >
          {saveMutation.isPending ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
          {t('backup.save')}
        </button>
        {saveMutation.isSuccess && (
          <span className="flex items-center gap-1 text-sm text-success">
            <CheckCircle2 size={14} />
            {t('backup.saved')}
          </span>
        )}
        {saveMutation.isError && (
          <span className="flex items-center gap-1 text-sm text-critical">
            <AlertTriangle size={14} />
            {t('backup.saveError')}
          </span>
        )}
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function BackupCoordinatorPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const statusQuery = useQuery({
    queryKey: ['backup-status'],
    queryFn: platformAdminApi.getBackupStatus,
    staleTime: 30_000,
    refetchInterval: 60_000,
  });

  const runNowMutation = useMutation({
    mutationFn: platformAdminApi.runBackupNow,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['backup-status'] }),
  });

  const data = statusQuery.data;

  return (
    <div className="p-6 max-w-3xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-heading">{t('backup.title')}</h1>
          <p className="mt-1 text-sm text-muted">{t('backup.subtitle')}</p>
        </div>
        <button
          onClick={() => statusQuery.refetch()}
          className="flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors"
        >
          <RefreshCw size={14} className={statusQuery.isFetching ? 'animate-spin' : ''} />
          {t('backup.refresh')}
        </button>
      </div>

      {statusQuery.isLoading && (
        <div className="flex items-center justify-center py-16 gap-2 text-muted">
          <Loader2 size={18} className="animate-spin" />
          <span className="text-sm">{t('backup.loading')}</span>
        </div>
      )}

      {statusQuery.isError && (
        <div className="flex items-center gap-2 py-8 text-critical text-sm">
          <AlertTriangle size={16} />
          {t('backup.loadError')}
        </div>
      )}

      {data && (
        <>
          {/* Last successful backup summary */}
          {data.lastSuccessfulBackup ? (
            <div className="flex items-center gap-4 bg-success/10 border border-success/20 rounded-xl p-4">
              <CheckCircle2 size={20} className="text-success shrink-0" />
              <div>
                <p className="text-sm font-medium text-heading">
                  {t('backup.lastSuccessfulLabel')}
                </p>
                <p className="text-xs text-muted">
                  {formatDate(data.lastSuccessfulBackup.startedAt)} —{' '}
                  {formatSizeGb(data.lastSuccessfulBackup.fileSizeGb)}
                </p>
              </div>
              <button
                onClick={() => runNowMutation.mutate()}
                disabled={runNowMutation.isPending}
                className="ml-auto flex items-center gap-1.5 px-3 py-1.5 bg-success text-white rounded-lg text-xs font-medium hover:bg-success/90 transition-colors disabled:opacity-60"
              >
                {runNowMutation.isPending ? (
                  <Loader2 size={12} className="animate-spin" />
                ) : (
                  <Play size={12} />
                )}
                {t('backup.runNow')}
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-4 bg-warning/10 border border-warning/20 rounded-xl p-4">
              <AlertTriangle size={20} className="text-warning shrink-0" />
              <div>
                <p className="text-sm font-medium text-heading">{t('backup.noBackupYet')}</p>
                <p className="text-xs text-muted">{t('backup.noBackupNote')}</p>
              </div>
              <button
                onClick={() => runNowMutation.mutate()}
                disabled={runNowMutation.isPending}
                className="ml-auto flex items-center gap-1.5 px-3 py-1.5 bg-warning text-white rounded-lg text-xs font-medium hover:bg-warning/90 transition-colors disabled:opacity-60"
              >
                {runNowMutation.isPending ? (
                  <Loader2 size={12} className="animate-spin" />
                ) : (
                  <DatabaseBackup size={12} />
                )}
                {t('backup.runNow')}
              </button>
            </div>
          )}

          {/* Schedule form */}
          <ScheduleForm initial={data.schedule} />

          {/* Backup history */}
          <div>
            <h2 className="text-base font-semibold text-heading mb-3">{t('backup.historyTitle')}</h2>
            <div className="bg-card border border-border rounded-xl">
              {data.recentBackups.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-10 text-muted text-sm gap-2">
                  <DatabaseBackup size={24} className="text-muted/40" />
                  {t('backup.emptyHistory')}
                </div>
              ) : (
                <div className="px-4">
                  {data.recentBackups.map((b) => (
                    <BackupRow key={b.id} backup={b} />
                  ))}
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
