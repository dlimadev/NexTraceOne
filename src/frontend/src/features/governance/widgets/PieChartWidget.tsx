/**
 * PieChartWidget — exibe distribuição de métricas por dimensão (serviço, equipa, severidade).
 * Suporta modo donut, data labels e cross-filter por segmento.
 * Dados via GET /api/v1/governance/observability/distribution.
 */
import { useQuery } from '@tanstack/react-query';
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { PieChart as PieChartIcon } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps } from './WidgetRegistry';
import client from '../../../api/client';
import { getChartPalette, CHART_CHROME } from '../../../lib/chartColors';

// ── Types ──────────────────────────────────────────────────────────────────

interface DistributionSegment {
  name: string;
  value: number;
  color?: string;
}

interface DistributionResult {
  segments: DistributionSegment[];
  total: number;
  unit?: string;
  isBackendAvailable: boolean;
}

// ── Dados simulados ────────────────────────────────────────────────────────

const SIMULATED_DATA: DistributionResult = {
  segments: [
    { name: 'payment-service',      value: 423 },
    { name: 'user-service',         value: 218 },
    { name: 'gateway-service',      value: 156 },
    { name: 'analytics-service',    value: 89  },
    { name: 'notification-service', value: 34  },
  ],
  total: 920,
  unit: 'errors',
  isBackendAvailable: false,
};

// ── Tooltip customizado ────────────────────────────────────────────────────

interface TooltipPayloadEntry {
  name: string;
  value: number;
  payload: DistributionSegment;
}

function CustomTooltip({
  active,
  payload,
  total,
  unit,
}: {
  active?: boolean;
  payload?: TooltipPayloadEntry[];
  total: number;
  unit: string;
}): React.ReactElement | null {
  if (!active || !payload || payload.length === 0) return null;
  const item = payload[0];
  if (!item) return null;
  const pct = total > 0 ? ((item.value / total) * 100).toFixed(1) : '0.0';
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
      <div className="font-semibold">{item.name}</div>
      <div>{item.value.toLocaleString()} {unit}</div>
      <div style={{ color: CHART_CHROME.tooltipMuted }}>{pct}% do total</div>
    </div>
  );
}

// ── Label externo (quando showDataLabels=true) ─────────────────────────────

interface LabelProps {
  cx: number;
  cy: number;
  midAngle: number;
  outerRadius: number;
  name: string;
  value: number;
  total: number;
}

function renderLabel({ cx, cy, midAngle, outerRadius, name, value, total }: LabelProps): React.ReactElement {
  const RADIAN = Math.PI / 180;
  const radius = outerRadius + 20;
  const x = cx + radius * Math.cos(-midAngle * RADIAN);
  const y = cy + radius * Math.sin(-midAngle * RADIAN);
  const pct = total > 0 ? ((value / total) * 100).toFixed(0) : '0';
  return (
    <text
      x={x}
      y={y}
      fill="#9ca3af"
      textAnchor={x > cx ? 'start' : 'end'}
      dominantBaseline="central"
      fontSize={9}
    >
      {name} ({pct}%)
    </text>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function PieChartWidget({
  config,
  environmentId,
  timeRange,
  title,
  onCrossFilter,
  activeCrossFilter,
}: WidgetProps): React.ReactElement {
  const metricName = config.metricName ?? 'errors';
  const groupBy    = config.groupBy    ?? 'service';

  const displayTitle =
    title ??
    config.customTitle ??
    `Distribution: ${metricName.charAt(0).toUpperCase() + metricName.slice(1)} by ${groupBy.charAt(0).toUpperCase() + groupBy.slice(1)}`;

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-pie-chart', metricName, groupBy, timeRange, environmentId],
    queryFn: (): Promise<DistributionResult> =>
      client
        .get<DistributionResult>('/governance/observability/distribution', {
          params: { metric: metricName, groupBy, timeRange, environmentId },
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
          <PieChartIcon size={13} className="text-blue-500 shrink-0" />
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
  const isSimulated = !effectiveData.isBackendAvailable;

  const { segments, total, unit = '' } = effectiveData;
  const palette = getChartPalette(config.colorScheme);

  const innerRadius = config.donut === true ? 60 : 0;
  const showLabels  = config.showDataLabels === true;

  // Determina opacidade com base no cross-filter activo
  function getOpacity(segmentName: string): number {
    if (!activeCrossFilter?.serviceId) return 1;
    return activeCrossFilter.serviceId === segmentName ? 1 : 0.35;
  }

  function handleClick(segment: DistributionSegment): void {
    if (groupBy === 'service' && onCrossFilter) {
      onCrossFilter({ serviceId: segment.name });
    }
  }

  // Prepara label renderer com acesso a total
  function labelRenderer(props: object): React.ReactElement {
    return renderLabel({ ...(props as LabelProps), total });
  }

  return (
    <div className="flex flex-col h-full gap-2 p-1">
      {/* Título */}
      <div className="flex items-center gap-1.5 shrink-0 px-1">
        <PieChartIcon size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-900 dark:text-white truncate">{displayTitle}</span>
      </div>

      {/* Banner de dados simulados */}
      {isSimulated && (
        <div className="shrink-0 flex items-center gap-1.5 px-2 py-1 rounded bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-700">
          <span className="text-yellow-600 dark:text-yellow-400 text-[10px]">⚠ Simulated data</span>
        </div>
      )}

      {/* Gráfico */}
      <div className="flex-1 min-h-0">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={segments}
              dataKey="value"
              nameKey="name"
              innerRadius={innerRadius}
              outerRadius="70%"
              label={showLabels ? labelRenderer : undefined}
              labelLine={showLabels}
              onClick={(entry) => handleClick(entry as DistributionSegment)}
              style={{ cursor: groupBy === 'service' ? 'pointer' : 'default' }}
            >
              {segments.map((seg, idx) => (
                <Cell
                  key={seg.name}
                  fill={seg.color ?? palette[idx % palette.length]}
                  opacity={getOpacity(seg.name)}
                />
              ))}
            </Pie>
            <Tooltip
              content={<CustomTooltip total={total} unit={unit} />}
            />
            <Legend
              iconSize={8}
              iconType="circle"
              wrapperStyle={{ fontSize: 9, color: CHART_CHROME.tooltipMuted }}
            />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
