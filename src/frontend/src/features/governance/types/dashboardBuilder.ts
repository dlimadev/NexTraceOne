// src/frontend/src/features/governance/types/dashboardBuilder.ts

/** Template variable shown in DashboardVariablesBar */
export interface DashboardVariable {
  name: string;          // e.g. "service" → referenced as $service
  label: string;         // display label, e.g. "Serviço"
  type: 'query' | 'custom' | 'text' | 'interval';
  options: string[];     // available values
  value: string | string[];  // current selection (string[] when multi=true)
  multi: boolean;        // allow multiple selections
  includeAll: boolean;   // adds "Todas" option
}

/** One query row inside VisualQueryBuilder */
export interface VisualQueryRow {
  queryId: string;       // "A", "B", "C"...
  mode: 'visual' | 'nql';
  // Visual mode fields
  serviceId: string;     // supports $variable references
  metric: string;
  filters: Array<{ key: string; op: string; value: string }>;
  groupBy: string;
  fn: string;            // e.g. "rate()", "sum()", "avg()"
  aggFn: string;         // e.g. "sum by", "avg by"
  // NQL mode
  nqlText: string;
}

/** Visualization type */
export type VizType =
  | 'timeseries' | 'bar' | 'stat' | 'gauge'
  | 'donut' | 'heatmap' | 'table' | 'state-timeline'
  | 'histogram' | 'scatter' | 'candlestick';

export interface VizTypeMeta {
  id: VizType;
  label: string;
  /** Inline SVG content for the mini thumbnail icon */
  svgContent: string;
}

/** Replace $varName with the variable's current value */
export function interpolateVariables(
  text: string,
  variables: DashboardVariable[]
): string {
  return variables.reduce((acc, v) => {
    const val = Array.isArray(v.value) ? v.value.join(',') : v.value;
    return acc.replaceAll(`$${v.name}`, val);
  }, text);
}

/** Default empty visual query row */
export function makeVisualQueryRow(queryId: string): VisualQueryRow {
  return {
    queryId,
    mode: 'visual',
    serviceId: '',
    metric: '',
    filters: [],
    groupBy: '',
    fn: 'rate()',
    aggFn: 'sum by',
    nqlText: '',
  };
}

/** Compile visual fields to NQL string (one-way, best-effort) */
export function compileToNql(row: VisualQueryRow): string {
  const filterStr = row.filters
    .map((f) => `${f.key}${f.op}"${f.value}"`)
    .join(', ');
  const metricPart = row.metric
    ? `${row.fn.replace('()', '')}(${row.metric}{service="${row.serviceId}"${filterStr ? ', ' + filterStr : ''}})`
    : '';
  const groupPart = row.groupBy ? ` | ${row.aggFn} (${row.groupBy})` : '';
  return metricPart ? `${metricPart}${groupPart}` : '';
}

export const VIZ_TYPE_META: VizTypeMeta[] = [
  {
    id: 'timeseries',
    label: 'Série Temporal',
    svgContent: `<polyline points="2,14 5,9 8,11 11,5 14,7 17,3 20,6" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>`,
  },
  {
    id: 'bar',
    label: 'Barras',
    svgContent: `<rect x="3" y="10" width="3" height="6" rx="0.5" fill="currentColor" opacity="0.7"/><rect x="8" y="6" width="3" height="10" rx="0.5" fill="currentColor"/><rect x="13" y="8" width="3" height="8" rx="0.5" fill="currentColor" opacity="0.85"/>`,
  },
  {
    id: 'stat',
    label: 'Stat',
    svgContent: `<text x="11" y="14" text-anchor="middle" font-size="8" font-weight="700" fill="currentColor">42</text><line x1="4" y1="17" x2="18" y2="17" stroke="currentColor" stroke-width="1" opacity="0.4"/>`,
  },
  {
    id: 'gauge',
    label: 'Gauge',
    svgContent: `<path d="M4 16 A8 8 0 0 1 18 16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" opacity="0.3"/><path d="M4 16 A8 8 0 0 1 14 9" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/><line x1="11" y1="16" x2="14" y2="9" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>`,
  },
  {
    id: 'donut',
    label: 'Donut',
    svgContent: `<circle cx="11" cy="11" r="7" fill="none" stroke="currentColor" stroke-width="3" stroke-dasharray="22 22" stroke-dashoffset="0"/><circle cx="11" cy="11" r="7" fill="none" stroke="currentColor" stroke-width="3" stroke-dasharray="14 30" stroke-dashoffset="-22" opacity="0.5"/><circle cx="11" cy="11" r="4" fill="#0d1117"/>`,
  },
  {
    id: 'heatmap',
    label: 'Heatmap',
    svgContent: `<rect x="2" y="2" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.2"/><rect x="8" y="2" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.6"/><rect x="14" y="2" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.9"/><rect x="2" y="8" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.5"/><rect x="8" y="8" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.3"/><rect x="14" y="8" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.7"/><rect x="2" y="14" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.8"/><rect x="8" y="14" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.4"/><rect x="14" y="14" width="4" height="4" rx="0.5" fill="currentColor" opacity="0.95"/>`,
  },
  {
    id: 'table',
    label: 'Tabela',
    svgContent: `<rect x="2" y="3" width="18" height="3" rx="0.5" fill="currentColor" opacity="0.8"/><rect x="2" y="8" width="18" height="2" rx="0.5" fill="currentColor" opacity="0.3"/><rect x="2" y="12" width="18" height="2" rx="0.5" fill="currentColor" opacity="0.3"/><rect x="2" y="16" width="18" height="2" rx="0.5" fill="currentColor" opacity="0.3"/><line x1="8" y1="3" x2="8" y2="18" stroke="#0d1117" stroke-width="0.5"/><line x1="14" y1="3" x2="14" y2="18" stroke="#0d1117" stroke-width="0.5"/>`,
  },
  {
    id: 'state-timeline',
    label: 'State Timeline',
    svgContent: `<rect x="2" y="5" width="5" height="4" rx="0.5" fill="currentColor"/><rect x="9" y="5" width="3" height="4" rx="0.5" fill="currentColor" opacity="0.4"/><rect x="14" y="5" width="6" height="4" rx="0.5" fill="currentColor" opacity="0.7"/><rect x="2" y="12" width="8" height="4" rx="0.5" fill="currentColor" opacity="0.5"/><rect x="12" y="12" width="8" height="4" rx="0.5" fill="currentColor" opacity="0.9"/>`,
  },
  {
    id: 'histogram',
    label: 'Histograma',
    svgContent: `<rect x="2" y="15" width="3" height="2" rx="0.5" fill="currentColor" opacity="0.5"/><rect x="6" y="11" width="3" height="6" rx="0.5" fill="currentColor" opacity="0.7"/><rect x="10" y="6" width="3" height="11" rx="0.5" fill="currentColor"/><rect x="14" y="9" width="3" height="8" rx="0.5" fill="currentColor" opacity="0.8"/><rect x="18" y="13" width="2" height="4" rx="0.5" fill="currentColor" opacity="0.4"/>`,
  },
  {
    id: 'scatter',
    label: 'Scatter',
    svgContent: `<circle cx="5" cy="15" r="1.5" fill="currentColor"/><circle cx="9" cy="10" r="1.5" fill="currentColor" opacity="0.7"/><circle cx="13" cy="7" r="1.5" fill="currentColor"/><circle cx="7" cy="13" r="1.5" fill="currentColor" opacity="0.5"/><circle cx="16" cy="5" r="1.5" fill="currentColor" opacity="0.9"/><circle cx="11" cy="12" r="1.5" fill="currentColor" opacity="0.6"/>`,
  },
  {
    id: 'candlestick',
    label: 'Candlestick',
    svgContent: `<line x1="5" y1="4" x2="5" y2="18" stroke="currentColor" stroke-width="1"/><rect x="3" y="8" width="4" height="6" rx="0.5" fill="currentColor" opacity="0.8"/><line x1="11" y1="6" x2="11" y2="18" stroke="currentColor" stroke-width="1" opacity="0.5"/><rect x="9" y="10" width="4" height="5" rx="0.5" fill="currentColor" opacity="0.4"/><line x1="17" y1="3" x2="17" y2="16" stroke="currentColor" stroke-width="1"/><rect x="15" y="7" width="4" height="7" rx="0.5" fill="currentColor" opacity="0.9"/>`,
  },
];
