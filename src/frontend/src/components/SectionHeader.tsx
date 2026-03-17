import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

interface SectionHeaderProps {
  /** Título da seção. */
  title: string;
  /** Ícone opcional ao lado do título. */
  icon?: ReactNode;
  /** Ações da seção (links, botões). */
  actions?: ReactNode;
  className?: string;
}

/**
 * Cabeçalho de seção dentro de uma página.
 *
 * Usado para agrupar cards, tabelas e blocos analíticos.
 * Título menor e mais sutil que o PageHeader.
 */
export function SectionHeader({
  title,
  icon,
  actions,
  className,
}: SectionHeaderProps) {
  return (
    <div className={cn('flex items-center justify-between gap-4 mb-4', className)}>
      <div className="flex items-center gap-2 min-w-0">
        {icon && <span className="text-muted shrink-0">{icon}</span>}
        <h2 className="text-sm font-semibold text-heading uppercase tracking-wider truncate">{title}</h2>
      </div>
      {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
    </div>
  );
}
