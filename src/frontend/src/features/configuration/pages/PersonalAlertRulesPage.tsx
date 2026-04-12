import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Bell, Plus, Trash2, ToggleLeft, ToggleRight } from 'lucide-react';
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

type Channel = 'in-app' | 'email' | 'webhook';
type EntityField = 'risk_level' | 'status' | 'criticality' | 'severity' | 'compliance_status';
type Operator = '>=' | '<=' | '==' | '!=' | 'contains' | 'changed';

interface RuleSummary {
  ruleId: string;
  name: string;
  condition: string;
  channel: Channel;
  isEnabled: boolean;
  createdAt: string;
}

interface ConditionBuilder {
  entity: string;
  field: EntityField | '';
  operator: Operator | '';
  value: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useAlertRules = () =>
  useQuery({
    queryKey: ['alert-rules'],
    queryFn: () =>
      client
        .get<{ items: RuleSummary[]; totalCount: number }>('/api/v1/alert-rules')
        .then((r) => r.data),
  });

const useCreateRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; condition: string; channel: Channel }) =>
      client.post('/api/v1/alert-rules', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['alert-rules'] }),
  });
};

const useToggleRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ruleId, enabled }: { ruleId: string; enabled: boolean }) =>
      client.patch(`/api/v1/alert-rules/${ruleId}/toggle`, { ruleId, enabled }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['alert-rules'] }),
  });
};

const useDeleteRule = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ruleId: string) => client.delete(`/api/v1/alert-rules/${ruleId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['alert-rules'] }),
  });
};

// ── Constants ──────────────────────────────────────────────────────────────────

const ENTITIES = ['service', 'contract', 'change', 'incident'];

const ENTITY_FIELDS: Record<string, EntityField[]> = {
  service: ['risk_level', 'status', 'criticality'],
  contract: ['status', 'compliance_status'],
  change: ['risk_level', 'status'],
  incident: ['severity', 'status'],
};

const OPERATORS: Operator[] = ['>=', '<=', '==', '!=', 'contains', 'changed'];
const CHANNELS: Channel[] = ['in-app', 'email', 'webhook'];

const CHANNEL_BADGE: Record<Channel, 'info' | 'neutral' | 'warning'> = {
  'in-app': 'info',
  'email': 'neutral',
  'webhook': 'warning',
};

const initialCondition: ConditionBuilder = { entity: 'service', field: '', operator: '', value: '' };

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * PersonalAlertRulesPage — gestão de regras de alerta personalizadas do utilizador.
 * Permite criar condições para receber notificações sobre eventos específicos.
 * Pilar: Platform Customization — Alertas & Notificações
 */
export function PersonalAlertRulesPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useAlertRules();
  const createRule = useCreateRule();
  const toggleRule = useToggleRule();
  const deleteRule = useDeleteRule();

  const [showBuilder, setShowBuilder] = useState(false);
  const [ruleName, setRuleName] = useState('');
  const [condition, setCondition] = useState<ConditionBuilder>(initialCondition);
  const [channel, setChannel] = useState<Channel>('in-app');

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const conditionJson = () =>
    JSON.stringify({
      entity: condition.entity,
      field: condition.field,
      operator: condition.operator,
      value: condition.value,
    });

  const canCreate = ruleName.trim().length > 0 && condition.field !== '' && condition.operator !== '';

  const handleCreate = () => {
    if (!canCreate) return;
    createRule.mutate(
      { name: ruleName.trim(), condition: conditionJson(), channel },
      {
        onSuccess: () => {
          setShowBuilder(false);
          setRuleName('');
          setCondition(initialCondition);
          setChannel('in-app');
        },
      }
    );
  };

  const handleCloseBuilder = () => {
    setShowBuilder(false);
    setRuleName('');
    setCondition(initialCondition);
    setChannel('in-app');
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('alertRules.title')}
        subtitle={t('alertRules.subtitle')}
        actions={
          <Button size="sm" onClick={() => setShowBuilder(true)}>
            <Plus className="w-4 h-4 mr-1" />
            {t('alertRules.newRule')}
          </Button>
        }
      />

      <PageSection>
        {!data?.items.length ? (
          <EmptyState
            icon={<Bell className="w-8 h-8 text-gray-400" />}
            title={t('alertRules.empty.title')}
            description={t('alertRules.empty.description')}
            action={
              <Button size="sm" onClick={() => setShowBuilder(true)}>
                {t('alertRules.empty.cta')}
              </Button>
            }
          />
        ) : (
          <div className="space-y-3">
            {data.items.map((rule) => {
              let parsedCondition: Record<string, string> | null = null;
              try {
                parsedCondition = JSON.parse(rule.condition) as Record<string, string>;
              } catch {
                // ignore malformed condition
              }

              return (
                <Card key={rule.ruleId}>
                  <CardBody>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <button
                          onClick={() =>
                            toggleRule.mutate({ ruleId: rule.ruleId, enabled: !rule.isEnabled })
                          }
                          className="text-gray-400 hover:text-blue-600 transition-colors"
                          aria-label={rule.isEnabled ? t('alertRules.disable') : t('alertRules.enable')}
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
                          {parsedCondition && (
                            <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                              {parsedCondition.entity} · {parsedCondition.field}{' '}
                              {parsedCondition.operator} {parsedCondition.value}
                            </p>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={CHANNEL_BADGE[rule.channel as Channel]}>
                          {rule.channel}
                        </Badge>
                        <button
                          onClick={() => deleteRule.mutate(rule.ruleId)}
                          className="text-gray-400 hover:text-red-500 transition-colors ml-2"
                          aria-label={t('alertRules.delete')}
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </div>
                  </CardBody>
                </Card>
              );
            })}
          </div>
        )}
      </PageSection>

      {/* ── Builder modal ────────────────────────────────────────────────── */}
      {showBuilder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-md p-6">
            <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              {t('alertRules.builder.title')}
            </h2>

            <div className="space-y-4">
              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('alertRules.builder.name')}
                </label>
                <input
                  type="text"
                  value={ruleName}
                  onChange={(e) => setRuleName(e.target.value)}
                  className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-transparent text-gray-700 dark:text-gray-300"
                  placeholder={t('alertRules.builder.namePlaceholder')}
                />
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                    {t('alertRules.builder.entity')}
                  </label>
                  <select
                    value={condition.entity}
                    onChange={(e) =>
                      setCondition((c) => ({ ...c, entity: e.target.value, field: '', operator: '' }))
                    }
                    className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                  >
                    {ENTITIES.map((e) => (
                      <option key={e} value={e}>
                        {e}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                    {t('alertRules.builder.field')}
                  </label>
                  <select
                    value={condition.field}
                    onChange={(e) =>
                      setCondition((c) => ({ ...c, field: e.target.value as EntityField }))
                    }
                    className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                  >
                    <option value="">{t('alertRules.builder.selectField')}</option>
                    {(ENTITY_FIELDS[condition.entity] ?? []).map((f) => (
                      <option key={f} value={f}>
                        {f}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                    {t('alertRules.builder.operator')}
                  </label>
                  <select
                    value={condition.operator}
                    onChange={(e) =>
                      setCondition((c) => ({ ...c, operator: e.target.value as Operator }))
                    }
                    className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                  >
                    <option value="">{t('alertRules.builder.selectOperator')}</option>
                    {OPERATORS.map((op) => (
                      <option key={op} value={op}>
                        {op}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                    {t('alertRules.builder.value')}
                  </label>
                  <input
                    type="text"
                    value={condition.value}
                    onChange={(e) => setCondition((c) => ({ ...c, value: e.target.value }))}
                    className="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm bg-transparent text-gray-700 dark:text-gray-300"
                    placeholder="high"
                  />
                </div>
              </div>

              <div>
                <label className="text-xs text-gray-500 dark:text-gray-400 mb-1 block">
                  {t('alertRules.builder.channel')}
                </label>
                <div className="flex gap-2">
                  {CHANNELS.map((ch) => (
                    <button
                      key={ch}
                      onClick={() => setChannel(ch)}
                      className={`text-sm px-3 py-1.5 rounded-lg border transition-colors ${
                        channel === ch
                          ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300'
                          : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-blue-300'
                      }`}
                    >
                      {ch}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="mt-6 flex items-center justify-between">
              <Button variant="outline" size="sm" onClick={handleCloseBuilder}>
                {t('common.cancel')}
              </Button>
              <Button
                size="sm"
                disabled={!canCreate || createRule.isPending}
                onClick={handleCreate}
              >
                {t('alertRules.builder.save')}
              </Button>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
