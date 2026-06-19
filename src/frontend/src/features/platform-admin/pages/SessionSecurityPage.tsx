import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Shield, Settings, CheckCircle2, RefreshCw } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { TextField } from '../../../components/TextField';
import { Toggle } from '../../../components/Toggle';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
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

        {isLoading && <PageLoadingState message={t('loading')} />}

        {isError && (
          <PageErrorState message={t('error')} onRetry={() => void refetch()} />
        )}

        {data && (
          <>
            {/* Cards de resumo das configurações actuais */}
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

            {/* Secção de configuração editável */}
            <section>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-medium text-heading">{t('configTitle')}</h2>
                {!editing && (
                  <Button variant="outline" size="sm" onClick={startEdit}>
                    <Settings size={14} />
                    {t('editBtn')}
                  </Button>
                )}
              </div>

              {editing ? (
                <div className="border border-edge rounded-lg p-5 space-y-4">
                  {/* Campo numérico: timeout de inatividade */}
                  <div className="space-y-1">
                    <TextField
                      type="number"
                      label={t('inactivityLabel')}
                      helperText={t('inactivityHint')}
                      value={String(form.inactivityTimeoutMinutes)}
                      min={5}
                      max={2880}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, inactivityTimeoutMinutes: Number(e.target.value) }))
                      }
                      className="w-40"
                      size="sm"
                    />
                  </div>

                  {/* Campo numérico: sessões concorrentes máximas */}
                  <div className="space-y-1">
                    <TextField
                      type="number"
                      label={t('maxSessionsLabel')}
                      helperText={t('maxSessionsHint')}
                      value={String(form.maxConcurrentSessions)}
                      min={1}
                      max={20}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, maxConcurrentSessions: Number(e.target.value) }))
                      }
                      className="w-40"
                      size="sm"
                    />
                  </div>

                  {/* Toggle: reautenticação para ações sensíveis */}
                  <div className="flex items-start gap-4">
                    <Toggle
                      checked={form.requireReauthForSensitiveActions}
                      onChange={(v) => setForm((f) => ({ ...f, requireReauthForSensitiveActions: v }))}
                      size="sm"
                    />
                    <div>
                      <p className="text-sm font-medium text-body">{t('reauthLabel')}</p>
                      <p className="text-xs text-muted">{t('reauthHint')}</p>
                    </div>
                  </div>

                  {/* Toggle: deteção de mudança anómala de IP */}
                  <div className="flex items-start gap-4">
                    <Toggle
                      checked={form.detectAnomalousIpChange}
                      onChange={(v) => setForm((f) => ({ ...f, detectAnomalousIpChange: v }))}
                      size="sm"
                    />
                    <div>
                      <p className="text-sm font-medium text-body">{t('ipChangeLabel')}</p>
                      <p className="text-xs text-muted">{t('ipChangeHint')}</p>
                    </div>
                  </div>

                  <div className="flex gap-3 pt-2">
                    <Button
                      variant="primary"
                      onClick={() => mutation.mutate(form)}
                      disabled={mutation.isPending}
                    >
                      {mutation.isPending ? t('saving') : t('save')}
                    </Button>
                    <Button variant="outline" onClick={() => setEditing(false)}>
                      {t('cancel')}
                    </Button>
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

            {/* Ações sensíveis que requerem reautenticação */}
            {data.sensitiveActions.length > 0 && (
              <section>
                <h2 className="text-lg font-medium text-heading mb-3">{t('sensitiveActionsTitle')}</h2>
                <div className="flex flex-wrap gap-2">
                  {data.sensitiveActions.map((action) => (
                    <Badge key={action} variant="info">
                      {action}
                    </Badge>
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
