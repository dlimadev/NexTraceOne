import type { ReactNode } from 'react';
import { cn } from '../lib/cn';

type Severity = 'info' | 'success' | 'warning' | 'danger' | 'critical';

interface InlineMessageProps {
  /** Severidade semântica. */
  severity: Severity;
  /** Ícone principal. */
  icon?: ReactNode;
  /** Título. */
  title?: string;
  /** Descrição. */
  children: ReactNode;
  /** Ação opcional (link, botão). */
  action?: ReactNode;
  className?: string;
}

const severityStyles: Record<Severity, string> = {
  info: 'bg-info-muted border border-info/25 border-l-2 border-l-info text-info',
  success: 'bg-success-muted border border-success/25 border-l-2 border-l-success text-success',
  warning: 'bg-warning-muted border border-warning/25 border-l-2 border-l-warning text-warning',
  danger: 'bg-critical-muted border border-danger/25 border-l-2 border-l-danger text-danger',
  critical: 'bg-critical-muted border border-critical/25 border-l-2 border-l-critical text-critical',
};

/**
 * Mensagem inline semântica para alertas, banners e feedbacks em contexto.
 *
 * Diferente de Toast (ephemeral), InlineMessage permanece visível na página.
 * Usado para alertas operacionais, erros de validação, avisos de compliance.
 *
 * @see docs/DESIGN-SYSTEM.md §4.12
 */
export function InlineMessage({
  severity,
  icon,
  title,
  children,
  action,
  className,
}: InlineMessageProps) {
  return (
    <div
      role={severity === 'danger' || severity === 'critical' ? 'alert' : 'status'}
      className={cn(
        'flex items-start gap-3 rounded-sm px-4 py-3 text-sm',
        severityStyles[severity],
        className,
      )}
    >
      {icon && <span className="shrink-0 mt-0.5">{icon}</span>}
      <div className="flex-1 min-w-0">
        {title && <p className="font-medium mb-0.5">{title}</p>}
        <div className="text-xs opacity-90">{children}</div>
      </div>
      {action && <div className="shrink-0">{action}</div>}
    </div>
  );
}
