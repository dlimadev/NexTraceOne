import { useTranslation } from 'react-i18next';
import { Globe, Server, Zap, Cog, Database, Lock, FileSignature } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Badge } from '../../../../shared/ui';
import { LifecycleBadge } from '../../shared/components/LifecycleBadge';
import { IdentityMiniStat, IdentityMetaRow } from '../../shared/components/identityCardPrimitives';
import { PROTOCOL_COLORS } from '../../shared/constants';
import type { StudioContract } from '../studioTypes';

const TYPE_ICON: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
};

/** Cartão de identidade sticky do workspace de contrato (padrão v5). Apresentacional. */
export function ContractWorkspaceIdentityCard({ contract }: { contract: StudioContract }) {
  const { t } = useTranslation();
  const Icon = TYPE_ICON[contract.serviceType] ?? Globe;
  const approved = contract.approvalChecklist.filter((a) => a.state === 'Approved').length;
  const totalApprovals = contract.approvalChecklist.length;
  const passedPolicies = contract.policyChecks.filter((p) => p.passed).length;
  const totalPolicies = contract.policyChecks.length;
  const compliance = contract.complianceScore;

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-accent text-white shrink-0">
            <Icon size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className="font-mono text-sm font-semibold text-heading truncate">{contract.technicalName}</p>
            <p className="text-xs text-muted truncate mt-0.5">{contract.friendlyName || '—'}</p>
          </div>
          <LifecycleBadge state={contract.lifecycleState} />
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3 items-center">
          <span className={cn('text-[10px] px-1.5 py-0.5 rounded font-medium', PROTOCOL_COLORS[contract.protocol] ?? 'bg-muted/15 text-muted border border-muted/25')}>
            {contract.protocol}
          </span>
          <Badge variant="primary" size="sm">{`v${contract.semVer}`}</Badge>
          {contract.isLocked && (
            <Badge variant="default" size="sm">
              <Lock size={9} className="inline mr-0.5" />
              {t('contracts.locked', 'Locked')}
            </Badge>
          )}
          {contract.signedBy && (
            <Badge variant="success" size="sm">
              <FileSignature size={9} className="inline mr-0.5" />
              {t('contracts.signed', 'Signed')}
            </Badge>
          )}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-px bg-edge border-t border-b border-edge">
        <IdentityMiniStat
          value={totalApprovals ? `${approved}/${totalApprovals}` : '—'}
          label={t('contracts.workspace.approvals', 'Approvals')}
        />
        <IdentityMiniStat
          value={totalPolicies ? `${passedPolicies}/${totalPolicies}` : '—'}
          label={t('contracts.workspace.compliance', 'Policies')}
        />
        <IdentityMiniStat
          value={compliance == null ? '—' : `${compliance}%`}
          label={t('contracts.studio.rail.compliance', 'Compliance')}
          muted={compliance == null}
        />
      </div>

      <div className="px-4 py-2 divide-y divide-edge/60">
        <IdentityMetaRow label={t('contracts.studio.rail.owner', 'Owner')} value={contract.owner ? `@${contract.owner}` : '—'} />
        <IdentityMetaRow label={t('contracts.studio.rail.team', 'Team')} value={contract.team || '—'} />
        <IdentityMetaRow label={t('contracts.studio.rail.domain', 'Domain')} value={contract.domain || '—'} />
      </div>
    </div>
  );
}
