import client from '../../../api/client';
import type {
  ModuleAdoptionResponse,
  ProductAnalyticsSummaryResponse,
  PersonaUsageResponse,
  JourneysResponse,
  ValueMilestonesResponse,
  FrictionIndicatorsResponse,
  AdoptionFunnelResponse,
  FeatureHeatmapResponse,
} from '../../../types';

// ─── Cohort Analysis types ────────────────────────────────────────────────────

export interface CohortPeriodEntry {
  period: number;
  count: number;
  rate: number;
}

export interface CohortRow {
  cohortLabel: string;
  cohortStart: string;
  totalUsers: number;
  periods: CohortPeriodEntry[];
}

export interface CohortAnalysisResponse {
  granularity: string;
  metric: string;
  cohorts: CohortRow[];
  maxPeriods: number;
}

// ─── Journey Definition types ─────────────────────────────────────────────────

export interface JourneyStepDefinition {
  stepId: string;
  stepName: string;
  eventType: string;
  order: number;
}

export interface JourneyDefinitionDto {
  id: string;
  tenantId: string | null;
  name: string;
  key: string;
  steps: JourneyStepDefinition[];
  isActive: boolean;
  isGlobal: boolean;
  createdAt: string;
}

export interface CreateJourneyDefinitionRequest {
  name: string;
  key: string;
  steps: JourneyStepDefinition[];
  isActive: boolean;
}

export interface UpdateJourneyDefinitionRequest {
  name: string;
  steps: JourneyStepDefinition[];
  isActive: boolean;
}

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

  getPersonaUsage: (params?: { persona?: string; teamId?: string; range?: string }) =>
    client.get<PersonaUsageResponse>('/product-analytics/adoption/personas', { params }).then((r) => r.data),

  getJourneys: (params?: { journeyId?: string; persona?: string; range?: string }) =>
    client.get<JourneysResponse>('/product-analytics/journeys', { params }).then((r) => r.data),

  getValueMilestones: (params?: { persona?: string; teamId?: string; range?: string }) =>
    client.get<ValueMilestonesResponse>('/product-analytics/value-milestones', { params }).then((r) => r.data),

  getFriction: (params?: { persona?: string; module?: string; range?: string }) =>
    client.get<FrictionIndicatorsResponse>('/product-analytics/friction', { params }).then((r) => r.data),

  getAdoptionFunnel: (params?: { module?: string; persona?: string; teamId?: string; range?: string }) =>
    client.get<AdoptionFunnelResponse>('/product-analytics/adoption/funnel', { params }).then((r) => r.data),

  getFeatureHeatmap: (params?: { persona?: string; teamId?: string; range?: string }) =>
    client.get<FeatureHeatmapResponse>('/product-analytics/heatmap', { params }).then((r) => r.data),

  // ─── FEAT-05: Cohort Analysis ───────────────────────────────────────────────

  getCohortAnalysis: (params?: {
    granularity?: 'week' | 'month';
    periods?: number;
    metric?: 'retention' | 'activation';
    range?: string;
  }) =>
    client.get<CohortAnalysisResponse>('/product-analytics/cohorts', { params }).then((r) => r.data),

  // ─── FEAT-03: Journey Definition CRUD ──────────────────────────────────────

  listJourneyDefinitions: () =>
    client.get<JourneyDefinitionDto[]>('/product-analytics/config/journeys').then((r) => r.data),

  createJourneyDefinition: (data: CreateJourneyDefinitionRequest) =>
    client.post<JourneyDefinitionDto>('/product-analytics/config/journeys', data).then((r) => r.data),

  updateJourneyDefinition: (id: string, data: UpdateJourneyDefinitionRequest) =>
    client.put<JourneyDefinitionDto>(`/product-analytics/config/journeys/${id}`, data).then((r) => r.data),

  deleteJourneyDefinition: (id: string) =>
    client.delete(`/product-analytics/config/journeys/${id}`).then((r) => r.data),

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
