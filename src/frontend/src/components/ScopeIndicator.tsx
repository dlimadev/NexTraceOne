import { useTranslation } from 'react-i18next';
import { Users, Globe, ChevronDown } from 'lucide-react';

interface ScopeIndicatorProps {
  teamName?: string;
  domainName?: string;
}

/**
 * Indicador de escopo organizacional.
 * Mostra a equipa e/ou domínio ativo para o utilizador.
 * Integrado no AppHeader para visibilidade contínua.
 */
export function ScopeIndicator({ teamName, domainName }: ScopeIndicatorProps) {
  const { t } = useTranslation();

  if (!teamName && !domainName) return null;

  return (
    <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-accent/5 border border-accent/20 text-sm">
      {teamName && (
        <span className="flex items-center gap-1.5 text-heading">
          <Users size={14} className="text-accent" />
          <span className="font-medium">{teamName}</span>
        </span>
      )}
      {teamName && domainName && (
        <span className="text-faded mx-1">·</span>
      )}
      {domainName && (
        <span className="flex items-center gap-1.5 text-muted">
          <Globe size={14} className="text-accent/70" />
          <span>{domainName}</span>
        </span>
      )}
      <ChevronDown size={12} className="text-faded ml-1" />
    </div>
  );
}
