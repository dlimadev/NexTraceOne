/**
 * DashboardViewPage — renderiza um dashboard customizado a partir do registry de widgets.
 * Layout via CSS Grid baseado em position.x, position.y, width, height dos widgets.
 * Suporta seletor de período global, seletor de ambiente, auto-refresh, partilha e
 * variáveis de dashboard (estilo Grafana template variables) que sobrepõem serviceId/teamId
 * em todos os widgets.
 * Kiosk/NOC mode: ?kiosk=tv oculta toda a navegação e toolbar para ecrã panorâmico.
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { CrossFilterProvider, useCrossFilter } from '../context/CrossFilterContext';
import { CrossFilterBreadcrumb } from '../components/CrossFilterBreadcrumb';
import { useDashboardLive } from '../hooks/useDashboardLive';
import { getDrillRoute } from '../widgets/drillRoutes';
import {
  ArrowLeft,
  RefreshCw,
  Settings,
  Clock,
  Share2,
  LayoutDashboard,
  SlidersHorizontal,
  ChevronDown,
  ChevronUp,
  Tv,
  X,
  Maximize2,
  MoreVertical,
  BarChart2,
  Download,
  Link as LinkIcon,
  Bell,
} from 'lucide-react';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { Select } from '../../../components/Select';
import { Badge } from '../../../components/Badge';
import { Skeleton } from '../../../components/Skeleton';
import client from '../../../api/client';
import { DoraMetricsWidget } from '../widgets/DoraMetricsWidget';
import { ServiceScorecardWidget } from '../widgets/ServiceScorecardWidget';
import { IncidentSummaryWidget } from '../widgets/IncidentSummaryWidget';
import { ChangeConfidenceWidget } from '../widgets/ChangeConfidenceWidget';
import { CostTrendWidget } from '../widgets/CostTrendWidget';
import { ReliabilitySloWidget } from '../widgets/ReliabilitySloWidget';
import { KnowledgeGraphWidget } from '../widgets/KnowledgeGraphWidget';
import { OnCallStatusWidget } from '../widgets/OnCallStatusWidget';
import { AlertStatusWidget } from '../widgets/AlertStatusWidget';
import { ChangeTimelineWidget } from '../widgets/ChangeTimelineWidget';
import { SloGaugeWidget } from '../widgets/SloGaugeWidget';
import { DeploymentFrequencyWidget } from '../widgets/DeploymentFrequencyWidget';
import { StatWidget } from '../widgets/StatWidget';
import { TextMarkdownWidget } from '../widgets/TextMarkdownWidget';
import { TopServicesWidget } from '../widgets/TopServicesWidget';
import { ContractCoverageWidget } from '../widgets/ContractCoverageWidget';
import { BlastRadiusWidget } from '../widgets/BlastRadiusWidget';
import { TeamHealthWidget } from '../widgets/TeamHealthWidget';
import { ReleaseCalendarWidget } from '../widgets/ReleaseCalendarWidget';
import { QueryWidget } from '../widgets/QueryWidget';
import { OtelMetricsWidget } from '../widgets/OtelMetricsWidget';
import { OtelLogsWidget } from '../widgets/OtelLogsWidget';
import { OtelTracesWidget } from '../widgets/OtelTracesWidget';
import { OtelErrorRateWidget } from '../widgets/OtelErrorRateWidget';
import { OtelServiceMapWidget } from '../widgets/OtelServiceMapWidget';
import { PieChartWidget } from '../widgets/PieChartWidget';
import { BarGaugeWidget } from '../widgets/BarGaugeWidget';
import { HeatmapCalendarWidget } from '../widgets/HeatmapCalendarWidget';
import { TreemapWidget } from '../widgets/TreemapWidget';
import { HistogramWidget } from '../widgets/HistogramWidget';
import { TIME_RANGE_OPTIONS, type WidgetType } from '../widgets/WidgetRegistry';
import type { WidgetProps } from '../widgets/WidgetRegistry';
import type { ComponentType } from 'react';
import { DashboardHistoryDrawer } from '../components/DashboardHistoryDrawer';
import { DashboardSharingModal } from '../components/DashboardSharingModal';
import { TimeRangePicker, parseTimeRange } from '../components/TimeRangePicker';
import { AnnotationsOverlay } from '../components/AnnotationsOverlay';
import { DashboardVariablesPanel } from '../components/DashboardVariablesPanel';
import { KibanaQueryBar } from '../components/KibanaQueryBar';

// ── Widget registry map ────────────────────────────────────────────────────

const WIDGET_MAP: Record<WidgetType, ComponentType<WidgetProps>> = {
  'dora-metrics': DoraMetricsWidget,
  'service-scorecard': ServiceScorecardWidget,
  'incident-summary': IncidentSummaryWidget,
  'change-confidence': ChangeConfidenceWidget,
  'cost-trend': CostTrendWidget,
  'reliability-slo': ReliabilitySloWidget,
  'knowledge-graph': KnowledgeGraphWidget,
  'on-call-status': OnCallStatusWidget,
  'alert-status': AlertStatusWidget,
  'change-timeline': ChangeTimelineWidget,
  'slo-gauge': SloGaugeWidget,
  'deployment-frequency': DeploymentFrequencyWidget,
  'stat': StatWidget,
  'text-markdown': TextMarkdownWidget,
  'top-services': TopServicesWidget,
  'contract-coverage': ContractCoverageWidget,
  'blast-radius': BlastRadiusWidget,
  'team-health': TeamHealthWidget,
  'release-calendar': ReleaseCalendarWidget,
  'query-widget': QueryWidget,
  // Observability widgets
  'obs-metrics': OtelMetricsWidget,
  'obs-logs': OtelLogsWidget,
  'obs-traces': OtelTracesWidget,
  'obs-error-rate': OtelErrorRateWidget,
  'obs-service-map': OtelServiceMapWidget,
  'obs-pie-chart': PieChartWidget,
  'obs-bar-gauge': BarGaugeWidget,
  'obs-heatmap-calendar': HeatmapCalendarWidget,
  'obs-treemap': TreemapWidget,
  'obs-histogram': HistogramWidget,
  // Extended widget types - placeholder components
  'incident-count': StatWidget,
  'mttr-widget': StatWidget,
  'slo-tracker': StatWidget,
  'change-failure-rate': StatWidget,
  'change-score-trend': StatWidget,
  'service-health-matrix': StatWidget,
  'maturity-score': StatWidget,
  'dependency-map': StatWidget,
  'compliance-summary': StatWidget,
  'policy-violations': StatWidget,
  'risk-heatmap': StatWidget,
  'cost-attribution': StatWidget,
  'finops-summary': StatWidget,
  'tech-debt-trend': StatWidget,
  'executive-kpis': StatWidget,
};

// ── Types ──────────────────────────────────────────────────────────────────

interface WidgetSlot {
  widgetId: string;
  type: string;
  posX: number;
  posY: number;
  width: number;
  height: number;
  effectiveServiceId?: string | null;
  effectiveTeamId?: string | null;
  effectiveTimeRange: string;
  customTitle?: string | null;
  metric?: string | null;
  content?: string | null;
  nqlQuery?: string | null;
  renderHint?: string | null;
}

interface RenderDataResponse {
  dashboardId: string;
  name: string;
  description?: string | null;
  layout: string;
  persona: string;
  environmentId?: string | null;
  globalTimeRange: string;
  widgets: WidgetSlot[];
  generatedAt: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────

const useRenderData = (dashboardId: string, tenantId: string, environmentId?: string | null, timeRange?: string) =>
  useQuery({
    queryKey: ['dashboard-render-data', dashboardId, tenantId, environmentId, timeRange],
    queryFn: () =>
      client
        .get<RenderDataResponse>(`/governance/dashboards/${dashboardId}/render-data`, {
          params: { tenantId, environmentId, timeRange },
        })
        .then((r) => r.data),
    enabled: Boolean(dashboardId),
  });

// useShareDashboard removed in V3.1 — replaced by DashboardSharingModal (granular policy)

// ── CSS Grid helpers ───────────────────────────────────────────────────────

/** Convert widget position into CSS grid placement style */
function widgetGridStyle(widget: WidgetSlot) {
  return {
    gridColumn: `${widget.posX + 1} / span ${widget.width}`,
    gridRow: `${widget.posY + 1} / span ${widget.height}`,
  };
}

/** Derive grid template columns from layout string */
function layoutToGridCols(layout: string): string {
  switch (layout) {
    case 'single-column': return 'grid-cols-1';
    case 'two-column': return 'grid-cols-2';
    case 'three-column': return 'grid-cols-3';
    case 'grid': return 'grid-cols-4';
    case 'custom': return 'grid-cols-4';
    default: return 'grid-cols-2';
  }
}

// ── Widget Actions Menu ────────────────────────────────────────────────────

interface WidgetActionsMenuProps {
  widgetId: string;
  dashboardId: string;
  slot: WidgetSlot;
  onClose: () => void;
  onInspect: () => void;
  navigate: ReturnType<typeof import('react-router-dom').useNavigate>;
  t: ReturnType<typeof import('react-i18next').useTranslation>['t'];
}

function WidgetActionsMenu({ widgetId, dashboardId, slot, onClose, onInspect, navigate, t }: WidgetActionsMenuProps) {
  const menuRef = useRef<HTMLDivElement>(null);

  // Fecha ao clicar fora
  useEffect(() => {
    function handleOutside(e: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        onClose();
      }
    }
    document.addEventListener('mousedown', handleOutside);
    return () => document.removeEventListener('mousedown', handleOutside);
  }, [onClose]);

  function handleDownloadJson() {
    const blob = new Blob([JSON.stringify(slot, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `widget-${widgetId}.json`;
    a.click();
    URL.revokeObjectURL(url);
    onClose();
  }

  function handleCopyLink() {
    navigator.clipboard.writeText(`${window.location.href.split('#')[0]}#widget-${widgetId}`).catch(() => {});
    onClose();
  }

  function handleCreateAlert() {
    navigate(`/governance/dashboards/${dashboardId}/monitors/new?widgetId=${widgetId}`);
    onClose();
  }

  function handleEditWidget() {
    navigate(`/governance/dashboards/${dashboardId}/edit`);
    onClose();
  }

  return (
    <div
      ref={menuRef}
      role="menu"
      className="absolute top-6 right-0 z-50 w-48 rounded-lg border border-edge bg-card shadow-xl"
    >
      <button
        type="button"
        role="menuitem"
        onClick={onInspect}
        className="flex w-full items-center gap-2 px-3 py-2 text-xs text-body hover:bg-hover dark:hover:bg-elevated cursor-pointer"
      >
        <BarChart2 size={12} className="shrink-0" />
        {t('governance.dashboardView.widgetMenu.inspect', 'Inspect')}
      </button>
      <button
        type="button"
        role="menuitem"
        onClick={handleDownloadJson}
        className="flex w-full items-center gap-2 px-3 py-2 text-xs text-body hover:bg-hover dark:hover:bg-elevated cursor-pointer"
      >
        <Download size={12} className="shrink-0" />
        {t('governance.dashboardView.widgetMenu.downloadCsv', 'Download CSV')}
      </button>
      <button
        type="button"
        role="menuitem"
        onClick={handleCopyLink}
        className="flex w-full items-center gap-2 px-3 py-2 text-xs text-body hover:bg-hover dark:hover:bg-elevated cursor-pointer"
      >
        <LinkIcon size={12} className="shrink-0" />
        {t('governance.dashboardView.widgetMenu.copyLink', 'Copy link')}
      </button>
      <button
        type="button"
        role="menuitem"
        onClick={handleCreateAlert}
        className="flex w-full items-center gap-2 px-3 py-2 text-xs text-body hover:bg-hover dark:hover:bg-elevated cursor-pointer"
      >
        <Bell size={12} className="shrink-0" />
        {t('governance.dashboardView.widgetMenu.createAlert', 'Create alert')}
      </button>
      <button
        type="button"
        role="menuitem"
        onClick={handleEditWidget}
        className="flex w-full items-center gap-2 px-3 py-2 text-xs text-body hover:bg-hover dark:hover:bg-elevated cursor-pointer"
      >
        <Settings size={12} className="shrink-0" />
        {t('governance.dashboardView.widgetMenu.editWidget', 'Edit widget')}
      </button>
    </div>
  );
}

// ── Widget Inspect Modal ───────────────────────────────────────────────────

interface WidgetInspectModalProps {
  slot: WidgetSlot;
  onClose: () => void;
  t: ReturnType<typeof import('react-i18next').useTranslation>['t'];
}

function WidgetInspectModal({ slot, onClose, t }: WidgetInspectModalProps) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4"
      role="dialog"
      aria-modal="true"
      aria-label={t('governance.dashboardView.inspectWidget', 'Inspect widget')}
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <div className="relative w-full max-w-lg rounded-md border border-edge bg-card shadow-2xl overflow-auto max-h-[80vh]">
        <div className="flex items-center justify-between border-b border-edge px-4 py-3">
          <div className="flex items-center gap-2">
            <BarChart2 size={14} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('governance.dashboardView.inspectTitle', 'Widget Inspector')}
            </h3>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1 rounded-full text-faded hover:text-heading transition-colors"
            aria-label={t('governance.dashboardView.closeInspect', 'Close inspector')}
          >
            <X size={14} />
          </button>
        </div>
        <div className="p-4">
          <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-xs">
            {([
              ['widgetId', slot.widgetId],
              ['type', slot.type],
              ['posX', slot.posX],
              ['posY', slot.posY],
              ['width', slot.width],
              ['height', slot.height],
              ['effectiveServiceId', slot.effectiveServiceId ?? '—'],
              ['effectiveTeamId', slot.effectiveTeamId ?? '—'],
              ['effectiveTimeRange', slot.effectiveTimeRange],
              ['customTitle', slot.customTitle ?? '—'],
              ['metric', slot.metric ?? '—'],
              ['nqlQuery', slot.nqlQuery ?? '—'],
              ['renderHint', slot.renderHint ?? '—'],
            ] as [string, string | number][]).map(([key, val]) => (
              <div key={key} className="contents">
                <dt className="text-muted font-medium">{key}</dt>
                <dd className="text-heading font-mono truncate">{String(val)}</dd>
              </div>
            ))}
          </dl>
          <div className="mt-4 rounded bg-elevated p-2">
            <p className="text-[10px] font-semibold text-faded uppercase tracking-wider mb-1">
              {t('governance.dashboardView.inspectRawJson', 'Raw JSON')}
            </p>
            <pre className="text-[10px] text-body overflow-x-auto whitespace-pre-wrap">
              {JSON.stringify(slot, null, 2)}
            </pre>
          </div>
        </div>
      </div>
    </div>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

const AUTO_REFRESH_OPTIONS = [
  { value: 0,   label: 'Off' },
  { value: 10,  label: '10s' },
  { value: 30,  label: '30s' },
  { value: 60,  label: '1m' },
  { value: 300, label: '5m' },
];

export function DashboardViewPage() {
  return (
    <CrossFilterProvider>
      <DashboardViewInner />
    </CrossFilterProvider>
  );
}

function DashboardViewInner() {
  const { t } = useTranslation();
  const { dashboardId } = useParams<{ dashboardId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { activeEnvironmentId } = useEnvironment();
  const TENANT_ID = 'default';
  const { filter: crossFilter, hasFilter: hasCrossFilter, applyFilter: applyCrossFilter } = useCrossFilter();

  const isKiosk = searchParams.get('kiosk') === 'tv';

  // ── Deep-link: initialise from URL params ──────────────────────────────
  const [timeRange, setTimeRange] = useState(() => searchParams.get('timeRange') ?? '24h');
  const [autoRefreshSeconds, setAutoRefreshSeconds] = useState(0);
  // Dashboard variables — Grafana-style global overrides applied to all widgets
  const [varService, setVarService] = useState(() => searchParams.get('service') ?? '');
  const [varTeam, setVarTeam] = useState(() => searchParams.get('team') ?? '');
  const [dashboardVariables, setDashboardVariables] = useState<Record<string, string[]>>({});
  const [kibanaQuery, setKibanaQuery] = useState(() => searchParams.get('q') ?? '');
  const [showVars, setShowVars] = useState(false);
  // Fullscreen expand: stores the widgetId of the widget being expanded (null = closed)
  const [expandedWidgetId, setExpandedWidgetId] = useState<string | null>(null);
  // V3.1 — History drawer + Sharing modal
  const [showHistory, setShowHistory] = useState(false);
  const [showSharingModal, setShowSharingModal] = useState(false);
  // V3.3 — Live toggle
  const [isLiveEnabled, setIsLiveEnabled] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const qc = useQueryClient();
  // Widget context menu — stores the widgetId with an open menu (null = closed)
  const [widgetMenuOpenId, setWidgetMenuOpenId] = useState<string | null>(null);
  // Widget inspect modal — stores widgetId being inspected
  const [inspectWidgetId, setInspectWidgetId] = useState<string | null>(null);
  const gridRef = useRef<HTMLDivElement>(null);

  // ── Deep-link: sync state → URL params ────────────────────────────────
  useEffect(() => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev);
      if (timeRange !== '24h') next.set('timeRange', timeRange); else next.delete('timeRange');
      if (varService) next.set('service', varService); else next.delete('service');
      if (varTeam) next.set('team', varTeam); else next.delete('team');
      if (crossFilter.serviceId) next.set('filterService', crossFilter.serviceId); else next.delete('filterService');
      if (crossFilter.teamId) next.set('filterTeam', crossFilter.teamId); else next.delete('filterTeam');
      if (crossFilter.from) next.set('filterFrom', crossFilter.from); else next.delete('filterFrom');
      if (crossFilter.to) next.set('filterTo', crossFilter.to); else next.delete('filterTo');
      if (kibanaQuery) next.set('q', kibanaQuery); else next.delete('q');
      // Persist dashboard variables to URL
      Object.entries(dashboardVariables).forEach(([key, vals]) => {
        if (vals.length > 0) next.set(`var_${key}`, vals.join(','));
        else next.delete(`var_${key}`);
      });
      return next;
    }, { replace: true });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [timeRange, varService, varTeam, crossFilter.serviceId, crossFilter.teamId, crossFilter.from, crossFilter.to, dashboardVariables, kibanaQuery]);

  // ── V3.3 live hook ────────────────────────────────────────────────────
  const { isLive, isSimulated: liveSimulated, error: liveError, reconnect: liveReconnect } = useDashboardLive({
    dashboardId: dashboardId ?? '',
    tenantId: TENANT_ID,
    enabled: isLiveEnabled && Boolean(dashboardId),
  });

  const { data, isLoading, isError, refetch } = useRenderData(
    dashboardId ?? '',
    TENANT_ID,
    activeEnvironmentId,
    timeRange,
  );

  // ── Auto-refresh ──────────────────────────────────────────────────────
  const doRefresh = useCallback(() => {
    qc.invalidateQueries({ queryKey: ['dashboard-render-data', dashboardId] });
    // Also invalidate all widget queries
    qc.invalidateQueries({ queryKey: ['widget-'] });
  }, [qc, dashboardId]);

  useEffect(() => {
    if (intervalRef.current) clearInterval(intervalRef.current);
    if (autoRefreshSeconds > 0) {
      intervalRef.current = setInterval(doRefresh, autoRefreshSeconds * 1000);
    }
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [autoRefreshSeconds, doRefresh]);

  const handleToggleKiosk = () => {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev);
      if (isKiosk) {
        next.delete('kiosk');
      } else {
        next.set('kiosk', 'tv');
      }
      return next;
    });
  };

  if (!dashboardId) {
    return <PageErrorState message={t('governance.dashboardView.notFound', 'Dashboard not found')} onRetry={() => navigate('/governance/custom-dashboards')} />;
  }

  if (isLoading) return <PageLoadingState message={t('governance.dashboardView.loading', 'Loading dashboard...')} />;
  if (isError || !data) {
    return <PageErrorState message={t('governance.dashboardView.error', 'Failed to load dashboard')} onRetry={() => refetch()} />;
  }

  const gridColsClass = layoutToGridCols(data.layout);

  // ── Kiosk/NOC mode: full-screen widget grid with minimal chrome ────────────
  if (isKiosk) {
    return (
      <div className="min-h-screen bg-canvas p-4">
        {/* Kiosk toolbar — only time range + exit */}
        <div className="flex items-center justify-between mb-3">
          <span className="text-sm font-semibold text-white flex items-center gap-2">
            <Tv size={16} className="text-accent" />
            {data.name}
          </span>
          <div className="flex items-center gap-2">
            <Select
              size="sm"
              value={timeRange}
              onChange={(e) => setTimeRange(e.target.value)}
              aria-label={t('governance.dashboardView.timeRangeLabel', 'Time range')}
              options={TIME_RANGE_OPTIONS.map((opt) => ({
                value: opt.value,
                label: t(opt.labelKey, opt.value),
              }))}
            />
            <Button
              size="sm"
              variant="secondary"
              onClick={() => refetch()}
              aria-label={t('governance.dashboardView.refresh', 'Refresh')}
            >
              <RefreshCw size={12} />
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={handleToggleKiosk}
              aria-label={t('governance.dashboardView.exitKiosk', 'Exit TV Mode')}
            >
              <X size={12} />
              <span className="ml-1">{t('governance.dashboardView.exitKiosk', 'Exit TV Mode')}</span>
            </Button>
          </div>
        </div>

        {data.widgets.length === 0 ? (
          <div className="flex items-center justify-center h-48 text-sm text-muted">
            {t('governance.dashboardView.noWidgets', 'No widgets configured. Click Edit to add widgets.')}
          </div>
        ) : (
          <div className={`grid gap-3 auto-rows-[180px] ${gridColsClass}`}>
            {data.widgets.map((slot) => {
              const WidgetComponent = WIDGET_MAP[slot.type as WidgetType];
              const style = widgetGridStyle(slot);
              const resolvedServiceId = varService || slot.effectiveServiceId;
              const resolvedTeamId = varTeam || slot.effectiveTeamId;
              return (
                <div
                  key={slot.widgetId}
                  style={style}
                  className="rounded-lg border border-gray-700 bg-canvas shadow overflow-hidden"
                >
                  {WidgetComponent ? (
                    <WidgetComponent
                      widgetId={slot.widgetId}
                      config={{
                        serviceId: resolvedServiceId,
                        teamId: resolvedTeamId,
                        timeRange: slot.effectiveTimeRange,
                        customTitle: slot.customTitle,
                        metric: slot.metric,
                        content: slot.content,
                        nqlQuery: slot.nqlQuery,
                        renderHint: slot.renderHint,
                      }}
                      environmentId={activeEnvironmentId}
                      timeRange={slot.effectiveTimeRange}
                      title={slot.customTitle}
                      onCrossFilter={(f) => applyCrossFilter({ ...f, sourceWidgetId: slot.widgetId })}
                      onDrillDown={(path) => navigate(path)}
                      activeCrossFilter={hasCrossFilter ? crossFilter : null}
                    />
                  ) : (
                    <div className="h-full flex items-center justify-center p-2">
                      <Skeleton variant="rectangular" className="h-full w-full" />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    );
  }

  return (
    <PageContainer>
      {/* Back + Header controls */}
      <div className="flex flex-col gap-4 mb-6">
        <Link
          to="/governance/custom-dashboards"
          className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors"
        >
          <ArrowLeft size={14} />
          {t('governance.dashboardView.backToDashboards', 'Back to Dashboards')}
        </Link>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-2">
              <LayoutDashboard size={20} className="text-accent" />
              <h1 className="text-xl font-bold text-heading">{data.name}</h1>
              <Badge variant="secondary">{data.persona}</Badge>
            </div>
            {data.description && (
              <p className="text-sm text-muted ml-7 leading-snug">
                {data.description}
              </p>
            )}
          </div>

          {/* Global controls */}
          <div className="flex flex-wrap items-center gap-2">
            {/* Time range */}
            <TimeRangePicker value={timeRange} onChange={setTimeRange} />

            {/* Auto-refresh */}
            <Select
              size="sm"
              value={String(autoRefreshSeconds)}
              onChange={(e) => setAutoRefreshSeconds(Number(e.target.value))}
              aria-label={t('governance.dashboardView.autoRefreshLabel', 'Auto-refresh')}
              options={AUTO_REFRESH_OPTIONS.map((opt) => ({
                value: String(opt.value),
                label: opt.label === 'Off'
                  ? t('governance.dashboardView.autoRefreshOff', 'Auto-refresh Off')
                  : `${t('governance.dashboardView.autoRefreshEvery', 'Every')} ${opt.label}`,
              }))}
            />

            {/* Refresh */}
            <Button
              size="sm"
              variant="secondary"
              onClick={() => refetch()}
              aria-label={t('governance.dashboardView.refresh', 'Refresh')}
            >
              <RefreshCw size={12} />
            </Button>

            {/* Live toggle (V3.3) */}
            <button
              type="button"
              onClick={() => setIsLiveEnabled(v => !v)}
              aria-label={isLiveEnabled ? t('dashboardLive.liveOn', 'Live updates on') : t('dashboardLive.liveOff', 'Live updates off')}
              aria-pressed={isLiveEnabled}
              className={`flex items-center gap-1 rounded border px-2 py-1 text-xs transition-colors ${
                isLiveEnabled
                  ? isLive
                    ? 'border-green-600 bg-green-950 text-green-300'
                    : 'border-yellow-600 bg-yellow-950 text-yellow-300 animate-pulse'
                  : 'border-edge bg-card text-muted hover:text-accent'
              }`}
            >
              <span className={`inline-block h-2 w-2 rounded-full ${isLiveEnabled && isLive ? 'bg-green-400' : isLiveEnabled ? 'bg-yellow-400' : 'bg-elevated'}`} />
              <span>{t('dashboardLive.liveToggle', 'Live')}</span>
            </button>

            {/* History (V3.1) */}
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setShowHistory(true)}
              aria-label={t('dashboardHistory.title', 'Dashboard History')}
            >
              <Clock size={12} />
              <span className="ml-1">{t('dashboardHistory.title', 'History')}</span>
            </Button>

            {/* Share (V3.1 — granular) */}
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setShowSharingModal(true)}
            >
              <Share2 size={12} />
              <span className="ml-1">{t('governance.dashboardView.share', 'Share')}</span>
            </Button>

            {/* Edit */}
            <Button
              size="sm"
              variant="secondary"
              onClick={() => navigate(`/governance/dashboards/${dashboardId}/edit`)}
            >
              <Settings size={12} />
              <span className="ml-1">{t('governance.dashboardView.edit', 'Edit')}</span>
            </Button>

            {/* TV / Kiosk mode */}
            <Button
              size="sm"
              variant="secondary"
              onClick={handleToggleKiosk}
              aria-label={t('governance.dashboardView.tvMode', 'TV Mode')}
            >
              <Tv size={12} />
              <span className="ml-1">{t('governance.dashboardView.tvMode', 'TV Mode')}</span>
            </Button>

            {/* Variables toggle */}
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setShowVars((v) => !v)}
              aria-label={t('governance.dashboardView.toggleVars', 'Toggle variables')}
              aria-expanded={showVars}
            >
              <SlidersHorizontal size={12} />
              <span className="ml-1">{t('governance.dashboardView.variables', 'Variables')}</span>
              {showVars ? <ChevronUp size={10} className="ml-0.5" /> : <ChevronDown size={10} className="ml-0.5" />}
            </Button>
          </div>
        </div>
      </div>

      {/* Kibana-style Query Bar */}
      <div className="mb-4">
        <KibanaQueryBar
          value={kibanaQuery}
          onChange={setKibanaQuery}
          onSubmit={(q) => {
            setKibanaQuery(q);
            // Invalidate all widget queries to re-fetch with new query context
            qc.invalidateQueries({ queryKey: ['widget-'] });
          }}
        />
      </div>

      {/* Dashboard Variables panel — Grafana-style dynamic dropdowns */}
      {showVars && (
        <DashboardVariablesPanel
          dashboardId={dashboardId!}
          tenantId={TENANT_ID}
          environmentId={activeEnvironmentId}
          values={dashboardVariables}
          onChange={(key, vals) => setDashboardVariables(prev => ({ ...prev, [key]: vals }))}
          onClearAll={() => setDashboardVariables({})}
        />
      )}

      {/* V3.3 — Live error banner */}
      {isLiveEnabled && liveError && (
        <div className="mb-3 flex items-center justify-between rounded-md border border-yellow-700 bg-yellow-950 px-3 py-2 text-xs text-yellow-300">
          <span>⚠ {t('dashboardLive.errorBanner', 'Live connection lost')} — {liveError}</span>
          <button type="button" onClick={liveReconnect} className="ml-4 underline hover:no-underline">
            {t('dashboardLive.reconnect', 'Reconnect')}
          </button>
        </div>
      )}

      {/* V3.3 — Live simulated banner */}
      {isLiveEnabled && isLive && liveSimulated && (
        <div className="mb-3 rounded-md border border-blue-800 bg-blue-950/50 px-3 py-1.5 text-xs text-blue-300">
          ⚠ {t('dashboardLive.simulatedBanner', 'Live data is simulated')} — {t('dashboardLive.simulatedNote', 'Connect real-time ingestion to enable live widget updates.')}
        </div>
      )}

      {/* V3.3 — Cross-filter breadcrumb */}
      {hasCrossFilter && (
        <div className="mb-3">
          <CrossFilterBreadcrumb />
        </div>
      )}

      {/* Widget grid */}
      {data.widgets.length === 0 ? (
        <div className="flex items-center justify-center h-48 text-sm text-faded">
          {t('governance.dashboardView.noWidgets', 'No widgets configured. Click Edit to add widgets.')}
        </div>
      ) : (
        <div className="relative">
          {/* Annotations overlay — positioned relative to the grid container */}
          <div className="absolute top-0 right-0 z-10">
            {(() => {
              try {
                const { from, to } = parseTimeRange(timeRange);
                return (
                  <AnnotationsOverlay
                    tenantId={TENANT_ID}
                    from={from.toISOString()}
                    to={to.toISOString()}
                    serviceNames={varService ? [varService] : undefined}
                    enabled={Boolean(dashboardId)}
                  />
                );
              } catch {
                return null;
              }
            })()}
          </div>

          <div ref={gridRef} className={`grid gap-4 auto-rows-[160px] ${gridColsClass}`}>
            {data.widgets.map((slot) => {
              const WidgetComponent = WIDGET_MAP[slot.type as WidgetType];
              const style = widgetGridStyle(slot);
              // Variables override: if a variable is set it takes precedence over the slot's effectiveServiceId/TeamId
              const resolvedServiceId = varService || slot.effectiveServiceId;
              const resolvedTeamId = varTeam || slot.effectiveTeamId;
              // V3.3 — Drill-down destination for this widget
              const drillDest = getDrillRoute(slot.type as WidgetType, {
                serviceId: resolvedServiceId,
                teamId: resolvedTeamId,
                environmentId: activeEnvironmentId,
                from: crossFilter.from,
                to: crossFilter.to,
              });
              const isMenuOpen = widgetMenuOpenId === slot.widgetId;

              return (
                <div
                  key={slot.widgetId}
                  id={`widget-${slot.widgetId}`}
                  style={style}
                  className="group relative rounded-lg border border-edge bg-card shadow-sm overflow-hidden"
                >
                  {WidgetComponent ? (
                    <WidgetComponent
                      widgetId={slot.widgetId}
                      config={{
                        serviceId: resolvedServiceId,
                        teamId: resolvedTeamId,
                        timeRange: slot.effectiveTimeRange,
                        customTitle: slot.customTitle,
                        metric: slot.metric,
                        content: slot.content,
                        nqlQuery: slot.nqlQuery,
                        renderHint: slot.renderHint,
                      }}
                      environmentId={activeEnvironmentId}
                      timeRange={slot.effectiveTimeRange}
                      title={slot.customTitle}
                      onCrossFilter={(f) => applyCrossFilter({ ...f, sourceWidgetId: slot.widgetId })}
                      onDrillDown={(path) => navigate(path)}
                      activeCrossFilter={hasCrossFilter ? crossFilter : null}
                    />
                  ) : (
                    <div className="h-full flex items-center justify-center p-2">
                      <Skeleton variant="rectangular" className="h-full w-full" />
                    </div>
                  )}
                  {/* Hover controls row: Drill-down + Expand + Widget actions menu */}
                  <div className="absolute top-1 right-1 flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity focus-within:opacity-100">
                    {drillDest && (
                      <button
                        type="button"
                        onClick={() => navigate(drillDest.path)}
                        className="p-1 rounded bg-white/80 dark:bg-canvas/80 text-faded hover:text-accent text-xs"
                        title={t(drillDest.labelKey, drillDest.label)}
                        aria-label={t(drillDest.labelKey, drillDest.label)}
                      >
                        ↗
                      </button>
                    )}
                    <button
                      type="button"
                      onClick={() => setExpandedWidgetId(slot.widgetId)}
                      className="p-1 rounded bg-white/80 dark:bg-canvas/80 text-faded hover:text-accent"
                      aria-label={t('governance.dashboardView.expandWidget', 'Expand widget')}
                    >
                      <Maximize2 size={12} />
                    </button>
                    {/* Widget actions menu button */}
                    <div className="relative">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setWidgetMenuOpenId(isMenuOpen ? null : slot.widgetId);
                        }}
                        className="p-1 rounded bg-white/80 dark:bg-canvas/80 text-faded hover:text-accent"
                        aria-label={t('governance.dashboardView.widgetActions', 'Widget actions')}
                        aria-haspopup="menu"
                        aria-expanded={isMenuOpen}
                      >
                        <MoreVertical size={12} />
                      </button>
                      {/* Widget actions dropdown */}
                      {isMenuOpen && (
                        <WidgetActionsMenu
                          widgetId={slot.widgetId}
                          dashboardId={dashboardId!}
                          slot={slot}
                          onClose={() => setWidgetMenuOpenId(null)}
                          onInspect={() => { setInspectWidgetId(slot.widgetId); setWidgetMenuOpenId(null); }}
                          navigate={navigate}
                          t={t}
                        />
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Fullscreen widget overlay modal */}
      {expandedWidgetId && (() => {
        const slot = data.widgets.find((w) => w.widgetId === expandedWidgetId);
        if (!slot) return null;
        const WidgetComponent = WIDGET_MAP[slot.type as WidgetType];
        const resolvedServiceId = varService || slot.effectiveServiceId;
        const resolvedTeamId = varTeam || slot.effectiveTeamId;
        return (
          <div
            className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4"
            role="dialog"
            aria-modal="true"
            aria-label={t('governance.dashboardView.expandedWidget', 'Expanded widget')}
            onClick={(e) => { if (e.target === e.currentTarget) setExpandedWidgetId(null); }}
          >
            <div className="relative w-full max-w-3xl max-h-[80vh] rounded-md border border-edge bg-card shadow-2xl overflow-auto">
              <button
                onClick={() => setExpandedWidgetId(null)}
                className="absolute top-3 right-3 p-1.5 rounded-full bg-elevated text-muted hover:text-heading transition-colors z-10"
                aria-label={t('governance.dashboardView.closeExpanded', 'Close expanded view')}
              >
                <X size={16} />
              </button>
              <div className="p-6 min-h-[320px]">
                {WidgetComponent ? (
                  <WidgetComponent
                    widgetId={slot.widgetId}
                    config={{
                      serviceId: resolvedServiceId,
                      teamId: resolvedTeamId,
                      timeRange: slot.effectiveTimeRange,
                      customTitle: slot.customTitle,
                      metric: slot.metric,
                      content: slot.content,
                      nqlQuery: slot.nqlQuery,
                      renderHint: slot.renderHint,
                    }}
                    environmentId={activeEnvironmentId}
                    timeRange={slot.effectiveTimeRange}
                    title={slot.customTitle}
                    onCrossFilter={(f) => applyCrossFilter({ ...f, sourceWidgetId: slot.widgetId })}
                    onDrillDown={(path) => { setExpandedWidgetId(null); navigate(path); }}
                    activeCrossFilter={hasCrossFilter ? crossFilter : null}
                  />
                ) : (
                  <div className="h-full flex items-center justify-center p-2">
                    <Skeleton variant="rectangular" className="h-64 w-full" />
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })()}

      <p className="mt-4 text-xs text-faded text-right">
        {t('governance.dashboardView.generatedAt', 'Generated at')}{' '}
        {new Date(data.generatedAt).toLocaleTimeString()}
      </p>

      {/* V3.1 — History Drawer */}
      <DashboardHistoryDrawer
        dashboardId={dashboardId}
        tenantId={TENANT_ID}
        currentRevisionNumber={0}
        isOpen={showHistory}
        onClose={() => setShowHistory(false)}
      />

      {/* V3.1 — Sharing Modal */}
      <DashboardSharingModal
        dashboardId={dashboardId}
        tenantId={TENANT_ID}
        currentScope={0}
        currentPermission={0}
        isOpen={showSharingModal}
        onClose={() => setShowSharingModal(false)}
      />

      {/* Widget Inspect Modal */}
      {inspectWidgetId && (() => {
        const slot = data.widgets.find((w) => w.widgetId === inspectWidgetId);
        if (!slot) return null;
        return (
          <WidgetInspectModal
            slot={slot}
            onClose={() => setInspectWidgetId(null)}
            t={t}
          />
        );
      })()}
    </PageContainer>
  );
}
