import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Users,
  BarChart3,
  Target,
  Award,
  AlertTriangle,
  Filter,
  Flame,
  Clock,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { StatCard } from '../../../components/StatCard';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { productAnalyticsApi } from '../api/productAnalyticsApi';

/**
 * Página principal de Product Analytics.
 *
 * Fornece visão consolidada de adoção, valor, fricção e tendências.
 * Orientada a decisão de produto — não a vanity metrics.
 * Persona-aware: destaque e linguagem adaptados à persona do utilizador.
 *
 * @see docs/PRODUCT-VISION.md — analytics como capacidade do produto
 */

function trendIcon(trend: 'Improving' | 'Stable' | 'Declining') {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    default: return <Minus size={14} className="text-muted" />;
  }
}

export function ProductAnalyticsOverviewPage() {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-summary'],
    queryFn: () => productAnalyticsApi.getSummary({ range: 'last_30d' }),
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

  const d = data;

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.title')}
        subtitle={t('analytics.subtitle')}
      />

      {/* Score cards */}
      <PageSection>
        <StatsGrid columns={4}>
          <StatCard
            title={t('analytics.adoptionScore')}
            value={`${d.adoptionScore}%`}
            icon={<BarChart3 size={20} />}
            color="text-accent"
            trend={{ direction: 'up', label: t('analytics.trendImproving') }}
          />
          <StatCard
            title={t('analytics.valueScore')}
            value={`${d.valueScore}%`}
            icon={<Award size={20} />}
            color="text-success"
            trend={{ direction: 'up', label: t('analytics.trendImproving') }}
          />
          <StatCard
            title={t('analytics.frictionScore')}
            value={`${d.frictionScore}%`}
            icon={<AlertTriangle size={20} />}
            color="text-warning"
            trend={{ direction: 'down', label: t('analytics.trendDeclining') }}
          />
          <StatCard
            title={t('analytics.uniqueUsers')}
            value={d.uniqueUsers}
            icon={<Users size={20} />}
            color="text-info"
          />
        </StatsGrid>
      </PageSection>

      {/* Time to value */}
      <PageSection>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Target size={18} className="text-accent" />
                <span className="font-semibold text-heading">{t('analytics.timeToFirstValue')}</span>
              </div>
            </CardHeader>
            <CardBody>
              <div className="text-3xl font-bold text-accent">{d.avgTimeToFirstValueMinutes} {t('analytics.minutes')}</div>
              <p className="text-muted text-sm mt-1">{t('analytics.timeToFirstValueDesc')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Award size={18} className="text-success" />
                <span className="font-semibold text-heading">{t('analytics.timeToCoreValue')}</span>
              </div>
            </CardHeader>
            <CardBody>
              <div className="text-3xl font-bold text-success">{d.avgTimeToCoreValueMinutes} {t('analytics.minutes')}</div>
              <p className="text-muted text-sm mt-1">{t('analytics.timeToCoreValueDesc')}</p>
            </CardBody>
          </Card>
        </div>
      </PageSection>

      {/* Top Modules */}
      <PageSection>
        <Card>
          <CardHeader>
            <span className="font-semibold text-heading">{t('analytics.topModules')}</span>
          </CardHeader>
          <CardBody>
            <div className="divide-y divide-edge">
              {d.topModules.map((mod) => (
                <div key={mod.module} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <span className="text-heading font-medium">{mod.moduleName}</span>
                    {trendIcon(mod.trend)}
                  </div>
                  <div className="flex items-center gap-6 text-sm">
                    <span className="text-muted">{mod.eventCount.toLocaleString()} {t('analytics.actions')}</span>
                    <span className="text-muted">{mod.uniqueUsers} {t('analytics.users')}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Quick links */}
      <StatsGrid columns={4}>
        <Link to="/analytics/adoption" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <BarChart3 size={24} className="mx-auto mb-2 text-accent" />
          <span className="text-sm text-body">{t('analytics.viewModuleAdoption')}</span>
        </Link>
        <Link to="/analytics/personas" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <Users size={24} className="mx-auto mb-2 text-info" />
          <span className="text-sm text-body">{t('analytics.viewPersonaUsage')}</span>
        </Link>
        <Link to="/analytics/journeys" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <Target size={24} className="mx-auto mb-2 text-success" />
          <span className="text-sm text-body">{t('analytics.viewJourneys')}</span>
        </Link>
        <Link to="/analytics/value" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <Award size={24} className="mx-auto mb-2 text-warning" />
          <span className="text-sm text-body">{t('analytics.viewValueTracking')}</span>
        </Link>
        <Link to="/analytics/funnel" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <Filter size={24} className="mx-auto mb-2 text-accent" />
          <span className="text-sm text-body">{t('analytics.funnel.title')}</span>
        </Link>
        <Link to="/analytics/heatmap" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <Flame size={24} className="mx-auto mb-2 text-critical" />
          <span className="text-sm text-body">{t('analytics.heatmap.title')}</span>
        </Link>
        <Link to="/analytics/time-to-value" className="p-4 rounded-xl bg-panel border border-edge hover:border-accent/40 transition text-center">
          <Clock size={24} className="mx-auto mb-2 text-success" />
          <span className="text-sm text-body">{t('analytics.timeToValue.title')}</span>
        </Link>
      </StatsGrid>

      {d.totalEvents === 0 && (
        <div className="text-center py-10 text-faded text-sm">
          {t('common.noData')}
        </div>
      )}
    </PageContainer>
  );
}
