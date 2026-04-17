import { useTranslation } from 'react-i18next';
import { useParams, NavLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft, Zap, CheckCircle, XCircle, Clock,
  AlertTriangle, Shield, FileText, User, Server,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { automationApi, type AutomationWorkflowDetail } from '../api/automation';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const statusBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default'; icon: React.ReactNode } => {
  switch (status) {
    case 'Completed': return { variant: 'success', icon: <CheckCircle size={14} /> };
    case 'Executing': return { variant: 'warning', icon: <Clock size={14} /> };
    case 'Failed': return { variant: 'danger', icon: <XCircle size={14} /> };
    case 'PendingApproval': return { variant: 'warning', icon: <Clock size={14} /> };
    case 'Rejected':
    case 'Cancelled': return { variant: 'danger', icon: <AlertTriangle size={14} /> };
    default: return { variant: 'default', icon: <Zap size={14} /> };
  }
};

const riskBadge = (risk: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (risk) {
    case 'Low': return 'success';
    case 'Medium': return 'warning';
    case 'High':
    case 'Critical': return 'danger';
    default: return 'default';
  }
};

const stepStatusIcon = (status: string) => {
  switch (status) {
    case 'Completed': return <CheckCircle size={14} className="text-success" />;
    case 'InProgress': return <Clock size={14} className="text-warning" />;
    case 'Failed': return <XCircle size={14} className="text-critical" />;
    default: return <Clock size={14} className="text-muted" />;
  }
};

/**
 * Página de detalhe de Automation Workflow.
 * Mostra estado real de execução, pré-condições, passos, validação e trilha de auditoria.
 */
export function AutomationWorkflowDetailPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const { workflowId } = useParams<{ workflowId: string }>();

  const { data: workflow, isLoading, isError, refetch } = useQuery<AutomationWorkflowDetail>({
    queryKey: ['automation-workflow', workflowId, activeEnvironmentId],
    queryFn: () => automationApi.getWorkflow(workflowId!),
    enabled: !!workflowId,
  });

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={() => refetch()} />;
  if (!workflow) {
    return (
      <PageContainer>
        <EmptyState
          icon={<Zap size={24} />}
          title={t('automation.detail.notFound', 'Workflow not found')}
          description={t('automation.detail.notFoundDescription', 'The requested workflow could not be found.')}
          action={
            <NavLink to="/operations/automation" className="inline-flex items-center gap-2 rounded-md border border-accent/30 bg-accent/10 px-4 py-2 text-sm font-medium text-accent hover:bg-accent/15 transition-colors">
              <ArrowLeft size={14} />
              {t('automation.detail.backToList')}
            </NavLink>
          }
        />
      </PageContainer>
    );
  }

  const sb = statusBadge(workflow.status);

  return (
    <PageContainer>
      <NavLink to="/operations/automation" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
        <ArrowLeft size={16} />
        {t('automation.detail.backToList')}
      </NavLink>

      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
          <Zap size={22} className="text-accent" />
          {workflow.actionDisplayName}
        </h1>
        <div className="flex items-center gap-2 mt-2 flex-wrap">
          <Badge variant={sb.variant} className="flex items-center gap-1">
            {sb.icon} {workflow.status}
          </Badge>
          <Badge variant={riskBadge(workflow.riskLevel)}>{workflow.riskLevel}</Badge>
          {workflow.environment && <Badge variant="default">{workflow.environment}</Badge>}
        </div>
      </div>

      {/* Overview */}
      <Card className="mb-4">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <FileText size={16} className="text-accent" />
            {t('automation.detail.overview', 'Overview')}
          </h2>
        </CardHeader>
        <CardBody>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
            <div>
              <dt className="text-xs text-muted">{t('automation.detail.rationale', 'Rationale')}</dt>
              <dd className="text-body mt-0.5">{workflow.rationale || '—'}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted">{t('automation.detail.requestedBy', 'Requested By')}</dt>
              <dd className="text-body mt-0.5 flex items-center gap-1"><User size={12} /> {workflow.requestedBy}</dd>
            </div>
            {workflow.serviceId && (
              <div>
                <dt className="text-xs text-muted">{t('automation.detail.service', 'Service')}</dt>
                <dd className="text-body mt-0.5 flex items-center gap-1"><Server size={12} /> {workflow.serviceId}</dd>
              </div>
            )}
            {workflow.scope && (
              <div>
                <dt className="text-xs text-muted">{t('automation.detail.scope', 'Scope')}</dt>
                <dd className="text-body mt-0.5">{workflow.scope}</dd>
              </div>
            )}
            <div>
              <dt className="text-xs text-muted">{t('automation.detail.created', 'Created')}</dt>
              <dd className="text-body mt-0.5">{new Date(workflow.createdAt).toLocaleString()}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted">{t('automation.detail.updated', 'Last Updated')}</dt>
              <dd className="text-body mt-0.5">{new Date(workflow.updatedAt).toLocaleString()}</dd>
            </div>
          </dl>
        </CardBody>
      </Card>

      {/* Approval */}
      {workflow.approverInfo && (
        <Card className="mb-4">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              {t('automation.detail.approval', 'Approval')}
            </h2>
          </CardHeader>
          <CardBody>
            <dl className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm">
              <div>
                <dt className="text-xs text-muted">{t('automation.detail.approvedBy', 'Approved By')}</dt>
                <dd className="text-body mt-0.5">{workflow.approverInfo.approvedBy}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted">{t('automation.detail.approvalStatus', 'Status')}</dt>
                <dd className="text-body mt-0.5">{workflow.approverInfo.approvalStatus}</dd>
              </div>
              <div>
                <dt className="text-xs text-muted">{t('automation.detail.approvedAt', 'Approved At')}</dt>
                <dd className="text-body mt-0.5">{new Date(workflow.approverInfo.approvedAt).toLocaleString()}</dd>
              </div>
            </dl>
          </CardBody>
        </Card>
      )}

      {/* Preconditions */}
      {workflow.preconditions.length > 0 && (
        <Card className="mb-4">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-accent" />
              {t('automation.detail.preconditions', 'Preconditions')}
            </h2>
          </CardHeader>
          <CardBody>
            <ul className="space-y-2">
              {workflow.preconditions.map((pc, i) => (
                <li key={i} className="flex items-start gap-2 text-sm">
                  {pc.status === 'Met' ? (
                    <CheckCircle size={14} className="text-success mt-0.5 shrink-0" />
                  ) : (
                    <AlertTriangle size={14} className="text-warning mt-0.5 shrink-0" />
                  )}
                  <div>
                    <span className="text-body">{pc.description}</span>
                    <span className="text-xs text-muted ml-2">({pc.type}: {pc.status})</span>
                  </div>
                </li>
              ))}
            </ul>
          </CardBody>
        </Card>
      )}

      {/* Execution Steps */}
      {workflow.executionSteps.length > 0 && (
        <Card className="mb-4">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Zap size={16} className="text-accent" />
              {t('automation.detail.executionSteps', 'Execution Steps')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-xs text-muted">
                    <th className="text-left px-4 py-3 font-medium">#</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.stepTitle', 'Step')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.stepStatus', 'Status')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.stepCompletedBy', 'Completed By')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.stepCompletedAt', 'Completed At')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {workflow.executionSteps.map((step) => (
                    <tr key={step.stepOrder} className="hover:bg-hover transition-colors">
                      <td className="px-4 py-3 text-muted">{step.stepOrder}</td>
                      <td className="px-4 py-3 font-medium text-heading">{step.title}</td>
                      <td className="px-4 py-3">
                        <span className="flex items-center gap-1">{stepStatusIcon(step.status)} {step.status}</span>
                      </td>
                      <td className="px-4 py-3 text-muted">{step.completedBy ?? '—'}</td>
                      <td className="px-4 py-3 text-muted">{step.completedAt ? new Date(step.completedAt).toLocaleString() : '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Validation */}
      {workflow.validationInfo && (
        <Card className="mb-4">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <CheckCircle size={16} className="text-accent" />
              {t('automation.detail.validation', 'Post-Execution Validation')}
            </h2>
          </CardHeader>
          <CardBody>
            <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div>
                <dt className="text-xs text-muted">{t('automation.detail.validationStatus', 'Status')}</dt>
                <dd className="text-body mt-0.5">{workflow.validationInfo.status}</dd>
              </div>
              {workflow.validationInfo.observedOutcome && (
                <div>
                  <dt className="text-xs text-muted">{t('automation.detail.observedOutcome', 'Observed Outcome')}</dt>
                  <dd className="text-body mt-0.5">{workflow.validationInfo.observedOutcome}</dd>
                </div>
              )}
              {workflow.validationInfo.validatedBy && (
                <div>
                  <dt className="text-xs text-muted">{t('automation.detail.validatedBy', 'Validated By')}</dt>
                  <dd className="text-body mt-0.5">{workflow.validationInfo.validatedBy}</dd>
                </div>
              )}
            </dl>
          </CardBody>
        </Card>
      )}

      {/* Audit Trail */}
      {workflow.auditEntries.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <FileText size={16} className="text-accent" />
              {t('automation.detail.auditTrail', 'Audit Trail')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-xs text-muted">
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.auditAction', 'Action')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.auditPerformedBy', 'Performed By')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.auditAt', 'At')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.detail.auditDetails', 'Details')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {workflow.auditEntries.map((entry, i) => (
                    <tr key={i} className="hover:bg-hover transition-colors">
                      <td className="px-4 py-3 font-medium text-heading">{entry.action}</td>
                      <td className="px-4 py-3 text-muted">{entry.performedBy}</td>
                      <td className="px-4 py-3 text-muted">{new Date(entry.performedAt).toLocaleString()}</td>
                      <td className="px-4 py-3 text-muted">{entry.details ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
