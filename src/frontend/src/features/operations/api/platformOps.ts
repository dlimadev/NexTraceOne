import client from '../../../api/client';
import type { PlatformOperationsResponse } from '../../../types';

/** Cliente de API para Platform Operations (runtime health e alertas). */
export const platformOpsApi = {
  getHealth: () =>
    client.get<PlatformOperationsResponse>('/runtime/health').then((r) => r.data),
};
