import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  KeyRound,
  RefreshCw,
  CheckCircle2,
  AlertTriangle,
  Loader2,
  Download,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Select } from '../../../components/Select';
import { Toggle } from '../../../components/Toggle';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import {
  platformAdminApi,
  type SamlSsoStatus,
  type SamlSsoConfigUpdate,
} from '../api/platformAdmin';

// Variante de Badge por status SAML SSO
const statusVariant: Record<SamlSsoStatus, 'success' | 'warning' | 'secondary'> = {
  Enabled: 'success',
  Disabled: 'warning',
  NotConfigured: 'secondary',
};

// Opções de papel padrão JIT
const DEFAULT_ROLE_OPTIONS = ['Engineer', 'TechLead', 'Architect', 'Product', 'Auditor'].map(
  (role) => ({ value: role, label: role }),
);

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

  // Inicializa formulário a partir dos dados carregados
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

        {/* Banner de aviso sobre impacto de alterações */}
        <div className="flex items-start gap-3 p-4 bg-warning/10 border border-warning/20 rounded-lg text-sm text-warning">
          <AlertTriangle size={18} className="shrink-0 mt-0.5" />
          <span>{t('warningBanner')}</span>
        </div>

        {isLoading && <PageLoadingState message={t('loading')} />}

        {isError && (
          <PageErrorState message={t('error')} onRetry={() => void refetch()} />
        )}

        {data && currentForm && (
          <>
            {/* Status atual da integração SAML */}
            <div className="flex items-center gap-3">
              <p className="text-sm font-medium text-body">{t('labelStatus')}:</p>
              <Badge variant={statusVariant[data.status]}>
                {t(`status.${data.status}`)}
              </Badge>
            </div>

            {/* Formulário de configuração IdP */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-4">
              <h2 className="text-base font-semibold text-heading">{t('sectionConfig')}</h2>

              <TextField
                label={t('fieldEntityId')}
                value={currentForm.entityId}
                placeholder="https://nextraceone.example.com/saml/metadata"
                onChange={(e) => setForm({ ...currentForm, entityId: e.target.value })}
              />
              <TextField
                label={t('fieldSsoUrl')}
                value={currentForm.ssoUrl}
                placeholder="https://idp.example.com/saml/sso"
                onChange={(e) => setForm({ ...currentForm, ssoUrl: e.target.value })}
              />
              <TextField
                label={t('fieldSloUrl')}
                value={currentForm.sloUrl}
                placeholder="https://idp.example.com/saml/slo"
                onChange={(e) => setForm({ ...currentForm, sloUrl: e.target.value })}
              />

              <TextArea
                label={t('fieldIdpCert')}
                rows={5}
                value={currentForm.idpCertificate}
                onChange={(e) => setForm({ ...currentForm, idpCertificate: e.target.value })}
                placeholder={"-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----"}
                className="font-mono"
              />

              <div className="flex items-center gap-2">
                <Button variant="outline">
                  <Download size={14} />
                  {t('btnDownloadSpCert')}
                </Button>
              </div>
            </section>

            {/* Provisionamento JIT e papel padrão */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-4">
              <h2 className="text-base font-semibold text-heading">{t('sectionProvisioning')}</h2>

              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-body">{t('fieldJit')}</p>
                  <p className="text-xs text-muted mt-0.5">{t('fieldJitDesc')}</p>
                </div>
                <Toggle
                  checked={currentForm.jitProvisioningEnabled}
                  onChange={(v) => setForm({ ...currentForm, jitProvisioningEnabled: v })}
                  label={t('fieldJit')}
                />
              </div>

              <Select
                label={t('fieldDefaultRole')}
                options={DEFAULT_ROLE_OPTIONS}
                value={currentForm.defaultRole}
                onChange={(e) => setForm({ ...currentForm, defaultRole: e.target.value })}
              />
            </section>

            {/* Mapeamentos de atributos SAML → campos NXT */}
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
                        <td className="px-4 py-2">
                          <TextField
                            size="sm"
                            value={mapping.samlAttr}
                            onChange={(e) => {
                              const updated = [...currentForm.attributeMappings];
                              updated[idx] = { ...mapping, samlAttr: e.target.value };
                              setForm({ ...currentForm, attributeMappings: updated });
                            }}
                          />
                        </td>
                        <td className="px-4 py-2">
                          <TextField
                            size="sm"
                            value={mapping.nxtField}
                            onChange={(e) => {
                              const updated = [...currentForm.attributeMappings];
                              updated[idx] = { ...mapping, nxtField: e.target.value };
                              setForm({ ...currentForm, attributeMappings: updated });
                            }}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            {/* Teste de conectividade SAML */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-3">
              <h2 className="text-base font-semibold text-heading">{t('sectionTest')}</h2>
              <Button
                variant="outline"
                onClick={() => void handleTest()}
                disabled={isTesting}
              >
                {isTesting ? <Loader2 size={14} className="animate-spin" /> : <CheckCircle2 size={14} />}
                {t('btnTestConnection')}
              </Button>
              {testResult && (
                <div
                  className={`flex items-center gap-2 p-3 rounded-lg text-sm ${
                    testResult.success
                      ? 'bg-success/10 text-success border border-success/20'
                      : 'bg-critical/10 text-critical border border-critical/20'
                  }`}
                >
                  {testResult.success ? <CheckCircle2 size={16} /> : null}
                  {testResult.message}
                </div>
              )}
            </section>

            {/* Ações de persistência */}
            <div className="flex items-center gap-3">
              <Button
                variant="primary"
                onClick={handleSave}
                disabled={updateMutation.isPending}
              >
                {updateMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                {t('btnSave')}
              </Button>
              {updateMutation.isSuccess && (
                <span className="text-sm text-success flex items-center gap-1">
                  <CheckCircle2 size={14} />
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
