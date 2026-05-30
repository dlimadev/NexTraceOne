/**
 * PanelEditorOverlay — full-screen overlay (fixed inset-0, z-50).
 * Grafana 10.x panel editor layout:
 *   - Header: back button, title input, Discard, Apply
 *   - Top 40%: live preview of the panel
 *   - Bottom 60%: left=query/transform tabs, right=viz picker (230px)
 * Internal state: draftSlot (copy of the passed slot).
 * Nothing is committed until Apply is pressed.
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react';
import {
  type DashboardVariable,
  type VisualQueryRow,
  type VizType,
  makeVisualQueryRow,
  compileToNql,
} from '../types/dashboardBuilder';
import { PanelVisualizationPicker } from './PanelVisualizationPicker';
import { VisualQueryBuilder } from './VisualQueryBuilder';
import { DataTransformPanel, type DataTransform } from './DataTransformPanel';

// ── Types ──────────────────────────────────────────────────────────────────

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
  transforms: DataTransform[];
  visualQueryRows?: VisualQueryRow[];
  [key: string]: unknown;
}

interface PanelEditorOverlayProps {
  slot: PanelEditorSlot;
  variables: DashboardVariable[];
  onApply: (updatedSlot: PanelEditorSlot) => void;
  onClose: () => void;
}

// ── Tab type ───────────────────────────────────────────────────────────────

type EditorTab = 'query' | 'transforms' | 'alerts';

// ── Component ──────────────────────────────────────────────────────────────

export function PanelEditorOverlay({ slot, variables, onApply, onClose }: PanelEditorOverlayProps) {
  const { t } = useTranslation();
  const [draftSlot, setDraftSlot] = useState<PanelEditorSlot>({ ...slot });
  const [activeTab, setActiveTab] = useState<EditorTab>('query');
  const [tableView, setTableView] = useState(false);
  const [previewSlot, setPreviewSlot] = useState<PanelEditorSlot>({ ...slot });
  const previewTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Reset draft whenever the slot identity changes
  useEffect(() => {
    setDraftSlot({ ...slot });
    setPreviewSlot({ ...slot });
  }, [slot.tempId]); // eslint-disable-line react-hooks/exhaustive-deps

  // Debounced preview update
  useEffect(() => {
    if (previewTimerRef.current !== null) {
      clearTimeout(previewTimerRef.current);
    }
    // Use longer debounce when NQL changed, shorter for visual changes
    const delay = draftSlot.nqlQuery !== previewSlot.nqlQuery ? 500 : 300;
    previewTimerRef.current = setTimeout(() => {
      setPreviewSlot({ ...draftSlot });
    }, delay);

    return () => {
      if (previewTimerRef.current !== null) {
        clearTimeout(previewTimerRef.current);
      }
    };
  }, [draftSlot]); // eslint-disable-line react-hooks/exhaustive-deps

  // Typed draft updater
  const updateDraft = useCallback(<K extends keyof PanelEditorSlot>(key: K, value: PanelEditorSlot[K]) => {
    setDraftSlot((prev) => ({ ...prev, [key]: value }));
  }, []);

  // Query rows with default
  const vizRows: VisualQueryRow[] = draftSlot.visualQueryRows ?? [makeVisualQueryRow('A')];

  const handleRowsChange = useCallback((rows: VisualQueryRow[]) => {
    const nql = rows.length === 1
      ? compileToNql(rows[0])
      : rows.map((r) => compileToNql(r)).join('\n');
    setDraftSlot((prev) => ({
      ...prev,
      visualQueryRows: rows,
      nqlQuery: nql,
    }));
  }, []);

  const handleApply = useCallback(() => {
    onApply(draftSlot);
  }, [draftSlot, onApply]);

  return (
    <div className="fixed inset-0 z-50 flex flex-col bg-white dark:bg-gray-950 text-heading">

      {/* ── Header ─────────────────────────────────────────────────────── */}
      <header className="flex items-center gap-3 px-4 py-2 border-b border-edge bg-card shrink-0">
        <button
          type="button"
          onClick={onClose}
          className="flex items-center gap-1.5 text-sm text-muted hover:text-gray-800 dark:hover:text-gray-100 transition-colors"
        >
          <ArrowLeft size={16} />
          <span>{t('governance.dashboardBuilder.panelEditor.backToDashboard')}</span>
        </button>

        <div className="h-5 w-px bg-elevated" />

        <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider shrink-0">
          {t('governance.dashboardBuilder.panelEditor.title')}
        </span>

        <input
          type="text"
          value={draftSlot.customTitle}
          onChange={(e) => updateDraft('customTitle', e.target.value)}
          className="flex-1 rounded border border-edge bg-card px-2 py-1 text-sm text-heading focus:outline-none focus:border-accent min-w-0"
          placeholder={t('governance.dashboardBuilder.panelEditor.title')}
        />

        <div className="flex items-center gap-2 ml-auto shrink-0">
          <button
            type="button"
            onClick={onClose}
            className="px-3 py-1.5 text-sm rounded border border-edge text-muted hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
          >
            {t('governance.dashboardBuilder.panelEditor.discard')}
          </button>
          <button
            type="button"
            onClick={handleApply}
            className="px-3 py-1.5 text-sm rounded bg-accent text-white hover:bg-accent/90 transition-colors font-semibold"
          >
            {t('governance.dashboardBuilder.panelEditor.apply')}
          </button>
        </div>
      </header>

      {/* ── Preview (top 40vh) ──────────────────────────────────────────── */}
      <section
        className="shrink-0 border-b border-edge bg-canvas flex flex-col"
        style={{ height: '40vh' }}
      >
        {/* Preview header */}
        <div className="flex items-center gap-2 px-4 py-2 border-b border-edge">
          <span className="text-xs font-semibold text-muted uppercase tracking-wider">
            {t('governance.dashboardBuilder.panelEditor.preview')}
          </span>
          <span className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted font-mono border border-edge">
            {previewSlot.chartType}
          </span>
          <div className="ml-auto flex items-center gap-2">
            <button
              type="button"
              onClick={() => setTableView((v) => !v)}
              className={`text-xs px-2 py-1 rounded border transition-colors ${
                tableView
                  ? 'bg-accent/10 border-accent/40 text-accent'
                  : 'border-edge text-muted hover:border-gray-300 dark:hover:border-gray-600'
              }`}
            >
              {t('governance.dashboardBuilder.panelEditor.tableView')}
            </button>
          </div>
        </div>

        {/* Preview body */}
        <div className="flex-1 flex items-center justify-center p-4 overflow-hidden">
          {tableView ? (
            <div className="w-full h-full flex items-center justify-center">
              <p className="text-xs text-gray-400 italic font-mono">
                {/* Raw data placeholder when table view is active */}
                {'[ raw data table — connect a data source to populate ]'}
              </p>
            </div>
          ) : previewSlot.nqlQuery ? (
            <div className="w-full h-full flex flex-col items-start justify-start gap-2">
              <div className="w-full rounded border border-edge bg-card p-3 overflow-auto max-h-full">
                <pre className="text-xs font-mono text-gray-700 dark:text-gray-300 whitespace-pre-wrap break-all">
                  {previewSlot.nqlQuery}
                </pre>
              </div>
            </div>
          ) : (
            <p className="text-sm text-gray-400 italic">
              {/* No NQL query placeholder */}
              {'— configure a query to see the preview —'}
            </p>
          )}
        </div>
      </section>

      {/* ── Bottom section (bottom 60vh) ────────────────────────────────── */}
      <div className="flex flex-1 min-h-0 overflow-hidden">

        {/* Left panel: tabs + content */}
        <div className="flex flex-col flex-1 min-w-0 overflow-hidden">

          {/* Tab bar */}
          <div className="flex items-center border-b border-edge bg-card shrink-0">
            {(
              [
                { id: 'query' as EditorTab, label: t('governance.dashboardBuilder.panelEditor.tabs.query') },
                { id: 'transforms' as EditorTab, label: t('governance.dashboardBuilder.panelEditor.tabs.transforms') },
                { id: 'alerts' as EditorTab, label: t('governance.dashboardBuilder.panelEditor.tabs.alerts') },
              ] as const
            ).map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
                  activeTab === tab.id
                    ? 'border-accent text-accent'
                    : 'border-transparent text-muted hover:text-gray-800 dark:hover:text-gray-100 hover:border-gray-300 dark:hover:border-gray-600'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          {/* Tab content */}
          <div className="flex-1 overflow-y-auto min-h-0">
            {activeTab === 'query' && (
              <div className="h-full flex flex-col">
                {/* Query tab header */}
                <div className="flex items-center gap-2 px-3 py-1.5 border-b border-gray-100 dark:border-gray-800 shrink-0">
                  <span className="text-[10px] text-gray-400 uppercase tracking-wider">
                    {t('governance.dashboardBuilder.queryBuilder.service')}
                  </span>
                </div>
                <div className="flex-1 overflow-y-auto min-h-0">
                  <VisualQueryBuilder
                    rows={vizRows}
                    variables={variables}
                    onRowsChange={handleRowsChange}
                  />
                </div>
              </div>
            )}

            {activeTab === 'transforms' && (
              <div className="p-3">
                <DataTransformPanel
                  transforms={draftSlot.transforms}
                  onChange={(transforms) => updateDraft('transforms', transforms)}
                />
              </div>
            )}

            {activeTab === 'alerts' && (
              <div className="flex items-center justify-center h-full p-8">
                <p className="text-sm text-gray-400 italic">
                  {t('governance.dashboardBuilder.panelEditor.tabs.alerts')} — coming in v2
                </p>
              </div>
            )}
          </div>
        </div>

        {/* Right panel: visualization picker (230px) */}
        <PanelVisualizationPicker
          currentViz={(draftSlot.chartType as VizType) || 'timeseries'}
          unit={draftSlot.unit}
          yAxisMin={draftSlot.yAxisMin}
          yAxisMax={draftSlot.yAxisMax}
          thresholds={draftSlot.thresholds}
          onVizChange={(viz) => updateDraft('chartType', viz)}
          onUnitChange={(unit) => updateDraft('unit', unit)}
          onYAxisMinChange={(val) => updateDraft('yAxisMin', val)}
          onYAxisMaxChange={(val) => updateDraft('yAxisMax', val)}
        />
      </div>
    </div>
  );
}
