/**
 * Rail contextual do studio de contratos.
 *
 * Mostra apenas riscos, actividade recente e aviso de versão bloqueada.
 * Status, owners, approvals e policy checks vivem agora no identity card.
 * Transições de ciclo de vida vivem no PageHeader.
 */
import { useTranslation } from 'react-i18next';
import { AlertTriangle, Activity } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import type { StudioContract } from '../studioTypes';

interface StudioRailProps {
  contract: StudioContract;
  className?: string;
}

/**
 * Rail contextual slim — apenas Risks, Recent Activity e Locked notice.
 */
export function StudioRail({ contract, className }: StudioRailProps) {
  const { t } = useTranslation();

  return (
    <div className={cn('space-y-5', className)}>
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
