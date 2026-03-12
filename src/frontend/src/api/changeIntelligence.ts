import client from './client';
import type {
  Release,
  BlastRadiusReport,
  ChangeScore,
  PagedList,
  DeploymentState,
  ChangeLevel,
} from '../types';

export const changeIntelligenceApi = {
  notifyDeployment: (data: {
    apiAssetId: string;
    version: string;
    environment: string;
    commitSha?: string;
    pipelineUrl?: string;
  }) =>
    client.post<{ id: string }>('/releases', data).then((r) => r.data),

  getRelease: (id: string) =>
    client.get<Release>(`/releases/${id}`).then((r) => r.data),

  listReleases: (apiAssetId: string, page = 1, pageSize = 20) =>
    client
      .get<PagedList<Release>>('/releases', { params: { apiAssetId, page, pageSize } })
      .then((r) => r.data),

  getReleaseHistory: (apiAssetId: string, page = 1, pageSize = 20) =>
    client
      .get<PagedList<Release>>(`/releases/${apiAssetId}/history`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),

  classifyChangeLevel: (releaseId: string, changeLevel: ChangeLevel) =>
    client
      .put(`/releases/${releaseId}/classify`, { releaseId, changeLevel })
      .then((r) => r.data),

  updateDeploymentState: (releaseId: string, state: DeploymentState) =>
    client
      .put(`/releases/${releaseId}/status`, { releaseId, newState: state })
      .then((r) => r.data),

  registerRollback: (releaseId: string, reason: string) =>
    client
      .post(`/releases/${releaseId}/rollback`, { releaseId, reason })
      .then((r) => r.data),

  calculateBlastRadius: (releaseId: string) =>
    client
      .post(`/releases/${releaseId}/blast-radius`, { releaseId })
      .then((r) => r.data),

  getBlastRadius: (releaseId: string) =>
    client.get<BlastRadiusReport>(`/releases/${releaseId}/blast-radius`).then((r) => r.data),

  computeScore: (releaseId: string) =>
    client
      .post(`/releases/${releaseId}/score`, { releaseId })
      .then((r) => r.data),

  getScore: (releaseId: string) =>
    client.get<ChangeScore>(`/releases/${releaseId}/score`).then((r) => r.data),

  attachWorkItem: (
    releaseId: string,
    data: { provider: string; workItemId: string; url: string }
  ) =>
    client.put(`/releases/${releaseId}/workitem`, { releaseId, ...data }).then((r) => r.data),
};
