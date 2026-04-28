import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

export interface BreadcrumbItem {
  label: string;
  to?: string;
}

export interface PageHeaderProps {
  /** Título principal da página. */
  title: string;
  /** Subtítulo/descrição curta. */
  subtitle?: string;
  /** Descrição alternativa (alias de subtitle). */
  description?: string;
  /** Ícone ao lado do título. */
  icon?: ReactNode;
  /** Badge ou indicador ao lado do título. */
  badge?: ReactNode;
  /** Ações globais da página (botões). */
  actions?: ReactNode;
  /** Breadcrumbs de navegação. */
  breadcrumb?: BreadcrumbItem[];
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
  description,
  icon,
  badge,
  actions,
  breadcrumb,
  children,
  className,
}: PageHeaderProps) {
  const subtitleText = subtitle ?? description;
  return (
    <div className={cn('mb-6', className)}>
      {breadcrumb && breadcrumb.length > 0 && (
        <nav className="flex items-center gap-1.5 text-xs text-muted mb-2">
          {breadcrumb.map((crumb, i) => (
            <span key={i} className="flex items-center gap-1.5">
              {i > 0 && <span className="text-faded">/</span>}
              {crumb.to ? (
                <a href={crumb.to} className="hover:text-body transition-colors">{crumb.label}</a>
              ) : (
                <span>{crumb.label}</span>
              )}
            </span>
          ))}
        </nav>
      )}
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3 sm:gap-4">
        <div className="min-w-0">
          <div className="flex items-center gap-3">
            {icon && <span className="shrink-0 text-muted" aria-hidden="true">{icon}</span>}
            <h1 className="text-xl sm:text-2xl font-bold text-heading truncate">{title}</h1>
            {badge}
          </div>
          {subtitleText && (
            <p className="text-sm text-muted mt-1">{subtitleText}</p>
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
