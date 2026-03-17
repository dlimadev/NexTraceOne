import { forwardRef, type TextareaHTMLAttributes } from 'react';
import { cn } from '../lib/cn';

interface TextAreaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  /** Label exibida acima do campo. */
  label?: string;
  /** Texto de ajuda. */
  helperText?: string;
  /** Mensagem de erro — ativa estilo danger. */
  error?: string;
}

/**
 * TextArea base com tokens NTO.
 *
 * Segue o mesmo padrão de TextField para manter consistência:
 * bg-input, border-edge, rounded-lg, focus com glow-cyan.
 *
 * @see docs/DESIGN-SYSTEM.md §4.4
 */
export const TextArea = forwardRef<HTMLTextAreaElement, TextAreaProps>(
  function TextArea({ label, helperText, error, className, id, ...rest }, ref) {
    const fieldId = id ?? (label ? `textarea-${label.toLowerCase().replace(/\s/g, '-')}` : undefined);
    const hasError = Boolean(error);

    return (
      <div className={cn('flex flex-col gap-1.5', className)}>
        {label && (
          <label htmlFor={fieldId} className="text-sm font-medium text-body">
            {label}
          </label>
        )}
        <textarea
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
            'w-full min-h-[120px] rounded-lg bg-input border px-4 py-3 text-sm text-heading',
            'placeholder:text-muted resize-y',
            'transition-colors',
            'focus:outline-none focus:border-edge-focus focus:shadow-glow-cyan',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            hasError
              ? 'border-danger shadow-glow-danger'
              : 'border-edge hover:border-edge-strong',
          )}
          style={{ transitionDuration: 'var(--nto-motion-fast)' }}
          {...rest}
        />
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
