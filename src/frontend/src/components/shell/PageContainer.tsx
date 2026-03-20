import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface PageContainerProps {
  children: ReactNode;
  className?: string;
  /** Full-width layout (no max-width constraint). */
  fluid?: boolean;
  /** Compact vertical padding for dense layouts. */
  compact?: boolean;
}

/**
 * Container de página padronizado — DESIGN-SYSTEM §4.1
 *
 * Garante padding, max-width e responsividade consistentes em todas as páginas.
 * Todos os módulos devem usar PageContainer como wrapper raiz de conteúdo.
 *
 * Breakpoints:
 * - Mobile: px-4 py-4
 * - Tablet: px-5 py-5
 * - Desktop: px-6 py-6
 * - Wide: px-8 py-6, max-w-[1600px]
 */
export function PageContainer({ children, className, fluid = false, compact = false }: PageContainerProps) {
  return (
    <div
      className={cn(
        'px-4 sm:px-5 lg:px-6 xl:px-8 animate-fade-in w-full',
        compact ? 'py-4' : 'py-5 lg:py-6',
        !fluid && 'max-w-[1600px]',
        className,
      )}
    >
      {children}
    </div>
  );
}
