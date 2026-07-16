import client from '../../../api/client';

/** Feature flag escopada por serviço (tabela ctr_feature_flag_records). */
export interface ServiceFeatureFlag {
  id: string;
  serviceId: string;
  serviceName: string;
  flagKey: string;
  displayName: string;
  description?: string;
  enabled: boolean;
  environment: string;
  updatedAt: string;
  updatedBy?: string;
}

/** Dashboard agregado de feature flags de todos os serviços. */
export interface ServiceFeatureFlagDashboard {
  totalFlags: number;
  enabledFlags: number;
  disabledFlags: number;
  affectedServices: number;
  flags: ServiceFeatureFlag[];
}

/** Cliente de feature flags do catálogo. */
export const serviceFeatureFlagsApi = {
  getDashboard: async (): Promise<ServiceFeatureFlagDashboard> => {
    const res = await client.get<ServiceFeatureFlagDashboard>('/catalog/feature-flags');
    return res.data;
  },
  toggle: async (flagId: string, enabled: boolean): Promise<void> => {
    await client.patch(`/catalog/feature-flags/${flagId}`, { enabled });
  },
};
