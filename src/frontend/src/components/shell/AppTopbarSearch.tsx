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
        'flex items-center gap-2.5 px-3.5 py-2 rounded-lg',
        'bg-input border border-edge text-muted text-sm',
        'hover:border-edge-strong hover:text-body hover:bg-panel',
        'transition-all duration-[var(--nto-motion-base)]',
        'max-w-sm w-72',
      )}
      aria-label={t('commandPalette.title')}
    >
      <Search size={15} className="shrink-0 text-faded" />
      <span className="flex-1 text-left truncate hidden sm:inline">{t('commandPalette.placeholder')}</span>
      <kbd className="hidden md:inline-flex items-center gap-0.5 rounded border border-edge px-1.5 py-0.5 text-[10px] font-mono text-faded bg-elevated/60">
        ⌘K
      </kbd>
    </button>
  );
}
