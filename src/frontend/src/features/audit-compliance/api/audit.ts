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
    correlationId?: string | null;
    chainHash?: string | null;
    previousHash?: string | null;
    sequenceNumber?: number | null;
  }>;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

type VerifyChainResponse = {
  isIntact: boolean;
  totalLinks: number;
  violations: Array<{
    sequenceNumber: number;
    reason: string;
  }>;
  isTruncated?: boolean;
  truncatedAtSequence?: number | null;
};

function toPagedAuditEvents(response: AuditSearchResponse): PagedList<AuditEvent> {
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
    hash: item.chainHash ?? item.eventId,
    occurredAt: item.occurredAt,
    correlationId: item.correlationId ?? undefined,
    sourceModule: item.sourceModule,
  }));

  return {
    items,
    totalCount: response.totalCount,
    page: response.page,
    pageSize: response.pageSize,
    totalPages: response.totalPages,
  };
}

export const auditApi = {
  listEvents: (params?: {
    page?: number;
    pageSize?: number;
    eventType?: string;
    actorEmail?: string;
    sourceModule?: string;
    correlationId?: string;
    from?: string;
    to?: string;
    resourceType?: string;
    resourceId?: string;
  }) => {
    const page = params?.page ?? 1;
    const pageSize = params?.pageSize ?? 20;

    return client
      .get<AuditSearchResponse>('/audit/search', {
        params: {
          page,
          pageSize,
          actionType: params?.eventType,
          sourceModule: params?.sourceModule,
          correlationId: params?.correlationId,
          from: params?.from,
          to: params?.to,
          resourceType: params?.resourceType,
          resourceId: params?.resourceId,
        },
      })
      .then((r) => toPagedAuditEvents(r.data));
  },

  verifyIntegrity: () =>
    client
      .get<VerifyChainResponse>('/audit/verify-chain')
      .then((r) => ({
        valid: r.data.isIntact,
        totalLinks: r.data.totalLinks,
        violations: r.data.violations,
        isTruncated: r.data.isTruncated ?? false,
        truncatedAtSequence: r.data.truncatedAtSequence ?? null,
      })),

  exportReport: (from: string, to: string) =>
    client
      .get('/audit/report', { params: { from, to }, responseType: 'blob' })
      .then((r) => r.data),
};
