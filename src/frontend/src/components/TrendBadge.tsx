import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { cn } from '../lib/cn';

export interface TrendBadgeProps {
  /** Direção da tendência. */
  direction: 'up' | 'down' | 'neutral';
  /** Texto da tendência (ex: "+2.57%", "-0.8%"). */
  value: string;
  /** Tamanho do badge. */
  size?: 'sm' | 'md';
  className?: string;
}

const sizeClasses = {
  sm: 'text-[10px] px-1.5 py-0.5 gap-0.5',
  md: 'text-xs px-2.5 py-1 gap-1',
};

const directionClasses = {
  up: 'bg-success-muted text-success',
  down: 'bg-critical-muted text-critical',
  neutral: 'bg-elevated text-muted',
};

const icons = {
  up: TrendingUp,
  down: TrendingDown,
  neutral: Minus,
};

/**
 * TrendBadge — badge pill com fundo semântico sutil para indicadores de tendência.
 * Inspirado pelo padrão `bg-success-subtle text-success` do template NexLink.
 *
 * Usa os tokens semânticos `-muted` (ex: `bg-success-muted`) para fundo colorido
 * e `bg-elevated` para o neutro + texto na cor semântica completa.
 * Inclui ícone direcional (TrendingUp, TrendingDown, Minus).
 */
export function TrendBadge({ direction, value, size = 'md', className }: TrendBadgeProps) {
  const Icon = icons[direction];

  return (
    <span
      className={cn(
        'inline-flex items-center font-semibold rounded-full whitespace-nowrap',
        sizeClasses[size],
        directionClasses[direction],
        className,
      )}
    >
      <Icon size={size === 'sm' ? 10 : 12} aria-hidden="true" />
      {value}
    </span>
  );
}
