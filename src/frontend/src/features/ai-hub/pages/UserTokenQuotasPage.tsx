import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Trash2, Pencil, Coins, Search } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { Checkbox } from '../../../components/Checkbox';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { aiGovernanceApi } from '../api';

interface UserTokenQuota {
  quotaId: string;
  userId: string;
  policyName: string;
  providerId?: string;
  modelId?: string;
  maxInputTokensPerRequest: number;
  maxOutputTokensPerRequest: number;
  maxTotalTokensPerRequest: number;
  maxTokensPerDay: number;
  maxTokensPerMonth: number;
  isHardLimit: boolean;
  isEnabled: boolean;
}

interface QuotaFormState {
  userId: string;
  providerId: string;
  modelId: string;
  maxInputTokensPerRequest: number;
  maxOutputTokensPerRequest: number;
  maxTokensPerDay: number;
  maxTokensPerMonth: number;
  isHardLimit: boolean;
}

const EMPTY_FORM: QuotaFormState = {
  userId: '',
  providerId: '',
  modelId: '',
  maxInputTokensPerRequest: 8000,
  maxOutputTokensPerRequest: 4096,
  maxTokensPerDay: 100_000,
  maxTokensPerMonth: 3_000_000,
  isHardLimit: true,
};

/**
 * Página de Quotas de Tokens por Utilizador.
 * Configura limites de consumo de tokens de IA por utilizador, provider e modelo.
 */
export function UserTokenQuotasPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<QuotaFormState>(EMPTY_FORM);
  const [searchUser, setSearchUser] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-governance', 'user-token-quotas', searchUser],
    queryFn: () => aiGovernanceApi.listUserTokenQuotas(searchUser ? { userId: searchUser } : undefined),
    staleTime: 30_000,
  });

  const quotas: UserTokenQuota[] = (data?.items ?? []) as UserTokenQuota[];

  const createMutation = useMutation({
    mutationFn: (payload: QuotaFormState) =>
      aiGovernanceApi.createUserTokenQuota({
        userId: payload.userId,
        providerId: payload.providerId || undefined,
        modelId: payload.modelId || undefined,
        maxInputTokensPerRequest: payload.maxInputTokensPerRequest,
        maxOutputTokensPerRequest: payload.maxOutputTokensPerRequest,
        maxTokensPerDay: payload.maxTokensPerDay,
        maxTokensPerMonth: payload.maxTokensPerMonth,
        isHardLimit: payload.isHardLimit,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'user-token-quotas'] });
      setShowForm(false);
      setForm(EMPTY_FORM);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: QuotaFormState }) =>
      aiGovernanceApi.updateUserTokenQuota(id, {
        maxInputTokensPerRequest: payload.maxInputTokensPerRequest,
        maxOutputTokensPerRequest: payload.maxOutputTokensPerRequest,
        maxTokensPerDay: payload.maxTokensPerDay,
        maxTokensPerMonth: payload.maxTokensPerMonth,
        isHardLimit: payload.isHardLimit,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'user-token-quotas'] });
      setShowForm(false);
      setEditingId(null);
      setForm(EMPTY_FORM);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => aiGovernanceApi.deleteUserTokenQuota(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance', 'user-token-quotas'] });
    },
  });

  function handleEdit(quota: UserTokenQuota) {
    setEditingId(quota.quotaId);
    setForm({
      userId: quota.userId,
      providerId: quota.providerId ?? '',
      modelId: quota.modelId ?? '',
      maxInputTokensPerRequest: quota.maxInputTokensPerRequest,
      maxOutputTokensPerRequest: quota.maxOutputTokensPerRequest,
      maxTokensPerDay: quota.maxTokensPerDay,
      maxTokensPerMonth: quota.maxTokensPerMonth,
      isHardLimit: quota.isHardLimit,
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
        title={t('ai.userTokenQuotas.title', 'Quotas de Tokens por Utilizador')}
        subtitle={t('ai.userTokenQuotas.subtitle', 'Configure limites de consumo de tokens de IA por utilizador')}
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

      <div className="mb-4">
        <TextField
          size="sm"
          leadingIcon={<Search className="w-4 h-4" />}
          placeholder={t('ai.userTokenQuotas.searchPlaceholder', 'Filtrar por utilizador...')}
          value={searchUser}
          onChange={e => setSearchUser(e.target.value)}
        />
      </div>

      {showForm && (
        <Card className="mb-6">
          <CardBody>
            <h3 className="text-base font-semibold mb-4">
              {editingId
                ? t('ai.userTokenQuotas.editTitle', 'Editar Quota')
                : t('ai.userTokenQuotas.createTitle', 'Nova Quota de Tokens')}
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {!editingId && (
                <div className="md:col-span-2">
                  <TextField
                    size="sm"
                    label={t('ai.userTokenQuotas.userId', 'ID do Utilizador')}
                    placeholder="user@empresa.com ou UUID"
                    value={form.userId}
                    onChange={e => setForm(f => ({ ...f, userId: e.target.value }))}
                  />
                </div>
              )}
              <TextField
                size="sm"
                label={t('ai.userTokenQuotas.provider', 'Provider (vazio = todos)')}
                placeholder="anthropic, ollama, openai..."
                value={form.providerId}
                onChange={e => setForm(f => ({ ...f, providerId: e.target.value }))}
              />
              <TextField
                size="sm"
                label={t('ai.userTokenQuotas.model', 'Modelo (vazio = todos)')}
                placeholder="claude-sonnet-4-6..."
                value={form.modelId}
                onChange={e => setForm(f => ({ ...f, modelId: e.target.value }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('ai.userTokenQuotas.maxInputPerReq', 'Máx. Input Tokens/Pedido')}
                min={1}
                value={form.maxInputTokensPerRequest}
                onChange={e => setForm(f => ({ ...f, maxInputTokensPerRequest: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('ai.userTokenQuotas.maxOutputPerReq', 'Máx. Output Tokens/Pedido')}
                min={1}
                value={form.maxOutputTokensPerRequest}
                onChange={e => setForm(f => ({ ...f, maxOutputTokensPerRequest: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('ai.userTokenQuotas.maxPerDay', 'Máx. Tokens/Dia')}
                min={1}
                value={form.maxTokensPerDay}
                onChange={e => setForm(f => ({ ...f, maxTokensPerDay: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('ai.userTokenQuotas.maxPerMonth', 'Máx. Tokens/Mês')}
                min={1}
                value={form.maxTokensPerMonth}
                onChange={e => setForm(f => ({ ...f, maxTokensPerMonth: Number(e.target.value) }))}
              />
              <div className="flex items-center pt-6">
                <Checkbox
                  checked={form.isHardLimit}
                  onChange={e => setForm(f => ({ ...f, isHardLimit: e.target.checked }))}
                  label={t('ai.userTokenQuotas.hardLimit', 'Limite Rígido (bloqueia)')}
                  description={t('ai.userTokenQuotas.hardLimitHint', '— desmarcado = aviso')}
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

      {quotas.length === 0 ? (
        <EmptyState
          icon={<Coins className="w-12 h-12 text-muted" />}
          title={t('ai.userTokenQuotas.empty', 'Nenhuma quota configurada')}
          description={t('ai.userTokenQuotas.emptyDesc', 'Crie uma quota para controlar o consumo de tokens de IA por utilizador.')}
        />
      ) : (
        <div className="space-y-3">
          {quotas.map(quota => (
            <Card key={quota.quotaId}>
              <CardBody className="flex items-start justify-between gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-medium text-sm">{quota.userId}</span>
                    <Badge variant={quota.isEnabled ? 'success' : 'default'}>
                      {quota.isEnabled ? t('common.active', 'Ativo') : t('common.inactive', 'Inativo')}
                    </Badge>
                    {quota.isHardLimit && <Badge variant="critical">{t('ai.userTokenQuotas.hard', 'Rígido')}</Badge>}
                    {quota.providerId && (
                      <Badge variant="info">{quota.providerId}</Badge>
                    )}
                    {quota.modelId && (
                      <Badge variant="default">{quota.modelId}</Badge>
                    )}
                  </div>
                  <div className="text-xs text-muted space-y-0.5 mt-1">
                    <p>
                      <span className="font-medium">{t('ai.userTokenQuotas.perRequest', 'Por pedido')}: </span>
                      {quota.maxInputTokensPerRequest.toLocaleString()} input +{' '}
                      {quota.maxOutputTokensPerRequest.toLocaleString()} output
                    </p>
                    <p>
                      <span className="font-medium">{t('ai.userTokenQuotas.daily', 'Diário')}: </span>
                      {quota.maxTokensPerDay.toLocaleString()} tokens
                      <span className="mx-2">·</span>
                      <span className="font-medium">{t('ai.userTokenQuotas.monthly', 'Mensal')}: </span>
                      {quota.maxTokensPerMonth.toLocaleString()} tokens
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <IconButton
                    variant="ghost"
                    size="sm"
                    onClick={() => handleEdit(quota)}
                    label={t('common.edit', 'Editar')}
                    title={t('common.edit', 'Editar')}
                    icon={<Pencil className="w-4 h-4" />}
                  />
                  <IconButton
                    variant="ghost"
                    size="sm"
                    className="hover:text-critical"
                    onClick={() => deleteMutation.mutate(quota.quotaId)}
                    label={t('common.delete', 'Eliminar')}
                    title={t('common.delete', 'Eliminar')}
                    disabled={deleteMutation.isPending}
                    icon={<Trash2 className="w-4 h-4" />}
                  />
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
