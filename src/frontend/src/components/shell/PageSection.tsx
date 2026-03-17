import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';

interface PageSectionProps {
  children: ReactNode;
  className?: string;
  /** Optional section title. */
  title?: string;
  /** Optional icon alongside title. */
  icon?: ReactNode;
  /** Optional actions in the header row. */
  actions?: ReactNode;
}

export function PageSection({ children, className, title, icon, actions }: PageSectionProps) {
  return (
    <section className={cn('mb-8', className)}>
      {title && (
        <div className="flex items-center justify-between gap-4 mb-4">
          <div className="flex items-center gap-2 min-w-0">
            {icon && <span className="text-muted shrink-0">{icon}</span>}
            <h2 className="text-sm font-semibold text-heading uppercase tracking-wider truncate">{title}</h2>
          </div>
          {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
        </div>
      )}
      {children}
    </section>
  );
}
