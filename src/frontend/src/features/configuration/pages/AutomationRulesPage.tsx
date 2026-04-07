import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Zap, Plus, Trash2, ToggleLeft, ToggleRight } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

type Trigger =
  | 'on_change_created'
  | 'on_incident_opened'
  | 'on_contract_published'
  | 'on_approval_expired';

interface RuleSummary {
  ruleId: string;
  name: string;
  trigger: Trigger;
  conditionsJson: string;
  actionsJson: string;
  isEnabled: boolean;
  createdAt: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useAutomationRules = () =>
  useQuery({
    queryKey: ['automation-rules'],
    queryFn: () =>
      client
        .get<{ items: RuleSummary[]; totalCount: number }>('/api/v1/automation-rules')
        .then((r) => r.data),
  });

const useCreateRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      name: string;
      trigger: string;
      conditionsJson: string;
      actionsJson: string;
    }) => client.post('/api/v1/automation-rules', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-rules'] }),
  });
};

const useToggleRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ruleId, enabled }: { ruleId: string; enabled: boolean }) =>
      client.patch(`/api/v1/automation-rules/${ruleId}/toggle`, { ruleId, enabled }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-rules'] }),
  });
};

const useDeleteRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ruleId: string) => client.delete(`/api/v1/automation-rules/${ruleId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-rules'] }),
  });
};

// ── Constants ──────────────────────────────────────────────────────────────────

const TRIGGERS: Trigger[] = [
  'on_change_created',
  'on_incident_opened',
  'on_contract_published',
  'on_approval_expired',
];

const TRIGGER_BADGE: Record<Trigger, 'info' | 'warning' | 'success' | 'neutral'> = {
  on_change_created: 'info',
  on_incident_opened: 'warning',
  on_contract_published: 'success',
  on_approval_expired: 'neutral',
};

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * AutomationRulesPage — gestão de regras de automação If-Then do tenant.
 * Permite criar regras que reagam a eventos da plataforma e executem acções.
 * Pilar: Platform Customization — Workflows & Automation
 */
export function AutomationRulesPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useAutomationRules();
  const createRule = useCreateRule();
  const toggleRule = useToggleRule();
  const deleteRule = useDeleteRule();

  const [showBuilder, setShowBuilder] = useState(false);
  const [ruleName, setRuleName] = useState('');
  const [trigger, setTrigger] = useState<Trigger>('on_change_created');

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const canCreate = ruleName.trim().length > 0;

  const handleCreate = () => {
    if (!canCreate) return;
    createRule.mutate(
      { name: ruleName.trim(), trigger, conditionsJson: '[]', actionsJson: '[]' },
      {
        onSuccess: () => {
          setShowBuilder(false);
          setRuleName('');
          setTrigger('on_change_created');
        },
      }
    );
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('workflows.automationRules.title')}
        actions={
          <Button size="sm" onClick={() => setShowBuilder(true)}>
            <Plus className="w-4 h-4 mr-1" />
            {t('workflows.automationRules.create')}
          </Button>
        }
      />

      <PageSection>
        {!data?.items.length ? (
          <EmptyState
            icon={<Zap className="w-8 h-8 text-gray-400" />}
            title={t('workflows.automationRules.empty')}
            action={
              <Button size="sm" onClick={() => setShowBuilder(true)}>
                {t('workflows.automationRules.create')}
              </Button>
            }
          />
        ) : (
          <div className="space-y-3">
            {data.items.map((rule) => (
              <Card key={rule.ruleId}>
                <CardBody>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <button
                        onClick={() =>
                          toggleRule.mutate({ ruleId: rule.ruleId, enabled: !rule.isEnabled })
                        }
                        className="text-gray-400 hover:text-blue-600 transition-colors"
                        aria-label={
                          rule.isEnabled
                            ? t('workflows.automationRules.disabled')
                            : t('workflows.automationRules.enabled')
                        }
                      >
                        {rule.isEnabled ? (
                          <ToggleRight className="w-6 h-6 text-blue-600" />
                        ) : (
                          <ToggleLeft className="w-6 h-6" />
                        )}
                      </button>
                      <div>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">
                          {rule.name}
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                          {t('workflows.automationRules.trigger')}: {rule.trigger}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={TRIGGER_BADGE[rule.trigger]}>
                        {rule.isEnabled
                          ? t('workflows.automationRules.enabled')
                          : t('workflows.automationRules.disabled')}
                      </Badge>
                      <button
                        onClick={() => deleteRule.mutate(rule.ruleId)}
                        className="text-gray-400 hover:text-red-500 transition-colors ml-2"
                        aria-label={t('common.delete')}
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      {showBuilder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-md p-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              {t('workflows.automationRules.create')}
            </h2>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('common.name')}
                </label>
                <input
                  type="text"
                  value={ruleName}
                  onChange={(e) => setRuleName(e.target.value)}
                  className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-transparent text-gray-700 dark:text-gray-300"
                  placeholder={t('workflows.automationRules.title')}
                />
              </div>

              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('workflows.automationRules.trigger')}
                </label>
                <select
                  value={trigger}
                  onChange={(e) => setTrigger(e.target.value as Trigger)}
                  className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                >
                  {TRIGGERS.map((tr) => (
                    <option key={tr} value={tr}>
                      {tr}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="mt-6 flex items-center justify-between">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setShowBuilder(false);
                  setRuleName('');
                  setTrigger('on_change_created');
                }}
              >
                {t('common.cancel')}
              </Button>
              <Button
                size="sm"
                disabled={!canCreate || createRule.isPending}
                onClick={handleCreate}
              >
                {t('common.save')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
