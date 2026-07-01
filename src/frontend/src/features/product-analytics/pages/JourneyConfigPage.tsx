import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Globe, Building2, CheckCircle2, XCircle, Route } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Checkbox } from '../../../components/Checkbox';
import { Modal } from '../../../components/Modal';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';
import type {
  JourneyDefinitionDto,
  CreateJourneyDefinitionRequest,
  UpdateJourneyDefinitionRequest,
} from '../api/productAnalyticsApi';

/**
 * Página de configuração de definições de jornadas.
 *
 * Permite criar, editar e eliminar definições de jornadas e funis configuráveis
 * por tenant. As definições tenant sobrepõem-se às definições globais.
 * Requer permissão analytics:configure.
 *
 * @see docs/analysis/PRODUCT-ANALYTICS-IMPROVEMENT-PLAN.md — FEAT-03
 */

const DEFAULT_STEPS_JSON = JSON.stringify(
  [
    { stepId: 'step_1', stepName: 'Step 1', eventType: 'ModuleViewed', order: 1 },
    { stepId: 'step_2', stepName: 'Step 2', eventType: 'FeatureUsed', order: 2 },
  ],
  null,
  2,
);

interface JourneyFormState {
  name: string;
  key: string;
  stepsJson: string;
  isActive: boolean;
}

function getInitialFormState(): JourneyFormState {
  return { name: '', key: '', stepsJson: DEFAULT_STEPS_JSON, isActive: true };
}

function JourneyFormModal({
  initial,
  onSave,
  onCancel,
  isEdit,
}: {
  initial: JourneyFormState;
  onSave: (form: JourneyFormState) => void;
  onCancel: () => void;
  isEdit: boolean;
}) {
  const { t } = useTranslation();
  const [form, setForm] = useState<JourneyFormState>(initial);
  const [jsonError, setJsonError] = useState<string | null>(null);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    try {
      JSON.parse(form.stepsJson);
      setJsonError(null);
    } catch {
      setJsonError('Invalid JSON in steps field');
      return;
    }
    onSave(form);
  }

  return (
    <Modal
      open
      onClose={onCancel}
      title={isEdit ? t('analytics.journeyConfig.editJourney') : t('analytics.journeyConfig.addJourney')}
      size="lg"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button type="button" variant="secondary" size="sm" onClick={onCancel}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" size="sm" form="journey-form">
            {t('common.save')}
          </Button>
        </div>
      }
    >
      <form id="journey-form" onSubmit={handleSubmit} className="space-y-4">
        {/* Name */}
        <TextField
          label={`${t('analytics.journeyConfig.journeyName')} *`}
          type="text"
          required
          maxLength={100}
          value={form.name}
          onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
        />

        {/* Key */}
        {!isEdit && (
          <TextField
            label={`${t('analytics.journeyConfig.journeyId')} *`}
            type="text"
            required
            maxLength={50}
            pattern="[a-z0-9_]+"
            value={form.key}
            onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))}
            placeholder="e.g. my_custom_journey"
            helperText="Lowercase letters, numbers and underscores only. At least 1 character required."
          />
        )}

        {/* Steps JSON */}
        <TextArea
          label={`${t('analytics.journeyConfig.steps')} *`}
          required
          rows={10}
          value={form.stepsJson}
          onChange={(e) => setForm((f) => ({ ...f, stepsJson: e.target.value }))}
          textareaClassName="font-mono"
          error={jsonError ?? undefined}
        />

        {/* Active */}
        <Checkbox
          label={t('analytics.journeyConfig.isActive')}
          checked={form.isActive}
          onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
        />
      </form>
    </Modal>
  );
}

export function JourneyConfigPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [modal, setModal] = useState<null | { mode: 'create' } | { mode: 'edit'; item: JourneyDefinitionDto }>(null);
  const [deleteTarget, setDeleteTarget] = useState<JourneyDefinitionDto | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-journey-definitions'],
    queryFn: () => productAnalyticsApi.listJourneyDefinitions(),
    staleTime: 30_000,
  });

  const createMutation = useMutation({
    mutationFn: (req: CreateJourneyDefinitionRequest) => productAnalyticsApi.createJourneyDefinition(req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['product-analytics-journey-definitions'] });
      setModal(null);
      setStatusMessage(t('analytics.journeyConfig.saveSuccess'));
      setTimeout(() => setStatusMessage(null), 3000);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, req }: { id: string; req: UpdateJourneyDefinitionRequest }) =>
      productAnalyticsApi.updateJourneyDefinition(id, req),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['product-analytics-journey-definitions'] });
      setModal(null);
      setStatusMessage(t('analytics.journeyConfig.saveSuccess'));
      setTimeout(() => setStatusMessage(null), 3000);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => productAnalyticsApi.deleteJourneyDefinition(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['product-analytics-journey-definitions'] });
      setDeleteTarget(null);
      setStatusMessage(t('analytics.journeyConfig.deleteSuccess'));
      setTimeout(() => setStatusMessage(null), 3000);
    },
  });

  function handleSave(form: JourneyFormState) {
    const steps = JSON.parse(form.stepsJson);
    if (modal?.mode === 'edit') {
      updateMutation.mutate({ id: modal.item.id, req: { name: form.name, steps, isActive: form.isActive } });
    } else {
      createMutation.mutate({ name: form.name, key: form.key, steps, isActive: form.isActive });
    }
  }

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('common.loading')} />
      </PageContainer>
    );
  }

  if (isError) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <Button type="button" variant="secondary" size="sm" onClick={() => refetch()}>
              {t('common.retry')}
            </Button>
          }
        />
      </PageContainer>
    );
  }

  const definitions = data ?? [];

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.journeyConfig.title')}
        subtitle={t('analytics.journeyConfig.subtitle')}
        actions={
          <Button
            type="button"
            size="sm"
            icon={<Plus className="w-3.5 h-3.5" />}
            onClick={() => setModal({ mode: 'create' })}
          >
            {t('analytics.journeyConfig.addJourney')}
          </Button>
        }
      />

      {/* Status toast */}
      {statusMessage && (
        <div className="mb-4 flex items-center gap-2 px-4 py-2 rounded-md bg-success/10 border border-success/30 text-success text-xs">
          <CheckCircle2 className="w-3.5 h-3.5 shrink-0" />
          {statusMessage}
        </div>
      )}

      <Card>
        {definitions.length === 0 ? (
          <CardBody>
            <EmptyState
              icon={<Route />}
              title={t('analytics.journeyConfig.empty.title')}
              description={t('analytics.journeyConfig.empty.description')}
              action={
                <Button
                  type="button"
                  size="sm"
                  icon={<Plus className="w-3.5 h-3.5" />}
                  onClick={() => setModal({ mode: 'create' })}
                >
                  {t('analytics.journeyConfig.addJourney')}
                </Button>
              }
            />
          </CardBody>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-edge">
                  <th className="text-left py-3 px-4 text-muted font-medium">{t('analytics.journeyConfig.journeyName')}</th>
                  <th className="text-left py-3 px-4 text-muted font-medium">{t('analytics.journeyConfig.journeyId')}</th>
                  <th className="text-left py-3 px-4 text-muted font-medium">{t('analytics.journeyConfig.steps')}</th>
                  <th className="text-left py-3 px-4 text-muted font-medium">{t('analytics.journeyConfig.isActive')}</th>
                  <th className="text-left py-3 px-4 text-muted font-medium">Scope</th>
                  <th className="py-3 px-4" />
                </tr>
              </thead>
              <tbody>
                {definitions.map((def) => (
                  <tr key={def.id} className="border-b border-edge last:border-0 hover:bg-bg/50 transition-colors">
                    <td className="py-3 px-4 text-heading font-medium">{def.name}</td>
                    <td className="py-3 px-4 text-muted font-mono">{def.key}</td>
                    <td className="py-3 px-4 text-muted">{def.steps.length} step(s)</td>
                    <td className="py-3 px-4">
                      {def.isActive ? (
                        <span className="flex items-center gap-1 text-success">
                          <CheckCircle2 className="w-3 h-3" /> {t('analytics.journeyConfig.isActive')}
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-muted">
                          <XCircle className="w-3 h-3" /> {t('analytics.journeyConfig.inactive')}
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4">
                      {def.isGlobal ? (
                        <span className="flex items-center gap-1 text-muted">
                          <Globe className="w-3 h-3" /> {t('analytics.journeyConfig.global')}
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-accent">
                          <Building2 className="w-3 h-3" /> {t('analytics.journeyConfig.tenant')}
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4">
                      {!def.isGlobal && (
                        <div className="flex items-center justify-end gap-2">
                          <IconButton
                            type="button"
                            variant="ghost"
                            size="sm"
                            icon={<Pencil className="w-3.5 h-3.5" />}
                            onClick={() => setModal({ mode: 'edit', item: def })}
                            label={t('analytics.journeyConfig.editJourney')}
                            title={t('analytics.journeyConfig.editJourney')}
                          />
                          <IconButton
                            type="button"
                            variant="ghost"
                            size="sm"
                            className="hover:text-critical"
                            icon={<Trash2 className="w-3.5 h-3.5" />}
                            onClick={() => setDeleteTarget(def)}
                            label={t('analytics.journeyConfig.deleteJourney')}
                            title={t('analytics.journeyConfig.deleteJourney')}
                          />
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Create / Edit modal */}
      {modal && (
        <JourneyFormModal
          isEdit={modal.mode === 'edit'}
          initial={
            modal.mode === 'edit'
              ? {
                  name: modal.item.name,
                  key: modal.item.key,
                  stepsJson: JSON.stringify(modal.item.steps, null, 2),
                  isActive: modal.item.isActive,
                }
              : getInitialFormState()
          }
          onSave={handleSave}
          onCancel={() => setModal(null)}
        />
      )}

      {/* Delete confirm dialog */}
      <Modal
        open={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        title={t('analytics.journeyConfig.deleteJourney')}
        size="sm"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button type="button" variant="secondary" size="sm" onClick={() => setDeleteTarget(null)}>
              {t('common.cancel')}
            </Button>
            <Button
              type="button"
              variant="danger"
              size="sm"
              onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
              disabled={deleteMutation.isPending}
            >
              {t('common.delete')}
            </Button>
          </div>
        }
      >
        <p className="text-sm text-heading">{t('analytics.journeyConfig.deleteConfirm')}</p>
        <p className="text-xs text-muted font-medium mt-2">{deleteTarget?.name}</p>
      </Modal>
    </PageContainer>
  );
}
