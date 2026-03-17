import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { cn } from '../../lib/cn';

interface ModuleUnavailableProps {
  className?: string;
  /** i18n key for the module name. */
  moduleKey?: string;
}

export function ModuleUnavailable({ className, moduleKey }: ModuleUnavailableProps) {
  const { t } = useTranslation();

  return (
    <div className={cn('flex items-center justify-center h-full min-h-[40vh]', className)} role="alert">
      <div className="flex flex-col items-center gap-4 max-w-md text-center">
        <div className="w-14 h-14 rounded-xl bg-warning-muted flex items-center justify-center">
          <AlertTriangle size={24} className="text-warning" />
        </div>
        <h2 className="text-lg font-semibold text-heading">{t('shell.moduleUnavailable')}</h2>
        <p className="text-sm text-muted">
          {moduleKey
            ? t('shell.moduleUnavailableDescriptionNamed', { module: t(moduleKey) })
            : t('shell.moduleUnavailableDescription')}
        </p>
      </div>
    </div>
  );
}
