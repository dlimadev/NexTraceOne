import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Bell,
  BellOff,
  XCircle,
  AlertTriangle,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import {
  platformAdminApi,
  type PlatformAlertRule,
  type PlatformAlertHistoryEntry,
  type PlatformAlertSeverity,
  type PlatformAlertStatus,
} from '../api/platformAdmin';

export function PlatformAlertRulesPage() {
  const { t } = useTranslation('platformAlerts');
  const queryClient = useQueryClient();
  const [editingRuleId, setEditingRuleId] = useState<string | null>(null);
  const [editValues, setEditValues] = useState<Record<string, string>>({});

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['platform-alerts'],
    queryFn: platformAdminApi.getPlatformAlerts,
    refetchInterval: 30_000,
  });

  const updateMutation = useMutation({
    mutationFn: ({ ruleId, values }: { ruleId: string; values: Record<string, string> }) =>
      platformAdminApi.updateAlertRule(ruleId, {
        warningThreshold: parseFloat(values.warning ?? '0'),
        criticalThreshold: parseFloat(values.critical ?? '0'),
        enabled: values.enabled === 'true',
        cooldownMinutes: parseInt(values.cooldown ?? '15', 10),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platform-alerts'] });
      setEditingRuleId(null);
    },
  });

  function startEdit(rule: PlatformAlertRule) {
    setEditingRuleId(rule.id);
    setEditValues({
      warning: String(rule.warningThreshold),
      critical: String(rule.criticalThreshold),
      enabled: String(rule.enabled),
      cooldown: String(rule.cooldownMinutes),
    });
  }

  function cancelEdit() {
    setEditingRuleId(null);
    setEditValues({});
  }

  function saveEdit(ruleId: string) {
    updateMutation.mutate({ ruleId, values: editValues });
  }

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          actions={
            <Button variant="primary" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && (
          <>
            {/* Summary */}
            <div className="grid grid-cols-3 gap-4">
              <SummaryCard
                label={t('activeAlerts')}
                value={data.activeAlertCount}
                color={data.activeAlertCount > 0 ? 'critical' : 'success'}
              />
              <SummaryCard
                label={t('totalRules')}
                value={data.rules.length}
                color="body"
              />
              <SummaryCard
                label={t('enabledRules')}
                value={data.rules.filter((r) => r.enabled).length}
                color="accent"
              />
            </div>

            {/* Rules Table */}
            <section>
              <h2 className="text-lg font-medium text-heading mb-3">{t('rulesTitle')}</h2>
              <div className="border border-edge rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-elevated border-b border-edge">
                    <tr>
                      <th className="text-left px-4 py-3 text-muted font-medium">{t('colRule')}</th>
                      <th className="text-left px-4 py-3 text-muted font-medium">{t('colWarning')}</th>
                      <th className="text-left px-4 py-3 text-muted font-medium">{t('colCritical')}</th>
                      <th className="text-left px-4 py-3 text-muted font-medium">{t('colCooldown')}</th>
                      <th className="text-left px-4 py-3 text-muted font-medium">{t('colStatus')}</th>
                      <th className="text-left px-4 py-3 text-muted font-medium">{t('colActions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge/50">
                    {data.rules.map((rule) => (
                      <RuleRow
                        key={rule.id}
                        rule={rule}
                        isEditing={editingRuleId === rule.id}
                        editValues={editValues}
                        setEditValues={setEditValues}
                        onEdit={() => startEdit(rule)}
                        onSave={() => saveEdit(rule.id)}
                        onCancel={cancelEdit}
                        isSaving={updateMutation.isPending && editingRuleId === rule.id}
                        t={t}
                      />
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            {/* Alert History */}
            <section>
              <h2 className="text-lg font-medium text-heading mb-3">{t('historyTitle')}</h2>
              {data.recentAlerts.length === 0 ? (
                <div className="flex items-center justify-center h-24 border border-edge rounded-lg text-faded text-sm">
                  {t('noHistory')}
                </div>
              ) : (
                <div className="border border-edge rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-elevated border-b border-edge">
                      <tr>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colTriggered')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colRule')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colSeverity')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colValue')}</th>
                        <th className="text-left px-4 py-3 text-muted font-medium">{t('colAlertStatus')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge/50">
                      {data.recentAlerts.map((alert) => (
                        <AlertHistoryRow key={alert.id} alert={alert} />
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>
          </>
        )}
      </div>
    </PageContainer>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function SummaryCard({
  label,
  value,
  color,
}: {
  label: string;
  value: number;
  color: 'critical' | 'success' | 'body' | 'accent';
}) {
  const colorMap = {
    critical: 'text-critical',
    success: 'text-success',
    body: 'text-body',
    accent: 'text-accent',
  };
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}

function RuleRow({
  rule,
  isEditing,
  editValues,
  setEditValues,
  onEdit,
  onSave,
  onCancel,
  isSaving,
  t,
}: {
  rule: PlatformAlertRule;
  isEditing: boolean;
  editValues: Record<string, string>;
  setEditValues: (v: Record<string, string>) => void;
  onEdit: () => void;
  onSave: () => void;
  onCancel: () => void;
  isSaving: boolean;
  t: (k: string) => string;
}) {
  if (isEditing) {
    return (
      <tr className="bg-accent/5">
        <td className="px-4 py-3">
          <div className="font-medium text-heading">{rule.name}</div>
          <div className="text-xs text-muted">{rule.description}</div>
        </td>
        <td className="px-4 py-3">
          <span className="inline-flex items-center gap-1">
            <TextField
              type="number"
              size="sm"
              value={editValues.warning ?? ''}
              onChange={(e) => setEditValues({ ...editValues, warning: e.target.value })}
              className="w-20"
              aria-label={t('colWarning')}
            />
            <span className="text-xs text-muted">{rule.unit}</span>
          </span>
        </td>
        <td className="px-4 py-3">
          <span className="inline-flex items-center gap-1">
            <TextField
              type="number"
              size="sm"
              value={editValues.critical ?? ''}
              onChange={(e) => setEditValues({ ...editValues, critical: e.target.value })}
              className="w-20"
              aria-label={t('colCritical')}
            />
            <span className="text-xs text-muted">{rule.unit}</span>
          </span>
        </td>
        <td className="px-4 py-3">
          <span className="inline-flex items-center gap-1">
            <TextField
              type="number"
              size="sm"
              value={editValues.cooldown ?? ''}
              onChange={(e) => setEditValues({ ...editValues, cooldown: e.target.value })}
              className="w-16"
              aria-label={t('colCooldown')}
            />
            <span className="text-xs text-muted">{t('minutes')}</span>
          </span>
        </td>
        <td className="px-4 py-3">
          <Select
            size="sm"
            value={editValues.enabled ?? 'true'}
            onChange={(e) => setEditValues({ ...editValues, enabled: e.target.value })}
            aria-label={t('colStatus')}
            options={[
              { value: 'true', label: t('enabled') },
              { value: 'false', label: t('disabled') },
            ]}
          />
        </td>
        <td className="px-4 py-3 flex gap-2">
          <button
            onClick={onSave}
            disabled={isSaving}
            className="px-3 py-1 bg-accent text-white text-xs rounded hover:bg-accent/90 disabled:opacity-50"
          >
            {isSaving ? t('saving') : t('save')}
          </button>
          <button
            onClick={onCancel}
            className="px-3 py-1 bg-card border border-edge text-xs rounded hover:bg-elevated text-muted"
          >
            {t('cancel')}
          </button>
        </td>
      </tr>
    );
  }

  return (
    <tr className="hover:bg-elevated">
      <td className="px-4 py-3">
        <div className="font-medium text-heading">{rule.name}</div>
        <div className="text-xs text-muted">{rule.description}</div>
      </td>
      <td className="px-4 py-3 text-warning font-medium">
        {rule.warningThreshold} {rule.unit}
      </td>
      <td className="px-4 py-3 text-critical font-medium">
        {rule.criticalThreshold} {rule.unit}
      </td>
      <td className="px-4 py-3 text-muted">{rule.cooldownMinutes} {t('minutes')}</td>
      <td className="px-4 py-3">
        {rule.enabled ? (
          <span className="flex items-center gap-1 text-success text-xs">
            <Bell size={12} /> {t('enabled')}
          </span>
        ) : (
          <span className="flex items-center gap-1 text-faded text-xs">
            <BellOff size={12} /> {t('disabled')}
          </span>
        )}
      </td>
      <td className="px-4 py-3">
        <button
          onClick={onEdit}
          className="px-3 py-1 text-xs text-accent border border-accent/20 rounded hover:bg-accent/10"
        >
          {t('edit')}
        </button>
      </td>
    </tr>
  );
}

function severityIcon(severity: PlatformAlertSeverity) {
  return severity === 'Critical' ? (
    <AlertCircle size={14} className="text-critical" />
  ) : (
    <AlertTriangle size={14} className="text-warning" />
  );
}

function statusBadge(status: PlatformAlertStatus) {
  const map: Record<PlatformAlertStatus, string> = {
    Active: 'bg-critical/10 text-critical',
    Resolved: 'bg-success/10 text-success',
    Suppressed: 'bg-elevated text-muted',
  };
  return (
    <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${map[status]}`}>
      {status}
    </span>
  );
}

function AlertHistoryRow({
  alert,
}: {
  alert: PlatformAlertHistoryEntry;
}) {
  const triggeredDate = new Date(alert.triggeredAt).toLocaleString();
  return (
    <tr className="hover:bg-elevated">
      <td className="px-4 py-3 text-muted text-xs">{triggeredDate}</td>
      <td className="px-4 py-3 font-medium text-heading">{alert.ruleName}</td>
      <td className="px-4 py-3">
        <span className="flex items-center gap-1">
          {severityIcon(alert.severity)}
          {alert.severity}
        </span>
      </td>
      <td className="px-4 py-3 text-body">
        {alert.value} {alert.unit}
      </td>
      <td className="px-4 py-3">{statusBadge(alert.status)}</td>
    </tr>
  );
}
