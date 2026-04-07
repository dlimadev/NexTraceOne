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
  /** Ícone customizado (padrão: AlertTriangle). */
  icon?: ReactNode;
  /** Variante: default (full-page) ou compact (inline, menor padding). */
  variant?: 'default' | 'compact';
  className?: string;
}

/**
 * Estado de erro padronizado para uso em páginas e secções.
 *
 * Combina feedback visual semântico (ícone + cor critical) com
 * mensagem i18n e ação de recuperação opcional.
 *
 * Variantes:
 * - default: uso em páginas inteiras (padding generoso, ícone grande)
 * - compact: uso inline em cards/secções (padding reduzido, ícone pequeno)
 *
 * Substitui tanto o antigo ErrorState como os vários padrões inline ad-hoc.
 *
 * @see docs/DESIGN-SYSTEM.md §4.14
 */
export function PageErrorState({
  title,
  message,
  action,
  onRetry,
  icon,
  variant = 'default',
  className,
}: PageErrorStateProps) {
  const { t } = useTranslation();
  const isCompact = variant === 'compact';

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center text-center animate-fade-in',
        isCompact ? 'py-6 px-4' : 'py-12 px-6',
        className,
      )}
      role="alert"
    >
      <div
        className={cn(
          'flex items-center justify-center rounded-lg bg-critical/15 border border-critical/25 text-critical',
          isCompact ? 'w-10 h-10 mb-2' : 'w-12 h-12 mb-3',
        )}
      >
        {icon ?? <AlertTriangle size={isCompact ? 18 : 22} />}
      </div>
      <h3 className={cn('font-semibold text-heading mb-1', isCompact ? 'text-xs' : 'text-sm')}>
        {title ?? t('common.error')}
      </h3>
      <p className={cn('text-muted mb-4', isCompact ? 'text-xs max-w-xs' : 'text-xs max-w-sm')}>
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
