import { useState, useEffect, useCallback } from 'react';
import { Outlet, Navigate, useLocation } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { AppHeader } from './AppHeader';
import { CommandPalette } from './CommandPalette';
import { Breadcrumbs } from './Breadcrumbs';
import { useAuth } from '../contexts/AuthContext';

/**
 * Shell principal da aplicação autenticada.
 *
 * Estrutura: Sidebar (fixa, colapsável) à esquerda + coluna direita com AppHeader + conteúdo scrollável.
 * A CommandPalette é montada aqui e aberta via Cmd+K / Ctrl+K ou botão no header.
 */
export function AppLayout() {
  const { isAuthenticated } = useAuth();
  const { pathname } = useLocation();
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const openPalette = useCallback(() => setPaletteOpen(true), []);
  const closePalette = useCallback(() => setPaletteOpen(false), []);
  const toggleSidebar = useCallback(() => setSidebarCollapsed((prev) => !prev), []);

  /** Atalho global: Cmd+K (macOS) / Ctrl+K (Windows/Linux) abre a Command Palette. */
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

  const marginLeft = sidebarCollapsed ? 'ml-16' : 'ml-64';

  return (
    <div className="flex h-screen bg-canvas">
      <Sidebar collapsed={sidebarCollapsed} onToggleCollapse={toggleSidebar} />
      <div className={`flex-1 ${marginLeft} flex flex-col min-h-0 transition-all duration-200`}>
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
