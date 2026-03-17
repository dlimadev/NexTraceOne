import { cn } from '../lib/cn';

type LoaderSize = 'sm' | 'md' | 'lg';

interface LoaderProps {
  /** Tamanho do spinner. */
  size?: LoaderSize;
  className?: string;
}

const sizeClasses: Record<LoaderSize, string> = {
  sm: 'h-4 w-4',
  md: 'h-6 w-6',
  lg: 'h-8 w-8',
};

/**
 * Spinner de loading — DESIGN-SYSTEM.md §4.14
 *
 * Usar para estados de carregamento inline e botões.
 * Para loading de página densa, preferir Skeleton.
 */
export function Loader({ size = 'md', className }: LoaderProps) {
  return (
    <svg
      className={cn('animate-spin text-accent', sizeClasses[size], className)}
      viewBox="0 0 24 24"
      fill="none"
      aria-hidden="true"
    >
      <circle
        className="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        strokeWidth="3"
      />
      <path
        className="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
      />
    </svg>
  );
}
