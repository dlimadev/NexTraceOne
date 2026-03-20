import client from '../../../api/client';
import type { EvidencePackageListResponse } from '../../../types';

/** Cliente de API para Evidence Packages. */
export const evidenceApi = {
  listPackages: (params?: { scope?: string; status?: string }) =>
    client.get<EvidencePackageListResponse>('/evidence/packages', { params }).then((r) => r.data),

  getPackage: (packageId: string) =>
    client.get<EvidencePackageListResponse>(`/evidence/packages/${packageId}`).then((r) => r.data),
};
