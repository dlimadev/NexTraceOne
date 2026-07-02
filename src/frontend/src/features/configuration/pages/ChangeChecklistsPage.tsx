import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ClipboardList, Plus, Trash2 } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Checkbox } from '../../../components/Checkbox';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

interface ChecklistSummary {
  checklistId: string;
  name: string;
  changeType: string;
  environment?: string;
  isRequired: boolean;
  items: string[];
  createdAt: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useChangeLists = () =>
  useQuery({
    queryKey: ['change-checklists'],
    queryFn: () =>
      client
        .get<{ items: ChecklistSummary[]; totalCount: number }>('/api/v1/change-checklists')
        .then((r) => r.data),
  });

const useCreateChecklist = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      name: string;
      changeType: string;
      environment?: string;
      isRequired: boolean;
      items: string[];
    }) => client.post('/change-checklists', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['change-checklists'] }),
  });
};

const useDeleteChecklist = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => client.delete(`/change-checklists/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['change-checklists'] }),
  });
};

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * ChangeChecklistsPage — gestão de checklists de mudança customizáveis por tenant.
 * Admins definem checklists por tipo de mudança, criticidade e ambiente.
 * Pilar: Platform Customization — Workflows & Change Governance
 */
export function ChangeChecklistsPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useChangeLists();
  const createChecklist = useCreateChecklist();
  const deleteChecklist = useDeleteChecklist();

  const [showBuilder, setShowBuilder] = useState(false);
  const [checklistName, setChecklistName] = useState('');
  const [changeType, setChangeType] = useState('standard');
  const [environment, setEnvironment] = useState('');
  const [isRequired, setIsRequired] = useState(true);
  const [itemsText, setItemsText] = useState('');

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const canCreate = checklistName.trim().length > 0 && changeType.trim().length > 0;

  const handleCreate = () => {
    if (!canCreate) return;
    const items = itemsText
      .split('\n')
      .map((s) => s.trim())
      .filter(Boolean);
    createChecklist.mutate(
      {
        name: checklistName.trim(),
        changeType: changeType.trim(),
        environment: environment.trim() || undefined,
        isRequired,
        items,
      },
      {
        onSuccess: () => {
          setShowBuilder(false);
          setChecklistName('');
          setChangeType('standard');
          setEnvironment('');
          setIsRequired(true);
          setItemsText('');
        },
      }
    );
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('workflows.checklists.title')}
        actions={
          <Button size="sm" onClick={() => setShowBuilder(true)}>
            <Plus className="w-4 h-4 mr-1" />
            {t('workflows.checklists.create')}
          </Button>
        }
      />

      <PageSection>
        {!data?.items.length ? (
          <EmptyState
            icon={<ClipboardList className="w-8 h-8 text-muted" />}
            title={t('workflows.checklists.empty')}
            action={
              <Button size="sm" onClick={() => setShowBuilder(true)}>
                {t('workflows.checklists.create')}
              </Button>
            }
          />
        ) : (
          <div className="space-y-3">
            {data.items.map((cl) => (
              <Card key={cl.checklistId}>
                <CardBody>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <p className="text-sm font-medium text-heading">
                          {cl.name}
                        </p>
                        <Badge variant={cl.isRequired ? 'warning' : 'neutral'}>
                          {cl.isRequired
                            ? t('workflows.checklists.required')
                            : t('workflows.checklists.optional')}
                        </Badge>
                        <Badge variant="info">{cl.changeType}</Badge>
                        {cl.environment && (
                          <Badge variant="neutral">{cl.environment}</Badge>
                        )}
                      </div>
                      {cl.items.length > 0 && (
                        <ul className="mt-1 space-y-0.5">
                          {cl.items.map((item, idx) => (
                            <li
                              key={idx}
                              className="text-xs text-muted flex items-center gap-1"
                            >
                              <span className="w-1 h-1 rounded-full bg-muted inline-block" />
                              {item}
                            </li>
                          ))}
                        </ul>
                      )}
                    </div>
                    <button
                      onClick={() => deleteChecklist.mutate(cl.checklistId)}
                      className="text-muted hover:text-critical transition-colors ml-2 flex-shrink-0"
                      aria-label={t('common.delete')}
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      {showBuilder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-elevated rounded-lg shadow-xl w-full max-w-md p-6">
            <h2 className="text-lg font-semibold text-heading mb-4">
              {t('workflows.checklists.create')}
            </h2>

            <div className="space-y-4">
              <TextField
                label={t('common.name')}
                type="text"
                value={checklistName}
                onChange={(e) => setChecklistName(e.target.value)}
              />

              <div className="grid grid-cols-2 gap-2">
                <TextField
                  label={t('common.type')}
                  type="text"
                  value={changeType}
                  onChange={(e) => setChangeType(e.target.value)}
                  placeholder={t('configuration.checklists.categoryPlaceholder', 'standard')}
                />
                <TextField
                  label={t('common.environment')}
                  type="text"
                  value={environment}
                  onChange={(e) => setEnvironment(e.target.value)}
                  placeholder={t('configuration.checklists.environmentPlaceholder', 'production')}
                />
              </div>

              <Checkbox
                label={t('workflows.checklists.required')}
                checked={isRequired}
                onChange={(e) => setIsRequired(e.target.checked)}
              />

              <TextArea
                label={`${t('common.items')} (${t('common.onePerLine')})`}
                value={itemsText}
                onChange={(e) => setItemsText(e.target.value)}
                rows={4}
                placeholder={t('configuration.checklists.itemsPlaceholder', 'Review test results\nCheck rollback plan\nNotify stakeholders')}
              />
            </div>

            <div className="mt-6 flex items-center justify-between">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setShowBuilder(false);
                  setChecklistName('');
                  setChangeType('standard');
                  setEnvironment('');
                  setIsRequired(true);
                  setItemsText('');
                }}
              >
                {t('common.cancel')}
              </Button>
              <Button
                size="sm"
                disabled={!canCreate || createChecklist.isPending}
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
