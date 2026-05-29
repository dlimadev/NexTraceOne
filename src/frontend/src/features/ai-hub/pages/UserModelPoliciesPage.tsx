import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, Edit2, Shield } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { aiGovernanceApi } from '../api';

interface UserModelPolicy {
  policyId: string;
  userId: string;
  policyName: string;
  description: string;
  allowedModelIds: string;
  blockedModelIds: string;
  allowExternalAI: boolean;
  internalOnly: boolean;
  maxTokensPerRequest: number;
  isActive: boolean;
}

interface PolicyFormState {
  userId: string;
  userDisplayName: string;
  allowedModelIds: string;
  blockedModelIds: string;
  allowExternalAI: boolean;
  internalOnly: boolean;
  maxTokensPerRequest: number;
}

const EMPTY_FORM: PolicyFormState = {
  userId: '',
  userDisplayName: '',
  allowedModelIds: '',
  blockedModelIds: '',
  allowExternalAI: true,
  internalOnly: false,
  maxTokensPerRequest: 8000,
};

/**
 * Página de Políticas de Acesso a Modelos por Utilizador.
 * Define quais modelos cada utilizador pode usar (allowlist / denylist).
 */
export function UserModelPoliciesPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<PolicyFormState>(EMPTY_FORM);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-governance', 'user-model-policies'],
    queryFn: () => aiGovernanceApi.listUserModelPolicies(),
    staleTime: 30_000,
  });

  const policies: UserModelPolicy[] = (data?.items ?? []) as UserModelPolicy[];

  const createMutation = useMutation({
    mutationFn: (payload: PolicyFormState) =>
      aiGovernanceApi.createUserModelPolicy(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'user-model-policies'] });
      setShowForm(false);
      setForm(EMPTY_FORM);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: PolicyFormState }) =>
      aiGovernanceApi.updateUserModelPolicy(id, {
        allowedModelIds: payload.allowedModelIds,
        blockedModelIds: payload.blockedModelIds,
        allowExternalAI: payload.allowExternalAI,
        internalOnly: payload.internalOnly,
        maxTokensPerRequest: payload.maxTokensPerRequest,
        description: `Política de acesso a modelos para ${payload.userDisplayName || payload.userId}`,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'user-model-policies'] });
      setShowForm(false);
      setEditingId(null);
      setForm(EMPTY_FORM);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => aiGovernanceApi.deleteUserModelPolicy(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'user-model-policies'] });
    },
  });

  function handleEdit(policy: UserModelPolicy) {
    setEditingId(policy.policyId);
    setForm({
      userId: policy.userId,
      userDisplayName: policy.userId,
      allowedModelIds: policy.allowedModelIds,
      blockedModelIds: policy.blockedModelIds,
      allowExternalAI: policy.allowExternalAI,
      internalOnly: policy.internalOnly,
      maxTokensPerRequest: policy.maxTokensPerRequest,
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
        title={t('ai.userModelPolicies.title', 'Políticas de Modelos por Utilizador')}
        subtitle={t('ai.userModelPolicies.subtitle', 'Defina quais modelos de IA cada utilizador pode usar')}
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
                ? t('ai.userModelPolicies.editTitle', 'Editar Política')
                : t('ai.userModelPolicies.createTitle', 'Nova Política de Utilizador')}
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {!editingId && (
                <>
                  <div>
                    <label className="block text-sm font-medium mb-1">
                      {t('ai.userModelPolicies.userId', 'ID do Utilizador')}
                    </label>
                    <input
                      type="text"
                      className="w-full border rounded px-3 py-2 text-sm"
                      placeholder="user@empresa.com ou UUID"
                      value={form.userId}
                      onChange={e => setForm(f => ({ ...f, userId: e.target.value }))}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">
                      {t('ai.userModelPolicies.userName', 'Nome do Utilizador')}
                    </label>
                    <input
                      type="text"
                      className="w-full border rounded px-3 py-2 text-sm"
                      placeholder="Maria Silva"
                      value={form.userDisplayName}
                      onChange={e => setForm(f => ({ ...f, userDisplayName: e.target.value }))}
                    />
                  </div>
                </>
              )}
              <div className="md:col-span-2">
                <label className="block text-sm font-medium mb-1">
                  {t('ai.userModelPolicies.allowedModels', 'Modelos Permitidos (IDs separados por vírgula)')}
                  <span className="text-muted ml-1 text-xs">— vazio = todos permitidos</span>
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm font-mono"
                  placeholder="uuid1,uuid2,uuid3"
                  value={form.allowedModelIds}
                  onChange={e => setForm(f => ({ ...f, allowedModelIds: e.target.value }))}
                />
              </div>
              <div className="md:col-span-2">
                <label className="block text-sm font-medium mb-1">
                  {t('ai.userModelPolicies.blockedModels', 'Modelos Bloqueados (IDs separados por vírgula)')}
                </label>
                <input
                  type="text"
                  className="w-full border rounded px-3 py-2 text-sm font-mono"
                  placeholder="uuid1,uuid2"
                  value={form.blockedModelIds}
                  onChange={e => setForm(f => ({ ...f, blockedModelIds: e.target.value }))}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">
                  {t('ai.userModelPolicies.maxTokens', 'Máx. Tokens/Pedido')}
                </label>
                <input
                  type="number"
                  className="w-full border rounded px-3 py-2 text-sm"
                  min={1}
                  value={form.maxTokensPerRequest}
                  onChange={e => setForm(f => ({ ...f, maxTokensPerRequest: Number(e.target.value) }))}
                />
              </div>
              <div className="flex items-center gap-6 pt-6">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.allowExternalAI}
                    onChange={e => setForm(f => ({ ...f, allowExternalAI: e.target.checked }))}
                  />
                  <span className="text-sm">{t('ai.userModelPolicies.allowExternal', 'Permitir IA Externa')}</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.internalOnly}
                    onChange={e => setForm(f => ({ ...f, internalOnly: e.target.checked }))}
                  />
                  <span className="text-sm">{t('ai.userModelPolicies.internalOnly', 'Apenas Interna')}</span>
                </label>
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

      {policies.length === 0 ? (
        <EmptyState
          icon={<Shield className="w-12 h-12 text-muted" />}
          title={t('ai.userModelPolicies.empty', 'Nenhuma política configurada')}
          description={t('ai.userModelPolicies.emptyDesc', 'Crie uma política para controlar quais modelos cada utilizador pode usar.')}
        />
      ) : (
        <div className="space-y-3">
          {policies.map(policy => (
            <Card key={policy.policyId}>
              <CardBody className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-medium text-sm">{policy.userId}</span>
                    <Badge variant={policy.isActive ? 'success' : 'default'}>
                      {policy.isActive ? 'Ativo' : 'Inativo'}
                    </Badge>
                    {policy.internalOnly && (
                      <Badge variant="warning">Apenas Interna</Badge>
                    )}
                    {policy.allowExternalAI && !policy.internalOnly && (
                      <Badge variant="info">IA Externa Permitida</Badge>
                    )}
                  </div>
                  <div className="text-xs text-muted space-y-0.5 mt-1">
                    {policy.allowedModelIds && (
                      <p><span className="font-medium">Permitidos: </span>{policy.allowedModelIds}</p>
                    )}
                    {policy.blockedModelIds && (
                      <p><span className="font-medium">Bloqueados: </span>{policy.blockedModelIds}</p>
                    )}
                    <p><span className="font-medium">Máx. tokens/pedido: </span>{policy.maxTokensPerRequest.toLocaleString()}</p>
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <button
                    onClick={() => handleEdit(policy)}
                    className="p-1.5 rounded hover:bg-surface-hover text-muted hover:text-foreground transition-colors"
                    title={t('common.edit', 'Editar')}
                  >
                    <Edit2 className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => deleteMutation.mutate(policy.policyId)}
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
