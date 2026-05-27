import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Lock, RefreshCw, Edit2, Save, X } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import {
  platformAdminApi,
  type EnvironmentAccessPolicy,
  type EnvironmentPolicyUpdate,
  type EnvPolicyRole,
} from '../api/platformAdmin';

const ALL_ROLES: EnvPolicyRole[] = ['Engineer', 'TechLead', 'Architect', 'PlatformAdmin', 'Auditor'];

export function EnvironmentPoliciesPage() {
  const { t } = useTranslation('environmentPolicies');
  const queryClient = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState<EnvironmentPolicyUpdate>({
    allowedRoles: [],
    requireJitFor: [],
    description: '',
  });

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['environment-policies'],
    queryFn: platformAdminApi.getEnvironmentPolicies,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, update }: { id: string; update: EnvironmentPolicyUpdate }) =>
      platformAdminApi.updateEnvironmentPolicy(id, update),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['environment-policies'] });
      setEditingId(null);
    },
  });

  function startEdit(policy: EnvironmentAccessPolicy) {
    setEditingId(policy.id);
    setEditForm({
      allowedRoles: [...policy.allowedRoles],
      requireJitFor: [...policy.requireJitFor],
      jitApprovalRequiredFrom: policy.jitApprovalRequiredFrom,
      description: policy.description,
    });
  }

  function toggleRole(list: EnvPolicyRole[], role: EnvPolicyRole): EnvPolicyRole[] {
    return list.includes(role) ? list.filter((r) => r !== role) : [...list, role];
  }

  if (isLoading) return <div className="p-6 text-sm text-muted">{t('loading')}</div>;
  if (isError) return <div className="p-6 text-sm text-critical">{t('error')}</div>;

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Lock size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        <div className="bg-accent/10 border border-accent/20 rounded-lg p-4 text-sm text-accent">
          {t('infoNote')}
        </div>

        <div className="space-y-4">
          {data?.policies.map((policy) => (
            <div key={policy.id} className="bg-card border border-edge rounded-lg overflow-hidden">
              <div className="px-4 py-3 border-b border-edge bg-elevated flex items-center justify-between">
                <div>
                  <span className="text-sm font-medium text-heading">{policy.policyName}</span>
                  <div className="flex gap-1 mt-1">
                    {policy.environments.map((env) => (
                      <span key={env} className="px-1.5 py-0.5 rounded text-xs font-medium bg-warning/10 text-warning">
                        {env}
                      </span>
                    ))}
                  </div>
                </div>
                {editingId !== policy.id && (
                  <button
                    onClick={() => startEdit(policy)}
                    className="flex items-center gap-1 px-2 py-1 text-xs border border-edge rounded hover:bg-elevated text-muted"
                  >
                    <Edit2 size={12} />
                    {t('edit')}
                  </button>
                )}
              </div>

              {editingId === policy.id ? (
                <div className="p-4 space-y-4">
                  <div>
                    <label className="block text-xs font-medium text-body mb-1">{t('allowedRolesLabel')}</label>
                    <div className="flex flex-wrap gap-2">
                      {ALL_ROLES.map((role) => (
                        <button
                          key={role}
                          type="button"
                          onClick={() => setEditForm((f) => ({ ...f, allowedRoles: toggleRole(f.allowedRoles, role) }))}
                          className={`px-2 py-1 rounded text-xs font-medium border transition-colors ${
                            editForm.allowedRoles.includes(role)
                              ? 'bg-accent text-white border-accent'
                              : 'bg-card text-body border-edge hover:bg-elevated'
                          }`}
                        >
                          {role}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-body mb-1">{t('requireJitLabel')}</label>
                    <div className="flex flex-wrap gap-2">
                      {ALL_ROLES.map((role) => (
                        <button
                          key={role}
                          type="button"
                          onClick={() => setEditForm((f) => ({ ...f, requireJitFor: toggleRole(f.requireJitFor, role) }))}
                          className={`px-2 py-1 rounded text-xs font-medium border transition-colors ${
                            editForm.requireJitFor.includes(role)
                              ? 'bg-warning text-white border-warning'
                              : 'bg-card text-body border-edge hover:bg-elevated'
                          }`}
                        >
                          {role}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-body mb-1">{t('descriptionLabel')}</label>
                    <textarea
                      value={editForm.description}
                      onChange={(e) => setEditForm((f) => ({ ...f, description: e.target.value }))}
                      rows={2}
                      className="w-full text-sm border border-edge rounded px-2 py-1 bg-canvas text-body focus:outline-none focus:ring-1 focus:ring-accent/50"
                    />
                  </div>
                  <div className="flex gap-2 justify-end">
                    <button
                      onClick={() => setEditingId(null)}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm border border-edge rounded hover:bg-elevated text-muted"
                    >
                      <X size={14} />
                      {t('cancel')}
                    </button>
                    <button
                      onClick={() => updateMutation.mutate({ id: policy.id, update: editForm })}
                      disabled={updateMutation.isPending}
                      className="flex items-center gap-1 px-3 py-1.5 text-sm bg-accent text-white rounded hover:bg-accent/90 disabled:opacity-50"
                    >
                      <Save size={14} />
                      {updateMutation.isPending ? t('saving') : t('save')}
                    </button>
                  </div>
                </div>
              ) : (
                <div className="p-4 text-sm text-muted space-y-2">
                  <div>
                    <span className="font-medium text-body">{t('allowedRolesLabel')}: </span>
                    {policy.allowedRoles.join(', ') || '—'}
                  </div>
                  {policy.requireJitFor.length > 0 && (
                    <div>
                      <span className="font-medium text-body">{t('requireJitLabel')}: </span>
                      {policy.requireJitFor.join(', ')}
                    </div>
                  )}
                  <div className="text-xs text-faded">{policy.description}</div>
                </div>
              )}
            </div>
          ))}
        </div>

        {data?.simulatedNote && (
          <p className="text-xs text-faded italic">{data.simulatedNote}</p>
        )}
      </div>
    </PageContainer>
  );
}
