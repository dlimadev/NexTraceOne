/**
 * Barra de pesquisa + facetas para o surface de descoberta do catálogo.
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
import { SearchInput } from '../../../components/SearchInput';
import { FilterChip } from '../../../components/FilterChip';
import { Select } from '../../../components/Select';
import { Tabs } from '../../../components/Tabs';
import { IconButton } from '../../../components/IconButton';
import { Button } from '../../../components/Button';
import type {
  FacetGroups,
  CatalogFilters,
  ResultViewMode,
  Density,
  SortKey,
  Exposure,
  Lifecycle,
} from './catalogTypes';

/* ─── Props ──────────────────────────────────────────────────────────────────── */

export interface CatalogFacetBarProps {
  facets:       FacetGroups;
  filters:      CatalogFilters;
  /** Callback genérico de filtro — preserva tipagem por chave. */
  onSetFilter:  <K extends keyof CatalogFilters>(key: K, value: CatalogFilters[K]) => void;
  viewMode:     ResultViewMode;
  onViewMode:   (m: ResultViewMode) => void;
  sort:         SortKey;
  onSort:       (s: SortKey) => void;
  density:      Density;
  onDensity:    (d: Density) => void;
  onClearAll:   () => void;
  /** Número de resultados — exibido na barra de controlos. */
  resultCount:  number;
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

export function CatalogFacetBar({
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
}: CatalogFacetBarProps) {
  const { t } = useTranslation();

  const hasActiveFilters =
    filters.q !== '' ||
    filters.domains.length > 0 ||
    filters.protocols.length > 0 ||
    filters.exposures.length > 0 ||
    filters.lifecycles.length > 0 ||
    filters.teams.length > 0 ||
    filters.hasContract !== null;

  const sortOptions = [
    { value: 'relevance', label: t('serviceCatalog.browse.sort.relevance') },
    { value: 'name',      label: t('serviceCatalog.browse.sort.name') },
    { value: 'consumers', label: t('serviceCatalog.browse.sort.consumers') },
    { value: 'recent',    label: t('serviceCatalog.browse.sort.recent') },
  ];

  const viewItems = [
    { id: 'services', label: t('serviceCatalog.browse.viewAs.services') },
    { id: 'apis',     label: t('serviceCatalog.browse.viewAs.apis') },
  ];

  return (
    <div className="flex flex-col gap-3">

      {/* ── Pesquisa ──────────────────────────────────────────────────────── */}
      <SearchInput
        size="lg"
        value={filters.q}
        onChange={(e) => onSetFilter('q', e.target.value)}
        placeholder={t('serviceCatalog.browse.searchPlaceholder')}
        aria-label={t('serviceCatalog.browse.searchPlaceholder')}
      />

      {/* ── Grupos de facetas ─────────────────────────────────────────────── */}
      <div className="flex flex-col gap-2">

        {facets.domains.length > 0 && (
          <FacetGroup label={t('serviceCatalog.browse.facets.domain')}>
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

        {facets.protocols.length > 0 && (
          <FacetGroup label={t('serviceCatalog.browse.facets.protocol')}>
            {facets.protocols.map((f) => (
              <FilterChip
                key={f.value}
                label={f.label}
                count={f.count}
                active={filters.protocols.includes(f.value)}
                onClick={() =>
                  onSetFilter('protocols', toggleIn(filters.protocols, f.value))
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.exposures.length > 0 && (
          <FacetGroup label={t('serviceCatalog.browse.facets.exposure')}>
            {facets.exposures.map((f) => (
              <FilterChip
                key={f.value}
                label={t(`serviceCatalog.browse.exposure.${f.value.toLowerCase()}`)}
                count={f.count}
                active={filters.exposures.includes(f.value as Exposure)}
                onClick={() =>
                  onSetFilter(
                    'exposures',
                    toggleIn(filters.exposures, f.value as Exposure),
                  )
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.lifecycles.length > 0 && (
          <FacetGroup label={t('serviceCatalog.browse.facets.lifecycle')}>
            {facets.lifecycles.map((f) => (
              <FilterChip
                key={f.value}
                label={t(`serviceCatalog.browse.lifecycle.${f.value.toLowerCase()}`)}
                count={f.count}
                active={filters.lifecycles.includes(f.value as Lifecycle)}
                onClick={() =>
                  onSetFilter(
                    'lifecycles',
                    toggleIn(filters.lifecycles, f.value as Lifecycle),
                  )
                }
              />
            ))}
          </FacetGroup>
        )}

        {facets.teams.length > 0 && (
          <FacetGroup label={t('serviceCatalog.browse.facets.team')}>
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

      </div>

      {/* ── Controlos: ordenação, viewMode, densidade, limpar, contagem ───── */}
      <div className="flex items-center gap-2 flex-wrap">

        <Select
          options={sortOptions}
          value={sort}
          onChange={(e) => onSort(e.target.value as SortKey)}
          size="sm"
        />

        <Tabs
          items={viewItems}
          activeId={viewMode}
          onChange={(id) => onViewMode(id as ResultViewMode)}
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
              ? t('serviceCatalog.browse.density.compact')
              : t('serviceCatalog.browse.density.comfortable')
          }
          variant="outline"
          size="sm"
          onClick={() =>
            onDensity(density === 'comfortable' ? 'compact' : 'comfortable')
          }
        />

        <span className="ml-auto text-xs text-muted tabular-nums">
          {resultCount}
        </span>

        {hasActiveFilters && (
          <Button variant="ghost" size="xs" onClick={onClearAll}>
            {t('serviceCatalog.browse.clearAll')}
          </Button>
        )}

      </div>
    </div>
  );
}
