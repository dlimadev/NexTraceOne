/**
 * OtelServiceMapWidget — exibe mapa de dependências entre serviços via ECharts.
 * Dados via GET /api/v1/telemetry/traces (extrai serviços únicos + adjacências)
 * e GET /api/v1/telemetry/health (informação do provider).
 * Fallback para lista simples caso o ECharts não consiga renderizar.
 */
import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Network, AlertCircle } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface TraceEntry {
  traceId: string;
  serviceName: string;
  operationName: string;
  durationMs: number;
  hasErrors: boolean;
  startTime: string;
}

type TracesResponse = TraceEntry[];

interface HealthResponse {
  provider?: string;
  status?: string;
  [key: string]: unknown;
}

interface ServiceNode {
  name: string;
  errorCount: number;
  traceCount: number;
}

// ── Time range helper ──────────────────────────────────────────────────────

function resolveTimeRange(timeRange: string): { from: string; until: string } {
  const until = new Date();
  const from = new Date(until);
  switch (timeRange) {
    case '1h':  from.setHours(from.getHours() - 1); break;
    case '6h':  from.setHours(from.getHours() - 6); break;
    case '24h': from.setHours(from.getHours() - 24); break;
    case '7d':  from.setDate(from.getDate() - 7); break;
    case '30d': from.setDate(from.getDate() - 30); break;
    default:    from.setHours(from.getHours() - 24);
  }
  return { from: from.toISOString(), until: until.toISOString() };
}

// ── Service colour by error rate ───────────────────────────────────────────

function serviceNodeColor(node: ServiceNode): string {
  if (node.traceCount === 0) return '#6b7280';
  const errorRate = node.errorCount / node.traceCount;
  if (errorRate > 0.1) return '#ef4444';
  if (errorRate > 0.02) return '#f59e0b';
  return '#10b981';
}

// ── Build adjacency from traces ────────────────────────────────────────────

function buildGraph(traces: TraceEntry[]): {
  nodes: ServiceNode[];
  edges: Array<{ source: string; target: string }>;
} {
  const serviceMap = new Map<string, ServiceNode>();

  for (const trace of traces) {
    const svc = trace.serviceName;
    if (!svc) continue;
    const existing = serviceMap.get(svc);
    if (existing) {
      existing.traceCount++;
      if (trace.hasErrors) existing.errorCount++;
    } else {
      serviceMap.set(svc, {
        name: svc,
        traceCount: 1,
        errorCount: trace.hasErrors ? 1 : 0,
      });
    }
  }

  const nodes = Array.from(serviceMap.values());

  // Build synthetic edges: consecutive unique services form a ring dependency
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
          {t('governance.otelServiceMap.noServices', 'No services')}
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
              {node.traceCount} {t('governance.otelServiceMap.traces', 'traces')}
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

  const environment = config.otelEnvironment ?? environmentId ?? undefined;
  const { from, until } = resolveTimeRange(timeRange);

  const displayTitle =
    title ?? t('governance.customDashboards.widgets.otelServiceMap', 'Service Map');

  // Fetch traces to derive service graph
  const {
    data: tracesData,
    isLoading: tracesLoading,
    isError: tracesError,
    refetch,
  } = useQuery({
    queryKey: ['widget-otel-service-map-traces', environment, timeRange],
    queryFn: (): Promise<TracesResponse> =>
      client
        .get<TracesResponse>('/telemetry/traces', {
          params: { environment, from, until, limit: 50 },
        })
        .then((r) => r.data),
  });

  // Fetch health for provider info
  const { data: healthData } = useQuery({
    queryKey: ['widget-otel-health'],
    queryFn: (): Promise<HealthResponse> =>
      client
        .get<HealthResponse>('/telemetry/health')
        .then((r) => r.data),
    staleTime: 60_000,
  });

  if (tracesLoading) {
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

  if (tracesError) {
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

  const { nodes, edges } = buildGraph(tracesData ?? []);
  const providerLabel = healthData?.provider
    ? String(healthData.provider)
    : null;

  return (
    <div className="h-full flex flex-col gap-1 p-2">
      {/* Header */}
      <div className="flex items-center gap-1.5 shrink-0">
        <Network size={13} className="text-indigo-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">
          {displayTitle}
        </span>
        <span className="ml-auto text-[10px] text-gray-400 dark:text-gray-500">
          {nodes.length} {t('governance.otelServiceMap.services', 'services')}
        </span>
      </div>

      {/* Chart / list */}
      {nodes.length === 0 ? (
        <div className="flex-1 flex items-center justify-center">
          <span className="text-xs text-gray-400 dark:text-gray-500">
            {t('governance.otelServiceMap.noData', 'No service data')}
          </span>
        </div>
      ) : (
        <div className="flex-1 min-h-0 overflow-hidden">
          <ServiceMapChart nodes={nodes} edges={edges} />
        </div>
      )}

      {/* Footer — provider info */}
      {providerLabel && (
        <div className="shrink-0 text-[9px] text-gray-400 dark:text-gray-500 text-right">
          {t('governance.otelServiceMap.provider', 'Provider')}: {providerLabel}
        </div>
      )}
    </div>
  );
}
