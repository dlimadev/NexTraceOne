import client from '../../../api/client';
import type { ContractDraft, ContractDraftExample, ContractReviewEntry, DraftListResponse, ContractType, ContractProtocol, DraftStatus } from '../../../types';

/**
 * API client para o Contract Studio Enterprise.
 * Endpoints de draft CRUD, review, publicação e geração assistida por IA.
 */
export const contractStudioApi = {
  /** Cria um novo draft de contrato. */
  createDraft: (data: {
    title: string;
    author: string;
    contractType: ContractType;
    protocol: ContractProtocol;
    serviceId?: string;
    description?: string;
  }) =>
    client.post<{ draftId: string; title: string; status: string; createdAt: string }>('/contracts/drafts', data).then(r => r.data),

  /** Obtém um draft pelo ID. */
  getDraft: (draftId: string) =>
    client.get<ContractDraft>(`/contracts/drafts/${draftId}`).then(r => r.data),

  /** Lista drafts com filtros e paginação. */
  listDrafts: (params?: {
    status?: DraftStatus;
    serviceId?: string;
    author?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<DraftListResponse>('/contracts/drafts', { params }).then(r => r.data),

  /** Atualiza o conteúdo do artefato de um draft. */
  updateContent: (draftId: string, data: {
    specContent: string;
    format: string;
    editedBy: string;
  }) =>
    client.patch(`/contracts/drafts/${draftId}/content`, data).then(r => r.data),

  /** Atualiza os metadados de um draft. */
  updateMetadata: (draftId: string, data: {
    title?: string;
    description?: string;
    proposedVersion?: string;
    serviceId?: string;
    editedBy: string;
  }) =>
    client.patch(`/contracts/drafts/${draftId}/metadata`, data).then(r => r.data),

  /** Submete um draft para revisão. */
  submitForReview: (draftId: string) =>
    client.post(`/contracts/drafts/${draftId}/submit-review`).then(r => r.data),

  /** Aprova um draft. */
  approve: (draftId: string, data: { approvedBy: string; comment?: string }) =>
    client.post(`/contracts/drafts/${draftId}/approve`, data).then(r => r.data),

  /** Rejeita um draft. */
  reject: (draftId: string, data: { rejectedBy: string; comment?: string }) =>
    client.post(`/contracts/drafts/${draftId}/reject`, data).then(r => r.data),

  /** Publica um draft como versão oficial. */
  publish: (draftId: string, data: { publishedBy: string }) =>
    client.post<{ contractVersionId: string }>(`/contracts/drafts/${draftId}/publish`, data).then(r => r.data),

  /** Gera um draft assistido por IA. */
  generateFromAi: (data: {
    title: string;
    author: string;
    contractType: ContractType;
    protocol: ContractProtocol;
    prompt: string;
    serviceId?: string;
  }) =>
    client.post<{ draftId: string; title: string; generatedContentPreview: string; createdAt: string }>('/contracts/drafts/generate', data).then(r => r.data),

  /** Adiciona um exemplo a um draft. */
  addExample: (draftId: string, data: {
    name: string;
    content: string;
    contentFormat: string;
    exampleType: string;
    createdBy: string;
    description?: string;
  }) =>
    client.post<{ exampleId: string }>(`/contracts/drafts/${draftId}/examples`, data).then(r => r.data),

  /** Lista revisões de um draft. */
  listReviews: (draftId: string) =>
    client.get<ContractReviewEntry[]>(`/contracts/drafts/${draftId}/reviews`).then(r => r.data),
};
