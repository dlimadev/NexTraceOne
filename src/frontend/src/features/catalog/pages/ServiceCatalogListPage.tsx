import { useState, useMemo, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
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
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
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

/** Intervalo de debounce para pesquisa (ms). */
const SEARCH_DEBOUNCE_MS = 350;

/** Página principal de listagem do catálogo de serviços. */
export function ServiceCatalogListPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<ServiceFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [showServiceForm, setShowServiceForm] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const PAGE_SIZE = 50;
  const [serviceForm, setServiceForm] = useState({
    name: '', team: '', description: '', domain: '',
    serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
    technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
  });

  /** Debounce da pesquisa para evitar chamadas excessivas à API. */
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(filters.search), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [filters.search]);

  const registerService = useMutation({
    mutationFn: serviceCatalogApi.registerService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-services'] });
      setShowServiceForm(false);
      setServiceForm({
        name: '', team: '', description: '', domain: '',
        serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
        technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
      });
    },
  });

  /** Parâmetros enviados à API — omite chaves vazias. */
  const queryParams = useMemo(() => {
    const p: Record<string, string> = {};
    if (debouncedSearch) p.search = debouncedSearch;
    if (filters.serviceType) p.serviceType = filters.serviceType;
    if (filters.criticality) p.criticality = filters.criticality;
    if (filters.lifecycleStatus) p.lifecycleStatus = filters.lifecycleStatus;
    if (filters.exposureType) p.exposureType = filters.exposureType;
    if (filters.domain) p.domain = filters.domain;
    if (filters.teamName) p.teamName = filters.teamName;
    p.page = currentPage.toString();
    p.pageSize = PAGE_SIZE.toString();
    return p;
  }, [debouncedSearch, filters.serviceType, filters.criticality, filters.lifecycleStatus, filters.exposureType, filters.domain, filters.teamName, currentPage]);

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

  const summary = summaryQuery.data;
  const services: ServiceListItem[] = data?.items ?? [];

  /** Atualiza um campo de filtro individual. */
  const setFilter = (key: keyof ServiceFilters, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
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
            onClick={() => setShowServiceForm((v) => !v)}
          >
            {t('serviceCatalog.registerService')}
          </Button>
        }
      />

      {/* ── Formulário de registro de serviço ── */}
      {showServiceForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('serviceCatalog.registerServiceTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); registerService.mutate(serviceForm); }}
              className="space-y-4"
            >
              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.basicInfo', 'Basic Information')}</h3>
                {/* Campos básicos — DS TextField */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  <TextField
                    label={`${t('serviceCatalog.name')} *`}
                    size="sm"
                    value={serviceForm.name}
                    onChange={(e) => setServiceForm((f) => ({ ...f, name: e.target.value }))}
                    required
                    placeholder={t('serviceCatalog.namePlaceholder', 'e.g., payment-service')}
                    className="font-mono"
                  />
                  <TextField
                    label={`${t('serviceCatalog.domain', 'Domain')} *`}
                    size="sm"
                    value={serviceForm.domain}
                    onChange={(e) => setServiceForm((f) => ({ ...f, domain: e.target.value }))}
                    required
                    placeholder={t('serviceCatalog.domainPlaceholder', 'e.g., payments, identity, orders')}
                  />
                  <TextField
                    label={`${t('serviceCatalog.team')} *`}
                    size="sm"
                    value={serviceForm.team}
                    onChange={(e) => setServiceForm((f) => ({ ...f, team: e.target.value }))}
                    required
                    placeholder={t('serviceCatalog.teamPlaceholder', 'e.g., platform-team')}
                  />
                </div>
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.classification', 'Classification')}</h3>
                {/* Classificação — DS Select */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Select
                    label={t('serviceCatalog.serviceType', 'Service Type')}
                    size="sm"
                    value={serviceForm.serviceType}
                    onChange={(e) => setServiceForm((f) => ({ ...f, serviceType: e.target.value }))}
                    options={[
                      { value: 'RestApi', label: t('serviceCatalog.typeRestApi', 'REST API') },
                      { value: 'GraphqlApi', label: t('serviceCatalog.typeGraphqlApi', 'GraphQL API') },
                      { value: 'GrpcService', label: t('serviceCatalog.typeGrpcService', 'gRPC Service') },
                      { value: 'SoapService', label: t('serviceCatalog.typeSoapService', 'SOAP Service') },
                      { value: 'KafkaProducer', label: t('serviceCatalog.typeKafkaProducer', 'Kafka Producer') },
                      { value: 'KafkaConsumer', label: t('serviceCatalog.typeKafkaConsumer', 'Kafka Consumer') },
                      { value: 'BackgroundService', label: t('serviceCatalog.typeBackgroundService', 'Background Service') },
                      { value: 'ScheduledProcess', label: t('serviceCatalog.typeScheduledProcess', 'Scheduled Process') },
                      { value: 'Gateway', label: t('serviceCatalog.typeGateway', 'API Gateway') },
                      { value: 'IntegrationComponent', label: t('serviceCatalog.typeIntegrationComponent', 'Integration Component') },
                      { value: 'SharedPlatformService', label: t('serviceCatalog.typeSharedPlatformService', 'Shared Platform Service') },
                      { value: 'Framework', label: t('serviceCatalog.typeFramework', 'Framework / SDK') },
                      { value: 'ThirdParty', label: t('serviceCatalog.typeThirdParty', 'Third-Party Service') },
                      { value: 'LegacySystem', label: t('serviceCatalog.typeLegacySystem', 'Legacy System') },
                      { value: 'CobolProgram', label: t('serviceCatalog.typeCobolProgram', 'COBOL Program') },
                      { value: 'CicsTransaction', label: t('serviceCatalog.typeCicsTransaction', 'CICS Transaction') },
                      { value: 'ImsTransaction', label: t('serviceCatalog.typeImsTransaction', 'IMS Transaction') },
                      { value: 'BatchJob', label: t('serviceCatalog.typeBatchJob', 'Batch Job') },
                      { value: 'MainframeSystem', label: t('serviceCatalog.typeMainframeSystem', 'Mainframe System') },
                      { value: 'MqQueueManager', label: t('serviceCatalog.typeMqQueueManager', 'MQ Queue Manager') },
                      { value: 'ZosConnectApi', label: t('serviceCatalog.typeZosConnectApi', 'z/OS Connect API') },
                    ]}
                  />
                  <Select
                    label={t('serviceCatalog.criticality', 'Criticality')}
                    size="sm"
                    value={serviceForm.criticality}
                    onChange={(e) => setServiceForm((f) => ({ ...f, criticality: e.target.value }))}
                    options={[
                      { value: 'Low', label: t('serviceCatalog.criticalityLow', 'Low') },
                      { value: 'Medium', label: t('serviceCatalog.criticalityMedium', 'Medium') },
                      { value: 'High', label: t('serviceCatalog.criticalityHigh', 'High') },
                      { value: 'Critical', label: t('serviceCatalog.criticalityCritical', 'Critical') },
                    ]}
                  />
                  <Select
                    label={t('serviceCatalog.exposure', 'Exposure')}
                    size="sm"
                    value={serviceForm.exposureType}
                    onChange={(e) => setServiceForm((f) => ({ ...f, exposureType: e.target.value }))}
                    options={[
                      { value: 'Internal', label: t('serviceCatalog.exposureInternal', 'Internal') },
                      { value: 'Partner', label: t('serviceCatalog.exposurePartner', 'Partner') },
                      { value: 'External', label: t('serviceCatalog.exposureExternal', 'External') },
                    ]}
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.description')}</label>
                <textarea value={serviceForm.description}
                  onChange={(e) => setServiceForm((f) => ({ ...f, description: e.target.value }))}
                  rows={2} placeholder={t('serviceCatalog.descriptionPlaceholder', 'Describe the purpose and responsibilities of this service...')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors resize-none" />
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.ownership', 'Ownership')}</h3>
                {/* Propriedade — DS TextField */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <TextField
                    label={t('serviceCatalog.technicalOwner', 'Technical Owner')}
                    size="sm"
                    value={serviceForm.technicalOwner}
                    onChange={(e) => setServiceForm((f) => ({ ...f, technicalOwner: e.target.value }))}
                    placeholder={t('serviceCatalog.technicalOwnerPlaceholder', 'e.g., john.smith@company.com')}
                  />
                  <TextField
                    label={t('serviceCatalog.businessOwner', 'Business Owner')}
                    size="sm"
                    value={serviceForm.businessOwner}
                    onChange={(e) => setServiceForm((f) => ({ ...f, businessOwner: e.target.value }))}
                    placeholder={t('serviceCatalog.businessOwnerPlaceholder', 'e.g., Product Manager Name')}
                  />
                </div>
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.references', 'References')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <TextField
                    label={t('serviceCatalog.documentationUrl', 'Documentation URL')}
                    type="url"
                    value={serviceForm.documentationUrl}
                    onChange={(e) => setServiceForm((f) => ({ ...f, documentationUrl: e.target.value }))}
                    placeholder={t('catalog.detail.placeholder.documentationUrl', 'https://docs.company.com/payment-service')}
                    size="sm"
                    className="font-mono"
                  />
                  <TextField
                    label={t('serviceCatalog.repositoryUrl', 'Repository URL')}
                    type="url"
                    value={serviceForm.repositoryUrl}
                    onChange={(e) => setServiceForm((f) => ({ ...f, repositoryUrl: e.target.value }))}
                    placeholder={t('catalog.detail.placeholder.repositoryUrl', 'https://github.com/org/payment-service')}
                    size="sm"
                    className="font-mono"
                  />
                </div>
              </div>

              <div className="flex gap-2 justify-end pt-2 border-t border-edge">
                <Button variant="secondary" type="button" onClick={() => setShowServiceForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={registerService.isPending}>{t('serviceCatalog.register')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

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
            <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
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
