import type { ReactNode } from 'react';
import { Inbox, AlertCircle, Rocket, ShieldX } from 'lucide-react';
import { cn } from '../lib/cn';

type EmptyStateVariant = 'default' | 'error' | 'onboarding' | 'permission-denied';

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
  size?: 'default' | 'compact';
  /** Semantic variant — controls default icon and accent color. */
  variant?: EmptyStateVariant;
}

const variantConfig: Record<EmptyStateVariant, {
  icon: ReactNode;
  /** Inline style para ícone — borda dashed accent. */
  iconStyle: React.CSSProperties;
  iconColor: string;
}> = {
  default: {
    icon: <Inbox size={24} aria-hidden="true" />,
    iconStyle: {
      border: '1.5px dashed rgba(27,127,232,.25)',
      background: 'rgba(27,127,232,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-accent',
  },
  error: {
    icon: <AlertCircle size={24} aria-hidden="true" />,
    iconStyle: {
      border: '1.5px dashed rgba(220,38,38,.25)',
      background: 'rgba(220,38,38,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-critical',
  },
  onboarding: {
    icon: <Rocket size={24} aria-hidden="true" />,
    iconStyle: {
      border: '1.5px dashed rgba(8,145,178,.25)',
      background: 'rgba(8,145,178,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-info',
  },
  'permission-denied': {
    icon: <ShieldX size={24} aria-hidden="true" />,
    iconStyle: {
      border: '1.5px dashed rgba(217,119,6,.25)',
      background: 'rgba(217,119,6,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-warning',
  },
};

/**
 * Estado vazio — DESIGN-SYSTEM.md §4.13
 * Título + explicação + ação recomendada. Nunca genérico, sempre contextual.
 *
 * Ícone com borda dashed accent (1.5px) + fundo tonal (6% opacity).
 * CTA inline fornecido pelo consumidor via `action` prop.
 *
 * Variantes:
 * - default: ícone neutro para listas/tabelas vazias
 * - error: falha de carregamento com sugestão de retry
 * - onboarding: primeiro uso, convite à ação
 * - permission-denied: sem permissão para ver o recurso
 */
export function EmptyState({ icon, title, description, action, size = 'default', variant = 'default' }: EmptyStateProps) {
  const config = variantConfig[variant];

  return (
    <div className={cn(
      'flex flex-col items-center justify-center text-center animate-fade-in',
      size === 'compact' ? 'py-8 px-4' : 'py-16 px-6',
    )}>
      <div
        data-testid="empty-state-icon"
        data-variant={variant}
        className={cn(
          'flex items-center justify-center mb-4',
          config.iconColor,
          size === 'compact' ? 'w-10 h-10' : 'w-14 h-14',
        )}
        style={config.iconStyle}
      >
        {icon ?? (
          size === 'compact'
            ? <span className="[&>svg]:w-[18px] [&>svg]:h-[18px]">{config.icon}</span>
            : config.icon
        )}
      </div>
      <h3 className="text-sm font-semibold text-heading mb-1">{title}</h3>
      {description && (
        <p className="text-xs text-muted max-w-xs mb-4">{description}</p>
      )}
      {action}
    </div>
  );
}
