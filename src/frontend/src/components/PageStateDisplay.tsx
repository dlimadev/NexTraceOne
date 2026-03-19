import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { PageLoadingState } from './PageLoadingState';
import { PageErrorState } from './PageErrorState';
import { EmptyState } from './EmptyState';
import { Button } from './Button';

interface PageStateDisplayProps {
  isLoading?: boolean;
  isError?: boolean;
  isEmpty?: boolean;
  /** Custom error message. Falls back to common.errorDescription. */
  errorMessage?: string;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: ReactNode;
  /** Called when the retry button is clicked (only shown when isError is true). */
  onRetry?: () => void;
  loadingMessage?: string;
  size?: 'sm' | 'md' | 'lg';
  children?: ReactNode;
}

/**
 * Componente unificado para estados de página: loading, erro e vazio.
 *
 * Precedência: isLoading > isError > isEmpty > children.
 * Usa PageLoadingState, PageErrorState e EmptyState internamente.
 *
 * @see docs/DESIGN-SYSTEM.md §4.14
 */
export function PageStateDisplay({
  isLoading,
  isError,
  isEmpty,
  errorMessage,
  emptyTitle,
  emptyDescription,
  emptyAction,
  onRetry,
  loadingMessage,
  size,
  children,
}: PageStateDisplayProps) {
  const { t } = useTranslation();

  if (isLoading) {
    return <PageLoadingState message={loadingMessage} size={size} />;
  }

  if (isError) {
    return (
      <PageErrorState
        message={errorMessage}
        action={
          onRetry ? (
            <Button variant="secondary" size="sm" onClick={onRetry}>
              {t('common.retry')}
            </Button>
          ) : undefined
        }
      />
    );
  }

  if (isEmpty) {
    return (
      <EmptyState
        title={emptyTitle ?? t('common.noData')}
        description={emptyDescription}
        action={emptyAction}
      />
    );
  }

  return <>{children}</>;
}
