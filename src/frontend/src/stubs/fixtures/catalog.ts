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
  ServiceInterface,
  ServiceApiSummary,
  AssetGraph,
} from '../../types';
import type {
  ServiceLinksResponse,
  MaturityDashboardResponse,
  OwnershipAuditResponse,
  DiscoveredServicesResponse,
  DiscoveryDashboardResponse,
  ServiceMaturityResponse,
} from '../../features/catalog/api/serviceCatalog';

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

/** APIs expostas por serviço (usadas no detalhe e no grafo). */
const apisByService: Record<string, ServiceApiSummary[]> = {
  'svc-payments-api': [
    { apiId: 'api-payments-v2', name: 'Payments API v2', routePattern: '/api/v2/payments', version: '2.3.0', visibility: 'Public', isDecommissioned: false, consumerCount: 6 },
    { apiId: 'api-refunds-v1', name: 'Refunds API', routePattern: '/api/v1/refunds', version: '1.1.0', visibility: 'Public', isDecommissioned: false, consumerCount: 3 },
  ],
  'svc-orders-api': [
    { apiId: 'api-orders-v1', name: 'Orders API', routePattern: '/api/v1/orders', version: '1.4.2', visibility: 'Internal', isDecommissioned: false, consumerCount: 4 },
  ],
  'svc-inventory-graphql': [
    { apiId: 'api-inventory-gql', name: 'Inventory GraphQL', routePattern: '/graphql', version: '1.0.0', visibility: 'Partner', isDecommissioned: false, consumerCount: 2 },
  ],
  'svc-legacy-billing': [
    { apiId: 'api-legacy-billing', name: 'Legacy Billing SOAP', routePattern: '/soap/billing', version: '3.0.0', visibility: 'Internal', isDecommissioned: false, consumerCount: 1 },
  ],
};

/** Builder conciso de ServiceInterface (preenche os campos obrigatórios). */
function iface(partial: Partial<ServiceInterface> & Pick<ServiceInterface, 'interfaceId' | 'serviceAssetId' | 'name' | 'interfaceType'>): ServiceInterface {
  return {
    description: '',
    status: 'Active',
    exposureScope: 'Internal',
    basePath: '',
    topicName: '',
    wsdlNamespace: '',
    grpcServiceName: '',
    scheduleCron: '',
    environmentId: '',
    sloTarget: '99.9%',
    requiresContract: true,
    authScheme: 'OAuth2',
    rateLimitPolicy: 'Standard',
    documentationUrl: '',
    isDeprecated: false,
    createdAt: '2026-02-01T00:00:00.000Z',
    updatedAt: '2026-06-01T00:00:00.000Z',
    ...partial,
  };
}

/** Interfaces de exposição por serviço. */
export const stubInterfacesByService: Record<string, ServiceInterface[]> = {
  'svc-payments-api': [
    iface({ interfaceId: 'if-pay-rest', serviceAssetId: 'svc-payments-api', name: 'Payments REST v2', interfaceType: 'RestApi', exposureScope: 'External', basePath: '/api/v2/payments', description: 'API REST pública de pagamentos.' }),
    iface({ interfaceId: 'if-pay-events', serviceAssetId: 'svc-payments-api', name: 'payment.settled', interfaceType: 'KafkaProducer', topicName: 'payments.settled.v1', description: 'Eventos de liquidação de pagamento.' }),
  ],
  'svc-orders-api': [
    iface({ interfaceId: 'if-ord-rest', serviceAssetId: 'svc-orders-api', name: 'Orders REST', interfaceType: 'RestApi', basePath: '/api/v1/orders', description: 'Ciclo de vida de encomendas.' }),
  ],
  'svc-notifications-worker': [
    iface({ interfaceId: 'if-ntf-consumer', serviceAssetId: 'svc-notifications-worker', name: 'notifications.dispatch', interfaceType: 'KafkaConsumer', topicName: 'notifications.dispatch.v1', requiresContract: false, description: 'Consome pedidos de notificação.' }),
  ],
  'svc-inventory-graphql': [
    iface({ interfaceId: 'if-inv-gql', serviceAssetId: 'svc-inventory-graphql', name: 'Inventory GraphQL', interfaceType: 'GraphqlApi', exposureScope: 'Partner', basePath: '/graphql', description: 'Consulta de stock e disponibilidade.' }),
  ],
  'svc-legacy-billing': [
    iface({ interfaceId: 'if-leg-soap', serviceAssetId: 'svc-legacy-billing', name: 'Billing SOAP', interfaceType: 'SoapService', status: 'Deprecated', isDeprecated: true, wsdlNamespace: 'urn:nextraceone:billing', description: 'Serviço SOAP legado (a descomissionar).' }),
  ],
};

/** Constrói um detalhe completo a partir de um item da listagem. */
function toDetail(item: ServiceListItem): ServiceDetail {
  const apis = apisByService[item.serviceId] ?? [];
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
    interfaces: stubInterfacesByService[item.serviceId] ?? [],
    apis,
    apiCount: apis.length,
    totalConsumers: apis.reduce((sum, a) => sum + a.consumerCount, 0),
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

// ── Links de serviço ────────────────────────────────────────────────
export const stubLinksByService: Record<string, ServiceLinksResponse> = {
  'svc-payments-api': {
    items: [
      { linkId: 'ln-1', serviceAssetId: 'svc-payments-api', category: 'Repository', title: 'GitHub', url: 'https://github.com/nextraceone/payments-api', description: 'Código-fonte', iconHint: 'github', sortOrder: 0, createdAt: '2026-02-01T00:00:00.000Z' },
      { linkId: 'ln-2', serviceAssetId: 'svc-payments-api', category: 'Runbook', title: 'Runbook de incidentes', url: 'https://runbooks.nextraceone.dev/payments', description: 'Procedimentos on-call', iconHint: 'book', sortOrder: 1, createdAt: '2026-02-01T00:00:00.000Z' },
      { linkId: 'ln-3', serviceAssetId: 'svc-payments-api', category: 'Dashboard', title: 'Grafana', url: 'https://grafana.nextraceone.dev/d/payments', description: 'Métricas de runtime', iconHint: 'chart', sortOrder: 2, createdAt: '2026-02-01T00:00:00.000Z' },
    ],
    totalCount: 3,
  },
};

// ── Maturidade por serviço ──────────────────────────────────────────
interface MaturitySeed { level: string; score: number; ownership: boolean; contracts: boolean; docs: boolean; runbook: boolean; monitoring: boolean; repo: boolean; }
const maturitySeed: Record<string, MaturitySeed> = {
  'svc-payments-api': { level: 'Managed', score: 82, ownership: true, contracts: true, docs: true, runbook: true, monitoring: true, repo: true },
  'svc-orders-api': { level: 'Defined', score: 68, ownership: true, contracts: true, docs: true, runbook: false, monitoring: true, repo: true },
  'svc-notifications-worker': { level: 'Developing', score: 54, ownership: true, contracts: false, docs: false, runbook: false, monitoring: true, repo: true },
  'svc-inventory-graphql': { level: 'Defined', score: 71, ownership: true, contracts: true, docs: true, runbook: false, monitoring: false, repo: true },
  'svc-legacy-billing': { level: 'Initial', score: 33, ownership: false, contracts: false, docs: false, runbook: false, monitoring: false, repo: true },
};

export const stubMaturityByService: Record<string, ServiceMaturityResponse> = Object.fromEntries(
  stubServices.map((s) => {
    const seed = maturitySeed[s.serviceId];
    return [s.serviceId, {
      serviceId: s.serviceId,
      serviceName: s.name,
      displayName: s.displayName,
      teamName: s.teamName,
      domain: s.domain,
      level: seed.level,
      overallScore: seed.score,
      dimensions: [
        { dimension: 'Ownership', score: seed.ownership ? 100 : 20, maxScore: 100, explanation: seed.ownership ? 'Equipa e owner técnico definidos.' : 'Sem owner técnico atribuído.' },
        { dimension: 'Contracts', score: seed.contracts ? 90 : 30, maxScore: 100, explanation: seed.contracts ? 'Contratos publicados e versionados.' : 'APIs sem contrato formal.' },
        { dimension: 'Documentation', score: seed.docs ? 85 : 25, maxScore: 100, explanation: seed.docs ? 'Documentação disponível.' : 'Documentação em falta.' },
        { dimension: 'Observability', score: seed.monitoring ? 80 : 40, maxScore: 100, explanation: seed.monitoring ? 'Métricas e alertas configurados.' : 'Monitorização limitada.' },
      ],
      computedAt: new Date().toISOString(),
    }];
  }),
);

export const stubMaturityDashboard: MaturityDashboardResponse = {
  summary: {
    totalServices: stubServices.length,
    // Escala 0–1: a página faz Math.round(averageScore * 100)%.
    averageScore: Object.values(maturitySeed).reduce((s, m) => s + m.score, 0) / stubServices.length / 100,
    optimizing: 0,
    managed: Object.values(maturitySeed).filter((m) => m.level === 'Managed').length,
    defined: Object.values(maturitySeed).filter((m) => m.level === 'Defined').length,
    developing: Object.values(maturitySeed).filter((m) => m.level === 'Developing').length,
    initial: Object.values(maturitySeed).filter((m) => m.level === 'Initial').length,
    withoutOwnership: Object.values(maturitySeed).filter((m) => !m.ownership).length,
    withoutContracts: Object.values(maturitySeed).filter((m) => !m.contracts).length,
    withoutDocumentation: Object.values(maturitySeed).filter((m) => !m.docs).length,
    withoutRunbooks: Object.values(maturitySeed).filter((m) => !m.runbook).length,
    withoutMonitoring: Object.values(maturitySeed).filter((m) => !m.monitoring).length,
  },
  services: stubServices.map((s) => {
    const seed = maturitySeed[s.serviceId];
    const apis = apisByService[s.serviceId] ?? [];
    return {
      serviceId: s.serviceId,
      serviceName: s.name,
      displayName: s.displayName,
      teamName: s.teamName,
      domain: s.domain,
      criticality: s.criticality,
      lifecycleStatus: s.lifecycleStatus,
      level: seed.level,
      // Escala 0–1: a página faz svc.overallScore * 100 (barra + label).
      overallScore: seed.score / 100,
      hasOwnership: seed.ownership,
      hasContracts: seed.contracts,
      hasDocumentation: seed.docs,
      hasRunbook: seed.runbook,
      hasMonitoring: seed.monitoring,
      hasRepository: seed.repo,
      apiCount: apis.length,
      contractCount: seed.contracts ? apis.length : 0,
      linkCount: stubLinksByService[s.serviceId]?.totalCount ?? 0,
    };
  }),
  computedAt: new Date().toISOString(),
};

// ── Auditoria de ownership ──────────────────────────────────────────
export const stubOwnershipAudit: OwnershipAuditResponse = {
  summary: {
    totalServicesAudited: stubServices.length,
    servicesWithIssues: 2,
    healthyServices: stubServices.length - 2,
    criticalFindings: 1,
    highFindings: 1,
    mediumFindings: 2,
    withoutTeam: 0,
    withoutTechnicalOwner: 1,
    withoutDocumentation: 2,
    withoutRunbook: 3,
    apisWithoutContracts: 1,
  },
  findings: [
    { serviceId: 'svc-legacy-billing', serviceName: 'legacy-billing', displayName: 'Legacy Billing', teamName: 'Billing', domain: 'Billing', criticality: 'Medium', lifecycleStatus: 'Deprecating', severity: 'Critical', findings: ['Sem owner técnico', 'Sem documentação', 'Sem runbook'], findingCount: 3 },
    { serviceId: 'svc-notifications-worker', serviceName: 'notifications-worker', displayName: 'Notifications Worker', teamName: 'Platform', domain: 'Platform', criticality: 'Medium', lifecycleStatus: 'Active', severity: 'Medium', findings: ['Sem documentação', 'Sem runbook'], findingCount: 2 },
  ],
  auditedAt: new Date().toISOString(),
};

// ── Discovery ───────────────────────────────────────────────────────
export const stubDiscoveredServices: DiscoveredServicesResponse = {
  items: [
    { id: 'disc-1', serviceName: 'fraud-scoring', serviceNamespace: 'billing', environment: 'production', firstSeenAt: '2026-06-20T08:00:00.000Z', lastSeenAt: '2026-07-09T20:00:00.000Z', traceCount: 12840, endpointCount: 4, status: 'Pending', matchedServiceAssetId: null, ignoreReason: null },
    { id: 'disc-2', serviceName: 'cart-service', serviceNamespace: 'commerce', environment: 'production', firstSeenAt: '2026-06-18T10:00:00.000Z', lastSeenAt: '2026-07-09T19:30:00.000Z', traceCount: 45210, endpointCount: 7, status: 'Pending', matchedServiceAssetId: null, ignoreReason: null },
    { id: 'disc-3', serviceName: 'payments-api', serviceNamespace: 'billing', environment: 'production', firstSeenAt: '2026-05-01T00:00:00.000Z', lastSeenAt: '2026-07-09T21:00:00.000Z', traceCount: 98230, endpointCount: 6, status: 'Matched', matchedServiceAssetId: 'svc-payments-api', ignoreReason: null },
    { id: 'disc-4', serviceName: 'debug-proxy', serviceNamespace: 'infra', environment: 'staging', firstSeenAt: '2026-06-25T00:00:00.000Z', lastSeenAt: '2026-06-28T00:00:00.000Z', traceCount: 120, endpointCount: 1, status: 'Ignored', matchedServiceAssetId: null, ignoreReason: 'Ferramenta interna de debug' },
  ],
  totalCount: 4,
};

export const stubDiscoveryDashboard: DiscoveryDashboardResponse = {
  totalDiscovered: 4,
  pending: 2,
  matched: 1,
  registered: 0,
  ignored: 1,
  newThisWeek: 2,
  recentRuns: [
    { runId: 'run-1', startedAt: '2026-07-09T06:00:00.000Z', completedAt: '2026-07-09T06:04:00.000Z', source: 'OpenTelemetry', environment: 'production', servicesFound: 18, newServicesFound: 2, status: 'Completed' },
    { runId: 'run-2', startedAt: '2026-07-08T06:00:00.000Z', completedAt: '2026-07-08T06:03:00.000Z', source: 'OpenTelemetry', environment: 'production', servicesFound: 16, newServicesFound: 0, status: 'Completed' },
  ],
};

// ── Grafo de dependências ───────────────────────────────────────────
export const stubGraph: AssetGraph = {
  services: stubServices.map((s) => ({
    serviceAssetId: s.serviceId,
    name: s.displayName,
    domain: s.domain,
    teamName: s.teamName,
    serviceType: s.serviceType,
    criticality: s.criticality,
    lifecycleStatus: s.lifecycleStatus,
  })),
  apis: [
    {
      apiAssetId: 'api-payments-v2', name: 'Payments API v2', routePattern: '/api/v2/payments', version: '2.3.0', visibility: 'Public', ownerServiceAssetId: 'svc-payments-api',
      consumers: [
        { relationshipId: 'r1', consumerName: 'Orders API', sourceType: 'Runtime', confidenceScore: 0.98, lastObservedAt: '2026-07-09T21:00:00.000Z' },
        { relationshipId: 'r2', consumerName: 'Inventory GraphQL', sourceType: 'Contract', confidenceScore: 0.9, lastObservedAt: '2026-07-08T12:00:00.000Z' },
      ],
    },
    {
      apiAssetId: 'api-orders-v1', name: 'Orders API', routePattern: '/api/v1/orders', version: '1.4.2', visibility: 'Internal', ownerServiceAssetId: 'svc-orders-api',
      consumers: [
        { relationshipId: 'r3', consumerName: 'Notifications Worker', sourceType: 'Runtime', confidenceScore: 0.85, lastObservedAt: '2026-07-09T20:00:00.000Z' },
      ],
    },
  ],
};
