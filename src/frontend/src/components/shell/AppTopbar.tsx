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

/**
 * AppTopbar — barra de topo da aplicação.
 *
 * Layout: [menu mobile] [search flex-1] — [workspace] [actions] [divider] [user]
 * Height 80px (alinhado com Template NexLink), bg deep translúcido com backdrop-blur.
 * Breadcrumbs strip abaixo, ocultados na rota home.
 */
export function AppTopbar({ onOpenCommandPalette, onOpenMobileMenu }: AppTopbarProps) {
  const { t } = useTranslation();
  const { pathname } = useLocation();

  return (
    <div className="shrink-0">
      <header
        className={cn(
          'h-20 border-b border-edge',
          'flex items-center justify-between px-5 lg:px-6 gap-3',
          'bg-deep/90 backdrop-blur-md sticky top-0 z-[var(--z-header)]',
        )}
        role="banner"
      >
        {/* Mobile menu button */}
        <button
          onClick={onOpenMobileMenu}
          className="lg:hidden p-2 rounded-lg text-muted hover:bg-hover hover:text-body transition-all duration-[var(--nto-motion-base)]"
          aria-label={t('shell.openMenu')}
        >
          <Menu size={20} />
        </button>

        {/* Search — flex-1 to consume available horizontal space */}
        <div className="flex-1 min-w-0 max-w-md">
          <AppTopbarSearch onOpenCommandPalette={onOpenCommandPalette} />
        </div>

        {/* Right section */}
        <div className="flex items-center gap-1.5">
          <WorkspaceSwitcher />
          <AppTopbarActions />
          <div className="w-px h-6 bg-edge mx-1.5" aria-hidden="true" />
          <AppUserMenu />
        </div>
      </header>

      {/* Breadcrumbs strip — hidden on home */}
      {pathname !== '/' && <Breadcrumbs />}
    </div>
  );
}
