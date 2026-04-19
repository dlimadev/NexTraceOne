import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Settings, DollarSign, Shield, CheckCircle, AlertTriangle, Plus, X, Activity, Trash2, BookOpen, Eye } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { finOpsApi } from '../api/finOps';
import { queryKeys } from '../../../shared/api/queryKeys';

const SUPPORTED_CURRENCIES = ['USD', 'EUR', 'GBP', 'BRL', 'JPY', 'CAD', 'AUD', 'CHF', 'CNY', 'INR'];
const CONFIG_API_BASE = '/api/v1/configuration/values';

export function FinOpsConfigurationPage() {
  const { t } = useTranslation();
  const qc = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: queryKeys.governance.finops.configuration(),
    queryFn: finOpsApi.getConfiguration,
    staleTime: 60_000,
  });

  const [newApprover, setNewApprover] = useState('');

  const saveMutation = useMutation({
    mutationFn: async (patch: {
      currency?: string;
      budgetGateEnabled?: boolean;
      blockOnExceed?: boolean;
      requireApproval?: boolean;
      alertThresholdPct?: number;
      approvers?: string[];
    }) => {
      const entries: Promise<unknown>[] = [];
      if (patch.currency !== undefined)
        entries.push(fetch(`${CONFIG_API_BASE}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ key: 'finops.budget.default_currency', value: patch.currency }) }));
      if (patch.budgetGateEnabled !== undefined)
        entries.push(fetch(`${CONFIG_API_BASE}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ key: 'finops.release.budget_gate.enabled', value: String(patch.budgetGateEnabled) }) }));
      if (patch.blockOnExceed !== undefined)
        entries.push(fetch(`${CONFIG_API_BASE}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ key: 'finops.release.budget_gate.block_on_exceed', value: String(patch.blockOnExceed) }) }));
      if (patch.requireApproval !== undefined)
        entries.push(fetch(`${CONFIG_API_BASE}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ key: 'finops.release.budget_gate.require_approval', value: String(patch.requireApproval) }) }));
      if (patch.approvers !== undefined)
        entries.push(fetch(`${CONFIG_API_BASE}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ key: 'finops.release.budget_gate.approvers', value: JSON.stringify(patch.approvers) }) }));
      if (patch.alertThresholdPct !== undefined)
        entries.push(fetch(`${CONFIG_API_BASE}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ key: 'finops.budget_alert_threshold', value: String(patch.alertThresholdPct) }) }));
      await Promise.all(entries);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.governance.finops.configuration() });
    },
  });

  const handleAddApprover = () => {
    if (!newApprover.trim() || !data) return;
    const updated = [...(data.approvers ?? []), newApprover.trim()];
    saveMutation.mutate({ approvers: updated });
    setNewApprover('');
  };

  const handleRemoveApprover = (email: string) => {
    if (!data) return;
    const updated = (data.approvers ?? []).filter((a) => a !== email);
    saveMutation.mutate({ approvers: updated });
  };

  if (isLoading) return <PageLoadingState />;
  if (isError || !data) return <PageErrorState onRetry={refetch} />;

  const gateAction = !data.budgetGateEnabled
    ? t('finops.config.gateDisabled')
    : !data.blockOnExceed
    ? t('finops.config.gateWarn')
    : data.requireApproval
    ? t('finops.config.gateRequireApproval')
    : t('finops.config.gateBlock');

  const gateVariant: 'success' | 'warning' | 'danger' | 'default' = !data.budgetGateEnabled
    ? 'default'
    : !data.blockOnExceed
    ? 'warning'
    : data.requireApproval
    ? 'warning'
    : 'danger';

  return (
    <PageContainer>
      <PageHeader
        icon={<Settings size={22} />}
        title={t('finops.config.title')}
        subtitle={t('finops.config.subtitle')}
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">

        {/* ── Moeda padrão ─────────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <DollarSign size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.currency.title')}</span>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-3">{t('finops.config.currency.description')}</p>
            <select
              value={data.currency}
              onChange={(e) => saveMutation.mutate({ currency: e.target.value })}
              className="input w-full text-sm"
              aria-label={t('finops.config.currency.selectLabel')}
            >
              {SUPPORTED_CURRENCIES.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
            {saveMutation.isPending && (
              <p className="text-xs text-muted mt-2">{t('common.saving')}</p>
            )}
          </CardBody>
        </Card>

        {/* ── Gate de orçamento ─────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.gate.title')}</span>
              <Badge variant={gateVariant} size="sm">{gateAction}</Badge>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-4">{t('finops.config.gate.description')}</p>

            <div className="space-y-3">
              <label className="flex items-center gap-3 cursor-pointer">
                <input
                  type="checkbox"
                  className="checkbox"
                  checked={data.budgetGateEnabled}
                  onChange={(e) => saveMutation.mutate({ budgetGateEnabled: e.target.checked })}
                  aria-label={t('finops.config.gate.enabled')}
                />
                <span className="text-sm">{t('finops.config.gate.enabled')}</span>
              </label>

              <label className={`flex items-center gap-3 cursor-pointer ${!data.budgetGateEnabled ? 'opacity-40 pointer-events-none' : ''}`}>
                <input
                  type="checkbox"
                  className="checkbox"
                  checked={data.blockOnExceed}
                  onChange={(e) => saveMutation.mutate({ blockOnExceed: e.target.checked })}
                  disabled={!data.budgetGateEnabled}
                  aria-label={t('finops.config.gate.blockOnExceed')}
                />
                <span className="text-sm">{t('finops.config.gate.blockOnExceed')}</span>
              </label>

              <label className={`flex items-center gap-3 cursor-pointer ${(!data.budgetGateEnabled || !data.blockOnExceed) ? 'opacity-40 pointer-events-none' : ''}`}>
                <input
                  type="checkbox"
                  className="checkbox"
                  checked={data.requireApproval}
                  onChange={(e) => saveMutation.mutate({ requireApproval: e.target.checked })}
                  disabled={!data.budgetGateEnabled || !data.blockOnExceed}
                  aria-label={t('finops.config.gate.requireApproval')}
                />
                <span className="text-sm">{t('finops.config.gate.requireApproval')}</span>
              </label>

              <div className={!data.budgetGateEnabled ? 'opacity-40' : ''}>
                <label className="text-xs text-muted mb-1 block">{t('finops.config.gate.alertThreshold')}</label>
                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    className="input w-20 text-sm"
                    min={1}
                    max={200}
                    value={data.alertThresholdPct}
                    disabled={!data.budgetGateEnabled}
                    onChange={(e) => saveMutation.mutate({ alertThresholdPct: Number(e.target.value) })}
                    aria-label={t('finops.config.gate.alertThreshold')}
                  />
                  <span className="text-sm text-muted">%</span>
                </div>
              </div>
            </div>
          </CardBody>
        </Card>

        {/* ── Aprovadores ────────────────────────────────────── */}
        <Card className="md:col-span-2">
          <CardHeader>
            <div className="flex items-center gap-2">
              <CheckCircle size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.approvers.title')}</span>
              <Badge variant="default" size="sm">{data.approvers.length}</Badge>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-4">{t('finops.config.approvers.description')}</p>

            {(!data.budgetGateEnabled || !data.blockOnExceed || !data.requireApproval) && (
              <div className="flex items-center gap-2 p-3 bg-warning/10 rounded mb-4">
                <AlertTriangle size={14} className="text-warning flex-shrink-0" />
                <p className="text-xs text-muted">{t('finops.config.approvers.inactiveNote')}</p>
              </div>
            )}

            <div className="flex flex-wrap gap-2 mb-4">
              {data.approvers.map((email) => (
                <div key={email} className="flex items-center gap-1.5 bg-surface-hover rounded-md px-2 py-1">
                  <span className="text-xs">{email}</span>
                  <button
                    onClick={() => handleRemoveApprover(email)}
                    className="text-muted hover:text-critical transition-colors"
                    aria-label={t('finops.config.approvers.remove', { email })}
                  >
                    <X size={12} />
                  </button>
                </div>
              ))}
              {data.approvers.length === 0 && (
                <p className="text-xs text-muted italic">{t('finops.config.approvers.empty')}</p>
              )}
            </div>

            <div className="flex items-center gap-2">
              <input
                type="email"
                className="input flex-1 text-sm"
                placeholder={t('finops.config.approvers.placeholder')}
                value={newApprover}
                onChange={(e) => setNewApprover(e.target.value)}
                onKeyDown={(e) => { if (e.key === 'Enter') handleAddApprover(); }}
                aria-label={t('finops.config.approvers.placeholder')}
              />
              <button
                onClick={handleAddApprover}
                disabled={!newApprover.trim()}
                className="btn btn-primary btn-sm flex items-center gap-1"
              >
                <Plus size={14} />
                {t('finops.config.approvers.add')}
              </button>
            </div>
          </CardBody>
        </Card>

        {/* ── Detecção de anomalias ─────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Activity size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.anomaly.title')}</span>
              <Badge variant={data.anomalyDetectionEnabled ? 'success' : 'default'} size="sm">
                {data.anomalyDetectionEnabled ? t('common.active') : t('common.inactive')}
              </Badge>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-3">{t('finops.config.anomaly.description')}</p>
            <dl className="space-y-2 text-xs">
              <div className="flex justify-between">
                <dt className="text-muted">{t('finops.config.anomaly.enabled')}</dt>
                <dd>{data.anomalyDetectionEnabled ? t('common.yes') : t('common.no')}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted">{t('finops.config.anomaly.comparisonWindow')}</dt>
                <dd>{data.comparisonWindowDays} {t('common.days')}</dd>
              </div>
            </dl>
          </CardBody>
        </Card>

        {/* ── Detecção de desperdício ───────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Trash2 size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.waste.title')}</span>
              <Badge variant={data.wasteDetectionEnabled ? 'success' : 'default'} size="sm">
                {data.wasteDetectionEnabled ? t('common.active') : t('common.inactive')}
              </Badge>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-3">{t('finops.config.waste.description')}</p>
            <dl className="space-y-2 text-xs">
              <div className="flex justify-between">
                <dt className="text-muted">{t('finops.config.waste.enabled')}</dt>
                <dd>{data.wasteDetectionEnabled ? t('common.yes') : t('common.no')}</dd>
              </div>
              {data.wasteCategories.length > 0 && (
                <div>
                  <dt className="text-muted mb-1">{t('finops.config.waste.categories')}</dt>
                  <dd className="flex flex-wrap gap-1">
                    {data.wasteCategories.map((cat) => (
                      <Badge key={cat} variant="default" size="sm">{cat}</Badge>
                    ))}
                  </dd>
                </div>
              )}
            </dl>
          </CardBody>
        </Card>

        {/* ── Recomendações ─────────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <BookOpen size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.recommendations.title')}</span>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-3">{t('finops.config.recommendations.description')}</p>
            {data.recommendationPolicy ? (
              <pre className="text-xs bg-surface-hover rounded p-2 overflow-x-auto whitespace-pre-wrap">
                {(() => { try { return JSON.stringify(JSON.parse(data.recommendationPolicy), null, 2); } catch { return data.recommendationPolicy; } })()}
              </pre>
            ) : (
              <p className="text-xs text-muted italic">{t('finops.config.recommendations.notConfigured')}</p>
            )}
          </CardBody>
        </Card>

        {/* ── Showback / Chargeback ─────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Eye size={16} className="text-accent" />
              <span className="font-semibold text-sm">{t('finops.config.showback.title')}</span>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-xs text-muted mb-3">{t('finops.config.showback.description')}</p>
            <dl className="space-y-2 text-xs">
              <div className="flex justify-between">
                <dt className="text-muted">{t('finops.config.showback.showbackEnabled')}</dt>
                <dd>
                  <Badge variant={data.showbackEnabled ? 'success' : 'default'} size="sm">
                    {data.showbackEnabled ? t('common.active') : t('common.inactive')}
                  </Badge>
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-muted">{t('finops.config.showback.chargebackEnabled')}</dt>
                <dd>
                  <Badge variant={data.chargebackEnabled ? 'warning' : 'default'} size="sm">
                    {data.chargebackEnabled ? t('common.active') : t('common.inactive')}
                  </Badge>
                </dd>
              </div>
            </dl>
          </CardBody>
        </Card>

      </div>
    </PageContainer>
  );
}
