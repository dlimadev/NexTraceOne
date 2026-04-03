import { useState, useCallback, useEffect } from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { CommandPalette } from '../CommandPalette';
import { AppSidebar } from './AppSidebar';
import { AppTopbar } from './AppTopbar';
import { AppContentFrame } from './AppContentFrame';
import { MobileDrawer } from './MobileDrawer';
import { EnvironmentBanner } from './EnvironmentBanner';
import { cn } from '../../lib/cn';

export function AppShell() {
  const { isAuthenticated, isLoadingUser } = useAuth();
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
    if (typeof window.matchMedia !== 'function') return;
    const mq = window.matchMedia('(min-width: 1024px)');
    const handler = () => { if (mq.matches) setMobileOpen(false); };
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, []);

  // Deve verificar isLoadingUser ANTES de isAuthenticated.
  // Durante o bootstrap, isAuthenticated inicia como false e isLoadingUser como true.
  // Se verificarmos isAuthenticated primeiro, redireciona para /login antes
  // da chamada /auth/me ter oportunidade de definir isAuthenticated=true.
  if (isLoadingUser) {
    return (
      <div className="flex items-center justify-center h-screen bg-canvas">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
      </div>
    );
  }

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

      {/* Main column — marginLeft matches sidebar width on desktop only */}
      <div
        className={cn(
          'flex-1 flex flex-col min-h-0 min-w-0',
          'lg:transition-[margin] lg:duration-[var(--nto-motion-medium)] lg:ease-[var(--ease-standard)]',
          sidebarCollapsed ? 'lg:ml-20' : 'lg:ml-[320px]',
        )}
        data-testid="app-shell-main"
      >
        <AppTopbar
          onOpenCommandPalette={openPalette}
          onOpenMobileMenu={openMobile}
          sidebarCollapsed={sidebarCollapsed}
        />
        <EnvironmentBanner />
        <AppContentFrame>
          <Outlet />
        </AppContentFrame>
      </div>

      <CommandPalette open={paletteOpen} onClose={closePalette} />
    </div>
  );
}
