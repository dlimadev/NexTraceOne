/**
 * PanelVisualizationPicker — right sidebar (230px) inside PanelEditorOverlay.
 * Shows current viz type, a grid of all 11 types with SVG thumbnails,
 * smart suggestions, and display options (unit, decimals, min/max, thresholds).
 */
import { useTranslation } from 'react-i18next';
import { type VizType, VIZ_TYPE_META } from '../types/dashboardBuilder';

const SIDEBAR_WIDTH_PX = 230;
const UNIT_OPTIONS = ['none', 'req/s', 'ms', 'bytes', '%', 'rpm', 'errors/s'] as const;

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

function parseThresholdCount(json: string): number {
  try {
    return (JSON.parse(json) as unknown[]).length;
  } catch {
    return 0;
  }
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
  const thresholdCount = parseThresholdCount(thresholds);

  return (
    <div style={{ width: SIDEBAR_WIDTH_PX }} className="flex flex-col gap-3 h-full overflow-y-auto p-3 border-l border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 shrink-0">

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
            {UNIT_OPTIONS.map((u) => (
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
