import client from '../../../api/client';
import type { PromotionRequest, PagedList } from '../../../types';

export const promotionApi = {
  listRequests: (page = 1, pageSize = 20) =>
    client
      .get<PagedList<PromotionRequest>>('/promotion/requests', { params: { page, pageSize } })
      .then((r) => r.data),

  getRequest: (id: string) =>
    client.get<PromotionRequest>(`/promotion/requests/${id}`).then((r) => r.data),

  createRequest: (data: {
    releaseId: string;
    sourceEnvironment: string;
    targetEnvironment: string;
  }) =>
    client.post<{ id: string }>('/promotion/requests', data).then((r) => r.data),

  runGates: (requestId: string) =>
    client.post(`/promotion/requests/${requestId}/gates`).then((r) => r.data),

  promote: (requestId: string, justification?: string) =>
    client
      .post(`/promotion/requests/${requestId}/promote`, { justification })
      .then((r) => r.data),

  reject: (requestId: string, reason: string) =>
    client
      .post(`/promotion/requests/${requestId}/reject`, { reason })
      .then((r) => r.data),
};
