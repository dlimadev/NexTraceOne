/**
 * Tab de interfaces de exposição de um serviço no ServiceDetailPage.
 * Exibe a lista de ServiceInterface registadas e permite navegar para criar nova.
 */
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Plus,
  Globe,
  Zap,
  Server,
  GitBranch,
  Cpu,
  Clock,
  Webhook,
  Database,
  Layers,
  Link2,
  FileText,
  CheckCircle2,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { Drawer } from '../../../components/Drawer';
import { TableWrapper } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { serviceCatalogApi } from '../api';
import type { InterfaceType, InterfaceStatus, ServiceInterface } from '../../../types';

interface ServiceInterfacesTabProps {
  serviceId: string;
}

/** Ícone por tipo de interface. */
const interfaceTypeIcon: Record<InterfaceType, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  SoapService: Server,
  KafkaProducer: Zap,
  KafkaConsumer: Zap,
  GrpcService: Cpu,
  GraphqlApi: GitBranch,
  BackgroundWorker: Cpu,
  ScheduledJob: Clock,
  WebhookProducer: Webhook,
  WebhookConsumer: Webhook,
  ZosConnectApi: Layers,
  MqQueue: Database,
  IntegrationBridge: Layers,
};

/** Variante de badge por estado da interface. */
const statusBadgeVariant = (status: InterfaceStatus): 'success' | 'warning' | 'danger' | 'default' => {
  switch (status) {
    case 'Active': return 'success';
    case 'Deprecated': return 'warning';
    case 'Sunset': return 'danger';
    case 'Retired': return 'default';
    default: return 'default';
  }
};

/** Variante de badge por escopo de exposição. */
const exposureBadgeVariant = (scope: string): 'info' | 'warning' | 'default' => {
  switch (scope) {
    case 'External': return 'warning';
    case 'Partner': return 'info';
    default: return 'default';
  }
};

/** Tipos de interface que expõem uma API consumível externamente. */
const API_INTERFACE_TYPES: ReadonlySet<InterfaceType> = new Set([
  'RestApi', 'GraphqlApi', 'GrpcService', 'SoapService',
]);

/** Campo contextual principal da interface (basePath, topicName, etc.) */
function interfacePrimaryField(iface: ServiceInterface): string {
  if (iface.basePath) return iface.basePath;
  if (iface.topicName) return iface.topicName;
  if (iface.grpcServiceName) return iface.grpcServiceName;
  if (iface.scheduleCron) return iface.scheduleCron;
  if (iface.wsdlNamespace) return iface.wsdlNamespace;
  return '—';
}

/** Componente de tab de interfaces de um serviço. */
export function ServiceInterfacesTab({ serviceId }: ServiceInterfacesTabProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [bindingsFor, setBindingsFor] = useState<ServiceInterface | null>(null);

  const { data: interfaces, isLoading, isError } = useQuery({
    queryKey: ['service-interfaces', serviceId],
    queryFn: () => serviceCatalogApi.listServiceInterfaces(serviceId),
    enabled: !!serviceId,
  });

  if (isLoading) {
    return <PageLoadingState size="sm" />;
  }

  if (isError) {
    return (
      <PageErrorState
        message={t('common.errorLoading', 'Failed to load interfaces.')}
      />
    );
  }

  const items = interfaces ?? [];

  return (
    <>
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Layers size={16} className="text-accent" aria-hidden="true" />
            <h2 className="text-base font-semibold text-heading">
              {t('serviceInterfaces.title', 'Service Interfaces')}
            </h2>
            {items.length > 0 && (
              <span className="text-xs text-muted">({items.length})</span>
            )}
          </div>
          <Button
            size="sm"
            onClick={() => navigate(`/services/${serviceId}/interfaces/new`)}
          >
            <Plus size={14} className="mr-1" />
            {t('serviceInterfaces.add', 'Add Interface')}
          </Button>
        </div>
      </CardHeader>
      <CardBody className="p-0">
        {items.length === 0 ? (
          <div className="py-12 text-center px-4">
            <Layers size={32} className="mx-auto text-muted mb-3" aria-hidden="true" />
            <p className="text-sm font-medium text-heading mb-1">
              {t('serviceInterfaces.empty', 'No interfaces registered for this service.')}
            </p>
            <p className="text-xs text-muted mb-4">
              {t('serviceInterfaces.emptyHint', 'Add an interface to associate contracts to this service.')}
            </p>
            <Button
              size="sm"
              onClick={() => navigate(`/services/${serviceId}/interfaces/new`)}
            >
              <Plus size={14} className="mr-1" />
              {t('serviceInterfaces.add', 'Add Interface')}
            </Button>
          </div>
        ) : (
          <TableWrapper>
            <table className="w-full text-sm">
              <thead className="sticky top-0 z-10 bg-panel">
                <tr className="border-b border-edge text-left">
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceInterfaces.name', 'Name')}
                  </th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceInterfaces.type', 'Type')}
                  </th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceInterfaces.exposure', 'Exposure')}
                  </th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceInterfaces.status', 'Status')}
                  </th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceInterfaces.basePath', 'Path / Topic')}
                  </th>
                  <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceInterfaces.requiresContract', 'Contract')}
                  </th>
                  <th scope="col" className="px-4 py-3" />
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {items.map((iface) => {
                  const Icon = interfaceTypeIcon[iface.interfaceType] ?? Layers;
                  return (
                    <tr key={iface.interfaceId} className="hover:bg-elevated/50 transition-colors">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <Icon size={14} className="text-accent shrink-0" aria-hidden="true" />
                          <span className="font-medium text-heading">{iface.name}</span>
                        </div>
                        {iface.description && (
                          <p className="text-xs text-muted mt-0.5 truncate max-w-xs">{iface.description}</p>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant="info" size="sm">
                          {t(`serviceInterfaces.type${iface.interfaceType}`, iface.interfaceType)}
                        </Badge>
                        {API_INTERFACE_TYPES.has(iface.interfaceType) && (
                          <Badge variant="success" size="sm" className="ml-1">{t('serviceInterfaces.apiBadge', 'API')}</Badge>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={exposureBadgeVariant(iface.exposureScope)} size="sm">
                          {t(`catalog.badges.exposure.${iface.exposureScope}`, iface.exposureScope)}
                        </Badge>
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={statusBadgeVariant(iface.status)} size="sm">
                          {t(`serviceInterfaces.status${iface.status}`, iface.status)}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-muted">
                        {interfacePrimaryField(iface)}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {iface.requiresContract ? (
                          <CheckCircle2 size={14} className="text-mint mx-auto" aria-label={t('serviceInterfaces.requiresContract')} />
                        ) : (
                          <AlertTriangle size={14} className="text-muted mx-auto" aria-label={t('serviceInterfaces.requiresContract')} />
                        )}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button
                          type="button"
                          className="text-xs text-accent hover:underline"
                          onClick={() => setBindingsFor(iface)}
                        >
                          {t('serviceInterfaces.viewBindings', 'View Bindings')}
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </TableWrapper>
        )}
      </CardBody>
    </Card>
    <InterfaceBindingsDrawer iface={bindingsFor} onClose={() => setBindingsFor(null)} />
    </>
  );
}

/** Variante de badge por estado de um contract binding. */
function bindingStatusVariant(status: string): 'success' | 'warning' | 'danger' | 'default' {
  switch (status) {
    case 'Active': return 'success';
    case 'Pending': return 'warning';
    case 'Deprecated': return 'warning';
    case 'Revoked': return 'danger';
    default: return 'default';
  }
}

/**
 * Drawer lateral que lista os contract bindings de uma interface de serviço.
 * Abre in-place a partir de "Ver Ligações" — não navega para fora do detalhe.
 */
function InterfaceBindingsDrawer({
  iface,
  onClose,
}: {
  iface: ServiceInterface | null;
  onClose: () => void;
}) {
  const { t } = useTranslation();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['interface-bindings', iface?.interfaceId],
    queryFn: () => serviceCatalogApi.listContractBindings(iface!.interfaceId),
    enabled: !!iface,
  });

  const bindings = data ?? [];

  return (
    <Drawer
      open={!!iface}
      onClose={onClose}
      size="lg"
      title={t('serviceInterfaces.bindings.title', 'Contract Bindings')}
      description={iface?.name}
    >
      {isLoading ? (
        <PageLoadingState size="sm" />
      ) : isError ? (
        <PageErrorState message={t('common.errorLoading', 'Failed to load bindings.')} />
      ) : bindings.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-2 py-12 text-center">
          <Link2 size={28} className="text-muted" aria-hidden="true" />
          <p className="text-sm font-medium text-heading">
            {t('serviceInterfaces.bindings.empty', 'No contract bindings')}
          </p>
          <p className="text-xs text-muted max-w-xs">
            {t('serviceInterfaces.bindings.emptyDesc', 'This interface has no contracts bound to it yet.')}
          </p>
        </div>
      ) : (
        <ul className="space-y-3">
          {bindings.map((b) => (
            <li key={b.bindingId} className="rounded-lg border border-edge bg-elevated/40 p-4">
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-2 min-w-0">
                  <FileText size={14} className="text-accent shrink-0" aria-hidden="true" />
                  <span className="font-mono text-xs text-heading truncate">{b.contractVersionId}</span>
                </div>
                <div className="flex items-center gap-1.5 shrink-0">
                  {b.isDefaultVersion && (
                    <Badge variant="info" size="sm">{t('serviceInterfaces.bindings.default', 'Default')}</Badge>
                  )}
                  <Badge variant={bindingStatusVariant(b.status)} size="sm">
                    {t(`serviceInterfaces.bindings.status${b.status}`, b.status)}
                  </Badge>
                </div>
              </div>
              <dl className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-xs">
                <div className="flex justify-between">
                  <dt className="text-muted">{t('serviceInterfaces.bindings.environment', 'Environment')}</dt>
                  <dd className="text-body">{b.bindingEnvironment}</dd>
                </div>
                {b.activatedBy && (
                  <div className="flex justify-between">
                    <dt className="text-muted">{t('serviceInterfaces.bindings.activatedBy', 'Activated by')}</dt>
                    <dd className="text-body truncate ml-2">{b.activatedBy}</dd>
                  </div>
                )}
              </dl>
              {b.migrationNotes && (
                <p className="mt-2 text-xs text-muted border-t border-edge/60 pt-2">{b.migrationNotes}</p>
              )}
            </li>
          ))}
        </ul>
      )}
    </Drawer>
  );
}
