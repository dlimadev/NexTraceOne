import React, { useEffect, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { observabilityService } from '../services/ObservabilityService';
import type { SystemHealthMetrics, DashboardFilters } from '../types/ObservabilityTypes';
import { Loader2, Server, Cpu, HardDrive, Activity } from 'lucide-react';

export const SystemHealthDashboard: React.FC = () => {
  const [health, setHealth] = useState<SystemHealthMetrics[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<DashboardFilters>({
    timeRange: '1h'
  });

  useEffect(() => {
    loadHealth();
  }, [filters]);

  const loadHealth = async () => {
    try {
      setLoading(true);
      const data = await observabilityService.getSystemHealth(filters);
      setHealth(data);
    } catch {
      // Erro tratado silenciosamente - component mostra estado vazio
      // Logging estruturado deve ser feito pelo backend
    } finally {
      setLoading(false);
    }
  };

  const latest = health[0];
  const avgCpu = health.length > 0 ? health.reduce((sum, h) => sum + h.cpuUsagePercent, 0) / health.length : 0;
  const avgMemory = health.length > 0 ? health.reduce((sum, h) => sum + h.memoryUsageMB, 0) / health.length : 0;
  const avgRps = health.length > 0 ? health.reduce((sum, h) => sum + h.requestsPerSecond, 0) / health.length : 0;

  const chartData = health.map(h => ({
    time: new Date(h.timestamp).toLocaleTimeString(),
    cpu: h.cpuUsagePercent.toFixed(1),
    memory: (h.memoryUsageMB / 1024).toFixed(2), // Convert to GB
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

  return (
    <div className="space-y-6">
      {/* Current Status Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Cpu className="h-4 w-4" />
              CPU Usage
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold mb-2">
              {latest?.cpuUsagePercent.toFixed(1) || 0}%
            </div>
            <Progress value={latest?.cpuUsagePercent || 0} className="h-2" />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Server className="h-4 w-4" />
              Memory
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold mb-2">
              {latest ? (latest.memoryUsageMB / 1024).toFixed(2) : 0} GB
            </div>
            <Progress value={(latest?.memoryUsageMB || 0) / 80} className="h-2" />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <Activity className="h-4 w-4" />
              RPS
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {latest?.requestsPerSecond.toFixed(0) || 0}
            </div>
            <div className="text-xs text-gray-500 mt-1">Requests per second</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <HardDrive className="h-4 w-4" />
              Disk Usage
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold mb-2">
              {latest?.diskUsagePercent.toFixed(1) || 0}%
            </div>
            <Progress value={latest?.diskUsagePercent || 0} className="h-2" />
          </CardContent>
        </Card>
      </div>

      {/* System Metrics Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>CPU & Memory Trends</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis yAxisId="left" />
                <YAxis yAxisId="right" orientation="right" />
                <Tooltip />
                <Legend />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="cpu"
                  stroke="#3b82f6"
                  strokeWidth={2}
                  name="CPU %"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="memory"
                  stroke="#10b981"
                  strokeWidth={2}
                  name="Memory (GB)"
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Request Rate & Error Rate</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis yAxisId="left" />
                <YAxis yAxisId="right" orientation="right" />
                <Tooltip />
                <Legend />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="rps"
                  stroke="#8b5cf6"
                  strokeWidth={2}
                  name="RPS"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="errorRate"
                  stroke="#ef4444"
                  strokeWidth={2}
                  name="Error Rate %"
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </div>

      {/* Averages Summary */}
      <Card>
        <CardHeader>
          <CardTitle>Average Metrics (Selected Period)</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="text-center p-4 bg-blue-50 rounded-lg">
              <div className="text-sm text-gray-600 mb-1">Avg CPU</div>
              <div className="text-2xl font-bold text-blue-600">{avgCpu.toFixed(1)}%</div>
            </div>
            <div className="text-center p-4 bg-green-50 rounded-lg">
              <div className="text-sm text-gray-600 mb-1">Avg Memory</div>
              <div className="text-2xl font-bold text-green-600">{(avgMemory / 1024).toFixed(2)} GB</div>
            </div>
            <div className="text-center p-4 bg-purple-50 rounded-lg">
              <div className="text-sm text-gray-600 mb-1">Avg RPS</div>
              <div className="text-2xl font-bold text-purple-600">{avgRps.toFixed(0)}</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
