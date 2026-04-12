/**
 * Secção colapsável reutilizável para sub-blocos do visual builder.
 *
 * Apresenta título, contagem de itens e estado expandido/colapsado
 * com ícones de seta (chevron).
 */
import type { ReactNode } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';

export function CollapsibleSubSection({
  title, count, isOpen, onToggle, children,
}: {
  title: string; count: number; isOpen: boolean; onToggle: () => void; children: ReactNode;
}) {
  return (
    <div className="border border-edge rounded-md">
      <button type="button" onClick={onToggle} className="w-full flex items-center gap-2 px-3 py-2 text-left hover:bg-elevated/20 transition-colors">
        {isOpen ? <ChevronDown size={10} className="text-muted" /> : <ChevronRight size={10} className="text-muted" />}
        <span className="text-[10px] font-semibold uppercase tracking-wider text-muted/70 flex-1">{title}</span>
        {count > 0 && <span className="text-[9px] text-accent bg-accent/10 px-1.5 py-0.5 rounded">{count}</span>}
      </button>
      {isOpen && <div className="px-3 pb-3 space-y-2">{children}</div>}
    </div>
  );
}
