import type { HTMLAttributes, ReactNode } from 'react';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  className?: string;
}

export function Card({ children, className = '', ...rest }: CardProps) {
  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 ${className}`} {...rest}>
      {children}
    </div>
  );
}

export function CardHeader({ children, className = '', ...rest }: CardProps) {
  return (
    <div className={`px-6 py-4 border-b border-gray-200 ${className}`} {...rest}>
      {children}
    </div>
  );
}

export function CardBody({ children, className = '', ...rest }: CardProps) {
  return <div className={`px-6 py-4 ${className}`} {...rest}>{children}</div>;
}
