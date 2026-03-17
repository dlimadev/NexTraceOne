import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import {
  Zap, RotateCcw, Server, Trash2, Scale, ShieldAlert,
  Settings, RefreshCw, Activity, CheckSquare, Clock,
  Users, AlertTriangle, Play, XCircle, FileText,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { PageContainer } from '../../../components/shell';

/* ── Types ── */

interface ActionDefinition {
  actionType: string;
  displayName: string;
  descriptionKey: string;
  icon: React.ReactNode;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  requiresApproval: boolean;
  allowedPersonas: string[];
}

type WorkflowStatus = 'Draft' | 'AwaitingApproval' | 'Executing' | 'Completed' | 'Cancelled';

interface WorkflowEntry {
  workflowId: string;
  actionDisplayName: string;
  status: WorkflowStatus;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  requestedBy: string;
  createdAt: string;
}

/* ── Mock data ── */

const actionCatalog: ActionDefinition[] = [
  { actionType: 'RestartControlled', displayName: 'automation.actions.restartControlled', descriptionKey: 'automation.actions.restartControlledDesc', icon: <RotateCcw size={20} />, riskLevel: 'Medium', requiresApproval: true, allowedPersonas: ['Engineer', 'TechLead'] },
  { actionType: 'ScaleOut', displayName: 'automation.actions.scaleOut', descriptionKey: 'automation.actions.scaleOutDesc', icon: <Server size={20} />, riskLevel: 'Low', requiresApproval: false, allowedPersonas: ['Engineer', 'TechLead', 'Architect'] },
  { actionType: 'ScaleIn', displayName: 'automation.actions.scaleIn', descriptionKey: 'automation.actions.scaleInDesc', icon: <Scale size={20} />, riskLevel: 'Medium', requiresApproval: true, allowedPersonas: ['TechLead', 'Architect'] },
  { actionType: 'ToggleFeatureFlag', displayName: 'automation.actions.toggleFeatureFlag', descriptionKey: 'automation.actions.toggleFeatureFlagDesc', icon: <Settings size={20} />, riskLevel: 'Low', requiresApproval: false, allowedPersonas: ['Engineer', 'TechLead', 'Product'] },
  { actionType: 'DrainInstance', displayName: 'automation.actions.drainInstance', descriptionKey: 'automation.actions.drainInstanceDesc', icon: <Trash2 size={20} />, riskLevel: 'High', requiresApproval: true, allowedPersonas: ['TechLead', 'Architect'] },
  { actionType: 'RollbackDeployment', displayName: 'automation.actions.rollbackDeployment', descriptionKey: 'automation.actions.rollbackDeploymentDesc', icon: <RefreshCw size={20} />, riskLevel: 'Critical', requiresApproval: true, allowedPersonas: ['TechLead'] },
  { actionType: 'PurgeQueue', displayName: 'automation.actions.purgeQueue', descriptionKey: 'automation.actions.purgeQueueDesc', icon: <Trash2 size={20} />, riskLevel: 'High', requiresApproval: true, allowedPersonas: ['Engineer', 'TechLead'] },
  { actionType: 'RunDiagnostics', displayName: 'automation.actions.runDiagnostics', descriptionKey: 'automation.actions.runDiagnosticsDesc', icon: <Activity size={20} />, riskLevel: 'Low', requiresApproval: false, allowedPersonas: ['Engineer', 'TechLead', 'Architect'] },
];

const mockWorkflows: WorkflowEntry[] = [
  { workflowId: 'wf-001', actionDisplayName: 'automation.actions.restartControlled', status: 'Executing', riskLevel: 'Medium', requestedBy: 'alice@corp.com', createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString() },
  { workflowId: 'wf-002', actionDisplayName: 'automation.actions.scaleOut', status: 'Completed', riskLevel: 'Low', requestedBy: 'bob@corp.com', createdAt: new Date(Date.now() - 5 * 60 * 60 * 1000).toISOString() },
  { workflowId: 'wf-003', actionDisplayName: 'automation.actions.rollbackDeployment', status: 'AwaitingApproval', riskLevel: 'Critical', requestedBy: 'carol@corp.com', createdAt: new Date(Date.now() - 1 * 60 * 60 * 1000).toISOString() },
  { workflowId: 'wf-004', actionDisplayName: 'automation.actions.toggleFeatureFlag', status: 'Draft', riskLevel: 'Low', requestedBy: 'dave@corp.com', createdAt: new Date(Date.now() - 8 * 60 * 60 * 1000).toISOString() },
  { workflowId: 'wf-005', actionDisplayName: 'automation.actions.drainInstance', status: 'Cancelled', riskLevel: 'High', requestedBy: 'eve@corp.com', createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString() },
];

/* ── Helpers ── */

const riskBadge = (risk: string): { variant: 'success' | 'warning' | 'danger' | 'default'; label: string } => {
  switch (risk) {
    case 'Low': return { variant: 'success', label: 'automation.risk.low' };
    case 'Medium': return { variant: 'warning', label: 'automation.risk.medium' };
    case 'High': return { variant: 'danger', label: 'automation.risk.high' };
    case 'Critical': return { variant: 'danger', label: 'automation.risk.critical' };
    default: return { variant: 'default', label: 'automation.risk.unknown' };
  }
};

const statusBadge = (status: WorkflowStatus): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info'; icon: React.ReactNode } => {
  switch (status) {
    case 'Draft': return { variant: 'default', icon: <FileText size={14} /> };
    case 'AwaitingApproval': return { variant: 'warning', icon: <Clock size={14} /> };
    case 'Executing': return { variant: 'info', icon: <Play size={14} /> };
    case 'Completed': return { variant: 'success', icon: <CheckSquare size={14} /> };
    case 'Cancelled': return { variant: 'danger', icon: <XCircle size={14} /> };
    default: return { variant: 'default', icon: <Clock size={14} /> };
  }
};

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours < 1) return '< 1h';
  if (hours < 24) return `${hours}h`;
  const days = Math.floor(hours / 24);
  return `${days}d`;
}

/**
 * Página de Automation Workflows — catálogo de ações automatizadas e workflows recentes.
 * Parte do módulo Operations do NexTraceOne.
 */
export function AutomationWorkflowsPage() {
  const { t } = useTranslation();

  const stats = {
    totalActions: actionCatalog.length,
    activeWorkflows: mockWorkflows.filter(w => w.status === 'Executing').length,
    pendingApprovals: mockWorkflows.filter(w => w.status === 'AwaitingApproval').length,
    completedToday: mockWorkflows.filter(w => w.status === 'Completed').length,
  };

  return (
    <PageContainer>
      {/* Page header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('automation.title')}</h1>
        <p className="text-muted mt-1">{t('automation.subtitle')}</p>
      </div>

      <OnboardingHints module="operations" />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('automation.stats.totalActions')} value={stats.totalActions} icon={<Zap size={20} />} color="text-accent" />
        <StatCard title={t('automation.stats.activeWorkflows')} value={stats.activeWorkflows} icon={<Play size={20} />} color="text-info" />
        <StatCard title={t('automation.stats.pendingApprovals')} value={stats.pendingApprovals} icon={<Clock size={20} />} color="text-warning" />
        <StatCard title={t('automation.stats.completedToday')} value={stats.completedToday} icon={<CheckSquare size={20} />} color="text-success" />
      </div>

      {/* Admin note */}
      <div className="mb-6 p-3 rounded-md bg-accent/5 border border-accent/20 text-xs text-muted flex items-center gap-2">
        <ShieldAlert size={14} className="text-accent shrink-0" />
        <span>
          {t('automation.adminHint')}{' '}
          <NavLink to="/operations/automation/admin" className="text-accent hover:text-accent/80 transition-colors">
            {t('automation.adminLink')}
          </NavLink>
        </span>
      </div>

      {/* Action Catalog */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Zap size={16} className="text-accent" />
            {t('automation.catalog.title')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
            {actionCatalog.map(action => {
              const risk = riskBadge(action.riskLevel);
              return (
                <div
                  key={action.actionType}
                  className="p-4 rounded-lg border border-edge bg-surface hover:border-edge-strong transition-colors"
                >
                  <div className="flex items-start gap-3 mb-3">
                    <div className="text-accent shrink-0 mt-0.5">{action.icon}</div>
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-heading">{t(action.displayName)}</p>
                      <p className="text-xs text-muted mt-0.5">{t(action.descriptionKey)}</p>
                    </div>
                  </div>
                  <div className="flex flex-wrap items-center gap-1.5 mt-2">
                    <Badge variant={risk.variant}>{t(risk.label)}</Badge>
                    {action.requiresApproval && (
                      <Badge variant="warning" className="flex items-center gap-1">
                        <AlertTriangle size={10} />
                        {t('automation.requiresApproval')}
                      </Badge>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-1 mt-2">
                    {action.allowedPersonas.map(p => (
                      <span key={p} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">
                        {p}
                      </span>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* Recent Workflows */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Users size={16} className="text-accent" />
            {t('automation.recentWorkflows.title')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {mockWorkflows.length === 0 ? (
              <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
            ) : (
              mockWorkflows.map(wf => {
                const sBadge = statusBadge(wf.status);
                const rBadge = riskBadge(wf.riskLevel);
                return (
                  <NavLink
                    key={wf.workflowId}
                    to={`/operations/automation/${wf.workflowId}`}
                    className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors"
                  >
                    <div className="flex items-center gap-2 min-w-0 flex-1">
                      <Badge variant={sBadge.variant} className="flex items-center gap-1 shrink-0">
                        {sBadge.icon}
                        {t(`automation.status.${wf.status}`)}
                      </Badge>
                      <p className="text-sm font-medium text-heading truncate">{t(wf.actionDisplayName)}</p>
                    </div>
                    <div className="hidden md:flex items-center gap-3 text-xs text-muted shrink-0">
                      <Badge variant={rBadge.variant} className="text-[10px]">{t(rBadge.label)}</Badge>
                      <span className="w-28 truncate">{wf.requestedBy}</span>
                      <span className="w-12 text-right">{timeAgo(wf.createdAt)}</span>
                    </div>
                  </NavLink>
                );
              })
            )}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
