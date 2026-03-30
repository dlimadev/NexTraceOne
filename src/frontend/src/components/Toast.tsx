import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';
import { CheckCircle2, AlertCircle, Info, AlertTriangle, X } from 'lucide-react';
import { cn } from '../lib/cn';

// ── Types ───────────────────────────────────────────────────────────────

type ToastVariant = 'success' | 'error' | 'warning' | 'info';

interface Toast {
  id: string;
  message: string;
  variant: ToastVariant;
  /** Duração em ms antes de auto-dismiss. */
  duration: number;
}

interface ToastContextValue {
  /** Mostra um toast efémero. */
  toast: (message: string, variant?: ToastVariant, duration?: number) => void;
  /** Atalho para toast de sucesso. */
  toastSuccess: (message: string) => void;
  /** Atalho para toast de erro. */
  toastError: (message: string) => void;
}

// ── Context ─────────────────────────────────────────────────────────────

const ToastContext = createContext<ToastContextValue | null>(null);

/**
 * Hook para aceder ao sistema de toast.
 * Deve ser usado dentro de <ToastProvider>.
 */
export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within <ToastProvider>');
  return ctx;
}

// ── Styles ──────────────────────────────────────────────────────────────

const variantStyles: Record<ToastVariant, { bg: string; icon: ReactNode }> = {
  success: {
    bg: 'bg-success-muted border-success/30 text-success',
    icon: <CheckCircle2 size={18} />,
  },
  error: {
    bg: 'bg-critical-muted border-danger/30 text-danger',
    icon: <AlertCircle size={18} />,
  },
  warning: {
    bg: 'bg-warning-muted border-warning/30 text-warning',
    icon: <AlertTriangle size={18} />,
  },
  info: {
    bg: 'bg-info-muted border-info/30 text-info',
    icon: <Info size={18} />,
  },
};

// ── Provider ────────────────────────────────────────────────────────────

let toastCounter = 0;

/**
 * Provider de toast efémero para feedback visual de ações (sucesso, erro, warning).
 *
 * Deve envolver a App root. Os toasts aparecem no canto superior direito,
 * auto-dismiss após `duration` ms (padrão 4000), e podem ser fechados manualmente.
 *
 * Diferente de InlineMessage (persistente em contexto), Toast é temporário e global.
 *
 * @see docs/DESIGN-SYSTEM.md §4.13
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const addToast = useCallback(
    (message: string, variant: ToastVariant = 'info', duration = 4000) => {
      const id = `toast-${++toastCounter}`;
      const newToast: Toast = { id, message, variant, duration };

      setToasts((prev) => [...prev.slice(-4), newToast]); // Keep last 4 + new = max 5 visible

      if (duration > 0) {
        setTimeout(() => removeToast(id), duration);
      }
    },
    [removeToast],
  );

  const toast = useCallback(
    (message: string, variant?: ToastVariant, duration?: number) =>
      addToast(message, variant, duration),
    [addToast],
  );

  const toastSuccess = useCallback(
    (message: string) => addToast(message, 'success'),
    [addToast],
  );

  const toastError = useCallback(
    (message: string) => addToast(message, 'error', 6000),
    [addToast],
  );

  return (
    <ToastContext.Provider value={{ toast, toastSuccess, toastError }}>
      {children}

      {/* Toast container — fixed top-right */}
      {toasts.length > 0 && (
        <div
          className="fixed top-4 right-4 z-[var(--z-toast,9999)] flex flex-col gap-2 pointer-events-none"
          aria-live="polite"
          aria-label="Notifications"
        >
          {toasts.map((t) => {
            const styles = variantStyles[t.variant];
            return (
              <div
                key={t.id}
                role="status"
                className={cn(
                  'pointer-events-auto flex items-center gap-3 rounded-lg border px-4 py-3 text-sm shadow-lg',
                  'animate-slide-up min-w-[280px] max-w-md',
                  styles.bg,
                )}
              >
                <span className="shrink-0">{styles.icon}</span>
                <span className="flex-1 text-xs font-medium">{t.message}</span>
                <button
                  type="button"
                  onClick={() => removeToast(t.id)}
                  className="shrink-0 p-0.5 rounded hover:bg-black/10 transition-colors"
                  aria-label="Dismiss"
                >
                  <X size={14} />
                </button>
              </div>
            );
          })}
        </div>
      )}
    </ToastContext.Provider>
  );
}
