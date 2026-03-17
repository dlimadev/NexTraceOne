import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, Bell, Globe, Settings, Check } from 'lucide-react';
import { cn } from '../lib/cn';
import { useAuth } from '../contexts/AuthContext';

interface AppHeaderProps {
  onOpenCommandPalette: () => void;
}

const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'pt-BR', label: 'Português (Brasil)' },
  { code: 'pt-PT', label: 'Português (Portugal)' },
  { code: 'es', label: 'Español' },
] as const;

/**
 * Topbar — DESIGN-SYSTEM.md §4.1
 * Altura 64-72px, busca global, seletor de idioma, notificações, perfil.
 * Superfície premium com borda inferior translúcida.
 */
export function AppHeader({ onOpenCommandPalette }: AppHeaderProps) {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const [langOpen, setLangOpen] = useState(false);

  const selectLanguage = useCallback((code: string) => {
    i18n.changeLanguage(code);
    setLangOpen(false);
  }, [i18n]);

  return (
    <header className="h-16 shrink-0 bg-deep/80 backdrop-blur-sm border-b border-edge flex items-center justify-between px-5 gap-4">
      {/* Search trigger */}
      <button
        onClick={onOpenCommandPalette}
        className={cn(
          'flex items-center gap-2.5 px-4 py-2 rounded-md',
          'bg-input border border-edge text-muted text-sm',
          'hover:border-edge-strong hover:text-body',
          'transition-all duration-[var(--nto-motion-base)]',
          'max-w-xs w-72',
        )}
        aria-label={t('commandPalette.title')}
      >
        <Search size={15} className="shrink-0" />
        <span className="flex-1 text-left truncate">{t('commandPalette.placeholder')}</span>
        <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded-sm border border-edge px-1.5 py-0.5 text-[10px] font-mono text-faded">
          ⌘K
        </kbd>
      </button>

      {/* Right section */}
      <div className="flex items-center gap-1">
        {/* Language selector */}
        <div className="relative">
          <button
            onClick={() => setLangOpen(!langOpen)}
            className="p-2.5 rounded-md text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)]"
            title={t('header.toggleLanguage')}
            aria-label={t('header.toggleLanguage')}
          >
            <Globe size={18} />
          </button>
          {langOpen && (
            <>
              <div className="fixed inset-0 z-[var(--z-dropdown)]" onClick={() => setLangOpen(false)} />
              <div className="absolute right-0 top-full mt-1.5 z-[calc(var(--z-dropdown)+1)] bg-elevated border border-edge rounded-lg shadow-floating py-1.5 min-w-[190px] animate-fade-in">
                {SUPPORTED_LANGUAGES.map(({ code, label }) => (
                  <button
                    key={code}
                    onClick={() => selectLanguage(code)}
                    className="w-full text-left px-4 py-2 text-sm text-body hover:bg-hover transition-colors flex items-center justify-between gap-2"
                  >
                    <span>{label}</span>
                    {i18n.language === code && <Check size={14} className="text-cyan shrink-0" />}
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        {/* Notifications */}
        <button
          className="p-2.5 rounded-md text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)] relative"
          title={t('header.notifications')}
          aria-label={t('header.notifications')}
        >
          <Bell size={18} />
        </button>

        {/* Settings */}
        <button
          className="p-2.5 rounded-md text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)]"
          title={t('header.settings')}
          aria-label={t('header.settings')}
        >
          <Settings size={18} />
        </button>

        {/* Separator */}
        <div className="w-px h-7 bg-edge mx-2" />

        {/* User avatar */}
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center text-accent text-sm font-semibold">
            {user?.email?.[0]?.toUpperCase() ?? 'U'}
          </div>
          <div className="hidden lg:block">
            <p className="text-sm font-medium text-heading leading-tight truncate max-w-[140px]">
              {user?.fullName || user?.email || t('common.user')}
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
