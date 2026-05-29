// src/frontend/src/features/governance/components/VisualQueryBuilder.tsx
/**
 * VisualQueryBuilder — hybrid query builder inside PanelEditorOverlay (Query tab).
 * Each row has a [Visual] / [NQL] mode toggle.
 * Visual mode: service, metric, filters, groupBy, fn, aggFn dropdowns.
 * NQL mode: NqlMonacoEditor.
 * Uses TanStack Query to fetch service list from /catalog/services.
 */
import { useState, useCallback, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Plus, Trash2, Copy, MoreHorizontal } from 'lucide-react';
import client from '../../../api/client';
import { NqlMonacoEditor } from './NqlMonacoEditor';
import {
  type VisualQueryRow,
  type DashboardVariable,
  makeVisualQueryRow,
  compileToNql,
} from '../types/dashboardBuilder';

const FUNCTIONS = ['rate()', 'sum()', 'avg()', 'max()', 'min()', 'count()'] as const;
const AGG_FUNCTIONS = ['sum by', 'avg by', 'max by', 'min by'] as const;
const FILTER_OPS = ['=', '!=', '=~', '!~', '>', '<'] as const;
const QUERY_IDS = ['A', 'B', 'C', 'D', 'E'] as const;

interface ServiceOption {
  serviceId: string;
  name: string;
}

export interface VisualQueryBuilderProps {
  rows: VisualQueryRow[];
  variables: DashboardVariable[];
  onRowsChange: (rows: VisualQueryRow[]) => void;
}

export function VisualQueryBuilder({ rows, variables, onRowsChange }: VisualQueryBuilderProps) {
  const { t } = useTranslation();
  const [openMenuId, setOpenMenuId] = useState<string | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!openMenuId) return;
    const handleKey = (e: KeyboardEvent) => { if (e.key === 'Escape') setOpenMenuId(null); };
    const handleClick = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setOpenMenuId(null);
      }
    };
    document.addEventListener('keydown', handleKey);
    document.addEventListener('mousedown', handleClick);
    return () => {
      document.removeEventListener('keydown', handleKey);
      document.removeEventListener('mousedown', handleClick);
    };
  }, [openMenuId]);

  const { data: services = [] } = useQuery<ServiceOption[]>({
    queryKey: ['catalog-services-for-builder'],
    queryFn: () =>
      client.get<ServiceOption[]>('/catalog/services', { params: { pageSize: 100 } }).then((r) => r.data),
    staleTime: 60_000,
  });

  const updateRow = useCallback((queryId: string, patch: Partial<VisualQueryRow>) => {
    onRowsChange(rows.map((r) => (r.queryId === queryId ? { ...r, ...patch } : r)));
  }, [rows, onRowsChange]);

  const addRow = useCallback(() => {
    const usedIds = new Set(rows.map((r) => r.queryId));
    const nextId = QUERY_IDS.find((id) => !usedIds.has(id)) ?? `Q${rows.length + 1}`;
    onRowsChange([...rows, makeVisualQueryRow(nextId)]);
  }, [rows, onRowsChange]);

  const removeRow = useCallback((queryId: string) => {
    if (rows.length === 1) return; // keep at least one row
    onRowsChange(rows.filter((r) => r.queryId !== queryId));
  }, [rows, onRowsChange]);

  const duplicateRow = useCallback((queryId: string) => {
    const row = rows.find((r) => r.queryId === queryId);
    if (!row) return;
    const usedIds = new Set(rows.map((r) => r.queryId));
    const nextId = QUERY_IDS.find((id) => !usedIds.has(id)) ?? `Q${rows.length + 1}`;
    onRowsChange([...rows, { ...row, queryId: nextId }]);
  }, [rows, onRowsChange]);

  const copyNql = useCallback((row: VisualQueryRow) => {
    const nql = row.mode === 'nql' ? row.nqlText : compileToNql(row);
    void navigator.clipboard.writeText(nql);
  }, []);

  const switchToNql = useCallback((queryId: string) => {
    const row = rows.find((r) => r.queryId === queryId);
    if (!row) return;
    if (row.mode === 'nql') return;  // already in NQL mode, don't overwrite user's text
    updateRow(queryId, { mode: 'nql', nqlText: compileToNql(row) });
  }, [rows, updateRow]);

  // Variable names available as $variable references
  const variableOptions = variables.map((v) => `$${v.name}`);
  const serviceOptions = [...variableOptions, ...services.map((s) => s.serviceId)];

  return (
    <div className="flex flex-col gap-4 p-3 overflow-y-auto h-full">
      {rows.map((row) => (
        <div
          key={row.queryId}
          className="flex flex-col gap-2 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-3"
        >
          {/* Row header */}
          <div className="flex items-center gap-2">
            <span className="text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-4">
              {row.queryId}
            </span>

            {/* Mode toggle */}
            <div className="flex rounded overflow-hidden border border-gray-200 dark:border-gray-700 text-[10px]">
              <button
                type="button"
                onClick={() => updateRow(row.queryId, { mode: 'visual' })}
                className={`px-2.5 py-1 font-semibold transition-colors ${
                  row.mode === 'visual'
                    ? 'bg-accent text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
              >
                {t('governance.dashboardBuilder.queryBuilder.visual', 'Visual')}
              </button>
              <button
                type="button"
                onClick={() => switchToNql(row.queryId)}
                className={`px-2.5 py-1 font-mono font-semibold transition-colors ${
                  row.mode === 'nql'
                    ? 'bg-accent text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
              >
                {t('governance.dashboardBuilder.queryBuilder.nql', 'NQL')}
              </button>
            </div>

            <div className="flex-1" />

            {/* Row ⋮ menu */}
            <div ref={openMenuId === row.queryId ? menuRef : null} className="relative">
              <button
                type="button"
                onClick={() => setOpenMenuId((prev) => prev === row.queryId ? null : row.queryId)}
                className="p-1 rounded text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
                aria-label="Row menu"
              >
                <MoreHorizontal size={12} />
              </button>
              {openMenuId === row.queryId && (
                <div className="absolute right-0 top-full mt-1 z-10 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg py-1 min-w-[120px]">
                  <button
                    type="button"
                    onClick={() => { duplicateRow(row.queryId); setOpenMenuId(null); }}
                    className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <Copy size={11} />
                    {t('governance.dashboardBuilder.queryBuilder.duplicate', 'Duplicate')}
                  </button>
                  <button
                    type="button"
                    onClick={() => { void copyNql(row); setOpenMenuId(null); }}
                    className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
                  >
                    <Copy size={11} />
                    {t('governance.dashboardBuilder.queryBuilder.copyNql', 'Copy NQL')}
                  </button>
                  <button
                    type="button"
                    onClick={() => { removeRow(row.queryId); setOpenMenuId(null); }}
                    className="flex items-center gap-2 w-full px-3 py-1.5 text-xs text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950 disabled:opacity-40"
                    disabled={rows.length === 1}
                  >
                    <Trash2 size={11} />
                    {t('governance.dashboardBuilder.queryBuilder.delete', 'Delete')}
                  </button>
                </div>
              )}
            </div>
          </div>

          {/* Visual mode fields */}
          {row.mode === 'visual' && (
            <div className="grid gap-2 text-xs">
              {/* Service */}
              <div className="grid grid-cols-[80px_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.service', 'Service')}
                </label>
                <select
                  value={row.serviceId}
                  onChange={(e) => updateRow(row.queryId, { serviceId: e.target.value })}
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent"
                >
                  <option value="">— select —</option>
                  {serviceOptions.map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </select>
              </div>

              {/* Metric */}
              <div className="grid grid-cols-[80px_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.metric', 'Metric')}
                </label>
                <input
                  type="text"
                  value={row.metric}
                  onChange={(e) => updateRow(row.queryId, { metric: e.target.value })}
                  placeholder="http_requests_total"
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                />
              </div>

              {/* Filters */}
              <div className="grid grid-cols-[80px_1fr] items-start gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px] pt-1">
                  {t('governance.dashboardBuilder.queryBuilder.filters', 'Filters')}
                </label>
                <div className="flex flex-wrap gap-1 items-center">
                  {row.filters.map((f, i) => (
                    <span
                      key={i}
                      className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-accent/10 text-accent text-[10px] border border-accent/30"
                    >
                      <span className="font-mono">{f.key}{f.op}&quot;{f.value}&quot;</span>
                      <button
                        type="button"
                        onClick={() => updateRow(row.queryId, {
                          filters: row.filters.filter((_, j) => j !== i),
                        })}
                        className="ml-0.5 hover:text-red-400"
                        aria-label="Remove filter"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                  <button
                    type="button"
                    onClick={() => updateRow(row.queryId, {
                      filters: [...row.filters, { key: '', op: '=', value: '' }],
                    })}
                    className="text-[10px] text-gray-400 hover:text-accent"
                  >
                    {t('governance.dashboardBuilder.queryBuilder.addFilter', '+ Add')}
                  </button>
                </div>
              </div>

              {/* Inline filter editors for incomplete filters */}
              {row.filters.map((f, i) =>
                (f.key === '' || f.value === '') ? (
                  <div key={`fe-${i}-${f.key}`} className="grid grid-cols-[80px_1fr] items-center gap-2">
                    <span />
                    <div className="flex items-center gap-1">
                      <input
                        type="text"
                        value={f.key}
                        onChange={(e) => {
                          const updated = [...row.filters];
                          updated[i] = { ...updated[i], key: e.target.value };
                          updateRow(row.queryId, { filters: updated });
                        }}
                        placeholder="key"
                        className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-1.5 py-0.5 text-[10px] w-20 font-mono focus:outline-none focus:border-accent"
                      />
                      <select
                        value={f.op}
                        onChange={(e) => {
                          const updated = [...row.filters];
                          updated[i] = { ...updated[i], op: e.target.value };
                          updateRow(row.queryId, { filters: updated });
                        }}
                        className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-1 py-0.5 text-[10px] focus:outline-none focus:border-accent"
                      >
                        {FILTER_OPS.map((op) => <option key={op} value={op}>{op}</option>)}
                      </select>
                      <input
                        type="text"
                        value={f.value}
                        onChange={(e) => {
                          const updated = [...row.filters];
                          updated[i] = { ...updated[i], value: e.target.value };
                          updateRow(row.queryId, { filters: updated });
                        }}
                        placeholder="value or $var"
                        className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-1.5 py-0.5 text-[10px] flex-1 font-mono focus:outline-none focus:border-accent"
                      />
                    </div>
                  </div>
                ) : null
              )}

              {/* Group by */}
              <div className="grid grid-cols-[80px_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.groupBy', 'Group by')}
                </label>
                <input
                  type="text"
                  value={row.groupBy}
                  onChange={(e) => updateRow(row.queryId, { groupBy: e.target.value })}
                  placeholder="endpoint, status_code..."
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                />
              </div>

              {/* Function + Aggregation */}
              <div className="grid grid-cols-[80px_1fr_1fr] items-center gap-2">
                <label className="text-gray-500 dark:text-gray-400 text-right text-[11px]">
                  {t('governance.dashboardBuilder.queryBuilder.function', 'Function')}
                </label>
                <select
                  value={row.fn}
                  onChange={(e) => updateRow(row.queryId, { fn: e.target.value })}
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                >
                  {FUNCTIONS.map((f) => <option key={f} value={f}>{f}</option>)}
                </select>
                <select
                  value={row.aggFn}
                  onChange={(e) => updateRow(row.queryId, { aggFn: e.target.value })}
                  className="rounded border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-900 dark:text-white focus:outline-none focus:border-accent font-mono"
                >
                  {AGG_FUNCTIONS.map((f) => <option key={f} value={f}>{f}</option>)}
                </select>
              </div>
            </div>
          )}

          {/* NQL mode */}
          {row.mode === 'nql' && (
            <NqlMonacoEditor
              value={row.nqlText}
              onChange={(val) => updateRow(row.queryId, { nqlText: val })}
              height="180px"
            />
          )}
        </div>
      ))}

      {/* Add query button */}
      {rows.length < QUERY_IDS.length && (
        <button
          type="button"
          onClick={addRow}
          className="flex items-center justify-center gap-1.5 w-full rounded-lg border border-dashed border-gray-300 dark:border-gray-600 py-2 text-xs text-gray-500 dark:text-gray-400 hover:border-accent hover:text-accent transition-colors"
        >
          <Plus size={12} />
          {t('governance.dashboardBuilder.queryBuilder.addQuery', '+ Add Query')}
        </button>
      )}
    </div>
  );
}
