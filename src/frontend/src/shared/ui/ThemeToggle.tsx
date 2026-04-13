import { Sun, Moon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useTheme } from '../../contexts/ThemeContext';
import { cn } from '../../lib/cn';

interface ThemeToggleProps {
  className?: string;
}

/**
 * ThemeToggle — pill-shaped sun/moon toggle inspirado no template NexLink.
 *
 * Layout: botão pill com dois slots (sol | lua), slider animado sobre o activo.
 * Usa useTheme() para persistir preferência em localStorage.
 */
export function ThemeToggle({ className }: ThemeToggleProps) {
  const { t } = useTranslation();
  const { resolvedTheme, setTheme } = useTheme();
  const isDark = resolvedTheme === 'dark';

  return (
    <div
      className={cn(
        'relative inline-flex items-center rounded-full p-1',
        'bg-elevated border border-edge shadow-sm',
        className,
      )}
      role="radiogroup"
      aria-label={t('header.toggleTheme')}
    >
      {/* Slider indicator */}
      <div
        className={cn(
          'absolute top-1 h-[30px] w-[30px] rounded-full transition-transform duration-300 ease-[var(--ease-emphasis)]',
          'bg-panel border border-edge shadow-sm',
          isDark ? 'translate-x-[32px]' : 'translate-x-0',
        )}
        aria-hidden="true"
      />

      {/* Light button */}
      <button
        type="button"
        onClick={() => setTheme('light')}
        className={cn(
          'relative z-10 flex items-center justify-center w-[30px] h-[30px] rounded-full transition-colors duration-200',
          !isDark ? 'text-warning' : 'text-muted hover:text-body',
        )}
        role="radio"
        aria-checked={!isDark}
        aria-label={t('header.switchToLight', 'Switch to light mode')}
      >
        <Sun size={16} />
      </button>

      {/* Dark button */}
      <button
        type="button"
        onClick={() => setTheme('dark')}
        className={cn(
          'relative z-10 flex items-center justify-center w-[30px] h-[30px] rounded-full transition-colors duration-200 ml-0.5',
          isDark ? 'text-accent' : 'text-muted hover:text-body',
        )}
        role="radio"
        aria-checked={isDark}
        aria-label={t('header.switchToDark', 'Switch to dark mode')}
      >
        <Moon size={16} />
      </button>
    </div>
  );
}
