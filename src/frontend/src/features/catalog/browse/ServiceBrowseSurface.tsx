/**
 * ServiceBrowseSurface — orquestrador do surface de descoberta do catálogo.
 *
 * Compõe: adapter (Task 1) + hook de estado URL (Task 2) +
 * CatalogFacetBar (Task 3) + ServiceResultCard / ApiResultRow (Task 4).
 *
 * Estados (precedência):
 * 1. loading / graph indefinido  → CardListSkeleton
 * 2. services.length === 0       → EmptyState (onboarding — sem serviços registados)
 * 3. filtered.length === 0 com filtros activos → estado "sem resultados" + botão limpar
 * 4. otherwise                   → grelha de ServiceResultCard (services) ou
 *                                  lista de ApiResultRow (apis)
 *
 * Design system only — zero cores hardcoded, zero strings hardcoded (i18n).
 * Chaves i18n introduzidas (novas — registar no ficheiro de i18n):
 *   serviceCatalog.browse.empty.title
 *   serviceCatalog.browse.empty.desc
 *   serviceCatalog.browse.noResults.title
 *   serviceCatalog.browse.noResults.desc
 *   (serviceCatalog.browse.clearAll já utilizado por CatalogFacetBar)
 */
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { AssetGraph } from '../../../types';
import {
  toServiceVMs,
  computeFacets,
  filterServices,
  sortServices,
  toApiVMs,
} from './catalogAdapter';
import { useCatalogBrowseState } from './useCatalogBrowseState';
import { CatalogFacetBar } from './CatalogFacetBar';
import { ServiceResultCard } from './ServiceResultCard';
import { ApiResultRow } from './ApiResultRow';
import { EmptyState } from '../../../components/EmptyState';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { Button } from '../../../components/Button';

/* ─── Constante estável fora do componente (evita deps instáveis no useMemo) ── */
const EMPTY_ASSET_GRAPH: AssetGraph = { services: [], apis: [] };

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface ServiceBrowseSurfaceProps {
  /** Grafo de activos. Undefined enquanto a query carrega (aciona skeleton). */
  graph?: AssetGraph;
  /** Skeleton explícito — útil quando graph ainda não chegou mas loading está activo. */
  loading?: boolean;
  onOpenService:  (id: string) => void;
  onOpenApi:      (id: string) => void;
  onViewContract: (apiId: string) => void;
  /** CTA de registo de serviço no estado-vazio de onboarding (grafo sem serviços). */
  onRegisterService?: () => void;
}

/* ─── Componente ─────────────────────────────────────────────────────────────── */

export function ServiceBrowseSurface({
  graph,
  loading,
  onOpenService,
  onOpenApi,
  onViewContract,
  onRegisterService,
}: ServiceBrowseSurfaceProps) {
  const { t } = useTranslation();

  const {
    filters,
    setFilter,
    clearAll,
    viewMode,
    setViewMode,
    density,
    setDensity,
    sort,
    setSort,
  } = useCatalogBrowseState();

  /* ── Derivação de dados ── */

  const services = useMemo(
    () => toServiceVMs(graph ?? EMPTY_ASSET_GRAPH),
    [graph],
  );

  const facets = useMemo(() => computeFacets(services), [services]);

  const filtered = useMemo(
    () => sortServices(filterServices(services, filters), sort),
    [services, filters, sort],
  );

  const apiVMs = useMemo(
    () => (viewMode === 'apis' ? toApiVMs(filtered) : []),
    [viewMode, filtered],
  );

  const hasActiveFilters =
    filters.q !== '' ||
    filters.domains.length > 0 ||
    filters.protocols.length > 0 ||
    filters.exposures.length > 0 ||
    filters.lifecycles.length > 0 ||
    filters.teams.length > 0 ||
    filters.hasContract !== null;

  /* ── Estado 1: loading / graph ainda indefinido ── */
  if (loading || graph === undefined) {
    return (
      <div className="flex flex-col gap-6">
        <CardListSkeleton count={4} showStats={false} />
      </div>
    );
  }

  /* ── Estado 2: graph carregado mas sem serviços (grafo genuinamente vazio) ── */
  if (services.length === 0) {
    return (
      <div className="flex flex-col gap-6">
        <EmptyState
          title={t('serviceCatalog.browse.empty.title')}
          description={t('serviceCatalog.browse.empty.desc')}
          variant="onboarding"
          action={
            onRegisterService ? (
              <Button variant="primary" size="sm" onClick={onRegisterService}>
                {t('serviceCatalog.registerService')}
              </Button>
            ) : undefined
          }
        />
      </div>
    );
  }

  /* ── Estados 3 + 4: barra de facetas + resultados (ou "sem resultados") ── */
  const isNoResults = filtered.length === 0 && hasActiveFilters;

  return (
    <div className="flex flex-col gap-6">

      {/* Barra de pesquisa + facetas + controlos */}
      <CatalogFacetBar
        facets={facets}
        filters={filters}
        onSetFilter={setFilter}
        viewMode={viewMode}
        onViewMode={setViewMode}
        sort={sort}
        onSort={setSort}
        density={density}
        onDensity={setDensity}
        onClearAll={clearAll}
        resultCount={filtered.length}
      />

      {/* Estado 3: sem resultados com filtros activos */}
      {isNoResults && (
        <EmptyState
          size="compact"
          title={t('serviceCatalog.browse.noResults.title')}
          description={t('serviceCatalog.browse.noResults.desc')}
          action={
            <Button variant="outline" size="sm" onClick={clearAll}>
              {t('serviceCatalog.browse.clearAll')}
            </Button>
          }
        />
      )}

      {/* Estado 4a: grelha de serviços (viewMode = services) */}
      {!isNoResults && viewMode === 'services' && (
        <section
          aria-label={t('serviceCatalog.browse.viewAs.services')}
          className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4"
        >
          {filtered.map((svc) => (
            <ServiceResultCard
              key={svc.id}
              service={svc}
              density={density}
              onOpenService={onOpenService}
              onOpenApi={onOpenApi}
              onViewContract={onViewContract}
            />
          ))}
        </section>
      )}

      {/* Estado 4b: lista de APIs (viewMode = apis) */}
      {!isNoResults && viewMode === 'apis' && (
        <section
          aria-label={t('serviceCatalog.browse.viewAs.apis')}
          className="flex flex-col gap-2"
        >
          {apiVMs.map((api) => (
            <ApiResultRow
              key={api.id}
              api={api}
              onOpenApi={onOpenApi}
              onViewContract={onViewContract}
            />
          ))}
        </section>
      )}

    </div>
  );
}
