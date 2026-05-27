import React, { useEffect, useState } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, AreaChart, Area } from 'recharts';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { observabilityService } from '../services/ObservabilityService';
import type { RequestMetrics, DashboardFilters } from '../types/ObservabilityTypes';
import { CHART_SEMANTIC } from '../../../lib/chartColors';
import { Loader2 } from 'lucide-react';

export const RequestMetricsDashboard: React.FC = () => {
  const [metrics, setMetrics] = useState<RequestMetrics[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<DashboardFilters>({
    timeRange: '24h'
  });

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    observabilityService.getRequestMetrics(filters)
      .then(data => { if (!cancelled) setMetrics(data); })
      .catch(() => { /* Erro tratado silenciosamente — estado vazio */ })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [filters]);

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

  const timeRangeOptions: { value: DashboardFilters['timeRange']; label: string }[] = [
    { value: '1h',  label: 'Last 1 Hour' },
    { value: '6h',  label: 'Last 6 Hours' },
    { value: '24h', label: 'Last 24 Hours' },
    { value: '7d',  label: 'Last 7 Days' },
    { value: '30d', label: 'Last 30 Days' },
  ];

  return (
    <div className="space-y-6">
      {/* Filters */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-medium text-heading">Filters</h3>
        </CardHeader>
        <CardBody>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label className="text-sm font-medium text-body mb-2 block">Time Range</label>
              <select
                value={filters.timeRange}
                onChange={(e) => setFilters({ ...filters, timeRange: e.target.value as DashboardFilters['timeRange'] })}
                className="w-full h-9 rounded-md border border-edge bg-card text-body text-sm px-3 focus:outline-none focus:ring-2 focus:ring-accent/40"
              >
                {timeRangeOptions.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Key Metrics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Total Requests</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">
              {metrics.reduce((sum, m) => sum + m.requestCount, 0).toLocaleString()}
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Avg Response Time</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">
              {metrics.length > 0
                ? Math.round(metrics.reduce((sum, m) => sum + m.avgDurationMs, 0) / metrics.length)
                : 0} ms
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">P95 Latency</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">
              {metrics.length > 0
                ? Math.round(metrics.reduce((sum, m) => sum + m.p95DurationMs, 0) / metrics.length)
                : 0} ms
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Error Rate</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-critical">
              {metrics.length > 0
                ? (metrics.reduce((sum, m) => sum + m.errorRate, 0) / metrics.length).toFixed(2)
                : 0}%
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Request Volume Chart */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-medium text-heading">Request Volume Over Time</h3>
        </CardHeader>
        <CardBody>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke={CHART_SEMANTIC.grid} />
              <XAxis dataKey="time" stroke={CHART_SEMANTIC.axis} />
              <YAxis stroke={CHART_SEMANTIC.axis} />
              <Tooltip />
              <Legend />
              <Area
                type="monotone"
                dataKey="requests"
                stroke={CHART_SEMANTIC.accent}
                fill={CHART_SEMANTIC.accent}
                fillOpacity={0.3}
                name="Requests"
              />
            </AreaChart>
          </ResponsiveContainer>
        </CardBody>
      </Card>

      {/* Response Time Chart */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-medium text-heading">Response Time Trends</h3>
        </CardHeader>
        <CardBody>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" stroke={CHART_SEMANTIC.grid} />
              <XAxis dataKey="time" stroke={CHART_SEMANTIC.axis} />
              <YAxis stroke={CHART_SEMANTIC.axis} />
              <Tooltip />
              <Legend />
              <Line
                type="monotone"
                dataKey="avgDuration"
                stroke={CHART_SEMANTIC.success}
                strokeWidth={2}
                name="Avg (ms)"
              />
              <Line
                type="monotone"
                dataKey="p95Duration"
                stroke={CHART_SEMANTIC.warning}
                strokeWidth={2}
                name="P95 (ms)"
              />
            </LineChart>
          </ResponsiveContainer>
        </CardBody>
      </Card>
    </div>
  );
};
