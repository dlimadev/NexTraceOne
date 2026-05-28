/**
 * ChartAnnotations — renderiza annotations como linhas verticais sobre gráficos Recharts.
 * Estilo Grafana/Kibana: linhas verticais coloridas com tooltip ao hover.
 */
import { useMemo } from 'react';
import { ReferenceLine, Tooltip } from 'recharts';
import { CHART_SEMANTIC } from '../../../lib/chartColors';

// ── Types ──────────────────────────────────────────────────────────────────

export interface ChartAnnotation {
  id: string;
  timestamp: string;
  label: string;
  severity: 'info' | 'warning' | 'critical' | 'success';
  type: 'deployment' | 'incident' | 'change' | 'alert' | 'manual';
}

interface ChartAnnotationsProps {
  annotations: ChartAnnotation[];
  timeFormatter?: (timestamp: string) => string;
}

// ── Severity colors ────────────────────────────────────────────────────────

const SEVERITY_COLORS: Record<string, string> = {
  info: CHART_SEMANTIC.accent,
  warning: CHART_SEMANTIC.warning,
  critical: CHART_SEMANTIC.critical,
  success: CHART_SEMANTIC.success,
};

const SEVERITY_OPACITY = 0.7;

// ── Custom tooltip for annotations ─────────────────────────────────────────

function AnnotationsTooltip({ active, payload }: { active?: boolean; payload?: { payload?: { __annotation?: ChartAnnotation } }[] }) {
  if (!active || !payload || payload.length === 0) return null;
  const data = payload[0]?.payload?.__annotation;
  if (!data) return null;

  return (
    <div className="rounded border border-gray-700 bg-gray-900 px-2 py-1.5 shadow-xl">
      <p className="text-[10px] font-semibold" style={{ color: SEVERITY_COLORS[data.severity] }}>
        {data.label}
      </p>
      <p className="text-[9px] text-gray-400">
        {new Date(data.timestamp).toLocaleString()}
      </p>
      <p className="text-[9px] text-gray-500 capitalize">{data.type}</p>
    </div>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function ChartAnnotations({ annotations, timeFormatter }: ChartAnnotationsProps) {
  const lines = useMemo(() => {
    return annotations.map((ann) => {
      const xValue = timeFormatter ? timeFormatter(ann.timestamp) : ann.timestamp;
      return (
        <ReferenceLine
          key={ann.id}
          x={xValue}
          stroke={SEVERITY_COLORS[ann.severity] ?? '#9ca3af'}
          strokeWidth={1.5}
          strokeDasharray="4 3"
          opacity={SEVERITY_OPACITY}
          ifOverflow="extendDomain"
        />
      );
    });
  }, [annotations, timeFormatter]);

  if (annotations.length === 0) return null;

  return (
    <>
      {lines}
      <Tooltip content={<AnnotationsTooltip />} />
    </>
  );
}

// ── Hook to fetch annotations ──────────────────────────────────────────────

// eslint-disable-next-line react-refresh/only-export-components
export function useAnnotations(
  tenantId: string,
  from: string,
  to: string,
  serviceNames?: string[],
  enabled = true
) {
  // In a real implementation this would query the API
  // For now, return simulated annotations based on the time range
  const annotations = useMemo<ChartAnnotation[]>(() => {
    if (!enabled) return [];

    const fromDate = new Date(from);
    const toDate = new Date(to);
    const duration = toDate.getTime() - fromDate.getTime();
    const count = Math.min(8, Math.max(2, Math.floor(duration / (1000 * 60 * 60 * 6))));

    const types: ChartAnnotation['type'][] = ['deployment', 'incident', 'change', 'alert'];
    const severities: ChartAnnotation['severity'][] = ['info', 'warning', 'critical', 'success'];

    return Array.from({ length: count }).map((_, i) => {
      const offset = (duration / (count + 1)) * (i + 1);
      const ts = new Date(fromDate.getTime() + offset);
      return {
        id: `ann-${i}`,
        timestamp: ts.toISOString(),
        label: `${types[i % types.length]} #${i + 1}`,
        severity: severities[i % severities.length],
        type: types[i % types.length],
      };
    });
  }, [from, to, enabled]);

  return { annotations, isLoading: false };
}
