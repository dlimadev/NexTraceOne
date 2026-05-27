import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { TrendingUp, RefreshCw, AlertTriangle, CheckCircle } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type CapacityResourceForecast } from '../api/platformAdmin';

export function CapacityForecastPage() {
  const { t } = useTranslation('capacityForecast');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['capacity-forecast'],
    queryFn: platformAdminApi.getCapacityForecast,
  });

  function riskBadgeClass(risk: CapacityResourceForecast['riskLevel']) {
    switch (risk) {
      case 'Critical': return 'bg-critical/10 text-critical';
      case 'High': return 'bg-warning/10 text-warning';
      case 'Medium': return 'bg-warning/10 text-warning';
      default: return 'bg-success/10 text-success';
    }
  }

  function riskIcon(risk: CapacityResourceForecast['riskLevel']) {
    if (risk === 'Critical' || risk === 'High') {
      return <AlertTriangle size={14} className="text-warning" />;
    }
    return <CheckCircle size={14} className="text-success" />;
  }

  function usageBarWidth(current: number, capacity: number) {
    const pct = Math.min((current / capacity) * 100, 100);
    return `${pct.toFixed(0)}%`;
  }

  function usageBarColor(risk: CapacityResourceForecast['riskLevel']) {
    switch (risk) {
      case 'Critical': return 'bg-critical';
      case 'High': return 'bg-warning';
      case 'Medium': return 'bg-warning';
      default: return 'bg-success';
    }
  }

  if (isLoading) return <div className="p-6 text-sm text-muted">{t('loading')}</div>;
  if (isError) return <div className="p-6 text-sm text-critical">{t('error')}</div>;

  const criticalCount = data?.forecasts.filter((f) => f.riskLevel === 'Critical' || f.riskLevel === 'High').length ?? 0;

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<TrendingUp size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Summary banner */}
        {criticalCount > 0 ? (
          <div className="bg-warning/10 border border-warning/20 rounded-lg p-4 flex items-start gap-3">
            <AlertTriangle size={18} className="text-warning mt-0.5 shrink-0" />
            <div>
              <div className="text-sm font-medium text-warning">
                {t('attentionNeeded', { count: criticalCount })}
              </div>
              <div className="text-xs text-warning/80 mt-0.5">{t('attentionDesc')}</div>
            </div>
          </div>
        ) : (
          <div className="bg-success/10 border border-success/20 rounded-lg p-4 flex items-center gap-3">
            <CheckCircle size={18} className="text-success" />
            <div className="text-sm text-success">{t('allGood')}</div>
          </div>
        )}

        {/* Forecast cards */}
        <div className="space-y-4">
          {data?.forecasts.map((forecast) => (
            <div key={forecast.resource} className="bg-card border border-edge rounded-lg p-5 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  {riskIcon(forecast.riskLevel)}
                  <span className="text-sm font-medium text-heading">{forecast.resource}</span>
                  <span className={`px-1.5 py-0.5 rounded text-xs font-medium ${riskBadgeClass(forecast.riskLevel)}`}>
                    {t(`risk.${forecast.riskLevel}`)}
                  </span>
                </div>
                {forecast.estimatedFullDate && (
                  <div className="text-xs text-muted">
                    {t('estimatedFullDate', { date: new Date(forecast.estimatedFullDate).toLocaleDateString() })}
                  </div>
                )}
              </div>

              {/* Usage bar */}
              <div>
                <div className="flex justify-between text-xs text-muted mb-1">
                  <span>
                    {t('usage')}: {forecast.current} {forecast.unit} / {forecast.capacity} {forecast.unit}
                  </span>
                  <span>{((forecast.current / forecast.capacity) * 100).toFixed(0)}%</span>
                </div>
                <div className="w-full bg-elevated rounded-full h-2">
                  <div
                    className={`h-2 rounded-full ${usageBarColor(forecast.riskLevel)}`}
                    style={{ width: usageBarWidth(forecast.current, forecast.capacity) }}
                  />
                </div>
              </div>

              {/* Growth and forecast */}
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted">{t('weeklyGrowth')}: </span>
                  <span className="font-medium text-heading">
                    +{forecast.weeklyGrowthRate} {forecast.unit}/{t('week')}
                  </span>
                </div>
                {forecast.daysUntilFull != null && (
                  <div>
                    <span className="text-muted">{t('daysUntilFull')}: </span>
                    <span className={`font-medium ${forecast.daysUntilFull < 60 ? 'text-warning' : 'text-heading'}`}>
                      {forecast.daysUntilFull} {t('days')}
                    </span>
                  </div>
                )}
              </div>

              {/* Recommendation */}
              {forecast.recommendation && (
                <div className="text-xs text-accent bg-accent/10 rounded p-2 border border-accent/20">
                  {forecast.recommendation}
                </div>
              )}
            </div>
          ))}
        </div>

        {/* Footer meta */}
        {data && (
          <div className="text-xs text-faded flex justify-between">
            <span>{t('basedOnWeeks', { weeks: data.analysisWeeks })}</span>
            <span>{t('nextReview', { date: new Date(data.nextReviewDate).toLocaleDateString() })}</span>
          </div>
        )}

        {data?.simulatedNote && (
          <p className="text-xs text-faded italic">{data.simulatedNote}</p>
        )}
      </div>
    </PageContainer>
  );
}
