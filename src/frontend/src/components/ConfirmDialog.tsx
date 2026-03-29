import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle } from 'lucide-react';
import { Modal } from './Modal';
import { Button } from './Button';

type ConfirmVariant = 'danger' | 'warning' | 'info';

interface ConfirmDialogProps {
  /** Controle de abertura/fechamento. */
  open: boolean;
  /** Callback ao fechar (cancelar). */
  onClose: () => void;
  /** Callback ao confirmar. */
  onConfirm: () => void;
  /** Título do diálogo. */
  title: string;
  /** Descrição/mensagem explicativa. */
  description?: string;
  /** Conteúdo adicional. */
  children?: ReactNode;
  /** Label do botão de confirmação. Padrão: common.confirm. */
  confirmLabel?: string;
  /** Label do botão de cancelamento. Padrão: common.cancel. */
  cancelLabel?: string;
  /** Variante visual. Afeta ícone e cor do botão de confirmação. */
  variant?: ConfirmVariant;
  /** Se a ação está em progresso (disable botões). */
  loading?: boolean;
}

const variantStyles: Record<ConfirmVariant, { iconBg: string; iconColor: string; buttonVariant: 'danger' | 'primary' | 'secondary' }> = {
  danger: { iconBg: 'bg-critical-muted', iconColor: 'text-critical', buttonVariant: 'danger' },
  warning: { iconBg: 'bg-warning-muted', iconColor: 'text-warning', buttonVariant: 'primary' },
  info: { iconBg: 'bg-info-muted', iconColor: 'text-info', buttonVariant: 'primary' },
};

/**
 * Diálogo de confirmação reutilizável para ações destrutivas ou irreversíveis.
 *
 * Usa o componente Modal existente com layout padronizado:
 * ícone + título + descrição + botões Cancelar/Confirmar.
 *
 * Todas as labels suportam i18n via props ou fallback para common.confirm/common.cancel.
 *
 * @see docs/DESIGN-SYSTEM.md §4.11
 */
export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  children,
  confirmLabel,
  cancelLabel,
  variant = 'danger',
  loading = false,
}: ConfirmDialogProps) {
  const { t } = useTranslation();
  const styles = variantStyles[variant];

  return (
    <Modal
      open={open}
      onClose={onClose}
      size="sm"
      footer={
        <>
          <Button
            variant="secondary"
            size="sm"
            onClick={onClose}
            disabled={loading}
          >
            {cancelLabel ?? t('common.cancel')}
          </Button>
          <Button
            variant={styles.buttonVariant}
            size="sm"
            onClick={onConfirm}
            disabled={loading}
            aria-label={confirmLabel ?? t('common.confirm')}
          >
            {loading ? t('common.loading') : (confirmLabel ?? t('common.confirm'))}
          </Button>
        </>
      }
    >
      <div className="flex flex-col items-center text-center gap-4">
        <div className={`w-12 h-12 rounded-full flex items-center justify-center ${styles.iconBg}`}>
          <AlertTriangle size={24} className={styles.iconColor} />
        </div>
        <div>
          <h3 className="text-base font-semibold text-heading mb-1">{title}</h3>
          {description && (
            <p className="text-sm text-muted">{description}</p>
          )}
        </div>
        {children}
      </div>
    </Modal>
  );
}
