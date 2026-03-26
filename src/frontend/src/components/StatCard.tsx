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
 * Padding 24px, radius-lg, número em peso 600-700.
 */
export function StatCard({ title, value, icon, color = 'text-accent', trend, href, ariaLabel, context }: StatCardProps) {
  const content = (
    <div
      className={cn(
        'bg-card rounded-lg border border-edge shadow-surface p-5 flex items-start gap-4 transition-all duration-[var(--nto-motion-base)] hover:border-edge-strong hover:shadow-elevated group',
        href && 'cursor-pointer',
      )}
      aria-label={ariaLabel ?? `${title}: ${value}`}
      role="group"
    >
      <div className={cn('shrink-0 mt-0.5 opacity-80 group-hover:opacity-100 transition-opacity', color)} aria-hidden="true">
        {icon}
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-xs text-muted truncate mb-1">{title}</p>
        <p className="text-2xl font-bold text-heading tabular-nums">{value}</p>
        {context && (
          <p className="text-[11px] mt-1 text-muted">{context}</p>
        )}
        {trend && (
          <p className={cn(
            'text-[11px] mt-1.5 font-semibold inline-flex items-center gap-0.5',
            trend.direction === 'up' ? 'text-success' : 'text-critical',
          )}>
            {trend.direction === 'up' ? <TrendingUp size={12} aria-hidden="true" /> : <TrendingDown size={12} aria-hidden="true" />} {trend.label}
          </p>
        )}
      </div>
    </div>
  );

  if (href) {
    return <Link to={href}>{content}</Link>;
  }

  return content;
}
