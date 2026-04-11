/**
 * ConsumerDrivenContractPage — painel de Consumer-Driven Contract Testing (CDCT).
 * Permite registar expectativas de consumidores, verificar compatibilidade
 * com o contrato do provider e acompanhar resultados de validação.
 * Pilar: Contract Governance + Operational Reliability.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import {
  ShieldCheck,
  ShieldAlert,
  Plus,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Users,
  FileCheck,
  RefreshCw,
} from 'lucide-react';

type ConsumerExpectation = {
  id: string;
  consumerServiceName: string;
  consumerDomain: string;
  expectedSubsetJson: string;
  notes: string;
  createdAt: string;
};

type CdctResult = {
  totalExpectations: number;
  compatible: number;
  incompatible: number;
  results: Array<{
    consumerServiceName: string;
    isCompatible: boolean;
    missingPaths: string[];
    missingFields: string[];
    notes: string;
  }>;
};

export function ConsumerDrivenContractPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [apiAssetId, setApiAssetId] = useState('');
  const [versionId, setVersionId] = useState('');
  const [showNewForm, setShowNewForm] = useState(false);
  const [newExpectation, setNewExpectation] = useState({
    consumerServiceName: '',
    consumerDomain: '',
    expectedSubsetJson: '{\n  "paths": {\n    "/api/v1/resource": {\n      "get": {}\n    }\n  }\n}',
    notes: '',
  });

  const { data: expectations, isLoading: loadingExpectations, isError: isExpectationsError, refetch: refetchExpectations } = useQuery({
    queryKey: ['consumer-expectations', apiAssetId],
    queryFn: () => contractsApi.getConsumerExpectations(apiAssetId),
    enabled: !!apiAssetId,
  });

  const { data: cdctResult, isLoading: loadingCdct, refetch: runCdct } = useQuery({
    queryKey: ['cdct-verify', apiAssetId, versionId],
    queryFn: () => contractsApi.verifyCdct(apiAssetId, versionId),
    enabled: false,
  });

  const registerMutation = useMutation({
    mutationFn: () => contractsApi.registerConsumerExpectation(apiAssetId, newExpectation),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['consumer-expectations', apiAssetId] });
      setShowNewForm(false);
      setNewExpectation({ consumerServiceName: '', consumerDomain: '', expectedSubsetJson: '{}', notes: '' });
    },
  });

  const typed = cdctResult as CdctResult | undefined;
  const expectationList = (expectations as { items?: ConsumerExpectation[] })?.items ?? [];

  if (isExpectationsError) {
    return (
      <PageContainer>
        <PageErrorState onRetry={() => refetchExpectations()} />
      </PageContainer>
    );
  }

  return (
    <div className="min-h-screen bg-background px-6 py-6 text-body">
      {/* ─── Header ─── */}
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <ShieldCheck size={20} className="text-accent" />
          <h1 className="text-lg font-semibold text-heading">
            {t('contracts.cdct.title', 'Consumer-Driven Contract Testing')}
          </h1>
        </div>
        <p className="text-xs text-muted">
          {t(
            'contracts.cdct.subtitle',
            'Register consumer expectations and verify provider contracts satisfy them — prevent breaking changes.'
          )}
        </p>
      </div>

      {/* ─── Contract Selector ─── */}
      <div className="bg-panel border border-edge rounded-lg p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1.5 block">
              {t('contracts.cdct.apiAssetId', 'API Asset ID')}
            </label>
            <input
              type="text"
              value={apiAssetId}
              onChange={(e) => setApiAssetId(e.target.value)}
              placeholder={t('contracts.cdct.apiAssetIdPlaceholder', 'Provider API Asset ID...')}
              className="w-full text-xs bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
            />
          </div>
          <div>
            <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1.5 block">
              {t('contracts.cdct.versionId', 'Contract Version ID')}
            </label>
            <div className="flex gap-2">
              <input
                type="text"
                value={versionId}
                onChange={(e) => setVersionId(e.target.value)}
                placeholder={t('contracts.cdct.versionIdPlaceholder', 'Version to verify...')}
                className="flex-1 text-xs bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
              />
              <button
                type="button"
                onClick={() => runCdct()}
                disabled={!apiAssetId || !versionId || loadingCdct}
                className="flex items-center gap-1.5 text-xs font-medium bg-accent text-white rounded px-3 py-1.5 hover:bg-accent/90 transition-colors disabled:opacity-40"
              >
                <RefreshCw size={12} className={loadingCdct ? 'animate-spin' : ''} />
                {t('contracts.cdct.verify', 'Verify')}
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* ─── CDCT Results ─── */}
      {typed && (
        <div className="bg-panel border border-edge rounded-lg p-4 mb-4">
          <h2 className="text-xs font-semibold text-heading mb-3 flex items-center gap-2">
            <FileCheck size={14} className="text-accent" />
            {t('contracts.cdct.results', 'Verification Results')}
          </h2>

          <div className="grid grid-cols-3 gap-3 mb-4">
            <div className="bg-elevated rounded-lg p-3 text-center">
              <p className="text-lg font-bold text-heading">{typed.totalExpectations}</p>
              <p className="text-[10px] text-muted">{t('contracts.cdct.total', 'Total Expectations')}</p>
            </div>
            <div className="bg-elevated rounded-lg p-3 text-center">
              <p className="text-lg font-bold text-green-400">{typed.compatible}</p>
              <p className="text-[10px] text-muted">{t('contracts.cdct.compatible', 'Compatible')}</p>
            </div>
            <div className="bg-elevated rounded-lg p-3 text-center">
              <p className="text-lg font-bold text-red-400">{typed.incompatible}</p>
              <p className="text-[10px] text-muted">{t('contracts.cdct.incompatible', 'Incompatible')}</p>
            </div>
          </div>

          <div className="space-y-2">
            {typed.results?.map((r, idx) => (
              <div key={idx} className="flex items-start gap-2 bg-elevated/50 rounded p-2">
                {r.isCompatible ? (
                  <CheckCircle2 size={14} className="text-green-400 mt-0.5 flex-shrink-0" />
                ) : (
                  <XCircle size={14} className="text-red-400 mt-0.5 flex-shrink-0" />
                )}
                <div className="min-w-0">
                  <p className="text-xs font-medium text-heading">{r.consumerServiceName}</p>
                  {!r.isCompatible && (
                    <div className="text-[10px] text-muted mt-0.5">
                      {r.missingPaths?.length > 0 && (
                        <p>
                          <span className="text-red-400">{t('contracts.cdct.missingPaths', 'Missing paths')}: </span>
                          {r.missingPaths.join(', ')}
                        </p>
                      )}
                      {r.missingFields?.length > 0 && (
                        <p>
                          <span className="text-orange-400">{t('contracts.cdct.missingFields', 'Missing fields')}: </span>
                          {r.missingFields.join(', ')}
                        </p>
                      )}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ─── Consumer Expectations ─── */}
      <div className="bg-panel border border-edge rounded-lg p-4">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-xs font-semibold text-heading flex items-center gap-2">
            <Users size={14} className="text-accent" />
            {t('contracts.cdct.expectations', 'Consumer Expectations')}
            {expectationList.length > 0 && (
              <span className="text-[10px] text-muted">({expectationList.length})</span>
            )}
          </h2>
          <button
            type="button"
            onClick={() => setShowNewForm(true)}
            disabled={!apiAssetId}
            className="flex items-center gap-1 text-[10px] font-medium text-accent hover:text-accent/80 transition-colors disabled:opacity-40"
          >
            <Plus size={12} />
            {t('contracts.cdct.addExpectation', 'Register Expectation')}
          </button>
        </div>

        {loadingExpectations ? (
          <p className="text-xs text-muted py-4 text-center">{t('common.loading', 'Loading...')}</p>
        ) : expectationList.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-muted">
            <AlertTriangle size={20} className="mb-2 opacity-30" />
            <p className="text-xs">
              {t('contracts.cdct.noExpectations', 'No consumer expectations registered yet')}
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {expectationList.map((exp: ConsumerExpectation) => (
              <div key={exp.id} className="bg-elevated/50 rounded p-3">
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-medium text-heading">{exp.consumerServiceName}</span>
                  <span className="text-[10px] text-muted">{exp.consumerDomain}</span>
                </div>
                {exp.notes && <p className="text-[10px] text-muted">{exp.notes}</p>}
                <pre className="text-[10px] font-mono text-body mt-1 bg-elevated rounded p-2 overflow-auto max-h-24">
                  {formatJson(exp.expectedSubsetJson)}
                </pre>
              </div>
            ))}
          </div>
        )}

        {/* ─── New Expectation Form ─── */}
        {showNewForm && (
          <div className="mt-4 border-t border-edge pt-4">
            <h3 className="text-xs font-semibold text-heading mb-3">
              {t('contracts.cdct.newExpectation', 'Register New Expectation')}
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-3">
              <div>
                <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1 block">
                  {t('contracts.cdct.consumerService', 'Consumer Service')}
                </label>
                <input
                  type="text"
                  value={newExpectation.consumerServiceName}
                  onChange={(e) => setNewExpectation((p) => ({ ...p, consumerServiceName: e.target.value }))}
                  placeholder="e.g., checkout-service"
                  className="w-full text-xs bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
                />
              </div>
              <div>
                <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1 block">
                  {t('contracts.cdct.consumerDomain', 'Consumer Domain')}
                </label>
                <input
                  type="text"
                  value={newExpectation.consumerDomain}
                  onChange={(e) => setNewExpectation((p) => ({ ...p, consumerDomain: e.target.value }))}
                  placeholder="e.g., Commerce"
                  className="w-full text-xs bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
                />
              </div>
            </div>
            <div className="mb-3">
              <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1 block">
                {t('contracts.cdct.expectedSubset', 'Expected Subset (JSON)')}
              </label>
              <textarea
                value={newExpectation.expectedSubsetJson}
                onChange={(e) => setNewExpectation((p) => ({ ...p, expectedSubsetJson: e.target.value }))}
                rows={6}
                className="w-full text-xs font-mono bg-elevated border border-edge rounded px-3 py-2 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
              />
            </div>
            <div className="mb-3">
              <label className="text-[10px] uppercase tracking-wider text-muted font-medium mb-1 block">
                {t('contracts.cdct.notes', 'Notes')}
              </label>
              <input
                type="text"
                value={newExpectation.notes}
                onChange={(e) => setNewExpectation((p) => ({ ...p, notes: e.target.value }))}
                placeholder={t('contracts.cdct.notesPlaceholder', 'Optional notes about this expectation...')}
                className="w-full text-xs bg-elevated border border-edge rounded px-3 py-1.5 text-body placeholder:text-muted/30 focus:outline-none focus:border-accent"
              />
            </div>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => registerMutation.mutate()}
                disabled={!newExpectation.consumerServiceName || registerMutation.isPending}
                className="flex items-center gap-1.5 text-xs font-medium bg-accent text-white rounded px-3 py-1.5 hover:bg-accent/90 transition-colors disabled:opacity-40"
              >
                <ShieldAlert size={12} />
                {t('contracts.cdct.register', 'Register')}
              </button>
              <button
                type="button"
                onClick={() => setShowNewForm(false)}
                className="text-xs text-muted hover:text-body transition-colors px-3 py-1.5"
              >
                {t('common.cancel', 'Cancel')}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

function formatJson(s: string): string {
  try {
    return JSON.stringify(JSON.parse(s), null, 2);
  } catch {
    return s;
  }
}
