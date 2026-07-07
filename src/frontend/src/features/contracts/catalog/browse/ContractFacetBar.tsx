/**
 * Barra de pesquisa + facetas para o surface de descoberta de contratos.
 *
 * Inclui: SearchInput, chips de faceta (FilterChip), Select de ordenação,
 * segmento de viewMode (Tabs pill), toggle de densidade (IconButton)
 * e botão "limpar tudo" (condicional).
 *
 * Design system only — zero cores hardcoded, zero strings hardcoded.
 */
import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { LayoutList, LayoutGrid } from 'lucide-react';
import { SearchInput } from '../../../../components/SearchInput';
import { FilterChip } from '../../../../components/FilterChip';
import { Select } from '../../../../components/Select';
import { Tabs } from '../../../../components/Tabs';
import { IconButton } from '../../../../components/IconButton';
import { Button } from '../../../../components/Button';
import { isFiltersActive } from './contractBrowseAdapter';
import type {
  ContractFacetGroups,
  ContractBrowseFilters,
  ContractViewMode,
  ContractDensity,
  ContractSortKey,
} from './contractBrowseTypes';

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface ContractFacetBarProps {
  facets:      ContractFacetGroups;
  filters:     ContractBrowseFilters;
  /** Callback genérico de filtro — preserva tipagem por chave. */
  onSetFilter: <K extends keyof ContractBrowseFilters>(key: K, value: ContractBrowseFilters[K]) => void;
  viewMode:    ContractViewMode;
  onViewMode:  (m: ContractViewMode) => void;
  sort:        ContractSortKey;
  onSort:      (s: ContractSortKey) => void;
  density:     ContractDensity;
  onDensity:   (d: ContractDensity) => void;
  onClearAll:  () => void;
  /** Número de resultados exibido na barra de controlos. */
  resultCount: number;
}

/* ─── Utilitário ─────────────────────────────────────────────────────────────── */

/** Adiciona ou remove um valor de um array mantendo imutabilidade. */
function toggleIn<T>(arr: T[], value: T): T[] {
  return arr.includes(value)
    ? arr.filter((v) => v !== value)
    : [...arr, value];
}

/* ─── Sub-componente interno ─────────────────────────────────────────────────── */

function FacetGroup({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <span className="text-xs font-medium text-muted">{label}</span>
      <div className="flex flex-wrap gap-1.5">{children}</div>
    </div>
  );
}

/* ─── Componente principal ───────────────────────────────────────────────────── */

export function ContractFacetBar({
  facets,
  filters,
  onSetFilter,
  viewMode,
  onViewMode,
  sort,
  onSort,
  density,
  onDensity,
  onClearAll,
  resultCount,
}: ContractFacetBarProps) {
  const { t } = useTranslation();

  const hasActiveFilters = isFiltersActive(filters);

  const sortOptions = [
    { value: 'relevance',   label: t('contracts.catalog.browse.sort.relevance') },
    { value: 'name',        label: t('contracts.catalog.browse.sort.name') },
    { value: 'updated',     label: t('contracts.catalog.browse.sort.updated') },
    { value: 'criticality', label: t('contracts.catalog.browse.sort.criticality') },
  ];

  const viewItems = [
    { id: 'table', label: t('contracts.catalog.browse.viewAs.table') },
    { id: 'cards', label: t('contracts.catalog.browse.viewAs.cards') },
  ];

  return (
    <div className="flex flex-col gap-3">

      {/* ── Pesquisa ──────────────────────────────────────────────────────── */}
      <SearchInput
        size="lg"
        value={filters.q}
        onChange={(e) => onSetFilter('q', e.target.value)}
        placeholder={t('contracts.catalog.browse.searchPlaceholder')}
        aria-label={t('contracts.catalog.browse.searchPlaceholder')}
      />

      {/* ── Grupos de facetas ─────────────────────────────────────────────── */}
      <div className="flex flex-col gap-2">

        {facets.serviceTypes.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.serviceType')}>
            {facets.serviceTypes.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.serviceTypes.includes(f.value)}
                onClick={() =>
                  onSetFilter('serviceTypes', toggleIn(filters.serviceTypes, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.lifecycles.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.lifecycle')}>
            {facets.lifecycles.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.lifecycles.includes(f.value)}
                onClick={() =>
                  onSetFilter('lifecycles', toggleIn(filters.lifecycles, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.domains.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.domain')}>
            {facets.domains.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.domains.includes(f.value)}
                onClick={() =>
                  onSetFilter('domains', toggleIn(filters.domains, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.teams.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.team')}>
            {facets.teams.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.teams.includes(f.value)}
                onClick={() =>
                  onSetFilter('teams', toggleIn(filters.teams, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.criticalities.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.criticality')}>
            {facets.criticalities.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.criticalities.includes(f.value)}
                onClick={() =>
                  onSetFilter('criticalities', toggleIn(filters.criticalities, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.exposures.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.exposure')}>
            {facets.exposures.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.exposures.includes(f.value)}
                onClick={() =>
                  onSetFilter('exposures', toggleIn(filters.exposures, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.approvals.length > 0 && (
          <FacetGroup label={t('contracts.catalog.browse.facets.approval')}>
            {facets.approvals.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.approvals.includes(f.value)}
                onClick={() =>
                  onSetFilter('approvals', toggleIn(filters.approvals, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

      </div>

      {/* ── Controlos: ordenação, viewMode, densidade, limpar, contagem ───── */}
      <div className="flex items-center gap-2 flex-wrap">

        {/* Ordenação só é relevante nos cartões — na tabela, os cabeçalhos ordenam. */}
        {viewMode !== 'table' && (
          <Select
            options={sortOptions}
            value={sort}
            onChange={(e) => onSort(e.target.value as ContractSortKey)}
            size="sm"
          />
        )}

        <Tabs
          items={viewItems}
          activeId={viewMode}
          onChange={(id) => onViewMode(id as ContractViewMode)}
          variant="pill"
          size="sm"
        />

        <IconButton
          icon={
            density === 'comfortable'
              ? <LayoutList size={14} />
              : <LayoutGrid size={14} />
          }
          label={
            density === 'comfortable'
              ? t('contracts.catalog.browse.density.compact')
              : t('contracts.catalog.browse.density.comfortable')
          }
          variant="outline"
          size="sm"
          onClick={() =>
            onDensity(density === 'comfortable' ? 'compact' : 'comfortable')
          }
        />

        <span className="ml-auto text-xs text-muted tabular-nums">
          {t('contracts.catalog.browse.resultCount', { count: resultCount })}
        </span>

        {hasActiveFilters && (
          <Button variant="ghost" size="xs" onClick={onClearAll}>
            {t('contracts.catalog.browse.clearAll')}
          </Button>
        )}

      </div>
    </div>
  );
}
