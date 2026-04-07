import { useId, useMemo } from 'react';
import { cn } from '../lib/cn';

export interface MiniSparklineProps {
  /** Valores numéricos para o sparkline. */
  data: number[];
  /** Largura do SVG. */
  width?: number;
  /** Altura do SVG. */
  height?: number;
  /** Cor do traço — CSS variable ou classe Tailwind. */
  color?: string;
  /** Mostrar preenchimento abaixo da linha. */
  filled?: boolean;
  className?: string;
}

/**
 * MiniSparkline — gráfico inline minimalista para stat cards.
 * Inspirado pelos mini charts embutidos nos stat cards do template NexLink.
 *
 * Renderiza SVG puro sem dependências externas.
 * Suporta preenchimento gradiente sutil abaixo da linha.
 */
export function MiniSparkline({
  data,
  width = 80,
  height = 32,
  color = 'var(--t-accent)',
  filled = true,
  className,
}: MiniSparklineProps) {
  const gradientId = `sparkline-${useId().replace(/:/g, '')}`;

  const pathData = useMemo(() => {
    if (!data.length) return { line: '', area: '' };

    const min = Math.min(...data);
    const max = Math.max(...data);
    const range = max - min || 1;
    const padding = 2;
    const drawWidth = width - padding * 2;
    const drawHeight = height - padding * 2;

    const points = data.map((v, i) => ({
      x: padding + (i / Math.max(data.length - 1, 1)) * drawWidth,
      y: padding + drawHeight - ((v - min) / range) * drawHeight,
    }));

    const line = points
      .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`)
      .join(' ');

    const area = `${line} L${points[points.length - 1].x.toFixed(1)},${height} L${points[0].x.toFixed(1)},${height} Z`;

    return { line, area };
  }, [data, width, height]);

  if (!data.length) return null;

  return (
    <svg
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      className={cn('shrink-0', className)}
      aria-hidden="true"
    >
      {filled && (
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={color} stopOpacity={0.2} />
            <stop offset="100%" stopColor={color} stopOpacity={0.02} />
          </linearGradient>
        </defs>
      )}
      {filled && pathData.area && (
        <path d={pathData.area} fill={`url(#${gradientId})`} />
      )}
      {pathData.line && (
        <path
          d={pathData.line}
          fill="none"
          stroke={color}
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      )}
    </svg>
  );
}
