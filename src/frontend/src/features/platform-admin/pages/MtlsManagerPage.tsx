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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ShieldCheck size={24} className="text-violet-600" />
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

      {/* Revoke Confirm Dialog */}
      {revokeConfirmId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl shadow-xl p-6 max-w-sm w-full mx-4">
            <div className="flex items-center gap-3 mb-4">
              <AlertTriangle size={22} className="text-red-600" />
              <h2 className="text-lg font-semibold text-slate-900">{t('revokeConfirmTitle')}</h2>
            </div>
            <p className="text-sm text-slate-600 mb-6">{t('revokeConfirmBody')}</p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setRevokeConfirmId(null)}
                className="px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
              >
                {t('cancel')}
              </button>
              <button
                onClick={() => revokeMutation.mutate(revokeConfirmId)}
                disabled={revokeMutation.isPending}
                className="flex items-center gap-2 px-4 py-2 text-sm bg-red-600 text-white rounded-lg hover:bg-red-700 font-medium disabled:opacity-50"
              >
                {revokeMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                {t('confirmRevoke')}
              </button>
            </div>
          </div>
        </div>
      )}

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
          {/* Expiry Alert */}
          {expiringCerts.length > 0 && (
            <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg text-sm text-amber-800">
              <AlertTriangle size={18} className="shrink-0 mt-0.5" />
              <span>{t('expiryAlert', { count: expiringCerts.length })}</span>
            </div>
          )}

          {/* Overview Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatCard label={t('statTotal')} value={String(data.certificates.length)} color="violet" />
            <StatCard
              label={t('statValid')}
              value={String(data.certificates.filter((c) => c.status === 'Valid').length)}
              color="emerald"
            />
            <StatCard
              label={t('statExpiring')}
              value={String(expiringCerts.length)}
              color="amber"
            />
            <StatCard
              label={t('statExpired')}
              value={String(
                data.certificates.filter((c) => c.status === 'Expired' || c.status === 'Revoked').length,
              )}
              color="red"
            />
          </div>

          {/* mTLS Policy */}
          <section className="border border-slate-200 rounded-lg p-5 bg-white space-y-3">
            <h2 className="text-base font-semibold text-slate-800">{t('sectionPolicy')}</h2>
            <div className="flex items-center gap-3 flex-wrap">
              <p className="text-sm text-slate-600">{t('policyMode')}:</p>
              {(['Required', 'PerService', 'Disabled'] as MtlsPolicyMode[]).map((mode) => (
                <button
                  key={mode}
                  onClick={() => updatePolicyMutation.mutate(mode)}
                  disabled={updatePolicyMutation.isPending}
                  className={`px-3 py-1.5 text-xs rounded border font-medium transition-colors disabled:opacity-50 ${
                    data.policy.mode === mode
                      ? 'bg-violet-600 text-white border-violet-600'
                      : 'border-slate-300 text-slate-600 hover:bg-slate-50'
                  }`}
                >
                  {t(`policyMode.${mode}`)}
                </button>
              ))}
            </div>
          </section>

          {/* Root CA Section */}
          <section className="border border-slate-200 rounded-lg p-5 bg-white space-y-3">
            <h2 className="text-base font-semibold text-slate-800">{t('sectionRootCa')}</h2>
            <div className="flex items-center gap-4">
              <div
                className={`flex items-center gap-2 text-sm font-medium ${
                  data.policy.rootCaCertPresent ? 'text-emerald-700' : 'text-amber-700'
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
                <span className="text-xs text-slate-500">
                  {t('rootCaExpiry')}: {new Date(data.policy.rootCaCertExpiry).toLocaleDateString()}
                </span>
              )}
              <button className="flex items-center gap-2 px-3 py-1.5 text-xs border border-slate-300 rounded-lg hover:bg-slate-50">
                <Upload size={12} />
                {t('btnUploadRootCa')}
              </button>
            </div>
          </section>

          {/* Certificate Inventory */}
          <section>
            <h2 className="text-lg font-medium text-slate-800 mb-3">{t('inventoryTitle')}</h2>
            {data.certificates.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-40 text-slate-400 gap-2">
                <CheckCircle size={32} className="text-emerald-400" />
                <p className="text-sm">{t('noCerts')}</p>
              </div>
            ) : (
              <div className="border border-slate-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colService')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium hidden md:table-cell">
                        {t('colFingerprint')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colExpiry')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colStatus')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colActions')}
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
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

          <p className="text-xs text-slate-400 italic">
            {t('lastSync')}: {new Date(data.lastSyncAt).toLocaleTimeString()} · {data.simulatedNote}
          </p>
        </>
      )}
    </div>
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
    Valid: 'text-emerald-700 bg-emerald-50 border-emerald-200',
    Expiring: 'text-amber-700 bg-amber-50 border-amber-200',
    Expired: 'text-red-700 bg-red-50 border-red-200',
    Revoked: 'text-slate-600 bg-slate-100 border-slate-200',
  };

  const rowBg =
    cert.status === 'Expiring'
      ? 'bg-amber-50/40'
      : cert.status === 'Expired'
        ? 'bg-red-50/40'
        : '';

  return (
    <tr className={`hover:bg-slate-50 ${rowBg}`}>
      <td className="px-4 py-3">
        <p className="font-medium text-slate-800">{cert.serviceName}</p>
        <p className="text-xs text-slate-400">{cert.issuer}</p>
      </td>
      <td className="px-4 py-3 hidden md:table-cell">
        <span className="font-mono text-xs text-slate-500 truncate block max-w-[200px]">
          {cert.fingerprint}
        </span>
      </td>
      <td className="px-4 py-3">
        <p className="text-slate-700">{new Date(cert.validTo).toLocaleDateString()}</p>
        <p
          className={`text-xs ${
            cert.daysUntilExpiry < 0
              ? 'text-red-600'
              : cert.daysUntilExpiry < 30
                ? 'text-amber-600'
                : 'text-slate-400'
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
      className={`p-1.5 rounded hover:bg-slate-100 ${danger ? 'text-red-500 hover:bg-red-50' : 'text-slate-500'}`}
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
  color: 'violet' | 'emerald' | 'red' | 'amber';
}) {
  const colorMap = {
    violet: 'text-violet-600',
    emerald: 'text-emerald-600',
    red: 'text-red-600',
    amber: 'text-amber-600',
  };
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}
