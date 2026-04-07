import { useEffect, useRef, useCallback, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { cn } from '../lib/cn';

interface ModalProps {
  /** Controle de abertura/fechamento. */
  open: boolean;
  /** Callback ao fechar. */
  onClose: () => void;
  /** Título do modal. */
  title?: string;
  /** Descrição do modal. */
  description?: string;
  /** Tamanho horizontal. */
  size?: 'sm' | 'md' | 'lg' | 'xl';
  /** Conteúdo principal. */
  children: ReactNode;
  /** Ações do rodapé (botões). */
  footer?: ReactNode;
  className?: string;
}

const sizeClasses = {
  sm: 'max-w-md',
  md: 'max-w-lg',
  lg: 'max-w-2xl',
  xl: 'max-w-4xl',
};

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Modal dialog enterprise com overlay, focus trap completo e escape key.
 *
 * WCAG 2.1 AA compliant:
 * - Focus trap: Tab/Shift+Tab cicla dentro do modal
 * - Retorno de foco ao elemento trigger ao fechar
 * - Escape key para fechar
 * - aria-modal, aria-labelledby, aria-describedby
 * - Usa z-modal para layering consistente
 *
 * @see docs/DESIGN-SYSTEM.md §4.11
 */
export function Modal({
  open,
  onClose,
  title,
  description,
  size = 'md',
  children,
  footer,
  className,
}: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<Element | null>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  // Capture the trigger element and manage dialog open/close
  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open) {
      triggerRef.current = document.activeElement;
      if (!dialog.open) dialog.showModal();
      // Focus first focusable element inside the modal content
      requestAnimationFrame(() => {
        const content = contentRef.current;
        if (!content) return;
        const firstFocusable = content.querySelector<HTMLElement>(FOCUSABLE_SELECTOR);
        if (firstFocusable) firstFocusable.focus();
      });
    } else {
      dialog.close();
      // Return focus to trigger element
      if (triggerRef.current instanceof HTMLElement) {
        triggerRef.current.focus();
        triggerRef.current = null;
      }
    }
  }, [open]);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    const handleCancel = (e: Event) => {
      e.preventDefault();
      onClose();
    };

    dialog.addEventListener('cancel', handleCancel);
    return () => dialog.removeEventListener('cancel', handleCancel);
  }, [onClose]);

  // Focus trap: Tab/Shift+Tab cycles within modal
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key !== 'Tab') return;
      const content = contentRef.current;
      if (!content) return;

      const focusableEls = Array.from(content.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR));
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

  const titleId = title ? 'nto-modal-title' : undefined;
  const descId = description ? 'nto-modal-desc' : undefined;

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby={titleId}
      aria-describedby={descId}
      className={cn(
        'fixed inset-0 z-[var(--z-modal)] m-0 h-full w-full max-h-full max-w-full',
        'bg-transparent backdrop:bg-overlay',
        'flex items-center justify-center p-4',
      )}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
      onKeyDown={handleKeyDown}
    >
      <div
        ref={contentRef}
        className={cn(
          'w-full rounded-lg bg-panel border border-edge shadow-floating',
          'animate-slide-up',
          sizeClasses[size],
          className,
        )}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        {(title || description) && (
          <div className="flex items-start justify-between gap-4 border-b border-divider px-6 py-4">
            <div>
              {title && <h2 id={titleId} className="text-lg font-semibold text-heading">{title}</h2>}
              {description && <p id={descId} className="text-sm text-muted mt-1">{description}</p>}
            </div>
            <button
              type="button"
              onClick={onClose}
              className="rounded-sm p-1.5 text-muted hover:text-heading hover:bg-hover transition-colors"
              style={{ transitionDuration: 'var(--nto-motion-fast)' }}
              aria-label="Close"
            >
              <X size={18} />
            </button>
          </div>
        )}

        {/* Body */}
        <div className="px-6 py-5">{children}</div>

        {/* Footer */}
        {footer && (
          <div className="flex items-center justify-end gap-3 border-t border-divider px-6 py-4">
            {footer}
          </div>
        )}
      </div>
    </dialog>
  );
}
