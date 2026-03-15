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
 * Estrutura: Sidebar fixa à esquerda + coluna direita com AppHeader (h-14) + conteúdo scrollável.
 * A CommandPalette é montada aqui e aberta via Cmd+K / Ctrl+K ou botão no header.
 */
export function AppLayout() {
  const { isAuthenticated } = useAuth();
  const { pathname } = useLocation();
  const [paletteOpen, setPaletteOpen] = useState(false);

  const openPalette = useCallback(() => setPaletteOpen(true), []);
  const closePalette = useCallback(() => setPaletteOpen(false), []);

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

  return (
    <div className="flex h-screen bg-canvas">
      <Sidebar />
      <div className="flex-1 ml-64 flex flex-col min-h-0">
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
