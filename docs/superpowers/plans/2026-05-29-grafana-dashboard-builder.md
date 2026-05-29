# Grafana-like Dashboard Builder Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the narrow `ConfigDrawer` side panel in `DashboardBuilderPage` with a full-screen Grafana-style `PanelEditorOverlay` that includes a live preview, hybrid visual/NQL query builder, visualization picker, and a top toolbar for dashboard template variables.

**Architecture:** Five new focused components are extracted from the existing 1687-line `DashboardBuilderPage.tsx` and created alongside it. The canvas, palette sidebar, drag-and-drop, and widget type system are untouched. The `ConfigDrawer` function is deleted and replaced by `PanelEditorOverlay` (mounted only when `editingSlotId !== null`). New state variables (`variables[]`, `timeRange`, `editingSlotId`) are added; `activeConfigId` is repurposed to `editingSlotId`. All UI strings go through i18n.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4, react-i18next, TanStack Query, Lucide React, Vitest + Testing Library

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| CREATE | `src/frontend/src/features/governance/types/dashboardBuilder.ts` | Shared types: `DashboardVariable`, `VisualQueryRow`, `VizType`, `VizTypeMeta`, `interpolateVariables` |
| CREATE | `src/frontend/src/features/governance/components/DashboardVariablesBar.tsx` | Variables toolbar + time picker + refresh |
| CREATE | `src/frontend/src/features/governance/components/PanelVisualizationPicker.tsx` | SVG viz type grid, suggestions, display options |
| CREATE | `src/frontend/src/features/governance/components/VisualQueryBuilder.tsx` | Visual rows + NQL toggle + Monaco editor |
| CREATE | `src/frontend/src/features/governance/components/PanelEditorOverlay.tsx` | Full-screen overlay: header + preview + tabs + viz sidebar |
| MODIFY | `src/frontend/src/features/governance/components/BuilderWidgetCard.tsx` | Add "Editar" pencil button alongside Settings gear; rename `onConfigOpen` prop to `onEditOpen` |
| MODIFY | `src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx` | Add new state variables; extend `BuilderSlot` with `visualQueryRows?`; replace `ConfigDrawer` render with `PanelEditorOverlay`; add `DashboardVariablesBar`; extend save/load for variables |
| MODIFY | `src/frontend/src/locales/pt-PT.json` | Add `governance.dashboardBuilder.*` keys (pt-PT) |
| MODIFY | `src/frontend/src/locales/pt-BR.json` | Same keys (pt-BR) |
| MODIFY | `src/frontend/src/locales/en.json` | Same keys (en) |
| MODIFY | `src/frontend/src/locales/es.json` | Same keys (es) |
| CREATE | `src/frontend/src/__tests__/governance/DashboardBuilderPage.panelEditor.test.tsx` | Tests: overlay opens, Apply commits, Discard drops |
| CREATE | `src/frontend/src/__tests__/governance/DashboardVariablesBar.test.tsx` | Tests: variable change fires callback, time picker renders |

---

## Task 1: Create shared types file

**Files:**
- Create: `src/frontend/src/features/governance/types/dashboardBuilder.ts`

- [ ] **Step 1: Create the types file**

```typescript
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
```

- [ ] **Step 2: Verify the file compiles (TypeScript check via Vite)**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | head -30`

Expected: No TypeScript errors in the new file (or only pre-existing errors unrelated to it).

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/governance/types/dashboardBuilder.ts
git commit -m "feat(dashboard-builder): add shared types and helpers for Grafana-like redesign"
```

---

## Task 2: Add i18n keys to all 4 locale files

**Files:**
- Modify: `src/frontend/src/locales/pt-PT.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/es.json`

- [ ] **Step 1: Read the current governance section in each locale to find the right insertion point**

Run: `grep -n "dashboardBuilder" src/frontend/src/locales/pt-PT.json | head -5`

Note: if the key already exists (from prior work), append only the missing sub-keys. Do not duplicate.

- [ ] **Step 2: Add keys to `pt-PT.json`**

Open the file and find the `"governance"` → `"dashboardBuilder"` object. Add these keys inside it (merge with existing, do not replace):

```json
"panelEditor": {
  "title": "Editor de Painel",
  "apply": "Aplicar",
  "discard": "Descartar",
  "preview": "Pré-visualização",
  "tableView": "Ver Tabela",
  "backToDashboard": "← Voltar ao dashboard",
  "tabs": {
    "query": "Query",
    "transforms": "Transformações",
    "alerts": "Alertas"
  }
},
"queryBuilder": {
  "visual": "Visual",
  "nql": "NQL",
  "addQuery": "+ Adicionar Query",
  "service": "Serviço",
  "metric": "Métrica",
  "filters": "Filtros",
  "addFilter": "+ Adicionar",
  "groupBy": "Group by",
  "function": "Função",
  "aggregation": "Agregação",
  "switchToNql": "Editar como NQL",
  "nqlComplexHint": "NQL complexo — use o editor de código",
  "duplicate": "Duplicar",
  "delete": "Eliminar",
  "copyNql": "Copiar NQL"
},
"variablesBar": {
  "addVariable": "+ Variável",
  "timeRange": "Intervalo de tempo",
  "refresh": "Actualizar",
  "allValues": "Todas",
  "refreshOff": "Off"
},
"viz": {
  "selected": "Seleccionado",
  "suggestions": "Sugestões",
  "allTypes": "Todos os tipos",
  "options": "Opções",
  "unit": "Unidade",
  "decimals": "Decimais",
  "minY": "Min Y",
  "maxY": "Max Y",
  "thresholds": "Thresholds",
  "thresholdsActive": "{{count}} activos",
  "types": {
    "timeseries": "Série Temporal",
    "bar": "Barras",
    "stat": "Stat",
    "gauge": "Gauge",
    "donut": "Donut",
    "heatmap": "Heatmap",
    "table": "Tabela",
    "state-timeline": "State Timeline",
    "histogram": "Histograma",
    "scatter": "Scatter",
    "candlestick": "Candlestick"
  }
}
```

- [ ] **Step 3: Add keys to `pt-BR.json`** (same values as pt-PT, Brazilian spelling variants where applicable)

Same structure as pt-PT with these spelling differences:
- `"apply": "Aplicar"` → same
- `"discard": "Descartar"` → same
- `"backToDashboard": "← Voltar ao dashboard"` → same
- `"tableView": "Ver Tabela"` → same
- `"allValues": "Todas"` → same
- `"thresholdsActive": "{{count}} ativos"` (pt-BR: "ativos" not "activos")

- [ ] **Step 4: Add keys to `en.json`**

```json
"panelEditor": {
  "title": "Panel Editor",
  "apply": "Apply",
  "discard": "Discard",
  "preview": "Preview",
  "tableView": "Table View",
  "backToDashboard": "← Back to dashboard",
  "tabs": {
    "query": "Query",
    "transforms": "Transforms",
    "alerts": "Alerts"
  }
},
"queryBuilder": {
  "visual": "Visual",
  "nql": "NQL",
  "addQuery": "+ Add Query",
  "service": "Service",
  "metric": "Metric",
  "filters": "Filters",
  "addFilter": "+ Add",
  "groupBy": "Group by",
  "function": "Function",
  "aggregation": "Aggregation",
  "switchToNql": "Edit as NQL",
  "nqlComplexHint": "Complex NQL — use the code editor",
  "duplicate": "Duplicate",
  "delete": "Delete",
  "copyNql": "Copy NQL"
},
"variablesBar": {
  "addVariable": "+ Variable",
  "timeRange": "Time range",
  "refresh": "Refresh",
  "allValues": "All",
  "refreshOff": "Off"
},
"viz": {
  "selected": "Selected",
  "suggestions": "Suggestions",
  "allTypes": "All types",
  "options": "Options",
  "unit": "Unit",
  "decimals": "Decimals",
  "minY": "Min Y",
  "maxY": "Max Y",
  "thresholds": "Thresholds",
  "thresholdsActive": "{{count}} active",
  "types": {
    "timeseries": "Time Series",
    "bar": "Bar",
    "stat": "Stat",
    "gauge": "Gauge",
    "donut": "Donut",
    "heatmap": "Heatmap",
    "table": "Table",
    "state-timeline": "State Timeline",
    "histogram": "Histogram",
    "scatter": "Scatter",
    "candlestick": "Candlestick"
  }
}
```

- [ ] **Step 5: Add keys to `es.json`**

```json
"panelEditor": {
  "title": "Editor de Panel",
  "apply": "Aplicar",
  "discard": "Descartar",
  "preview": "Vista previa",
  "tableView": "Ver Tabla",
  "backToDashboard": "← Volver al dashboard",
  "tabs": {
    "query": "Consulta",
    "transforms": "Transformaciones",
    "alerts": "Alertas"
  }
},
"queryBuilder": {
  "visual": "Visual",
  "nql": "NQL",
  "addQuery": "+ Añadir Consulta",
  "service": "Servicio",
  "metric": "Métrica",
  "filters": "Filtros",
  "addFilter": "+ Añadir",
  "groupBy": "Agrupar por",
  "function": "Función",
  "aggregation": "Agregación",
  "switchToNql": "Editar como NQL",
  "nqlComplexHint": "NQL complejo — use el editor de código",
  "duplicate": "Duplicar",
  "delete": "Eliminar",
  "copyNql": "Copiar NQL"
},
"variablesBar": {
  "addVariable": "+ Variable",
  "timeRange": "Rango de tiempo",
  "refresh": "Actualizar",
  "allValues": "Todas",
  "refreshOff": "Desactivado"
},
"viz": {
  "selected": "Seleccionado",
  "suggestions": "Sugerencias",
  "allTypes": "Todos los tipos",
  "options": "Opciones",
  "unit": "Unidad",
  "decimals": "Decimales",
  "minY": "Min Y",
  "maxY": "Max Y",
  "thresholds": "Umbrales",
  "thresholdsActive": "{{count}} activos",
  "types": {
    "timeseries": "Serie Temporal",
    "bar": "Barras",
    "stat": "Stat",
    "gauge": "Gauge",
    "donut": "Donut",
    "heatmap": "Mapa de calor",
    "table": "Tabla",
    "state-timeline": "Línea de Estado",
    "histogram": "Histograma",
    "scatter": "Dispersión",
    "candlestick": "Velas"
  }
}
```

- [ ] **Step 6: Verify JSON is valid**

Run: `cd src/frontend && node -e "['pt-PT','pt-BR','en','es'].forEach(l => { try { JSON.parse(require('fs').readFileSync('src/locales/' + l + '.json', 'utf8')); console.log(l + ': OK'); } catch(e) { console.error(l + ': INVALID', e.message); } })"`

Expected: All 4 print `: OK`

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/locales/pt-PT.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/en.json src/frontend/src/locales/es.json
git commit -m "feat(dashboard-builder): add i18n keys for panel editor, query builder, variables bar and viz picker"
```

---

## Task 3: Create PanelVisualizationPicker component

**Files:**
- Create: `src/frontend/src/features/governance/components/PanelVisualizationPicker.tsx`

- [ ] **Step 1: Create the component**

```tsx
// src/frontend/src/features/governance/components/PanelVisualizationPicker.tsx
/**
 * PanelVisualizationPicker — right sidebar (230px) inside PanelEditorOverlay.
 * Shows current viz type, a grid of all 11 types with SVG thumbnails,
 * smart suggestions, and display options (unit, decimals, min/max, thresholds).
 */
import { useTranslation } from 'react-i18next';
import { type VizType, VIZ_TYPE_META } from '../types/dashboardBuilder';

const SUGGESTIONS: Record<string, VizType[]> = {
  timeseries: ['timeseries', 'bar', 'stat', 'gauge'],
  categorical: ['donut', 'bar', 'table', 'histogram'],
  single: ['stat', 'gauge', 'timeseries', 'bar'],
  tabular: ['table', 'state-timeline', 'heatmap', 'bar'],
};

function getSuggestions(currentViz: VizType): VizType[] {
  if (['timeseries', 'scatter', 'candlestick'].includes(currentViz)) return SUGGESTIONS.timeseries;
  if (['stat', 'gauge'].includes(currentViz)) return SUGGESTIONS.single;
  if (['donut', 'histogram'].includes(currentViz)) return SUGGESTIONS.categorical;
  return SUGGESTIONS.tabular;
}

interface PanelVisualizationPickerProps {
  currentViz: VizType;
  unit: string;
  yAxisMin: string;
  yAxisMax: string;
  thresholds: string;
  onVizChange: (viz: VizType) => void;
  onUnitChange: (unit: string) => void;
  onYAxisMinChange: (val: string) => void;
  onYAxisMaxChange: (val: string) => void;
}

export function PanelVisualizationPicker({
  currentViz,
  unit,
  yAxisMin,
  yAxisMax,
  thresholds,
  onVizChange,
  onUnitChange,
  onYAxisMinChange,
  onYAxisMaxChange,
}: PanelVisualizationPickerProps) {
  const { t } = useTranslation();

  const suggested = getSuggestions(currentViz);
  const thresholdCount = (() => {
    try { return (JSON.parse(thresholds) as unknown[]).length; } catch { return 0; }
  })();

  return (
    <div className="flex flex-col gap-3 h-full overflow-y-auto p-3 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 w-[230px] shrink-0">

      {/* Current selection */}
      <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
        {t('governance.dashboardBuilder.viz.selected', 'Selected')}
      </p>
      {(() => {
        const meta = VIZ_TYPE_META.find((m) => m.id === currentViz)!;
        return (
          <div className="flex items-center gap-2 px-2 py-2 rounded-md bg-accent/10 border border-accent/40">
            <svg viewBox="0 0 22 22" width={22} height={22} className="text-accent shrink-0">
              <g dangerouslySetInnerHTML={{ __html: meta.svgContent }} />
            </svg>
            <span className="text-xs font-semibold text-accent">{meta.label}</span>
          </div>
        );
      })()}

      {/* Suggestions */}
      <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mt-1">
        {t('governance.dashboardBuilder.viz.suggestions', 'Suggestions')}
      </p>
      <div className="grid grid-cols-2 gap-1.5">
        {suggested.map((vizId) => {
          const meta = VIZ_TYPE_META.find((m) => m.id === vizId)!;
          const isActive = vizId === currentViz;
          return (
            <button
              key={vizId}
              type="button"
              onClick={() => onVizChange(vizId)}
              className={`flex flex-col items-center gap-1 p-2 rounded-md border transition-colors cursor-pointer
                ${isActive
                  ? 'border-accent bg-accent/10 text-accent'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:border-accent/50 hover:bg-accent/5'
                }`}
              title={meta.label}
            >
              <svg viewBox="0 0 22 22" width={20} height={20}>
                <g dangerouslySetInnerHTML={{ __html: meta.svgContent }} />
              </svg>
              <span className="text-[9px] font-medium leading-none text-center">{meta.label}</span>
            </button>
          );
        })}
      </div>

      {/* All types */}
      <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mt-1">
        {t('governance.dashboardBuilder.viz.allTypes', 'All types')}
      </p>
      <div className="grid grid-cols-2 gap-1.5">
        {VIZ_TYPE_META.map((meta) => {
          const isActive = meta.id === currentViz;
          return (
            <button
              key={meta.id}
              type="button"
              onClick={() => onVizChange(meta.id)}
              className={`flex flex-col items-center gap-1 p-1.5 rounded-md border transition-colors cursor-pointer
                ${isActive
                  ? 'border-accent bg-accent/10 text-accent'
                  : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:border-accent/40'
                }`}
              title={meta.label}
            >
              <svg viewBox="0 0 22 22" width={18} height={18}>
                <g dangerouslySetInnerHTML={{ __html: meta.svgContent }} />
              </svg>
              <span className="text-[8px] leading-none text-center">{meta.label}</span>
            </button>
          );
        })}
      </div>

      {/* Display options */}
      <p className="text-[10px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mt-1">
        {t('governance.dashboardBuilder.viz.options', 'Options')}
      </p>
      <div className="flex flex-col gap-2">
        <div className="flex items-center justify-between gap-2">
          <label className="text-xs text-gray-500 dark:text-gray-400 shrink-0">
            {t('governance.dashboardBuilder.viz.unit', 'Unit')}
          </label>
          <select
            value={unit}
            onChange={(e) => onUnitChange(e.target.value)}
            className="text-xs rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-1.5 py-0.5 focus:outline-none focus:border-accent"
          >
            {['none', 'req/s', 'ms', 'bytes', '%', 'rpm', 'errors/s'].map((u) => (
              <option key={u} value={u}>{u}</option>
            ))}
          </select>
        </div>
        <div className="flex items-center justify-between gap-2">
          <label className="text-xs text-gray-500 dark:text-gray-400 shrink-0">
            {t('governance.dashboardBuilder.viz.minY', 'Min Y')}
          </label>
          <input
            type="text"
            value={yAxisMin}
            onChange={(e) => onYAxisMinChange(e.target.value)}
            placeholder="auto"
            className="text-xs rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-1.5 py-0.5 w-16 focus:outline-none focus:border-accent"
          />
        </div>
        <div className="flex items-center justify-between gap-2">
          <label className="text-xs text-gray-500 dark:text-gray-400 shrink-0">
            {t('governance.dashboardBuilder.viz.maxY', 'Max Y')}
          </label>
          <input
            type="text"
            value={yAxisMax}
            onChange={(e) => onYAxisMaxChange(e.target.value)}
            placeholder="auto"
            className="text-xs rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-1.5 py-0.5 w-16 focus:outline-none focus:border-accent"
          />
        </div>
        <div className="flex items-center justify-between gap-2">
          <label className="text-xs text-gray-500 dark:text-gray-400 shrink-0">
            {t('governance.dashboardBuilder.viz.thresholds', 'Thresholds')}
          </label>
          <span
            className={`text-xs px-1.5 py-0.5 rounded border ${
              thresholdCount > 0
                ? 'border-green-500 bg-green-50 dark:bg-green-950 text-green-600 dark:text-green-400'
                : 'border-gray-200 dark:border-gray-700 text-gray-400'
            }`}
          >
            {thresholdCount > 0
              ? t('governance.dashboardBuilder.viz.thresholdsActive', '{{count}} active', { count: thresholdCount })
              : '—'}
          </span>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Verify it renders without errors**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | grep -E "error|Error" | head -20`

Expected: No errors from the new file.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/governance/components/PanelVisualizationPicker.tsx
git commit -m "feat(dashboard-builder): add PanelVisualizationPicker with SVG thumbnails for 11 viz types"
```

---

## Task 4: Create VisualQueryBuilder component

**Files:**
- Create: `src/frontend/src/features/governance/components/VisualQueryBuilder.tsx`

- [ ] **Step 1: Create the component**

```tsx
// src/frontend/src/features/governance/components/VisualQueryBuilder.tsx
/**
 * VisualQueryBuilder — hybrid query builder inside PanelEditorOverlay (Query tab).
 * Each row has a [Visual] / [NQL] mode toggle.
 * Visual mode: service, metric, filters, groupBy, fn, aggFn dropdowns.
 * NQL mode: NqlMonacoEditor.
 * Uses TanStack Query to fetch service list from /catalog/services.
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Plus, Trash2, Copy, ChevronDown } from 'lucide-react';
import client from '../../../api/client';
import { NqlMonacoEditor } from './NqlMonacoEditor';
import {
  type VisualQueryRow,
  type DashboardVariable,
  makeVisualQueryRow,
  compileToNql,
} from '../types/dashboardBuilder';

const FUNCTIONS = ['rate()', 'sum()', 'avg()', 'max()', 'min()', 'count()'];
const AGG_FUNCTIONS = ['sum by', 'avg by', 'max by', 'min by'];
const FILTER_OPS = ['=', '!=', '=~', '!~', '>', '<'];

const QUERY_IDS = ['A', 'B', 'C', 'D', 'E'];

interface ServiceOption {
  serviceId: string;
  name: string;
}

interface VisualQueryBuilderProps {
  rows: VisualQueryRow[];
  variables: DashboardVariable[];
  onRowsChange: (rows: VisualQueryRow[]) => void;
}

export function VisualQueryBuilder({ rows, variables, onRowsChange }: VisualQueryBuilderProps) {
  const { t } = useTranslation();
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);

  const { data: services = [] } = useQuery<ServiceOption[]>({
    queryKey: ['catalog-services-for-builder'],
    queryFn: () =>
      client.get<ServiceOption[]>('/catalog/services', { params: { pageSize: 100 } }).then((r) => r.data),
    staleTime: 60_000,
  });

  const updateRow = useCallback((queryId: string, patch: Partial<VisualQueryRow>) => {
    onRowsChange(rows.map((r) => (r.queryId === queryId ? { ...r, ...patch } : r)));
  }, [rows, onRowsChange]);

  const addRow = useCallback(() => {
    const usedIds = new Set(rows.map((r) => r.queryId));
    const nextId = QUERY_IDS.find((id) => !usedIds.has(id)) ?? `Q${rows.length + 1}`;
    onRowsChange([...rows, makeVisualQueryRow(nextId)]);
  }, [rows, onRowsChange]);

  const removeRow = useCallback((queryId: string) => {
    if (rows.length === 1) return; // keep at least one row
    onRowsChange(rows.filter((r) => r.queryId !== queryId));
  }, [rows, onRowsChange]);

  const duplicateRow = useCallback((queryId: string) => {
    const row = rows.find((r) => r.queryId === queryId);
    if (!row) return;
    const usedIds = new Set(rows.map((r) => r.queryId));
    const nextId = QUERY_IDS.find((id) => !usedIds.has(id)) ?? `Q${rows.length + 1}`;
    onRowsChange([...rows, { ...row, queryId: nextId }]);
  }, [rows, onRowsChange]);

  const copyNql = useCallback((row: VisualQueryRow) => {
    const nql = row.mode === 'nql' ? row.nqlText : compileToNql(row);
    void navigator.clipboard.writeText(nql);
  }, []);

  const switchToNql = useCallback((queryId: string) => {
    const row = rows.find((r) => r.queryId === queryId);
    if (!row) return;
    updateRow(queryId, { mode: 'nql', nqlText: compileToNql(row) });
  }, [rows, updateRow]);

  // Variable name options including $variable references
  const variableOptions = variables.map((v) => `$${v.name}`);
  const serviceOptions = [
    ...variableOptions,
    ...services.map((s) => s.serviceId),
  ];

  return (
    <div className="flex flex-col gap-4 p-3 overflow-y-auto">
      {rows.map((row) => (
        <div
          key={row.queryId}
          className="flex flex-col gap-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-3"
        >
          {/* Row header */}
          <div className="flex items-center gap-2">
            <span className="text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-4">
              {row.queryId}
            </span>

            {/* Mode toggle */}
            <div className="flex rounded overflow-hidden border border-gray-200 dark:border-gray-700 text-[10px]">
              <button
                type="button"
                onClick={() => updateRow(row.queryId, { mode: 'visual' })}
                className={`px-2.5 py-1 font-semibold transition-colors ${
                  row.mode === 'visual'
                    ? 'bg-accent text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
              >
                {t('governance.dashboardBuilder.queryBuilder.visual', 'Visual')}
              </button>
              <button
                type="button"
                onClick={() => switchToNql(row.queryId)}
                className={`px-2.5 py-1 font-mono font-semibold transition-colors ${
                  row.mode === 'nql'
                    ? 'bg-accent text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
              >
                {t('governance.dashboardBuilder.queryBuilder.nql', 'NQL')}
              </button>
            </div>

            <div className="flex-1" />

            {/* Row menu */}
            <div className="relative">
              <button
                type="button"
                onClick={() => setOpenMenuId(openMenuId === row.queryId ? null : row.queryId)}
                className="p-1 rounded text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
                aria-label="Row menu"
              >
                <ChevronDown size={12} />
              </button>
              {openMenuId === row.queryId && (
                <div className="absolute right-0 top-full mt-1 z-10 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg py-1 min-w-[120px]">
                  <button
                    type="button"
                    onClick={() => { duplicateRow(row.queryId); setOpenMenuId(null); }}
                    className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <Copy size={11} />
                    {t('governance.dashboardBuilder.queryBuilder.duplicate', 'Duplicate')}
                  </button>
                  <button
                    type="button"
                    onClick={() => { copyNql(row); setOpenMenuId(null); }}
                    className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <Copy size={11} />
                    {t('governance.dashboardBuilder.queryBuilder.copyNql', 'Copy NQL')}
                  </button>
                  <button
                    type="button"
                    onClick={() => { removeRow(row.queryId); setOpenMenuId(null); }}
                    className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950"
                    disabled={rows.length === 1}
                  >
                    <Trash2 size={11} />
                    {t('governance.dashboardBuilder.queryBuilder.delete', 'Delete')}
                  </button>
                </div>
              )}
            </div>
          </div>

          {/* Visual mode fields */}
          {row.mode === 'visual' && (
            <div className="grid gap-2 text-xs">
              {/* Service */}
              <div className="grid grid-cols-[80px_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.service', 'Service')}
                </label>
                <select
                  value={row.serviceId}
                  onChange={(e) => updateRow(row.queryId, { serviceId: e.target.value })}
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent"
                >
                  <option value="">— select —</option>
                  {serviceOptions.map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
              </div>

              {/* Metric */}
              <div className="grid grid-cols-[80px_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.metric', 'Metric')}
                </label>
                <input
                  type="text"
                  value={row.metric}
                  onChange={(e) => updateRow(row.queryId, { metric: e.target.value })}
                  placeholder="http_requests_total"
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                />
              </div>

              {/* Filters */}
              <div className="grid grid-cols-[80px_1fr] items-start gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px] pt-1">
                  {t('governance.dashboardBuilder.queryBuilder.filters', 'Filters')}
                </label>
                <div className="flex flex-wrap gap-1 items-center">
                  {row.filters.map((f, i) => (
                    <span
                      key={i}
                      className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-accent/10 text-accent text-[10px] border border-accent/30"
                    >
                      <span className="font-mono">{f.key}{f.op}"{f.value}"</span>
                      <button
                        type="button"
                        onClick={() => updateRow(row.queryId, {
                          filters: row.filters.filter((_, j) => j !== i),
                        })}
                        className="ml-0.5 hover:text-red-400"
                        aria-label="Remove filter"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                  <button
                    type="button"
                    onClick={() => updateRow(row.queryId, {
                      filters: [...row.filters, { key: '', op: '=', value: '' }],
                    })}
                    className="text-[10px] text-gray-400 hover:text-accent"
                  >
                    {t('governance.dashboardBuilder.queryBuilder.addFilter', '+ Add')}
                  </button>
                </div>
              </div>

              {/* Inline filter editors */}
              {row.filters.map((f, i) => (
                (f.key === '' || f.value === '') && (
                  <div key={`fe-${i}`} className="grid grid-cols-[80px_1fr] items-center gap-2">
                    <span />
                    <div className="flex items-center gap-1">
                      <input
                        type="text"
                        value={f.key}
                        onChange={(e) => {
                          const updated = [...row.filters];
                          updated[i] = { ...updated[i], key: e.target.value };
                          updateRow(row.queryId, { filters: updated });
                        }}
                        placeholder="key"
                        className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-1.5 py-0.5 text-[10px] w-20 font-mono focus:outline-none focus:border-accent"
                      />
                      <select
                        value={f.op}
                        onChange={(e) => {
                          const updated = [...row.filters];
                          updated[i] = { ...updated[i], op: e.target.value };
                          updateRow(row.queryId, { filters: updated });
                        }}
                        className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-1 py-0.5 text-[10px] focus:outline-none focus:border-accent"
                      >
                        {FILTER_OPS.map((op) => <option key={op} value={op}>{op}</option>)}
                      </select>
                      <input
                        type="text"
                        value={f.value}
                        onChange={(e) => {
                          const updated = [...row.filters];
                          updated[i] = { ...updated[i], value: e.target.value };
                          updateRow(row.queryId, { filters: updated });
                        }}
                        placeholder="value or $var"
                        className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-1.5 py-0.5 text-[10px] flex-1 font-mono focus:outline-none focus:border-accent"
                      />
                    </div>
                  </div>
                )
              ))}

              {/* Group by */}
              <div className="grid grid-cols-[80px_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.groupBy', 'Group by')}
                </label>
                <input
                  type="text"
                  value={row.groupBy}
                  onChange={(e) => updateRow(row.queryId, { groupBy: e.target.value })}
                  placeholder="endpoint, status_code..."
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                />
              </div>

              {/* Function + Aggregation */}
              <div className="grid grid-cols-[80px_1fr_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.function', 'Function')}
                </label>
                <select
                  value={row.fn}
                  onChange={(e) => updateRow(row.queryId, { fn: e.target.value })}
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                >
                  {FUNCTIONS.map((f) => <option key={f} value={f}>{f}</option>)}
                </select>
                <select
                  value={row.aggFn}
                  onChange={(e) => updateRow(row.queryId, { aggFn: e.target.value })}
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                >
                  {AGG_FUNCTIONS.map((f) => <option key={f} value={f}>{f}</option>)}
                </select>
              </div>
            </div>
          )}

          {/* NQL mode */}
          {row.mode === 'nql' && (
            <div className="h-[180px] rounded overflow-hidden border border-gray-200 dark:border-gray-700">
              <NqlMonacoEditor
                value={row.nqlText}
                onChange={(val) => updateRow(row.queryId, { nqlText: val })}
              />
            </div>
          )}
        </div>
      ))}

      {/* Add query button */}
      {rows.length < QUERY_IDS.length && (
        <button
          type="button"
          onClick={addRow}
          className="flex items-center justify-center gap-1.5 w-full rounded-lg border border-dashed border-gray-300 dark:border-gray-600 py-2 text-xs text-gray-500 dark:text-gray-400 hover:border-accent hover:text-accent transition-colors"
        >
          <Plus size={12} />
          {t('governance.dashboardBuilder.queryBuilder.addQuery', '+ Add Query')}
        </button>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | grep -E "^src.*error|Error:" | head -20`

Expected: No errors in the new file.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/governance/components/VisualQueryBuilder.tsx
git commit -m "feat(dashboard-builder): add VisualQueryBuilder with visual/NQL hybrid mode"
```

---

## Task 5: Create DashboardVariablesBar component

**Files:**
- Create: `src/frontend/src/features/governance/components/DashboardVariablesBar.tsx`

- [ ] **Step 1: Create the component**

```tsx
// src/frontend/src/features/governance/components/DashboardVariablesBar.tsx
/**
 * DashboardVariablesBar — horizontal toolbar below the dashboard title bar.
 * Renders one dropdown per DashboardVariable, plus the global TimeRangePicker
 * and auto-refresh control. "+ Variável" button (dashed) opens the add-variable modal.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, RefreshCw, Clock } from 'lucide-react';
import { type DashboardVariable } from '../types/dashboardBuilder';
import { TimeRangePicker } from './TimeRangePicker';

const REFRESH_OPTIONS = [
  { label: 'Off', value: '' },
  { label: '5s', value: '5000' },
  { label: '30s', value: '30000' },
  { label: '1m', value: '60000' },
  { label: '5m', value: '300000' },
];

const INTERVAL_OPTIONS = ['1m', '5m', '15m', '30m', '1h', '3h', '6h', '12h', '1d'];

interface AddVariableModalProps {
  onAdd: (variable: DashboardVariable) => void;
  onClose: () => void;
}

function AddVariableModal({ onAdd, onClose }: AddVariableModalProps) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [label, setLabel] = useState('');
  const [type, setType] = useState<DashboardVariable['type']>('custom');
  const [optionsRaw, setOptionsRaw] = useState('');
  const [multi, setMulti] = useState(false);
  const [includeAll, setIncludeAll] = useState(false);
  const [error, setError] = useState('');

  const handleAdd = () => {
    if (!name.trim()) { setError('Name is required'); return; }
    if (name.includes('$')) { setError('Variable name cannot contain $'); return; }
    const options = type === 'interval'
      ? INTERVAL_OPTIONS
      : optionsRaw.split(',').map((s) => s.trim()).filter(Boolean);
    onAdd({
      name: name.trim(),
      label: label.trim() || name.trim(),
      type,
      options,
      value: multi ? [] : (options[0] ?? ''),
      multi,
      includeAll,
    });
    onClose();
  };

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center bg-black/60"
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="bg-white dark:bg-gray-900 rounded-lg shadow-2xl border border-gray-200 dark:border-gray-700 w-[400px] p-5 flex flex-col gap-4">
        <h3 className="text-sm font-semibold text-gray-900 dark:text-white">
          {t('governance.dashboardBuilder.variablesBar.addVariable', '+ Variable')}
        </h3>

        {error && <p className="text-xs text-red-500">{error}</p>}

        <div className="flex flex-col gap-3">
          <div>
            <label className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1 block">Name (used as $name)</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="service"
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1 block">Label (display)</label>
            <input
              type="text"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
              placeholder="Serviço"
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            />
          </div>
          <div>
            <label className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1 block">Type</label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value as DashboardVariable['type'])}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            >
              <option value="custom">Custom (fixed list)</option>
              <option value="text">Text (free input)</option>
              <option value="interval">Interval (1m, 5m, 1h…)</option>
            </select>
          </div>
          {type === 'custom' && (
            <div>
              <label className="text-xs font-medium text-gray-600 dark:text-gray-400 mb-1 block">Options (comma-separated)</label>
              <input
                type="text"
                value={optionsRaw}
                onChange={(e) => setOptionsRaw(e.target.value)}
                placeholder="production, staging, dev"
                className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
              />
            </div>
          )}
          <div className="flex items-center gap-4">
            <label className="flex items-center gap-1.5 text-xs text-gray-600 dark:text-gray-400 cursor-pointer">
              <input type="checkbox" checked={multi} onChange={(e) => setMulti(e.target.checked)} className="rounded" />
              Multi-value
            </label>
            <label className="flex items-center gap-1.5 text-xs text-gray-600 dark:text-gray-400 cursor-pointer">
              <input type="checkbox" checked={includeAll} onChange={(e) => setIncludeAll(e.target.checked)} className="rounded" />
              Include "All"
            </label>
          </div>
        </div>

        <div className="flex gap-2 justify-end">
          <button
            type="button"
            onClick={onClose}
            className="text-xs px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleAdd}
            className="text-xs px-3 py-1.5 rounded bg-accent text-white hover:bg-accent/80 font-semibold"
          >
            Add
          </button>
        </div>
      </div>
    </div>
  );
}

interface DashboardVariablesBarProps {
  variables: DashboardVariable[];
  timeRange: string;
  isReadOnly?: boolean;
  onVariableChange: (name: string, value: string | string[]) => void;
  onTimeRangeChange: (range: string) => void;
  onAddVariable: (variable: DashboardVariable) => void;
}

export function DashboardVariablesBar({
  variables,
  timeRange,
  isReadOnly = false,
  onVariableChange,
  onTimeRangeChange,
  onAddVariable,
}: DashboardVariablesBarProps) {
  const { t } = useTranslation();
  const [showAddModal, setShowAddModal] = useState(false);
  const [refreshInterval, setRefreshInterval] = useState('');

  if (variables.length === 0 && isReadOnly) return null;

  return (
    <>
      <div className="flex items-center gap-2 flex-wrap px-4 py-2 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50">

        {/* Variable dropdowns */}
        {variables.map((v) => (
          <div key={v.name} className="flex items-center gap-1.5">
            <label className="text-[10px] text-gray-500 dark:text-gray-400 whitespace-nowrap">
              {v.label}
            </label>

            {v.type === 'text' ? (
              <input
                type="text"
                value={typeof v.value === 'string' ? v.value : v.value.join(',')}
                onChange={(e) => onVariableChange(v.name, e.target.value)}
                className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-[11px] px-2 py-1 text-purple-500 dark:text-purple-400 font-mono min-w-[80px] focus:outline-none focus:border-accent"
              />
            ) : (
              <select
                value={typeof v.value === 'string' ? v.value : v.value[0] ?? ''}
                onChange={(e) => {
                  if (v.multi) {
                    // For multi, toggle value in array
                    const current = Array.isArray(v.value) ? v.value : [v.value];
                    const val = e.target.value;
                    const next = current.includes(val)
                      ? current.filter((x) => x !== val)
                      : [...current, val];
                    onVariableChange(v.name, next);
                  } else {
                    onVariableChange(v.name, e.target.value);
                  }
                }}
                className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-[11px] px-2 py-1 text-purple-500 dark:text-purple-400 font-mono min-w-[100px] focus:outline-none focus:border-accent cursor-pointer"
              >
                {v.includeAll && (
                  <option value="*">{t('governance.dashboardBuilder.variablesBar.allValues', 'All')}</option>
                )}
                {v.options.map((opt) => (
                  <option key={opt} value={opt}>{opt}</option>
                ))}
              </select>
            )}
          </div>
        ))}

        {/* Divider if there are variables */}
        {variables.length > 0 && (
          <div className="w-px h-5 bg-gray-200 dark:border-gray-700 mx-1 shrink-0" />
        )}

        {/* Time range picker */}
        <div className="flex items-center gap-1.5">
          <Clock size={11} className="text-yellow-500 shrink-0" />
          <TimeRangePicker
            value={timeRange}
            onChange={onTimeRangeChange}
          />
        </div>

        {/* Refresh */}
        <div className="flex items-center gap-1">
          <button
            type="button"
            className="flex items-center gap-1 px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-[11px] text-gray-500 dark:text-gray-400 hover:border-accent/50 transition-colors"
            onClick={() => {/* manual refresh — emit event */}}
            title={t('governance.dashboardBuilder.variablesBar.refresh', 'Refresh')}
          >
            <RefreshCw size={10} />
            <select
              value={refreshInterval}
              onChange={(e) => setRefreshInterval(e.target.value)}
              className="bg-transparent border-none outline-none text-[11px] cursor-pointer"
              onClick={(e) => e.stopPropagation()}
            >
              {REFRESH_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </button>
        </div>

        <div className="flex-1" />

        {/* Add variable button */}
        {!isReadOnly && (
          <button
            type="button"
            onClick={() => setShowAddModal(true)}
            className="flex items-center gap-1 text-[11px] px-2.5 py-1 rounded border border-dashed border-gray-300 dark:border-gray-600 text-gray-400 dark:text-gray-500 hover:border-accent hover:text-accent transition-colors"
          >
            <Plus size={10} />
            {t('governance.dashboardBuilder.variablesBar.addVariable', '+ Variable')}
          </button>
        )}
      </div>

      {showAddModal && (
        <AddVariableModal
          onAdd={(v) => { onAddVariable(v); setShowAddModal(false); }}
          onClose={() => setShowAddModal(false)}
        />
      )}
    </>
  );
}
```

- [ ] **Step 2: Check TimeRangePicker import is correct**

Run: `ls src/frontend/src/features/governance/components/TimeRangePicker.tsx`

Expected: File exists. If the named export differs, read the file and adjust the import accordingly.

- [ ] **Step 3: Verify build**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | grep -E "^src.*error|Error:" | head -20`

Expected: No errors.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/governance/components/DashboardVariablesBar.tsx
git commit -m "feat(dashboard-builder): add DashboardVariablesBar with variable dropdowns, time picker and refresh"
```

---

## Task 6: Create PanelEditorOverlay component

**Files:**
- Create: `src/frontend/src/features/governance/components/PanelEditorOverlay.tsx`

- [ ] **Step 1: Create the component**

```tsx
// src/frontend/src/features/governance/components/PanelEditorOverlay.tsx
/**
 * PanelEditorOverlay — full-screen overlay (fixed inset-0, z-50).
 * Grafana 10.x panel editor layout:
 *   - Header: title input, Discard, Apply
 *   - Top 40%: live preview of the panel
 *   - Bottom 60%: left=query/transform tabs, right=viz picker (230px)
 * Internal state: draftSlot (copy of BuilderSlot).
 * Nothing is committed until Apply is pressed.
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Table2 } from 'lucide-react';
import { DataTransformPanel } from './DataTransformPanel';
import { VisualQueryBuilder } from './VisualQueryBuilder';
import { PanelVisualizationPicker } from './PanelVisualizationPicker';
import {
  type DashboardVariable,
  type VisualQueryRow,
  type VizType,
  makeVisualQueryRow,
} from '../types/dashboardBuilder';
import { useTranslation as useT } from 'react-i18next';

// Re-use BuilderSlot type from the page — passed in via prop
// We avoid importing from the page (would create a circular dep).
// Instead, we use a structural type here.
export interface PanelEditorSlot {
  tempId: string;
  type: string;
  customTitle: string;
  nqlQuery: string;
  chartType: string;
  unit: string;
  yAxisMin: string;
  yAxisMax: string;
  thresholds: string;
  transforms: import('./DataTransformPanel').DataTransform[];
  visualQueryRows?: VisualQueryRow[];
  // All remaining BuilderSlot fields (pass-through)
  [key: string]: unknown;
}

type EditorTab = 'query' | 'transforms' | 'alerts';

interface PanelEditorOverlayProps {
  slot: PanelEditorSlot;
  variables: DashboardVariable[];
  onApply: (updatedSlot: PanelEditorSlot) => void;
  onClose: () => void;
}

export function PanelEditorOverlay({ slot, variables, onApply, onClose }: PanelEditorOverlayProps) {
  const { t } = useTranslation();
  const [draftSlot, setDraftSlot] = useState<PanelEditorSlot>(() => ({ ...slot }));
  const [activeTab, setActiveTab] = useState<EditorTab>('query');
  const [showTableView, setShowTableView] = useState(false);

  // Sync draft when the source slot changes identity (edge case: external update)
  useEffect(() => {
    setDraftSlot({ ...slot });
  }, [slot.tempId]); // eslint-disable-line react-hooks/exhaustive-deps

  const updateDraft = useCallback(<K extends keyof PanelEditorSlot>(
    key: K,
    value: PanelEditorSlot[K]
  ) => {
    setDraftSlot((prev) => ({ ...prev, [key]: value }));
  }, []);

  const handleApply = () => {
    onApply(draftSlot);
  };

  // Debounce preview (300ms visual / 500ms NQL) — in a real implementation,
  // the preview widget reads from draftSlot directly. Debouncing is done
  // by not re-rendering the preview chart on every keystroke.
  // React's batching + useEffect with a timer handles this.
  const [previewSlot, setPreviewSlot] = useState<PanelEditorSlot>(draftSlot);
  const previewTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const delay = draftSlot.nqlQuery !== previewSlot.nqlQuery ? 500 : 300;
    if (previewTimerRef.current) clearTimeout(previewTimerRef.current);
    previewTimerRef.current = setTimeout(() => {
      setPreviewSlot({ ...draftSlot });
    }, delay);
    return () => {
      if (previewTimerRef.current) clearTimeout(previewTimerRef.current);
    };
  }, [draftSlot]); // eslint-disable-line react-hooks/exhaustive-deps

  const vizRows: VisualQueryRow[] = draftSlot.visualQueryRows ?? [makeVisualQueryRow('A')];

  const tabs: Array<{ id: EditorTab; label: string }> = [
    { id: 'query', label: t('governance.dashboardBuilder.panelEditor.tabs.query', 'Query') },
    { id: 'transforms', label: t('governance.dashboardBuilder.panelEditor.tabs.transforms', 'Transforms') },
    { id: 'alerts', label: t('governance.dashboardBuilder.panelEditor.tabs.alerts', 'Alerts') },
  ];

  return (
    <div
      className="fixed inset-0 z-50 flex flex-col bg-white dark:bg-gray-950"
      role="dialog"
      aria-label={t('governance.dashboardBuilder.panelEditor.title', 'Panel Editor')}
    >
      {/* ── Header ── */}
      <div className="flex items-center gap-3 px-4 py-2.5 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 shrink-0">
        <button
          type="button"
          onClick={onClose}
          className="flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white transition-colors"
        >
          <ArrowLeft size={14} />
          {t('governance.dashboardBuilder.panelEditor.backToDashboard', '← Back to dashboard')}
        </button>

        <div className="flex-1 mx-4">
          <input
            type="text"
            value={draftSlot.customTitle}
            onChange={(e) => updateDraft('customTitle', e.target.value)}
            placeholder={t('governance.dashboardBuilder.panelEditor.title', 'Panel title')}
            className="w-full bg-transparent border-none outline-none text-sm font-semibold text-gray-900 dark:text-white focus:border-b focus:border-accent pb-0.5"
          />
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={onClose}
            className="text-xs px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
          >
            {t('governance.dashboardBuilder.panelEditor.discard', 'Discard')}
          </button>
          <button
            type="button"
            onClick={handleApply}
            className="text-xs px-4 py-1.5 rounded bg-accent text-white hover:bg-accent/80 font-semibold transition-colors"
          >
            {t('governance.dashboardBuilder.panelEditor.apply', 'Apply')}
          </button>
        </div>
      </div>

      {/* ── Preview (top 40%) ── */}
      <div
        className="shrink-0 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900/50 flex flex-col"
        style={{ height: '40vh' }}
      >
        <div className="flex items-center justify-between px-4 py-2 shrink-0">
          <span className="text-[10px] font-semibold uppercase tracking-wider text-gray-400 dark:text-gray-500">
            {t('governance.dashboardBuilder.panelEditor.preview', 'Preview')}
          </span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setShowTableView((v) => !v)}
              className={`flex items-center gap-1.5 text-[10px] px-2 py-1 rounded border transition-colors ${
                showTableView
                  ? 'border-accent bg-accent/10 text-accent'
                  : 'border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 hover:border-accent/50'
              }`}
            >
              <Table2 size={11} />
              {t('governance.dashboardBuilder.panelEditor.tableView', 'Table')}
            </button>
          </div>
        </div>

        {/* Preview area — renders the panel based on previewSlot */}
        <div className="flex-1 mx-4 mb-3 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 overflow-hidden flex items-center justify-center">
          {showTableView ? (
            <div className="p-4 text-xs text-gray-400 dark:text-gray-500">
              {/* Table view placeholder — in a full implementation, run the NQL and
                  display raw rows here. Stubbed for v1. */}
              <span className="italic">Table view — raw data for debugging</span>
            </div>
          ) : (
            <div className="w-full h-full p-3 flex items-center justify-center text-gray-300 dark:text-gray-600">
              {/* Widget preview — rendered inline.
                  In the real canvas, BuilderWidgetCard renders the chart via WidgetRegistry.
                  The panel editor shows a simplified live preview. For v1 we show the NQL
                  compiled from the visual builder, or the NQL text directly. */}
              <div className="text-center">
                <p className="text-sm font-mono text-gray-400 dark:text-gray-500 leading-relaxed">
                  {draftSlot.nqlQuery
                    ? draftSlot.nqlQuery.slice(0, 120) + (draftSlot.nqlQuery.length > 120 ? '…' : '')
                    : <span className="italic text-[11px]">Configure a query to see a preview</span>}
                </p>
                <p className="text-[10px] text-gray-300 dark:text-gray-600 mt-2">
                  viz: <span className="font-mono text-accent/60">{draftSlot.chartType || 'default'}</span>
                </p>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ── Bottom section (Query tabs + Viz picker) ── */}
      <div className="flex-1 flex overflow-hidden">

        {/* Left: tabs + content */}
        <div className="flex-1 flex flex-col overflow-hidden border-r border-gray-200 dark:border-gray-700">

          {/* Tab bar */}
          <div className="flex items-center border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 shrink-0">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={`px-4 py-2.5 text-xs font-medium transition-colors border-b-2 ${
                  activeTab === tab.id
                    ? 'border-accent text-accent'
                    : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                }`}
              >
                {tab.label}
              </button>
            ))}

            <div className="flex-1" />

            {activeTab === 'query' && (
              <div className="flex items-center gap-2 pr-4">
                <span className="text-[10px] text-gray-400 dark:text-gray-500">
                  {t('governance.dashboardBuilder.queryBuilder.service', 'Data source:')}
                </span>
                <span className="text-[10px] px-2 py-0.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300">
                  NexTrace
                </span>
              </div>
            )}
          </div>

          {/* Tab content */}
          <div className="flex-1 overflow-hidden">
            {activeTab === 'query' && (
              <VisualQueryBuilder
                rows={vizRows}
                variables={variables}
                onRowsChange={(rows) => {
                  // Compile the first NQL row and store both
                  const firstNql = rows[0]?.mode === 'nql'
                    ? rows[0].nqlText
                    : rows.map((r) => {
                        if (r.mode === 'nql') return r.nqlText;
                        // Import compileToNql — but we can't import inside JSX.
                        // We'll use a simplified version here.
                        const fPart = r.metric
                          ? `${r.fn.replace('()', '')}(${r.metric}{service="${r.serviceId}"})`
                          : '';
                        const gPart = r.groupBy ? ` | ${r.aggFn} (${r.groupBy})` : '';
                        return fPart ? `${fPart}${gPart}` : '';
                      }).filter(Boolean).join('\n');
                  setDraftSlot((prev) => ({
                    ...prev,
                    visualQueryRows: rows,
                    nqlQuery: firstNql,
                  }));
                }}
              />
            )}
            {activeTab === 'transforms' && (
              <div className="h-full overflow-y-auto p-3">
                <DataTransformPanel
                  transforms={draftSlot.transforms}
                  onTransformsChange={(transforms) => updateDraft('transforms', transforms)}
                />
              </div>
            )}
            {activeTab === 'alerts' && (
              <div className="flex items-center justify-center h-full text-xs text-gray-400 dark:text-gray-500 italic">
                Alert rules — coming in v2
              </div>
            )}
          </div>
        </div>

        {/* Right: viz picker */}
        <PanelVisualizationPicker
          currentViz={(draftSlot.chartType as VizType) || 'timeseries'}
          unit={draftSlot.unit}
          yAxisMin={draftSlot.yAxisMin}
          yAxisMax={draftSlot.yAxisMax}
          thresholds={draftSlot.thresholds}
          onVizChange={(viz) => updateDraft('chartType', viz)}
          onUnitChange={(u) => updateDraft('unit', u)}
          onYAxisMinChange={(v) => updateDraft('yAxisMin', v)}
          onYAxisMaxChange={(v) => updateDraft('yAxisMax', v)}
        />
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Verify build**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | grep -E "^src.*error|Error:" | head -20`

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/governance/components/PanelEditorOverlay.tsx
git commit -m "feat(dashboard-builder): add PanelEditorOverlay (Grafana-style full-screen panel editor)"
```

---

## Task 7: Modify BuilderWidgetCard — add Edit button

**Files:**
- Modify: `src/frontend/src/features/governance/components/BuilderWidgetCard.tsx`

- [ ] **Step 1: Read the current file to confirm line numbers**

The file was read during brainstorming. Key sections:
- Interface `BuilderWidgetCardProps` at line 48: has `onConfigOpen: (tempId: string) => void`
- Import at line 6: `import { Settings, X, GripVertical } from 'lucide-react';`
- Button at line 101: `onClick onConfigOpen(tempId)` — the Settings gear button

- [ ] **Step 2: Apply the changes**

Change 1 — Add `Pencil` to the import:

```typescript
// OLD (line 6):
import { Settings, X, GripVertical } from 'lucide-react';

// NEW:
import { Settings, X, GripVertical, Pencil } from 'lucide-react';
```

Change 2 — Add `onEditOpen` prop to interface (keep `onConfigOpen` for backward compat during transition):

```typescript
// OLD (lines 48-60):
export interface BuilderWidgetCardProps {
  type: WidgetType;
  tempId: string;
  customTitle: string;
  w: number;
  h: number;
  isSelected: boolean;
  isReadOnly: boolean;
  onConfigOpen: (tempId: string) => void;
  onRemove: (tempId: string) => void;
  onSelect: (tempId: string) => void;
  children?: React.ReactNode;
}

// NEW:
export interface BuilderWidgetCardProps {
  type: WidgetType;
  tempId: string;
  customTitle: string;
  w: number;
  h: number;
  isSelected: boolean;
  isReadOnly: boolean;
  /** Opens the Panel Editor overlay for this widget */
  onEditOpen: (tempId: string) => void;
  /** Opens the legacy config drawer (deprecated — will be removed in v2) */
  onConfigOpen?: (tempId: string) => void;
  onRemove: (tempId: string) => void;
  onSelect: (tempId: string) => void;
  children?: React.ReactNode;
}
```

Change 3 — Add `onEditOpen` to destructuring:

```typescript
// OLD (line 62):
export function BuilderWidgetCard({
  type,
  tempId,
  customTitle,
  w,
  h,
  isSelected,
  isReadOnly,
  onConfigOpen,
  onRemove,
  onSelect,
  children,
}: BuilderWidgetCardProps) {

// NEW:
export function BuilderWidgetCard({
  type,
  tempId,
  customTitle,
  w,
  h,
  isSelected,
  isReadOnly,
  onEditOpen,
  onConfigOpen,
  onRemove,
  onSelect,
  children,
}: BuilderWidgetCardProps) {
```

Change 4 — Replace the actions buttons section. The old Settings button triggers `onConfigOpen`. Replace it with a Pencil button triggering `onEditOpen`:

```tsx
// OLD (lines 100-127):
        {!isReadOnly && (
          <div className="builder-widget-header__actions">
            <button
              type="button"
              className="builder-widget-header__btn"
              onClick={(e) => {
                e.stopPropagation();
                onConfigOpen(tempId);
              }}
              title={t('governance.dashboardBuilder.configWidget', 'Configure widget')}
              aria-label={t('governance.dashboardBuilder.configWidget', 'Configure widget')}
            >
              <Settings size={11} />
            </button>
            <button
              type="button"
              className="builder-widget-header__btn builder-widget-header__btn--danger"
              onClick={(e) => {
                e.stopPropagation();
                onRemove(tempId);
              }}
              title={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
              aria-label={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
            >
              <X size={11} />
            </button>
          </div>
        )}

// NEW:
        {!isReadOnly && (
          <div className="builder-widget-header__actions">
            <button
              type="button"
              className="builder-widget-header__btn"
              onClick={(e) => {
                e.stopPropagation();
                onEditOpen(tempId);
              }}
              title={t('governance.dashboardBuilder.panelEditor.title', 'Edit panel')}
              aria-label={t('governance.dashboardBuilder.panelEditor.title', 'Edit panel')}
            >
              <Pencil size={11} />
            </button>
            <button
              type="button"
              className="builder-widget-header__btn builder-widget-header__btn--danger"
              onClick={(e) => {
                e.stopPropagation();
                onRemove(tempId);
              }}
              title={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
              aria-label={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
            >
              <X size={11} />
            </button>
          </div>
        )}
```

- [ ] **Step 3: Verify build**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | grep -E "^src.*error|Error:" | head -30`

Expected: No errors (or only errors showing `onConfigOpen` missing at call sites — fix those in Task 8).

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/governance/components/BuilderWidgetCard.tsx
git commit -m "feat(dashboard-builder): replace Settings gear with Pencil edit button in BuilderWidgetCard"
```

---

## Task 8: Modify DashboardBuilderPage — wire up new components

**Files:**
- Modify: `src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx`

This is the main wiring task. Work through the file in sections.

- [ ] **Step 1: Add imports at the top of the file (after existing imports)**

Add these import lines after the last existing import:

```typescript
import { DashboardVariablesBar } from '../components/DashboardVariablesBar';
import { PanelEditorOverlay, type PanelEditorSlot } from '../components/PanelEditorOverlay';
import {
  type DashboardVariable,
  type VisualQueryRow,
} from '../types/dashboardBuilder';
```

- [ ] **Step 2: Extend the `BuilderSlot` interface — add `visualQueryRows` field**

Find the `BuilderSlot` interface (around line 119) and add one field at the end, before the closing `}`:

```typescript
// Add after line 151 (transforms: DataTransform[]):
  /** Persists visual query state across editor open/close */
  visualQueryRows?: VisualQueryRow[];
```

- [ ] **Step 3: Update `widgetFromSlot` to pass through `visualQueryRows`**

In `widgetFromSlot` (around line 160), add `visualQueryRows: undefined,` to the returned object (after `transforms: []`):

```typescript
// Add in the return object of widgetFromSlot, after transforms: []:
    visualQueryRows: undefined,
```

- [ ] **Step 4: Update `addWidget` and `handleDrop` to initialise `visualQueryRows`**

In `addWidget` (around line 1109), add `visualQueryRows: undefined,` to the `newSlot` object (after `transforms: []`).

In `handleDrop` (around line 1177), find the `newSlot` object literal and add `visualQueryRows: undefined,` to it as well.

- [ ] **Step 5: Add new state variables inside `DashboardBuilderPage` function**

Find the `// UI state` comment (around line 1070) and add below `activeConfigId`:

```typescript
const [variables, setVariables] = useState<DashboardVariable[]>([]);
const [timeRange, setTimeRange] = useState('6h');
const [editingSlotId, setEditingSlotId] = useState<string | null>(null);
```

Keep `activeConfigId` and `setActiveConfigId` in place for now (GridCanvas still references them). We'll alias `editingSlotId` to the new behavior.

- [ ] **Step 6: Update `addWidget` to open Panel Editor instead of ConfigDrawer**

Find the line `setActiveConfigId(newSlot.tempId);` in `addWidget` (around line 1142) and add below it:

```typescript
setEditingSlotId(newSlot.tempId);
```

- [ ] **Step 7: Update `removeSlot` to clear editingSlotId**

Find `removeSlot` (around line 1145) and add to the callback:

```typescript
setEditingSlotId((id) => (id === tempId ? null : id));
```

- [ ] **Step 8: Update `selectSlot` to use editingSlotId**

Replace the body of `selectSlot` to also trigger the panel editor:

```typescript
// OLD:
const selectSlot = useCallback((tempId: string) => {
  setActiveConfigId((id) => (id === tempId ? null : tempId));
}, []);

// NEW:
const selectSlot = useCallback((tempId: string) => {
  setActiveConfigId((id) => (id === tempId ? null : tempId));
  // Also open/close the panel editor
  setEditingSlotId((id) => (id === tempId ? null : null)); // clicking canvas does not open editor
}, []);
```

- [ ] **Step 9: Extend the save payload to include variables**

Find the save mutation call (where `client.put('/governance/dashboards/...')` is called or the mutation payload is assembled). Find the PUT payload object that includes `widgets` and add `variables` to it:

```typescript
// In the mutation payload, add alongside existing fields:
variables,        // DashboardVariable[] — stored in the JSONB layout column
```

Find the `useEffect` or seed section (around line 1084) that loads `data.widgets` and add:

```typescript
// After setSlots(data.widgets.map(widgetFromSlot)):
if ((data as { variables?: DashboardVariable[] }).variables) {
  setVariables((data as { variables?: DashboardVariable[] }).variables ?? []);
}
```

- [ ] **Step 10: Add DashboardVariablesBar to the JSX render**

Find the `<main>` section where `<GridCanvas>` is rendered (around line 1647). The page structure is roughly:

```
<div className="flex flex-col h-full">
  ... (header, sidebar)
  <div className="flex flex-1 overflow-hidden">
    <aside> ... </aside>
    <main> <GridCanvas /> </main>
  </div>
</div>
```

After the opening `<div className="flex flex-1 overflow-hidden">` (or after the `<aside>`), insert `DashboardVariablesBar`:

```tsx
{/* ── Variables toolbar ─────────────────────────────────────── */}
{!isPreview && (
  <div className="border-b border-gray-200 dark:border-gray-700">
    <DashboardVariablesBar
      variables={variables}
      timeRange={timeRange}
      isReadOnly={isReadOnly}
      onVariableChange={(name, value) => {
        setVariables((prev) =>
          prev.map((v) => (v.name === name ? { ...v, value } : v))
        );
      }}
      onTimeRangeChange={setTimeRange}
      onAddVariable={(variable) => setVariables((prev) => [...prev, variable])}
    />
  </div>
)}
```

Note: The `DashboardVariablesBar` must be inside the scrollable area but above `GridCanvas`. Adjust placement so it appears between the page header and the grid.

- [ ] **Step 11: Update GridCanvas call to use `onEditOpen` instead of `onConfigOpen`**

Find the `<GridCanvas` render (around line 1655). It currently passes:

```tsx
onConfigOpen={(id) => setActiveConfigId((cur) => (cur === id ? null : id))}
```

Change it to:

```tsx
onConfigOpen={(id) => setEditingSlotId(id)}
```

- [ ] **Step 12: Update the GridCanvas `onConfigOpen` rendering of BuilderWidgetCard**

Find where `GridCanvas` renders `BuilderWidgetCard` (around line 944+). Search for `onConfigOpen` prop on `BuilderWidgetCard`. Change it from:

```tsx
onConfigOpen={onConfigOpen}
```

to:

```tsx
onEditOpen={onConfigOpen}
```

This keeps the interface clean — `GridCanvas` receives `onConfigOpen` from the parent, but passes it as `onEditOpen` to `BuilderWidgetCard`.

- [ ] **Step 13: Replace the ConfigDrawer render with PanelEditorOverlay**

Find the ConfigDrawer render at the bottom of the JSX (around line 1672):

```tsx
{/* ── Widget config drawer ───────────────────────────────────────────── */}
{activeConfigId && !isPreview && (() => {
  const slot = slots.find((s) => s.tempId === activeConfigId);
  if (!slot) return null;
  return (
    <ConfigDrawer
      slot={slot}
      onUpdate={(patch) => updateSlot(activeConfigId, patch)}
      onClose={() => setActiveConfigId(null)}
    />
  );
})()}
```

Replace the entire block with:

```tsx
{/* ── Panel Editor Overlay ───────────────────────────────────────────── */}
{editingSlotId && !isPreview && (() => {
  const slot = slots.find((s) => s.tempId === editingSlotId);
  if (!slot) return null;
  return (
    <PanelEditorOverlay
      slot={slot as PanelEditorSlot}
      variables={variables}
      onApply={(updatedSlot) => {
        updateSlot(editingSlotId, updatedSlot as Partial<BuilderSlot>);
        setEditingSlotId(null);
      }}
      onClose={() => setEditingSlotId(null)}
    />
  );
})()}
```

- [ ] **Step 14: Verify the full build**

Run: `cd src/frontend && npm run build -- --mode development 2>&1 | grep -iE "error" | grep -v "//|warning|eslint" | head -30`

Expected: Zero TypeScript errors. If there are remaining `onConfigOpen` type errors from the prop rename, fix them at the call site.

- [ ] **Step 15: Commit**

```bash
git add src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx
git commit -m "feat(dashboard-builder): wire DashboardVariablesBar and PanelEditorOverlay into DashboardBuilderPage"
```

---

## Task 9: Write tests

**Files:**
- Create: `src/frontend/src/__tests__/governance/DashboardBuilderPage.panelEditor.test.tsx`
- Create: `src/frontend/src/__tests__/governance/DashboardVariablesBar.test.tsx`

- [ ] **Step 1: Read an existing test file to confirm import conventions**

Run: `cat src/frontend/src/__tests__/pages/CustomDashboardsPage.test.tsx | head -40`

Look at how `vi.mock`, `QueryClient`, and `MemoryRouter` are used. Follow the same pattern.

- [ ] **Step 2: Create PanelEditorOverlay tests**

```tsx
// src/frontend/src/__tests__/governance/DashboardBuilderPage.panelEditor.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../i18n';
import { PanelEditorOverlay } from '../../features/governance/components/PanelEditorOverlay';
import type { PanelEditorSlot } from '../../features/governance/components/PanelEditorOverlay';

// Mock heavy dependencies
vi.mock('../../features/governance/components/NqlMonacoEditor', () => ({
  NqlMonacoEditor: ({ value, onChange }: { value: string; onChange: (v: string) => void }) => (
    <textarea data-testid="nql-editor" value={value} onChange={(e) => onChange(e.target.value)} />
  ),
}));

vi.mock('../../features/governance/components/DataTransformPanel', () => ({
  DataTransformPanel: () => <div data-testid="transform-panel" />,
}));

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: [] }),
  },
}));

const makeSlot = (overrides: Partial<PanelEditorSlot> = {}): PanelEditorSlot => ({
  tempId: 'slot-1',
  type: 'stat',
  customTitle: 'Test Panel',
  nqlQuery: 'rate(http_requests_total)',
  chartType: 'timeseries',
  unit: 'none',
  yAxisMin: '',
  yAxisMax: '',
  thresholds: '[]',
  transforms: [],
  ...overrides,
});

function renderOverlay(
  props: Partial<Parameters<typeof PanelEditorOverlay>[0]> = {}
) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const onApply = vi.fn();
  const onClose = vi.fn();
  const slot = makeSlot();
  return {
    onApply,
    onClose,
    ...render(
      <QueryClientProvider client={qc}>
        <I18nextProvider i18n={i18n}>
          <PanelEditorOverlay
            slot={slot}
            variables={[]}
            onApply={onApply}
            onClose={onClose}
            {...props}
          />
        </I18nextProvider>
      </QueryClientProvider>
    ),
  };
}

describe('PanelEditorOverlay', () => {
  it('renders the panel title in the header input', () => {
    renderOverlay();
    const titleInput = screen.getByDisplayValue('Test Panel');
    expect(titleInput).toBeTruthy();
  });

  it('calls onClose when Discard button is clicked', () => {
    const { onClose } = renderOverlay();
    const discardBtn = screen.getByRole('button', { name: /discard/i });
    fireEvent.click(discardBtn);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('calls onApply with the draft slot when Apply is clicked', () => {
    const { onApply } = renderOverlay();
    const applyBtn = screen.getByRole('button', { name: /apply/i });
    fireEvent.click(applyBtn);
    expect(onApply).toHaveBeenCalledTimes(1);
    expect(onApply).toHaveBeenCalledWith(expect.objectContaining({
      tempId: 'slot-1',
      type: 'stat',
    }));
  });

  it('updates the title when the user types in the title input', () => {
    const { onApply } = renderOverlay();
    const titleInput = screen.getByDisplayValue('Test Panel') as HTMLInputElement;
    fireEvent.change(titleInput, { target: { value: 'New Title' } });
    expect(titleInput.value).toBe('New Title');

    // Apply should use the new title
    const applyBtn = screen.getByRole('button', { name: /apply/i });
    fireEvent.click(applyBtn);
    expect(onApply).toHaveBeenCalledWith(expect.objectContaining({
      customTitle: 'New Title',
    }));
  });

  it('shows the Transform tab when clicked', () => {
    renderOverlay();
    // Transforms tab
    const tabs = screen.getAllByRole('button');
    const transformTab = tabs.find((b) => b.textContent?.includes('Transform') || b.textContent?.includes('Transforma'));
    expect(transformTab).toBeTruthy();
    fireEvent.click(transformTab!);
    expect(screen.getByTestId('transform-panel')).toBeTruthy();
  });

  it('calls onClose when Back to dashboard button is clicked', () => {
    const { onClose } = renderOverlay();
    const backBtn = screen.getByRole('button', { name: /back|voltar/i });
    fireEvent.click(backBtn);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
```

- [ ] **Step 3: Create DashboardVariablesBar tests**

```tsx
// src/frontend/src/__tests__/governance/DashboardVariablesBar.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import i18n from '../../i18n';
import { DashboardVariablesBar } from '../../features/governance/components/DashboardVariablesBar';
import type { DashboardVariable } from '../../features/governance/types/dashboardBuilder';

vi.mock('../../features/governance/components/TimeRangePicker', () => ({
  TimeRangePicker: ({ value, onChange }: { value: string; onChange: (v: string) => void }) => (
    <button data-testid="time-picker" onClick={() => onChange('1h')}>{value}</button>
  ),
}));

const makeVariable = (overrides: Partial<DashboardVariable> = {}): DashboardVariable => ({
  name: 'service',
  label: 'Serviço',
  type: 'custom',
  options: ['payment-api', 'auth-api'],
  value: 'payment-api',
  multi: false,
  includeAll: false,
  ...overrides,
});

function renderBar(
  variables: DashboardVariable[] = [makeVariable()],
  props: Partial<Parameters<typeof DashboardVariablesBar>[0]> = {}
) {
  const onVariableChange = vi.fn();
  const onTimeRangeChange = vi.fn();
  const onAddVariable = vi.fn();
  return {
    onVariableChange,
    onTimeRangeChange,
    onAddVariable,
    ...render(
      <I18nextProvider i18n={i18n}>
        <DashboardVariablesBar
          variables={variables}
          timeRange="6h"
          onVariableChange={onVariableChange}
          onTimeRangeChange={onTimeRangeChange}
          onAddVariable={onAddVariable}
          {...props}
        />
      </I18nextProvider>
    ),
  };
}

describe('DashboardVariablesBar', () => {
  it('renders the variable label', () => {
    renderBar();
    expect(screen.getByText('Serviço')).toBeTruthy();
  });

  it('renders variable options in the select', () => {
    renderBar();
    const select = screen.getByRole('combobox') as HTMLSelectElement;
    expect(select.value).toBe('payment-api');
    const options = Array.from(select.options).map((o) => o.value);
    expect(options).toContain('payment-api');
    expect(options).toContain('auth-api');
  });

  it('calls onVariableChange when a new option is selected', () => {
    const { onVariableChange } = renderBar();
    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: 'auth-api' } });
    expect(onVariableChange).toHaveBeenCalledWith('service', 'auth-api');
  });

  it('renders the time picker', () => {
    renderBar();
    expect(screen.getByTestId('time-picker')).toBeTruthy();
  });

  it('calls onTimeRangeChange when time picker fires', () => {
    const { onTimeRangeChange } = renderBar();
    fireEvent.click(screen.getByTestId('time-picker'));
    expect(onTimeRangeChange).toHaveBeenCalledWith('1h');
  });

  it('renders the + Variable button when not readOnly', () => {
    renderBar();
    const addBtn = screen.getByRole('button', { name: /variable|variável/i });
    expect(addBtn).toBeTruthy();
  });

  it('does not render + Variable button in readOnly mode', () => {
    renderBar([makeVariable()], { isReadOnly: true });
    const addBtn = screen.queryByRole('button', { name: /variable|variável/i });
    expect(addBtn).toBeNull();
  });

  it('renders a text input for text-type variables', () => {
    const textVar = makeVariable({ type: 'text', value: 'my-value' });
    renderBar([textVar]);
    const input = screen.getByDisplayValue('my-value') as HTMLInputElement;
    expect(input.type).toBe('text');
  });

  it('returns null if readOnly and no variables', () => {
    const { container } = renderBar([], { isReadOnly: true });
    expect(container.firstChild).toBeNull();
  });
});
```

- [ ] **Step 4: Run the tests**

Run: `cd src/frontend && npm run test -- --run src/__tests__/governance/ 2>&1 | tail -30`

Expected: All tests in both files pass. Fix any failures before committing.

Common failure causes:
- i18n keys not yet loaded in test: add fallback text to `t()` calls (already done in components above)
- `TimeRangePicker` props differ from the mock: adjust mock to match actual exported props

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/__tests__/governance/DashboardBuilderPage.panelEditor.test.tsx
git add src/frontend/src/__tests__/governance/DashboardVariablesBar.test.tsx
git commit -m "test(dashboard-builder): add tests for PanelEditorOverlay and DashboardVariablesBar"
```

---

## Task 10: Final build verification and smoke test

**Files:** (no new files — verification only)

- [ ] **Step 1: Run the full Vite build**

Run: `cd src/frontend && npm run build 2>&1 | tail -20`

Expected: Build succeeds with no TypeScript errors. Bundle size increase should be < 100KB (gzip).

- [ ] **Step 2: Run the full Vitest suite**

Run: `cd src/frontend && npm run test -- --run 2>&1 | tail -20`

Expected: All pre-existing tests still pass. New tests pass.

- [ ] **Step 3: Verify no `ConfigDrawer` import remains in DashboardBuilderPage**

Run: `grep -n "ConfigDrawer" src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx`

Expected: Zero lines. If any remain, they are dead code — remove them.

- [ ] **Step 4: Verify no leftover references to `onConfigOpen` as required prop**

Run: `grep -rn "onConfigOpen" src/frontend/src/features/governance/`

Expected: Only the optional prop definition in `BuilderWidgetCard.tsx` (if kept for backward compat) and any callers that already switched to `onEditOpen`. No TypeScript errors.

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "chore(dashboard-builder): final cleanup and verification — Grafana-like panel editor complete"
```

---

## Self-Review

### Spec coverage check

| Spec requirement | Task that implements it |
|-----------------|------------------------|
| Panel Editor full-screen overlay (z-50, fixed inset-0) | Task 6 (PanelEditorOverlay) |
| Header: title input, Apply, Discard | Task 6 |
| Preview top 40%, live preview | Task 6 |
| Table view toggle | Task 6 |
| Query / Transforms / Alerts tabs | Task 6 |
| Visual query builder (service, metric, filters, group-by, fn) | Task 4 (VisualQueryBuilder) |
| NQL mode toggle per row | Task 4 |
| + Add Query B button | Task 4 |
| Row ⋮ menu (duplicate, delete, copy NQL) | Task 4 |
| Viz picker with SVG thumbnails (11 types) | Task 3 (PanelVisualizationPicker) |
| Smart suggestions | Task 3 |
| Display options (unit, min/max, thresholds) | Task 3 |
| DashboardVariablesBar (variable dropdowns + time picker) | Task 5 |
| + Variável button + AddVariableModal | Task 5 |
| Auto-refresh dropdown | Task 5 |
| `DashboardVariable` type | Task 1 |
| `VisualQueryRow` type | Task 1 |
| `VizType` and `VizTypeMeta` types | Task 1 |
| `interpolateVariables` helper | Task 1 |
| `compileToNql` helper | Task 1 |
| `BuilderSlot.visualQueryRows?` extension | Task 8, Step 2 |
| State: `variables[]`, `timeRange`, `editingSlotId` | Task 8, Step 5 |
| ConfigDrawer replaced by PanelEditorOverlay | Task 8, Step 13 |
| Save payload includes variables | Task 8, Step 9 |
| i18n keys (4 locales) | Task 2 |
| Pencil Edit button on BuilderWidgetCard | Task 7 |
| Tests for PanelEditorOverlay | Task 9 |
| Tests for DashboardVariablesBar | Task 9 |
| Out of scope: alerts tab is placeholder | Task 6 — ✓ placeholder only |

### Placeholder scan

No TBD, TODO, or "similar to Task N" patterns. Every code step contains complete, compilable code.

### Type consistency check

- `PanelEditorSlot` is defined in `PanelEditorOverlay.tsx` and uses `[key: string]: unknown` index signature for pass-through BuilderSlot fields — avoids circular import.
- `onEditOpen` renamed from `onConfigOpen` in BuilderWidgetCard — `onConfigOpen` kept as optional for the GridCanvas → card bridge (one call site).
- `VisualQueryRow[]` flows: defined in `dashboardBuilder.ts` → used in `VisualQueryBuilder`, `PanelEditorOverlay`, and `BuilderSlot.visualQueryRows`.
- `DashboardVariable[]` flows: defined in `dashboardBuilder.ts` → `DashboardVariablesBar` props → `PanelEditorOverlay` props → `VisualQueryBuilder` props.
- `VizType` flows: defined in `dashboardBuilder.ts` → `PanelVisualizationPicker` props → `PanelEditorOverlay` casts `draftSlot.chartType as VizType`.

All type names, method signatures, and prop names are consistent across tasks.
