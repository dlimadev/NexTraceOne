import client from '../../../api/client';
import type {
  ChangesListResponse,
  ChangesSummaryResponse,
  ChangeAdvisoryResponse,
  RecordDecisionRequest,
  RecordDecisionResponse,
  DecisionHistoryResponse,
} from '../../../types';

/** Parâmetros de filtro para o catálogo de mudanças. */
export interface ChangesFilterParams {
  serviceName?: string;
  teamName?: string;
  environment?: string;
  changeType?: string;
  confidenceStatus?: string;
  deploymentStatus?: string;
  searchTerm?: string;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
}

export interface ChangeFilterOptionsResponse {
  changeTypes: string[];
  confidenceStatuses: string[];
  deploymentStatuses: string[];
}

/** Cliente de API para o módulo Change Confidence do NexTraceOne. */
export const changeConfidenceApi = {
  /** Lista mudanças com filtros avançados. */
  listChanges: (params: ChangesFilterParams) =>
    client.get<ChangesListResponse>('/changes', { params }).then((r) => r.data),

  /** Obtém resumo agregado de mudanças. */
  getSummary: (params?: { teamName?: string; environment?: string; from?: string; to?: string }) =>
    client.get<ChangesSummaryResponse>('/changes/summary', { params }).then((r) => r.data),

  /** Obtém opções dinâmicas de filtro para o catálogo de mudanças. */
  getFilterOptions: () =>
    client.get<ChangeFilterOptionsResponse>('/changes/filter-options').then((r) => r.data),

  /** Lista mudanças por serviço. */
  listByService: (serviceName: string, page = 1, pageSize = 20) =>
    client
      .get<ChangesListResponse>(`/changes/by-service/${encodeURIComponent(serviceName)}`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  /** Obtém detalhe de uma mudança. */
  getChange: (changeId: string) =>
    client.get(`/changes/${changeId}`).then((r) => r.data),

  /** Obtém blast radius de uma mudança. */
  getBlastRadius: (changeId: string) =>
    client.get(`/changes/${changeId}/blast-radius`).then((r) => r.data),

  /** Obtém intelligence summary de uma mudança. */
  getIntelligence: (changeId: string) =>
    client.get(`/changes/${changeId}/intelligence`).then((r) => r.data),

  /** Obtém advisory/recomendação de confiança de uma mudança. */
  getAdvisory: (changeId: string) =>
    client.get<ChangeAdvisoryResponse>(`/changes/${changeId}/advisory`).then((r) => r.data),

  /** Regista uma decisão de governança sobre uma mudança. */
  recordDecision: (changeId: string, data: RecordDecisionRequest) =>
    client.post<RecordDecisionResponse>(`/changes/${changeId}/decision`, data).then((r) => r.data),

  /** Obtém o histórico de decisões de uma mudança. */
  getDecisionHistory: (changeId: string) =>
    client.get<DecisionHistoryResponse>(`/changes/${changeId}/decisions`).then((r) => r.data),
};
