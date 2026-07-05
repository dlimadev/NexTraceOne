/**
 * Hook de estado do catálogo sincronizado com o URL.
 *
 * Todos os filtros, viewMode, density e sort são espelhados nos search params:
 *   q, domain, protocol, exposure, lifecycle, contract, team, view, density, sort
 *
 * Arrays são serializados como CSV (e.g. "payments,orders").
 * hasContract: '1' → true | '0' → false | ausente → null.
 *
 * setFilter é genérico sobre as chaves de CatalogFilters (sem `any`).
 * clearAll apaga todos os filtros preservando 'view' e 'density'.
 */
import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type {
  CatalogFilters,
  Density,
  Exposure,
  Lifecycle,
  ResultViewMode,
  SortKey,
} from './catalogTypes';

// Mapa de chave CatalogFilters → nome do param URL para campos array.
type ArrayFilterKey = Exclude<keyof CatalogFilters, 'q' | 'hasContract'>;

const ARRAY_PARAM: Record<ArrayFilterKey, string> = {
  domains: 'domain',
  protocols: 'protocol',
  exposures: 'exposure',
  lifecycles: 'lifecycle',
  teams: 'team',
};

function csvToArray(value: string | null): string[] {
  if (!value) return [];
  return value.split(',').filter(Boolean);
}

function arrayToCsv(arr: readonly string[]): string {
  return arr.join(',');
}

export function useCatalogBrowseState() {
  const [searchParams, setSearchParams] = useSearchParams();

  // --- Leitura derivada dos search params ---

  const filters = useMemo<CatalogFilters>(() => {
    const contractParam = searchParams.get('contract');
    return {
      q: searchParams.get('q') ?? '',
      domains: csvToArray(searchParams.get('domain')),
      protocols: csvToArray(searchParams.get('protocol')),
      exposures: csvToArray(searchParams.get('exposure')) as Exposure[],
      lifecycles: csvToArray(searchParams.get('lifecycle')) as Lifecycle[],
      teams: csvToArray(searchParams.get('team')),
      hasContract:
        contractParam === '1' ? true
        : contractParam === '0' ? false
        : null,
    };
  }, [searchParams]);

  const viewMode = useMemo<ResultViewMode>(
    () => (searchParams.get('view') as ResultViewMode | null) ?? 'services',
    [searchParams],
  );

  const density = useMemo<Density>(
    () => (searchParams.get('density') as Density | null) ?? 'comfortable',
    [searchParams],
  );

  const sort = useMemo<SortKey>(
    () => (searchParams.get('sort') as SortKey | null) ?? 'relevance',
    [searchParams],
  );

  // --- Escritores (preservam os outros params via functional update) ---

  /**
   * Actualiza um único filtro no URL preservando todos os outros params.
   * Tipado genericamente sobre as chaves de CatalogFilters — sem `any`.
   */
  const setFilter = useCallback(
    <K extends keyof CatalogFilters>(key: K, value: CatalogFilters[K]) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);

        if (key === 'q') {
          const v = value as string;
          if (v) {
            next.set('q', v);
          } else {
            next.delete('q');
          }
        } else if (key === 'hasContract') {
          const v = value as boolean | null;
          if (v === true) {
            next.set('contract', '1');
          } else if (v === false) {
            next.set('contract', '0');
          } else {
            next.delete('contract');
          }
        } else {
          const paramKey = ARRAY_PARAM[key as ArrayFilterKey];
          const arr = value as readonly string[];
          if (arr.length > 0) {
            next.set(paramKey, arrayToCsv(arr));
          } else {
            next.delete(paramKey);
          }
        }

        return next;
      });
    },
    [setSearchParams],
  );

  const setViewMode = useCallback(
    (mode: ResultViewMode) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('view', mode);
        return next;
      });
    },
    [setSearchParams],
  );

  const setDensity = useCallback(
    (d: Density) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('density', d);
        return next;
      });
    },
    [setSearchParams],
  );

  const setSort = useCallback(
    (s: SortKey) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('sort', s);
        return next;
      });
    },
    [setSearchParams],
  );

  /**
   * Limpa todos os filtros mas preserva 'view' e 'density'.
   */
  const clearAll = useCallback(() => {
    setSearchParams((prev) => {
      const next = new URLSearchParams();
      const view = prev.get('view');
      const den = prev.get('density');
      if (view) next.set('view', view);
      if (den) next.set('density', den);
      return next;
    });
  }, [setSearchParams]);

  return {
    filters,
    setFilter,
    clearAll,
    viewMode,
    setViewMode,
    density,
    setDensity,
    sort,
    setSort,
  };
}
