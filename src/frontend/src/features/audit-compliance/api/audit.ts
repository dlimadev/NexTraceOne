import client from '../../../api/client';
import type { AuditEvent, PagedList } from '../../../types';

type AuditSearchResponse = {
  items: Array<{
    eventId: string;
    sourceModule: string;
    actionType: string;
    resourceType: string;
    resourceId: string;
    performedBy: string;
    occurredAt: string;
  }>;
};

type VerifyChainResponse = {
  isIntact: boolean;
  totalLinks: number;
  violations: Array<{
    sequenceNumber: number;
    reason: string;
  }>;
};

function toPagedAuditEvents(response: AuditSearchResponse, page: number, pageSize: number): PagedList<AuditEvent> {
  const items: AuditEvent[] = response.items.map((item) => ({
    eventId: item.eventId,
    id: item.eventId,
    eventType: item.actionType,
    actor: item.performedBy,
    action: item.actionType,
    aggregateType: item.resourceType,
    entityType: item.resourceType,
    entityId: item.resourceId,
    actorEmail: item.performedBy,
    hash: item.eventId,
    occurredAt: item.occurredAt,
  }));

  return {
    items,
    totalCount: items.length,
    page,
    pageSize,
    totalPages: items.length > 0 ? 1 : 0,
  };
}

export const auditApi = {
  listEvents: (params?: {
    page?: number;
    pageSize?: number;
    eventType?: string;
    actorEmail?: string;
    from?: string;
    to?: string;
  }) => {
    const page = params?.page ?? 1;
    const pageSize = params?.pageSize ?? 20;

    return client
      .get<AuditSearchResponse>('/audit/search', {
        params: {
          page,
          pageSize,
          actionType: params?.eventType,
          from: params?.from,
          to: params?.to,
        },
      })
      .then((r) => toPagedAuditEvents(r.data, page, pageSize));
  },

  verifyIntegrity: () =>
    client
      .get<VerifyChainResponse>('/audit/verify-chain')
      .then((r) => ({
        valid: r.data.isIntact,
        message: r.data.isIntact
          ? `Hash chain is valid. All ${r.data.totalLinks} events verified.`
          : `Integrity violation detected. ${r.data.violations.length} issue(s) found.`,
      })),

  exportReport: (from: string, to: string) =>
    client
      .get('/audit/report', { params: { from, to }, responseType: 'blob' })
      .then((r) => r.data),
};
