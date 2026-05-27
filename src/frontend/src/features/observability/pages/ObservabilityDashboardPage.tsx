import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { RequestMetricsDashboard } from '../components/RequestMetricsDashboard';
import { ErrorAnalyticsDashboard } from '../components/ErrorAnalyticsDashboard';
import { SystemHealthDashboard } from '../components/SystemHealthDashboard';
import { Activity, AlertTriangle, RefreshCw, Server, TrendingUp } from 'lucide-react';

export const ObservabilityDashboardPage: React.FC = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState('overview');

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

        {/* Overview Tab */}
        {activeTab === 'overview' && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <h3 className="text-sm font-medium text-heading">{t('observability.quickStats', 'Quick Stats')}</h3>
                </CardHeader>
                <CardBody>
                  <div className="space-y-4">
                    <div className="flex items-center justify-between p-3 bg-accent/10 rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.totalRequests24h', 'Total Requests (24h)')}</div>
                        <div className="text-2xl font-bold text-accent">1.2M</div>
                      </div>
                      <TrendingUp className="h-8 w-8 text-accent" />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-success/10 rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.avgResponseTime', 'Avg Response Time')}</div>
                        <div className="text-2xl font-bold text-success">145ms</div>
                      </div>
                      <Activity className="h-8 w-8 text-success" />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-critical/10 rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.errorRate', 'Error Rate')}</div>
                        <div className="text-2xl font-bold text-critical">0.8%</div>
                      </div>
                      <AlertTriangle className="h-8 w-8 text-critical" />
                    </div>
                    <div className="flex items-center justify-between p-3 bg-elevated rounded-lg">
                      <div>
                        <div className="text-sm text-muted">{t('observability.activeServices', 'Active Services')}</div>
                        <div className="text-2xl font-bold text-heading">12</div>
                      </div>
                      <Server className="h-8 w-8 text-muted" />
                    </div>
                  </div>
                </CardBody>
              </Card>

              <Card>
                <CardHeader>
                  <h3 className="text-sm font-medium text-heading">{t('observability.systemStatus', 'System Status')}</h3>
                </CardHeader>
                <CardBody>
                  <div className="space-y-3">
                    {[
                      { label: 'API Host', status: 'Healthy' },
                      { label: 'Background Workers', status: 'Healthy' },
                      { label: 'PostgreSQL', status: 'Healthy' },
                      { label: 'Redis', status: 'Healthy' },
                      { label: 'Kafka', status: 'Healthy' },
                      { label: 'ClickHouse', status: 'Healthy' },
                    ].map((service) => (
                      <div key={service.label} className="flex items-center justify-between">
                        <span className="text-sm font-medium text-body">{service.label}</span>
                        <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-success/10 text-success">
                          {service.status}
                        </span>
                      </div>
                    ))}
                  </div>
                </CardBody>
              </Card>
            </div>

            <Card>
              <CardHeader>
                <h3 className="text-sm font-medium text-heading">{t('observability.recentActivity', 'Recent Activity')}</h3>
              </CardHeader>
              <CardBody>
                <div className="space-y-3">
                  {[
                    { time: '2 min ago', event: 'Deployment completed successfully', type: 'success' },
                    { time: '15 min ago', event: 'Auto-scaling triggered: 3 → 5 replicas', type: 'info' },
                    { time: '1 hour ago', event: 'Database backup completed', type: 'success' },
                    { time: '2 hours ago', event: 'High memory usage detected (85%)', type: 'warning' },
                    { time: '3 hours ago', event: 'SSL certificate renewed', type: 'success' }
                  ].map((activity, idx) => (
                    <div key={idx} className="flex items-start gap-3 p-3 border border-edge rounded-lg hover:bg-elevated transition-colors">
                      <div className={`w-2 h-2 rounded-full mt-2 ${
                        activity.type === 'success' ? 'bg-success' :
                        activity.type === 'warning' ? 'bg-warning' :
                        'bg-accent'
                      }`} />
                      <div className="flex-1">
                        <div className="text-sm text-body">{activity.event}</div>
                        <div className="text-xs text-faded">{activity.time}</div>
                      </div>
                    </div>
                  ))}
                </div>
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
