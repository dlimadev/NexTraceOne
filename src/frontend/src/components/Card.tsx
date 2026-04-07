import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/cn';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  /** Variante visual do card. */
  variant?: 'default' | 'interactive' | 'elevated' | 'flat';
  /** Exibe skeleton de loading sobre o conteúdo. */
  loading?: boolean;
  className?: string;
}

const variantClasses: Record<NonNullable<CardProps['variant']>, string> = {
  default: 'bg-card rounded-2xl border border-edge shadow-surface overflow-hidden',
  interactive:
    'bg-card rounded-2xl border border-edge shadow-surface overflow-hidden cursor-pointer hover:shadow-elevated hover:border-edge-strong transition-all duration-[var(--nto-motion-base)]',
  elevated: 'bg-card rounded-2xl border border-edge shadow-elevated overflow-hidden',
  flat: 'bg-card rounded-2xl overflow-hidden',
};

/**
 * Card base — superfície elevada sobre canvas profundo navy.
 * DESIGN-SYSTEM.md §4.7: radius-xl (18px), borda soft translúcida, shadow-surface.
 *
 * Variantes:
 * - default: card padrão com borda e sombra surface
 * - interactive: hover com elevação e cursor pointer
 * - elevated: sombra mais forte permanente
 * - flat: sem borda/sombra
 * - loading: exibe skeleton overlay
 */
export function Card({ children, variant = 'default', loading, className, ...rest }: CardProps) {
  return (
    <div className={cn(variantClasses[variant], className)} {...rest}>
      {loading ? (
        <div className="px-5 py-5 space-y-3">
          <div className="skeleton h-4 w-3/4 rounded-sm" />
          <div className="skeleton h-4 w-1/2 rounded-sm" />
          <div className="skeleton h-8 w-full rounded-sm" />
        </div>
      ) : (
        children
      )}
    </div>
  );
}

export function CardHeader({ children, className, ...rest }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return (
    <div className={cn('px-5 py-4 border-b border-edge/60', className)} {...rest}>
      {children}
    </div>
  );
}

export function CardBody({ children, className, ...rest }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div className={cn('px-5 py-5', className)} {...rest}>{children}</div>;
}
