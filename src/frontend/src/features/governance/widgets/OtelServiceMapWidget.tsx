/**
 * ObsServiceMapWidget — exibe mapa de saúde de serviços via ECharts.
 * Dados via GET /api/v1/governance/observability/service-health
 * e GET /api/v1/governance/observability/backend-info (informação do backend).
 * Fallback para lista simples caso o ECharts não consiga renderizar.
 */
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Network, AlertCircle, Settings } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface ServiceHealthEntry {
  serviceName: string;
  healthStatus: 'healthy' | 'degraded' | 'critical';
  errorRate: number;
  avgLatencyMs: number;
  traceCount: number;
}

interface DashboardServiceHealthResult {
  services: ServiceHealthEntry[];
  isBackendAvailable: boolean;
}

interface BackendInfoResponse {
  backendName?: string;
  [key: string]: unknown;
}

interface ServiceNode {
  name: string;
  healthStatus: 'healthy' | 'degraded' | 'critical';
  errorRate: number;
  traceCount: number;
}

// ── Service colour by health status ───────────────────────────────────────

function serviceNodeColor(node: ServiceNode): string {
  switch (node.healthStatus) {
    case 'critical':  return '#ef4444';
    case 'degraded':  return '#f59e0b';
    case 'healthy':   return '#10b981';
    default: {
      // Fallback: derive from error rate if healthStatus is unknown
      if (node.traceCount === 0) return '#6b7280';
      if (node.errorRate > 0.1) return '#ef4444';
      if (node.errorRate > 0.02) return '#f59e0b';
      return '#10b981';
    }
  }
}

// ── Build graph from service health entries ────────────────────────────────

function buildGraph(services: ServiceHealthEntry[]): {
  nodes: ServiceNode[];
  edges: Array<{ source: string; target: string }>;
} {
  const nodes: ServiceNode[] = services.map((s) => ({
    name: s.serviceName,
    healthStatus: s.healthStatus,
    errorRate: s.errorRate,
    traceCount: s.traceCount,
  }));

  // Build synthetic edges: consecutive services form a ring dependency
  // to illustrate adjacency when no explicit call-graph data is available.
  const edges: Array<{ source: string; target: string }> = [];
  if (nodes.length >= 2) {
    for (let i = 0; i < nodes.length - 1; i++) {
      edges.push({ source: nodes[i].name, target: nodes[i + 1].name });
    }
    // Close the ring if there are 3+ services
    if (nodes.length >= 3) {
      edges.push({ source: nodes[nodes.length - 1].name, target: nodes[0].name });
    }
  }

  return { nodes, edges };
}

// ── ECharts renderer ───────────────────────────────────────────────────────

interface EChartsLike {
  setOption: (option: unknown) => void;
  resize: () => void;
  dispose: () => void;
}

function ServiceMapChart({
  nodes,
  edges,
}: {
  nodes: ServiceNode[];
  edges: Array<{ source: string; target: string }>;
}): React.ReactElement {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<EChartsLike | null>(null);
  const [echartsError, setEchartsError] = useState(false);

  useEffect(() => {
    let disposed = false;

    import('echarts')
      .then((echarts) => {
        if (disposed || !containerRef.current) return;
        try {
          chartRef.current = echarts.init(containerRef.current) as EChartsLike;

          chartRef.current.setOption({
            backgroundColor: 'transparent',
            series: [
              {
                type: 'graph',
                layout: 'force',
                animation: false,
                roam: false,
                label: {
                  show: true,
                  fontSize: 9,
                  color: '#d1d5db',
                },
                force: {
                  repulsion: 80,
                  gravity: 0.1,
                  edgeLength: [50, 100],
                },
                data: nodes.map((n) => ({
                  name: n.name,
                  symbolSize: Math.max(14, Math.min(n.traceCount * 2 + 10, 36)),
                  itemStyle: { color: serviceNodeColor(n) },
                  label: { show: true },
                })),
                edges: edges.map((e) => ({
                  source: e.source,
                  target: e.target,
                  lineStyle: { color: '#374151', width: 1 },
                })),
              },
            ],
          });
        } catch {
          setEchartsError(true);
        }
      })
      .catch(() => {
        setEchartsError(true);
      });

    const handleResize = () => {
      chartRef.current?.resize();
    };
    window.addEventListener('resize', handleResize);

    return () => {
      disposed = true;
      window.removeEventListener('resize', handleResize);
      chartRef.current?.dispose();
      chartRef.current = null;
    };
  }, [nodes, edges]);

  if (echartsError) {
    return (
      <ServiceMapFallbackList nodes={nodes} />
    );
  }

  return <div ref={containerRef} className="w-full h-full" />;
}

// ── Fallback list ──────────────────────────────────────────────────────────

function ServiceMapFallbackList({ nodes }: { nodes: ServiceNode[] }): React.ReactElement {
  const { t } = useTranslation();
  return (
    <div className="flex-1 overflow-y-auto min-h-0 flex flex-col gap-1">
      {nodes.length === 0 ? (
        <span className="text-xs text-gray-400 dark:text-gray-500 self-center mt-4">
          {t('obs.serviceMap.noServices', 'No services')}
        </span>
      ) : (
        nodes.map((node) => (
          <div
            key={node.name}
            className="flex items-center gap-2 px-1 py-0.5"
          >
            <span
              className="inline-block h-2 w-2 rounded-full shrink-0"
              style={{ backgroundColor: serviceNodeColor(node) }}
              aria-hidden="true"
            />
            <span className="text-[11px] text-gray-700 dark:text-gray-300 truncate flex-1">
              {node.name}
            </span>
            <span className="text-[10px] text-gray-400 dark:text-gray-500 tabular-nums shrink-0">
              {node.traceCount} {t('obs.serviceMap.traces', 'traces')}
            </span>
          </div>
        ))
      )}
    </div>
  );
}

// ── Main component ─────────────────────────────────────────────────────────

export function OtelServiceMapWidget({
  config,
  environmentId,
  timeRange,
  title,
}: WidgetProps): React.ReactElement {
  const { t } = useTranslation();

  const environment = environmentId ?? config.serviceId ?? undefined;

  const displayTitle =
    title ??
    config.customTitle ??
    t('governance.customDashboards.widgets.obsServiceMap', 'Service Health Map');

  // Fetch service health data
  const {
    data: healthData,
    isLoading: healthLoading,
    isError: healthError,
    refetch,
  } = useQuery({
    queryKey: ['widget-obs-service-map', environment, timeRange],
    queryFn: (): Promise<DashboardServiceHealthResult> =>
      client
        .get<DashboardServiceHealthResult>('/governance/observability/service-health', {
          params: { environment, timeRange },
        })
        .then((r) => r.data),
  });

  // Fetch backend info for footer label
  const { data: backendInfo } = useQuery({
    queryKey: ['widget-obs-backend-info'],
    queryFn: (): Promise<BackendInfoResponse> =>
      client
        .get<BackendInfoResponse>('/governance/observability/backend-info')
        .then((r) => r.data),
    staleTime: 60_000,
  });

  if (healthLoading) {
    return (
      <div className="h-full flex flex-col gap-1 p-2">
        <div className="flex items-center gap-1.5 shrink-0">
          <Network size={13} className="text-indigo-500 shrink-0" />
          <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
            {displayTitle}
          </span>
        </div>
        <Skeleton variant="rectangular" className="flex-1 w-full" />
      </div>
    );
  }

  if (healthError) {
    return (
      <div className="h-full flex flex-col items-center justify-center gap-2 p-2">
        <AlertCircle size={18} className="text-red-400" />
        <span className="text-xs text-red-500 dark:text-red-400 text-center">
          {t('governance.dashboardView.widgetError', 'Could not load data')}
        </span>
        <button
          type="button"
          onClick={() => refetch()}
          className="text-xs text-blue-500 underline hover:no-underline"
        >
          {t('common.retry', 'Retry')}
        </button>
      </div>
    );
  }

  // Backend not configured — neutral info state
  if (healthData && !healthData.isBackendAvailable) {
    return (
      <div className="h-full flex flex-col gap-1 p-2">
        <div className="flex items-center gap-1.5 shrink-0">
          <Network size={13} className="text-indigo-500 shrink-0" />
          <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
            {displayTitle}
          </span>
        </div>
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <Settings size={18} className="text-gray-400 dark:text-gray-500" />
          <span className="text-xs text-gray-500 dark:text-gray-400 text-center">
            {t('obs.backendNotConfigured', 'Backend not configured')}
          </span>
        </div>
      </div>
    );
  }

  const { nodes, edges } = buildGraph(healthData?.services ?? []);
  const backendName = backendInfo?.backendName ? String(backendInfo.backendName) : null;

  return (
    <div className="h-full flex flex-col gap-1 p-2">
      {/* Header */}
      <div className="flex items-center gap-1.5 shrink-0">
        <Network size={13} className="text-indigo-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        <span className="ml-auto text-[10px] text-gray-400 dark:text-gray-500">
          {nodes.length} {t('obs.serviceMap.services', 'services')}
        </span>
      </div>

      {/* Chart / list */}
      {nodes.length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {t('obs.serviceMap.noData', 'No service data')}
          </span>
        </div>
      ) : (
        <div className="flex-1 min-h-0 overflow-hidden">
          <ServiceMapChart nodes={nodes} edges={edges} />
        </div>
      )}

      {/* Footer — observability backend info */}
      {backendName && (
        <div className="shrink-0 text-[9px] text-gray-400 dark:text-gray-500 text-right">
          {t('obs.backendInfo', 'Data: {{backend}}', { backend: backendName })}
        </div>
      )}
    </div>
  );
}
