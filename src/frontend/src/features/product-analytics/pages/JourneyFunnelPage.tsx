import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowRight,
  Clock,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { productAnalyticsApi } from '../api/productAnalyticsApi';
import type { JourneyItemDto } from '../../../types';

/**
 * Página de jornadas e funis do produto.
 *
 * Mostra jornadas-chave com steps, taxas de conclusão, pontos de abandono
 * e tempo médio. Alimentada pelo endpoint real /product-analytics/journeys.
 *
 * @see docs/PRODUCT-VISION.md — jornadas de valor do produto
 */

function completionColor(rate: number): string {
  if (rate >= 60) return 'text-success';
  if (rate >= 40) return 'text-accent';
  if (rate >= 25) return 'text-warning';
  return 'text-critical';
}

function barColor(percent: number): string {
  if (percent >= 80) return 'bg-success';
  if (percent >= 50) return 'bg-accent';
  if (percent >= 30) return 'bg-warning';
  return 'bg-critical';
}

export function JourneyFunnelPage() {
  const { t } = useTranslation();
  const [selectedJourney, setSelectedJourney] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['product-analytics-journeys'],
    queryFn: () => productAnalyticsApi.getJourneys({ range: 'last_30d' }),
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

  const allJourneys = data.journeys;
  const journeys: JourneyItemDto[] = selectedJourney
    ? allJourneys.filter((j) => j.journeyId === selectedJourney)
    : allJourneys;

  return (
    <PageContainer>
      <PageHeader
        title={t('analytics.journey.title')}
        subtitle={t('analytics.journey.subtitle')}
      />

      {/* Journey filter */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          type="button"
          onClick={() => setSelectedJourney(null)}
          className={`px-3 py-1.5 rounded-lg text-sm transition ${!selectedJourney ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-elevated text-muted border border-edge hover:border-edge-strong'}`}
        >
          {t('analytics.journey.all')}
        </button>
        {allJourneys.map((j) => (
          <button
            type="button"
            key={j.journeyId}
            onClick={() => setSelectedJourney(j.journeyId)}
            className={`px-3 py-1.5 rounded-lg text-sm transition ${selectedJourney === j.journeyId ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-elevated text-muted border border-edge hover:border-edge-strong'}`}
          >
            {j.journeyName}
          </button>
        ))}
      </div>

      {journeys.length === 0 ? (
        <div className="text-center py-12 text-faded">{t('common.noData')}</div>
      ) : (
        /* Journey cards */
        <div className="space-y-6">
          {journeys.map((j) => (
            <Card key={j.journeyId}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <span className="font-semibold text-heading">{j.journeyName}</span>
                  <div className="flex items-center gap-4 text-sm">
                    <span className={`font-medium ${completionColor(j.completionRate)}`}>
                      {j.completionRate}% {t('analytics.journey.completion')}
                    </span>
                    <span className="text-muted flex items-center gap-1">
                      <Clock size={14} />
                      {j.avgDurationMinutes} {t('analytics.minutes')}
                    </span>
                  </div>
                </div>
              </CardHeader>
              <CardBody>
                {/* Funnel visualization */}
                <div className="space-y-3 mb-4">
                  {j.steps.map((step, idx) => {
                    const previousStep = idx > 0 ? j.steps[idx - 1] : undefined;
                    const prevPercent = previousStep?.completionPercent ?? 100;
                    const dropOff = prevPercent - step.completionPercent;

                    return (
                      <div key={step.stepId}>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center gap-2">
                            {idx > 0 && <ArrowRight size={12} className="text-faded" />}
                            <span className="text-sm text-body">{step.stepName}</span>
                          </div>
                          <div className="flex items-center gap-3 text-sm">
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

                {/* Drop-off insight */}
                {j.biggestDropOff && (
                  <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-warning/10 border border-warning/25">
                    <AlertTriangle size={14} className="text-warning flex-shrink-0" />
                    <span className="text-sm text-warning">
                      {t('analytics.journey.biggestDropOff')}: {j.biggestDropOff}
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
