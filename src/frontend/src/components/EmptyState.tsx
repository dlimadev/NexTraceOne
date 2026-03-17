import type { ReactNode } from 'react';
import { Inbox } from 'lucide-react';

interface EmptyStateProps {
  /** Ícone opcional — padrão é uma caixa vazia. */
  icon?: ReactNode;
  title: string;
  description?: string;
  /** Ação principal (botão CTA) exibida abaixo da descrição. */
  action?: ReactNode;
  /** Size variant — compact uses less vertical space, used inside cards. */
  size?: 'default' | 'compact';
}

/**
 * Estado vazio reutilizável para listas, tabelas e seções sem dados.
 * Centralizado visual e textualmente — mantém a UI informativa mesmo sem conteúdo.
 *
 * Variantes:
 * - default: espaçamento amplo para secções principais.
 * - compact: espaçamento reduzido para uso dentro de cards.
 */
export function EmptyState({ icon, title, description, action, size = 'default' }: EmptyStateProps) {
  const padding = size === 'compact' ? 'py-8 px-4' : 'py-16 px-6';
  const iconSize = size === 'compact' ? 'w-10 h-10' : 'w-14 h-14';

  return (
    <div className={`flex flex-col items-center justify-center ${padding} text-center animate-fade-in`}>
      <div className={`flex items-center justify-center ${iconSize} rounded-full bg-elevated text-muted mb-3`}>
        {icon ?? <Inbox size={size === 'compact' ? 18 : 24} />}
      </div>
      <h3 className="text-sm font-semibold text-heading mb-1">{title}</h3>
      {description && (
        <p className="text-xs text-muted max-w-xs mb-3">{description}</p>
      )}
      {action}
    </div>
  );
}
