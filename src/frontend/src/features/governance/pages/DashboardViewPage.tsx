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
} from 'lucide-react';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
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
import { TIME_RANGE_OPTIONS, type WidgetType } from '../widgets/WidgetRegistry';
import type { WidgetProps } from '../widgets/WidgetRegistry';
import type { ComponentType } from 'react';
import { DashboardHistoryDrawer } from '../components/DashboardHistoryDrawer';
import { DashboardSharingModal } from '../components/DashboardSharingModal';

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

// ── Component ──────────────────────────────────────────────────────────────

const AUTO_REFRESH_OPTIONS = [
  { value: 0, label: 'Off' },
  { value: 30, label: '30s' },
  { value: 60, label: '60s' },
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
      return next;
    }, { replace: true });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [timeRange, varService, varTeam, crossFilter.serviceId, crossFilter.teamId, crossFilter.from, crossFilter.to]);

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
      <div className="min-h-screen bg-gray-950 p-4">
        {/* Kiosk toolbar — only time range + exit */}
        <div className="flex items-center justify-between mb-3">
          <span className="text-sm font-semibold text-white flex items-center gap-2">
            <Tv size={16} className="text-accent" />
            {data.name}
          </span>
          <div className="flex items-center gap-2">
            <select
              value={timeRange}
              onChange={(e) => setTimeRange(e.target.value)}
              className="text-xs rounded border border-gray-700 bg-gray-900 text-gray-300 px-2 py-1 focus:outline-none"
              aria-label={t('governance.dashboardView.timeRangeLabel', 'Time range')}
            >
              {TIME_RANGE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {t(opt.labelKey, opt.value)}
                </option>
              ))}
            </select>
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
          <div className="flex items-center justify-center h-48 text-sm text-gray-500">
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
                  className="rounded-lg border border-gray-700 bg-gray-900 shadow overflow-hidden"
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
              <h1 className="text-xl font-bold text-gray-900 dark:text-white">{data.name}</h1>
              <Badge variant="secondary">{data.persona}</Badge>
            </div>
            {data.description && (
              <p className="text-sm text-gray-500 dark:text-gray-400 ml-7 leading-snug">
                {data.description}
              </p>
            )}
          </div>

          {/* Global controls */}
          <div className="flex flex-wrap items-center gap-2">
            {/* Time range */}
            <div className="flex items-center gap-1.5 rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 px-2 py-1">
              <Clock size={12} className="text-gray-400" />
              <select
                value={timeRange}
                onChange={(e) => setTimeRange(e.target.value)}
                className="text-xs bg-transparent text-gray-700 dark:text-gray-300 focus:outline-none"
                aria-label={t('governance.dashboardView.timeRangeLabel', 'Time range')}
              >
                {TIME_RANGE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {t(opt.labelKey, opt.value)}
                  </option>
                ))}
              </select>
            </div>

            {/* Auto-refresh */}
            <select
              value={autoRefreshSeconds}
              onChange={(e) => setAutoRefreshSeconds(Number(e.target.value))}
              className="text-xs rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 px-2 py-1 focus:outline-none"
              aria-label={t('governance.dashboardView.autoRefreshLabel', 'Auto-refresh')}
            >
              {AUTO_REFRESH_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label === 'Off' ? t('governance.dashboardView.autoRefreshOff', 'Auto-refresh Off') : `${t('governance.dashboardView.autoRefreshEvery', 'Every')} ${opt.label}`}
                </option>
              ))}
            </select>

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
                  : 'border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-500 hover:text-accent'
              }`}
            >
              <span className={`inline-block h-2 w-2 rounded-full ${isLiveEnabled && isLive ? 'bg-green-400' : isLiveEnabled ? 'bg-yellow-400' : 'bg-gray-500'}`} />
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

      {/* Dashboard Variables panel — Grafana-style global overrides */}
      {showVars && (
        <div className="mb-4 rounded-lg border border-accent/30 bg-accent/5 px-4 py-3 flex flex-wrap items-center gap-4">
          <span className="text-xs font-semibold text-accent flex items-center gap-1">
            <SlidersHorizontal size={12} />
            {t('governance.dashboardView.variablesLabel', 'Variables')}
          </span>
          <label className="flex items-center gap-2 text-xs text-gray-700 dark:text-gray-300">
            <span className="font-medium text-accent">$service</span>
            <input
              type="text"
              value={varService}
              onChange={(e) => setVarService(e.target.value)}
              placeholder={t('governance.dashboardView.varServicePlaceholder', 'All services')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1 text-gray-900 dark:text-white w-36"
            />
          </label>
          <label className="flex items-center gap-2 text-xs text-gray-700 dark:text-gray-300">
            <span className="font-medium text-accent">$team</span>
            <input
              type="text"
              value={varTeam}
              onChange={(e) => setVarTeam(e.target.value)}
              placeholder={t('governance.dashboardView.varTeamPlaceholder', 'All teams')}
              className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-xs px-2 py-1 text-gray-900 dark:text-white w-36"
            />
          </label>
          {(varService || varTeam) && (
            <Button
              size="sm"
              variant="secondary"
              onClick={() => { setVarService(''); setVarTeam(''); }}
            >
              {t('governance.dashboardView.clearVars', 'Clear')}
            </Button>
          )}
          <span className="text-[10px] text-gray-400 ml-auto">
            {t('governance.dashboardView.varsHint', 'Variable values override per-widget service/team filters')}
          </span>
        </div>
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
        <div className="flex items-center justify-center h-48 text-sm text-gray-400">
          {t('governance.dashboardView.noWidgets', 'No widgets configured. Click Edit to add widgets.')}
        </div>
      ) : (
        <div className={`grid gap-4 auto-rows-[160px] ${gridColsClass}`}>
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

            return (
              <div
                key={slot.widgetId}
                style={style}
                className="group relative rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-sm overflow-hidden"
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
                {/* Hover controls row: Drill-down + Expand */}
                <div className="absolute top-1 right-1 flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity focus-within:opacity-100">
                  {drillDest && (
                    <button
                      type="button"
                      onClick={() => navigate(drillDest.path)}
                      className="p-1 rounded bg-white/80 dark:bg-gray-900/80 text-gray-400 hover:text-accent text-xs"
                      title={t(drillDest.labelKey, drillDest.label)}
                      aria-label={t(drillDest.labelKey, drillDest.label)}
                    >
                      ↗
                    </button>
                  )}
                  <button
                    type="button"
                    onClick={() => setExpandedWidgetId(slot.widgetId)}
                    className="p-1 rounded bg-white/80 dark:bg-gray-900/80 text-gray-400 hover:text-accent"
                    aria-label={t('governance.dashboardView.expandWidget', 'Expand widget')}
                  >
                    <Maximize2 size={12} />
                  </button>
                </div>
              </div>
            );
          })}
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
            <div className="relative w-full max-w-3xl max-h-[80vh] rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-2xl overflow-auto">
              <button
                onClick={() => setExpandedWidgetId(null)}
                className="absolute top-3 right-3 p-1.5 rounded-full bg-gray-100 dark:bg-gray-800 text-gray-500 hover:text-gray-900 dark:hover:text-white transition-colors z-10"
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

      <p className="mt-4 text-xs text-gray-400 text-right">
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
    </PageContainer>
  );
}
