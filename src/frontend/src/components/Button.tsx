import type { ReactNode, ButtonHTMLAttributes } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  loading?: boolean;
  children: ReactNode;
}

/**
 * Mapa de variantes visuais alinhado ao tema dark enterprise.
 * - primary: fundo accent (azul marca), texto claro
 * - secondary: fundo card (superfície elevada), borda sutil
 * - danger: fundo crítico, para ações destrutivas
 * - ghost: sem fundo, apenas texto — hover revela superfície
 */
const variantClasses: Record<string, string> = {
  primary: 'bg-accent text-heading hover:bg-accent-hover disabled:opacity-40',
  secondary: 'bg-card text-body border border-edge hover:bg-hover disabled:opacity-40',
  danger: 'bg-critical text-heading hover:brightness-110 disabled:opacity-40',
  ghost: 'text-muted hover:bg-hover hover:text-body',
};

const sizeClasses: Record<string, string> = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2 text-sm',
  lg: 'px-6 py-3 text-base',
};

export function Button({
  variant = 'primary',
  size = 'md',
  loading,
  children,
  disabled,
  className = '',
  ...rest
}: ButtonProps) {
  return (
    <button
      className={`inline-flex items-center justify-center gap-2 rounded-md font-medium transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-accent focus-visible:ring-offset-2 focus-visible:ring-offset-canvas disabled:cursor-not-allowed ${variantClasses[variant]} ${sizeClasses[size]} ${className}`}
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
