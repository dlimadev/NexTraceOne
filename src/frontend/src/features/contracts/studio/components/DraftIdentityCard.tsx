import { useTranslation } from 'react-i18next';
import { Globe } from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Badge } from '../../../../shared/ui';
import { TYPE_ICONS } from '../../create/contractCreateConstants';
import { IdentityMetaRow } from '../../shared/components/identityCardPrimitives';
import { PROTOCOL_COLORS } from '../../shared/constants';
import type { ContractDraft } from '../../types';

/** Cartão de identidade sticky do editor de draft (padrão v5). Apresentacional. */
export function DraftIdentityCard({ draft, serviceName }: { draft: ContractDraft; serviceName?: string }) {
  const { t } = useTranslation();
  const Icon = TYPE_ICONS[draft.contractType] ?? Globe;
  const created = draft.createdAt ? new Date(draft.createdAt).toLocaleString() : '—';
  const lastEdited = draft.lastEditedAt
    ? `${new Date(draft.lastEditedAt).toLocaleString()}${draft.lastEditedBy ? ` · ${draft.lastEditedBy}` : ''}`
    : '—';

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-accent text-white shrink-0">
            <Icon size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className="font-mono text-sm font-semibold text-heading truncate">
              {draft.title || t('contracts.studio.untitledDraft', 'untitled-draft')}
            </p>
            <p className="text-xs text-muted truncate mt-0.5">
              {t(`contracts.contractTypes.${draft.contractType}`, draft.contractType)}
            </p>
          </div>
          <Badge variant="warning" size="sm">
            {t(`contracts.draftStatus.${draft.status}`, draft.status)}
          </Badge>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3 items-center">
          <span
            className={cn(
              'text-[10px] px-1.5 py-0.5 rounded font-medium',
              PROTOCOL_COLORS[draft.protocol] ?? 'bg-muted/15 text-muted border border-muted/25',
            )}
          >
            {draft.protocol}
          </span>
          <Badge variant="primary" size="sm">{`v${draft.proposedVersion}`}</Badge>
        </div>
      </div>

      <div className="px-4 py-2 divide-y divide-edge/60">
        <IdentityMetaRow label={t('contracts.studio.linkedService', 'Service')} value={serviceName ?? '—'} />
        <IdentityMetaRow label={t('contracts.studio.author', 'Author')} value={draft.author || '—'} />
        <IdentityMetaRow label={t('contracts.studio.createdAt', 'Created')} value={created} />
        <IdentityMetaRow label={t('contracts.studio.lastEdited', 'Last edited')} value={lastEdited} />
      </div>
    </div>
  );
}
