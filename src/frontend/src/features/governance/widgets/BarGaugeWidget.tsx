/**
 * BarGaugeWidget — exibe barras de gauge horizontais ao estilo Grafana.
 * Cada barra representa um serviço/equipa com o seu valor percentual ou absoluto.
 * Dados via GET /api/v1/governance/observability/gauges.
 */
import { useQuery } from '@tanstack/react-query';
import { Gauge } from 'lucide-react';
import { Skeleton } from '../../../components/Skeleton';
import type { WidgetProps, WidgetConfig } from './WidgetRegistry';
import client from '../../../api/client';
import { CHART_SEMANTIC } from '../../../lib/chartColors';

// ── Types ──────────────────────────────────────────────────────────────────

interface GaugeEntry {
  label: string;
  value: number;
  min?: number;
  max?: number;
  unit?: string;
}

interface GaugesResult {
  gauges: GaugeEntry[];
  isBackendAvailable: boolean;
}

// ── Dados simulados ────────────────────────────────────────────────────────

const SIMULATED_DATA: GaugesResult = {
  gauges: [
    { label: 'payment-service',      value: 99.7, min: 0, max: 100, unit: '%' },
    { label: 'user-service',         value: 98.2, min: 0, max: 100, unit: '%' },
    { label: 'gateway-service',      value: 94.1, min: 0, max: 100, unit: '%' },
    { label: 'analytics-service',    value: 87.5, min: 0, max: 100, unit: '%' },
    { label: 'notification-service', value: 78.3, min: 0, max: 100, unit: '%' },
  ],
  isBackendAvailable: false,
};

// ── Lógica de cor baseada em thresholds ────────────────────────────────────

type Threshold = { value: number; color: string; label?: string };

function resolveBarColor(value: number, thresholds?: WidgetConfig['thresholds']): string {
  if (thresholds && thresholds.length > 0) {
    // Ordena thresholds descendente e retorna a cor do primeiro threshold atingido
    const sorted = [...thresholds].sort((a, b) => b.value - a.value);
    for (const t of sorted) {
      if (value >= t.value) return t.color;
    }
    // Abaixo de todos os thresholds: usa cor do threshold mais baixo
    const last = sorted[sorted.length - 1] as Threshold;
    return last.color;
  }

  // Cores padrão quando sem thresholds configurados
  if (value >= 95) return CHART_SEMANTIC.success; // verde
  if (value >= 80) return CHART_SEMANTIC.warning; // amarelo
  return CHART_SEMANTIC.critical;                  // vermelho
}

// ── Linha de threshold sobre a barra ──────────────────────────────────────

function ThresholdMarker({ value, min, max }: { value: number; min: number; max: number }): React.ReactElement {
  const range = max - min;
  const pct = range > 0 ? ((value - min) / range) * 100 : 0;
  return (
    <div
      className="absolute top-0 bottom-0 w-px bg-white/60 z-10"
      style={{ left: `${Math.min(Math.max(pct, 0), 100)}%` }}
      title={`Threshold: ${value}`}
    />
  );
}

// ── Linha de gauge individual ──────────────────────────────────────────────

interface GaugeRowProps {
  gauge: GaugeEntry;
  unit: string;
  thresholds?: WidgetConfig['thresholds'];
  onClick?: () => void;
}

function GaugeRow({ gauge, unit, thresholds, onClick }: GaugeRowProps): React.ReactElement {
  const min = gauge.min ?? 0;
  const max = gauge.max ?? 100;
  const range = max - min;
  const fillPct = range > 0 ? Math.min(Math.max(((gauge.value - min) / range) * 100, 0), 100) : 0;
  const barColor = resolveBarColor(gauge.value, thresholds);

  return (
    <div
      className="flex items-center gap-2 cursor-pointer hover:bg-gray-800/40 rounded px-1 py-0.5"
      onClick={onClick}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => e.key === 'Enter' && onClick?.()}
    >
      {/* Label à esquerda em font-mono — estilo Grafana */}
      <span className="font-mono text-[10px] text-gray-300 w-36 shrink-0 truncate" title={gauge.label}>
        {gauge.label}
      </span>

      {/* Barra de progresso com marcadores de threshold */}
      <div className="flex-1 relative bg-gray-700 rounded-sm overflow-hidden" style={{ height: 8 }}>
        <div
          className="h-full rounded-sm transition-all duration-300"
          style={{ width: `${fillPct}%`, backgroundColor: barColor }}
        />
        {/* Marca as linhas de threshold sobre a barra */}
        {thresholds?.map((t) => (
          <ThresholdMarker key={t.value} value={t.value} min={min} max={max} />
        ))}
      </div>

      {/* Valor à direita */}
      <span className="font-mono text-[10px] tabular-nums shrink-0" style={{ color: barColor, minWidth: 44, textAlign: 'right' }}>
        {gauge.value.toFixed(1)}{unit}
      </span>
    </div>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function BarGaugeWidget({
  config,
  environmentId,
  timeRange,
  title,
  onCrossFilter,
}: WidgetProps): React.ReactElement {
  const metricName = config.metricName ?? 'slo-compliance';
  const unit       = config.unit       ?? '';

  const displayTitle =
    title ??
    config.customTitle ??
    `Gauge: ${metricName.charAt(0).toUpperCase() + metricName.slice(1)}`;

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['widget-bar-gauge', metricName, timeRange, environmentId],
    queryFn: (): Promise<GaugesResult> =>
      client
        .get<GaugesResult>('/governance/observability/gauges', {
          params: { metric: metricName, timeRange, environmentId },
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
          <Gauge size={13} className="text-blue-500 shrink-0" />
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

  // Limita a 10 gauges visíveis (resto com scroll interno)
  const gauges = effectiveData.gauges.slice(0, 10);
  const effectiveUnit = unit || (gauges[0]?.unit ?? '');

  return (
    <div className="flex flex-col h-full gap-2 p-1 bg-gray-900/50 rounded">
      {/* Título */}
      <div className="flex items-center gap-1.5 shrink-0 px-1">
        <Gauge size={13} className="text-blue-500 shrink-0" />
        <span className="text-xs font-semibold text-gray-100 truncate">{displayTitle}</span>
      </div>

      {/* Banner de dados simulados */}
      {isSimulated && (
        <div className="shrink-0 flex items-center gap-1.5 px-2 py-1 rounded bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-700">
          <span className="text-yellow-600 dark:text-yellow-400 text-[10px]">⚠ Simulated data</span>
        </div>
      )}

      {/* Lista de gauges com scroll interno */}
      <div className="flex-1 overflow-y-auto flex flex-col gap-0.5 min-h-0">
        {gauges.map((gauge) => (
          <GaugeRow
            key={gauge.label}
            gauge={gauge}
            unit={effectiveUnit}
            thresholds={config.thresholds ?? undefined}
            onClick={() => onCrossFilter?.({ serviceId: gauge.label })}
          />
        ))}
      </div>
    </div>
  );
}
