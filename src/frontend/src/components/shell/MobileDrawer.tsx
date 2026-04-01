import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';

interface MobileDrawerProps {
  open: boolean;
  onClose: () => void;
  children: ReactNode;
}

export function MobileDrawer({ open, onClose, children }: MobileDrawerProps) {
  const { t } = useTranslation();

  if (!open) return null;

  return (
    <div className="lg:hidden fixed inset-0 z-[var(--z-modal)]" role="dialog" aria-modal="true" aria-label={t('shell.mobileMenu')}>
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-overlay animate-fade-in"
        onClick={onClose}
        aria-hidden="true"
      />
      {/* Drawer panel */}
      <div className="relative h-full w-[250px] animate-slide-right">
        <button
          onClick={onClose}
          className="absolute top-4 right-[-44px] p-2 rounded-lg text-muted hover:text-heading bg-elevated/80"
          aria-label={t('common.close')}
        >
          <X size={18} />
        </button>
        {children}
      </div>
    </div>
  );
}
