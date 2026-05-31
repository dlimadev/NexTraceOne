import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Shield, Settings, CheckCircle2, XCircle, RefreshCw } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type SessionSecurityConfigUpdate } from '../api/platformAdmin';

export function SessionSecurityPage() {
  const { t } = useTranslation('sessionSecurity');
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState<SessionSecurityConfigUpdate>({
    inactivityTimeoutMinutes: 480,
    maxConcurrentSessions: 5,
    requireReauthForSensitiveActions: true,
    detectAnomalousIpChange: true,
  });

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['session-security-config'],
    queryFn: platformAdminApi.getSessionSecurityConfig,
  });

  const mutation = useMutation({
    mutationFn: (config: SessionSecurityConfigUpdate) =>
      platformAdminApi.updateSessionSecurityConfig(config),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['session-security-config'] });
      setEditing(false);
    },
  });

  function startEdit() {
    if (!data) return;
    setForm({
      inactivityTimeoutMinutes: data.inactivityTimeoutMinutes,
      maxConcurrentSessions: data.maxConcurrentSessions,
      requireReauthForSensitiveActions: data.requireReauthForSensitiveActions,
      detectAnomalousIpChange: data.detectAnomalousIpChange,
    });
    setEditing(true);
  }

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Shield size={24} className="text-accent" />}
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
            {/* Summary cards */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <SummaryCard
                label={t('inactivityLabel')}
                value={`${data.inactivityTimeoutMinutes}m`}
                color="accent"
              />
              <SummaryCard
                label={t('maxSessionsLabel')}
                value={String(data.maxConcurrentSessions)}
                color="accent"
              />
              <SummaryCard
                label={t('reauthLabel')}
                value={data.requireReauthForSensitiveActions ? t('on') : t('off')}
                color={data.requireReauthForSensitiveActions ? 'success' : 'faded'}
              />
              <SummaryCard
                label={t('ipChangeLabel')}
                value={data.detectAnomalousIpChange ? t('on') : t('off')}
                color={data.detectAnomalousIpChange ? 'success' : 'faded'}
              />
            </div>

            {/* Config */}
            <section>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-medium text-heading">{t('configTitle')}</h2>
                {!editing && (
                  <button
                    onClick={startEdit}
                    className="flex items-center gap-2 px-3 py-1.5 text-sm text-accent border border-accent/20 rounded hover:bg-accent/10"
                  >
                    <Settings size={14} />
                    {t('editBtn')}
                  </button>
                )}
              </div>

              {editing ? (
                <div className="border border-edge rounded-lg p-5 space-y-4">
                  <NumericField
                    label={t('inactivityLabel')}
                    hint={t('inactivityHint')}
                    value={form.inactivityTimeoutMinutes}
                    onChange={(v) => setForm((f) => ({ ...f, inactivityTimeoutMinutes: v }))}
                    min={5}
                    max={2880}
                  />
                  <NumericField
                    label={t('maxSessionsLabel')}
                    hint={t('maxSessionsHint')}
                    value={form.maxConcurrentSessions}
                    onChange={(v) => setForm((f) => ({ ...f, maxConcurrentSessions: v }))}
                    min={1}
                    max={20}
                  />
                  <ToggleField
                    label={t('reauthLabel')}
                    hint={t('reauthHint')}
                    value={form.requireReauthForSensitiveActions}
                    onChange={(v) => setForm((f) => ({ ...f, requireReauthForSensitiveActions: v }))}
                  />
                  <ToggleField
                    label={t('ipChangeLabel')}
                    hint={t('ipChangeHint')}
                    value={form.detectAnomalousIpChange}
                    onChange={(v) => setForm((f) => ({ ...f, detectAnomalousIpChange: v }))}
                  />
                  <div className="flex gap-3 pt-2">
                    <button
                      onClick={() => mutation.mutate(form)}
                      disabled={mutation.isPending}
                      className="px-4 py-2 bg-accent text-white text-sm rounded-lg hover:bg-accent/90 disabled:opacity-50"
                    >
                      {mutation.isPending ? t('saving') : t('save')}
                    </button>
                    <button
                      onClick={() => setEditing(false)}
                      className="px-4 py-2 text-sm border border-edge rounded-lg hover:bg-elevated text-muted"
                    >
                      {t('cancel')}
                    </button>
                  </div>
                </div>
              ) : (
                <div className="border border-edge rounded-lg divide-y divide-edge/50">
                  <ConfigRow label={t('inactivityLabel')} value={`${data.inactivityTimeoutMinutes} ${t('minutes')}`} />
                  <ConfigRow label={t('maxSessionsLabel')} value={String(data.maxConcurrentSessions)} />
                  <ConfigRow
                    label={t('reauthLabel')}
                    value={data.requireReauthForSensitiveActions ? t('enabled') : t('disabled')}
                  />
                  <ConfigRow
                    label={t('ipChangeLabel')}
                    value={data.detectAnomalousIpChange ? t('enabled') : t('disabled')}
                  />
                </div>
              )}
            </section>

            {/* Sensitive actions */}
            {data.sensitiveActions.length > 0 && (
              <section>
                <h2 className="text-lg font-medium text-heading mb-3">{t('sensitiveActionsTitle')}</h2>
                <div className="flex flex-wrap gap-2">
                  {data.sensitiveActions.map((action) => (
                    <span
                      key={action}
                      className="px-2.5 py-1 bg-accent/10 text-accent text-xs rounded-full border border-accent/20"
                    >
                      {action}
                    </span>
                  ))}
                </div>
              </section>
            )}

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

function SummaryCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: 'accent' | 'success' | 'faded';
}) {
  const colorMap = {
    accent: 'text-accent',
    success: 'text-success',
    faded: 'text-faded',
  };
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
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
      <label className="block text-sm font-medium text-body">{label}</label>
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-40 px-3 py-2 border border-edge rounded-lg bg-canvas text-body text-sm focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
        aria-label={label}
      />
      <p className="text-xs text-muted">{hint}</p>
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
        className={`mt-0.5 relative inline-flex h-5 w-9 items-center rounded-full transition-colors shrink-0 ${value ? 'bg-accent' : 'bg-elevated'}`}
        aria-label={label}
      >
        <span
          className={`inline-block h-3.5 w-3.5 transform rounded-full bg-white transition-transform ${value ? 'translate-x-4' : 'translate-x-1'}`}
        />
      </button>
      <div>
        <p className="text-sm font-medium text-body">{label}</p>
        <p className="text-xs text-muted">{hint}</p>
      </div>
    </div>
  );
}
