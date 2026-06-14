import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '../lib/cn';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  /** Variante visual do card. */
  variant?: 'default' | 'interactive' | 'elevated' | 'flat' | 'glass' | 'gradient';
  /** Exibe skeleton de loading sobre o conteúdo. */
  loading?: boolean;
  className?: string;
}

export interface CardHeaderProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  /** Cor CSS do dot indicator (ex: 'var(--t-cyan)' ou '#1B7FE8'). Omitir para sem dot. */
  dot?: string;
  /** Anima o dot com pulse (requer dot). Para cards de incidentes/críticos. */
  pulsing?: boolean;
}

export interface CardTitleProps extends HTMLAttributes<HTMLHeadingElement> {
  children: ReactNode;
}

export interface CardDescriptionProps extends HTMLAttributes<HTMLParagraphElement> {
  children: ReactNode;
}

export interface CardContentProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

export interface CardFooterProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
}

const variantClasses: Record<NonNullable<CardProps['variant']>, string> = {
  default: 'bg-card rounded-xl border border-edge shadow-sm overflow-hidden',
  interactive:
    'bg-card rounded-xl border border-edge shadow-sm overflow-hidden cursor-pointer hover:border-edge-strong transition-all duration-[var(--nto-motion-base)]',
  elevated: 'bg-card rounded-xl border border-edge shadow-md overflow-hidden',
  flat: 'bg-card rounded-xl overflow-hidden',
  glass: 'backdrop-blur-xl bg-card/70 rounded-xl border border-edge shadow-sm overflow-hidden',
  gradient: 'rounded-xl border-0 shadow-md overflow-hidden text-white bg-gradient-to-br from-accent via-blue to-cyan',
};

/**
 * Card base — superfície elevada sobre canvas profundo navy.
 * DESIGN-SYSTEM.md §4.7: radius-xl (18px), borda soft translúcida, shadow-surface.
 *
 * Variantes:
 * - default: card padrão com borda e sombra surface
 * - interactive: hover com elevação e cursor pointer
 * - elevated: sombra mais forte permanente
 * - flat: sem borda/sombra
 * - glass: glassmorphism com backdrop-blur para overlays
 * - gradient: fundo gradiente com texto branco (inspirado pelo template)
 * - loading: exibe skeleton overlay
 */
export function Card({ children, variant = 'default', loading, className, ...rest }: CardProps) {
  return (
    <div className={cn(variantClasses[variant], className)} {...rest}>
      {loading ? (
        <div className="px-5 py-5 space-y-3">
          <div className="skeleton h-4 w-3/4 rounded-sm" />
          <div className="skeleton h-4 w-1/2 rounded-sm" />
          <div className="skeleton h-8 w-full rounded-sm" />
        </div>
      ) : (
        children
      )}
    </div>
  );
}

export function CardHeader({ children, dot, pulsing, className, ...rest }: CardHeaderProps) {
  return (
    <div className={cn('px-4 py-3 border-b border-edge/60 flex items-center gap-2.5', className)} {...rest}>
      {dot && (
        <span
          data-testid="card-header-dot"
          style={{
            width: 3,
            height: 20,
            borderRadius: 2,
            background: dot,
            flexShrink: 0,
            animation: pulsing ? 'pulse-badge 1.5s ease-in-out infinite' : undefined,
          }}
          aria-hidden="true"
        />
      )}
      {children}
    </div>
  );
}

export function CardBody({ children, className, ...rest }: HTMLAttributes<HTMLDivElement> & { children: ReactNode }) {
  return <div className={cn('px-4 py-4', className)} {...rest}>{children}</div>;
}

export function CardTitle({ children, className, ...rest }: CardTitleProps) {
  return (
    <h3 className={cn('text-lg font-semibold leading-none tracking-tight', className)} {...rest}>
      {children}
    </h3>
  );
}

export function CardDescription({ children, className, ...rest }: CardDescriptionProps) {
  return (
    <p className={cn('text-sm text-muted', className)} {...rest}>
      {children}
    </p>
  );
}

export function CardContent({ children, className, ...rest }: CardContentProps) {
  return (
    <div className={cn('px-5 py-4', className)} {...rest}>
      {children}
    </div>
  );
}

/**
 * CardFooter — rodapé semântico para legendas, ações e contexto.
 * Inspirado pelo padrão .card-footer do template NexLink.
 */
export function CardFooter({ children, className, ...rest }: CardFooterProps) {
  return (
    <div className={cn('px-4 py-2.5 border-t border-edge/60 bg-elevated/30', className)} {...rest}>
      {children}
    </div>
  );
}
