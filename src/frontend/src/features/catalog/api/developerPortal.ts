/**
 * Cliente API do Developer Portal — catálogo de APIs, subscriptions,
 * playground, geração de código e analytics.
 *
 * Segue o padrão dos demais módulos (promotion.ts).
 * Todo acesso HTTP passa pelo client centralizado com interceptors de
 * autenticação e refresh token.
 */
import client from '../../../api/client';
import type { PagedList } from '../../../types';

interface CatalogItem {
  apiAssetId: string;
  name: string;
  apiName?: string;
  displayName?: string;
  description?: string;
  version?: string;
  healthStatus?: string;
  ownerServiceName?: string;
}

interface ApiDetail {
  apiAssetId: string;
  name: string;
  description?: string;
  version?: string;
}

interface ApiHealthInfo {
  status: string;
  score?: number;
  lastCheckedAt?: string;
}

interface TimelineEntry {
  id: string;
  title: string;
  description?: string;
  occurredAt: string;
}

interface ApiConsumer {
  consumerId: string;
  consumerName: string;
  consumerType?: string;
}

type SubscriptionLevel = 'BreakingChangesOnly' | 'AllChanges' | 'DeprecationNotices' | 'SecurityAdvisories';
type NotificationChannel = 'Email' | 'Webhook' | 'Teams' | 'Slack';
type GenerationType = 'ClientSdk' | 'ServerStub' | 'Example';

interface Subscription {
  id: string;
  apiAssetId: string;
  apiName: string;
  subscriberEmail: string;
  consumerServiceName: string;
  level: SubscriptionLevel;
  channel: NotificationChannel;
  isActive?: boolean;
}

interface PlaygroundResult {
  statusCode: number;
  responseStatusCode?: number;
  responseBody: string;
  durationMs: number;
  executedAt?: string;
}

interface PlaygroundHistoryItem {
  id: string;
  sessionId?: string;
  apiAssetId: string;
  apiName?: string;
  httpMethod?: string;
  requestPath: string;
  executedAt: string;
  statusCode: number;
  responseStatusCode?: number;
  durationMs?: number;
}

interface CodeGenerationResult {
  content: string;
  fileName: string;
  language: string;
}

interface PortalAnalytics {
  totalExecutions: number;
  totalSubscriptions: number;
  totalSearches?: number;
  totalApiViews?: number;
  totalPlaygroundExecutions?: number;
  totalCodeGenerations?: number;
  popularApis: Array<{ apiAssetId: string; count: number }>;
  topSearches?: Array<{ term: string; count: number }>;
}

export const developerPortalApi = {
  // ── Catálogo ────────────────────────────────────────────────────────────────

  searchCatalog: (query: string, page = 1, pageSize = 20) =>
    client
      .get<PagedList<CatalogItem>>('/developerportal/catalog/search', {
        params: { query, page, pageSize },
      })
      .then((r) => r.data),

  getMyApis: (page = 1, pageSize = 20) =>
    client
      .get<PagedList<CatalogItem>>('/developerportal/catalog/my-apis', {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getConsuming: (page = 1, pageSize = 20) =>
    client
      .get<PagedList<CatalogItem>>('/developerportal/catalog/consuming', {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  getApiDetail: (apiAssetId: string) =>
    client.get<ApiDetail>(`/developerportal/catalog/${apiAssetId}`).then((r) => r.data),

  getApiHealth: (apiAssetId: string) =>
    client.get<ApiHealthInfo>(`/developerportal/catalog/${apiAssetId}/health`).then((r) => r.data),

  getApiTimeline: (apiAssetId: string) =>
    client
      .get<TimelineEntry[]>(`/developerportal/catalog/${apiAssetId}/timeline`)
      .then((r) => r.data),

  getApiConsumers: (apiAssetId: string) =>
    client
      .get<ApiConsumer[]>(`/developerportal/catalog/${apiAssetId}/consumers`)
      .then((r) => r.data),

  getApiContract: (apiAssetId: string) =>
    client.get<string>(`/developerportal/catalog/${apiAssetId}/contract`).then((r) => r.data),

  // ── Subscriptions ───────────────────────────────────────────────────────────

  createSubscription: (data: {
    apiAssetId: string;
    apiName: string;
    subscriberEmail: string;
    consumerServiceName: string;
    consumerServiceVersion: string;
    level: SubscriptionLevel;
    channel: NotificationChannel;
    webhookUrl?: string;
  }) =>
    client.post<{ id: string }>('/developerportal/subscriptions', data).then((r) => r.data),

  listSubscriptions: () =>
    client.get<Subscription[]>('/developerportal/subscriptions').then((r) => r.data),

  deleteSubscription: (subscriptionId: string) =>
    client.delete(`/developerportal/subscriptions/${subscriptionId}`).then((r) => r.data),

  // ── Playground ──────────────────────────────────────────────────────────────

  executePlayground: (data: {
    apiAssetId: string;
    apiName: string;
    httpMethod: string;
    requestPath: string;
    requestBody?: string;
    requestHeaders?: string;
    environment?: string;
  }) =>
    client.post<PlaygroundResult>('/developerportal/playground/execute', data).then((r) => r.data),

  getPlaygroundHistory: (page = 1, pageSize = 20) =>
    client
      .get<PagedList<PlaygroundHistoryItem>>('/developerportal/playground/history', {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  // ── Code Generation ─────────────────────────────────────────────────────────

  generateCode: (data: {
    apiAssetId: string;
    apiName: string;
    contractVersion: string;
    language: string;
    generationType: GenerationType;
  }) =>
    client
      .post<CodeGenerationResult>('/developerportal/codegen', data)
      .then((r) => r.data),

  // ── Analytics ───────────────────────────────────────────────────────────────

  trackEvent: (data: {
    eventType: string;
    entityId?: string;
    entityType?: string;
    searchQuery?: string;
    metadata?: Record<string, unknown>;
  }) =>
    client.post('/developerportal/analytics/events', data).then((r) => r.data),

  getAnalytics: (since: string) =>
    client
      .get<PortalAnalytics>('/developerportal/analytics', { params: { since } })
      .then((r) => r.data),
};
