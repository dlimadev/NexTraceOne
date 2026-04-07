import { useState, useCallback, useId, type ReactNode, type KeyboardEvent } from 'react';
import { ChevronDown } from 'lucide-react';
import { cn } from '../lib/cn';

/* ─── Types ─────────────────────────────────────────────────────────────────── */

interface CollapsibleProps {
  /** Título/header do item colapsável. */
  title: ReactNode;
  /** Conteúdo expandido. */
  children: ReactNode;
  /** Controlado externamente: está aberto? */
  open?: boolean;
  /** Callback ao mudar estado (controlado). */
  onOpenChange?: (open: boolean) => void;
  /** Começa aberto (não-controlado). */
  defaultOpen?: boolean;
  /** Desabilitado. */
  disabled?: boolean;
  className?: string;
}

interface AccordionItem {
  id: string;
  title: ReactNode;
  content: ReactNode;
  disabled?: boolean;
}

interface AccordionProps {
  /** Items do accordion. */
  items: AccordionItem[];
  /** Modo: single (uma aba aberta) ou multi (várias abertas). */
  mode?: 'single' | 'multi';
  /** IDs dos items abertos (controlado). */
  openIds?: string[];
  /** Callback ao mudar items abertos (controlado). */
  onOpenIdsChange?: (ids: string[]) => void;
  /** IDs inicialmente abertos (não-controlado). */
  defaultOpenIds?: string[];
  className?: string;
}

/* ─── Collapsible ───────────────────────────────────────────────────────────── */

/**
 * Seção expansível com animação suave e suporte a teclado.
 *
 * WCAG 2.1 AA compliant:
 * - Enter/Space para toggle
 * - aria-expanded no trigger
 * - aria-controls para linkage
 */
export function Collapsible({
  title,
  children,
  open: controlledOpen,
  onOpenChange,
  defaultOpen = false,
  disabled = false,
  className,
}: CollapsibleProps) {
  const [internalOpen, setInternalOpen] = useState(defaultOpen);
  const contentId = useId();

  const isOpen = controlledOpen !== undefined ? controlledOpen : internalOpen;

  const toggle = useCallback(() => {
    if (disabled) return;
    if (onOpenChange) {
      onOpenChange(!isOpen);
    } else {
      setInternalOpen(!isOpen);
    }
  }, [disabled, isOpen, onOpenChange]);

  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLButtonElement>) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        toggle();
      }
    },
    [toggle],
  );

  return (
    <div className={cn('border border-edge rounded-lg overflow-hidden', className)}>
      <button
        type="button"
        onClick={toggle}
        onKeyDown={handleKeyDown}
        aria-expanded={isOpen}
        aria-controls={contentId}
        disabled={disabled}
        className={cn(
          'flex w-full items-center justify-between gap-3 px-4 py-3 text-sm font-medium text-heading',
          'bg-elevated/50 hover:bg-hover transition-colors',
          disabled && 'opacity-50 cursor-not-allowed',
        )}
      >
        <span className="flex-1 text-left">{title}</span>
        <ChevronDown
          size={16}
          className={cn(
            'shrink-0 text-muted transition-transform duration-200',
            isOpen && 'rotate-180',
          )}
          aria-hidden="true"
        />
      </button>
      <div
        id={contentId}
        role="region"
        hidden={!isOpen}
        className={cn(
          'overflow-hidden transition-all duration-200',
          isOpen ? 'max-h-[2000px] opacity-100' : 'max-h-0 opacity-0',
        )}
      >
        <div className="px-4 py-3 border-t border-edge/60">
          {children}
        </div>
      </div>
    </div>
  );
}

/* ─── Accordion ─────────────────────────────────────────────────────────────── */

/**
 * Accordion (grupo de Collapsibles) com modo single ou multi.
 *
 * Single: apenas um item aberto de cada vez.
 * Multi: vários items podem estar abertos simultaneamente.
 */
export function Accordion({
  items,
  mode = 'single',
  openIds: controlledIds,
  onOpenIdsChange,
  defaultOpenIds = [],
  className,
}: AccordionProps) {
  const [internalIds, setInternalIds] = useState<string[]>(defaultOpenIds);

  const openIds = controlledIds ?? internalIds;
  const setOpenIds = onOpenIdsChange ?? setInternalIds;

  const handleToggle = useCallback(
    (id: string) => {
      if (mode === 'single') {
        setOpenIds(openIds.includes(id) ? [] : [id]);
      } else {
        setOpenIds(
          openIds.includes(id)
            ? openIds.filter((oid) => oid !== id)
            : [...openIds, id],
        );
      }
    },
    [mode, openIds, setOpenIds],
  );

  return (
    <div className={cn('space-y-2', className)}>
      {items.map((item) => (
        <Collapsible
          key={item.id}
          title={item.title}
          open={openIds.includes(item.id)}
          onOpenChange={() => handleToggle(item.id)}
          disabled={item.disabled}
        >
          {item.content}
        </Collapsible>
      ))}
    </div>
  );
}
