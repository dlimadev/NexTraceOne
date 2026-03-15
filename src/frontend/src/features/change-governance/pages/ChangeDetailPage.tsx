import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  GitCommit,
  ExternalLink,
  Users,
  Clock,
  Shield,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { changeConfidenceApi } from '../api/changeConfidence';

/** Mapeia status de confiança para variante visual do Badge. */
function confidenceVariant(status: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  switch (status) {
    case 'Validated':
      return 'success';
    case 'NeedsAttention':
      return 'warning';
    case 'SuspectedRegression':
    case 'CorrelatedWithIncident':
      return 'danger';
    case 'Mitigated':
      return 'info';
    default:
      return 'default';
  }
}

/** Determina cor da barra de score de mudança. */
function scoreColor(score: number): string {
  if (score < 0.3) return 'bg-success';
  if (score <= 0.6) return 'bg-warning';
  return 'bg-critical';
}

/**
 * ChangeDetailPage — página de detalhe de uma mudança no módulo Change Confidence.
 *
 * Exibe informações completas da mudança: overview, risco/confiança,
 * blast radius, validação, timeline e links rápidos.
 */
export function ChangeDetailPage() {
  const { changeId } = useParams<{ changeId: string }>();
  const { t } = useTranslation();

  // ── Queries ──
  const changeQuery = useQuery({
    queryKey: ['change-detail', changeId],
    queryFn: () => changeConfidenceApi.getChange(changeId!),
    enabled: !!changeId,
  });

  const intelligenceQuery = useQuery({
    queryKey: ['change-intelligence', changeId],
    queryFn: () => changeConfidenceApi.getIntelligence(changeId!),
    enabled: !!changeId,
  });

  const change = changeQuery.data;
  const intelligence = intelligenceQuery.data;

  if (changeQuery.isLoading) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <p className="text-muted">{t('common.loading')}</p>
      </div>
    );
  }

  if (changeQuery.isError || !change) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <p className="text-critical">{t('common.error')}</p>
        <Link to="/changes" className="text-accent hover:underline text-sm mt-2 inline-block">
          {t('changeConfidence.detail.backToCatalog')}
        </Link>
      </div>
    );
  }

  const blastRadius = intelligence?.blastRadius ?? null;
  const timeline = intelligence?.timeline ?? null;
  const validation = intelligence?.validation ?? null;

  return (
    <div className="p-6 lg:p-8 animate-fade-in space-y-6">
      {/* ── Back link ── */}
      <Link to="/changes" className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline">
        <ArrowLeft size={16} />
        {t('changeConfidence.detail.backToCatalog')}
      </Link>

      {/* ── Header ── */}
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-bold text-heading">{change.serviceName}</h1>
        <Badge>{change.version}</Badge>
        <Badge>{change.environment}</Badge>
        <Badge variant={confidenceVariant(change.confidenceStatus)}>
          {t(`changeConfidence.confidenceStatus.${change.confidenceStatus}`) || change.confidenceStatus}
        </Badge>
        <Badge variant={change.deploymentStatus === 'Completed' ? 'success' : 'default'}>
          {change.deploymentStatus}
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* ── Overview ── */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading">{t('changeConfidence.detail.overview')}</h2>
          </CardHeader>
          <CardBody className="space-y-3">
            <DetailRow label={t('changeConfidence.detail.description')} value={change.description ?? '—'} />
            <DetailRow
              label={t('changeConfidence.detail.changeType')}
              value={t(`changeConfidence.changeType.${change.changeType}`) || change.changeType}
            />
            <DetailRow
              label={t('changeConfidence.detail.commitSha')}
              value={
                <span className="font-mono text-xs">{change.commitSha}</span>
              }
            />
            {change.pipelineSource && (
              <DetailRow label={t('changeConfidence.detail.pipelineSource')} value={change.pipelineSource} />
            )}
            {change.workItemReference && (
              <DetailRow
                label={t('changeConfidence.detail.workItem')}
                value={
                  <a
                    href={change.workItemReference}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-accent hover:underline inline-flex items-center gap-1"
                  >
                    {change.workItemReference}
                    <ExternalLink size={12} />
                  </a>
                }
              />
            )}
          </CardBody>
        </Card>

        {/* ── Risk & Confidence ── */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              {t('changeConfidence.detail.riskAndConfidence')}
            </h2>
          </CardHeader>
          <CardBody className="space-y-4">
            {/* Score visual bar */}
            <div>
              <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.changeScore')}</p>
              <div className="flex items-center gap-3">
                <div className="flex-1 h-3 rounded-full bg-elevated overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all ${scoreColor(change.changeScore)}`}
                    style={{ width: `${Math.min(100, change.changeScore * 100)}%` }}
                  />
                </div>
                <span className="text-sm font-semibold text-heading">
                  {(change.changeScore * 100).toFixed(0)}%
                </span>
              </div>
            </div>

            <DetailRow
              label={t('changeConfidence.detail.confidenceStatus')}
              value={
                <Badge variant={confidenceVariant(change.confidenceStatus)}>
                  {t(`changeConfidence.confidenceStatus.${change.confidenceStatus}`) || change.confidenceStatus}
                </Badge>
              }
            />
            <DetailRow
              label={t('changeConfidence.detail.validationStatus')}
              value={change.validationStatus ?? '—'}
            />
          </CardBody>
        </Card>

        {/* ── Blast Radius ── */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Users size={16} className="text-warning" />
              {t('changeConfidence.detail.blastRadius')}
            </h2>
          </CardHeader>
          <CardBody>
            {blastRadius ? (
              <div className="space-y-3">
                <DetailRow
                  label={t('changeConfidence.detail.totalAffected')}
                  value={
                    <span className="font-semibold text-heading">{blastRadius.totalAffected ?? 0}</span>
                  }
                />
                {blastRadius.directConsumers?.length > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.directConsumers')}</p>
                    <div className="flex flex-wrap gap-1.5">
                      {blastRadius.directConsumers.map((c: string) => (
                        <Badge key={c}>{c}</Badge>
                      ))}
                    </div>
                  </div>
                )}
                {blastRadius.transitiveConsumers?.length > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.transitiveConsumers')}</p>
                    <div className="flex flex-wrap gap-1.5">
                      {blastRadius.transitiveConsumers.map((c: string) => (
                        <Badge key={c} variant="info">{c}</Badge>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <p className="text-sm text-muted py-4">{t('changeConfidence.detail.noBlastRadius')}</p>
            )}
          </CardBody>
        </Card>

        {/* ── Validation & Evidence ── */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <GitCommit size={16} className="text-success" />
              {t('changeConfidence.detail.validation')}
            </h2>
          </CardHeader>
          <CardBody>
            {validation ? (
              <div className="space-y-3">
                {validation.baselineMetrics && (
                  <div>
                    <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.baselineMetrics')}</p>
                    <pre className="text-xs bg-elevated rounded-md p-3 overflow-x-auto text-body">
                      {JSON.stringify(validation.baselineMetrics, null, 2)}
                    </pre>
                  </div>
                )}
                {validation.reviewStatus && (
                  <DetailRow label={t('changeConfidence.detail.reviewStatus')} value={validation.reviewStatus} />
                )}
              </div>
            ) : (
              <p className="text-sm text-muted py-4">{t('changeConfidence.detail.noValidation')}</p>
            )}
          </CardBody>
        </Card>
      </div>

      {/* ── Timeline ── */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Clock size={16} className="text-info" />
            {t('changeConfidence.detail.timeline')}
          </h2>
        </CardHeader>
        <CardBody>
          {timeline && Array.isArray(timeline) && timeline.length > 0 ? (
            <div className="space-y-3">
              {timeline.map((event: { timestamp: string; eventType: string; description?: string }, idx: number) => (
                <div key={idx} className="flex items-start gap-3 border-l-2 border-edge pl-4 py-1">
                  <div className="shrink-0 text-xs text-muted whitespace-nowrap">
                    {new Date(event.timestamp).toLocaleString()}
                  </div>
                  <div>
                    <p className="text-sm font-medium text-heading">{event.eventType}</p>
                    {event.description && <p className="text-xs text-muted">{event.description}</p>}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted py-4">{t('changeConfidence.detail.noTimeline')}</p>
          )}
        </CardBody>
      </Card>

      {/* ── Quick Links ── */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading">{t('changeConfidence.detail.quickLinks')}</h2>
        </CardHeader>
        <CardBody>
          <div className="flex flex-wrap gap-3">
            <Link
              to="/changes"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-elevated border border-edge text-sm text-body hover:text-heading hover:border-accent transition-colors"
            >
              <ArrowLeft size={14} />
              {t('changeConfidence.detail.backToCatalog')}
            </Link>
            {change.serviceName && (
              <Link
                to={`/source-of-truth`}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-elevated border border-edge text-sm text-body hover:text-heading hover:border-accent transition-colors"
              >
                <ExternalLink size={14} />
                {t('changeConfidence.detail.viewServiceSot')}
              </Link>
            )}
            <Link
              to="/releases"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-elevated border border-edge text-sm text-body hover:text-heading hover:border-accent transition-colors"
            >
              <ExternalLink size={14} />
              {t('changeConfidence.detail.viewInReleases')}
            </Link>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}

// ── Sub-component ────────────────────────────────────────────────────────────

interface DetailRowProps {
  label: string;
  value: React.ReactNode;
}

/** Linha label/value reutilizável no detalhe da mudança. */
function DetailRow({ label, value }: DetailRowProps) {
  return (
    <div className="flex items-start gap-2">
      <span className="text-xs text-muted min-w-[120px] shrink-0 pt-0.5">{label}</span>
      <span className="text-sm text-body">{value}</span>
    </div>
  );
}
