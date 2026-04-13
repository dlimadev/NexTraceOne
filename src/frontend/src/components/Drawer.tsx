import { useEffect, useRef, useCallback, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { cn } from '../lib/cn';

interface DrawerProps {
  /** Controle de abertura/fechamento. */
  open: boolean;
  /** Callback ao fechar. */
  onClose: () => void;
  /** Título do drawer. */
  title?: string;
  /** Descrição. */
  description?: string;
  /** Lado de abertura. */
  side?: 'right' | 'left';
  /** Largura. */
  size?: 'sm' | 'md' | 'lg';
  /** Conteúdo principal. */
  children: ReactNode;
  /** Ações do rodapé. */
  footer?: ReactNode;
  className?: string;
}

const sizeClasses = {
  sm: 'w-80',
  md: 'w-[480px]',
  lg: 'w-[640px]',
};

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Drawer lateral enterprise para detalhes, edições e painéis de contexto.
 *
 * WCAG 2.1 AA compliant:
 * - Focus trap: Tab/Shift+Tab cicla dentro do drawer
 * - Retorno de foco ao elemento trigger ao fechar
 * - Escape key para fechar
 * - aria-modal, aria-labelledby
 *
 * Abre pelo lado direito por padrão (padrão enterprise para detalhes).
 * Usa z-modal para layering.
 *
 * @see docs/DESIGN-SYSTEM.md §4.11
 */
export function Drawer({
  open,
  onClose,
  title,
  description,
  side = 'right',
  size = 'md',
  children,
  footer,
  className,
}: DrawerProps) {
  const { t } = useTranslation();
  const panelRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<Element | null>(null);

  useEffect(() => {
    if (!open) return;

    triggerRef.current = document.activeElement;

    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };

    document.addEventListener('keydown', handleEscape);
    document.body.style.overflow = 'hidden';

    // Focus first focusable element in the drawer
    requestAnimationFrame(() => {
      const panel = panelRef.current;
      if (!panel) return;
      const firstFocusable = panel.querySelector<HTMLElement>(FOCUSABLE_SELECTOR);
      if (firstFocusable) firstFocusable.focus();
    });

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = '';
      // Return focus to trigger element
      if (triggerRef.current instanceof HTMLElement) {
        triggerRef.current.focus();
        triggerRef.current = null;
      }
    };
  }, [open, onClose]);

  // Focus trap: Tab/Shift+Tab cycles within drawer
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key !== 'Tab') return;
      const panel = panelRef.current;
      if (!panel) return;

      const focusableEls = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
      if (focusableEls.length === 0) {
        e.preventDefault();
        return;
      }

      const first = focusableEls[0];
      const last = focusableEls[focusableEls.length - 1];

      if (e.shiftKey) {
        if (document.activeElement === first) {
          e.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    },
    [],
  );

  if (!open) return null;

  return (
    <div
      className="fixed inset-0 z-[var(--z-modal)]"
      role="dialog"
      aria-modal="true"
      aria-labelledby={title ? 'nto-drawer-title' : undefined}
      onKeyDown={handleKeyDown}
    >
      {/* Overlay */}
      <div
        className="absolute inset-0 bg-overlay animate-fade-in"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Panel */}
      <div
        ref={panelRef}
        className={cn(
          'absolute top-0 bottom-0 flex flex-col bg-panel border-edge shadow-floating',
          side === 'right'
            ? 'right-0 border-l animate-slide-right'
            : 'left-0 border-r animate-slide-right',
          sizeClasses[size],
          className,
        )}
      >
        {/* Header */}
        {(title || description) && (
          <div className="flex items-start justify-between gap-4 border-b border-divider px-6 py-4 shrink-0">
            <div>
              {title && <h2 id="nto-drawer-title" className="text-lg font-semibold text-heading">{title}</h2>}
              {description && <p className="text-sm text-muted mt-1">{description}</p>}
            </div>
            <button
              type="button"
              onClick={onClose}
              className="rounded-sm p-1.5 text-muted hover:text-heading hover:bg-hover transition-colors"
              style={{ transitionDuration: 'var(--nto-motion-fast)' }}
              aria-label={t('common.close', 'Close')}
            >
              <X size={18} />
            </button>
          </div>
        )}

        {/* Body */}
        <div className="flex-1 overflow-y-auto px-6 py-5">{children}</div>

        {/* Footer */}
        {footer && (
          <div className="flex items-center justify-end gap-3 border-t border-divider px-6 py-4 shrink-0">
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}
