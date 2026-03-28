import type { ReactNode, ButtonHTMLAttributes } from 'react';
import { cn } from '../lib/cn';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'subtle' | 'institutional';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  children: ReactNode;
}

/**
 * Mapa de variantes visuais — DESIGN-SYSTEM.md §4.3
 *
 * - institutional: CTA de nível institucional (SSO, ações de alto destaque)
 *   Usa azul profundo (#1B7FE8) — separado do cyan para hierarquia clara.
 * - primary: CTA operacional com gradient cyan, texto escuro (on-accent)
 * - secondary: superfície elevada, borda suave, hover com brilho discreto
 * - danger: fundo crítico translúcido, para ações destrutivas
 * - ghost: sem fundo, texto muted — hover revela superfície
 * - subtle: fundo accent-muted, tom suave
 */
const variantClasses: Record<NonNullable<ButtonProps['variant']>, string> = {
  institutional:
    'blue-gradient text-white shadow-md hover:brightness-110 hover:shadow-[0_0_24px_rgba(27,127,232,0.30)] disabled:opacity-40',
  primary:
    'cta-gradient text-on-accent shadow-sm hover:brightness-110 hover:shadow-glow-sm disabled:opacity-40',
  secondary:
    'bg-elevated text-body border border-edge hover:border-edge-strong hover:bg-hover disabled:opacity-40',
  danger:
    'bg-critical/15 text-critical border border-critical/25 hover:bg-critical/20 disabled:opacity-40',
  ghost:
    'text-muted hover:bg-hover hover:text-body disabled:opacity-40',
  subtle:
    'bg-accent-muted text-body hover:bg-accent/15 hover:text-heading disabled:opacity-40',
};

const sizeClasses: Record<NonNullable<ButtonProps['size']>, string> = {
  sm: 'h-9 px-4 text-sm gap-1.5',
  md: 'h-11 px-5 text-sm gap-2',
  lg: 'h-14 px-7 text-base font-bold gap-2.5',
};

export function Button({
  variant = 'primary',
  size = 'md',
  loading,
  children,
  disabled,
  className,
  ...rest
}: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex items-center justify-center rounded-lg font-semibold',
        'transition-all duration-[var(--nto-motion-base)]',
        'focus:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
        'disabled:cursor-not-allowed select-none',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      disabled={disabled || loading}
      {...rest}
    >
      {loading && (
        <svg className="h-4 w-4 animate-spin shrink-0" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3V4a8 8 0 00-8 8h4z" />
        </svg>
      )}
      {children}
    </button>
  );
}
