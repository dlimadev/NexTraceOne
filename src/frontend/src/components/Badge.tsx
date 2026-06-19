import { memo } from 'react';
import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

export interface BadgeProps {
  children: ReactNode;
  variant?: 'default' | 'neutral' | 'success' | 'warning' | 'danger' | 'info'
    // Aliases for legacy call sites
    | 'secondary' | 'error' | 'destructive' | 'critical' | 'outline' | 'muted'
    | 'primary' | 'blue' | 'gray' | 'green' | 'yellow' | 'purple';
  size?: 'xs' | 'sm' | 'md';
  icon?: ReactNode;
  /** Exibe um indicador circular (5×5px) antes do texto, na cor da variante. */
  dot?: boolean;
  /** Anima o dot com pulse (requer dot=true). Útil para estados críticos/activos. */
  pulsing?: boolean;
  className?: string;
}

/**
 * Badge semântico — DESIGN-SYSTEM.md §4.9
 * Radius pill, fundo translúcido tonal por variante semântica + borda suave.
 * Alinhado com padrão Template NexLink: border visível, fundo tonal.
 * Altura: 24-28px (md) ou 20-22px (sm), peso 500-600.
 *
 * dot=true: indicador circular (5×5px, background: currentColor) antes do texto.
 * pulsing=true: anima o dot com pulse-badge 1.5s ease-in-out infinite.
 */
const variantClasses: Record<NonNullable<BadgeProps['variant']>, string> = {
  default: 'bg-elevated text-muted',
  neutral: 'bg-elevated text-muted',
  success: 'bg-success-muted text-success',
  warning: 'bg-warning-muted text-warning',
  danger: 'bg-critical-muted text-critical',
  info: 'bg-info-muted text-info',
  // Aliases
  secondary: 'bg-elevated text-muted',
  error: 'bg-critical-muted text-critical',
  destructive: 'bg-critical-muted text-critical',
  critical: 'bg-critical-muted text-critical',
  outline: 'bg-transparent text-body border border-edge',
  muted: 'bg-elevated text-muted',
  primary: 'bg-accent-muted text-accent',
  blue: 'bg-accent-muted text-accent',
  gray: 'bg-elevated text-muted',
  green: 'bg-success-muted text-success',
  yellow: 'bg-warning-muted text-warning',
  purple: 'bg-accent-muted text-accent',
};

const sizeClasses: Record<NonNullable<BadgeProps['size']>, string> = {
  xs: 'px-1 py-px text-[9px]',
  sm: 'px-1.5 py-px text-[10px]',
  md: 'px-2 py-0.5 text-[11px]',
};

export const Badge = memo(function Badge({ children, variant = 'default', size = 'md', icon, dot, pulsing, className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full font-medium tracking-wide',
        sizeClasses[size],
        variantClasses[variant],
        className,
      )}
    >
      {dot && (
        <span
          data-testid="badge-dot"
          style={{
            width: 5,
            height: 5,
            borderRadius: '50%',
            background: 'currentColor',
            flexShrink: 0,
            animation: pulsing ? 'pulse-badge 1.5s ease-in-out infinite' : undefined,
          }}
          aria-hidden="true"
        />
      )}
      {icon && !dot && <span className="shrink-0" aria-hidden="true">{icon}</span>}
      {children}
    </span>
  );
});
