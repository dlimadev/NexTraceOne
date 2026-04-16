import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Database,
  RefreshCw,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Layers,
  Settings,
  Save,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { platformAdminApi } from '../api/platformAdmin';
import type { EsClusterHealth, EsIndexInfo, EsIlmPolicy } from '../api/platformAdmin';

// ─── Helpers ─────────────────────────────────────────────────────────────────

type Tab = 'health' | 'indices' | 'policies';

function clusterStatusIcon(status: EsClusterHealth['status']) {
  switch (status) {
    case 'green': return <CheckCircle2 size={16} className="text-success" />;
    case 'yellow': return <AlertTriangle size={16} className="text-warning" />;
    case 'red': return <XCircle size={16} className="text-critical" />;
  }
}

function clusterStatusVariant(status: EsClusterHealth['status']): 'success' | 'warning' | 'danger' {
  switch (status) { case 'green': return 'success'; case 'yellow': return 'warning'; case 'red': return 'danger'; }
}

const phaseColors: Record<string, string> = {
  hot: 'bg-warning/20 text-warning border-warning/30',
  warm: 'bg-accent/20 text-accent border-accent/30',
  cold: 'bg-surface text-muted border-border',
  delete: 'bg-critical/10 text-critical border-critical/30',
};

// ─── Cluster Health tab ───────────────────────────────────────────────────────

function ClusterHealthTab({ health }: { health: EsClusterHealth }) {
  const { t } = useTranslation();

  const metrics = [
    { label: t('elasticsearchManager.nodes'), value: health.numberOfNodes },
    { label: t('elasticsearchManager.activeShards'), value: health.activeShards },
    { label: t('elasticsearchManager.unassignedShards'), value: health.unassignedShards, warn: health.unassignedShards > 0 },
    { label: t('elasticsearchManager.jvmHeap'), value: `${health.jvmHeapUsedPercent}%`, warn: health.jvmHeapUsedPercent > 75 },
    { label: t('elasticsearchManager.diskUsed'), value: `${health.diskUsedGb.toFixed(1)} / ${health.diskTotalGb.toFixed(1)} GB`, warn: health.diskUsedPercent > 80 },
    { label: t('elasticsearchManager.diskPct'), value: `${health.diskUsedPercent}%`, warn: health.diskUsedPercent > 80 },
  ];

  return (
    <div className="space-y-4">
      {/* Status banner */}
      <Card>
        <CardBody className="flex items-center gap-3">
          {clusterStatusIcon(health.status)}
          <div>
            <p className="text-sm font-medium">
              {t('elasticsearchManager.cluster')}: <span className="font-mono">{health.clusterName}</span>
            </p>
            <p className="text-xs text-muted">
              {t('elasticsearchManager.checkedAt')}: {new Date(health.checkedAt).toLocaleString()}
            </p>
          </div>
          <Badge variant={clusterStatusVariant(health.status)} className="ml-auto uppercase">
            {health.status}
          </Badge>
          {health.isReadOnly && (
            <Badge variant="danger">{t('elasticsearchManager.readOnly')}</Badge>
          )}
        </CardBody>
      </Card>

      {/* Projection */}
      {health.projectedDaysUntilFull != null && health.projectedDaysUntilFull < 30 && (
        <div className="flex items-start gap-2 p-3 rounded-lg border border-warning/40 bg-warning/5 text-sm text-warning">
          <AlertTriangle size={14} className="shrink-0 mt-0.5" />
          {t('elasticsearchManager.diskFull', { days: health.projectedDaysUntilFull })}
        </div>
      )}

      {/* Metrics grid */}
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        {metrics.map(({ label, value, warn }) => (
          <Card key={label}>
            <CardBody>
              <p className="text-xs text-muted mb-1">{label}</p>
              <p className={`text-lg font-semibold ${warn ? 'text-warning' : ''}`}>{value}</p>
            </CardBody>
          </Card>
        ))}
      </div>
    </div>
  );
}

// ─── Indices tab ──────────────────────────────────────────────────────────────

function IndicesTab({ indices }: { indices: EsIndexInfo[] }) {
  const { t } = useTranslation();

  return (
    <div className="space-y-2">
      {indices.length === 0 && (
        <p className="text-sm text-muted text-center py-8">{t('elasticsearchManager.noIndices')}</p>
      )}
      {indices.map((idx) => (
        <Card key={idx.name}>
          <CardBody className="flex flex-wrap items-center gap-3">
            <Layers size={14} className="text-muted shrink-0" />
            <code className="text-sm font-mono">{idx.name}</code>
            <span className={`text-xs px-2 py-0.5 rounded-full border font-medium ${phaseColors[idx.currentPhase] ?? phaseColors.cold}`}>
              {idx.currentPhase}
            </span>
            <span className="text-xs text-muted ml-auto">
              {idx.docsCount.toLocaleString()} {t('elasticsearchManager.docs')} · {idx.storeSizeGb.toFixed(2)} GB
            </span>
            {idx.ilmPolicyName && (
              <span className="text-xs text-muted font-mono">{idx.ilmPolicyName}</span>
            )}
          </CardBody>
        </Card>
      ))}
    </div>
  );
}

// ─── ILM Policies tab ─────────────────────────────────────────────────────────

function PolicyRow({ policy }: { policy: EsIlmPolicy }) {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<EsIlmPolicy>({ ...policy });

  const saveMutation = useMutation({
    mutationFn: () => platformAdminApi.updateIlmPolicy(policy.name, form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['elasticsearch-manager'] });
      setEditing(false);
    },
  });

  const numField = (
    label: string,
    key: keyof EsIlmPolicy
  ) => (
    <div>
      <label className="block text-xs text-muted mb-1">{label}</label>
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
        placeholder={t('elasticsearchManager.noPhase')}
        className="w-full text-sm px-2.5 py-1.5 rounded border border-border bg-input focus:outline-none focus:ring-1 focus:ring-accent"
      />
    </div>
  );

  return (
    <Card>
      <CardHeader className="flex items-center gap-2">
        <Settings size={14} className="text-muted shrink-0" />
        <code className="text-sm font-mono">{policy.name}</code>
        <Button variant="ghost" size="sm" onClick={() => setEditing((e) => !e)} className="ml-auto">
          {editing ? <XCircle size={13} /> : <Settings size={13} />}
        </Button>
      </CardHeader>
      <CardBody>
        <div className="flex flex-wrap gap-3 text-xs text-muted">
          {[
            { label: t('elasticsearchManager.hotDays'), value: policy.hotMaxAgeDays },
            { label: t('elasticsearchManager.warmDays'), value: policy.warmAfterDays },
            { label: t('elasticsearchManager.deleteDays'), value: policy.deleteAfterDays },
          ].map(({ label, value }) => (
            <span key={label}>
              {label}: <strong className="text-body">{value != null ? `${value}d` : '—'}</strong>
            </span>
          ))}
        </div>

        {editing && (
          <div className="mt-3 p-3 rounded-lg border border-border bg-surface space-y-3">
            <div className="grid grid-cols-3 gap-3">
              {numField(t('elasticsearchManager.hotDays'), 'hotMaxAgeDays')}
              {numField(t('elasticsearchManager.warmDays'), 'warmAfterDays')}
              {numField(t('elasticsearchManager.deleteDays'), 'deleteAfterDays')}
            </div>
            <div className="flex justify-end">
              <Button
                variant="primary"
                size="sm"
                onClick={() => saveMutation.mutate()}
                disabled={saveMutation.isPending}
              >
                <Save size={13} className="mr-1" />
                {saveMutation.isPending ? t('elasticsearchManager.saving') : t('elasticsearchManager.save')}
              </Button>
            </div>
          </div>
        )}
      </CardBody>
    </Card>
  );
}

function PoliciesTab({ policies }: { policies: EsIlmPolicy[] }) {
  const { t } = useTranslation();

  return (
    <div className="space-y-3">
      {policies.length === 0 && (
        <p className="text-sm text-muted text-center py-8">{t('elasticsearchManager.noPolicies')}</p>
      )}
      {policies.map((p) => (
        <PolicyRow key={p.name} policy={p} />
      ))}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function ElasticsearchManagerPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<Tab>('health');

  const query = useQuery({
    queryKey: ['elasticsearch-manager'],
    queryFn: () => platformAdminApi.getElasticsearchManager(),
    staleTime: 30_000,
  });

  const tabs: { id: Tab; label: string }[] = [
    { id: 'health', label: t('elasticsearchManager.tabHealth') },
    { id: 'indices', label: t('elasticsearchManager.tabIndices') },
    { id: 'policies', label: t('elasticsearchManager.tabPolicies') },
  ];

  return (
    <PageContainer>
      <PageHeader
        icon={<Database size={22} />}
        title={t('elasticsearchManager.title')}
        subtitle={t('elasticsearchManager.subtitle')}
        actions={
          <Button variant="secondary" onClick={() => query.refetch()} size="sm">
            <RefreshCw size={14} className="mr-1.5" />
            {t('elasticsearchManager.refresh')}
          </Button>
        }
      />

      <PageSection>
        {query.isLoading && <PageLoadingState />}
        {query.isError && <PageErrorState message={t('elasticsearchManager.loadError')} />}
        {query.isSuccess && (
          <>
            {/* Tabs */}
            <div className="flex gap-1 border-b border-border mb-4">
              {tabs.map(({ id, label }) => (
                <button
                  key={id}
                  onClick={() => setActiveTab(id)}
                  className={`px-4 py-2 text-sm font-medium rounded-t transition-colors ${
                    activeTab === id
                      ? 'text-accent border-b-2 border-accent -mb-px'
                      : 'text-muted hover:text-body'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>

            {activeTab === 'health' && (
              <ClusterHealthTab health={query.data.clusterHealth} />
            )}
            {activeTab === 'indices' && (
              <IndicesTab indices={query.data.indices} />
            )}
            {activeTab === 'policies' && (
              <PoliciesTab policies={query.data.ilmPolicies} />
            )}
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
