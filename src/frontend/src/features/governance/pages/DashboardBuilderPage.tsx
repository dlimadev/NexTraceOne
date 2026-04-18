/**
 * DashboardBuilderPage — editor de dashboard por slots (Fase 4, melhorias de UX).
 * Interface slot-based: utilizador seleciona posição num grid fixo e atribui um widget.
 * Cada slot tem: tipo, serviço alvo, time range override, título personalizado,
 * e para StatWidget: metric selector; para TextMarkdownWidget: content textarea.
 * Preview em tempo real à direita. Guarda via PUT /governance/dashboards/{id}.
 * Suporta Export/Import JSON (compatível com Grafana-like portability) e Auto-arrange.
 */
import { useState, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Plus,
  Trash2,
  Save,
  LayoutGrid,
  Eye,
  Download,
  Upload,
  Shuffle,
  Copy,
  ChevronDown,
  Search,
} from 'lucide-react';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { Card, CardBody } from '../../../components/Card';
import client from '../../../api/client';
import {
  ALL_WIDGET_TYPES,
  WIDGET_META,
  TIME_RANGE_OPTIONS,
  STAT_METRIC_OPTIONS,
  type WidgetType,
  type WidgetSlot,
} from '../widgets/WidgetRegistry';

// ── Types ──────────────────────────────────────────────────────────────────

interface DashboardDetail {
  dashboardId: string;
  name: string;
  description?: string | null;
  layout: string;
  persona: string;
  widgets: WidgetSlot[];
  isSystem: boolean;
  teamId?: string | null;
}

const LAYOUTS = ['single-column', 'two-column', 'three-column', 'grid', 'custom'] as const;

const MAX_COLS: Record<string, number> = {
  'single-column': 1,
  'two-column': 2,
  'three-column': 3,
  grid: 4,
  custom: 4,
};

// ── Hooks ──────────────────────────────────────────────────────────────────

const useGetDashboard = (dashboardId: string, tenantId: string) =>
  useQuery({
    queryKey: ['dashboard-detail', dashboardId, tenantId],
    queryFn: () =>
      client
        .get<DashboardDetail>(`/governance/dashboards/${dashboardId}`, {
          params: { tenantId },
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

// ── Builder slot editor ────────────────────────────────────────────────────

interface BuilderSlot {
  tempId: string;
  existingWidgetId?: string | null;
  type: WidgetType;
  posX: number;
  posY: number;
  width: number;
  height: number;
  serviceId: string;
  teamId: string;
  timeRange: string;
  customTitle: string;
  metric: string;
  content: string;
}

function makeSlotId() {
  return `slot-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
}

function widgetFromDetail(w: WidgetSlot): BuilderSlot {
  return {
    tempId: makeSlotId(),
    existingWidgetId: w.widgetId,
    type: (w.type as WidgetType) ?? 'dora-metrics',
    posX: w.posX,
    posY: w.posY,
    width: w.width ?? 2,
    height: w.height ?? 2,
    serviceId: '',
    teamId: '',
    timeRange: w.timeRange ?? '24h',
    customTitle: w.customTitle ?? '',
    metric: w.metric ?? '',
    content: w.content ?? '',
  };
}

// ── Grid Preview ────────────────────────────────────────────────────────────

/** Derives grid columns count from layout string */
function layoutCols(layout: string): number {
  switch (layout) {
    case 'single-column': return 1;
    case 'two-column': return 2;
    case 'three-column': return 3;
    default: return 4;
  }
}

/** Visual preview of the current slot layout */
function GridPreview({ slots, layout }: { slots: BuilderSlot[]; layout: string }) {
  const { t } = useTranslation();
  const cols = layoutCols(layout);

  if (slots.length === 0) {
    return (
      <div className="h-40 flex items-center justify-center text-xs text-gray-400 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-lg">
        {t('governance.dashboardBuilder.previewEmpty', 'No widgets yet')}
      </div>
    );
  }

  // Compute grid height: max (posY + height) across all slots
  const gridRows = Math.max(...slots.map((s) => s.posY + s.height), 2);

  return (
    <div
      className="border border-gray-200 dark:border-gray-700 rounded-lg p-2 bg-gray-50 dark:bg-gray-900/50"
      style={{
        display: 'grid',
        gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))`,
        gridTemplateRows: `repeat(${gridRows}, 40px)`,
        gap: '4px',
      }}
      aria-label={t('governance.dashboardBuilder.previewLabel', 'Dashboard preview')}
    >
      {slots.map((slot) => {
        const meta = WIDGET_META[slot.type];
        const clampedPosX = Math.min(Math.max(slot.posX, 0), cols - 1);
        const clampedWidth = Math.min(slot.width, cols - clampedPosX);
        return (
          <div
            key={slot.tempId}
            style={{
              gridColumn: `${clampedPosX + 1} / span ${clampedWidth}`,
              gridRow: `${slot.posY + 1} / span ${slot.height}`,
            }}
            className="rounded bg-accent/10 border border-accent/30 flex flex-col items-center justify-center p-1 overflow-hidden"
            title={slot.customTitle || t(meta?.labelKey ?? slot.type, slot.type)}
          >
            <span className="text-[9px] font-semibold text-accent truncate w-full text-center leading-tight">
              {slot.customTitle || t(meta?.labelKey ?? slot.type, slot.type)}
            </span>
            <span className="text-[8px] text-gray-400 tabular-nums">{slot.width}×{slot.height}</span>
          </div>
        );
      })}
    </div>
  );
}

// ── Size presets ───────────────────────────────────────────────────────────

const SIZE_PRESETS = [
  { label: 'S', labelKey: 'governance.dashboardBuilder.sizePreset.s', width: 1, height: 1 },
  { label: 'M', labelKey: 'governance.dashboardBuilder.sizePreset.m', width: 2, height: 2 },
  { label: 'W', labelKey: 'governance.dashboardBuilder.sizePreset.w', width: 3, height: 2 },
  { label: 'L', labelKey: 'governance.dashboardBuilder.sizePreset.l', width: 3, height: 3 },
] as const;

// ── Widget Picker Panel ────────────────────────────────────────────────────

const PICKER_PERSONAS = [
  'Engineer',
  'TechLead',
  'Architect',
  'Executive',
  'Product',
  'PlatformAdmin',
  'Auditor',
] as const;

interface WidgetPickerPanelProps {
  selected: WidgetType;
  onSelect: (type: WidgetType) => void;
  disabled?: boolean;
}

function WidgetPickerPanel({ selected, onSelect, disabled }: WidgetPickerPanelProps) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [personaFilter, setPersonaFilter] = useState('');

  const filtered = ALL_WIDGET_TYPES.filter((wt) => {
    const meta = WIDGET_META[wt];
    const label = t(meta.labelKey, wt).toLowerCase();
    const matchesSearch = !search || label.includes(search.toLowerCase());
    const matchesPersona = !personaFilter || meta.personas.includes(personaFilter);
    return matchesSearch && matchesPersona;
  });

  if (disabled) {
    return (
      <button
        type="button"
        disabled
        className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white text-left opacity-50 flex items-center justify-between"
      >
        <span className="truncate">{t(WIDGET_META[selected].labelKey, selected)}</span>
        <ChevronDown size={12} className="shrink-0 ml-1 text-gray-400" />
      </button>
    );
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white text-left flex items-center justify-between hover:border-accent transition-colors"
        aria-label={t('governance.dashboardBuilder.widgetPickerOpen', 'Open widget picker')}
        aria-expanded={open}
        aria-haspopup="dialog"
      >
        <span className="truncate">{t(WIDGET_META[selected].labelKey, selected)}</span>
        <ChevronDown
          size={12}
          className={`shrink-0 ml-1 text-gray-400 transition-transform ${open ? 'rotate-180' : ''}`}
        />
      </button>

      {open && (
        <div
          className="absolute top-full left-0 z-50 mt-1 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-xl p-3 w-72"
          role="dialog"
          aria-label={t('governance.dashboardBuilder.widgetPickerPanel', 'Widget picker')}
        >
          {/* Search input */}
          <div className="relative mb-2">
            <Search
              size={11}
              className="absolute left-2 top-1/2 -translate-y-1/2 text-gray-400 pointer-events-none"
            />
            <input
              autoFocus
              type="text"
              placeholder={t('governance.dashboardBuilder.widgetSearch', 'Search widgets…')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs pl-6 pr-2 py-1.5 text-gray-900 dark:text-white focus:outline-none focus:border-accent"
            />
          </div>

          {/* Persona filter chips */}
          <div className="flex flex-wrap gap-1 mb-2">
            <button
              type="button"
              onClick={() => setPersonaFilter('')}
              className={`rounded-full px-2 py-0.5 text-[9px] font-medium transition-colors ${
                !personaFilter
                  ? 'bg-accent text-white'
                  : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'
              }`}
            >
              {t('governance.dashboardBuilder.allPersonas', 'All')}
            </button>
            {PICKER_PERSONAS.map((p) => (
              <button
                key={p}
                type="button"
                onClick={() => setPersonaFilter(personaFilter === p ? '' : p)}
                className={`rounded-full px-2 py-0.5 text-[9px] font-medium transition-colors ${
                  personaFilter === p
                    ? 'bg-accent text-white'
                    : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'
                }`}
              >
                {p}
              </button>
            ))}
          </div>

          {/* Widget cards grid */}
          <div
            className="max-h-52 overflow-y-auto grid grid-cols-2 gap-1"
            role="listbox"
            aria-label={t('governance.dashboardBuilder.widgetPickerPanel', 'Widget picker')}
          >
            {filtered.map((wt) => {
              const meta = WIDGET_META[wt];
              const isSelected = wt === selected;
              return (
                <button
                  key={wt}
                  type="button"
                  role="option"
                  aria-selected={isSelected}
                  onClick={() => {
                    onSelect(wt);
                    setOpen(false);
                    setSearch('');
                    setPersonaFilter('');
                  }}
                  className={`text-left rounded p-2 text-xs transition-colors border ${
                    isSelected
                      ? 'bg-accent text-white border-accent'
                      : 'bg-gray-50 dark:bg-gray-800 text-gray-700 dark:text-gray-300 border-transparent hover:border-accent/30 hover:bg-accent/5'
                  }`}
                  title={meta.personas.join(', ')}
                >
                  <div className="font-medium truncate leading-tight">
                    {t(meta.labelKey, wt)}
                  </div>
                  <div
                    className={`text-[8px] mt-0.5 truncate ${
                      isSelected ? 'text-white/70' : 'text-gray-400'
                    }`}
                  >
                    {meta.defaultWidth}×{meta.defaultHeight} ·{' '}
                    {meta.personas.slice(0, 2).join(', ')}
                    {meta.personas.length > 2 ? ` +${meta.personas.length - 2}` : ''}
                  </div>
                </button>
              );
            })}
          </div>

          {filtered.length === 0 && (
            <div className="text-center text-xs text-gray-400 py-3">
              {t('governance.dashboardBuilder.noWidgetsFound', 'No widgets match the filter')}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function DashboardBuilderPage() {
  const { t } = useTranslation();
  const { dashboardId } = useParams<{ dashboardId: string }>();
  const navigate = useNavigate();
  const TENANT_ID = 'default';
  const importInputRef = useRef<HTMLInputElement>(null);

  const { data, isLoading, isError, refetch } = useGetDashboard(dashboardId ?? '', TENANT_ID);
  const updateMutation = useUpdateDashboard(dashboardId ?? '');

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [layout, setLayout] = useState<string>('two-column');
  const [slots, setSlots] = useState<BuilderSlot[]>([]);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [initialized, setInitialized] = useState(false);
  const [showPreview, setShowPreview] = useState(true);
  const [importError, setImportError] = useState<string | null>(null);

  // Initialize state from loaded dashboard
  if (data && !initialized) {
    setName(data.name);
    setDescription(data.description ?? '');
    setLayout(data.layout);
    setSlots(data.widgets.map(widgetFromDetail));
    setInitialized(true);
  }

  const maxCols = MAX_COLS[layout] ?? 4;

  const addSlot = () => {
    const meta = WIDGET_META['dora-metrics'];
    setSlots((prev) => [
      ...prev,
      {
        tempId: makeSlotId(),
        type: 'dora-metrics',
        posX: 0,
        posY: prev.length * 2,
        width: meta.defaultWidth,
        height: meta.defaultHeight,
        serviceId: '',
        teamId: '',
        timeRange: '24h',
        customTitle: '',
        metric: 'incidents-open',
        content: '',
      },
    ]);
  };

  const removeSlot = (tempId: string) => {
    setSlots((prev) => prev.filter((s) => s.tempId !== tempId));
  };

  const duplicateSlot = (tempId: string) => {
    setSlots((prev) => {
      const source = prev.find((s) => s.tempId === tempId);
      if (!source) return prev;
      const newSlot: BuilderSlot = {
        ...source,
        tempId: makeSlotId(),
        existingWidgetId: null,
        posY: source.posY + source.height,
      };
      return [...prev, newSlot];
    });
  };

  const updateSlot = (tempId: string, patch: Partial<BuilderSlot>) => {
    setSlots((prev) => prev.map((s) => (s.tempId === tempId ? { ...s, ...patch } : s)));
  };

  /** Auto-arrange: reflows all slots into a tidy grid with no position overlaps */
  const handleAutoArrange = () => {
    const cols = maxCols;
    let curX = 0;
    let curY = 0;
    let rowH = 0;
    const arranged = slots.map((slot) => {
      const w = Math.min(slot.width, cols);
      if (curX + w > cols) {
        curX = 0;
        curY += rowH;
        rowH = 0;
      }
      const placed = { ...slot, posX: curX, posY: curY, width: w };
      curX += w;
      rowH = Math.max(rowH, slot.height);
      return placed;
    });
    setSlots(arranged);
  };

  /** Export: downloads current dashboard config as a JSON file */
  const handleExportJson = () => {
    const payload = {
      version: 1,
      name,
      description,
      layout,
      widgets: slots.map((s) => ({
        type: s.type,
        posX: s.posX,
        posY: s.posY,
        width: s.width,
        height: s.height,
        serviceId: s.serviceId || null,
        teamId: s.teamId || null,
        timeRange: s.timeRange || null,
        customTitle: s.customTitle || null,
        metric: s.metric || null,
        content: s.content || null,
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

  /** Import: parses a JSON file and populates slots */
  const handleImportJson = (e: React.ChangeEvent<HTMLInputElement>) => {
    setImportError(null);
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (ev) => {
      try {
        const json = JSON.parse(ev.target?.result as string);
        if (!Array.isArray(json.widgets)) throw new Error('Invalid format');
        if (json.name) setName(json.name);
        if (json.description) setDescription(json.description);
        if (json.layout) setLayout(json.layout);
        setSlots(
          json.widgets.map((w: Record<string, unknown>) => ({
            tempId: makeSlotId(),
            existingWidgetId: null,
            type: (w.type as WidgetType) ?? 'dora-metrics',
            posX: Number(w.posX ?? 0),
            posY: Number(w.posY ?? 0),
            width: Number(w.width ?? 2),
            height: Number(w.height ?? 2),
            serviceId: String(w.serviceId ?? ''),
            teamId: String(w.teamId ?? ''),
            timeRange: String(w.timeRange ?? '24h'),
            customTitle: String(w.customTitle ?? ''),
            metric: String(w.metric ?? ''),
            content: String(w.content ?? ''),
          }))
        );
      } catch {
        setImportError(t('governance.dashboardBuilder.importError', 'Invalid dashboard JSON file.'));
      }
    };
    reader.readAsText(file);
    // reset file input so the same file can be re-imported
    e.target.value = '';
  };

  const handleSave = async () => {
    setSaveError(null);
    setSaveSuccess(false);
    try {
      await updateMutation.mutateAsync({
        dashboardId,
        tenantId: TENANT_ID,
        name,
        description: description || null,
        layout,
        widgets: slots.map((s) => ({
          existingWidgetId: s.existingWidgetId ?? null,
          type: s.type,
          posX: s.posX,
          posY: s.posY,
          width: s.width,
          height: s.height,
          serviceId: s.serviceId || null,
          teamId: s.teamId || null,
          timeRange: s.timeRange || null,
          customTitle: s.customTitle || null,
          metric: s.metric || null,
          content: s.content || null,
        })),
      });
      setSaveSuccess(true);
      navigate(`/governance/dashboards/${dashboardId}`);
    } catch {
      setSaveError(t('governance.dashboardBuilder.saveError', 'Failed to save dashboard.'));
    }
  };

  if (!dashboardId) {
    return <PageErrorState message={t('governance.dashboardView.notFound', 'Dashboard not found')} onRetry={() => navigate('/governance/custom-dashboards')} />;
  }

  if (isLoading) return <PageLoadingState message={t('governance.dashboardBuilder.loading', 'Loading dashboard editor...')} />;
  if (isError) return <PageErrorState message={t('governance.dashboardBuilder.error', 'Failed to load dashboard')} onRetry={() => refetch()} />;

  const isReadOnly = data?.isSystem;

  return (
    <PageContainer>
      {/* Header */}
      <div className="flex flex-col gap-3 mb-6">
        <Link
          to={`/governance/dashboards/${dashboardId}`}
          className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors"
        >
          <ArrowLeft size={14} />
          {t('governance.dashboardBuilder.backToView', 'Back to Dashboard')}
        </Link>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <LayoutGrid size={20} className="text-accent" />
            <h1 className="text-xl font-bold text-gray-900 dark:text-white">
              {t('governance.dashboardBuilder.title', 'Edit Dashboard')}
            </h1>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setShowPreview((v) => !v)}
              aria-label={t('governance.dashboardBuilder.togglePreview', 'Toggle preview')}
            >
              <Eye size={14} className="mr-1" />
              {showPreview
                ? t('governance.dashboardBuilder.hidePreview', 'Hide Preview')
                : t('governance.dashboardBuilder.showPreview', 'Show Preview')}
            </Button>

            {/* Export JSON */}
            {!isReadOnly && (
              <Button
                size="sm"
                variant="secondary"
                onClick={handleExportJson}
                aria-label={t('governance.dashboardBuilder.exportJson', 'Export JSON')}
              >
                <Download size={14} className="mr-1" />
                {t('governance.dashboardBuilder.exportJson', 'Export JSON')}
              </Button>
            )}

            {/* Import JSON — hidden file input triggered by button */}
            {!isReadOnly && (
              <>
                <input
                  ref={importInputRef}
                  type="file"
                  accept="application/json,.json"
                  className="hidden"
                  aria-hidden="true"
                  onChange={handleImportJson}
                />
                <Button
                  size="sm"
                  variant="secondary"
                  onClick={() => importInputRef.current?.click()}
                  aria-label={t('governance.dashboardBuilder.importJson', 'Import JSON')}
                >
                  <Upload size={14} className="mr-1" />
                  {t('governance.dashboardBuilder.importJson', 'Import JSON')}
                </Button>
              </>
            )}

            {/* Auto-arrange */}
            {!isReadOnly && slots.length > 0 && (
              <Button
                size="sm"
                variant="secondary"
                onClick={handleAutoArrange}
                aria-label={t('governance.dashboardBuilder.autoArrange', 'Auto-arrange')}
              >
                <Shuffle size={14} className="mr-1" />
                {t('governance.dashboardBuilder.autoArrange', 'Auto-arrange')}
              </Button>
            )}

            {!isReadOnly && (
              <Button onClick={handleSave} disabled={updateMutation.isPending}>
                <Save size={14} className="mr-1" />
                {t('governance.dashboardBuilder.save', 'Save Dashboard')}
              </Button>
            )}
          </div>
        </div>
        {isReadOnly && (
          <p className="text-sm text-yellow-600 dark:text-yellow-400">
            {t('governance.dashboardBuilder.systemReadOnly', 'This is a system dashboard and cannot be edited.')}
          </p>
        )}
      </div>

      {/* Dashboard metadata */}
      {!isReadOnly && (
        <Card className="mb-6">
          <CardBody>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.customDashboards.dashboardName', 'Dashboard Name')}
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  maxLength={100}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.customDashboards.layout', 'Layout')}
                </label>
                <select
                  value={layout}
                  onChange={(e) => setLayout(e.target.value)}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                >
                  {LAYOUTS.map((l) => (
                    <option key={l} value={l}>{l}</option>
                  ))}
                </select>
              </div>
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.customDashboards.description', 'Description')}
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                />
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Widget slots + live preview */}
      {importError && (
        <div className="mb-4 rounded border border-red-300 bg-red-50 dark:bg-red-900/20 px-4 py-2 text-sm text-red-700 dark:text-red-300" role="alert">
          {importError}
        </div>
      )}
      {saveError && (
        <div className="mb-4 rounded border border-red-300 bg-red-50 dark:bg-red-900/20 px-4 py-2 text-sm text-red-700 dark:text-red-300" role="alert">
          {saveError}
        </div>
      )}

      <div className={`flex gap-6 ${showPreview ? 'lg:flex-row' : ''} flex-col`}>
        {/* Left: slot editor */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-gray-900 dark:text-white">
              {t('governance.dashboardBuilder.widgets', 'Widgets')} ({slots.length})
            </h2>
            {!isReadOnly && (
              <Button size="sm" variant="secondary" onClick={addSlot} disabled={slots.length >= 20}>
                <Plus size={14} className="mr-1" />
                {t('governance.dashboardBuilder.addWidget', 'Add Widget')}
              </Button>
            )}
          </div>

          {slots.length === 0 ? (
            <div className="border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-lg p-8 text-center text-sm text-gray-400">
              {t('governance.dashboardBuilder.noWidgets', 'No widgets yet. Click "Add Widget" to start building.')}
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              {slots.map((slot, index) => (
                <Card key={slot.tempId}>
                  <CardBody>
                    <div className="flex items-start gap-2">
                      <span className="text-xs font-mono text-gray-400 pt-2 w-5 shrink-0">{index + 1}</span>
                      <div className="flex-1 grid grid-cols-1 gap-3 sm:grid-cols-4">
                        {/* Widget type — visual picker */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.widgetType', 'Widget Type')}
                          </label>
                          <WidgetPickerPanel
                            selected={slot.type}
                            onSelect={(type) => {
                              const meta = WIDGET_META[type];
                              updateSlot(slot.tempId, {
                                type,
                                width: meta.defaultWidth,
                                height: meta.defaultHeight,
                              });
                            }}
                            disabled={isReadOnly}
                          />
                        </div>

                        {/* Position */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.position', 'Position (col, row)')}
                          </label>
                          <div className="flex gap-1">
                            <input
                              type="number"
                              min={0}
                              max={maxCols - 1}
                              value={slot.posX}
                              onChange={(e) => updateSlot(slot.tempId, { posX: Number(e.target.value) })}
                              disabled={isReadOnly}
                              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                              placeholder="Col"
                              aria-label={t('governance.dashboardBuilder.col', 'Column')}
                            />
                            <input
                              type="number"
                              min={0}
                              value={slot.posY}
                              onChange={(e) => updateSlot(slot.tempId, { posY: Number(e.target.value) })}
                              disabled={isReadOnly}
                              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                              placeholder="Row"
                              aria-label={t('governance.dashboardBuilder.row', 'Row')}
                            />
                          </div>
                        </div>

                        {/* Size */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.size', 'Size (w × h)')}
                          </label>
                          <div className="flex gap-1">
                            <input
                              type="number"
                              min={1}
                              max={maxCols}
                              value={slot.width}
                              onChange={(e) => updateSlot(slot.tempId, { width: Number(e.target.value) })}
                              disabled={isReadOnly}
                              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                              aria-label={t('governance.dashboardBuilder.width', 'Width')}
                            />
                            <input
                              type="number"
                              min={1}
                              value={slot.height}
                              onChange={(e) => updateSlot(slot.tempId, { height: Number(e.target.value) })}
                              disabled={isReadOnly}
                              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                              aria-label={t('governance.dashboardBuilder.height', 'Height')}
                            />
                          </div>
                        </div>

                        {/* Size presets */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.sizePresets', 'Quick Size')}
                          </label>
                          <div className="flex gap-1 flex-wrap">
                            {SIZE_PRESETS.map((preset) => {
                              const isActive =
                                slot.width === preset.width && slot.height === preset.height;
                              return (
                                <button
                                  key={preset.label}
                                  type="button"
                                  onClick={() =>
                                    updateSlot(slot.tempId, {
                                      width: preset.width,
                                      height: preset.height,
                                    })
                                  }
                                  disabled={isReadOnly}
                                  aria-pressed={isActive}
                                  className={`rounded px-2 py-0.5 text-[10px] font-semibold transition-colors disabled:opacity-50 ${
                                    isActive
                                      ? 'bg-accent text-white'
                                      : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'
                                  }`}
                                  title={`${preset.width}×${preset.height}`}
                                >
                                  {t(preset.labelKey, preset.label)}{' '}
                                  <span className="font-normal opacity-70">
                                    {preset.width}×{preset.height}
                                  </span>
                                </button>
                              );
                            })}
                          </div>
                        </div>

                        {/* Custom title */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.customTitle', 'Custom Title')}
                          </label>
                          <input
                            type="text"
                            value={slot.customTitle}
                            onChange={(e) => updateSlot(slot.tempId, { customTitle: e.target.value })}
                            disabled={isReadOnly}
                            maxLength={80}
                            placeholder={t('governance.dashboardBuilder.customTitlePlaceholder', 'Optional override')}
                            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                          />
                        </div>

                        {/* Time range override */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.timeRangeOverride', 'Time Range Override')}
                          </label>
                          <select
                            value={slot.timeRange}
                            onChange={(e) => updateSlot(slot.tempId, { timeRange: e.target.value })}
                            disabled={isReadOnly}
                            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                          >
                            {TIME_RANGE_OPTIONS.map((opt) => (
                              <option key={opt.value} value={opt.value}>
                                {t(opt.labelKey, opt.value)}
                              </option>
                            ))}
                          </select>
                        </div>

                        {/* Service filter */}
                        <div>
                          <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                            {t('governance.dashboardBuilder.serviceFilter', 'Service Filter')}
                          </label>
                          <input
                            type="text"
                            value={slot.serviceId}
                            onChange={(e) => updateSlot(slot.tempId, { serviceId: e.target.value })}
                            disabled={isReadOnly}
                            placeholder={t('governance.dashboardBuilder.serviceFilterPlaceholder', 'All services')}
                            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                          />
                        </div>

                         {/* Stat metric selector — only for 'stat' widget type */}
                         {slot.type === 'stat' && (
                           <div>
                             <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                               {t('governance.dashboardBuilder.statMetric', 'KPI Metric')}
                             </label>
                             <select
                               value={slot.metric || 'incidents-open'}
                               onChange={(e) => updateSlot(slot.tempId, { metric: e.target.value })}
                               disabled={isReadOnly}
                               className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                             >
                               {STAT_METRIC_OPTIONS.map((opt) => (
                                 <option key={opt.value} value={opt.value}>
                                   {t(opt.labelKey, opt.value)}
                                 </option>
                               ))}
                             </select>
                           </div>
                         )}

                         {/* Content textarea — only for 'text-markdown' widget type */}
                         {slot.type === 'text-markdown' && (
                           <div className="sm:col-span-3">
                             <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                               {t('governance.dashboardBuilder.textContent', 'Content (Markdown)')}
                             </label>
                             <textarea
                               value={slot.content}
                               onChange={(e) => updateSlot(slot.tempId, { content: e.target.value })}
                               disabled={isReadOnly}
                               rows={4}
                               maxLength={2000}
                               placeholder={t('governance.dashboardBuilder.textContentPlaceholder', 'Supports **bold**, *italic*, # headings, - lists, [label](url)')}
                               className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white font-mono disabled:opacity-50 resize-y"
                             />
                             <p className="mt-1 text-[10px] text-gray-400">
                               {slot.content.length}/2000
                             </p>
                           </div>
                         )}
                      </div>

                      {!isReadOnly && (
                        <div className="flex flex-col gap-1 shrink-0 mt-1">
                          <button
                            onClick={() => duplicateSlot(slot.tempId)}
                            className="p-1 text-gray-400 hover:text-accent transition-colors"
                            aria-label={t('governance.dashboardBuilder.duplicateWidget', 'Duplicate widget')}
                            title={t('governance.dashboardBuilder.duplicateWidget', 'Duplicate widget')}
                          >
                            <Copy size={14} />
                          </button>
                          <button
                            onClick={() => removeSlot(slot.tempId)}
                            className="p-1 text-gray-400 hover:text-red-500 transition-colors"
                            aria-label={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
                          >
                            <Trash2 size={14} />
                          </button>
                        </div>
                      )}
                    </div>
                  </CardBody>
                </Card>
              ))}
            </div>
          )}
        </div>

      {/* Right: live preview */}
        {showPreview && (
          <div className="lg:w-72 shrink-0">
            <div className="sticky top-4">
              <h2 className="text-sm font-semibold text-gray-900 dark:text-white mb-2">
                {t('governance.dashboardBuilder.previewTitle', 'Live Preview')}
              </h2>
              <GridPreview slots={slots} layout={layout} />
              <p className="mt-1 text-xs text-gray-400">
                {t('governance.dashboardBuilder.previewHint', 'Updates as you configure widgets')}
              </p>
            </div>
          </div>
        )}
      </div>

      {saveSuccess && (
        <p className="mt-4 text-sm text-green-600 dark:text-green-400">
          {t('governance.dashboardBuilder.saveSuccess', 'Dashboard saved successfully.')}
        </p>
      )}

      {!isReadOnly && slots.length > 0 && (
        <div className="mt-6">
          <Button onClick={handleSave} disabled={updateMutation.isPending}>
            <Save size={14} className="mr-1" />
            {t('governance.dashboardBuilder.save', 'Save Dashboard')}
          </Button>
        </div>
      )}
    </PageContainer>
  );
}
