/**
 * DashboardVariablesPanel — painel de variáveis de template estilo Grafana.
 * Resolve valores dinâmicos do backend (services, teams, environments)
 * e permite multi-select, busca e cascading filters.
 */
import { useState, useEffect, useRef, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { SlidersHorizontal, X, ChevronDown, Search, Check } from 'lucide-react';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

export interface DashboardVariableValue {
  key: string;
  label: string;
  values: string[];
  allowMultiple: boolean;
}

interface DashboardVariablesPanelProps {
  dashboardId: string;
  tenantId: string;
  environmentId?: string | null;
  values: Record<string, string[]>;
  onChange: (key: string, values: string[]) => void;
  onClearAll: () => void;
}

interface ResolvedVariableApi {
  key: string;
  label: string;
  type: string;
  defaultValue: string | null;
  values: string[];
  allowMultiple: boolean;
}

// ── API hook ───────────────────────────────────────────────────────────────

const useResolveVariables = (dashboardId: string, tenantId: string, environmentId?: string | null) =>
  useQuery({
    queryKey: ['dashboard-variables', dashboardId, tenantId, environmentId],
    queryFn: () =>
      client
        .get<{ variables: ResolvedVariableApi[] }>(`/governance/dashboards/${dashboardId}/variables`, {
          params: { tenantId, environmentId },
        })
        .then((r) => r.data.variables),
    enabled: Boolean(dashboardId),
  });

// ── Dropdown component ─────────────────────────────────────────────────────

interface VariableDropdownProps {
  variable: ResolvedVariableApi;
  selected: string[];
  onChange: (values: string[]) => void;
}

function VariableDropdown({ variable, selected, onChange }: VariableDropdownProps) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    function handleOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleOutside);
    return () => document.removeEventListener('mousedown', handleOutside);
  }, [isOpen]);

  const filtered = variable.values.filter(v =>
    v.toLowerCase().includes(search.toLowerCase())
  );

  const displayLabel = selected.length === 0
    ? t('governance.dashboardView.varAll', 'All')
    : selected.length === 1
      ? selected[0]
      : t('governance.dashboardView.varSelectedCount', '{{count}} selected', { count: selected.length });

  const toggleValue = useCallback((val: string) => {
    if (variable.allowMultiple) {
      if (selected.includes(val)) {
        onChange(selected.filter(s => s !== val));
      } else {
        onChange([...selected, val]);
      }
    } else {
      onChange([val]);
      setIsOpen(false);
    }
  }, [selected, variable.allowMultiple, onChange]);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setIsOpen(v => !v)}
        className="flex items-center gap-1.5 rounded border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 px-2 py-1 text-xs text-gray-700 dark:text-gray-300 hover:border-accent transition-colors min-w-[120px]"
      >
        <span className="font-medium text-accent">${variable.key}</span>
        <span className="truncate max-w-[100px]">{displayLabel}</span>
        <ChevronDown size={10} className={`shrink-0 text-gray-400 transition-transform ${isOpen ? 'rotate-180' : ''}`} />
      </button>

      {isOpen && (
        <div className="absolute top-full left-0 z-50 mt-1 w-56 rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-xl">
          <div className="p-2 border-b border-gray-100 dark:border-gray-800">
            <div className="flex items-center gap-1 rounded border border-gray-200 dark:border-gray-600 bg-gray-50 dark:bg-gray-800 px-1.5 py-1">
              <Search size={10} className="text-gray-400 shrink-0" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={t('common.search', 'Search...')}
                className="bg-transparent text-xs text-gray-900 dark:text-white focus:outline-none w-full"
                autoFocus
              />
            </div>
          </div>
          <div className="max-h-48 overflow-y-auto p-1">
            {variable.allowMultiple && (
              <button
                type="button"
                onClick={() => onChange([])}
                className="flex items-center gap-2 w-full px-2 py-1.5 text-xs text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800 rounded"
              >
                <span className={`w-3.5 h-3.5 rounded border ${selected.length === 0 ? 'bg-accent border-accent' : 'border-gray-300 dark:border-gray-600'} flex items-center justify-center`}>
                  {selected.length === 0 && <Check size={10} className="text-white" />}
                </span>
                {t('governance.dashboardView.varAll', 'All')}
              </button>
            )}
            {filtered.map(val => (
              <button
                key={val}
                type="button"
                onClick={() => toggleValue(val)}
                className="flex items-center gap-2 w-full px-2 py-1.5 text-xs text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 rounded"
              >
                {variable.allowMultiple ? (
                  <span className={`w-3.5 h-3.5 rounded border ${selected.includes(val) ? 'bg-accent border-accent' : 'border-gray-300 dark:border-gray-600'} flex items-center justify-center shrink-0`}>
                    {selected.includes(val) && <Check size={10} className="text-white" />}
                  </span>
                ) : (
                  <span className={`w-3.5 h-3.5 rounded-full border ${selected.includes(val) ? 'bg-accent border-accent' : 'border-gray-300 dark:border-gray-600'} flex items-center justify-center shrink-0`}>
                    {selected.includes(val) && <span className="w-1.5 h-1.5 rounded-full bg-white" />}
                  </span>
                )}
                <span className="truncate">{val}</span>
              </button>
            ))}
            {filtered.length === 0 && (
              <p className="text-xs text-gray-400 text-center py-2">
                {t('common.noResults', 'No results')}
              </p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Main component ─────────────────────────────────────────────────────────

export function DashboardVariablesPanel({
  dashboardId,
  tenantId,
  environmentId,
  values,
  onChange,
  onClearAll,
}: DashboardVariablesPanelProps) {
  const { t } = useTranslation();
  const { data: variables, isLoading } = useResolveVariables(dashboardId, tenantId, environmentId);

  const hasValues = Object.values(values).some(v => v.length > 0);

  if (isLoading) {
    return (
      <div className="mb-4 rounded-lg border border-accent/30 bg-accent/5 px-4 py-3 flex items-center gap-4">
        <SlidersHorizontal size={14} className="text-accent animate-pulse" />
        <span className="text-xs text-gray-500">
          {t('governance.dashboardView.loadingVariables', 'Loading variables...')}
        </span>
      </div>
    );
  }

  if (!variables || variables.length === 0) {
    return null;
  }

  return (
    <div className="mb-4 rounded-lg border border-accent/30 bg-accent/5 px-4 py-3 flex flex-wrap items-center gap-3">
      <span className="text-xs font-semibold text-accent flex items-center gap-1 shrink-0">
        <SlidersHorizontal size={12} />
        {t('governance.dashboardView.variablesLabel', 'Variables')}
      </span>

      {variables.map(v => (
        <VariableDropdown
          key={v.key}
          variable={v}
          selected={values[v.key] ?? []}
          onChange={(vals) => onChange(v.key, vals)}
        />
      ))}

      {hasValues && (
        <Button
          size="sm"
          variant="secondary"
          onClick={onClearAll}
          className="text-xs"
        >
          <X size={10} className="mr-1" />
          {t('governance.dashboardView.clearVars', 'Clear')}
        </Button>
      )}

      <span className="text-[10px] text-gray-400 ml-auto">
        {t('governance.dashboardView.varsHint', 'Variable values override per-widget filters')}
      </span>
    </div>
  );
}
