import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  FileText,
  Shield,
  Lock,
  Unlock,
  CheckCircle,
  XCircle,
  Bot,
  Clock,
  Fingerprint,
  History,
  GitCompare,
  AlertTriangle,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { contractsApi } from '../api/contracts';
import { AssistantPanel } from '../../ai-hub/components/AssistantPanel';

/** Variantes visuais para badges de protocolo. */
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Protobuf: 'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

/** Variantes visuais para badges de estado do ciclo de vida. */
const lifecycleColors: Record<string, string> = {
  Draft: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
  InReview: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Approved: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Locked: 'bg-purple-900/40 text-purple-300 border border-purple-700/50',
  Deprecated: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Sunset: 'bg-red-900/40 text-red-300 border border-red-700/50',
  Retired: 'bg-slate-900/40 text-slate-400 border border-slate-700/50',
};

/** Página de detalhe de uma versão de contrato — governança e proveniência. */
export function ContractDetailPage() {
  const { t } = useTranslation();
  const { contractVersionId } = useParams<{ contractVersionId: string }>();

  const { data: detail, isLoading, isError } = useQuery({
    queryKey: ['contract-detail', contractVersionId],
    queryFn: () => contractsApi.getDetail(contractVersionId!),
    enabled: !!contractVersionId,
  });

  const { data: versionHistory } = useQuery({
    queryKey: ['contract-history', detail?.apiAssetId],
    queryFn: () => contractsApi.getHistory(detail!.apiAssetId),
    enabled: !!detail?.apiAssetId,
  });

  const { data: violations } = useQuery({
    queryKey: ['contract-violations', contractVersionId],
    queryFn: () => contractsApi.listRuleViolations(contractVersionId!),
    enabled: !!contractVersionId,
  });

  if (isLoading) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <div className="flex items-center justify-center py-24">
          <p className="text-sm text-muted">{t('common.loading')}</p>
        </div>
      </div>
    );
  }

  if (isError || !detail) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <EmptyState
          icon={<FileText size={24} />}
          title={t('common.error')}
          description={t('common.errorDescription')}
          action={
            <Link to="/contracts" className="text-sm text-accent hover:underline">
              {t('contractGov.detail.backToList')}
            </Link>
          }
        />
      </div>
    );
  }

  const otherVersions = versionHistory?.filter((v) => v.id !== contractVersionId) ?? [];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* ── Navegação ── */}
      <Link
        to="/contracts"
        className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-accent transition-colors mb-4"
      >
        <ArrowLeft size={14} />
        {t('contractGov.detail.backToList')}
      </Link>

      {/* ── Cabeçalho ── */}
      <div className="mb-6">
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold text-heading">{detail.apiAssetId}</h1>
          <span className="text-sm font-mono text-muted">v{detail.semVer}</span>
          <span
            className={`inline-flex text-xs px-2 py-0.5 rounded-full ${protocolColors[detail.protocol] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}
          >
            {t(`contractGov.badges.protocols.${detail.protocol}`, detail.protocol)}
          </span>
          <span
            className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[detail.lifecycleState] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}
          >
            {t(`contractGov.badges.lifecycle.${detail.lifecycleState}`, detail.lifecycleState)}
          </span>
        </div>
        <p className="text-muted mt-1">{t('contractGov.detail.title')}</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* ── Coluna principal (2/3) ── */}
        <div className="lg:col-span-2 flex flex-col gap-6">
          {/* ── Visão Geral ── */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <FileText size={18} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('contractGov.detail.overview')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              <dl className="space-y-4">
                <DetailRow label={t('contractGov.detail.apiAssetId')} value={detail.apiAssetId} />
                <DetailRow label={t('contractGov.detail.version')} value={detail.semVer} mono />
                <DetailRow label={t('contractGov.detail.protocol')} value={t(`contractGov.badges.protocols.${detail.protocol}`, detail.protocol)} />
                <DetailRow label={t('contractGov.detail.format')} value={detail.format} />
                <DetailRow label={t('contractGov.detail.importedFrom')} value={detail.importedFrom ?? t('contractGov.detail.notAvailable')} />
                <div>
                  <dt className="text-xs text-muted mb-1">{t('contractGov.detail.specPreview')}</dt>
                  <dd className="bg-elevated border border-edge rounded-md p-3 max-h-48 overflow-y-auto">
                    <pre className="text-xs text-heading font-mono whitespace-pre-wrap break-all">
                      {detail.specContent
                        ? detail.specContent.length > 2000
                          ? detail.specContent.slice(0, 2000) + '…'
                          : detail.specContent
                        : t('contractGov.detail.notAvailable')}
                    </pre>
                  </dd>
                </div>
              </dl>
            </CardBody>
          </Card>

          {/* ── Governança ── */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Shield size={18} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('contractGov.detail.governance')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              <dl className="space-y-4">
                <DetailRow
                  label={t('contractGov.detail.lifecycleState')}
                  value={t(`contractGov.badges.lifecycle.${detail.lifecycleState}`, detail.lifecycleState)}
                />
                <div>
                  <dt className="text-xs text-muted mb-1">{t('contractGov.detail.isLocked')}</dt>
                  <dd className="flex items-center gap-1.5 text-sm">
                    {detail.isLocked ? (
                      <>
                        <Lock size={14} className="text-purple-400" />
                        <span className="text-purple-300">{t('contractGov.badges.lockedLabel')}</span>
                      </>
                    ) : (
                      <>
                        <Unlock size={14} className="text-muted" />
                        <span className="text-muted">{t('contractGov.badges.unlockedLabel')}</span>
                      </>
                    )}
                  </dd>
                </div>
                {detail.lockedAt && (
                  <DetailRow label={t('contractGov.detail.lockedAt')} value={new Date(detail.lockedAt).toLocaleString()} />
                )}
                {detail.lockedBy && (
                  <DetailRow label={t('contractGov.detail.lockedBy')} value={detail.lockedBy} />
                )}
                <div>
                  <dt className="text-xs text-muted mb-1">{t('contractGov.detail.isSigned')}</dt>
                  <dd className="flex items-center gap-1.5 text-sm">
                    {detail.signedBy ? (
                      <>
                        <CheckCircle size={14} className="text-emerald-400" />
                        <span className="text-emerald-300">{t('contractGov.badges.signed')}</span>
                      </>
                    ) : (
                      <>
                        <XCircle size={14} className="text-muted" />
                        <span className="text-muted">{t('contractGov.badges.unsigned')}</span>
                      </>
                    )}
                  </dd>
                </div>
                {detail.fingerprint && (
                  <DetailRow
                    label={t('contractGov.detail.fingerprint')}
                    value={detail.fingerprint}
                    mono
                  />
                )}
                {detail.algorithm && (
                  <DetailRow label={t('contractGov.detail.algorithm')} value={detail.algorithm} />
                )}
                {detail.deprecationNotice && (
                  <DetailRow label={t('contractGov.detail.deprecationNotice')} value={detail.deprecationNotice} />
                )}
                {detail.sunsetDate && (
                  <DetailRow label={t('contractGov.detail.sunsetDate')} value={new Date(detail.sunsetDate).toLocaleDateString()} />
                )}
                <DetailRow
                  label={t('contractGov.columns.createdAt')}
                  value={new Date(detail.createdAt).toLocaleString()}
                />
              </dl>
            </CardBody>
          </Card>

          {/* ── Proveniência ── */}
          {detail.provenance && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Fingerprint size={18} className="text-accent" />
                  <h2 className="text-base font-semibold text-heading">{t('contractGov.detail.provenance')}</h2>
                </div>
              </CardHeader>
              <CardBody>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  <dl className="space-y-4">
                    <DetailRow label={t('contractGov.detail.importedBy')} value={detail.provenance.importedBy} />
                  </dl>
                  <dl className="space-y-4">
                    <DetailRow label={t('contractGov.detail.parsedBy')} value={detail.provenance.parserUsed} />
                  </dl>
                  <dl className="space-y-4">
                    <div>
                      <dt className="text-xs text-muted mb-1">{t('contractGov.detail.aiGenerated')}</dt>
                      <dd className="flex items-center gap-1.5 text-sm">
                        {detail.provenance.isAiGenerated ? (
                          <>
                            <Bot size={14} className="text-accent" />
                            <span className="text-accent">{t('contractGov.detail.yes')}</span>
                          </>
                        ) : (
                          <>
                            <Clock size={14} className="text-muted" />
                            <span className="text-muted">{t('contractGov.detail.no')}</span>
                          </>
                        )}
                      </dd>
                    </div>
                  </dl>
                </div>
              </CardBody>
            </Card>
          )}
        </div>

        {/* ── Barra lateral (1/3) ── */}
        <div className="flex flex-col gap-6">
          {/* ── Versões ── */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <History size={16} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('contractGov.detail.versionHistory')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              {otherVersions.length === 0 ? (
                <p className="text-xs text-muted">{t('contractGov.detail.noOtherVersions')}</p>
              ) : (
                <ul className="space-y-2">
                  {otherVersions.slice(0, 10).map((v) => (
                    <li key={v.id}>
                      <Link
                        to={`/contracts/${v.id}`}
                        className="flex items-center justify-between p-2 rounded-md hover:bg-elevated border border-transparent hover:border-edge transition-colors"
                      >
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-mono text-heading">v{v.version}</span>
                          <span className={`inline-flex text-[10px] px-1.5 py-0.5 rounded-full ${lifecycleColors[v.lifecycleState] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                            {t(`contractGov.badges.lifecycle.${v.lifecycleState}`, v.lifecycleState)}
                          </span>
                        </div>
                        <span className="text-[10px] text-muted">{new Date(v.createdAt).toLocaleDateString()}</span>
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
              <div className="mt-3 pt-3 border-t border-edge">
                <Link
                  to="/contracts/studio"
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <GitCompare size={12} />
                  {t('contractGov.detail.compareVersions')}
                </Link>
              </div>
            </CardBody>
          </Card>

          {/* ── Violações ── */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <AlertTriangle size={16} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('contractGov.detail.violations')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              {!violations || violations.length === 0 ? (
                <div className="flex items-center gap-2 text-sm">
                  <CheckCircle size={14} className="text-emerald-400" />
                  <span className="text-emerald-300">{t('contractGov.detail.noViolations')}</span>
                </div>
              ) : (
                <ul className="space-y-2">
                  {violations.slice(0, 5).map((v, idx) => (
                    <li key={idx} className="p-2 rounded-md bg-elevated border border-edge">
                      <div className="flex items-start gap-2">
                        <AlertTriangle size={12} className={v.severity === 'Error' ? 'text-red-400 mt-0.5' : 'text-amber-400 mt-0.5'} />
                        <div className="min-w-0">
                          <p className="text-xs font-medium text-heading">{v.ruleName}</p>
                          <p className="text-[11px] text-muted truncate">{v.message}</p>
                        </div>
                      </div>
                    </li>
                  ))}
                  {violations.length > 5 && (
                    <p className="text-[11px] text-muted text-center pt-1">
                      +{violations.length - 5} {t('contractGov.detail.moreViolations')}
                    </p>
                  )}
                </ul>
              )}
            </CardBody>
          </Card>
        </div>
      </div>

      {/* ── AI Assistant Panel ── */}
      <div className="mt-6">
        <AssistantPanel
          contextType="contract"
          contextId={contractVersionId!}
          contextSummary={{
            name: detail.serviceName ? `${detail.serviceName} — ${detail.protocol}` : detail.contractVersionId,
            description: detail.specPreview,
            status: detail.lifecycleState,
            additionalInfo: {
              ...(detail.protocol ? { protocol: detail.protocol } : {}),
              ...(detail.version ? { version: detail.version } : {}),
              ...(detail.serviceName ? { service: detail.serviceName } : {}),
            },
          }}
        />
      </div>
    </div>
  );
}

/* ── Componentes internos ─────────────────────────────────────────── */

/** Linha de detalhe reutilizável (label + valor). */
function DetailRow({
  label,
  value,
  mono = false,
}: {
  label: string;
  value: string;
  mono?: boolean;
}) {
  return (
    <div>
      <dt className="text-xs text-muted mb-0.5">{label}</dt>
      <dd className={`text-sm text-heading ${mono ? 'font-mono' : ''}`}>{value}</dd>
    </div>
  );
}
