import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Activity, CheckCircle, XCircle, RefreshCw, AlertTriangle } from 'lucide-react';
import { platformAdminApi, type ObservabilityMode } from '../api/platformAdmin';

const MODES: ObservabilityMode[] = ['Full', 'Lite', 'Minimal'];

export function ObservabilityModePage() {
  const { t } = useTranslation('observabilityMode');
  const queryClient = useQueryClient();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['observability-mode'],
    queryFn: platformAdminApi.getObservabilityMode,
  });

  const mutation = useMutation({
    mutationFn: (mode: ObservabilityMode) =>
      platformAdminApi.updateObservabilityMode({ mode }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['observability-mode'] });
    },
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Activity size={24} className="text-cyan-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
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
          {/* Current status */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatusCard
              label={t('currentMode')}
              value={t(`mode.${data.currentMode}`)}
              active={true}
            />
            <StatusCard
              label={t('elasticsearch')}
              value={data.elasticsearchConnected ? (data.elasticsearchVersion ?? t('connected')) : t('notConnected')}
              active={data.elasticsearchConnected}
            />
            <StatusCard
              label={t('otelCollector')}
              value={data.otelCollectorConnected ? t('connected') : t('notConnected')}
              active={data.otelCollectorConnected}
            />
            <StatusCard
              label={t('additionalRam')}
              value={`${data.additionalRamUsageGb} GB`}
              active={true}
            />
          </div>

          {/* Mode selector */}
          <section>
            <h2 className="text-lg font-medium text-slate-800 mb-3">{t('selectModeTitle')}</h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {MODES.map((mode) => {
                const isActive = data.currentMode === mode;
                return (
                  <button
                    key={mode}
                    onClick={() => mutation.mutate(mode)}
                    disabled={isActive || mutation.isPending}
                    className={`text-left p-5 border rounded-lg transition-all ${
                      isActive
                        ? 'border-cyan-500 bg-cyan-50 ring-2 ring-cyan-200'
                        : 'border-slate-200 hover:border-cyan-300 hover:bg-cyan-50/50'
                    } disabled:cursor-not-allowed`}
                    aria-label={`${t('selectMode')} ${t(`mode.${mode}`)}`}
                  >
                    <div className="flex items-center justify-between mb-2">
                      <span className="font-medium text-slate-800">{t(`mode.${mode}`)}</span>
                      {isActive && <CheckCircle size={16} className="text-cyan-600" />}
                    </div>
                    <p className="text-xs text-slate-500">{t(`modeDesc.${mode}`)}</p>
                    <p className="text-xs text-slate-400 mt-1">{t(`modeRam.${mode}`)}</p>
                  </button>
                );
              })}
            </div>
          </section>

          {/* Trade-offs */}
          {data.tradeOffs.length > 0 && (
            <section className="p-4 bg-amber-50 border border-amber-200 rounded-lg">
              <div className="flex items-start gap-2">
                <AlertTriangle size={16} className="text-amber-600 mt-0.5 shrink-0" />
                <div>
                  <p className="text-sm font-medium text-amber-900 mb-2">{t('tradeOffsTitle')}</p>
                  <ul className="space-y-1">
                    {data.tradeOffs.map((note, idx) => (
                      <li key={idx} className="text-xs text-amber-800">
                        • {note}
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </section>
          )}

          {mutation.isSuccess && (
            <div className="flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-lg p-3">
              <CheckCircle size={16} />
              {t('saveSuccess')}
            </div>
          )}

          <p className="text-xs text-slate-400 italic">{data.simulatedNote}</p>
        </>
      )}
    </div>
  );
}

function StatusCard({
  label,
  value,
  active,
}: {
  label: string;
  value: string;
  active: boolean;
}) {
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-sm font-semibold mt-1 ${active ? 'text-cyan-600' : 'text-slate-400'}`}>
        {value}
      </p>
    </div>
  );
}
