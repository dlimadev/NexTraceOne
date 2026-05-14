import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { 
  Activity, 
  AlertTriangle, 
  CheckCircle,
  Server,
  Zap,
  BarChart3,
  RefreshCw
} from 'lucide-react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
import { runtimeIntelligenceApi, type DriftFindingItem, type ObservabilityScoreItem, type RuntimeSnapshot } from '@/features/operations/api/runtimeIntelligence';

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
  const [selectedService, setSelectedService] = useState<string>('all');
  const [selectedEnvironment, setSelectedEnvironment] = useState<string>('production');
  const [timeRange, setTimeRange] = useState<string>('24h');

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
      environment: selectedEnvironment
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
      { name: 'Saudável', value: aggregatedMetrics.healthyCount, color: '#10b981' },
      { name: 'Degradado', value: aggregatedMetrics.degradedCount, color: '#f59e0b' },
      { name: 'Não Saudável', value: aggregatedMetrics.unhealthyCount, color: '#ef4444' },
    ].filter(item => item.value > 0);
  }, [aggregatedMetrics]);

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
      
      toast.success('Snapshot ingerido com sucesso');
    } catch {
      // Erro tratado via toast - logging estruturado deve ser feito pelo backend
      toast.error('Falha ao ingerir snapshot');
    }
  };

  if (loadingSnapshots || loadingScores || loadingDrifts) {
    return (
      <div className="flex items-center justify-center h-96">
        <RefreshCw className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Runtime Intelligence</h1>
          <p className="text-muted-foreground">
            Monitoramento em tempo real da saúde e performance dos serviços
          </p>
        </div>
        <div className="flex gap-2">
          <Button onClick={handleIngestSnapshot} variant="outline">
            <Zap className="mr-2 h-4 w-4" />
            Ingerir Snapshot
          </Button>
          <Button onClick={() => window.location.reload()} variant="outline">
            <RefreshCw className="mr-2 h-4 w-4" />
            Atualizar
          </Button>
        </div>
      </div>

      {/* Filtros */}
      <Card>
        <CardContent className="pt-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Serviço</label>
              <Select value={selectedService} onValueChange={setSelectedService}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecionar serviço" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Todos os Serviços</SelectItem>
                  <SelectItem value="order-api">Order API</SelectItem>
                  <SelectItem value="payment-service">Payment Service</SelectItem>
                  <SelectItem value="user-service">User Service</SelectItem>
                  <SelectItem value="inventory-service">Inventory Service</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Ambiente</label>
              <Select value={selectedEnvironment} onValueChange={setSelectedEnvironment}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecionar ambiente" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="production">Produção</SelectItem>
                  <SelectItem value="staging">Staging</SelectItem>
                  <SelectItem value="development">Desenvolvimento</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">Período</label>
              <Select value={timeRange} onValueChange={setTimeRange}>
                <SelectTrigger>
                  <SelectValue placeholder="Selecionar período" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1h">Última Hora</SelectItem>
                  <SelectItem value="6h">Últimas 6 Horas</SelectItem>
                  <SelectItem value="24h">Últimas 24 Horas</SelectItem>
                  <SelectItem value="7d">Últimos 7 Dias</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* KPIs Principais */}
      {aggregatedMetrics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Latência Média</CardTitle>
              <Activity className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{aggregatedMetrics.avgLatency} ms</div>
              <p className="text-xs text-muted-foreground">
                Tempo médio de resposta
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Taxa de Erro</CardTitle>
              <AlertTriangle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{aggregatedMetrics.avgErrorRate}%</div>
              <p className="text-xs text-muted-foreground">
                Requisições com falha
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Throughput</CardTitle>
              <Server className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{aggregatedMetrics.avgThroughput} req/s</div>
              <p className="text-xs text-muted-foreground">
                Requisições por segundo
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Serviços Saudáveis</CardTitle>
              <CheckCircle className="h-4 w-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {aggregatedMetrics.healthyCount}/{aggregatedMetrics.totalSnapshots}
              </div>
              <p className="text-xs text-muted-foreground">
                {Math.round((aggregatedMetrics.healthyCount / aggregatedMetrics.totalSnapshots) * 100)}% saudáveis
              </p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Gráficos Principais */}
      <Tabs defaultValue="health" className="space-y-4">
        <TabsList>
          <TabsTrigger value="health">Distribuição de Saúde</TabsTrigger>
          <TabsTrigger value="latency">Tendência de Latência</TabsTrigger>
          <TabsTrigger value="drift">Detecção de Drift</TabsTrigger>
          <TabsTrigger value="observability">Score de Observabilidade</TabsTrigger>
        </TabsList>

        <TabsContent value="health" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Distribuição de Saúde dos Serviços</CardTitle>
              <CardDescription>
                Visualização da saúde atual dos serviços monitorados
              </CardDescription>
            </CardHeader>
            <CardContent>
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
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="latency" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Tendência de Latência</CardTitle>
              <CardDescription>
                Evolução da latência média, p95 e p99 ao longo do tempo
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="h-[400px]">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={latencyTrendData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="time" />
                    <YAxis />
                    <Tooltip />
                    <Legend />
                    <Line type="monotone" dataKey="latency" stroke="#10b981" name="Média" strokeWidth={2} />
                    <Line type="monotone" dataKey="p95" stroke="#f59e0b" name="P95" strokeWidth={2} />
                    <Line type="monotone" dataKey="p99" stroke="#ef4444" name="P99" strokeWidth={2} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="drift" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Drift Findings Detectados</CardTitle>
              <CardDescription>
                Desvios identificados entre baseline esperada e comportamento real
              </CardDescription>
            </CardHeader>
            <CardContent>
              {driftFindings?.items && driftFindings.items.length > 0 ? (
                <div className="space-y-4">
                  {driftFindings.items.slice(0, 10).map((finding: DriftFindingItem) => (
                    <Alert key={finding.id} variant={finding.severity === 'Critical' ? 'destructive' : 'default'}>
                      <AlertTriangle className="h-4 w-4" />
                      <AlertTitle>{finding.metricName}</AlertTitle>
                      <AlertDescription>
                        <div className="mt-2 space-y-1">
                          <p><strong>Serviço:</strong> {finding.serviceName}</p>
                          <p><strong>Ambiente:</strong> {finding.environment}</p>
                          <p><strong>Severidade:</strong> {finding.severity}</p>
                          <p><strong>Desvio:</strong> {finding.deviationPercent.toFixed(2)}%</p>
                          <p><strong>Esperado:</strong> {finding.expectedValue} | <strong>Atual:</strong> {finding.actualValue}</p>
                          <p><strong>Detectado em:</strong> {new Date(finding.detectedAt).toLocaleString('pt-BR')}</p>
                        </div>
                      </AlertDescription>
                    </Alert>
                  ))}
                </div>
              ) : (
                <div className="text-center py-12">
                  <CheckCircle className="h-12 w-12 text-green-500 mx-auto mb-4" />
                  <p className="text-lg font-medium">Nenhum drift detectado</p>
                  <p className="text-muted-foreground">Todos os serviços estão dentro da baseline esperada</p>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="observability" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Score de Observabilidade</CardTitle>
              <CardDescription>
                Maturidade de observabilidade por serviço (0-100%)
              </CardDescription>
            </CardHeader>
            <CardContent>
              {observabilityScores?.items && observabilityScores.items.length > 0 ? (
                <div className="space-y-4">
                  {observabilityScores.items.map((score: ObservabilityScoreItem) => (
                    <div key={score.serviceName} className="flex items-center justify-between p-4 border rounded-lg">
                      <div className="flex-1">
                        <h4 className="font-semibold">{score.serviceName}</h4>
                        <p className="text-sm text-muted-foreground">{score.environment}</p>
                      </div>
                      <div className="flex items-center gap-4">
                        <div className="text-right">
                          <div className="text-2xl font-bold" style={{ color: score.score >= 80 ? '#10b981' : score.score >= 60 ? '#f59e0b' : '#ef4444' }}>
                            {(score.score * 100).toFixed(0)}%
                          </div>
                          <p className="text-xs text-muted-foreground">Score</p>
                        </div>
                        <Badge variant={score.hasCriticalDrift ? 'destructive' : 'default'}>
                          {score.hasCriticalDrift ? 'Crítico' : 'Normal'}
                        </Badge>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-12">
                  <BarChart3 className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <p className="text-lg font-medium">Nenhum score disponível</p>
                  <p className="text-muted-foreground">Configure perfis de observabilidade para os serviços</p>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Tabela de Snapshots Recentes */}
      <Card>
        <CardHeader>
          <CardTitle>Snapshots Recentes</CardTitle>
          <CardDescription>
            Últimos snapshots de saúde capturados
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border">
            <table className="w-full">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="text-left p-3 font-medium">Serviço</th>
                  <th className="text-left p-3 font-medium">Ambiente</th>
                  <th className="text-left p-3 font-medium">Saúde</th>
                  <th className="text-left p-3 font-medium">Latência (ms)</th>
                  <th className="text-left p-3 font-medium">Error Rate</th>
                  <th className="text-left p-3 font-medium">Throughput</th>
                  <th className="text-left p-3 font-medium">Timestamp</th>
                </tr>
              </thead>
              <tbody>
                {snapshots?.items?.slice(0, 10).map((snapshot: RuntimeSnapshot) => (
                  <tr key={snapshot.id} className="border-b hover:bg-muted/50">
                    <td className="p-3">{snapshot.serviceName}</td>
                    <td className="p-3">
                      <Badge variant="outline">{snapshot.environment}</Badge>
                    </td>
                    <td className="p-3">
                      <Badge 
                        variant={
                          snapshot.healthStatus === 'Healthy' ? 'default' :
                          snapshot.healthStatus === 'Degraded' ? 'secondary' : 'destructive'
                        }
                      >
                        {snapshot.healthStatus}
                      </Badge>
                    </td>
                    <td className="p-3">{snapshot.avgLatencyMs.toFixed(2)}</td>
                    <td className="p-3">{(snapshot.errorRate * 100).toFixed(2)}%</td>
                    <td className="p-3">{snapshot.requestsPerSecond.toFixed(0)} req/s</td>
                    <td className="p-3 text-sm text-muted-foreground">
                      {new Date(snapshot.timestamp).toLocaleString('pt-BR')}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
