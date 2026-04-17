import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ShieldAlert, Plus, Trash2 } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

const APPROVAL_TYPES = ['Manual', 'ExternalWebhook', 'ExternalServiceNow', 'AutoApprove'];

function approvalTypeBadge(type: string): 'default' | 'info' | 'success' | 'warning' | 'danger' {
  switch (type) {
    case 'Manual':
      return 'warning';
    case 'ExternalWebhook':
      return 'info';
    case 'ExternalServiceNow':
      return 'info';
    case 'AutoApprove':
      return 'success';
    default:
      return 'default';
  }
}

/**
 * ReleaseApprovalPoliciesPage — gestão de políticas de aprovação de releases.
 *
 * Permite que administradores configurem as políticas que definem:
 * - Para qual ambiente/serviço a aprovação é necessária
 * - O tipo de aprovação (Manual, External Webhook, ServiceNow, AutoApprove)
 * - Requisitos adicionais (Evidence Pack, Checklist completion, Risk score threshold)
 * - Janelas de congelamento e expiração do token de callback
 */
export function ReleaseApprovalPoliciesPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const queryClient = useQueryClient();

  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    name: '',
    approvalType: 'Manual',
    environmentId: '',
    serviceTag: '',
    externalWebhookUrl: '',
    minApprovers: 1,
    expirationHours: 48,
    requireEvidencePack: false,
    requireChecklistCompletion: false,
    minRiskScoreForManualApproval: '',
    priority: 100,
  });

  const { data: policies, isLoading, error } = useQuery({
    queryKey: ['release-approval-policies', activeEnvironmentId],
    queryFn: () => changeIntelligenceApi.listApprovalPolicies(),
  });

  const createMutation = useMutation({
    mutationFn: () => changeIntelligenceApi.createApprovalPolicy({
      name: form.name,
      approvalType: form.approvalType,
      environmentId: form.environmentId || undefined,
      serviceTag: form.serviceTag || undefined,
      externalWebhookUrl: form.externalWebhookUrl || undefined,
      minApprovers: form.minApprovers,
      expirationHours: form.expirationHours,
      requireEvidencePack: form.requireEvidencePack,
      requireChecklistCompletion: form.requireChecklistCompletion,
      minRiskScoreForManualApproval: form.minRiskScoreForManualApproval
        ? Number(form.minRiskScoreForManualApproval)
        : undefined,
      priority: form.priority,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-approval-policies'] });
      setShowForm(false);
      setForm({
        name: '',
        approvalType: 'Manual',
        environmentId: '',
        serviceTag: '',
        externalWebhookUrl: '',
        minApprovers: 1,
        expirationHours: 48,
        requireEvidencePack: false,
        requireChecklistCompletion: false,
        minRiskScoreForManualApproval: '',
        priority: 100,
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (policyId: string) => changeIntelligenceApi.deleteApprovalPolicy(policyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['release-approval-policies'] });
    },
  });

  if (isLoading) return <PageLoadingState />;
  if (error) return <PageErrorState />;

  return (
    <PageContainer>
      <PageHeader
        icon={<ShieldAlert className="w-6 h-6 text-accent" />}
        title={t('approvalPolicies.title')}
        subtitle={t('approvalPolicies.subtitle')}
        actions={
          <button
            onClick={() => setShowForm(!showForm)}
            className="inline-flex items-center gap-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent/90 transition-colors"
          >
            <Plus className="w-4 h-4" />
            {t('approvalPolicies.newPolicy')}
          </button>
        }
      />

      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h3 className="text-sm font-semibold text-heading">{t('approvalPolicies.newPolicyTitle')}</h3>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.nameLabel')}</label>
                <input
                  className={INPUT_CLS}
                  value={form.name}
                  onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                  placeholder={t('approvalPolicies.namePlaceholder')}
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.approvalTypeLabel')}</label>
                <select
                  className={INPUT_CLS}
                  value={form.approvalType}
                  onChange={e => setForm(f => ({ ...f, approvalType: e.target.value }))}
                >
                  {APPROVAL_TYPES.map(t => (
                    <option key={t} value={t}>{t}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.environmentLabel')}</label>
                <input
                  className={INPUT_CLS}
                  value={form.environmentId}
                  onChange={e => setForm(f => ({ ...f, environmentId: e.target.value }))}
                  placeholder={t('approvalPolicies.environmentPlaceholder')}
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.serviceTagLabel')}</label>
                <input
                  className={INPUT_CLS}
                  value={form.serviceTag}
                  onChange={e => setForm(f => ({ ...f, serviceTag: e.target.value }))}
                  placeholder={t('approvalPolicies.serviceTagPlaceholder')}
                />
              </div>

              {form.approvalType === 'ExternalWebhook' && (
                <div className="sm:col-span-2">
                  <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.webhookUrlLabel')}</label>
                  <input
                    className={INPUT_CLS}
                    value={form.externalWebhookUrl}
                    onChange={e => setForm(f => ({ ...f, externalWebhookUrl: e.target.value }))}
                    placeholder="https://external-system.example.com/approval-request"
                  />
                </div>
              )}

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.minApproversLabel')}</label>
                <input
                  type="number"
                  min={1}
                  max={20}
                  className={INPUT_CLS}
                  value={form.minApprovers}
                  onChange={e => setForm(f => ({ ...f, minApprovers: Number(e.target.value) }))}
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.expirationHoursLabel')}</label>
                <input
                  type="number"
                  min={1}
                  max={168}
                  className={INPUT_CLS}
                  value={form.expirationHours}
                  onChange={e => setForm(f => ({ ...f, expirationHours: Number(e.target.value) }))}
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.minRiskScoreLabel')}</label>
                <input
                  type="number"
                  min={1}
                  max={100}
                  className={INPUT_CLS}
                  value={form.minRiskScoreForManualApproval}
                  onChange={e => setForm(f => ({ ...f, minRiskScoreForManualApproval: e.target.value }))}
                  placeholder={t('approvalPolicies.minRiskScorePlaceholder')}
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('approvalPolicies.priorityLabel')}</label>
                <input
                  type="number"
                  min={1}
                  max={999}
                  className={INPUT_CLS}
                  value={form.priority}
                  onChange={e => setForm(f => ({ ...f, priority: Number(e.target.value) }))}
                />
              </div>

              <div className="flex items-center gap-4 sm:col-span-2">
                <label className="flex items-center gap-2 text-sm text-heading cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.requireEvidencePack}
                    onChange={e => setForm(f => ({ ...f, requireEvidencePack: e.target.checked }))}
                    className="rounded border-edge"
                  />
                  {t('approvalPolicies.requireEvidencePack')}
                </label>
                <label className="flex items-center gap-2 text-sm text-heading cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.requireChecklistCompletion}
                    onChange={e => setForm(f => ({ ...f, requireChecklistCompletion: e.target.checked }))}
                    className="rounded border-edge"
                  />
                  {t('approvalPolicies.requireChecklist')}
                </label>
              </div>
            </div>

            <div className="flex items-center gap-3 mt-4 pt-4 border-t border-edge">
              <button
                onClick={() => createMutation.mutate()}
                disabled={!form.name || createMutation.isPending}
                className="inline-flex items-center gap-2 rounded-md bg-accent px-4 py-2 text-sm font-medium text-white hover:bg-accent/90 disabled:opacity-50 transition-colors"
              >
                {t('approvalPolicies.savePolicy')}
              </button>
              <button
                onClick={() => setShowForm(false)}
                className="text-sm text-muted hover:text-heading transition-colors"
              >
                {t('common.cancel')}
              </button>
            </div>
          </CardBody>
        </Card>
      )}

      <Card>
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">{t('approvalPolicies.activePolicies')}</h3>
        </CardHeader>
        <CardBody>
          {!policies || policies.length === 0 ? (
            <p className="text-sm text-muted py-4 text-center">{t('approvalPolicies.noPolicies')}</p>
          ) : (
            <div className="space-y-3">
              {policies.map((policy: any) => (
                <div
                  key={policy.id}
                  className="flex items-start justify-between rounded-lg border border-edge bg-surface p-4"
                >
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-sm font-medium text-heading">{policy.name}</span>
                      <Badge variant={approvalTypeBadge(policy.approvalType)}>{policy.approvalType}</Badge>
                      <span className="text-xs text-muted">{t('approvalPolicies.priorityBadge', { priority: policy.priority })}</span>
                    </div>
                    <div className="mt-1 flex items-center gap-3 text-xs text-muted flex-wrap">
                      {policy.environmentId && (
                        <span>{t('approvalPolicies.envLabel')}: <span className="text-heading">{policy.environmentId}</span></span>
                      )}
                      {policy.serviceTag && (
                        <span>{t('approvalPolicies.serviceTagShort')}: <span className="text-heading">{policy.serviceTag}</span></span>
                      )}
                      {policy.requireEvidencePack && (
                        <span className="text-accent">{t('approvalPolicies.requiresEvidenceBadge')}</span>
                      )}
                      {policy.requireChecklistCompletion && (
                        <span className="text-accent">{t('approvalPolicies.requiresChecklistBadge')}</span>
                      )}
                      {policy.minRiskScoreForManualApproval && (
                        <span>{t('approvalPolicies.riskThreshold')}: {policy.minRiskScoreForManualApproval}</span>
                      )}
                    </div>
                  </div>
                  <button
                    onClick={() => deleteMutation.mutate(policy.id)}
                    disabled={deleteMutation.isPending}
                    className="ml-4 text-muted hover:text-danger transition-colors"
                    aria-label={t('approvalPolicies.deletePolicy')}
                    title={t('approvalPolicies.deletePolicy')}
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </PageContainer>
  );
}
