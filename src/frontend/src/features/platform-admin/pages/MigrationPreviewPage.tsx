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
  CheckCircle2,
  Shield,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Database size={24} className="text-accent" />}
          actions={
            <div className="flex items-center gap-2">
              {data && data.pending.length > 0 && (
                <Button variant="danger" onClick={() => setShowApplyConfirm(true)}>
                  <Shield size={14} />
                  {t('applyAll')}
                </Button>
              )}
              <Button variant="ghost" onClick={() => refetch()}>
                <RefreshCw size={14} />
                {t('refresh')}
              </Button>
            </div>
          }
        />

        {/* Apply Confirm Dialog */}
        {showApplyConfirm && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="bg-card border border-edge rounded-md shadow-xl p-6 max-w-md w-full mx-4">
              <div className="flex items-center gap-3 mb-4">
                <AlertTriangle size={22} className="text-critical" />
                <h2 className="text-lg font-semibold text-heading">{t('confirmTitle')}</h2>
              </div>
              <p className="text-sm text-muted mb-6">{t('confirmBody')}</p>
              <div className="flex gap-3 justify-end">
                <button
                  onClick={() => setShowApplyConfirm(false)}
                  className="px-4 py-2 text-sm border border-edge rounded-lg hover:bg-elevated text-muted"
                >
                  {t('cancel')}
                </button>
                <button
                  onClick={() => setShowApplyConfirm(false)}
                  className="px-4 py-2 text-sm bg-critical text-white rounded-lg hover:bg-critical/90 font-medium"
                >
                  {t('confirmApply')}
                </button>
              </div>
            </div>
          </div>
        )}

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && (
          <>
            {/* Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatCard label={t('statPending')} value={String(data.pending.length)} color="accent" />
              <StatCard label={t('statApplied')} value={String(data.appliedCount)} color="success" />
              <StatCard label={t('statHighRisk')} value={String(highRiskCount)} color="critical" />
              <StatCard
                label={t('statEstimated')}
                value={`${(totalDurationMs / 1000).toFixed(1)}s`}
                color="warning"
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
                      ? 'bg-accent text-white border-accent'
                      : 'border-edge text-muted hover:bg-elevated'
                  }`}
                >
                  {mod === 'all' ? t('filterAll') : mod}
                </button>
              ))}
            </div>

            {/* Migrations list */}
            {filtered.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-40 text-faded gap-2">
                <CheckCircle2 size={32} className="text-success" />
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

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
          </>
        )}
      </div>
    </PageContainer>
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
    Low: 'text-success bg-success/10 border-success/20',
    Medium: 'text-warning bg-warning/10 border-warning/20',
    High: 'text-critical bg-critical/10 border-critical/20',
  };

  return (
    <div className="border border-edge rounded-lg overflow-hidden bg-card">
      <button
        onClick={onToggle}
        className="w-full flex items-center justify-between px-4 py-3 hover:bg-elevated text-left"
      >
        <div className="flex items-center gap-3">
          {expanded ? (
            <ChevronDown size={16} className="text-faded shrink-0" />
          ) : (
            <ChevronRight size={16} className="text-faded shrink-0" />
          )}
          <div>
            <p className="text-sm font-medium text-heading">{migration.name}</p>
            <p className="text-xs text-muted mt-0.5">
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
          <span className="px-2 py-0.5 text-xs text-muted bg-elevated rounded">
            {migration.operations.join(', ')}
          </span>
        </div>
      </button>

      {expanded && (
        <div className="border-t border-edge/50 bg-elevated px-4 py-3">
          <p className="text-xs font-medium text-muted mb-2">{t('sqlPreview')}</p>
          <pre className="text-xs text-body bg-card border border-edge rounded p-3 overflow-x-auto whitespace-pre-wrap">
            {migration.sqlPreview}
          </pre>
          <div className="mt-2 flex items-center gap-4 text-xs text-muted">
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
  color: 'accent' | 'success' | 'critical' | 'warning';
}) {
  const colorMap = {
    accent: 'text-accent',
    success: 'text-success',
    critical: 'text-critical',
    warning: 'text-warning',
  };
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}
