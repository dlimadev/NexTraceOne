import { Loader2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '../lib/cn';

interface PageLoadingStateProps {
  /** Mensagem descritiva do loading (padrão: common.loading). */
  message?: string;
  /** Variante de tamanho. */
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

/**
 * Estado de carregamento padronizado para uso em páginas e secções.
 *
 * Garante consistência visual em todos os módulos do produto:
 * spinner animado + mensagem i18n + padding proporcional ao contexto.
 *
 * @see docs/DESIGN-SYSTEM.md §4.14
 */
export function PageLoadingState({ message, size = 'md', className }: PageLoadingStateProps) {
  const { t } = useTranslation();
  const spinnerSize = size === 'sm' ? 16 : size === 'md' ? 20 : 28;
  const padding = size === 'sm' ? 'py-6' : size === 'md' ? 'py-12' : 'py-16';

  return (
    <div className={cn('flex flex-col items-center justify-center animate-fade-in', padding, className)}>
      <Loader2 size={spinnerSize} className="animate-spin text-accent mb-2" />
      <span className="text-xs text-muted">{message ?? t('common.loading')}</span>
    </div>
  );
}
