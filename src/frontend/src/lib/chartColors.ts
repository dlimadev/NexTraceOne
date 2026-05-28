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

/**
 * Cores de chrome para tooltips e UI interna de gráficos.
 * Usar em vez de hex hardcoded em contentStyle / style de Recharts/ECharts.
 */
export const CHART_CHROME = {
  tooltipBg:      '#1f2937', // fundo de tooltip escuro
  tooltipBorder:  '#374151', // borda de tooltip / grid lines
  tooltipBorderDark: '#111827', // borda mais escura (cell borders no heatmap)
  tooltipText:    '#f9fafb', // texto primário no tooltip
  tooltipMuted:   '#9ca3af', // texto secundário / eixos
  tooltipFaded:   '#6b7280', // texto muito discreto (y-axis label)
  labelText:      '#d1d5db', // labels de nó em grafos
  itemBlue:       '#93c5fd', // item color azul claro (CHART_BLUE[4])
} as const;

/**
 * Gradiente de calor para heatmaps — do "vazio" ao "crítico".
 * Índice 0 = zero eventos, índice 4 = máximo (crítico).
 * Usar como: [CHART_HEAT[0], CHART_HEAT[1], CHART_HEAT[2], CHART_HEAT[3], CHART_SEMANTIC.critical]
 */
export const CHART_HEAT = [
  '#1f2937', // 0 — sem eventos (fundo escuro)
  '#fef9c3', // 1 — poucos eventos (amarelo muito claro)
  '#fde68a', // 2 — moderado (amarelo)
  '#f97316', // 3 — alto (laranja)
] as const;

/** Retorna a paleta certa dado um colorScheme string. */
export function getChartPalette(colorScheme?: string | null): readonly string[] {
  switch (colorScheme) {
    case 'rainbow': return CHART_RAINBOW;
    case 'blue':    return CHART_BLUE;
    case 'red':     return CHART_RED;
    default:        return CHART_SERIES;
  }
}
