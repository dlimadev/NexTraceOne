/**
 * ContractBrowseSurface — orquestrador do surface de descoberta de contratos.
 *
 * Compõe: adapter (Task 1) + hook de estado URL (Task 2) +
 * ContractFacetBar (Task 3) + ContractResultCard / renderTable prop (Task 4).
 *
 * Estados (precedência):
 * 1. loading                          → CardListSkeleton
 * 2. items.length === 0               → EmptyState onboarding
 * 3. filtered.length === 0 + filtros  → estado "sem resultados" + botão limpar
 * 4. viewMode === 'cards'             → grelha de ContractResultCard
 *    viewMode === 'table'             → renderTable(filtered)
 *
 * Design system only — zero cores hardcoded, zero strings hardcoded (i18n).
 * Chaves i18n introduzidas (novas — registadas no ficheiro de i18n):
 *   contracts.catalog.browse.empty.title
 *   contracts.catalog.browse.empty.desc
 *   contracts.catalog.browse.noResults.title
 *   contracts.catalog.browse.noResults.desc
 *   (contracts.catalog.browse.clearAll já usada por ContractFacetBar)
 */
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import type { ReactNode } from 'react';
import {
  computeContractFacets,
  filterContracts,
  sortContracts,
} from './contractBrowseAdapter';
import { useContractBrowseState } from './useContractBrowseState';
import { ContractFacetBar } from './ContractFacetBar';
import { ContractResultCard } from './ContractResultCard';
import { EmptyState } from '../../../../components/EmptyState';
import { CardListSkeleton } from '../../../../components/CardListSkeleton';
import { Button } from '../../../../components/Button';
import type { CatalogItem } from '../types';

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface ContractBrowseSurfaceProps {
  /** Lista plana de itens do catálogo de contratos. */
  items: CatalogItem[];
  /** Skeleton explícito — aciona o estado de carregamento. */
  loading?: boolean;
  /** Callback ao abrir um item (navega para detalhe/workspace). */
  onOpen: (item: CatalogItem) => void;
  /**
   * Render prop para o modo tabela.
   * A página fornece `<CatalogTable items sort onSort/>` pré-configurada;
   * o surface aplica filtragem/ordenação e passa os itens filtrados.
   */
  renderTable: (items: CatalogItem[]) => ReactNode;
}

/* ─── Componente ─────────────────────────────────────────────────────────────── */

export function ContractBrowseSurface({
  items,
  loading,
  onOpen,
  renderTable,
}: ContractBrowseSurfaceProps) {
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
  } = useContractBrowseState();

  /* ── Derivação de dados ── */

  const facets = useMemo(() => computeContractFacets(items), [items]);

  const filtered = useMemo(
    () => sortContracts(filterContracts(items, filters), sort),
    [items, filters, sort],
  );

  const hasActiveFilters =
    filters.q !== '' ||
    filters.serviceTypes.length > 0 ||
    filters.lifecycles.length > 0 ||
    filters.domains.length > 0 ||
    filters.teams.length > 0 ||
    filters.criticalities.length > 0 ||
    filters.exposures.length > 0 ||
    filters.approvals.length > 0;

  /* ── Estado 1: loading ── */
  if (loading) {
    return (
      <div className="flex flex-col gap-6">
        <CardListSkeleton count={4} showStats={false} />
      </div>
    );
  }

  /* ── Estado 2: genuinamente vazio (sem contratos registados) ── */
  if (items.length === 0) {
    return (
      <div className="flex flex-col gap-6">
        <EmptyState
          title={t('contracts.catalog.browse.empty.title')}
          description={t('contracts.catalog.browse.empty.desc')}
          variant="onboarding"
        />
      </div>
    );
  }

  /* ── Estados 3 + 4: barra de facetas + resultados (ou "sem resultados") ── */
  const isNoResults = filtered.length === 0 && hasActiveFilters;

  return (
    <div className="flex flex-col gap-6">

      {/* Barra de pesquisa + facetas + controlos */}
      <ContractFacetBar
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
          title={t('contracts.catalog.browse.noResults.title')}
          description={t('contracts.catalog.browse.noResults.desc')}
          action={
            <Button variant="outline" size="sm" onClick={clearAll}>
              {t('contracts.catalog.browse.clearAll')}
            </Button>
          }
        />
      )}

      {/* Estado 4a: grelha de cartões (viewMode = cards) */}
      {!isNoResults && viewMode === 'cards' && (
        <section
          aria-label={t('contracts.catalog.browse.viewAs.cards')}
          className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4"
        >
          {filtered.map((item) => (
            <ContractResultCard
              key={item.versionId ?? item.apiAssetId}
              item={item}
              density={density}
              onOpen={onOpen}
            />
          ))}
        </section>
      )}

      {/* Estado 4b: tabela (viewMode = table) — renderTable traz CatalogTable */}
      {!isNoResults && viewMode === 'table' && renderTable(filtered)}

    </div>
  );
}
