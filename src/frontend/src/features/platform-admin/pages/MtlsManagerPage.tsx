import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ShieldCheck,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CheckCircle,
  Loader2,
  Upload,
  Eye,
  Download,
  Ban,
  RotateCcw,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import {
  platformAdminApi,
  type MtlsCertificate,
  type CertStatus,
  type MtlsPolicyMode,
} from '../api/platformAdmin';

export function MtlsManagerPage() {
  const { t } = useTranslation('mtlsManager');
  const qc = useQueryClient();
  const [revokeConfirmId, setRevokeConfirmId] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['mtls-manager'],
    queryFn: platformAdminApi.getMtlsManager,
  });

  const revokeMutation = useMutation({
    mutationFn: (certId: string) => platformAdminApi.revokeMtlsCert(certId),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['mtls-manager'] });
      setRevokeConfirmId(null);
    },
  });

  const updatePolicyMutation = useMutation({
    mutationFn: (mode: MtlsPolicyMode) =>
      platformAdminApi.updateMtlsPolicy({ mode }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['mtls-manager'] });
    },
  });

  const expiringCerts = data?.certificates.filter((c) => c.daysUntilExpiry < 30 && c.status !== 'Revoked') ?? [];

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<ShieldCheck size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Revoke Confirm Dialog */}
        {revokeConfirmId && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="bg-card border border-edge rounded-xl shadow-xl p-6 max-w-sm w-full mx-4">
              <div className="flex items-center gap-3 mb-4">
                <AlertTriangle size={22} className="text-critical" />
                <h2 className="text-lg font-semibold text-heading">{t('revokeConfirmTitle')}</h2>
              </div>
              <p className="text-sm text-muted mb-6">{t('revokeConfirmBody')}</p>
              <div className="flex gap-3 justify-end">
                <button
                  onClick={() => setRevokeConfirmId(null)}
                  className="px-4 py-2 text-sm border border-edge rounded-lg hover:bg-elevated text-muted"
                >
                  {t('cancel')}
                </button>
                <button
                  onClick={() => revokeMutation.mutate(revokeConfirmId)}
                  disabled={revokeMutation.isPending}
                  className="flex items-center gap-2 px-4 py-2 text-sm bg-critical text-white rounded-lg hover:bg-critical/90 font-medium disabled:opacity-50"
                >
                  {revokeMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                  {t('confirmRevoke')}
                </button>
              </div>
            </div>
          </div>
        )}

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
            {/* Expiry Alert */}
            {expiringCerts.length > 0 && (
              <div className="flex items-start gap-3 p-4 bg-warning/10 border border-warning/20 rounded-lg text-sm text-warning">
                <AlertTriangle size={18} className="shrink-0 mt-0.5" />
                <span>{t('expiryAlert', { count: expiringCerts.length })}</span>
              </div>
            )}

            {/* Overview Stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatCard label={t('statTotal')} value={String(data.certificates.length)} color="accent" />
              <StatCard
                label={t('statValid')}
                value={String(data.certificates.filter((c) => c.status === 'Valid').length)}
                color="success"
              />
              <StatCard
                label={t('statExpiring')}
                value={String(expiringCerts.length)}
                color="warning"
              />
              <StatCard
                label={t('statExpired')}
                value={String(
                  data.certificates.filter((c) => c.status === 'Expired' || c.status === 'Revoked').length,
                )}
                color="critical"
              />
            </div>

            {/* mTLS Policy */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-3">
              <h2 className="text-base font-semibold text-heading">{t('sectionPolicy')}</h2>
              <div className="flex items-center gap-3 flex-wrap">
                <p className="text-sm text-muted">{t('policyMode')}:</p>
                {(['Required', 'PerService', 'Disabled'] as MtlsPolicyMode[]).map((mode) => (
                  <button
                    key={mode}
                    onClick={() => updatePolicyMutation.mutate(mode)}
                    disabled={updatePolicyMutation.isPending}
                    className={`px-3 py-1.5 text-xs rounded border font-medium transition-colors disabled:opacity-50 ${
                      data.policy.mode === mode
                        ? 'bg-accent text-white border-accent'
                        : 'border-edge text-muted hover:bg-elevated'
                    }`}
                  >
                    {t(`policyMode.${mode}`)}
                  </button>
                ))}
              </div>
            </section>

            {/* Root CA Section */}
            <section className="border border-edge rounded-lg p-5 bg-card space-y-3">
              <h2 className="text-base font-semibold text-heading">{t('sectionRootCa')}</h2>
              <div className="flex items-center gap-4">
                <div
                  className={`flex items-center gap-2 text-sm font-medium ${
                    data.policy.rootCaCertPresent ? 'text-success' : 'text-warning'
                  }`}
                >
                  {data.policy.rootCaCertPresent ? (
                    <CheckCircle size={16} />
                  ) : (
                    <AlertTriangle size={16} />
                  )}
                  {data.policy.rootCaCertPresent ? t('rootCaPresent') : t('rootCaAbsent')}
                </div>
                {data.policy.rootCaCertExpiry && (
                  <span className="text-xs text-muted">
                    {t('rootCaExpiry')}: {new Date(data.policy.rootCaCertExpiry).toLocaleDateString()}
                  </span>
                )}
                <button className="flex items-center gap-2 px-3 py-1.5 text-xs border border-edge rounded-lg hover:bg-elevated text-muted">
                  <Upload size={12} />
                  {t('btnUploadRootCa')}
                </button>
              </div>
            </section>

            {/* Certificate Inventory */}
            <section>
              <h2 className="text-lg font-medium text-heading mb-3">{t('inventoryTitle')}</h2>
              {data.certificates.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-40 text-faded gap-2">
                  <CheckCircle size={32} className="text-success" />
                  <p className="text-sm">{t('noCerts')}</p>
                </div>
              ) : (
                <div className="border border-edge rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-elevated border-b border-edge">
                      <tr>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colService')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium hidden md:table-cell">
                          {t('colFingerprint')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colExpiry')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colStatus')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colActions')}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge/50">
                      {data.certificates.map((cert) => (
                        <CertRow
                          key={cert.id}
                          cert={cert}
                          onRevoke={() => setRevokeConfirmId(cert.id)}
                          t={t}
                        />
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>

            <p className="text-xs text-faded italic">
              {t('lastSync')}: {new Date(data.lastSyncAt).toLocaleTimeString()} · {data.simulatedNote}
            </p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function CertRow({
  cert,
  onRevoke,
  t,
}: {
  cert: MtlsCertificate;
  onRevoke: () => void;
  t: (key: string) => string;
}) {
  const statusStyle: Record<CertStatus, string> = {
    Valid: 'text-success bg-success/10 border-success/20',
    Expiring: 'text-warning bg-warning/10 border-warning/20',
    Expired: 'text-critical bg-critical/10 border-critical/20',
    Revoked: 'text-muted bg-elevated border-edge',
  };

  const rowBg =
    cert.status === 'Expiring'
      ? 'bg-warning/5'
      : cert.status === 'Expired'
        ? 'bg-critical/5'
        : '';

  return (
    <tr className={`hover:bg-elevated ${rowBg}`}>
      <td className="px-4 py-3">
        <p className="font-medium text-heading">{cert.serviceName}</p>
        <p className="text-xs text-faded">{cert.issuer}</p>
      </td>
      <td className="px-4 py-3 hidden md:table-cell">
        <span className="font-mono text-xs text-muted truncate block max-w-[200px]">
          {cert.fingerprint}
        </span>
      </td>
      <td className="px-4 py-3">
        <p className="text-body">{new Date(cert.validTo).toLocaleDateString()}</p>
        <p
          className={`text-xs ${
            cert.daysUntilExpiry < 0
              ? 'text-critical'
              : cert.daysUntilExpiry < 30
                ? 'text-warning'
                : 'text-faded'
          }`}
        >
          {cert.daysUntilExpiry < 0
            ? `${Math.abs(cert.daysUntilExpiry)} dias atrás`
            : `${cert.daysUntilExpiry} dias restantes`}
        </p>
      </td>
      <td className="px-4 py-3">
        <span className={`px-2 py-0.5 text-xs font-medium rounded border ${statusStyle[cert.status]}`}>
          {t(`certStatus.${cert.status}`)}
        </span>
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-1">
          <ActionButton icon={<Eye size={13} />} label={t('actionView')} />
          <ActionButton icon={<Download size={13} />} label={t('actionDownload')} />
          <ActionButton icon={<RotateCcw size={13} />} label={t('actionRenew')} />
          {cert.status !== 'Revoked' && (
            <ActionButton
              icon={<Ban size={13} />}
              label={t('actionRevoke')}
              danger
              onClick={onRevoke}
            />
          )}
        </div>
      </td>
    </tr>
  );
}

function ActionButton({
  icon,
  label,
  danger,
  onClick,
}: {
  icon: React.ReactNode;
  label: string;
  danger?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      title={label}
      onClick={onClick}
      className={`p-1.5 rounded hover:bg-elevated ${danger ? 'text-critical hover:bg-critical/10' : 'text-muted'}`}
    >
      {icon}
    </button>
  );
}

function StatCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: 'accent' | 'success' | 'critical' | 'warning';
}) {
  const colorMap = {
    accent: 'text-accent',
    success: 'text-success',
    critical: 'text-critical',
    warning: 'text-warning',
  };
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}
