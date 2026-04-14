import { useState, useMemo, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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
  Plus,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { serviceCatalogApi } from '../api';
import type { ServiceListItem, Criticality, LifecycleStatus } from '../../../types';

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
  const queryClient = useQueryClient();
  const [filters, setFilters] = useState<ServiceFilters>(emptyFilters);
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [showServiceForm, setShowServiceForm] = useState(false);
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
    return p;
  }, [debouncedSearch, filters.serviceType, filters.criticality, filters.lifecycleStatus, filters.exposureType, filters.domain, filters.teamName]);

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
    <PageContainer>
      {/* ── Cabeçalho ── */}
      <div className="mb-6 flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('catalog.title')}</h1>
          <p className="text-muted mt-1">{t('catalog.subtitle')}</p>
        </div>
        <Button onClick={() => setShowServiceForm((v) => !v)}>
          <Plus size={16} /> {t('serviceCatalog.registerService')}
        </Button>
      </div>

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
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('serviceCatalog.name')} <span className="text-danger">*</span>
                    </label>
                    <input type="text" value={serviceForm.name}
                      onChange={(e) => setServiceForm((f) => ({ ...f, name: e.target.value }))}
                      required placeholder={t('serviceCatalog.namePlaceholder', 'e.g., payment-service')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('serviceCatalog.domain', 'Domain')} <span className="text-danger">*</span>
                    </label>
                    <input type="text" value={serviceForm.domain}
                      onChange={(e) => setServiceForm((f) => ({ ...f, domain: e.target.value }))}
                      required placeholder={t('serviceCatalog.domainPlaceholder', 'e.g., payments, identity, orders')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('serviceCatalog.team')} <span className="text-danger">*</span>
                    </label>
                    <input type="text" value={serviceForm.team}
                      onChange={(e) => setServiceForm((f) => ({ ...f, team: e.target.value }))}
                      required placeholder={t('serviceCatalog.teamPlaceholder', 'e.g., platform-team')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                </div>
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.classification', 'Classification')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.serviceType', 'Service Type')}</label>
                    <select value={serviceForm.serviceType}
                      onChange={(e) => setServiceForm((f) => ({ ...f, serviceType: e.target.value }))}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors">
                      <optgroup label={t('serviceCatalog.typeGroupModern', 'Modern Services')}>
                        <option value="RestApi">{t('serviceCatalog.typeRestApi', 'REST API')}</option>
                        <option value="GraphqlApi">{t('serviceCatalog.typeGraphqlApi', 'GraphQL API')}</option>
                        <option value="GrpcService">{t('serviceCatalog.typeGrpcService', 'gRPC Service')}</option>
                        <option value="SoapService">{t('serviceCatalog.typeSoapService', 'SOAP Service')}</option>
                        <option value="KafkaProducer">{t('serviceCatalog.typeKafkaProducer', 'Kafka Producer')}</option>
                        <option value="KafkaConsumer">{t('serviceCatalog.typeKafkaConsumer', 'Kafka Consumer')}</option>
                        <option value="BackgroundService">{t('serviceCatalog.typeBackgroundService', 'Background Service')}</option>
                        <option value="ScheduledProcess">{t('serviceCatalog.typeScheduledProcess', 'Scheduled Process')}</option>
                        <option value="Gateway">{t('serviceCatalog.typeGateway', 'API Gateway')}</option>
                      </optgroup>
                      <optgroup label={t('serviceCatalog.typeGroupPlatform', 'Platform & Integration')}>
                        <option value="IntegrationComponent">{t('serviceCatalog.typeIntegrationComponent', 'Integration Component')}</option>
                        <option value="SharedPlatformService">{t('serviceCatalog.typeSharedPlatformService', 'Shared Platform Service')}</option>
                        <option value="Framework">{t('serviceCatalog.typeFramework', 'Framework / SDK')}</option>
                        <option value="ThirdParty">{t('serviceCatalog.typeThirdParty', 'Third-Party Service')}</option>
                        <option value="LegacySystem">{t('serviceCatalog.typeLegacySystem', 'Legacy System')}</option>
                      </optgroup>
                      <optgroup label={t('serviceCatalog.typeGroupMainframe', 'Mainframe')}>
                        <option value="CobolProgram">{t('serviceCatalog.typeCobolProgram', 'COBOL Program')}</option>
                        <option value="CicsTransaction">{t('serviceCatalog.typeCicsTransaction', 'CICS Transaction')}</option>
                        <option value="ImsTransaction">{t('serviceCatalog.typeImsTransaction', 'IMS Transaction')}</option>
                        <option value="BatchJob">{t('serviceCatalog.typeBatchJob', 'Batch Job')}</option>
                        <option value="MainframeSystem">{t('serviceCatalog.typeMainframeSystem', 'Mainframe System')}</option>
                        <option value="MqQueueManager">{t('serviceCatalog.typeMqQueueManager', 'MQ Queue Manager')}</option>
                        <option value="ZosConnectApi">{t('serviceCatalog.typeZosConnectApi', 'z/OS Connect API')}</option>
                      </optgroup>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.criticality', 'Criticality')}</label>
                    <select value={serviceForm.criticality}
                      onChange={(e) => setServiceForm((f) => ({ ...f, criticality: e.target.value }))}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors">
                      <option value="Low">{t('serviceCatalog.criticalityLow', 'Low')}</option>
                      <option value="Medium">{t('serviceCatalog.criticalityMedium', 'Medium')}</option>
                      <option value="High">{t('serviceCatalog.criticalityHigh', 'High')}</option>
                      <option value="Critical">{t('serviceCatalog.criticalityCritical', 'Critical')}</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.exposure', 'Exposure')}</label>
                    <select value={serviceForm.exposureType}
                      onChange={(e) => setServiceForm((f) => ({ ...f, exposureType: e.target.value }))}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors">
                      <option value="Internal">{t('serviceCatalog.exposureInternal', 'Internal')}</option>
                      <option value="Partner">{t('serviceCatalog.exposurePartner', 'Partner')}</option>
                      <option value="External">{t('serviceCatalog.exposureExternal', 'External / Public')}</option>
                    </select>
                  </div>
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
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.technicalOwner', 'Technical Owner')}</label>
                    <input type="text" value={serviceForm.technicalOwner}
                      onChange={(e) => setServiceForm((f) => ({ ...f, technicalOwner: e.target.value }))}
                      placeholder={t('serviceCatalog.technicalOwnerPlaceholder', 'e.g., john.smith@company.com')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.businessOwner', 'Business Owner')}</label>
                    <input type="text" value={serviceForm.businessOwner}
                      onChange={(e) => setServiceForm((f) => ({ ...f, businessOwner: e.target.value }))}
                      placeholder={t('serviceCatalog.businessOwnerPlaceholder', 'e.g., Product Manager Name')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                </div>
              </div>

              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.references', 'References')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.documentationUrl', 'Documentation URL')}</label>
                    <input type="url" value={serviceForm.documentationUrl}
                      onChange={(e) => setServiceForm((f) => ({ ...f, documentationUrl: e.target.value }))}
                      placeholder={t('catalog.detail.placeholder.documentationUrl', 'https://docs.company.com/payment-service')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.repositoryUrl', 'Repository URL')}</label>
                    <input type="url" value={serviceForm.repositoryUrl}
                      onChange={(e) => setServiceForm((f) => ({ ...f, repositoryUrl: e.target.value }))}
                      placeholder={t('catalog.detail.placeholder.repositoryUrl', 'https://github.com/org/payment-service')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono" />
                  </div>
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
