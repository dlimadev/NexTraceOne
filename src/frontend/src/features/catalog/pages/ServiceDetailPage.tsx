import { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Shield,
  Users,
  Globe,
  FileText,
  ExternalLink,
  Eye,
  Lock,
  GitCommit,
  AlertTriangle,
  Server,
  Plus,
  Info,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Tabs } from '../../../components/Tabs';
import { EntityHeader } from '../../../components/EntityHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { serviceCatalogApi } from '../api';
import { contractsApi } from '../api/contracts';
import { AssistantPanel } from '../../ai-hub/components/AssistantPanel';
import { ServiceLinksSection } from '../components/ServiceLinksSection';
import { ServiceLifecyclePanel } from '../components/ServiceLifecyclePanel';
import type { Criticality, LifecycleStatus, ServiceApiSummary, ServiceContractItem } from '../../../types';
import type { ServiceType } from '../../../types';
import { PageContainer, PageSection, TableWrapper } from '../../../components/shell';
import { isRouteAvailableInFinalProductionScope } from '../../../releaseScope';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { supportsContracts } from '../../contracts/shared/serviceContractPolicy';
import { ServiceInterfacesTab } from '../components/ServiceInterfacesTab';

/** Mapeia criticidade para variante do Badge. */
const criticalityBadgeVariant = (level: Criticality): 'danger' | 'warning' | 'default' => {
  switch (level) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'warning';
    case 'Low': return 'default';
    default: return 'default';
  }
};

/** Mapeia ciclo de vida para variante do Badge. */
const lifecycleBadgeVariant = (status: LifecycleStatus): 'success' | 'info' | 'warning' | 'default' => {
  switch (status) {
    case 'Active': return 'success';
    case 'Planning': case 'Development': case 'Staging': return 'info';
    case 'Deprecating': case 'Deprecated': return 'warning';
    case 'Retired': return 'default';
    default: return 'default';
  }
};

/** Mapeia protocolo de contrato para variante do Badge. */
const protocolBadgeVariant = (protocol: string): 'success' | 'info' | 'warning' | 'default' => {
  switch (protocol) {
    case 'OpenApi': case 'Swagger': return 'success';
    case 'AsyncApi': case 'Wsdl': return 'info';
    case 'Protobuf': case 'GraphQl': return 'warning';
    default: return 'default';
  }
};

/** Mapeia ciclo de vida de contrato para variante do Badge. */
const contractLifecycleBadgeVariant = (state: string): 'success' | 'info' | 'warning' | 'danger' | 'default' => {
  switch (state) {
    case 'Approved': return 'success';
    case 'InReview': return 'info';
    case 'Locked': return 'info';
    case 'Draft': return 'default';
    case 'Deprecated': return 'warning';
    case 'Sunset': case 'Retired': return 'danger';
    default: return 'default';
  }
};

type ServiceTab = 'overview' | 'apis' | 'contracts' | 'interfaces';

/** Página de detalhe de um serviço do catálogo — redesenhada com EntityHeader + Tabs. */
export function ServiceDetailPage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string}>();
  const { activeEnvironment } = useEnvironment();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<ServiceTab>('overview');

  const { data: service, isLoading, isError } = useQuery({
    queryKey: ['catalog-service-detail', serviceId],
    queryFn: () => serviceCatalogApi.getServiceDetail(serviceId!),
    enabled: !!serviceId,
  });

  const { data: serviceContracts } = useQuery({
    queryKey: ['catalog-service-contracts', serviceId],
    queryFn: () => contractsApi.listContractsByService(serviceId!),
    enabled: !!serviceId,
  });

  const contracts = serviceContracts?.contracts ?? serviceContracts?.items ?? [];

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState size="lg" />
      </PageContainer>
    );
  }

  if (isError || !service) {
    return (
      <PageContainer>
        <PageErrorState
          message={t('common.noResults')}
          action={
            <Link to="/services" className="text-sm text-accent hover:underline">
              {t('common.back')}
            </Link>
          }
        />
      </PageContainer>
    );
  }

  const tabItems = [
    { id: 'overview', label: t('catalog.detail.overview'), icon: <FileText size={14} /> },
    {
      id: 'apis',
      label: `${t('catalog.detail.apis')} (${service.apiCount ?? service.apis?.length ?? 0})`,
      icon: <Globe size={14} />,
    },
    {
      id: 'contracts',
      label: `${t('catalog.detail.contracts')} (${serviceContracts?.totalCount ?? contracts.length})`,
      icon: <FileText size={14} />,
    },
    {
      id: 'interfaces',
      label: t('serviceDetail.tabInterfaces', 'Interfaces'),
      icon: <Server size={14} />,
    },
  ];

  return (
    <PageContainer className="animate-fade-in">
      {/* ── EntityHeader ── */}
      <EntityHeader
        name={service.displayName || service.name}
        entityType={t(`catalog.badges.type.${service.serviceType}`)}
        icon={<Server size={22} />}
        status={{
          label: t(`catalog.badges.lifecycle.${service.lifecycleStatus}`),
          variant: lifecycleBadgeVariant(service.lifecycleStatus),
        }}
        criticality={{
          label: t(`catalog.badges.criticality.${service.criticality}`),
          variant: criticalityBadgeVariant(service.criticality),
        }}
        owner={service.teamName}
        meta={[service.domain, service.systemArea].filter(Boolean) as string[]}
        description={service.description}
        backLink={{
          to: '/services',
          label: t('catalog.detail.backToCatalog', 'Back to Service Catalog'),
        }}
      />

      {/* ── Tabs ── */}
      <Tabs
        items={tabItems}
        activeId={activeTab}
        onChange={(id) => setActiveTab(id as ServiceTab)}
        className="mb-6"
      />

      {/* ── Tab content ── */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main column */}
        <div className="lg:col-span-2 flex flex-col gap-6">

          {/* OVERVIEW TAB */}
          {activeTab === 'overview' && (
            <PageSection>
              <Card>
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <FileText size={16} className="text-accent" aria-hidden="true" />
                    <h2 className="text-base font-semibold text-heading">
                      {t('catalog.detail.overview')}
                    </h2>
                  </div>
                </CardHeader>
                <CardBody>
                  <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                    <DetailField label={t('catalog.detail.description')} value={service.description} />
                    <DetailField label={t('catalog.detail.systemArea')} value={service.systemArea} />
                    <DetailField label={t('catalog.columns.domain')} value={service.domain} />
                    <DetailField
                      label={t('catalog.columns.serviceType')}
                      value={t(`catalog.badges.type.${service.serviceType}`)}
                    />
                  </dl>
                </CardBody>
              </Card>

              {/* Recent Changes */}
              <Card className="mt-4">
                <CardHeader>
                  <div className="flex items-center gap-2">
                    <GitCommit size={16} className="text-accent" aria-hidden="true" />
                    <h2 className="text-base font-semibold text-heading">
                      {t('catalog.detail.recentChanges')}
                    </h2>
                  </div>
                </CardHeader>
                <CardBody>
                  <p className="text-xs text-muted mb-3">{t('catalog.detail.recentChangesDescription')}</p>
                  <Link
                    to={`/changes?serviceName=${encodeURIComponent(service.name)}`}
                    className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                  >
                    <ExternalLink size={12} />
                    {t('catalog.detail.viewChange')}
                  </Link>
                </CardBody>
              </Card>

              {isRouteAvailableInFinalProductionScope('/operations/incidents') && (
                <Card className="mt-4">
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <AlertTriangle size={16} className="text-accent" aria-hidden="true" />
                      <h2 className="text-base font-semibold text-heading">
                        {t('catalog.detail.relatedIncidents')}
                      </h2>
                    </div>
                  </CardHeader>
                  <CardBody>
                    <p className="text-xs text-muted mb-3">{t('catalog.detail.relatedIncidentsDescription')}</p>
                    <Link
                      to="/operations/incidents"
                      className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                    >
                      <ExternalLink size={12} />
                      {t('catalog.detail.viewIncident')}
                    </Link>
                  </CardBody>
                </Card>
              )}
            </PageSection>
          )}

          {/* APIS TAB */}
          {activeTab === 'apis' && (
            <PageSection>
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Globe size={16} className="text-accent" aria-hidden="true" />
                      <h2 className="text-base font-semibold text-heading">
                        {t('catalog.detail.apis')}
                      </h2>
                    </div>
                    <span className="text-xs text-muted">
                      {t('catalog.detail.apiAndConsumerSummary', {
                        apiCount: service.apiCount,
                        consumerCount: service.totalConsumers,
                      })}
                    </span>
                  </div>
                </CardHeader>
                <CardBody className="p-0">
                  {service.apis.length === 0 ? (
                    <div className="py-10 text-center">
                      <p className="text-sm text-muted">{t('catalog.detail.noApis')}</p>
                    </div>
                  ) : (
                    <TableWrapper>
                      <table className="w-full text-sm">
                        <thead className="sticky top-0 z-10 bg-panel">
                          <tr className="border-b border-edge text-left">
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.columns.name')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.detail.routePattern')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.detail.version')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.detail.visibility')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.detail.consumers')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.detail.status')}
                            </th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-edge">
                          {service.apis.map((api: ServiceApiSummary) => (
                            <tr key={api.apiId} className="hover:bg-elevated/50 transition-colors">
                              <td className="px-4 py-3 font-medium text-heading">{api.name}</td>
                              <td className="px-4 py-3 text-muted font-mono text-xs">{api.routePattern}</td>
                              <td className="px-4 py-3 text-muted">{api.version}</td>
                              <td className="px-4 py-3">
                                <span className="inline-flex items-center gap-1 text-xs text-muted">
                                  <Eye size={12} aria-hidden="true" />
                                  {api.visibility}
                                </span>
                              </td>
                              <td className="px-4 py-3 text-muted">{api.consumerCount}</td>
                              <td className="px-4 py-3">
                                {api.isDecommissioned ? (
                                  <Badge variant="danger" size="sm">
                                    {t('catalog.detail.decommissioned')}
                                  </Badge>
                                ) : (
                                  <Badge variant="success" size="sm">
                                    {t('catalog.detail.active')}
                                  </Badge>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </TableWrapper>
                  )}
                </CardBody>
              </Card>
            </PageSection>
          )}

          {/* CONTRACTS TAB */}
          {activeTab === 'contracts' && (
            <PageSection>
              <Card>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <FileText size={16} className="text-accent" aria-hidden="true" />
                      <h2 className="text-base font-semibold text-heading">
                        {t('catalog.detail.contracts')}
                      </h2>
                    </div>
                    {serviceContracts && (
                      <span className="text-xs text-muted">
                        {serviceContracts.totalCount} {t('catalog.detail.contractsCount')}
                      </span>
                    )}
                    {service.serviceType && supportsContracts(service.serviceType as ServiceType) ? (
                      <button
                        type="button"
                        onClick={() => navigate(`/contracts/new?serviceId=${serviceId}`)}
                        className="flex items-center gap-1 text-xs font-medium text-accent hover:text-accent/80 border border-accent/30 rounded px-2.5 py-1 transition-colors"
                      >
                        <Plus size={13} aria-hidden="true" />
                        {t('catalog.services.addContract', 'Add Contract')}
                      </button>
                    ) : service.serviceType ? (
                      <div className="flex items-center gap-1 text-xs text-muted border border-edge rounded px-2.5 py-1">
                        <Info size={12} aria-hidden="true" />
                        {t('catalog.services.noContractsForType', 'No public contracts for this type')}
                      </div>
                    ) : null}
                  </div>
                </CardHeader>
                <CardBody className="p-0">
                  {contracts.length === 0 ? (
                    <div className="py-10 text-center">
                      <p className="text-sm text-muted">{t('catalog.detail.noContracts')}</p>
                    </div>
                  ) : (
                    <TableWrapper>
                      <table className="w-full text-sm">
                        <thead className="sticky top-0 z-10 bg-panel">
                          <tr className="border-b border-edge text-left">
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.columns.name')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.detail.version')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('contractGov.columns.protocol')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('contractGov.columns.lifecycle')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('contractGov.columns.locked')}
                            </th>
                            <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                              {t('catalog.columns.actions')}
                            </th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-edge">
                          {contracts.map((contract: ServiceContractItem) => (
                            <tr key={contract.versionId ?? contract.contractVersionId} className="hover:bg-elevated/50 transition-colors">
                              <td className="px-4 py-3">
                                <span className="font-medium text-heading">{contract.apiName ?? '—'}</span>
                                <span className="block text-xs text-muted font-mono">{contract.apiRoutePattern ?? '—'}</span>
                              </td>
                              <td className="px-4 py-3 text-muted font-mono text-xs">v{contract.semVer ?? contract.version}</td>
                              <td className="px-4 py-3">
                                <Badge variant={protocolBadgeVariant(contract.protocol)} size="sm">
                                  {t(`contractGov.badges.protocols.${contract.protocol}`, contract.protocol)}
                                </Badge>
                              </td>
                              <td className="px-4 py-3">
                                <Badge variant={contractLifecycleBadgeVariant(contract.lifecycleState)} size="sm">
                                  {t(`contractGov.badges.lifecycle.${contract.lifecycleState}`, contract.lifecycleState)}
                                </Badge>
                              </td>
                              <td className="px-4 py-3">
                                {contract.isLocked ? (
                                  <Lock size={14} className="text-info" aria-label={t('contractGov.columns.locked')} />
                                ) : (
                                  <span className="text-xs text-muted">—</span>
                                )}
                              </td>
                              <td className="px-4 py-3">
                                <Link
                                  to={`/source-of-truth/contracts/${contract.versionId ?? contract.contractVersionId}`}
                                  className="text-xs text-accent hover:underline"
                                >
                                  {t('catalog.detail.viewContract')}
                                </Link>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </TableWrapper>
                  )}
                </CardBody>
              </Card>
            </PageSection>
          )}

          {/* INTERFACES TAB */}
          {activeTab === 'interfaces' && (
            <PageSection>
              <ServiceInterfacesTab serviceId={serviceId!} />
            </PageSection>
          )}
        </div>

        {/* ── Sidebar column (always visible) ── */}
        <div className="flex flex-col gap-6">
          {/* Ownership */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Users size={16} className="text-accent" aria-hidden="true" />
                <h2 className="text-base font-semibold text-heading">
                  {t('catalog.detail.ownership')}
                </h2>
              </div>
            </CardHeader>
            <CardBody>
              <dl className="flex flex-col gap-3 text-sm">
                <DetailField label={t('catalog.detail.team')} value={service.teamName} />
                <DetailField
                  label={t('catalog.detail.technicalOwner')}
                  value={service.technicalOwner}
                />
                <DetailField
                  label={t('catalog.detail.businessOwner')}
                  value={service.businessOwner}
                />
              </dl>
            </CardBody>
          </Card>

          {/* Classificação */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Shield size={16} className="text-accent" aria-hidden="true" />
                <h2 className="text-base font-semibold text-heading">
                  {t('catalog.detail.classification')}
                </h2>
              </div>
            </CardHeader>
            <CardBody>
              <dl className="flex flex-col gap-3 text-sm">
                <div>
                  <dt className="text-xs text-muted mb-1">{t('catalog.detail.criticality')}</dt>
                  <dd>
                    <Badge variant={criticalityBadgeVariant(service.criticality)}>
                      {t(`catalog.badges.criticality.${service.criticality}`)}
                    </Badge>
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-muted mb-1">{t('catalog.detail.lifecycleStatus')}</dt>
                  <dd>
                    <Badge variant={lifecycleBadgeVariant(service.lifecycleStatus)}>
                      {t(`catalog.badges.lifecycle.${service.lifecycleStatus}`)}
                    </Badge>
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-muted mb-1">{t('catalog.detail.exposureType')}</dt>
                  <dd className="text-heading">
                    {t(`catalog.badges.exposure.${service.exposureType}`)}
                  </dd>
                </div>
              </dl>
            </CardBody>
          </Card>

          {/* Lifecycle Transition */}
          <ServiceLifecyclePanel
            serviceId={serviceId!}
            serviceName={service.displayName || service.name}
            currentStatus={service.lifecycleStatus}
          />

          {/* Links (enriched) */}
          <ServiceLinksSection serviceId={serviceId!} />
        </div>
      </div>

      <div className="mt-6">
        <AssistantPanel
          contextType="service"
          contextId={serviceId!}
          contextSummary={{
            name: service.displayName || service.name,
            description: service.description,
            status: service.lifecycleStatus,
            additionalInfo: {
              ...(service.criticality ? { criticality: service.criticality } : {}),
              ...(service.teamName ? { team: service.teamName } : {}),
              ...(service.domain ? { domain: service.domain } : {}),
            },
          }}
          contextData={{
            entityType: 'service',
            entityName: service.displayName || service.name,
            entityStatus: service.lifecycleStatus,
            entityDescription: service.description,
            properties: {
              ...(service.criticality ? { criticality: service.criticality } : {}),
              ...(service.teamName ? { team: service.teamName } : {}),
              ...(service.domain ? { domain: service.domain } : {}),
              ...(service.serviceType ? { serviceType: service.serviceType } : {}),
              ...(service.technicalOwner ? { technicalOwner: service.technicalOwner } : {}),
              ...(service.exposureType ? { exposure: service.exposureType } : {}),
              ...(service.apiCount != null ? { apiCount: String(service.apiCount) } : {}),
              ...(service.totalConsumers != null ? { consumers: String(service.totalConsumers) } : {}),
            },
            relations: [
              ...(contracts.map((c: ServiceContractItem) => ({
                relationType: 'Contracts',
                entityType: 'contract',
                name: c.apiName || c.versionId || c.contractVersionId,
                status: c.lifecycleState,
                properties: {
                  ...(c.protocol ? { protocol: c.protocol } : {}),
                  ...(c.semVer || c.version ? { version: c.semVer ?? c.version } : {}),
                },
              })) || []),
            ],
            caveats: [
              ...(!contracts.length ? [t('assistantPanel.contextCaveats.noContractsLoaded')] : []),
            ].filter(Boolean),
          }}
          activeEnvironmentId={activeEnvironment?.id}
          activeEnvironmentName={activeEnvironment?.name}
          isNonProductionEnvironment={activeEnvironment ? !activeEnvironment.isProductionLike : false}
        />
      </div>
    </PageContainer>
  );
}

/* ── Componentes internos ─────────────────────────────────────────── */

/** Campo de detalhe reutilizável (label + valor). */
function DetailField({ label, value }: { label: string; value: string | undefined | null }) {
  const { t } = useTranslation();
  return (
    <div>
      <dt className="text-xs text-muted mb-0.5">{label}</dt>
      <dd className="text-heading">{value || t('common.noData')}</dd>
    </div>
  );
}
