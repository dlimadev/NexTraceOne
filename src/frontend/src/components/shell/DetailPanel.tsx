import type { ReactNode } from 'react';
import { cn } from '../../lib/cn';
import { X } from 'lucide-react';
import { useTranslation } from 'react-i18next';

interface DetailPanelProps {
  children: ReactNode;
  className?: string;
  title?: string;
  onClose?: () => void;
  open?: boolean;
}

export function DetailPanel({ children, className, title, onClose, open = true }: DetailPanelProps) {
  const { t } = useTranslation();

  if (!open) return null;

  return (
    <aside
      className={cn(
        'bg-panel border-l border-edge w-full lg:w-[400px] xl:w-[480px] shrink-0',
        'overflow-y-auto animate-slide-right',
        className,
      )}
      role="complementary"
      aria-label={title}
    >
      {(title || onClose) && (
        <div className="flex items-center justify-between gap-3 px-5 py-4 border-b border-edge sticky top-0 bg-panel/95 backdrop-blur-sm z-10">
          {title && <h3 className="text-sm font-semibold text-heading truncate">{title}</h3>}
          {onClose && (
            <button
              onClick={onClose}
              className="p-1 rounded-md text-muted hover:text-heading hover:bg-hover transition-all duration-[var(--nto-motion-base)]"
              aria-label={t('common.close')}
            >
              <X size={16} />
            </button>
          )}
        </div>
      )}
      <div className="p-5">{children}</div>
    </aside>
  );
}
