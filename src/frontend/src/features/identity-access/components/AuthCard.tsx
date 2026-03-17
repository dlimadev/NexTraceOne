import type { ReactNode } from 'react';
import { cn } from '../../../lib/cn';

interface AuthCardProps {
  children: ReactNode;
  className?: string;
}

/**
 * Auth Card — container visual para o formulário de autenticação.
 * DESIGN-SYSTEM.md §4.2: 420-460px, padding 40px, radius-lg, shadow-elevated.
 * Inclui accent stripe superior para identidade visual.
 */
export function AuthCard({ children, className }: AuthCardProps) {
  return (
    <div className={cn('bg-card rounded-lg shadow-elevated border border-edge p-10', className)}>
      <div className="h-0.5 brand-gradient rounded-pill mb-8" aria-hidden="true" />
      {children}
    </div>
  );
}
