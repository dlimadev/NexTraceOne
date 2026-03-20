import client from '../../../api/client';
import type {
  FinOpsSummaryResponse,
  ServiceFinOpsResponse,
  TeamFinOpsResponse,
  DomainFinOpsResponse,
  FinOpsTrendsResponse,
} from '../../../types';

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
};
