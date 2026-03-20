import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface StatsGridProps {
  children: ReactNode;
  className?: string;
  /** Number of columns at largest breakpoint (default: 4). */
  columns?: 2 | 3 | 4 | 5;
}

const colClasses: Record<number, string> = {
  2: 'grid-cols-1 sm:grid-cols-2',
  3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
  4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4',
  5: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5',
};

/**
 * Grid padronizado para KPI / stat cards — DESIGN-SYSTEM §4.7
 *
 * Garante layout responsivo consistente para linhas de estatísticas.
 * Deve ser usado em todos os módulos que apresentam KPI summary cards.
 * Gap e margin-bottom padronizados em todo o produto.
 */
export function StatsGrid({ children, className, columns = 4 }: StatsGridProps) {
  return (
    <div className={cn('grid gap-4 mb-6', colClasses[columns], className)}>
      {children}
    </div>
  );
}
