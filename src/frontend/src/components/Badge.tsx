import { memo } from 'react';
import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

interface BadgeProps {
  children: ReactNode;
  variant?: 'default' | 'neutral' | 'success' | 'warning' | 'danger' | 'info';
  size?: 'sm' | 'md';
  icon?: ReactNode;
  className?: string;
}

/**
 * Badge semântico — DESIGN-SYSTEM.md §4.9
 * Radius pill, fundo translúcido tonal por variante semântica + borda suave.
 * Alinhado com padrão Template NexLink: border visível, fundo tonal.
 * Altura: 24-28px (md) ou 20-22px (sm), peso 500-600.
 */
const variantClasses: Record<NonNullable<BadgeProps['variant']>, string> = {
  default: 'bg-elevated text-body border border-edge',
  neutral: 'bg-elevated text-muted border border-edge',
  success: 'bg-success/12 text-success border border-success/25',
  warning: 'bg-warning/12 text-warning border border-warning/25',
  danger: 'bg-critical/12 text-critical border border-critical/25',
  info: 'bg-info/12 text-info border border-info/25',
};

const sizeClasses: Record<NonNullable<BadgeProps['size']>, string> = {
  sm: 'px-1.5 py-px type-micro',
  md: 'px-2.5 py-0.5 text-xs',
};

export const Badge = memo(function Badge({ children, variant = 'default', size = 'md', icon, className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-pill font-semibold',
        sizeClasses[size],
        variantClasses[variant],
        className,
      )}
    >
      {icon && <span className="shrink-0" aria-hidden="true">{icon}</span>}
      {children}
    </span>
  );
});
