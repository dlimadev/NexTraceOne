import { apiClient } from '@/lib/api-client';
import type { RuntimeSnapshot, ObservabilityScore, DriftFinding } from './types';

export interface IngestSnapshotRequest {
  serviceName: string;
  environment: string;
  timestamp: string;
  avgLatencyMs: number;
  p95LatencyMs?: number;
  p99LatencyMs?: number;
  errorRate: number;
  requestsPerSecond: number;
  cpuUsagePercent: number;
  memoryUsageMb: number;
}

export const runtimeIntelligenceApi = {
  /** Get runtime snapshots */
  getSnapshots: async (serviceName?: string, environment?: string): Promise<{ items: RuntimeSnapshot[] }> => {
    const params = new URLSearchParams();
    if (serviceName) params.append('serviceName', serviceName);
    if (environment) params.append('environment', environment);
    
    const response = await apiClient.get(`/api/runtime-intelligence/snapshots?${params.toString()}`);
    return response.data;
  },

  /** Ingest a runtime snapshot */
  ingestSnapshot: async (data: IngestSnapshotRequest): Promise<void> => {
    await apiClient.post('/api/runtime-intelligence/snapshots/ingest', data);
  },

  /** Get observability scores */
  getObservabilityScores: async (serviceName?: string, environment?: string): Promise<{ items: ObservabilityScore[] }> => {
    const params = new URLSearchParams();
    if (serviceName) params.append('serviceName', serviceName);
    if (environment) params.append('environment', environment);
    
    const response = await apiClient.get(`/api/runtime-intelligence/observability-scores?${params.toString()}`);
    return response.data;
  },

  /** Get drift findings */
  getDriftFindings: async (serviceName?: string, environment?: string): Promise<{ items: DriftFinding[] }> => {
    const params = new URLSearchParams();
    if (serviceName) params.append('serviceName', serviceName);
    if (environment) params.append('environment', environment);
    
    const response = await apiClient.get(`/api/runtime-intelligence/drift-findings?${params.toString()}`);
    return response.data;
  },
};
