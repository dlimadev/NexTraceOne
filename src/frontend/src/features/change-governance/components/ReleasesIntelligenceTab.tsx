import { RefreshCw, ShieldAlert, Target, Gauge, Undo2, Tag, LineChart, CheckCircle2, XCircle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import type { IntelligenceSummary } from '../api/changeIntelligence';
import type { DeploymentState } from '../../../types';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function stateVariant(state: DeploymentState): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (state === 'Succeeded') return 'success';
  if (state === 'Failed' || state === 'RolledBack') return 'danger';
  if (state === 'Running') return 'info';
  return 'default';
}

function riskLevel(score: number): 'low' | 'medium' | 'high' {
  if (score < 0.4) return 'low';
  if (score < 0.7) return 'medium';
  return 'high';
}

function riskVariant(score: number): 'success' | 'warning' | 'danger' {
  if (score < 0.4) return 'success';
  if (score < 0.7) return 'warning';
  return 'danger';
}

// ─── Props ───────────────────────────────────────────────────────────────────

interface ReleasesIntelligenceTabProps {
  intel: IntelligenceSummary | undefined;
  selectedReleaseId: string | null;
  isLoading: boolean;
  isError: boolean;
  onStartReview: (releaseId: string) => void;
  startReviewPending: boolean;
}

// ─── Component ───────────────────────────────────────────────────────────────

/**
 * Conteúdo da aba Intelligence na ReleasesPage.
 * Exibe score de risco, blast radius, markers externos, baseline,
 * revisão pós-release e avaliação de rollback para um release selecionado.
 */
export function ReleasesIntelligenceTab({
  intel,
  selectedReleaseId,
  isLoading,
  isError,
  onStartReview,
  startReviewPending,
}: ReleasesIntelligenceTabProps) {
  const { t } = useTranslation();

  if (!selectedReleaseId) {
    return (
      <Card>
        <CardBody>
          <p className="py-12 text-sm text-muted text-center">
            {t('releases.intelligence.selectRelease')}
          </p>
        </CardBody>
      </Card>
    );
  }

  if (isLoading) return <PageLoadingState />;

  if (isError) {
    return (
      <Card>
        <CardBody>
          <PageErrorState message={t('releases.loadFailed')} />
        </CardBody>
      </Card>
    );
  }

  if (!intel) return null;

  return (
    <div className="space-y-6">
      {/* Intelligence Header */}
      <div className="flex items-center gap-3 text-sm text-muted">
        <span className="font-mono text-heading">{intel.release.serviceName}</span>
        <span>v{intel.release.version}</span>
        <Badge variant="info">{intel.release.environment}</Badge>
        <Badge variant={stateVariant(intel.release.status as DeploymentState)}>
          {intel.release.status}
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* ── Score & Risk ──────────────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <ShieldAlert size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('releases.intelligence.score.title')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            {intel.score ? (
              <div className="space-y-4">
                <div className="flex items-center gap-4">
                  <div className="text-3xl font-bold text-heading">
                    {(intel.score.score * 100).toFixed(0)}%
                  </div>
                  <Badge variant={riskVariant(intel.score.score)}>
                    {t(`releases.intelligence.score.${riskLevel(intel.score.score)}`)}
                  </Badge>
                </div>
                <div className="h-2 rounded-full bg-elevated overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all ${
                      intel.score.score < 0.4
                        ? 'bg-success'
                        : intel.score.score < 0.7
                          ? 'bg-warning'
                          : 'bg-critical'
                    }`}
                    style={{ width: `${intel.score.score * 100}%` }}
                  />
                </div>
                <div className="grid grid-cols-3 gap-3 text-xs">
                  <div>
                    <span className="text-muted">{t('releases.intelligence.score.breakingChange')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {(intel.score.breakingChangeWeight * 100).toFixed(0)}%
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.score.blastRadius')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {(intel.score.blastRadiusWeight * 100).toFixed(0)}%
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.score.environment')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {(intel.score.environmentWeight * 100).toFixed(0)}%
                    </p>
                  </div>
                </div>
                <p className="text-xs text-muted">
                  {t('releases.intelligence.score.computedAt')}:{' '}
                  {new Date(intel.score.computedAt).toLocaleString()}
                </p>
              </div>
            ) : (
              <p className="py-4 text-sm text-muted text-center">—</p>
            )}
          </CardBody>
        </Card>

        {/* ── Blast Radius ──────────────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Target size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('releases.intelligence.blastRadius.title')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            {intel.blastRadius ? (
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <span className="text-3xl font-bold text-heading">
                    {intel.blastRadius.totalAffectedConsumers}
                  </span>
                  <span className="text-sm text-muted">
                    {t('releases.intelligence.blastRadius.totalAffected')}
                  </span>
                </div>
                <div className="space-y-2">
                  <div>
                    <p className="text-xs font-medium text-muted mb-1">
                      {t('releases.intelligence.blastRadius.direct')} ({intel.blastRadius.directConsumers.length})
                    </p>
                    <div className="flex flex-wrap gap-1">
                      {intel.blastRadius.directConsumers.map((c) => (
                        <Badge key={c} variant="danger">{c}</Badge>
                      ))}
                      {intel.blastRadius.directConsumers.length === 0 && (
                        <span className="text-xs text-muted">—</span>
                      )}
                    </div>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-muted mb-1">
                      {t('releases.intelligence.blastRadius.transitive')} ({intel.blastRadius.transitiveConsumers.length})
                    </p>
                    <div className="flex flex-wrap gap-1">
                      {intel.blastRadius.transitiveConsumers.map((c) => (
                        <Badge key={c} variant="warning">{c}</Badge>
                      ))}
                      {intel.blastRadius.transitiveConsumers.length === 0 && (
                        <span className="text-xs text-muted">—</span>
                      )}
                    </div>
                  </div>
                </div>
                <p className="text-xs text-muted">
                  {t('releases.intelligence.blastRadius.calculatedAt')}:{' '}
                  {new Date(intel.blastRadius.calculatedAt).toLocaleString()}
                </p>
              </div>
            ) : (
              <p className="py-4 text-sm text-muted text-center">—</p>
            )}
          </CardBody>
        </Card>

        {/* ── External Markers ──────────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Tag size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('releases.intelligence.markers.title')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            {intel.markers.length === 0 ? (
              <p className="py-4 text-sm text-muted text-center">
                {t('releases.intelligence.markers.noMarkers')}
              </p>
            ) : (
              <div className="divide-y divide-edge -mx-6">
                {intel.markers.map((m) => (
                  <div key={m.id} className="px-6 py-3 flex items-center justify-between text-xs">
                    <div className="space-y-0.5">
                      <p className="text-heading font-medium">{m.markerType}</p>
                      <p className="text-muted">
                        {t('releases.intelligence.markers.source')}: {m.sourceSystem}
                      </p>
                    </div>
                    <div className="text-right space-y-0.5">
                      <p className="text-body font-mono">{m.externalId}</p>
                      <p className="text-muted">
                        {new Date(m.occurredAt).toLocaleString()}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>

        {/* ── Baseline ──────────────────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <LineChart size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('releases.intelligence.baseline.title')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            {intel.baseline ? (
              <div className="space-y-3">
                <div className="grid grid-cols-3 gap-3 text-xs">
                  <div>
                    <span className="text-muted">{t('releases.intelligence.baseline.requestsPerMinute')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.baseline.requestsPerMinute.toLocaleString()}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.baseline.errorRate')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {(intel.baseline.errorRate * 100).toFixed(2)}%
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.baseline.throughput')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.baseline.throughput.toLocaleString()}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.baseline.avgLatency')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.baseline.avgLatencyMs.toFixed(1)}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.baseline.p95Latency')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.baseline.p95LatencyMs.toFixed(1)}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.baseline.p99Latency')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.baseline.p99LatencyMs.toFixed(1)}
                    </p>
                  </div>
                </div>
                <p className="text-xs text-muted">
                  {t('releases.intelligence.baseline.period')}:{' '}
                  {new Date(intel.baseline.collectedFrom).toLocaleDateString()} –{' '}
                  {new Date(intel.baseline.collectedTo).toLocaleDateString()}
                </p>
              </div>
            ) : (
              <p className="py-4 text-sm text-muted text-center">
                {t('releases.intelligence.baseline.noBaseline')}
              </p>
            )}
          </CardBody>
        </Card>

        {/* ── Post-Release Review ──────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Gauge size={16} className="text-accent" />
                <h3 className="text-sm font-semibold text-heading">
                  {t('releases.intelligence.review.title')}
                </h3>
              </div>
              {!intel.postReleaseReview && selectedReleaseId && (
                <Button
                  size="sm"
                  variant="secondary"
                  loading={startReviewPending}
                  onClick={() => onStartReview(selectedReleaseId)}
                >
                  {t('releases.intelligence.review.startReview')}
                </Button>
              )}
            </div>
          </CardHeader>
          <CardBody>
            {intel.postReleaseReview ? (
              <div className="space-y-3">
                <div className="flex items-center gap-3">
                  {intel.postReleaseReview.isCompleted ? (
                    <CheckCircle2 size={18} className="text-success" />
                  ) : (
                    <RefreshCw size={18} className="text-info animate-spin" />
                  )}
                  <span className="text-sm text-heading font-medium">
                    {intel.postReleaseReview.isCompleted
                      ? t('releases.intelligence.review.completed')
                      : t('releases.intelligence.review.inProgress')}
                  </span>
                </div>
                <div className="grid grid-cols-2 gap-3 text-xs">
                  <div>
                    <span className="text-muted">{t('releases.intelligence.review.phase')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.postReleaseReview.currentPhase}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.review.outcome')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.postReleaseReview.outcome}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.review.confidence')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {(intel.postReleaseReview.confidenceScore * 100).toFixed(0)}%
                    </p>
                  </div>
                </div>
                {intel.postReleaseReview.summary && (
                  <p className="text-xs text-body bg-elevated rounded p-2">
                    {intel.postReleaseReview.summary}
                  </p>
                )}
              </div>
            ) : (
              <p className="py-4 text-sm text-muted text-center">
                {t('releases.intelligence.review.noReview')}
              </p>
            )}
          </CardBody>
        </Card>

        {/* ── Rollback Assessment ──────────────────────────────── */}
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Undo2 size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('releases.intelligence.rollback.title')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            {intel.rollbackAssessment ? (
              <div className="space-y-3">
                <div className="flex items-center gap-3">
                  {intel.rollbackAssessment.isViable ? (
                    <Badge variant="success">
                      <CheckCircle2 size={12} className="mr-1" />
                      {t('releases.intelligence.rollback.viable')}
                    </Badge>
                  ) : (
                    <Badge variant="danger">
                      <XCircle size={12} className="mr-1" />
                      {t('releases.intelligence.rollback.notViable')}
                    </Badge>
                  )}
                </div>
                <div className="grid grid-cols-2 gap-3 text-xs">
                  <div>
                    <span className="text-muted">{t('releases.intelligence.rollback.readiness')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {(intel.rollbackAssessment.readinessScore * 100).toFixed(0)}%
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.rollback.previousVersion')}</span>
                    <p className="text-heading font-medium font-mono mt-0.5">
                      {intel.rollbackAssessment.previousVersion ?? '—'}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.rollback.reversibleMigrations')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.rollbackAssessment.hasReversibleMigrations ? '✓' : '✗'}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('releases.intelligence.rollback.consumersMigrated')}</span>
                    <p className="text-heading font-medium mt-0.5">
                      {intel.rollbackAssessment.consumersAlreadyMigrated} / {intel.rollbackAssessment.totalConsumersImpacted}
                    </p>
                  </div>
                </div>
                <div className="text-xs">
                  <span className="text-muted">{t('releases.intelligence.rollback.recommendation')}</span>
                  <p className="text-body mt-0.5 bg-elevated rounded p-2">
                    {intel.rollbackAssessment.recommendation}
                  </p>
                </div>
              </div>
            ) : (
              <p className="py-4 text-sm text-muted text-center">
                {t('releases.intelligence.rollback.noAssessment')}
              </p>
            )}
          </CardBody>
        </Card>
      </div>
    </div>
  );
}
