import type { ReactNode } from 'react';

interface BadgeProps {
  children: ReactNode;
  variant?: 'default' | 'success' | 'warning' | 'danger' | 'info';
  className?: string;
}

/**
 * Variantes semânticas com fundo translúcido sobre superfícies escuras.
 * Cada cor de status usa opacidade de 15% para manter legibilidade sem poluir o layout.
 */
const variantClasses: Record<string, string> = {
  default: 'bg-elevated text-body',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/15 text-warning',
  danger: 'bg-critical/15 text-critical',
  info: 'bg-info/15 text-info',
};

export function Badge({ children, variant = 'default', className = '' }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${variantClasses[variant]} ${className}`}
    >
      {children}
    </span>
  );
}
