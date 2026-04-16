import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { MapPin, CheckCircle2, Clock, AlertTriangle, ArrowRight, Lock } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { promotionApi } from '../api/promotion';
import type { PromotionPathStep } from '../api/promotion';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function stepStatusVariant(status: string): 'success' | 'warning' | 'danger' | 'info' | 'default' {
  if (status === 'Approved') return 'success';
  if (status === 'Blocked') return 'danger';
  if (status === 'Rejected') return 'danger';
  if (status === 'InEvaluation') return 'info';
  if (status === 'Pending') return 'warning';
  if (status === 'Cancelled') return 'default';
  return 'default';
}

function stepStatusIcon(status: string) {
  if (status === 'Approved') return <CheckCircle2 size={16} className="text-success" />;
  if (status === 'Blocked') return <Lock size={16} className="text-critical" />;
  if (status === 'Rejected') return <AlertTriangle size={16} className="text-critical" />;
  if (status === 'InEvaluation') return <Clock size={16} className="text-info" />;
  return <Clock size={16} className="text-warning" />;
}

function PromotionStep({ step, isLast, t }: {
  step: PromotionPathStep;
  isLast: boolean;
  t: (k: string, opts?: Record<string, unknown>) => string;
}) {
  return (
    <div className="flex items-start gap-4">
      {/* Step indicator */}
      <div className="flex flex-col items-center">
        <div className="flex items-center justify-center w-8 h-8 rounded-full border-2 border-edge bg-surface">
          {stepStatusIcon(step.status)}
        </div>
        {!isLast && (
          <div className="w-0.5 h-8 bg-edge mt-1" />
        )}
      </div>

      {/* Step content */}
      <div className="flex-1 pb-4">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-sm text-muted">{step.sourceEnvironment}</span>
          <ArrowRight size={12} className="text-muted" />
          <span className="text-sm font-medium text-heading">{step.targetEnvironment}</span>
          <Badge variant={stepStatusVariant(step.status)}>{step.status}</Badge>
        </div>
        <div className="text-xs text-muted space-y-0.5">
          <p>
            {t('promotionPath.requestedBy')}: <span className="text-body">{step.requestedBy}</span>
          </p>
          <p>
            {t('promotionPath.requestedAt')}: {' '}
            <span className="text-body">{new Date(step.requestedAt).toLocaleString()}</span>
          </p>
          {step.completedAt && (
            <p>
              {t('promotionPath.completedAt')}: {' '}
              <span className="text-body">{new Date(step.completedAt).toLocaleString()}</span>
            </p>
          )}
          {step.justification && (
            <p className="italic text-muted">"{step.justification}"</p>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * Painel de Caminho de Promoção por Ambiente.
 * Mostra o trajeto de promoção de uma release: Dev → Staging → Production.
 * Gap 10: Environment Promotion Path Timeline (4.5).
 */
export function EnvironmentPromotionPathPanel({ releaseId }: { releaseId?: string | null }) {
  const { t } = useTranslation();
  const [inputId, setInputId] = useState(releaseId ?? '');

  const {
    data,
    isLoading,
    isError,
    refetch,
    isFetching,
  } = useQuery({
    queryKey: ['environment-promotion-path', inputId],
    queryFn: () => promotionApi.getEnvironmentPromotionPath(inputId),
    enabled: !!inputId.trim(),
    staleTime: 30_000,
  });

  return (
    <div className="space-y-4">
      {/* Release ID input */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">{t('promotionPath.title')}</h3>
        </CardHeader>
        <CardBody>
          <div className="flex gap-2">
            <input
              className={INPUT_CLS}
              placeholder={t('promotionPath.releasePlaceholder')}
              value={inputId}
              onChange={(e) => setInputId(e.target.value)}
            />
            <button
              onClick={() => refetch()}
              disabled={!inputId.trim() || isFetching}
              className="px-4 py-2 rounded-md bg-accent text-white text-sm font-medium disabled:opacity-50 hover:bg-accent/90 transition-colors"
            >
              {isFetching ? t('common.loading') : t('common.search')}
            </button>
          </div>
        </CardBody>
      </Card>

      {/* Results */}
      {isLoading && <PageLoadingState />}

      {isError && (
        <PageErrorState message={t('common.errorLoading')} />
      )}

      {data && (
        <>
          {/* Summary bar */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('promotionPath.currentEnv')}</p>
                <p className="text-sm font-bold text-heading mt-1">
                  {data.currentEnvironment ?? t('promotionPath.unknown')}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('promotionPath.progress')}</p>
                <p className="text-sm font-bold text-heading mt-1">
                  {data.completedSteps}/{data.totalSteps}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('promotionPath.fullyPromoted')}</p>
                <div className="mt-1">
                  {data.isFullyPromoted ? (
                    <Badge variant="success">{t('common.yes')}</Badge>
                  ) : (
                    <Badge variant="warning">{t('common.no')}</Badge>
                  )}
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <p className="text-xs text-muted">{t('promotionPath.hasBlockers')}</p>
                <div className="mt-1">
                  {data.hasBlockers ? (
                    <Badge variant="danger">{t('common.yes')}</Badge>
                  ) : (
                    <Badge variant="success">{t('common.no')}</Badge>
                  )}
                </div>
              </CardBody>
            </Card>
          </div>

          {/* Timeline */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <MapPin size={16} className="text-accent" />
                <h3 className="text-sm font-semibold text-heading">{t('promotionPath.timelineTitle')}</h3>
              </div>
            </CardHeader>
            <CardBody>
              {data.steps.length === 0 ? (
                <EmptyState title={t('promotionPath.noSteps')} />
              ) : (
                <div className="pt-2">
                  {data.steps.map((step, idx) => (
                    <PromotionStep
                      key={step.promotionRequestId}
                      step={step}
                      isLast={idx === data.steps.length - 1}
                      t={t}
                    />
                  ))}
                </div>
              )}
            </CardBody>
          </Card>
        </>
      )}

      {!data && !isLoading && !inputId && (
        <EmptyState
          icon={<MapPin size={32} />}
          title={t('promotionPath.emptyTitle')}
          description={t('promotionPath.emptyMessage')}
        />
      )}
    </div>
  );
}
