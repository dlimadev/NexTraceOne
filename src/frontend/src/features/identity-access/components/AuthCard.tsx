import type { ReactNode } from 'react';
import { cn } from '../../../lib/cn';

interface AuthCardProps {
  children: ReactNode;
  className?: string;
}

/**
 * Auth Card — container visual para o formulário de autenticação.
 * Estética Betterstack: ~440px, padding generoso, radius-xl, borda hairline,
 * sombra plana. Stripe fino da marca no topo como toque de identidade.
 */
export function AuthCard({ children, className }: AuthCardProps) {
  return (
    <div
      className={cn(
        'relative bg-card rounded-xl border border-edge overflow-hidden',
        'p-8 sm:p-10',
        'shadow-[var(--t-shadow-lg)]',
        className,
      )}
    >
      {/* Accent stripe superior — gradiente da marca */}
      <div className="absolute inset-x-0 top-0 h-0.5 brand-gradient" aria-hidden="true" />

      <div className="relative">
        {children}
      </div>
    </div>
  );
}
