import { useState, useMemo, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import {
  Server,
  Shield,
  Activity,
  Users,
  AlertTriangle,
  ChevronRight,
  Layers,
  Globe,
  Archive,
  Plus,
  ArrowUp,
  ArrowDown,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge } from '../../../components/Badge';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { SearchInput } from '../../../components/SearchInput';
import { Select } from '../../../components/Select';
import { TextField } from '../../../components/TextField';
import { serviceCatalogApi } from '../api';
import type { ServiceListItem, Criticality, LifecycleStatus } from '../../../types';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/** Variantes visuais para badges de criticidade. */
const criticalityColors: Record<Criticality, string> = {
  Critical: 'bg-critical/15 text-critical border border-critical/25',
  High: 'bg-warning/15 text-warning border border-warning/25',
  Medium: 'bg-warning/15 text-warning border border-warning/25',
  Low: 'bg-elevated text-muted border border-edge',
};

/** Variante de badge por nível de maturidade. */
function maturityBadgeVariant(level: string): 'success' | 'warning' | 'danger' | 'info' | 'default' {
  switch (level) {
    case 'Optimizing':
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
}

/** Variantes visuais para badges de ciclo de vida. */
const lifecycleColors: Record<LifecycleStatus, string> = {
  Planning: 'bg-info/15 text-info border border-info/25',
  Development: 'bg-info/15 text-accent border border-accent',
  Staging: 'bg-info/15 text-info border border-info/25',
  Active: 'bg-success/15 text-success border border-success/25',
  Deprecating: 'bg-warning/15 text-warning border border-warning/25',
  Deprecated: 'bg-warning/15 text-warning border border-warning/25',
  Retired: 'bg-elevated text-muted border border-edge',
};

/** Valores disponíveis nos filtros de tipo de serviço. */
const SERVICE_TYPES = [
  'RestApi',
  'SoapService',
  'KafkaProducer',
  'KafkaConsumer',
  'BackgroundService',
  'ScheduledProcess',
  'IntegrationComponent',
  'SharedPlatformService',
] as const;

/** Valores disponíveis nos filtros de criticidade. */
const CRITICALITY_VALUES = ['Low', 'Medium', 'High', 'Critical'] as const;

/** Valores disponíveis nos filtros de ciclo de vida. */
const LIFECYCLE_VALUES = [
  'Planning',
  'Development',
  'Staging',
  'Active',
  'Deprecating',
  'Deprecated',
  'Retired',
] as const;

/** Valores disponíveis nos filtros de tipo de exposição. */
const EXPOSURE_VALUES = ['Internal', 'External', 'Partner'] as const;

/** Níveis de maturidade disponíveis como filtro. */
const MATURITY_LEVELS = ['Initial', 'Developing', 'Defined', 'Managed', 'Optimizing'] as const;

/** Interface dos filtros ativos na listagem. */
interface ServiceFilters {
  search: string;
  serviceType: string;
  criticality: string;
  lifecycleStatus: string;
  exposureType: string;
  domain: string;
  teamName: string;
  maturityFilter: string;
}

const emptyFilters: ServiceFilters = {
  search: '',
  serviceType: '',
  criticality: '',
  lifecycleStatus: '',
  exposureType: '',
  domain: '',
  teamName: '',
  maturityFilter: '',
};

/** Intervalo de debounce para pesquisa (ms). */
const SEARCH_DEBOUNCE_MS = 350;

/** Página principal de listagem do catálogo de serviços. */
export function ServiceCatalogListPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const navigate = useNavigate();
  const [filters, setFilters] = useState<ServiceFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  // Estado de ordenação por maturidade — estado único para evitar closures stale
  type MaturitySortState = { by: '' } | { by: 'maturity'; desc: boolean };
  const [maturitySortState, setMaturitySortState] = useState<MaturitySortState>({ by: '' });
  const PAGE_SIZE = 50;

  /** Debounce da pesquisa para evitar chamadas excessivas à API. */
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(filters.search), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [filters.search]);

  /** Parâmetros enviados à API — omite chaves vazias. */
  const queryParams = useMemo((): NonNullable<Parameters<typeof serviceCatalogApi.listServices>[0]> => {
    const p: NonNullable<Parameters<typeof serviceCatalogApi.listServices>[0]> = {
      page: currentPage,
      pageSize: PAGE_SIZE,
    };
    if (debouncedSearch) p.search = debouncedSearch;
    if (filters.serviceType) p.serviceType = filters.serviceType;
    if (filters.criticality) p.criticality = filters.criticality;
    if (filters.lifecycleStatus) p.lifecycleStatus = filters.lifecycleStatus;
    if (filters.exposureType) p.exposureType = filters.exposureType;
    if (filters.domain) p.domain = filters.domain;
    if (filters.teamName) p.teamName = filters.teamName;
    if (filters.maturityFilter) p.maturityLevel = filters.maturityFilter;
    if (maturitySortState.by === 'maturity') {
      p.sortBy = 'maturity';
      p.sortDescending = maturitySortState.desc;
    }
    return p;
  }, [debouncedSearch, filters.serviceType, filters.criticality, filters.lifecycleStatus, filters.exposureType, filters.domain, filters.teamName, filters.maturityFilter, maturitySortState, currentPage]);

  const {
    data,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['catalog-services', queryParams, activeEnvironmentId],
    queryFn: () => serviceCatalogApi.listServices(queryParams),
  });

  const summaryQuery = useQuery({
    queryKey: ['catalog-services-summary', activeEnvironmentId],
    queryFn: () => serviceCatalogApi.getServicesSummary(),
  });

  // Query independente — loading/erro não bloqueiam a lista principal
  const { data: maturityDash } = useQuery({
    queryKey: ['catalog-maturity-dashboard'],
    queryFn: () => serviceCatalogApi.getMaturityDashboard(),
  });

  /** Mapa serviceId → entrada de maturidade para lookup O(1) por linha. */
  const maturityById = useMemo(
    () => new Map((maturityDash?.services ?? []).map((s) => [s.serviceId, s])),
    [maturityDash],
  );

  const summary = summaryQuery.data;
  const services: ServiceListItem[] = data?.items ?? [];

  /** Atualiza um campo de filtro individual. */
  const setFilter = (key: keyof ServiceFilters, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
    setCurrentPage(1);
  };

  /** Alterna a ordenação por maturidade (asc → desc → asc…). Usa update funcional
   *  para garantir que prev reflecte sempre o estado mais recente, evitando closures stale. */
  const handleMaturitySortClick = () => {
    setMaturitySortState((prev) =>
      prev.by !== 'maturity'
        ? { by: 'maturity', desc: false }
        : { by: 'maturity', desc: !prev.desc },
    );
    setCurrentPage(1);
  };

  return (
    <PageContainer>
      {/* CTA integrado ao PageHeader como actions */}
      <PageHeader
        title={t('catalog.title')}
        subtitle={t('catalog.subtitle')}
        icon={<Server size={24} />}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={() => navigate('/services/onboard')}
          >
            {t('serviceCatalog.registerService')}
          </Button>
        }
      />

      {/* ── Métricas de resumo ── */}
      {summary && (
        <PageSection>
          <ContentGrid className="!grid-cols-2 md:!grid-cols-3 xl:!grid-cols-6">
          <SummaryCard
            icon={<Server size={18} />}
            label={t('catalog.summary.total')}
            value={summary.totalCount}
            accent="text-accent"
          />
          <SummaryCard
            icon={<AlertTriangle size={18} />}
            label={t('catalog.summary.critical')}
            value={summary.criticalCount}
            accent="text-critical"
          />
          <SummaryCard
            icon={<Shield size={18} />}
            label={t('catalog.summary.high')}
            value={summary.highCriticalityCount}
            accent="text-warning"
          />
          <SummaryCard
            icon={<Activity size={18} />}
            label={t('catalog.summary.active')}
            value={summary.activeCount}
            accent="text-success"
          />
          <SummaryCard
            icon={<Archive size={18} />}
            label={t('catalog.summary.deprecated')}
            value={summary.deprecatedCount}
            accent="text-warning"
          />
          <SummaryCard
            icon={<Layers size={18} />}
            label={t('catalog.summary.retired')}
            value={summary.retiredCount}
            accent="text-muted"
          />
          </ContentGrid>
        </PageSection>
      )}

      {/* ── Pesquisa e filtros + Tabela ── */}
      <PageSection>
        <Card className="mb-6">
        <CardBody>
          <div className="flex flex-col gap-4">
            {/* Barra de pesquisa — DS SearchInput */}
            <SearchInput
              size="sm"
              placeholder={t('catalog.search.placeholder')}
              value={filters.search}
              onChange={(e) => setFilter('search', e.target.value)}
              aria-label={t('catalog.search.placeholder')}
            />

            {/* Filtros dropdown — DS Select + TextField */}
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
              <Select
                label={t('catalog.filters.serviceType')}
                size="sm"
                value={filters.serviceType}
                onChange={(e) => setFilter('serviceType', e.target.value)}
                options={[
                  { value: '', label: t('catalog.filters.all') },
                  ...SERVICE_TYPES.map((v) => ({ value: v, label: t(`catalog.badges.type.${v}`) })),
                ]}
              />
              <Select
                label={t('catalog.filters.criticality')}
                size="sm"
                value={filters.criticality}
                onChange={(e) => setFilter('criticality', e.target.value)}
                options={[
                  { value: '', label: t('catalog.filters.all') },
                  ...CRITICALITY_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.criticality.${v}`) })),
                ]}
              />
              <Select
                label={t('catalog.filters.lifecycleStatus')}
                size="sm"
                value={filters.lifecycleStatus}
                onChange={(e) => setFilter('lifecycleStatus', e.target.value)}
                options={[
                  { value: '', label: t('catalog.filters.all') },
                  ...LIFECYCLE_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.lifecycle.${v}`) })),
                ]}
              />
              <Select
                label={t('catalog.filters.exposureType')}
                size="sm"
                value={filters.exposureType}
                onChange={(e) => setFilter('exposureType', e.target.value)}
                options={[
                  { value: '', label: t('catalog.filters.all') },
                  ...EXPOSURE_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.exposure.${v}`) })),
                ]}
              />
              {/* Filtro de equipa como TextField size sm */}
              <TextField
                label={t('catalog.filters.team')}
                size="sm"
                placeholder={t('catalog.filters.team')}
                value={filters.teamName}
                onChange={(e) => setFilter('teamName', e.target.value)}
              />
              {/* Filtro por nível de maturidade */}
              <Select
                label={t('catalog.filters.maturity')}
                size="sm"
                value={filters.maturityFilter}
                onChange={(e) => setFilter('maturityFilter', e.target.value)}
                options={[
                  { value: '', label: t('catalog.filters.all') },
                  ...MATURITY_LEVELS.map((v) => ({ value: v, label: t(`serviceMaturity.level.${v}`) })),
                ]}
              />
            </div>
          </div>
        </CardBody>
      </Card>

      {/* ── Tabela de serviços ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Server size={18} className="text-accent" />
              <h2 className="text-base font-semibold text-heading">{t('catalog.title')}</h2>
              {data && (
                <span className="text-xs text-muted ml-1">
                  {t('catalog.totalCount', { count: data.totalCount })}
                </span>
              )}
            </div>
            <div className="flex items-center gap-3">
              <Link
                to="/services/maturity"
                className="text-xs text-accent hover:underline"
              >
                {t('catalog.maturity.viewDashboard', 'View maturity dashboard')} →
              </Link>
              <Link
                to="/services/graph"
                className="text-xs text-accent hover:underline flex items-center gap-1"
              >
                <Globe size={14} />
                {t('catalog.actions.viewGraph')}
              </Link>
            </div>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {isLoading && <PageLoadingState />}

          {isError && <PageErrorState />}

          {!isLoading && !isError && services.length === 0 && (
            <EmptyState
              icon={<Server size={24} />}
              title={t('catalog.empty.title')}
              description={t('catalog.empty.description')}
            />
          )}

          {!isLoading && !isError && services.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="sticky top-0 z-10 bg-panel">
                  <tr className="border-b border-edge text-left">
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.name')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.serviceType')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.domain')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.team')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.criticality')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.lifecycle')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('catalog.columns.exposure')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      <button
                        type="button"
                        aria-label={t('catalog.columns.sortByMaturity')}
                        onClick={handleMaturitySortClick}
                        className="inline-flex items-center gap-1 hover:text-heading transition-colors cursor-pointer"
                      >
                        {t('catalog.columns.maturity', 'Maturity')}
                        {maturitySortState.by === 'maturity' && (
                          maturitySortState.desc
                            ? <ArrowDown size={12} />
                            : <ArrowUp size={12} />
                        )}
                      </button>
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('common.actions')}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {services.map((svc) => (
                    <tr key={svc.serviceId} className="hover:bg-elevated/50 transition-colors">
                      <td className="px-4 py-3">
                        <div>
                          <Link
                            to={`/services/${svc.serviceId}`}
                            className="font-medium text-heading hover:text-accent transition-colors"
                          >
                            {svc.displayName || svc.name}
                          </Link>
                          {svc.description && (
                            <p className="text-xs text-muted mt-0.5 truncate max-w-xs">
                              {svc.description}
                            </p>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-info/15 text-info border border-info/25">
                          {t(`catalog.badges.type.${svc.serviceType}`)}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-muted text-xs">{svc.domain}</td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center gap-1 text-xs text-muted">
                          <Users size={12} />
                          {svc.teamName}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`inline-flex text-xs px-2 py-0.5 rounded-full ${criticalityColors[svc.criticality]}`}
                        >
                          {t(`catalog.badges.criticality.${svc.criticality}`)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[svc.lifecycleStatus]}`}
                        >
                          {t(`catalog.badges.lifecycle.${svc.lifecycleStatus}`)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-xs text-muted">
                          {t(`catalog.badges.exposure.${svc.exposureType}`)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        {(() => {
                          const m = maturityById.get(svc.serviceId);
                          return m ? (
                            <span className="inline-flex items-center gap-1.5">
                              <Badge variant={maturityBadgeVariant(m.level)} size="sm">{t(`serviceMaturity.level.${m.level}`)}</Badge>
                              <span className="text-xs text-muted">{Math.round(m.overallScore * 100)}</span>
                            </span>
                          ) : <span className="text-xs text-muted">—</span>;
                        })()}
                      </td>
                      <td className="px-4 py-3">
                        <Link
                          to={`/services/${svc.serviceId}`}
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

          {!isLoading && !isError && data && data.totalCount > PAGE_SIZE && (
            <div className="flex items-center justify-between px-4 py-3 border-t border-edge">
              <span className="text-xs text-muted">
                {t('catalog.pagination.showing', {
                  start: (currentPage - 1) * PAGE_SIZE + 1,
                  end: Math.min(currentPage * PAGE_SIZE, data.totalCount),
                  total: data.totalCount,
                })}
              </span>
              {/* Paginação com DS Button */}
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  size="xs"
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                >
                  {t('common.previous')}
                </Button>
                <Button
                  variant="secondary"
                  size="xs"
                  onClick={() => setCurrentPage((p) => p + 1)}
                  disabled={currentPage * PAGE_SIZE >= data.totalCount}
                >
                  {t('common.next')}
                </Button>
              </div>
            </div>
          )}
        </CardBody>
      </Card>
      </PageSection>
    </PageContainer>
  );
}

/* ── Componentes internos ─────────────────────────────────────────── */

/** Card de métrica individual no resumo do catálogo. */
function SummaryCard({
  icon,
  label,
  value,
  accent,
}: {
  icon: React.ReactNode;
  label: string;
  value: number;
  accent: string;
}) {
  return (
    <div className="bg-card rounded-lg border border-edge p-4 flex flex-col gap-1">
      <div className={`flex items-center gap-2 ${accent}`}>
        {icon}
        <span className="text-xs text-muted">{label}</span>
      </div>
      <span className="text-xl font-bold text-heading">{value}</span>
    </div>
  );
}

