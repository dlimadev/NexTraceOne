/**
 * TreemapWidget — visualiza distribuição de custo/utilização por serviço em treemap ECharts.
 * Suporta hierarquia com filhos, colorização por esquema e cross-filter.
 * Dados via GET /api/v1/governance/observability/treemap.
 */
import { useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import * as echarts from 'echarts';
import { LayoutGrid } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface TreemapNode {
  name: string;
  value: number;
  children?: TreemapNode[];
}

interface TreemapResult {
  nodes: TreemapNode[];
  unit?: string;
  isBackendAvailable: boolean;
}

// ── Dados simulados de custo por serviço ───────────────────────────────────

const SIMULATED_DATA: TreemapResult = {
  nodes: [
    { name: 'payment-service',      value: 12400 },
    { name: 'analytics-service',    value: 8900  },
    { name: 'user-service',         value: 4200  },
    { name: 'gateway-service',      value: 3100  },
    { name: 'notification-service', value: 1800  },
    { name: 'data-pipeline',        value: 6700  },
    { name: 'ml-inference',         value: 9200  },
    { name: 'cdn-edge',             value: 2100  },
  ],
  unit: 'USD/month',
  isBackendAvailable: false,
};

// ── Paletas para ECharts ───────────────────────────────────────────────────

const COLOR_PALETTES: Record<string, string[]> = {
  rainbow: ['#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4', '#f97316', '#14b8a6'],
  blue:    ['#1d4ed8', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd', '#bfdbfe'],
  green:   ['#065f46', '#047857', '#059669', '#10b981', '#34d399', '#6ee7b7'],
  red:     ['#991b1b', '#b91c1c', '#dc2626', '#ef4444', '#f87171', '#fca5a5'],
  purple:  ['#4c1d95', '#6d28d9', '#7c3aed', '#8b5cf6', '#a78bfa', '#c4b5fd'],
};

function getColorPalette(colorScheme?: string | null): string[] {
  return colorScheme && colorScheme in COLOR_PALETTES
    ? (COLOR_PALETTES[colorScheme] as string[])
    : (COLOR_PALETTES['rainbow'] as string[]);
}

// ── Formatador de valor ────────────────────────────────────────────────────

function formatValue(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000)     return `${(value / 1_000).toFixed(1)}k`;
  return String(value);
}

// ── Componente ECharts Treemap ─────────────────────────────────────────────

interface EChartsTreemapProps {
  nodes: TreemapNode[];
  unit: string;
  colorPalette: string[];
  activeServiceId?: string | null;
  onNodeClick?: (name: string) => void;
}

function EChartsTreemap({ nodes, unit, colorPalette, activeServiceId, onNodeClick }: EChartsTreemapProps): React.ReactElement {
  const chartRef  = useRef<HTMLDivElement>(null);
  const chartInst = useRef<echarts.ECharts | null>(null);

  // Inicializa instância e ResizeObserver
  useEffect(() => {
    if (!chartRef.current) return;

    const instance = echarts.init(chartRef.current, null, { renderer: 'canvas' });
    chartInst.current = instance;

    // Reage a clicks para cross-filter
    instance.on('click', (params) => {
      if (params.name && onNodeClick) {
        onNodeClick(params.name);
      }
    });

    const ro = new ResizeObserver(() => instance.resize());
    ro.observe(chartRef.current);

    return () => {
      ro.disconnect();
      instance.dispose();
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Actualiza opções quando dados ou cross-filter mudam
  useEffect(() => {
    if (!chartInst.current) return;

    // Aplica opacidade reduzida nos nós que não correspondem ao cross-filter activo
    const nodesWithOpacity: (TreemapNode & { itemStyle?: { opacity: number } })[] = nodes.map((n) => ({
      ...n,
      itemStyle: activeServiceId
        ? { opacity: n.name === activeServiceId ? 1 : 0.35 }
        : undefined,
    }));

    const option: echarts.EChartsOption = {
      color: colorPalette,
      tooltip: {
        formatter: (params) => {
          const p = params as echarts.DefaultLabelFormatterCallbackParams;
          return `${p.name}: ${formatValue(p.value as number)} ${unit}`;
        },
      },
      series: [
        {
          type: 'treemap',
          data: nodesWithOpacity,
          roam: false,
          nodeClick: 'zoomToNode',
          breadcrumb: { show: false },
          label: {
            show: true,
            formatter: '{b}\n{c}',
            fontSize: 10,
            color: '#f9fafb',
          },
          upperLabel: {
            show: true,
            height: 30,
            fontSize: 10,
            color: '#f9fafb',
          },
          itemStyle: {
            borderWidth: 1,
            borderColor: '#1f2937',
            gapWidth: 2,
          },
          levels: [
            {
              itemStyle: { borderWidth: 2, borderColor: '#374151' },
              upperLabel: { show: true },
            },
            {
              colorSaturation: [0.35, 0.7],
            },
          ],
        },
      ],
    };

    chartInst.current.setOption(option, true);
  }, [nodes, unit, colorPalette, activeServiceId]);

  return <div ref={chartRef} style={{ width: '100%', height: '100%' }} />;
}

// ── Component ──────────────────────────────────────────────────────────────

export function TreemapWidget({
  config,
  environmentId,
  timeRange,
  title,
  onCrossFilter,
  activeCrossFilter,
}: WidgetProps): React.ReactElement {
  const groupBy    = config.groupBy    ?? 'service';
  const metricName = config.metricName ?? 'cost';

  const displayTitle =
    title ??
    config.customTitle ??
    `Treemap: ${metricName.charAt(0).toUpperCase() + metricName.slice(1)} by ${groupBy.charAt(0).toUpperCase() + groupBy.slice(1)}`;

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-treemap', groupBy, metricName, timeRange, environmentId],
    queryFn: (): Promise<TreemapResult> =>
      client
        .get<TreemapResult>('/governance/observability/treemap', {
          params: { groupBy, metric: metricName, timeRange, environmentId },
        })
        .then((r) => r.data),
  });

  if (isLoading) {
    return (
      <div className="flex flex-col h-full gap-2 p-1">
        <Skeleton className="h-full w-full rounded" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex flex-col h-full gap-2 p-2">
        <div className="flex items-center gap-1.5 shrink-0">
          <LayoutGrid size={13} className="text-blue-500 shrink-0" />
          <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
        </div>
        <div className="flex-1 flex flex-col items-center justify-center gap-2">
          <span className="text-xs text-red-500 dark:text-red-400 text-center">
            Não foi possível carregar dados
          </span>
          <button
            type="button"
            onClick={() => refetch()}
            className="text-xs text-blue-500 underline hover:no-underline"
          >
            Tentar novamente
          </button>
        </div>
      </div>
    );
  }

  // Usa dados simulados quando backend indisponível
  const effectiveData = (data && data.isBackendAvailable) ? data : SIMULATED_DATA;
  const isSimulated   = !effectiveData.isBackendAvailable;
  const unit          = config.unit ?? effectiveData.unit ?? '';

  const colorPalette = getColorPalette(config.colorScheme);

  function handleNodeClick(name: string): void {
    onCrossFilter?.({ serviceId: name });
  }

  return (
    <div className="flex flex-col h-full gap-2 p-1">
      {/* Título */}
      <div className="flex items-center gap-1.5 shrink-0 px-1">
        <LayoutGrid size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>

      {/* Banner de dados simulados */}
      {isSimulated && (
        <div className="shrink-0 flex items-center gap-1.5 px-2 py-1 rounded bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-700">
          <span className="text-yellow-600 dark:text-yellow-400 text-[10px]">⚠ Simulated data</span>
        </div>
      )}

      {/* Gráfico Treemap */}
      <div className="flex-1 min-h-0">
        <EChartsTreemap
          nodes={effectiveData.nodes}
          unit={unit}
          colorPalette={colorPalette}
          activeServiceId={activeCrossFilter?.serviceId}
          onNodeClick={handleNodeClick}
        />
      </div>
    </div>
  );
}
