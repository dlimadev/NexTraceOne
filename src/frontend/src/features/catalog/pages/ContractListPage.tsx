import { useState, useMemo, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Search,
  FileText,
  Shield,
  Lock,
  CheckCircle,
  ChevronRight,
  Layers,
  Clock,
  Archive,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { contractsApi } from '../api/contracts';
import type { ContractListItem } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/** Variantes visuais para badges de protocolo. */
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-success/15 text-success border border-success/25',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-info/15 text-info border border-info/25',
  Protobuf: 'bg-warning/15 text-warning border border-warning/25',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

/** Variantes visuais para badges de estado do ciclo de vida. */
const lifecycleColors: Record<string, string> = {
  Draft: 'bg-elevated text-muted border border-edge',
  InReview: 'bg-info/15 text-info border border-info/25',
  Approved: 'bg-success/15 text-success border border-success/25',
  Locked: 'bg-info/15 text-info border border-info/25',
  Deprecated: 'bg-warning/15 text-warning border border-warning/25',
  Sunset: 'bg-critical/15 text-critical border border-critical/25',
  Retired: 'bg-elevated text-muted border border-edge',
};

/** Valores disponíveis nos filtros de protocolo. */
const PROTOCOL_VALUES = ['OpenApi', 'Swagger', 'Wsdl', 'AsyncApi', 'Protobuf', 'GraphQl'] as const;

/** Valores disponíveis nos filtros de ciclo de vida. */
const LIFECYCLE_VALUES = ['Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired'] as const;

/** Interface dos filtros ativos na listagem de contratos. */
interface ContractFilters {
  search: string;
  protocol: string;
  lifecycleState: string;
}

const emptyFilters: ContractFilters = {
  search: '',
  protocol: '',
  lifecycleState: '',
};

/** Página principal de governança de contratos — listagem, filtros e resumo. */
/** Intervalo de debounce para pesquisa (ms). */
const SEARCH_DEBOUNCE_MS = 350;

/** Página principal de governança de contratos. */
export function ContractListPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [filters, setFilters] = useState<ContractFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState('');

  /** Debounce da pesquisa para evitar chamadas excessivas à API. */
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(filters.search), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [filters.search]);

  /** Parâmetros enviados à API — omite chaves vazias. */
  const queryParams = useMemo(() => {
    const p: Record<string, string> = {};
    if (debouncedSearch) p.searchTerm = debouncedSearch;
    if (filters.protocol) p.protocol = filters.protocol;
    if (filters.lifecycleState) p.lifecycleState = filters.lifecycleState;
    return p;
  }, [debouncedSearch, filters.protocol, filters.lifecycleState]);

  const {
    data,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ['contract-governance-list', queryParams, activeEnvironmentId],
    queryFn: () => contractsApi.listContracts(queryParams),
  });

  const summaryQuery = useQuery({
    queryKey: ['contract-governance-summary', activeEnvironmentId],
    queryFn: () => contractsApi.getContractsSummary(),
  });

  const summary = summaryQuery.data;
  const contracts: ContractListItem[] = data?.items ?? [];

  /** Atualiza um campo de filtro individual. */
  const setFilter = (key: keyof ContractFilters, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
  };

  return (
    <PageContainer>
      {/* ── Cabeçalho ── */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('contractGov.title')}</h1>
        <p className="text-muted mt-1">{t('contractGov.subtitle')}</p>
      </div>

      {/* ── Métricas de resumo ── */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-7 gap-4 mb-6">
          <SummaryCard
            icon={<Layers size={18} />}
            label={t('contractGov.summary.totalVersions')}
            value={summary.totalVersions}
            accent="text-accent"
          />
          <SummaryCard
            icon={<FileText size={18} />}
            label={t('contractGov.summary.distinctContracts')}
            value={summary.distinctContracts}
            accent="text-info"
          />
          <SummaryCard
            icon={<Clock size={18} />}
            label={t('contractGov.summary.draft')}
            value={summary.draftCount}
            accent="text-muted"
          />
          <SummaryCard
            icon={<Search size={18} />}
            label={t('contractGov.summary.inReview')}
            value={summary.inReviewCount}
            accent="text-info"
          />
          <SummaryCard
            icon={<CheckCircle size={18} />}
            label={t('contractGov.summary.approved')}
            value={summary.approvedCount}
            accent="text-success"
          />
          <SummaryCard
            icon={<Lock size={18} />}
            label={t('contractGov.summary.locked')}
            value={summary.lockedCount}
            accent="text-info"
          />
          <SummaryCard
            icon={<Archive size={18} />}
            label={t('contractGov.summary.deprecated')}
            value={summary.deprecatedCount}
            accent="text-warning"
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
                placeholder={t('contractGov.search.placeholder')}
                value={filters.search}
                onChange={(e) => setFilter('search', e.target.value)}
              />
            </div>

            {/* Filtros dropdown */}
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
              <FilterSelect
                label={t('contractGov.filters.protocol')}
                value={filters.protocol}
                onChange={(v) => setFilter('protocol', v)}
                options={PROTOCOL_VALUES.map((v) => ({ value: v, label: t(`contractGov.badges.protocols.${v}`) }))}
                allLabel={t('contractGov.filters.all')}
              />
              <FilterSelect
                label={t('contractGov.filters.lifecycleState')}
                value={filters.lifecycleState}
                onChange={(v) => setFilter('lifecycleState', v)}
                options={LIFECYCLE_VALUES.map((v) => ({ value: v, label: t(`contractGov.badges.lifecycle.${v}`) }))}
                allLabel={t('contractGov.filters.all')}
              />
            </div>
          </div>
        </CardBody>
      </Card>

      {/* ── Tabela de contratos ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Shield size={18} className="text-accent" />
              <h2 className="text-base font-semibold text-heading">{t('contractGov.title')}</h2>
              {data && (
                <span className="text-xs text-muted ml-1">
                  ({data.totalCount})
                </span>
              )}
            </div>
            <Link
              to="/contracts/studio"
              className="text-xs text-accent hover:underline flex items-center gap-1"
            >
              <Layers size={14} />
              {t('contractGov.actions.goToStudio')}
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
              <p className="text-sm text-critical">{t('common.error')}</p>
            </div>
          )}

          {!isLoading && !isError && contracts.length === 0 && (
            <EmptyState
              icon={<FileText size={24} />}
              title={t('contractGov.empty.title')}
              description={t('contractGov.empty.description')}
            />
          )}

          {!isLoading && !isError && contracts.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-left">
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.api')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.version')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.protocol')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.lifecycle')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.format')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.signed')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.createdAt')}
                    </th>
                    <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                      {t('contractGov.columns.actions')}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge">
                  {contracts.map((c) => (
                    <tr key={c.versionId} className="hover:bg-elevated/50 transition-colors">
                      <td className="px-4 py-3">
                        <Link
                          to={`/contracts/${c.versionId}`}
                          className="font-medium text-heading hover:text-accent transition-colors"
                        >
                          {c.apiAssetId}
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-muted text-xs font-mono">{c.semVer}</td>
                      <td className="px-4 py-3">
                        <span
                          className={`inline-flex text-xs px-2 py-0.5 rounded-full ${protocolColors[c.protocol] ?? 'bg-elevated text-muted border border-edge'}`}
                        >
                          {t(`contractGov.badges.protocols.${c.protocol}`, c.protocol)}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[c.lifecycleState] ?? 'bg-elevated text-muted border border-edge'}`}
                        >
                          {t(`contractGov.badges.lifecycle.${c.lifecycleState}`, c.lifecycleState)}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-muted text-xs">{c.format}</td>
                      <td className="px-4 py-3">
                        {c.isSigned ? (
                          <span className="inline-flex items-center gap-1 text-xs text-success">
                            <CheckCircle size={12} />
                            {t('contractGov.badges.signed')}
                          </span>
                        ) : (
                          <span className="text-xs text-muted">{t('contractGov.badges.unsigned')}</span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-muted text-xs">
                        {new Date(c.createdAt).toLocaleDateString()}
                      </td>
                      <td className="px-4 py-3">
                        <Link
                          to={`/contracts/${c.versionId}`}
                          className="inline-flex items-center gap-1 text-xs text-accent hover:underline"
                        >
                          {t('contractGov.actions.viewDetail')}
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
    </PageContainer>
  );
}

/* ── Componentes internos ─────────────────────────────────────────── */

/** Card de métrica individual no resumo de governança de contratos. */
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
