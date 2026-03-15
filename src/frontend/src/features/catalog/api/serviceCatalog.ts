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
};
