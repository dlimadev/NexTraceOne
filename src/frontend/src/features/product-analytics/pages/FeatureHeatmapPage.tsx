import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';

/**
 * Página de mapa de calor de adoção de funcionalidades.
 *
 * Mostra módulo × intensidade de utilização, identificando
 * áreas de baixa adoção que podem necessitar de melhoria de UX ou onboarding.
 * Alimentada pelo endpoint /product-analytics/heatmap.
 */

function intensityClass(intensity: number): string {
  if (intensity >= 0.8) return 'bg-success/80 text-white';
  if (intensity >= 0.6) return 'bg-success/50 text-heading';
  if (intensity >= 0.4) return 'bg-accent/40 text-heading';
  if (intensity >= 0.2) return 'bg-warning/40 text-heading';
  return 'bg-critical/30 text-heading';
}

function intensityBorder(intensity: number): string {
  if (intensity >= 0.8) return 'border-success/60';
  if (intensity >= 0.6) return 'border-success/30';
  if (intensity >= 0.4) return 'border-accent/30';
  if (intensity >= 0.2) return 'border-warning/30';
  return 'border-critical/30';
}

export function FeatureHeatmapPage() {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-feature-heatmap'],
    queryFn: () => productAnalyticsApi.getFeatureHeatmap({ range: 'last_30d' }),
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

  const { cells, totalUniqueUsers, periodLabel } = data;

  const sortedCells = [...cells].sort((a, b) => b.intensity - a.intensity);
  const lowAdoption = sortedCells.filter((c) => c.intensity < 0.3);

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.heatmap.title')}
        subtitle={t('analytics.heatmap.subtitle')}
      />

      {/* Summary stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <div className="bg-panel border border-edge rounded-xl p-4">
          <span className="text-xs text-faded uppercase tracking-widest">
            {t('analytics.heatmap.totalModules')}
          </span>
          <p className="text-2xl font-bold text-heading mt-1">{cells.length}</p>
        </div>
        <div className="bg-panel border border-edge rounded-xl p-4">
          <span className="text-xs text-faded uppercase tracking-widest">
            {t('analytics.heatmap.uniqueUsers')}
          </span>
          <p className="text-2xl font-bold text-heading mt-1">{totalUniqueUsers}</p>
        </div>
        <div className="bg-panel border border-edge rounded-xl p-4">
          <span className="text-xs text-faded uppercase tracking-widest">
            {t('analytics.heatmap.period')}
          </span>
          <p className="text-2xl font-bold text-heading mt-1">{periodLabel}</p>
        </div>
      </div>

      {/* Heatmap grid */}
      <Card>
        <CardHeader>
          <span className="font-semibold text-heading">{t('analytics.heatmap.adoptionMap')}</span>
        </CardHeader>
        <CardBody>
          {cells.length === 0 ? (
            <p className="text-center text-faded py-8">{t('common.noData')}</p>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
              {sortedCells.map((cell) => (
                <div
                  key={cell.module}
                  className={`rounded-xl border p-4 transition hover:scale-[1.02] ${intensityClass(cell.intensity)} ${intensityBorder(cell.intensity)}`}
                >
                  <p className="font-semibold text-sm truncate">{cell.moduleName}</p>
                  <p className="text-2xl font-bold mt-1">{cell.adoptionPercent}%</p>
                  <div className="flex justify-between text-xs mt-2 opacity-80">
                    <span>{cell.uniqueUsers} {t('analytics.heatmap.users')}</span>
                    <span>{cell.totalActions} {t('analytics.heatmap.actions')}</span>
                  </div>
                  {cell.topFeatures.length > 0 && (
                    <div className="mt-2 pt-2 border-t border-white/20">
                      <span className="text-[10px] uppercase tracking-wider opacity-70">
                        {t('analytics.heatmap.topFeatures')}
                      </span>
                      <ul className="mt-1 space-y-0.5">
                        {cell.topFeatures.slice(0, 3).map((f) => (
                          <li key={f.feature} className="text-xs truncate">
                            {f.feature} ({f.count})
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}

          {/* Legend */}
          <div className="flex items-center gap-4 mt-6 pt-4 border-t border-edge">
            <span className="text-xs text-faded">{t('analytics.heatmap.legend')}:</span>
            <div className="flex items-center gap-1">
              <span className="w-4 h-3 rounded bg-critical/30" />
              <span className="text-xs text-faded">&lt;20%</span>
            </div>
            <div className="flex items-center gap-1">
              <span className="w-4 h-3 rounded bg-warning/40" />
              <span className="text-xs text-faded">20-40%</span>
            </div>
            <div className="flex items-center gap-1">
              <span className="w-4 h-3 rounded bg-accent/40" />
              <span className="text-xs text-faded">40-60%</span>
            </div>
            <div className="flex items-center gap-1">
              <span className="w-4 h-3 rounded bg-success/50" />
              <span className="text-xs text-faded">60-80%</span>
            </div>
            <div className="flex items-center gap-1">
              <span className="w-4 h-3 rounded bg-success/80" />
              <span className="text-xs text-faded">&gt;80%</span>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Low adoption alert */}
      {lowAdoption.length > 0 && (
        <Card className="mt-6">
          <CardHeader>
            <span className="font-semibold text-warning">
              {t('analytics.heatmap.lowAdoptionTitle')}
            </span>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted mb-3">{t('analytics.heatmap.lowAdoptionDesc')}</p>
            <div className="space-y-2">
              {lowAdoption.map((cell) => (
                <div
                  key={cell.module}
                  className="flex items-center justify-between px-3 py-2 rounded-lg bg-elevated border border-edge"
                >
                  <span className="text-sm text-heading">{cell.moduleName}</span>
                  <div className="flex items-center gap-4 text-sm">
                    <span className="text-critical font-medium">{cell.adoptionPercent}%</span>
                    <span className="text-faded">{cell.uniqueUsers} {t('analytics.heatmap.users')}</span>
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
