import type { ReactNode } from 'react';
import { cn } from '../../../lib/cn';

interface AuthCardProps {
  children: ReactNode;
  className?: string;
}

/**
 * Auth Card — container visual para o formulário de autenticação.
 * DESIGN-SYSTEM.md §4.2: 420-460px, padding 40px, radius-lg, shadow-elevated.
 *
 * Stripe de gradiente no topo para identidade visual da marca.
 * Borda sutil com leve glow azul para distinguir o card do fundo.
 */
export function AuthCard({ children, className }: AuthCardProps) {
  return (
    <div
      className={cn(
        'relative bg-card rounded-xl shadow-elevated border border-edge overflow-hidden',
        'p-8 sm:p-10',
        'shadow-[var(--t-shadow-xl)]',
        className,
      )}
    >
      {/* Accent stripe superior — gradiente da marca */}
      <div className="absolute inset-x-0 top-0 h-0.5 brand-gradient" aria-hidden="true" />

      {/* Subtle top glow */}
      <div
        className="absolute inset-x-0 top-0 h-24 pointer-events-none bg-[linear-gradient(180deg,rgba(27,127,232,0.04)_0%,transparent_100%)]"
        aria-hidden="true"
      />

      <div className="relative">
        {children}
      </div>
    </div>
  );
}
