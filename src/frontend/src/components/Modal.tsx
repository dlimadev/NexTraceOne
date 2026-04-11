import { useEffect, useRef, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
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

/**
 * Modal dialog enterprise com overlay, focus trap básico e escape key.
 *
 * Usa z-modal para layering consistente.
 * Overlay com bg-overlay para integração com o dark theme.
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
  const { t } = useTranslation();
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open) {
      if (!dialog.open) dialog.showModal();
    } else {
      dialog.close();
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

  if (!open) return null;

  return (
    <dialog
      ref={dialogRef}
      className={cn(
        'fixed inset-0 z-[var(--z-modal)] m-0 h-full w-full max-h-full max-w-full',
        'bg-transparent backdrop:bg-overlay',
        'flex items-center justify-center p-4',
      )}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div
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
              {title && <h2 className="text-lg font-semibold text-heading">{title}</h2>}
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
