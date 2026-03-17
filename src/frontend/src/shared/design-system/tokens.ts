/**
 * NexTraceOne Design System — Token Constants (TypeScript)
 *
 * Referências programáticas para os tokens CSS definidos em index.css.
 * Use quando valores precisam ser acessados em lógica JS/TS
 * (ex.: cálculos dinâmicos, animações imperativas, integração com bibliotecas).
 *
 * Os tokens CSS em index.css @theme são a fonte primária de verdade.
 * Este ficheiro espelha esses valores para uso em TypeScript.
 *
 * Fonte da verdade: docs/DESIGN-SYSTEM.md
 * Tokens CSS: src/index.css @theme block + :root
 */

// ── Z-Index Layers (DESIGN-SYSTEM.md §3.3) ─────────────────────────────────
export const zIndex = {
  base: 0,
  sticky: 20,
  header: 40,
  dropdown: 60,
  modal: 80,
  toast: 100,
} as const;

export type ZIndexLayer = keyof typeof zIndex;

// ── Motion Durations (DESIGN-SYSTEM.md §2.7) ───────────────────────────────
export const motion = {
  fast: '120ms',
  base: '180ms',
  medium: '240ms',
  slow: '320ms',
} as const;

export type MotionSpeed = keyof typeof motion;

// ── Easing Curves ───────────────────────────────────────────────────────────
export const easing = {
  standard: 'cubic-bezier(0.2, 0, 0, 1)',
  emphasis: 'cubic-bezier(0.2, 0.8, 0.2, 1)',
} as const;

// ── Breakpoints (DESIGN-SYSTEM.md §3.1) ─────────────────────────────────────
export const breakpoints = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  '2xl': 1440,
  '3xl': 1600,
} as const;

export type Breakpoint = keyof typeof breakpoints;

// ── Icon Sizes (DESIGN-SYSTEM.md §5) ────────────────────────────────────────
export const iconSize = {
  xs: 16,
  sm: 18,
  md: 20,
  lg: 24,
  xl: 32,
} as const;

export type IconSize = keyof typeof iconSize;

// ── Modal Sizes (DESIGN-SYSTEM.md §3.2) ─────────────────────────────────────
export const modalMaxWidth = {
  sm: '480px',
  md: '640px',
  lg: '840px',
  xl: '1120px',
} as const;

// ── Semantic Status ─────────────────────────────────────────────────────────
export const semanticStatuses = ['success', 'info', 'warning', 'danger', 'neutral'] as const;
export type SemanticStatus = (typeof semanticStatuses)[number];

// ── Component Size Scale ────────────────────────────────────────────────────
export const componentSizes = ['sm', 'md', 'lg'] as const;
export type ComponentSize = (typeof componentSizes)[number];
