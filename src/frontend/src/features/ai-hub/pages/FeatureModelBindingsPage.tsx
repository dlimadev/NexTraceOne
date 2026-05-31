import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, Pencil, GitBranch } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { aiGovernanceApi } from '../api';

interface FeatureBinding {
  id: string;
  featureKey: string;
  description: string;
  requiredModelId: string;
  requiredModelName: string;
  requiredProviderId: string;
  fallbackModelId?: string;
  fallbackModelName?: string;
  fallbackProviderId?: string;
  isActive: boolean;
}

interface BindingFormState {
  featureKey: string;
  description: string;
  requiredModelId: string;
  requiredModelName: string;
  requiredProviderId: string;
  fallbackModelId: string;
  fallbackModelName: string;
  fallbackProviderId: string;
}

const EMPTY_FORM: BindingFormState = {
  featureKey: '',
  description: '',
  requiredModelId: '',
  requiredModelName: '',
  requiredProviderId: '',
  fallbackModelId: '',
  fallbackModelName: '',
  fallbackProviderId: '',
};

/**
 * Página de Vinculações Feature → Modelo de IA.
 * Permite configurar qual modelo de IA é usado para cada funcionalidade da plataforma.
 */
export function FeatureModelBindingsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<BindingFormState>(EMPTY_FORM);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-governance', 'feature-model-bindings'],
    queryFn: () => aiGovernanceApi.listFeatureModelBindings(),
    staleTime: 30_000,
  });

  const bindings: FeatureBinding[] = (data?.items ?? []) as FeatureBinding[];

  const createMutation = useMutation({
    mutationFn: (payload: BindingFormState) =>
      aiGovernanceApi.createFeatureModelBinding({
        featureKey: payload.featureKey,
        description: payload.description,
        requiredModelId: payload.requiredModelId,
        requiredModelName: payload.requiredModelName,
        requiredProviderId: payload.requiredProviderId,
        fallbackModelId: payload.fallbackModelId || undefined,
        fallbackModelName: payload.fallbackModelName || undefined,
        fallbackProviderId: payload.fallbackProviderId || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'feature-model-bindings'] });
      setShowForm(false);
      setForm(EMPTY_FORM);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: BindingFormState }) =>
      aiGovernanceApi.updateFeatureModelBinding(id, {
        description: payload.description,
        requiredModelId: payload.requiredModelId,
        requiredModelName: payload.requiredModelName,
        requiredProviderId: payload.requiredProviderId,
        fallbackModelId: payload.fallbackModelId || undefined,
        fallbackModelName: payload.fallbackModelName || undefined,
        fallbackProviderId: payload.fallbackProviderId || undefined,
        clearFallback: !payload.fallbackModelId,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'feature-model-bindings'] });
      setShowForm(false);
      setEditingId(null);
      setForm(EMPTY_FORM);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => aiGovernanceApi.deleteFeatureModelBinding(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'feature-model-bindings'] });
    },
  });

  function handleEdit(binding: FeatureBinding) {
    setEditingId(binding.id);
    setForm({
      featureKey: binding.featureKey,
      description: binding.description,
      requiredModelId: binding.requiredModelId,
      requiredModelName: binding.requiredModelName,
      requiredProviderId: binding.requiredProviderId,
      fallbackModelId: binding.fallbackModelId ?? '',
      fallbackModelName: binding.fallbackModelName ?? '',
      fallbackProviderId: binding.fallbackProviderId ?? '',
    });
    setShowForm(true);
  }

  function handleSubmit() {
    if (editingId) {
      updateMutation.mutate({ id: editingId, payload: form });
    } else {
      createMutation.mutate(form);
    }
  }

  if (isLoading) return <CardListSkeleton />;
  if (isError) return <PageErrorState onRetry={refetch} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('ai.featureBindings.title', 'Vinculações Feature → Modelo')}
        subtitle={t('ai.featureBindings.subtitle', 'Defina qual modelo de IA é utilizado para cada funcionalidade da plataforma')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus className="w-4 h-4" />}
            onClick={() => { setShowForm(true); setEditingId(null); setForm(EMPTY_FORM); }}
          >
            {t('common.create', 'Criar')}
          </Button>
        }
      />

      {showForm && (
        <Card className="mb-6">
          <CardBody>
            <h3 className="text-base font-semibold mb-4">
              {editingId
                ? t('ai.featureBindings.editTitle', 'Editar Vinculação')
                : t('ai.featureBindings.createTitle', 'Nova Vinculação')}
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.featureKey', 'Chave da Funcionalidade')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="catalog.contract-draft"
                  value={form.featureKey}
                  onChange={e => setForm(f => ({ ...f, featureKey: e.target.value }))}
                  disabled={!!editingId}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('common.description', 'Descrição')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  value={form.description}
                  onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.requiredModelId', 'ID Modelo Obrigatório')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm font-mono"
                  placeholder="UUID do modelo"
                  value={form.requiredModelId}
                  onChange={e => setForm(f => ({ ...f, requiredModelId: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.requiredModelName', 'Nome Modelo Obrigatório')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="claude-sonnet-4-6"
                  value={form.requiredModelName}
                  onChange={e => setForm(f => ({ ...f, requiredModelName: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.requiredProvider', 'Provider Obrigatório')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="anthropic"
                  value={form.requiredProviderId}
                  onChange={e => setForm(f => ({ ...f, requiredProviderId: e.target.value }))}
                />
              </div>
            </div>

            <h4 className="text-sm font-medium mt-4 mb-2 text-muted">
              {t('ai.featureBindings.fallback', 'Fallback (opcional)')}
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.fallbackModelId', 'ID Fallback')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm font-mono"
                  placeholder="UUID (opcional)"
                  value={form.fallbackModelId}
                  onChange={e => setForm(f => ({ ...f, fallbackModelId: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.fallbackModelName', 'Nome Fallback')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="llama3.2:3b"
                  value={form.fallbackModelName}
                  onChange={e => setForm(f => ({ ...f, fallbackModelName: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.featureBindings.fallbackProvider', 'Provider Fallback')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm"
                  placeholder="ollama"
                  value={form.fallbackProviderId}
                  onChange={e => setForm(f => ({ ...f, fallbackProviderId: e.target.value }))}
                />
              </div>
            </div>

            <div className="flex gap-2 mt-4">
              <Button
                variant="primary"
                size="sm"
                onClick={handleSubmit}
                disabled={createMutation.isPending || updateMutation.isPending}
              >
                {editingId ? t('common.save', 'Guardar') : t('common.create', 'Criar')}
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => { setShowForm(false); setEditingId(null); setForm(EMPTY_FORM); }}
              >
                {t('common.cancel', 'Cancelar')}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      {bindings.length === 0 ? (
        <EmptyState
          icon={<GitBranch className="w-12 h-12 text-muted" />}
          title={t('ai.featureBindings.empty', 'Nenhuma vinculação configurada')}
          description={t('ai.featureBindings.emptyDesc', 'Crie uma vinculação para definir qual modelo é usado para cada funcionalidade.')}
        />
      ) : (
        <div className="space-y-3">
          {bindings.map(binding => (
            <Card key={binding.id}>
              <CardBody className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-mono text-sm font-semibold">{binding.featureKey}</span>
                    <Badge variant={binding.isActive ? 'success' : 'default'}>
                      {binding.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                  </div>
                  <p className="text-sm text-muted truncate">{binding.description}</p>
                  <div className="flex flex-wrap gap-3 mt-2 text-xs">
                    <span>
                      <span className="text-muted">Modelo: </span>
                      <span className="font-medium">{binding.requiredModelName}</span>
                      <span className="text-muted ml-1">({binding.requiredProviderId})</span>
                    </span>
                    {binding.fallbackModelName && (
                      <span>
                        <span className="text-muted">Fallback: </span>
                        <span className="font-medium">{binding.fallbackModelName}</span>
                      </span>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <button
                    onClick={() => handleEdit(binding)}
                    className="p-1.5 rounded hover:bg-surface-hover text-muted hover:text-foreground transition-colors"
                    title={t('common.edit', 'Editar')}
                  >
                    <Pencil className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => deleteMutation.mutate(binding.id)}
                    className="p-1.5 rounded hover:bg-surface-hover text-muted hover:text-critical transition-colors"
                    title={t('common.delete', 'Eliminar')}
                    disabled={deleteMutation.isPending}
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
