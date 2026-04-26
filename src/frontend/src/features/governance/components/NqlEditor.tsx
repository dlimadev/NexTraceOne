import { useCallback, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import client from '../../../api/client';
import type { NqlRenderHint } from '../widgets/WidgetRegistry';
import { NQL_RENDER_HINTS } from '../widgets/WidgetRegistry';

// ── Types ──────────────────────────────────────────────────────────────────

interface NqlValidateResponse {
  isValid: boolean;
  parsedEntity?: string;
  parsedLimit?: number;
  parsedRenderHint?: string;
  filterCount?: number;
  groupByCount?: number;
  error?: { code: string; message: string };
}

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

interface NqlEditorProps {
  tenantId: string;
  environmentId?: string | null;
  persona?: string;
  userId?: string;
  initialQuery?: string;
  initialRenderHint?: NqlRenderHint;
  onQueryChange?: (query: string) => void;
  onRenderHintChange?: (hint: NqlRenderHint) => void;
  onResultChange?: (result: NqlExecuteResponse | null) => void;
}

// ── NQL syntax highlighting tokens (Monaco-compatible patterns) ────────────

const NQL_KEYWORDS = ['FROM', 'WHERE', 'AND', 'GROUP', 'BY', 'ORDER', 'LIMIT', 'ASC', 'DESC', 'RENDER', 'AS', 'LIKE'];
const NQL_SOURCES = [
  'catalog.services', 'catalog.contracts',
  'changes.releases', 'changes.changescores',
  'operations.incidents', 'operations.slos',
  'knowledge.docs',
  'finops.costs',
  'governance.teams', 'governance.domains',
];

// ── Component ─────────────────────────────────────────────────────────────

export function NqlEditor({
  tenantId,
  environmentId,
  persona = 'Engineer',
  userId = 'current-user',
  initialQuery = '',
  initialRenderHint = 'table',
  onQueryChange,
  onRenderHintChange,
  onResultChange,
}: NqlEditorProps) {
  const { t } = useTranslation();
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const [query, setQuery] = useState(initialQuery);
  const [renderHint, setRenderHint] = useState<NqlRenderHint>(initialRenderHint);
  const [validationState, setValidationState] = useState<NqlValidateResponse | null>(null);

  const validateMutation = useMutation<NqlValidateResponse, Error, string>({
    mutationFn: async (nqlQuery) => {
      const res = await client.post('/api/v1/governance/nql/validate', {
        nqlQuery,
        tenantId,
        persona,
        userId,
      });
      return res.data;
    },
    onSuccess: (data) => {
      setValidationState(data);
    },
  });

  const executeMutation = useMutation<NqlExecuteResponse, Error, string>({
    mutationFn: async (nqlQuery) => {
      const res = await client.post('/api/v1/governance/nql/execute', {
        nqlQuery,
        tenantId,
        environmentId,
        persona,
        userId,
      });
      return res.data;
    },
    onSuccess: (data) => {
      onResultChange?.(data);
    },
    onError: () => {
      onResultChange?.(null);
    },
  });

  const handleQueryChange = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const val = e.target.value;
    setQuery(val);
    setValidationState(null);
    onQueryChange?.(val);
  }, [onQueryChange]);

  const handleValidate = useCallback(() => {
    if (query.trim()) validateMutation.mutate(query);
  }, [query, validateMutation]);

  const handleRun = useCallback(() => {
    if (query.trim()) executeMutation.mutate(query);
  }, [query, executeMutation]);

  const handleRenderHintChange = useCallback((e: React.ChangeEvent<HTMLSelectElement>) => {
    const hint = e.target.value as NqlRenderHint;
    setRenderHint(hint);
    onRenderHintChange?.(hint);
  }, [onRenderHintChange]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      handleRun();
    }
  }, [handleRun]);

  const isValid = validationState?.isValid === true;
  const isInvalid = validationState?.isValid === false;
  const isBusy = validateMutation.isPending || executeMutation.isPending;

  return (
    <div className="flex flex-col gap-3">
      {/* Query textarea with syntax hint */}
      <div className="relative">
        <textarea
          ref={textareaRef}
          value={query}
          onChange={handleQueryChange}
          onKeyDown={handleKeyDown}
          placeholder={t('nqlEditor.placeholder')}
          rows={4}
          spellCheck={false}
          className="w-full rounded-md border border-neutral-700 bg-neutral-900 px-3 py-2 font-mono text-sm text-neutral-100 placeholder:text-neutral-500 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-y"
          aria-label={t('nqlEditor.title')}
        />
        {/* Keyword autocomplete hint */}
        <div className="mt-1 flex flex-wrap gap-1">
          {NQL_KEYWORDS.slice(0, 7).map(kw => (
            <button
              key={kw}
              type="button"
              onClick={() => {
                const newQ = query ? `${query} ${kw} ` : `${kw} `;
                setQuery(newQ);
                onQueryChange?.(newQ);
                textareaRef.current?.focus();
              }}
              className="rounded bg-neutral-800 px-1.5 py-0.5 text-xs text-blue-400 hover:bg-neutral-700 font-mono"
            >
              {kw}
            </button>
          ))}
        </div>
        {/* Source autocomplete hint */}
        <div className="mt-1 flex flex-wrap gap-1">
          {NQL_SOURCES.map(src => (
            <button
              key={src}
              type="button"
              onClick={() => {
                const newQ = `FROM ${src} `;
                setQuery(newQ);
                onQueryChange?.(newQ);
                textareaRef.current?.focus();
              }}
              className="rounded bg-neutral-800 px-1.5 py-0.5 text-xs text-green-400 hover:bg-neutral-700 font-mono"
            >
              {src}
            </button>
          ))}
        </div>
      </div>

      {/* Validation result */}
      {validationState && (
        <div className={`rounded-md border px-3 py-2 text-sm ${
          isValid
            ? 'border-green-700 bg-green-950 text-green-300'
            : 'border-red-700 bg-red-950 text-red-300'
        }`}>
          {isValid ? (
            <span>
              ✓ {t('nqlEditor.validSyntax')} — {validationState.parsedEntity} · {validationState.filterCount} filters · limit {validationState.parsedLimit}
            </span>
          ) : (
            <span>✗ {t('nqlEditor.invalidSyntax')}: {validationState.error?.message}</span>
          )}
        </div>
      )}

      {validateMutation.isError && (
        <div className="rounded-md border border-red-700 bg-red-950 px-3 py-2 text-sm text-red-300">
          ✗ {t('nqlEditor.invalidSyntax')}: {validateMutation.error?.message}
        </div>
      )}

      {/* Controls */}
      <div className="flex items-center gap-3 flex-wrap">
        {/* Render hint selector */}
        <div className="flex items-center gap-2">
          <label className="text-xs text-neutral-400">{t('nqlEditor.renderHintLabel')}:</label>
          <select
            value={renderHint}
            onChange={handleRenderHintChange}
            className="rounded border border-neutral-700 bg-neutral-800 px-2 py-1 text-xs text-neutral-200"
          >
            {NQL_RENDER_HINTS.map(h => (
              <option key={h.value} value={h.value}>{t(h.labelKey)}</option>
            ))}
          </select>
        </div>

        <div className="ml-auto flex gap-2">
          <button
            type="button"
            onClick={handleValidate}
            disabled={isBusy || !query.trim()}
            className="rounded border border-neutral-600 bg-neutral-800 px-3 py-1.5 text-xs text-neutral-200 hover:bg-neutral-700 disabled:opacity-50"
          >
            {validateMutation.isPending ? '…' : t('nqlEditor.validateQuery')}
          </button>
          <button
            type="button"
            onClick={handleRun}
            disabled={isBusy || !query.trim() || isInvalid}
            className="rounded bg-blue-600 px-3 py-1.5 text-xs text-white hover:bg-blue-500 disabled:opacity-50"
          >
            {executeMutation.isPending ? '…' : t('nqlEditor.runQuery')}
          </button>
        </div>
      </div>

      <p className="text-xs text-neutral-500">
        Ctrl+Enter {t('nqlEditor.runQuery').toLowerCase()}
      </p>
    </div>
  );
}

// ── NQL Result Viewer ──────────────────────────────────────────────────────

interface NqlResultViewerProps {
  result: NqlExecuteResponse;
}

export function NqlResultViewer({ result }: NqlResultViewerProps) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-3">
      {/* Simulated banner */}
      {result.isSimulated && (
        <div className="flex items-start gap-2 rounded-md border border-yellow-700 bg-yellow-950 px-3 py-2 text-sm text-yellow-300">
          <span className="mt-0.5 shrink-0">⚠</span>
          <div>
            <p className="font-medium">{t('nqlEditor.simulatedBanner')}</p>
            <p className="text-xs text-yellow-400">{result.simulatedNote || t('nqlEditor.simulatedNote')}</p>
          </div>
        </div>
      )}

      {/* Meta row */}
      <div className="flex items-center gap-4 text-xs text-neutral-400">
        <span>{t('nqlEditor.entity')}: <strong className="text-neutral-200 font-mono">{result.parsedEntity}</strong></span>
        <span>{t('nqlEditor.rows')}: <strong className="text-neutral-200">{result.totalRows}</strong></span>
        <span>{t('nqlEditor.appliedLimit')}: <strong className="text-neutral-200">{result.appliedLimit}</strong></span>
        <span className="ml-auto">{t('nqlEditor.executedIn', { ms: result.executionMs })}</span>
      </div>

      {/* Table view */}
      {result.totalRows === 0 ? (
        <p className="py-4 text-center text-sm text-neutral-500">{t('nqlEditor.noResults')}</p>
      ) : (
        <div className="overflow-x-auto rounded-md border border-neutral-700">
          <table className="min-w-full text-sm">
            <thead>
              <tr className="border-b border-neutral-700 bg-neutral-800">
                {result.columns.map(col => (
                  <th key={col} className="px-3 py-2 text-left text-xs font-medium text-neutral-400 uppercase tracking-wide">
                    {col}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-800">
              {result.rows.map((row, ri) => (
                <tr key={ri} className="hover:bg-neutral-800/40">
                  {row.map((cell, ci) => (
                    <td key={ci} className="px-3 py-2 text-neutral-200 font-mono text-xs whitespace-nowrap">
                      {cell === null ? <span className="text-neutral-500">null</span> : String(cell)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
