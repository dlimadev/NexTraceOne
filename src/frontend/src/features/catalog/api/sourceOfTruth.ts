import client from '../../../api/client';
import type {
  ServiceSourceOfTruth,
  ContractSourceOfTruth,
  ServiceCoverageResponse,
  SourceOfTruthSearchResponse,
} from '../../../types';

/** Cliente de API para o módulo Source of Truth do NexTraceOne. */
export const sourceOfTruthApi = {
  /** Obtém a visão consolidada de Source of Truth de um serviço. */
  getServiceSot: (serviceId: string) =>
    client.get<ServiceSourceOfTruth>(`/source-of-truth/services/${serviceId}`).then((r) => r.data),

  /** Obtém a visão consolidada de Source of Truth de um contrato. */
  getContractSot: (contractVersionId: string) =>
    client.get<ContractSourceOfTruth>(`/source-of-truth/contracts/${contractVersionId}`).then((r) => r.data),

  /** Obtém indicadores de cobertura de um serviço. */
  getServiceCoverage: (serviceId: string) =>
    client.get<ServiceCoverageResponse>(`/source-of-truth/services/${serviceId}/coverage`).then((r) => r.data),

  /** Pesquisa unificada de descoberta no Source of Truth. */
  search: (params: { q: string; scope?: string; maxResults?: number }) =>
    client.get<SourceOfTruthSearchResponse>('/source-of-truth/search', { params }).then((r) => r.data),
};
