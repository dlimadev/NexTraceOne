/**
 * DashboardBuilderPage — editor visual de dashboard com drag-and-drop (Fase 5).
 * Substitui o editor de formulário baseado em lista por um canvas interativo
 * estilo Grafana/Kibana usando react-grid-layout v2.
 * Paleta lateral de widgets com busca e filtro por categoria,
 * arrastar widgets para o canvas, redimensionar, configurar por painel lateral.
 * Guarda via PUT /governance/dashboards/{id}.
 */
import 'react-grid-layout/css/styles.css';
import 'react-resizable/css/styles.css';

import { useState, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Save,
  Eye,
  EyeOff,
  Download,
  Upload,
  Shuffle,
  Search,
  Settings,
  X,
  ChevronDown,
  ChevronUp,
  LayoutGrid,
} from 'lucide-react';
import {
  GridLayout,
  useContainerWidth,
  type LayoutItem,
  type Layout,
} from 'react-grid-layout';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import client from '../../../api/client';
import {
  ALL_WIDGET_TYPES,
  WIDGET_META,
  WIDGET_CATEGORIES,
  TIME_RANGE_OPTIONS,
  STAT_METRIC_OPTIONS,
  NQL_RENDER_HINTS,
  type WidgetType,
  type WidgetSlot,
  type WidgetCategory,
} from '../widgets/WidgetRegistry';

// ── Emoji icon map ─────────────────────────────────────────────────────────

const WIDGET_ICONS: Record<string, string> = {
  'dora-metrics':          '📊',
  'service-scorecard':     '🏆',
  'incident-summary':      '🚨',
  'change-confidence':     '🎯',
  'cost-trend':            '💰',
  'reliability-slo':       '🛡️',
  'knowledge-graph':       '🕸️',
  'on-call-status':        '📞',
  'alert-status':          '⚠️',
  'change-timeline':       '📅',
  'slo-gauge':             '⏱️',
  'deployment-frequency':  '🚀',
  'stat':                  '📈',
  'text-markdown':         '📝',
  'top-services':          '🔝',
  'contract-coverage':     '📋',
  'blast-radius':          '💥',
  'team-health':           '💪',
  'release-calendar':      '📆',
  'query-widget':          '🔍',
  'obs-metrics':           '📡',
  'obs-logs':              '📜',
  'obs-traces':            '🔗',
  'obs-error-rate':        '🚦',
  'obs-service-map':       '🗺️',
};

function widgetIcon(type: string): string {
  return WIDGET_ICONS[type] ?? '📦';
}

// ── Types ──────────────────────────────────────────────────────────────────

interface DashboardDetail {
  dashboardId: string;
  name: string;
  description?: string | null;
  layout: string;
  persona: string;
  tags?: string[] | null;
  widgets: WidgetSlot[];
  isSystem: boolean;
  teamId?: string | null;
}

const LAYOUTS = ['single-column', 'two-column', 'three-column', 'grid', 'custom'] as const;

const PERSONAS = [
  'Engineer', 'TechLead', 'Architect', 'Executive',
  'Product', 'PlatformAdmin', 'Auditor',
] as const;

/** Internal builder slot — uses x/y/w/h for react-grid-layout */
interface BuilderSlot {
  tempId: string;
  existingWidgetId?: string | null;
  type: WidgetType;
  x: number;
  y: number;
  w: number;
  h: number;
  serviceId: string;
  teamId: string;
  timeRange: string;
  customTitle: string;
  metric: string;
  content: string;
  nqlQuery: string;
  renderHint: string;
  metricName: string;
  otelEnvironment: string;
  logSeverity: string;
  minDurationMs: string;
}

// ── Helpers ────────────────────────────────────────────────────────────────

function makeTempId(): string {
  return `slot-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}

function widgetFromSlot(w: WidgetSlot): BuilderSlot {
  return {
    tempId: makeTempId(),
    existingWidgetId: w.widgetId,
    type: (w.type as WidgetType) ?? 'dora-metrics',
    x: w.posX ?? 0,
    y: w.posY ?? 0,
    w: w.width ?? 2,
    h: w.height ?? 2,
    serviceId: '',
    teamId: '',
    timeRange: w.timeRange ?? '24h',
    customTitle: w.customTitle ?? '',
    metric: w.metric ?? '',
    content: w.content ?? '',
    nqlQuery: w.nqlQuery ?? '',
    renderHint: w.renderHint ?? 'table',
    metricName: '',
    otelEnvironment: '',
    logSeverity: 'ERROR',
    minDurationMs: '',
  };
}

function slotsToLayout(slots: BuilderSlot[]): Layout {
  return slots.map((s) => ({
    i: s.tempId,
    x: s.x,
    y: s.y,
    w: s.w,
    h: s.h,
    minW: 1,
    minH: 1,
  }));
}

// ── API hooks ──────────────────────────────────────────────────────────────

const TENANT_ID = 'default';

const useGetDashboard = (dashboardId: string) =>
  useQuery({
    queryKey: ['dashboard-detail', dashboardId, TENANT_ID],
    queryFn: () =>
      client
        .get<DashboardDetail>(`/governance/dashboards/${dashboardId}`, {
          params: { tenantId: TENANT_ID },
        })
        .then((r) => r.data),
    enabled: Boolean(dashboardId),
  });

const useUpdateDashboard = (dashboardId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: object) =>
      client.put(`/governance/dashboards/${dashboardId}`, payload).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['dashboard-detail', dashboardId] });
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
      qc.invalidateQueries({ queryKey: ['dashboard-render-data', dashboardId] });
    },
  });
};

// ── Auto-arrange (bin-packing, 12-col grid) ────────────────────────────────

function autoArrange(slots: BuilderSlot[]): BuilderSlot[] {
  const COLS = 12;
  // Ocupação de células: Set<`${x},${y}`>
  const occupied = new Set<string>();

  const isFree = (x: number, y: number, w: number, h: number): boolean => {
    for (let dy = 0; dy < h; dy++) {
      for (let dx = 0; dx < w; dx++) {
        if (x + dx >= COLS) return false;
        if (occupied.has(`${x + dx},${y + dy}`)) return false;
      }
    }
    return true;
  };

  const occupy = (x: number, y: number, w: number, h: number) => {
    for (let dy = 0; dy < h; dy++) {
      for (let dx = 0; dx < w; dx++) {
        occupied.add(`${x + dx},${y + dy}`);
      }
    }
  };

  const sorted = [...slots].sort((a, b) => b.w * b.h - a.w * a.h);
  let maxY = 0;

  return sorted.map((slot) => {
    const w = Math.min(slot.w, COLS);
    const h = slot.h;
    // Scan rows from top
    for (let y = 0; y <= maxY + 1; y++) {
      for (let x = 0; x <= COLS - w; x++) {
        if (isFree(x, y, w, h)) {
          occupy(x, y, w, h);
          maxY = Math.max(maxY, y + h - 1);
          return { ...slot, x, y, w };
        }
      }
    }
    // Fallback: append at bottom
    const y = maxY + 1;
    occupy(0, y, w, h);
    maxY = y + h - 1;
    return { ...slot, x: 0, y, w };
  });
}

// ── Widget Palette ─────────────────────────────────────────────────────────

interface PaletteCardProps {
  type: WidgetType;
  label: string;
  meta: (typeof WIDGET_META)[WidgetType];
  onAdd: (type: WidgetType) => void;
  onDragStart: (type: WidgetType) => void;
}

function PaletteCard({ type, label, meta, onAdd, onDragStart }: PaletteCardProps) {
  return (
    <div
      className="flex flex-col items-center gap-1 p-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 cursor-grab active:cursor-grabbing hover:border-accent/60 hover:shadow-md transition-all select-none"
      draggable
      onDragStart={(e) => {
        e.dataTransfer.effectAllowed = 'copy';
        e.dataTransfer.setData('application/x-widget-type', type);
        onDragStart(type);
      }}
      onClick={() => onAdd(type)}
      title={`${label} — ${meta.defaultWidth}×${meta.defaultHeight} — drag or click to add`}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') onAdd(type); }}
    >
      <span className="text-xl leading-none">{widgetIcon(type)}</span>
      <span className="text-[10px] font-medium text-gray-700 dark:text-gray-300 text-center leading-tight line-clamp-2">
        {label}
      </span>
      <span className="text-[9px] text-gray-400 tabular-nums">
        {meta.defaultWidth}×{meta.defaultHeight}
      </span>
    </div>
  );
}

// ── Widget Config Drawer ───────────────────────────────────────────────────

interface ConfigDrawerProps {
  slot: BuilderSlot;
  onUpdate: (patch: Partial<BuilderSlot>) => void;
  onClose: () => void;
}

function ConfigDrawer({ slot, onUpdate, onClose }: ConfigDrawerProps) {
  const { t } = useTranslation();
  const isOtel = slot.type.startsWith('obs-');

  return (
    <div
      className="fixed top-0 right-0 h-full w-80 bg-white dark:bg-gray-900 border-l border-gray-200 dark:border-gray-700 shadow-2xl z-50 flex flex-col"
      role="dialog"
      aria-label={t('governance.dashboardBuilder.configPanel', 'Widget configuration')}
    >
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700 shrink-0">
        <div className="flex items-center gap-2">
          <span className="text-lg">{widgetIcon(slot.type)}</span>
          <span className="text-sm font-semibold text-gray-900 dark:text-white truncate">
            {slot.customTitle || t(WIDGET_META[slot.type]?.labelKey ?? slot.type, slot.type)}
          </span>
        </div>
        <button
          onClick={onClose}
          className="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-500 dark:text-gray-400 transition-colors"
          aria-label={t('common.close', 'Close')}
        >
          <X size={16} />
        </button>
      </div>

      {/* Fields */}
      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4">

        {/* Custom title */}
        <div>
          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            {t('governance.dashboardBuilder.customTitle', 'Custom Title')}
          </label>
          <input
            type="text"
            value={slot.customTitle}
            onChange={(e) => onUpdate({ customTitle: e.target.value })}
            maxLength={80}
            placeholder={t('governance.dashboardBuilder.customTitlePlaceholder', 'Optional override')}
            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
          />
        </div>

        {/* Time range */}
        <div>
          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            {t('governance.dashboardBuilder.timeRangeOverride', 'Time Range Override')}
          </label>
          <select
            value={slot.timeRange}
            onChange={(e) => onUpdate({ timeRange: e.target.value })}
            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
          >
            {TIME_RANGE_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {t(opt.labelKey, opt.value)}
              </option>
            ))}
          </select>
        </div>

        {/* Service */}
        <div>
          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            {t('governance.dashboardBuilder.serviceFilter', 'Service')}
          </label>
          <input
            type="text"
            value={slot.serviceId}
            onChange={(e) => onUpdate({ serviceId: e.target.value })}
            placeholder={t('governance.dashboardBuilder.serviceFilterPlaceholder', 'All services')}
            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
          />
        </div>

        {/* Team */}
        <div>
          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            {t('governance.dashboardBuilder.teamFilter', 'Team')}
          </label>
          <input
            type="text"
            value={slot.teamId}
            onChange={(e) => onUpdate({ teamId: e.target.value })}
            placeholder={t('governance.dashboardBuilder.teamFilterPlaceholder', 'All teams')}
            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
          />
        </div>

        {/* Conditional: stat */}
        {slot.type === 'stat' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
              {t('governance.dashboardBuilder.statMetric', 'KPI Metric')}
            </label>
            <select
              value={slot.metric || 'incidents-open'}
              onChange={(e) => onUpdate({ metric: e.target.value })}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            >
              {STAT_METRIC_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {t(opt.labelKey, opt.value)}
                </option>
              ))}
            </select>
          </div>
        )}

        {/* Conditional: text-markdown */}
        {slot.type === 'text-markdown' && (
          <div>
            <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
              {t('governance.dashboardBuilder.textContent', 'Content (Markdown)')}
            </label>
            <textarea
              value={slot.content}
              onChange={(e) => onUpdate({ content: e.target.value })}
              rows={8}
              maxLength={2000}
              placeholder={t('governance.dashboardBuilder.textContentPlaceholder', 'Supports **bold**, *italic*, # headings')}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white font-mono focus:outline-none focus:border-accent resize-y"
            />
            <p className="mt-1 text-[9px] text-gray-400">{slot.content.length}/2000</p>
          </div>
        )}

        {/* Conditional: query-widget */}
        {slot.type === 'query-widget' && (
          <>
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                {t('governance.dashboardBuilder.nqlQuery', 'NQL Query')}
              </label>
              <textarea
                value={slot.nqlQuery}
                onChange={(e) => onUpdate({ nqlQuery: e.target.value })}
                rows={4}
                placeholder="SELECT * FROM services LIMIT 20"
                className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white font-mono focus:outline-none focus:border-accent resize-y"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                {t('governance.dashboardBuilder.renderHint', 'Render Hint')}
              </label>
              <select
                value={slot.renderHint || 'table'}
                onChange={(e) => onUpdate({ renderHint: e.target.value })}
                className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
              >
                {NQL_RENDER_HINTS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {t(opt.labelKey, opt.value)}
                  </option>
                ))}
              </select>
            </div>
          </>
        )}

        {/* Conditional: Observability widgets */}
        {isOtel && (
          <>
            {slot.type === 'obs-metrics' && (
              <div>
                <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                  {t('governance.dashboardBuilder.metricName', 'Metric Name')}
                </label>
                <input
                  type="text"
                  value={slot.metricName}
                  onChange={(e) => onUpdate({ metricName: e.target.value })}
                  placeholder="http.server.request.duration"
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
                />
              </div>
            )}
            <div>
              <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                {t('governance.dashboardBuilder.otelEnvironment', 'Environment')}
              </label>
              <input
                type="text"
                value={slot.otelEnvironment}
                onChange={(e) => onUpdate({ otelEnvironment: e.target.value })}
                placeholder="production"
                className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
              />
            </div>
            {slot.type === 'obs-logs' && (
              <div>
                <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                  {t('governance.dashboardBuilder.logSeverity', 'Log Severity')}
                </label>
                <select
                  value={slot.logSeverity}
                  onChange={(e) => onUpdate({ logSeverity: e.target.value })}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
                >
                  {['ERROR', 'WARN', 'INFO', 'DEBUG'].map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
              </div>
            )}
            {slot.type === 'obs-traces' && (
              <div>
                <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                  {t('governance.dashboardBuilder.minDurationMs', 'Min Duration (ms)')}
                </label>
                <input
                  type="number"
                  min={0}
                  value={slot.minDurationMs}
                  onChange={(e) => onUpdate({ minDurationMs: e.target.value })}
                  placeholder="100"
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
                />
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

// ── Metadata Accordion ─────────────────────────────────────────────────────

interface MetaAccordionProps {
  name: string;
  description: string;
  layout: string;
  persona: string;
  tags: string;
  onChangeName: (v: string) => void;
  onChangeDescription: (v: string) => void;
  onChangeLayout: (v: string) => void;
  onChangePersona: (v: string) => void;
  onChangeTags: (v: string) => void;
}

function MetaAccordion({
  name, description, layout, persona, tags,
  onChangeName, onChangeDescription, onChangeLayout, onChangePersona, onChangeTags,
}: MetaAccordionProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  return (
    <div className="border-t border-gray-200 dark:border-gray-700 mt-3 pt-3">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="w-full flex items-center justify-between text-xs font-semibold text-gray-700 dark:text-gray-300 hover:text-accent transition-colors"
      >
        <span>{t('governance.dashboardBuilder.metadata', 'Dashboard Metadata')}</span>
        {open ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
      </button>

      {open && (
        <div className="mt-3 space-y-3">
          <div>
            <label className="block text-[10px] font-medium text-gray-500 dark:text-gray-400 mb-1">
              {t('governance.customDashboards.dashboardName', 'Name')}
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => onChangeName(e.target.value)}
              maxLength={100}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            />
          </div>
          <div>
            <label className="block text-[10px] font-medium text-gray-500 dark:text-gray-400 mb-1">
              {t('governance.customDashboards.description', 'Description')}
            </label>
            <textarea
              value={description}
              onChange={(e) => onChangeDescription(e.target.value)}
              rows={2}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent resize-none"
            />
          </div>
          <div>
            <label className="block text-[10px] font-medium text-gray-500 dark:text-gray-400 mb-1">
              {t('governance.customDashboards.layout', 'Layout')}
            </label>
            <select
              value={layout}
              onChange={(e) => onChangeLayout(e.target.value)}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            >
              {LAYOUTS.map((l) => (
                <option key={l} value={l}>{l}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-[10px] font-medium text-gray-500 dark:text-gray-400 mb-1">
              {t('governance.dashboardBuilder.persona', 'Persona')}
            </label>
            <select
              value={persona}
              onChange={(e) => onChangePersona(e.target.value)}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            >
              {PERSONAS.map((p) => (
                <option key={p} value={p}>{p}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-[10px] font-medium text-gray-500 dark:text-gray-400 mb-1">
              {t('governance.dashboardBuilder.tags', 'Tags (comma-separated)')}
            </label>
            <input
              type="text"
              value={tags}
              onChange={(e) => onChangeTags(e.target.value)}
              placeholder="sre, dora, executive"
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            />
          </div>
        </div>
      )}
    </div>
  );
}

// ── Grid canvas wrapper (width-aware) ──────────────────────────────────────

interface GridCanvasProps {
  slots: BuilderSlot[];
  isReadOnly: boolean;
  isPreview: boolean;
  activeConfigId: string | null;
  draggingType: WidgetType | null;
  onLayoutChange: (layout: Layout) => void;
  onDrop: (layout: Layout, item: LayoutItem | undefined, e: Event) => void;
  onDropDragOver: (e: DragEvent) => { w: number; h: number } | false | void;
  onConfigOpen: (tempId: string) => void;
  onRemove: (tempId: string) => void;
}

function GridCanvas({
  slots,
  isReadOnly,
  isPreview,
  activeConfigId,
  draggingType,
  onLayoutChange,
  onDrop,
  onDropDragOver,
  onConfigOpen,
  onRemove,
}: GridCanvasProps) {
  const { t } = useTranslation();
  const { width, containerRef, mounted } = useContainerWidth({ measureBeforeMount: true });
  const rowHeight = isPreview ? 40 : 60;

  const layout = slotsToLayout(slots);

  const slotMap = new Map(slots.map((s) => [s.tempId, s]));

  return (
    <div
      ref={containerRef}
      className="flex-1 min-h-[600px] relative rounded-xl overflow-hidden"
      style={{
        background: 'var(--grid-canvas-bg, #f8fafc)',
        backgroundImage: `
          radial-gradient(circle, #cbd5e1 1px, transparent 1px)
        `,
        backgroundSize: '24px 24px',
      }}
    >
      {slots.length === 0 && !draggingType && (
        <div className="absolute inset-0 flex flex-col items-center justify-center text-gray-400 pointer-events-none select-none z-10">
          <LayoutGrid size={48} className="mb-3 opacity-30" />
          <p className="text-sm font-medium opacity-60">
            {t('governance.dashboardBuilder.canvasEmpty', 'Drag widgets from the palette to build your dashboard')}
          </p>
        </div>
      )}

      {mounted && (
        <GridLayout
          width={width}
          layout={layout}
          gridConfig={{ cols: 12, rowHeight, margin: [8, 8], containerPadding: [12, 12] }}
          dragConfig={{ enabled: !isReadOnly, bounded: false }}
          resizeConfig={{ enabled: !isReadOnly }}
          dropConfig={{
            enabled: !isReadOnly,
            defaultItem: { w: 2, h: 2 },
            onDragOver: onDropDragOver,
          }}
          onLayoutChange={onLayoutChange}
          onDrop={onDrop}
          autoSize
        >
          {slots.map((slot) => {
            const meta = WIDGET_META[slot.type];
            const label = slot.customTitle || t(meta?.labelKey ?? slot.type, slot.type);
            const isConfigOpen = activeConfigId === slot.tempId;

            return (
              <div
                key={slot.tempId}
                className={`group relative bg-white dark:bg-gray-800 border rounded-lg shadow-sm overflow-hidden flex flex-col items-center justify-center transition-shadow ${
                  isConfigOpen
                    ? 'border-accent shadow-accent/20 shadow-md'
                    : 'border-gray-200 dark:border-gray-700 hover:shadow-md'
                }`}
              >
                {/* Drag handle area (non-button area = full card minus buttons) */}
                <div className="drag-handle absolute inset-0 cursor-grab active:cursor-grabbing" />

                {/* Widget content */}
                <div className="relative z-10 flex flex-col items-center gap-1 px-3 pointer-events-none select-none">
                  <span className="text-2xl">{widgetIcon(slot.type)}</span>
                  <span className="text-xs font-semibold text-gray-700 dark:text-gray-200 text-center leading-tight line-clamp-2">
                    {label}
                  </span>
                  <span className="text-[9px] text-gray-400 tabular-nums">
                    {slot.w}×{slot.h}
                  </span>
                </div>

                {/* Action buttons — shown on hover */}
                {!isReadOnly && (
                  <div className="absolute top-1.5 right-1.5 z-20 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-auto">
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); onConfigOpen(slot.tempId); }}
                      className="p-1 rounded bg-white/90 dark:bg-gray-700/90 border border-gray-200 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:text-accent hover:border-accent transition-colors shadow-sm"
                      aria-label={t('governance.dashboardBuilder.configWidget', 'Configure widget')}
                      title="Configure"
                    >
                      <Settings size={12} />
                    </button>
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); onRemove(slot.tempId); }}
                      className="p-1 rounded bg-white/90 dark:bg-gray-700/90 border border-gray-200 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:text-red-500 hover:border-red-400 transition-colors shadow-sm"
                      aria-label={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
                      title="Remove"
                    >
                      <X size={12} />
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </GridLayout>
      )}
    </div>
  );
}

// ── Main component ─────────────────────────────────────────────────────────

export function DashboardBuilderPage() {
  const { t } = useTranslation();
  const { dashboardId } = useParams<{ dashboardId: string }>();
  const navigate = useNavigate();
  const importInputRef = useRef<HTMLInputElement>(null);

  // ── Remote data ──────────────────────────────────────────────────────────
  const { data, isLoading, isError, refetch } = useGetDashboard(dashboardId ?? '');
  const updateMutation = useUpdateDashboard(dashboardId ?? '');

  // ── Local editor state ───────────────────────────────────────────────────
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [layout, setLayout] = useState<string>('two-column');
  const [persona, setPersona] = useState<string>('Engineer');
  const [tags, setTags] = useState<string>(''); // comma-separated raw string
  const [slots, setSlots] = useState<BuilderSlot[]>([]);
  const [initialized, setInitialized] = useState(false);

  // UI state
  const [isPreview, setIsPreview] = useState(false);
  const [activeConfigId, setActiveConfigId] = useState<string | null>(null);
  const [draggingType, setDraggingType] = useState<WidgetType | null>(null);
  const [paletteSearch, setPaletteSearch] = useState('');
  const [paletteCategory, setPaletteCategory] = useState<WidgetCategory>('all');

  // Feedback
  const [saveError, setSaveError] = useState<string | null>(null);
  const [importError, setImportError] = useState<string | null>(null);
  const [editingTitle, setEditingTitle] = useState(false);

  // ── Seed from API ────────────────────────────────────────────────────────
  if (data && !initialized) {
    setName(data.name);
    setDescription(data.description ?? '');
    setLayout(data.layout ?? 'two-column');
    setPersona(data.persona ?? 'Engineer');
    setTags((data.tags ?? []).join(', '));
    setSlots(data.widgets.map(widgetFromSlot));
    setInitialized(true);
  }

  const isReadOnly = Boolean(data?.isSystem);

  // ── Palette filtering ────────────────────────────────────────────────────
  const paletteWidgets = ALL_WIDGET_TYPES.filter((wt) => {
    const meta = WIDGET_META[wt];
    if (!meta) return false;
    const label = t(meta.labelKey, wt).toLowerCase();
    const matchSearch = !paletteSearch || label.includes(paletteSearch.toLowerCase());
    const matchCat = paletteCategory === 'all' || meta.category === paletteCategory;
    return matchSearch && matchCat;
  });

  // ── Slot operations ──────────────────────────────────────────────────────
  const addWidget = useCallback((type: WidgetType) => {
    const meta = WIDGET_META[type];
    const newSlot: BuilderSlot = {
      tempId: makeTempId(),
      type,
      x: 0,
      y: slots.length > 0 ? Math.max(...slots.map((s) => s.y + s.h)) : 0,
      w: meta?.defaultWidth ?? 2,
      h: meta?.defaultHeight ?? 2,
      serviceId: '',
      teamId: '',
      timeRange: '24h',
      customTitle: '',
      metric: 'incidents-open',
      content: '',
      nqlQuery: '',
      renderHint: 'table',
      metricName: '',
      otelEnvironment: '',
      logSeverity: 'ERROR',
      minDurationMs: '',
    };
    setSlots((prev) => [...prev, newSlot]);
    setActiveConfigId(newSlot.tempId);
  }, [slots]);

  const removeSlot = useCallback((tempId: string) => {
    setSlots((prev) => prev.filter((s) => s.tempId !== tempId));
    setActiveConfigId((id) => (id === tempId ? null : id));
  }, []);

  const updateSlot = useCallback((tempId: string, patch: Partial<BuilderSlot>) => {
    setSlots((prev) => prev.map((s) => (s.tempId === tempId ? { ...s, ...patch } : s)));
  }, []);

  // ── Layout change from react-grid-layout ─────────────────────────────────
  const handleLayoutChange = useCallback((rglLayout: Layout) => {
    setSlots((prev) =>
      prev.map((slot) => {
        const item = rglLayout.find((li) => li.i === slot.tempId);
        if (!item) return slot;
        return { ...slot, x: item.x, y: item.y, w: item.w, h: item.h };
      })
    );
  }, []);

  // ── Drop from palette ────────────────────────────────────────────────────
  const handleDrop = useCallback(
    (rglLayout: Layout, item: LayoutItem | undefined, e: Event) => {
      const dragEvent = e as DragEvent;
      const type = dragEvent.dataTransfer?.getData('application/x-widget-type') as WidgetType | undefined;
      if (!type || !item) return;

      const meta = WIDGET_META[type];
      const newSlot: BuilderSlot = {
        tempId: makeTempId(),
        type,
        x: item.x,
        y: item.y,
        w: meta?.defaultWidth ?? 2,
        h: meta?.defaultHeight ?? 2,
        serviceId: '',
        teamId: '',
        timeRange: '24h',
        customTitle: '',
        metric: 'incidents-open',
        content: '',
        nqlQuery: '',
        renderHint: 'table',
        metricName: '',
        otelEnvironment: '',
        logSeverity: 'ERROR',
        minDurationMs: '',
      };
      setSlots((prev) => [...prev, newSlot]);
      setDraggingType(null);
      setActiveConfigId(newSlot.tempId);
    },
    []
  );

  const handleDropDragOver = useCallback(
    (_e: DragEvent): { w: number; h: number } | false | void => {
      if (!draggingType) return false;
      const meta = WIDGET_META[draggingType];
      return { w: meta?.defaultWidth ?? 2, h: meta?.defaultHeight ?? 2 };
    },
    [draggingType]
  );

  // ── Auto-arrange ─────────────────────────────────────────────────────────
  const handleAutoArrange = () => {
    setSlots(autoArrange(slots));
  };

  // ── Export / Import JSON ─────────────────────────────────────────────────
  const handleExportJson = () => {
    const payload = {
      version: 2,
      name,
      description,
      layout,
      persona,
      tags: tags.split(',').map((t) => t.trim()).filter(Boolean),
      widgets: slots.map((s) => ({
        type: s.type,
        posX: s.x,
        posY: s.y,
        width: s.w,
        height: s.h,
        serviceId: s.serviceId || null,
        teamId: s.teamId || null,
        timeRange: s.timeRange || null,
        customTitle: s.customTitle || null,
        metric: s.metric || null,
        content: s.content || null,
        nqlQuery: s.nqlQuery || null,
        renderHint: s.renderHint || null,
        metricName: s.metricName || null,
        otelEnvironment: s.otelEnvironment || null,
        logSeverity: s.logSeverity || null,
        minDurationMs: s.minDurationMs ? Number(s.minDurationMs) : null,
      })),
    };
    const blob = new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${name.replace(/\s+/g, '_').toLowerCase() || 'dashboard'}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleImportJson = (e: React.ChangeEvent<HTMLInputElement>) => {
    setImportError(null);
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (ev) => {
      try {
        const json = JSON.parse(ev.target?.result as string);
        if (!Array.isArray(json.widgets)) throw new Error('Missing widgets array');
        if (json.name) setName(json.name);
        if (json.description) setDescription(json.description);
        if (json.layout) setLayout(json.layout);
        if (json.persona) setPersona(json.persona);
        if (Array.isArray(json.tags)) setTags(json.tags.join(', '));
        setSlots(
          (json.widgets as Record<string, unknown>[]).map((w) => ({
            tempId: makeTempId(),
            existingWidgetId: null,
            type: (w.type as WidgetType) ?? 'dora-metrics',
            x: Number(w.posX ?? 0),
            y: Number(w.posY ?? 0),
            w: Number(w.width ?? 2),
            h: Number(w.height ?? 2),
            serviceId: String(w.serviceId ?? ''),
            teamId: String(w.teamId ?? ''),
            timeRange: String(w.timeRange ?? '24h'),
            customTitle: String(w.customTitle ?? ''),
            metric: String(w.metric ?? ''),
            content: String(w.content ?? ''),
            nqlQuery: String(w.nqlQuery ?? ''),
            renderHint: String(w.renderHint ?? 'table'),
            metricName: String(w.metricName ?? ''),
            otelEnvironment: String(w.otelEnvironment ?? ''),
            logSeverity: String(w.logSeverity ?? 'ERROR'),
            minDurationMs: w.minDurationMs != null ? String(w.minDurationMs) : '',
          }))
        );
      } catch (err) {
        setImportError(
          t('governance.dashboardBuilder.importError', 'Invalid dashboard JSON file.')
        );
      }
    };
    reader.readAsText(file);
    e.target.value = '';
  };

  // ── Save ─────────────────────────────────────────────────────────────────
  const handleSave = async () => {
    setSaveError(null);
    try {
      await updateMutation.mutateAsync({
        dashboardId,
        tenantId: TENANT_ID,
        name,
        description: description || null,
        layout,
        tags: tags.split(',').map((t) => t.trim()).filter(Boolean),
        widgets: slots.map((s) => ({
          existingWidgetId: s.existingWidgetId ?? null,
          type: s.type,
          posX: s.x,
          posY: s.y,
          width: s.w,
          height: s.h,
          serviceId: s.serviceId || null,
          teamId: s.teamId || null,
          timeRange: s.timeRange || null,
          customTitle: s.customTitle || null,
          metric: s.metric || null,
          content: s.content || null,
          nqlQuery: s.nqlQuery || null,
          renderHint: s.renderHint || null,
        })),
      });
      navigate(`/governance/dashboards/${dashboardId}`);
    } catch {
      setSaveError(t('governance.dashboardBuilder.saveError', 'Failed to save dashboard.'));
    }
  };

  // ── Guards ───────────────────────────────────────────────────────────────
  if (!dashboardId) {
    return (
      <PageErrorState
        message={t('governance.dashboardView.notFound', 'Dashboard not found')}
        onRetry={() => navigate('/governance/custom-dashboards')}
      />
    );
  }
  if (isLoading) {
    return <PageLoadingState message={t('governance.dashboardBuilder.loading', 'Loading dashboard editor...')} />;
  }
  if (isError) {
    return <PageErrorState message={t('governance.dashboardBuilder.error', 'Failed to load dashboard')} onRetry={() => refetch()} />;
  }

  // ── Render ───────────────────────────────────────────────────────────────
  return (
    <div className="flex flex-col h-screen bg-gray-50 dark:bg-gray-950 overflow-hidden">

      {/* ── Toolbar ────────────────────────────────────────────────────────── */}
      <header className="shrink-0 flex items-center gap-3 px-4 py-2 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 shadow-sm z-30">
        <Link
          to={`/governance/dashboards/${dashboardId}`}
          className="inline-flex items-center gap-1 text-sm text-gray-500 dark:text-gray-400 hover:text-accent transition-colors shrink-0"
          aria-label={t('governance.dashboardBuilder.backToView', 'Back to Dashboard')}
        >
          <ArrowLeft size={16} />
        </Link>

        {/* Inline title edit */}
        <div className="flex-1 min-w-0">
          {editingTitle && !isReadOnly ? (
            <input
              autoFocus
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              onBlur={() => setEditingTitle(false)}
              onKeyDown={(e) => { if (e.key === 'Enter' || e.key === 'Escape') setEditingTitle(false); }}
              maxLength={100}
              className="w-full max-w-sm text-sm font-semibold bg-transparent border-b border-accent text-gray-900 dark:text-white focus:outline-none"
            />
          ) : (
            <button
              type="button"
              onClick={() => !isReadOnly && setEditingTitle(true)}
              className={`text-sm font-semibold text-gray-900 dark:text-white truncate max-w-sm text-left ${!isReadOnly ? 'hover:text-accent cursor-text' : 'cursor-default'}`}
              title={isReadOnly ? undefined : t('governance.dashboardBuilder.clickToEditTitle', 'Click to edit title')}
            >
              {name || t('governance.dashboardBuilder.untitled', 'Untitled Dashboard')}
            </button>
          )}
        </div>

        {isReadOnly && (
          <span className="text-xs text-yellow-600 dark:text-yellow-400 bg-yellow-50 dark:bg-yellow-900/30 px-2 py-0.5 rounded shrink-0">
            {t('governance.dashboardBuilder.systemReadOnly', 'Read-only')}
          </span>
        )}

        {/* Right actions */}
        <div className="flex items-center gap-2 shrink-0">
          {/* Preview toggle */}
          <Button
            size="sm"
            variant="secondary"
            onClick={() => setIsPreview((v) => !v)}
            aria-label={t('governance.dashboardBuilder.togglePreview', 'Toggle preview mode')}
          >
            {isPreview ? <EyeOff size={13} className="mr-1" /> : <Eye size={13} className="mr-1" />}
            {isPreview
              ? t('governance.dashboardBuilder.editMode', 'Edit')
              : t('governance.dashboardBuilder.previewMode', 'Preview')}
          </Button>

          {!isReadOnly && (
            <>
              <Button size="sm" variant="secondary" onClick={handleExportJson}>
                <Download size={13} className="mr-1" />
                {t('governance.dashboardBuilder.exportJson', 'Export')}
              </Button>

              <>
                <input
                  ref={importInputRef}
                  type="file"
                  accept="application/json,.json"
                  className="hidden"
                  aria-hidden="true"
                  onChange={handleImportJson}
                />
                <Button size="sm" variant="secondary" onClick={() => importInputRef.current?.click()}>
                  <Upload size={13} className="mr-1" />
                  {t('governance.dashboardBuilder.importJson', 'Import')}
                </Button>
              </>

              {slots.length > 0 && (
                <Button size="sm" variant="secondary" onClick={handleAutoArrange}>
                  <Shuffle size={13} className="mr-1" />
                  {t('governance.dashboardBuilder.autoArrange', 'Auto-arrange')}
                </Button>
              )}

              <Button onClick={handleSave} disabled={updateMutation.isPending}>
                <Save size={13} className="mr-1" />
                {t('governance.dashboardBuilder.save', 'Save')}
              </Button>
            </>
          )}
        </div>
      </header>

      {/* ── Error banners ──────────────────────────────────────────────────── */}
      {(saveError || importError) && (
        <div
          className="shrink-0 px-4 py-2 bg-red-50 dark:bg-red-900/20 border-b border-red-200 dark:border-red-800 text-sm text-red-700 dark:text-red-300 flex items-center justify-between"
          role="alert"
        >
          <span>{saveError ?? importError}</span>
          <button
            type="button"
            onClick={() => { setSaveError(null); setImportError(null); }}
            className="ml-3 text-red-400 hover:text-red-600"
          >
            <X size={14} />
          </button>
        </div>
      )}

      {/* ── Body (sidebar + canvas) ────────────────────────────────────────── */}
      <div className="flex flex-1 min-h-0 overflow-hidden">

        {/* ── Left sidebar: palette ──────────────────────────────────────── */}
        {!isPreview && (
          <aside className="w-60 shrink-0 flex flex-col bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 overflow-hidden">
            {/* Search */}
            <div className="px-3 pt-3 pb-2 shrink-0">
              <div className="relative">
                <Search
                  size={11}
                  className="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none"
                />
                <input
                  type="text"
                  placeholder={t('governance.dashboardBuilder.searchWidgets', 'Search widgets…')}
                  value={paletteSearch}
                  onChange={(e) => setPaletteSearch(e.target.value)}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs pl-6 pr-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
                />
              </div>
            </div>

            {/* Category tabs */}
            <div className="px-3 pb-2 shrink-0 flex flex-wrap gap-1">
              {WIDGET_CATEGORIES.map((cat) => (
                <button
                  key={cat.value}
                  type="button"
                  onClick={() => setPaletteCategory(cat.value)}
                  className={`rounded-full px-2 py-0.5 text-[9px] font-semibold transition-colors ${
                    paletteCategory === cat.value
                      ? 'bg-accent text-white'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'
                  }`}
                >
                  {t(cat.labelKey, cat.value)}
                </button>
              ))}
            </div>

            {/* Widget cards */}
            <div className="flex-1 overflow-y-auto px-3 pb-3">
              {paletteWidgets.length === 0 ? (
                <p className="text-xs text-gray-400 text-center py-6">
                  {t('governance.dashboardBuilder.noWidgetsFound', 'No widgets match the filter')}
                </p>
              ) : (
                <div className="grid grid-cols-2 gap-2">
                  {paletteWidgets.map((type) => {
                    const meta = WIDGET_META[type];
                    if (!meta) return null;
                    return (
                      <PaletteCard
                        key={type}
                        type={type}
                        label={t(meta.labelKey, type)}
                        meta={meta}
                        onAdd={addWidget}
                        onDragStart={setDraggingType}
                      />
                    );
                  })}
                </div>
              )}
            </div>

            {/* Metadata accordion */}
            {!isReadOnly && (
              <div className="px-3 pb-3 shrink-0">
                <MetaAccordion
                  name={name}
                  description={description}
                  layout={layout}
                  persona={persona}
                  tags={tags}
                  onChangeName={setName}
                  onChangeDescription={setDescription}
                  onChangeLayout={setLayout}
                  onChangePersona={setPersona}
                  onChangeTags={setTags}
                />
              </div>
            )}
          </aside>
        )}

        {/* ── Grid canvas ─────────────────────────────────────────────────── */}
        <main
          className="flex-1 overflow-auto p-4"
          onDragLeave={() => {
            // Reset dragging indicator when leaving the canvas entirely
          }}
        >
          <GridCanvas
            slots={slots}
            isReadOnly={isReadOnly || isPreview}
            isPreview={isPreview}
            activeConfigId={activeConfigId}
            draggingType={draggingType}
            onLayoutChange={handleLayoutChange}
            onDrop={handleDrop}
            onDropDragOver={handleDropDragOver}
            onConfigOpen={(id) => setActiveConfigId((cur) => (cur === id ? null : id))}
            onRemove={removeSlot}
          />
        </main>
      </div>

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
    </div>
  );
}
