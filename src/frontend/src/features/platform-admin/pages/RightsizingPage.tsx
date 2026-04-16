import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Sliders, RefreshCw, XCircle, CheckCircle, AlertTriangle, TrendingDown } from 'lucide-react';
import { platformAdminApi, type RightsizingRecommendation } from '../api/platformAdmin';

export function RightsizingPage() {
  const { t } = useTranslation('rightsizing');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['rightsizing-report'],
    queryFn: platformAdminApi.getRightsizingReport,
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Sliders size={24} className="text-violet-600" />
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
          {/* Summary */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <SummaryCard
              label={t('statServices')}
              value={String(data.totalServicesAnalysed)}
              color="violet"
            />
            <SummaryCard
              label={t('statRecs')}
              value={String(data.recommendations.length)}
              color="amber"
            />
            <SummaryCard
              label={t('statCpuSaving')}
              value={`${data.totalSavingEstimateCpuPercent.toFixed(0)}%`}
              color="emerald"
            />
            <SummaryCard
              label={t('statMemSaving')}
              value={`${data.totalSavingEstimateMemoryPercent.toFixed(0)}%`}
              color="indigo"
            />
          </div>

          {/* Recommendations table */}
          {data.recommendations.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-40 text-slate-400 gap-2">
              <CheckCircle size={32} className="text-emerald-400" />
              <p className="text-sm">{t('noRecs')}</p>
            </div>
          ) : (
            <section>
              <h2 className="text-lg font-medium text-slate-800 mb-3">{t('recsTitle')}</h2>
              <div className="border border-slate-200 rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colService')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colResource')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colCurrent')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colRecommended')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colSaving')}</th>
                      <th className="text-left px-4 py-3 text-slate-600 font-medium">{t('colImpact')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {data.recommendations.map((rec) => (
                      <RecommendationRow key={`${rec.serviceId}-${rec.resource}`} rec={rec} t={t} />
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          <p className="text-xs text-slate-400 italic">
            {t('safetyMarginNote', { percent: data.safetyMarginPercent })} · {data.simulatedNote}
          </p>
        </>
      )}
    </div>
  );
}

function RecommendationRow({
  rec,
  t,
}: {
  rec: RightsizingRecommendation;
  t: (key: string) => string;
}) {
  const impactColor = {
    Low: 'text-emerald-600 bg-emerald-50 border-emerald-200',
    Medium: 'text-amber-600 bg-amber-50 border-amber-200',
    High: 'text-red-600 bg-red-50 border-red-200',
  }[rec.reliabilityImpact];

  return (
    <tr className="hover:bg-slate-50">
      <td className="px-4 py-3">
        <p className="font-medium text-slate-800">{rec.serviceName}</p>
        <p className="text-xs text-slate-400">{rec.teamName}</p>
      </td>
      <td className="px-4 py-3">
        <span className="px-2 py-0.5 rounded text-xs font-medium bg-violet-50 text-violet-700 border border-violet-200">
          {rec.resource}
        </span>
      </td>
      <td className="px-4 py-3 text-slate-600">
        {rec.currentAllocation} {rec.unit}
      </td>
      <td className="px-4 py-3 font-medium text-slate-800">
        {rec.recommendedAllocation} {rec.unit}
      </td>
      <td className="px-4 py-3">
        <span className="flex items-center gap-1 text-emerald-600 text-xs font-medium">
          <TrendingDown size={12} />
          {rec.savingPercent.toFixed(0)}%
        </span>
      </td>
      <td className="px-4 py-3">
        <span className={`px-2 py-0.5 rounded text-xs font-medium border ${impactColor}`}>
          {rec.sloAtRisk ? <AlertTriangle size={10} className="inline mr-1" /> : null}
          {t(`impact.${rec.reliabilityImpact}`)}
        </span>
      </td>
    </tr>
  );
}

function SummaryCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: 'violet' | 'amber' | 'emerald' | 'indigo';
}) {
  const colorMap = {
    violet: 'text-violet-600',
    amber: 'text-amber-600',
    emerald: 'text-emerald-600',
    indigo: 'text-indigo-600',
  };
  return (
    <div className="border border-slate-200 rounded-lg p-4 bg-white">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
    </div>
  );
}
