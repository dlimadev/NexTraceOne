import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';
import { cn } from '../lib/cn';

interface TextFieldProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label?: string;
  helperText?: string;
  error?: string;
  leadingIcon?: ReactNode;
  trailingIcon?: ReactNode;
  /** Altura do campo: 'md' (56px, padrão) ou 'sm' (36px, p/ toolbars/filtros). */
  size?: 'sm' | 'md';
}

/**
 * Input padrão — DESIGN-SYSTEM.md §4.4
 * Altura 56px (h-14), radius-lg (18px→rounded-lg), bg-input, borda soft.
 * Focus: border-strong + glow cyan. Error: border danger + mensagem textual.
 */
export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(
  ({ label, helperText, error, leadingIcon, trailingIcon, size = 'md', className, id, ...rest }, ref) => {
    const fieldId = id ?? label?.toLowerCase().replace(/\s+/g, '-');
    const isSm = size === 'sm';

    return (
      <div className="space-y-2">
        {label && (
          <label className="block text-sm font-medium text-body" htmlFor={fieldId}>
            {label}
          </label>
        )}
        <div className="relative">
          {leadingIcon && (
            <span className={cn('absolute top-1/2 -translate-y-1/2 text-faded pointer-events-none', isSm ? 'left-3' : 'left-4')}>
              {leadingIcon}
            </span>
          )}
          <input
            ref={ref}
            id={fieldId}
            className={cn(
              'w-full rounded-lg bg-input border text-sm text-heading placeholder:text-faded',
              isSm ? 'h-9' : 'h-14',
              'transition-all duration-[var(--nto-motion-base)]',
              'focus:outline-none',
              leadingIcon ? (isSm ? 'pl-9' : 'pl-11') : (isSm ? 'pl-3' : 'pl-4'),
              trailingIcon ? (isSm ? 'pr-9' : 'pr-11') : (isSm ? 'pr-3' : 'pr-4'),
              error
                ? 'border-critical/60 focus:border-critical focus:shadow-sm'
                : 'border-edge focus:border-edge-focus focus:shadow-sm',
              className,
            )}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? `${fieldId}-error` : helperText ? `${fieldId}-helper` : undefined}
            {...rest}
          />
          {trailingIcon && (
            <span className="absolute right-4 top-1/2 -translate-y-1/2">
              {trailingIcon}
            </span>
          )}
        </div>
        {error && (
          <p id={`${fieldId}-error`} className="text-xs text-critical" role="alert">
            {error}
          </p>
        )}
        {!error && helperText && (
          <p id={`${fieldId}-helper`} className="text-xs text-faded">
            {helperText}
          </p>
        )}
      </div>
    );
  },
);

TextField.displayName = 'TextField';
