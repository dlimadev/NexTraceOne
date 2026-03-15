import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Search,
  Server,
  Shield,
  Activity,
  Users,
  AlertTriangle,
  ChevronRight,
  Layers,
  Globe,
  Archive,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { serviceCatalogApi } from '../api';
import type { ServiceListItem, Criticality, LifecycleStatus } from '../../../types';

/** Variantes visuais para badges de criticidade. */
const criticalityColors: Record<Criticality, string> = {
  Critical: 'bg-red-900/40 text-red-300 border border-red-700/50',
  High: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Medium: 'bg-yellow-900/40 text-yellow-300 border border-yellow-700/50',
  Low: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
};

/** Variantes visuais para badges de ciclo de vida. */
const lifecycleColors: Record<LifecycleStatus, string> = {
  Planning: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Development: 'bg-indigo-900/40 text-indigo-300 border border-indigo-700/50',
  Staging: 'bg-purple-900/40 text-purple-300 border border-purple-700/50',
  Active: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Deprecating: 'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  Deprecated: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Retired: 'bg-slate-900/40 text-slate-400 border border-slate-700/50',
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

/** Interface dos filtros ativos na listagem. */
interface ServiceFilters {
  search: string;
  serviceType: string;
  criticality: string;
  lifecycleStatus: string;
  exposureType: string;
  domain: string;
  teamName: string;
}

const emptyFilters: ServiceFilters = {
  search: '',
  serviceType: '',
  criticality: '',
  lifecycleStatus: '',
  exposureType: '',
  domain: '',
  teamName: '',
};

/** Página principal de listagem do catálogo de serviços. */
export function ServiceCatalogListPage() {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<ServiceFilters>(emptyFilters);

  /** Parâmetros enviados à API — omite chaves vazias. */
  const queryParams = useMemo(() => {
    const p: Record<string, string> = {};
    if (filters.search) p.search = filters.search;
    if (filters.serviceType) p.serviceType = filters.serviceType;
    if (filters.criticality) p.criticality = filters.criticality;
    if (filters.lifecycleStatus) p.lifecycleStatus = filters.lifecycleStatus;
    if (filters.exposureType) p.exposureType = filters.exposureType;
    if (filters.domain) p.domain = filters.domain;
    if (filters.teamName) p.teamName = filters.teamName;
    return p;
  }, [filters]);

  const {
    data,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['catalog-services', queryParams],
    queryFn: () => serviceCatalogApi.listServices(queryParams),
  });

  const summaryQuery = useQuery({
    queryKey: ['catalog-services-summary'],
    queryFn: () => serviceCatalogApi.getServicesSummary(),
  });

  const summary = summaryQuery.data;
  const services: ServiceListItem[] = data?.items ?? [];

  /** Atualiza um campo de filtro individual. */
  const setFilter = (key: keyof ServiceFilters, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* ── Cabeçalho ── */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('catalog.title')}</h1>
        <p className="text-muted mt-1">{t('catalog.subtitle')}</p>
      </div>

      {/* ── Métricas de resumo ── */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-6">
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
            accent="text-red-400"
          />
          <SummaryCard
            icon={<Shield size={18} />}
            label={t('catalog.summary.high')}
            value={summary.highCriticalityCount}
            accent="text-orange-400"
          />
          <SummaryCard
            icon={<Activity size={18} />}
            label={t('catalog.summary.active')}
            value={summary.activeCount}
            accent="text-emerald-400"
          />
          <SummaryCard
            icon={<Archive size={18} />}
            label={t('catalog.summary.deprecated')}
            value={summary.deprecatedCount}
            accent="text-amber-400"
          />
          <SummaryCard
            icon={<Layers size={18} />}
            label={t('catalog.summary.retired')}
            value={summary.retiredCount}
            accent="text-slate-400"
          />
        </div>
      )}

      {/* ── Pesquisa e filtros ── */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex flex-col gap-4">
            {/* Barra de pesquisa */}
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" size={16} />
              <input
                type="text"
                className="w-full pl-10 pr-4 py-2 bg-elevated border border-edge rounded-md text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                placeholder={t('catalog.search.placeholder')}
                value={filters.search}
                onChange={(e) => setFilter('search', e.target.value)}
              />
            </div>

            {/* Filtros dropdown */}
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
              <FilterSelect
                label={t('catalog.filters.serviceType')}
                value={filters.serviceType}
                onChange={(v) => setFilter('serviceType', v)}
                options={SERVICE_TYPES.map((v) => ({ value: v, label: t(`catalog.badges.type.${v}`) }))}
                allLabel={t('catalog.filters.all')}
              />
              <FilterSelect
                label={t('catalog.filters.criticality')}
                value={filters.criticality}
                onChange={(v) => setFilter('criticality', v)}
                options={CRITICALITY_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.criticality.${v}`) }))}
                allLabel={t('catalog.filters.all')}
              />
              <FilterSelect
                label={t('catalog.filters.lifecycleStatus')}
                value={filters.lifecycleStatus}
                onChange={(v) => setFilter('lifecycleStatus', v)}
                options={LIFECYCLE_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.lifecycle.${v}`) }))}
                allLabel={t('catalog.filters.all')}
              />
              <FilterSelect
                label={t('catalog.filters.exposureType')}
                value={filters.exposureType}
                onChange={(v) => setFilter('exposureType', v)}
                options={EXPOSURE_VALUES.map((v) => ({ value: v, label: t(`catalog.badges.exposure.${v}`) }))}
                allLabel={t('catalog.filters.all')}
              />
              <div className="flex flex-col gap-1">
                <label className="text-xs text-muted">{t('catalog.filters.team')}</label>
                <input
                  type="text"
                  className="w-full px-3 py-1.5 bg-elevated border border-edge rounded-md text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
                  placeholder={t('catalog.filters.team')}
                  value={filters.teamName}
                  onChange={(e) => setFilter('teamName', e.target.value)}
                />
              </div>
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
                  ({data.totalCount} {t('common.total')})
                </span>
              )}
            </div>
            <Link
              to="/services/graph"
              className="text-xs text-accent hover:underline flex items-center gap-1"
            >
              <Globe size={14} />
              {t('catalog.actions.viewGraph')}
            </Link>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {isLoading && (
            <div className="flex items-center justify-center py-16">
              <p className="text-sm text-muted">{t('common.loading')}</p>
            </div>
          )}

          {isError && (
            <div className="flex items-center justify-center py-16">
              <p className="text-sm text-red-400">{t('common.error')}</p>
            </div>
          )}

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
                <thead>
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
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-blue-900/30 text-blue-300 border border-blue-700/50">
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
        </CardBody>
      </Card>
    </div>
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
