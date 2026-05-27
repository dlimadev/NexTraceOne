// src/frontend/src/lib/chartColors.ts

/**
 * Paleta de cores aprovada para gráficos ECharts / Recharts.
 * Usar sempre estas constantes em vez de hex hardcoded nos widgets.
 * Design-Audit §1.1 — Chart Colors Exception.
 */

/** Série de cores para dados multi-série (compatível com ECharts e Recharts). */
export const CHART_SERIES = [
  '#1B7FE8', // brand blue  (≈ accent)
  '#0891B2', // cyan        (≈ t-cyan)
  '#059669', // emerald     (≈ t-success)
  '#D97706', // amber       (≈ t-warning)
  '#DC2626', // red         (≈ t-critical)
  '#7C3AED', // violet      (extra)
  '#EA580C', // orange      (extra)
  '#0369A1', // sky         (extra)
] as const;

/** Paleta rainbow — para distribuições circulares (pie/donut). */
export const CHART_RAINBOW = [
  '#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4',
] as const;

/** Paleta blue — para barras de comparação em tons azuis. */
export const CHART_BLUE = [
  '#1d4ed8', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd', '#bfdbfe',
] as const;

/** Paleta red — para métricas de erro/degradação. */
export const CHART_RED = [
  '#991b1b', '#b91c1c', '#dc2626', '#ef4444', '#f87171', '#fca5a5',
] as const;

/** Cores semânticas — para eixos, thresholds, indicadores. */
export const CHART_SEMANTIC = {
  success:  '#059669', // var(--t-success)
  warning:  '#D97706', // var(--t-warning)
  critical: '#DC2626', // var(--t-critical)
  info:     '#0891B2', // var(--t-cyan)
  accent:   '#1B7FE8', // var(--t-accent)
  muted:    '#64748b', // var(--t-muted)
  axis:     '#94a3b8', // var(--t-faded) — eixos e gridlines
  grid:     'rgba(148,163,184,0.15)', // gridline muito subtil
} as const;

/** Retorna a paleta certa dado um colorScheme string. */
export function getChartPalette(colorScheme?: string | null): readonly string[] {
  switch (colorScheme) {
    case 'rainbow': return CHART_RAINBOW;
    case 'blue':    return CHART_BLUE;
    case 'red':     return CHART_RED;
    default:        return CHART_SERIES;
  }
}
