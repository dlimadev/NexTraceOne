import { useTranslation } from 'react-i18next';
import { LogOut } from 'lucide-react';
import { cn } from '../../lib/cn';

interface AppSidebarFooterProps {
  collapsed?: boolean;
  email?: string;
  persona?: string;
  roleName?: string;
  onLogout: () => void;
}

export function AppSidebarFooter({ collapsed = false, email, persona, roleName, onLogout }: AppSidebarFooterProps) {
  const { t } = useTranslation();
  const initial = email?.[0]?.toUpperCase() ?? 'U';

  return (
    <div className={cn('py-3 border-t border-edge shrink-0', collapsed ? 'px-2 flex justify-center' : 'px-4')}>
      {collapsed ? (
        <button
          onClick={onLogout}
          className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center text-cyan text-sm font-semibold hover:bg-critical/20 hover:text-critical transition-all duration-[var(--nto-motion-base)]"
          title={t('auth.signOut')}
          aria-label={t('auth.signOut')}
        >
          {initial}
        </button>
      ) : (
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center text-cyan text-sm font-semibold shrink-0" aria-hidden="true">
            {initial}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-heading truncate">{email ?? t('common.user')}</p>
            <p className="text-[11px] text-muted truncate">
              {persona ? t(`persona.${persona}.label`) : ''}{roleName ? ` · ${roleName}` : ''}
            </p>
          </div>
          <button
            onClick={onLogout}
            className="text-muted hover:text-critical transition-colors p-1 rounded hover:bg-critical/10"
            title={t('auth.signOut')}
            aria-label={t('auth.signOut')}
          >
            <LogOut size={16} />
          </button>
        </div>
      )}
    </div>
  );
}
