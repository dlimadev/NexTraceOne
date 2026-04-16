import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  AlertTriangle,
  TrendingDown,
  Trash2,
  XCircle,
  CheckCircle,
  Server,
  Clock,
} from 'lucide-react';
import { finOpsApi, type WasteSignalDetail } from '../api/finOps';

export function WasteDetectionPage() {
  const { t } = useTranslation('wasteDetection');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['waste-signals'],
    queryFn: () => finOpsApi.getWasteSignals(),
  });

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
          className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
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
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-white border border-slate-200 rounded-lg p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wide">{t('totalWaste')}</p>
              <p className="text-2xl font-bold text-amber-600 mt-1">
                {Number(data.totalWaste).toLocaleString(undefined, { maximumFractionDigits: 2 })}
              </p>
            </div>
            <div className="bg-white border border-slate-200 rounded-lg p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wide">{t('signalCount')}</p>
              <p className="text-2xl font-bold text-slate-800 mt-1">{data.signalCount}</p>
            </div>
            <div className="bg-white border border-slate-200 rounded-lg p-4">
              <p className="text-xs text-slate-500 uppercase tracking-wide">{t('byTypeTitle')}</p>
              <div className="mt-2 space-y-1">
                {data.byType.slice(0, 3).map((bt) => (
                  <div key={bt.type} className="flex items-center justify-between text-xs">
                    <span className="text-slate-600">{bt.type}</span>
                    <span className="font-medium text-slate-800">{bt.count}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Signals List */}
          {data.signals.length === 0 ? (
            <div className="flex items-center gap-3 p-4 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-700 text-sm">
              <CheckCircle size={18} />
              {t('noWaste')}
            </div>
          ) : (
            <section>
              <h2 className="text-base font-medium text-slate-800 mb-3">{t('signalsTitle')}</h2>
              <div className="space-y-3">
                {data.signals.map((s) => (
                  <WasteSignalCard key={s.signalId} signal={s} t={t} />
                ))}
              </div>
            </section>
          )}

          {data.isSimulated && (
            <p className="text-xs text-slate-400 italic">{t('simulatedNote')}</p>
          )}

          <p className="text-xs text-slate-400">
            {t('generatedAt')}: {new Date(data.generatedAt).toLocaleString()}
          </p>
        </>
      )}
    </div>
  );
}

function WasteSignalCard({
  signal,
  t,
}: {
  signal: WasteSignalDetail;
  t: (key: string) => string;
}) {
  const severityConfig = {
    Critical: { cls: 'border-red-300 bg-red-50', badge: 'bg-red-100 text-red-700' },
    High:     { cls: 'border-orange-300 bg-orange-50', badge: 'bg-orange-100 text-orange-700' },
    Medium:   { cls: 'border-amber-300 bg-amber-50', badge: 'bg-amber-100 text-amber-700' },
    Low:      { cls: 'border-slate-200 bg-white', badge: 'bg-slate-100 text-slate-600' },
  }[signal.severity] ?? { cls: 'border-slate-200 bg-white', badge: 'bg-slate-100 text-slate-600' };

  const typeIcon = signal.type.includes('Idle')
    ? <Clock size={14} className="text-slate-400" />
    : signal.type.includes('Cpu') || signal.type.includes('Memory')
    ? <Server size={14} className="text-slate-400" />
    : <Trash2 size={14} className="text-slate-400" />;

  return (
    <div className={`border rounded-lg p-4 ${severityConfig.cls}`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className="mt-0.5">
            <AlertTriangle size={16} className="text-amber-500" />
          </div>
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <span className="font-medium text-sm text-slate-800">{signal.serviceName}</span>
              <span className={`inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full font-medium ${severityConfig.badge}`}>
                {typeIcon}
                {signal.type}
              </span>
              <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${severityConfig.badge}`}>
                {signal.severity}
              </span>
            </div>
            <p className="text-xs text-slate-600 mt-1">{signal.description}</p>
            <p className="text-xs text-slate-400 mt-1">{signal.pattern}</p>
            {signal.correlatedCause && (
              <p className="text-xs text-slate-500 mt-1">
                {t('correlatedCause')}: {signal.correlatedCause}
              </p>
            )}
            <div className="flex items-center gap-3 mt-2 text-xs text-slate-400">
              <span>{t('team')}: {signal.team}</span>
              <span>·</span>
              <span>{t('domain')}: {signal.domain}</span>
            </div>
          </div>
        </div>
        <div className="text-right flex-shrink-0">
          <div className="flex items-center gap-1 text-amber-600 text-sm font-semibold">
            <TrendingDown size={14} />
            {Number(signal.estimatedWaste).toLocaleString(undefined, { maximumFractionDigits: 2 })}
          </div>
          <p className="text-xs text-slate-400">{t('estimatedWaste')}</p>
        </div>
      </div>
    </div>
  );
}
