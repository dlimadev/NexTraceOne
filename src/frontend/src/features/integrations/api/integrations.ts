import client from '../../../api/client';
import type {
  IntegrationConnectorsListResponse,
  IntegrationFilterOptionsResponse,
  IntegrationConnectorDetailDto,
  IngestionSourcesListResponse,
  IngestionExecutionsListResponse,
  IngestionHealthDto,
  IngestionFreshnessResponse,
} from '../../../types';

export interface WebhookEventTypeDto {
  code: string;
  category: string;
  description: string;
}

export interface WebhookSubscriptionDto {
  subscriptionId: string;
  name: string;
  targetUrl: string;
  eventTypes: string[];
  hasSecret: boolean;
  isActive: boolean;
  eventCount: number;
  createdAt: string;
  lastTriggeredAt: string | null;
}

export interface WebhookSubscriptionsListResponse {
  items: WebhookSubscriptionDto[];
  totalCount: number;
}

export interface WebhookEventTypesResponse {
  eventTypes: WebhookEventTypeDto[];
}

export interface RegisterWebhookSubscriptionRequest {
  tenantId: string;
  name: string;
  targetUrl: string;
  eventTypes: string[];
  secret?: string;
  description?: string;
  isActive?: boolean;
}

/** Cliente de API para Integration Hub & Ingestion. */
export const integrationsApi = {
  listConnectors: (params?: {
    connectorType?: string;
    status?: string;
    environment?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<IntegrationConnectorsListResponse>('/integrations/connectors', { params }).then((r) => r.data),

  getFilterOptions: () =>
    client.get<IntegrationFilterOptionsResponse>('/integrations/filter-options').then((r) => r.data),

  getConnector: (id: string) =>
    client.get<IntegrationConnectorDetailDto>(`/integrations/connectors/${id}`).then((r) => r.data),

  retryConnector: (id: string) =>
    client.post(`/integrations/connectors/${id}/retry`).then((r) => r.data),

  listSources: (params?: {
    connectorId?: string;
    dataDomain?: string;
    trustLevel?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<IngestionSourcesListResponse>('/ingestion/sources', { params }).then((r) => r.data),

  listExecutions: (params?: {
    connectorId?: string;
    sourceId?: string;
    resultFilter?: string;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<IngestionExecutionsListResponse>('/ingestion/executions', { params }).then((r) => r.data),

  reprocessExecution: (id: string) =>
    client.post(`/ingestion/executions/${id}/reprocess`).then((r) => r.data),

  getHealth: (connectorId?: string) =>
    client.get<IngestionHealthDto>('/integrations/health', { params: { connectorId } }).then((r) => r.data),

  getFreshness: (dataDomain?: string) =>
    client.get<IngestionFreshnessResponse>('/ingestion/freshness', { params: { dataDomain } }).then((r) => r.data),

  getWebhookEventTypes: () =>
    client.get<WebhookEventTypesResponse>('/integrations/webhooks/event-types').then((r) => r.data),

  listWebhookSubscriptions: (params?: { tenantId?: string; isActive?: boolean; page?: number; pageSize?: number }) =>
    client.get<WebhookSubscriptionsListResponse>('/integrations/webhooks/subscriptions', { params }).then((r) => r.data),

  registerWebhookSubscription: (data: RegisterWebhookSubscriptionRequest) =>
    client.post<WebhookSubscriptionDto>('/integrations/webhooks/subscriptions', data).then((r) => r.data),
};
