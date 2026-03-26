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
 * Radius pill, fundo translúcido tonal por variante semântica.
 * Altura: 24-28px (md) ou 20-22px (sm), peso 500-600.
 */
const variantClasses: Record<NonNullable<BadgeProps['variant']>, string> = {
  default: 'bg-elevated text-body',
  neutral: 'bg-elevated text-body',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/15 text-warning',
  danger: 'bg-critical/15 text-critical',
  info: 'bg-info/15 text-info',
};

const sizeClasses: Record<NonNullable<BadgeProps['size']>, string> = {
  sm: 'px-1.5 py-px text-[10px]',
  md: 'px-2.5 py-0.5 text-xs',
};

export function Badge({ children, variant = 'default', size = 'md', icon, className }: BadgeProps) {
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
}
