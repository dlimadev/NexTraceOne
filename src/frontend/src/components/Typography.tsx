import { type HTMLAttributes, type LabelHTMLAttributes } from 'react';
import { cn } from '../lib/cn';

// ═══════════════════════════════════════════════════════════════════════════════
// NexTraceOne — Typography Components
//
// Componentes semânticos de tipografia baseados na escala definida em
// DESIGN-SYSTEM.md §2.3. Cada componente aplica automaticamente a classe
// de tipografia correspondente e aceita className externo para extensão.
//
// As classes .type-* são definidas em index.css e seguem a escala oficial.
// ═══════════════════════════════════════════════════════════════════════════════

// ── Heading ─────────────────────────────────────────────────────────────────

type HeadingLevel = 1 | 2 | 3 | 4 | 5 | 6;

interface HeadingProps extends HTMLAttributes<HTMLHeadingElement> {
  /** Nível semântico do heading (1-6). Determina o tag HTML e a escala. */
  level?: HeadingLevel;
}

const headingClasses: Record<HeadingLevel, string> = {
  1: 'type-heading-01 text-heading',
  2: 'type-heading-02 text-heading',
  3: 'type-title-01 text-heading',
  4: 'type-title-02 text-heading',
  5: 'type-body-lg font-semibold text-heading',
  6: 'type-body-md font-semibold text-heading',
};

/**
 * Heading semântico com escala automática por nível.
 *
 * h1 → heading-01 (32px/700)
 * h2 → heading-02 (24px/600)
 * h3 → title-01 (20px/600)
 * h4 → title-02 (18px/600)
 * h5 → body-lg semibold
 * h6 → body-md semibold
 */
export function Heading({ level = 2, className, children, ...rest }: HeadingProps) {
  const Tag = `h${level}` as const;
  return (
    <Tag className={cn(headingClasses[level], className)} {...rest}>
      {children}
    </Tag>
  );
}

// ── Text ────────────────────────────────────────────────────────────────────

type TextSize = 'lg' | 'md' | 'sm' | 'xs';

type TextTone =
  | 'heading'
  | 'body'
  | 'muted'
  | 'faded'
  | 'accent'
  | 'success'
  | 'warning'
  | 'danger'
  | 'info';

interface TextProps extends HTMLAttributes<HTMLElement> {
  /** Escala tipográfica: lg (18px), md (16px), sm (14px), xs/caption (12px). */
  size?: TextSize;
  /** Tom de cor semântico. */
  tone?: TextTone;
  /** Tag HTML a renderizar. */
  as?: 'p' | 'span' | 'div';
}

const textSizeClasses: Record<TextSize, string> = {
  lg: 'type-body-lg',
  md: 'type-body-md',
  sm: 'type-body-sm',
  xs: 'type-caption',
};

const textToneClasses: Record<TextTone, string> = {
  heading: 'text-heading',
  body: 'text-body',
  muted: 'text-muted',
  faded: 'text-faded',
  accent: 'text-accent',
  success: 'text-success',
  warning: 'text-warning',
  danger: 'text-critical',
  info: 'text-info',
};

/**
 * Texto base com escala e tom semântico.
 *
 * Uso: descrições, parágrafos, labels secundários e textos de apoio.
 */
export function Text({
  size = 'md',
  tone = 'body',
  as: Tag = 'p',
  className,
  children,
  ...rest
}: TextProps) {
  return (
    <Tag className={cn(textSizeClasses[size], textToneClasses[tone], className)} {...rest}>
      {children}
    </Tag>
  );
}

// ── Label ───────────────────────────────────────────────────────────────────

/**
 * Label de formulário com escala .type-label (14px/500).
 *
 * Compatível com htmlFor para associação acessível ao campo.
 */
export function Label({ className, children, ...rest }: LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label className={cn('type-label text-body', className)} {...rest}>
      {children}
    </label>
  );
}

// ── MonoText ────────────────────────────────────────────────────────────────

interface MonoTextProps extends HTMLAttributes<HTMLElement> {
  /** Tag HTML a renderizar. */
  as?: 'span' | 'code' | 'pre';
}

/**
 * Texto monospace para IDs técnicos, traces, eventos, versões e métricas.
 *
 * Usa JetBrains Mono / IBM Plex Mono conforme DESIGN-SYSTEM.md §2.3.
 */
export function MonoText({ as: Tag = 'code', className, children, ...rest }: MonoTextProps) {
  return (
    <Tag className={cn('type-mono-sm text-muted', className)} {...rest}>
      {children}
    </Tag>
  );
}
