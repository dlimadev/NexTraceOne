import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Globe,
  RefreshCw,
  XCircle,
  CheckCircle2,
  AlertTriangle,
  ShieldAlert,
  TestTube,
  Clock,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type ProxyConfigUpdate } from '../api/platformAdmin';

export function ProxyConfigPage() {
  const { t } = useTranslation('proxyConfig');
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({
    proxyUrl: '',
    bypassList: '',
    username: '',
    password: '',
    customCaCertificatePath: '',
  });
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['proxy-config'],
    queryFn: platformAdminApi.getProxyConfig,
  });

  const updateMutation = useMutation({
    mutationFn: (payload: ProxyConfigUpdate) => platformAdminApi.updateProxyConfig(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['proxy-config'] });
      setEditing(false);
    },
  });

  const testMutation = useMutation({
    mutationFn: () => platformAdminApi.testProxyConnectivity(),
    onSuccess: (result) => {
      setTestResult({
        success: result.success,
        message: result.success
          ? `${t('testPassedMsg')} (${result.durationMs} ms)`
          : (result.error ?? t('testFailedMsg')),
      });
    },
    onError: () => {
      setTestResult({ success: false, message: t('testFailedMsg') });
    },
  });

  function startEdit() {
    if (!data) return;
    setForm({
      proxyUrl: data.proxyUrl ?? '',
      bypassList: data.bypassList.join(', '),
      username: data.username ?? '',
      password: '',
      customCaCertificatePath: data.customCaCertificatePath ?? '',
    });
    setEditing(true);
  }

  function saveConfig() {
    updateMutation.mutate({
      proxyUrl: form.proxyUrl || undefined,
      bypassList: form.bypassList
        .split(',')
        .map((s) => s.trim())
        .filter(Boolean),
      username: form.username || undefined,
      password: form.password || undefined,
      customCaCertificatePath: form.customCaCertificatePath || undefined,
    });
  }

  const isConfigured = data?.status !== 'NotConfigured';

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Globe size={24} className="text-accent" />}
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
            {/* Status Banner */}
            {data.status === 'TestFailed' ? (
              <div className="flex items-start gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg">
                <ShieldAlert size={18} className="text-critical mt-0.5 shrink-0" />
                <div>
                  <p className="text-sm font-medium text-critical">{t('testFailedBanner')}</p>
                  {data.lastTestError && (
                    <p className="text-xs text-critical mt-0.5 font-mono">{data.lastTestError}</p>
                  )}
                </div>
              </div>
            ) : data.status === 'TestPassed' ? (
              <div className="flex items-start gap-3 p-4 bg-success/10 border border-success/20 rounded-lg">
                <CheckCircle2 size={18} className="text-success mt-0.5 shrink-0" />
                <p className="text-sm text-success">{t('testPassedBanner')}</p>
              </div>
            ) : !isConfigured ? (
              <div className="flex items-start gap-3 p-4 bg-elevated border border-edge rounded-lg">
                <AlertTriangle size={18} className="text-muted mt-0.5 shrink-0" />
                <p className="text-sm text-muted">{t('notConfiguredMsg')}</p>
              </div>
            ) : null}

            {/* Test Result (inline) */}
            {testResult && (
              <div
                className={`flex items-center gap-3 p-3 rounded-lg text-sm ${
                  testResult.success
                    ? 'bg-success/10 border border-success/20 text-success'
                    : 'bg-critical/10 border border-critical/20 text-critical'
                }`}
              >
                {testResult.success ? <CheckCircle2 size={16} /> : <XCircle size={16} />}
                {testResult.message}
              </div>
            )}

            {/* Status Cards */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatusCard
                label={t('statusLabel')}
                value={t(`status.${data.status}`)}
                ok={data.status === 'TestPassed'}
                warn={data.status === 'TestFailed'}
              />
              <StatusCard
                label={t('proxyConfiguredLabel')}
                value={isConfigured ? t('yes') : t('no')}
                ok={isConfigured}
              />
              <StatusCard
                label={t('caConfiguredLabel')}
                value={data.hasCaCertificate ? t('yes') : t('no')}
                ok={data.hasCaCertificate}
              />
              <StatusCard
                label={t('authConfiguredLabel')}
                value={data.hasPassword ? t('yes') : t('no')}
              />
            </div>

            {/* Current Config */}
            {!editing && (
              <section>
                <div className="flex items-center justify-between mb-3">
                  <h2 className="text-lg font-medium text-heading">{t('configTitle')}</h2>
                  <div className="flex gap-2">
                    {isConfigured && (
                      <button
                        onClick={() => {
                          setTestResult(null);
                          testMutation.mutate();
                        }}
                        disabled={testMutation.isPending}
                        className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-accent border border-accent/20 rounded hover:bg-accent/10 disabled:opacity-50"
                      >
                        <TestTube size={14} />
                        {testMutation.isPending ? t('testing') : t('testConnection')}
                      </button>
                    )}
                    <button
                      onClick={startEdit}
                      className="px-3 py-1.5 text-sm text-accent border border-accent/20 rounded hover:bg-accent/10"
                    >
                      {t('editConfig')}
                    </button>
                  </div>
                </div>
                <div className="border border-edge rounded-lg divide-y divide-edge/50">
                  <ConfigRow label={t('proxyUrlLabel')} value={data.proxyUrl ?? t('notSet')} />
                  <ConfigRow
                    label={t('bypassListLabel')}
                    value={data.bypassList.length > 0 ? data.bypassList.join(', ') : t('notSet')}
                  />
                  <ConfigRow
                    label={t('usernameLabel')}
                    value={data.username ?? t('notSet')}
                  />
                  <ConfigRow
                    label={t('passwordLabel')}
                    value={data.hasPassword ? '••••••••' : t('notSet')}
                  />
                  <ConfigRow
                    label={t('caCertLabel')}
                    value={data.customCaCertificatePath ?? t('notSet')}
                  />
                  {data.lastTestedAt && (
                    <div className="flex px-4 py-3 gap-4 items-center">
                      <span className="text-sm text-muted w-56 shrink-0">{t('lastTestedLabel')}</span>
                      <span className="flex items-center gap-1.5 text-sm text-body">
                        <Clock size={14} />
                        {data.lastTestedAt}
                      </span>
                    </div>
                  )}
                </div>
              </section>
            )}

            {/* Edit Form */}
            {editing && (
              <section>
                <h2 className="text-lg font-medium text-heading mb-3">{t('editConfigTitle')}</h2>
                <div className="border border-edge rounded-lg p-5 space-y-4">
                  <TextField
                    label={t('proxyUrlLabel')}
                    hint={t('proxyUrlHint')}
                    placeholder="http://proxy.acme.com:3128"
                    value={form.proxyUrl}
                    onChange={(v) => setForm((f) => ({ ...f, proxyUrl: v }))}
                  />
                  <TextField
                    label={t('bypassListLabel')}
                    hint={t('bypassListHint')}
                    placeholder="localhost, *.acme.internal"
                    value={form.bypassList}
                    onChange={(v) => setForm((f) => ({ ...f, bypassList: v }))}
                  />
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <TextField
                      label={t('usernameLabel')}
                      hint={t('usernameHint')}
                      placeholder="svc-nextraceone"
                      value={form.username}
                      onChange={(v) => setForm((f) => ({ ...f, username: v }))}
                    />
                    <PasswordField
                      label={t('passwordLabel')}
                      hint={t('passwordHint')}
                      value={form.password}
                      onChange={(v) => setForm((f) => ({ ...f, password: v }))}
                    />
                  </div>
                  <TextField
                    label={t('caCertLabel')}
                    hint={t('caCertHint')}
                    placeholder="/etc/nextraceone/custom-ca.pem"
                    value={form.customCaCertificatePath}
                    onChange={(v) => setForm((f) => ({ ...f, customCaCertificatePath: v }))}
                  />
                  <div className="flex gap-3 pt-2">
                    <button
                      onClick={saveConfig}
                      disabled={updateMutation.isPending}
                      className="px-4 py-2 bg-accent text-white text-sm rounded-lg hover:bg-accent/90 disabled:opacity-50"
                    >
                      {updateMutation.isPending ? t('saving') : t('save')}
                    </button>
                    <button
                      onClick={() => setEditing(false)}
                      className="px-4 py-2 text-sm border border-edge rounded-lg hover:bg-elevated text-muted"
                    >
                      {t('cancel')}
                    </button>
                  </div>
                </div>
              </section>
            )}
          </>
        )}
      </div>
    </PageContainer>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function StatusCard({
  label,
  value,
  ok,
  warn,
}: {
  label: string;
  value: string;
  ok?: boolean;
  warn?: boolean;
}) {
  const textColor = warn ? 'text-critical' : ok ? 'text-success' : 'text-body';
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-lg font-semibold mt-1 ${textColor}`}>{value}</p>
    </div>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-muted w-56 shrink-0">{label}</span>
      <span className="text-sm font-medium text-heading break-all">{value}</span>
    </div>
  );
}

function TextField({
  label,
  hint,
  placeholder,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  placeholder?: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-body">{label}</label>
      <input
        type="text"
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-edge rounded-lg bg-canvas text-body text-sm focus:outline-none focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
      />
      <p className="text-xs text-muted">{hint}</p>
    </div>
  );
}

function PasswordField({
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
        type="password"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-edge rounded-lg bg-canvas text-body text-sm focus:outline-none focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
      />
      <p className="text-xs text-muted">{hint}</p>
    </div>
  );
}
