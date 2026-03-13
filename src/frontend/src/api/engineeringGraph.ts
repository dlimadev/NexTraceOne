import client from './client';
import type {
  AssetGraph,
  ImpactPropagationResult,
  SubgraphResult,
  GraphSnapshotSummary,
  TemporalDiffResult,
  NodeHealthResult,
} from '../types';

/** Cliente de API para o módulo Engineering Graph. */
export const engineeringGraphApi = {
  /** Obtém o grafo completo de ativos e relacionamentos. */
  getGraph: () =>
    client.get<AssetGraph>('/engineeringgraph/graph').then((r) => r.data),

  /** Obtém detalhe de um ativo de API específico. */
  getApiAsset: (id: string) =>
    client.get(`/engineeringgraph/apis/${id}`).then((r) => r.data),

  /** Pesquisa ativos de API por termo. */
  searchApis: (searchTerm: string) =>
    client
      .get('/engineeringgraph/apis/search', { params: { searchTerm } })
      .then((r) => r.data),

  /** Registra um novo serviço no catálogo. */
  registerService: (data: { name: string; team: string; description?: string }) =>
    client.post<{ id: string }>('/engineeringgraph/services', data).then((r) => r.data),

  /** Registra um novo ativo de API. */
  registerApi: (data: {
    name: string;
    baseUrl: string;
    ownerServiceId: string;
    description?: string;
  }) => client.post<{ id: string }>('/engineeringgraph/apis', data).then((r) => r.data),

  /** Mapeia uma relação de consumo para uma API. */
  mapConsumer: (
    apiAssetId: string,
    data: { consumerServiceId: string; trustLevel: string }
  ) =>
    client
      .post(`/engineeringgraph/apis/${apiAssetId}/consumers`, data)
      .then((r) => r.data),

  /** Calcula a propagação de impacto (blast radius) a partir de um nó raiz. */
  getImpactPropagation: (rootNodeId: string, maxDepth = 3) =>
    client
      .get<ImpactPropagationResult>(`/engineeringgraph/impact/${rootNodeId}`, {
        params: { maxDepth },
      })
      .then((r) => r.data),

  /** Obtém um subgrafo contextual centrado em um nó. */
  getSubgraph: (rootNodeId: string, maxDepth = 2, maxNodes = 50) =>
    client
      .get<SubgraphResult>(`/engineeringgraph/subgraph/${rootNodeId}`, {
        params: { maxDepth, maxNodes },
      })
      .then((r) => r.data),

  /** Lista snapshots temporais do grafo. */
  listSnapshots: (limit = 50) =>
    client
      .get<{ items: GraphSnapshotSummary[] }>('/engineeringgraph/snapshots', {
        params: { limit },
      })
      .then((r) => r.data),

  /** Cria um snapshot temporal do estado atual do grafo. */
  createSnapshot: (label: string) =>
    client
      .post<GraphSnapshotSummary>('/engineeringgraph/snapshots', { label })
      .then((r) => r.data),

  /** Calcula o diff entre dois snapshots temporais. */
  getTemporalDiff: (fromSnapshotId: string, toSnapshotId: string) =>
    client
      .get<TemporalDiffResult>('/engineeringgraph/snapshots/diff', {
        params: { fromSnapshotId, toSnapshotId },
      })
      .then((r) => r.data),

  /** Obtém dados de saúde/overlay para os nós do grafo. */
  getNodeHealth: (overlayMode: string) =>
    client
      .get<NodeHealthResult>('/engineeringgraph/health', {
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
      .post('/engineeringgraph/integration/v1/consumers/sync', data)
      .then((r) => r.data),
};
