import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Layers, Plus, Pencil, Trash2, CheckCircle2, AlertTriangle } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { EmptyState } from '../../../components/EmptyState';
import { configurationApi } from '../../configuration/api/configurationApi';

const ENVIRONMENTS = ['Development', 'Pre-Production', 'Production'] as const;
type Environment = (typeof ENVIRONMENTS)[number];

interface OverrideFormState {
  open: boolean;
  paramKey: string;
  environment: Environment;
  value: string;
  reason: string;
  editing: boolean;
}

const EMPTY_FORM: OverrideFormState = {
  open: false,
  paramKey: '',
  environment: 'Development',
  value: '',
  reason: '',
  editing: false,
};

/**
 * ReleaseParameterEnvironmentOverridePage — overrides de parâmetros de release por ambiente.
 *
 * Permite platform admins e architects:
 * - Visualizar todos os overrides de parâmetros de release configurados por ambiente
 * - Adicionar, editar ou remover overrides para Development, Pre-Production e Production
 * - Fornecer um motivo de negócio para cada override (auditável)
 *
 * Consome o endpoint /configuration/entries?scope=Environment para carregar overrides existentes
 * e o endpoint /configuration/entries (PUT) para criar/editar overrides.
 *
 * Personas beneficiadas: Platform Admin, Architect, Tech Lead.
 */
export function ReleaseParameterEnvironmentOverridePage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<OverrideFormState>(EMPTY_FORM);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; key: string } | null>(null);

  const { data: overrides, isLoading, isError } = useQuery({
    queryKey: ['release-parameter-env-overrides'],
    queryFn: () => configurationApi.getReleaseParameterEnvironmentOverrides('change.release.'),
  });

  const notify = (type: 'success' | 'error', key: string) => {
    setNotification({ type, key });
    setTimeout(() => setNotification(null), 3000);
  };

  const saveMutation = useMutation({
    mutationFn: () =>
      configurationApi.setConfigurationValue(form.paramKey, {
        scope: 'Environment',
        scopeReferenceId: form.environment,
        value: form.value,
        changeReason: form.reason,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-parameter-env-overrides'] });
      setForm(EMPTY_FORM);
      notify('success', 'saveSuccess');
    },
    onError: () => notify('error', 'errorTitle'),
  });

  const removeMutation = useMutation({
    mutationFn: (entry: { key: string; env: string }) =>
      configurationApi.removeOverride(entry.key, 'Environment', entry.env, 'Override removed via UI'),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-parameter-env-overrides'] });
      notify('success', 'removeSuccess');
    },
    onError: () => notify('error', 'errorTitle'),
  });

  const openAdd = () => setForm({ ...EMPTY_FORM, open: true });
  const openEdit = (entry: { definitionKey: string; scopeReferenceId: string | null; value: string | null }) =>
    setForm({
      open: true,
      paramKey: entry.definitionKey,
      environment: (entry.scopeReferenceId ?? 'Development') as Environment,
      value: entry.value ?? '',
      reason: '',
      editing: true,
    });

  const envBadgeVariant = (env: string): 'success' | 'warning' | 'danger' | 'neutral' => {
    if (env === 'Production') return 'danger';
    if (env === 'Pre-Production') return 'warning';
    return 'neutral';
  };

  return (
    <PageContainer>
      <PageHeader
        icon={<Layers className="w-6 h-6 text-accent" />}
        title={t('releaseParameterOverride.title')}
        subtitle={t('releaseParameterOverride.subtitle')}
        actions={
          <Button variant="primary" size="sm" onClick={openAdd}>
            <Plus className="w-4 h-4 mr-1" />
            {t('releaseParameterOverride.addOverride')}
          </Button>
        }
      />

      {notification && (
        <div
          className={`mb-4 flex items-center gap-2 rounded-lg px-4 py-3 text-sm font-medium ${
            notification.type === 'success'
              ? 'bg-success/10 text-success border border-success/20'
              : 'bg-danger/10 text-danger border border-danger/20'
          }`}
        >
          {notification.type === 'success' ? (
            <CheckCircle2 className="w-4 h-4 flex-shrink-0" />
          ) : (
            <AlertTriangle className="w-4 h-4 flex-shrink-0" />
          )}
          {t(`releaseParameterOverride.${notification.key}`)}
        </div>
      )}

      {form.open && (
        <Card className="mb-4 border-accent/40">
          <CardHeader>
            <h3 className="text-sm font-semibold text-heading">
              {form.editing
                ? t('releaseParameterOverride.editOverride')
                : t('releaseParameterOverride.addOverride')}
            </h3>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('releaseParameterOverride.colParameter')}
                </label>
                <input
                  type="text"
                  value={form.paramKey}
                  onChange={e => setForm(prev => ({ ...prev, paramKey: e.target.value }))}
                  placeholder="change.release…"
                  disabled={form.editing}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm font-mono text-heading focus:outline-none focus:ring-2 focus:ring-accent disabled:opacity-60"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('releaseParameterOverride.environmentLabel')}
                </label>
                <select
                  value={form.environment}
                  onChange={e => setForm(prev => ({ ...prev, environment: e.target.value as Environment }))}
                  disabled={form.editing}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent disabled:opacity-60"
                >
                  {ENVIRONMENTS.map(env => (
                    <option key={env} value={env}>
                      {env}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('releaseParameterOverride.valueLabel')}
                </label>
                <input
                  type="text"
                  value={form.value}
                  onChange={e => setForm(prev => ({ ...prev, value: e.target.value }))}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-muted mb-1">
                  {t('releaseParameterOverride.reasonLabel')}
                </label>
                <input
                  type="text"
                  value={form.reason}
                  onChange={e => setForm(prev => ({ ...prev, reason: e.target.value }))}
                  placeholder={t('releaseParameterOverride.reasonPlaceholder')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
                />
              </div>
            </div>
            <div className="flex gap-2 mt-4">
              <Button
                variant="primary"
                size="sm"
                onClick={() => saveMutation.mutate()}
                disabled={!form.paramKey || !form.value || saveMutation.isPending}
              >
                {t('releaseParameterOverride.saveButton')}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setForm(EMPTY_FORM)}
              >
                {t('releaseParameterOverride.cancelButton')}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      <Card>
        <CardBody>
          {isLoading && <PageLoadingState message={t('releaseParameterOverride.loading')} />}
          {isError && (
            <EmptyState
              icon={<Layers className="w-10 h-10" />}
              title={t('releaseParameterOverride.errorTitle')}
              description={t('releaseParameterOverride.errorDescription')}
            />
          )}
          {!isLoading && !isError && (!overrides || overrides.length === 0) && (
            <EmptyState
              icon={<Layers className="w-10 h-10" />}
              title={t('releaseParameterOverride.emptyTitle')}
              description={t('releaseParameterOverride.emptyDescription')}
            />
          )}
          {!isLoading && !isError && overrides && overrides.length > 0 && (
            <div className="overflow-x-auto -mx-4 sm:mx-0">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-xs text-muted uppercase tracking-wide">
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterOverride.colParameter')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterOverride.colEnvironment')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterOverride.colValue')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterOverride.colUpdatedAt')}</th>
                    <th className="px-4 py-3 text-left font-medium">{t('releaseParameterOverride.colUpdatedBy')}</th>
                    <th className="px-4 py-3 text-right font-medium"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {overrides.map(entry => (
                    <tr key={entry.id} className="hover:bg-surface/50 transition-colors">
                      <td className="px-4 py-3 font-mono text-xs text-heading">{entry.definitionKey}</td>
                      <td className="px-4 py-3">
                        <Badge variant={envBadgeVariant(entry.scopeReferenceId ?? '')} size="sm">
                          {entry.scopeReferenceId ?? '—'}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-heading max-w-[180px] truncate">
                        {entry.value ?? '—'}
                      </td>
                      <td className="px-4 py-3 text-muted text-xs whitespace-nowrap">
                        {entry.updatedAt ? new Date(entry.updatedAt).toLocaleString() : '—'}
                      </td>
                      <td className="px-4 py-3 text-muted text-xs">{entry.updatedBy ?? '—'}</td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <button
                            onClick={() => openEdit(entry)}
                            className="rounded p-1.5 text-muted hover:text-heading hover:bg-surface transition-colors"
                            title={t('releaseParameterOverride.editOverride')}
                          >
                            <Pencil className="w-3.5 h-3.5" />
                          </button>
                          <button
                            onClick={() =>
                              removeMutation.mutate({
                                key: entry.definitionKey,
                                env: entry.scopeReferenceId ?? '',
                              })
                            }
                            className="rounded p-1.5 text-muted hover:text-danger hover:bg-danger/10 transition-colors"
                            title={t('releaseParameterOverride.removeOverride')}
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardBody>
      </Card>
    </PageContainer>
  );
}
