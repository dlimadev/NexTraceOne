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
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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
    Enabled: 'text-success bg-success/10 border-success/20',
    Disabled: 'text-warning bg-warning/10 border-warning/20',
    NotConfigured: 'text-muted bg-elevated border-edge',
  };

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<KeyRound size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Warning banner */}
        <div className="flex items-start gap-3 p-4 bg-warning/10 border border-warning/20 rounded-lg text-sm text-warning">
          <AlertTriangle size={18} className="shrink-0 mt-0.5" />
          <span>{t('warningBanner')}</span>
        </div>

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

        {data && currentForm && (
          <>
            {/* Status */}
            <div className="flex items-center gap-3">
              <p className="text-sm font-medium text-body">{t('labelStatus')}:</p>
              <span className={`px-3 py-1 text-xs font-semibold rounded-full border ${statusStyle[data.status]}`}>
                {t(`status.${data.status}`)}
              </span>
            </div>

            {/* Configuration Form */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-4">
              <h2 className="text-base font-semibold text-heading">{t('sectionConfig')}</h2>

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
                <label className="block text-sm font-medium text-body mb-1">
                  {t('fieldIdpCert')}
                </label>
                <textarea
                  rows={5}
                  value={currentForm.idpCertificate}
                  onChange={(e) => setForm({ ...currentForm, idpCertificate: e.target.value })}
                  placeholder="-----BEGIN CERTIFICATE-----&#10;...&#10;-----END CERTIFICATE-----"
                  className="w-full text-sm border border-edge rounded-lg bg-canvas px-3 py-2 font-mono text-body focus:outline-none focus:ring-2 focus:ring-accent/40 resize-y"
                />
              </div>

              <div className="flex items-center gap-3">
                <button className="flex items-center gap-2 px-3 py-2 text-sm border border-edge rounded-lg hover:bg-elevated text-muted">
                  <Download size={14} />
                  {t('btnDownloadSpCert')}
                </button>
              </div>
            </section>

            {/* JIT Provisioning & Default Role */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-4">
              <h2 className="text-base font-semibold text-heading">{t('sectionProvisioning')}</h2>

              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-body">{t('fieldJit')}</p>
                  <p className="text-xs text-muted mt-0.5">{t('fieldJitDesc')}</p>
                </div>
                <button
                  onClick={() =>
                    setForm({ ...currentForm, jitProvisioningEnabled: !currentForm.jitProvisioningEnabled })
                  }
                  className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                    currentForm.jitProvisioningEnabled ? 'bg-accent' : 'bg-elevated'
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
                <label className="block text-sm font-medium text-body mb-1">
                  {t('fieldDefaultRole')}
                </label>
                <select
                  value={currentForm.defaultRole}
                  onChange={(e) => setForm({ ...currentForm, defaultRole: e.target.value })}
                  className="text-sm border border-edge rounded-lg bg-canvas px-3 py-2 text-body focus:outline-none focus:ring-2 focus:ring-accent/40"
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
            <section className="border border-edge rounded-lg p-5 bg-card space-y-3">
              <h2 className="text-base font-semibold text-heading">{t('sectionAttrMapping')}</h2>
              <div className="border border-edge rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-elevated border-b border-edge">
                    <tr>
                      <th className="text-left px-4 py-3 text-muted font-medium">
                        {t('colSamlAttr')}
                      </th>
                      <th className="text-left px-4 py-3 text-muted font-medium">
                        {t('colNxtField')}
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge/50">
                    {currentForm.attributeMappings.map((mapping, idx) => (
                      <tr key={idx} className="hover:bg-elevated">
                        <td className="px-4 py-3">
                          <input
                            value={mapping.samlAttr}
                            onChange={(e) => {
                              const updated = [...currentForm.attributeMappings];
                              updated[idx] = { ...mapping, samlAttr: e.target.value };
                              setForm({ ...currentForm, attributeMappings: updated });
                            }}
                            className="text-sm border border-edge rounded bg-canvas px-2 py-1 w-full text-body focus:outline-none focus:ring-1 focus:ring-accent/40"
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
                            className="text-sm border border-edge rounded bg-canvas px-2 py-1 w-full text-body focus:outline-none focus:ring-1 focus:ring-accent/40"
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            {/* Test Connection */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-3">
              <h2 className="text-base font-semibold text-heading">{t('sectionTest')}</h2>
              <button
                onClick={() => void handleTest()}
                disabled={isTesting}
                className="flex items-center gap-2 px-4 py-2 text-sm border border-edge rounded-lg hover:bg-elevated disabled:opacity-50 text-muted"
              >
                {isTesting ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle size={14} />}
                {t('btnTestConnection')}
              </button>
              {testResult && (
                <div
                  className={`flex items-center gap-2 p-3 rounded-lg text-sm ${
                    testResult.success
                      ? 'bg-success/10 text-success border border-success/20'
                      : 'bg-critical/10 text-critical border border-critical/20'
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
                className="flex items-center gap-2 px-5 py-2 text-sm bg-accent text-white rounded-lg hover:bg-accent/90 font-medium disabled:opacity-50"
              >
                {updateMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                {t('btnSave')}
              </button>
              {updateMutation.isSuccess && (
                <span className="text-sm text-success flex items-center gap-1">
                  <CheckCircle size={14} />
                  {t('saveSuccess')}
                </span>
              )}
            </div>

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
          </>
        )}
      </div>
    </PageContainer>
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
      <label className="block text-sm font-medium text-body mb-1">{label}</label>
      <input
        type="text"
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className="w-full text-sm border border-edge rounded-lg bg-canvas px-3 py-2 text-body focus:outline-none focus:ring-2 focus:ring-accent/40"
      />
    </div>
  );
}
