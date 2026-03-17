/**
 * Badges semânticos específicos do catálogo — approval, compliance, criticality.
 * Usam tokens NTO e padrão visual consistente com ProtocolBadge/LifecycleBadge.
 */
import { useTranslation } from 'react-i18next';
import { cn } from '../../../../lib/cn';
import type { ApprovalState } from '../../types';

// ── Color maps ────────────────────────────────────────────────────────────────

const APPROVAL_COLORS: Record<ApprovalState, string> = {
  Pending: 'bg-muted/15 text-muted border border-muted/25',
  InReview: 'bg-cyan/15 text-cyan border border-cyan/25',
  Approved: 'bg-mint/15 text-mint border border-mint/25',
  Rejected: 'bg-danger/15 text-danger border border-danger/25',
  Escalated: 'bg-warning/15 text-warning border border-warning/25',
};

const CRITICALITY_COLORS: Record<string, string> = {
  Low: 'text-muted',
  Medium: 'text-accent',
  High: 'text-warning',
  Critical: 'text-danger',
};

// ── ApprovalStateBadge ────────────────────────────────────────────────────────

interface ApprovalStateBadgeProps {
  state: ApprovalState;
  size?: 'sm' | 'md';
  className?: string;
}

export function ApprovalStateBadge({ state, size = 'sm', className }: ApprovalStateBadgeProps) {
  const { t } = useTranslation();
  const colors = APPROVAL_COLORS[state] ?? APPROVAL_COLORS.Pending;
  const sizeClass = size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs';

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full font-medium whitespace-nowrap',
        colors,
        sizeClass,
        className,
      )}
    >
      {t(`contracts.catalog.approvalStates.${state}`, state)}
    </span>
  );
}

// ── ComplianceBadge ───────────────────────────────────────────────────────────

interface ComplianceBadgeProps {
  score: number;
  className?: string;
}

export function ComplianceBadge({ score, className }: ComplianceBadgeProps) {
  const level = score >= 80 ? 'high' : score >= 50 ? 'medium' : 'low';
  const colors =
    level === 'high'
      ? 'text-mint'
      : level === 'medium'
        ? 'text-warning'
        : 'text-danger';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 text-xs font-semibold tabular-nums',
        colors,
        className,
      )}
    >
      <span
        className={cn(
          'w-1.5 h-1.5 rounded-full',
          level === 'high' ? 'bg-mint' : level === 'medium' ? 'bg-warning' : 'bg-danger',
        )}
      />
      {score}%
    </span>
  );
}

// ── CriticalityBadge ─────────────────────────────────────────────────────────

interface CriticalityBadgeProps {
  level: 'Low' | 'Medium' | 'High' | 'Critical';
  className?: string;
}

export function CriticalityBadge({ level, className }: CriticalityBadgeProps) {
  const { t } = useTranslation();
  const color = CRITICALITY_COLORS[level] ?? 'text-muted';

  return (
    <span className={cn('text-xs font-medium', color, className)}>
      {t(`contracts.catalog.criticality.${level}`, level)}
    </span>
  );
}
