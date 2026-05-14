export interface RequestMetrics {
  timeBucket: string;
  endpoint: string;
  httpMethod: string;
  requestCount: number;
  avgDurationMs: number;
  p50DurationMs: number;
  p95DurationMs: number;
  p99DurationMs: number;
  errorCount: number;
  errorRate: number;
}

export interface ErrorAnalytics {
  timeBucket: string;
  errorType: string;
  errorMessage: string;
  serviceName: string;
  occurrenceCount: number;
  affectedEndpoints: string[];
  sampleStackTraces: string[];
}

export interface UserActivityMetrics {
  timeBucket: string;
  userId: string;
  actionCount: number;
  topEndpoints: string[];
  avgSessionDurationMinutes: number;
}

export interface SystemHealthMetrics {
  timestamp: string;
  serviceName: string;
  cpuUsagePercent: number;
  memoryUsageMB: number;
  diskUsagePercent: number;
  activeConnections: number;
  requestsPerSecond: number;
  errorRatePercent: number;
}

export interface DashboardFilters {
  timeRange: '1h' | '6h' | '24h' | '7d' | '30d';
  serviceName?: string;
  endpoint?: string;
  environment?: string;
}
