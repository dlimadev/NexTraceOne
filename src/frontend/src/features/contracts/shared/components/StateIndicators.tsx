import type { ReactNode } from 'react';
import { Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface LoadingStateProps {
  message?: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

/**
 * Estado de carregamento reutilizável — spinner + mensagem i18n.
 */
export function LoadingState({ message, size = 'md', className = '' }: LoadingStateProps) {
  const { t } = useTranslation();
  const spinnerSize = size === 'sm' ? 16 : size === 'md' ? 24 : 32;
  const padding = size === 'sm' ? 'py-6' : size === 'md' ? 'py-12' : 'py-16';

  return (
    <div className={`flex flex-col items-center justify-center ${padding} ${className}`}>
      <Loader2 size={spinnerSize} className="animate-spin text-accent mb-2" />
      <span className="text-xs text-muted">{message ?? t('common.loading')}</span>
    </div>
  );
}

interface ErrorStateProps {
  message?: string;
  onRetry?: () => void;
  className?: string;
}

/**
 * Estado de erro reutilizável — mensagem + acção de retry.
 */
export function ErrorState({ message, onRetry, className = '' }: ErrorStateProps) {
  const { t } = useTranslation();

  return (
    <div className={`flex flex-col items-center justify-center py-12 ${className}`}>
      <div className="w-12 h-12 rounded-full bg-red-900/20 border border-red-700/30 flex items-center justify-center mb-3">
        <span className="text-red-400 text-lg font-bold">!</span>
      </div>
      <p className="text-sm text-heading mb-1">{t('common.error')}</p>
      <p className="text-xs text-muted max-w-sm text-center mb-3">
        {message ?? t('common.errorDescription')}
      </p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="px-3 py-1.5 text-xs font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors"
        >
          {t('common.retry')}
        </button>
      )}
    </div>
  );
}
