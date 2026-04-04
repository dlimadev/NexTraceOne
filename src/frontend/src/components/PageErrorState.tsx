import { AlertTriangle } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '../lib/cn';
import type { ReactNode } from 'react';

interface PageErrorStateProps {
  /** Título do erro (padrão: common.error). */
  title?: string;
  /** Mensagem descritiva (padrão: common.errorDescription). */
  message?: string;
  /** Ação de recuperação (botão retry, link back, etc.). */
  action?: ReactNode;
  /** Atalho: callback de retry — renderiza um botão de retry padronizado quando `action` não for fornecido. */
  onRetry?: () => unknown;
  className?: string;
}

/**
 * Estado de erro padronizado para uso em páginas e secções.
 *
 * Combina feedback visual semântico (ícone + cor critical) com
 * mensagem i18n e ação de recuperação opcional.
 * Substituí os vários padrões inline ad-hoc usados nos módulos core.
 *
 * @see docs/DESIGN-SYSTEM.md §4.14
 */
export function PageErrorState({ title, message, action, onRetry, className }: PageErrorStateProps) {
  const { t } = useTranslation();

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center text-center py-12 px-6 animate-fade-in',
        className,
      )}
      role="alert"
    >
      <div className="flex items-center justify-center w-12 h-12 rounded-lg bg-critical/15 border border-critical/25 text-critical mb-3">
        <AlertTriangle size={22} />
      </div>
      <h3 className="text-sm font-semibold text-heading mb-1">
        {title ?? t('common.error')}
      </h3>
      <p className="text-xs text-muted max-w-sm mb-4">
        {message ?? t('common.errorDescription')}
      </p>
      {action ?? (onRetry ? (
        <button
          type="button"
          onClick={() => onRetry()}
          className="text-xs font-medium text-accent hover:underline"
        >
          {t('common.retry')}
        </button>
      ) : null)}
    </div>
  );
}
