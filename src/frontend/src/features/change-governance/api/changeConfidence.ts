import client from '../../../api/client';
import type {
  ChangesListResponse,
  ChangesSummaryResponse,
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

/** Cliente de API para o módulo Change Confidence do NexTraceOne. */
export const changeConfidenceApi = {
  /** Lista mudanças com filtros avançados. */
  listChanges: (params: ChangesFilterParams) =>
    client.get<ChangesListResponse>('/changes', { params }).then((r) => r.data),

  /** Obtém resumo agregado de mudanças. */
  getSummary: (params?: { teamName?: string; environment?: string; from?: string; to?: string }) =>
    client.get<ChangesSummaryResponse>('/changes/summary', { params }).then((r) => r.data),

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
};
