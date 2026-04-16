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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Cpu size={24} className="text-violet-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-violet-600 text-white rounded-lg hover:bg-violet-700 transition-colors"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

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
          {/* Circuit Breaker Banner */}
          {cbState === 'Open' && (
            <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
              <AlertTriangle size={18} className="text-red-600 mt-0.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-red-800">{t('cbOpenTitle')}</p>
                <p className="text-xs text-red-600 mt-0.5">
                  {t('cbOpenDetail', { since: data.metrics.circuitBreakerOpenSince ?? '—' })}
                </p>
              </div>
            </div>
          )}

          {cbState === 'HalfOpen' && (
            <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg">
              <AlertTriangle size={18} className="text-amber-600 mt-0.5 shrink-0" />
              <p className="text-sm text-amber-800">{t('cbHalfOpenMsg')}</p>
            </div>
          )}

          {cbState === 'Closed' && (
            <div className="flex items-start gap-3 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
              <CheckCircle size={18} className="text-emerald-600 mt-0.5 shrink-0" />
              <p className="text-sm text-emerald-800">{t('cbClosedMsg')}</p>
            </div>
          )}

          {/* Metrics Grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <MetricCard
              icon={<Layers size={18} className="text-violet-500" />}
              label={t('metricActiveReq')}
              value={String(data.metrics.activeRequests)}
              sub={`/ ${data.config.maxConcurrency} ${t('max')}`}
            />
            <MetricCard
              icon={<Activity size={18} className="text-indigo-500" />}
              label={t('metricQueueDepth')}
              value={String(data.metrics.queueDepth)}
            />
            <MetricCard
              icon={<Zap size={18} className="text-amber-500" />}
              label={t('metricP95Latency')}
              value={`${data.metrics.latencyP95Ms} ms`}
            />
            <MetricCard
              icon={<Clock size={18} className="text-rose-500" />}
              label={t('metricErrorRate')}
              value={`${data.metrics.errorRatePercent.toFixed(1)}%`}
            />
          </div>

          {/* Config Section */}
          <section>
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-medium text-slate-800">{t('configTitle')}</h2>
              {!editing && (
                <button
                  onClick={startEdit}
                  className="px-3 py-1.5 text-sm text-violet-600 border border-violet-200 rounded hover:bg-violet-50"
                >
                  {t('editConfig')}
                </button>
              )}
            </div>

            {editing ? (
              <div className="border border-slate-200 rounded-lg p-5 space-y-5">
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
                  <button
                    onClick={saveConfig}
                    disabled={updateMutation.isPending}
                    className="px-4 py-2 bg-violet-600 text-white text-sm rounded-lg hover:bg-violet-700 disabled:opacity-50"
                  >
                    {updateMutation.isPending ? t('saving') : t('save')}
                  </button>
                  <button
                    onClick={() => setEditing(false)}
                    className="px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
                  >
                    {t('cancel')}
                  </button>
                </div>
              </div>
            ) : (
              <div className="border border-slate-200 rounded-lg divide-y divide-slate-100">
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

          <p className="text-xs text-slate-400">
            {t('updatedAt', { date: data.config.updatedAt })}
          </p>
        </>
      )}
    </div>
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
    <div className="border border-slate-200 rounded-lg p-4 bg-white flex items-start gap-3">
      <div className="mt-0.5">{icon}</div>
      <div>
        <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
        <p className="text-xl font-semibold text-slate-900 mt-0.5">{value}</p>
        {sub && <p className="text-xs text-slate-400 mt-0.5">{sub}</p>}
      </div>
    </div>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-slate-500 w-56 shrink-0">{label}</span>
      <span className="text-sm font-medium text-slate-800">{value}</span>
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
      <label className="block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-violet-500 focus:border-violet-500"
      />
      <p className="text-xs text-slate-500">{hint}</p>
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
        className="mt-0.5 h-4 w-4 rounded border-slate-300 text-violet-600"
      />
      <div>
        <span className="block text-sm font-medium text-slate-700">{label}</span>
        <span className="block text-xs text-slate-500">{hint}</span>
      </div>
    </label>
  );
}
