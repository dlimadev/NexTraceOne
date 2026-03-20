import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface FilterBarProps {
  children: ReactNode;
  className?: string;
}

/**
 * Barra de filtros padronizada — DESIGN-SYSTEM §4.9
 *
 * Container horizontal para filtros, pesquisa e ações de filtragem.
 * Reflui para layout vertical em viewports menores.
 * Padding e gap consistentes em todo o produto.
 */
export function FilterBar({ children, className }: FilterBarProps) {
  return (
    <div
      className={cn(
        'flex flex-wrap items-center gap-3 mb-5',
        className,
      )}
    >
      {children}
    </div>
  );
}
