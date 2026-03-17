import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface ContentGridProps {
  children: ReactNode;
  className?: string;
  /** Grid column preset. */
  columns?: 1 | 2 | 3 | 4;
}

const columnClasses: Record<number, string> = {
  1: 'grid-cols-1',
  2: 'grid-cols-1 md:grid-cols-2',
  3: 'grid-cols-1 md:grid-cols-2 xl:grid-cols-3',
  4: 'grid-cols-1 md:grid-cols-2 xl:grid-cols-4',
};

export function ContentGrid({ children, className, columns = 3 }: ContentGridProps) {
  return (
    <div className={cn('grid gap-4 lg:gap-5', columnClasses[columns], className)}>
      {children}
    </div>
  );
}
