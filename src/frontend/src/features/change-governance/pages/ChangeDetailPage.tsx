import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  GitCommit,
  ExternalLink,
  Users,
  Clock,
  Shield,
  Lightbulb,
  CheckCircle,
  XCircle,
  AlertTriangle,
  HelpCircle,
  History,
  ClipboardCheck,
  Link2,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { changeConfidenceApi } from '../api/changeConfidence';
import type { AdvisoryFactorDto, ChangeAdvisoryResponse, DecisionHistoryItemDto } from '../../../types';

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

/** Mapeia recommendation para variante visual do Badge. */
function recommendationVariant(rec: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  switch (rec) {
    case 'Approve':
      return 'success';
    case 'ApproveConditionally':
      return 'warning';
    case 'Reject':
      return 'danger';
    case 'NeedsMoreEvidence':
      return 'info';
    default:
      return 'default';
  }
}

/** Ícone para status de factor advisory. */
function FactorStatusIcon({ status }: { status: string }) {
  switch (status) {
    case 'Pass':
      return <CheckCircle size={14} className="text-success" />;
    case 'Warning':
      return <AlertTriangle size={14} className="text-warning" />;
    case 'Fail':
      return <XCircle size={14} className="text-critical" />;
    default:
      return <HelpCircle size={14} className="text-muted" />;
  }
}

/**
 * ChangeDetailPage — página de detalhe de uma mudança no módulo Change Confidence.
 *
 * Exibe informações completas da mudança: overview, risco/confiança,
 * blast radius, evidence readiness, advisory, decision actions,
 * decision history, timeline e links para serviços/contratos afetados.
 */
export function ChangeDetailPage() {
  const { changeId } = useParams<{ changeId: string }>();
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  // ── Decision form state ──
  const [selectedDecision, setSelectedDecision] = useState<string>('');
  const [decisionRationale, setDecisionRationale] = useState('');
  const [decisionConditions, setDecisionConditions] = useState('');
  const [decisionSuccess, setDecisionSuccess] = useState(false);
  const [decisionError, setDecisionError] = useState(false);

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

  const advisoryQuery = useQuery({
    queryKey: ['change-advisory', changeId],
    queryFn: () => changeConfidenceApi.getAdvisory(changeId!),
    enabled: !!changeId,
  });

  const historyQuery = useQuery({
    queryKey: ['change-decisions', changeId],
    queryFn: () => changeConfidenceApi.getDecisionHistory(changeId!),
    enabled: !!changeId,
  });

  // ── Decision mutation ──
  const decisionMutation = useMutation({
    mutationFn: () =>
      changeConfidenceApi.recordDecision(changeId!, {
        decision: selectedDecision as 'Approved' | 'Rejected' | 'ApprovedConditionally',
        decidedBy: 'current-user',
        rationale: decisionRationale,
        conditions: decisionConditions || undefined,
      }),
    onSuccess: () => {
      setDecisionSuccess(true);
      setDecisionError(false);
      setSelectedDecision('');
      setDecisionRationale('');
      setDecisionConditions('');
      queryClient.invalidateQueries({ queryKey: ['change-decisions', changeId] });
      queryClient.invalidateQueries({ queryKey: ['change-advisory', changeId] });
      setTimeout(() => setDecisionSuccess(false), 4000);
    },
    onError: () => {
      setDecisionError(true);
      setDecisionSuccess(false);
      setTimeout(() => setDecisionError(false), 4000);
    },
  });

  const change = changeQuery.data;
  const intelligence = intelligenceQuery.data;
  const advisory: ChangeAdvisoryResponse | undefined = advisoryQuery.data;
  const decisions: DecisionHistoryItemDto[] = historyQuery.data?.decisions ?? [];

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

  // Evidence readiness computation from advisory factors
  const evidenceFactors = advisory?.factors ?? [];
  const passCount = evidenceFactors.filter((f: AdvisoryFactorDto) => f.status === 'Pass').length;
  const totalFactors = evidenceFactors.length;
  const evidencePercent = totalFactors > 0 ? Math.round((passCount / totalFactors) * 100) : 0;

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

        {/* ── Evidence Readiness ── */}
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <ClipboardCheck size={16} className="text-success" />
              {t('changeConfidence.detail.evidenceReadiness')}
            </h2>
          </CardHeader>
          <CardBody>
            {advisoryQuery.isLoading ? (
              <p className="text-sm text-muted py-4">{t('common.loading')}</p>
            ) : totalFactors > 0 ? (
              <div className="space-y-4">
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <p className="text-xs text-muted">{t('changeConfidence.detail.evidenceReadinessSubtitle')}</p>
                    <span className="text-sm font-semibold text-heading">{evidencePercent}%</span>
                  </div>
                  <div className="w-full h-3 rounded-full bg-elevated overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all ${evidencePercent === 100 ? 'bg-success' : evidencePercent >= 50 ? 'bg-warning' : 'bg-critical'}`}
                      style={{ width: `${evidencePercent}%` }}
                    />
                  </div>
                  <p className="text-xs text-muted mt-1">
                    {evidencePercent === 100
                      ? t('changeConfidence.detail.evidenceComplete')
                      : evidencePercent > 0
                        ? t('changeConfidence.detail.evidencePartial')
                        : t('changeConfidence.detail.evidenceNone')}
                  </p>
                </div>
                <div className="space-y-2">
                  {evidenceFactors.map((factor: AdvisoryFactorDto) => (
                    <div key={factor.factorName} className="flex items-center gap-2">
                      <FactorStatusIcon status={factor.status} />
                      <span className="text-xs text-body flex-1">
                        {t(`changeConfidence.detail.factorName.${factor.factorName}`) || factor.factorName}
                      </span>
                      <Badge variant={factor.status === 'Pass' ? 'success' : factor.status === 'Warning' ? 'warning' : factor.status === 'Fail' ? 'danger' : 'default'}>
                        {t(`changeConfidence.detail.factorStatus.${factor.status}`) || factor.status}
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted py-4">{t('changeConfidence.detail.evidenceNone')}</p>
            )}
          </CardBody>
        </Card>
      </div>

      {/* ── Advisory / Recommendation ── */}
      <Card>
        <CardHeader>
          <div>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Lightbulb size={16} className="text-accent" />
              {t('changeConfidence.detail.advisory')}
            </h2>
            <p className="text-xs text-muted mt-0.5">{t('changeConfidence.detail.advisorySubtitle')}</p>
          </div>
        </CardHeader>
        <CardBody>
          {advisoryQuery.isLoading ? (
            <p className="text-sm text-muted py-4">{t('common.loading')}</p>
          ) : advisory ? (
            <div className="space-y-4">
              <div className="flex items-center gap-4 flex-wrap">
                <div>
                  <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.recommendation')}</p>
                  <Badge variant={recommendationVariant(advisory.recommendation)}>
                    {t(`changeConfidence.detail.recommendationType.${advisory.recommendation}`) || advisory.recommendation}
                  </Badge>
                </div>
                <div>
                  <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.overallConfidence')}</p>
                  <div className="flex items-center gap-2">
                    <div className="w-20 h-2 rounded-full bg-elevated overflow-hidden">
                      <div
                        className={`h-full rounded-full ${scoreColor(1 - advisory.overallConfidence)}`}
                        style={{ width: `${Math.min(100, advisory.overallConfidence * 100)}%` }}
                      />
                    </div>
                    <span className="text-sm font-semibold text-heading">
                      {(advisory.overallConfidence * 100).toFixed(0)}%
                    </span>
                  </div>
                </div>
              </div>
              <div className="bg-elevated rounded-md p-3 border border-edge">
                <p className="text-xs text-muted mb-1">{t('changeConfidence.detail.rationale')}</p>
                <p className="text-sm text-body">{advisory.rationale}</p>
              </div>
              <div>
                <p className="text-xs text-muted mb-2">{t('changeConfidence.detail.advisoryFactors')}</p>
                <div className="space-y-2">
                  {advisory.factors.map((factor: AdvisoryFactorDto) => (
                    <div key={factor.factorName} className="flex items-start gap-2 p-2 rounded-md bg-elevated">
                      <FactorStatusIcon status={factor.status} />
                      <div className="flex-1 min-w-0">
                        <p className="text-xs font-medium text-heading">
                          {t(`changeConfidence.detail.factorName.${factor.factorName}`) || factor.factorName}
                        </p>
                        <p className="text-xs text-muted">{factor.description}</p>
                      </div>
                      <Badge variant={factor.status === 'Pass' ? 'success' : factor.status === 'Warning' ? 'warning' : factor.status === 'Fail' ? 'danger' : 'default'}>
                        {t(`changeConfidence.detail.factorStatus.${factor.status}`) || factor.status}
                      </Badge>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted py-4">{t('changeConfidence.detail.noAdvisory')}</p>
          )}
        </CardBody>
      </Card>

      {/* ── Decision Actions ── */}
      <Card>
        <CardHeader>
          <div>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              {t('changeConfidence.detail.decisionActions')}
            </h2>
            <p className="text-xs text-muted mt-0.5">{t('changeConfidence.detail.decisionSubtitle')}</p>
          </div>
        </CardHeader>
        <CardBody>
          <div className="space-y-4">
            {/* Decision buttons */}
            <div className="flex flex-wrap gap-3">
              <button
                onClick={() => setSelectedDecision('Approved')}
                className={`inline-flex items-center gap-1.5 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                  selectedDecision === 'Approved'
                    ? 'bg-success text-white'
                    : 'bg-elevated border border-edge text-body hover:text-heading hover:border-success'
                }`}
              >
                <CheckCircle size={14} />
                {t('changeConfidence.detail.approve')}
              </button>
              <button
                onClick={() => setSelectedDecision('Rejected')}
                className={`inline-flex items-center gap-1.5 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                  selectedDecision === 'Rejected'
                    ? 'bg-critical text-white'
                    : 'bg-elevated border border-edge text-body hover:text-heading hover:border-critical'
                }`}
              >
                <XCircle size={14} />
                {t('changeConfidence.detail.reject')}
              </button>
              <button
                onClick={() => setSelectedDecision('ApprovedConditionally')}
                className={`inline-flex items-center gap-1.5 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                  selectedDecision === 'ApprovedConditionally'
                    ? 'bg-warning text-white'
                    : 'bg-elevated border border-edge text-body hover:text-heading hover:border-warning'
                }`}
              >
                <AlertTriangle size={14} />
                {t('changeConfidence.detail.approveConditionally')}
              </button>
            </div>

            {/* Decision form — shown when a decision is selected */}
            {selectedDecision && (
              <div className="space-y-3 p-4 rounded-md bg-elevated border border-edge">
                <div>
                  <label className="text-xs text-muted block mb-1">
                    {t('changeConfidence.detail.decisionRationale')} *
                  </label>
                  <textarea
                    value={decisionRationale}
                    onChange={(e) => setDecisionRationale(e.target.value)}
                    placeholder={t('changeConfidence.detail.decisionRationalePlaceholder')}
                    className="w-full px-3 py-2 rounded-md bg-surface border border-edge text-sm text-heading placeholder:text-muted outline-none focus:border-accent transition-colors resize-y min-h-[80px]"
                    rows={3}
                  />
                </div>

                {selectedDecision === 'ApprovedConditionally' && (
                  <div>
                    <label className="text-xs text-muted block mb-1">
                      {t('changeConfidence.detail.decisionConditions')}
                    </label>
                    <textarea
                      value={decisionConditions}
                      onChange={(e) => setDecisionConditions(e.target.value)}
                      placeholder={t('changeConfidence.detail.decisionConditionsPlaceholder')}
                      className="w-full px-3 py-2 rounded-md bg-surface border border-edge text-sm text-heading placeholder:text-muted outline-none focus:border-accent transition-colors resize-y min-h-[60px]"
                      rows={2}
                    />
                  </div>
                )}

                <div className="flex items-center gap-3">
                  <button
                    onClick={() => decisionMutation.mutate()}
                    disabled={!decisionRationale.trim() || decisionMutation.isPending}
                    className="px-4 py-2 rounded-md bg-accent text-white text-sm font-medium hover:bg-accent/80 disabled:opacity-40 transition-colors"
                  >
                    {decisionMutation.isPending ? t('common.loading') : t('changeConfidence.detail.submitDecision')}
                  </button>
                  <button
                    onClick={() => {
                      setSelectedDecision('');
                      setDecisionRationale('');
                      setDecisionConditions('');
                    }}
                    className="px-4 py-2 rounded-md bg-elevated border border-edge text-sm text-body hover:text-heading transition-colors"
                  >
                    {t('common.cancel')}
                  </button>
                </div>
              </div>
            )}

            {/* Success/Error feedback */}
            {decisionSuccess && (
              <div className="flex items-center gap-2 p-3 rounded-md bg-success/10 border border-success/30">
                <CheckCircle size={14} className="text-success" />
                <p className="text-sm text-success">{t('changeConfidence.detail.decisionSuccess')}</p>
              </div>
            )}
            {decisionError && (
              <div className="flex items-center gap-2 p-3 rounded-md bg-critical/10 border border-critical/30">
                <XCircle size={14} className="text-critical" />
                <p className="text-sm text-critical">{t('changeConfidence.detail.decisionError')}</p>
              </div>
            )}
          </div>
        </CardBody>
      </Card>

      {/* ── Decision History ── */}
      <Card>
        <CardHeader>
          <div>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <History size={16} className="text-info" />
              {t('changeConfidence.detail.decisionHistory')}
            </h2>
            <p className="text-xs text-muted mt-0.5">{t('changeConfidence.detail.decisionHistorySubtitle')}</p>
          </div>
        </CardHeader>
        <CardBody>
          {historyQuery.isLoading ? (
            <p className="text-sm text-muted py-4">{t('common.loading')}</p>
          ) : decisions.length > 0 ? (
            <div className="space-y-3">
              {decisions.map((decision: DecisionHistoryItemDto) => (
                <div key={decision.eventId} className="flex items-start gap-3 border-l-2 border-accent pl-4 py-2">
                  <div className="shrink-0">
                    {decision.eventType.includes('approved') ? (
                      <CheckCircle size={16} className="text-success" />
                    ) : decision.eventType.includes('rejected') ? (
                      <XCircle size={16} className="text-critical" />
                    ) : decision.eventType.includes('conditionally') ? (
                      <AlertTriangle size={16} className="text-warning" />
                    ) : (
                      <Clock size={16} className="text-muted" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <p className="text-sm font-medium text-heading">{decision.eventType}</p>
                      <span className="text-xs text-muted">
                        {new Date(decision.occurredAt).toLocaleString()}
                      </span>
                    </div>
                    <p className="text-xs text-body mt-0.5">{decision.description}</p>
                    <p className="text-xs text-muted mt-0.5">
                      {t('changeConfidence.detail.decidedBy')}: {decision.source}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="py-4 text-center">
              <p className="text-sm text-muted font-medium">{t('changeConfidence.detail.noDecisions')}</p>
              <p className="text-xs text-faded mt-1">{t('changeConfidence.detail.noDecisionsDescription')}</p>
            </div>
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

      {/* ── Affected Services & Contracts ── */}
      <Card>
        <CardHeader>
          <div>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Link2 size={16} className="text-accent" />
              {t('changeConfidence.detail.affectedServices')}
            </h2>
            <p className="text-xs text-muted mt-0.5">{t('changeConfidence.detail.affectedServicesSubtitle')}</p>
          </div>
        </CardHeader>
        <CardBody>
          <div className="space-y-3">
            {change.serviceName ? (
              <div className="flex items-center gap-3 p-3 rounded-md bg-elevated border border-edge">
                <div className="flex-1">
                  <p className="text-sm font-medium text-heading">{change.serviceName}</p>
                  <p className="text-xs text-muted">{change.domain ?? change.teamName ?? '—'}</p>
                </div>
                <Link
                  to={`/catalog/services`}
                  className="inline-flex items-center gap-1 px-3 py-1.5 rounded-md bg-surface border border-edge text-xs text-accent hover:border-accent transition-colors"
                >
                  <ExternalLink size={12} />
                  {t('changeConfidence.detail.viewService')}
                </Link>
              </div>
            ) : (
              <p className="text-sm text-muted py-4">{t('changeConfidence.detail.noAffectedServices')}</p>
            )}

            {/* Blast radius consumers as affected services */}
            {blastRadius?.directConsumers?.length > 0 && (
              <div>
                <p className="text-xs text-muted mb-2">{t('changeConfidence.detail.directConsumers')}</p>
                {blastRadius.directConsumers.map((consumer: string) => (
                  <div key={consumer} className="flex items-center gap-3 p-2 rounded-md bg-elevated border border-edge mb-1.5">
                    <span className="text-sm text-body flex-1">{consumer}</span>
                    <Link
                      to={`/catalog/services`}
                      className="inline-flex items-center gap-1 px-2 py-1 rounded text-xs text-accent hover:underline"
                    >
                      <ExternalLink size={10} />
                      {t('changeConfidence.detail.viewService')}
                    </Link>
                  </div>
                ))}
              </div>
            )}
          </div>
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

// ── Sub-components ───────────────────────────────────────────────────────────

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
