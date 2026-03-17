import { type ReactNode } from 'react';
import { cn } from '../lib/cn';

interface TooltipProps {
  /** Conteúdo do tooltip. */
  content: string;
  /** Posição do tooltip. */
  position?: 'top' | 'bottom' | 'left' | 'right';
  /** Elemento trigger. */
  children: ReactNode;
  className?: string;
}

/**
 * Tooltip simples baseado em CSS (título nativo aprimorado).
 *
 * Para tooltips complexos com conteúdo rico, considerar um componente com portal.
 * Este componente cobre o caso de uso padrão enterprise: hint textual discreto.
 *
 * @see docs/DESIGN-SYSTEM.md §4.8
 */
export function Tooltip({ content, position = 'top', children, className }: TooltipProps) {
  return (
    <div className={cn('group relative inline-flex', className)}>
      {children}
      <div
        role="tooltip"
        className={cn(
          'pointer-events-none absolute z-[var(--z-dropdown)] whitespace-nowrap',
          'rounded-sm bg-elevated px-3 py-1.5 text-xs text-heading shadow-floating border border-edge',
          'opacity-0 group-hover:opacity-100 transition-opacity',
          position === 'top' && 'bottom-full left-1/2 -translate-x-1/2 mb-2',
          position === 'bottom' && 'top-full left-1/2 -translate-x-1/2 mt-2',
          position === 'left' && 'right-full top-1/2 -translate-y-1/2 mr-2',
          position === 'right' && 'left-full top-1/2 -translate-y-1/2 ml-2',
        )}
        style={{ transitionDuration: 'var(--nto-motion-fast)' }}
      >
        {content}
      </div>
    </div>
  );
}
