import client from '../../../api/client';

export interface LegacyAssetSummary {
  id: string;
  assetType: string;
  name: string;
  displayName: string;
  teamName: string;
  domain: string;
  criticality: string;
  lifecycleStatus: string;
}

export interface LegacyAssetDetail extends LegacyAssetSummary {
  description: string;
  metadata: Record<string, string>;
  createdAt: string;
  updatedAt: string | null;
}

export interface LegacyAssetFilters {
  teamName?: string;
  domain?: string;
  criticality?: string;
  lifecycleStatus?: string;
  searchTerm?: string;
}

export const legacyAssetsApi = {
  list: (filters?: LegacyAssetFilters) =>
    client.get<LegacyAssetSummary[]>('/catalog/legacy/assets', { params: filters }).then((r) => r.data),

  getDetail: (assetType: string, assetId: string) =>
    client.get<LegacyAssetDetail>(`/catalog/legacy/assets/${assetType}/${assetId}`).then((r) => r.data),
};
