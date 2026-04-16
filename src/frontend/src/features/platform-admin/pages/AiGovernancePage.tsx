import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ShieldCheck,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CheckCircle,
  ThumbsDown,
  BarChart2,
} from 'lucide-react';
import { platformAdminApi, type AiGovernanceConfigUpdate } from '../api/platformAdmin';

export function AiGovernancePage() {
  const { t } = useTranslation('aiGovernance');
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState({
    groundingCheckEnabled: true,
    hallucinationFlagThreshold: '',
    feedbackEnabled: true,
    autoSuspendOnHighHallucinationRate: false,
    highHallucinationThresholdPercent: '',
  });

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-governance-dashboard'],
    queryFn: platformAdminApi.getAiGovernanceDashboard,
  });

  const configMutation = useMutation({
    mutationFn: (payload: AiGovernanceConfigUpdate) =>
      platformAdminApi.updateAiGovernanceConfig(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-governance-dashboard'] });
      setEditing(false);
    },
  });

  function startEdit() {
    if (!data) return;
    const c = data.config;
    setForm({
      groundingCheckEnabled: c.groundingCheckEnabled,
      hallucinationFlagThreshold: String(c.hallucinationFlagThreshold),
      feedbackEnabled: c.feedbackEnabled,
      autoSuspendOnHighHallucinationRate: c.autoSuspendOnHighHallucinationRate,
      highHallucinationThresholdPercent: String(c.highHallucinationThresholdPercent),
    });
    setEditing(true);
  }

  function saveConfig() {
    configMutation.mutate({
      groundingCheckEnabled: form.groundingCheckEnabled,
      hallucinationFlagThreshold: parseFloat(form.hallucinationFlagThreshold),
      feedbackEnabled: form.feedbackEnabled,
      autoSuspendOnHighHallucinationRate: form.autoSuspendOnHighHallucinationRate,
      highHallucinationThresholdPercent: parseFloat(form.highHallucinationThresholdPercent),
    });
  }

  const hasHighHallucination =
    data && data.modelStats.some((m) => m.hallucinationPercent > (data.config.highHallucinationThresholdPercent ?? 20));

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ShieldCheck size={24} className="text-indigo-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
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
          {/* Hallucination Alert Banner */}
          {hasHighHallucination ? (
            <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg">
              <AlertTriangle size={18} className="text-amber-600 mt-0.5 shrink-0" />
              <p className="text-sm text-amber-800">{t('highHallucinationWarning')}</p>
            </div>
          ) : (
            <div className="flex items-start gap-3 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
              <CheckCircle size={18} className="text-emerald-600 mt-0.5 shrink-0" />
              <p className="text-sm text-emerald-800">{t('qualityOkMsg')}</p>
            </div>
          )}

          {/* Summary Cards */}
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <SummaryCard
              label={t('totalFeedback')}
              value={String(data.totalFeedbackCount)}
              color="indigo"
            />
            <SummaryCard
              label={t('negativeFeedbackRate')}
              value={`${data.negativeFeedbackPercent.toFixed(1)}%`}
              color={data.negativeFeedbackPercent > 20 ? 'red' : 'emerald'}
            />
            <SummaryCard
              label={t('modelsMonitored')}
              value={String(data.modelStats.length)}
              color="slate"
            />
          </div>

          {/* Model Quality Table */}
          <section>
            <div className="flex items-center gap-2 mb-3">
              <BarChart2 size={18} className="text-indigo-500" />
              <h2 className="text-lg font-medium text-slate-800">{t('modelQualityTitle')}</h2>
            </div>
            {data.modelStats.length === 0 ? (
              <p className="text-sm text-slate-500 italic">{t('noModelData')}</p>
            ) : (
              <div className="border border-slate-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colModel')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colTotal')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colGood')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colLowConf')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colHallucination')}
                      </th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">
                        {t('colFeedback')}
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {data.modelStats.map((m) => (
                      <tr key={m.modelName} className="hover:bg-slate-50">
                        <td className="px-4 py-3 font-medium text-slate-800">{m.modelName}</td>
                        <td className="px-4 py-3 text-slate-600">{m.totalResponses}</td>
                        <td className="px-4 py-3">
                          <span className="text-emerald-700 font-medium">
                            {m.goodPercent.toFixed(1)}%
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <span className="text-amber-700 font-medium">
                            {m.lowConfidencePercent.toFixed(1)}%
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={
                              m.hallucinationPercent > 10
                                ? 'text-red-700 font-medium'
                                : 'text-slate-600'
                            }
                          >
                            {m.hallucinationPercent.toFixed(1)}%
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-1 text-rose-600">
                            <ThumbsDown size={12} />
                            <span>{m.negativeFeedbackCount}</span>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          {/* Top Hallucination Patterns */}
          {data.topHallucinationPatterns.length > 0 && (
            <section>
              <h2 className="text-lg font-medium text-slate-800 mb-3">
                {t('topPatternsTitle')}
              </h2>
              <ul className="border border-slate-200 rounded-lg divide-y divide-slate-100">
                {data.topHallucinationPatterns.map((pattern, idx) => (
                  <li key={idx} className="flex items-center gap-3 px-4 py-3">
                    <span className="text-xs text-slate-400 font-mono w-5">#{idx + 1}</span>
                    <span className="text-sm text-slate-700">{pattern}</span>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {/* Config Section */}
          <section>
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-medium text-slate-800">{t('configTitle')}</h2>
              {!editing && (
                <button
                  onClick={startEdit}
                  className="px-3 py-1.5 text-sm text-indigo-600 border border-indigo-200 rounded hover:bg-indigo-50"
                >
                  {t('editConfig')}
                </button>
              )}
            </div>

            {editing ? (
              <div className="border border-slate-200 rounded-lg p-5 space-y-5">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <NumberField
                    label={t('hallucinationThresholdLabel')}
                    hint={t('hallucinationThresholdHint')}
                    value={form.hallucinationFlagThreshold}
                    onChange={(v) => setForm((f) => ({ ...f, hallucinationFlagThreshold: v }))}
                  />
                  <NumberField
                    label={t('autoSuspendThresholdLabel')}
                    hint={t('autoSuspendThresholdHint')}
                    value={form.highHallucinationThresholdPercent}
                    onChange={(v) =>
                      setForm((f) => ({ ...f, highHallucinationThresholdPercent: v }))
                    }
                  />
                </div>
                <div className="flex flex-col gap-3">
                  <ToggleField
                    label={t('groundingCheckLabel')}
                    hint={t('groundingCheckHint')}
                    checked={form.groundingCheckEnabled}
                    onChange={(v) => setForm((f) => ({ ...f, groundingCheckEnabled: v }))}
                  />
                  <ToggleField
                    label={t('feedbackEnabledLabel')}
                    hint={t('feedbackEnabledHint')}
                    checked={form.feedbackEnabled}
                    onChange={(v) => setForm((f) => ({ ...f, feedbackEnabled: v }))}
                  />
                  <ToggleField
                    label={t('autoSuspendLabel')}
                    hint={t('autoSuspendHint')}
                    checked={form.autoSuspendOnHighHallucinationRate}
                    onChange={(v) =>
                      setForm((f) => ({ ...f, autoSuspendOnHighHallucinationRate: v }))
                    }
                  />
                </div>
                <div className="flex gap-3 pt-2">
                  <button
                    onClick={saveConfig}
                    disabled={configMutation.isPending}
                    className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50"
                  >
                    {configMutation.isPending ? t('saving') : t('save')}
                  </button>
                  <button
                    onClick={() => setEditing(false)}
                    className="px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
                  >
                    {t('cancel')}
                  </button>
                </div>
              </div>
            ) : (
              <div className="border border-slate-200 rounded-lg divide-y divide-slate-100">
                <ConfigRow
                  label={t('groundingCheckLabel')}
                  value={data.config.groundingCheckEnabled ? t('enabled') : t('disabled')}
                />
                <ConfigRow
                  label={t('hallucinationThresholdLabel')}
                  value={String(data.config.hallucinationFlagThreshold)}
                />
                <ConfigRow
                  label={t('feedbackEnabledLabel')}
                  value={data.config.feedbackEnabled ? t('enabled') : t('disabled')}
                />
                <ConfigRow
                  label={t('autoSuspendLabel')}
                  value={
                    data.config.autoSuspendOnHighHallucinationRate ? t('enabled') : t('disabled')
                  }
                />
                <ConfigRow
                  label={t('autoSuspendThresholdLabel')}
                  value={`${data.config.highHallucinationThresholdPercent}%`}
                />
              </div>
            )}
          </section>

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
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
  value: string;
  color: 'indigo' | 'emerald' | 'red' | 'slate';
}) {
  const colorMap = {
    indigo: 'text-indigo-600',
    emerald: 'text-emerald-600',
    red: 'text-red-600',
    slate: 'text-slate-700',
  };
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-slate-500 w-56 shrink-0">{label}</span>
      <span className="text-sm font-medium text-slate-800">{value}</span>
    </div>
  );
}

function NumberField({
  label,
  hint,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
        className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500"
      />
      <p className="text-xs text-slate-500">{hint}</p>
    </div>
  );
}

function ToggleField({
  label,
  hint,
  checked,
  onChange,
}: {
  label: string;
  hint: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex items-start gap-3 cursor-pointer">
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        aria-label={label}
        className="mt-0.5 h-4 w-4 rounded border-slate-300 text-indigo-600"
      />
      <div>
        <span className="block text-sm font-medium text-slate-700">{label}</span>
        <span className="block text-xs text-slate-500">{hint}</span>
      </div>
    </label>
  );
}
