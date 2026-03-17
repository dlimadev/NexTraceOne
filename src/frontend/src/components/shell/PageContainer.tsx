import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface PageContainerProps {
  children: ReactNode;
  className?: string;
  /** Full-width layout (no max-width constraint). */
  fluid?: boolean;
}

export function PageContainer({ children, className, fluid = false }: PageContainerProps) {
  return (
    <div
      className={cn(
        'px-5 lg:px-8 py-6 animate-fade-in',
        !fluid && 'max-w-[1600px]',
        className,
      )}
    >
      {children}
    </div>
  );
}
