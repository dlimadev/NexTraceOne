import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Zap, ShieldAlert, Settings, CheckCircle, AlertTriangle,
  Clock, Activity, Scale, Server, RotateCcw, RefreshCw,
  Trash2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { automationApi, type AutomationActionsResponse, type AuditTrailResponse } from '../api/automation';

const actionIcon = (actionType: string) => {
  switch (actionType) {
    case 'RestartControlled': return <RotateCcw size={14} />;
    case 'ScaleOut': return <Server size={14} />;
    case 'ScaleIn': return <Scale size={14} />;
    case 'ToggleFeatureFlag': return <Settings size={14} />;
    case 'DrainInstance': return <Trash2 size={14} />;
    case 'RollbackDeployment': return <RefreshCw size={14} />;
    case 'PurgeQueue': return <Trash2 size={14} />;
    case 'RunDiagnostics': return <Activity size={14} />;
    default: return <Zap size={14} />;
  }
};

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
 * visão geral de auditoria e utilização.
 * Restrita a Platform Admins. Parte do módulo Operations do NexTraceOne.
 */
export function AutomationAdminPage() {
  const { t } = useTranslation();

  const { data: actionsData, isLoading: actionsLoading, isError: actionsError, refetch: refetchActions } = useQuery<AutomationActionsResponse>({
    queryKey: ['automation-actions'],
    queryFn: () => automationApi.listActions(),
  });

  const { data: auditData } = useQuery<AuditTrailResponse>({
    queryKey: ['automation-audit'],
    queryFn: () => automationApi.getAuditTrail(),
  });

  if (actionsLoading) return <PageLoadingState />;
  if (actionsError) return <PageErrorState onRetry={() => refetchActions()} />;

  const actions = actionsData?.items ?? [];
  const auditEntries = auditData?.entries ?? [];
  const totalActions = actions.length;
  const approvalRequired = actions.filter(a => a.requiresApproval).length;
  const recentAuditCount = auditEntries.length;

  return (
    <PageContainer>
      <PageHeader
        title={t('automation.admin.title')}
        subtitle={t('automation.admin.subtitle')}
        badge={<ShieldAlert size={22} className="text-accent" />}
      />

      <OnboardingHints module="operations" />

      {/* Stats */}
      <StatsGrid columns={4}>
        <StatCard title={t('automation.admin.stats.totalActions')} value={totalActions} icon={<Zap size={20} />} color="text-accent" />
        <StatCard title={t('automation.admin.stats.approvalRequired', 'Approval Required')} value={approvalRequired} icon={<ShieldAlert size={20} />} color="text-warning" />
        <StatCard title={t('automation.admin.stats.withValidation', 'With Post-Validation')} value={actions.filter(a => a.hasPostValidation).length} icon={<CheckCircle size={20} />} color="text-info" />
        <StatCard title={t('automation.admin.stats.auditEntries', 'Audit Entries')} value={recentAuditCount} icon={<Activity size={20} />} color="text-critical" />
      </StatsGrid>

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
                {actions.map(action => (
                  <tr key={action.actionId} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        <span className="text-accent">{actionIcon(action.actionType)}</span>
                        <span className="font-medium text-heading">{action.displayName}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={riskBadgeVariant(action.riskLevel)}>
                        {action.riskLevel}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      {action.requiresApproval
                        ? <Badge variant="warning" className="flex items-center gap-1"><AlertTriangle size={10} />{t('common.yes')}</Badge>
                        : <span className="text-muted text-xs">{t('common.no')}</span>}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {action.allowedEnvironments.map(env => (
                          <span key={env} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{env}</span>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {action.allowedPersonas.map(p => (
                          <span key={p} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{p}</span>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-right text-muted">{action.preconditionTypes.length}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>

      {/* Audit Trail */}
      {auditEntries.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Clock size={16} className="text-accent" />
              {t('automation.admin.auditTitle', 'Recent Audit Trail')}
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
                  {auditEntries.slice(0, 20).map(entry => (
                    <tr key={entry.entryId} className="hover:bg-hover transition-colors">
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
