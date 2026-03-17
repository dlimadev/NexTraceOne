import { AlertCircle, CheckCircle2, Info } from 'lucide-react';
import { cn } from '../../../lib/cn';

type FeedbackVariant = 'error' | 'success' | 'info';

interface AuthFeedbackProps {
  variant: FeedbackVariant;
  message: string;
  className?: string;
}

const config: Record<FeedbackVariant, { icon: typeof AlertCircle; bg: string; border: string; text: string }> = {
  error: {
    icon: AlertCircle,
    bg: 'bg-critical/10',
    border: 'border-critical/25',
    text: 'text-critical',
  },
  success: {
    icon: CheckCircle2,
    bg: 'bg-success/10',
    border: 'border-success/25',
    text: 'text-success',
  },
  info: {
    icon: Info,
    bg: 'bg-info/10',
    border: 'border-info/25',
    text: 'text-info',
  },
};

/**
 * Feedback inline para fluxos de autenticação — erro, sucesso, informação.
 * Usa ícone + mensagem com cores semânticas.
 */
export function AuthFeedback({ variant, message, className }: AuthFeedbackProps) {
  const { icon: Icon, bg, border, text } = config[variant];

  return (
    <div
      role="alert"
      className={cn(
        'rounded-md px-4 py-3 flex items-start gap-3 animate-fade-in border',
        bg,
        border,
        className,
      )}
    >
      <Icon size={16} className={cn(text, 'shrink-0 mt-0.5')} />
      <p className={cn('text-sm', text)}>{message}</p>
    </div>
  );
}
