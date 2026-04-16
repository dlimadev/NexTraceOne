import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Globe,
  RefreshCw,
  XCircle,
  CheckCircle,
  AlertTriangle,
  ShieldAlert,
  TestTube,
  Clock,
} from 'lucide-react';
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Globe size={24} className="text-cyan-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-cyan-600 text-white rounded-lg hover:bg-cyan-700 transition-colors"
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
          {/* Status Banner */}
          {data.status === 'TestFailed' ? (
            <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
              <ShieldAlert size={18} className="text-red-600 mt-0.5 shrink-0" />
              <div>
                <p className="text-sm font-medium text-red-800">{t('testFailedBanner')}</p>
                {data.lastTestError && (
                  <p className="text-xs text-red-600 mt-0.5 font-mono">{data.lastTestError}</p>
                )}
              </div>
            </div>
          ) : data.status === 'TestPassed' ? (
            <div className="flex items-start gap-3 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
              <CheckCircle size={18} className="text-emerald-600 mt-0.5 shrink-0" />
              <p className="text-sm text-emerald-800">{t('testPassedBanner')}</p>
            </div>
          ) : !isConfigured ? (
            <div className="flex items-start gap-3 p-4 bg-slate-50 border border-slate-200 rounded-lg">
              <AlertTriangle size={18} className="text-slate-500 mt-0.5 shrink-0" />
              <p className="text-sm text-slate-600">{t('notConfiguredMsg')}</p>
            </div>
          ) : null}

          {/* Test Result (inline) */}
          {testResult && (
            <div
              className={`flex items-center gap-3 p-3 rounded-lg text-sm ${
                testResult.success
                  ? 'bg-emerald-50 border border-emerald-200 text-emerald-800'
                  : 'bg-red-50 border border-red-200 text-red-800'
              }`}
            >
              {testResult.success ? <CheckCircle size={16} /> : <XCircle size={16} />}
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
                <h2 className="text-lg font-medium text-slate-800">{t('configTitle')}</h2>
                <div className="flex gap-2">
                  {isConfigured && (
                    <button
                      onClick={() => {
                        setTestResult(null);
                        testMutation.mutate();
                      }}
                      disabled={testMutation.isPending}
                      className="flex items-center gap-1.5 px-3 py-1.5 text-sm text-cyan-700 border border-cyan-200 rounded hover:bg-cyan-50 disabled:opacity-50"
                    >
                      <TestTube size={14} />
                      {testMutation.isPending ? t('testing') : t('testConnection')}
                    </button>
                  )}
                  <button
                    onClick={startEdit}
                    className="px-3 py-1.5 text-sm text-cyan-600 border border-cyan-200 rounded hover:bg-cyan-50"
                  >
                    {t('editConfig')}
                  </button>
                </div>
              </div>
              <div className="border border-slate-200 rounded-lg divide-y divide-slate-100">
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
                    <span className="text-sm text-slate-500 w-56 shrink-0">{t('lastTestedLabel')}</span>
                    <span className="flex items-center gap-1.5 text-sm text-slate-600">
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
              <h2 className="text-lg font-medium text-slate-800 mb-3">{t('editConfigTitle')}</h2>
              <div className="border border-slate-200 rounded-lg p-5 space-y-4">
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
                    className="px-4 py-2 bg-cyan-600 text-white text-sm rounded-lg hover:bg-cyan-700 disabled:opacity-50"
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
            </section>
          )}
        </>
      )}
    </div>
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
  const textColor = warn ? 'text-red-600' : ok ? 'text-emerald-600' : 'text-slate-700';
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-lg font-semibold mt-1 ${textColor}`}>{value}</p>
    </div>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-slate-500 w-56 shrink-0">{label}</span>
      <span className="text-sm font-medium text-slate-800 break-all">{value}</span>
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
      <label className="block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="text"
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-cyan-500 focus:border-cyan-500"
      />
      <p className="text-xs text-slate-500">{hint}</p>
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
      <label className="block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="password"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-cyan-500 focus:border-cyan-500"
      />
      <p className="text-xs text-slate-500">{hint}</p>
    </div>
  );
}
