import { useTranslation } from 'react-i18next';
import { Menu } from 'lucide-react';
import { cn } from '../../lib/cn';
import { Breadcrumbs } from '../Breadcrumbs';
import { AppTopbarSearch } from './AppTopbarSearch';
import { AppTopbarActions } from './AppTopbarActions';
import { WorkspaceSwitcher } from './WorkspaceSwitcher';
import { AppUserMenu } from './AppUserMenu';
import { useLocation } from 'react-router-dom';

interface AppTopbarProps {
  onOpenCommandPalette: () => void;
  onOpenMobileMenu: () => void;
  sidebarCollapsed?: boolean;
}

export function AppTopbar({ onOpenCommandPalette, onOpenMobileMenu }: AppTopbarProps) {
  const { t } = useTranslation();
  const { pathname } = useLocation();

  return (
    <div className="shrink-0">
      <header
        className={cn(
          'h-14 bg-deep/80 backdrop-blur-sm border-b border-edge',
          'flex items-center justify-between px-4 lg:px-5 gap-3',
        )}
        role="banner"
      >
        {/* Mobile menu button */}
        <button
          onClick={onOpenMobileMenu}
          className="lg:hidden p-2 rounded-md text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)]"
          aria-label={t('shell.openMenu')}
        >
          <Menu size={20} />
        </button>

        {/* Search */}
        <AppTopbarSearch onOpenCommandPalette={onOpenCommandPalette} />

        {/* Right section */}
        <div className="flex items-center gap-1.5">
          <WorkspaceSwitcher />
          <AppTopbarActions />
          <div className="w-px h-7 bg-edge mx-1.5" aria-hidden="true" />
          <AppUserMenu />
        </div>
      </header>

      {/* Breadcrumbs strip */}
      {pathname !== '/' && <Breadcrumbs />}
    </div>
  );
}
