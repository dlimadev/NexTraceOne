import { useState, useCallback, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, Sun, Moon, Monitor, ChevronDown } from 'lucide-react';
import { cn } from '../../lib/cn';
import { NotificationBell } from '../../features/notifications';
import { useTheme } from '../../contexts/ThemeContext';

const SUPPORTED_LANGUAGES = [
  { code: 'en', label: 'English', flag: '🇺🇸', short: 'EN' },
  { code: 'pt-BR', label: 'Português (Brasil)', flag: '🇧🇷', short: 'BR' },
  { code: 'pt-PT', label: 'Português (Portugal)', flag: '🇵🇹', short: 'PT' },
  { code: 'es', label: 'Español', flag: '🇪🇸', short: 'ES' },
] as const;

export function AppTopbarActions() {
  const { t, i18n } = useTranslation();
  const { theme, toggleTheme } = useTheme();
  const [langOpen, setLangOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const selectLanguage = useCallback((code: string) => {
    i18n.changeLanguage(code);
    setLangOpen(false);
  }, [i18n]);

  const currentLang = SUPPORTED_LANGUAGES.find(l => l.code === i18n.language) ?? SUPPORTED_LANGUAGES[0];

  // Close on Escape key
  useEffect(() => {
    if (!langOpen) return;
    const handleKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setLangOpen(false);
    };
    document.addEventListener('keydown', handleKey);
    return () => document.removeEventListener('keydown', handleKey);
  }, [langOpen]);

  const themeTitleKeys: Record<string, string> = {
    dark: 'header.switchToLight',
    light: 'header.switchToAuto',
    auto: 'header.switchToDark',
  };
  const themeTitle = t(themeTitleKeys[theme] ?? 'header.switchToDark', 'Switch to dark mode');

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

      {/* Language selector — flag + code + dropdown */}
      <div className="relative" ref={menuRef}>
        <button
          onClick={() => setLangOpen(!langOpen)}
          className={cn(
            'flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg',
            'text-muted hover:bg-hover hover:text-body',
            'transition-all duration-[var(--nto-motion-base)]',
            'border border-transparent hover:border-edge',
            langOpen && 'bg-hover border-edge text-body',
          )}
          title={t('header.toggleLanguage')}
          aria-label={t('header.toggleLanguage')}
          aria-haspopup="true"
          aria-expanded={langOpen}
        >
          <span className="text-base leading-none" aria-hidden="true">{currentLang.flag}</span>
          <span className="text-xs font-semibold hidden sm:inline">{currentLang.short}</span>
          <ChevronDown size={12} className={cn(
            'transition-transform duration-[var(--nto-motion-base)]',
            langOpen && 'rotate-180',
          )} />
        </button>
        {langOpen && (
          <>
            <div className="fixed inset-0 z-[var(--z-dropdown)]" onClick={() => setLangOpen(false)} aria-hidden="true" />
            <div
              className="absolute right-0 top-full mt-2 z-[calc(var(--z-dropdown)+1)] bg-elevated border border-edge rounded-xl shadow-floating py-2 min-w-[220px] animate-fade-in"
              role="menu"
            >
              <div className="px-3 pb-2 mb-1 border-b border-edge">
                <p className="type-overline text-faded">{t('header.selectLanguage')}</p>
              </div>
              {SUPPORTED_LANGUAGES.map(({ code, label, flag, short }) => {
                const isActive = i18n.language === code;
                return (
                  <button
                    key={code}
                    onClick={() => selectLanguage(code)}
                    className={cn(
                      'w-full text-left px-3 py-2.5 text-sm transition-colors',
                      'flex items-center gap-3',
                      isActive
                        ? 'bg-accent/10 text-cyan'
                        : 'text-body hover:bg-hover',
                    )}
                    role="menuitem"
                    aria-current={isActive ? 'true' : undefined}
                  >
                    <span className="text-lg leading-none shrink-0" aria-hidden="true">{flag}</span>
                    <div className="flex-1 min-w-0">
                      <span className="block font-medium truncate">{label}</span>
                      <span className="block text-[10px] text-muted uppercase tracking-wider">{short}</span>
                    </div>
                    {isActive && <Check size={14} className="text-cyan shrink-0" />}
                  </button>
                );
              })}
            </div>
          </>
        )}
      </div>

      {/* Notifications */}
      <NotificationBell />
    </>
  );
}
