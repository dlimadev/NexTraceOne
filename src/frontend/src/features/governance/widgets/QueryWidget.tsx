import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  PieChart,
  Pie,
  Cell,
  Tooltip as RechartsTooltip,
  Legend,
  ScatterChart,
  Scatter,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
} from 'recharts';
import client from '../../../api/client';
import { NqlEditor, NqlResultViewer } from '../components/NqlEditor';
import { CHART_RAINBOW, CHART_SEMANTIC } from '../../../lib/chartColors';
import type { WidgetProps } from './WidgetRegistry';
import type { NqlRenderHint } from './WidgetRegistry';

// ── Types ─────────────────────────────────────────────────────────────────

interface NqlExecuteResponse {
  isSimulated: boolean;
  simulatedNote?: string;
  columns: string[];
  rows: (string | number | null)[][];
  totalRows: number;
  renderHint: string;
  executionMs: number;
  parsedEntity: string;
  appliedLimit: number;
}

// ── Render hint palette ───────────────────────────────────────────────────

interface NqlResultRendererProps {
  result: NqlExecuteResponse;
  renderHint: string;
}

function NqlResultRenderer({ result, renderHint }: NqlResultRendererProps) {
  const effectiveHint = renderHint || result.renderHint || 'table';

  if (effectiveHint === 'pie') {
    const data = result.rows
      .map((row) => ({ name: String(row[0] ?? ''), value: Number(row[1] ?? 0) }))
      .filter((d) => d.value > 0);
    if (data.length === 0) return <NqlResultViewer result={result} />;
    return (
      <ResponsiveContainer width="100%" height={220}>
        <PieChart>
          <Pie data={data} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={80} label>
            {data.map((_, i) => (
              <Cell key={i} fill={CHART_RAINBOW[i % CHART_RAINBOW.length]} />
            ))}
          </Pie>
          <RechartsTooltip />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
    );
  }

  if (effectiveHint === 'gauge') {
    const raw = result.rows[0]?.[0];
    const val = Number(raw ?? 0);
    const pct = Math.min(100, Math.max(0, val));
    const color = pct >= 80 ? CHART_SEMANTIC.critical : pct >= 60 ? CHART_SEMANTIC.warning : CHART_SEMANTIC.success;
    return (
      <div className="flex flex-col items-center justify-center h-full gap-2 py-4">
        <span className="text-3xl font-bold text-neutral-100 tabular-nums">{val.toLocaleString()}</span>
        {result.columns[0] && (
          <span className="text-xs text-neutral-400">{result.columns[0]}</span>
        )}
        <div className="w-full max-w-[200px] h-3 rounded-full bg-neutral-800 overflow-hidden">
          <div
            className="h-full rounded-full transition-all"
            style={{ width: `${pct}%`, backgroundColor: color }}
          />
        </div>
        <span className="text-xs text-neutral-500">{pct.toFixed(1)}%</span>
      </div>
    );
  }

  if (effectiveHint === 'scatter') {
    const xCol = result.columns[0] ?? 'x';
    const yCol = result.columns[1] ?? 'y';
    const zCol = result.columns[2];
    const data = result.rows.map((row) => ({
      x: Number(row[0] ?? 0),
      y: Number(row[1] ?? 0),
      z: zCol != null ? String(row[2] ?? '') : undefined,
    }));
    return (
      <ResponsiveContainer width="100%" height={220}>
        <ScatterChart>
          <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
          <XAxis dataKey="x" name={xCol} tick={{ fill: '#9ca3af', fontSize: 10 }} />
          <YAxis dataKey="y" name={yCol} tick={{ fill: '#9ca3af', fontSize: 10 }} />
          <RechartsTooltip
            cursor={{ strokeDasharray: '3 3' }}
            content={({ payload }) => {
              if (!payload?.length) return null;
              const d = payload[0]?.payload as { x: number; y: number; z?: string };
              return (
                <div className="rounded border border-neutral-700 bg-neutral-900 px-2 py-1 text-xs text-neutral-200">
                  <p>{xCol}: {d.x}</p>
                  <p>{yCol}: {d.y}</p>
                  {zCol && d.z != null && <p>{zCol}: {d.z}</p>}
                </div>
              );
            }}
          />
          <Scatter data={data} fill={CHART_RAINBOW[0]} />
        </ScatterChart>
      </ResponsiveContainer>
    );
  }

  if (effectiveHint === 'treemap') {
    const data = result.rows.map((row) => ({
      name: String(row[0] ?? ''),
      value: Number(row[1] ?? 0),
    })).filter((d) => d.value > 0);
    if (data.length === 0) return <NqlResultViewer result={result} />;
    const total = data.reduce((s, d) => s + d.value, 0);
    return (
      <div className="flex flex-wrap gap-1 p-2 h-full content-start overflow-hidden">
        {data.map((item, i) => {
          const pct = total > 0 ? (item.value / total) * 100 : 0;
          const minW = Math.max(pct * 1.5, 10);
          return (
            <div
              key={i}
              className="flex items-center justify-center rounded text-[9px] font-medium text-white overflow-hidden"
              style={{
                backgroundColor: CHART_RAINBOW[i % CHART_RAINBOW.length],
                minWidth: `${minW}%`,
                flexGrow: pct,
                height: pct > 20 ? '48%' : '22%',
                padding: '2px 4px',
              }}
              title={`${item.name}: ${item.value}`}
            >
              <span className="truncate">{item.name}</span>
            </div>
          );
        })}
      </div>
    );
  }

  return <NqlResultViewer result={result} />;
}

// ── Component ─────────────────────────────────────────────────────────────

export function QueryWidget({ config, environmentId, timeRange, title }: WidgetProps) {
  const { t } = useTranslation();
  const [nqlQuery, setNqlQuery] = useState(config.nqlQuery ?? '');
  const [renderHint, setRenderHint] = useState<NqlRenderHint>(
    (config.renderHint as NqlRenderHint | undefined) ?? 'table'
  );
  const [result, setResult] = useState<NqlExecuteResponse | null>(null);
  const [isEditMode, setIsEditMode] = useState(!config.nqlQuery);

  const tenantId = (environmentId ?? 'default').split('-')[0] || 'default';

  const autoRunMutation = useMutation<NqlExecuteResponse, Error, string>({
    mutationFn: async (query) => {
      const res = await client.post('/governance/nql/execute', {
        nqlQuery: query,
        tenantId,
        environmentId,
        persona: 'Engineer',
        userId: 'current-user',
      });
      return res.data;
    },
    onSuccess: (data) => setResult(data),
  });

  // Auto-run if query is pre-configured (view mode)
  const handleAutoRun = useCallback(() => {
    if (config.nqlQuery && !result && !autoRunMutation.isPending) {
      autoRunMutation.mutate(config.nqlQuery);
    }
  }, [config.nqlQuery, result, autoRunMutation]);

  // Trigger auto-run once on mount when query exists
  if (config.nqlQuery && !result && !autoRunMutation.isPending && !autoRunMutation.isError) {
    handleAutoRun();
  }

  const displayTitle = title ?? config.customTitle ?? t('governance.customDashboards.widgets.queryWidget');

  return (
    <div className="flex h-full flex-col gap-2 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between gap-2 shrink-0">
        <h3 className="text-sm font-semibold text-neutral-200 truncate">{displayTitle}</h3>
        <button
          type="button"
          onClick={() => setIsEditMode(prev => !prev)}
          className="shrink-0 rounded border border-neutral-700 bg-neutral-800 px-2 py-0.5 text-xs text-neutral-400 hover:text-neutral-200"
          aria-label={isEditMode ? 'View results' : 'Edit query'}
        >
          {isEditMode ? '▶ Results' : '✏ Edit'}
        </button>
      </div>

      {/* Edit mode: NQL editor */}
      {isEditMode && (
        <div className="overflow-y-auto flex-1">
          <NqlEditor
            tenantId={tenantId}
            environmentId={environmentId}
            initialQuery={nqlQuery}
            initialRenderHint={renderHint}
            onQueryChange={setNqlQuery}
            onRenderHintChange={setRenderHint}
            onResultChange={(r) => {
              setResult(r);
              if (r) setIsEditMode(false);
            }}
          />
        </div>
      )}

      {/* View mode: results */}
      {!isEditMode && (
        <div className="overflow-y-auto flex-1">
          {autoRunMutation.isPending && (
            <div className="flex items-center justify-center py-8 text-sm text-neutral-500">
              <span className="animate-pulse">⟳ {t('nqlEditor.runQuery')}…</span>
            </div>
          )}

          {autoRunMutation.isError && (
            <div className="rounded border border-red-700 bg-red-950 px-3 py-2 text-sm text-red-300">
              {autoRunMutation.error.message}
            </div>
          )}

          {!config.nqlQuery && !result && (
            <div className="flex flex-col items-center justify-center gap-3 py-8 text-center">
              <span className="text-2xl">⌨</span>
              <p className="text-sm text-neutral-400">
                {t('nqlEditor.placeholder')}
              </p>
              <button
                type="button"
                onClick={() => setIsEditMode(true)}
                className="rounded bg-blue-600 px-3 py-1.5 text-xs text-white hover:bg-blue-500"
              >
                {t('nqlEditor.title')}
              </button>
            </div>
          )}

          {result && <NqlResultRenderer result={result} renderHint={renderHint} />}
        </div>
      )}

      {/* Time range context */}
      <p className="shrink-0 text-right text-xs text-neutral-600">{timeRange}</p>
    </div>
  );
}
