import client from './client';
import type { ContractVersion, SemanticDiff, ContractProtocol } from '../types';

export const contractsApi = {
  importContract: (data: {
    apiAssetId: string;
    content: string;
    version: string;
    protocol?: ContractProtocol;
  }) =>
    client.post<{ id: string }>('/contracts', data).then((r) => r.data),

  createVersion: (data: {
    contractId: string;
    content: string;
    version: string;
  }) =>
    client.post<{ id: string }>('/contracts/versions', data).then((r) => r.data),

  computeDiff: (fromVersionId: string, toVersionId: string) =>
    client
      .post<SemanticDiff>('/contracts/diff', { fromVersionId, toVersionId })
      .then((r) => r.data),

  getClassification: (contractVersionId: string) =>
    client
      .get(`/contracts/${contractVersionId}/classification`)
      .then((r) => r.data),

  suggestVersion: (apiAssetId: string, changeLevel: number) =>
    client
      .get<{ suggestedVersion: string }>('/contracts/suggest-version', {
        params: { apiAssetId, changeLevel },
      })
      .then((r) => r.data),

  getHistory: (apiAssetId: string) =>
    client
      .get<ContractVersion[]>(`/contracts/history/${apiAssetId}`)
      .then((r) => r.data),

  getDetail: (contractVersionId: string) =>
    client
      .get(`/contracts/${contractVersionId}/detail`)
      .then((r) => r.data),

  lockVersion: (contractVersionId: string, reason: string) =>
    client
      .post(`/contracts/${contractVersionId}/lock`, { reason })
      .then((r) => r.data),

  transitionLifecycle: (contractVersionId: string, newState: string) =>
    client
      .post(`/contracts/${contractVersionId}/lifecycle`, { contractVersionId, newState })
      .then((r) => r.data),

  deprecateVersion: (contractVersionId: string, deprecationNotice: string, sunsetDate?: string) =>
    client
      .post(`/contracts/${contractVersionId}/deprecate`, { contractVersionId, deprecationNotice, sunsetDate })
      .then((r) => r.data),

  signVersion: (contractVersionId: string) =>
    client
      .post(`/contracts/${contractVersionId}/sign`, { contractVersionId })
      .then((r) => r.data),

  verifySignature: (contractVersionId: string) =>
    client
      .get(`/contracts/${contractVersionId}/verify`)
      .then((r) => r.data),

  exportVersion: (contractVersionId: string) =>
    client.get(`/contracts/${contractVersionId}/export`).then((r) => r.data),
};
