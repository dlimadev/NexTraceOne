import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Zap, ShieldAlert, Search, Clock, AlertTriangle,
  CheckCircle, XCircle, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { automationApi, type AutomationWorkflowsResponse } from '../api/automation';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

type StatusFilter = 'all' | 'Created' | 'PendingApproval' | 'Approved' | 'Executing' | 'Completed' | 'Failed' | 'Cancelled' | 'Rejected';

const statusBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default'; icon: React.ReactNode } => {
  switch (status) {
    case 'Completed': return { variant: 'success', icon: <CheckCircle size={14} /> };
    case 'Executing': return { variant: 'warning', icon: <Loader2 size={14} /> };
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

/**
 * Página de Automation Workflows.
 * Lista os workflows de automação operacional com dados reais do backend.
 */
export function AutomationWorkflowsPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');

  const { data, isLoading, isError, refetch } = useQuery<AutomationWorkflowsResponse>({
    queryKey: ['automation-workflows', statusFilter, activeEnvironmentId],
    queryFn: () => automationApi.listWorkflows(statusFilter !== 'all' ? { status: statusFilter } : undefined),
  });

  const items = data?.items ?? [];
  const filtered = search
    ? items.filter(
        (w) =>
          w.actionDisplayName.toLowerCase().includes(search.toLowerCase()) ||
          w.requestedBy.toLowerCase().includes(search.toLowerCase()) ||
          (w.serviceId && w.serviceId.toLowerCase().includes(search.toLowerCase())),
      )
    : items;

  const completedCount = items.filter((w) => w.status === 'Completed').length;
  const failedCount = items.filter((w) => w.status === 'Failed').length;
  const pendingCount = items.filter((w) => w.status === 'PendingApproval' || w.status === 'Created').length;

  if (isLoading) return <PageLoadingState />;
  if (isError) return <PageErrorState onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('automation.title')}
        subtitle={t('automation.subtitle')}
      >
        <div className="flex items-center gap-2 mt-2">
          <NavLink
            to="/operations/automation/admin"
            className="inline-flex items-center gap-1 text-xs text-accent hover:text-accent/80 transition-colors"
          >
            <ShieldAlert size={14} />
            {t('automation.adminLink')}
          </NavLink>
        </div>
      </PageHeader>

      <StatsGrid columns={4}>
        <StatCard title={t('automation.stats.total')} value={data?.totalCount ?? 0} icon={<Zap size={20} />} color="text-accent" />
        <StatCard title={t('automation.stats.completed')} value={completedCount} icon={<CheckCircle size={20} />} color="text-success" />
        <StatCard title={t('automation.stats.failed')} value={failedCount} icon={<XCircle size={20} />} color="text-critical" />
        <StatCard title={t('automation.stats.pending')} value={pendingCount} icon={<Clock size={20} />} color="text-warning" />
      </StatsGrid>

      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="relative flex-1">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            placeholder={t('automation.searchPlaceholder', 'Search workflows...')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-md border border-edge bg-surface text-sm text-body placeholder:text-muted focus:border-accent focus:ring-1 focus:ring-accent transition-colors"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
          className="rounded-md border border-edge bg-surface px-3 py-2 text-sm text-body focus:border-accent focus:ring-1 focus:ring-accent transition-colors"
        >
          <option value="all">{t('common.allStatuses', 'All statuses')}</option>
          <option value="Created">{t('automation.status.created', 'Created')}</option>
          <option value="PendingApproval">{t('automation.status.pendingApproval', 'Pending Approval')}</option>
          <option value="Approved">{t('automation.status.approved', 'Approved')}</option>
          <option value="Executing">{t('automation.status.executing', 'Executing')}</option>
          <option value="Completed">{t('automation.status.completed', 'Completed')}</option>
          <option value="Failed">{t('automation.status.failed', 'Failed')}</option>
          <option value="Cancelled">{t('automation.status.cancelled', 'Cancelled')}</option>
          <option value="Rejected">{t('automation.status.rejected', 'Rejected')}</option>
        </select>
      </div>

      {filtered.length === 0 ? (
        <EmptyState
          icon={<Zap size={24} />}
          title={t('automation.emptyTitle', 'No automation workflows found')}
          description={t('automation.emptyDescription', 'No workflows match the current filters. Workflows are created from the incident or service context.')}
        />
      ) : (
        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Zap size={16} className="text-accent" />
              {t('automation.workflowsTitle', 'Automation Workflows')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-xs text-muted">
                    <th className="text-left px-4 py-3 font-medium">{t('automation.table.action', 'Action')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.table.status', 'Status')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.table.risk', 'Risk')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.table.requestedBy', 'Requested By')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.table.service', 'Service')}</th>
                    <th className="text-left px-4 py-3 font-medium">{t('automation.table.createdAt', 'Created')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {filtered.map((wf) => {
                    const sb = statusBadge(wf.status);
                    return (
                      <tr key={wf.workflowId} className="hover:bg-hover transition-colors">
                        <td className="px-4 py-3">
                          <NavLink
                            to={`/operations/automation/${wf.workflowId}`}
                            className="font-medium text-heading hover:text-accent transition-colors"
                          >
                            {wf.actionDisplayName}
                          </NavLink>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={sb.variant} className="flex items-center gap-1 w-fit">
                            {sb.icon} {wf.status}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <Badge variant={riskBadge(wf.riskLevel)}>{wf.riskLevel}</Badge>
                        </td>
                        <td className="px-4 py-3 text-muted">{wf.requestedBy}</td>
                        <td className="px-4 py-3 text-muted">{wf.serviceId ?? '—'}</td>
                        <td className="px-4 py-3 text-muted">{new Date(wf.createdAt).toLocaleDateString()}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}
