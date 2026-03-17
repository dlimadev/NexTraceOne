import { forwardRef, useState, type InputHTMLAttributes } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { cn } from '../lib/cn';

interface PasswordInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  /** Label exibida acima do campo. */
  label?: string;
  /** Texto de ajuda abaixo do campo. */
  helperText?: string;
  /** Mensagem de erro — ativa estilo danger. */
  error?: string;
}

/**
 * Campo de password com toggle mostrar/ocultar — DESIGN-SYSTEM.md §4.4
 *
 * Mesmo padrão visual de TextField: h-14, rounded-lg, bg-input.
 * O botão de toggle é clickável mas fora do tab order para não
 * interromper o fluxo natural do formulário.
 *
 * Segurança percebida: transmite confiança visual e comportamental.
 * Compatível com react-hook-form via forwardRef.
 */
export const PasswordInput = forwardRef<HTMLInputElement, PasswordInputProps>(
  function PasswordInput({ label, helperText, error, className, id, ...rest }, ref) {
    const [visible, setVisible] = useState(false);
    const fieldId = id ?? label?.toLowerCase().replace(/\s+/g, '-');
    const hasError = Boolean(error);

    return (
      <div className="space-y-2">
        {label && (
          <label htmlFor={fieldId} className="block text-sm font-medium text-body">
            {label}
          </label>
        )}
        <div className="relative">
          <input
            ref={ref}
            id={fieldId}
            type={visible ? 'text' : 'password'}
            autoComplete={rest.autoComplete ?? 'current-password'}
            className={cn(
              'w-full h-14 rounded-lg bg-input border text-sm text-heading placeholder:text-faded',
              'transition-colors pl-4 pr-12',
              'focus:outline-none',
              hasError
                ? 'border-critical/60 focus:border-critical focus:shadow-glow-danger'
                : 'border-edge hover:border-edge-strong focus:border-edge-focus focus:shadow-glow-cyan',
              'disabled:opacity-50 disabled:cursor-not-allowed',
              className,
            )}
            style={{ transitionDuration: 'var(--nto-motion-base)' }}
            aria-invalid={hasError || undefined}
            aria-describedby={
              hasError ? `${fieldId}-error` : helperText ? `${fieldId}-helper` : undefined
            }
            {...rest}
          />
          <button
            type="button"
            tabIndex={-1}
            onClick={() => setVisible((v) => !v)}
            className="absolute right-3 top-1/2 -translate-y-1/2 p-1.5 text-faded hover:text-body transition-colors rounded-sm"
            style={{ transitionDuration: 'var(--nto-motion-fast)' }}
            aria-label={visible ? 'Hide password' : 'Show password'}
          >
            {visible ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>
        {hasError && (
          <p id={`${fieldId}-error`} className="text-xs text-critical" role="alert">
            {error}
          </p>
        )}
        {!hasError && helperText && (
          <p id={`${fieldId}-helper`} className="text-xs text-faded">
            {helperText}
          </p>
        )}
      </div>
    );
  },
);
