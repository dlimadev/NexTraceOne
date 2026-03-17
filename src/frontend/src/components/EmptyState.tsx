import type { ReactNode } from 'react';
import { Inbox } from 'lucide-react';
import { cn } from '../lib/cn';

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
  size?: 'default' | 'compact';
}

/**
 * Estado vazio — DESIGN-SYSTEM.md §4.13
 * Título + explicação + ação recomendada. Nunca genérico, sempre contextual.
 */
export function EmptyState({ icon, title, description, action, size = 'default' }: EmptyStateProps) {
  return (
    <div className={cn(
      'flex flex-col items-center justify-center text-center animate-fade-in',
      size === 'compact' ? 'py-8 px-4' : 'py-16 px-6',
    )}>
      <div className={cn(
        'flex items-center justify-center rounded-lg bg-elevated border border-edge text-muted mb-4',
        size === 'compact' ? 'w-10 h-10' : 'w-14 h-14',
      )}>
        {icon ?? <Inbox size={size === 'compact' ? 18 : 24} />}
      </div>
      <h3 className="text-sm font-semibold text-heading mb-1">{title}</h3>
      {description && (
        <p className="text-xs text-muted max-w-xs mb-4">{description}</p>
      )}
      {action}
    </div>
  );
}
