import { cn } from '../../../../lib/cn';

/** Coluna de mini-estatística no topo de um identity card (ex.: Approvals 3/3). */
export function IdentityMiniStat({ value, label, mono, muted }: { value: string; label: string; mono?: boolean; muted?: boolean }) {
  return (
    <div className="bg-deep text-center py-3">
      <p className={cn('text-sm font-bold', muted ? 'text-muted' : 'text-heading', mono && 'font-mono')}>{value}</p>
      <p className="text-[10px] text-muted mt-0.5">{label}</p>
    </div>
  );
}

/** Linha de meta-dado (label à esquerda, valor à direita) num identity card. */
export function IdentityMetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className="text-heading font-medium truncate ml-2">{value}</span>
    </div>
  );
}
