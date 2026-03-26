import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { cn } from '../lib/cn';
import { Badge } from './Badge';

export interface EntityBadge {
  label: string;
  variant?: 'default' | 'neutral' | 'success' | 'warning' | 'danger' | 'info';
}

interface EntityHeaderProps {
  /** Display name of the entity. */
  name: string;
  /** Type label (e.g. "Service", "Incident", "Contract"). */
  entityType?: string;
  /** Main status badge. */
  status?: EntityBadge;
  /** Criticality badge. */
  criticality?: EntityBadge;
  /** Owner name / team. */
  owner?: string;
  /** Extra badges rendered after status/criticality. */
  badges?: EntityBadge[];
  /** Action buttons rendered in the top-right. */
  actions?: ReactNode;
  /** Icon rendered to the left of the entity name. */
  icon?: ReactNode;
  /** Short description rendered below the title. */
  description?: string;
  /** Secondary meta items (e.g. type · team · env). */
  meta?: string[];
  /** If provided, renders a back-link with ArrowLeft. */
  backLink?: { to: string; label: string };
  className?: string;
}

/**
 * EntityHeader — header reutilizável para páginas de detalhe de entidade.
 * Renderiza nome, badges de status/criticidade/extras, metadados e ações.
 *
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F3-01
 */
export function EntityHeader({
  name,
  entityType,
  status,
  criticality,
  owner,
  badges = [],
  actions,
  icon,
  description,
  meta = [],
  backLink,
  className,
}: EntityHeaderProps) {
  return (
    <div className={cn('mb-6', className)}>
      {backLink && (
        <Link
          to={backLink.to}
          className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4"
          aria-label={backLink.label}
        >
          <ArrowLeft size={14} aria-hidden="true" />
          {backLink.label}
        </Link>
      )}

      <div className="flex flex-wrap items-start justify-between gap-4">
        {/* Left: icon + title block */}
        <div className="flex items-start gap-3 min-w-0">
          {icon && (
            <div className="shrink-0 mt-0.5 text-accent" aria-hidden="true">
              {icon}
            </div>
          )}
          <div className="min-w-0">
            {/* Badge row */}
            <div className="flex flex-wrap items-center gap-2 mb-1.5">
              {entityType && (
                <Badge variant="info" size="sm">
                  {entityType}
                </Badge>
              )}
              {status && (
                <Badge variant={status.variant ?? 'default'} size="sm">
                  {status.label}
                </Badge>
              )}
              {criticality && (
                <Badge variant={criticality.variant ?? 'default'} size="sm">
                  {criticality.label}
                </Badge>
              )}
              {badges.map((b, idx) => (
                <Badge key={idx} variant={b.variant ?? 'default'} size="sm">
                  {b.label}
                </Badge>
              ))}
            </div>

            {/* Title */}
            <h1 className="text-2xl font-bold text-heading leading-snug truncate">
              {name}
            </h1>

            {/* Description */}
            {description && (
              <p className="text-sm text-muted mt-1 max-w-2xl">{description}</p>
            )}

            {/* Meta row */}
            {(meta.length > 0 || owner) && (
              <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 mt-2 text-xs text-muted">
                {owner && <span key="owner">{owner}</span>}
                {owner && meta.length > 0 && <span key="sep-owner" aria-hidden="true">·</span>}
                {meta.map((item, idx) => (
                  <span key={idx}>
                    {item}
                    {idx < meta.length - 1 && (
                      <span aria-hidden="true"> ·</span>
                    )}
                  </span>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Right: actions */}
        {actions && (
          <div className="flex items-center gap-2 shrink-0">
            {actions}
          </div>
        )}
      </div>
    </div>
  );
}
