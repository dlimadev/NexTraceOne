import { useState, useRef, useEffect, useCallback, useId, type ReactNode, type KeyboardEvent } from 'react';
import { ChevronDown, Check } from 'lucide-react';
import { cn } from '../lib/cn';

/* ─── Types ─────────────────────────────────────────────────────────────────── */

export interface DropdownMenuItem {
  /** ID único do item. */
  id: string;
  /** Label exibida. */
  label: string;
  /** Ícone à esquerda. */
  icon?: ReactNode;
  /** Indica se o item está desabilitado. */
  disabled?: boolean;
  /** Variante semântica. */
  variant?: 'default' | 'danger';
  /** Tipo: item normal ou separador. */
  type?: 'item' | 'separator';
}

interface DropdownMenuProps {
  /** Items do menu. */
  items: DropdownMenuItem[];
  /** Callback ao selecionar um item. */
  onSelect: (id: string) => void;
  /** Trigger element (botão customizado). */
  trigger?: ReactNode;
  /** Label do trigger padrão. */
  label?: string;
  /** Alinhamento do dropdown. */
  align?: 'left' | 'right';
  className?: string;
}

const FOCUSABLE = '[role="menuitem"]:not([aria-disabled="true"])';

/**
 * DropdownMenu enterprise com suporte completo a teclado.
 *
 * WCAG 2.1 AA compliant:
 * - Arrow keys para navegar items
 * - Enter/Space para selecionar
 * - Escape para fechar
 * - Home/End para primeiro/último item
 * - role="menu" / role="menuitem"
 */
export function DropdownMenu({
  items,
  onSelect,
  trigger,
  label,
  align = 'left',
  className,
}: DropdownMenuProps) {
  const [open, setOpen] = useState(false);
  const [focusIndex, setFocusIndex] = useState(-1);
  const menuRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuId = useId();

  const actionItems = items.filter((i) => i.type !== 'separator' && !i.disabled);

  // Close on click outside
  useEffect(() => {
    if (!open) return;
    const handleClick = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [open]);

  // Focus management
  useEffect(() => {
    if (!open || focusIndex < 0) return;
    const menu = menuRef.current?.querySelector(`[data-index="${focusIndex}"]`) as HTMLElement;
    menu?.focus();
  }, [open, focusIndex]);

  const handleOpen = useCallback(() => {
    setOpen(true);
    setFocusIndex(0);
  }, []);

  const handleClose = useCallback(() => {
    setOpen(false);
    setFocusIndex(-1);
    triggerRef.current?.focus();
  }, []);

  const handleTriggerKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (e.key === 'ArrowDown' || e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        handleOpen();
      }
    },
    [handleOpen],
  );

  const handleMenuKeyDown = useCallback(
    (e: KeyboardEvent) => {
      switch (e.key) {
        case 'Escape':
          e.preventDefault();
          handleClose();
          break;
        case 'ArrowDown':
          e.preventDefault();
          setFocusIndex((prev) => {
            const next = prev + 1;
            return next >= actionItems.length ? 0 : next;
          });
          break;
        case 'ArrowUp':
          e.preventDefault();
          setFocusIndex((prev) => {
            const next = prev - 1;
            return next < 0 ? actionItems.length - 1 : next;
          });
          break;
        case 'Home':
          e.preventDefault();
          setFocusIndex(0);
          break;
        case 'End':
          e.preventDefault();
          setFocusIndex(actionItems.length - 1);
          break;
        case 'Enter':
        case ' ':
          e.preventDefault();
          if (focusIndex >= 0 && focusIndex < actionItems.length) {
            onSelect(actionItems[focusIndex].id);
            handleClose();
          }
          break;
      }
    },
    [actionItems, focusIndex, handleClose, onSelect],
  );

  let actionIndex = -1;

  return (
    <div className={cn('relative inline-flex', className)} ref={menuRef}>
      {/* Trigger */}
      {trigger ? (
        <div
          onClick={() => (open ? handleClose() : handleOpen())}
          onKeyDown={handleTriggerKeyDown}
        >
          {trigger}
        </div>
      ) : (
        <button
          ref={triggerRef}
          type="button"
          onClick={() => (open ? handleClose() : handleOpen())}
          onKeyDown={handleTriggerKeyDown}
          aria-haspopup="menu"
          aria-expanded={open}
          aria-controls={open ? menuId : undefined}
          className={cn(
            'inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium',
            'bg-elevated text-body border border-edge hover:bg-hover transition-colors',
          )}
        >
          {label}
          <ChevronDown size={14} className={cn('transition-transform', open && 'rotate-180')} />
        </button>
      )}

      {/* Menu */}
      {open && (
        <div
          id={menuId}
          role="menu"
          aria-orientation="vertical"
          onKeyDown={handleMenuKeyDown}
          className={cn(
            'absolute top-full mt-1 z-[var(--z-dropdown)] min-w-[180px]',
            'rounded-lg bg-panel border border-edge shadow-floating animate-fade-in',
            'py-1',
            align === 'right' ? 'right-0' : 'left-0',
          )}
        >
          {items.map((item, i) => {
            if (item.type === 'separator') {
              return <div key={`sep-${i}`} className="my-1 border-t border-divider" role="separator" />;
            }

            const aIdx = item.disabled ? -1 : ++actionIndex;

            return (
              <button
                key={item.id}
                role="menuitem"
                data-index={aIdx}
                tabIndex={aIdx === focusIndex ? 0 : -1}
                aria-disabled={item.disabled || undefined}
                onClick={() => {
                  if (!item.disabled) {
                    onSelect(item.id);
                    handleClose();
                  }
                }}
                className={cn(
                  'flex w-full items-center gap-2 px-3 py-2 text-sm',
                  'transition-colors',
                  item.disabled
                    ? 'opacity-40 cursor-not-allowed'
                    : item.variant === 'danger'
                      ? 'text-critical hover:bg-critical/10'
                      : 'text-body hover:bg-hover hover:text-heading',
                  aIdx === focusIndex && !item.disabled && 'bg-hover text-heading',
                )}
              >
                {item.icon && <span className="shrink-0 w-4 h-4" aria-hidden="true">{item.icon}</span>}
                {item.label}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
