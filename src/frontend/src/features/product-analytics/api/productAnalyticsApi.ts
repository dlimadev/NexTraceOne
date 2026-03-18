import client from '../../../api/client';
import type { ModuleAdoptionResponse, ProductAnalyticsSummaryResponse } from '../../../types';

export const productAnalyticsApi = {
  getSummary: (params?: {
    persona?: string;
    module?: string;
    teamId?: string;
    domainId?: string;
    range?: string;
  }) =>
    client.get<ProductAnalyticsSummaryResponse>('/product-analytics/summary', { params }).then((r) => r.data),

  getModuleAdoption: (params?: { persona?: string; teamId?: string; range?: string }) =>
    client.get<ModuleAdoptionResponse>('/product-analytics/adoption/modules', { params }).then((r) => r.data),

  recordEvent: (data: {
    eventType: string;
    module: string;
    route: string;
    feature?: string | null;
    entityType?: string | null;
    outcome?: string | null;
    personaHint?: string | null;
    teamId?: string | null;
    domainId?: string | null;
    sessionCorrelationId?: string | null;
    clientType?: string | null;
    metadataJson?: string | null;
  }) => client.post('/product-analytics/events', data).then((r) => r.data),
};
