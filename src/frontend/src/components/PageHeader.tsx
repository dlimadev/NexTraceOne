import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

interface PageHeaderProps {
  /** Título principal da página. */
  title: string;
  /** Subtítulo/descrição curta. */
  subtitle?: string;
  /** Badge ou indicador ao lado do título. */
  badge?: ReactNode;
  /** Ações globais da página (botões). */
  actions?: ReactNode;
  /** Conteúdo abaixo do título (breadcrumbs, filtros). */
  children?: ReactNode;
  className?: string;
}

/**
 * Cabeçalho de página padronizado para todo o produto.
 *
 * Garante hierarquia visual consistente: título + descrição + ações.
 * Responsivo: ações reflui para baixo do título em viewports pequenos.
 * Segue a estrutura definida no DESIGN.md §4.3.
 *
 * @see docs/DESIGN.md — Anatomia padrão de página
 */
export function PageHeader({
  title,
  subtitle,
  badge,
  actions,
  children,
  className,
}: PageHeaderProps) {
  return (
    <div className={cn('mb-6', className)}>
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3 sm:gap-4">
        <div className="min-w-0">
          <div className="flex items-center gap-3">
            <h1 className="text-xl sm:text-2xl font-bold text-heading truncate">{title}</h1>
            {badge}
          </div>
          {subtitle && (
            <p className="text-sm text-muted mt-1">{subtitle}</p>
          )}
        </div>
        {actions && (
          <div className="flex items-center gap-2 shrink-0">{actions}</div>
        )}
      </div>
      {children}
    </div>
  );
}
