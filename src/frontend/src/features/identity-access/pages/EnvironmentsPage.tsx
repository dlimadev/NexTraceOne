import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Plus, RefreshCw, Globe, Star, Shield } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { identityApi } from '../api';
import type { EnvironmentItem, CreateEnvironmentRequest } from '../api/identity';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

const PROFILE_OPTIONS = [
  'Development',
  'Validation',
  'Staging',
  'Production',
  'Sandbox',
  'DisasterRecovery',
  'Training',
  'UserAcceptanceTesting',
  'PerformanceTesting',
];

const CRITICALITY_OPTIONS = ['Low', 'Medium', 'High', 'Critical'];

interface EnvironmentForm {
  name: string;
  slug: string;
  sortOrder: number;
  profile: string;
  criticality: string;
  code: string;
  description: string;
  region: string;
  isPrimaryProduction: boolean;
}

const DEFAULT_FORM: EnvironmentForm = {
  name: '',
  slug: '',
  sortOrder: 0,
  profile: 'Development',
  criticality: 'Low',
  code: '',
  description: '',
  region: '',
  isPrimaryProduction: false,
};

/**
 * Página de gestão de ambientes operacionais do tenant.
 *
 * Permite criar, editar, ativar/desativar ambientes e designar o ambiente
 * produtivo principal. O sistema não impõe ambientes fixos (DEV/PRE/PROD) —
 * cada tenant define os seus próprios ambientes e perfis operacionais.
 *
 * O ambiente produtivo principal é a referência usada pela IA para comparação
 * com ambientes não produtivos e análise de risco de release.
 */
export function EnvironmentsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<EnvironmentForm>(DEFAULT_FORM);

  const {
    data: environments,
    isLoading,
    isError,
    refetch,
  } = useQuery<EnvironmentItem[]>({
    queryKey: ['environments'],
    queryFn: () => identityApi.listEnvironments(),
    staleTime: 30_000,
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateEnvironmentRequest) => identityApi.createEnvironment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['environments'] });
      setShowForm(false);
      setForm(DEFAULT_FORM);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateEnvironmentRequest }) =>
      identityApi.updateEnvironment(id, { ...data, environmentId: id }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['environments'] });
      setShowForm(false);
      setEditingId(null);
      setForm(DEFAULT_FORM);
    },
  });

  const setPrimaryMutation = useMutation({
    mutationFn: (envId: string) => identityApi.setPrimaryProductionEnvironment(envId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['environments'] });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const payload: CreateEnvironmentRequest = {
      name: form.name,
      slug: form.slug.toLowerCase(),
      sortOrder: form.sortOrder,
      profile: form.profile,
      criticality: form.criticality,
      code: form.code || undefined,
      description: form.description || undefined,
      region: form.region || undefined,
      isPrimaryProduction: form.isPrimaryProduction,
    };

    if (editingId) {
      updateMutation.mutate({ id: editingId, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const handleEdit = (env: EnvironmentItem) => {
    setForm({
      name: env.name,
      slug: env.slug,
      sortOrder: env.sortOrder,
      profile: env.profile,
      criticality: env.criticality ?? 'Low',
      code: env.code ?? '',
      description: env.description ?? '',
      region: env.region ?? '',
      isPrimaryProduction: env.isPrimaryProduction,
    });
    setEditingId(env.id);
    setShowForm(true);
  };

  const handleSetPrimary = (env: EnvironmentItem) => {
    if (window.confirm(t('environments.confirmSetPrimary', { name: env.name }))) {
      setPrimaryMutation.mutate(env.id);
    }
  };

  const getProfileBadgeVariant = (profile: string): 'default' | 'warning' | 'danger' | 'success' => {
    switch (profile.toLowerCase()) {
      case 'production': return 'danger';
      case 'staging': case 'useracceptancetesting': return 'warning';
      case 'development': case 'sandbox': return 'default';
      default: return 'default';
    }
  };

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={refetch} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('environments.title')}
        subtitle={t('environments.subtitle')}
        actions={
          <div style={{ display: 'flex', gap: '8px' }}>
            <Button type="button" onClick={() => void refetch()}>
              <RefreshCw size={16} />
            </Button>
            <Button
              type="button"
              onClick={() => {
                setForm(DEFAULT_FORM);
                setEditingId(null);
                setShowForm((v) => !v);
              }}
            >
              <Plus size={16} /> {t('environments.createEnvironment')}
            </Button>
          </div>
        }
      />

      <PageSection>
        {showForm && (
          <Card>
            <CardHeader>
              <h3>{editingId ? t('environments.editEnvironment') : t('environments.createNewEnvironment')}</h3>
            </CardHeader>
            <CardBody>
              <form onSubmit={handleSubmit}>
                <div style={{ display: 'grid', gap: '12px' }}>
                  <div>
                    <label htmlFor="env-name">{t('environments.name')} *</label>
                    <input
                      id="env-name"
                      type="text"
                      required
                      maxLength={100}
                      value={form.name}
                      onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                      style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                    />
                  </div>

                  {!editingId && (
                    <div>
                      <label htmlFor="env-slug">{t('environments.slug')} *</label>
                      <input
                        id="env-slug"
                        type="text"
                        required
                        maxLength={50}
                        pattern="^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$"
                        value={form.slug}
                        onChange={(e) => setForm((f) => ({ ...f, slug: e.target.value.toLowerCase() }))}
                        style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                      />
                      <small style={{ color: 'var(--t-muted)' }}>{t('environments.slugHelp')}</small>
                    </div>
                  )}

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                    <div>
                      <label htmlFor="env-profile">{t('environments.profile')} *</label>
                      <select
                        id="env-profile"
                        required
                        value={form.profile}
                        onChange={(e) => setForm((f) => ({ ...f, profile: e.target.value }))}
                        style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                      >
                        {PROFILE_OPTIONS.map((p) => (
                          <option key={p} value={p}>
                            {t(`environments.profiles.${p}`, { defaultValue: p })}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div>
                      <label htmlFor="env-criticality">{t('environments.criticality')} *</label>
                      <select
                        id="env-criticality"
                        required
                        value={form.criticality}
                        onChange={(e) => setForm((f) => ({ ...f, criticality: e.target.value }))}
                        style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                      >
                        {CRITICALITY_OPTIONS.map((c) => (
                          <option key={c} value={c}>
                            {t(`environments.criticalities.${c}`, { defaultValue: c })}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
                    <div>
                      <label htmlFor="env-code">{t('environments.code')}</label>
                      <input
                        id="env-code"
                        type="text"
                        maxLength={50}
                        value={form.code}
                        onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))}
                        style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                      />
                      <small style={{ color: 'var(--t-muted)' }}>{t('environments.codeHelp')}</small>
                    </div>

                    <div>
                      <label htmlFor="env-region">{t('environments.region')}</label>
                      <input
                        id="env-region"
                        type="text"
                        maxLength={100}
                        value={form.region}
                        onChange={(e) => setForm((f) => ({ ...f, region: e.target.value }))}
                        style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                      />
                    </div>

                    <div>
                      <label htmlFor="env-sortorder">{t('environments.sortOrder')} *</label>
                      <input
                        id="env-sortorder"
                        type="number"
                        min={0}
                        required
                        value={form.sortOrder}
                        onChange={(e) => setForm((f) => ({ ...f, sortOrder: parseInt(e.target.value, 10) || 0 }))}
                        style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)' }}
                      />
                    </div>
                  </div>

                  <div>
                    <label htmlFor="env-description">{t('environments.description')}</label>
                    <textarea
                      id="env-description"
                      rows={2}
                      value={form.description}
                      onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                      style={{ display: 'block', width: '100%', marginTop: '4px', padding: '6px 8px', borderRadius: '4px', border: '1px solid var(--t-edge-strong)', resize: 'vertical' }}
                    />
                  </div>

                  <div>
                    <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                      <input
                        id="env-is-primary"
                        type="checkbox"
                        checked={form.isPrimaryProduction}
                        onChange={(e) => setForm((f) => ({ ...f, isPrimaryProduction: e.target.checked }))}
                      />
                      <span>{t('environments.isPrimaryProduction')}</span>
                    </label>
                    <small style={{ color: 'var(--t-muted)', marginLeft: '24px' }}>
                      {t('environments.isPrimaryProductionHelp')}
                    </small>
                  </div>

                  <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                    <Button
                      type="button"
                      onClick={() => {
                        setShowForm(false);
                        setEditingId(null);
                        setForm(DEFAULT_FORM);
                      }}
                    >
                      {t('common.cancel')}
                    </Button>
                    <Button
                      type="submit"
                      disabled={createMutation.isPending || updateMutation.isPending}
                    >
                      {editingId ? t('common.save') : t('environments.createEnvironment')}
                    </Button>
                  </div>
                </div>
              </form>
            </CardBody>
          </Card>
        )}

        {(!environments || environments.length === 0) ? (
          <EmptyState
            title={t('environments.noEnvironmentsFound')}
            action={
              <Button type="button" onClick={() => { setShowForm(true); setEditingId(null); setForm(DEFAULT_FORM); }}>
                <Plus size={16} /> {t('environments.createEnvironment')}
              </Button>
            }
          />
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {environments.map((env) => (
              <Card key={env.id}>
                <CardBody>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '8px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
                      <Globe size={16} style={{ color: 'var(--t-faded)', flexShrink: 0 }} />
                      <div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <strong>{env.name}</strong>
                          {env.code && (
                            <code style={{ fontSize: '11px', background: 'var(--t-elevated)', padding: '1px 4px', borderRadius: '3px', color: 'var(--t-muted)' }}>
                              {env.code}
                            </code>
                          )}
                        </div>
                        <div style={{ fontSize: '12px', color: 'var(--t-muted)' }}>{env.slug}{env.region ? ` · ${env.region}` : ''}</div>
                      </div>
                      <Badge variant={getProfileBadgeVariant(env.profile)}>
                        {t(`environments.profiles.${env.profile}`, { defaultValue: env.profile })}
                      </Badge>
                      <Badge variant="default">
                        {t(`environments.criticalities.${env.criticality ?? 'Low'}`, { defaultValue: env.criticality ?? 'Low' })}
                      </Badge>
                      {env.isPrimaryProduction && (
                        <Badge variant="warning">
                          <Star size={10} style={{ marginRight: '3px' }} />
                          {t('environments.primaryProductionBadge')}
                        </Badge>
                      )}
                      {!env.isPrimaryProduction && env.isProductionLike && (
                        <Badge variant="default">
                          <Shield size={10} style={{ marginRight: '3px' }} />
                          {t('environments.productionLikeBadge')}
                        </Badge>
                      )}
                      <Badge variant={env.isActive ? 'success' : 'default'}>
                        {env.isActive ? t('environments.active') : t('environments.inactive')}
                      </Badge>
                    </div>

                    <div style={{ display: 'flex', gap: '8px', flexShrink: 0 }}>
                      {env.isActive && !env.isPrimaryProduction && (
                        <Button
                          type="button"
                          onClick={() => handleSetPrimary(env)}
                          disabled={setPrimaryMutation.isPending}
                          title={t('environments.setAsPrimary')}
                        >
                          <Star size={14} /> {t('environments.setAsPrimary')}
                        </Button>
                      )}
                      <Button type="button" onClick={() => handleEdit(env)}>
                        {t('common.edit')}
                      </Button>
                    </div>
                  </div>
                  {env.description && (
                    <div style={{ marginTop: '6px', fontSize: '12px', color: 'var(--t-muted)', paddingLeft: '26px' }}>
                      {env.description}
                    </div>
                  )}
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
