/**
 * Barra de ferramentas do catálogo — pesquisa + filtros compactos + contagem.
 *
 * Utiliza componentes base NTO (SearchInput) e selects inline compactos
 * para manter a densidade de informação sem comprometer a usabilidade.
 * Dados de filtro (domains, teams, owners) vêm do backend real.
 */
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { X, SlidersHorizontal } from 'lucide-react';
import { SearchInput } from '../../../../components/SearchInput';
import { cn } from '../../../../lib/cn';
import { PROTOCOLS, LIFECYCLE_STATES, SERVICE_TYPES } from '../../shared/constants';
import type { CatalogFilters, CatalogServiceType } from '../types';
import { EMPTY_FILTERS, activeFilterCount } from '../types';

const SEARCH_DEBOUNCE_MS = 350;

const APPROVAL_STATES = ['Pending', 'InReview', 'Approved', 'Rejected', 'Escalated'] as const;
const CRITICALITIES = ['Low', 'Medium', 'High', 'Critical'] as const;

interface CatalogToolbarProps {
  filters: CatalogFilters;
  onChange: (filters: CatalogFilters) => void;
  resultCount: number;
  dynamicOptions?: {
    domains: string[];
    teams: string[];
    owners: string[];
    serviceTypes: CatalogServiceType[];
    exposures: string[];
  };
}

export function CatalogToolbar({
  filters,
  onChange,
  resultCount,
  dynamicOptions,
}: CatalogToolbarProps) {
  const { t } = useTranslation();
  const [expanded, setExpanded] = useState(false);
  const [localSearch, setLocalSearch] = useState(filters.search);

  useEffect(() => {
    const timer = setTimeout(
      () => onChange({ ...filters, search: localSearch }),
      SEARCH_DEBOUNCE_MS,
    );
    return () => clearTimeout(timer);
  }, [localSearch]);

  const filterCount = activeFilterCount(filters);

  const set = (key: keyof CatalogFilters, value: string) =>
    onChange({ ...filters, [key]: value });

  const clearAll = () => {
    setLocalSearch('');
    onChange(EMPTY_FILTERS);
  };

  return (
    <div className="space-y-3">
      {/* Primary row: search + expand toggle + count */}
      <div className="flex items-center gap-3">
        <SearchInput
          size="sm"
          value={localSearch}
          onChange={(e) => setLocalSearch(e.target.value)}
          placeholder={t('contracts.catalog.searchPlaceholder', 'Search contracts, APIs, services...')}
          className="flex-1 max-w-md"
        />

        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className={cn(
            'inline-flex items-center gap-1.5 px-3 py-2 rounded-lg border text-xs font-medium transition-colors',
            expanded || filterCount > 0
              ? 'bg-accent-muted text-cyan border-edge-focus'
              : 'bg-elevated text-muted border-edge hover:border-edge-strong hover:text-body',
          )}
          style={{ transitionDuration: 'var(--nto-motion-fast)' }}
        >
          <SlidersHorizontal size={13} />
          {t('contracts.catalog.filters.label', 'Filters')}
          {filterCount > 0 && (
            <span className="inline-flex items-center justify-center min-w-[18px] h-[18px] rounded-pill px-1 text-[10px] font-semibold bg-cyan/20 text-cyan">
              {filterCount}
            </span>
          )}
        </button>

        {filterCount > 0 && (
          <button
            type="button"
            onClick={clearAll}
            className="inline-flex items-center gap-1 text-xs text-muted hover:text-body transition-colors"
          >
            <X size={12} />
            {t('contracts.catalog.filters.clearAll', 'Clear')}
          </button>
        )}

        <span className="ml-auto text-xs text-muted tabular-nums">
          {resultCount} {t('common.total', 'total')}
        </span>
      </div>

      {/* Expanded filter row */}
      {expanded && (
        <div className="flex items-center gap-2 flex-wrap animate-fade-in">
          <InlineSelect
            value={filters.serviceType}
            onChange={(v) => set('serviceType', v)}
            options={SERVICE_TYPES.map((s) => ({
              value: s.value,
              label: t(s.labelKey, s.value),
            }))}
            placeholder={t('contracts.catalog.filters.allTypes', 'All types')}
          />

          <InlineSelect
            value={filters.protocol}
            onChange={(v) => set('protocol', v)}
            options={PROTOCOLS.map((p) => ({
              value: p,
              label: t(`contracts.protocols.${p}`, p),
            }))}
            placeholder={t('contracts.catalog.filters.allProtocols', 'All protocols')}
          />

          <InlineSelect
            value={filters.lifecycle}
            onChange={(v) => set('lifecycle', v)}
            options={LIFECYCLE_STATES.map((s) => ({
              value: s,
              label: t(`contracts.lifecycleStates.${s}`, s),
            }))}
            placeholder={t('contracts.catalog.filters.allStates', 'All states')}
          />

          <InlineSelect
            value={filters.approvalState}
            onChange={(v) => set('approvalState', v)}
            options={APPROVAL_STATES.map((s) => ({
              value: s,
              label: t(`contracts.catalog.approvalStates.${s}`, s),
            }))}
            placeholder={t('contracts.catalog.filters.allApprovals', 'All approvals')}
          />

          {dynamicOptions && (
            <>
              <InlineSelect
                value={filters.domain}
                onChange={(v) => set('domain', v)}
                options={dynamicOptions.domains.map((d) => ({ value: d, label: d }))}
                placeholder={t('contracts.catalog.filters.allDomains', 'All domains')}
              />

              <InlineSelect
                value={filters.owner}
                onChange={(v) => set('owner', v)}
                options={dynamicOptions.owners.map((o) => ({ value: o, label: o }))}
                placeholder={t('contracts.catalog.filters.allOwners', 'All owners')}
              />

              <InlineSelect
                value={filters.team}
                onChange={(v) => set('team', v)}
                options={dynamicOptions.teams.map((t) => ({ value: t, label: t }))}
                placeholder={t('contracts.catalog.filters.allTeams', 'All teams')}
              />

              <InlineSelect
                value={filters.exposure}
                onChange={(v) => set('exposure', v)}
                options={dynamicOptions.exposures.map((e) => ({ value: e, label: e }))}
                placeholder={t('contracts.catalog.filters.allExposure', 'All exposure')}
              />

              <InlineSelect
                value={filters.risk}
                onChange={(v) => set('risk', v)}
                options={CRITICALITIES.map((c) => ({
                  value: c,
                  label: t(`contracts.catalog.criticality.${c}`, c),
                }))}
                placeholder={t('contracts.catalog.filters.allRisks', 'All risks')}
              />
            </>
          )}
        </div>
      )}
    </div>
  );
}

// ── Inline select (compact) ──────────────────────────────────────────────────

function InlineSelect({
  value,
  onChange,
  options,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  placeholder: string;
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className={cn(
        'h-8 text-xs rounded-lg border px-2.5 pr-7 appearance-none bg-elevated transition-colors',
        'focus:outline-none focus:border-edge-focus focus:shadow-glow-cyan',
        value ? 'text-heading border-edge-focus' : 'text-muted border-edge',
      )}
      style={{ transitionDuration: 'var(--nto-motion-fast)' }}
    >
      <option value="">{placeholder}</option>
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}
