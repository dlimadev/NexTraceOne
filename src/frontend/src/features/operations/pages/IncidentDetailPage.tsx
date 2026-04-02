import * as React from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, NavLink } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle, ArrowLeft, ShieldAlert, AlertCircle, Eye,
  CheckCircle, XCircle, Clock, Search, Wrench,
  GitBranch, FileText, BookOpen, Shield, Activity,
  ExternalLink,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { incidentsApi, type IncidentDetailResponse, type DynamicCorrelatedChange } from '../api/incidents';
import { AssistantPanel } from '../../ai-hub/components/AssistantPanel';
import { KnowledgeContextPanel } from '../../knowledge/components/KnowledgeContextPanel';
import { PageContainer, PageSection } from '../../../components/shell';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Helpers ──────────────────────────────────────────────────────────

const severityBadge = (severity: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info'; icon: React.ReactNode } => {
  switch (severity) {
    case 'Critical': return { variant: 'danger', icon: <ShieldAlert size={14} /> };
    case 'Major': return { variant: 'warning', icon: <AlertCircle size={14} /> };
    case 'Minor': return { variant: 'info', icon: <AlertTriangle size={14} /> };
    default: return { variant: 'default', icon: <Eye size={14} /> };
  }
};

const statusBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info'; icon: React.ReactNode } => {
  switch (status) {
    case 'Open': return { variant: 'danger', icon: <AlertCircle size={14} /> };
    case 'Investigating': return { variant: 'warning', icon: <Search size={14} /> };
    case 'Mitigating': return { variant: 'warning', icon: <Wrench size={14} /> };
    case 'Monitoring': return { variant: 'info', icon: <Eye size={14} /> };
    case 'Resolved': return { variant: 'success', icon: <CheckCircle size={14} /> };
    case 'Closed': return { variant: 'default', icon: <XCircle size={14} /> };
    default: return { variant: 'default', icon: <Clock size={14} /> };
  }
};

const confidenceBadge = (confidence: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info' } => {
  switch (confidence) {
    case 'Confirmed': return { variant: 'success' };
    case 'High': return { variant: 'success' };
    case 'Medium': return { variant: 'warning' };
    case 'Low': return { variant: 'default' };
    default: return { variant: 'default' };
  }
};

const mitigationBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info' } => {
  switch (status) {
    case 'Verified': return { variant: 'success' };
    case 'Applied': return { variant: 'info' };
    case 'InProgress': return { variant: 'warning' };
    case 'Failed': return { variant: 'danger' };
    default: return { variant: 'default' };
  }
};

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

function formatChangeToIncidentDelta(changeAt: string, incidentAt: string, t: (key: string, options?: Record<string, unknown>) => string): string {
  const deltaMs = new Date(incidentAt).getTime() - new Date(changeAt).getTime();
  const deltaMinutes = Math.round(deltaMs / (1000 * 60));
  if (deltaMinutes >= 0) {
    return t('incidents.postChangeVerification.afterIncident', { minutes: deltaMinutes });
  }
  return t('incidents.postChangeVerification.beforeIncident', { minutes: Math.abs(deltaMinutes) });
}

/**
 * Página de detalhe do incidente — visão consolidada com correlação, evidência,
 * mitigação, runbooks, timeline, serviços vinculados e contratos relacionados.
 */
export function IncidentDetailPage() {
  const { t } = useTranslation();
  const { incidentId } = useParams<{ incidentId: string }>();
  const queryClient = useQueryClient();
  const { activeEnvironment } = useEnvironment();
  const [refreshFeedback, setRefreshFeedback] = useState<string | null>(null);

  const detailQuery = useQuery({
    queryKey: ['incident-detail', incidentId],
    queryFn: () => incidentsApi.getIncidentDetail(incidentId!),
    enabled: !!incidentId,
  });

  const correlatedChangesQuery = useQuery({
    queryKey: ['incident-correlated-changes', incidentId],
    queryFn: () => incidentsApi.getCorrelatedChanges(incidentId!),
    enabled: !!incidentId,
  });

  const mitigationHistoryQuery = useQuery({
    queryKey: ['incident-mitigation-history', incidentId],
    queryFn: () => incidentsApi.getMitigationHistory(incidentId!),
    enabled: !!incidentId,
  });

  const latestWorkflowId = useMemo(() => {
    const entries = mitigationHistoryQuery.data?.entries ?? [];
    const firstWithWorkflow = entries.find((entry) => !!entry.workflowId);
    return firstWithWorkflow?.workflowId;
  }, [mitigationHistoryQuery.data?.entries]);

  const mitigationValidationQuery = useQuery({
    queryKey: ['incident-mitigation-validation', incidentId, latestWorkflowId],
    queryFn: () => incidentsApi.getMitigationValidation(incidentId!, latestWorkflowId!),
    enabled: !!incidentId && !!latestWorkflowId,
  });

  const triggerCorrelationMutation = useMutation({
    mutationFn: async () => incidentsApi.triggerCorrelation(incidentId!),
    onSuccess: (data) => {
      setRefreshFeedback(
        t('incidents.correlation.triggeredMessage', { count: data.newCorrelations }),
      );
      queryClient.invalidateQueries({ queryKey: ['incident-correlated-changes', incidentId] });
      queryClient.invalidateQueries({ queryKey: ['incident-detail', incidentId] });
      queryClient.invalidateQueries({ queryKey: ['incidents'] });
      queryClient.invalidateQueries({ queryKey: ['incidents-summary'] });
    },
    onError: (error: unknown) => {
      const message = error instanceof Error ? error.message : t('common.errorLoading');
      setRefreshFeedback(message);
    },
  });

  const refreshCorrelationMutation = useMutation({
    mutationFn: async () => incidentsApi.refreshIncidentCorrelation(incidentId!),
    onSuccess: (data) => {
      setRefreshFeedback(
        t('incidents.correlation.refreshedMessage', 'Correlation refreshed. Score: {{score}}', { score: data.score.toFixed(0) }),
      );
      queryClient.invalidateQueries({ queryKey: ['incident-detail', incidentId] });
      queryClient.invalidateQueries({ queryKey: ['incidents'] });
      queryClient.invalidateQueries({ queryKey: ['incidents-summary'] });
    },
    onError: (error: unknown) => {
      const message = error instanceof Error ? error.message : t('common.errorLoading');
      setRefreshFeedback(message);
    },
  });

  // Loading state
  if (detailQuery.isLoading) {
    return (
      <PageContainer>
        <NavLink to="/operations/incidents" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
          <ArrowLeft size={14} /> {t('incidents.detail.backToList')}
        </NavLink>
        <Card>
          <CardBody>
            <PageLoadingState />
          </CardBody>
        </Card>
      </PageContainer>
    );
  }

  // Error state
  if (detailQuery.isError) {
    return (
      <PageContainer>
        <NavLink to="/operations/incidents" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
          <ArrowLeft size={14} /> {t('incidents.detail.backToList')}
        </NavLink>
        <Card>
          <CardBody>
            <PageErrorState
              title={t('incidents.detail.notFound')}
              message={t('incidents.detail.notFoundDescription')}
            />
          </CardBody>
        </Card>
      </PageContainer>
    );
  }

  const detail: IncidentDetailResponse | undefined = detailQuery.data;

  if (!detail) {
    return (
      <PageContainer>
        <NavLink to="/operations/incidents" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
          <ArrowLeft size={14} /> {t('incidents.detail.backToList')}
        </NavLink>
        <Card>
          <CardBody>
            <PageErrorState
              title={t('incidents.detail.notFound')}
              message={t('incidents.detail.notFoundDescription')}
            />
          </CardBody>
        </Card>
      </PageContainer>
    );
  }

  const { identity, linkedServices, ownerTeam, impactedDomain, impactedEnvironment, timeline, correlation, evidence, relatedContracts, runbooks, mitigation } = detail;
  const sevBadge = severityBadge(identity.severity);
  const stsBadge = statusBadge(identity.status);
  const confBadge = confidenceBadge(correlation.confidence);
  const mitBadge = mitigationBadge(mitigation.status);
  const temporalCorrelations = [...correlation.relatedChanges]
    .filter((change) => !!change.deployedAt)
    .sort((a, b) => new Date(b.deployedAt).getTime() - new Date(a.deployedAt).getTime());

  return (
    <PageContainer className="animate-fade-in">
      {/* Back link */}
      <NavLink to="/operations/incidents" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
        <ArrowLeft size={14} /> {t('incidents.detail.backToList')}
      </NavLink>

      {/* Header */}
      <div className="mb-6">
        <div className="flex flex-wrap items-center gap-3 mb-2">
          <span className="text-xs font-mono text-muted">{identity.reference}</span>
          <Badge variant={sevBadge.variant} className="flex items-center gap-1">
            {sevBadge.icon} {t(`incidents.severity.${identity.severity}`)}
          </Badge>
          <Badge variant={stsBadge.variant} className="flex items-center gap-1">
            {stsBadge.icon} {t(`incidents.status.${identity.status}`)}
          </Badge>
          <Badge variant={confBadge.variant} className="flex items-center gap-1">
            <GitBranch size={12} /> {t(`incidents.correlation.confidenceLevel.${correlation.confidence}`)}
          </Badge>
          <Badge variant={mitBadge.variant} className="flex items-center gap-1">
            <Wrench size={12} /> {t(`incidents.mitigation.mitigationStatus.${mitigation.status}`)}
          </Badge>
        </div>
        <h1 className="text-2xl font-bold text-heading">{identity.title}</h1>
        <p className="text-muted mt-1">{identity.summary}</p>
        <div className="flex flex-wrap gap-4 mt-2 text-xs text-muted">
          <span>{t(`incidents.type.${identity.incidentType}`)}</span>
          <span>•</span>
          <span>{ownerTeam}</span>
          <span>•</span>
          <span>{impactedDomain} / {impactedEnvironment}</span>
          <span>•</span>
          <span>{formatDate(identity.createdAt)}</span>
        </div>
      </div>

      <PageSection>
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left column */}
        <div className="space-y-6">
          {/* Timeline */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Clock size={16} className="text-accent" aria-hidden="true" /> {t('incidents.detail.timeline')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-3">
                {timeline.map((entry, idx) => (
                  <div key={idx} className="flex gap-3">
                    <div className="flex flex-col items-center">
                      <div className={`w-2 h-2 rounded-full mt-1.5 ${idx === 0 ? 'bg-accent' : 'bg-edge-strong'}`} />
                      {idx < timeline.length - 1 && <div className="w-px flex-1 bg-edge" />}
                    </div>
                    <div className="pb-3">
                      <p className="text-xs text-muted">{formatDate(entry.timestamp)}</p>
                      <p className="text-sm text-body">{entry.description}</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>

          {/* Correlation */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between gap-2">
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <GitBranch size={16} className="text-accent" aria-hidden="true" /> {t('incidents.correlation.title')}
                </h2>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => {
                      setRefreshFeedback(null);
                      triggerCorrelationMutation.mutate();
                    }}
                    disabled={triggerCorrelationMutation.isPending || refreshCorrelationMutation.isPending}
                    className="px-2.5 py-1.5 text-xs rounded-md border border-accent/30 text-accent hover:bg-accent/10 disabled:opacity-60"
                  >
                    {triggerCorrelationMutation.isPending
                      ? t('common.loading', 'Loading...')
                      : t('incidents.correlation.runEngineAction', 'Run correlation engine')}
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setRefreshFeedback(null);
                      refreshCorrelationMutation.mutate();
                    }}
                    disabled={refreshCorrelationMutation.isPending || triggerCorrelationMutation.isPending}
                    className="px-2.5 py-1.5 text-xs rounded-md border border-edge text-muted hover:text-body disabled:opacity-60"
                  >
                    {refreshCorrelationMutation.isPending
                      ? t('common.loading', 'Loading...')
                      : t('incidents.correlation.refreshAction', 'Refresh correlation')}
                  </button>
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                {refreshFeedback && (
                  <p className="text-xs text-muted">{refreshFeedback}</p>
                )}
                <div>
                  <p className="text-xs text-muted mb-1">{t('incidents.correlation.confidence')}</p>
                  <Badge variant={confBadge.variant}>
                    {t(`incidents.correlation.confidenceLevel.${correlation.confidence}`)}
                  </Badge>
                </div>
                <div>
                  <p className="text-xs text-muted mb-1">{t('incidents.correlation.reason')}</p>
                  <p className="text-sm text-body">{correlation.reason}</p>
                </div>
                <div>
                  <p className="text-xs text-muted mb-2">{t('incidents.correlation.relatedChanges')}</p>
                  {correlation.relatedChanges.length > 0 ? (
                    correlation.relatedChanges.map((change, idx) => (
                      <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated text-sm">
                        <GitBranch size={14} className="text-warning shrink-0" aria-hidden="true" />
                        <span className="text-body">{change.description}</span>
                        <Badge variant="warning" className="text-[10px] ml-auto shrink-0">{change.confidenceStatus}</Badge>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-muted">{t('incidents.correlation.noRelatedChanges', 'No correlated changes were confirmed for this incident yet.')}</p>
                  )}
                </div>
                {temporalCorrelations.length > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-2">{t('incidents.postChangeVerification.title')}</p>
                    <div className="space-y-2">
                      {temporalCorrelations.map((change, idx) => (
                        <div key={`${change.changeId}-${idx}`} className="p-2 rounded bg-elevated">
                          <p className="text-sm text-body">{change.description}</p>
                          <p className="text-xs text-muted mt-1">
                            {formatDate(change.deployedAt)} • {formatChangeToIncidentDelta(change.deployedAt, identity.createdAt, t)}
                          </p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
                {/* Dynamic correlation results from P01.1 engine */}
                {correlatedChangesQuery.data && correlatedChangesQuery.data.totalCorrelations > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-2">
                      {t('incidents.correlation.dynamicCorrelations', 'Dynamic correlations ({{count}})', { count: correlatedChangesQuery.data.totalCorrelations })}
                    </p>
                    {correlatedChangesQuery.data.correlations.map((c: DynamicCorrelatedChange) => (
                      <div key={c.changeId} className="flex items-center gap-2 p-2 rounded bg-elevated text-sm mb-1">
                        <GitBranch size={14} className="text-accent shrink-0" aria-hidden="true" />
                        <span className="text-body truncate">{c.description || c.serviceName}</span>
                        <Badge
                          variant={c.confidenceLevel === 'High' ? 'danger' : c.confidenceLevel === 'Medium' ? 'warning' : 'default'}
                          className="text-[10px] ml-auto shrink-0"
                        >
                          {c.confidenceLevel}
                        </Badge>
                      </div>
                    ))}
                  </div>
                )}
                <div>
                  <p className="text-xs text-muted mb-2">{t('incidents.correlation.relatedServices')}</p>
                  {correlation.relatedServices.length > 0 ? (
                    correlation.relatedServices.map((svc, idx) => (
                      <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated text-sm">
                        <Activity size={14} className="text-accent shrink-0" aria-hidden="true" />
                        <NavLink to={`/services/${svc.serviceId}`} className="text-accent hover:underline">{svc.displayName}</NavLink>
                        <span className="text-muted text-xs ml-auto">{svc.impactDescription}</span>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-muted">{t('incidents.correlation.noRelatedServices', 'No impacted services beyond the primary context are currently linked.')}</p>
                  )}
                </div>
               </div>
             </CardBody>
           </Card>

          {/* Evidence */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Shield size={16} className="text-accent" aria-hidden="true" /> {t('incidents.evidence.title')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                <div>
                  <p className="text-xs text-muted mb-1">{t('incidents.evidence.operationalSignals')}</p>
                  <p className="text-sm text-body font-mono bg-elevated rounded p-2">{evidence.operationalSignalsSummary}</p>
                </div>
                <div>
                  <p className="text-xs text-muted mb-1">{t('incidents.evidence.degradationSummary')}</p>
                  <p className="text-sm text-body">{evidence.degradationSummary}</p>
                </div>
                {evidence.observations.length > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-2">{t('incidents.evidence.observations')}</p>
                    <div className="space-y-2">
                      {evidence.observations.map((obs, idx) => (
                        <div key={idx} className="p-2 rounded bg-elevated">
                          <p className="text-xs font-semibold text-heading">{obs.title}</p>
                          <p className="text-xs text-muted">{obs.description}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </CardBody>
          </Card>
        </div>

        {/* Right column */}
        <div className="space-y-6">
          {/* Impacted Services */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Activity size={16} className="text-accent" aria-hidden="true" /> {t('incidents.detail.impactedServices')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-2">
                {linkedServices.length > 0 ? linkedServices.map(svc => (
                  <NavLink
                    key={svc.serviceId}
                    to={`/services/${svc.serviceId}`}
                    className="flex items-center justify-between p-2 rounded bg-elevated hover:bg-hover transition-colors"
                  >
                    <div>
                      <p className="text-sm font-medium text-heading">{svc.displayName}</p>
                      <p className="text-xs text-muted">{svc.serviceId} · {svc.serviceType}</p>
                    </div>
                    <Badge variant={svc.criticality === 'Critical' ? 'danger' : 'default'} className="text-[10px]">
                      {svc.criticality}
                    </Badge>
                  </NavLink>
                )) : (
                  <p className="text-sm text-muted">{t('incidents.detail.noLinkedServices', 'No linked services are currently persisted for this incident.')}</p>
                )}
               </div>
             </CardBody>
           </Card>

          {/* Mitigation */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Wrench size={16} className="text-accent" aria-hidden="true" /> {t('incidents.mitigation.title')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                <div>
                  <p className="text-xs text-muted mb-1">{t('incidents.mitigation.status')}</p>
                  <Badge variant={mitBadge.variant}>
                    {t(`incidents.mitigation.mitigationStatus.${mitigation.status}`)}
                  </Badge>
                </div>
                {mitigation.actions.length > 0 ? (
                  <div>
                    <p className="text-xs text-muted mb-2">{t('incidents.mitigation.suggestedActions')}</p>
                    <div className="space-y-2">
                      {mitigation.actions.map((action, idx) => (
                        <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated">
                          {action.completed
                            ? <CheckCircle size={14} className="text-success shrink-0" aria-hidden="true" />
                            : <Clock size={14} className="text-warning shrink-0" aria-hidden="true" />
                          }
                          <span className="text-sm text-body flex-1">{action.description}</span>
                          <span className="text-xs text-muted">{action.status}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <p className="text-sm text-muted">{t('incidents.mitigation.noActions')}</p>
                )}
                {mitigation.rollbackRelevant && mitigation.rollbackGuidance && (
                  <div>
                    <p className="text-xs text-muted mb-1">{t('incidents.mitigation.rollbackGuidance')}</p>
                    <p className="text-sm text-body">{mitigation.rollbackGuidance}</p>
                  </div>
                )}
                {mitigation.escalationGuidance && (
                  <div>
                    <p className="text-xs text-muted mb-1">{t('incidents.mitigation.escalationGuidance')}</p>
                    <p className="text-sm text-body">{mitigation.escalationGuidance}</p>
                  </div>
                )}
              </div>
            </CardBody>
          </Card>

          {/* Post-change verification */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Shield size={16} className="text-accent" aria-hidden="true" /> {t('incidents.postChangeVerification.title')}
              </h2>
            </CardHeader>
            <CardBody>
              {!latestWorkflowId ? (
                <p className="text-sm text-muted">{t('incidents.postChangeVerification.noWorkflow')}</p>
              ) : mitigationValidationQuery.isLoading ? (
                <p className="text-sm text-muted">{t('common.loading')}</p>
              ) : mitigationValidationQuery.data ? (
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-muted">{t('incidents.postChangeVerification.validationStatus')}</span>
                    <Badge variant={mitigationValidationQuery.data.status === 'Verified' ? 'success' : 'warning'}>
                      {mitigationValidationQuery.data.status}
                    </Badge>
                  </div>
                  <div className="text-xs text-muted">
                    {mitigationValidationQuery.data.validatedAt
                      ? `${t('incidents.postChangeVerification.lastValidatedAt')}: ${formatDate(mitigationValidationQuery.data.validatedAt)}`
                      : t('incidents.postChangeVerification.notValidatedYet')}
                  </div>
                  {mitigationValidationQuery.data.expectedChecks.length > 0 && (
                    <div className="space-y-1">
                      {mitigationValidationQuery.data.expectedChecks.map((check, idx) => (
                        <div key={`${check.checkName}-${idx}`} className="flex items-center justify-between text-sm p-2 rounded bg-elevated">
                          <span className="text-body">{check.checkName}</span>
                          <Badge variant={check.isPassed ? 'success' : 'danger'}>{check.isPassed ? t('common.yes') : t('common.no')}</Badge>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ) : (
                <p className="text-sm text-muted">{t('incidents.postChangeVerification.noValidation')}</p>
              )}
            </CardBody>
          </Card>

          {/* Runbooks */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <BookOpen size={16} className="text-accent" aria-hidden="true" /> {t('incidents.runbooks.title')}
              </h2>
            </CardHeader>
            <CardBody>
              {runbooks.length > 0 ? (
                <div className="space-y-2">
                  {runbooks.map((rb, idx) => (
                    <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated">
                      <FileText size={14} className="text-accent shrink-0" aria-hidden="true" />
                      {rb.url ? (
                        <a href={rb.url} target="_blank" rel="noopener noreferrer" className="text-sm text-accent hover:underline flex items-center gap-1">
                          {rb.title} <ExternalLink size={12} />
                        </a>
                      ) : (
                        <span className="text-sm text-body">{rb.title}</span>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-sm text-muted">{t('incidents.runbooks.noRunbooks')}</p>
              )}
            </CardBody>
          </Card>

          {/* Related Contracts */}
          {relatedContracts.length > 0 && (
            <Card>
              <CardHeader>
                <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                  <FileText size={16} className="text-accent" aria-hidden="true" /> {t('incidents.detail.relatedContracts')}
                </h2>
              </CardHeader>
              <CardBody>
                <div className="space-y-2">
                  {relatedContracts.map((contract, idx) => (
                    <NavLink
                      key={idx}
                      to={`/contracts/${contract.contractVersionId}`}
                      className="flex items-center justify-between p-2 rounded bg-elevated hover:bg-hover transition-colors"
                    >
                      <div>
                        <p className="text-sm font-medium text-heading">{contract.name}</p>
                        <p className="text-xs text-muted">{contract.version} · {contract.protocol}</p>
                      </div>
                      <Badge variant="default" className="text-[10px]">{contract.lifecycleState}</Badge>
                    </NavLink>
                  ))}
                </div>
              </CardBody>
            </Card>
          )}

          <KnowledgeContextPanel targetType="Incident" targetEntityId={incidentId} />
        </div>
      </div>
      </PageSection>

      {/* ── AI Assistant Panel ── */}
      <div className="mt-6">
        <AssistantPanel
          contextType="incident"
          contextId={incidentId!}
          contextSummary={{
            name: `${identity.reference} — ${identity.title}`,
            description: identity.summary,
            status: identity.status,
            additionalInfo: {
              severity: identity.severity,
              ...(ownerTeam ? { team: ownerTeam } : {}),
              ...(impactedDomain ? { domain: impactedDomain } : {}),
              ...(correlation.relatedChanges[0]?.changeId ? { correlatedChange: correlation.relatedChanges[0].changeId } : {}),
            },
          }}
          contextData={{
            entityType: 'incident',
            entityName: `${identity.reference} — ${identity.title}`,
            entityStatus: identity.status,
            entityDescription: identity.summary,
            properties: {
              severity: identity.severity,
              ...(ownerTeam ? { team: ownerTeam } : {}),
              ...(impactedDomain ? { domain: impactedDomain } : {}),
              ...(impactedEnvironment ? { environment: impactedEnvironment } : {}),
              ...(correlation.confidence ? { correlationConfidence: correlation.confidence } : {}),
              ...(correlation.reason ? { correlationReason: correlation.reason } : {}),
              ...(mitigation.status ? { mitigationStatus: mitigation.status } : {}),
              ...(evidence.operationalSignalsSummary ? { operationalSignals: evidence.operationalSignalsSummary } : {}),
            },
            relations: [
              ...(linkedServices?.map(s => ({
                relationType: 'Affected Services',
                entityType: 'service',
                name: s.displayName,
                status: s.criticality,
                properties: {
                  ...(s.serviceType ? { type: s.serviceType } : {}),
                },
              })) || []),
              ...(relatedContracts?.map(c => ({
                relationType: 'Related Contracts',
                entityType: 'contract',
                name: c.name,
                status: c.lifecycleState,
                properties: {
                  ...(c.version ? { version: c.version } : {}),
                  ...(c.protocol ? { protocol: c.protocol } : {}),
                },
              })) || []),
              ...(runbooks?.map(r => ({
                relationType: 'Runbooks',
                entityType: 'runbook',
                name: r.title,
                properties: {
                  ...(r.url ? { url: r.url } : {}),
                },
              })) || []),
              ...(correlation.relatedChanges?.map(c => ({
                relationType: 'Correlated Changes',
                entityType: 'change',
                name: c.description || c.changeId,
              })) || []),
            ],
            caveats: [
              ...(!linkedServices?.length ? [t('assistantPanel.contextCaveats.noLinkedServices')] : []),
              ...(!runbooks?.length ? [t('assistantPanel.contextCaveats.noRunbooks')] : []),
              ...(!correlation.relatedChanges?.length ? [t('assistantPanel.contextCaveats.noCorrelatedChanges')] : []),
            ].filter(Boolean),
          }}
          activeEnvironmentId={activeEnvironment?.id}
          activeEnvironmentName={activeEnvironment?.name}
          isNonProductionEnvironment={activeEnvironment ? !activeEnvironment.isProductionLike : false}
        />
      </div>
    </PageContainer>
  );
 }
