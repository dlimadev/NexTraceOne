import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Gauge,
  RefreshCw,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Pencil,
  Save,
  X,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { platformAdminApi } from '../api/platformAdmin';
import type { TenantBudgetEntry, TenantResourceQuota } from '../api/platformAdmin';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function usagePercent(used: number, max: number | null): number | null {
  if (max == null || max === 0) return null;
  return Math.round((used / max) * 100);
}

function statusVariant(pct: number | null): 'success' | 'warning' | 'danger' | 'default' {
  if (pct == null) return 'default';
  if (pct >= 100) return 'danger';
  if (pct >= 80) return 'warning';
  return 'success';
}

function ProgressBar({ pct }: { pct: number | null }) {
  if (pct == null) return <span className="text-xs text-muted">—</span>;
  const color =
    pct >= 100 ? 'bg-critical' : pct >= 80 ? 'bg-warning' : 'bg-success';
  return (
    <div className="w-full bg-surface rounded-full h-1.5 overflow-hidden">
      <div className={`h-1.5 rounded-full ${color}`} style={{ width: `${Math.min(pct, 100)}%` }} />
    </div>
  );
}

// ─── Quota editor ────────────────────────────────────────────────────────────

interface QuotaEditorProps {
  entry: TenantBudgetEntry;
  onSave: (quota: TenantResourceQuota) => void;
  onCancel: () => void;
  saving: boolean;
}

function QuotaEditor({ entry, onSave, onCancel, saving }: QuotaEditorProps) {
  const { t } = useTranslation();
  const [form, setForm] = useState<TenantResourceQuota>({ ...entry.quota });

  const field = (
    key: keyof TenantResourceQuota,
    label: string,
    unit: string
  ) => (
    <div>
      <label className="block text-xs text-muted mb-1">
        {label} <span className="text-muted/60">({unit})</span>
      </label>
      <input
        type="number"
        min={0}
        value={form[key] ?? ''}
        onChange={(e) =>
          setForm((f) => ({
            ...f,
            [key]: e.target.value === '' ? null : Number(e.target.value),
          }))
        }
        placeholder={t('resourceBudget.unlimited')}
        className="w-full text-sm px-2.5 py-1.5 rounded border border-border bg-input focus:outline-none focus:ring-1 focus:ring-accent"
      />
    </div>
  );

  return (
    <div className="mt-4 p-3 rounded-lg border border-border bg-surface space-y-3">
      <p className="text-xs font-medium text-muted">{t('resourceBudget.editQuota')}</p>
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        {field('maxCpuCores', t('resourceBudget.cpu'), 'cores')}
        {field('maxMemoryGb', t('resourceBudget.memory'), 'GB')}
        {field('maxDiskGb', t('resourceBudget.disk'), 'GB')}
        {field('maxAiTokensPerMonth', t('resourceBudget.aiTokens'), t('resourceBudget.perMonth'))}
        {field('maxConnections', t('resourceBudget.connections'), t('resourceBudget.max'))}
      </div>
      <div className="flex gap-2 justify-end">
        <Button variant="ghost" size="sm" onClick={onCancel} disabled={saving}>
          <X size={14} className="mr-1" />
          {t('resourceBudget.cancel')}
        </Button>
        <Button variant="primary" size="sm" onClick={() => onSave(form)} disabled={saving}>
          <Save size={14} className="mr-1" />
          {saving ? t('resourceBudget.saving') : t('resourceBudget.save')}
        </Button>
      </div>
    </div>
  );
}

// ─── Tenant row ───────────────────────────────────────────────────────────────

function TenantRow({ entry }: { entry: TenantBudgetEntry }) {
  const { t } = useTranslation();
  const [editing, setEditing] = useState(false);
  const qc = useQueryClient();

  const updateMutation = useMutation({
    mutationFn: (quota: TenantResourceQuota) =>
      platformAdminApi.updateTenantQuota(entry.tenantId, quota),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['resource-budget'] });
      setEditing(false);
    },
  });

  const metrics: { label: string; used: number; max: number | null; unit: string }[] = [
    { label: t('resourceBudget.cpu'), used: entry.usage.cpuRequestsCores, max: entry.quota.maxCpuCores, unit: 'cores' },
    { label: t('resourceBudget.memory'), used: entry.usage.memoryRequestsGb, max: entry.quota.maxMemoryGb, unit: 'GB' },
    { label: t('resourceBudget.disk'), used: entry.usage.diskUsageGb, max: entry.quota.maxDiskGb, unit: 'GB' },
    { label: t('resourceBudget.aiTokens'), used: entry.usage.aiTokensUsedThisMonth, max: entry.quota.maxAiTokensPerMonth, unit: '' },
  ];

  return (
    <Card>
      <CardHeader className="flex items-center gap-2">
        {entry.isBlocked ? (
          <XCircle size={15} className="text-critical shrink-0" />
        ) : (
          <CheckCircle2 size={15} className="text-success shrink-0" />
        )}
        <span className="font-medium text-sm">{entry.tenantName}</span>
        <code className="text-xs text-muted font-mono">{entry.tenantId}</code>
        {entry.isBlocked && (
          <Badge variant="danger" className="ml-auto">
            {t('resourceBudget.blocked')}
          </Badge>
        )}
        {entry.overrideUntil && (
          <Badge variant="warning" className={entry.isBlocked ? '' : 'ml-auto'}>
            {t('resourceBudget.override')}
          </Badge>
        )}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setEditing((e) => !e)}
          className={entry.isBlocked || entry.overrideUntil ? '' : 'ml-auto'}
        >
          <Pencil size={13} />
        </Button>
      </CardHeader>

      <CardBody>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {metrics.map(({ label, used, max, unit }) => {
            const pct = usagePercent(used, max);
            return (
              <div key={label}>
                <div className="flex justify-between text-xs mb-1">
                  <span className="text-muted">{label}</span>
                  <Badge variant={statusVariant(pct)} className="text-[10px] px-1 py-0">
                    {pct != null ? `${pct}%` : t('resourceBudget.unlimited')}
                  </Badge>
                </div>
                <ProgressBar pct={pct} />
                <p className="text-xs text-muted mt-1">
                  {used.toLocaleString()}{unit ? ` ${unit}` : ''}{' '}
                  {max != null ? `/ ${max.toLocaleString()}${unit ? ` ${unit}` : ''}` : ''}
                </p>
              </div>
            );
          })}
        </div>

        {entry.isBlocked && entry.blockReason && (
          <div className="mt-3 flex items-start gap-2 text-xs text-critical">
            <AlertTriangle size={13} className="shrink-0 mt-0.5" />
            {entry.blockReason}
          </div>
        )}

        {editing && (
          <QuotaEditor
            entry={entry}
            onSave={(quota) => updateMutation.mutate(quota)}
            onCancel={() => setEditing(false)}
            saving={updateMutation.isPending}
          />
        )}
      </CardBody>
    </Card>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function ResourceBudgetPage() {
  const { t } = useTranslation();

  const query = useQuery({
    queryKey: ['resource-budget'],
    queryFn: () => platformAdminApi.getResourceBudget(),
    staleTime: 30_000,
  });

  const blocked = query.data?.tenants.filter((t) => t.isBlocked).length ?? 0;
  const total = query.data?.tenants.length ?? 0;

  return (
    <PageContainer>
      <PageHeader
        icon={<Gauge size={22} />}
        title={t('resourceBudget.title')}
        subtitle={t('resourceBudget.subtitle')}
        actions={
          <Button variant="secondary" onClick={() => query.refetch()} size="sm">
            <RefreshCw size={14} className="mr-1.5" />
            {t('resourceBudget.refresh')}
          </Button>
        }
      />

      <PageSection>
        {query.isLoading && <PageLoadingState />}
        {query.isError && <PageErrorState message={t('resourceBudget.loadError')} />}
        {query.isSuccess && (
          <>
            {/* Summary */}
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 mb-4">
              <Card>
                <CardBody className="flex items-center gap-3">
                  <Gauge size={20} className="text-accent" />
                  <div>
                    <p className="text-xs text-muted">{t('resourceBudget.totalTenants')}</p>
                    <p className="text-xl font-semibold">{total}</p>
                  </div>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="flex items-center gap-3">
                  <XCircle size={20} className={blocked > 0 ? 'text-critical' : 'text-success'} />
                  <div>
                    <p className="text-xs text-muted">{t('resourceBudget.blockedTenants')}</p>
                    <p className="text-xl font-semibold">{blocked}</p>
                  </div>
                </CardBody>
              </Card>
              <Card>
                <CardBody className="flex items-center gap-3">
                  <CheckCircle2 size={20} className="text-success" />
                  <div>
                    <p className="text-xs text-muted">{t('resourceBudget.healthyTenants')}</p>
                    <p className="text-xl font-semibold">{total - blocked}</p>
                  </div>
                </CardBody>
              </Card>
            </div>

            {query.data.tenants.length === 0 ? (
              <p className="text-sm text-muted text-center py-12">{t('resourceBudget.empty')}</p>
            ) : (
              <div className="space-y-4">
                {query.data.tenants.map((entry) => (
                  <TenantRow key={entry.tenantId} entry={entry} />
                ))}
              </div>
            )}
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
