import { useState, useRef, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Plus, Lock, Shield, FileCheck, AlertTriangle,
  X, Eye, GitCompare, Download, CheckCircle, XCircle,
  FilePlus,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { ContractVersionDetailPanel } from '../components/ContractVersionDetailPanel';
import { contractsApi, serviceCatalogApi } from '../api';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type {
  ContractLifecycleState, ContractProtocol, ContractVersion,
  ContractVersionDetail, SemanticDiff,
} from '../../../types';

/**
 * Retorna a variante visual do Badge conforme o estado do lifecycle.
 */
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

/**
 * Retorna a variante visual do Badge conforme o protocolo.
 */
function protocolBadgeVariant(protocol: ContractProtocol): 'success' | 'warning' | 'info' {
  switch (protocol) {
    case 'OpenApi': return 'success';
    case 'Swagger': return 'warning';
    case 'Wsdl': return 'info';
    case 'AsyncApi': return 'success';
    default: return 'info';
  }
}

/**
 * Retorna a variante do Badge para o nível de mudança do diff.
 */
function changeLevelBadgeVariant(level: string): 'danger' | 'success' | 'warning' {
  switch (level) {
    case 'Breaking': return 'danger';
    case 'Additive': return 'success';
    case 'NonBreaking': return 'warning';
    default: return 'warning';
  }
}

export function ContractsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  // Estado do filtro e formulário de importação
  const [apiAssetId, setApiAssetId] = useState('');
  const [showImportForm, setShowImportForm] = useState(false);
  const [importForm, setImportForm] = useState({
    apiAssetId: '',
    content: '',
    version: '',
    protocol: 'OpenApi' as ContractProtocol,
  });

  // Estado do painel de detalhes
  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);

  // Estado do diff semântico
  const [showDiffPanel, setShowDiffPanel] = useState(false);
  const [diffBaseId, setDiffBaseId] = useState('');
  const [diffTargetId, setDiffTargetId] = useState('');
  const [diffResult, setDiffResult] = useState<SemanticDiff | null>(null);

  // Estado do painel de exportação
  const [exportContent, setExportContent] = useState<{ id: string; specContent: string; format: string } | null>(null);

  // Estado do formulário de criação de nova versão
  const [showCreateVersionForm, setShowCreateVersionForm] = useState(false);
  const [createVersionForm, setCreateVersionForm] = useState({
    apiAssetId: '',
    content: '',
    version: '',
    protocol: undefined as ContractProtocol | undefined,
  });

  // Estado para feedback de notificação inline
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  // Referência para limpeza do timeout de notificação ao desmontar o componente
  const notificationTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (notificationTimerRef.current) {
        clearTimeout(notificationTimerRef.current);
      }
    };
  }, []);

  /**
   * Exibe uma notificação temporária que desaparece após 4 segundos.
   */
  function showNotification(type: 'success' | 'error', message: string) {
    setNotification({ type, message });
    if (notificationTimerRef.current) {
      clearTimeout(notificationTimerRef.current);
    }
    notificationTimerRef.current = setTimeout(() => setNotification(null), 4000);
  }

  // ─── Queries ────────────────────────────────────────────────────────────────

  /** Fetch available API assets for the entity picker (replaces raw GUID text inputs). */
  const { data: graph } = useQuery({
    queryKey: ['graph'],
    queryFn: () => serviceCatalogApi.getGraph(),
    staleTime: 60_000,
  });
  const availableApis = graph?.apis ?? [];

  const { data: history, isLoading, isError: isHistoryError } = useQuery({
    queryKey: ['contracts', 'history', apiAssetId],
    queryFn: () => contractsApi.getHistory(apiAssetId),
    enabled: !!apiAssetId,
  });

  const { data: detail, isLoading: detailLoading } = useQuery<ContractVersionDetail>({
    queryKey: ['contracts', 'detail', selectedVersionId],
    queryFn: () => contractsApi.getDetail(selectedVersionId!),
    enabled: !!selectedVersionId,
  });

  // ─── Mutations ──────────────────────────────────────────────────────────────

  const importMutation = useMutation({
    mutationFn: contractsApi.importContract,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      setShowImportForm(false);
      setImportForm({ apiAssetId: '', content: '', version: '', protocol: 'OpenApi' });
    },
  });

  const createVersionMutation = useMutation({
    mutationFn: contractsApi.createVersion,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      setShowCreateVersionForm(false);
      setCreateVersionForm({ apiAssetId: '', content: '', version: '', protocol: undefined });
      showNotification('success', t('contracts.createVersionSuccess'));
    },
    onError: () => {
      showNotification('error', t('contracts.errors.createVersionFailed'));
    },
  });

  const lockMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      contractsApi.lockVersion(id, reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contracts'] }),
  });

  const signMutation = useMutation({
    mutationFn: (id: string) => contractsApi.signVersion(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contracts'] }),
  });

  const diffMutation = useMutation({
    mutationFn: ({ from, to }: { from: string; to: string }) =>
      contractsApi.computeDiff(from, to),
    onSuccess: (data) => {
      setDiffResult(data);
    },
    onError: () => {
      showNotification('error', t('contracts.errors.diffFailed'));
    },
  });

  // ─── Handlers ───────────────────────────────────────────────────────────────

  async function handleExport(versionId: string) {
    try {
      const result = await contractsApi.exportVersion(versionId);
      setExportContent({ id: versionId, specContent: result.specContent, format: result.format });
    } catch {
      showNotification('error', t('contracts.errors.exportFailed'));
    }
  }

  function handleSelectDetail(versionId: string) {
    setSelectedVersionId((prev) => (prev === versionId ? null : versionId));
    setExportContent(null);
  }

  /**
   * Renderiza a lista de mudanças de um tipo específico do diff.
   */
  function renderChangeList(changes: { path: string; changeType: string; description: string }[], color: string) {
    if (!changes.length) return null;
    return (
      <ul className="space-y-1">
        {changes.map((c, i) => (
          <li key={i} className={`text-xs ${color} flex items-start gap-2`}>
            <span className="font-mono shrink-0">{c.path}</span>
            <span className="text-muted">—</span>
            <span>{c.description}</span>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <PageContainer>
      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="contracts" />

      {/* Notificação inline */}
      {notification && (
        <div className={`mb-4 px-4 py-3 rounded-md text-sm flex items-center gap-2 ${
          notification.type === 'success'
            ? 'bg-success/15 text-success border border-success/30'
            : 'bg-critical/15 text-critical border border-critical/30'
        }`}>
          {notification.type === 'success' ? <CheckCircle size={16} /> : <XCircle size={16} />}
          {notification.message}
        </div>
      )}

      <PageHeader
        title={t('contracts.title')}
        subtitle={t('contracts.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => { setShowDiffPanel((v) => !v); setDiffResult(null); }}>
              <GitCompare size={16} /> {t('contracts.diffCompare.title')}
            </Button>
            <Button onClick={() => setShowImportForm((v) => !v)}>
              <Plus size={16} /> {t('contracts.importContract')}
            </Button>
            <Button variant="secondary" onClick={() => setShowCreateVersionForm((v) => !v)}>
              <FilePlus size={16} /> {t('contracts.createVersion')}
            </Button>
          </div>
        }
      />

      {/* Import Form */}
      {showImportForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('contracts.importTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); importMutation.mutate(importForm); }}
              className="space-y-4"
            >
              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.apiAssetId')}</label>
                  <select
                    value={importForm.apiAssetId}
                    onChange={(e) => setImportForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  >
                    <option value="">{t('contracts.selectApiAsset')}</option>
                    {availableApis.map((api) => (
                      <option key={api.apiAssetId} value={api.apiAssetId}>
                        {api.name} — {api.routePattern}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.version')}</label>
                  <input
                    type="text"
                    value={importForm.version}
                    onChange={(e) => setImportForm((f) => ({ ...f, version: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    placeholder={t('contracts.versionExample')}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.protocol')}</label>
                  <select
                    value={importForm.protocol}
                    onChange={(e) => setImportForm((f) => ({ ...f, protocol: e.target.value as ContractProtocol }))}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  >
                    <option value="OpenApi">{t('contracts.protocols.OpenApi')}</option>
                    <option value="Swagger">{t('contracts.protocols.Swagger')}</option>
                    <option value="Wsdl">{t('contracts.protocols.Wsdl')}</option>
                    <option value="AsyncApi">{t('contracts.protocols.AsyncApi')}</option>
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('contracts.specContent')}</label>
                <textarea
                  value={importForm.content}
                  onChange={(e) => setImportForm((f) => ({ ...f, content: e.target.value }))}
                  required
                  rows={6}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading font-mono placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  placeholder={t('contracts.specContentPlaceholder')}
                />
              </div>
              <div className="flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowImportForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={importMutation.isPending}>{t('contracts.import')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Create Version Form */}
      {showCreateVersionForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('contracts.createVersionTitle')}</h2></CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-4">{t('contracts.createVersionDescription')}</p>
            <form
              onSubmit={(e) => { e.preventDefault(); createVersionMutation.mutate(createVersionForm); }}
              className="space-y-4"
            >
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.apiAssetId')}</label>
                  <select
                    value={createVersionForm.apiAssetId}
                    onChange={(e) => setCreateVersionForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  >
                    <option value="">{t('contracts.selectApiAsset')}</option>
                    {availableApis.map((api) => (
                      <option key={api.apiAssetId} value={api.apiAssetId}>
                        {api.name} — {api.routePattern}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.version')}</label>
                  <input
                    type="text"
                    value={createVersionForm.version}
                    onChange={(e) => setCreateVersionForm((f) => ({ ...f, version: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    placeholder={t('contracts.versionExample')}
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('contracts.specContent')}</label>
                <textarea
                  value={createVersionForm.content}
                  onChange={(e) => setCreateVersionForm((f) => ({ ...f, content: e.target.value }))}
                  required
                  rows={6}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading font-mono placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  placeholder={t('contracts.specContentPlaceholder')}
                />
              </div>
              <div className="flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowCreateVersionForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={createVersionMutation.isPending}>{t('contracts.createVersion')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Semantic Diff Panel */}
      {showDiffPanel && history && history.length >= 2 && (
        <Card className="mb-6">
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <h2 className="font-semibold text-heading">{t('contracts.diffCompare.title')}</h2>
              <button onClick={() => { setShowDiffPanel(false); setDiffResult(null); }} className="text-muted hover:text-heading transition-colors">
                <X size={16} />
              </button>
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex gap-4 items-end mb-4">
              <div className="flex-1">
                <label className="block text-sm font-medium text-body mb-1">{t('contracts.diffCompare.baseVersion')}</label>
                <select
                  value={diffBaseId}
                  onChange={(e) => setDiffBaseId(e.target.value)}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                >
                  <option value="">{t('contracts.diffView.selectBaseVersion')}</option>
                  {history.map((cv: ContractVersion) => (
                    <option key={cv.id} value={cv.id}>{cv.version}</option>
                  ))}
                </select>
              </div>
              <div className="flex-1">
                <label className="block text-sm font-medium text-body mb-1">{t('contracts.diffCompare.targetVersion')}</label>
                <select
                  value={diffTargetId}
                  onChange={(e) => setDiffTargetId(e.target.value)}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                >
                  <option value="">{t('contracts.diffView.selectTargetVersion')}</option>
                  {history.map((cv: ContractVersion) => (
                    <option key={cv.id} value={cv.id}>{cv.version}</option>
                  ))}
                </select>
              </div>
              <Button
                onClick={() => diffMutation.mutate({ from: diffBaseId, to: diffTargetId })}
                loading={diffMutation.isPending}
                disabled={!diffBaseId || !diffTargetId || diffBaseId === diffTargetId}
              >
                <GitCompare size={16} /> {t('contracts.diffCompare.compare')}
              </Button>
            </div>

            {/* Resultado do Diff */}
            {diffResult && (
              <div className="space-y-4 border-t border-edge pt-4">
                <div className="flex items-center gap-4">
                  <h3 className="text-sm font-semibold text-heading">{t('contracts.diffCompare.result')}</h3>
                  {diffResult.isBreaking !== undefined && (
                    <Badge variant={changeLevelBadgeVariant(diffResult.isBreaking ? 'Breaking' : 'NonBreaking')}>
                      {t('contracts.diffCompare.changeLevel')}: {diffResult.isBreaking ? 'Breaking' : 'Non-Breaking'}
                    </Badge>
                  )}
                  <span className="text-xs text-muted">
                    {t('contracts.diffView.suggestedVersion')}: <span className="font-mono text-heading">{diffResult.suggestedVersion}</span>
                  </span>
                </div>

                {/* Mudanças breaking */}
                {diffResult.changes?.filter((c) => c.isBreaking).length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-critical mb-2">{t('contracts.diffView.breakingChanges')}</h4>
                    {renderChangeList(diffResult.changes.filter((c) => c.isBreaking), 'text-critical')}
                  </div>
                )}

                {/* Mudanças não-breaking */}
                {diffResult.changes?.filter((c) => !c.isBreaking && c.changeType !== 'Added').length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-warning mb-2">{t('contracts.diffView.nonBreakingChanges')}</h4>
                    {renderChangeList(diffResult.changes.filter((c) => !c.isBreaking && c.changeType !== 'Added'), 'text-warning')}
                  </div>
                )}

                {/* Mudanças aditivas */}
                {diffResult.changes?.filter((c) => !c.isBreaking && c.changeType === 'Added').length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-success mb-2">{t('contracts.diffView.additiveChanges')}</h4>
                    {renderChangeList(diffResult.changes.filter((c) => !c.isBreaking && c.changeType === 'Added'), 'text-success')}
                  </div>
                )}

                {/* Nenhuma mudança detectada */}
                {(!diffResult.changes || diffResult.changes.length === 0) && (
                  <p className="text-sm text-muted">{t('contracts.diffView.noChanges')}</p>
                )}
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* History Filter */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex gap-3 items-center">
            <label className="text-sm font-medium text-body whitespace-nowrap">{t('contracts.selectApiAssetLabel')}</label>
            <select
              value={apiAssetId}
              onChange={(e) => setApiAssetId(e.target.value)}
              className="flex-1 text-sm bg-canvas border border-edge rounded-md px-3 py-1.5 text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
            >
              <option value="">{t('contracts.selectApiAsset')}</option>
              {availableApis.map((api) => (
                <option key={api.apiAssetId} value={api.apiAssetId}>
                  {api.name} — {api.routePattern}
                </option>
              ))}
            </select>
          </div>
        </CardBody>
      </Card>

      {/* Contract History */}
      <Card>
        <CardHeader>
          <h2 className="text-base font-semibold text-heading">{t('contracts.contractVersions')}</h2>
        </CardHeader>
        <div className="overflow-x-auto">
          {!apiAssetId ? (
            <div className="px-6 py-12 text-center">
              <FileCheck size={40} className="mx-auto mb-3 text-muted opacity-50" />
              <p className="text-sm text-muted">{t('contracts.selectApiAssetPrompt')}</p>
            </div>
          ) : isLoading ? (
            <PageLoadingState />
          ) : isHistoryError ? (
            <PageErrorState />
          ) : !history?.length ? (
            <div className="px-6 py-12 text-center">
              <AlertTriangle size={40} className="mx-auto mb-3 text-muted opacity-50" />
              <p className="text-sm font-medium text-heading mb-1">{t('contracts.emptyState.title')}</p>
              <p className="text-xs text-muted">{t('contracts.emptyState.description')}</p>
            </div>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-4 py-3 font-medium text-muted">{t('contracts.version')}</th>
                  <th className="px-4 py-3 font-medium text-muted">{t('contracts.protocol')}</th>
                  <th className="px-4 py-3 font-medium text-muted">{t('contracts.lifecycle')}</th>
                  <th className="px-4 py-3 font-medium text-muted">{t('contracts.signing.signatureStatus')}</th>
                  <th className="px-4 py-3 font-medium text-muted">{t('contracts.created')}</th>
                  <th className="px-4 py-3 font-medium text-muted">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {history.map((cv) => (
                  <tr
                    key={cv.id}
                    className={`hover:bg-hover transition-colors ${selectedVersionId === cv.id ? 'bg-hover' : ''}`}
                  >
                    <td className="px-4 py-3 font-mono font-medium text-heading">{cv.version}</td>
                    <td className="px-4 py-3">
                      <Badge variant={protocolBadgeVariant(cv.protocol || 'OpenApi')}>
                        {t(`contracts.protocols.${cv.protocol || 'OpenApi'}`)}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={lifecycleBadgeVariant(cv.lifecycleState || 'Draft')}>
                        {t(`contracts.lifecycleStates.${cv.lifecycleState || 'Draft'}`)}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      {cv.fingerprint ? (
                        <span className="inline-flex items-center gap-1 text-xs text-success">
                          <Shield size={12} /> {t('contracts.signed')}
                        </span>
                      ) : (
                        <span className="text-xs text-muted">{t('contracts.unsigned')}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-xs text-muted">
                      {new Date(cv.createdAt).toLocaleString()}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {!cv.isLocked && (
                          <button
                            onClick={() => lockMutation.mutate({ id: cv.id, reason: t('contracts.lockedViaUi') })}
                            className="inline-flex items-center gap-1 text-xs text-muted hover:text-critical transition-colors"
                            title={t('contracts.lock')}
                          >
                            <Lock size={12} /> {t('contracts.lock')}
                          </button>
                        )}
                        {cv.isLocked && !cv.fingerprint && (
                          <button
                            onClick={() => signMutation.mutate(cv.id)}
                            className="inline-flex items-center gap-1 text-xs text-muted hover:text-accent transition-colors"
                            title={t('contracts.sign')}
                          >
                            <Shield size={12} /> {t('contracts.sign')}
                          </button>
                        )}
                        <button
                          onClick={() => handleExport(cv.id)}
                          className="inline-flex items-center gap-1 text-xs text-muted hover:text-accent transition-colors"
                          title={t('contracts.export')}
                        >
                          <Download size={12} /> {t('contracts.export')}
                        </button>
                        <button
                          onClick={() => handleSelectDetail(cv.id)}
                          className="inline-flex items-center gap-1 text-xs text-muted hover:text-accent transition-colors"
                          title={t('contracts.detail')}
                        >
                          <Eye size={12} /> {t('contracts.detail')}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>

      {/* Version Detail Panel */}
      {selectedVersionId && (
        <ContractVersionDetailPanel
          key={selectedVersionId}
          selectedVersionId={selectedVersionId}
          detail={detail}
          detailLoading={detailLoading}
          onClose={() => setSelectedVersionId(null)}
          onShowNotification={showNotification}
        />
      )}

      {/* Export Content Panel */}
      {exportContent && (
        <Card className="mt-6">
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <h2 className="font-semibold text-heading">{t('contracts.export')} — {exportContent.format}</h2>
              <button onClick={() => setExportContent(null)} className="text-muted hover:text-heading transition-colors">
                <X size={16} />
              </button>
            </div>
          </CardHeader>
          <CardBody>
            <pre className="bg-canvas border border-edge rounded-md p-4 text-xs font-mono text-heading overflow-x-auto max-h-80 overflow-y-auto">
              {exportContent.specContent}
            </pre>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
