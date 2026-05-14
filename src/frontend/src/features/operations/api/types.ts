/** Runtime Intelligence API Types */

export interface RuntimeSnapshot {
  id: string;
  serviceName: string;
  environment: string;
  timestamp: string;
  healthStatus: 'Healthy' | 'Degraded' | 'Unhealthy';
  avgLatencyMs: number;
  p95LatencyMs?: number;
  p99LatencyMs?: number;
  errorRate: number;
  requestsPerSecond: number;
  cpuUsagePercent: number;
  memoryUsageMb: number;
}

export interface ObservabilityScore {
  serviceName: string;
  environment: string;
  score: number; // 0-1 scale
  hasCriticalDrift: boolean;
  metrics: {
    logging: number;
    tracing: number;
    metrics: number;
    alerting: number;
  };
}

export interface DriftFinding {
  id: string;
  serviceName: string;
  environment: string;
  metricName: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  deviationPercent: number;
  baselineValue: number;
  currentValue: number;
  detectedAt: string;
  description?: string;
}

export interface RuntimeIntelligenceMetrics {
  totalSnapshots: number;
  healthyCount: number;
  degradedCount: number;
  unhealthyCount: number;
  avgLatency: number;
  avgErrorRate: number;
  avgThroughput: number;
}
