import type { ReactNode } from 'react';
import { ThemeToggle } from '../../../shared/ui';
import { cn } from '../../../lib/cn';

interface AuthShellProps {
  children: ReactNode;
  /** Max-width da coluna central. Default: max-w-[400px] */
  cardMaxWidth?: string;
}

/**
 * Auth Shell — layout reutilizável para todas as telas de autenticação.
 *
 * Estética Betterstack: coluna única centrada sobre canvas escuro, minimalista.
 * Sem painel lateral. Fundo com halo azul sutil + linha diagonal tênue.
 * Theme toggle discreto no canto superior direito.
 */
export function AuthShell({ children, cardMaxWidth = 'max-w-[400px]' }: AuthShellProps) {
  return (
    <div className="min-h-screen bg-canvas relative flex flex-col overflow-hidden">
      {/* Textura de fundo — halo azul sutil + linha diagonal tênue */}
      <div className="absolute inset-0 pointer-events-none" aria-hidden="true">
        <div className="absolute top-[-15%] left-1/2 -translate-x-1/2 w-[70%] h-[55%] rounded-full blur-[160px] bg-[radial-gradient(circle,rgba(59,130,246,0.06)_0%,transparent_70%)]" />
        <div className="absolute inset-0 opacity-[0.035] [background-image:linear-gradient(115deg,transparent_49.6%,rgba(255,255,255,1)_50%,transparent_50.4%)]" />
      </div>

      {/* Top bar — theme toggle */}
      <div className="relative z-10 flex items-center justify-end px-5 sm:px-8 py-5">
        <ThemeToggle />
      </div>

      {/* Conteúdo centrado */}
      <main id="main-content" className="relative z-10 flex-1 flex items-center justify-center px-6 pb-20">
        <div className={cn('w-full animate-fade-in', cardMaxWidth)}>
          {children}
        </div>
      </main>
    </div>
  );
}
