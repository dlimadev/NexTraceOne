import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { cn } from '../lib/cn';

type IconButtonVariant = 'ghost' | 'subtle' | 'outline';
type IconButtonSize = 'sm' | 'md' | 'lg';

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Ícone a renderizar (Lucide ou SVG). */
  icon: ReactNode;
  /** Label acessível obrigatório (aria-label). */
  label: string;
  /** Variante visual. */
  variant?: IconButtonVariant;
  /** Tamanho (hit area mínima 40px conforme DESIGN-SYSTEM.md §7). */
  size?: IconButtonSize;
}

const variantClasses: Record<IconButtonVariant, string> = {
  ghost:
    'text-muted hover:text-heading hover:bg-hover',
  subtle:
    'text-body bg-elevated hover:bg-hover border border-transparent hover:border-edge',
  outline:
    'text-body border border-edge hover:bg-hover hover:border-edge-strong',
};

const sizeClasses: Record<IconButtonSize, string> = {
  sm: 'h-8 w-8',
  md: 'h-10 w-10',
  lg: 'h-12 w-12',
};

/**
 * Botão de ícone — DESIGN-SYSTEM.md §4.3 (Ghost / Toolbar button)
 *
 * Hit area mínima de 40px (md). Requer label acessível.
 * Usado em toolbars, grids, filtros e ações inline.
 */
export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
  function IconButton({ icon, label, variant = 'ghost', size = 'md', className, ...rest }, ref) {
    return (
      <button
        ref={ref}
        type="button"
        aria-label={label}
        className={cn(
          'inline-flex items-center justify-center rounded-md transition-colors',
          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-canvas',
          'disabled:opacity-50 disabled:cursor-not-allowed',
          variantClasses[variant],
          sizeClasses[size],
          className,
        )}
        style={{ transitionDuration: 'var(--nto-motion-fast)' }}
        {...rest}
      >
        {icon}
      </button>
    );
  },
);
