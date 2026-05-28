/**
 * HistogramWidget — histograma de distribuição de latência (ou outra métrica) via recharts.
 * Mostra percentis p50/p95/p99 e colore as barras por faixas de performance.
 * Dados via GET /api/v1/governance/observability/histogram.
 */
import { useQuery } from '@tanstack/react-query';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ReferenceLine,
  ResponsiveContainer,
  Cell,
} from 'recharts';
import { BarChart2 } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps, WidgetConfig } from './WidgetRegistry';
import client from '../../../api/client';
import { CHART_SEMANTIC, CHART_CHROME } from '../../../lib/chartColors';

// ── Types ──────────────────────────────────────────────────────────────────

interface HistogramBucket {
  rangeLabel: string;
  min: number;
  max: number;
  count: number;
}

interface HistogramResult {
  buckets: HistogramBucket[];
  unit?: string;
  p50?: number;
  p95?: number;
  p99?: number;
  isBackendAvailable: boolean;
}

// ── Dados simulados — distribuição log-normal centrada ~100ms ──────────────

const SIMULATED_DATA: HistogramResult = {
  buckets: [
    { rangeLabel: '0-50ms',   min: 0,   max: 50,   count: 2341  },
    { rangeLabel: '50-100ms', min: 50,  max: 100,  count: 8234  },
    { rangeLabel: '100-150ms',min: 100, max: 150,  count: 12891 },
    { rangeLabel: '150-200ms',min: 150, max: 200,  count: 6734  },
    { rangeLabel: '200-300ms',min: 200, max: 300,  count: 3421  },
    { rangeLabel: '300-500ms',min: 300, max: 500,  count: 1234  },
    { rangeLabel: '500ms+',   min: 500, max: 9999, count: 456   },
  ],
  p50: 132,
  p95: 287,
  p99: 412,
  unit: 'ms',
  isBackendAvailable: false,
};

// ── Cor de cada bucket baseada em thresholds ou defaults ──────────────────

function resolveBucketColor(bucket: HistogramBucket, thresholds?: WidgetConfig['thresholds']): string {
  const midpoint = bucket.min === bucket.max ? bucket.min : (bucket.min + bucket.max) / 2;

  if (thresholds && thresholds.length > 0) {
    // Usa o midpoint do bucket para determinar a cor
    const sorted = [...thresholds].sort((a, b) => b.value - a.value);
    for (const t of sorted) {
      if (midpoint <= t.value) return t.color;
    }
    return CHART_SEMANTIC.success;
  }

  // Defaults para latência: verde <150ms, amarelo <300ms, vermelho >=300ms
  if (bucket.max <= 150)  return CHART_SEMANTIC.success; // verde
  if (bucket.max <= 300)  return CHART_SEMANTIC.warning; // amarelo
  return CHART_SEMANTIC.critical;                         // vermelho
}

// ── Tooltip customizado ────────────────────────────────────────────────────

interface TooltipPayloadEntry {
  value: number;
  payload: HistogramBucket & { pct: number };
}

function CustomTooltip({
  active,
  payload,
  unit,
}: {
  active?: boolean;
  payload?: TooltipPayloadEntry[];
  unit: string;
}): React.ReactElement | null {
  if (!active || !payload || payload.length === 0) return null;
  const item = payload[0];
  if (!item) return null;
  const { rangeLabel, count, pct } = item.payload;

  return (
    <div
      style={{
        backgroundColor: CHART_CHROME.tooltipBg,
        border: `1px solid ${CHART_CHROME.tooltipBorder}`,
        borderRadius: 6,
        fontSize: 11,
        color: CHART_CHROME.tooltipText,
        padding: '6px 10px',
      }}
    >
      <div className="font-semibold">{rangeLabel}</div>
      <div>{count.toLocaleString()} requests</div>
      <div style={{ color: CHART_CHROME.tooltipMuted }}>{pct.toFixed(1)}% do total · {unit}</div>
    </div>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function HistogramWidget({
  config,
  environmentId,
  timeRange,
  title,
}: WidgetProps): React.ReactElement {
  const metricName = config.metricName ?? 'latency';
  const unit       = config.unit       ?? 'ms';

  const displayTitle =
    title ??
    config.customTitle ??
    `Histogram: ${metricName.charAt(0).toUpperCase() + metricName.slice(1)}`;

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-histogram', metricName, timeRange, environmentId],
    queryFn: (): Promise<HistogramResult> =>
      client
        .get<HistogramResult>('/governance/observability/histogram', {
          params: {
            metric: metricName,
            timeRange,
            environmentId,
          },
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
          <BarChart2 size={13} className="text-blue-500 shrink-0" />
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

  const effectiveUnit = unit || effectiveData.unit || 'ms';
  const { buckets, p50, p95, p99 } = effectiveData;

  // Calcula total para percentagens
  const total = buckets.reduce((sum, b) => sum + b.count, 0);
  const chartData = buckets.map((b) => ({
    ...b,
    pct: total > 0 ? (b.count / total) * 100 : 0,
  }));

  // Sub-título com percentis
  const percentilesLabel = [
    p50 != null ? `p50: ${p50}${effectiveUnit}` : null,
    p95 != null ? `p95: ${p95}${effectiveUnit}` : null,
    p99 != null ? `p99: ${p99}${effectiveUnit}` : null,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <div className="flex flex-col h-full gap-2 p-1">
      {/* Título */}
      <div className="flex items-center gap-1.5 shrink-0 px-1">
        <BarChart2 size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>

      {/* Banner de dados simulados */}
      {isSimulated && (
        <div className="shrink-0 flex items-center gap-1.5 px-2 py-1 rounded bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-700">
          <span className="text-yellow-600 dark:text-yellow-400 text-[10px]">⚠ Simulated data</span>
        </div>
      )}

      {/* Sub-título com percentis */}
      {percentilesLabel && (
        <div className="shrink-0 px-1">
          <span className="text-[10px] text-gray-500 dark:text-gray-400 font-mono">{percentilesLabel}</span>
        </div>
      )}

      {/* Gráfico */}
      <div className="flex-1 min-h-0">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart
            data={chartData}
            margin={{ top: 4, right: 4, left: -16, bottom: 0 }}
            barCategoryGap={1}
          >
            <XAxis
              dataKey="rangeLabel"
              tick={{ fontSize: 8, fill: CHART_CHROME.tooltipMuted }}
              tickLine={false}
              axisLine={false}
              interval={0}
              angle={-30}
              textAnchor="end"
              height={32}
            />
            <YAxis
              tick={{ fontSize: 9, fill: CHART_CHROME.tooltipMuted }}
              tickLine={false}
              axisLine={false}
              width={36}
              label={{
                value: 'Count',
                angle: -90,
                position: 'insideLeft',
                offset: 12,
                style: { fontSize: 9, fill: CHART_CHROME.tooltipFaded },
              }}
            />
            <Tooltip
              content={<CustomTooltip unit={effectiveUnit} />}
              cursor={{ fill: 'rgba(255,255,255,0.05)' }}
            />

            {/* Linha de referência para p95 */}
            {p95 != null && (
              <ReferenceLine
                x={buckets.find((b) => b.min <= p95 && b.max >= p95)?.rangeLabel}
                stroke={CHART_SEMANTIC.warning}
                strokeDasharray="3 3"
                strokeWidth={1.5}
                label={{ value: 'p95', position: 'top', fontSize: 8, fill: CHART_SEMANTIC.warning }}
              />
            )}

            {/* Linha de referência para p99 */}
            {p99 != null && (
              <ReferenceLine
                x={buckets.find((b) => b.min <= p99 && b.max >= p99)?.rangeLabel}
                stroke={CHART_SEMANTIC.critical}
                strokeDasharray="3 3"
                strokeWidth={1.5}
                label={{ value: 'p99', position: 'top', fontSize: 8, fill: CHART_SEMANTIC.critical }}
              />
            )}

            <Bar dataKey="count" isAnimationActive={false}>
              {chartData.map((bucket, idx) => (
                <Cell
                  key={`cell-${idx}`}
                  fill={resolveBucketColor(bucket, config.thresholds ?? undefined)}
                />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
