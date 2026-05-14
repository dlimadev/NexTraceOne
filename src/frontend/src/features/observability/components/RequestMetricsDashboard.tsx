import React, { useEffect, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, AreaChart, Area } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { observabilityService } from '../services/ObservabilityService';
import type { RequestMetrics, DashboardFilters } from '../types/ObservabilityTypes';
import { Loader2 } from 'lucide-react';

export const RequestMetricsDashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<RequestMetrics[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<DashboardFilters>({
    timeRange: '24h'
  });

  useEffect(() => {
    loadMetrics();
  }, [filters]);

  const loadMetrics = async () => {
    try {
      setLoading(true);
      const data = await observabilityService.getRequestMetrics(filters);
      setMetrics(data);
    } catch {
      // Erro tratado silenciosamente - component mostra estado vazio
      // Logging estruturado deve ser feito pelo backend
    } finally {
      setLoading(false);
    }
  };

  const chartData = metrics.map(m => ({
    time: new Date(m.timeBucket).toLocaleTimeString(),
    requests: m.requestCount,
    avgDuration: Math.round(m.avgDurationMs),
    p95Duration: Math.round(m.p95DurationMs),
    errorRate: m.errorRate.toFixed(2)
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
      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filters</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Time Range</label>
              <Select
                value={filters.timeRange}
                onValueChange={(value) => setFilters({ ...filters, timeRange: value as any })}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1h">Last 1 Hour</SelectItem>
                  <SelectItem value="6h">Last 6 Hours</SelectItem>
                  <SelectItem value="24h">Last 24 Hours</SelectItem>
                  <SelectItem value="7d">Last 7 Days</SelectItem>
                  <SelectItem value="30d">Last 30 Days</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Key Metrics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Total Requests</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {metrics.reduce((sum, m) => sum + m.requestCount, 0).toLocaleString()}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Avg Response Time</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {metrics.length > 0 
                ? Math.round(metrics.reduce((sum, m) => sum + m.avgDurationMs, 0) / metrics.length)
                : 0} ms
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">P95 Latency</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {metrics.length > 0
                ? Math.round(metrics.reduce((sum, m) => sum + m.p95DurationMs, 0) / metrics.length)
                : 0} ms
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Error Rate</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {metrics.length > 0
                ? (metrics.reduce((sum, m) => sum + m.errorRate, 0) / metrics.length).toFixed(2)
                : 0}%
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Request Volume Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Request Volume Over Time</CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Area
                type="monotone"
                dataKey="requests"
                stroke="#3b82f6"
                fill="#3b82f6"
                fillOpacity={0.3}
                name="Requests"
              />
            </AreaChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>

      {/* Response Time Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Response Time Trends</CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Line
                type="monotone"
                dataKey="avgDuration"
                stroke="#10b981"
                strokeWidth={2}
                name="Avg (ms)"
              />
              <Line
                type="monotone"
                dataKey="p95Duration"
                stroke="#f59e0b"
                strokeWidth={2}
                name="P95 (ms)"
              />
            </LineChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </div>
  );
};
