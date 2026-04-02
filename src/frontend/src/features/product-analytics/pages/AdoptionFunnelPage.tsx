import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ArrowRight, AlertTriangle, Filter } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';

/**
 * Página de funil de adoção por módulo.
 *
 * Mostra funis cohort-based para cada módulo do produto,
 * identificando onde os utilizadores abandonam fluxos críticos.
 * Alimentada pelo endpoint /product-analytics/adoption/funnel.
 */

function barColor(percent: number): string {
  if (percent >= 80) return 'bg-success';
  if (percent >= 50) return 'bg-accent';
  if (percent >= 30) return 'bg-warning';
  return 'bg-critical';
}

export function AdoptionFunnelPage() {
  const { t } = useTranslation();
  const [selectedModule, setSelectedModule] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-adoption-funnel', selectedModule],
    queryFn: () =>
      productAnalyticsApi.getAdoptionFunnel({
        module: selectedModule ?? undefined,
        range: 'last_30d',
      }),
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

  const funnels = data.funnels;

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.funnel.title')}
        subtitle={t('analytics.funnel.subtitle')}
      />

      {/* Module filter */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          type="button"
          onClick={() => setSelectedModule(null)}
          className={`px-3 py-1.5 rounded-lg text-sm transition flex items-center gap-1.5 ${!selectedModule ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-elevated text-muted border border-edge hover:border-edge-strong'}`}
        >
          <Filter size={14} />
          {t('analytics.funnel.allModules')}
        </button>
        {funnels.map((f) => (
          <button
            type="button"
            key={f.module}
            onClick={() => setSelectedModule(f.module)}
            className={`px-3 py-1.5 rounded-lg text-sm transition ${selectedModule === f.module ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-elevated text-muted border border-edge hover:border-edge-strong'}`}
          >
            {f.moduleName}
          </button>
        ))}
      </div>

      {funnels.length === 0 ? (
        <div className="text-center py-12 text-faded">{t('common.noData')}</div>
      ) : (
        <div className="space-y-6">
          {funnels.map((funnel) => (
            <Card key={funnel.module}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <span className="font-semibold text-heading">{funnel.moduleName}</span>
                    <span className="text-xs text-faded bg-elevated px-2 py-0.5 rounded-full">
                      {funnel.totalSessions} {t('analytics.funnel.sessions')}
                    </span>
                  </div>
                  <span
                    className={`font-medium text-sm ${funnel.completionRate >= 50 ? 'text-success' : funnel.completionRate >= 25 ? 'text-warning' : 'text-critical'}`}
                  >
                    {funnel.completionRate}% {t('analytics.journey.completion')}
                  </span>
                </div>
              </CardHeader>
              <CardBody>
                <div className="space-y-3 mb-4">
                  {funnel.steps.map((step, idx) => {
                    const prev = idx > 0 ? funnel.steps[idx - 1] : undefined;
                    const dropOff = prev ? prev.completionPercent - step.completionPercent : 0;

                    return (
                      <div key={step.stepId}>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center gap-2">
                            {idx > 0 && <ArrowRight size={12} className="text-faded" />}
                            <span className="text-sm text-body">{step.stepName}</span>
                          </div>
                          <div className="flex items-center gap-3 text-sm">
                            <span className="text-faded">{step.sessionCount} {t('analytics.funnel.sessions')}</span>
                            <span className="text-heading font-medium">{step.completionPercent}%</span>
                            {dropOff > 5 && (
                              <span className="text-critical text-xs">-{dropOff.toFixed(1)}%</span>
                            )}
                          </div>
                        </div>
                        <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
                          <div
                            className={`h-full rounded-full ${barColor(step.completionPercent)} transition-all`}
                            style={{ width: `${step.completionPercent}%` }}
                          />
                        </div>
                      </div>
                    );
                  })}
                </div>

                {funnel.biggestDropOff && (
                  <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-warning/10 border border-warning/25">
                    <AlertTriangle size={14} className="text-warning flex-shrink-0" />
                    <span className="text-sm text-warning">
                      {t('analytics.journey.biggestDropOff')}: {funnel.biggestDropOff}
                    </span>
                  </div>
                )}
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
