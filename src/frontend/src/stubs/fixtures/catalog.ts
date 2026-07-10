/**
 * Fixtures do catálogo de serviços para o modo stub (npm run stub).
 *
 * Cobrem a jornada principal do catálogo: listagem, resumo agregado e detalhe.
 * Servem também de template — copiar o padrão para stubar outros módulos.
 *
 * @see src/stubs/handlers/catalog.ts
 */
import type {
  ServiceListItem,
  ServiceListResponse,
  ServicesSummary,
  ServiceDetail,
} from '../../types';

/** Serviços do catálogo stub. */
export const stubServices: ServiceListItem[] = [
  {
    serviceId: 'svc-payments-api',
    name: 'payments-api',
    displayName: 'Payments API',
    description: 'Serviço central de processamento de pagamentos e reconciliação.',
    serviceType: 'RestApi',
    domain: 'Billing',
    systemArea: 'Core',
    teamName: 'Payments',
    technicalOwner: 'ana.silva@nextraceone.dev',
    criticality: 'Critical',
    lifecycleStatus: 'Active',
    exposureType: 'External',
  },
  {
    serviceId: 'svc-orders-api',
    name: 'orders-api',
    displayName: 'Orders API',
    description: 'Gestão do ciclo de vida de encomendas.',
    serviceType: 'RestApi',
    domain: 'Commerce',
    systemArea: 'Core',
    teamName: 'Commerce',
    technicalOwner: 'joao.pereira@nextraceone.dev',
    criticality: 'High',
    lifecycleStatus: 'Active',
    exposureType: 'Internal',
  },
  {
    serviceId: 'svc-notifications-worker',
    name: 'notifications-worker',
    displayName: 'Notifications Worker',
    description: 'Processador assíncrono de notificações por email e Slack.',
    serviceType: 'BackgroundService',
    domain: 'Platform',
    systemArea: 'Supporting',
    teamName: 'Platform',
    technicalOwner: 'maria.costa@nextraceone.dev',
    criticality: 'Medium',
    lifecycleStatus: 'Active',
    exposureType: 'Internal',
  },
  {
    serviceId: 'svc-inventory-graphql',
    name: 'inventory-graphql',
    displayName: 'Inventory GraphQL',
    description: 'API GraphQL de inventário e disponibilidade de stock.',
    serviceType: 'GraphqlApi',
    domain: 'Commerce',
    systemArea: 'Core',
    teamName: 'Commerce',
    technicalOwner: 'joao.pereira@nextraceone.dev',
    criticality: 'High',
    lifecycleStatus: 'Staging',
    exposureType: 'Partner',
  },
  {
    serviceId: 'svc-legacy-billing',
    name: 'legacy-billing',
    displayName: 'Legacy Billing',
    description: 'Sistema mainframe de faturação em processo de descomissionamento.',
    serviceType: 'LegacySystem',
    domain: 'Billing',
    systemArea: 'Legacy',
    teamName: 'Billing',
    technicalOwner: 'carlos.dias@nextraceone.dev',
    criticality: 'Medium',
    lifecycleStatus: 'Deprecating',
    exposureType: 'Internal',
  },
];

/** Resposta da listagem. */
export const stubServiceList: ServiceListResponse = {
  items: stubServices,
  totalCount: stubServices.length,
};

/** Contagem de ocorrências por chave num array de serviços. */
function groupBy<K extends keyof ServiceListItem>(key: K) {
  const counts = new Map<string, number>();
  for (const svc of stubServices) {
    const value = String(svc[key]);
    counts.set(value, (counts.get(value) ?? 0) + 1);
  }
  return Array.from(counts, ([k, count]) => ({ key: k, count }));
}

/** Resumo agregado (stat cards da página de catálogo). */
export const stubServicesSummary: ServicesSummary = {
  totalCount: stubServices.length,
  criticalCount: stubServices.filter((s) => s.criticality === 'Critical').length,
  highCriticalityCount: stubServices.filter((s) => s.criticality === 'High').length,
  activeCount: stubServices.filter((s) => s.lifecycleStatus === 'Active').length,
  deprecatedCount: stubServices.filter((s) => s.lifecycleStatus === 'Deprecated').length,
  retiredCount: stubServices.filter((s) => s.lifecycleStatus === 'Retired').length,
  byServiceType: groupBy('serviceType'),
  byCriticality: groupBy('criticality'),
  byLifecycle: groupBy('lifecycleStatus'),
  byDomain: groupBy('domain'),
  byTeam: groupBy('teamName'),
};

/** Constrói um detalhe completo a partir de um item da listagem. */
function toDetail(item: ServiceListItem): ServiceDetail {
  return {
    ...item,
    businessOwner: 'business.owner@nextraceone.dev',
    documentationUrl: `https://docs.nextraceone.dev/${item.name}`,
    repositoryUrl: `https://github.com/nextraceone/${item.name}`,
    subDomain: '',
    capability: '',
    dataClassification: 'Internal',
    regulatoryScope: '',
    infrastructureProvider: 'AWS',
    hostingPlatform: 'Kubernetes',
    runtimeLanguage: 'C#',
    runtimeVersion: '.NET 10',
    sloTarget: '99.9%',
    changeFrequency: 'Weekly',
    productOwner: 'product.owner@nextraceone.dev',
    contactChannel: '#team-channel',
    gitRepository: `https://github.com/nextraceone/${item.name}`,
    ciPipelineUrl: `https://ci.nextraceone.dev/${item.name}`,
    interfaces: [],
    apis: [],
    apiCount: 0,
    totalConsumers: 0,
  };
}

/** Mapa serviceId → detalhe, para GET /catalog/services/:id. */
export const stubServiceDetails: Record<string, ServiceDetail> = Object.fromEntries(
  stubServices.map((s) => [s.serviceId, toDetail(s)]),
);

/** Detalhe genérico para IDs desconhecidos (serviços criados no stub, etc.). */
export function buildFallbackDetail(serviceId: string): ServiceDetail {
  return toDetail({
    serviceId,
    name: serviceId,
    displayName: serviceId,
    description: 'Serviço stub gerado dinamicamente.',
    serviceType: 'RestApi',
    domain: 'Platform',
    systemArea: 'Core',
    teamName: 'Platform',
    technicalOwner: 'stub.admin@nextraceone.dev',
    criticality: 'Medium',
    lifecycleStatus: 'Active',
    exposureType: 'Internal',
  });
}
