import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

interface FormFieldProps {
  /** Label do campo. */
  label: string;
  /** ID do campo de input (para htmlFor no label). */
  htmlFor?: string;
  /** Mensagem de erro — ativa estilo danger. */
  error?: string;
  /** Texto de ajuda abaixo do campo. */
  helperText?: string;
  /** Indica campo obrigatório com asterisco. */
  required?: boolean;
  /** Conteúdo: input, select, textarea, etc. */
  children: ReactNode;
  className?: string;
}

/**
 * Wrapper padronizado para campos de formulário.
 *
 * Envolve qualquer input com label, mensagem de erro, helper text
 * e indicador de obrigatório. Reduz boilerplate em formulários.
 *
 * Compatível com react-hook-form Controller.
 */
export function FormField({
  label,
  htmlFor,
  error,
  helperText,
  required = false,
  children,
  className,
}: FormFieldProps) {
  const errorId = htmlFor ? `${htmlFor}-error` : undefined;
  const helperId = htmlFor ? `${htmlFor}-helper` : undefined;

  return (
    <div className={cn('flex flex-col gap-1.5', className)}>
      <label
        htmlFor={htmlFor}
        className="text-sm font-medium text-body"
      >
        {label}
        {required && <span className="text-danger ml-0.5" aria-hidden="true">*</span>}
      </label>

      {children}

      {error && (
        <p id={errorId} className="text-xs text-danger" role="alert">
          {error}
        </p>
      )}
      {!error && helperText && (
        <p id={helperId} className="text-xs text-muted">
          {helperText}
        </p>
      )}
    </div>
  );
}
