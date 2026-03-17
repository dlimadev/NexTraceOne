import { useTranslation } from 'react-i18next';
import { CheckCircle, Circle, Clock, AlertTriangle, ArrowRight, ChevronRight } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { LifecycleBadge } from '../../shared/components/LifecycleBadge';
import { LIFECYCLE_TRANSITIONS } from '../../shared/constants';
import type { ContractLifecycleState } from '../../types';
import type { StudioContract } from '../studioTypes';

interface ApprovalsSectionProps {
  contract: StudioContract;
  onTransition?: (state: ContractLifecycleState) => void;
  className?: string;
}

/**
 * Secção de Approvals do studio — workflow de aprovação completo.
 * Mostra estado de lifecycle, checklist de aprovação por role,
 * progresso, transições disponíveis e histórico de aprovações.
 */
export function ApprovalsSection({ contract, onTransition, className = '' }: ApprovalsSectionProps) {
  const { t } = useTranslation();

  const approvedCount = contract.approvalChecklist.filter((a) => a.state === 'Approved').length;
  const totalApprovals = contract.approvalChecklist.length;
  const progressPercent = totalApprovals > 0 ? Math.round((approvedCount / totalApprovals) * 100) : 0;
  const transitions = LIFECYCLE_TRANSITIONS[contract.lifecycleState as ContractLifecycleState] ?? [];

  return (
    <div className={`space-y-6 ${className}`}>
      {/* ── Status Overview ── */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <StatusCard
          label={t('contracts.studio.approvals.lifecycle', 'Lifecycle')}
          value={contract.lifecycleState}
          badge={<LifecycleBadge state={contract.lifecycleState} />}
        />
        <StatusCard
          label={t('contracts.studio.approvals.approval', 'Approval')}
          value={contract.approvalState}
          variant={contract.approvalState === 'Approved' ? 'success' : contract.approvalState === 'Rejected' ? 'danger' : 'default'}
        />
        <StatusCard
          label={t('contracts.studio.approvals.progress', 'Progress')}
          value={`${approvedCount}/${totalApprovals}`}
        />
        <StatusCard
          label={t('contracts.studio.approvals.score', 'Approval Score')}
          value={`${progressPercent}%`}
          variant={progressPercent === 100 ? 'success' : progressPercent >= 50 ? 'warning' : 'danger'}
        />
      </div>

      {/* ── Approval Progress ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.approvals.checklist', 'Approval Checklist')}
            </h3>
            <span className="text-[10px] text-muted">{approvedCount} / {totalApprovals} {t('contracts.approvals.approved', 'approved')}</span>
          </div>
        </CardHeader>
        <CardBody>
          {/* Progress bar */}
          <div className="mb-4">
            <div className="h-2 bg-elevated rounded-full overflow-hidden">
              <div
                className={cn(
                  'h-full rounded-full transition-all duration-500',
                  progressPercent === 100 ? 'bg-mint' :
                  progressPercent >= 50 ? 'bg-cyan' :
                  'bg-warning',
                )}
                style={{ width: `${progressPercent}%` }}
              />
            </div>
          </div>

          {/* Checklist */}
          <div className="space-y-2">
            {contract.approvalChecklist.map((item) => {
              const roleKey = item.role.charAt(0).toLowerCase() + item.role.slice(1);
              return (
                <div
                  key={item.role}
                  className={cn(
                    'flex items-center gap-3 px-4 py-3 rounded-md border transition-colors',
                    item.state === 'Approved'
                      ? 'bg-mint/5 border-mint/20'
                      : 'bg-card border-edge',
                  )}
                >
                  {item.state === 'Approved' ? (
                    <CheckCircle size={16} className="text-mint flex-shrink-0" />
                  ) : (
                    <Circle size={16} className="text-muted/40 flex-shrink-0" />
                  )}

                  <div className="flex-1 min-w-0">
                    <p className={cn(
                      'text-xs font-medium',
                      item.state === 'Approved' ? 'text-heading' : 'text-muted',
                    )}>
                      {t(`contracts.approvals.${roleKey}`, item.role)}
                    </p>
                    {item.reviewedBy && (
                      <p className="text-[10px] text-muted">
                        {t('contracts.studio.approvals.reviewedBy', 'Reviewed by')} @{item.reviewedBy} · {item.reviewedAt ? formatDate(item.reviewedAt) : ''}
                      </p>
                    )}
                    {item.comment && (
                      <p className="text-[10px] text-body mt-0.5 italic">"{item.comment}"</p>
                    )}
                  </div>

                  <span className={cn(
                    'text-[10px] font-medium px-2 py-0.5 rounded-md',
                    item.state === 'Approved' ? 'bg-mint/10 text-mint' :
                    item.state === 'InReview' ? 'bg-cyan/10 text-cyan' :
                    item.state === 'Rejected' ? 'bg-danger/10 text-danger' :
                    'bg-elevated text-muted',
                  )}>
                    {item.state}
                  </span>
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* ── Available Transitions ── */}
      {transitions.length > 0 && !contract.isLocked && (
        <Card>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.approvals.actions', 'Available Actions')}
            </h3>
          </CardHeader>
          <CardBody>
            <div className="flex items-center gap-2 flex-wrap">
              {transitions.map((tr) => (
                <button
                  key={tr.state}
                  onClick={() => onTransition?.(tr.state)}
                  className="inline-flex items-center gap-2 px-4 py-2.5 text-xs font-medium rounded-md bg-elevated border border-edge hover:border-accent/40 hover:bg-accent/5 text-body transition-colors"
                >
                  <ArrowRight size={12} className="text-accent" />
                  {t(tr.actionKey, tr.state)}
                  <ChevronRight size={10} className="text-muted" />
                </button>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Locked notice */}
      {contract.isLocked && (
        <div className="flex items-start gap-3 p-4 rounded-lg border border-accent/20 bg-accent/5">
          <AlertTriangle size={16} className="text-accent flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-xs font-semibold text-heading mb-0.5">
              {t('contracts.locked', 'Locked')}
            </p>
            <p className="text-xs text-body">
              {t('contracts.approvals.locked', 'This version is locked. No further changes can be made.')}
            </p>
            {contract.lockedBy && (
              <p className="text-[10px] text-muted mt-1">
                {t('contracts.studio.approvals.lockedBy', 'Locked by')} @{contract.lockedBy} · {contract.lockedAt ? formatDate(contract.lockedAt) : ''}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function StatusCard({
  label,
  value,
  variant = 'default',
  badge,
}: {
  label: string;
  value: string;
  variant?: 'default' | 'success' | 'warning' | 'danger';
  badge?: React.ReactNode;
}) {
  const colors = {
    default: 'border-edge',
    success: 'border-mint/20',
    warning: 'border-warning/20',
    danger: 'border-danger/20',
  };

  return (
    <div className={cn('rounded-lg border bg-card px-4 py-3', colors[variant])}>
      <p className="text-[10px] text-muted mb-1">{label}</p>
      {badge ?? <p className="text-sm font-bold text-heading">{value}</p>}
    </div>
  );
}

function formatDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      year: 'numeric', month: 'short', day: 'numeric',
    });
  } catch {
    return dateStr;
  }
}
