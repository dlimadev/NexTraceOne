import type { ReactNode } from 'react';
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
  className?: string;
}

/**
 * Estado de erro — feedback visual para falhas com ação de recuperação.
 *
 * Nunca genérico: sempre fornecer título e ação contextual.
 * Usa cor semântica critical para comunicar o estado de erro
 * sem depender apenas da cor (ícone + texto).
 */
export function ErrorState({
  title = 'Something went wrong',
  message,
  action,
  icon,
  className,
}: ErrorStateProps) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center text-center py-16 px-6 animate-fade-in',
        className,
      )}
      role="alert"
    >
      <div className="flex items-center justify-center w-14 h-14 rounded-lg bg-critical/15 border border-critical/25 text-critical mb-4">
        {icon ?? <AlertTriangle size={24} />}
      </div>
      <h3 className="text-sm font-semibold text-heading mb-1">{title}</h3>
      {message && <p className="text-xs text-muted max-w-xs mb-4">{message}</p>}
      {action}
    </div>
  );
}
