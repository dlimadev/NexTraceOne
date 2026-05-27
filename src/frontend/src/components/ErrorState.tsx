import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { cn } from '../lib/cn';

interface ErrorStateProps {
  /** Título do erro. */
  title?: string;
  /** Mensagem descritiva do erro. */
  message?: string;
  /** Ação de recuperação (botão de retry, link, etc.). */
  action?: ReactNode;
  /** Ícone customizado (padrão: AlertTriangle). */
  icon?: ReactNode;
  /** Variante de severidade — controla cor do icon-box. */
  variant?: 'critical' | 'warning' | 'info';
  /** Tamanho do componente. */
  size?: 'default' | 'compact';
  className?: string;
}

/** Configuração visual por variante de severidade. */
const variantConfig = {
  critical: {
    iconStyle: {
      border: '1.5px dashed rgba(220,38,38,.25)',
      background: 'rgba(220,38,38,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-critical',
  },
  warning: {
    iconStyle: {
      border: '1.5px dashed rgba(217,119,6,.25)',
      background: 'rgba(217,119,6,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-warning',
  },
  info: {
    iconStyle: {
      border: '1.5px dashed rgba(8,145,178,.25)',
      background: 'rgba(8,145,178,.06)',
      borderRadius: 14,
    },
    iconColor: 'text-info',
  },
};

/**
 * Estado de erro — feedback visual para falhas com ação de recuperação.
 *
 * Nunca genérico: sempre fornecer título e ação contextual.
 * Padrão visual alinhado com EmptyState: borda dashed + fundo tonal (6%).
 * Título: font-weight 600. Descrição: opacity 0.7.
 */
export function ErrorState({
  title,
  message,
  action,
  icon,
  variant = 'critical',
  size = 'default',
  className,
}: ErrorStateProps) {
  const { t } = useTranslation();
  const resolvedTitle = title ?? t('common.error');
  const config = variantConfig[variant];

  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center text-center animate-fade-in',
        size === 'compact' ? 'py-8 px-4' : 'py-16 px-6',
        className,
      )}
      role="alert"
    >
      {/* Icon-box: dashed border + fundo tonal — consistente com EmptyState */}
      <div
        className={cn(
          'flex items-center justify-center mb-4',
          config.iconColor,
          size === 'compact' ? 'w-10 h-10' : 'w-14 h-14',
        )}
        style={config.iconStyle}
        aria-hidden="true"
      >
        {icon ?? (
          size === 'compact'
            ? <span className="[&>svg]:w-[18px] [&>svg]:h-[18px]"><AlertTriangle /></span>
            : <AlertTriangle size={24} />
        )}
      </div>

      {/* Título: font-weight 600 */}
      <h3 className="text-sm font-semibold text-heading mb-1">{resolvedTitle}</h3>

      {/* Descrição: opacity 70% */}
      {message && (
        <p className="text-xs text-muted max-w-xs mb-4" style={{ opacity: 0.7 }}>{message}</p>
      )}

      {action}
    </div>
  );
}
