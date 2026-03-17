import { useState, useCallback, useEffect } from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { CommandPalette } from '../CommandPalette';
import { AppSidebar } from './AppSidebar';
import { AppTopbar } from './AppTopbar';
import { AppContentFrame } from './AppContentFrame';
import { MobileDrawer } from './MobileDrawer';
import { cn } from '../../lib/cn';
import { SIDEBAR_WIDTH_COLLAPSED, SIDEBAR_WIDTH_EXPANDED } from './constants';

export function AppShell() {
  const { isAuthenticated } = useAuth();
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);

  const toggleSidebar = useCallback(() => setSidebarCollapsed(prev => !prev), []);
  const openPalette = useCallback(() => setPaletteOpen(true), []);
  const closePalette = useCallback(() => setPaletteOpen(false), []);
  const openMobile = useCallback(() => setMobileOpen(true), []);
  const closeMobile = useCallback(() => setMobileOpen(false), []);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setPaletteOpen(prev => !prev);
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  useEffect(() => {
    const mq = window.matchMedia('(min-width: 1024px)');
    const handler = () => { if (mq.matches) setMobileOpen(false); };
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, []);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-screen bg-canvas overflow-hidden" data-testid="app-shell">
      {/* Desktop sidebar */}
      <AppSidebar
        collapsed={sidebarCollapsed}
        onToggleCollapse={toggleSidebar}
        className="hidden lg:flex"
      />

      {/* Mobile drawer */}
      <MobileDrawer open={mobileOpen} onClose={closeMobile}>
        <AppSidebar collapsed={false} onToggleCollapse={closeMobile} mobile />
      </MobileDrawer>

      {/* Main column */}
      <div
        className={cn(
          'flex-1 flex flex-col min-h-0 min-w-0',
          'transition-[margin] duration-[var(--nto-motion-medium)] ease-[var(--ease-standard)]',
        )}
        style={{ marginLeft: sidebarCollapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH_EXPANDED }}
        data-testid="app-shell-main"
      >
        <AppTopbar
          onOpenCommandPalette={openPalette}
          onOpenMobileMenu={openMobile}
          sidebarCollapsed={sidebarCollapsed}
        />
        <AppContentFrame>
          <Outlet />
        </AppContentFrame>
      </div>

      <CommandPalette open={paletteOpen} onClose={closePalette} />
    </div>
  );
}
