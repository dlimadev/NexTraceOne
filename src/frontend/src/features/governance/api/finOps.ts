import client from '../../../api/client';
import type {
  FinOpsSummaryResponse,
  ServiceFinOpsResponse,
  TeamFinOpsResponse,
  DomainFinOpsResponse,
  FinOpsTrendsResponse,
  EfficiencyIndicatorsResponse,
} from '../../../types';

export interface WasteSignalDetail {
  signalId: string;
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  type: string;
  description: string;
  pattern: string;
  estimatedWaste: number;
  severity: string;
  detectedAt: string;
  correlatedCause: string | null;
}

export interface WasteByType {
  type: string;
  count: number;
  totalWaste: number;
}

export interface WasteSignalsResponse {
  totalWaste: number;
  signalCount: number;
  signals: WasteSignalDetail[];
  byType: WasteByType[];
  generatedAt: string;
  isSimulated?: boolean;
  dataSource?: string;
}

/** Cliente de API para FinOps contextual do módulo Governance. */
export const finOpsApi = {
  getSummary: (params?: { teamId?: string; domainId?: string; serviceId?: string; range?: string }) =>
    client.get<FinOpsSummaryResponse>('/finops/summary', { params }).then((r) => r.data),

  getServiceFinOps: (serviceId: string) =>
    client.get<ServiceFinOpsResponse>(`/finops/services/${serviceId}`).then((r) => r.data),

  getTeamFinOps: (teamId: string) =>
    client.get<TeamFinOpsResponse>(`/finops/teams/${teamId}`).then((r) => r.data),

  getDomainFinOps: (domainId: string) =>
    client.get<DomainFinOpsResponse>(`/finops/domains/${domainId}`).then((r) => r.data),

  getTrends: (params?: { dimension?: string; filterId?: string }) =>
    client.get<FinOpsTrendsResponse>('/finops/trends', { params }).then((r) => r.data),

  getWasteSignals: (params?: { serviceId?: string; teamId?: string; domainId?: string }) =>
    client.get<WasteSignalsResponse>('/finops/waste', { params }).then((r) => r.data),

  getEfficiency: (params?: { serviceId?: string; teamId?: string }) =>
    client.get<EfficiencyIndicatorsResponse>('/finops/efficiency', { params }).then((r) => r.data),
};
