import { cn } from '../lib/cn';

export interface StackedProgressSegment {
  /** Percentagem (0–100) que este segmento ocupa. */
  value: number;
  /** Cor de fundo do segmento (classe Tailwind, ex: "bg-success"). */
  color: string;
  /** Label acessível. */
  label: string;
}

export interface StackedProgressBarProps {
  /** Segmentos a renderizar. */
  segments: StackedProgressSegment[];
  /** Altura da barra. */
  height?: 'sm' | 'md' | 'lg';
  /** Mostrar legenda abaixo da barra. */
  showLegend?: boolean;
  className?: string;
}

const heightClasses = {
  sm: 'h-1.5',
  md: 'h-2.5',
  lg: 'h-4',
};

/**
 * StackedProgressBar — barra de progresso empilhada com segmentos coloridos.
 * Inspirado pelo padrão `.progress-stacked` do template NexLink.
 *
 * Suporta múltiplos segmentos com cores semânticas e legenda opcional.
 */
export function StackedProgressBar({
  segments,
  height = 'md',
  showLegend = false,
  className,
}: StackedProgressBarProps) {
  return (
    <div className={cn('space-y-2', className)}>
      <div
        className={cn(
          'flex w-full rounded-full overflow-hidden bg-elevated',
          heightClasses[height],
        )}
        role="progressbar"
        aria-label="Progress"
      >
        {segments.map((seg) => (
          <div
            key={seg.label}
            className={cn('transition-all duration-[var(--nto-motion-medium)]', seg.color)}
            style={{ width: `${Math.max(seg.value, 0)}%` }}
            title={`${seg.label}: ${seg.value}%`}
          />
        ))}
      </div>

      {showLegend && (
        <div className="flex flex-wrap gap-x-4 gap-y-1">
          {segments.map((seg) => (
            <div key={seg.label} className="flex items-center gap-1.5 text-xs text-muted">
              <span className={cn('w-2.5 h-2.5 rounded-sm shrink-0', seg.color)} aria-hidden="true" />
              <span>{seg.label}</span>
              <span className="font-semibold text-heading ml-auto tabular-nums">{seg.value}%</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
