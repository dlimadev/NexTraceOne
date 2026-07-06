/**
 * Hook de estado da descoberta de contratos sincronizado com o URL.
 *
 * Todos os filtros, viewMode, density e sort são espelhados nos search params:
 *   q, type, lifecycle, domain, team, crit, exposure, approval, view, density, sort
 *
 * Arrays são serializados como CSV (ex.: "payments,orders").
 * Array vazio → param removido (sem `type=` em branco no URL).
 *
 * setFilter é genérico sobre as chaves de ContractBrowseFilters (sem `any`).
 * clearAll apaga todos os filtros preservando 'view' e 'density'.
 */
import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type {
  ContractBrowseFilters,
  ContractDensity,
  ContractSortKey,
  ContractViewMode,
} from './contractBrowseTypes';

// Mapa de chave ContractBrowseFilters → nome do param URL para campos array.
type ArrayFilterKey = Exclude<keyof ContractBrowseFilters, 'q'>;

const ARRAY_PARAM: Record<ArrayFilterKey, string> = {
  serviceTypes: 'type',
  lifecycles: 'lifecycle',
  domains: 'domain',
  teams: 'team',
  criticalities: 'crit',
  exposures: 'exposure',
  approvals: 'approval',
};

function csvToArray(value: string | null): string[] {
  if (!value) return [];
  return value.split(',').filter(Boolean);
}

function arrayToCsv(arr: readonly string[]): string {
  return arr.join(',');
}

export function useContractBrowseState() {
  const [searchParams, setSearchParams] = useSearchParams();

  // --- Leitura derivada dos search params ---

  const filters = useMemo<ContractBrowseFilters>(() => ({
    q: searchParams.get('q') ?? '',
    serviceTypes: csvToArray(searchParams.get('type')),
    lifecycles: csvToArray(searchParams.get('lifecycle')),
    domains: csvToArray(searchParams.get('domain')),
    teams: csvToArray(searchParams.get('team')),
    criticalities: csvToArray(searchParams.get('crit')),
    exposures: csvToArray(searchParams.get('exposure')),
    approvals: csvToArray(searchParams.get('approval')),
  }), [searchParams]);

  const viewMode = useMemo<ContractViewMode>(() => {
    const rawView = searchParams.get('view');
    return rawView === 'cards' ? 'cards' : 'table';
  }, [searchParams]);

  const density = useMemo<ContractDensity>(
    () => (searchParams.get('density') as ContractDensity | null) ?? 'comfortable',
    [searchParams],
  );

  const sort = useMemo<ContractSortKey>(
    () => (searchParams.get('sort') as ContractSortKey | null) ?? 'relevance',
    [searchParams],
  );

  // --- Escritores (preservam os outros params via functional update) ---

  /**
   * Actualiza um único filtro no URL preservando todos os outros params.
   * Tipado genericamente sobre as chaves de ContractBrowseFilters — sem `any`.
   */
  const setFilter = useCallback(
    <K extends keyof ContractBrowseFilters>(key: K, value: ContractBrowseFilters[K]) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);

        if (key === 'q') {
          const v = value as string;
          if (v) {
            next.set('q', v);
          } else {
            next.delete('q');
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
    (mode: ContractViewMode) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('view', mode);
        return next;
      });
    },
    [setSearchParams],
  );

  const setDensity = useCallback(
    (d: ContractDensity) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.set('density', d);
        return next;
      });
    },
    [setSearchParams],
  );

  const setSort = useCallback(
    (s: ContractSortKey) => {
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
