import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Database,
  RefreshCw,
  XCircle,
  ChevronDown,
  ChevronRight,
  AlertTriangle,
  CheckCircle,
  Shield,
} from 'lucide-react';
import {
  platformAdminApi,
  type PendingMigration,
  type MigrationRisk,
} from '../api/platformAdmin';

export function MigrationPreviewPage() {
  const { t } = useTranslation('migrationPreview');
  const [expandedIds, setExpandedIds] = useState<Set<string>>(new Set());
  const [moduleFilter, setModuleFilter] = useState<string>('all');
  const [showApplyConfirm, setShowApplyConfirm] = useState(false);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['migration-preview'],
    queryFn: platformAdminApi.getMigrationPreview,
  });

  const toggleExpand = (id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const modules = data
    ? ['all', ...Array.from(new Set(data.pending.map((m) => m.module)))]
    : ['all'];

  const filtered =
    data?.pending.filter((m) => moduleFilter === 'all' || m.module === moduleFilter) ?? [];

  const highRiskCount = data?.pending.filter((m) => m.risk === 'High').length ?? 0;
  const totalDurationMs = data?.pending.reduce((acc, m) => acc + m.estimatedDurationMs, 0) ?? 0;

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Database size={24} className="text-violet-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {data && data.pending.length > 0 && (
            <button
              onClick={() => setShowApplyConfirm(true)}
              className="flex items-center gap-2 px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 font-medium"
            >
              <Shield size={14} />
              {t('applyAll')}
            </button>
          )}
          <button
            onClick={() => refetch()}
            className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
          >
            <RefreshCw size={14} />
            {t('refresh')}
          </button>
        </div>
      </div>

      {/* Apply Confirm Dialog */}
      {showApplyConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl p-6 max-w-md w-full mx-4">
            <div className="flex items-center gap-3 mb-4">
              <AlertTriangle size={22} className="text-red-600" />
              <h2 className="text-lg font-semibold text-slate-900">{t('confirmTitle')}</h2>
            </div>
            <p className="text-sm text-slate-600 mb-6">{t('confirmBody')}</p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setShowApplyConfirm(false)}
                className="px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
              >
                {t('cancel')}
              </button>
              <button
                onClick={() => setShowApplyConfirm(false)}
                className="px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 font-medium"
              >
                {t('confirmApply')}
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <XCircle size={18} />
          {t('error')}
        </div>
      )}

      {data && (
        <>
          {/* Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatCard label={t('statPending')} value={String(data.pending.length)} color="violet" />
            <StatCard label={t('statApplied')} value={String(data.appliedCount)} color="emerald" />
            <StatCard label={t('statHighRisk')} value={String(highRiskCount)} color="red" />
            <StatCard
              label={t('statEstimated')}
              value={`${(totalDurationMs / 1000).toFixed(1)}s`}
              color="amber"
            />
          </div>

          {/* Module Filter */}
          <div className="flex items-center gap-2 flex-wrap">
            {modules.map((mod) => (
              <button
                key={mod}
                onClick={() => setModuleFilter(mod)}
                className={`px-3 py-1.5 text-xs rounded-full border font-medium transition-colors ${
                  moduleFilter === mod
                    ? 'bg-violet-600 text-white border-violet-600'
                    : 'border-slate-300 text-slate-600 hover:bg-slate-50'
                }`}
              >
                {mod === 'all' ? t('filterAll') : mod}
              </button>
            ))}
          </div>

          {/* Migrations list */}
          {filtered.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-40 text-slate-400 gap-2">
              <CheckCircle size={32} className="text-emerald-400" />
              <p className="text-sm">{t('noPending')}</p>
            </div>
          ) : (
            <div className="space-y-2">
              {filtered.map((migration) => (
                <MigrationCard
                  key={migration.id}
                  migration={migration}
                  expanded={expandedIds.has(migration.id)}
                  onToggle={() => toggleExpand(migration.id)}
                  t={t}
                />
              ))}
            </div>
          )}

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
        </>
      )}
    </div>
  );
}

function MigrationCard({
  migration,
  expanded,
  onToggle,
  t,
}: {
  migration: PendingMigration;
  expanded: boolean;
  onToggle: () => void;
  t: (key: string) => string;
}) {
  const riskStyle: Record<MigrationRisk, string> = {
    Low: 'text-emerald-700 bg-emerald-50 border-emerald-200',
    Medium: 'text-amber-700 bg-amber-50 border-amber-200',
    High: 'text-red-700 bg-red-50 border-red-200',
  };

  return (
    <div className="border border-slate-200 rounded-lg overflow-hidden bg-white">
      <button
        onClick={onToggle}
        className="w-full flex items-center justify-between px-4 py-3 hover:bg-slate-50 text-left"
      >
        <div className="flex items-center gap-3">
          {expanded ? (
            <ChevronDown size={16} className="text-slate-400 shrink-0" />
          ) : (
            <ChevronRight size={16} className="text-slate-400 shrink-0" />
          )}
          <div>
            <p className="text-sm font-medium text-slate-800">{migration.name}</p>
            <p className="text-xs text-slate-500 mt-0.5">
              {migration.module} · {migration.timestamp}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <span
            className={`px-2 py-0.5 text-xs font-medium rounded border ${riskStyle[migration.risk]}`}
          >
            {t(`risk.${migration.risk}`)}
          </span>
          <span className="px-2 py-0.5 text-xs text-slate-500 bg-slate-100 rounded">
            {migration.operations.join(', ')}
          </span>
        </div>
      </button>

      {expanded && (
        <div className="border-t border-slate-100 bg-slate-50 px-4 py-3">
          <p className="text-xs font-medium text-slate-600 mb-2">{t('sqlPreview')}</p>
          <pre className="text-xs text-slate-700 bg-white border border-slate-200 rounded p-3 overflow-x-auto whitespace-pre-wrap">
            {migration.sqlPreview}
          </pre>
          <div className="mt-2 flex items-center gap-4 text-xs text-slate-500">
            <span>
              {t('estimatedDuration')}: {migration.estimatedDurationMs}ms
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

function StatCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: 'violet' | 'emerald' | 'red' | 'amber';
}) {
  const colorMap = {
    violet: 'text-violet-600',
    emerald: 'text-emerald-600',
    red: 'text-red-600',
    amber: 'text-amber-600',
  };
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}
