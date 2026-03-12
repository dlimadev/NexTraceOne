import client from './client';
import type { ApiAsset, AssetGraph } from '../types';

export const engineeringGraphApi = {
  getGraph: () =>
    client.get<AssetGraph>('/engineeringgraph/graph').then((r) => r.data),

  getApiAsset: (id: string) =>
    client.get<ApiAsset>(`/engineeringgraph/apis/${id}`).then((r) => r.data),

  searchApis: (searchTerm: string) =>
    client
      .get<ApiAsset[]>('/engineeringgraph/apis/search', { params: { searchTerm } })
      .then((r) => r.data),

  registerService: (data: { name: string; team: string; description?: string }) =>
    client.post<{ id: string }>('/engineeringgraph/services', data).then((r) => r.data),

  registerApi: (data: {
    name: string;
    baseUrl: string;
    ownerServiceId: string;
    description?: string;
  }) => client.post<{ id: string }>('/engineeringgraph/apis', data).then((r) => r.data),

  mapConsumer: (
    apiAssetId: string,
    data: { consumerServiceId: string; trustLevel: string }
  ) =>
    client
      .post(`/engineeringgraph/apis/${apiAssetId}/consumers`, data)
      .then((r) => r.data),
};
