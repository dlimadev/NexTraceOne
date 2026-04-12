import { memo } from 'react';
import { Link } from 'react-router-dom';
import { TrendingUp, TrendingDown } from 'lucide-react';
import { cn } from '../lib/cn';

interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color?: string;
  trend?: { direction: 'up' | 'down'; label: string };
  href?: string;
  ariaLabel?: string;
  context?: string;
}

/**
 * KPI Card — DESIGN-SYSTEM.md §4.7
 * Título + métrica principal + tendência opcional.
 *
 * Ícone com área tonal e borda suave.
 * Valor em texto grande, peso 700, tabular-nums.
 * Hover: borda forte + sombra elevada + micro translate para depth.
 */
export const StatCard = memo(function StatCard({ title, value, icon, color = 'text-accent', trend, href, ariaLabel, context }: StatCardProps) {
  const content = (
    <div
      className={cn(
        'bg-card rounded-2xl border border-edge shadow-surface p-5',
        'flex items-start gap-4',
        'transition-all duration-[var(--nto-motion-base)]',
        'hover:border-edge-strong hover:shadow-elevated hover:-translate-y-0.5',
        href && 'cursor-pointer',
        'group',
      )}
      aria-label={ariaLabel ?? `${title}: ${value}`}
      role="group"
    >
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
        <p className="text-xs font-medium text-muted truncate mb-1">{title}</p>
        <p className="text-2xl font-bold text-heading tabular-nums leading-none">{value}</p>
        {context && (
          <p className="text-[11px] mt-1.5 text-muted">{context}</p>
        )}
        {trend && (
          <p className={cn(
            'text-[11px] mt-1.5 font-semibold inline-flex items-center gap-0.5',
            trend.direction === 'up' ? 'text-success' : 'text-critical',
          )}>
            {trend.direction === 'up'
              ? <TrendingUp size={11} aria-hidden="true" />
              : <TrendingDown size={11} aria-hidden="true" />
            }
            {trend.label}
          </p>
        )}
      </div>
    </div>
  );

  if (href) {
    return <Link to={href}>{content}</Link>;
  }

  return content;
});
