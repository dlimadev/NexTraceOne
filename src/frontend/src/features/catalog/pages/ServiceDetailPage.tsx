import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  Server,
  Shield,
  Users,
  Globe,
  FileText,
  GitBranch,
  ExternalLink,
  Layers,
  Eye,
  Lock,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { serviceCatalogApi } from '../api';
import { contractsApi } from '../api/contracts';
import { AssistantPanel } from '../../ai-hub/components/AssistantPanel';
import type { Criticality, LifecycleStatus, ServiceApiSummary, ServiceContractItem } from '../../../types';

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

/** Variantes visuais para badges de protocolo de contrato. */
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Protobuf: 'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

/** Variantes visuais para badges de ciclo de vida de contrato. */
const contractLifecycleColors: Record<string, string> = {
  Draft: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
  InReview: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Approved: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Locked: 'bg-purple-900/40 text-purple-300 border border-purple-700/50',
  Deprecated: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Sunset: 'bg-red-900/40 text-red-300 border border-red-700/50',
  Retired: 'bg-slate-900/40 text-slate-400 border border-slate-700/50',
};

/** Página de detalhe de um serviço do catálogo. */
export function ServiceDetailPage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string }>();

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

  if (isLoading) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in flex items-center justify-center min-h-[60vh]">
        <p className="text-sm text-muted">{t('common.loading')}</p>
      </div>
    );
  }

  if (isError || !service) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <EmptyState
          icon={<Server size={24} />}
          title={t('common.error')}
          description={t('common.noResults')}
          action={
            <Link to="/services" className="text-sm text-accent hover:underline">
              {t('common.back')}
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* ── Navegação ── */}
      <Link
        to="/services"
        className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4"
      >
        <ArrowLeft size={14} />
        {t('common.back')}
      </Link>

      {/* ── Cabeçalho do serviço ── */}
      <div className="mb-6">
        <div className="flex flex-wrap items-center gap-3 mb-2">
          <h1 className="text-2xl font-bold text-heading">
            {service.displayName || service.name}
          </h1>
          <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full bg-blue-900/30 text-blue-300 border border-blue-700/50">
            {t(`catalog.badges.type.${service.serviceType}`)}
          </span>
          <span
            className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[service.lifecycleStatus]}`}
          >
            {t(`catalog.badges.lifecycle.${service.lifecycleStatus}`)}
          </span>
          <span
            className={`inline-flex text-xs px-2 py-0.5 rounded-full ${criticalityColors[service.criticality]}`}
          >
            {t(`catalog.badges.criticality.${service.criticality}`)}
          </span>
        </div>
        {service.description && (
          <p className="text-muted text-sm max-w-2xl">{service.description}</p>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* ── Coluna principal ── */}
        <div className="lg:col-span-2 flex flex-col gap-6">
          {/* Visão geral */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <FileText size={16} className="text-accent" />
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

          {/* APIs associadas */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Globe size={16} className="text-accent" />
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
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge text-left">
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.columns.name')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.detail.routePattern')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.detail.version')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.detail.visibility')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.detail.consumers')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
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
                              <Eye size={12} />
                              {api.visibility}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-muted">{api.consumerCount}</td>
                          <td className="px-4 py-3">
                            {api.isDecommissioned ? (
                              <span className="text-xs px-2 py-0.5 rounded-full bg-red-900/40 text-red-300 border border-red-700/50">
                                {t('catalog.detail.decommissioned')}
                              </span>
                            ) : (
                              <span className="text-xs px-2 py-0.5 rounded-full bg-emerald-900/40 text-emerald-300 border border-emerald-700/50">
                                {t('catalog.detail.active')}
                              </span>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>

          {/* Contratos associados */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileText size={16} className="text-accent" />
                  <h2 className="text-base font-semibold text-heading">
                    {t('catalog.detail.contracts')}
                  </h2>
                </div>
                {serviceContracts && (
                  <span className="text-xs text-muted">
                    {serviceContracts.totalCount} {t('catalog.detail.contractsCount')}
                  </span>
                )}
              </div>
            </CardHeader>
            <CardBody className="p-0">
              {!serviceContracts || serviceContracts.contracts.length === 0 ? (
                <div className="py-10 text-center">
                  <p className="text-sm text-muted">{t('catalog.detail.noContracts')}</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge text-left">
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.columns.name')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.detail.version')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('contractGov.columns.protocol')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('contractGov.columns.lifecycle')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('contractGov.columns.locked')}
                        </th>
                        <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                          {t('catalog.columns.actions')}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge">
                      {serviceContracts.contracts.map((contract: ServiceContractItem) => (
                        <tr key={contract.versionId} className="hover:bg-elevated/50 transition-colors">
                          <td className="px-4 py-3">
                            <span className="font-medium text-heading">{contract.apiName}</span>
                            <span className="block text-xs text-muted font-mono">{contract.apiRoutePattern}</span>
                          </td>
                          <td className="px-4 py-3 text-muted font-mono text-xs">v{contract.semVer}</td>
                          <td className="px-4 py-3">
                            <span className={`inline-flex text-xs px-2 py-0.5 rounded-full ${protocolColors[contract.protocol] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                              {t(`contractGov.badges.protocols.${contract.protocol}`, contract.protocol)}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span className={`inline-flex text-xs px-2 py-0.5 rounded-full ${contractLifecycleColors[contract.lifecycleState] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                              {t(`contractGov.badges.lifecycle.${contract.lifecycleState}`, contract.lifecycleState)}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            {contract.isLocked ? (
                              <Lock size={14} className="text-purple-400" />
                            ) : (
                              <span className="text-xs text-muted">—</span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <Link
                              to={`/contracts/${contract.versionId}`}
                              className="text-xs text-accent hover:underline"
                            >
                              {t('catalog.detail.viewContract')}
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

        {/* ── Barra lateral ── */}
        <div className="flex flex-col gap-6">
          {/* Ownership */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Users size={16} className="text-accent" />
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
                <Shield size={16} className="text-accent" />
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
                    <span
                      className={`inline-flex text-xs px-2 py-0.5 rounded-full ${criticalityColors[service.criticality]}`}
                    >
                      {t(`catalog.badges.criticality.${service.criticality}`)}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt className="text-xs text-muted mb-1">{t('catalog.detail.lifecycleStatus')}</dt>
                  <dd>
                    <span
                      className={`inline-flex text-xs px-2 py-0.5 rounded-full ${lifecycleColors[service.lifecycleStatus]}`}
                    >
                      {t(`catalog.badges.lifecycle.${service.lifecycleStatus}`)}
                    </span>
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

          {/* Links */}
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Layers size={16} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">
                  {t('catalog.detail.links')}
                </h2>
              </div>
            </CardHeader>
            <CardBody>
              <div className="flex flex-col gap-3 text-sm">
                {service.documentationUrl ? (
                  <a
                    href={service.documentationUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-2 text-accent hover:underline"
                  >
                    <FileText size={14} />
                    {t('catalog.detail.documentationUrl')}
                    <ExternalLink size={12} />
                  </a>
                ) : (
                  <span className="text-muted text-xs">
                    {t('catalog.detail.documentationUrl')}: {t('common.noData')}
                  </span>
                )}
                {service.repositoryUrl ? (
                  <a
                    href={service.repositoryUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-2 text-accent hover:underline"
                  >
                    <GitBranch size={14} />
                    {t('catalog.detail.repositoryUrl')}
                    <ExternalLink size={12} />
                  </a>
                ) : (
                  <span className="text-muted text-xs">
                    {t('catalog.detail.repositoryUrl')}: {t('common.noData')}
                  </span>
                )}
              </div>
            </CardBody>
          </Card>
        </div>
      </div>

      {/* ── AI Assistant Panel ── */}
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
        />
      </div>
    </div>
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
