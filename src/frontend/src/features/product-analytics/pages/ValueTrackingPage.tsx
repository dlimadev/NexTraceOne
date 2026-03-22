import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  CheckCircle,
  Clock,
  Users,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { StatCard } from '../../../components/StatCard';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';
import type { MilestoneTrend } from '../../../types';

/**
 * Página de value tracking — marcos de valor.
 *
 * Mostra progressão dos utilizadores em atingir marcos de valor do produto.
 * Alimentada pelo endpoint real /product-analytics/value-milestones.
 *
 * @see docs/PRODUCT-VISION.md — marcos de valor do produto
 */

function trendIcon(trend: MilestoneTrend) {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-emerald-400" />;
    case 'Declining': return <TrendingDown size={14} className="text-red-400" />;
    default: return <Minus size={14} className="text-muted" />;
  }
}

function completionColor(rate: number): string {
  if (rate >= 75) return 'bg-emerald-500';
  if (rate >= 50) return 'bg-accent';
  if (rate >= 30) return 'bg-amber-500';
  return 'bg-red-500';
}

function formatTime(minutes: number): string {
  if (minutes < 60) return `${minutes.toFixed(0)}m`;
  const hours = Math.floor(minutes / 60);
  const mins = Math.round(minutes % 60);
  return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
}

export function ValueTrackingPage() {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-value-milestones'],
    queryFn: () => productAnalyticsApi.getValueMilestones({ range: 'last_30d' }),
    staleTime: 15_000,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState message={t('common.loading')} />
      </PageContainer>
    );
  }

  if (isError || !data) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <button
              type="button"
              onClick={() => refetch()}
              className="px-3 py-2 rounded-md bg-panel border border-edge text-heading text-xs hover:border-accent/50"
            >
              {t('common.retry')}
            </button>
          }
        />
      </PageContainer>
    );
  }

  const milestones = data.milestones;

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.value.title')}
        subtitle={t('analytics.value.subtitle')}
      />

      {/* Summary cards */}
      <StatsGrid columns={4}>
        <StatCard
          title={t('analytics.timeToFirstValue')}
          value={formatTime(data.avgTimeToFirstValueMinutes)}
          icon={<Clock size={20} />}
          color="text-accent"
          trend={{ direction: 'down', label: t('analytics.trendImproving') }}
        />
        <StatCard
          title={t('analytics.timeToCoreValue')}
          value={formatTime(data.avgTimeToCoreValueMinutes)}
          icon={<CheckCircle size={20} />}
          color="text-emerald-400"
          trend={{ direction: 'down', label: t('analytics.trendImproving') }}
        />
        <StatCard
          title={t('analytics.value.avgCompletion')}
          value={`${data.overallCompletionRate.toFixed(1)}%`}
          icon={<TrendingUp size={20} />}
          color="text-blue-400"
        />
        <StatCard
          title={t('analytics.value.totalMilestones')}
          value={milestones.length}
          icon={<Users size={20} />}
          color="text-amber-400"
        />
      </StatsGrid>

      {milestones.length === 0 ? (
        <div className="text-center py-12 text-faded">{t('common.noData')}</div>
      ) : (
        <Card>
          <CardHeader>
            <span className="font-semibold text-heading">{t('analytics.value.milestoneProgress')}</span>
          </CardHeader>
          <CardBody>
            <div className="space-y-4">
              {milestones.map((m) => (
                <div key={m.milestoneType} className="flex flex-col md:flex-row md:items-center gap-3">
                  {/* Milestone name & trend */}
                  <div className="md:w-72 flex items-center gap-2">
                    <CheckCircle size={16} className={m.completionRate >= 50 ? 'text-emerald-400' : 'text-zinc-600'} />
                    <span className="text-sm text-heading">{t(`analytics.milestone.${m.milestoneType}`, { defaultValue: m.milestoneName })}</span>
                    {trendIcon(m.trend)}
                  </div>

                  {/* Progress bar */}
                  <div className="flex-1 flex items-center gap-3">
                    <div className="flex-1 h-2 rounded-full bg-elevated overflow-hidden">
                      <div
                        className={`h-full rounded-full ${completionColor(m.completionRate)} transition-all`}
                        style={{ width: `${m.completionRate}%` }}
                      />
                    </div>
                    <span className="text-sm text-heading font-medium w-14 text-right">{m.completionRate}%</span>
                  </div>

                  {/* Stats */}
                  <div className="flex items-center gap-4 text-sm md:w-48">
                    <span className="text-muted flex items-center gap-1">
                      <Clock size={12} />
                      {formatTime(m.avgTimeToReachMinutes)}
                    </span>
                    <span className="text-muted flex items-center gap-1">
                      <Users size={12} />
                      {m.usersReached}
                    </span>
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
