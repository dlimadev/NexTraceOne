import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';

// ── Types ──────────────────────────────────────────────────────────────────

export interface CrossFilter {
  serviceId?: string | null;
  teamId?: string | null;
  from?: string | null;
  to?: string | null;
  /** Which widget triggered this filter (so that widget doesn't filter itself) */
  sourceWidgetId?: string | null;
}

interface CrossFilterContextValue {
  filter: CrossFilter;
  hasFilter: boolean;
  applyFilter: (filter: CrossFilter) => void;
  clearFilter: () => void;
  /** Merge partial updates into the existing filter */
  patchFilter: (partial: Partial<CrossFilter>) => void;
}

const CrossFilterContext = createContext<CrossFilterContextValue | null>(null);

// ── Provider ──────────────────────────────────────────────────────────────

export function CrossFilterProvider({ children }: { children: ReactNode }) {
  const [filter, setFilter] = useState<CrossFilter>({});

  const hasFilter = Boolean(
    filter.serviceId || filter.teamId || filter.from || filter.to
  );

  const applyFilter = useCallback((f: CrossFilter) => setFilter(f), []);

  const clearFilter = useCallback(() => setFilter({}), []);

  const patchFilter = useCallback((partial: Partial<CrossFilter>) => {
    setFilter(prev => ({ ...prev, ...partial }));
  }, []);

  return (
    <CrossFilterContext.Provider value={{ filter, hasFilter, applyFilter, clearFilter, patchFilter }}>
      {children}
    </CrossFilterContext.Provider>
  );
}

// ── Hook ──────────────────────────────────────────────────────────────────

export function useCrossFilter(): CrossFilterContextValue {
  const ctx = useContext(CrossFilterContext);
  if (!ctx) throw new Error('useCrossFilter must be used within <CrossFilterProvider>');
  return ctx;
}
