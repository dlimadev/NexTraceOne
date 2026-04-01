import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/cn';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  className?: string;
}

/**
 * Card base — superfície elevada sobre canvas profundo navy.
 * DESIGN-SYSTEM.md §4.7: radius-xl (18px), borda soft translúcida, shadow-surface.
 * Hover disponível via className extra quando necessário.
 */
export function Card({ children, className, ...rest }: CardProps) {
  return (
    <div className={cn('bg-card rounded-2xl border border-edge shadow-surface overflow-hidden', className)} {...rest}>
      {children}
    </div>
  );
}

export function CardHeader({ children, className, ...rest }: CardProps) {
  return (
    <div className={cn('px-5 py-4 border-b border-edge/60', className)} {...rest}>
      {children}
    </div>
  );
}

export function CardBody({ children, className, ...rest }: CardProps) {
  return <div className={cn('px-5 py-5', className)} {...rest}>{children}</div>;
}
