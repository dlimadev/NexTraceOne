import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';

interface ShellLoaderProps {
  className?: string;
}

export function ShellLoader({ className }: ShellLoaderProps) {
  const { t } = useTranslation();

  return (
    <div className={cn('flex items-center justify-center h-full min-h-[50vh]', className)} role="status">
      <div className="flex flex-col items-center gap-4">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
        <p className="text-sm text-muted">{t('common.loading')}</p>
      </div>
    </div>
  );
}
