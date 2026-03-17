import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface ContextStripProps {
  children: ReactNode;
  className?: string;
}

export function ContextStrip({ children, className }: ContextStripProps) {
  return (
    <div
      className={cn(
        'flex items-center gap-3 flex-wrap px-5 lg:px-8 py-3',
        'border-b border-edge bg-deep/40',
        className,
      )}
    >
      {children}
    </div>
  );
}
