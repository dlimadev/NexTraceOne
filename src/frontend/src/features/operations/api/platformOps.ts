import client from '../../../api/client';
import type {
  PlatformHealthResponse,
  PlatformJobsResponse,
  PlatformQueuesResponse,
  PlatformEventsResponse,
  PlatformConfigResponse,
} from '../../../types';

/** Cliente de API para Platform Operations — health, jobs, filas, eventos e configuração. */
export const platformOpsApi = {
  getHealth: () =>
    client.get<PlatformHealthResponse>('/platform/health').then((r) => r.data),

  getJobs: (params?: { status?: string; page?: number; pageSize?: number }) =>
    client.get<PlatformJobsResponse>('/platform/jobs', { params }).then((r) => r.data),

  getQueues: () =>
    client.get<PlatformQueuesResponse>('/platform/queues').then((r) => r.data),

  getEvents: (params?: { severity?: string; subsystem?: string; page?: number; pageSize?: number }) =>
    client.get<PlatformEventsResponse>('/platform/events', { params }).then((r) => r.data),

  getConfig: () =>
    client.get<PlatformConfigResponse>('/platform/config').then((r) => r.data),
};
