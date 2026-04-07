import { Link } from 'react-router-dom';
import { MoreVertical, ArrowRight } from 'lucide-react';
import { cn } from '../lib/cn';
import { TrendBadge } from './TrendBadge';
import type { MiniSparklineProps } from './MiniSparkline';
import { MiniSparkline } from './MiniSparkline';

interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color?: string;
  trend?: { direction: 'up' | 'down'; label: string };
  href?: string;
  ariaLabel?: string;
  context?: string;
  /** Dados opcionais para sparkline inline (inspirado pelo template NexLink). */
  sparkline?: Pick<MiniSparklineProps, 'data' | 'color'>;
  /** Texto de rodapé com contexto de comparação (ex: "Vs last month: 1,195"). */
  footer?: string;
  /** Link de rodapé (ex: "Ver tudo"). */
  footerHref?: string;
  /** Itens do menu de overflow (3-dot). */
  actions?: Array<{ label: string; onClick: () => void }>;
}

/**
 * KPI Card — DESIGN-SYSTEM.md §4.7
 * Título + métrica principal + tendência opcional.
 *
 * Ícone com área tonal e borda suave.
 * Valor em texto grande, peso 700, tabular-nums.
 * Hover: borda forte + sombra elevada + micro translate para depth.
 *
 * Template-inspired enhancements:
 * - Optional inline sparkline chart
 * - Optional footer with comparison context
 * - Optional overflow actions menu (3-dot)
 * - TrendBadge with semantic subtle background
 */
export function StatCard({
  title,
  value,
  icon,
  color = 'text-accent',
  trend,
  href,
  ariaLabel,
  context,
  sparkline,
  footer,
  footerHref,
  actions,
}: StatCardProps) {
  const content = (
    <div
      className={cn(
        'bg-card rounded-2xl border border-edge shadow-surface',
        'flex flex-col',
        'transition-all duration-[var(--nto-motion-base)]',
        'hover:border-edge-strong hover:shadow-elevated hover:-translate-y-0.5',
        href && 'cursor-pointer',
        'group',
      )}
      aria-label={ariaLabel ?? `${title}: ${value}`}
      role="group"
    >
      {/* Corpo principal */}
      <div className="p-5 flex items-start gap-4 flex-1">
        {/* Ícone com fundo tonal da cor semântica */}
        <div
          className={cn(
            'shrink-0 w-11 h-11 rounded-xl flex items-center justify-center',
            'bg-elevated border border-edge',
            'transition-all duration-[var(--nto-motion-base)] group-hover:border-edge-strong',
            color,
          )}
          aria-hidden="true"
        >
          {icon}
        </div>

        <div className="min-w-0 flex-1">
          {/* Header row: title + actions */}
          <div className="flex items-start justify-between gap-2">
            <p className="text-xs font-medium text-muted truncate mb-1">{title}</p>
            {actions && actions.length > 0 && (
              <div className="relative group/menu shrink-0">
                <button
                  className="p-1 -mt-1 -mr-1 rounded-md text-muted hover:text-heading hover:bg-hover transition-colors opacity-0 group-hover:opacity-100"
                  aria-label="More options"
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                  }}
                >
                  <MoreVertical size={14} />
                </button>
              </div>
            )}
          </div>

          {/* Value + sparkline row */}
          <div className="flex items-end justify-between gap-2">
            <div>
              <p className="text-2xl font-bold text-heading tabular-nums leading-none">{value}</p>
              {trend && (
                <TrendBadge
                  direction={trend.direction}
                  value={trend.label}
                  size="sm"
                  className="mt-2"
                />
              )}
              {context && (
                <p className="text-[11px] mt-1.5 text-muted">{context}</p>
              )}
            </div>
            {sparkline && (
              <MiniSparkline
                data={sparkline.data}
                color={sparkline.color}
                width={80}
                height={32}
                className="opacity-70 group-hover:opacity-100 transition-opacity"
              />
            )}
          </div>
        </div>
      </div>

      {/* Rodapé com contexto de comparação (inspirado pelo template) */}
      {footer && (
        <div className="px-5 py-2.5 border-t border-edge/40 flex items-center justify-between">
          <p className="text-[11px] text-muted">{footer}</p>
          {footerHref && (
            <Link
              to={footerHref}
              className="text-accent hover:text-accent-hover transition-colors"
              onClick={(e) => e.stopPropagation()}
            >
              <ArrowRight size={14} />
            </Link>
          )}
        </div>
      )}
    </div>
  );

  if (href) {
    return <Link to={href}>{content}</Link>;
  }

  return content;
}
