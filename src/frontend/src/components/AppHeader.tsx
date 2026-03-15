import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Search, Bell, Globe, Settings, Check } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';

interface AppHeaderProps {
  /** Callback para abrir a Command Palette (Cmd+K). */
  onOpenCommandPalette: () => void;
}

/**
 * Lista de idiomas suportados pelo frontend.
 * Cada entrada contém o código i18n, o rótulo exibido e um indicador visual.
 */
const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'pt-BR', label: 'Português (Brasil)' },
  { code: 'pt-PT', label: 'Português (Portugal)' },
  { code: 'es', label: 'Español' },
] as const;

/**
 * Header global do aplicativo — barra superior fixa dentro do layout principal.
 *
 * Contém:
 * - Gatilho de busca global (abre CommandPalette)
 * - Seletor de idioma (en, pt-BR, pt-PT, es)
 * - Sino de notificações (placeholder para MVP1)
 * - Avatar do utilizador autenticado com nome e papel
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
        {/* Language selector */}
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
              <div className="absolute right-0 top-full mt-1 z-50 bg-elevated border border-edge rounded-lg shadow-lg py-1 min-w-[180px] animate-fade-in">
                {SUPPORTED_LANGUAGES.map(({ code, label }) => (
                  <button
                    key={code}
                    onClick={() => selectLanguage(code)}
                    className="w-full text-left px-3 py-2 text-sm text-body hover:bg-hover transition-colors flex items-center justify-between gap-2"
                  >
                    <span>{label}</span>
                    {i18n.language === code && <Check size={14} className="text-accent shrink-0" />}
                  </button>
                ))}
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
