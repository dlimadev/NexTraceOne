import { useState, useEffect, useCallback } from 'react';
import { Outlet, Navigate, useLocation } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { AppHeader } from './AppHeader';
import { CommandPalette } from './CommandPalette';
import { Breadcrumbs } from './Breadcrumbs';
import { useAuth } from '../contexts/AuthContext';

/**
 * App Shell — DESIGN-SYSTEM.md §4.1 / GUIDELINE.md §5.1
 *
 * Sidebar fixa (264-280px) + coluna direita com Topbar (64-72px) + conteúdo scrollável.
 * Fundo canvas profundo, transição suave ao colapsar sidebar.
 */
export function AppLayout() {
  const { isAuthenticated } = useAuth();
  const { pathname } = useLocation();
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const openPalette = useCallback(() => setPaletteOpen(true), []);
  const closePalette = useCallback(() => setPaletteOpen(false), []);
  const toggleSidebar = useCallback(() => setSidebarCollapsed((prev) => !prev), []);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setPaletteOpen((prev) => !prev);
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-screen bg-canvas">
      <Sidebar collapsed={sidebarCollapsed} onToggleCollapse={toggleSidebar} />
      <div
        className="flex-1 flex flex-col min-h-0 transition-all duration-[var(--nto-motion-medium)]"
        style={{ marginLeft: sidebarCollapsed ? 64 : 272 }}
      >
        <AppHeader onOpenCommandPalette={openPalette} />
        {pathname !== '/' && <Breadcrumbs />}
        <main className="flex-1 overflow-y-auto">
          <Outlet />
        </main>
      </div>
      <CommandPalette open={paletteOpen} onClose={closePalette} />
    </div>
  );
}
