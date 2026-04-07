import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Globe, Check, Sun, Moon, Monitor } from 'lucide-react';
import { NotificationBell } from '../../features/notifications';
import { useTheme } from '../../contexts/ThemeContext';

const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English' },
  { code: 'pt-BR', label: 'Português (Brasil)' },
  { code: 'pt-PT', label: 'Português (Portugal)' },
  { code: 'es', label: 'Español' },
] as const;

export function AppTopbarActions() {
  const { t, i18n } = useTranslation();
  const { theme, toggleTheme } = useTheme();
  const [langOpen, setLangOpen] = useState(false);

  const selectLanguage = useCallback((code: string) => {
    i18n.changeLanguage(code);
    setLangOpen(false);
  }, [i18n]);

  const themeTitle = theme === 'dark'
    ? t('header.switchToLight', 'Switch to light mode')
    : theme === 'light'
      ? t('header.switchToAuto', 'Switch to system mode')
      : t('header.switchToDark', 'Switch to dark mode');

  const ThemeIcon = theme === 'dark' ? Sun : theme === 'light' ? Monitor : Moon;

  return (
    <>
      {/* Theme toggle — cycles dark→light→auto */}
      <button
        onClick={toggleTheme}
        className="p-2.5 rounded-lg text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)]"
        title={themeTitle}
        aria-label={themeTitle}
      >
        <ThemeIcon size={18} aria-hidden="true" />
      </button>

      {/* Language selector */}
      <div className="relative">
        <button
          onClick={() => setLangOpen(!langOpen)}
          className="p-2.5 rounded-lg text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)]"
          title={t('header.toggleLanguage')}
          aria-label={t('header.toggleLanguage')}
          aria-haspopup="true"
          aria-expanded={langOpen}
        >
          <Globe size={18} />
        </button>
        {langOpen && (
          <>
            <div className="fixed inset-0 z-[var(--z-dropdown)]" onClick={() => setLangOpen(false)} aria-hidden="true" />
            <div
              className="absolute right-0 top-full mt-1.5 z-[calc(var(--z-dropdown)+1)] bg-elevated border border-edge rounded-lg shadow-floating py-1.5 min-w-[190px] animate-fade-in"
              role="menu"
            >
              {SUPPORTED_LANGUAGES.map(({ code, label }) => (
                <button
                  key={code}
                  onClick={() => selectLanguage(code)}
                  className="w-full text-left px-4 py-2 text-sm text-body hover:bg-hover transition-colors flex items-center justify-between gap-2"
                  role="menuitem"
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
      <NotificationBell />
    </>
  );
}
