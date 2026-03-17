import type { ReactNode, ButtonHTMLAttributes } from 'react';
import { cn } from '../lib/cn';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  children: ReactNode;
}

/**
 * Mapa de variantes visuais — DESIGN-SYSTEM.md §4.3
 *
 * - primary: CTA institucional com gradient, texto escuro (on-accent)
 * - secondary: superfície elevada, borda suave, hover com brilho discreto
 * - danger: fundo crítico translúcido, para ações destrutivas
 * - ghost: sem fundo, texto muted — hover revela superfície
 */
const variantClasses: Record<string, string> = {
  primary:
    'cta-gradient text-on-accent shadow-sm hover:brightness-110 hover:shadow-glow-sm disabled:opacity-40',
  secondary:
    'bg-elevated text-body border border-edge hover:border-edge-strong hover:bg-hover disabled:opacity-40',
  danger:
    'bg-critical/15 text-critical border border-critical/25 hover:bg-critical/20 disabled:opacity-40',
  ghost:
    'text-muted hover:bg-hover hover:text-body',
};

const sizeClasses: Record<string, string> = {
  sm: 'h-9 px-4 text-sm',
  md: 'h-11 px-5 text-sm',
  lg: 'h-14 px-6 text-base font-bold',
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
        'inline-flex items-center justify-center gap-2 rounded-lg font-semibold',
        'transition-all duration-[var(--nto-motion-base)]',
        'focus:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
        'disabled:cursor-not-allowed',
        variantClasses[variant],
        sizeClasses[size],
        className,
      )}
      disabled={disabled || loading}
      {...rest}
    >
      {loading && (
        <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4l3-3-3-3V4a8 8 0 00-8 8h4z" />
        </svg>
      )}
      {children}
    </button>
  );
}
