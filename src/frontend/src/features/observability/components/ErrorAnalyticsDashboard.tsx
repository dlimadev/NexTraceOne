import React, { useEffect, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { observabilityService } from '../services/ObservabilityService';
import type { ErrorAnalytics, DashboardFilters } from '../types/ObservabilityTypes';
import { CHART_SEMANTIC, CHART_RAINBOW } from '../../../lib/chartColors';
import { Loader2, AlertTriangle } from 'lucide-react';

export const ErrorAnalyticsDashboard: React.FC = () => {
  const [errors, setErrors] = useState<ErrorAnalytics[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters] = useState<DashboardFilters>({
    timeRange: '24h'
  });

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    observabilityService.getErrorAnalytics(filters)
      .then(data => { if (!cancelled) setErrors(data); })
      .catch(() => { /* Erro tratado silenciosamente — estado vazio */ })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [filters]);

  const errorTypeData = errors.reduce((acc, err) => {
    const existing = acc.find(e => e.name === err.errorType);
    if (existing) {
      existing.value += err.occurrenceCount;
    } else {
      acc.push({ name: err.errorType, value: err.occurrenceCount });
    }
    return acc;
  }, [] as { name: string; value: number }[]);

  const severityCounts = {
    critical: errors.filter(e => e.errorType.toLowerCase().includes('critical')).length,
    high: errors.filter(e => e.errorType.toLowerCase().includes('high') || e.errorType.toLowerCase().includes('exception')).length,
    medium: errors.filter(e => e.errorType.toLowerCase().includes('medium') || e.errorType.toLowerCase().includes('warning')).length,
    low: errors.filter(e => e.errorType.toLowerCase().includes('low')).length
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-critical" />
              Total Errors
            </h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-critical">
              {errors.reduce((sum, e) => sum + e.occurrenceCount, 0).toLocaleString()}
            </div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Critical</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">{severityCounts.critical}</div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">High</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">{severityCounts.high}</div>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Unique Types</h3>
          </CardHeader>
          <CardBody>
            <div className="text-2xl font-bold text-heading">{errorTypeData.length}</div>
          </CardBody>
        </Card>
      </div>

      {/* Error Distribution Pie Chart */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Error Distribution by Type</h3>
          </CardHeader>
          <CardBody>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={errorTypeData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => `${name} ${percent ? (percent * 100).toFixed(0) : '0'}%`}
                  outerRadius={100}
                  fill={CHART_RAINBOW[0]}
                  dataKey="value"
                >
                  {errorTypeData.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={CHART_RAINBOW[index % CHART_RAINBOW.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </CardBody>
        </Card>

        {/* Top Errors List */}
        <Card>
          <CardHeader>
            <h3 className="text-sm font-medium text-heading">Top Errors</h3>
          </CardHeader>
          <CardBody>
            <div className="space-y-3 max-h-[300px] overflow-y-auto">
              {errors.slice(0, 10).map((err, idx) => (
                <div key={idx} className="border border-edge rounded-lg p-3 hover:bg-elevated transition-colors">
                  <div className="flex items-start justify-between mb-2">
                    <div className="font-medium text-sm text-body truncate flex-1">{err.errorType}</div>
                    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-critical/10 text-critical ml-2 shrink-0">
                      {err.occurrenceCount}
                    </span>
                  </div>
                  <div className="text-xs text-muted mb-1 line-clamp-2">{err.errorMessage}</div>
                  <div className="text-xs text-faded">
                    Service: {err.serviceName} | Affected: {err.affectedEndpoints.length} endpoints
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      </div>

      {/* Error Trend Bar Chart */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-medium text-heading">Error Trend Over Time</h3>
        </CardHeader>
        <CardBody>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart
              data={errors.slice(0, 20).map(e => ({
                time: new Date(e.timeBucket).toLocaleTimeString(),
                count: e.occurrenceCount,
                type: e.errorType.substring(0, 30)
              }))}
            >
              <CartesianGrid strokeDasharray="3 3" stroke={CHART_SEMANTIC.grid} />
              <XAxis dataKey="time" stroke={CHART_SEMANTIC.axis} />
              <YAxis stroke={CHART_SEMANTIC.axis} />
              <Tooltip />
              <Legend />
              <Bar dataKey="count" fill={CHART_SEMANTIC.critical} name="Error Count" />
            </BarChart>
          </ResponsiveContainer>
        </CardBody>
      </Card>
    </div>
  );
};
