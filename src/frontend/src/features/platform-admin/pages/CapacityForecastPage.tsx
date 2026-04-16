import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, RefreshCw, AlertTriangle, CheckCircle } from 'lucide-react';
import { platformAdminApi, type CapacityResourceForecast } from '../api/platformAdmin';

export function CapacityForecastPage() {
  const { t } = useTranslation('capacityForecast');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['capacity-forecast'],
    queryFn: platformAdminApi.getCapacityForecast,
  });

  function riskBadgeClass(risk: CapacityResourceForecast['riskLevel']) {
    switch (risk) {
      case 'Critical': return 'bg-red-100 text-red-800';
      case 'High': return 'bg-orange-100 text-orange-800';
      case 'Medium': return 'bg-yellow-100 text-yellow-800';
      default: return 'bg-green-100 text-green-800';
    }
  }

  function riskIcon(risk: CapacityResourceForecast['riskLevel']) {
    if (risk === 'Critical' || risk === 'High') {
      return <AlertTriangle size={14} className="text-orange-500" />;
    }
    return <CheckCircle size={14} className="text-green-500" />;
  }

  function usageBarWidth(current: number, capacity: number) {
    const pct = Math.min((current / capacity) * 100, 100);
    return `${pct.toFixed(0)}%`;
  }

  function usageBarColor(risk: CapacityResourceForecast['riskLevel']) {
    switch (risk) {
      case 'Critical': return 'bg-red-500';
      case 'High': return 'bg-orange-500';
      case 'Medium': return 'bg-yellow-400';
      default: return 'bg-green-500';
    }
  }

  if (isLoading) return <div className="p-6 text-sm text-gray-500">{t('loading')}</div>;
  if (isError) return <div className="p-6 text-sm text-red-500">{t('error')}</div>;

  const criticalCount = data?.forecasts.filter((f) => f.riskLevel === 'Critical' || f.riskLevel === 'High').length ?? 0;

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <TrendingUp size={24} className="text-blue-600" />
          <div>
            <h1 className="text-xl font-semibold text-gray-900">{t('title')}</h1>
            <p className="text-sm text-gray-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-3 py-1.5 text-sm border rounded-md hover:bg-gray-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Summary banner */}
      {criticalCount > 0 ? (
        <div className="bg-orange-50 border border-orange-200 rounded-lg p-4 flex items-start gap-3">
          <AlertTriangle size={18} className="text-orange-500 mt-0.5 shrink-0" />
          <div>
            <div className="text-sm font-medium text-orange-800">
              {t('attentionNeeded', { count: criticalCount })}
            </div>
            <div className="text-xs text-orange-600 mt-0.5">{t('attentionDesc')}</div>
          </div>
        </div>
      ) : (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 flex items-center gap-3">
          <CheckCircle size={18} className="text-green-500" />
          <div className="text-sm text-green-800">{t('allGood')}</div>
        </div>
      )}

      {/* Forecast cards */}
      <div className="space-y-4">
        {data?.forecasts.map((forecast) => (
          <div key={forecast.resource} className="bg-white border rounded-lg p-5 space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                {riskIcon(forecast.riskLevel)}
                <span className="text-sm font-medium text-gray-900">{forecast.resource}</span>
                <span className={`px-1.5 py-0.5 rounded text-xs font-medium ${riskBadgeClass(forecast.riskLevel)}`}>
                  {t(`risk.${forecast.riskLevel}`)}
                </span>
              </div>
              {forecast.estimatedFullDate && (
                <div className="text-xs text-gray-500">
                  {t('estimatedFullDate', { date: new Date(forecast.estimatedFullDate).toLocaleDateString() })}
                </div>
              )}
            </div>

            {/* Usage bar */}
            <div>
              <div className="flex justify-between text-xs text-gray-500 mb-1">
                <span>
                  {t('usage')}: {forecast.current} {forecast.unit} / {forecast.capacity} {forecast.unit}
                </span>
                <span>{((forecast.current / forecast.capacity) * 100).toFixed(0)}%</span>
              </div>
              <div className="w-full bg-gray-100 rounded-full h-2">
                <div
                  className={`h-2 rounded-full ${usageBarColor(forecast.riskLevel)}`}
                  style={{ width: usageBarWidth(forecast.current, forecast.capacity) }}
                />
              </div>
            </div>

            {/* Growth and forecast */}
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <span className="text-gray-500">{t('weeklyGrowth')}: </span>
                <span className="font-medium text-gray-900">
                  +{forecast.weeklyGrowthRate} {forecast.unit}/{t('week')}
                </span>
              </div>
              {forecast.daysUntilFull != null && (
                <div>
                  <span className="text-gray-500">{t('daysUntilFull')}: </span>
                  <span className={`font-medium ${forecast.daysUntilFull < 60 ? 'text-orange-600' : 'text-gray-900'}`}>
                    {forecast.daysUntilFull} {t('days')}
                  </span>
                </div>
              )}
            </div>

            {/* Recommendation */}
            {forecast.recommendation && (
              <div className="text-xs text-blue-700 bg-blue-50 rounded p-2 border border-blue-100">
                💡 {forecast.recommendation}
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Footer meta */}
      {data && (
        <div className="text-xs text-gray-400 flex justify-between">
          <span>{t('basedOnWeeks', { weeks: data.analysisWeeks })}</span>
          <span>{t('nextReview', { date: new Date(data.nextReviewDate).toLocaleDateString() })}</span>
        </div>
      )}

      {data?.simulatedNote && (
        <p className="text-xs text-gray-400 italic">{data.simulatedNote}</p>
      )}
    </div>
  );
}
