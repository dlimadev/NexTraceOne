import React, { useEffect, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { observabilityService } from '../services/ObservabilityService';
import type { SystemHealthMetrics, DashboardFilters } from '../types/ObservabilityTypes';
import { CHART_SEMANTIC } from '../../../lib/chartColors';
import { Loader2, Server, Cpu, HardDrive, Activity } from 'lucide-react';

export const SystemHealthDashboard: React.FC = () => {
  const [health, setHealth] = useState<SystemHealthMetrics[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [filters] = useState<DashboardFilters>({
    timeRange: '1h'
  });

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setLoadError(false);
    observabilityService.getSystemHealth(filters)
      .then(data => { if (!cancelled) setHealth(data); })
      .catch(() => { if (!cancelled) setLoadError(true); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [filters]);

  const latest = health[0];
  const avgCpu = health.length > 0 ? health.reduce((sum, h) => sum + h.cpuUsagePercent, 0) / health.length : 0;
  const avgMemory = health.length > 0 ? health.reduce((sum, h) => sum + h.memoryUsageMB, 0) / health.length : 0;
  const avgRps = health.length > 0 ? health.reduce((sum, h) => sum + h.requestsPerSecond, 0) / health.length : 0;

  const chartData = health.map(h => ({
    time: new Date(h.timestamp).toLocaleTimeString(),
    cpu: h.cpuUsagePercent.toFixed(1),
    memory: (h.memoryUsageMB / 1024).toFixed(2),
    rps: h.requestsPerSecond.toFixed(0),
    errorRate: h.errorRatePercent.toFixed(2)
  }));

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (loadError) {
    return (
      <div className="flex items-center justify-center h-96 text-sm text-critical">
        Failed to load system health metrics. Check the observability backend and try again.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Current Status Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading flex items-center gap-2">
              <Cpu className="h-4 w-4" />
              CPU Usage
            </h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading mb-2">
              {latest?.cpuUsagePercent.toFixed(1) ?? 0}%
            </div>
            <div className="w-full h-2 bg-elevated rounded-full overflow-hidden">
              <div
                className="h-full bg-accent rounded-full transition-all"
                style={{ width: `${Math.min(latest?.cpuUsagePercent ?? 0, 100)}%` }}
              />
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading flex items-center gap-2">
              <Server className="h-4 w-4" />
              Memory
            </h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading mb-2">
              {latest ? (latest.memoryUsageMB / 1024).toFixed(2) : 0} GB
            </div>
            <div className="w-full h-2 bg-elevated rounded-full overflow-hidden">
              <div
                className="h-full bg-success rounded-full transition-all"
                style={{ width: `${Math.min((latest?.memoryUsageMB ?? 0) / 80, 100)}%` }}
              />
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading flex items-center gap-2">
              <Activity className="h-4 w-4" />
              RPS
            </h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">
              {latest?.requestsPerSecond.toFixed(0) ?? 0}
            </div>
            <div className="text-xs text-muted mt-1">Requests per second</div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading flex items-center gap-2">
              <HardDrive className="h-4 w-4" />
              Disk Usage
            </h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading mb-2">
              {latest?.diskUsagePercent.toFixed(1) ?? 0}%
            </div>
            <div className="w-full h-2 bg-elevated rounded-full overflow-hidden">
              <div
                className="h-full bg-warning rounded-full transition-all"
                style={{ width: `${Math.min(latest?.diskUsagePercent ?? 0, 100)}%` }}
              />
            </div>
          </CardBody>
        </Card>
      </div>

      {/* System Metrics Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">CPU &amp; Memory Trends</h3>
          </CardHeader>
          <CardBody>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_SEMANTIC.grid} />
                <XAxis dataKey="time" stroke={CHART_SEMANTIC.axis} />
                <YAxis yAxisId="left" stroke={CHART_SEMANTIC.axis} />
                <YAxis yAxisId="right" orientation="right" stroke={CHART_SEMANTIC.axis} />
                <Tooltip />
                <Legend />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="cpu"
                  stroke={CHART_SEMANTIC.accent}
                  strokeWidth={2}
                  name="CPU %"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="memory"
                  stroke={CHART_SEMANTIC.success}
                  strokeWidth={2}
                  name="Memory (GB)"
                />
              </LineChart>
            </ResponsiveContainer>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Request Rate &amp; Error Rate</h3>
          </CardHeader>
          <CardBody>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_SEMANTIC.grid} />
                <XAxis dataKey="time" stroke={CHART_SEMANTIC.axis} />
                <YAxis yAxisId="left" stroke={CHART_SEMANTIC.axis} />
                <YAxis yAxisId="right" orientation="right" stroke={CHART_SEMANTIC.axis} />
                <Tooltip />
                <Legend />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="rps"
                  stroke={CHART_SEMANTIC.info}
                  strokeWidth={2}
                  name="RPS"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="errorRate"
                  stroke={CHART_SEMANTIC.critical}
                  strokeWidth={2}
                  name="Error Rate %"
                />
              </LineChart>
            </ResponsiveContainer>
          </CardBody>
        </Card>
      </div>

      {/* Averages Summary */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-medium text-heading">Average Metrics (Selected Period)</h3>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="text-center p-4 bg-accent/10 rounded-lg">
              <div className="text-sm text-muted mb-1">Avg CPU</div>
              <div className="text-2xl font-bold text-accent">{avgCpu.toFixed(1)}%</div>
            </div>
            <div className="text-center p-4 bg-success/10 rounded-lg">
              <div className="text-sm text-muted mb-1">Avg Memory</div>
              <div className="text-2xl font-bold text-success">{(avgMemory / 1024).toFixed(2)} GB</div>
            </div>
            <div className="text-center p-4 bg-elevated rounded-lg">
              <div className="text-sm text-muted mb-1">Avg RPS</div>
              <div className="text-2xl font-bold text-heading">{avgRps.toFixed(0)}</div>
            </div>
          </div>
        </CardBody>
      </Card>
    </div>
  );
};
