import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ShieldCheck,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CheckCircle2,
  ThumbsDown,
  BarChart2,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { TextField } from '../../../components/TextField';
import { Checkbox } from '../../../components/Checkbox';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<ShieldCheck size={24} className="text-accent" />}
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
            {/* Hallucination Alert Banner */}
            {hasHighHallucination ? (
              <div className="flex items-start gap-3 p-4 bg-warning/10 border border-warning/20 rounded-lg">
                <AlertTriangle size={18} className="text-warning mt-0.5 shrink-0" />
                <p className="text-sm text-warning">{t('highHallucinationWarning')}</p>
              </div>
            ) : (
              <div className="flex items-start gap-3 p-4 bg-success/10 border border-success/20 rounded-lg">
                <CheckCircle2 size={18} className="text-success mt-0.5 shrink-0" />
                <p className="text-sm text-success">{t('qualityOkMsg')}</p>
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
                <BarChart2 size={18} className="text-accent" />
                <h2 className="text-lg font-medium text-heading">{t('modelQualityTitle')}</h2>
              </div>
              {data.modelStats.length === 0 ? (
                <p className="text-sm text-muted italic">{t('noModelData')}</p>
              ) : (
                <div className="border border-edge rounded-lg overflow-hidden">
                  <table className="w-full text-sm">
                    <thead className="bg-elevated border-b border-edge">
                      <tr>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colModel')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colTotal')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colGood')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colLowConf')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colHallucination')}
                        </th>
                        <th className="text-left px-4 py-3 text-muted font-medium">
                          {t('colFeedback')}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge/50">
                      {data.modelStats.map((m) => (
                        <tr key={m.modelName} className="hover:bg-elevated">
                          <td className="px-4 py-3 font-medium text-heading">{m.modelName}</td>
                          <td className="px-4 py-3 text-muted">{m.totalResponses}</td>
                          <td className="px-4 py-3">
                            <span className="text-success font-medium">
                              {m.goodPercent.toFixed(1)}%
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span className="text-warning font-medium">
                              {m.lowConfidencePercent.toFixed(1)}%
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span
                              className={
                                m.hallucinationPercent > 10
                                  ? 'text-critical font-medium'
                                  : 'text-muted'
                              }
                            >
                              {m.hallucinationPercent.toFixed(1)}%
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <div className="flex items-center gap-1 text-critical">
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
                <h2 className="text-lg font-medium text-heading mb-3">
                  {t('topPatternsTitle')}
                </h2>
                <ul className="border border-edge rounded-lg divide-y divide-edge/50">
                  {data.topHallucinationPatterns.map((pattern, idx) => (
                    <li key={idx} className="flex items-center gap-3 px-4 py-3">
                      <span className="text-xs text-faded font-mono w-5">#{idx + 1}</span>
                      <span className="text-sm text-body">{pattern}</span>
                    </li>
                  ))}
                </ul>
              </section>
            )}

            {/* Config Section */}
            <section>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-medium text-heading">{t('configTitle')}</h2>
                {!editing && (
                  <Button variant="outline" onClick={startEdit}>
                    {t('editConfig')}
                  </Button>
                )}
              </div>

              {editing ? (
                <div className="border border-edge rounded-lg p-5 space-y-5">
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
                    <Button
                      variant="primary"
                      onClick={saveConfig}
                      disabled={configMutation.isPending}
                    >
                      {configMutation.isPending ? t('saving') : t('save')}
                    </Button>
                    <Button variant="ghost" onClick={() => setEditing(false)}>
                      {t('cancel')}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="border border-edge rounded-lg divide-y divide-edge/50">
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

            <p className="text-xs text-faded italic">{data.simulatedNote}</p>
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
  value: string;
  color: 'indigo' | 'emerald' | 'red' | 'slate';
}) {
  const colorMap = {
    indigo: 'text-accent',
    emerald: 'text-success',
    red: 'text-critical',
    slate: 'text-body',
  };
  return (
    <div className="border border-edge rounded-lg p-4 bg-card">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-muted w-56 shrink-0">{label}</span>
      <span className="text-sm font-medium text-heading">{value}</span>
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
      <label className="block text-sm font-medium text-body">{label}</label>
      <TextField
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        aria-label={label}
      />
      <p className="text-xs text-muted">{hint}</p>
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
    <Checkbox
      label={label}
      description={hint}
      checked={checked}
      onChange={(e) => onChange(e.target.checked)}
    />
  );
}
