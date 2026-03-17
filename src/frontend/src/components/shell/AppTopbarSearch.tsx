import { useTranslation } from 'react-i18next';
import { Search } from 'lucide-react';
import { cn } from '../../lib/cn';

interface AppTopbarSearchProps {
  onOpenCommandPalette: () => void;
}

export function AppTopbarSearch({ onOpenCommandPalette }: AppTopbarSearchProps) {
  const { t } = useTranslation();

  return (
    <button
      onClick={onOpenCommandPalette}
      className={cn(
        'flex items-center gap-2.5 px-3 py-1.5 rounded-md',
        'bg-input border border-edge text-muted text-sm',
        'hover:border-edge-strong hover:text-body',
        'transition-all duration-[var(--nto-motion-base)]',
        'max-w-xs w-64',
      )}
      aria-label={t('commandPalette.title')}
    >
      <Search size={14} className="shrink-0" />
      <span className="flex-1 text-left truncate hidden sm:inline">{t('commandPalette.placeholder')}</span>
      <kbd className="hidden md:inline-flex items-center gap-0.5 rounded-sm border border-edge px-1.5 py-0.5 text-[10px] font-mono text-faded">
        ⌘K
      </kbd>
    </button>
  );
}
