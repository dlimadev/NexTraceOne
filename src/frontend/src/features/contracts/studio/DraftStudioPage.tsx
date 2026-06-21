import { useState, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Save,
  Send,
  Download,
  Code,
  Settings,
  ScanSearch,
  ChevronLeft,
} from 'lucide-react';
import { ContractSection } from '../workspace/sections/ContractSection';
import { DraftValidationPanel } from '../workspace/sections/DraftValidationPanel';
import { Card, CardBody } from '../../../components/Card';
import { contractStudioApi } from '../api/contractStudio';
import { useDraftExport } from '../hooks/useDraftExport';
import { useDraftValidation } from '../hooks/useDraftValidation';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button, TextField, TextArea, Select, Tabs } from '../../../shared/ui';
import { useAuth } from '../../../contexts/AuthContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import { DraftIdentityCard } from './components/DraftIdentityCard';
import type { ContractProtocol } from '../types';

const draftKeys = {
  detail: (id: string) => ['contract-drafts', 'detail', id] as const,
};

type DraftTab = 'spec' | 'metadata' | 'validation';

/**
 * Página do studio de edição de draft de contrato.
 * Permite editar conteúdo do artefato, metadados e submeter para revisão.
 * Acedida após criação de um novo draft via CreateServicePage.
 */
export function DraftStudioPage() {
  const { draftId } = useParams<{ draftId: string }>();
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const currentActor = user?.email || user?.fullName || user?.id || 'system';
  const { exportDraft, isExporting, exportError } = useDraftExport();
  const draftValidation = useDraftValidation();

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

  const handleRunValidation = useCallback(() => {
    if (!specContent.trim() || !draft) return;
    draftValidation.validateAll(
      specContent,
      format,
      draft.protocol as ContractProtocol,
    );
  }, [specContent, format, draft, draftValidation]);

  if (draftQuery.isLoading) {
    return <PageContainer><PageLoadingState size="lg" /></PageContainer>;
  }

  if (draftQuery.isError || !draft) {
    return (
      <PageContainer>
        <PageErrorState
          message={t('contracts.studio.draftNotFound', 'Draft not found or failed to load.')}
          action={<Link to="/contracts" className="text-sm text-accent hover:underline">{t('contracts.studio.backToCatalog', 'Back to Contracts')}</Link>}
        />
      </PageContainer>
    );
  }

  const isEditable = draft.status === 'Editing';
  const isSaving = saveContentMutation.isPending || saveMetadataMutation.isPending;
  const validationIssueCount = draftValidation.state.summary.totalIssues;
  const linkedServiceName = services.find((s) => s.serviceId === serviceId)?.displayName;

  const tabItems = [
    { id: 'spec', label: t('contracts.studio.tabSpec', 'Spec'), icon: <Code size={13} /> },
    { id: 'metadata', label: t('contracts.studio.tabMetadata', 'Metadata'), icon: <Settings size={13} /> },
    { id: 'validation', label: `${t('contracts.draftValidation.tabValidation', 'Validation')}${validationIssueCount ? ` (${validationIssueCount > 99 ? '99+' : validationIssueCount})` : ''}`, icon: <ScanSearch size={13} /> },
  ];

  return (
    <PageContainer className="animate-fade-in">
      <div className="mb-4">
        <Link to="/contracts" className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors">
          <ChevronLeft size={14} /> {t('contracts.title', 'Contracts')}
        </Link>
      </div>

      <PageHeader
        title={draft.title}
        subtitle={`${draft.protocol} · v${draft.proposedVersion}`}
        actions={
          <div className="flex items-center gap-2">
            {draftId && specContent.trim() && (
              <Button variant="outline" size="sm" icon={<Download size={14} />} loading={isExporting} onClick={() => exportDraft(draftId)}>
                {t('contracts.studio.exportDraft', 'Export')}
              </Button>
            )}
            {isEditable && (
              <>
                <Button variant="outline" size="sm" icon={<Save size={14} />} loading={isSaving}
                  onClick={() => (activeTab === 'metadata' ? saveMetadataMutation.mutate() : saveContentMutation.mutate())}>
                  {t('common.save', 'Save')}
                </Button>
                <Button variant="primary" size="sm" icon={<Send size={14} />} loading={submitMutation.isPending}
                  disabled={!specContent.trim()} onClick={() => submitMutation.mutate()}>
                  {t('contracts.studio.submitForReview', 'Submit for Review')}
                </Button>
              </>
            )}
          </div>
        }
      />

      <div className="mb-4 space-y-2">
        {(saveContentMutation.isError || saveMetadataMutation.isError) && (
          <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">{t('contracts.studio.saveFailed', 'Failed to save changes.')}</div>
        )}
        {(saveContentMutation.isSuccess || saveMetadataMutation.isSuccess) && !isSaving && (
          <div className="text-xs text-success bg-success/15 border border-success/25 rounded-md px-3 py-2">{t('contracts.studio.saveSuccess', 'Changes saved successfully.')}</div>
        )}
        {submitMutation.isError && (
          <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">{t('contracts.studio.submitFailed', 'Failed to submit for review.')}</div>
        )}
        {submitMutation.isSuccess && (
          <div className="text-xs text-success bg-success/15 border border-success/25 rounded-md px-3 py-2">{t('contracts.studio.submitSuccess', 'Draft submitted for review successfully.')}</div>
        )}
        {exportError && (
          <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">{t('contracts.studio.exportDraftFailed', 'Failed to export draft.')}</div>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
        <div className="lg:sticky lg:top-4">
          <DraftIdentityCard draft={draft} serviceName={linkedServiceName} />
        </div>

        <div className="min-w-0">
          <Tabs items={tabItems} activeId={activeTab} onChange={(id) => setActiveTab(id as DraftTab)} className="mb-5" />

          {activeTab === 'spec' && (
            <div className="flex flex-col gap-3">
              <div className="flex items-center justify-between">
                <label className="text-xs font-medium text-heading">{t('contracts.studio.specContent', 'Specification Content')}</label>
                <Select value={format} onChange={(e) => setDraftFormat(e.target.value)} disabled={!isEditable}
                  options={[{ value: 'yaml', label: 'YAML' }, { value: 'json', label: 'JSON' }, { value: 'xml', label: 'XML' }]} className="w-32" />
              </div>
              <ContractSection key={saveKey} specContent={specContent} format={format} protocol={draft.protocol}
                contractType={draft.contractType} isReadOnly={!isEditable} onContentChange={setDraftSpecContent}
                className="border border-edge rounded-lg overflow-hidden h-[60vh] min-h-[420px]" />
            </div>
          )}

          {activeTab === 'validation' && (
            <DraftValidationPanel state={draftValidation.state} isRunning={draftValidation.isRunning}
              protocol={draft.protocol as ContractProtocol} onRunValidation={handleRunValidation} />
          )}

          {activeTab === 'metadata' && (
            <Card>
              <CardBody className="space-y-4 max-w-2xl">
                <TextField label={t('contracts.studio.draftTitle', 'Title')} value={title} onChange={(e) => setDraftTitle(e.target.value)} disabled={!isEditable} />
                <TextArea label={t('contracts.studio.draftDescription', 'Description')} value={description} onChange={(e) => setDraftDescription(e.target.value)} disabled={!isEditable} rows={3} />
                <TextField label={t('contracts.studio.proposedVersion', 'Proposed Version')} value={proposedVersion} onChange={(e) => setDraftProposedVersion(e.target.value)} disabled={!isEditable} />
                <Select label={t('contracts.studio.linkedService', 'Linked Service')} value={serviceId} onChange={(e) => setDraftServiceId(e.target.value)} disabled={!isEditable}
                  options={[{ value: '', label: t('contracts.studio.linkedServiceOptional', 'No linked service yet') }, ...services.map((s) => ({ value: s.serviceId, label: `${s.displayName} · ${s.domain} · ${s.teamName}` }))]}
                  helperText={t('contracts.studio.linkedServiceHint', 'Publishing requires a real catalog link. Select a service to let Contracts create or reuse the correct API asset.')} />
              </CardBody>
            </Card>
          )}
        </div>
      </div>
    </PageContainer>
  );
}
