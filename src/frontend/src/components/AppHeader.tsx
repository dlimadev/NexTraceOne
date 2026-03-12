import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, Bell, Globe, Settings } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

interface AppHeaderProps {
  /** Callback para abrir a Command Palette (Cmd+K). */
  onOpenCommandPalette: () => void;
}

/**
 * Header global do aplicativo — barra superior fixa dentro do layout principal.
 *
 * Contém:
 * - Gatilho de busca global (abre CommandPalette)
 * - Alternador de idioma (pt-BR / en)
 * - Sino de notificações (placeholder para MVP1)
 * - Avatar do usuário autenticado com nome e papel
 */
export function AppHeader({ onOpenCommandPalette }: AppHeaderProps) {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const [langOpen, setLangOpen] = useState(false);

  const toggleLang = useCallback(() => {
    const next = i18n.language?.startsWith('pt') ? 'en' : 'pt-BR';
    i18n.changeLanguage(next);
    setLangOpen(false);
  }, [i18n]);

  return (
    <header className="h-14 shrink-0 bg-panel border-b border-edge flex items-center justify-between px-4 gap-4">
      {/* Search trigger */}
      <button
        onClick={onOpenCommandPalette}
        className="flex items-center gap-2 px-3 py-1.5 rounded-md bg-canvas border border-edge text-muted text-sm hover:border-edge-strong hover:text-body transition-colors max-w-xs w-64"
        aria-label={t('commandPalette.title')}
      >
        <Search size={15} className="shrink-0" />
        <span className="flex-1 text-left truncate">{t('commandPalette.placeholder')}</span>
        <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded border border-edge px-1.5 py-0.5 text-[10px] font-mono">
          ⌘K
        </kbd>
      </button>

      {/* Right section */}
      <div className="flex items-center gap-1">
        {/* Language toggle */}
        <div className="relative">
          <button
            onClick={() => setLangOpen(!langOpen)}
            className="p-2 rounded-md text-muted hover:bg-hover hover:text-body transition-colors"
            title={t('header.toggleLanguage')}
            aria-label={t('header.toggleLanguage')}
          >
            <Globe size={18} />
          </button>
          {langOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setLangOpen(false)} />
              <div className="absolute right-0 top-full mt-1 z-50 bg-elevated border border-edge rounded-lg shadow-lg py-1 min-w-[120px] animate-fade-in">
                <button
                  onClick={toggleLang}
                  className="w-full text-left px-3 py-2 text-sm text-body hover:bg-hover transition-colors"
                >
                  {i18n.language?.startsWith('pt') ? t('header.languageEnglish') : t('header.languagePortuguese')}
                </button>
              </div>
            </>
          )}
        </div>

        {/* Notifications (placeholder) */}
        <button
          className="p-2 rounded-md text-muted hover:bg-hover hover:text-body transition-colors relative"
          title={t('header.notifications')}
          aria-label={t('header.notifications')}
        >
          <Bell size={18} />
        </button>

        {/* Settings (placeholder) */}
        <button
          className="p-2 rounded-md text-muted hover:bg-hover hover:text-body transition-colors"
          title={t('header.settings')}
          aria-label={t('header.settings')}
        >
          <Settings size={18} />
        </button>

        {/* Separator */}
        <div className="w-px h-6 bg-edge mx-2" />

        {/* User avatar */}
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-full bg-accent/20 flex items-center justify-center text-accent text-sm font-semibold">
            {user?.email?.[0]?.toUpperCase() ?? 'U'}
          </div>
          <div className="hidden lg:block">
            <p className="text-sm font-medium text-heading leading-tight truncate max-w-[140px]">
              {user?.fullName || user?.email || 'User'}
            </p>
            <p className="text-[11px] text-muted leading-tight truncate max-w-[140px]">
              {user?.roleName ?? ''}
            </p>
          </div>
        </div>
      </div>
    </header>
  );
}
