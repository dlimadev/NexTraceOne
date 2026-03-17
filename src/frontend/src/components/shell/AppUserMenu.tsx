import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { LogOut, Shield, User } from 'lucide-react';
import { cn } from '../../lib/cn';
import { useAuth } from '../../contexts/AuthContext';
import { usePersona } from '../../contexts/PersonaContext';

export function AppUserMenu() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const { persona } = usePersona();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const initial = user?.email?.[0]?.toUpperCase() ?? 'U';

  const handleLogout = () => {
    setOpen(false);
    logout();
    navigate('/login');
  };

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center gap-2 p-1 rounded-md hover:bg-hover transition-all duration-[var(--nto-motion-base)]"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t('shell.userMenu')}
        data-testid="user-menu-trigger"
      >
        <div className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center text-accent text-sm font-semibold">
          {initial}
        </div>
        <div className="hidden lg:block text-left">
          <p className="text-sm font-medium text-heading leading-tight truncate max-w-[120px]">
            {user?.fullName || user?.email || t('common.user')}
          </p>
          <p className="text-[10px] text-muted leading-tight truncate max-w-[120px]">
            {user?.roleName ?? ''}
          </p>
        </div>
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-[var(--z-dropdown)]" onClick={() => setOpen(false)} aria-hidden="true" />
          <div
            className="absolute right-0 top-full mt-1.5 z-[calc(var(--z-dropdown)+1)] bg-elevated border border-edge rounded-lg shadow-floating py-1 min-w-[200px] animate-fade-in"
            role="menu"
          >
            {/* User info header */}
            <div className="px-4 py-3 border-b border-edge">
              <p className="text-sm font-medium text-heading truncate">{user?.fullName || user?.email}</p>
              <p className="text-[11px] text-muted truncate">{user?.email}</p>
              {persona && (
                <span className="inline-block mt-1.5 text-[10px] text-cyan bg-accent/10 px-2 py-0.5 rounded">
                  {t(`persona.${persona}.label`)}
                </span>
              )}
            </div>

            {/* Menu items */}
            <div className="py-1">
              <button
                onClick={() => { setOpen(false); navigate('/my-sessions'); }}
                className="w-full text-left px-4 py-2 text-sm text-body hover:bg-hover transition-colors flex items-center gap-2.5"
                role="menuitem"
              >
                <User size={14} className="text-muted" />
                <span>{t('shell.myProfile')}</span>
              </button>
              <button
                onClick={() => { setOpen(false); navigate('/access-reviews'); }}
                className="w-full text-left px-4 py-2 text-sm text-body hover:bg-hover transition-colors flex items-center gap-2.5"
                role="menuitem"
              >
                <Shield size={14} className="text-muted" />
                <span>{t('shell.myAccess')}</span>
              </button>
            </div>

            {/* Logout */}
            <div className="border-t border-edge pt-1">
              <button
                onClick={handleLogout}
                className="w-full text-left px-4 py-2 text-sm text-critical hover:bg-critical/10 transition-colors flex items-center gap-2.5"
                role="menuitem"
                data-testid="user-menu-logout"
              >
                <LogOut size={14} />
                <span>{t('auth.signOut')}</span>
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
