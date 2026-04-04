import { useState, useMemo, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Search,
  Database,
  Users,
  ChevronRight,
  Server,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { legacyAssetsApi } from '../api/legacyAssets';
import type { LegacyAssetFilters } from '../api/legacyAssets';

/** Variantes visuais para badges de criticidade. */
const criticalityColors: Record<string, string> = {
  Critical: 'bg-critical/15 text-critical border border-critical/25',
  High: 'bg-warning/15 text-warning border border-warning/25',
  Medium: 'bg-warning/15 text-warning border border-warning/25',
  Low: 'bg-elevated text-muted border border-edge',
};

/** Variantes visuais para badges de ciclo de vida. */
const lifecycleColors: Record<string, string> = {
  Planning: 'bg-info/15 text-info border border-info/25',
  Development: 'bg-info/15 text-accent border border-accent',
  Staging: 'bg-info/15 text-info border border-info/25',
  Active: 'bg-success/15 text-success border border-success/25',
  Deprecating: 'bg-warning/15 text-warning border border-warning/25',
  Deprecated: 'bg-warning/15 text-warning border border-warning/25',
  Retired: 'bg-elevated text-muted border border-edge',
};

/** Valores disponíveis nos filtros de tipo de ativo. */
const ASSET_TYPES = [
  'mainframeSystem',
  'cobolProgram',
  'copybook',
  'cicsTransaction',
  'imsTransaction',
  'db2Artifact',
  'zosConnectBinding',
] as const;

/** Valores disponíveis nos filtros de criticidade. */
const CRITICALITY_VALUES = ['Low', 'Medium', 'High', 'Critical'] as const;

/** Interface dos filtros ativos na listagem. */
interface Filters {
  search: string;
  assetType: string;
  teamName: string;
  domain: string;
  criticality: string;
  lifecycleStatus: string;
}

const emptyFilters: Filters = {
  search: '',
  assetType: '',
  teamName: '',
  domain: '',
  criticality: '',
  lifecycleStatus: '',
};

/** Intervalo de debounce para pesquisa (ms). */
const SEARCH_DEBOUNCE_MS = 350;

/** Página principal de listagem do catálogo de ativos legacy. */
export function LegacyAssetCatalogPage() {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState('');

  /** Debounce da pesquisa para evitar chamadas excessivas à API. */
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(filters.search), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [filters.search]);

  /** Parâmetros enviados à API — omite chaves vazias. */
  const queryParams = useMemo(() => {
    const p: LegacyAssetFilters = {};
    if (debouncedSearch) p.searchTerm = debouncedSearch;
    if (filters.teamName) p.teamName = filters.teamName;
    if (filters.domain) p.domain = filters.domain;
    if (filters.criticality) p.criticality = filters.criticality;
    if (filters.lifecycleStatus) p.lifecycleStatus = filters.lifecycleStatus;
    return p;
  }, [debouncedSearch, filters.teamName, filters.domain, filters.criticality, filters.lifecycleStatus]);

  const {
    data: assets,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['legacy-assets', queryParams],
    queryFn: () => legacyAssetsApi.list(queryParams),
  });

  /** Filtra por tipo de ativo no lado do cliente quando a API não suporta o filtro. */
  const filteredAssets = useMemo(() => {
    if (!assets) return [];
    if (!filters.assetType) return assets;
    return assets.filter(
      (a) => a.assetType.toLowerCase() === filters.assetType.toLowerCase(),
    );
  }, [assets, filters.assetType]);

  /** Atualiza um campo de filtro individual. */
  const setFilter = (key: keyof Filters, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <PageContainer>
      {/* ── Cabeçalho ── */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('legacyCatalog.title')}</h1>
        <p className="text-muted mt-1">{t('legacyCatalog.subtitle')}</p>
      </div>

      {/* ── Pesquisa e filtros + Tabela ── */}
      <PageSection>
        <Card className="mb-6">
          <CardBody>
            <div className="flex flex-col gap-4">
              {/* Barra de pesquisa */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" size={16} />
                <input
                  type="text"
                  className="w-full pl-10 pr-4 py-2 bg-elevated border border-edge rounded-md text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                  placeholder={t('legacyCatalog.filters.search')}
                  value={filters.search}
                  onChange={(e) => setFilter('search', e.target.value)}
                />
              </div>

              {/* Filtros dropdown */}
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
                <FilterSelect
                  label={t('legacyCatalog.filters.assetType')}
                  value={filters.assetType}
                  onChange={(v) => setFilter('assetType', v)}
                  options={ASSET_TYPES.map((v) => ({ value: v, label: t(`legacyCatalog.assetTypes.${v}`) }))}
                  allLabel={t('legacyCatalog.filters.allTypes')}
                />
                <FilterSelect
                  label={t('legacyCatalog.filters.criticality')}
                  value={filters.criticality}
                  onChange={(v) => setFilter('criticality', v)}
                  options={CRITICALITY_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.criticality.${v}`) }))}
                  allLabel={t('legacyCatalog.filters.allTypes')}
                />
                <div className="flex flex-col gap-1">
                  <label className="text-xs text-muted">{t('legacyCatalog.filters.teamName')}</label>
                  <input
                    type="text"
                    className="w-full px-3 py-1.5 bg-elevated border border-edge rounded-md text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                    placeholder={t('legacyCatalog.filters.teamName')}
                    value={filters.teamName}
                    onChange={(e) => setFilter('teamName', e.target.value)}
                  />
                </div>
                <div className="flex flex-col gap-1">
                  <label className="text-xs text-muted">{t('legacyCatalog.filters.domain')}</label>
                  <input
                    type="text"
                    className="w-full px-3 py-1.5 bg-elevated border border-edge rounded-md text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                    placeholder={t('legacyCatalog.filters.domain')}
                    value={filters.domain}
                    onChange={(e) => setFilter('domain', e.target.value)}
                  />
                </div>
              </div>
            </div>
          </CardBody>
        </Card>

        {/* ── Tabela de ativos legacy ── */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Database size={18} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('legacyCatalog.title')}</h2>
                {filteredAssets.length > 0 && (
                  <span className="text-xs text-muted ml-1">
                    {t('legacyCatalog.filters.showingResults', { count: filteredAssets.length })}
                  </span>
                )}
              </div>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {isLoading && <PageLoadingState />}

            {isError && <PageErrorState />}

            {!isLoading && !isError && filteredAssets.length === 0 && (
              <EmptyState
                icon={<Server size={24} />}
                title={t('legacyCatalog.empty.title')}
                description={t('legacyCatalog.empty.description')}
              />
            )}

            {!isLoading && !isError && filteredAssets.length > 0 && (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="sticky top-0 z-10 bg-panel">
                    <tr className="border-b border-edge text-left">
                      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                        {t('legacyCatalog.filters.assetType')}
                      </th>
                      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                        {t('legacyCatalog.card.domain')}
                      </th>
                      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                        {t('legacyCatalog.card.team')}
                      </th>
                      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                        {t('legacyCatalog.card.criticality')}
                      </th>
                      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                        {t('legacyCatalog.card.lifecycle')}
                      </th>
                      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                        {t('common.actions')}
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {filteredAssets.map((asset) => (
                      <tr key={asset.id} className="hover:bg-elevated/50 transition-colors">
                        <td className="px-4 py-3">
                          <div>
                            <Link
                              to={`/services/legacy/${asset.assetType}/${asset.id}`}
                              className="font-medium text-heading hover:text-accent transition-colors"
                            >
                              {asset.displayName || asset.name}
                            </Link>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-info/15 text-info border border-info/25">
                            {asset.assetType}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-muted text-xs">{asset.domain}</td>
                        <td className="px-4 py-3">
                          <span className="inline-flex items-center gap-1 text-xs text-muted">
                            <Users size={12} />
                            {asset.teamName}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={`inline-flex text-xs px-2 py-0.5 rounded-full ${criticalityColors[asset.criticality] ?? 'bg-elevated text-muted border border-edge'}`}
                          >
                            {t(`catalog.badges.criticality.${asset.criticality}`, { defaultValue: asset.criticality })}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[asset.lifecycleStatus] ?? 'bg-elevated text-muted border border-edge'}`}
                          >
                            {t(`catalog.badges.lifecycle.${asset.lifecycleStatus}`, { defaultValue: asset.lifecycleStatus })}
                          </span>
                        </td>
                        <td className="px-4 py-3">
                          <Link
                            to={`/services/legacy/${asset.assetType}/${asset.id}`}
                            className="inline-flex items-center gap-1 text-xs text-accent hover:underline"
                          >
                            {t('catalog.actions.viewDetail')}
                            <ChevronRight size={12} />
                          </Link>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}

/** Dropdown de filtro reutilizável. */
function FilterSelect({
  label,
  value,
  onChange,
  options,
  allLabel,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
  allLabel: string;
}) {
  return (
    <div className="flex flex-col gap-1">
      <label className="text-xs text-muted">{label}</label>
      <select
        className="w-full px-3 py-1.5 bg-elevated border border-edge rounded-md text-sm text-heading focus:outline-none focus:ring-1 focus:ring-accent"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      >
        <option value="">{allLabel}</option>
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </div>
  );
}

export default LegacyAssetCatalogPage;
