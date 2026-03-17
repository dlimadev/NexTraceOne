import { forwardRef, type SelectHTMLAttributes } from 'react';
import { ChevronDown } from 'lucide-react';
import { cn } from '../lib/cn';

interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'size'> {
  /** Label exibida acima do select. */
  label?: string;
  /** Opções do select. */
  options: SelectOption[];
  /** Placeholder (primeiro item desabilitado). */
  placeholder?: string;
  /** Texto de ajuda abaixo do campo. */
  helperText?: string;
  /** Mensagem de erro — ativa estilo danger. */
  error?: string;
  /** Tamanho visual. */
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Select nativo estilizado com tokens NTO.
 *
 * Usa o mesmo padrão visual de TextField (h-14, rounded-lg, bg-input)
 * para manter consistência absoluta nos formulários.
 *
 * @see docs/DESIGN-SYSTEM.md §4.5
 */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  function Select(
    { label, options, placeholder, helperText, error, size = 'md', className, id, ...rest },
    ref,
  ) {
    const fieldId = id ?? (label ? `select-${label.toLowerCase().replace(/\s/g, '-')}` : undefined);

    const sizeClasses = {
      sm: 'h-9 text-sm',
      md: 'h-11 text-sm',
      lg: 'h-14 text-base',
    };

    const hasError = Boolean(error);

    return (
      <div className={cn('flex flex-col gap-1.5', className)}>
        {label && (
          <label htmlFor={fieldId} className="text-sm font-medium text-body">
            {label}
          </label>
        )}
        <div className="relative">
          <select
            ref={ref}
            id={fieldId}
            aria-invalid={hasError || undefined}
            aria-describedby={
              hasError
                ? `${fieldId}-error`
                : helperText
                  ? `${fieldId}-helper`
                  : undefined
            }
            className={cn(
              'w-full appearance-none rounded-lg bg-input border px-4 pr-10 text-heading',
              'transition-colors',
              'focus:outline-none focus:border-edge-focus focus:shadow-glow-cyan',
              'disabled:opacity-50 disabled:cursor-not-allowed',
              sizeClasses[size],
              hasError
                ? 'border-danger shadow-glow-danger'
                : 'border-edge hover:border-edge-strong',
            )}
            style={{ transitionDuration: 'var(--nto-motion-fast)' }}
            {...rest}
          >
            {placeholder && (
              <option value="" disabled>
                {placeholder}
              </option>
            )}
            {options.map((opt) => (
              <option key={opt.value} value={opt.value} disabled={opt.disabled}>
                {opt.label}
              </option>
            ))}
          </select>
          <ChevronDown
            size={16}
            className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-muted"
          />
        </div>
        {hasError && (
          <p id={`${fieldId}-error`} className="text-xs text-danger" role="alert">
            {error}
          </p>
        )}
        {!hasError && helperText && (
          <p id={`${fieldId}-helper`} className="text-xs text-muted">
            {helperText}
          </p>
        )}
      </div>
    );
  },
);
