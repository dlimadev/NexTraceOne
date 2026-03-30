import client from '../../../api/client';
import type {
  AssetGraph,
  ImpactPropagationResult,
  SubgraphResult,
  GraphSnapshotSummary,
  TemporalDiffResult,
  NodeHealthResult,
  ServiceListResponse,
  ServiceDetail,
  ServicesSummary,
} from '../../../types';

/** Categorias de link disponíveis para serviços. */
export type LinkCategory =
  | 'Repository'
  | 'Documentation'
  | 'CiCd'
  | 'Monitoring'
  | 'Wiki'
  | 'SwaggerUi'
  | 'ApiPortal'
  | 'Backstage'
  | 'Adr'
  | 'Runbook'
  | 'Changelog'
  | 'Dashboard'
  | 'Other';

/** Item de link de serviço retornado pela API. */
export interface ServiceLinkItem {
  linkId: string;
  serviceAssetId: string;
  category: LinkCategory;
  title: string;
  url: string;
  description: string;
  iconHint: string;
  sortOrder: number;
  createdAt: string;
}

/** Resposta da listagem de links de um serviço. */
export interface ServiceLinksResponse {
  items: ServiceLinkItem[];
  totalCount: number;
}

/** Payload para criar/atualizar um link de serviço. */
export interface ServiceLinkPayload {
  category: string;
  title: string;
  url: string;
  description?: string;
  iconHint?: string;
  sortOrder?: number;
}

/** Cliente de API para o módulo Service Catalog (Engineering Graph backend). */
export const serviceCatalogApi = {
  /** Obtém o grafo completo de ativos e relacionamentos. */
  getGraph: () =>
    client.get<AssetGraph>('/catalog/graph').then((r) => r.data),

  /** Obtém detalhe de um ativo de API específico. */
  getApiAsset: (id: string) =>
    client.get(`/catalog/apis/${id}`).then((r) => r.data),

  /** Pesquisa ativos de API por termo. */
  searchApis: (searchTerm: string) =>
    client
      .get('/catalog/apis/search', { params: { searchTerm } })
      .then((r) => r.data),

  /** Registra um novo serviço no catálogo. */
  registerService: (data: { name: string; team: string; description?: string }) =>
    client.post<{ id: string }>('/catalog/services', data).then((r) => r.data),

  /** Registra um novo ativo de API. */
  registerApi: (data: {
    name: string;
    baseUrl: string;
    ownerServiceId: string;
    description?: string;
  }) => client.post<{ id: string }>('/catalog/apis', data).then((r) => r.data),

  /** Mapeia uma relação de consumo para uma API. */
  mapConsumer: (
    apiAssetId: string,
    data: { consumerServiceId: string; trustLevel: string }
  ) =>
    client
      .post(`/catalog/apis/${apiAssetId}/consumers`, data)
      .then((r) => r.data),

  /** Calcula a propagação de impacto (blast radius) a partir de um nó raiz. */
  getImpactPropagation: (rootNodeId: string, maxDepth = 3) =>
    client
      .get<ImpactPropagationResult>(`/catalog/impact/${rootNodeId}`, {
        params: { maxDepth },
      })
      .then((r) => r.data),

  /** Obtém um subgrafo contextual centrado em um nó. */
  getSubgraph: (rootNodeId: string, maxDepth = 2, maxNodes = 50) =>
    client
      .get<SubgraphResult>(`/catalog/subgraph/${rootNodeId}`, {
        params: { maxDepth, maxNodes },
      })
      .then((r) => r.data),

  /** Lista snapshots temporais do grafo. */
  listSnapshots: (limit = 50) =>
    client
      .get<{ items: GraphSnapshotSummary[] }>('/catalog/snapshots', {
        params: { limit },
      })
      .then((r) => r.data),

  /** Cria um snapshot temporal do estado atual do grafo. */
  createSnapshot: (label: string) =>
    client
      .post<GraphSnapshotSummary>('/catalog/snapshots', { label })
      .then((r) => r.data),

  /** Calcula o diff entre dois snapshots temporais. */
  getTemporalDiff: (fromSnapshotId: string, toSnapshotId: string) =>
    client
      .get<TemporalDiffResult>('/catalog/snapshots/diff', {
        params: { fromSnapshotId, toSnapshotId },
      })
      .then((r) => r.data),

  /** Obtém dados de saúde/overlay para os nós do grafo. */
  getNodeHealth: (overlayMode: string) =>
    client
      .get<NodeHealthResult>('/catalog/health', {
        params: { overlayMode },
      })
      .then((r) => r.data),

  /** Sincroniza consumidores vindos de integração externa (sistema-a-sistema). */
  syncConsumers: (data: {
    items: Array<{
      apiAssetId: string;
      consumerName: string;
      consumerKind: string;
      consumerEnvironment: string;
      externalReference: string;
      confidenceScore: number;
    }>;
    sourceSystem: string;
    correlationId?: string;
  }) =>
    client
      .post('/catalog/integration/v1/consumers/sync', data)
      .then((r) => r.data),

  /** Lista serviços do catálogo com filtros opcionais. */
  listServices: (params?: {
    teamName?: string;
    domain?: string;
    serviceType?: string;
    criticality?: string;
    lifecycleStatus?: string;
    exposureType?: string;
    search?: string;
  }) =>
    client
      .get<ServiceListResponse>('/catalog/services', { params })
      .then((r) => r.data),

  /** Obtém o detalhe completo de um serviço. */
  getServiceDetail: (serviceId: string) =>
    client
      .get<ServiceDetail>(`/catalog/services/${serviceId}`)
      .then((r) => r.data),

  /** Atualiza detalhes e classificação de um serviço. */
  updateService: (serviceId: string, data: {
    displayName: string;
    description: string;
    serviceType: string;
    systemArea: string;
    criticality: string;
    lifecycleStatus: string;
    exposureType: string;
    documentationUrl: string;
    repositoryUrl: string;
  }) =>
    client.put(`/catalog/services/${serviceId}`, data).then((r) => r.data),

  /** Atualiza o ownership de um serviço. */
  updateOwnership: (serviceId: string, data: {
    teamName: string;
    technicalOwner: string;
    businessOwner: string;
  }) =>
    client.patch(`/catalog/services/${serviceId}/ownership`, data).then((r) => r.data),

  /** Obtém resumos agregados de serviços por equipa ou domínio. */
  getServicesSummary: (params?: { teamName?: string; domain?: string }) =>
    client
      .get<ServicesSummary>('/catalog/services/summary', { params })
      .then((r) => r.data),

  /** Pesquisa serviços do catálogo por termo textual. */
  searchServices: (q: string) =>
    client
      .get('/catalog/services/search', { params: { q } })
      .then((r) => r.data),

  // ── Service Links ──────────────────────────────────────────────────

  /** Lista todos os links de um serviço. */
  listServiceLinks: (serviceId: string) =>
    client
      .get<ServiceLinksResponse>(`/catalog/services/${serviceId}/links`)
      .then((r) => r.data),

  /** Adiciona um link a um serviço. */
  addServiceLink: (serviceId: string, data: ServiceLinkPayload) =>
    client
      .post<ServiceLinkItem>(`/catalog/services/${serviceId}/links`, data)
      .then((r) => r.data),

  /** Atualiza um link de um serviço. */
  updateServiceLink: (serviceId: string, linkId: string, data: ServiceLinkPayload) =>
    client
      .put<ServiceLinkItem>(`/catalog/services/${serviceId}/links/${linkId}`, data)
      .then((r) => r.data),

  /** Remove um link de um serviço. */
  removeServiceLink: (serviceId: string, linkId: string) =>
    client
      .delete(`/catalog/services/${serviceId}/links/${linkId}`)
      .then((r) => r.data),

  // ── Service Discovery ──────────────────────────────────────────────

  /** Executa uma discovery run num ambiente e janela temporal. */
  runServiceDiscovery: (data: { environment: string; from: string; until: string }) =>
    client
      .post<DiscoveryRunResponse>('/catalog/discovery/run', data)
      .then((r) => r.data),

  /** Lista serviços descobertos com filtros. */
  listDiscoveredServices: (params?: { status?: string; environment?: string; search?: string }) =>
    client
      .get<DiscoveredServicesResponse>('/catalog/discovery/services', { params })
      .then((r) => r.data),

  /** Obtém dashboard de discovery com estatísticas. */
  getDiscoveryDashboard: () =>
    client
      .get<DiscoveryDashboardResponse>('/catalog/discovery/dashboard')
      .then((r) => r.data),

  /** Faz match de um serviço descoberto com um ServiceAsset. */
  matchDiscoveredService: (discoveredServiceId: string, data: { serviceAssetId: string }) =>
    client
      .post(`/catalog/discovery/services/${discoveredServiceId}/match`, data)
      .then((r) => r.data),

  /** Regista um novo serviço no catálogo a partir de discovery. */
  registerFromDiscovery: (discoveredServiceId: string, data: { domain: string; teamName: string }) =>
    client
      .post(`/catalog/discovery/services/${discoveredServiceId}/register`, data)
      .then((r) => r.data),

  /** Ignora um serviço descoberto. */
  ignoreDiscoveredService: (discoveredServiceId: string, data: { reason: string }) =>
    client
      .post(`/catalog/discovery/services/${discoveredServiceId}/ignore`, data)
      .then((r) => r.data),

  // ── Service Maturity & Ownership Audit ────────────────────────────

  /** Obtém scorecard de maturidade de um serviço. */
  getServiceMaturity: (serviceId: string) =>
    client
      .get<ServiceMaturityResponse>(`/catalog/services/${serviceId}/maturity`)
      .then((r) => r.data),

  /** Obtém dashboard de maturidade de todos os serviços. */
  getMaturityDashboard: (params?: { teamName?: string; domain?: string }) =>
    client
      .get<MaturityDashboardResponse>('/catalog/maturity/dashboard', { params })
      .then((r) => r.data),

  /** Obtém auditoria de ownership dos serviços. */
  getOwnershipAudit: (params?: { teamName?: string; domain?: string }) =>
    client
      .get<OwnershipAuditResponse>('/catalog/ownership/audit', { params })
      .then((r) => r.data),
};

// ── Discovery Types ────────────────────────────────────────────────

/** Status de um serviço descoberto. */
export type DiscoveryStatus = 'Pending' | 'Matched' | 'Ignored' | 'Registered';

/** Item de serviço descoberto. */
export interface DiscoveredServiceItem {
  id: string;
  serviceName: string;
  serviceNamespace: string;
  environment: string;
  firstSeenAt: string;
  lastSeenAt: string;
  traceCount: number;
  endpointCount: number;
  status: DiscoveryStatus;
  matchedServiceAssetId: string | null;
  ignoreReason: string | null;
}

/** Resposta da listagem de serviços descobertos. */
export interface DiscoveredServicesResponse {
  items: DiscoveredServiceItem[];
  totalCount: number;
}

/** Resposta do dashboard de discovery. */
export interface DiscoveryDashboardResponse {
  totalDiscovered: number;
  pending: number;
  matched: number;
  registered: number;
  ignored: number;
  newThisWeek: number;
  recentRuns: DiscoveryRunSummary[];
}

/** Resumo de uma execução de discovery. */
export interface DiscoveryRunSummary {
  runId: string;
  startedAt: string;
  completedAt: string | null;
  source: string;
  environment: string;
  servicesFound: number;
  newServicesFound: number;
  status: string;
}

/** Resposta de uma execução de discovery. */
export interface DiscoveryRunResponse {
  runId: string;
  totalServicesFound: number;
  newServicesFound: number;
  errorCount: number;
  status: string;
}

// ── Maturity Types ─────────────────────────────────────────────────

/** Dimensão de maturidade de um serviço. */
export interface MaturityDimensionDto {
  dimension: string;
  score: number;
  maxScore: number;
  explanation: string;
}

/** Resposta do scorecard de maturidade de um serviço individual. */
export interface ServiceMaturityResponse {
  serviceId: string;
  serviceName: string;
  displayName: string;
  teamName: string;
  domain: string;
  level: string;
  overallScore: number;
  dimensions: MaturityDimensionDto[];
  computedAt: string;
}

/** Item de maturidade de um serviço no dashboard. */
export interface ServiceMaturityItemDto {
  serviceId: string;
  serviceName: string;
  displayName: string;
  teamName: string;
  domain: string;
  criticality: string;
  lifecycleStatus: string;
  level: string;
  overallScore: number;
  hasOwnership: boolean;
  hasContracts: boolean;
  hasDocumentation: boolean;
  hasRunbook: boolean;
  hasMonitoring: boolean;
  hasRepository: boolean;
  apiCount: number;
  contractCount: number;
  linkCount: number;
}

/** Resumo do dashboard de maturidade. */
export interface MaturityDashboardSummary {
  totalServices: number;
  averageScore: number;
  optimizing: number;
  managed: number;
  defined: number;
  developing: number;
  initial: number;
  withoutOwnership: number;
  withoutContracts: number;
  withoutDocumentation: number;
  withoutRunbooks: number;
  withoutMonitoring: number;
}

/** Resposta do dashboard de maturidade. */
export interface MaturityDashboardResponse {
  summary: MaturityDashboardSummary;
  services: ServiceMaturityItemDto[];
  computedAt: string;
}

// ── Ownership Audit Types ──────────────────────────────────────────

/** Resumo da auditoria de ownership. */
export interface AuditSummaryDto {
  totalServicesAudited: number;
  servicesWithIssues: number;
  healthyServices: number;
  criticalFindings: number;
  highFindings: number;
  mediumFindings: number;
  withoutTeam: number;
  withoutTechnicalOwner: number;
  withoutDocumentation: number;
  withoutRunbook: number;
  apisWithoutContracts: number;
}

/** Finding de auditoria para um serviço. */
export interface AuditFindingDto {
  serviceId: string;
  serviceName: string;
  displayName: string;
  teamName: string;
  domain: string;
  criticality: string;
  lifecycleStatus: string;
  severity: string;
  findings: string[];
  findingCount: number;
}

/** Resposta da auditoria de ownership. */
export interface OwnershipAuditResponse {
  summary: AuditSummaryDto;
  findings: AuditFindingDto[];
  auditedAt: string;
}
