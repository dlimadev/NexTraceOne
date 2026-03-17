/**
 * NexTraceOne Design System — Foundation Patterns
 *
 * Mapas de classes Tailwind reutilizáveis para padrões visuais recorrentes.
 * Importe nos componentes para garantir alinhamento com o design system
 * sem repetir strings de classe em cada ficheiro.
 *
 * Todas as classes referem tokens definidos em index.css @theme.
 * Fonte da verdade: docs/DESIGN-SYSTEM.md
 */

// ── Focus Ring ──────────────────────────────────────────────────────────────
/** Anel de foco acessível padrão — usar em todo elemento interativo. */
export const focusRingClass =
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-offset-2 focus-visible:ring-offset-canvas';

// ── Surface Patterns ────────────────────────────────────────────────────────
/** Padrões de superfície para layering consistente do dark theme. */
export const surfaceClass = {
  canvas: 'bg-canvas',
  deep: 'bg-deep',
  panel: 'bg-panel border border-edge rounded-lg shadow-surface',
  card: 'bg-card border border-edge rounded-lg shadow-surface',
  elevated: 'bg-elevated border border-edge rounded-lg shadow-elevated',
  input: 'bg-input border border-edge rounded-lg',
  overlay: 'bg-overlay',
} as const;

export type SurfaceVariant = keyof typeof surfaceClass;

// ── Text Tone ───────────────────────────────────────────────────────────────
/** Classes de cor de texto por tom semântico. */
export const textToneClass = {
  heading: 'text-heading',
  body: 'text-body',
  muted: 'text-muted',
  faded: 'text-faded',
  accent: 'text-accent',
  success: 'text-success',
  warning: 'text-warning',
  danger: 'text-critical',
  info: 'text-info',
} as const;

export type TextTone = keyof typeof textToneClass;

// ── Semantic Badge Styles ───────────────────────────────────────────────────
/** Fundo + texto tonal para badges e status pills. */
export const semanticBadgeClass = {
  neutral: 'bg-elevated text-body',
  success: 'bg-success/15 text-success',
  info: 'bg-info/15 text-info',
  warning: 'bg-warning/15 text-warning',
  danger: 'bg-critical/15 text-critical',
} as const;

// ── Input Foundation ────────────────────────────────────────────────────────
/** Classes base para todos os campos de formulário. */
export const inputBaseClass =
  'w-full rounded-lg bg-input border text-heading placeholder:text-faded transition-colors focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed';

/** Borda padrão (sem erro). */
export const inputDefaultBorderClass =
  'border-edge hover:border-edge-strong focus:border-edge-focus focus:shadow-glow-cyan';

/** Borda de erro. */
export const inputErrorBorderClass =
  'border-critical/60 focus:border-critical focus:shadow-glow-danger';

// ── Interactive Surface ─────────────────────────────────────────────────────
/** Padrão para superfícies clicáveis (cards interativos, list items). */
export const interactiveSurfaceClass =
  'bg-elevated border border-edge rounded-lg hover:bg-hover hover:border-edge-strong transition-colors cursor-pointer';

// ── Transition Helpers ──────────────────────────────────────────────────────
/** Style objects para duração de transição via inline style. */
export const transitionBase = { transitionDuration: 'var(--nto-motion-base)' } as const;
export const transitionFast = { transitionDuration: 'var(--nto-motion-fast)' } as const;
