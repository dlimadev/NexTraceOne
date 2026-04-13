import { memo, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  ShieldAlert, ShieldCheck, AlertTriangle, UserX,
  FileWarning, BarChart3, ArrowRight, CheckCircle,
  XCircle, Clock, Lock, Eye, FileText, Users, BookOpen,
  MessageSquare, Shield,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { ProtocolBadge, LifecycleBadge } from '../shared/components';
import { cn } from '../../../lib/cn';
import { KpiCard, PolicyStat, InsightCard, ContractList } from './ContractGovernanceComponents';
import { countByLifecycle, generateAuditTimeline } from './ContractGovernanceHelpers';
import type { ContractListItem } from '../types';
import type { GovernanceInsights, PolicySummary } from './ContractGovernanceHelpers';

// ── Overview View ─────────────────────────────────────────────────────────────

interface OverviewViewProps {
  summary: { distinctContracts?: number; draftCount?: number; inReviewCount?: number; approvedCount: number; byProtocol: { protocol: string; count: number }[] } | undefined;
  insights: GovernanceInsights;
  policyResults: PolicySummary;
  contracts: ContractListItem[];
}

export const OverviewView = memo(function OverviewView({
  summary, insights, policyResults, contracts,
}: OverviewViewProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      {/* KPI cards */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          <KpiCard label={t('contracts.governance.totalContracts', 'Total')} value={summary.distinctContracts ?? 0} icon={<BarChart3 size={16} />} color="text-accent" />
          <KpiCard label={t('contracts.governance.draftCount', 'Draft')} value={summary.draftCount ?? 0} icon={<FileText size={14} />} color="text-muted" />
          <KpiCard label={t('contracts.governance.inReviewCount', 'In Review')} value={summary.inReviewCount ?? 0} icon={<Eye size={14} />} color="text-cyan" />
          <KpiCard label={t('contracts.governance.approvedCount', 'Approved')} value={summary.approvedCount} icon={<CheckCircle size={14} />} color="text-mint" />
          <KpiCard label={t('contracts.governance.issuesCount', 'Issues')} value={policyResults.totalViolations} icon={<AlertTriangle size={14} />} color="text-danger" />
        </div>
      )}

      {/* Protocol distribution */}
      {summary && summary.byProtocol.length > 0 && (
        <Card>
          <CardHeader>
            <h2 className="text-xs font-semibold text-heading">
              {t('contracts.governance.byProtocol', 'Distribution by Protocol')}
            </h2>
          </CardHeader>
          <CardBody>
            <div className="flex items-center gap-4 flex-wrap">
              {summary.byProtocol.map((bp) => (
                <div key={bp.protocol} className="flex items-center gap-2">
                  <ProtocolBadge protocol={bp.protocol} size="sm" />
                  <span className="text-xs font-medium text-heading">{bp.count}</span>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Lifecycle distribution */}
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.lifecycleDist', 'Lifecycle Distribution')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-3 flex-wrap">
            {Object.entries(countByLifecycle(contracts)).map(([state, count]) => (
              <div key={state} className="flex items-center gap-2">
                <LifecycleBadge state={state} size="sm" />
                <span className="text-xs font-medium text-heading">{count}</span>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Risk areas grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <InsightCard
          icon={<AlertTriangle size={14} />}
          iconColor="text-warning"
          title={t('contracts.governance.deprecated', 'Deprecated Contracts')}
          count={insights.deprecated.length}
          items={insights.deprecated}
        />
        <InsightCard
          icon={<UserX size={14} />}
          iconColor="text-danger"
          title={t('contracts.governance.unsigned', 'Unsigned Contracts')}
          count={insights.unsigned.length}
          items={insights.unsigned}
        />
        <InsightCard
          icon={<FileWarning size={14} />}
          iconColor="text-warning"
          title={t('contracts.governance.staleDrafts', 'Stale Drafts')}
          count={insights.staleDrafts.length}
          items={insights.staleDrafts}
        />
        <InsightCard
          icon={<Lock size={14} />}
          iconColor="text-accent"
          title={t('contracts.governance.lockedUnsigned', 'Locked but Unsigned')}
          count={insights.lockedUnsigned.length}
          items={insights.lockedUnsigned}
        />
      </div>

      {/* Policy summary */}
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.policySummary', 'Policy Check Summary')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <PolicyStat label={t('contracts.governance.policyPass', 'Pass')} count={policyResults.passed} color="text-mint" bg="bg-mint/10" />
            <PolicyStat label={t('contracts.governance.policyWarning', 'Warning')} count={policyResults.warnings} color="text-warning" bg="bg-warning/10" />
            <PolicyStat label={t('contracts.governance.policyViolation', 'Violation')} count={policyResults.totalViolations} color="text-danger" bg="bg-danger/10" />
            <PolicyStat label={t('contracts.governance.policyBlocked', 'Blocked')} count={policyResults.blocked} color="text-danger" bg="bg-danger/15" />
          </div>
        </CardBody>
      </Card>
    </div>
  );
});

// ── Approvals View ────────────────────────────────────────────────────────────

export const ApprovalsView = memo(function ApprovalsView({ contracts }: { contracts: ContractListItem[] }) {
  const { t } = useTranslation();

  const pendingApproval = contracts.filter((c) => c.lifecycleState === 'InReview');
  const recentlyApproved = contracts.filter((c) => c.lifecycleState === 'Approved').slice(0, 10);

  const workflowSteps = [
    { state: 'Draft', labelKey: 'contracts.governance.workflow.draft', icon: <FileText size={14} /> },
    { state: 'InReview', labelKey: 'contracts.governance.workflow.inReview', icon: <Eye size={14} /> },
    { state: 'Approved', labelKey: 'contracts.governance.workflow.approved', icon: <CheckCircle size={14} /> },
    { state: 'Locked', labelKey: 'contracts.governance.workflow.locked', icon: <Lock size={14} /> },
    { state: 'Deprecated', labelKey: 'contracts.governance.workflow.deprecated', icon: <AlertTriangle size={14} /> },
    { state: 'Sunset', labelKey: 'contracts.governance.workflow.sunset', icon: <Clock size={14} /> },
    { state: 'Retired', labelKey: 'contracts.governance.workflow.retired', icon: <XCircle size={14} /> },
  ];

  const approvalRoles = [
    { role: 'Architecture', labelKey: 'contracts.governance.roles.architecture', icon: <BarChart3 size={12} /> },
    { role: 'Security', labelKey: 'contracts.governance.roles.security', icon: <Shield size={12} /> },
    { role: 'Platform', labelKey: 'contracts.governance.roles.platform', icon: <ShieldCheck size={12} /> },
    { role: 'Business', labelKey: 'contracts.governance.roles.business', icon: <Users size={12} /> },
    { role: 'TechnicalOwner', labelKey: 'contracts.governance.roles.technicalOwner', icon: <FileText size={12} /> },
    { role: 'Governance', labelKey: 'contracts.governance.roles.governance', icon: <ShieldAlert size={12} /> },
  ];

  return (
    <div className="space-y-6">
      {/* Workflow diagram */}
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.workflow.title', 'Lifecycle Workflow')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="flex items-center gap-1 overflow-x-auto pb-2">
            {workflowSteps.map((step, i) => {
              const count = contracts.filter((c) => c.lifecycleState === step.state).length;
              return (
                <div key={step.state} className="flex items-center gap-1 flex-shrink-0">
                  {i > 0 && <ArrowRight size={10} className="text-muted/40 mx-1" />}
                  <div className="flex flex-col items-center gap-1 px-3 py-2 rounded-md bg-elevated border border-edge min-w-[80px]">
                    <span className="text-muted">{step.icon}</span>
                    <span className="text-[10px] font-medium text-body">{t(step.labelKey, step.state)}</span>
                    <span className="text-sm font-bold text-heading">{count}</span>
                  </div>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* Approval roles */}
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.roles.title', 'Approval Roles')}
          </h2>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
            {approvalRoles.map((role) => (
              <div key={role.role} className="flex items-center gap-2 p-2.5 rounded-md bg-elevated/50 border border-edge">
                <span className="text-accent">{role.icon}</span>
                <span className="text-xs font-medium text-body">{t(role.labelKey, role.role)}</span>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Pending approval */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="text-xs font-semibold text-heading">
              {t('contracts.governance.pendingApproval', 'Pending Approval')}
            </h2>
            <span className={cn(
              'text-xs font-bold',
              pendingApproval.length > 0 ? 'text-cyan' : 'text-muted',
            )}>
              {pendingApproval.length}
            </span>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {pendingApproval.length === 0 ? (
            <EmptyState size="compact" title={t('contracts.governance.noApprovalsPending', 'No contracts pending approval')} />
          ) : (
            <ContractList items={pendingApproval} />
          )}
        </CardBody>
      </Card>

      {/* Recently approved */}
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.recentlyApproved', 'Recently Approved')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          {recentlyApproved.length === 0 ? (
            <EmptyState size="compact" title={t('contracts.governance.noRecentApprovals', 'No recently approved contracts')} />
          ) : (
            <ContractList items={recentlyApproved} />
          )}
        </CardBody>
      </Card>
    </div>
  );
});

// ── Compliance View ───────────────────────────────────────────────────────────

export const ComplianceView = memo(function ComplianceView({ policyResults }: { policyResults: PolicySummary; contracts: ContractListItem[] }) {
  const { t } = useTranslation();

  const policyChecks: { id: string; name: string; category: string; result: 'pass' | 'warning' | 'violation' | 'blocked' }[] = [
    { id: 'naming', name: t('contracts.governance.policy.naming', 'Naming Conventions'), category: 'Standard', result: policyResults.passed > 3 ? 'pass' : 'warning' },
    { id: 'versioning', name: t('contracts.governance.policy.versioning', 'Versioning Rules'), category: 'Standard', result: 'pass' },
    { id: 'security', name: t('contracts.governance.policy.security', 'Security Rules'), category: 'Security', result: policyResults.totalViolations > 0 ? 'violation' : 'pass' },
    { id: 'observability', name: t('contracts.governance.policy.observability', 'Observability Rules'), category: 'Operations', result: 'warning' },
    { id: 'ownership', name: t('contracts.governance.policy.ownership', 'Ownership Completeness'), category: 'Governance', result: policyResults.passed > 5 ? 'pass' : 'warning' },
    { id: 'documentation', name: t('contracts.governance.policy.documentation', 'Documentation Completeness'), category: 'Governance', result: 'warning' },
    { id: 'examples', name: t('contracts.governance.policy.examples', 'Examples Completeness'), category: 'Quality', result: 'warning' },
    { id: 'deprecation', name: t('contracts.governance.policy.deprecation', 'Deprecation Policy'), category: 'Lifecycle', result: 'pass' },
    { id: 'breaking', name: t('contracts.governance.policy.breaking', 'Breaking Change Policy'), category: 'Contract', result: policyResults.totalViolations > 2 ? 'violation' : 'pass' },
    { id: 'schema', name: t('contracts.governance.policy.schema', 'Schema Governance'), category: 'Contract', result: 'pass' },
  ];

  const resultColors = {
    pass: { bg: 'bg-mint/10', text: 'text-mint', icon: <CheckCircle size={12} /> },
    warning: { bg: 'bg-warning/10', text: 'text-warning', icon: <AlertTriangle size={12} /> },
    violation: { bg: 'bg-danger/10', text: 'text-danger', icon: <XCircle size={12} /> },
    blocked: { bg: 'bg-danger/15', text: 'text-danger', icon: <ShieldAlert size={12} /> },
  };

  const overallScore = Math.round(
    (policyChecks.filter((p) => p.result === 'pass').length / policyChecks.length) * 100,
  );

  return (
    <div className="space-y-6">
      {/* Overall score */}
      <Card>
        <CardBody>
          <div className="flex items-center gap-6">
            <div className="flex flex-col items-center">
              <span className={cn(
                'text-3xl font-bold',
                overallScore >= 80 ? 'text-mint' : overallScore >= 50 ? 'text-warning' : 'text-danger',
              )}>
                {overallScore}%
              </span>
              <span className="text-[10px] text-muted uppercase tracking-wider">
                {t('contracts.governance.overallCompliance', 'Overall Compliance')}
              </span>
            </div>
            <div className="flex-1">
              <div className="h-3 bg-elevated rounded-full overflow-hidden">
                <div
                  className={cn(
                    'h-full rounded-full transition-all',
                    overallScore >= 80 ? 'bg-mint' : overallScore >= 50 ? 'bg-warning' : 'bg-danger',
                  )}
                  style={{ width: `${overallScore}%` }}
                />
              </div>
              <div className="flex items-center justify-between mt-2 text-[10px] text-muted">
                <span>{policyChecks.filter((p) => p.result === 'pass').length} {t('contracts.governance.passed', 'passed')}</span>
                <span>{policyChecks.filter((p) => p.result === 'warning').length} {t('contracts.governance.warnings', 'warnings')}</span>
                <span>{policyChecks.filter((p) => p.result === 'violation' || p.result === 'blocked').length} {t('contracts.governance.violations', 'violations')}</span>
              </div>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Policy checks */}
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.policyChecks', 'Policy Checks')}
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          <div className="divide-y divide-edge">
            {policyChecks.map((check) => {
              const style = resultColors[check.result];
              return (
                <div key={check.id} className="flex items-center gap-3 px-4 py-3">
                  <span className={style.text}>{style.icon}</span>
                  <div className="flex-1 min-w-0">
                    <p className="text-xs font-medium text-body">{check.name}</p>
                    <p className="text-[10px] text-muted">{check.category}</p>
                  </div>
                  <span className={cn(
                    'text-[10px] font-medium px-2 py-0.5 rounded',
                    style.bg, style.text,
                  )}>
                    {t(`contracts.governance.result.${check.result}`, check.result)}
                  </span>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>
    </div>
  );
});

// ── Gaps View ─────────────────────────────────────────────────────────────────

export const GapsView = memo(function GapsView({ insights }: { insights: GovernanceInsights }) {
  const { t } = useTranslation();

  const gaps: { id: string; titleKey: string; icon: React.ReactNode; iconColor: string; items: ContractListItem[] }[] = [
    { id: 'noOwner', titleKey: 'contracts.governance.gaps.noOwner', icon: <UserX size={14} />, iconColor: 'text-danger', items: insights.noOwner },
    { id: 'noApproval', titleKey: 'contracts.governance.gaps.noApproval', icon: <ShieldAlert size={14} />, iconColor: 'text-warning', items: insights.unsigned },
    { id: 'incompleteDoc', titleKey: 'contracts.governance.gaps.incompleteDoc', icon: <BookOpen size={14} />, iconColor: 'text-cyan', items: insights.incompleteDoc },
    { id: 'noExamples', titleKey: 'contracts.governance.gaps.noExamples', icon: <MessageSquare size={14} />, iconColor: 'text-warning', items: insights.noExamples },
    { id: 'noSecurityEvidence', titleKey: 'contracts.governance.gaps.noSecurityEvidence', icon: <Shield size={14} />, iconColor: 'text-danger', items: insights.noSecurityEvidence },
    { id: 'breakingRisk', titleKey: 'contracts.governance.gaps.breakingRisk', icon: <AlertTriangle size={14} />, iconColor: 'text-danger', items: insights.breakingRisk },
    { id: 'staleDrafts', titleKey: 'contracts.governance.gaps.staleDrafts', icon: <Clock size={14} />, iconColor: 'text-warning', items: insights.staleDrafts },
    { id: 'deprecated', titleKey: 'contracts.governance.gaps.deprecated', icon: <FileWarning size={14} />, iconColor: 'text-warning', items: insights.deprecated },
  ];

  const totalGaps = gaps.reduce((sum, g) => sum + g.items.length, 0);

  return (
    <div className="space-y-6">
      {/* Summary */}
      <Card>
        <CardBody>
          <div className="flex items-center gap-4">
            <div className={cn(
              'w-12 h-12 rounded-lg flex items-center justify-center',
              totalGaps === 0 ? 'bg-mint/10 border border-mint/25' : 'bg-warning/10 border border-warning/25',
            )}>
              {totalGaps === 0 ? <CheckCircle size={20} className="text-mint" /> : <AlertTriangle size={20} className="text-warning" />}
            </div>
            <div>
              <p className="text-sm font-bold text-heading">
                {totalGaps === 0
                  ? t('contracts.governance.gaps.allClear', 'All contracts meet governance standards')
                  : t('contracts.governance.gaps.issuesFound', '{{count}} governance gap(s) detected across contracts', { count: totalGaps })}
              </p>
              <p className="text-xs text-muted mt-0.5">
                {t('contracts.governance.gaps.description', 'Gaps indicate missing ownership, documentation, security evidence, or policy violations.')}
              </p>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Gap cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {gaps.map((gap) => (
          <InsightCard
            key={gap.id}
            icon={gap.icon}
            iconColor={gap.iconColor}
            title={t(gap.titleKey, gap.id)}
            count={gap.items.length}
            items={gap.items}
          />
        ))}
      </div>
    </div>
  );
});

// ── Audit View ────────────────────────────────────────────────────────────────

export const AuditView = memo(function AuditView({ contracts }: { contracts: ContractListItem[] }) {
  const { t } = useTranslation();

  const auditEntries = useMemo(() => generateAuditTimeline(contracts), [contracts]);

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <h2 className="text-xs font-semibold text-heading">
            {t('contracts.governance.audit.title', 'Audit Trail')} ({auditEntries.length})
          </h2>
        </CardHeader>
        <CardBody className="p-0">
          {auditEntries.length === 0 ? (
            <EmptyState size="compact" title={t('contracts.governance.audit.empty', 'No audit entries')} />
          ) : (
            <div className="divide-y divide-edge max-h-[600px] overflow-y-auto">
              {auditEntries.map((entry, i) => (
                <div key={i} className="flex items-start gap-3 px-4 py-3">
                  <div className="flex-shrink-0 mt-0.5">
                    {entry.type === 'approval' && <CheckCircle size={12} className="text-mint" />}
                    {entry.type === 'lifecycle' && <ArrowRight size={12} className="text-cyan" />}
                    {entry.type === 'publication' && <ShieldCheck size={12} className="text-accent" />}
                    {entry.type === 'deprecation' && <AlertTriangle size={12} className="text-warning" />}
                    {entry.type === 'creation' && <FileText size={12} className="text-muted" />}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-xs text-body">{entry.description}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className="text-[10px] text-muted">{entry.contract}</span>
                      <span className="text-[10px] text-muted/50">·</span>
                      <span className="text-[10px] text-muted">{entry.date}</span>
                    </div>
                  </div>
                  <Link
                    to={`/contracts/${entry.versionId}`}
                    className="text-[10px] text-accent hover:underline flex-shrink-0"
                  >
                    {t('common.view', 'View')}
                  </Link>
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>
    </div>
  );
});
