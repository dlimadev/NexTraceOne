# Grafana-like Dashboard Builder Redesign — Design Spec

**Date:** 2026-05-29
**Author:** Diogo Lima (via brainstorming session)
**Status:** Approved for implementation

---

## 1. Overview

Redesign the custom dashboard creation experience to be Grafana 10.x-like. The key improvements are:

1. **Panel Editor overlay** — full-screen editor that opens when clicking "Edit" on any panel. Shows a live preview of the visualization (top), query/transform tabs (bottom-left), and visualization picker (bottom-right).
2. **Hybrid query builder** — visual dropdowns (service, metric, filters, group-by, function) with a "Switch to NQL" toggle for power users. Variables like `$service` can be referenced in the visual builder.
3. **Dashboard variables toolbar** — a bar below the dashboard title with template variable dropdowns and a global time picker. Changing a variable updates all panels simultaneously.

The existing canvas (react-grid-layout), palette sidebar, drag-and-drop, and widget type system are **preserved**. The main change is replacing the `ConfigDrawer` (right side panel) with the full `PanelEditorOverlay`, and adding `DashboardVariablesBar` to the top of the canvas.

---

## 2. Architecture — Component Decomposition

The existing `DashboardBuilderPage.tsx` (1687 lines) is refactored into focused components.

### File Map

| Action | Path | Responsibility | Est. Lines |
|--------|------|----------------|-----------|
| MOD | `features/governance/pages/DashboardBuilderPage.tsx` | Shell: canvas state, palette, grid, edit trigger | ~500 |
| NEW | `features/governance/types/dashboardBuilder.ts` | Shared types: `DashboardVariable`, `VisualQueryRow`, `VizType` | ~80 |
| NEW | `features/governance/components/DashboardVariablesBar.tsx` | Variables toolbar + time picker | ~200 |
| NEW | `features/governance/components/PanelEditorOverlay.tsx` | Full-screen overlay; preview + tabs + viz sidebar | ~400 |
| NEW | `features/governance/components/VisualQueryBuilder.tsx` | Visual builder + NQL toggle inside Panel Editor | ~300 |
| NEW | `features/governance/components/PanelVisualizationPicker.tsx` | SVG viz type grid + display options | ~200 |
| KEEP | `features/governance/components/NqlMonacoEditor.tsx` | Monaco NQL editor (used inside VisualQueryBuilder) | existing |
| KEEP | `features/governance/components/DataTransformPanel.tsx` | Transforms tab inside Panel Editor | existing |
| KEEP | `features/governance/components/BuilderWidgetCard.tsx` | Each panel card on canvas — gains "Edit" button | existing |
| KEEP | `features/governance/widgets/WidgetRegistry.ts` | Widget type definitions | existing |

### Component Tree

```
DashboardBuilderPage
├── DashboardVariablesBar          ← NEW (toolbar below dashboard title)
├── PaletteDrawer                  ← existing (left sidebar)
├── DashboardCanvas (GridLayout)   ← existing (12-col react-grid-layout)
│   └── BuilderWidgetCard[]        ← existing + new "Edit" button
└── PanelEditorOverlay             ← NEW (full-screen overlay, z-50)
    ├── PanelEditorHeader          ← inline (title input, Apply/Discard)
    ├── PanelPreview               ← inline (live chart preview, top 40%)
    ├── PanelEditorTabs            ← inline (Query | Transformações | Alertas)
    │   ├── VisualQueryBuilder     ← NEW (Query tab)
    │   └── DataTransformPanel     ← existing (Transformações tab)
    └── PanelVisualizationPicker   ← NEW (right sidebar, 230px)
```

### Data Flow

```
DashboardBuilderPage
  state: slots[]         → BuilderSlot[]  (canvas panels)
  state: variables[]     → DashboardVariable[]  (toolbar variables)
  state: timeRange       → string  (global time range)
  state: editingSlotId   → string | null  (which panel is open in editor)

DashboardVariablesBar
  props: variables[], timeRange
  emits: onVariableChange(name, value), onTimeRangeChange(range)
  → triggers re-render of all panels when a variable changes

BuilderWidgetCard
  props: slot, variables (for $var interpolation)
  onEdit: () => setEditingSlotId(slot.tempId)

PanelEditorOverlay
  receives: slot (copy), variables[], onApply(updatedSlot), onClose()
  internal state: draftSlot (edited copy — NOT committed until Apply)
  → Apply: calls onApply(draftSlot) → updates slots[] in parent
  → Discard / close: drops draftSlot, no state change in parent
```

---

## 3. New Types (`dashboardBuilder.ts`)

```typescript
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

/** Visualization type with SVG preview metadata */
export type VizType =
  | 'timeseries' | 'bar' | 'stat' | 'gauge'
  | 'donut' | 'heatmap' | 'table' | 'state-timeline'
  | 'histogram' | 'scatter' | 'candlestick';

export interface VizTypeMeta {
  id: VizType;
  label: string;
  /** SVG path data for the mini thumbnail icon */
  svgContent: string;
}
```

---

## 4. DashboardVariablesBar Component

### Behaviour

- Renders one dropdown per `DashboardVariable` in `variables[]`.
- Dropdowns for `query` type fetch options from the NexTrace catalog API (services, teams, environments).
- `custom` type uses the static `options[]` array.
- `text` type renders an `<input type="text">`.
- `interval` type uses a fixed list: `[1m, 5m, 15m, 30m, 1h, 3h, 6h, 12h, 1d]`.
- `multi: true` renders a multi-select dropdown with "Todas" option.
- The **time picker** is always present at the right of the variable row:
  - Relative ranges: Last 15m, 30m, 1h, 3h, 6h, 12h, 24h, 2d, 7d, 30d
  - Absolute range: date/time pickers for `from` / `to`
  - Active range shown in amber (`text-warning`) to distinguish from variable dropdowns.
- **Auto-refresh** dropdown: Off, 5s, 30s, 1m, 5m.
- **+ Variável** button (dashed, right-aligned): opens a modal to create a new variable (name, label, type, options).

### Variable interpolation

When a variable value changes, the parent (`DashboardBuilderPage`) updates `variables[]`. All `BuilderWidgetCard` components receive the new variables and interpolate `$varName` in their query before fetching data.

Interpolation helper (in `dashboardBuilder.ts`):
```typescript
export function interpolateVariables(
  text: string,
  variables: DashboardVariable[]
): string {
  return variables.reduce((acc, v) => {
    const val = Array.isArray(v.value) ? v.value.join(',') : v.value;
    return acc.replaceAll(`$${v.name}`, val);
  }, text);
}
```

---

## 5. PanelEditorOverlay Component

### Layout

```
┌─ Header ─────────────────────────────────────────────────────────────────────┐
│  ← Voltar   [título editável]                    [Descartar]  [Aplicar ✓]   │
├─ Preview (height: 40% of viewport) ─────────────────────────────────────────┤
│  Label: PREVIEW   [⊞ Tabela]  [↻ Actualizar]                                │
│                                                                              │
│                    Live chart render                                         │
│                                                                              │
├─ Query  Transformações  Alertas ─────────────────────┬─ Viz Picker (230px) ─┤
│  [🎛 Visual] [</> NQL]          Fonte: NexTrace ▾   │                      │
│                                                      │  (Section 6)         │
│  A ▾  serviceId  metric  filters  groupBy  fn        │                      │
│                                                      │                      │
│  [+ Adicionar Query B]                               │                      │
└──────────────────────────────────────────────────────┴──────────────────────┘
```

### Behaviour

- The overlay is rendered inside `DashboardBuilderPage` with `position: fixed; inset: 0; z-index: 50`.
- It is only mounted when `editingSlotId !== null`.
- On mount, it copies the matching `BuilderSlot` into local `draftSlot` state.
- **Apply**: calls `onApply(draftSlot)` → parent replaces the slot in `slots[]` → overlay closes.
- **Discard / Voltar**: calls `onClose()` → overlay unmounts, no state change.
- **Preview**: renders the same widget component as the canvas (via `BuilderWidgetCard` internals), driven by `draftSlot`. Updates are debounced: 300ms for visual mode field changes, 500ms for NQL text changes.
- **Table view**: button toggles the preview to show raw data in a table (useful for debugging queries).
- **Title input**: edits `draftSlot.customTitle` inline in the header.

### Tabs

| Tab | Component | Content |
|-----|-----------|---------|
| Query | `VisualQueryBuilder` | Visual/NQL query rows, data source selector |
| Transformações | `DataTransformPanel` | Existing transform pipeline UI |
| Alertas | `AlertRulesTab` (future) | Placeholder for v2 |

---

## 6. VisualQueryBuilder Component

### Behaviour

- Renders an array of `VisualQueryRow` objects (A, B, C...).
- Each row has a **mode toggle**: `[🎛 Visual]` / `[</> NQL]`.
- In **Visual mode**, each row shows:
  - **Serviço** — dropdown, loaded from catalog API (`/catalog/services`). Supports `$variable` values.
  - **Métrica** — dropdown, loaded from available metrics for the selected service.
  - **Filtros** — chip list + `+ Adicionar` button. Each filter: `key op value` (key and op are dropdowns; value is a text input that also supports `$variable`).
  - **Group by** — dropdown of available label keys for the selected metric.
  - **Função** — dropdown: `rate()`, `sum()`, `avg()`, `max()`, `min()`, `count()`.
  - **Agregação** — dropdown: `sum by`, `avg by`, `max by`.
- Clicking **"→ Editar como NQL"** or the NQL tab toggles the row to NQL mode:
  - Converts the visual fields to a NQL string and populates the Monaco editor.
  - If the NQL can be parsed back to visual fields, switching back to Visual works; otherwise the Visual button is disabled with a tooltip "NQL complexo — use o editor de código".
- The **+ Adicionar Query B** button appends a new `VisualQueryRow` with mode `visual`.
- Each row has a `⋮` menu: Duplicate, Delete, Copy NQL.

### Visual → NQL conversion

The conversion is a simple string builder (not a full parser):
```
rate(metric{service="$service", <filters>}) | group by <groupBy> | <aggFn>
```
This is a one-way transform for display. The NQL Monaco editor is authoritative when in NQL mode.

---

## 7. PanelVisualizationPicker Component

### Behaviour

- Shows the currently selected `VizType` at the top (highlighted in accent blue).
- Below: a 2-column grid of all supported viz types with SVG mini-thumbnail icons.
- **Smart suggestions**: the top section shows 4 suggested types based on the shape of the query result (time-series data → Time Series, Bar; single value → Stat, Gauge; categorical → Donut, Bar; log-like → Table, State Timeline).
- Clicking a viz type updates `draftSlot.chartType`.

### Supported visualization types (11)

| Type ID | Label | When to suggest |
|---------|-------|-----------------|
| `timeseries` | Série Temporal | Time-indexed data |
| `bar` | Barras | Categorical or time data |
| `stat` | Stat | Single aggregate value |
| `gauge` | Gauge | Single value with min/max range |
| `donut` | Donut | Part-of-whole data |
| `heatmap` | Heatmap | Two-dimensional bucketed data |
| `table` | Tabela | Any tabular data |
| `state-timeline` | State Timeline | Discrete state over time |
| `histogram` | Histograma | Distribution data |
| `scatter` | Scatter | Correlation between two metrics |
| `candlestick` | Candlestick | OHLC financial / latency percentile data |

### Display options (below the grid)

- **Unidade** — dropdown: `req/s`, `ms`, `bytes`, `%`, `none`, custom
- **Decimais** — input: Auto / 0 / 1 / 2
- **Min / Max** — inputs for Y-axis range (empty = auto)
- **Thresholds** — shows count of active thresholds with green badge; clicking opens the threshold editor

---

## 8. Changes to Existing Components

### `BuilderWidgetCard`

Add an "Editar" button (pencil icon, appears on hover) that triggers `onEdit()`. The card passes the current `variables[]` to the widget for `$var` interpolation in API calls.

### `DashboardBuilderPage`

- **State additions**:
  ```typescript
  const [variables, setVariables] = useState<DashboardVariable[]>([]);
  const [timeRange, setTimeRange] = useState('6h');
  const [editingSlotId, setEditingSlotId] = useState<string | null>(null);
  ```
- **Remove**: `ConfigDrawer` and its state (`configSlot`, `configDrawerOpen`). Replace with `PanelEditorOverlay`.
- **Keep**: all canvas state (`slots[]`, layout drag/drop, palette, auto-arrange, export/import, save).
- **`BuilderSlot` extension** — add one optional field to persist visual query state:

  ```typescript
  visualQueryRows?: VisualQueryRow[];  // undefined = NQL-only mode
  ```

  When the user applies from the Panel Editor: both `visualQueryRows` and `nqlQuery` (compiled) are saved. When reopening the editor: if `visualQueryRows` exists, open in Visual mode; otherwise open in NQL mode.
- **Dashboard JSON** — extend `DashboardDetail` to include `variables?: DashboardVariable[]`. Stored inside the existing JSONB `layout` payload alongside widgets. Additive — non-breaking for existing dashboards that have no variables.

---

## 9. API Compatibility

No backend changes required for this redesign:
- Variable values are resolved client-side via `interpolateVariables()` before query execution.
- The dashboard JSON stored in the `layout` field (JSONB) will include the new `variables` array — additive, non-breaking.
- The `BuilderSlot` gains no new fields (existing fields cover the visualization options).

---

## 10. i18n Keys Required

All new strings must use i18n keys (namespace `governance`):

```
governance.dashboardBuilder.panelEditor.title
governance.dashboardBuilder.panelEditor.apply
governance.dashboardBuilder.panelEditor.discard
governance.dashboardBuilder.panelEditor.preview
governance.dashboardBuilder.panelEditor.tableView
governance.dashboardBuilder.panelEditor.tabs.query
governance.dashboardBuilder.panelEditor.tabs.transforms
governance.dashboardBuilder.panelEditor.tabs.alerts
governance.dashboardBuilder.queryBuilder.visual
governance.dashboardBuilder.queryBuilder.nql
governance.dashboardBuilder.queryBuilder.addQuery
governance.dashboardBuilder.queryBuilder.service
governance.dashboardBuilder.queryBuilder.metric
governance.dashboardBuilder.queryBuilder.filters
governance.dashboardBuilder.queryBuilder.addFilter
governance.dashboardBuilder.queryBuilder.groupBy
governance.dashboardBuilder.queryBuilder.function
governance.dashboardBuilder.variablesBar.addVariable
governance.dashboardBuilder.variablesBar.timeRange
governance.dashboardBuilder.variablesBar.refresh
governance.dashboardBuilder.viz.selected
governance.dashboardBuilder.viz.suggestions
governance.dashboardBuilder.viz.types.timeseries
governance.dashboardBuilder.viz.types.bar
governance.dashboardBuilder.viz.types.stat
governance.dashboardBuilder.viz.types.gauge
governance.dashboardBuilder.viz.types.donut
governance.dashboardBuilder.viz.types.heatmap
governance.dashboardBuilder.viz.types.table
governance.dashboardBuilder.viz.types.state-timeline
governance.dashboardBuilder.viz.types.histogram
governance.dashboardBuilder.viz.types.scatter
governance.dashboardBuilder.viz.types.candlestick
```

Keys to be added to `src/frontend/src/locales/en/governance.json`, `pt/governance.json`, `es/governance.json`, `fr/governance.json`.

---

## 11. Error Handling

- **Query builder**: if the catalog API fails to load service/metric options, show an inline error with a retry button. Fall back to a text input.
- **Panel preview**: if the widget query fails, show the same `PageErrorState` component the widget uses in the canvas, contained within the preview area.
- **Apply with no query**: disable the Apply button if the active query row is in Visual mode with no service or metric selected.
- **Variable circular references**: `$service` cannot reference another variable. Simple guard: reject variable names that contain `$`.

---

## 12. Out of Scope (v1)

- Alert rules tab (placeholder only)
- Annotation layers
- Panel sharing / permalink
- Dashboard row grouping (collapsible sections)
- Variable chaining (variable B options depend on variable A value) — `custom` and `text` types cover the most common cases
- Grafana-style "Panel Styles" (preconfigured style presets)
