import { useTranslation } from 'react-i18next';
import { Badge } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { PROTOCOL_COLORS } from '../shared/constants';
import { TYPE_ICONS } from './contractCreateConstants';
import type { ContractTypeValue } from '../shared/constants';
import type { CreationMode } from './contractCreateConstants';

export interface ContractSummary {
  title: string;
  serviceName: string;
  type: ContractTypeValue | null;
  protocol: string;
  mode: CreationMode | null;
  proposedVersion: string;
  author: string;
}

/** Cartão de identidade do contrato — preview vivo à esquerda do create workspace (padrão v5). */
export function ContractIdentityCard({ summary }: { summary: ContractSummary }) {
  const { t } = useTranslation();
  const Icon = summary.type ? (TYPE_ICONS[summary.type] ?? TYPE_ICONS.RestApi) : TYPE_ICONS.RestApi;
  const name = summary.title.trim() || t('contracts.create.draftNamePlaceholder', 'novo-contrato');
  const hasTitle = summary.title.trim().length > 0;

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          <div className={cn('flex items-center justify-center w-11 h-11 rounded-xl shrink-0', summary.type ? 'bg-accent text-white' : 'bg-accent/20 text-accent')}>
            <Icon size={20} />
          </div>
          <div className="min-w-0 flex-1">
            <p className={cn('font-mono text-sm font-semibold truncate', hasTitle ? 'text-heading' : 'text-muted')}>{name}</p>
            <p className="text-xs text-muted truncate mt-0.5">{summary.serviceName ? `↳ ${summary.serviceName}` : '—'}</p>
          </div>
          <Badge variant="warning" size="sm" className="shrink-0 ml-auto">{t('contracts.draftStatus.Editing', 'Draft')}</Badge>
        </div>
        <div className="flex flex-wrap gap-1.5 mt-3">
          {summary.type && <Badge variant="primary" size="sm">{t(`contracts.contractTypes.${summary.type}`, summary.type)}</Badge>}
          {summary.protocol && <span className={cn('text-[10px] px-1.5 py-0.5 rounded font-medium', PROTOCOL_COLORS[summary.protocol] ?? 'bg-muted/15 text-muted border border-muted/25')}>{summary.protocol}</span>}
          {summary.mode && <Badge variant="default" size="sm">{t(`contracts.create.mode${summary.mode.charAt(0).toUpperCase() + summary.mode.slice(1)}`, summary.mode)}</Badge>}
        </div>
      </div>

      <div className="grid grid-cols-3 gap-px bg-edge border-t border-b border-edge">
        <MiniStat value={summary.proposedVersion} label={t('contracts.create.cardVersion', 'Version')} mono />
        <MiniStat value="0" label={t('contracts.create.cardOperations', 'Operations')} />
        <MiniStat value="—" label={t('contracts.create.cardValidation', 'Validation')} muted />
      </div>

      <div className="px-4 py-2 divide-y divide-edge/60">
        <MetaRow label={t('contracts.create.cardProtocol', 'Protocol')} value={summary.protocol || '—'} />
        <MetaRow label={t('contracts.create.cardAuthor', 'Author')} value={summary.author || '—'} />
      </div>

      <p className="text-[11px] text-muted text-center py-2 px-4 border-t border-edge">
        {t('contracts.create.livePreviewHint', 'Resumo atualiza ao vivo')}
      </p>
    </div>
  );
}

function MiniStat({ value, label, mono, muted }: { value: string; label: string; mono?: boolean; muted?: boolean }) {
  return (
    <div className="bg-deep text-center py-3">
      <p className={cn('text-sm font-bold', muted ? 'text-muted' : 'text-heading', mono && 'font-mono')}>{value}</p>
      <p className="text-[10px] text-muted mt-0.5">{label}</p>
    </div>
  );
}

function MetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className="text-heading font-medium truncate ml-2">{value}</span>
    </div>
  );
}
