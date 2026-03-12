import type { ReactNode } from 'react';

/**
 * Status semânticos suportados — cada um recebe cor e ícone indicativo.
 * A cor nunca é o único indicador: o texto e o dot reforçam a semântica.
 */
export type StatusKind =
  | 'healthy'
  | 'degraded'
  | 'warning'
  | 'critical'
  | 'unknown'
  | 'in-progress'
  | 'recently-changed';

interface StatusPillProps {
  status: StatusKind;
  children: ReactNode;
  /** Exibe dot animado de pulsação para estados ativos (in-progress). */
  pulse?: boolean;
}

const statusStyles: Record<StatusKind, { bg: string; text: string; dot: string }> = {
  healthy:          { bg: 'bg-success/15', text: 'text-success',  dot: 'bg-success' },
  degraded:         { bg: 'bg-warning/15', text: 'text-warning',  dot: 'bg-warning' },
  warning:          { bg: 'bg-warning/15', text: 'text-warning',  dot: 'bg-warning' },
  critical:         { bg: 'bg-critical/15', text: 'text-critical', dot: 'bg-critical' },
  unknown:          { bg: 'bg-elevated',   text: 'text-muted',    dot: 'bg-muted' },
  'in-progress':    { bg: 'bg-info/15',    text: 'text-info',     dot: 'bg-info' },
  'recently-changed': { bg: 'bg-brand-purple/15', text: 'text-brand-purple', dot: 'bg-brand-purple' },
};

/**
 * Indicador de estado operacional com dot colorido + label textual.
 * Acessível: cor nunca é o único indicador — o texto descreve o estado.
 * Uso: health badges, deployment status, change indicators.
 */
export function StatusPill({ status, children, pulse }: StatusPillProps) {
  const style = statusStyles[status];

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${style.bg} ${style.text}`}
      role="status"
    >
      <span className="relative flex h-2 w-2 shrink-0">
        {pulse && (
          <span
            className={`absolute inline-flex h-full w-full animate-ping rounded-full opacity-75 ${style.dot}`}
          />
        )}
        <span className={`relative inline-flex h-2 w-2 rounded-full ${style.dot}`} />
      </span>
      {children}
    </span>
  );
}
