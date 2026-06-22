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
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, TextArea } from '../../../shared/ui';
import {
  ShieldAlert,
  Plus,
  CheckCircle2,
  XCircle,
  Info,
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
    <PageContainer>
      {/* ─── Header ─── */}
      <PageHeader
        title={t('contracts.cdct.title', 'Consumer-Driven Contract Testing')}
        subtitle={t(
          'contracts.cdct.subtitle',
          'Register consumer expectations and verify provider contracts satisfy them — prevent breaking changes.'
        )}
      />

      {/* ─── Contract Selector ─── */}
      <div className="bg-panel border border-edge rounded-lg p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <TextField
            size="sm"
            label={t('contracts.cdct.apiAssetId', 'API Asset ID')}
            value={apiAssetId}
            onChange={(e) => setApiAssetId(e.target.value)}
            placeholder={t('contracts.cdct.apiAssetIdPlaceholder', 'Provider API Asset ID...')}
          />
          <div>
            <div className="flex gap-2 items-end">
              <div className="flex-1">
                <TextField
                  size="sm"
                  label={t('contracts.cdct.versionId', 'Contract Version ID')}
                  value={versionId}
                  onChange={(e) => setVersionId(e.target.value)}
                  placeholder={t('contracts.cdct.versionIdPlaceholder', 'Version to verify...')}
                />
              </div>
              <Button
                variant="primary"
                size="sm"
                icon={<RefreshCw size={12} className={loadingCdct ? 'animate-spin' : ''} />}
                onClick={() => runCdct()}
                disabled={!apiAssetId || !versionId || loadingCdct}
              >
                {t('contracts.cdct.verify', 'Verify')}
              </Button>
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
              <p className="text-lg font-bold text-success">{typed.compatible}</p>
              <p className="text-[10px] text-muted">{t('contracts.cdct.compatible', 'Compatible')}</p>
            </div>
            <div className="bg-elevated rounded-lg p-3 text-center">
              <p className="text-lg font-bold text-critical">{typed.incompatible}</p>
              <p className="text-[10px] text-muted">{t('contracts.cdct.incompatible', 'Incompatible')}</p>
            </div>
          </div>

          <div className="space-y-2">
            {typed.results?.map((r, idx) => (
              <div key={idx} className="flex items-start gap-2 bg-elevated/50 rounded p-2">
                {r.isCompatible ? (
                  <CheckCircle2 size={14} className="text-success mt-0.5 flex-shrink-0" />
                ) : (
                  <XCircle size={14} className="text-critical mt-0.5 flex-shrink-0" />
                )}
                <div className="min-w-0">
                  <p className="text-xs font-medium text-heading">{r.consumerServiceName}</p>
                  {!r.isCompatible && (
                    <div className="text-[10px] text-muted mt-0.5">
                      {r.missingPaths?.length > 0 && (
                        <p>
                          <span className="text-critical">{t('contracts.cdct.missingPaths', 'Missing paths')}: </span>
                          {r.missingPaths.join(', ')}
                        </p>
                      )}
                      {r.missingFields?.length > 0 && (
                        <p>
                          <span className="text-warning">{t('contracts.cdct.missingFields', 'Missing fields')}: </span>
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
          <Button
            variant="ghost"
            size="sm"
            icon={<Plus size={12} />}
            onClick={() => setShowNewForm(true)}
            disabled={!apiAssetId}
          >
            {t('contracts.cdct.addExpectation', 'Register Expectation')}
          </Button>
        </div>

        {loadingExpectations ? (
          <p className="text-xs text-muted py-4 text-center">{t('common.loading', 'Loading...')}</p>
        ) : expectationList.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-muted">
            <Info size={20} className="mb-2 opacity-30" />
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
              <TextField
                size="sm"
                label={t('contracts.cdct.consumerService', 'Consumer Service')}
                value={newExpectation.consumerServiceName}
                onChange={(e) => setNewExpectation((p) => ({ ...p, consumerServiceName: e.target.value }))}
                placeholder={t('contracts.cdct.placeholder.serviceName', 'e.g., checkout-service')}
              />
              <TextField
                size="sm"
                label={t('contracts.cdct.consumerDomain', 'Consumer Domain')}
                value={newExpectation.consumerDomain}
                onChange={(e) => setNewExpectation((p) => ({ ...p, consumerDomain: e.target.value }))}
                placeholder={t('contracts.cdct.placeholder.domain', 'e.g., Commerce')}
              />
            </div>
            <div className="mb-3">
              <TextArea
                label={t('contracts.cdct.expectedSubset', 'Expected Subset (JSON)')}
                value={newExpectation.expectedSubsetJson}
                onChange={(e) => setNewExpectation((p) => ({ ...p, expectedSubsetJson: e.target.value }))}
                rows={6}
                textareaClassName="font-mono"
              />
            </div>
            <div className="mb-3">
              <TextField
                size="sm"
                label={t('contracts.cdct.notes', 'Notes')}
                value={newExpectation.notes}
                onChange={(e) => setNewExpectation((p) => ({ ...p, notes: e.target.value }))}
                placeholder={t('contracts.cdct.notesPlaceholder', 'Optional notes about this expectation...')}
              />
            </div>
            <div className="flex gap-2">
              <Button
                variant="primary"
                size="sm"
                icon={<ShieldAlert size={12} />}
                onClick={() => registerMutation.mutate()}
                disabled={!newExpectation.consumerServiceName || registerMutation.isPending}
              >
                {t('contracts.cdct.register', 'Register')}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowNewForm(false)}
              >
                {t('common.cancel', 'Cancel')}
              </Button>
            </div>
          </div>
        )}
      </div>
    </PageContainer>
  );
}

function formatJson(s: string): string {
  try {
    return JSON.stringify(JSON.parse(s), null, 2);
  } catch {
    return s;
  }
}
