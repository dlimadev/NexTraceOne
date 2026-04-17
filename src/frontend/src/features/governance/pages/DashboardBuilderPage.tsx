/**
 * DashboardBuilderPage — editor de dashboard por slots (Fase 3, Opção A).
 * Interface slot-based: utilizador seleciona posição num grid fixo e atribui um widget.
 * Cada slot tem: tipo, serviço alvo, time range override, título personalizado.
 * Preview em tempo real à direita. Guarda via PUT /governance/dashboards/{id}.
 */
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Plus,
  Trash2,
  Save,
  LayoutGrid,
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
  };
}

// ── Component ──────────────────────────────────────────────────────────────

export function DashboardBuilderPage() {
  const { t } = useTranslation();
  const { dashboardId } = useParams<{ dashboardId: string }>();
  const navigate = useNavigate();
  const TENANT_ID = 'default';

  const { data, isLoading, isError, refetch } = useGetDashboard(dashboardId ?? '', TENANT_ID);
  const updateMutation = useUpdateDashboard(dashboardId ?? '');

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [layout, setLayout] = useState<string>('two-column');
  const [slots, setSlots] = useState<BuilderSlot[]>([]);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [initialized, setInitialized] = useState(false);

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
      },
    ]);
  };

  const removeSlot = (tempId: string) => {
    setSlots((prev) => prev.filter((s) => s.tempId !== tempId));
  };

  const updateSlot = (tempId: string, patch: Partial<BuilderSlot>) => {
    setSlots((prev) => prev.map((s) => (s.tempId === tempId ? { ...s, ...patch } : s)));
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
          {!isReadOnly && (
            <Button onClick={handleSave} disabled={updateMutation.isPending}>
              <Save size={14} className="mr-1" />
              {t('governance.dashboardBuilder.save', 'Save Dashboard')}
            </Button>
          )}
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

      {/* Widget slots */}
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
                  <div className="flex-1 grid grid-cols-1 gap-3 sm:grid-cols-3">
                    {/* Widget type */}
                    <div>
                      <label className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                        {t('governance.dashboardBuilder.widgetType', 'Widget Type')}
                      </label>
                      <select
                        value={slot.type}
                        onChange={(e) => updateSlot(slot.tempId, { type: e.target.value as WidgetType })}
                        disabled={isReadOnly}
                        className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1.5 text-gray-900 dark:text-white disabled:opacity-50"
                      >
                        {ALL_WIDGET_TYPES.map((wt) => (
                          <option key={wt} value={wt}>
                            {t(WIDGET_META[wt].labelKey, wt)}
                          </option>
                        ))}
                      </select>
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
                  </div>

                  {!isReadOnly && (
                    <button
                      onClick={() => removeSlot(slot.tempId)}
                      className="p-1 text-gray-400 hover:text-red-500 transition-colors shrink-0 mt-1"
                      aria-label={t('governance.dashboardBuilder.removeWidget', 'Remove widget')}
                    >
                      <Trash2 size={14} />
                    </button>
                  )}
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}

      {saveError && (
        <p className="mt-4 text-sm text-red-600 dark:text-red-400">{saveError}</p>
      )}
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
