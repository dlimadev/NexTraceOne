/**
 * HeatmapCalendarWidget — calendário de heatmap com ECharts (90 dias).
 * Visualiza densidade de eventos (incidentes, deploys, etc.) por dia.
 * Dados via GET /api/v1/governance/observability/calendar-heatmap.
 */
import { useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import * as echarts from 'echarts';
import { CalendarDays } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';
import { CHART_SEMANTIC } from '../../../lib/chartColors';

// ── Types ──────────────────────────────────────────────────────────────────

interface CalendarCell {
  date: string;
  value: number;
}

interface CalendarHeatmapResult {
  cells: CalendarCell[];
  maxValue: number;
  metric: string;
  isBackendAvailable: boolean;
}

// ── Geração de dados simulados (90 dias) ───────────────────────────────────

function generateSimulatedCells(days: number): CalendarCell[] {
  const cells: CalendarCell[] = [];
  const now = new Date();

  for (let i = days - 1; i >= 0; i--) {
    const d = new Date(now);
    d.setDate(d.getDate() - i);
    const dow = d.getDay(); // 0=domingo, 6=sábado

    // Dias úteis têm mais incidentes; fins-de-semana têm menos actividade
    const isWeekday = dow >= 1 && dow <= 5;
    const base      = isWeekday ? 0.4 : 0.15;
    const rand      = Math.random();
    let value       = 0;

    if (rand < base * 0.5)  value = 0;
    else if (rand < base)   value = 1;
    else if (rand < base + 0.15) value = 2;
    else if (rand < base + 0.2)  value = 3;
    else if (rand < base + 0.22) value = 4;
    else if (rand < base + 0.225) value = 5;

    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');

    cells.push({ date: `${yyyy}-${mm}-${dd}`, value });
  }

  return cells;
}

// ── Componente do gráfico ECharts ──────────────────────────────────────────

interface EChartsCalendarProps {
  cells: CalendarCell[];
  maxValue: number;
  startDate: string;
  endDate: string;
  compact: boolean;
}

function EChartsCalendar({ cells, maxValue, startDate, endDate, compact }: EChartsCalendarProps): React.ReactElement {
  const chartRef  = useRef<HTMLDivElement>(null);
  const chartInst = useRef<echarts.ECharts | null>(null);

  // Inicializa a instância ECharts e configura ResizeObserver
  useEffect(() => {
    if (!chartRef.current) return;

    const instance = echarts.init(chartRef.current, null, { renderer: 'canvas' });
    chartInst.current = instance;

    const ro = new ResizeObserver(() => instance.resize());
    ro.observe(chartRef.current);

    return () => {
      ro.disconnect();
      instance.dispose();
    };
  }, []);

  // Actualiza opções quando os dados mudam
  useEffect(() => {
    if (!chartInst.current) return;

    const option: echarts.EChartsOption = {
      tooltip: {
        position: 'top',
        formatter: (params) => {
          const p = params as echarts.DefaultLabelFormatterCallbackParams;
          const d = (p.data as [string, number]);
          return `${d[0]}: ${d[1]} events`;
        },
      },
      visualMap: compact
        ? { show: false, min: 0, max: maxValue, inRange: { color: ['#1f2937', '#fef9c3', '#fde68a', '#f97316', CHART_SEMANTIC.critical] } }
        : {
            min: 0,
            max: maxValue,
            calculable: true,
            orient: 'horizontal',
            left: 'center',
            bottom: 5,
            itemWidth: 12,
            itemHeight: 80,
            textStyle: { color: '#9ca3af', fontSize: 9 },
            inRange: { color: ['#1f2937', '#fef9c3', '#fde68a', '#f97316', CHART_SEMANTIC.critical] },
          },
      calendar: {
        range: [startDate, endDate],
        cellSize: ['auto', compact ? 10 : 14],
        itemStyle: {
          borderWidth: 2,
          borderColor: '#111827',
        },
        yearLabel: { show: false },
        dayLabel: {
          nameMap: ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'] as string[],
          color: '#9ca3af',
          fontSize: 10,
          firstDay: 0,
        },
        monthLabel: { color: '#9ca3af', fontSize: 10 },
        top: 10,
        left: 30,
        right: 10,
        bottom: compact ? 10 : 40,
      },
      series: [
        {
          type: 'heatmap',
          coordinateSystem: 'calendar',
          data: cells.map((c) => [c.date, c.value]),
        },
      ],
    };

    chartInst.current.setOption(option, true);
  }, [cells, maxValue, startDate, endDate, compact]);

  return <div ref={chartRef} style={{ width: '100%', height: '100%' }} />;
}

// ── Component ──────────────────────────────────────────────────────────────

export function HeatmapCalendarWidget({
  config,
  environmentId,
  title,
}: WidgetProps): React.ReactElement {
  const metricName = config.metricName ?? 'incidents';
  const DAYS       = 90;

  const displayTitle =
    title ??
    config.customTitle ??
    `Activity: ${metricName.charAt(0).toUpperCase() + metricName.slice(1)} — Last ${DAYS} days`;

  // Calcula o intervalo de datas dos últimos 90 dias
  const endDateObj   = new Date();
  const startDateObj = new Date();
  startDateObj.setDate(startDateObj.getDate() - (DAYS - 1));

  function fmtDate(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  const startDate = fmtDate(startDateObj);
  const endDate   = fmtDate(endDateObj);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-heatmap-calendar', metricName, DAYS, environmentId],
    queryFn: (): Promise<CalendarHeatmapResult> =>
      client
        .get<CalendarHeatmapResult>('/governance/observability/calendar-heatmap', {
          params: { metric: metricName, days: DAYS, environmentId },
        })
        .then((r) => r.data),
  });

  // Referência para medir a altura do container
  const containerRef = useRef<HTMLDivElement>(null);
  // eslint-disable-next-line react-hooks/refs
  const containerHeight = containerRef.current?.clientHeight ?? 300;
  // Modo compacto para widgets muito pequenos
  // eslint-disable-next-line react-hooks/refs
  const compact = containerHeight < 160;

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
          <CalendarDays size={13} className="text-blue-500 shrink-0" />
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
  const isSimulated = !(data?.isBackendAvailable);
  const cells    = isSimulated ? generateSimulatedCells(compact ? 30 : DAYS) : (data?.cells ?? []);
  const maxValue = isSimulated ? 5 : (data?.maxValue ?? 5);

  return (
    <div ref={containerRef} className="flex flex-col h-full gap-2 p-1">
      {/* Título */}
      <div className="flex items-center gap-1.5 shrink-0 px-1">
        <CalendarDays size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>

      {/* Banner de dados simulados */}
      {isSimulated && (
        <div className="shrink-0 flex items-center gap-1.5 px-2 py-1 rounded bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-700">
          <span className="text-yellow-600 dark:text-yellow-400 text-[10px]">⚠ Simulated data</span>
        </div>
      )}

      {/* Gráfico ECharts */}
      <div className="flex-1 min-h-0">
        <EChartsCalendar
          cells={cells}
          maxValue={maxValue}
          startDate={startDate}
          endDate={endDate}
          compact={compact}
        />
      </div>
    </div>
  );
}
