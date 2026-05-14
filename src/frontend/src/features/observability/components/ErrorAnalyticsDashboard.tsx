import React, { useEffect, useState } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { observabilityService } from '../services/ObservabilityService';
import type { ErrorAnalytics, DashboardFilters } from '../types/ObservabilityTypes';
import { Loader2, AlertTriangle } from 'lucide-react';

const COLORS = ['#ef4444', '#f59e0b', '#3b82f6', '#10b981', '#8b5cf6'];

export const ErrorAnalyticsDashboard: React.FC = () => {
  const [errors, setErrors] = useState<ErrorAnalytics[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState<DashboardFilters>({
    timeRange: '24h'
  });

  useEffect(() => {
    loadErrors();
  }, [filters]);

  const loadErrors = async () => {
    try {
      setLoading(true);
      const data = await observabilityService.getErrorAnalytics(filters);
      setErrors(data);
    } catch {
      // Erro tratado silenciosamente - component mostra estado vazio
      // Logging estruturado deve ser feito pelo backend
    } finally {
      setLoading(false);
    }
  };

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
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-red-600" />
              Total Errors
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600">
              {errors.reduce((sum, e) => sum + e.occurrenceCount, 0).toLocaleString()}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Critical</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{severityCounts.critical}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">High</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{severityCounts.high}</div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Unique Types</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{errorTypeData.length}</div>
          </CardContent>
        </Card>
      </div>

      {/* Error Distribution Pie Chart */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Error Distribution by Type</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={errorTypeData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => `${name} ${percent ? (percent * 100).toFixed(0) : '0'}%`}
                  outerRadius={100}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {errorTypeData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        {/* Top Errors List */}
        <Card>
          <CardHeader>
            <CardTitle>Top Errors</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3 max-h-[300px] overflow-y-auto">
              {errors.slice(0, 10).map((err, idx) => (
                <div key={idx} className="border rounded-lg p-3 hover:bg-gray-50 transition-colors">
                  <div className="flex items-start justify-between mb-2">
                    <div className="font-medium text-sm truncate flex-1">{err.errorType}</div>
                    <Badge variant="destructive">{err.occurrenceCount}</Badge>
                  </div>
                  <div className="text-xs text-gray-600 mb-1 line-clamp-2">{err.errorMessage}</div>
                  <div className="text-xs text-gray-500">
                    Service: {err.serviceName} | Affected: {err.affectedEndpoints.length} endpoints
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Error Trend Bar Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Error Trend Over Time</CardTitle>
        </CardHeader>
        <CardContent>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart
              data={errors.slice(0, 20).map(e => ({
                time: new Date(e.timeBucket).toLocaleTimeString(),
                count: e.occurrenceCount,
                type: e.errorType.substring(0, 30)
              }))}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="time" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Bar dataKey="count" fill="#ef4444" name="Error Count" />
            </BarChart>
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </div>
  );
};
