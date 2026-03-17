import { forwardRef, type InputHTMLAttributes } from 'react';
import { cn } from '../lib/cn';

interface RadioProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  /** Label exibida ao lado do radio. */
  label?: string;
  /** Descrição adicional abaixo da label. */
  description?: string;
}

/**
 * Radio button base com tokens NTO.
 *
 * @see docs/DESIGN-SYSTEM.md §4.6
 */
export const Radio = forwardRef<HTMLInputElement, RadioProps>(
  function Radio({ label, description, className, id, ...rest }, ref) {
    const fieldId = id ?? undefined;

    return (
      <label
        htmlFor={fieldId}
        className={cn(
          'inline-flex items-start gap-3 cursor-pointer select-none',
          rest.disabled && 'opacity-50 cursor-not-allowed',
          className,
        )}
      >
        <input
          ref={ref}
          id={fieldId}
          type="radio"
          className={cn(
            'mt-0.5 h-5 w-5 border border-edge bg-input',
            'accent-cyan',
            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring',
            'disabled:cursor-not-allowed',
          )}
          style={{ transitionDuration: 'var(--nto-motion-fast)' }}
          {...rest}
        />
        {(label || description) && (
          <div>
            {label && <span className="text-sm text-body">{label}</span>}
            {description && <p className="text-xs text-muted mt-0.5">{description}</p>}
          </div>
        )}
      </label>
    );
  },
);
