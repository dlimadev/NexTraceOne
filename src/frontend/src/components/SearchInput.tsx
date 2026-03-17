import { forwardRef, type InputHTMLAttributes } from 'react';
import { Search } from 'lucide-react';
import { cn } from '../lib/cn';

interface SearchInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'size'> {
  /** Tamanho visual. */
  size?: 'sm' | 'md' | 'lg';
}

/**
 * Campo de busca padronizado com ícone de lupa integrado.
 *
 * Segue os tokens NTO: bg-input, border-edge, rounded-lg.
 * Usado em filtros de tabelas, command palette e barras de filtros.
 */
export const SearchInput = forwardRef<HTMLInputElement, SearchInputProps>(
  function SearchInput({ size = 'md', className, ...rest }, ref) {
    const sizeClasses = {
      sm: 'h-9 text-xs pl-8',
      md: 'h-11 text-sm pl-10',
      lg: 'h-14 text-base pl-12',
    };

    const iconSizes = {
      sm: 14,
      md: 16,
      lg: 18,
    };

    const iconPositions = {
      sm: 'left-2.5',
      md: 'left-3',
      lg: 'left-4',
    };

    return (
      <div className={cn('relative', className)}>
        <Search
          size={iconSizes[size]}
          className={cn('absolute top-1/2 -translate-y-1/2 text-muted pointer-events-none', iconPositions[size])}
        />
        <input
          ref={ref}
          type="search"
          className={cn(
            'w-full rounded-lg bg-input border border-edge pr-4 text-heading',
            'placeholder:text-muted',
            'transition-colors',
            'focus:outline-none focus:border-edge-focus focus:shadow-glow-cyan',
            sizeClasses[size],
          )}
          style={{ transitionDuration: 'var(--nto-motion-fast)' }}
          {...rest}
        />
      </div>
    );
  },
);
