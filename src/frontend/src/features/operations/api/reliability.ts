import client from '../../../api/client';
import type {
  ServiceReliabilityListResponse,
  ServiceReliabilityDetailResponse,
  TeamReliabilitySummaryResponse,
} from '../../../types';

/** Cliente de API para Service Reliability. */
export const reliabilityApi = {
  listServices: (params?: {
    teamId?: string;
    serviceId?: string;
    domain?: string;
    environment?: string;
    status?: string;
    serviceType?: string;
    criticality?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<ServiceReliabilityListResponse>('/reliability/services', { params }).then((r) => r.data),

  getServiceDetail: (serviceId: string) =>
    client.get<ServiceReliabilityDetailResponse>(`/reliability/services/${serviceId}`).then((r) => r.data),

  getTeamSummary: (teamId: string) =>
    client.get<TeamReliabilitySummaryResponse>(`/reliability/teams/${teamId}/summary`).then((r) => r.data),

  listServiceSlos: (serviceId: string) =>
    client.get<ServiceSlosResponse>(`/reliability/services/${serviceId}/slos`).then((r) => r.data),

  registerSlo: (data: RegisterSloRequest) =>
    client.post<RegisterSloResponse>('/reliability/slos', data).then((r) => r.data),

  getErrorBudget: (sloId: string) =>
    client.get<SloErrorBudgetResponse>(`/reliability/slos/${sloId}/error-budget`).then((r) => r.data),

  getBurnRate: (sloId: string, window: BurnRateWindow = 'SixHours') =>
    client.get<SloBurnRateResponse>(`/reliability/slos/${sloId}/burn-rate`, { params: { window } }).then((r) => r.data),

  computeErrorBudget: (sloId: string) =>
    client.post<SloErrorBudgetResponse>(`/reliability/slos/${sloId}/compute-error-budget`).then((r) => r.data),

  computeBurnRate: (sloId: string, window?: BurnRateWindow) =>
    client.post<SloComputeBurnRateResponse>(`/reliability/slos/${sloId}/compute-burn-rate`, undefined, { params: { window } }).then((r) => r.data),

  listSloSlas: (sloId: string) =>
    client.get<SloSlasResponse>(`/reliability/slos/${sloId}/slas`).then((r) => r.data),
};

export type SloType = 'Availability' | 'Latency' | 'ErrorRate' | 'Throughput';
export type SloStatus = 'Healthy' | 'AtRisk' | 'Violated';
export type BurnRateWindow = 'OneHour' | 'SixHours' | 'TwentyFourHours' | 'SevenDays';

export interface RegisterSloRequest {
  serviceId: string;
  environment: string;
  name: string;
  type: SloType;
  targetPercent: number;
  windowDays: number;
  description?: string;
  alertThresholdPercent?: number;
}

export interface RegisterSloResponse {
  id: string;
  name: string;
  serviceId: string;
  environment: string;
  type: SloType;
  targetPercent: number;
  windowDays: number;
}

export interface ServiceSloItem {
  id: string;
  name: string;
  serviceId: string;
  environment: string;
  type: SloType;
  targetPercent: number;
  alertThresholdPercent: number | null;
  windowDays: number;
  isActive: boolean;
}

export interface ServiceSlosResponse {
  serviceId: string;
  items: ServiceSloItem[];
}

export interface SloErrorBudgetResponse {
  sloDefinitionId: string;
  sloName: string;
  serviceId: string;
  environment: string;
  targetPercent: number;
  windowDays: number;
  totalBudgetMinutes: number | null;
  consumedBudgetMinutes: number | null;
  remainingBudgetMinutes: number | null;
  consumedPercent: number | null;
  status: SloStatus;
  computedAt: string | null;
}

export interface SloBurnRateResponse {
  sloDefinitionId: string;
  sloName: string;
  serviceId: string;
  environment: string;
  window: BurnRateWindow;
  burnRate: number | null;
  observedErrorRate: number | null;
  toleratedErrorRate: number | null;
  status: SloStatus;
  computedAt: string | null;
}

export interface SloComputeBurnRateSnapshot {
  window: BurnRateWindow;
  burnRate: number;
  status: SloStatus;
}

export interface SloComputeBurnRateResponse {
  sloDefinitionId: string;
  sloName: string;
  serviceId: string;
  environment: string;
  observedErrorRate: number;
  toleratedErrorRate: number;
  snapshots: SloComputeBurnRateSnapshot[];
  computedAt: string;
}

export interface SloSlaItem {
  id: string;
  name: string;
  contractualTargetPercent: number;
  status: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  hasPenaltyClauses: boolean;
  isActive: boolean;
}

export interface SloSlasResponse {
  sloDefinitionId: string;
  sloName: string;
  items: SloSlaItem[];
}
