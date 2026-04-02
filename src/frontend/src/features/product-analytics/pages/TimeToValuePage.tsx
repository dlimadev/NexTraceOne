import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Clock, Target, TrendingDown } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { StatCardSmall } from '../../../components/StatCardSmall';
import { productAnalyticsApi } from '../api/productAnalyticsApi';

/**
 * Página de dashboard Time-to-Value.
 *
 * Apresenta métricas de Time-to-First-Value (TTFV) e
 * Time-to-Core-Value (TTCV), milestones atingidos e
 * tendência de adoção ao longo do tempo.
 * Alimentada pelos endpoints /product-analytics/value-milestones e /product-analytics/summary.
 */

function trendIcon(trend: string) {
  if (trend === 'up') return '↑';
  if (trend === 'down') return '↓';
  return '→';
}

function trendColor(trend: string) {
  if (trend === 'up') return 'text-success';
  if (trend === 'down') return 'text-critical';
  return 'text-faded';
}

export function TimeToValuePage() {
  const { t } = useTranslation();

  const valueMilestonesQuery = useQuery({
    queryKey: ['product-analytics-value-milestones'],
    queryFn: () => productAnalyticsApi.getValueMilestones({ range: 'last_30d' }),
    staleTime: 15_000,
  });

  const summaryQuery = useQuery({
    queryKey: ['product-analytics-summary'],
    queryFn: () => productAnalyticsApi.getSummary(),
    staleTime: 15_000,
  });

  const isLoading = valueMilestonesQuery.isLoading || summaryQuery.isLoading;
  const isError = valueMilestonesQuery.isError || summaryQuery.isError;

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('common.loading')} />
      </PageContainer>
    );
  }

  if (isError || !valueMilestonesQuery.data) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <button
              type="button"
              onClick={() => {
                valueMilestonesQuery.refetch();
                summaryQuery.refetch();
              }}
              className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
            >
              {t('common.retry')}
            </button>
          }
        />
      </PageContainer>
    );
  }

  const milestones = valueMilestonesQuery.data;
  const summary = summaryQuery.data;

  const reached = milestones.milestones.filter((m) => m.completionRate > 0);
  const highCompletion = milestones.milestones.filter((m) => m.completionRate >= 50);

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.timeToValue.title')}
        subtitle={t('analytics.timeToValue.subtitle')}
      />

      {/* Top-level time metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCardSmall
          icon={<Clock size={16} />}
          label={t('analytics.timeToValue.ttfv')}
          value={milestones.avgTimeToFirstValueMinutes != null
            ? `${milestones.avgTimeToFirstValueMinutes} min`
            : '—'}
          accent="accent"
        />
        <StatCardSmall
          icon={<Target size={16} />}
          label={t('analytics.timeToValue.ttcv')}
          value={milestones.avgTimeToCoreValueMinutes != null
            ? `${milestones.avgTimeToCoreValueMinutes} min`
            : '—'}
          accent="accent"
        />
        <StatCardSmall
          icon={<TrendingDown size={16} />}
          label={t('analytics.timeToValue.milestonesReached')}
          value={`${reached.length} / ${milestones.milestones.length}`}
          accent={reached.length > milestones.milestones.length / 2 ? 'success' : 'warning'}
        />
        {summary && (
          <StatCardSmall
            icon={<Target size={16} />}
            label={t('analytics.timeToValue.totalUsers')}
            value={String(summary.totalUsers ?? 0)}
            accent="accent"
          />
        )}
      </div>

      {/* Milestone progress */}
      <Card>
        <CardHeader>
          <span className="font-semibold text-heading">{t('analytics.timeToValue.milestoneProgress')}</span>
        </CardHeader>
        <CardBody>
          {milestones.milestones.length === 0 ? (
            <p className="text-center text-faded py-8">{t('common.noData')}</p>
          ) : (
            <div className="space-y-4">
              {milestones.milestones.map((m) => (
                <div key={m.milestone} className="space-y-1">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-heading font-medium">{m.label}</span>
                      <span className={`text-xs ${trendColor(m.trend)}`}>{trendIcon(m.trend)}</span>
                    </div>
                    <div className="flex items-center gap-4 text-sm">
                      {m.avgTimeToReachMinutes != null && (
                        <span className="text-faded">
                          {t('analytics.timeToValue.avgTime')}: {m.avgTimeToReachMinutes} min
                        </span>
                      )}
                      <span className="text-heading font-medium">{m.completionRate}%</span>
                    </div>
                  </div>
                  <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all ${m.completionRate >= 75 ? 'bg-success' : m.completionRate >= 40 ? 'bg-accent' : m.completionRate >= 15 ? 'bg-warning' : 'bg-critical'}`}
                      style={{ width: `${m.completionRate}%` }}
                    />
                  </div>
                  <div className="flex items-center gap-4 text-xs text-faded">
                    <span>
                      {m.usersReached} / {m.usersReached + (m.usersReached > 0 ? Math.round(m.usersReached * (100 / m.completionRate - 1)) : 0)} {t('analytics.timeToValue.users')}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>

      {/* High completion highlights */}
      {highCompletion.length > 0 && (
        <Card className="mt-6">
          <CardHeader>
            <span className="font-semibold text-success">
              {t('analytics.timeToValue.highCompletionTitle')}
            </span>
          </CardHeader>
          <CardBody>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {highCompletion.map((m) => (
                <div
                  key={m.milestone}
                  className="rounded-lg bg-success/10 border border-success/25 p-3"
                >
                  <p className="text-sm font-medium text-heading">{m.label}</p>
                  <div className="flex items-center justify-between mt-2">
                    <span className="text-lg font-bold text-success">{m.completionRate}%</span>
                    {m.avgTimeToReachMinutes != null && (
                      <span className="text-xs text-faded">
                        {m.avgTimeToReachMinutes} min
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
