/**
 * Rail contextual do studio de contratos.
 *
 * Mostra informação contextual permanente na coluna direita:
 * status, owners, approval checklist, policy checks, riscos e actividade recente.
 *
 * Substitui o antigo ApprovalRail com uma experiência mais rica.
 */
import { useTranslation } from 'react-i18next';
import {
  CheckCircle, Circle, AlertTriangle, Shield,
  Users, Activity, ChevronRight, ArrowRight,
} from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { LifecycleBadge } from '../../shared/components/LifecycleBadge';
import { LIFECYCLE_TRANSITIONS } from '../../shared/constants';
import type { ContractLifecycleState } from '../../types';
import type { StudioContract } from '../studioTypes';

interface StudioRailProps {
  contract: StudioContract;
  onTransition?: (state: ContractLifecycleState) => void;
  className?: string;
}

/**
 * Rail contextual expandido para o studio.
 */
export function StudioRail({ contract, onTransition, className }: StudioRailProps) {
  const { t } = useTranslation();

  const passedPolicies = contract.policyChecks.filter((p) => p.passed).length;
  const totalPolicies = contract.policyChecks.length;
  const approvedCount = contract.approvalChecklist.filter((a) => a.state === 'Approved').length;
  const totalApprovals = contract.approvalChecklist.length;

  const transitions = LIFECYCLE_TRANSITIONS[contract.lifecycleState as ContractLifecycleState] ?? [];

  return (
    <div className={cn('space-y-5', className)}>
      {/* ── Status ── */}
      <RailSection title={t('contracts.studio.rail.status', 'Status')}>
        <div className="space-y-2.5">
          <RailRow label={t('contracts.studio.rail.lifecycle', 'Lifecycle')}>
            <LifecycleBadge state={contract.lifecycleState} />
          </RailRow>
          {contract.approvalState && (
            <RailRow label={t('contracts.studio.rail.approval', 'Approval')}>
              <span className={cn(
                'text-[10px] font-medium px-2 py-0.5 rounded-md',
                contract.approvalState === 'Approved' ? 'bg-mint/10 text-mint' :
                contract.approvalState === 'InReview' ? 'bg-cyan/10 text-cyan' :
                contract.approvalState === 'Rejected' ? 'bg-danger/10 text-danger' :
                'bg-elevated text-muted',
              )}>
                {t(`contracts.catalog.approvalStates.${contract.approvalState}`, contract.approvalState)}
              </span>
            </RailRow>
          )}
          <RailRow label={t('contracts.studio.rail.compliance', 'Compliance')}>
            <span className={cn(
              'text-[10px] font-bold',
              contract.complianceScore == null ? 'text-muted' :
              contract.complianceScore >= 80 ? 'text-mint' :
              contract.complianceScore >= 60 ? 'text-warning' :
              'text-danger',
            )}>
              {contract.complianceScore == null ? t('common.notAvailable', 'Not available') : `${contract.complianceScore}%`}
            </span>
          </RailRow>
          <RailRow label={t('contracts.studio.rail.version', 'Version')}>
            <span className="text-[10px] font-mono text-body">v{contract.semVer}</span>
          </RailRow>
        </div>
      </RailSection>

      {/* ── Owners ── */}
      <RailSection title={t('contracts.studio.rail.owners', 'Owners')} icon={<Users size={12} />}>
        <div className="space-y-1.5">
          <RailRow label={t('contracts.studio.rail.owner', 'Owner')}>
            <span className="text-[10px] text-body">{contract.owner ? `@${contract.owner}` : t('common.notAvailable', 'Not available')}</span>
          </RailRow>
          <RailRow label={t('contracts.studio.rail.team', 'Team')}>
            <span className="text-[10px] text-body">{contract.team || t('common.notAvailable', 'Not available')}</span>
          </RailRow>
          <RailRow label={t('contracts.studio.rail.domain', 'Domain')}>
            <span className="text-[10px] text-body">{contract.domain || t('common.notAvailable', 'Not available')}</span>
          </RailRow>
        </div>
      </RailSection>

      {/* ── Approval Checklist ── */}
      <RailSection
        title={t('contracts.approvals.checklist', 'Approval Checklist')}
        badge={totalApprovals > 0 ? `${approvedCount}/${totalApprovals}` : undefined}
      >
        {totalApprovals === 0 ? (
          <p className="text-[10px] text-muted">
            {t('contracts.studio.rail.approvalUnavailable', 'No approval checklist is available for this contract version yet.')}
          </p>
        ) : (
          <>
            <div className="space-y-1">
              {contract.approvalChecklist.map((item) => (
                <div key={item.role} className="flex items-center gap-2 py-1">
                  {item.state === 'Approved' ? (
                    <CheckCircle size={12} className="text-mint flex-shrink-0" />
                  ) : (
                    <Circle size={12} className="text-muted/40 flex-shrink-0" />
                  )}
                  <span className={cn(
                    'text-[10px] flex-1',
                    item.state === 'Approved' ? 'text-body' : 'text-muted',
                  )}>
                    {t(`contracts.approvals.${item.role.charAt(0).toLowerCase() + item.role.slice(1)}`, item.role)}
                  </span>
                  {item.reviewedBy && (
                    <span className="text-[9px] text-muted/60">@{item.reviewedBy.split('.')[0]}</span>
                  )}
                </div>
              ))}
            </div>
            <div className="mt-2">
              <div className="h-1 bg-elevated rounded-full overflow-hidden">
                <div
                  className="h-full bg-mint rounded-full transition-all"
                  style={{ width: `${totalApprovals > 0 ? (approvedCount / totalApprovals) * 100 : 0}%` }}
                />
              </div>
            </div>
          </>
        )}
      </RailSection>

      {/* ── Policy Checks ── */}
      <RailSection
        title={t('contracts.studio.rail.policyChecks', 'Policy Checks')}
        icon={<Shield size={12} />}
        badge={totalPolicies > 0 ? `${passedPolicies}/${totalPolicies}` : undefined}
      >
        {totalPolicies === 0 ? (
          <p className="text-[10px] text-muted">
            {t('contracts.studio.rail.policyUnavailable', 'No policy check records are available for this contract version yet.')}
          </p>
        ) : (
          <div className="space-y-1">
            {contract.policyChecks.map((check) => (
              <div key={check.policyId} className="flex items-center gap-2 py-1">
                {check.passed ? (
                  <CheckCircle size={11} className="text-mint flex-shrink-0" />
                ) : (
                  <AlertTriangle size={11} className="text-warning flex-shrink-0" />
                )}
                <span className={cn(
                  'text-[10px] flex-1',
                  check.passed ? 'text-muted' : 'text-body',
                )}>
                  {check.policyName}
                </span>
              </div>
            ))}
          </div>
        )}
      </RailSection>

      {/* ── Risks ── */}
      {contract.risks.length > 0 && (
        <RailSection
          title={t('contracts.studio.rail.risks', 'Risks')}
          icon={<AlertTriangle size={12} />}
          badge={String(contract.risks.length)}
        >
          <div className="space-y-1.5">
            {contract.risks.map((risk) => (
              <div key={risk.id} className="flex items-start gap-2 py-1">
                <span className={cn(
                  'w-1.5 h-1.5 rounded-full mt-1 flex-shrink-0',
                  risk.level === 'Critical' ? 'bg-danger' :
                  risk.level === 'High' ? 'bg-warning' :
                  risk.level === 'Medium' ? 'bg-cyan' :
                  'bg-muted',
                )} />
                <div className="min-w-0">
                  <p className="text-[10px] text-body leading-snug">{risk.description}</p>
                  <p className="text-[9px] text-muted/60">{risk.category}</p>
                </div>
              </div>
            ))}
          </div>
        </RailSection>
      )}

      {/* ── Recent Activity ── */}
      <RailSection
        title={t('contracts.studio.rail.recentActivity', 'Recent Activity')}
        icon={<Activity size={12} />}
      >
        {contract.recentActivity.length === 0 ? (
          <p className="text-[10px] text-muted">
            {t('contracts.studio.rail.noRecentActivity', 'No recorded activity is available for this contract version yet.')}
          </p>
        ) : (
          <div className="space-y-1.5">
            {contract.recentActivity.slice(0, 5).map((item) => (
              <div key={item.id} className="py-1">
                <p className="text-[10px] text-body leading-snug">{item.action}</p>
                <p className="text-[9px] text-muted/60">
                  @{item.actor.split('.')[0]} · {formatRelativeTime(item.timestamp)}
                </p>
              </div>
            ))}
          </div>
        )}
      </RailSection>

      {/* ── Available Transitions ── */}
      {transitions.length > 0 && !contract.isLocked && onTransition && (
        <RailSection title={t('contracts.approvals.actions', 'Available Actions')}>
          <div className="space-y-1.5">
            {transitions.map((tr) => (
              <button
                key={tr.state}
                onClick={() => onTransition(tr.state)}
                className="w-full flex items-center gap-2 px-3 py-2 text-[10px] font-medium rounded-md bg-elevated border border-edge hover:border-accent/40 hover:bg-accent/5 text-body transition-colors"
              >
                <ArrowRight size={10} className="text-accent" />
                {t(tr.actionKey, tr.state)}
                <ChevronRight size={10} className="ml-auto text-muted" />
              </button>
            ))}
          </div>
        </RailSection>
      )}

      {/* Locked notice */}
      {contract.isLocked && (
        <div className="px-3 py-2.5 text-[10px] text-muted bg-accent/5 border border-accent/15 rounded-md">
          {t('contracts.approvals.locked', 'This version is locked. No further changes can be made.')}
        </div>
      )}
    </div>
  );
}

// ── Rail building blocks ──────────────────────────────────────────────────────

function RailSection({
  title,
  icon,
  badge,
  children,
}: {
  title: string;
  icon?: React.ReactNode;
  badge?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <div className="flex items-center gap-1.5 mb-2">
        {icon && <span className="text-muted">{icon}</span>}
        <h4 className="text-[10px] font-semibold uppercase tracking-wider text-muted/70 flex-1">
          {title}
        </h4>
        {badge && (
          <span className="text-[9px] text-muted bg-elevated px-1.5 py-0.5 rounded-md border border-edge">
            {badge}
          </span>
        )}
      </div>
      {children}
    </div>
  );
}

function RailRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between gap-2">
      <span className="text-[10px] text-muted">{label}</span>
      {children}
    </div>
  );
}

function formatRelativeTime(timestamp: string): string {
  try {
    const diff = Date.now() - new Date(timestamp).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 30) return `${days}d ago`;
    return `${Math.floor(days / 30)}mo ago`;
  } catch {
    return '';
  }
}
