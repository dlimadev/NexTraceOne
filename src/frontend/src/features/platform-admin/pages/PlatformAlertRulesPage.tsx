import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Bell,
  BellOff,
  CheckCircle,
  XCircle,
  AlertTriangle,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
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
              color={data.activeAlertCount > 0 ? 'red' : 'emerald'}
            />
            <SummaryCard
              label={t('totalRules')}
              value={data.rules.length}
              color="slate"
            />
            <SummaryCard
              label={t('enabledRules')}
              value={data.rules.filter((r) => r.enabled).length}
              color="indigo"
            />
          </div>

          {/* Rules Table */}
          <section>
            <h2 className="text-lg font-medium text-slate-800 mb-3">{t('rulesTitle')}</h2>
            <div className="border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colRule')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colWarning')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colCritical')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colCooldown')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colStatus')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colActions')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
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
            <h2 className="text-lg font-medium text-slate-800 mb-3">{t('historyTitle')}</h2>
            {data.recentAlerts.length === 0 ? (
              <div className="flex items-center justify-center h-24 border border-slate-200 rounded-lg text-slate-400 text-sm">
                {t('noHistory')}
              </div>
            ) : (
              <div className="border border-slate-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colTriggered')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colRule')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colSeverity')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colValue')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colAlertStatus')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {data.recentAlerts.map((alert) => (
                      <AlertHistoryRow key={alert.id} alert={alert} t={t} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
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
  color: 'red' | 'emerald' | 'slate' | 'indigo';
}) {
  const colorMap = {
    red: 'text-red-600',
    emerald: 'text-emerald-600',
    slate: 'text-slate-700',
    indigo: 'text-indigo-600',
  };
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
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
      <tr className="bg-indigo-50">
        <td className="px-4 py-3">
          <div className="font-medium text-slate-800">{rule.name}</div>
          <div className="text-xs text-slate-500">{rule.description}</div>
        </td>
        <td className="px-4 py-3">
          <input
            type="number"
            value={editValues.warning ?? ''}
            onChange={(e) => setEditValues({ ...editValues, warning: e.target.value })}
            className="w-20 px-2 py-1 border border-slate-300 rounded text-sm"
            aria-label={t('colWarning')}
          />
          <span className="ml-1 text-xs text-slate-500">{rule.unit}</span>
        </td>
        <td className="px-4 py-3">
          <input
            type="number"
            value={editValues.critical ?? ''}
            onChange={(e) => setEditValues({ ...editValues, critical: e.target.value })}
            className="w-20 px-2 py-1 border border-slate-300 rounded text-sm"
            aria-label={t('colCritical')}
          />
          <span className="ml-1 text-xs text-slate-500">{rule.unit}</span>
        </td>
        <td className="px-4 py-3">
          <input
            type="number"
            value={editValues.cooldown ?? ''}
            onChange={(e) => setEditValues({ ...editValues, cooldown: e.target.value })}
            className="w-16 px-2 py-1 border border-slate-300 rounded text-sm"
            aria-label={t('colCooldown')}
          />
          <span className="ml-1 text-xs text-slate-500">{t('minutes')}</span>
        </td>
        <td className="px-4 py-3">
          <select
            value={editValues.enabled ?? 'true'}
            onChange={(e) => setEditValues({ ...editValues, enabled: e.target.value })}
            className="px-2 py-1 border border-slate-300 rounded text-sm"
            aria-label={t('colStatus')}
          >
            <option value="true">{t('enabled')}</option>
            <option value="false">{t('disabled')}</option>
          </select>
        </td>
        <td className="px-4 py-3 flex gap-2">
          <button
            onClick={onSave}
            disabled={isSaving}
            className="px-3 py-1 bg-indigo-600 text-white text-xs rounded hover:bg-indigo-700 disabled:opacity-50"
          >
            {isSaving ? t('saving') : t('save')}
          </button>
          <button
            onClick={onCancel}
            className="px-3 py-1 bg-white border border-slate-300 text-xs rounded hover:bg-slate-50"
          >
            {t('cancel')}
          </button>
        </td>
      </tr>
    );
  }

  return (
    <tr className="hover:bg-slate-50">
      <td className="px-4 py-3">
        <div className="font-medium text-slate-800">{rule.name}</div>
        <div className="text-xs text-slate-500">{rule.description}</div>
      </td>
      <td className="px-4 py-3 text-amber-600 font-medium">
        {rule.warningThreshold} {rule.unit}
      </td>
      <td className="px-4 py-3 text-red-600 font-medium">
        {rule.criticalThreshold} {rule.unit}
      </td>
      <td className="px-4 py-3 text-slate-600">{rule.cooldownMinutes} {t('minutes')}</td>
      <td className="px-4 py-3">
        {rule.enabled ? (
          <span className="flex items-center gap-1 text-emerald-600 text-xs">
            <Bell size={12} /> {t('enabled')}
          </span>
        ) : (
          <span className="flex items-center gap-1 text-slate-400 text-xs">
            <BellOff size={12} /> {t('disabled')}
          </span>
        )}
      </td>
      <td className="px-4 py-3">
        <button
          onClick={onEdit}
          className="px-3 py-1 text-xs text-indigo-600 border border-indigo-200 rounded hover:bg-indigo-50"
        >
          {t('edit')}
        </button>
      </td>
    </tr>
  );
}

function severityIcon(severity: PlatformAlertSeverity) {
  return severity === 'Critical' ? (
    <AlertCircle size={14} className="text-red-500" />
  ) : (
    <AlertTriangle size={14} className="text-amber-500" />
  );
}

function statusBadge(status: PlatformAlertStatus) {
  const map: Record<PlatformAlertStatus, string> = {
    Active: 'bg-red-100 text-red-700',
    Resolved: 'bg-emerald-100 text-emerald-700',
    Suppressed: 'bg-slate-100 text-slate-600',
  };
  return (
    <span className={`px-2 py-0.5 text-xs rounded-full font-medium ${map[status]}`}>
      {status}
    </span>
  );
}

function AlertHistoryRow({
  alert,
  t,
}: {
  alert: PlatformAlertHistoryEntry;
  t: (k: string) => string;
}) {
  const triggeredDate = new Date(alert.triggeredAt).toLocaleString();
  return (
    <tr className="hover:bg-slate-50">
      <td className="px-4 py-3 text-slate-600 text-xs">{triggeredDate}</td>
      <td className="px-4 py-3 font-medium text-slate-800">{alert.ruleName}</td>
      <td className="px-4 py-3">
        <span className="flex items-center gap-1">
          {severityIcon(alert.severity)}
          {alert.severity}
        </span>
      </td>
      <td className="px-4 py-3 text-slate-700">
        {alert.value} {alert.unit}
      </td>
      <td className="px-4 py-3">{statusBadge(alert.status)}</td>
    </tr>
  );
}
