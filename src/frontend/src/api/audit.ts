import client from './client';
import type { AuditEvent, PagedList } from '../types';

export const auditApi = {
  listEvents: (params?: {
    page?: number;
    pageSize?: number;
    eventType?: string;
    actorEmail?: string;
    from?: string;
    to?: string;
  }) =>
    client
      .get<PagedList<AuditEvent>>('/audit/events', { params })
      .then((r) => r.data),

  getEvent: (id: string) =>
    client.get<AuditEvent>(`/audit/events/${id}`).then((r) => r.data),

  verifyIntegrity: () =>
    client.post<{ valid: boolean; message: string }>('/audit/verify').then((r) => r.data),

  exportReport: (from: string, to: string) =>
    client
      .get('/audit/export', { params: { from, to }, responseType: 'blob' })
      .then((r) => r.data),
};
