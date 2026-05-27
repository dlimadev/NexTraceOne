import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Cpu,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CheckCircle,
  Activity,
  Zap,
  Clock,
  Layers,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type AiResourceGovernorConfigUpdate } from '../api/platformAdmin';

export function AiResourceGovernorPage() {
  const { t } = useTranslation('aiResourceGovernor');
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({
    maxConcurrency: '',
    inferenceTimeoutSeconds: '',
    queueTimeoutSeconds: '',
    circuitBreakerEnabled: true,
    circuitBreakerErrorThresholdPercent: '',
    circuitBreakerResetAfterMinutes: '',
    priorityQueueEnabled: true,
  });

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-governor-status'],
    queryFn: platformAdminApi.getAiGovernorStatus,
    refetchInterval: 30_000,
  });

  const updateMutation = useMutation({
    mutationFn: (payload: AiResourceGovernorConfigUpdate) =>
      platformAdminApi.updateAiGovernorConfig(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governor-status'] });
      setEditing(false);
    },
  });

  function startEdit() {
    if (!data) return;
    const c = data.config;
    setForm({
      maxConcurrency: String(c.maxConcurrency),
      inferenceTimeoutSeconds: String(c.inferenceTimeoutSeconds),
      queueTimeoutSeconds: String(c.queueTimeoutSeconds),
      circuitBreakerEnabled: c.circuitBreakerEnabled,
      circuitBreakerErrorThresholdPercent: String(c.circuitBreakerErrorThresholdPercent),
      circuitBreakerResetAfterMinutes: String(c.circuitBreakerResetAfterMinutes),
      priorityQueueEnabled: c.priorityQueueEnabled,
    });
    setEditing(true);
  }

  function saveConfig() {
    updateMutation.mutate({
      maxConcurrency: parseInt(form.maxConcurrency, 10),
      inferenceTimeoutSeconds: parseInt(form.inferenceTimeoutSeconds, 10),
      queueTimeoutSeconds: parseInt(form.queueTimeoutSeconds, 10),
      circuitBreakerEnabled: form.circuitBreakerEnabled,
      circuitBreakerErrorThresholdPercent: parseFloat(form.circuitBreakerErrorThresholdPercent),
      circuitBreakerResetAfterMinutes: parseInt(form.circuitBreakerResetAfterMinutes, 10),
      priorityQueueEnabled: form.priorityQueueEnabled,
    });
  }

  const cbState = data?.metrics.circuitBreakerState;

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Cpu size={24} className="text-accent" />}
          actions={
            <Button variant="primary" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

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
            {/* Circuit Breaker Banner */}
            {cbState === 'Open' && (
              <div className="flex items-start gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg">
                <AlertTriangle size={18} className="text-critical mt-0.5 shrink-0" />
                <div>
                  <p className="text-sm font-medium text-critical">{t('cbOpenTitle')}</p>
                  <p className="text-xs text-critical mt-0.5">
                    {t('cbOpenDetail', { since: data.metrics.circuitBreakerOpenSince ?? '—' })}
                  </p>
                </div>
              </div>
            )}

            {cbState === 'HalfOpen' && (
              <div className="flex items-start gap-3 p-4 bg-warning/10 border border-warning/20 rounded-lg">
                <AlertTriangle size={18} className="text-warning mt-0.5 shrink-0" />
                <p className="text-sm text-warning">{t('cbHalfOpenMsg')}</p>
              </div>
            )}

            {cbState === 'Closed' && (
              <div className="flex items-start gap-3 p-4 bg-success/10 border border-success/20 rounded-lg">
                <CheckCircle size={18} className="text-success mt-0.5 shrink-0" />
                <p className="text-sm text-success">{t('cbClosedMsg')}</p>
              </div>
            )}

            {/* Metrics Grid */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <MetricCard
                icon={<Layers size={18} className="text-accent" />}
                label={t('metricActiveReq')}
                value={String(data.metrics.activeRequests)}
                sub={`/ ${data.config.maxConcurrency} ${t('max')}`}
              />
              <MetricCard
                icon={<Activity size={18} className="text-accent" />}
                label={t('metricQueueDepth')}
                value={String(data.metrics.queueDepth)}
              />
              <MetricCard
                icon={<Zap size={18} className="text-warning" />}
                label={t('metricP95Latency')}
                value={`${data.metrics.latencyP95Ms} ms`}
              />
              <MetricCard
                icon={<Clock size={18} className="text-critical" />}
                label={t('metricErrorRate')}
                value={`${data.metrics.errorRatePercent.toFixed(1)}%`}
              />
            </div>

            {/* Config Section */}
            <section>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-medium text-heading">{t('configTitle')}</h2>
                {!editing && (
                  <Button variant="outline" onClick={startEdit}>
                    {t('editConfig')}
                  </Button>
                )}
              </div>

              {editing ? (
                <div className="border border-edge rounded-lg p-5 space-y-5">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <NumberField
                      label={t('maxConcurrencyLabel')}
                      hint={t('maxConcurrencyHint')}
                      value={form.maxConcurrency}
                      onChange={(v) => setForm((f) => ({ ...f, maxConcurrency: v }))}
                    />
                    <NumberField
                      label={t('inferenceTimeoutLabel')}
                      hint={t('inferenceTimeoutHint')}
                      value={form.inferenceTimeoutSeconds}
                      onChange={(v) => setForm((f) => ({ ...f, inferenceTimeoutSeconds: v }))}
                    />
                    <NumberField
                      label={t('queueTimeoutLabel')}
                      hint={t('queueTimeoutHint')}
                      value={form.queueTimeoutSeconds}
                      onChange={(v) => setForm((f) => ({ ...f, queueTimeoutSeconds: v }))}
                    />
                    <NumberField
                      label={t('cbErrorThresholdLabel')}
                      hint={t('cbErrorThresholdHint')}
                      value={form.circuitBreakerErrorThresholdPercent}
                      onChange={(v) =>
                        setForm((f) => ({ ...f, circuitBreakerErrorThresholdPercent: v }))
                      }
                    />
                    <NumberField
                      label={t('cbResetLabel')}
                      hint={t('cbResetHint')}
                      value={form.circuitBreakerResetAfterMinutes}
                      onChange={(v) =>
                        setForm((f) => ({ ...f, circuitBreakerResetAfterMinutes: v }))
                      }
                    />
                  </div>
                  <div className="flex flex-col gap-3">
                    <ToggleField
                      label={t('cbEnabledLabel')}
                      hint={t('cbEnabledHint')}
                      checked={form.circuitBreakerEnabled}
                      onChange={(v) => setForm((f) => ({ ...f, circuitBreakerEnabled: v }))}
                    />
                    <ToggleField
                      label={t('priorityQueueLabel')}
                      hint={t('priorityQueueHint')}
                      checked={form.priorityQueueEnabled}
                      onChange={(v) => setForm((f) => ({ ...f, priorityQueueEnabled: v }))}
                    />
                  </div>
                  <div className="flex gap-3 pt-2">
                    <Button
                      variant="primary"
                      onClick={saveConfig}
                      disabled={updateMutation.isPending}
                    >
                      {updateMutation.isPending ? t('saving') : t('save')}
                    </Button>
                    <Button variant="ghost" onClick={() => setEditing(false)}>
                      {t('cancel')}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="border border-edge rounded-lg divide-y divide-edge/50">
                  <ConfigRow label={t('maxConcurrencyLabel')} value={String(data.config.maxConcurrency)} />
                  <ConfigRow
                    label={t('inferenceTimeoutLabel')}
                    value={`${data.config.inferenceTimeoutSeconds}s`}
                  />
                  <ConfigRow
                    label={t('queueTimeoutLabel')}
                    value={`${data.config.queueTimeoutSeconds}s`}
                  />
                  <ConfigRow
                    label={t('cbEnabledLabel')}
                    value={data.config.circuitBreakerEnabled ? t('enabled') : t('disabled')}
                  />
                  <ConfigRow
                    label={t('cbErrorThresholdLabel')}
                    value={`${data.config.circuitBreakerErrorThresholdPercent}%`}
                  />
                  <ConfigRow
                    label={t('cbResetLabel')}
                    value={`${data.config.circuitBreakerResetAfterMinutes} min`}
                  />
                  <ConfigRow
                    label={t('priorityQueueLabel')}
                    value={data.config.priorityQueueEnabled ? t('enabled') : t('disabled')}
                  />
                </div>
              )}
            </section>

            <p className="text-xs text-faded">
              {t('updatedAt', { date: data.config.updatedAt })}
            </p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function MetricCard({
  icon,
  label,
  value,
  sub,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
  sub?: string;
}) {
  return (
    <div className="border border-edge rounded-lg p-4 bg-card flex items-start gap-3">
      <div className="mt-0.5">{icon}</div>
      <div>
        <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
        <p className="text-xl font-semibold text-heading mt-0.5">{value}</p>
        {sub && <p className="text-xs text-faded mt-0.5">{sub}</p>}
      </div>
    </div>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-muted w-56 shrink-0">{label}</span>
      <span className="text-sm font-medium text-heading">{value}</span>
    </div>
  );
}

function NumberField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-body">{label}</label>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-edge rounded-lg bg-canvas text-body text-sm focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
      />
      <p className="text-xs text-muted">{hint}</p>
    </div>
  );
}

function ToggleField({
  label,
  hint,
  checked,
  onChange,
}: {
  label: string;
  hint: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex items-start gap-3 cursor-pointer">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        aria-label={label}
        className="mt-0.5 h-4 w-4 rounded border-edge text-accent"
      />
      <div>
        <span className="block text-sm font-medium text-body">{label}</span>
        <span className="block text-xs text-muted">{hint}</span>
      </div>
    </label>
  );
}
