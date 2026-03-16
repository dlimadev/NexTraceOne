import { useTranslation } from 'react-i18next';
import {
  Zap, ShieldAlert, Settings, CheckCircle, AlertTriangle,
  Clock, Activity, Scale, Server, RotateCcw, RefreshCw,
  Trash2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';

/* ── Types ── */

interface ActionAdmin {
  name: string;
  actionType: string;
  riskLevel: string;
  requiresApproval: boolean;
  environments: string[];
  personas: string[];
  preconditionsCount: number;
  icon: React.ReactNode;
}

interface SafetyPolicy {
  id: string;
  name: string;
  descriptionKey: string;
  enabled: boolean;
}

interface UsageEntry {
  period: string;
  total: number;
  completed: number;
  failed: number;
  cancelled: number;
}

/* ── Mock data ── */

const actionAdminList: ActionAdmin[] = [
  { name: 'automation.actions.restartControlled', actionType: 'RestartControlled', riskLevel: 'Medium', requiresApproval: true, environments: ['Production', 'Staging'], personas: ['Engineer', 'TechLead'], preconditionsCount: 4, icon: <RotateCcw size={14} /> },
  { name: 'automation.actions.scaleOut', actionType: 'ScaleOut', riskLevel: 'Low', requiresApproval: false, environments: ['Production', 'Staging', 'Dev'], personas: ['Engineer', 'TechLead', 'Architect'], preconditionsCount: 2, icon: <Server size={14} /> },
  { name: 'automation.actions.scaleIn', actionType: 'ScaleIn', riskLevel: 'Medium', requiresApproval: true, environments: ['Production', 'Staging'], personas: ['TechLead', 'Architect'], preconditionsCount: 3, icon: <Scale size={14} /> },
  { name: 'automation.actions.toggleFeatureFlag', actionType: 'ToggleFeatureFlag', riskLevel: 'Low', requiresApproval: false, environments: ['Production', 'Staging', 'Dev'], personas: ['Engineer', 'TechLead', 'Product'], preconditionsCount: 1, icon: <Settings size={14} /> },
  { name: 'automation.actions.drainInstance', actionType: 'DrainInstance', riskLevel: 'High', requiresApproval: true, environments: ['Production'], personas: ['TechLead', 'Architect'], preconditionsCount: 4, icon: <Trash2 size={14} /> },
  { name: 'automation.actions.rollbackDeployment', actionType: 'RollbackDeployment', riskLevel: 'Critical', requiresApproval: true, environments: ['Production'], personas: ['TechLead'], preconditionsCount: 5, icon: <RefreshCw size={14} /> },
  { name: 'automation.actions.purgeQueue', actionType: 'PurgeQueue', riskLevel: 'High', requiresApproval: true, environments: ['Production', 'Staging'], personas: ['Engineer', 'TechLead'], preconditionsCount: 3, icon: <Trash2 size={14} /> },
  { name: 'automation.actions.runDiagnostics', actionType: 'RunDiagnostics', riskLevel: 'Low', requiresApproval: false, environments: ['Production', 'Staging', 'Dev'], personas: ['Engineer', 'TechLead', 'Architect'], preconditionsCount: 0, icon: <Activity size={14} /> },
];

const safetyPolicies: SafetyPolicy[] = [
  { id: 'pol-1', name: 'automation.policies.productionApproval', descriptionKey: 'automation.policies.productionApprovalDesc', enabled: true },
  { id: 'pol-2', name: 'automation.policies.criticalApproval', descriptionKey: 'automation.policies.criticalApprovalDesc', enabled: true },
  { id: 'pol-3', name: 'automation.policies.cooldown', descriptionKey: 'automation.policies.cooldownDesc', enabled: true },
  { id: 'pol-4', name: 'automation.policies.maxConcurrent', descriptionKey: 'automation.policies.maxConcurrentDesc', enabled: false },
];

const usageOverview: UsageEntry[] = [
  { period: 'automation.usage.today', total: 12, completed: 9, failed: 1, cancelled: 2 },
  { period: 'automation.usage.thisWeek', total: 47, completed: 38, failed: 4, cancelled: 5 },
  { period: 'automation.usage.thisMonth', total: 183, completed: 156, failed: 12, cancelled: 15 },
];

const riskBadgeVariant = (risk: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (risk) {
    case 'Low': return 'success';
    case 'Medium': return 'warning';
    case 'High': return 'danger';
    case 'Critical': return 'danger';
    default: return 'default';
  }
};

/**
 * Página de Administração de Automation — gestão de catálogo de ações,
 * políticas de segurança e visão geral de utilização.
 * Restrita a Platform Admins. Parte do módulo Operations do NexTraceOne.
 */
export function AutomationAdminPage() {
  const { t } = useTranslation();

  const stats = {
    totalActions: actionAdminList.length,
    activePolicies: safetyPolicies.filter(p => p.enabled).length,
    workflowsToday: usageOverview[0].total,
    failureRate: usageOverview[0].total > 0
      ? `${Math.round((usageOverview[0].failed / usageOverview[0].total) * 100)}%`
      : '0%',
  };

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Page header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
          <ShieldAlert size={22} className="text-accent" />
          {t('automation.admin.title')}
        </h1>
        <p className="text-muted mt-1">{t('automation.admin.subtitle')}</p>
      </div>

      <OnboardingHints module="operations" />

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('automation.admin.stats.totalActions')} value={stats.totalActions} icon={<Zap size={20} />} color="text-accent" />
        <StatCard title={t('automation.admin.stats.activePolicies')} value={stats.activePolicies} icon={<ShieldAlert size={20} />} color="text-warning" />
        <StatCard title={t('automation.admin.stats.workflowsToday')} value={stats.workflowsToday} icon={<Activity size={20} />} color="text-info" />
        <StatCard title={t('automation.admin.stats.failureRate')} value={stats.failureRate} icon={<AlertTriangle size={20} />} color="text-critical" />
      </div>

      {/* Action Catalog Table */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Zap size={16} className="text-accent" />
            {t('automation.admin.catalogTitle')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-xs text-muted">
                  <th className="text-left px-4 py-3 font-medium">{t('automation.admin.table.name')}</th>
                  <th className="text-left px-4 py-3 font-medium">{t('automation.admin.table.risk')}</th>
                  <th className="text-left px-4 py-3 font-medium">{t('automation.admin.table.approval')}</th>
                  <th className="text-left px-4 py-3 font-medium">{t('automation.admin.table.environments')}</th>
                  <th className="text-left px-4 py-3 font-medium">{t('automation.admin.table.personas')}</th>
                  <th className="text-right px-4 py-3 font-medium">{t('automation.admin.table.preconditions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {actionAdminList.map(action => (
                  <tr key={action.actionType} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className="text-accent">{action.icon}</span>
                        <span className="font-medium text-heading">{t(action.name)}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={riskBadgeVariant(action.riskLevel)}>
                        {t(`automation.risk.${action.riskLevel.toLowerCase()}`)}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      {action.requiresApproval
                        ? <Badge variant="warning" className="flex items-center gap-1"><AlertTriangle size={10} />{t('common.yes')}</Badge>
                        : <span className="text-muted text-xs">{t('common.no')}</span>}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {action.environments.map(env => (
                          <span key={env} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{env}</span>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {action.personas.map(p => (
                          <span key={p} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{p}</span>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right text-muted">{action.preconditionsCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>

      {/* Safety Policies */}
      <Card className="mb-6">
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <ShieldAlert size={16} className="text-accent" />
            {t('automation.admin.policiesTitle')}
          </h2>
        </CardHeader>
        <CardBody>
          <ul className="space-y-3">
            {safetyPolicies.map(policy => (
              <li key={policy.id} className="flex items-center gap-3 p-3 rounded-lg border border-edge bg-surface">
                {policy.enabled
                  ? <CheckCircle size={16} className="text-success shrink-0" />
                  : <Clock size={16} className="text-muted shrink-0" />}
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-medium text-heading">{t(policy.name)}</p>
                  <p className="text-xs text-muted mt-0.5">{t(policy.descriptionKey)}</p>
                </div>
                <Badge variant={policy.enabled ? 'success' : 'default'} className="shrink-0">
                  {policy.enabled ? t('common.enabled') : t('common.disabled')}
                </Badge>
              </li>
            ))}
          </ul>
        </CardBody>
      </Card>

      {/* Usage Overview */}
      <Card>
        <CardHeader>
          <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
            <Activity size={16} className="text-accent" />
            {t('automation.admin.usageTitle')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-xs text-muted">
                  <th className="text-left px-4 py-3 font-medium">{t('automation.admin.usage.period')}</th>
                  <th className="text-right px-4 py-3 font-medium">{t('automation.admin.usage.total')}</th>
                  <th className="text-right px-4 py-3 font-medium">{t('automation.admin.usage.completed')}</th>
                  <th className="text-right px-4 py-3 font-medium">{t('automation.admin.usage.failed')}</th>
                  <th className="text-right px-4 py-3 font-medium">{t('automation.admin.usage.cancelled')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {usageOverview.map(entry => (
                  <tr key={entry.period} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-3 font-medium text-heading">{t(entry.period)}</td>
                    <td className="px-4 py-3 text-right text-body">{entry.total}</td>
                    <td className="px-4 py-3 text-right text-success">{entry.completed}</td>
                    <td className="px-4 py-3 text-right text-critical">{entry.failed}</td>
                    <td className="px-4 py-3 text-right text-muted">{entry.cancelled}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
