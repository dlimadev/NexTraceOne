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
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { StatCard } from '../../../components/StatCard';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageContainer, PageSection } from '../../../components/shell';
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
    case 'Improving': return <TrendingUp size={14} className="text-emerald-400" />;
    case 'Declining': return <TrendingDown size={14} className="text-red-400" />;
    default: return <Minus size={14} className="text-zinc-400" />;
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
              className="px-3 py-2 rounded-md bg-zinc-900 border border-zinc-700 text-white text-xs hover:border-accent/50"
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
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-white">{t('analytics.title')}</h1>
        <p className="text-zinc-400 mt-1">{t('analytics.subtitle')}</p>
      </div>

      {/* Score cards */}
      <PageSection>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
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
            color="text-emerald-400"
            trend={{ direction: 'up', label: t('analytics.trendImproving') }}
          />
          <StatCard
            title={t('analytics.frictionScore')}
            value={`${d.frictionScore}%`}
            icon={<AlertTriangle size={20} />}
            color="text-amber-400"
            trend={{ direction: 'down', label: t('analytics.trendDeclining') }}
          />
          <StatCard
            title={t('analytics.uniqueUsers')}
            value={d.uniqueUsers}
            icon={<Users size={20} />}
            color="text-blue-400"
          />
        </div>
      </PageSection>

      {/* Time to value */}
      <PageSection>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Target size={18} className="text-accent" />
                <span className="font-semibold text-white">{t('analytics.timeToFirstValue')}</span>
              </div>
            </CardHeader>
            <CardBody>
              <div className="text-3xl font-bold text-accent">{d.avgTimeToFirstValueMinutes} {t('analytics.minutes')}</div>
              <p className="text-zinc-400 text-sm mt-1">{t('analytics.timeToFirstValueDesc')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Award size={18} className="text-emerald-400" />
                <span className="font-semibold text-white">{t('analytics.timeToCoreValue')}</span>
              </div>
            </CardHeader>
            <CardBody>
              <div className="text-3xl font-bold text-emerald-400">{d.avgTimeToCoreValueMinutes} {t('analytics.minutes')}</div>
              <p className="text-zinc-400 text-sm mt-1">{t('analytics.timeToCoreValueDesc')}</p>
            </CardBody>
          </Card>
        </div>
      </PageSection>

      {/* Top Modules */}
      <PageSection>
        <Card>
          <CardHeader>
            <span className="font-semibold text-white">{t('analytics.topModules')}</span>
          </CardHeader>
          <CardBody>
            <div className="divide-y divide-zinc-800">
              {d.topModules.map((mod) => (
                <div key={mod.module} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <span className="text-white font-medium">{mod.moduleName}</span>
                    {trendIcon(mod.trend)}
                  </div>
                  <div className="flex items-center gap-6 text-sm">
                    <span className="text-zinc-400">{mod.eventCount.toLocaleString()} {t('analytics.actions')}</span>
                    <span className="text-zinc-400">{mod.uniqueUsers} {t('analytics.users')}</span>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Quick links */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Link to="/analytics/adoption" className="p-4 rounded-xl bg-zinc-900/50 border border-zinc-800 hover:border-accent/40 transition text-center">
          <BarChart3 size={24} className="mx-auto mb-2 text-accent" />
          <span className="text-sm text-zinc-300">{t('analytics.viewModuleAdoption')}</span>
        </Link>
        <Link to="/analytics/personas" className="p-4 rounded-xl bg-zinc-900/50 border border-zinc-800 hover:border-accent/40 transition text-center">
          <Users size={24} className="mx-auto mb-2 text-blue-400" />
          <span className="text-sm text-zinc-300">{t('analytics.viewPersonaUsage')}</span>
        </Link>
        <Link to="/analytics/journeys" className="p-4 rounded-xl bg-zinc-900/50 border border-zinc-800 hover:border-accent/40 transition text-center">
          <Target size={24} className="mx-auto mb-2 text-emerald-400" />
          <span className="text-sm text-zinc-300">{t('analytics.viewJourneys')}</span>
        </Link>
        <Link to="/analytics/value" className="p-4 rounded-xl bg-zinc-900/50 border border-zinc-800 hover:border-accent/40 transition text-center">
          <Award size={24} className="mx-auto mb-2 text-amber-400" />
          <span className="text-sm text-zinc-300">{t('analytics.viewValueTracking')}</span>
        </Link>
      </div>

      {d.totalEvents === 0 && (
        <div className="text-center py-10 text-zinc-500 text-sm">
          {t('common.noData')}
        </div>
      )}
    </PageContainer>
  );
}
