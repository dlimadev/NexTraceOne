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

/**
 * AppSidebarFooter — secção do utilizador no rodapé da sidebar.
 *
 * Collapsed: avatar com initial (gradiente de marca) + ação de logout ao hover.
 * Expanded: avatar, display name, persona/role, botão de logout ao hover.
 */
export function AppSidebarFooter({ collapsed = false, email, persona, roleName, onLogout }: AppSidebarFooterProps) {
  const { t } = useTranslation();
  const initial = email?.[0]?.toUpperCase() ?? 'U';
  const displayName = email?.split('@')[0] ?? t('common.user');

  return (
    <div className={cn('border-t border-edge shrink-0', collapsed ? 'p-2 flex justify-center' : 'p-3')}>
      {collapsed ? (
        <button
          onClick={onLogout}
          className="w-8 h-8 rounded-lg flex items-center justify-center text-white text-sm font-bold hover:brightness-90 transition-all duration-[var(--nto-motion-base)]"
          style={{ background: 'linear-gradient(135deg, #1B7FE8 0%, #12C4E8 100%)' }}
          title={t('auth.signOut')}
          aria-label={t('auth.signOut')}
        >
          {initial}
        </button>
      ) : (
        <div className="flex items-center gap-2.5 rounded-lg px-2 py-1.5 hover:bg-hover transition-colors group cursor-default">
          {/* Avatar com gradiente da marca */}
          <div
            className="w-7 h-7 rounded-lg flex items-center justify-center text-xs font-bold shrink-0 text-white"
            style={{ background: 'linear-gradient(135deg, #1B7FE8 0%, #12C4E8 100%)' }}
            aria-hidden="true"
          >
            {initial}
          </div>

          {/* User info */}
          <div className="flex-1 min-w-0">
            <p className="text-xs font-medium text-heading truncate leading-tight">{displayName}</p>
            <p className="text-[10px] text-muted truncate leading-tight">
              {persona ? t(`persona.${persona}.label`) : ''}
              {roleName ? ` · ${roleName}` : ''}
            </p>
          </div>

          {/* Logout — visível ao hover do grupo */}
          <button
            onClick={onLogout}
            className="p-1 rounded text-faded hover:text-critical opacity-0 group-hover:opacity-100 transition-all duration-[var(--nto-motion-fast)] hover:bg-critical/10 shrink-0"
            title={t('auth.signOut')}
            aria-label={t('auth.signOut')}
          >
            <LogOut size={13} />
          </button>
        </div>
      )}
    </div>
  );
}
