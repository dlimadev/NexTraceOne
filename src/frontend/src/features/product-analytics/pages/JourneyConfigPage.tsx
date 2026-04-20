import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Pencil, Trash2, Globe, Building2, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
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

function emptyForm(): JourneyFormState {
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
    <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
      <div className="bg-panel border border-edge rounded-xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <form onSubmit={handleSubmit}>
          <div className="flex items-center justify-between px-6 py-4 border-b border-edge">
            <h2 className="text-sm font-semibold text-heading">
              {isEdit ? t('analytics.journeyConfig.editJourney') : t('analytics.journeyConfig.addJourney')}
            </h2>
            <button type="button" onClick={onCancel} className="text-muted hover:text-heading">
              <XCircle className="w-4 h-4" />
            </button>
          </div>

          <div className="px-6 py-4 space-y-4">
            {/* Name */}
            <div>
              <label className="block text-xs text-muted mb-1">{t('analytics.journeyConfig.journeyName')} *</label>
              <input
                type="text"
                required
                maxLength={100}
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                className="w-full bg-bg border border-edge rounded-md px-3 py-2 text-xs text-heading focus:outline-none focus:border-accent/70"
              />
            </div>

            {/* Key */}
            {!isEdit && (
              <div>
                <label className="block text-xs text-muted mb-1">{t('analytics.journeyConfig.journeyId')} *</label>
                <input
                  type="text"
                  required
                  maxLength={50}
                  pattern="[a-z0-9_]+"
                  value={form.key}
                  onChange={(e) => setForm((f) => ({ ...f, key: e.target.value }))}
                  className="w-full bg-bg border border-edge rounded-md px-3 py-2 text-xs text-heading focus:outline-none focus:border-accent/70"
                  placeholder="e.g. my_custom_journey"
                />
                <p className="text-xs text-muted mt-1">Lowercase letters, numbers and underscores only.</p>
              </div>
            )}

            {/* Steps JSON */}
            <div>
              <label className="block text-xs text-muted mb-1">{t('analytics.journeyConfig.steps')} *</label>
              <textarea
                required
                rows={10}
                value={form.stepsJson}
                onChange={(e) => setForm((f) => ({ ...f, stepsJson: e.target.value }))}
                className="w-full bg-bg border border-edge rounded-md px-3 py-2 text-xs text-heading font-mono focus:outline-none focus:border-accent/70"
              />
              {jsonError && <p className="text-xs text-critical mt-1">{jsonError}</p>}
            </div>

            {/* Active */}
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                className="accent-accent"
              />
              <span className="text-xs text-heading">{t('analytics.journeyConfig.isActive')}</span>
            </label>
          </div>

          <div className="flex justify-end gap-2 px-6 py-4 border-t border-edge">
            <button
              type="button"
              onClick={onCancel}
              className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
            >
              {t('common.cancel')}
            </button>
            <button
              type="submit"
              className="px-3 py-2 rounded-md bg-accent text-white text-xs hover:bg-accent/80"
            >
              {t('common.save')}
            </button>
          </div>
        </form>
      </div>
    </div>
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
            <button
              type="button"
              onClick={() => refetch()}
              className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
            >
              {t('common.retry')}
            </button>
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
          <button
            type="button"
            onClick={() => setModal({ mode: 'create' })}
            className="flex items-center gap-2 px-3 py-2 rounded-md bg-accent text-white text-xs hover:bg-accent/80"
          >
            <Plus className="w-3.5 h-3.5" />
            {t('analytics.journeyConfig.addJourney')}
          </button>
        }
      />

      {/* Status toast */}
      {statusMessage && (
        <div className="mb-4 flex items-center gap-2 px-4 py-2 rounded-md bg-success/10 border border-success/30 text-success text-xs">
          <CheckCircle className="w-3.5 h-3.5 shrink-0" />
          {statusMessage}
        </div>
      )}

      <Card>
        {definitions.length === 0 ? (
          <CardBody>
            <div className="flex flex-col items-center justify-center py-16 text-center gap-3">
              <p className="text-muted text-sm">{t('analytics.journeyConfig.empty')}</p>
              <button
                type="button"
                onClick={() => setModal({ mode: 'create' })}
                className="flex items-center gap-2 px-3 py-2 rounded-md bg-accent text-white text-xs hover:bg-accent/80"
              >
                <Plus className="w-3.5 h-3.5" />
                {t('analytics.journeyConfig.addJourney')}
              </button>
            </div>
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
                          <CheckCircle className="w-3 h-3" /> {t('analytics.journeyConfig.isActive')}
                        </span>
                      ) : (
                        <span className="flex items-center gap-1 text-muted">
                          <XCircle className="w-3 h-3" /> Inactive
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
                          <button
                            type="button"
                            onClick={() => setModal({ mode: 'edit', item: def })}
                            className="text-muted hover:text-accent p-1"
                            title={t('analytics.journeyConfig.editJourney')}
                          >
                            <Pencil className="w-3.5 h-3.5" />
                          </button>
                          <button
                            type="button"
                            onClick={() => setDeleteTarget(def)}
                            className="text-muted hover:text-critical p-1"
                            title={t('analytics.journeyConfig.deleteJourney')}
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
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
              : emptyForm()
          }
          onSave={handleSave}
          onCancel={() => setModal(null)}
        />
      )}

      {/* Delete confirm dialog */}
      {deleteTarget && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-panel border border-edge rounded-xl shadow-xl max-w-sm w-full p-6 space-y-4">
            <p className="text-sm text-heading">{t('analytics.journeyConfig.deleteConfirm')}</p>
            <p className="text-xs text-muted font-medium">{deleteTarget.name}</p>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setDeleteTarget(null)}
                className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
              >
                {t('common.cancel')}
              </button>
              <button
                type="button"
                onClick={() => deleteMutation.mutate(deleteTarget.id)}
                disabled={deleteMutation.isPending}
                className="px-3 py-2 rounded-md bg-critical text-white text-xs hover:bg-critical/80 disabled:opacity-50"
              >
                {t('common.delete')}
              </button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
