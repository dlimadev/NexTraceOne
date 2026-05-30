import { Link } from 'react-router-dom';
import { MoreVertical, ArrowRight } from 'lucide-react';
import { cn } from '../lib/cn';
import { TrendBadge } from './TrendBadge';
import type { MiniSparklineProps } from './MiniSparkline';
import { MiniSparkline } from './MiniSparkline';

interface StatCardProps {
  title: string;
  value: string | number;
  icon?: React.ReactNode;
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

/** Mapeamento de classe de cor → variável CSS para border-top e fundo tonal do ícone. */
const colorTokenMap: Record<string, { borderVar: string; iconBg: string }> = {
  'text-cyan':     { borderVar: 'var(--t-cyan)',     iconBg: 'rgba(18,196,232,.08)' },
  'text-accent':   { borderVar: 'var(--t-accent)',   iconBg: 'rgba(27,127,232,.08)' },
  'text-blue':     { borderVar: 'var(--t-blue)',     iconBg: 'rgba(27,127,232,.08)' },
  'text-success':  { borderVar: 'var(--t-success)',  iconBg: 'rgba(5,150,105,.08)' },
  'text-mint':     { borderVar: 'var(--t-mint)',     iconBg: 'rgba(24,232,184,.08)' },
  'text-warning':  { borderVar: 'var(--t-warning)',  iconBg: 'rgba(217,119,6,.08)' },
  'text-critical': { borderVar: 'var(--t-critical)', iconBg: 'rgba(220,38,38,.08)' },
  'text-danger':   { borderVar: 'var(--t-danger)',   iconBg: 'rgba(220,38,38,.08)' },
  'text-info':     { borderVar: 'var(--t-info)',     iconBg: 'rgba(8,145,178,.08)' },
};

/**
 * KPI Card — DESIGN-SYSTEM.md §4.7
 * Título + métrica principal + tendência opcional.
 *
 * border-top: 3px sólido na cor semântica do card.
 * Ícone: 34×34 com fundo tonal (8% opacidade) e radius 9px.
 * Valor: cor semântica em vez de text-heading genérico.
 * Sparkline: 7 barras com gradiente de opacidade crescente.
 * Hover: borda forte + sombra elevada + micro translate.
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
  const token = colorTokenMap[color] ?? colorTokenMap['text-accent'];

  const content = (
    <div
      className={cn(
        'bg-card rounded-md border border-edge shadow-surface',
        'flex flex-col overflow-hidden',
        'transition-all duration-[var(--nto-motion-base)]',
        'hover:border-edge-strong hover:shadow-elevated hover:-translate-y-0.5',
        href && 'cursor-pointer',
        'group',
      )}
      style={{ borderTop: `3px solid ${token.borderVar}` }}
      aria-label={ariaLabel ?? `${title}: ${value}`}
      role="group"
    >
      {/* Corpo principal */}
      <div className="p-5 flex items-start gap-4 flex-1">
        {/* Ícone 34×34 com fundo tonal da cor semântica */}
        <div
          className={cn(
            'shrink-0 flex items-center justify-center',
            'transition-all duration-[var(--nto-motion-base)]',
            color,
          )}
          style={{ width: 34, height: 34, borderRadius: 9, background: token.iconBg, flexShrink: 0 }}
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
              {/* Valor na cor semântica do card */}
              <p className={cn('text-2xl font-bold tabular-nums leading-none', color)}>{value}</p>
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

      {/* Rodapé com contexto de comparação */}
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
