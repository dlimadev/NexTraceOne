import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { FileCode, Plus, Trash2 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

type ContractType = 'REST' | 'SOAP' | 'Event' | 'AsyncAPI' | 'Background';

interface TemplateSummary {
  templateId: string;
  name: string;
  contractType: ContractType;
  description: string;
  isBuiltIn: boolean;
  createdAt: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useContractTemplates = (type?: string) =>
  useQuery({
    queryKey: ['contract-templates', type],
    queryFn: () =>
      client
        .get<{ items: TemplateSummary[]; totalCount: number }>('/api/v1/contract-templates', {
          params: type && type !== 'All' ? { type } : undefined,
        })
        .then((r) => r.data),
  });

const useCreateTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      name: string;
      contractType: string;
      templateJson: string;
      description: string;
    }) => client.post('/api/v1/contract-templates', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['contract-templates'] }),
  });
};

const useDeleteTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (templateId: string) => client.delete(`/api/v1/contract-templates/${templateId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['contract-templates'] }),
  });
};

// ── Constants ──────────────────────────────────────────────────────────────────

const CONTRACT_TYPES: ContractType[] = ['REST', 'SOAP', 'Event', 'AsyncAPI', 'Background'];

const TYPE_BADGE: Record<ContractType, 'info' | 'neutral' | 'warning' | 'success' | 'error'> = {
  REST: 'info',
  SOAP: 'neutral',
  Event: 'warning',
  AsyncAPI: 'success',
  Background: 'error',
};

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * ContractTemplatesPage — gestão de templates de contrato customizáveis por tenant.
 * Permite criar templates pré-preenchidos para REST, SOAP, Event, AsyncAPI e Background.
 * Pilar: Platform Customization — Contract Templates
 */
export function ContractTemplatesPage() {
  const { t } = useTranslation();
  const [activeType, setActiveType] = useState<string>('All');
  const [showBuilder, setShowBuilder] = useState(false);
  const [templateName, setTemplateName] = useState('');
  const [contractType, setContractType] = useState<ContractType>('REST');
  const [description, setDescription] = useState('');

  const { data, isLoading, isError } = useContractTemplates(
    activeType !== 'All' ? activeType : undefined
  );
  const createTemplate = useCreateTemplate();
  const deleteTemplate = useDeleteTemplate();

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const canCreate = templateName.trim().length > 0;

  const handleCreate = () => {
    if (!canCreate) return;
    createTemplate.mutate(
      {
        name: templateName.trim(),
        contractType,
        templateJson: '{}',
        description: description.trim(),
      },
      {
        onSuccess: () => {
          setShowBuilder(false);
          setTemplateName('');
          setContractType('REST');
          setDescription('');
        },
      }
    );
  };

  const tabs = ['All', ...CONTRACT_TYPES];

  return (
    <PageContainer>
      <PageHeader
        title={t('workflows.contractTemplates.title')}
        actions={
          <Button size="sm" onClick={() => setShowBuilder(true)}>
            <Plus className="w-4 h-4 mr-1" />
            {t('workflows.contractTemplates.create')}
          </Button>
        }
      />

      <PageSection>
        {/* Type filter tabs */}
        <div className="flex gap-2 mb-4 flex-wrap">
          {tabs.map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveType(tab)}
              className={`text-sm px-3 py-1.5 rounded-lg border transition-colors ${
                activeType === tab
                  ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300'
                  : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-blue-300'
              }`}
            >
              {tab === 'All' ? t('workflows.contractTemplates.all') : tab}
            </button>
          ))}
        </div>

        {!data?.items.length ? (
          <EmptyState
            icon={<FileCode className="w-8 h-8 text-gray-400" />}
            title={t('workflows.contractTemplates.empty')}
            action={
              <Button size="sm" onClick={() => setShowBuilder(true)}>
                {t('workflows.contractTemplates.create')}
              </Button>
            }
          />
        ) : (
          <div className="space-y-3">
            {data.items.map((tmpl) => (
              <Card key={tmpl.templateId}>
                <CardBody>
                  <div className="flex items-center justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                          {tmpl.name}
                        </p>
                        <Badge variant={TYPE_BADGE[tmpl.contractType]}>
                          {tmpl.contractType}
                        </Badge>
                        {tmpl.isBuiltIn && (
                          <Badge variant="neutral">{t('common.default')}</Badge>
                        )}
                      </div>
                      {tmpl.description && (
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                          {tmpl.description}
                        </p>
                      )}
                    </div>
                    {!tmpl.isBuiltIn && (
                      <button
                        onClick={() => deleteTemplate.mutate(tmpl.templateId)}
                        className="text-gray-400 hover:text-red-500 transition-colors"
                        aria-label={t('common.delete')}
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    )}
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      {showBuilder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-md p-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              {t('workflows.contractTemplates.create')}
            </h2>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('common.name')}
                </label>
                <input
                  type="text"
                  value={templateName}
                  onChange={(e) => setTemplateName(e.target.value)}
                  className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-transparent text-gray-700 dark:text-gray-300"
                  placeholder={t('workflows.contractTemplates.title')}
                />
              </div>

              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('workflows.contractTemplates.type')}
                </label>
                <select
                  value={contractType}
                  onChange={(e) => setContractType(e.target.value as ContractType)}
                  className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                >
                  {CONTRACT_TYPES.map((ct) => (
                    <option key={ct} value={ct}>
                      {ct}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('common.description')}
                </label>
                <input
                  type="text"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-transparent text-gray-700 dark:text-gray-300"
                />
              </div>
            </div>

            <div className="mt-6 flex items-center justify-between">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setShowBuilder(false);
                  setTemplateName('');
                  setContractType('REST');
                  setDescription('');
                }}
              >
                {t('common.cancel')}
              </Button>
              <Button
                size="sm"
                disabled={!canCreate || createTemplate.isPending}
                onClick={handleCreate}
              >
                {t('common.save')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
