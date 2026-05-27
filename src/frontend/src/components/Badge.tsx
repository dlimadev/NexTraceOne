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
  default: 'bg-elevated text-body border border-edge',
  neutral: 'bg-elevated text-muted border border-edge',
  success: 'bg-success/12 text-success border border-success/25',
  warning: 'bg-warning/12 text-warning border border-warning/25',
  danger: 'bg-critical/12 text-critical border border-critical/25',
  info: 'bg-info/12 text-info border border-info/25',
  // Aliases
  secondary: 'bg-elevated text-muted border border-edge',
  error: 'bg-critical/12 text-critical border border-critical/25',
  destructive: 'bg-critical/12 text-critical border border-critical/25',
  critical: 'bg-critical/12 text-critical border border-critical/25',
  outline: 'bg-transparent text-body border border-edge',
  muted: 'bg-elevated text-muted border border-edge',
  primary: 'bg-info/12 text-info border border-info/25',
  blue: 'bg-info/12 text-info border border-info/25',
  gray: 'bg-elevated text-muted border border-edge',
  green: 'bg-success/12 text-success border border-success/25',
  yellow: 'bg-warning/12 text-warning border border-warning/25',
  purple: 'bg-info/12 text-info border border-info/25',
};

const sizeClasses: Record<NonNullable<BadgeProps['size']>, string> = {
  xs: 'px-1 py-px text-[10px]',
  sm: 'px-1.5 py-px type-micro',
  md: 'px-2.5 py-0.5 text-xs',
};

export const Badge = memo(function Badge({ children, variant = 'default', size = 'md', icon, dot, pulsing, className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-pill font-semibold',
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
