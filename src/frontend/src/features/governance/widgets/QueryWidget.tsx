import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import client from '../../../api/client';
import { NqlEditor, NqlResultViewer } from '../components/NqlEditor';
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

// ── Component ─────────────────────────────────────────────────────────────

export function QueryWidget({ widgetId, config, environmentId, timeRange, title }: WidgetProps) {
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
      const res = await client.post('/api/v1/governance/nql/execute', {
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

          {result && <NqlResultViewer result={result} />}
        </div>
      )}

      {/* Time range context */}
      <p className="shrink-0 text-right text-xs text-neutral-600">{timeRange}</p>
    </div>
  );
}
