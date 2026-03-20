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
};
