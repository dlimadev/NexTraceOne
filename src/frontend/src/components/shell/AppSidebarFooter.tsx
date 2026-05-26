import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LogOut, User, Settings, ChevronUp } from 'lucide-react';
import { cn } from '../../lib/cn';

interface AppSidebarFooterProps {
  collapsed?: boolean;
  email?: string;
  persona?: string;
  roleName?: string;
  onLogout: () => void;
}

/**
 * AppSidebarFooter — rodapé da sidebar com info do utilizador.
 *
 * Collapsed: avatar (gradiente de marca, 30×30, radius 8px).
 * Expanded: avatar + display name + persona/role + chevron.
 * Clicável → mini-menu com Perfil, Preferências, Logout.
 *
 * Nota: usa cores hardcoded para manter sempre-dark independente do tema.
 */
export function AppSidebarFooter({
  collapsed = false,
  email,
  persona,
  roleName,
  onLogout,
}: AppSidebarFooterProps) {
  const { t } = useTranslation();
  const [menuOpen, setMenuOpen] = useState(false);

  const initial = email?.[0]?.toUpperCase() ?? 'U';
  const displayName = email?.split('@')[0] ?? t('common.user');

  const avatarStyle: React.CSSProperties = {
    background: 'linear-gradient(135deg, #1B7FE8, #12C4E8, #18E8B8)',
    borderRadius: '8px',
    width: 30,
    height: 30,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    flexShrink: 0,
    fontSize: 12,
    fontWeight: 700,
    color: '#FFFFFF',
  };

  return (
    <div className="border-t border-[rgba(129,170,214,0.08)] shrink-0 relative">
      {collapsed ? (
        /* Rail mode — avatar only, click opens menu */
        <div className="p-2 flex justify-center">
          <button
            onClick={() => setMenuOpen((v) => !v)}
            style={avatarStyle}
            title={displayName}
            aria-label={t('shell.userMenu', 'User menu')}
            aria-expanded={menuOpen}
          >
            {initial}
          </button>
        </div>
      ) : (
        /* Expanded mode — avatar + name + role + chevron */
        <button
          onClick={() => setMenuOpen((v) => !v)}
          className={cn(
            'w-full flex items-center gap-2.5 px-3 py-3',
            'rounded-none transition-colors duration-150',
            'hover:bg-[rgba(255,255,255,.04)]',
          )}
          aria-expanded={menuOpen}
          aria-label={t('shell.userMenu', 'User menu')}
        >
          <div style={avatarStyle} aria-hidden="true">
            {initial}
          </div>
          <div className="flex-1 min-w-0 text-left">
            <p style={{ fontSize: 11, fontWeight: 600, color: '#F2F7FF' }} className="truncate leading-tight">
              {displayName}
            </p>
            <p style={{ fontSize: 9, color: 'rgba(142,160,183,.6)' }} className="truncate leading-tight">
              {persona ? t(`persona.${persona}.label`) : ''}
              {roleName ? ` · ${roleName}` : ''}
            </p>
          </div>
          <ChevronUp
            size={14}
            style={{ color: 'rgba(129,170,214,.4)', transform: menuOpen ? 'rotate(180deg)' : 'rotate(0deg)', transition: 'transform 150ms' }}
            aria-hidden="true"
          />
        </button>
      )}

      {/* Mini-menu */}
      {menuOpen && (
        <>
          {/* Backdrop */}
          <div
            className="fixed inset-0 z-[var(--z-dropdown)]"
            onClick={() => setMenuOpen(false)}
            aria-hidden="true"
          />
          <div
            className="absolute bottom-full left-2 right-2 mb-1 rounded-xl overflow-hidden z-[var(--z-dropdown)]"
            style={{
              background: '#0F1E38',
              border: '1px solid rgba(129,170,214,.14)',
              boxShadow: '0 8px 24px rgba(0,0,0,.4)',
            }}
            role="menu"
          >
            <button
              className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[12px] font-medium hover:bg-[rgba(255,255,255,.05)] transition-colors"
              style={{ color: 'rgba(181,196,216,.9)' }}
              onClick={() => setMenuOpen(false)}
              role="menuitem"
            >
              <User size={13} style={{ color: 'rgba(129,170,214,.6)' }} />
              {t('nav.profile', 'Perfil')}
            </button>
            <button
              className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[12px] font-medium hover:bg-[rgba(255,255,255,.05)] transition-colors"
              style={{ color: 'rgba(181,196,216,.9)' }}
              onClick={() => setMenuOpen(false)}
              role="menuitem"
            >
              <Settings size={13} style={{ color: 'rgba(129,170,214,.6)' }} />
              {t('nav.preferences', 'Preferências')}
            </button>
            <div style={{ height: 1, background: 'rgba(129,170,214,.08)', margin: '2px 0' }} />
            <button
              className="w-full flex items-center gap-2.5 px-3 py-2.5 text-left text-[12px] font-medium hover:bg-[rgba(239,68,68,.08)] transition-colors"
              style={{ color: 'rgba(239,68,68,.8)' }}
              onClick={() => { setMenuOpen(false); onLogout(); }}
              role="menuitem"
            >
              <LogOut size={13} />
              {t('auth.signOut')}
            </button>
          </div>
        </>
      )}
    </div>
  );
}
