import { useState, useRef, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Plus, Lock, RefreshCw, Shield, FileCheck, AlertTriangle,
  X, Eye, GitCompare, Download, CheckCircle, XCircle, ArrowRight,
  FilePlus, AlertOctagon, Search,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { contractsApi } from '../api';
import type {
  ContractLifecycleState, ContractProtocol, ContractVersion,
  ContractVersionDetail, SemanticDiff, ContractRuleViolation,
  ContractIntegrityResult,
} from '../../../types';

/**
 * Mapa de transições válidas por estado do lifecycle.
 * Cada entrada define o estado destino e a chave i18n da ação.
 */
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

  // Estado da verificação de assinatura
  const [verifyStatus, setVerifyStatus] = useState<{ id: string; valid: boolean; message: string } | null>(null);

  // Estado do formulário de deprecação
  const [showDeprecateForm, setShowDeprecateForm] = useState<string | null>(null);
  const [deprecateNotice, setDeprecateNotice] = useState('');
  const [deprecateSunsetDate, setDeprecateSunsetDate] = useState('');

  // Estado do painel de exportação
  const [exportContent, setExportContent] = useState<{ id: string; specContent: string; format: string } | null>(null);

  // Estado das violações de regras
  const [violationsVersionId, setViolationsVersionId] = useState<string | null>(null);

  // Estado da validação de integridade estrutural do contrato
  const [integrityResult, setIntegrityResult] = useState<{ id: string; result: ContractIntegrityResult } | null>(null);
  const [integrityLoading, setIntegrityLoading] = useState(false);

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

  const { data: history, isLoading } = useQuery({
    queryKey: ['contracts', 'history', apiAssetId],
    queryFn: () => contractsApi.getHistory(apiAssetId),
    enabled: !!apiAssetId,
  });

  const { data: detail, isLoading: detailLoading } = useQuery<ContractVersionDetail>({
    queryKey: ['contracts', 'detail', selectedVersionId],
    queryFn: () => contractsApi.getDetail(selectedVersionId!),
    enabled: !!selectedVersionId,
  });

  const { data: violations, isLoading: violationsLoading } = useQuery<ContractRuleViolation[]>({
    queryKey: ['contracts', 'violations', violationsVersionId],
    queryFn: () => contractsApi.listRuleViolations(violationsVersionId!),
    enabled: !!violationsVersionId,
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

  const transitionMutation = useMutation({
    mutationFn: ({ id, newState }: { id: string; newState: string }) =>
      contractsApi.transitionLifecycle(id, newState),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      showNotification('success', t('contracts.lifecycleTransition.success'));
    },
    onError: () => {
      showNotification('error', t('contracts.errors.transitionFailed'));
    },
  });

  const deprecateMutation = useMutation({
    mutationFn: ({ id, notice, sunset }: { id: string; notice: string; sunset?: string }) =>
      contractsApi.deprecateVersion(id, notice, sunset || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      setShowDeprecateForm(null);
      setDeprecateNotice('');
      setDeprecateSunsetDate('');
      showNotification('success', t('contracts.lifecycleTransition.success'));
    },
    onError: () => {
      showNotification('error', t('contracts.errors.deprecateFailed'));
    },
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

  async function handleVerifySignature(versionId: string) {
    try {
      const result = await contractsApi.verifySignature(versionId);
      setVerifyStatus({ id: versionId, valid: result.isValid, message: result.message });
    } catch {
      showNotification('error', t('contracts.errors.verifyFailed'));
    }
  }

  async function handleExport(versionId: string) {
    try {
      const result = await contractsApi.exportVersion(versionId);
      setExportContent({ id: versionId, specContent: result.specContent, format: result.format });
    } catch {
      showNotification('error', t('contracts.errors.exportFailed'));
    }
  }

  /**
   * Aciona a validação de integridade estrutural do contrato conforme seu protocolo.
   * Retorna contagem de paths/endpoints e versão extraída do spec, se disponível.
   */
  async function handleValidateIntegrity(versionId: string) {
    setIntegrityLoading(true);
    setIntegrityResult(null);
    try {
      const result = await contractsApi.validateIntegrity(versionId);
      setIntegrityResult({ id: versionId, result });
    } catch {
      showNotification('error', t('contracts.errors.validateFailed'));
    } finally {
      setIntegrityLoading(false);
    }
  }

  function handleSelectDetail(versionId: string) {
    setSelectedVersionId((prev) => (prev === versionId ? null : versionId));
    setVerifyStatus(null);
    setShowDeprecateForm(null);
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
    <div className="p-6 lg:p-8 animate-fade-in">
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

      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('contracts.title')}</h1>
          <p className="text-muted mt-1">{t('contracts.subtitle')}</p>
        </div>
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
      </div>

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
                  <input
                    type="text"
                    value={importForm.apiAssetId}
                    onChange={(e) => setImportForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    placeholder="UUID"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.version')}</label>
                  <input
                    type="text"
                    value={importForm.version}
                    onChange={(e) => setImportForm((f) => ({ ...f, version: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    placeholder="1.0.0"
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
                  <input
                    type="text"
                    value={createVersionForm.apiAssetId}
                    onChange={(e) => setCreateVersionForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    placeholder="UUID"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">{t('contracts.version')}</label>
                  <input
                    type="text"
                    value={createVersionForm.version}
                    onChange={(e) => setCreateVersionForm((f) => ({ ...f, version: e.target.value }))}
                    required
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    placeholder="2.0.0"
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
            <label className="text-sm font-medium text-body whitespace-nowrap">{t('contracts.apiAssetIdLabel')}</label>
            <input
              type="text"
              value={apiAssetId}
              onChange={(e) => setApiAssetId(e.target.value)}
              placeholder={t('contracts.filterPlaceholder')}
              className="flex-1 text-sm bg-canvas border border-edge rounded-md px-3 py-1.5 text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
            />
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
              <p className="text-sm text-muted">{t('contracts.enterApiAssetId')}</p>
            </div>
          ) : isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
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
        <Card className="mt-6">
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <h2 className="font-semibold text-heading">{t('contracts.detailView.title')}</h2>
              <button onClick={() => setSelectedVersionId(null)} className="text-muted hover:text-heading transition-colors">
                <X size={16} />
              </button>
            </div>
          </CardHeader>
          <CardBody>
            {detailLoading ? (
              <div className="flex items-center justify-center py-8">
                <RefreshCw size={20} className="animate-spin text-muted" />
              </div>
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
                        <div className="flex items-center justify-center py-4">
                          <RefreshCw size={16} className="animate-spin text-muted" />
                        </div>
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
                            setShowDeprecateForm(detail.id);
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
                {showDeprecateForm === detail.id && (
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
                        <Button variant="secondary" size="sm" onClick={() => setShowDeprecateForm(null)}>
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
                  <Button variant="secondary" onClick={() => setSelectedVersionId(null)}>
                    <X size={14} /> {t('contracts.detailView.close')}
                  </Button>
                </div>
              </div>
            ) : null}
          </CardBody>
        </Card>
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
    </div>
  );
}
