import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/cn';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  className?: string;
}

/**
 * Card base — superfície elevada sobre canvas profundo navy.
 * DESIGN-SYSTEM.md §4.7: padding 24px, radius-lg (18px), borda soft translúcida.
 */
export function Card({ children, className, ...rest }: CardProps) {
  return (
    <div className={cn('bg-card rounded-lg border border-edge shadow-surface', className)} {...rest}>
      {children}
    </div>
  );
}

export function CardHeader({ children, className, ...rest }: CardProps) {
  return (
    <div className={cn('px-6 py-5 border-b border-divider', className)} {...rest}>
      {children}
    </div>
  );
}

export function CardBody({ children, className, ...rest }: CardProps) {
  return <div className={cn('px-6 py-5', className)} {...rest}>{children}</div>;
}
