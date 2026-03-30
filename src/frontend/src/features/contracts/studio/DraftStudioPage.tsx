import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ArrowLeft,
  Save,
  Send,
  Download,
  Code,
  Settings,
  Loader2,
} from 'lucide-react';
import { ContractSection } from '../workspace/sections/ContractSection';
import { Card, CardBody } from '../../../components/Card';
import { contractStudioApi } from '../api/contractStudio';
import { useDraftExport } from '../hooks/useDraftExport';
import { PROTOCOL_COLORS, LIFECYCLE_COLORS } from '../shared/constants';
import { cn } from '../../../lib/cn';
import { PageContainer } from '../../../components/shell';
import { useAuth } from '../../../contexts/AuthContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import type { ServiceListItem } from '../types';

const draftKeys = {
  detail: (id: string) => ['contract-drafts', 'detail', id] as const,
};

type DraftTab = 'spec' | 'metadata';

/**
 * Página do studio de edição de draft de contrato.
 * Permite editar conteúdo do artefato, metadados e submeter para revisão.
 * Acedida após criação de um novo draft via CreateServicePage.
 */
export function DraftStudioPage() {
  const { draftId } = useParams<{ draftId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const currentActor = user?.email || user?.fullName || user?.id || 'system';
  const { exportDraft, isExporting, exportError } = useDraftExport();

  const [activeTab, setActiveTab] = useState<DraftTab>('spec');
  const [saveKey, setSaveKey] = useState(0);
  const [draftSpecContent, setDraftSpecContent] = useState<string | null>(null);
  const [draftFormat, setDraftFormat] = useState<string | null>(null);
  const [draftTitle, setDraftTitle] = useState<string | null>(null);
  const [draftDescription, setDraftDescription] = useState<string | null>(null);
  const [draftProposedVersion, setDraftProposedVersion] = useState<string | null>(null);
  const [draftServiceId, setDraftServiceId] = useState<string | null>(null);

  const draftQuery = useQuery({
    queryKey: draftKeys.detail(draftId ?? ''),
    queryFn: () => contractStudioApi.getDraft(draftId!),
    enabled: !!draftId,
  });

  const servicesQuery = useQuery({
    queryKey: ['catalog-services-for-contract-drafts'],
    queryFn: () => serviceCatalogApi.listServices(),
  });

  const draft = draftQuery.data;
  const specContent = draftSpecContent ?? draft?.specContent ?? '';
  const format = draftFormat ?? draft?.format ?? 'yaml';
  const title = draftTitle ?? draft?.title ?? '';
  const description = draftDescription ?? draft?.description ?? '';
  const proposedVersion = draftProposedVersion ?? draft?.proposedVersion ?? '1.0.0';
  const serviceId = draftServiceId ?? draft?.serviceId ?? '';
  const services = servicesQuery.data?.items ?? [];

  const resetOverrides = () => {
    setDraftSpecContent(null);
    setDraftFormat(null);
    setDraftTitle(null);
    setDraftDescription(null);
    setDraftProposedVersion(null);
    setDraftServiceId(null);
  };

  const saveContentMutation = useMutation({
    mutationFn: () =>
      contractStudioApi.updateContent(draftId!, {
        specContent,
        format,
        editedBy: currentActor,
      }),
    onSuccess: async () => {
      resetOverrides();
      setSaveKey((k) => k + 1);
      await queryClient.invalidateQueries({ queryKey: draftKeys.detail(draftId!) });
    },
  });

  const saveMetadataMutation = useMutation({
    mutationFn: () =>
      contractStudioApi.updateMetadata(draftId!, {
        title,
        description,
        proposedVersion,
        serviceId: serviceId || undefined,
        editedBy: currentActor,
      }),
    onSuccess: async () => {
      resetOverrides();
      await queryClient.invalidateQueries({ queryKey: draftKeys.detail(draftId!) });
    },
  });

  const submitMutation = useMutation({
    mutationFn: () => contractStudioApi.submitForReview(draftId!),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: draftKeys.detail(draftId!) });
    },
  });

  if (draftQuery.isLoading) {
    return (
      <PageContainer>
        <div className="flex items-center justify-center min-h-[50vh]">
          <div className="flex items-center gap-2 text-muted">
            <Loader2 size={20} className="animate-spin" />
            <span className="text-sm">{t('common.loading', 'Loading...')}</span>
          </div>
        </div>
      </PageContainer>
    );
  }

  if (draftQuery.isError || !draft) {
    return (
      <PageContainer>
        <div className="flex flex-col items-center justify-center min-h-[50vh] gap-4">
          <p className="text-sm text-danger">{t('contracts.studio.draftNotFound', 'Draft not found or failed to load.')}</p>
          <button
            onClick={() => navigate('/contracts')}
            className="text-sm text-accent hover:underline"
          >
            {t('contracts.studio.backToCatalog', 'Back to Contracts')}
          </button>
        </div>
      </PageContainer>
    );
  }

  const isEditable = draft.status === 'Editing';
  const isSaving = saveContentMutation.isPending || saveMetadataMutation.isPending;

  const TABS: { id: DraftTab; labelKey: string; Icon: React.ComponentType<{ size?: number }> }[] = [
    { id: 'spec', labelKey: 'contracts.studio.tabSpec', Icon: Code },
    { id: 'metadata', labelKey: 'contracts.studio.tabMetadata', Icon: Settings },
  ];

  return (
    <div className="flex flex-col h-full">
      {/* ── Header ── */}
      <div className="flex items-center justify-between px-4 sm:px-6 py-4 border-b border-edge bg-panel">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/contracts')}
            className="text-muted hover:text-heading transition-colors"
          >
            <ArrowLeft size={18} />
          </button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-base font-semibold text-heading">{draft.title}</h1>
            </div>
            <div className="flex items-center gap-2 mt-0.5">
              <span className={cn('inline-flex items-center px-2 py-0.5 text-[10px] font-medium rounded', PROTOCOL_COLORS[draft.protocol] ?? 'bg-muted/15 text-muted')}>
                {draft.protocol}
              </span>
              <span className={cn('inline-flex items-center px-2 py-0.5 text-[10px] font-medium rounded', LIFECYCLE_COLORS[draft.status] ?? 'bg-muted/15 text-muted')}>
                {t(`contracts.draftStatus.${draft.status}`, draft.status)}
              </span>
              <span className="text-[10px] text-muted">v{draft.proposedVersion}</span>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {draftId && specContent.trim() && (
            <button
              onClick={() => exportDraft(draftId)}
              disabled={isExporting}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md bg-elevated text-muted hover:text-heading disabled:opacity-40 transition-colors border border-edge"
            >
              {isExporting ? <Loader2 size={12} className="animate-spin" /> : <Download size={12} />}
              {t('contracts.studio.exportDraft', 'Export')}
            </button>
          )}
          {isEditable && (
            <>
              <button
                onClick={() => activeTab === 'metadata' ? saveMetadataMutation.mutate() : saveContentMutation.mutate()}
                disabled={isSaving}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md bg-elevated text-heading hover:bg-elevated/80 disabled:opacity-40 transition-colors border border-edge"
              >
                {isSaving ? <Loader2 size={12} className="animate-spin" /> : <Save size={12} />}
                {t('common.save', 'Save')}
              </button>
              <button
                onClick={() => submitMutation.mutate()}
                disabled={submitMutation.isPending || !specContent.trim()}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-40 transition-colors"
              >
                {submitMutation.isPending ? <Loader2 size={12} className="animate-spin" /> : <Send size={12} />}
                {t('contracts.studio.submitForReview', 'Submit for Review')}
              </button>
            </>
          )}
        </div>
      </div>

      {/* ── Tab Bar ── */}
      <div className="flex items-center gap-1 px-4 sm:px-6 pt-3 border-b border-edge bg-panel">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={cn(
              'inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium transition-colors border-b-2 -mb-px',
              activeTab === tab.id
                ? 'text-accent border-accent'
                : 'text-muted hover:text-heading border-transparent',
            )}
          >
            <tab.Icon size={13} />
            {t(tab.labelKey, tab.id)}
          </button>
        ))}
      </div>

      {/* ── Content ── */}
      <div className="flex-1 overflow-y-auto p-6">
        {/* Feedback messages */}
        {(saveContentMutation.isError || saveMetadataMutation.isError) && (
          <div className="mb-4 text-xs text-red-400 bg-red-900/10 border border-red-700/20 rounded-md px-3 py-2">
            {t('contracts.studio.saveFailed', 'Failed to save changes.')}
          </div>
        )}
        {(saveContentMutation.isSuccess || saveMetadataMutation.isSuccess) && !isSaving && (
          <div className="mb-4 text-xs text-emerald-400 bg-emerald-900/10 border border-emerald-700/20 rounded-md px-3 py-2">
            {t('contracts.studio.saveSuccess', 'Changes saved successfully.')}
          </div>
        )}
        {submitMutation.isError && (
          <div className="mb-4 text-xs text-red-400 bg-red-900/10 border border-red-700/20 rounded-md px-3 py-2">
            {t('contracts.studio.submitFailed', 'Failed to submit for review.')}
          </div>
        )}
        {submitMutation.isSuccess && (
          <div className="mb-4 text-xs text-emerald-400 bg-emerald-900/10 border border-emerald-700/20 rounded-md px-3 py-2">
            {t('contracts.studio.submitSuccess', 'Draft submitted for review successfully.')}
          </div>
        )}
        {exportError && (
          <div className="mb-4 text-xs text-red-400 bg-red-900/10 border border-red-700/20 rounded-md px-3 py-2">
            {t('contracts.studio.exportDraftFailed', 'Failed to export draft.')}
          </div>
        )}

        {/* Tab: Spec Content */}
        {activeTab === 'spec' && (
          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <label className="text-xs font-medium text-heading">
                {t('contracts.studio.specContent', 'Specification Content')}
              </label>
              <select
                value={format}
                onChange={(e) => setDraftFormat(e.target.value)}
                disabled={!isEditable}
                className="text-xs bg-elevated border border-edge rounded px-2 py-1 text-body"
              >
                <option value="yaml">YAML</option>
                <option value="json">JSON</option>
                <option value="xml">XML</option>
              </select>
            </div>
            <ContractSection
              key={saveKey}
              specContent={specContent}
              format={format}
              protocol={draft.protocol}
              contractType={draft.contractType}
              isReadOnly={!isEditable}
              onContentChange={setDraftSpecContent}
              className="border border-edge rounded-lg overflow-hidden min-h-[560px]"
            />
          </div>
        )}

        {/* Tab: Metadata */}
        {activeTab === 'metadata' && (
          <Card>
            <CardBody className="space-y-4 max-w-2xl">
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.studio.draftTitle', 'Title')}
                </label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setDraftTitle(e.target.value)}
                  readOnly={!isEditable}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.studio.draftDescription', 'Description')}
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDraftDescription(e.target.value)}
                  readOnly={!isEditable}
                  rows={3}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.studio.proposedVersion', 'Proposed Version')}
                </label>
                <input
                  type="text"
                  value={proposedVersion}
                  onChange={(e) => setDraftProposedVersion(e.target.value)}
                  readOnly={!isEditable}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.studio.linkedService', 'Linked Service')}
                </label>
                <select
                  value={serviceId}
                  onChange={(e) => setDraftServiceId(e.target.value)}
                  disabled={!isEditable}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                >
                  <option value="">{t('contracts.studio.linkedServiceOptional', 'No linked service yet')}</option>
                  {services.map((service: ServiceListItem) => (
                    <option key={service.serviceId} value={service.serviceId}>
                      {service.displayName} · {service.domain} · {service.teamName}
                    </option>
                  ))}
                </select>
                <p className="mt-1 text-[10px] text-muted">
                  {t('contracts.studio.linkedServiceHint', 'Publishing requires a real catalog link. Select a service to let Contracts create or reuse the correct API asset.')}
                </p>
              </div>
              <div className="pt-2 space-y-2 text-xs text-muted">
                <p>{t('contracts.studio.contractType', 'Type')}: <span className="text-heading font-medium">{draft.contractType}</span></p>
                <p>{t('contracts.studio.protocol', 'Protocol')}: <span className="text-heading font-medium">{draft.protocol}</span></p>
                <p>{t('contracts.studio.author', 'Author')}: <span className="text-heading font-medium">{draft.author}</span></p>
                {draft.createdAt && (
                  <p>{t('contracts.studio.createdAt', 'Created')}: <span className="text-heading font-medium">{new Date(draft.createdAt).toLocaleString()}</span></p>
                )}
                {draft.lastEditedAt && (
                  <p>{t('contracts.studio.lastEdited', 'Last edited')}: <span className="text-heading font-medium">{new Date(draft.lastEditedAt).toLocaleString()}{draft.lastEditedBy ? ` · ${draft.lastEditedBy}` : ''}</span></p>
                )}
              </div>
            </CardBody>
          </Card>
        )}
      </div>
    </div>
  );
}
