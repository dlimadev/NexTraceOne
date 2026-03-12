import type { ReactNode } from 'react';
import { Inbox } from 'lucide-react';

interface EmptyStateProps {
  /** Ícone opcional — padrão é uma caixa vazia. */
  icon?: ReactNode;
  title: string;
  description?: string;
  /** Ação principal (botão CTA) exibida abaixo da descrição. */
  action?: ReactNode;
}

/**
 * Estado vazio reutilizável para listas, tabelas e seções sem dados.
 * Centralizado visual e textualmente — mantém a UI informativa mesmo sem conteúdo.
 */
export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 px-6 text-center animate-fade-in">
      <div className="flex items-center justify-center w-14 h-14 rounded-full bg-elevated text-muted mb-4">
        {icon ?? <Inbox size={24} />}
      </div>
      <h3 className="text-base font-semibold text-heading mb-1">{title}</h3>
      {description && (
        <p className="text-sm text-muted max-w-sm mb-4">{description}</p>
      )}
      {action}
    </div>
  );
}
