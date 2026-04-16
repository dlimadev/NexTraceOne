import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Power, CheckCircle, XCircle, RefreshCw, Settings } from 'lucide-react';
import { platformAdminApi, type GracefulShutdownConfigUpdate } from '../api/platformAdmin';

export function GracefulShutdownPage() {
  const { t } = useTranslation('gracefulShutdown');
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<GracefulShutdownConfigUpdate>({
    requestDrainTimeoutSeconds: 30,
    outboxDrainTimeoutSeconds: 60,
    healthCheckReturns503OnShutdown: true,
    auditShutdownEvents: true,
  });

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['graceful-shutdown-config'],
    queryFn: platformAdminApi.getGracefulShutdownConfig,
  });

  const mutation = useMutation({
    mutationFn: (config: GracefulShutdownConfigUpdate) =>
      platformAdminApi.updateGracefulShutdownConfig(config),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['graceful-shutdown-config'] });
      setEditing(false);
    },
  });

  function startEdit() {
    if (!data) return;
    setForm({
      requestDrainTimeoutSeconds: data.requestDrainTimeoutSeconds,
      outboxDrainTimeoutSeconds: data.outboxDrainTimeoutSeconds,
      healthCheckReturns503OnShutdown: data.healthCheckReturns503OnShutdown,
      auditShutdownEvents: data.auditShutdownEvents,
    });
    setEditing(true);
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Power size={24} className="text-slate-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
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
          {/* Info card */}
          <div className="p-4 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-800">
            {t('infoMsg')}
          </div>

          {/* Config */}
          <section>
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-medium text-slate-800">{t('configTitle')}</h2>
              {!editing && (
                <button
                  onClick={startEdit}
                  className="flex items-center gap-2 px-3 py-1.5 text-sm text-indigo-600 border border-indigo-200 rounded hover:bg-indigo-50"
                >
                  <Settings size={14} />
                  {t('editBtn')}
                </button>
              )}
            </div>

            {editing ? (
              <div className="border border-slate-200 rounded-lg p-5 space-y-4">
                <NumericField
                  label={t('requestDrainLabel')}
                  hint={t('requestDrainHint')}
                  value={form.requestDrainTimeoutSeconds}
                  onChange={(v) => setForm((f) => ({ ...f, requestDrainTimeoutSeconds: v }))}
                  min={5}
                  max={300}
                />
                <NumericField
                  label={t('outboxDrainLabel')}
                  hint={t('outboxDrainHint')}
                  value={form.outboxDrainTimeoutSeconds}
                  onChange={(v) => setForm((f) => ({ ...f, outboxDrainTimeoutSeconds: v }))}
                  min={10}
                  max={600}
                />
                <ToggleField
                  label={t('healthCheck503Label')}
                  hint={t('healthCheck503Hint')}
                  value={form.healthCheckReturns503OnShutdown}
                  onChange={(v) => setForm((f) => ({ ...f, healthCheckReturns503OnShutdown: v }))}
                />
                <ToggleField
                  label={t('auditShutdownLabel')}
                  hint={t('auditShutdownHint')}
                  value={form.auditShutdownEvents}
                  onChange={(v) => setForm((f) => ({ ...f, auditShutdownEvents: v }))}
                />
                <div className="flex gap-3 pt-2">
                  <button
                    onClick={() => mutation.mutate(form)}
                    disabled={mutation.isPending}
                    className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                  >
                    {mutation.isPending ? t('saving') : t('save')}
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
                <ConfigRow label={t('requestDrainLabel')} value={`${data.requestDrainTimeoutSeconds}s`} />
                <ConfigRow label={t('outboxDrainLabel')} value={`${data.outboxDrainTimeoutSeconds}s`} />
                <ConfigRow
                  label={t('healthCheck503Label')}
                  value={data.healthCheckReturns503OnShutdown ? t('enabled') : t('disabled')}
                />
                <ConfigRow
                  label={t('auditShutdownLabel')}
                  value={data.auditShutdownEvents ? t('enabled') : t('disabled')}
                />
              </div>
            )}
          </section>

          {/* Shutdown sequence */}
          <section>
            <h2 className="text-lg font-medium text-slate-800 mb-3">{t('sequenceTitle')}</h2>
            <ol className="space-y-2">
              {[1, 2, 3, 4, 5, 6].map((step) => (
                <li key={step} className="flex items-start gap-3 text-sm text-slate-700">
                  <span className="flex-shrink-0 w-6 h-6 rounded-full bg-slate-100 text-slate-600 text-xs flex items-center justify-center font-medium">
                    {step}
                  </span>
                  {t(`step${step}`)}
                </li>
              ))}
            </ol>
          </section>

          {mutation.isSuccess && (
            <div className="flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-lg p-3">
              <CheckCircle size={16} />
              {t('saveSuccess')}
            </div>
          )}
        </>
      )}
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

function NumericField({
  label,
  hint,
  value,
  onChange,
  min,
  max,
}: {
  label: string;
  hint: string;
  value: number;
  onChange: (v: number) => void;
  min: number;
  max: number;
}) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-40 px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500"
        aria-label={label}
      />
      <p className="text-xs text-slate-500">{hint}</p>
    </div>
  );
}

function ToggleField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  value: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <div className="flex items-start gap-4">
      <button
        type="button"
        role="switch"
        aria-checked={value}
        onClick={() => onChange(!value)}
        className={`mt-0.5 relative inline-flex h-5 w-9 items-center rounded-full transition-colors shrink-0 ${value ? 'bg-indigo-600' : 'bg-slate-200'}`}
        aria-label={label}
      >
        <span
          className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white transition-transform ${value ? 'translate-x-4' : 'translate-x-1'}`}
        />
      </button>
      <div>
        <p className="text-sm font-medium text-slate-700">{label}</p>
        <p className="text-xs text-slate-500">{hint}</p>
      </div>
    </div>
  );
}
