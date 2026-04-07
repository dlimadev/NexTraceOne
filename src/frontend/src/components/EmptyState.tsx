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

const variantConfig: Record<EmptyStateVariant, { icon: ReactNode; iconBg: string }> = {
  default: {
    icon: <Inbox size={24} aria-hidden="true" />,
    iconBg: 'bg-elevated border border-edge text-muted',
  },
  error: {
    icon: <AlertCircle size={24} aria-hidden="true" />,
    iconBg: 'bg-danger/10 border border-danger/20 text-danger',
  },
  onboarding: {
    icon: <Rocket size={24} aria-hidden="true" />,
    iconBg: 'bg-info/10 border border-info/20 text-info',
  },
  'permission-denied': {
    icon: <ShieldX size={24} aria-hidden="true" />,
    iconBg: 'bg-warning/10 border border-warning/20 text-warning',
  },
};

/**
 * Estado vazio — DESIGN-SYSTEM.md §4.13
 * Título + explicação + ação recomendada. Nunca genérico, sempre contextual.
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
      <div className={cn(
        'flex items-center justify-center rounded-lg mb-4',
        size === 'compact' ? 'w-10 h-10' : 'w-14 h-14',
        config.iconBg,
      )}>
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
