import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Power, CheckCircle2, XCircle, RefreshCw, Settings } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { TextField } from '../../../components/TextField';
import { Toggle } from '../../../components/Toggle';
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
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Power size={24} className="text-muted" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
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
            {/* Info card */}
            <div className="p-4 bg-accent/10 border border-accent/20 rounded-lg text-sm text-accent">
              {t('infoMsg')}
            </div>

            {/* Config */}
            <section>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-medium text-heading">{t('configTitle')}</h2>
                {!editing && (
                  <Button
                    variant="outline"
                    size="sm"
                    icon={<Settings size={14} />}
                    onClick={startEdit}
                  >
                    {t('editBtn')}
                  </Button>
                )}
              </div>

              {editing ? (
                <div className="border border-edge rounded-lg p-5 space-y-4">
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
                    <Button
                      variant="primary"
                      size="sm"
                      loading={mutation.isPending}
                      onClick={() => mutation.mutate(form)}
                      disabled={mutation.isPending}
                    >
                      {mutation.isPending ? t('saving') : t('save')}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setEditing(false)}
                    >
                      {t('cancel')}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="border border-edge rounded-lg divide-y divide-edge/50">
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
              <h2 className="text-lg font-medium text-heading mb-3">{t('sequenceTitle')}</h2>
              <ol className="space-y-2">
                {[1, 2, 3, 4, 5, 6].map((step) => (
                  <li key={step} className="flex items-start gap-3 text-sm text-body">
                    <span className="flex-shrink-0 w-6 h-6 rounded-full bg-elevated text-muted text-xs flex items-center justify-center font-medium">
                      {step}
                    </span>
                    {t(`step${step}`)}
                  </li>
                ))}
              </ol>
            </section>

            {mutation.isSuccess && (
              <div className="flex items-center gap-2 text-sm text-success bg-success/10 border border-success/20 rounded-lg p-3">
                <CheckCircle2 size={16} />
                {t('saveSuccess')}
              </div>
            )}
          </>
        )}
      </div>
    </PageContainer>
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
    /* DS TextField com type="number" e size="sm" — mantém label e hint */
    <TextField
      label={label}
      helperText={hint}
      type="number"
      size="sm"
      value={value}
      min={min}
      max={max}
      className="w-40"
      onChange={(e) => onChange(Number(e.target.value))}
      aria-label={label}
    />
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
    /* DS Toggle com hint posicionado abaixo do label */
    <div className="flex items-start gap-4">
      <Toggle checked={value} onChange={onChange} size="sm" />
      <div>
        <p className="text-sm font-medium text-body">{label}</p>
        <p className="text-xs text-muted">{hint}</p>
      </div>
    </div>
  );
}
