import client from '../../../api/client';
import type {
  BenchmarkingResponse,
  ExecutiveDrillDownResponse,
} from '../../../types';

/** Cliente de API para Executive Governance (benchmarking, drill-down, trends). */
export const executiveApi = {
  getBenchmarking: (dimension: string) =>
    client.get<BenchmarkingResponse>(`/executive/benchmarking/${dimension}`).then((r) => r.data),

  getDrillDown: (entityType: string, entityId: string) =>
    client.get<ExecutiveDrillDownResponse>(`/executive/${entityType}/${entityId}`).then((r) => r.data),
};
