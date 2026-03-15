import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Inbox,
  AlertCircle,
  Loader2,
  Lock,
  Gauge,
  Settings,
  SearchX,
  CheckCircle2,
} from 'lucide-react';

type StateVariant =
  | 'empty'
  | 'noResults'
  | 'error'
  | 'loading'
  | 'blocked'
  | 'quotaExceeded'
  | 'notConfigured'
  | 'success';

interface StateDisplayProps {
  /** Variante do estado a exibir. */
  variant: StateVariant;
  /** Título override (i18n key ou string directa). */
  title?: string;
  /** Descrição override. */
  description?: string;
  /** Acção principal (botão, link). */
  action?: ReactNode;
  /** Ícone override. */
  icon?: ReactNode;
}

/**
 * Ícones e cores padrão por variante de estado.
 */
const variantDefaults: Record<
  StateVariant,
  { icon: ReactNode; titleKey: string; descKey: string; color: string }
> = {
  empty: {
    icon: <Inbox size={24} />,
    titleKey: 'productPolish.emptyStateDefault',
    descKey: 'productPolish.emptyStateAction',
    color: 'text-muted',
  },
  noResults: {
    icon: <SearchX size={24} />,
    titleKey: 'productPolish.noResultsTitle',
    descKey: 'productPolish.noResultsDesc',
    color: 'text-muted',
  },
  error: {
    icon: <AlertCircle size={24} />,
    titleKey: 'productPolish.errorTitle',
    descKey: 'productPolish.errorDesc',
    color: 'text-critical',
  },
  loading: {
    icon: <Loader2 size={24} className="animate-spin" />,
    titleKey: 'productPolish.loadingTitle',
    descKey: 'productPolish.loadingDesc',
    color: 'text-accent',
  },
  blocked: {
    icon: <Lock size={24} />,
    titleKey: 'productPolish.blockedTitle',
    descKey: 'productPolish.blockedDesc',
    color: 'text-warning',
  },
  quotaExceeded: {
    icon: <Gauge size={24} />,
    titleKey: 'productPolish.quotaExceededTitle',
    descKey: 'productPolish.quotaExceededDesc',
    color: 'text-warning',
  },
  notConfigured: {
    icon: <Settings size={24} />,
    titleKey: 'productPolish.notConfiguredTitle',
    descKey: 'productPolish.notConfiguredDesc',
    color: 'text-muted',
  },
  success: {
    icon: <CheckCircle2 size={24} />,
    titleKey: 'productPolish.successTitle',
    descKey: '',
    color: 'text-success',
  },
};

/**
 * Componente padronizado de exibição de estados.
 *
 * Substitui empty states genéricos e oferece variantes consistentes para:
 * empty, noResults, error, loading, blocked, quotaExceeded, notConfigured, success.
 *
 * Cada variante tem ícone, título, descrição e cor padrão — todos overridáveis.
 * Garantia de consistência visual e i18n em todos os módulos do produto.
 */
export function StateDisplay({ variant, title, description, action, icon }: StateDisplayProps) {
  const { t } = useTranslation();
  const defaults = variantDefaults[variant];

  const isLoading = variant === 'loading';

  return (
    <div className="flex flex-col items-center justify-center py-16 px-6 text-center animate-fade-in">
      <div
        className={`flex items-center justify-center w-14 h-14 rounded-full bg-elevated mb-4 ${defaults.color}`}
      >
        {icon ?? defaults.icon}
      </div>
      <h3 className="text-base font-semibold text-heading mb-1">
        {title ?? t(defaults.titleKey)}
      </h3>
      {(description || defaults.descKey) && !isLoading && (
        <p className="text-sm text-muted max-w-sm mb-4">
          {description ?? (defaults.descKey ? t(defaults.descKey) : '')}
        </p>
      )}
      {isLoading && (
        <p className="text-sm text-muted max-w-sm mb-4">
          {description ?? t(defaults.descKey)}
        </p>
      )}
      {action}
    </div>
  );
}
