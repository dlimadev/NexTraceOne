import client from '../../../api/client';
import type {
  ContractDraft,
  ContractReviewEntry,
  DraftListResponse,
  ContractType,
  ContractProtocol,
  DraftStatus,
} from '../types';

/**
 * API client para o Contract Studio Enterprise.
 * Endpoints de draft CRUD, review, publicação e geração assistida por IA.
 */
export const contractStudioApi = {
  createDraft: (data: {
    title: string;
    author: string;
    contractType: ContractType;
    protocol: ContractProtocol;
    serviceId?: string;
    description?: string;
  }) =>
    client.post<{ draftId: string; title: string; status: string; createdAt: string }>('/contracts/drafts', data).then(r => r.data),

  getDraft: (draftId: string) =>
    client.get<ContractDraft>(`/contracts/drafts/${draftId}`).then(r => r.data),

  listDrafts: (params?: {
    status?: DraftStatus;
    serviceId?: string;
    author?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<DraftListResponse>('/contracts/drafts', { params }).then(r => r.data),

  updateContent: (draftId: string, data: {
    specContent: string;
    format: string;
    editedBy: string;
  }) =>
    client.patch(`/contracts/drafts/${draftId}/content`, data).then(r => r.data),

  updateMetadata: (draftId: string, data: {
    title?: string;
    description?: string;
    proposedVersion?: string;
    serviceId?: string;
    editedBy: string;
  }) =>
    client.patch(`/contracts/drafts/${draftId}/metadata`, data).then(r => r.data),

  submitForReview: (draftId: string) =>
    client.post(`/contracts/drafts/${draftId}/submit-review`).then(r => r.data),

  approve: (draftId: string, data: { approvedBy: string; comment?: string }) =>
    client.post(`/contracts/drafts/${draftId}/approve`, data).then(r => r.data),

  reject: (draftId: string, data: { rejectedBy: string; comment?: string }) =>
    client.post(`/contracts/drafts/${draftId}/reject`, data).then(r => r.data),

  publish: (draftId: string, data: { publishedBy: string }) =>
    client.post<{ contractVersionId: string }>(`/contracts/drafts/${draftId}/publish`, data).then(r => r.data),

  generateFromAi: (data: {
    title: string;
    author: string;
    contractType: ContractType;
    protocol: ContractProtocol;
    prompt: string;
    serviceId?: string;
  }) =>
    client.post<{ draftId: string; title: string; generatedContentPreview: string; createdAt: string }>('/contracts/drafts/generate', data).then(r => r.data),

  addExample: (draftId: string, data: {
    name: string;
    content: string;
    contentFormat: string;
    exampleType: string;
    createdBy: string;
    description?: string;
  }) =>
    client.post<{ exampleId: string }>(`/contracts/drafts/${draftId}/examples`, data).then(r => r.data),

  listReviews: (draftId: string) =>
    client.get<ContractReviewEntry[]>(`/contracts/drafts/${draftId}/reviews`).then(r => r.data),
};
