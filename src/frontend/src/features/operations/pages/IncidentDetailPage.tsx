import { useTranslation } from 'react-i18next';
import { useParams, NavLink } from 'react-router-dom';
import {
  AlertTriangle, ArrowLeft, ShieldAlert, AlertCircle, Eye,
  CheckCircle, XCircle, Clock, Search, Wrench,
  GitBranch, FileText, BookOpen, Shield, Activity,
  ExternalLink,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';

/**
 * Dados simulados de detalhe de incidente — alinhados com o backend GetIncidentDetail.
 * Em produção, estes dados virão da API /api/v1/incidents/{incidentId}.
 */
const mockDetails: Record<string, IncidentDetail> = {
  'a1b2c3d4-0001-0000-0000-000000000001': {
    identity: {
      incidentId: 'a1b2c3d4-0001-0000-0000-000000000001',
      reference: 'INC-2026-0042',
      title: 'Payment Gateway — elevated error rate',
      summary: 'Error rate increased to 8.2% after deployment of v2.14.0. Multiple payment flows affected.',
      incidentType: 'ServiceDegradation',
      severity: 'Critical',
      status: 'Mitigating',
      createdAt: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(Date.now() - 15 * 60 * 1000).toISOString(),
    },
    linkedServices: [
      { serviceId: 'svc-payment-gateway', displayName: 'Payment Gateway', serviceType: 'RestApi', criticality: 'Critical' },
      { serviceId: 'svc-order-api', displayName: 'Order API', serviceType: 'RestApi', criticality: 'Critical' },
    ],
    ownerTeam: 'payment-squad',
    impactedDomain: 'Payments',
    impactedEnvironment: 'Production',
    timeline: [
      { timestamp: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(), description: 'Incident detected — error rate threshold breached' },
      { timestamp: new Date(Date.now() - 2.5 * 60 * 60 * 1000).toISOString(), description: 'Investigation started — payment-squad notified' },
      { timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), description: 'Root cause identified — v2.14.0 introduced regression in payment validation' },
      { timestamp: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(), description: 'Mitigation started — rollback initiated' },
      { timestamp: new Date(Date.now() - 15 * 60 * 1000).toISOString(), description: 'Rollback deployed — monitoring recovery' },
    ],
    correlation: {
      confidence: 'High',
      reason: 'Deployment of v2.14.0 strongly correlated with error rate increase. Temporal proximity and blast radius match.',
      relatedChanges: [
        { changeId: '1', description: 'Deploy v2.14.0 to Payment Gateway', changeType: 'Deployment', confidenceStatus: 'SuspectedRegression', deployedAt: new Date(Date.now() - 4 * 60 * 60 * 1000).toISOString() },
      ],
      relatedServices: [
        { serviceId: 'svc-order-api', displayName: 'Order API', impactDescription: 'Downstream payment calls failing' },
      ],
    },
    evidence: {
      operationalSignalsSummary: 'Error rate: 1.2% → 8.2%. P99 latency: 120ms → 890ms. Timeout rate increased 5x.',
      degradationSummary: 'Error rate crossed 5% threshold. P99 latency exceeded SLO. Payment success rate dropped to 91.8%.',
      observations: [
        { title: 'Error rate spike', description: 'Error rate increased from 1.2% to 8.2% within 15 minutes of deployment' },
        { title: 'Latency degradation', description: 'P99 latency increased from 120ms to 890ms' },
        { title: 'Downstream impact', description: 'Order API reporting payment timeouts' },
      ],
      anomalySummary: 'Clear before/after pattern: all key metrics degraded immediately post-deployment.',
    },
    relatedContracts: [
      { contractVersionId: '1', name: 'Payment Processing API', version: 'v2.14.0', protocol: 'REST', lifecycleState: 'Active' },
    ],
    runbooks: [
      { title: 'Payment Gateway Rollback Procedure', url: 'https://docs.internal/runbooks/payment-rollback' },
      { title: 'Payment Error Rate Troubleshooting', url: 'https://docs.internal/runbooks/payment-errors' },
    ],
    mitigation: {
      status: 'InProgress',
      actions: [
        { description: 'Rollback to v2.13.2', status: 'Applied', completed: true },
        { description: 'Monitor error rate recovery', status: 'In progress', completed: false },
        { description: 'Notify affected downstream teams', status: 'Completed', completed: true },
      ],
      rollbackGuidance: 'Rollback to v2.13.2 is the primary mitigation. Monitoring recovery.',
      rollbackRelevant: true,
      escalationGuidance: 'Escalate to payments-lead if error rate does not recover within 30 minutes post-rollback.',
    },
  },
  'a1b2c3d4-0002-0000-0000-000000000002': {
    identity: {
      incidentId: 'a1b2c3d4-0002-0000-0000-000000000002',
      reference: 'INC-2026-0041',
      title: 'Catalog Sync — integration partner unreachable',
      summary: 'External catalog provider API returning 503. Product sync stalled.',
      incidentType: 'DependencyFailure',
      severity: 'Major',
      status: 'Investigating',
      createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(),
    },
    linkedServices: [
      { serviceId: 'svc-catalog-sync', displayName: 'Catalog Sync', serviceType: 'IntegrationComponent', criticality: 'Medium' },
    ],
    ownerTeam: 'platform-squad',
    impactedDomain: 'Catalog',
    impactedEnvironment: 'Production',
    timeline: [
      { timestamp: new Date(Date.now() - 6 * 60 * 60 * 1000).toISOString(), description: 'External API health check failed — 503 responses' },
      { timestamp: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString(), description: 'Investigation started — platform-squad notified' },
      { timestamp: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString(), description: 'Vendor contacted — awaiting response' },
    ],
    correlation: {
      confidence: 'Low',
      reason: 'No internal changes correlated. External dependency failure suspected.',
      relatedChanges: [],
      relatedServices: [],
    },
    evidence: {
      operationalSignalsSummary: 'External API returning 503 since 06:00 UTC. Retry queue depth: 1,247.',
      degradationSummary: 'Product catalog sync halted. Stale data risk for product listings.',
      observations: [
        { title: 'External API failure', description: '503 Service Unavailable from catalog-provider.example.com' },
        { title: 'Queue buildup', description: 'Sync retry queue depth at 1,247 messages' },
      ],
      anomalySummary: 'External dependency failure — no internal anomalies detected.',
    },
    relatedContracts: [],
    runbooks: [
      { title: 'Catalog Sync Manual Recovery', url: 'https://docs.internal/runbooks/catalog-sync-recovery' },
    ],
    mitigation: {
      status: 'NotStarted',
      actions: [],
      rollbackGuidance: 'Not applicable — external dependency failure.',
      rollbackRelevant: false,
      escalationGuidance: 'Escalate to platform-lead if vendor does not respond within 2 hours.',
    },
  },
};

// ── Types ────────────────────────────────────────────────────────────

interface IncidentDetail {
  identity: { incidentId: string; reference: string; title: string; summary: string; incidentType: string; severity: string; status: string; createdAt: string; updatedAt: string };
  linkedServices: { serviceId: string; displayName: string; serviceType: string; criticality: string }[];
  ownerTeam: string;
  impactedDomain: string;
  impactedEnvironment: string;
  timeline: { timestamp: string; description: string }[];
  correlation: { confidence: string; reason: string; relatedChanges: { changeId: string; description: string; changeType: string; confidenceStatus: string; deployedAt: string }[]; relatedServices: { serviceId: string; displayName: string; impactDescription: string }[] };
  evidence: { operationalSignalsSummary: string; degradationSummary: string; observations: { title: string; description: string }[]; anomalySummary: string };
  relatedContracts: { contractVersionId: string; name: string; version: string; protocol: string; lifecycleState: string }[];
  runbooks: { title: string; url?: string }[];
  mitigation: { status: string; actions: { description: string; status: string; completed: boolean }[]; rollbackGuidance?: string; rollbackRelevant: boolean; escalationGuidance?: string };
}

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

/**
 * Página de detalhe do incidente — visão consolidada com correlação, evidência,
 * mitigação, runbooks, timeline, serviços vinculados e contratos relacionados.
 */
export function IncidentDetailPage() {
  const { t } = useTranslation();
  const { incidentId } = useParams<{ incidentId: string }>();
  const detail = incidentId ? mockDetails[incidentId] : undefined;

  if (!detail) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <NavLink to="/operations/incidents" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
          <ArrowLeft size={14} /> {t('incidents.detail.backToList')}
        </NavLink>
        <Card>
          <CardBody>
            <EmptyState
              icon={<AlertTriangle size={24} />}
              title={t('incidents.detail.notFound')}
              description={t('incidents.detail.notFoundDescription')}
            />
          </CardBody>
        </Card>
      </div>
    );
  }

  const { identity, linkedServices, ownerTeam, impactedDomain, impactedEnvironment, timeline, correlation, evidence, relatedContracts, runbooks, mitigation } = detail;
  const sevBadge = severityBadge(identity.severity);
  const stsBadge = statusBadge(identity.status);
  const confBadge = confidenceBadge(correlation.confidence);
  const mitBadge = mitigationBadge(mitigation.status);

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
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

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left column */}
        <div className="space-y-6">
          {/* Timeline */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Clock size={16} className="text-accent" /> {t('incidents.detail.timeline')}
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
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <GitBranch size={16} className="text-accent" /> {t('incidents.correlation.title')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
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
                {correlation.relatedChanges.length > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-2">{t('incidents.correlation.relatedChanges')}</p>
                    {correlation.relatedChanges.map((change, idx) => (
                      <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated text-sm">
                        <GitBranch size={14} className="text-amber-400 shrink-0" />
                        <span className="text-body">{change.description}</span>
                        <Badge variant="warning" className="text-[10px] ml-auto shrink-0">{change.confidenceStatus}</Badge>
                      </div>
                    ))}
                  </div>
                )}
                {correlation.relatedServices.length > 0 && (
                  <div>
                    <p className="text-xs text-muted mb-2">{t('incidents.correlation.relatedServices')}</p>
                    {correlation.relatedServices.map((svc, idx) => (
                      <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated text-sm">
                        <Activity size={14} className="text-accent shrink-0" />
                        <NavLink to={`/services/${svc.serviceId}`} className="text-accent hover:underline">{svc.displayName}</NavLink>
                        <span className="text-muted text-xs ml-auto">{svc.impactDescription}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </CardBody>
          </Card>

          {/* Evidence */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Shield size={16} className="text-accent" /> {t('incidents.evidence.title')}
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
                <div>
                  <p className="text-xs text-muted mb-1">{t('incidents.evidence.anomalySummary')}</p>
                  <p className="text-sm text-body">{evidence.anomalySummary}</p>
                </div>
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
                <Activity size={16} className="text-accent" /> {t('incidents.detail.impactedServices')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-2">
                {linkedServices.map(svc => (
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
                ))}
              </div>
            </CardBody>
          </Card>

          {/* Mitigation */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Wrench size={16} className="text-accent" /> {t('incidents.mitigation.title')}
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
                            ? <CheckCircle size={14} className="text-emerald-400 shrink-0" />
                            : <Clock size={14} className="text-amber-400 shrink-0" />
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

          {/* Runbooks */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <BookOpen size={16} className="text-accent" /> {t('incidents.runbooks.title')}
              </h2>
            </CardHeader>
            <CardBody>
              {runbooks.length > 0 ? (
                <div className="space-y-2">
                  {runbooks.map((rb, idx) => (
                    <div key={idx} className="flex items-center gap-2 p-2 rounded bg-elevated">
                      <FileText size={14} className="text-accent shrink-0" />
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
                  <FileText size={16} className="text-accent" /> {t('incidents.detail.relatedContracts')}
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
        </div>
      </div>
    </div>
  );
}
