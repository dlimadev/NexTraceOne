import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ShieldAlert, Plus, Trash2 } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { IconButton } from '../../../components/IconButton';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { Checkbox } from '../../../components/Checkbox';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

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
          <Button variant="primary" size="sm" onClick={() => setShowForm(!showForm)}>
            <Plus className="w-4 h-4 mr-2" />
            {t('approvalPolicies.newPolicy')}
          </Button>
        }
      />

      {showForm && (
        <Card className="mb-6">
          <CardHeader>
            <h3 className="text-sm font-semibold text-heading">{t('approvalPolicies.newPolicyTitle')}</h3>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <TextField
                size="sm"
                label={t('approvalPolicies.nameLabel')}
                value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder={t('approvalPolicies.namePlaceholder')}
              />
              <Select
                size="sm"
                label={t('approvalPolicies.approvalTypeLabel')}
                value={form.approvalType}
                onChange={e => setForm(f => ({ ...f, approvalType: e.target.value }))}
                options={APPROVAL_TYPES.map(ty => ({ value: ty, label: ty }))}
              />
              <TextField
                size="sm"
                label={t('approvalPolicies.environmentLabel')}
                value={form.environmentId}
                onChange={e => setForm(f => ({ ...f, environmentId: e.target.value }))}
                placeholder={t('approvalPolicies.environmentPlaceholder')}
              />
              <TextField
                size="sm"
                label={t('approvalPolicies.serviceTagLabel')}
                value={form.serviceTag}
                onChange={e => setForm(f => ({ ...f, serviceTag: e.target.value }))}
                placeholder={t('approvalPolicies.serviceTagPlaceholder')}
              />

              {form.approvalType === 'ExternalWebhook' && (
                <div className="sm:col-span-2">
                  <TextField
                    size="sm"
                    label={t('approvalPolicies.webhookUrlLabel')}
                    value={form.externalWebhookUrl}
                    onChange={e => setForm(f => ({ ...f, externalWebhookUrl: e.target.value }))}
                    placeholder="https://external-system.example.com/approval-request"
                  />
                </div>
              )}

              <TextField
                size="sm"
                type="number"
                label={t('approvalPolicies.minApproversLabel')}
                min={1}
                max={20}
                value={form.minApprovers}
                onChange={e => setForm(f => ({ ...f, minApprovers: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('approvalPolicies.expirationHoursLabel')}
                min={1}
                max={168}
                value={form.expirationHours}
                onChange={e => setForm(f => ({ ...f, expirationHours: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('approvalPolicies.minRiskScoreLabel')}
                min={1}
                max={100}
                value={form.minRiskScoreForManualApproval}
                onChange={e => setForm(f => ({ ...f, minRiskScoreForManualApproval: e.target.value }))}
                placeholder={t('approvalPolicies.minRiskScorePlaceholder')}
              />
              <TextField
                size="sm"
                type="number"
                label={t('approvalPolicies.priorityLabel')}
                min={1}
                max={999}
                value={form.priority}
                onChange={e => setForm(f => ({ ...f, priority: Number(e.target.value) }))}
              />

              <div className="flex items-center gap-4 sm:col-span-2">
                <Checkbox
                  checked={form.requireEvidencePack}
                  onChange={e => setForm(f => ({ ...f, requireEvidencePack: e.target.checked }))}
                  label={t('approvalPolicies.requireEvidencePack')}
                />
                <Checkbox
                  checked={form.requireChecklistCompletion}
                  onChange={e => setForm(f => ({ ...f, requireChecklistCompletion: e.target.checked }))}
                  label={t('approvalPolicies.requireChecklist')}
                />
              </div>
            </div>

            <div className="flex items-center gap-3 mt-4 pt-4 border-t border-edge">
              <Button
                variant="primary"
                size="sm"
                onClick={() => createMutation.mutate()}
                disabled={!form.name || createMutation.isPending}
                loading={createMutation.isPending}
              >
                {t('approvalPolicies.savePolicy')}
              </Button>
              <Button variant="ghost" size="sm" onClick={() => setShowForm(false)}>
                {t('common.cancel')}
              </Button>
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
                  className="flex items-start justify-between rounded-lg border border-edge bg-card p-4"
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
                  <IconButton
                    variant="ghost"
                    size="sm"
                    className="ml-4 hover:text-critical"
                    onClick={() => deleteMutation.mutate(policy.id)}
                    disabled={deleteMutation.isPending}
                    label={t('approvalPolicies.deletePolicy')}
                    title={t('approvalPolicies.deletePolicy')}
                    icon={<Trash2 className="w-4 h-4" />}
                  />
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </PageContainer>
  );
}
