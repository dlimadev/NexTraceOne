import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  KeyRound,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CheckCircle,
  Loader2,
  Download,
} from 'lucide-react';
import {
  platformAdminApi,
  type SamlSsoStatus,
  type SamlSsoConfigUpdate,
} from '../api/platformAdmin';

export function SamlSsoPage() {
  const { t } = useTranslation('samlSso');
  const qc = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['saml-sso-config'],
    queryFn: platformAdminApi.getSamlSsoConfig,
  });

  const [form, setForm] = useState<SamlSsoConfigUpdate | null>(null);
  const [testResult, setTestResult] = useState<{ success: boolean; message: string } | null>(null);
  const [isTesting, setIsTesting] = useState(false);

  // Initialise form from data once loaded
  const currentForm: SamlSsoConfigUpdate | null =
    form ??
    (data
      ? {
          entityId: data.entityId,
          ssoUrl: data.ssoUrl,
          sloUrl: data.sloUrl,
          idpCertificate: data.idpCertificate,
          jitProvisioningEnabled: data.jitProvisioningEnabled,
          defaultRole: data.defaultRole,
          attributeMappings: data.attributeMappings,
        }
      : null);

  const updateMutation = useMutation({
    mutationFn: (update: SamlSsoConfigUpdate) =>
      platformAdminApi.updateSamlSsoConfig(update),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['saml-sso-config'] });
      setForm(null);
    },
  });

  const handleTest = async () => {
    setIsTesting(true);
    setTestResult(null);
    try {
      const result = await platformAdminApi.testSamlConnection();
      setTestResult(result);
    } finally {
      setIsTesting(false);
    }
  };

  const handleSave = () => {
    if (!currentForm) return;
    updateMutation.mutate(currentForm);
  };

  const statusStyle: Record<SamlSsoStatus, string> = {
    Enabled: 'text-emerald-700 bg-emerald-50 border-emerald-200',
    Disabled: 'text-amber-700 bg-amber-50 border-amber-200',
    NotConfigured: 'text-slate-600 bg-slate-100 border-slate-200',
  };

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <KeyRound size={24} className="text-violet-600" />
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

      {/* Warning banner */}
      <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-800">
        <AlertTriangle size={18} className="shrink-0 mt-0.5" />
        <span>{t('warningBanner')}</span>
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

      {data && currentForm && (
        <>
          {/* Status */}
          <div className="flex items-center gap-3">
            <p className="text-sm font-medium text-slate-700">{t('labelStatus')}:</p>
            <span className={`px-3 py-1 text-xs font-semibold rounded-full border ${statusStyle[data.status]}`}>
              {t(`status.${data.status}`)}
            </span>
          </div>

          {/* Configuration Form */}
          <section className="border border-slate-200 rounded-lg p-5 bg-white space-y-4">
            <h2 className="text-base font-semibold text-slate-800">{t('sectionConfig')}</h2>

            <FormField
              label={t('fieldEntityId')}
              value={currentForm.entityId}
              placeholder="https://nextraceone.example.com/saml/metadata"
              onChange={(v) => setForm({ ...currentForm, entityId: v })}
            />
            <FormField
              label={t('fieldSsoUrl')}
              value={currentForm.ssoUrl}
              placeholder="https://idp.example.com/saml/sso"
              onChange={(v) => setForm({ ...currentForm, ssoUrl: v })}
            />
            <FormField
              label={t('fieldSloUrl')}
              value={currentForm.sloUrl}
              placeholder="https://idp.example.com/saml/slo"
              onChange={(v) => setForm({ ...currentForm, sloUrl: v })}
            />

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('fieldIdpCert')}
              </label>
              <textarea
                rows={5}
                value={currentForm.idpCertificate}
                onChange={(e) => setForm({ ...currentForm, idpCertificate: e.target.value })}
                placeholder="-----BEGIN CERTIFICATE-----&#10;...&#10;-----END CERTIFICATE-----"
                className="w-full text-sm border border-slate-300 rounded-lg px-3 py-2 font-mono focus:outline-none focus:ring-2 focus:ring-violet-400 resize-y"
              />
            </div>

            <div className="flex items-center gap-3">
              <button className="flex items-center gap-2 px-3 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50">
                <Download size={14} />
                {t('btnDownloadSpCert')}
              </button>
            </div>
          </section>

          {/* JIT Provisioning & Default Role */}
          <section className="border border-slate-200 rounded-lg p-5 bg-white space-y-4">
            <h2 className="text-base font-semibold text-slate-800">{t('sectionProvisioning')}</h2>

            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-slate-700">{t('fieldJit')}</p>
                <p className="text-xs text-slate-500 mt-0.5">{t('fieldJitDesc')}</p>
              </div>
              <button
                onClick={() =>
                  setForm({ ...currentForm, jitProvisioningEnabled: !currentForm.jitProvisioningEnabled })
                }
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                  currentForm.jitProvisioningEnabled ? 'bg-violet-600' : 'bg-slate-200'
                }`}
              >
                <span
                  className={`inline-block h-4 w-4 transform rounded-full bg-white shadow transition-transform ${
                    currentForm.jitProvisioningEnabled ? 'translate-x-6' : 'translate-x-1'
                  }`}
                />
              </button>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('fieldDefaultRole')}
              </label>
              <select
                value={currentForm.defaultRole}
                onChange={(e) => setForm({ ...currentForm, defaultRole: e.target.value })}
                className="text-sm border border-slate-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-violet-400"
              >
                {['Engineer', 'TechLead', 'Architect', 'Product', 'Auditor'].map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </div>
          </section>

          {/* Attribute Mappings */}
          <section className="border border-slate-200 rounded-lg p-5 bg-white space-y-3">
            <h2 className="text-base font-semibold text-slate-800">{t('sectionAttrMapping')}</h2>
            <div className="border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">
                      {t('colSamlAttr')}
                    </th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">
                      {t('colNxtField')}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {currentForm.attributeMappings.map((mapping, idx) => (
                    <tr key={idx} className="hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <input
                          value={mapping.samlAttr}
                          onChange={(e) => {
                            const updated = [...currentForm.attributeMappings];
                            updated[idx] = { ...mapping, samlAttr: e.target.value };
                            setForm({ ...currentForm, attributeMappings: updated });
                          }}
                          className="text-sm border border-slate-200 rounded px-2 py-1 w-full focus:outline-none focus:ring-1 focus:ring-violet-400"
                        />
                      </td>
                      <td className="px-4 py-3">
                        <input
                          value={mapping.nxtField}
                          onChange={(e) => {
                            const updated = [...currentForm.attributeMappings];
                            updated[idx] = { ...mapping, nxtField: e.target.value };
                            setForm({ ...currentForm, attributeMappings: updated });
                          }}
                          className="text-sm border border-slate-200 rounded px-2 py-1 w-full focus:outline-none focus:ring-1 focus:ring-violet-400"
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* Test Connection */}
          <section className="border border-slate-200 rounded-lg p-5 bg-white space-y-3">
            <h2 className="text-base font-semibold text-slate-800">{t('sectionTest')}</h2>
            <button
              onClick={() => void handleTest()}
              disabled={isTesting}
              className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50 disabled:opacity-50"
            >
              {isTesting ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle size={14} />}
              {t('btnTestConnection')}
            </button>
            {testResult && (
              <div
                className={`flex items-center gap-2 p-3 rounded-lg text-sm ${
                  testResult.success
                    ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                    : 'bg-red-50 text-red-700 border border-red-200'
                }`}
              >
                {testResult.success ? <CheckCircle size={16} /> : <XCircle size={16} />}
                {testResult.message}
              </div>
            )}
          </section>

          {/* Actions */}
          <div className="flex items-center gap-3">
            <button
              onClick={handleSave}
              disabled={updateMutation.isPending}
              className="flex items-center gap-2 px-5 py-2 text-sm bg-violet-600 text-white rounded-lg hover:bg-violet-700 font-medium disabled:opacity-50"
            >
              {updateMutation.isPending && <Loader2 size={14} className="animate-spin" />}
              {t('btnSave')}
            </button>
            {updateMutation.isSuccess && (
              <span className="text-sm text-emerald-600 flex items-center gap-1">
                <CheckCircle size={14} />
                {t('saveSuccess')}
              </span>
            )}
          </div>

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
        </>
      )}
    </div>
  );
}

function FormField({
  label,
  value,
  placeholder,
  onChange,
}: {
  label: string;
  value: string;
  placeholder?: string;
  onChange: (val: string) => void;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-700 mb-1">{label}</label>
      <input
        type="text"
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="w-full text-sm border border-slate-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-violet-400"
      />
    </div>
  );
}
