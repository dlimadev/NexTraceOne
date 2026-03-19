import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  X, Shield, CheckCircle, XCircle, ArrowRight, AlertOctagon, Search,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { contractsApi } from '../api';
import type {
  ContractLifecycleState, ContractRuleViolation, ContractIntegrityResult,
  ContractVersionDetail,
} from '../../../types';

// ─── Transições de lifecycle ─────────────────────────────────────────────────

const allowedTransitions: Record<ContractLifecycleState, { state: ContractLifecycleState; action: string }[]> = {
  'Draft': [{ state: 'InReview', action: 'lifecycleActions.submitForReview' }],
  'InReview': [
    { state: 'Approved', action: 'lifecycleActions.approve' },
    { state: 'Draft', action: 'lifecycleActions.returnToDraft' },
  ],
  'Approved': [
    { state: 'Locked', action: 'lifecycleActions.lockVersion' },
    { state: 'InReview', action: 'lifecycleActions.submitForReview' },
  ],
  'Locked': [{ state: 'Deprecated', action: 'lifecycleActions.deprecateVersion' }],
  'Deprecated': [{ state: 'Sunset', action: 'lifecycleActions.startSunset' }],
  'Sunset': [{ state: 'Retired', action: 'lifecycleActions.retire' }],
  'Retired': [],
};

// ─── Helpers ─────────────────────────────────────────────────────────────────

function lifecycleBadgeVariant(state: ContractLifecycleState): 'success' | 'warning' | 'danger' | 'info' {
  switch (state) {
    case 'Draft': return 'info';
    case 'InReview': return 'warning';
    case 'Approved': return 'success';
    case 'Locked': return 'danger';
    case 'Deprecated': return 'warning';
    case 'Sunset': return 'danger';
    case 'Retired': return 'info';
    default: return 'info';
  }
}

function protocolBadgeVariant(protocol: string): 'success' | 'warning' | 'info' {
  switch (protocol) {
    case 'OpenApi': return 'success';
    case 'Swagger': return 'warning';
    case 'Wsdl': return 'info';
    case 'AsyncApi': return 'success';
    default: return 'info';
  }
}

// ─── Props ───────────────────────────────────────────────────────────────────

interface ContractVersionDetailPanelProps {
  selectedVersionId: string;
  detail: ContractVersionDetail | undefined;
  detailLoading: boolean;
  onClose: () => void;
  onShowNotification: (type: 'success' | 'error', message: string) => void;
}

// ─── Component ───────────────────────────────────────────────────────────────

/**
 * Painel de detalhes de uma versão de contrato.
 * Gere localmente: violações, integridade, assinatura, estado de deprecação e lifecycle.
 * Recebe do pai: dados do detalhe (query gerida externamente), callbacks de fecho e notificação.
 */
export function ContractVersionDetailPanel({
  selectedVersionId,
  detail,
  detailLoading,
  onClose,
  onShowNotification,
}: ContractVersionDetailPanelProps) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  // ── Estado local ───────────────────────────────────────────────────────────

  const [violationsVersionId, setViolationsVersionId] = useState<string | null>(null);
  const [integrityResult, setIntegrityResult] = useState<{ id: string; result: ContractIntegrityResult } | null>(null);
  const [integrityLoading, setIntegrityLoading] = useState(false);
  const [verifyStatus, setVerifyStatus] = useState<{ id: string; valid: boolean; message: string } | null>(null);
  const [showDeprecateForm, setShowDeprecateForm] = useState(false);
  const [deprecateNotice, setDeprecateNotice] = useState('');
  const [deprecateSunsetDate, setDeprecateSunsetDate] = useState('');

  // ── Violations query ───────────────────────────────────────────────────────

  const { data: violations, isLoading: violationsLoading } = useQuery<ContractRuleViolation[]>({
    queryKey: ['contracts', 'violations', violationsVersionId],
    queryFn: () => contractsApi.listRuleViolations(violationsVersionId!),
    enabled: !!violationsVersionId,
  });

  // ── Mutations ──────────────────────────────────────────────────────────────

  const transitionMutation = useMutation({
    mutationFn: ({ id, newState }: { id: string; newState: string }) =>
      contractsApi.transitionLifecycle(id, newState),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      onShowNotification('success', t('contracts.lifecycleTransition.success'));
    },
    onError: () => {
      onShowNotification('error', t('contracts.errors.transitionFailed'));
    },
  });

  const deprecateMutation = useMutation({
    mutationFn: ({ id, notice, sunset }: { id: string; notice: string; sunset?: string }) =>
      contractsApi.deprecateVersion(id, notice, sunset || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      setShowDeprecateForm(false);
      setDeprecateNotice('');
      setDeprecateSunsetDate('');
      onShowNotification('success', t('contracts.lifecycleTransition.success'));
    },
    onError: () => {
      onShowNotification('error', t('contracts.errors.deprecateFailed'));
    },
  });

  // ── Handlers ───────────────────────────────────────────────────────────────

  async function handleVerifySignature(versionId: string) {
    try {
      const result = await contractsApi.verifySignature(versionId);
      setVerifyStatus({ id: versionId, valid: result.isValid, message: result.message });
    } catch {
      onShowNotification('error', t('contracts.errors.verifyFailed'));
    }
  }

  async function handleValidateIntegrity(versionId: string) {
    setIntegrityLoading(true);
    setIntegrityResult(null);
    try {
      const result = await contractsApi.validateIntegrity(versionId);
      setIntegrityResult({ id: versionId, result });
    } catch {
      onShowNotification('error', t('contracts.errors.validateFailed'));
    } finally {
      setIntegrityLoading(false);
    }
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <Card className="mt-6">
      <CardHeader>
        <div className="flex items-center justify-between w-full">
          <h2 className="font-semibold text-heading">{t('contracts.detailView.title')}</h2>
          <button onClick={onClose} className="text-muted hover:text-heading transition-colors">
            <X size={16} />
          </button>
        </div>
      </CardHeader>
      <CardBody>
        {detailLoading ? (
          <PageLoadingState size="sm" />
        ) : detail ? (
          <div className="space-y-6">
            {/* Metadados básicos */}
            <div>
              <h3 className="text-sm font-semibold text-heading mb-3">{t('contracts.detailView.metadata')}</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div>
                  <span className="block text-xs text-muted mb-1">{t('contracts.version')}</span>
                  <span className="text-sm font-mono text-heading">{detail.semVer}</span>
                </div>
                <div>
                  <span className="block text-xs text-muted mb-1">{t('contracts.protocol')}</span>
                  <Badge variant={protocolBadgeVariant(detail.protocol)}>
                    {t(`contracts.protocols.${detail.protocol}`)}
                  </Badge>
                </div>
                <div>
                  <span className="block text-xs text-muted mb-1">{t('contracts.lifecycle')}</span>
                  <Badge variant={lifecycleBadgeVariant(detail.lifecycleState)}>
                    {t(`contracts.lifecycleStates.${detail.lifecycleState}`)}
                  </Badge>
                </div>
                <div>
                  <span className="block text-xs text-muted mb-1">{t('contracts.format')}</span>
                  <span className="text-sm text-heading">{detail.format}</span>
                </div>
                {detail.importedFrom && (
                  <div>
                    <span className="block text-xs text-muted mb-1">{t('contracts.importedFrom')}</span>
                    <span className="text-sm text-heading">{detail.importedFrom}</span>
                  </div>
                )}
              </div>
            </div>

            {/* Informação de assinatura */}
            {detail.fingerprint && (
              <div className="border-t border-edge pt-4">
                <h3 className="text-sm font-semibold text-heading mb-3">{t('contracts.signing.signatureStatus')}</h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div>
                    <span className="block text-xs text-muted mb-1">{t('contracts.fingerprint')}</span>
                    <span className="text-xs font-mono text-heading break-all">{detail.fingerprint}</span>
                  </div>
                  {detail.algorithm && (
                    <div>
                      <span className="block text-xs text-muted mb-1">{t('contracts.algorithm')}</span>
                      <span className="text-sm text-heading">{detail.algorithm}</span>
                    </div>
                  )}
                  {detail.signedBy && (
                    <div>
                      <span className="block text-xs text-muted mb-1">{t('contracts.signedBy')}</span>
                      <span className="text-sm text-heading">{detail.signedBy}</span>
                    </div>
                  )}
                  {detail.signedAt && (
                    <div>
                      <span className="block text-xs text-muted mb-1">{t('contracts.signedAt')}</span>
                      <span className="text-xs text-muted">{new Date(detail.signedAt).toLocaleString()}</span>
                    </div>
                  )}
                </div>

                {/* Verificar assinatura */}
                <div className="mt-3 flex items-center gap-3">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => handleVerifySignature(detail.id)}
                  >
                    <Shield size={14} /> {t('contracts.signing.verifyIntegrity')}
                  </Button>
                  {verifyStatus && verifyStatus.id === detail.id && (
                    <span className={`inline-flex items-center gap-1 text-xs ${verifyStatus.valid ? 'text-success' : 'text-critical'}`}>
                      {verifyStatus.valid ? <CheckCircle size={14} /> : <XCircle size={14} />}
                      {verifyStatus.valid ? t('contracts.verifyResult.valid') : t('contracts.verifyResult.invalid')}
                    </span>
                  )}
                </div>
              </div>
            )}

            {/* Verificar assinatura — contrato sem assinatura */}
            {!detail.fingerprint && (
              <div className="border-t border-edge pt-4">
                <span className="text-xs text-muted">{t('contracts.verifyResult.noSignature')}</span>
              </div>
            )}

            {/* Validação de integridade estrutural */}
            <div className="border-t border-edge pt-4">
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-semibold text-heading">{t('contracts.validateIntegrity.title')}</h3>
                <Button
                  variant="secondary"
                  size="sm"
                  loading={integrityLoading}
                  onClick={() => handleValidateIntegrity(detail.id)}
                >
                  <Search size={14} /> {t('contracts.validateIntegrity.action')}
                </Button>
              </div>
              {integrityResult && integrityResult.id === detail.id && (
                <div className={`rounded-md px-4 py-3 text-sm border ${
                  integrityResult.result.isValid
                    ? 'bg-success/10 border-success/30 text-success'
                    : 'bg-critical/10 border-critical/30 text-critical'
                }`}>
                  <div className="flex items-center gap-2 mb-2">
                    {integrityResult.result.isValid
                      ? <CheckCircle size={14} />
                      : <XCircle size={14} />}
                    <span className="font-medium">
                      {integrityResult.result.isValid
                        ? t('contracts.validateIntegrity.valid')
                        : t('contracts.validateIntegrity.invalid')}
                    </span>
                  </div>
                  {integrityResult.result.isValid && (
                    <div className="grid grid-cols-3 gap-3 mt-2 text-xs text-body">
                      <div>
                        <span className="block text-muted">{t('contracts.validateIntegrity.paths')}</span>
                        <span className="font-semibold">{integrityResult.result.pathCount}</span>
                      </div>
                      <div>
                        <span className="block text-muted">{t('contracts.validateIntegrity.endpoints')}</span>
                        <span className="font-semibold">{integrityResult.result.endpointCount}</span>
                      </div>
                      {integrityResult.result.schemaVersion && (
                        <div>
                          <span className="block text-muted">{t('contracts.validateIntegrity.schemaVersion')}</span>
                          <span className="font-semibold">{integrityResult.result.schemaVersion}</span>
                        </div>
                      )}
                    </div>
                  )}
                  {!integrityResult.result.isValid && integrityResult.result.validationError && (
                    <p className="text-xs mt-1">{integrityResult.result.validationError}</p>
                  )}
                </div>
              )}
            </div>

            {/* Proveniência */}
            {detail.provenance && (
              <div className="border-t border-edge pt-4">
                <h3 className="text-sm font-semibold text-heading mb-3">{t('contracts.detailView.provenance')}</h3>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  <div>
                    <span className="block text-xs text-muted mb-1">{t('contracts.origin')}</span>
                    <span className="text-sm text-heading">{detail.provenance.origin}</span>
                  </div>
                  <div>
                    <span className="block text-xs text-muted mb-1">{t('contracts.format')}</span>
                    <span className="text-sm text-heading">{detail.provenance.originalFormat}</span>
                  </div>
                  <div>
                    <span className="block text-xs text-muted mb-1">
                      {detail.provenance.isAiGenerated ? t('contracts.aiGenerated') : t('contracts.humanCreated')}
                    </span>
                    <Badge variant={detail.provenance.isAiGenerated ? 'warning' : 'info'}>
                      {detail.provenance.isAiGenerated ? t('contracts.aiGenerated') : t('contracts.humanCreated')}
                    </Badge>
                  </div>
                </div>
              </div>
            )}

            {/* Deprecação */}
            {detail.deprecationNotice && (
              <div className="border-t border-edge pt-4">
                <h3 className="text-sm font-semibold text-heading mb-2">{t('contracts.deprecationNotice')}</h3>
                <p className="text-sm text-warning">{detail.deprecationNotice}</p>
                {detail.sunsetDate && (
                  <p className="text-xs text-muted mt-1">
                    {t('contracts.sunsetDate')}: {new Date(detail.sunsetDate).toLocaleDateString()}
                  </p>
                )}
              </div>
            )}

            {/* Violações de regras */}
            <div className="border-t border-edge pt-4">
              <div className="flex items-center justify-between mb-3">
                <h3 className="text-sm font-semibold text-heading">{t('contracts.violationsTitle')}</h3>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setViolationsVersionId(
                    violationsVersionId === detail.id ? null : detail.id
                  )}
                >
                  <AlertOctagon size={14} /> {t('contracts.violations')}
                </Button>
              </div>
              {violationsVersionId === detail.id && (
                <>
                  {violationsLoading ? (
                    <PageLoadingState size="sm" />
                  ) : violations && violations.length > 0 ? (
                    <table className="min-w-full text-xs">
                      <thead>
                        <tr className="border-b border-edge text-left">
                          <th className="px-2 py-2 font-medium text-muted">{t('contracts.violationRule')}</th>
                          <th className="px-2 py-2 font-medium text-muted">{t('contracts.violationSeverity')}</th>
                          <th className="px-2 py-2 font-medium text-muted">{t('contracts.violationMessage')}</th>
                          <th className="px-2 py-2 font-medium text-muted">{t('contracts.violationPath')}</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-edge">
                        {violations.map((v) => (
                          <tr key={v.id}>
                            <td className="px-2 py-2 font-mono text-heading">{v.ruleName}</td>
                            <td className="px-2 py-2">
                              <Badge variant={v.severity === 'Error' ? 'danger' : v.severity === 'Warning' ? 'warning' : 'info'}>
                                {v.severity}
                              </Badge>
                            </td>
                            <td className="px-2 py-2 text-body">{v.message}</td>
                            <td className="px-2 py-2 font-mono text-muted">{v.path}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  ) : (
                    <p className="text-xs text-muted">{t('contracts.noViolationsFound')}</p>
                  )}
                </>
              )}
            </div>

            {/* Transições de lifecycle */}
            <div className="border-t border-edge pt-4">
              <h3 className="text-sm font-semibold text-heading mb-3">{t('contracts.lifecycleTransition.title')}</h3>
              <div className="flex items-center gap-2 mb-3">
                <span className="text-xs text-muted">{t('contracts.lifecycleTransition.currentState')}:</span>
                <Badge variant={lifecycleBadgeVariant(detail.lifecycleState)}>
                  {t(`contracts.lifecycleStates.${detail.lifecycleState}`)}
                </Badge>
              </div>
              <div className="flex flex-wrap gap-2">
                {allowedTransitions[detail.lifecycleState]?.map((tr) => (
                  <Button
                    key={tr.state}
                    variant="secondary"
                    size="sm"
                    loading={transitionMutation.isPending}
                    onClick={() => {
                      if (tr.state === 'Deprecated') {
                        setShowDeprecateForm(true);
                      } else {
                        transitionMutation.mutate({ id: detail.id, newState: tr.state });
                      }
                    }}
                  >
                    <ArrowRight size={14} /> {t(`contracts.${tr.action}`)}
                  </Button>
                ))}
                {(!allowedTransitions[detail.lifecycleState] || allowedTransitions[detail.lifecycleState].length === 0) && (
                  <span className="text-xs text-muted">{t('contracts.lifecycleStates.Retired')}</span>
                )}
              </div>
            </div>

            {/* Formulário de deprecação */}
            {showDeprecateForm && (
              <div className="border-t border-edge pt-4">
                <h3 className="text-sm font-semibold text-heading mb-3">{t('contracts.deprecate')}</h3>
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('contracts.deprecationNotice')}</label>
                    <textarea
                      value={deprecateNotice}
                      onChange={(e) => setDeprecateNotice(e.target.value)}
                      rows={3}
                      required
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('contracts.sunsetDate')}</label>
                    <input
                      type="date"
                      value={deprecateSunsetDate}
                      onChange={(e) => setDeprecateSunsetDate(e.target.value)}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    />
                  </div>
                  <div className="flex gap-2 justify-end">
                    <Button variant="secondary" size="sm" onClick={() => setShowDeprecateForm(false)}>
                      {t('common.cancel')}
                    </Button>
                    <Button
                      variant="danger"
                      size="sm"
                      loading={deprecateMutation.isPending}
                      disabled={!deprecateNotice.trim()}
                      onClick={() => deprecateMutation.mutate({
                        id: detail.id,
                        notice: deprecateNotice,
                        sunset: deprecateSunsetDate || undefined,
                      })}
                    >
                      {t('contracts.lifecycleTransition.confirm')}
                    </Button>
                  </div>
                </div>
              </div>
            )}

            {/* Conteúdo da especificação */}
            <div className="border-t border-edge pt-4">
              <h3 className="text-sm font-semibold text-heading mb-3">{t('contracts.detailView.specContent')}</h3>
              <pre className="bg-canvas border border-edge rounded-md p-4 text-xs font-mono text-heading overflow-x-auto max-h-64 overflow-y-auto">
                {detail.specContent}
              </pre>
            </div>

            <div className="flex justify-end">
              <Button variant="secondary" onClick={onClose}>
                <X size={14} /> {t('contracts.detailView.close')}
              </Button>
            </div>
          </div>
        ) : null}
      </CardBody>
    </Card>
  );
}
