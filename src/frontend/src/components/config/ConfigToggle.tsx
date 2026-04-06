import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/cn';
import { useConfigValue } from '@/hooks/useConfigValue';

interface ConfigToggleProps {
  /** Configuration key (e.g., "catalog.service.creation.approval_required") */
  configKey: string;
  /** Callback when toggle value changes */
  onChange?: (key: string, value: boolean) => void;
  /** Whether the toggle is read-only */
  readOnly?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Toggle component for boolean configuration parameters.
 * Automatically resolves the i18n label/description and current effective value.
 *
 * @example
 * ```tsx
 * <ConfigToggle
 *   configKey="catalog.service.creation.approval_required"
 *   onChange={(key, value) => handleConfigChange(key, value)}
 * />
 * ```
 */
export function ConfigToggle({ configKey, onChange, readOnly = false, className }: ConfigToggleProps) {
  const { t } = useTranslation();
  const { data, isLoading } = useConfigValue(configKey);

  const isChecked = data?.effectiveValue === 'true';
  const label = t(`config.${configKey}.label`, { defaultValue: configKey });
  const description = t(`config.${configKey}.description`, { defaultValue: '' });

  const handleToggle = () => {
    if (!readOnly && onChange) {
      onChange(configKey, !isChecked);
    }
  };

  if (isLoading) {
    return (
      <div className={cn('flex items-center gap-3 animate-pulse', className)}>
        <div className="h-6 w-11 shrink-0 rounded-full bg-elevated" />
        <div className="flex-1 space-y-1">
          <div className="h-4 w-48 rounded bg-elevated" />
          <div className="h-3 w-72 rounded bg-elevated/60" />
        </div>
      </div>
    );
  }

  return (
    <div className={cn('flex items-start gap-3 py-3', className)}>
      <button
        type="button"
        role="switch"
        aria-checked={isChecked}
        aria-label={label}
        disabled={readOnly}
        onClick={handleToggle}
        className={cn(
          'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full',
          'border-2 border-transparent transition-colors duration-[var(--nto-motion-base)]',
          'focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
          readOnly && 'cursor-not-allowed opacity-50',
          !readOnly && 'cursor-pointer',
          isChecked ? 'bg-cyan' : 'bg-elevated',
        )}
      >
        <span
          className={cn(
            'pointer-events-none inline-block h-5 w-5 rounded-full bg-heading shadow-sm ring-0',
            'transition-transform duration-[var(--nto-motion-base)]',
            isChecked ? 'translate-x-5' : 'translate-x-0.5',
          )}
        />
      </button>
      <div className="min-w-0 flex-1">
        <p className="text-sm font-medium text-heading">{label}</p>
        {description && (
          <p className="mt-0.5 text-xs text-body">{description}</p>
        )}
        {data?.isDefault && (
          <span className="mt-1 inline-flex items-center rounded bg-elevated px-1.5 py-0.5 text-[10px] font-medium text-muted">
            {t('common.default', { defaultValue: 'Default' })}
          </span>
        )}
      </div>
    </div>
  );
}
