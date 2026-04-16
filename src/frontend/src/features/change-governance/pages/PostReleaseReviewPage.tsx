import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ClipboardCheck,
  Play,
  CheckCircle2,
  XCircle,
  Clock,
  BarChart2,
  Activity,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import { ReleaseSelector } from '../components/ReleaseSelector';

/**
 * PostReleaseReviewPage — revisão pós-release de uma release.
 *
 * Permite engenheiros e tech leads acompanhar o processo de revisão pós-deploy:
 * - Ver o estado atual da revisão (fase, outcome, confiança)
 * - Iniciar uma revisão pós-release para uma release específica
 * - Ver as janelas de observação de telemetria coletadas
 * - Acompanhar o baseline de performance pré/pós deploy
 */
export function PostReleaseReviewPage() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const [releaseId, setReleaseId] = useState('');

  const {
    data: review,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['post-release-review', releaseId],
    queryFn: () => changeIntelligenceApi.getPostReleaseReview(releaseId),
    enabled: !!releaseId,
    retry: false,
  });

  const startMutation = useMutation({
    mutationFn: (rid: string) => changeIntelligenceApi.startPostReleaseReview(rid),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['post-release-review', releaseId] }),
  });

  const progressMutation = useMutation({
    mutationFn: (rid: string) => changeIntelligenceApi.progressPostReleaseReview(rid),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['post-release-review', releaseId] }),
  });

  function outcomeBadge(outcome: string): 'success' | 'danger' | 'warning' | 'default' {
    switch (outcome) {
      case 'Pass':
        return 'success';
      case 'Fail':
        return 'danger';
      case 'Inconclusive':
        return 'warning';
      default:
        return 'default';
    }
  }

  function phaseBadge(phase: string): 'info' | 'warning' | 'success' | 'default' {
    switch (phase) {
      case 'WindowCollection':
        return 'info';
      case 'Analysis':
        return 'warning';
      case 'Completed':
        return 'success';
      default:
        return 'default';
    }
  }

  const inputCls =
    'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

  return (
    <PageContainer>
      <PageHeader
        icon={<ClipboardCheck className="text-accent" />}
        title={t('postReleaseReview.title', 'Post-Release Review')}
        subtitle={t(
          'postReleaseReview.subtitle',
          'Monitor post-deploy observation windows, performance baselines and review outcomes',
        )}
      />

      {/* Release selection */}
      <div className="mb-6">
        <Card>
          <CardBody>
            <p className="text-sm text-muted mb-3">
              {t('postReleaseReview.selectRelease', 'Select a release to load its post-release review')}
            </p>
            <ReleaseSelector
              value={releaseId}
              onChange={(id) => setReleaseId(id)}
              placeholder={t('postReleaseReview.selectorPlaceholder', 'Select a release…')}
            />
          </CardBody>
        </Card>
      </div>

      {/* Results */}
      {!releaseId && (
        <EmptyState
          icon={<ClipboardCheck size={40} />}
          title={t('postReleaseReview.emptyTitle', 'No review loaded')}
          description={t('postReleaseReview.emptyDescription', 'Select a release above to view its post-release review')}
        />
      )}

      {releaseId && isLoading && <PageLoadingState />}

      {releaseId && isError && (
        <div className="space-y-4">
          <Card>
            <CardBody>
              <div className="flex items-center gap-3">
                <XCircle className="text-danger" size={20} />
                <div>
                  <p className="text-sm font-medium text-heading">
                    {t('postReleaseReview.notFound', 'No post-release review found for this release')}
                  </p>
                  <p className="text-xs text-muted mt-1">
                    {t('postReleaseReview.notFoundHint', 'You can start a new review below')}
                  </p>
                </div>
              </div>
            </CardBody>
          </Card>
          <Button
            variant="primary"
            icon={<Play size={16} />}
            loading={startMutation.isPending}
            onClick={() => startMutation.mutate(releaseId)}
          >
            {t('postReleaseReview.startReview', 'Start Post-Release Review')}
          </Button>
        </div>
      )}

      {releaseId && review && (
        <div className="space-y-6">
          {/* Review header */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between flex-wrap gap-2">
                <div>
                  <h2 className="font-semibold text-heading">
                    {review.serviceName} — v{review.version}
                  </h2>
                  <p className="text-xs text-muted mt-0.5">{review.environment}</p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={phaseBadge(review.currentPhase)}>{review.currentPhase}</Badge>
                  <Badge variant={outcomeBadge(review.outcome)}>{review.outcome}</Badge>
                  {review.isCompleted && (
                    <Badge variant="success">
                      <CheckCircle2 size={12} className="mr-1" />
                      {t('postReleaseReview.completed', 'Completed')}
                    </Badge>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
                <div className="bg-surface rounded-lg p-3">
                  <p className="text-xs text-muted">{t('postReleaseReview.confidence', 'Confidence Score')}</p>
                  <p className="text-xl font-bold text-heading mt-1">
                    {(review.confidenceScore * 100).toFixed(0)}%
                  </p>
                </div>
                <div className="bg-surface rounded-lg p-3">
                  <p className="text-xs text-muted">{t('postReleaseReview.observationWindows', 'Observation Windows')}</p>
                  <p className="text-xl font-bold text-heading mt-1">
                    {review.observationWindows?.length ?? 0}
                  </p>
                </div>
                <div className="bg-surface rounded-lg p-3">
                  <p className="text-xs text-muted">{t('postReleaseReview.startedAt', 'Started At')}</p>
                  <p className="text-sm font-medium text-heading mt-1">
                    {new Date(review.startedAt).toLocaleDateString()}
                  </p>
                </div>
                <div className="bg-surface rounded-lg p-3">
                  <p className="text-xs text-muted">{t('postReleaseReview.completedAt', 'Completed At')}</p>
                  <p className="text-sm font-medium text-heading mt-1">
                    {review.completedAt
                      ? new Date(review.completedAt).toLocaleDateString()
                      : '—'}
                  </p>
                </div>
              </div>

              {review.summary && (
                <div className="bg-surface rounded-lg p-3 border border-edge">
                  <p className="text-xs text-muted mb-1">{t('postReleaseReview.summary', 'Summary')}</p>
                  <p className="text-sm text-heading">{review.summary}</p>
                </div>
              )}

              {!review.isCompleted && (
                <div className="flex gap-2 mt-4">
                  <Button
                    variant="secondary"
                    icon={<Activity size={16} />}
                    loading={progressMutation.isPending}
                    onClick={() => progressMutation.mutate(releaseId)}
                  >
                    {t('postReleaseReview.progressReview', 'Progress Review')}
                  </Button>
                </div>
              )}
            </CardBody>
          </Card>

          {/* Observation windows */}
          {review.observationWindows && review.observationWindows.length > 0 && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Clock size={16} className="text-muted" />
                  <h3 className="font-medium text-heading">
                    {t('postReleaseReview.observationWindowsTitle', 'Observation Windows')}
                  </h3>
                </div>
              </CardHeader>
              <CardBody>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge text-muted">
                        <th className="text-left pb-2 pr-4">{t('postReleaseReview.phase', 'Phase')}</th>
                        <th className="text-left pb-2 pr-4">{t('postReleaseReview.window', 'Window')}</th>
                        <th className="text-right pb-2 pr-4">{t('postReleaseReview.errorRate', 'Error Rate')}</th>
                        <th className="text-right pb-2 pr-4">{t('postReleaseReview.latency', 'Avg Latency')}</th>
                        <th className="text-right pb-2">{t('postReleaseReview.collected', 'Collected')}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {review.observationWindows.map((w: any) => (
                        <tr key={w.id} className="border-b border-edge/50 hover:bg-surface transition-colors">
                          <td className="py-2 pr-4 font-medium">{w.phase}</td>
                          <td className="py-2 pr-4 text-muted">
                            {new Date(w.startsAt).toLocaleTimeString()} –{' '}
                            {new Date(w.endsAt).toLocaleTimeString()}
                          </td>
                          <td className="py-2 pr-4 text-right">
                            {w.errorRate != null ? `${(w.errorRate * 100).toFixed(2)}%` : '—'}
                          </td>
                          <td className="py-2 pr-4 text-right">
                            {w.avgLatencyMs != null ? `${w.avgLatencyMs}ms` : '—'}
                          </td>
                          <td className="py-2 text-right">
                            {w.isCollected ? (
                              <CheckCircle2 size={14} className="text-success inline" />
                            ) : (
                              <Clock size={14} className="text-warning inline" />
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardBody>
            </Card>
          )}

          {/* Baseline */}
          {review.baseline && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <BarChart2 size={16} className="text-muted" />
                  <h3 className="font-medium text-heading">
                    {t('postReleaseReview.baselineTitle', 'Performance Baseline')}
                  </h3>
                </div>
              </CardHeader>
              <CardBody>
                <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
                  {[
                    { label: 'RPM', value: review.baseline.requestsPerMinute?.toFixed(1) },
                    { label: 'Error Rate', value: `${(review.baseline.errorRate * 100).toFixed(2)}%` },
                    { label: 'Avg Latency', value: `${review.baseline.avgLatencyMs}ms` },
                    { label: 'P95', value: `${review.baseline.p95LatencyMs}ms` },
                    { label: 'P99', value: `${review.baseline.p99LatencyMs}ms` },
                    { label: 'Throughput', value: review.baseline.throughput?.toFixed(1) },
                  ].map((m) => (
                    <div key={m.label} className="bg-surface rounded-lg p-3">
                      <p className="text-xs text-muted">{m.label}</p>
                      <p className="text-sm font-bold text-heading mt-0.5">{m.value}</p>
                    </div>
                  ))}
                </div>
              </CardBody>
            </Card>
          )}
        </div>
      )}
    </PageContainer>
  );
}
