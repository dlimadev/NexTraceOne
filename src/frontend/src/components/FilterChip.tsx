import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

interface FilterChipProps {
  /** Label exibida no chip. */
  label: string;
  /** Se o chip está selecionado/ativo. */
  active?: boolean;
  /** Callback ao clicar. */
  onClick?: () => void;
  /** Ícone opcional antes da label. */
  icon?: ReactNode;
  /** Contador opcional exibido ao lado da label. */
  count?: number;
  disabled?: boolean;
  className?: string;
}

/**
 * Chip de filtro para barras de filtros rápidos.
 *
 * Ativo: bg-accent/10, border-accent/30, text-cyan.
 * Inativo: bg-panel, border-edge, text-muted.
 *
 * Usado em filtros de status (incidents, changes, contracts).
 */
export function FilterChip({
  label,
  active = false,
  onClick,
  icon,
  count,
  disabled = false,
  className,
}: FilterChipProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className={cn(
        'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-sm border text-xs font-medium',
        'transition-colors',
        active
          ? 'bg-accent-muted text-cyan border-edge-focus'
          : 'bg-panel text-muted border-edge hover:text-body hover:border-edge-strong',
        disabled && 'opacity-50 cursor-not-allowed',
        className,
      )}
      style={{ transitionDuration: 'var(--nto-motion-fast)' }}
    >
      {icon}
      {label}
      {count !== undefined && (
        <span
          className={cn(
            'inline-flex items-center justify-center min-w-[18px] h-[18px] rounded-pill px-1 type-micro font-semibold',
            active ? 'bg-cyan/20 text-cyan' : 'bg-elevated text-faded',
          )}
        >
          {count}
        </span>
      )}
    </button>
  );
}
