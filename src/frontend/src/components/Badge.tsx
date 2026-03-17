import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

interface BadgeProps {
  children: ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info';
  className?: string;
}

/**
 * Badge semântico — DESIGN-SYSTEM.md §4.9
 * Radius pill, fundo translúcido tonal por variante semântica.
 * Altura: 24-28px, peso 500-600.
 */
const variantClasses: Record<string, string> = {
  default: 'bg-elevated text-body',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/15 text-warning',
  danger: 'bg-critical/15 text-critical',
  info: 'bg-info/15 text-info',
};

export function Badge({ children, variant = 'default', className }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-pill px-2.5 py-0.5 text-xs font-semibold',
        variantClasses[variant],
        className,
      )}
    >
      {children}
    </span>
  );
}
