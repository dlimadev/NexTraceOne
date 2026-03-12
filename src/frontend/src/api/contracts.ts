import client from './client';
import type { ContractVersion, SemanticDiff } from '../types';

export const contractsApi = {
  importContract: (data: {
    apiAssetId: string;
    content: string;
    version: string;
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

  lockVersion: (contractVersionId: string, reason: string) =>
    client
      .post(`/contracts/${contractVersionId}/lock`, { reason })
      .then((r) => r.data),

  exportVersion: (contractVersionId: string) =>
    client.get(`/contracts/${contractVersionId}/export`).then((r) => r.data),
};
