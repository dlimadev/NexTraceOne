import * as React from 'react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  Server,
  Zap,
  BarChart3,
  RefreshCw,
} from 'lucide-react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { Tabs } from '../../../components/Tabs';
import { Select } from '../../../components/Select';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { runtimeIntelligenceApi, type DriftFindingItem, type ObservabilityScoreItem, type RuntimeSnapshot } from '../api/runtimeIntelligence';

// Cores de data-viz (recharts) — categóricas semânticas, exceção documentada.
const VIZ_HEALTHY = '#10b981';
const VIZ_DEGRADED = '#f59e0b';
const VIZ_UNHEALTHY = '#ef4444';

type TabId = 'health' | 'latency' | 'drift' | 'observability';

/**
 * RuntimeIntelligenceDashboardPage
 *
 * Dashboard principal de Runtime Intelligence que exibe:
 * - Saúde atual dos serviços (health status)
 * - Score de observabilidade por serviço
 * - Detecção de drift entre baseline e realidade
 * - Métricas agregadas (latência, throughput, error rate)
 * - Análise comparativa entre ambientes
 */
export function RuntimeIntelligenceDashboardPage() {
  const { t } = useTranslation();
  const [selectedService, setSelectedService] = useState<string>('all');
  const [selectedEnvironment, setSelectedEnvironment] = useState<string>('production');
  const [timeRange, setTimeRange] = useState<string>('24h');
  const [activeTab, setActiveTab] = useState<TabId>('health');

  // Buscar snapshots de runtime
  const { data: snapshots, isLoading: loadingSnapshots } = useQuery({
    queryKey: ['runtime-snapshots', selectedService, selectedEnvironment],
    queryFn: () => runtimeIntelligenceApi.getSnapshots(selectedService === 'all' ? undefined : selectedService, selectedEnvironment),
    refetchInterval: 30000, // Refetch a cada 30 segundos
  });

  // Buscar scores de observabilidade
  const { data: observabilityScores, isLoading: loadingScores } = useQuery({
    queryKey: ['observability-scores', selectedService, selectedEnvironment],
    queryFn: () => runtimeIntelligenceApi.getObservabilityScores(selectedService === 'all' ? undefined : selectedService, selectedEnvironment),
  });

  // Buscar drift findings
  const { data: driftFindings, isLoading: loadingDrifts } = useQuery({
    queryKey: ['drift-findings', selectedService, selectedEnvironment],
    queryFn: () => runtimeIntelligenceApi.getDriftFindings({
      serviceName: selectedService === 'all' ? undefined : selectedService,
      environment: selectedEnvironment,
    }),
  });

  // Calcular métricas agregadas
  const aggregatedMetrics = React.useMemo(() => {
    if (!snapshots?.items || snapshots.items.length === 0) {
      return null;
    }

    const totalSnapshots = snapshots.items.length;
    const healthyCount = snapshots.items.filter((s: RuntimeSnapshot) => s.healthStatus === 'Healthy').length;
    const degradedCount = snapshots.items.filter((s: RuntimeSnapshot) => s.healthStatus === 'Degraded').length;
    const unhealthyCount = snapshots.items.filter((s: RuntimeSnapshot) => s.healthStatus === 'Unhealthy').length;

    const avgLatency = snapshots.items.reduce((sum: number, s: RuntimeSnapshot) => sum + s.avgLatencyMs, 0) / totalSnapshots;
    const avgErrorRate = snapshots.items.reduce((sum: number, s: RuntimeSnapshot) => sum + s.errorRate, 0) / totalSnapshots;
    const avgThroughput = snapshots.items.reduce((sum: number, s: RuntimeSnapshot) => sum + s.requestsPerSecond, 0) / totalSnapshots;

    return {
      totalSnapshots,
      healthyCount,
      degradedCount,
      unhealthyCount,
      avgLatency: Math.round(avgLatency * 100) / 100,
      avgErrorRate: Math.round(avgErrorRate * 10000) / 100,
      avgThroughput: Math.round(avgThroughput * 100) / 100,
    };
  }, [snapshots]);

  // Dados para gráfico de saúde
  const healthDistributionData = React.useMemo(() => {
    if (!aggregatedMetrics) return [];

    return [
      { name: t('runtimeIntelligence.health.healthy'), value: aggregatedMetrics.healthyCount, color: VIZ_HEALTHY },
      { name: t('runtimeIntelligence.health.degraded'), value: aggregatedMetrics.degradedCount, color: VIZ_DEGRADED },
      { name: t('runtimeIntelligence.health.unhealthy'), value: aggregatedMetrics.unhealthyCount, color: VIZ_UNHEALTHY },
    ].filter((item) => item.value > 0);
  }, [aggregatedMetrics, t]);

  // Dados para gráfico de latência ao longo do tempo
  const latencyTrendData = React.useMemo(() => {
    if (!snapshots?.items) return [];

    return snapshots.items
      .slice(-20) // Últimos 20 snapshots
      .map((snapshot: RuntimeSnapshot) => ({
        time: new Date(snapshot.timestamp).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }),
        latency: snapshot.avgLatencyMs,
        p95: snapshot.p95LatencyMs || 0,
        p99: snapshot.p99LatencyMs || 0,
      }))
      .reverse();
  }, [snapshots]);

  // Handler para ingestão manual de snapshot
  const handleIngestSnapshot = async () => {
    try {
      await runtimeIntelligenceApi.ingestSnapshot({
        serviceName: selectedService === 'all' ? 'order-api' : selectedService,
        environment: selectedEnvironment,
        timestamp: new Date().toISOString(),
        avgLatencyMs: Math.random() * 100 + 50,
        p95LatencyMs: Math.random() * 150 + 100,
        p99LatencyMs: Math.random() * 200 + 150,
        errorRate: Math.random() * 0.05,
        requestsPerSecond: Math.random() * 1000 + 500,
        cpuUsagePercent: Math.random() * 60 + 20,
        memoryUsageMb: Math.random() * 512 + 256,
      });

      toast.success(t('runtimeIntelligence.toast.ingestSuccess'));
    } catch {
      // Erro tratado via toast - logging estruturado deve ser feito pelo backend
      toast.error(t('runtimeIntelligence.toast.ingestError'));
    }
  };

  const healthBadgeVariant = (status: string): 'success' | 'warning' | 'danger' | 'neutral' => {
    if (status === 'Healthy') return 'success';
    if (status === 'Degraded') return 'warning';
    if (status === 'Unhealthy') return 'danger';
    return 'neutral';
  };

  if (loadingSnapshots || loadingScores || loadingDrifts) {
    return (
      <PageContainer>
        <PageLoadingState />
      </PageContainer>
    );
  }

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-start sm:justify-between">
        <PageHeader
          title={t('runtimeIntelligence.title')}
          subtitle={t('runtimeIntelligence.subtitle')}
          icon={<Activity className="w-5 h-5" />}
        />
        <div className="flex gap-2 flex-wrap">
          <Button onClick={handleIngestSnapshot} variant="outline" size="sm">
            <Zap className="mr-2 h-4 w-4" />
            {t('runtimeIntelligence.actions.ingest')}
          </Button>
          <Button onClick={() => window.location.reload()} variant="outline" size="sm">
            <RefreshCw className="mr-2 h-4 w-4" />
            {t('runtimeIntelligence.actions.refresh')}
          </Button>
        </div>
      </div>

      {/* Filtros */}
      <PageSection>
        <Card>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Select
                size="sm"
                label={t('runtimeIntelligence.filters.service')}
                value={selectedService}
                onChange={(e) => setSelectedService(e.target.value)}
                options={[
                  { value: 'all', label: t('runtimeIntelligence.filters.allServices') },
                  { value: 'order-api', label: 'Order API' },
                  { value: 'payment-service', label: 'Payment Service' },
                  { value: 'user-service', label: 'User Service' },
                  { value: 'inventory-service', label: 'Inventory Service' },
                ]}
              />
              <Select
                size="sm"
                label={t('runtimeIntelligence.filters.environment')}
                value={selectedEnvironment}
                onChange={(e) => setSelectedEnvironment(e.target.value)}
                options={[
                  { value: 'production', label: t('runtimeIntelligence.env.production') },
                  { value: 'staging', label: t('runtimeIntelligence.env.staging') },
                  { value: 'development', label: t('runtimeIntelligence.env.development') },
                ]}
              />
              <Select
                size="sm"
                label={t('runtimeIntelligence.filters.period')}
                value={timeRange}
                onChange={(e) => setTimeRange(e.target.value)}
                options={[
                  { value: '1h', label: t('runtimeIntelligence.period.1h') },
                  { value: '6h', label: t('runtimeIntelligence.period.6h') },
                  { value: '24h', label: t('runtimeIntelligence.period.24h') },
                  { value: '7d', label: t('runtimeIntelligence.period.7d') },
                ]}
              />
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* KPIs Principais */}
      {aggregatedMetrics && (
        <PageSection>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Card>
              <CardBody>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-body">{t('runtimeIntelligence.kpi.avgLatency')}</span>
                  <Activity className="h-4 w-4 text-muted" />
                </div>
                <div className="text-2xl font-bold tabular-nums text-heading">{aggregatedMetrics.avgLatency} ms</div>
                <p className="text-xs text-muted">{t('runtimeIntelligence.kpi.avgLatencyHint')}</p>
              </CardBody>
            </Card>

            <Card>
              <CardBody>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-body">{t('runtimeIntelligence.kpi.errorRate')}</span>
                  <AlertTriangle className="h-4 w-4 text-muted" />
                </div>
                <div className="text-2xl font-bold tabular-nums text-heading">{aggregatedMetrics.avgErrorRate}%</div>
                <p className="text-xs text-muted">{t('runtimeIntelligence.kpi.errorRateHint')}</p>
              </CardBody>
            </Card>

            <Card>
              <CardBody>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-body">{t('runtimeIntelligence.kpi.throughput')}</span>
                  <Server className="h-4 w-4 text-muted" />
                </div>
                <div className="text-2xl font-bold tabular-nums text-heading">{aggregatedMetrics.avgThroughput} req/s</div>
                <p className="text-xs text-muted">{t('runtimeIntelligence.kpi.throughputHint')}</p>
              </CardBody>
            </Card>

            <Card>
              <CardBody>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-body">{t('runtimeIntelligence.kpi.healthyServices')}</span>
                  <CheckCircle2 className="h-4 w-4 text-muted" />
                </div>
                <div className="text-2xl font-bold tabular-nums text-heading">
                  {aggregatedMetrics.healthyCount}/{aggregatedMetrics.totalSnapshots}
                </div>
                <p className="text-xs text-muted">
                  {t('runtimeIntelligence.kpi.healthyHint', {
                    percent: Math.round((aggregatedMetrics.healthyCount / aggregatedMetrics.totalSnapshots) * 100),
                  })}
                </p>
              </CardBody>
            </Card>
          </div>
        </PageSection>
      )}

      {/* Gráficos Principais */}
      <PageSection>
        <Tabs
          className="mb-4"
          items={[
            { id: 'health', label: t('runtimeIntelligence.tabs.health') },
            { id: 'latency', label: t('runtimeIntelligence.tabs.latency') },
            { id: 'drift', label: t('runtimeIntelligence.tabs.drift') },
            { id: 'observability', label: t('runtimeIntelligence.tabs.observability') },
          ]}
          activeId={activeTab}
          onChange={(id) => setActiveTab(id as TabId)}
        />

        {activeTab === 'health' && (
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('runtimeIntelligence.health.title')}</h3>
              <p className="text-xs text-muted mt-1">{t('runtimeIntelligence.health.description')}</p>
            </CardHeader>
            <CardBody>
              <div className="h-[400px]">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={healthDistributionData}
                      cx="50%"
                      cy="50%"
                      labelLine={false}
                      label={({ name, percent }) => `${name}: ${percent ? (percent * 100).toFixed(0) : '0'}%`}
                      outerRadius={150}
                      fill="#8884d8"
                      dataKey="value"
                    >
                      {healthDistributionData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </CardBody>
          </Card>
        )}

        {activeTab === 'latency' && (
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('runtimeIntelligence.latency.title')}</h3>
              <p className="text-xs text-muted mt-1">{t('runtimeIntelligence.latency.description')}</p>
            </CardHeader>
            <CardBody>
              <div className="h-[400px]">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={latencyTrendData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="time" />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="latency" stroke={VIZ_HEALTHY} name={t('runtimeIntelligence.latency.avg')} strokeWidth={2} />
                    <Line type="monotone" dataKey="p95" stroke={VIZ_DEGRADED} name={t('runtimeIntelligence.latency.p95')} strokeWidth={2} />
                    <Line type="monotone" dataKey="p99" stroke={VIZ_UNHEALTHY} name={t('runtimeIntelligence.latency.p99')} strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </CardBody>
          </Card>
        )}

        {activeTab === 'drift' && (
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('runtimeIntelligence.drift.title')}</h3>
              <p className="text-xs text-muted mt-1">{t('runtimeIntelligence.drift.description')}</p>
            </CardHeader>
            <CardBody>
              {driftFindings?.items && driftFindings.items.length > 0 ? (
                <div className="space-y-3">
                  {driftFindings.items.slice(0, 10).map((finding: DriftFindingItem) => (
                    <div
                      key={finding.id}
                      className={`rounded-md border border-edge border-l-2 p-4 ${finding.severity === 'Critical' ? 'border-l-critical' : 'border-l-warning'}`}
                    >
                      <div className="flex items-center gap-2 mb-2">
                        <AlertTriangle className={`h-4 w-4 ${finding.severity === 'Critical' ? 'text-critical' : 'text-warning'}`} />
                        <span className="text-sm font-semibold text-heading">{finding.metricName}</span>
                      </div>
                      <div className="space-y-1 text-sm text-body">
                        <p><strong>{t('runtimeIntelligence.drift.service')}:</strong> {finding.serviceName}</p>
                        <p><strong>{t('runtimeIntelligence.drift.environment')}:</strong> {finding.environment}</p>
                        <p><strong>{t('runtimeIntelligence.drift.severity')}:</strong> {finding.severity}</p>
                        <p><strong>{t('runtimeIntelligence.drift.deviation')}:</strong> {finding.deviationPercent.toFixed(2)}%</p>
                        <p><strong>{t('runtimeIntelligence.drift.expected')}:</strong> {finding.expectedValue} | <strong>{t('runtimeIntelligence.drift.actual')}:</strong> {finding.actualValue}</p>
                        <p><strong>{t('runtimeIntelligence.drift.detectedAt')}:</strong> {new Date(finding.detectedAt).toLocaleString('pt-BR')}</p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-12">
                  <CheckCircle2 className="h-12 w-12 text-success mx-auto mb-4" />
                  <p className="text-lg font-medium text-heading">{t('runtimeIntelligence.drift.emptyTitle')}</p>
                  <p className="text-muted">{t('runtimeIntelligence.drift.emptyDescription')}</p>
                </div>
              )}
            </CardBody>
          </Card>
        )}

        {activeTab === 'observability' && (
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('runtimeIntelligence.observability.title')}</h3>
              <p className="text-xs text-muted mt-1">{t('runtimeIntelligence.observability.description')}</p>
            </CardHeader>
            <CardBody>
              {observabilityScores?.items && observabilityScores.items.length > 0 ? (
                <div className="space-y-3">
                  {observabilityScores.items.map((score: ObservabilityScoreItem) => (
                    <div key={score.serviceName} className="flex items-center justify-between p-4 border border-edge rounded-lg">
                      <div className="flex-1">
                        <h4 className="font-semibold text-heading">{score.serviceName}</h4>
                        <p className="text-sm text-muted">{score.environment}</p>
                      </div>
                      <div className="flex items-center gap-4">
                        <div className="text-right">
                          <div className={`text-2xl font-bold tabular-nums ${score.score >= 0.8 ? 'text-success' : score.score >= 0.6 ? 'text-warning' : 'text-critical'}`}>
                            {(score.score * 100).toFixed(0)}%
                          </div>
                          <p className="text-xs text-muted">{t('runtimeIntelligence.observability.score')}</p>
                        </div>
                        <Badge variant={score.hasCriticalDrift ? 'danger' : 'success'}>
                          {score.hasCriticalDrift ? t('runtimeIntelligence.observability.critical') : t('runtimeIntelligence.observability.normal')}
                        </Badge>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-12">
                  <BarChart3 className="h-12 w-12 text-muted mx-auto mb-4" />
                  <p className="text-lg font-medium text-heading">{t('runtimeIntelligence.observability.emptyTitle')}</p>
                  <p className="text-muted">{t('runtimeIntelligence.observability.emptyDescription')}</p>
                </div>
              )}
            </CardBody>
          </Card>
        )}
      </PageSection>

      {/* Tabela de Snapshots Recentes */}
      <PageSection>
        <Card>
          <CardHeader>
            <h3 className="text-sm font-semibold text-heading">{t('runtimeIntelligence.snapshots.title')}</h3>
            <p className="text-xs text-muted mt-1">{t('runtimeIntelligence.snapshots.description')}</p>
          </CardHeader>
          <CardBody className="p-0">
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge bg-muted/40 text-xs text-muted">
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.service')}</th>
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.environment')}</th>
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.health')}</th>
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.latency')}</th>
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.errorRate')}</th>
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.throughput')}</th>
                    <th className="text-left px-4 py-2.5 font-medium">{t('runtimeIntelligence.snapshots.timestamp')}</th>
                  </tr>
                </thead>
                <tbody>
                  {snapshots?.items?.slice(0, 10).map((snapshot: RuntimeSnapshot) => (
                    <tr key={snapshot.id} className="border-b border-edge/50 hover:bg-muted/30 transition-colors">
                      <td className="px-4 py-2.5 font-medium text-heading">{snapshot.serviceName}</td>
                      <td className="px-4 py-2.5">
                        <Badge variant="neutral">{snapshot.environment}</Badge>
                      </td>
                      <td className="px-4 py-2.5">
                        <Badge variant={healthBadgeVariant(snapshot.healthStatus)}>{snapshot.healthStatus}</Badge>
                      </td>
                      <td className="px-4 py-2.5 tabular-nums">{snapshot.avgLatencyMs.toFixed(2)}</td>
                      <td className="px-4 py-2.5 tabular-nums">{(snapshot.errorRate * 100).toFixed(2)}%</td>
                      <td className="px-4 py-2.5 tabular-nums">{snapshot.requestsPerSecond.toFixed(0)} req/s</td>
                      <td className="px-4 py-2.5 text-sm text-muted">
                        {new Date(snapshot.timestamp).toLocaleString('pt-BR')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
