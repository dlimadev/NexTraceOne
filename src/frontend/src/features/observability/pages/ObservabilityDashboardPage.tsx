import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { RequestMetricsDashboard } from '../components/RequestMetricsDashboard';
import { ErrorAnalyticsDashboard } from '../components/ErrorAnalyticsDashboard';
import { SystemHealthDashboard } from '../components/SystemHealthDashboard';
import { observabilityService } from '../services/ObservabilityService';
import { Activity, AlertTriangle, RefreshCw, Server, TrendingUp, Users } from 'lucide-react';

const formatCount = (value: number): string => {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(1)}K`;
  return value.toString();
};

export const ObservabilityDashboardPage: React.FC = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState('overview');

  const statsQuery = useQuery({
    queryKey: ['observability', 'overall-stats'],
    queryFn: () => observabilityService.getOverallStats({ timeRange: '24h' }),
  });

  return (
    <PageContainer>
      <PageHeader
        title={t('observability.title', 'Observability Dashboard')}
        subtitle={t('observability.subtitle', 'Real-time monitoring and analytics powered by ClickHouse')}
        icon={<Activity size={20} />}
        actions={
          <Button variant="ghost" onClick={() => window.location.reload()} className="flex items-center gap-2">
            <RefreshCw size={14} />
            {t('common.refresh', 'Refresh')}
          </Button>
        }
      />

      <div className="space-y-6">
        {/* Tab bar */}
        <div className="flex gap-1 p-1 bg-elevated rounded-lg w-fit">
          {[
            { id: 'overview', label: t('observability.tab.overview', 'Overview'), icon: <Activity size={14} /> },
            { id: 'requests', label: t('observability.tab.requests', 'Requests'), icon: <TrendingUp size={14} /> },
            { id: 'errors',   label: t('observability.tab.errors', 'Errors'),   icon: <AlertTriangle size={14} /> },
            { id: 'system',   label: t('observability.tab.system', 'System Health'), icon: <Server size={14} /> },
          ].map(tab => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                activeTab === tab.id
                  ? 'bg-card text-body shadow-sm'
                  : 'text-muted hover:text-body'
              }`}
            >
              {tab.icon}
              {tab.label}
            </button>
          ))}
        </div>

        {/* Overview Tab — estatísticas reais de /observability/stats (24h) */}
        {activeTab === 'overview' && (
          <div className="space-y-4">
            <Card>
              <CardHeader>
                <h3 className="text-sm font-medium text-heading">{t('observability.quickStats', 'Quick Stats')}</h3>
              </CardHeader>
              <CardBody>
                {statsQuery.isLoading ? (
                  <div className="py-12 text-center text-sm text-muted">
                    {t('common.loading', 'Loading…')}
                  </div>
                ) : statsQuery.isError ? (
                  <div className="py-12 text-center text-sm text-critical">
                    {t('observability.statsError', 'Failed to load observability stats. Check the telemetry backend.')}
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="flex items-center justify-between p-3 bg-accent/10 rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.totalRequests24h', 'Total Requests (24h)')}</div>
                        <div className="text-2xl font-bold text-accent">{formatCount(statsQuery.data?.totalRequests ?? 0)}</div>
                      </div>
                      <TrendingUp className="h-8 w-8 text-accent" />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-success/10 rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.avgResponseTime', 'Avg Response Time')}</div>
                        <div className="text-2xl font-bold text-success">{Math.round(statsQuery.data?.avgResponseTime ?? 0)}ms</div>
                      </div>
                      <Activity className="h-8 w-8 text-success" />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-critical/10 rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.errorRate', 'Error Rate')}</div>
                        <div className="text-2xl font-bold text-critical">{(statsQuery.data?.errorRate ?? 0).toFixed(1)}%</div>
                      </div>
                      <AlertTriangle className="h-8 w-8 text-critical" />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-elevated rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.activeUsers', 'Active Users')}</div>
                        <div className="text-2xl font-bold text-heading">{formatCount(statsQuery.data?.activeUsers ?? 0)}</div>
                      </div>
                      <Users className="h-8 w-8 text-muted" />
                    </div>
                  </div>
                )}
              </CardBody>
            </Card>
          </div>
        )}

        {/* Requests Tab */}
        {activeTab === 'requests' && <RequestMetricsDashboard />}

        {/* Errors Tab */}
        {activeTab === 'errors' && <ErrorAnalyticsDashboard />}

        {/* System Health Tab */}
        {activeTab === 'system' && <SystemHealthDashboard />}
      </div>
    </PageContainer>
  );
};
