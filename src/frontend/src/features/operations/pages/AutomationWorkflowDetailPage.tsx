import { useTranslation } from 'react-i18next';
import { useParams, NavLink } from 'react-router-dom';
import {
  ArrowLeft, RotateCcw, CheckCircle, Clock, XCircle,
  ShieldCheck, AlertTriangle, Activity, FileText, Play,
  Server, Zap, Eye,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';

/* ── Types ── */

interface Precondition {
  name: string;
  status: 'Passed' | 'Pending' | 'Failed';
}

interface ExecutionStep {
  order: number;
  title: string;
  status: 'Completed' | 'Pending';
  completedBy?: string;
  completedAt?: string;
}

interface AuditEntry {
  timestamp: string;
  action: string;
  performer: string;
}

interface ApprovalDecision {
  status: 'Approved' | 'Rejected' | 'Pending';
  decidedBy?: string;
  decidedAt?: string;
  reason?: string;
}

interface ValidationCheck {
  name: string;
  passed: boolean;
}

interface WorkflowDetail {
  workflowId: string;
  actionType: string;
  actionDisplayName: string;
  rationale: string;
  scope: string;
  environment: string;
  status: string;
  riskLevel: string;
  requiresApproval: boolean;
  preconditions: Precondition[];
  steps: ExecutionStep[];
  approval: ApprovalDecision;
  audit: AuditEntry[];
  validation: ValidationCheck[] | null;
  linkedServiceId: string;
  linkedServiceName: string;
  linkedIncidentId: string;
  linkedIncidentRef: string;
  linkedChangeId?: string;
}

/* ── Mock data ── */

const mockWorkflow: WorkflowDetail = {
  workflowId: 'wf-001',
  actionType: 'RestartControlled',
  actionDisplayName: 'automation.actions.restartControlled',
  rationale: 'Payment Gateway pods showing memory leak after v2.14.0 deploy. Controlled restart to recover without full rollback.',
  scope: 'svc-payment-gateway (3 pods, production-eu-west)',
  environment: 'Production',
  status: 'Executing',
  riskLevel: 'Medium',
  requiresApproval: true,
  preconditions: [
    { name: 'automation.preconditions.healthCheck', status: 'Passed' },
    { name: 'automation.preconditions.noActiveIncident', status: 'Passed' },
    { name: 'automation.preconditions.cooldownElapsed', status: 'Passed' },
    { name: 'automation.preconditions.minInstances', status: 'Passed' },
  ],
  steps: [
    { order: 1, title: 'automation.steps.drainTraffic', status: 'Completed', completedBy: 'alice@corp.com', completedAt: new Date(Date.now() - 90 * 60 * 1000).toISOString() },
    { order: 2, title: 'automation.steps.restartPod', status: 'Completed', completedBy: 'alice@corp.com', completedAt: new Date(Date.now() - 60 * 60 * 1000).toISOString() },
    { order: 3, title: 'automation.steps.healthValidation', status: 'Pending' },
    { order: 4, title: 'automation.steps.restoreTraffic', status: 'Pending' },
  ],
  approval: {
    status: 'Approved',
    decidedBy: 'tech-lead@corp.com',
    decidedAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(),
    reason: 'Memory leak confirmed. Controlled restart is safe with current traffic levels.',
  },
  audit: [
    { timestamp: new Date(Date.now() - 3 * 60 * 60 * 1000).toISOString(), action: 'automation.audit.created', performer: 'alice@corp.com' },
    { timestamp: new Date(Date.now() - 2.5 * 60 * 60 * 1000).toISOString(), action: 'automation.audit.preconditionsEvaluated', performer: 'system' },
    { timestamp: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), action: 'automation.audit.approved', performer: 'tech-lead@corp.com' },
    { timestamp: new Date(Date.now() - 90 * 60 * 1000).toISOString(), action: 'automation.audit.stepCompleted', performer: 'alice@corp.com' },
    { timestamp: new Date(Date.now() - 60 * 60 * 1000).toISOString(), action: 'automation.audit.stepCompleted', performer: 'alice@corp.com' },
  ],
  validation: null,
  linkedServiceId: 'svc-payment-gateway',
  linkedServiceName: 'Payment Gateway',
  linkedIncidentId: 'a1b2c3d4-0001-0000-0000-000000000001',
  linkedIncidentRef: 'INC-2026-0042',
};

/* ── Helpers ── */

const riskBadgeVariant = (risk: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (risk) {
    case 'Low': return 'success';
    case 'Medium': return 'warning';
    case 'High': return 'danger';
    case 'Critical': return 'danger';
    default: return 'default';
  }
};

const preconditionIcon = (status: string) => {
  switch (status) {
    case 'Passed': return <CheckCircle size={14} className="text-success" />;
    case 'Failed': return <XCircle size={14} className="text-critical" />;
    default: return <Clock size={14} className="text-warning" />;
  }
};

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' });
}

/**
 * Página de detalhe de Automation Workflow — visão guiada com pré-condições,
 * passos de execução, aprovação, validação e auditoria.
 * Parte do módulo Operations do NexTraceOne.
 */
export function AutomationWorkflowDetailPage() {
  const { t } = useTranslation();
  const { workflowId } = useParams<{ workflowId: string }>();

  const wf = mockWorkflow;

  if (!workflowId) {
    return (
      <PageContainer>
        <EmptyState
          icon={<Zap size={24} />}
          title={t('automation.detail.notFound')}
          description={t('automation.detail.notFoundDesc')}
        />
      </PageContainer>
    );
  }

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Back link + header */}
      <NavLink to="/operations/automation" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
        <ArrowLeft size={16} />
        {t('automation.detail.backToList')}
      </NavLink>

      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
          <RotateCcw size={22} className="text-accent" />
          {t(wf.actionDisplayName)}
        </h1>
        <p className="text-muted mt-1 text-sm">{t('automation.detail.subtitle', { id: workflowId })}</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* ── Left column (2 cols) ── */}
        <div className="lg:col-span-2 space-y-6">
          {/* Overview */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <FileText size={16} className="text-accent" />
                {t('automation.detail.overview')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-3 text-sm">
                <div className="flex flex-wrap gap-2 mb-3">
                  <Badge variant={riskBadgeVariant(wf.riskLevel)}>{t(`automation.risk.${wf.riskLevel.toLowerCase()}`)}</Badge>
                  <Badge variant="info" className="flex items-center gap-1">
                    <Play size={10} />
                    {t(`automation.status.${wf.status}`)}
                  </Badge>
                  {wf.requiresApproval && (
                    <Badge variant="warning" className="flex items-center gap-1">
                      <AlertTriangle size={10} />
                      {t('automation.requiresApproval')}
                    </Badge>
                  )}
                </div>
                <div>
                  <span className="text-muted">{t('automation.detail.rationale')}:</span>
                  <p className="text-body mt-0.5">{wf.rationale}</p>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <span className="text-muted">{t('automation.detail.scope')}:</span>
                    <p className="text-body mt-0.5">{wf.scope}</p>
                  </div>
                  <div>
                    <span className="text-muted">{t('automation.detail.environment')}:</span>
                    <p className="text-body mt-0.5">{wf.environment}</p>
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>

          {/* Preconditions */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <ShieldCheck size={16} className="text-accent" />
                {t('automation.detail.preconditions')}
              </h2>
            </CardHeader>
            <CardBody>
              <ul className="space-y-2">
                {wf.preconditions.map((pc, i) => (
                  <li key={i} className="flex items-center gap-2 text-sm">
                    {preconditionIcon(pc.status)}
                    <span className="text-body">{t(pc.name)}</span>
                    <span className="text-xs text-muted ml-auto">{t(`automation.preconditionStatus.${pc.status}`)}</span>
                  </li>
                ))}
              </ul>
              <button className="mt-4 px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors">
                {t('automation.detail.evaluatePreconditions')}
              </button>
            </CardBody>
          </Card>

          {/* Execution Steps */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Activity size={16} className="text-accent" />
                {t('automation.detail.executionSteps')}
              </h2>
            </CardHeader>
            <CardBody>
              <ol className="space-y-3">
                {wf.steps.map(step => (
                  <li key={step.order} className="flex items-start gap-3 text-sm">
                    <span className="shrink-0 mt-0.5">
                      {step.status === 'Completed'
                        ? <CheckCircle size={16} className="text-success" />
                        : <Clock size={16} className="text-muted" />}
                    </span>
                    <div className="min-w-0 flex-1">
                      <p className={`font-medium ${step.status === 'Completed' ? 'text-heading' : 'text-muted'}`}>
                        {step.order}. {t(step.title)}
                      </p>
                      {step.completedBy && (
                        <p className="text-xs text-muted mt-0.5">
                          {step.completedBy} · {step.completedAt ? formatTime(step.completedAt) : ''}
                        </p>
                      )}
                    </div>
                    <Badge variant={step.status === 'Completed' ? 'success' : 'default'} className="text-[10px] shrink-0">
                      {t(`automation.stepStatus.${step.status}`)}
                    </Badge>
                  </li>
                ))}
              </ol>
              <div className="flex gap-2 mt-4">
                <button className="px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors">
                  {t('automation.detail.execute')}
                </button>
                <button className="px-3 py-1.5 text-xs rounded-md bg-success/10 text-success border border-success/30 hover:bg-success/20 transition-colors">
                  {t('automation.detail.completeStep')}
                </button>
              </div>
            </CardBody>
          </Card>

          {/* Audit Trail */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Eye size={16} className="text-accent" />
                {t('automation.detail.auditTrail')}
              </h2>
            </CardHeader>
            <CardBody>
              <ul className="space-y-2">
                {wf.audit.map((entry, i) => (
                  <li key={i} className="flex items-center gap-3 text-xs border-l-2 border-edge pl-3 py-1">
                    <span className="text-muted w-32 shrink-0">{formatTime(entry.timestamp)}</span>
                    <span className="text-body">{t(entry.action)}</span>
                    <span className="text-muted ml-auto">{entry.performer}</span>
                  </li>
                ))}
              </ul>
            </CardBody>
          </Card>
        </div>

        {/* ── Right column (1 col) ── */}
        <div className="space-y-6">
          {/* Approval */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <ShieldCheck size={16} className="text-accent" />
                {t('automation.detail.approval')}
              </h2>
            </CardHeader>
            <CardBody>
              {wf.approval.status === 'Approved' ? (
                <div className="space-y-2">
                  <Badge variant="success" className="flex items-center gap-1">
                    <CheckCircle size={12} />
                    {t('automation.approval.approved')}
                  </Badge>
                  <p className="text-xs text-muted">{t('automation.approval.decidedBy')}: {wf.approval.decidedBy}</p>
                  {wf.approval.decidedAt && (
                    <p className="text-xs text-muted">{t('automation.approval.decidedAt')}: {formatTime(wf.approval.decidedAt)}</p>
                  )}
                  {wf.approval.reason && (
                    <p className="text-xs text-body mt-1 p-2 rounded bg-surface border border-edge">{wf.approval.reason}</p>
                  )}
                </div>
              ) : wf.approval.status === 'Rejected' ? (
                <div className="space-y-2">
                  <Badge variant="danger" className="flex items-center gap-1">
                    <XCircle size={12} />
                    {t('automation.approval.rejected')}
                  </Badge>
                  <p className="text-xs text-muted">{t('automation.approval.decidedBy')}: {wf.approval.decidedBy}</p>
                </div>
              ) : (
                <div className="space-y-3">
                  <Badge variant="warning" className="flex items-center gap-1">
                    <Clock size={12} />
                    {t('automation.approval.pending')}
                  </Badge>
                  <textarea
                    className="w-full text-xs p-2 rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                    rows={3}
                    placeholder={t('automation.approval.reasonPlaceholder')}
                  />
                  <div className="flex gap-2">
                    <button className="flex-1 px-3 py-1.5 text-xs rounded-md bg-success/10 text-success border border-success/30 hover:bg-success/20 transition-colors">
                      {t('automation.approval.approve')}
                    </button>
                    <button className="flex-1 px-3 py-1.5 text-xs rounded-md bg-critical/10 text-critical border border-critical/30 hover:bg-critical/20 transition-colors">
                      {t('automation.approval.reject')}
                    </button>
                  </div>
                </div>
              )}
            </CardBody>
          </Card>

          {/* Validation */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Activity size={16} className="text-accent" />
                {t('automation.detail.validation')}
              </h2>
            </CardHeader>
            <CardBody>
              {wf.validation ? (
                <ul className="space-y-2">
                  {wf.validation.map((v, i) => (
                    <li key={i} className="flex items-center gap-2 text-xs">
                      {v.passed
                        ? <CheckCircle size={14} className="text-success" />
                        : <XCircle size={14} className="text-critical" />}
                      <span className="text-body">{v.name}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <div className="text-center py-4">
                  <Clock size={20} className="mx-auto text-muted mb-2" />
                  <p className="text-xs text-muted">{t('automation.validation.pending')}</p>
                  <button className="mt-3 px-3 py-1.5 text-xs rounded-md bg-accent/10 text-accent border border-accent/30 hover:bg-accent/20 transition-colors">
                    {t('automation.validation.record')}
                  </button>
                </div>
              )}
            </CardBody>
          </Card>

          {/* Context */}
          <Card>
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <Server size={16} className="text-accent" />
                {t('automation.detail.context')}
              </h2>
            </CardHeader>
            <CardBody>
              <ul className="space-y-3 text-sm">
                <li>
                  <span className="text-muted text-xs">{t('automation.context.service')}</span>
                  <NavLink to={`/services/${wf.linkedServiceId}`} className="block text-accent hover:text-accent/80 transition-colors text-sm">
                    {wf.linkedServiceName}
                  </NavLink>
                </li>
                <li>
                  <span className="text-muted text-xs">{t('automation.context.incident')}</span>
                  <NavLink to={`/operations/incidents/${wf.linkedIncidentId}`} className="block text-accent hover:text-accent/80 transition-colors text-sm">
                    {wf.linkedIncidentRef}
                  </NavLink>
                </li>
              </ul>
            </CardBody>
          </Card>
        </div>
      </div>
    </div>
  );
}
