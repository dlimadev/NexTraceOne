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
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link, useParams, useNavigate, useSearchParams } from 'react-router-dom';
import {
  ArrowLeft,
  RefreshCw,
  Settings,
  Clock,
  Share2,
  Check,
  LayoutDashboard,
  SlidersHorizontal,
  ChevronDown,
  ChevronUp,
  Tv,
  X,
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
import { TIME_RANGE_OPTIONS, type WidgetType } from '../widgets/WidgetRegistry';
import type { WidgetProps } from '../widgets/WidgetRegistry';
import type { ComponentType } from 'react';

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
}

interface RenderDataResponse {
  dashboardId: string;
  name: string;
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

const useShareDashboard = (dashboardId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () =>
      client.put(`/governance/dashboards/${dashboardId}`, {
        isShared: true,
      }).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['dashboard-render-data', dashboardId] });
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
    },
  });
};

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
  const { t } = useTranslation();
  const { dashboardId } = useParams<{ dashboardId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const { activeEnvironmentId } = useEnvironment();
  const TENANT_ID = 'default';

  const isKiosk = searchParams.get('kiosk') === 'tv';

  const [timeRange, setTimeRange] = useState('24h');
  const [autoRefreshSeconds, setAutoRefreshSeconds] = useState(0);
  const [copied, setCopied] = useState(false);
  // Dashboard variables — Grafana-style global overrides applied to all widgets
  const [varService, setVarService] = useState('');
  const [varTeam, setVarTeam] = useState('');
  const [showVars, setShowVars] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const qc = useQueryClient();

  const { data, isLoading, isError, refetch } = useRenderData(
    dashboardId ?? '',
    TENANT_ID,
    activeEnvironmentId,
    timeRange,
  );

  const shareMutation = useShareDashboard(dashboardId ?? '');

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

  const handleShare = async () => {
    await shareMutation.mutateAsync();
    const url = window.location.href;
    await navigator.clipboard.writeText(url).catch(() => {});
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

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
                      }}
                      environmentId={activeEnvironmentId}
                      timeRange={slot.effectiveTimeRange}
                      title={slot.customTitle}
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
          <div className="flex items-center gap-2">
            <LayoutDashboard size={20} className="text-accent" />
            <h1 className="text-xl font-bold text-gray-900 dark:text-white">{data.name}</h1>
            <Badge variant="secondary">{data.persona}</Badge>
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

            {/* Share */}
            <Button
              size="sm"
              variant="secondary"
              onClick={handleShare}
              disabled={shareMutation.isPending}
            >
              {copied ? <Check size={12} className="text-green-500" /> : <Share2 size={12} />}
              <span className="ml-1">
                {copied
                  ? t('governance.dashboardView.copied', 'Copied!')
                  : t('governance.dashboardView.share', 'Share')}
              </span>
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

            return (
              <div
                key={slot.widgetId}
                style={style}
                className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-sm overflow-hidden"
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
                    }}
                    environmentId={activeEnvironmentId}
                    timeRange={slot.effectiveTimeRange}
                    title={slot.customTitle}
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

      <p className="mt-4 text-xs text-gray-400 text-right">
        {t('governance.dashboardView.generatedAt', 'Generated at')}{' '}
        {new Date(data.generatedAt).toLocaleTimeString()}
      </p>
    </PageContainer>
  );
}
