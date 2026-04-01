import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Search,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { productAnalyticsApi } from '../api/productAnalyticsApi';

/**
 * Página de adoção por módulo.
 *
 * Mostra para cada módulo do produto: percentagem de adoção, número de ações,
 * utilizadores únicos, profundidade de uso e tendência.
 * Permite filtrar por persona e período.
 *
 * @see docs/MODULES-AND-PAGES.md — módulos oficiais do produto
 */

function trendIcon(trend: 'Improving' | 'Stable' | 'Declining') {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-success" />;
    case 'Declining': return <TrendingDown size={14} className="text-critical" />;
    default: return <Minus size={14} className="text-muted" />;
  }
}

function adoptionColor(percent: number): string {
  if (percent >= 75) return 'bg-success';
  if (percent >= 50) return 'bg-accent';
  if (percent >= 30) return 'bg-warning';
  return 'bg-critical';
}

export function ModuleAdoptionPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-module-adoption'],
    queryFn: () => productAnalyticsApi.getModuleAdoption({ range: 'last_30d' }),
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

  const modules = data.modules;

  const filtered = modules.filter((m) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return m.moduleName.toLowerCase().includes(q) || m.topFeatures.some((f) => f.includes(q));
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.adoption.title')}
        subtitle={t('analytics.adoption.subtitle')}
      />

      {/* Search */}
      <div className="flex items-center gap-2 mb-6">
        <div className="relative flex-1 max-w-sm">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-faded" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('analytics.adoption.searchPlaceholder')}
            className="w-full pl-9 pr-3 py-2 rounded-lg bg-input border border-edge text-heading placeholder-faded focus:border-accent/50 focus:outline-none text-sm"
          />
        </div>
      </div>

      {/* Module list */}
      <div className="space-y-3">
        {filtered.map((mod) => (
          <Card key={mod.module}>
            <CardBody>
              <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                {/* Module info */}
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <span className="text-heading font-semibold">{mod.moduleName}</span>
                    {trendIcon(mod.trend)}
                    <span className="text-xs text-faded">{t(`analytics.trend.${mod.trend}`)}</span>
                  </div>
                  {/* Adoption bar */}
                  <div className="flex items-center gap-3">
                    <div className="w-48 h-2 rounded-full bg-elevated overflow-hidden">
                      <div
                        className={`h-full rounded-full ${adoptionColor(mod.adoptionPercent)} transition-all`}
                        style={{ width: `${mod.adoptionPercent}%` }}
                      />
                    </div>
                    <span className="text-sm text-heading font-medium">{mod.adoptionPercent}%</span>
                  </div>
                </div>

                {/* Stats */}
                <div className="flex items-center gap-6 text-sm">
                  <div className="text-center">
                    <div className="text-heading font-medium">{mod.totalActions.toLocaleString()}</div>
                    <div className="text-faded text-xs">{t('analytics.actions')}</div>
                  </div>
                  <div className="text-center">
                    <div className="text-heading font-medium">{mod.uniqueUsers}</div>
                    <div className="text-faded text-xs">{t('analytics.users')}</div>
                  </div>
                  <div className="text-center">
                    <div className="text-heading font-medium">{mod.depthScore.toFixed(1)}</div>
                    <div className="text-faded text-xs">{t('analytics.adoption.depthScore')}</div>
                  </div>
                </div>
              </div>

              {/* Top features */}
              <div className="mt-3 flex flex-wrap gap-2">
                {mod.topFeatures.map((f) => (
                  <span key={f} className="px-2 py-0.5 rounded-md bg-elevated text-muted text-xs">
                    {f}
                  </span>
                ))}
              </div>
            </CardBody>
          </Card>
        ))}
      </div>

      {filtered.length === 0 && (
        <div className="text-center py-12 text-faded">{t('analytics.adoption.noResults')}</div>
      )}
    </PageContainer>
  );
}
