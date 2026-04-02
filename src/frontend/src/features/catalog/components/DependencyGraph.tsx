import { useEffect, useRef, useCallback } from 'react';
import * as echarts from 'echarts/core';
import { GraphChart } from 'echarts/charts';
import { TooltipComponent, LegendComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import type { GraphSeriesOption } from 'echarts/charts';
import type { AssetGraph, ServiceNode, ApiNode } from '../../../types/index';

echarts.use([GraphChart, TooltipComponent, LegendComponent, CanvasRenderer]);

type GraphNode = NonNullable<GraphSeriesOption['data']>[number] & { id: string };
type GraphLink = NonNullable<GraphSeriesOption['links']>[number];

/** Paleta de cores dos nós — alinha com o design system. */
const COLORS = {
  service: { fill: '#3b82f6', stroke: '#1d4ed8', label: '#eff6ff' },
  api: { fill: '#10b981', stroke: '#059669', label: '#ecfdf5' },
  edge: '#6b7280',
  selectedRing: '#f59e0b',
};

interface DependencyGraphProps {
  graph: AssetGraph;
  /** Nó actualmente seleccionado (serviceAssetId ou apiAssetId). */
  selectedNodeId?: string | null;
  /** Chamado quando o utilizador clica num nó. */
  onSelectNode?: (id: string) => void;
  /** Altura do canvas em pixels (default: 520). */
  height?: number;
}

/**
 * Grafo interactivo de dependências do catálogo usando Apache ECharts.
 * Utiliza layout force-directed para posicionamento automático dos nós.
 */
export function DependencyGraph({
  graph,
  selectedNodeId,
  onSelectNode,
  height = 520,
}: DependencyGraphProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<echarts.ECharts | null>(null);

  /** Constrói os nós e arestas a partir do AssetGraph. */
  const buildGraphData = useCallback((): { nodes: GraphNode[]; links: GraphLink[] } => {
    const nodes: GraphNode[] = [];
    const links: GraphLink[] = [];

    // Nós de serviços
    for (const svc of graph.services) {
      nodes.push({
        id: svc.serviceAssetId,
        name: svc.name,
        category: 0,
        symbolSize: 52,
        label: { show: true, formatter: svc.name, fontSize: 11, color: COLORS.service.label, overflow: 'break', width: 80 },
        itemStyle: {
          color: COLORS.service.fill,
          borderColor: selectedNodeId === svc.serviceAssetId ? COLORS.selectedRing : COLORS.service.stroke,
          borderWidth: selectedNodeId === svc.serviceAssetId ? 3 : 1.5,
          shadowBlur: selectedNodeId === svc.serviceAssetId ? 14 : 0,
          shadowColor: selectedNodeId === svc.serviceAssetId ? COLORS.selectedRing : 'transparent',
        },
        tooltip: { formatter: buildServiceTooltip(svc) },
      });
    }

    // Nós de APIs (menores, quadrados arredondados)
    for (const api of graph.apis) {
      nodes.push({
        id: api.apiAssetId,
        name: api.name,
        category: 1,
        symbol: 'roundRect',
        symbolSize: [64, 32],
        label: {
          show: true,
          formatter: api.name.length > 14 ? api.name.slice(0, 13) + '…' : api.name,
          fontSize: 10,
          color: COLORS.api.label,
        },
        itemStyle: {
          color: COLORS.api.fill,
          borderColor: selectedNodeId === api.apiAssetId ? COLORS.selectedRing : COLORS.api.stroke,
          borderWidth: selectedNodeId === api.apiAssetId ? 3 : 1.5,
          shadowBlur: selectedNodeId === api.apiAssetId ? 14 : 0,
          shadowColor: selectedNodeId === api.apiAssetId ? COLORS.selectedRing : 'transparent',
        },
        tooltip: { formatter: buildApiTooltip(api) },
      });

      // Aresta: API → serviço proprietário (linha fina de propriedade)
      if (api.ownerServiceAssetId) {
        links.push({
          source: api.ownerServiceAssetId,
          target: api.apiAssetId,
          lineStyle: { color: '#475569', width: 1, type: 'dashed' },
        });
      }

      // Arestas: API → consumidores (relações de dependência)
      for (const consumer of api.consumers ?? []) {
        const consumerSvc = graph.services.find((s) => s.name === consumer.consumerName || s.serviceAssetId === consumer.consumerName);
        if (consumerSvc) {
          links.push({
            source: api.apiAssetId,
            target: consumerSvc.serviceAssetId,
            lineStyle: {
              color: COLORS.edge,
              width: 1.5,
              curveness: 0.2,
            },
          });
        }
      }
    }

    return { nodes, links };
  }, [graph, selectedNodeId]);

  /** Inicializa e/ou actualiza o gráfico ECharts. */
  useEffect(() => {
    if (!containerRef.current) return;

    if (!chartRef.current) {
      chartRef.current = echarts.init(containerRef.current, null, { renderer: 'canvas' });
    }

    const chart = chartRef.current;
    const { nodes, links } = buildGraphData();

    chart.setOption(
      {
      tooltip: {
        trigger: 'item',
        confine: true,
        backgroundColor: 'var(--color-panel, #1e293b)',
        borderColor: 'var(--color-edge, #334155)',
        textStyle: { color: 'var(--color-body, #e2e8f0)', fontSize: 12 },
      },
      legend: [
        {
          data: ['Service', 'API'],
          bottom: 12,
          textStyle: { color: '#94a3b8', fontSize: 11 },
        },
      ],
      series: [
        {
          type: 'graph',
          layout: 'force',
          roam: true,
          draggable: true,
          animation: true,
          animationDuration: 600,
          data: nodes,
          links,
          categories: [
            { name: 'Service', itemStyle: { color: COLORS.service.fill } },
            { name: 'API', itemStyle: { color: COLORS.api.fill } },
          ],
          force: {
            repulsion: 260,
            gravity: 0.04,
            edgeLength: [80, 160],
            layoutAnimation: true,
          },
          edgeSymbol: ['none', 'arrow'],
          edgeSymbolSize: [4, 8],
          lineStyle: { opacity: 0.65, width: 1.5, curveness: 0 },
          emphasis: {
            focus: 'adjacency',
            lineStyle: { width: 2.5 },
            itemStyle: { shadowBlur: 12, shadowColor: 'rgba(255,255,255,0.25)' },
          },
          label: { show: true, position: 'inside' },
        },
      ],
      },
      true,
    );

    // Listener de clique em nó
    chart.off('click');
    chart.on('click', (params: { dataType?: string; data?: unknown }) => {
      if (params.dataType === 'node' && params.data) {
        const id = (params.data as { id?: string }).id;
        if (id) onSelectNode?.(id);
      }
    });
  }, [buildGraphData, onSelectNode]);

  // Resize reactivo via ResizeObserver
  useEffect(() => {
    if (!containerRef.current || !chartRef.current) return;
    const chart = chartRef.current;
    const ro = new ResizeObserver(() => chart.resize());
    ro.observe(containerRef.current);
    return () => ro.disconnect();
  }, []);

  // Cleanup ao desmontar
  useEffect(() => {
    return () => {
      chartRef.current?.dispose();
      chartRef.current = null;
    };
  }, []);

  return (
    <div
      ref={containerRef}
      style={{ width: '100%', height: `${height}px` }}
      className="rounded-lg overflow-hidden bg-panel border border-edge"
      aria-label="Service dependency graph"
    />
  );
}

// ── Helpers de tooltip ───────────────────────────────────────────────

function buildServiceTooltip(svc: ServiceNode): string {
  return `
    <div style="font-weight:600;margin-bottom:6px;font-size:13px">${svc.name}</div>
    <div style="color:#94a3b8;font-size:11px;line-height:1.8">
      Team: <span style="color:#e2e8f0">${svc.teamName}</span><br/>
      Domain: <span style="color:#e2e8f0">${svc.domain}</span><br/>
      Type: <span style="color:#e2e8f0">${svc.serviceType}</span><br/>
      Lifecycle: <span style="color:#e2e8f0">${svc.lifecycleStatus}</span><br/>
      Criticality: <span style="color:#e2e8f0">${svc.criticality}</span>
    </div>`;
}

function buildApiTooltip(api: ApiNode): string {
  return `
    <div style="font-weight:600;margin-bottom:6px;font-size:13px">${api.name}</div>
    <div style="color:#94a3b8;font-size:11px;line-height:1.8">
      Route: <span style="color:#e2e8f0;font-family:monospace">${api.routePattern}</span><br/>
      Version: <span style="color:#e2e8f0">v${api.version}</span><br/>
      Visibility: <span style="color:#e2e8f0">${api.visibility}</span><br/>
      Consumers: <span style="color:#e2e8f0">${api.consumers?.length ?? 0}</span>
    </div>`;
}
