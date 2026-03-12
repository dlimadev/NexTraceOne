import type { HTMLAttributes, ReactNode } from 'react';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  className?: string;
}

/**
 * Card base — superfície elevada sobre o canvas escuro.
 * Usa bg-card com borda edge para separação visual discreta.
 */
export function Card({ children, className = '', ...rest }: CardProps) {
  return (
    <div className={`bg-card rounded-lg shadow-sm border border-edge ${className}`} {...rest}>
      {children}
    </div>
  );
}

export function CardHeader({ children, className = '', ...rest }: CardProps) {
  return (
    <div className={`px-6 py-4 border-b border-edge ${className}`} {...rest}>
      {children}
    </div>
  );
}

export function CardBody({ children, className = '', ...rest }: CardProps) {
  return <div className={`px-6 py-4 ${className}`} {...rest}>{children}</div>;
}
