import { forwardRef, type InputHTMLAttributes, type ReactNode } from 'react';
import { cn } from '../lib/cn';

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  helperText?: string;
  error?: string;
  leadingIcon?: ReactNode;
  trailingIcon?: ReactNode;
}

/**
 * Input padrão — DESIGN-SYSTEM.md §4.4
 * Altura 56px (h-14), radius-lg (18px→rounded-lg), bg-input, borda soft.
 * Focus: border-strong + glow cyan. Error: border danger + mensagem textual.
 */
export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(
  ({ label, helperText, error, leadingIcon, trailingIcon, className, id, ...rest }, ref) => {
    const fieldId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className="space-y-2">
        {label && (
          <label className="block text-sm font-medium text-body" htmlFor={fieldId}>
            {label}
          </label>
        )}
        <div className="relative">
          {leadingIcon && (
            <span className="absolute left-4 top-1/2 -translate-y-1/2 text-faded pointer-events-none">
              {leadingIcon}
            </span>
          )}
          <input
            ref={ref}
            id={fieldId}
            className={cn(
              'w-full h-14 rounded-lg bg-input border text-sm text-heading placeholder:text-faded',
              'transition-all duration-[var(--nto-motion-base)]',
              'focus:outline-none',
              leadingIcon ? 'pl-11' : 'pl-4',
              trailingIcon ? 'pr-11' : 'pr-4',
              error
                ? 'border-critical/60 focus:border-critical focus:shadow-glow-danger'
                : 'border-edge focus:border-edge-focus focus:shadow-glow-cyan',
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
