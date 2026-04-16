import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Leaf,
  RefreshCw,
  XCircle,
  TrendingUp,
  TrendingDown,
  Minus,
  Target,
  AlertTriangle,
  CheckCircle,
} from 'lucide-react';
import { platformAdminApi, type GreenOpsConfigUpdate } from '../api/platformAdmin';

export function GreenOpsPage() {
  const { t } = useTranslation('greenOps');
  const queryClient = useQueryClient();
  const [editingConfig, setEditingConfig] = useState(false);
  const [configValues, setConfigValues] = useState({
    intensityFactor: '',
    esgTarget: '',
    datacenterRegion: '',
  });

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['greenops-report'],
    queryFn: platformAdminApi.getGreenOpsReport,
  });

  const configMutation = useMutation({
    mutationFn: (update: GreenOpsConfigUpdate) =>
      platformAdminApi.updateGreenOpsConfig(update),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['greenops-report'] });
      setEditingConfig(false);
    },
  });

  function startEditConfig() {
    if (!data) return;
    setConfigValues({
      intensityFactor: String(data.config.intensityFactorKgPerKwh),
      esgTarget: String(data.config.esgTargetKgCo2PerMonth),
      datacenterRegion: data.config.datacenterRegion,
    });
    setEditingConfig(true);
  }

  function saveConfig() {
    configMutation.mutate({
      intensityFactorKgPerKwh: parseFloat(configValues.intensityFactor),
      esgTargetKgCo2PerMonth: parseFloat(configValues.esgTarget),
      datacenterRegion: configValues.datacenterRegion,
    });
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Leaf size={24} className="text-emerald-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 transition-colors"
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
          {/* ESG Target Banner */}
          {data.percentAboveTarget > 0 ? (
            <div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg">
              <AlertTriangle size={18} className="text-amber-600 mt-0.5 shrink-0" />
              <p className="text-sm text-amber-800">
                {t('aboveTargetMsg', {
                  percent: data.percentAboveTarget.toFixed(1),
                  target: data.esgTargetKgCo2,
                })}
              </p>
            </div>
          ) : (
            <div className="flex items-start gap-3 p-4 bg-emerald-50 border border-emerald-200 rounded-lg">
              <CheckCircle size={18} className="text-emerald-600 mt-0.5 shrink-0" />
              <p className="text-sm text-emerald-800">{t('belowTargetMsg')}</p>
            </div>
          )}

          {/* Summary Cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <SummaryCard
              label={t('totalEmissions')}
              value={`${data.totalKgCo2.toFixed(1)} kgCO₂`}
              color="slate"
            />
            <SummaryCard
              label={t('esgTarget')}
              value={`${data.esgTargetKgCo2} kgCO₂`}
              color="emerald"
            />
            <SummaryCard
              label={t('equivalent')}
              value={`${data.equivalentKmByCar.toFixed(0)} km`}
              subtitle={t('byCar')}
              color="amber"
            />
            <SummaryCard
              label={t('intensityFactor')}
              value={`${data.config.intensityFactorKgPerKwh} kgCO₂/kWh`}
              subtitle={data.config.datacenterRegion}
              color="indigo"
            />
          </div>

          {/* Top Services */}
          <section>
            <h2 className="text-lg font-medium text-slate-800 mb-3">{t('topServicesTitle')}</h2>
            <div className="border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colService')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colTeam')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colEmissions')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colTrend')}</th>
                    <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colBar')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.topServices.map((svc, idx) => {
                    const maxCo2 = Math.max(...data.topServices.map((s) => s.carbonKgCo2));
                    const barWidth = maxCo2 > 0 ? (svc.carbonKgCo2 / maxCo2) * 100 : 0;
                    return (
                      <tr key={svc.serviceId} className="hover:bg-slate-50">
                        <td className="px-4 py-3">
                          <span className="text-xs text-slate-400 mr-2">#{idx + 1}</span>
                          <span className="font-medium text-slate-800">{svc.serviceName}</span>
                        </td>
                        <td className="px-4 py-3 text-slate-600">{svc.teamName}</td>
                        <td className="px-4 py-3 font-medium text-slate-800">
                          {svc.carbonKgCo2.toFixed(1)} kgCO₂
                        </td>
                        <td className="px-4 py-3">
                          <TrendBadge change={svc.changePercent} />
                        </td>
                        <td className="px-4 py-3 w-40">
                          <div className="h-2 bg-slate-100 rounded-full overflow-hidden">
                            <div
                              className="h-full bg-emerald-500 rounded-full"
                              style={{ width: `${barWidth}%` }}
                            />
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </section>

          {/* Monthly Trend */}
          {data.trend.length > 0 && (
            <section>
              <h2 className="text-lg font-medium text-slate-800 mb-3">{t('trendTitle')}</h2>
              <div className="border border-slate-200 rounded-lg p-4">
                <div className="flex items-end gap-2 h-24">
                  {data.trend.map((point) => {
                    const maxCo2 = Math.max(...data.trend.map((p) => p.totalKgCo2));
                    const barHeight = maxCo2 > 0 ? (point.totalKgCo2 / maxCo2) * 100 : 0;
                    return (
                      <div key={point.month} className="flex-1 flex flex-col items-center gap-1">
                        <span className="text-xs text-slate-500">{point.totalKgCo2.toFixed(0)}</span>
                        <div
                          className="w-full bg-emerald-400 rounded-t"
                          style={{ height: `${barHeight}%` }}
                          title={`${point.month}: ${point.totalKgCo2.toFixed(1)} kgCO₂`}
                        />
                        <span className="text-xs text-slate-400">{point.month.slice(0, 3)}</span>
                      </div>
                    );
                  })}
                  {/* ESG Target line indicator */}
                  <div className="flex items-center gap-1 ml-2">
                    <Target size={12} className="text-amber-500" />
                    <span className="text-xs text-amber-600">{t('esgTarget')}</span>
                  </div>
                </div>
              </div>
            </section>
          )}

          {/* Config */}
          <section>
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-lg font-medium text-slate-800">{t('configTitle')}</h2>
              {!editingConfig && (
                <button
                  onClick={startEditConfig}
                  className="px-3 py-1.5 text-sm text-indigo-600 border border-indigo-200 rounded hover:bg-indigo-50"
                >
                  {t('editConfig')}
                </button>
              )}
            </div>

            {editingConfig ? (
              <div className="border border-slate-200 rounded-lg p-4 space-y-4">
                <ConfigField
                  label={t('intensityFactorLabel')}
                  hint={t('intensityFactorHint')}
                  value={configValues.intensityFactor}
                  onChange={(v) => setConfigValues((c) => ({ ...c, intensityFactor: v }))}
                  type="number"
                />
                <ConfigField
                  label={t('esgTargetLabel')}
                  hint={t('esgTargetHint')}
                  value={configValues.esgTarget}
                  onChange={(v) => setConfigValues((c) => ({ ...c, esgTarget: v }))}
                  type="number"
                />
                <ConfigField
                  label={t('datacenterRegionLabel')}
                  hint={t('datacenterRegionHint')}
                  value={configValues.datacenterRegion}
                  onChange={(v) => setConfigValues((c) => ({ ...c, datacenterRegion: v }))}
                  type="text"
                />
                <div className="flex gap-3">
                  <button
                    onClick={saveConfig}
                    disabled={configMutation.isPending}
                    className="px-4 py-2 bg-emerald-600 text-white text-sm rounded-lg hover:bg-emerald-700 disabled:opacity-50"
                  >
                    {configMutation.isPending ? t('saving') : t('save')}
                  </button>
                  <button
                    onClick={() => setEditingConfig(false)}
                    className="px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
                  >
                    {t('cancel')}
                  </button>
                </div>
              </div>
            ) : (
              <div className="border border-slate-200 rounded-lg divide-y divide-slate-100">
                <ConfigRow label={t('intensityFactorLabel')} value={`${data.config.intensityFactorKgPerKwh} kgCO₂/kWh`} />
                <ConfigRow label={t('esgTargetLabel')} value={`${data.config.esgTargetKgCo2PerMonth} kgCO₂/month`} />
                <ConfigRow label={t('datacenterRegionLabel')} value={data.config.datacenterRegion} />
              </div>
            )}
          </section>

          {/* Simulated note */}
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
  subtitle,
  color,
}: {
  label: string;
  value: string;
  subtitle?: string;
  color: 'slate' | 'emerald' | 'amber' | 'indigo';
}) {
  const colorMap = {
    slate: 'text-slate-700',
    emerald: 'text-emerald-600',
    amber: 'text-amber-600',
    indigo: 'text-indigo-600',
  };
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-400 mt-0.5">{subtitle}</p>}
    </div>
  );
}

function TrendBadge({ change }: { change: number }) {
  if (change > 0) {
    return (
      <span className="flex items-center gap-1 text-red-600 text-xs font-medium">
        <TrendingUp size={12} />+{change.toFixed(1)}%
      </span>
    );
  }
  if (change < 0) {
    return (
      <span className="flex items-center gap-1 text-emerald-600 text-xs font-medium">
        <TrendingDown size={12} />{change.toFixed(1)}%
      </span>
    );
  }
  return (
    <span className="flex items-center gap-1 text-slate-400 text-xs">
      <Minus size={12} />0%
    </span>
  );
}

function ConfigRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex px-4 py-3 gap-4">
      <span className="text-sm text-slate-500 w-48 shrink-0">{label}</span>
      <span className="text-sm font-medium text-slate-800">{value}</span>
    </div>
  );
}

function ConfigField({
  label,
  hint,
  value,
  onChange,
  type,
}: {
  label: string;
  hint: string;
  value: string;
  onChange: (v: string) => void;
  type: 'text' | 'number';
}) {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-slate-700">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-emerald-500 focus:border-emerald-500"
        aria-label={label}
      />
      <p className="text-xs text-slate-500">{hint}</p>
    </div>
  );
}
