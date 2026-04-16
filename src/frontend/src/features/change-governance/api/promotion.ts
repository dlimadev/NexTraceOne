import client from '../../../api/client';
import type { PromotionRequest, PagedList } from '../../../types';

// ─── Gate Evaluation Types ────────────────────────────────────────────────────

export interface GateEvaluationItem {
  evaluationId: string;
  gateId: string;
  passed: boolean;
  evaluatedBy: string;
  details: string | null;
  overrideJustification: string | null;
  evaluatedAt: string;
}

export interface GateEvaluationsResponse {
  promotionRequestId: string;
  evaluations: GateEvaluationItem[];
}

export interface PromotionStatus {
  promotionRequestId: string;
  status: string;
  reviewedAt: string | null;
  reviewedBy: string | null;
  notes: string | null;
}

/** Step no caminho de promoção de uma release entre ambientes (Gap 10). */
export interface PromotionPathStep {
  promotionRequestId: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  status: string;
  requestedBy: string;
  requestedAt: string;
  completedAt: string | null;
  justification: string | null;
}

/** Resposta do caminho de promoção de uma release (Gap 10). */
export interface EnvironmentPromotionPathResponse {
  releaseId: string;
  steps: PromotionPathStep[];
  currentEnvironment: string | null;
  isFullyPromoted: boolean;
  hasBlockers: boolean;
  totalSteps: number;
  completedSteps: number;
}

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

  /** Obtém avaliações detalhadas de gates para um pedido de promoção. */
  getGateEvaluations: (requestId: string) =>
    client
      .get<GateEvaluationsResponse>(`/promotion/requests/${requestId}/gate-evaluations`)
      .then((r) => r.data),

  /** Override de um gate com justificativa documentada. */
  overrideGate: (gateEvaluationId: string, justification: string) =>
    client
      .post(`/promotion/gate-evaluations/${gateEvaluationId}/override`, {
        gateEvaluationId,
        justification,
      })
      .then((r) => r.data),

  /** Obtém status detalhado de um pedido de promoção. */
  getPromotionStatus: (requestId: string) =>
    client
      .get<PromotionStatus>(`/promotion/requests/${requestId}/status`)
      .then((r) => r.data),

  /** Avalia gates de promoção de forma síncrona. */
  evaluateGates: (requestId: string) =>
    client
      .post(`/promotion/requests/${requestId}/evaluate-gates`, { promotionRequestId: requestId })
      .then((r) => r.data),

  /** Obtém o caminho de promoção de uma release entre ambientes (Gap 10). */
  getEnvironmentPromotionPath: (releaseId: string) =>
    client
      .get<EnvironmentPromotionPathResponse>(`/promotion/releases/${releaseId}/path`)
      .then((r) => r.data),
};
