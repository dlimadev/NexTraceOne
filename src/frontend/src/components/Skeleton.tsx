import { cn } from '../lib/cn';

interface SkeletonProps {
  className?: string;
  /** Variante de forma padrão. */
  variant?: 'text' | 'circular' | 'rectangular';
  /** Largura explícita (ex: 'w-32', '120px'). */
  width?: string;
  /** Altura explícita (ex: 'h-4', '20px'). */
  height?: string;
}

/**
 * Placeholder animado para loading states.
 *
 * Usa a animação shimmer definida nos tokens globais (index.css).
 * Deve substituir os inline `.skeleton` usados em páginas para garantir
 * consistência visual e semântica entre todos os loading states.
 *
 * @see docs/DESIGN-SYSTEM.md §4.14
 */
export function Skeleton({
  className,
  variant = 'text',
  width,
  height,
}: SkeletonProps) {
  const variantClass =
    variant === 'circular'
      ? 'rounded-full'
      : variant === 'rectangular'
        ? 'rounded-sm'
        : 'rounded-sm';

  return (
    <div
      aria-hidden
      className={cn(
        'skeleton',
        variantClass,
        variant === 'text' && 'h-3.5',
        width,
        height,
        className,
      )}
    />
  );
}
