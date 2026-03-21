import { useState, useMemo, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Plus,
  RefreshCw,
  Server,
  Globe,
  Search,
  Zap,
  Clock,
  ChevronRight,
  AlertTriangle,
  ArrowRight,
  Layers,
  GitBranch,
  Shield,
  X,
  Activity,
  TrendingUp,
  Timer,
  BarChart3,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { serviceCatalogApi } from '../api';
import { ServiceCatalogOverviewTab } from '../components/ServiceCatalogOverviewTab';
import { ServiceCatalogServicesTab } from '../components/ServiceCatalogServicesTab';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type {
  AssetGraph,
  ApiNode,
  ServiceNode,
  ImpactPropagationResult,
  GraphSnapshotSummary,
  TemporalDiffResult,
  NodeHealthResult,
} from '../../../types';

/** Mapeamento de confiança para variantes visuais de badge. */
const confidenceVariant = (score: number): 'default' | 'success' | 'warning' | 'danger' | 'info' => {
  if (score >= 0.9) return 'success';
  if (score >= 0.7) return 'info';
  if (score >= 0.4) return 'warning';
  return 'danger';
};

/** Cores dos nós do grafo por tipo. */
const nodeColors = {
  service: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-700 dark:text-blue-300', border: 'border-blue-300 dark:border-blue-700' },
  api: { bg: 'bg-emerald-100 dark:bg-emerald-900/30', text: 'text-emerald-700 dark:text-emerald-300', border: 'border-emerald-300 dark:border-emerald-700' },
};

type Tab = 'overview' | 'services' | 'apis' | 'graph' | 'impact' | 'temporal';

export function ServiceCatalogPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('overview');
  const [selectedDetailNode, setSelectedDetailNode] = useState<string | null>(null);
  const [showServiceForm, setShowServiceForm] = useState(false);
  const [showApiForm, setShowApiForm] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [impactDepth, setImpactDepth] = useState(3);
  const [selectedFromSnapshot, setSelectedFromSnapshot] = useState<string>('');
  const [selectedToSnapshot, setSelectedToSnapshot] = useState<string>('');

  const [serviceForm, setServiceForm] = useState({ name: '', team: '', description: '' });
  const [apiForm, setApiForm] = useState({ name: '', baseUrl: '', ownerServiceId: '', description: '' });

  // ── Queries principais ──────────────────────────────────────────────
  const { data: graph, isLoading, isError: isGraphError } = useQuery({
    queryKey: ['graph'],
    queryFn: () => serviceCatalogApi.getGraph(),
    staleTime: 30_000,
  });

  const { data: impactResult, isLoading: impactLoading } = useQuery({
    queryKey: ['impact', selectedNodeId, impactDepth],
    queryFn: () => serviceCatalogApi.getImpactPropagation(selectedNodeId!, impactDepth),
    enabled: !!selectedNodeId && tab === 'impact',
    staleTime: 15_000,
  });

  const { data: snapshotsData } = useQuery({
    queryKey: ['snapshots'],
    queryFn: () => serviceCatalogApi.listSnapshots(20),
    enabled: tab === 'temporal',
    staleTime: 60_000,
  });

  const { data: diffResult, isLoading: diffLoading } = useQuery({
    queryKey: ['temporal-diff', selectedFromSnapshot, selectedToSnapshot],
    queryFn: () => serviceCatalogApi.getTemporalDiff(selectedFromSnapshot, selectedToSnapshot),
    enabled: !!selectedFromSnapshot && !!selectedToSnapshot && selectedFromSnapshot !== selectedToSnapshot,
    staleTime: 30_000,
  });

  const { data: healthData } = useQuery({
    queryKey: ['node-health'],
    queryFn: () => serviceCatalogApi.getNodeHealth('Health'),
    staleTime: 30_000,
  });

  // ── Mutations ───────────────────────────────────────────────────────
  const registerService = useMutation({
    mutationFn: serviceCatalogApi.registerService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['graph'] });
      setShowServiceForm(false);
      setServiceForm({ name: '', team: '', description: '' });
    },
  });

  const registerApi = useMutation({
    mutationFn: serviceCatalogApi.registerApi,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['graph'] });
      setShowApiForm(false);
      setApiForm({ name: '', baseUrl: '', ownerServiceId: '', description: '' });
    },
  });

  const createSnapshot = useMutation({
    mutationFn: (label: string) => serviceCatalogApi.createSnapshot(label),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['snapshots'] });
    },
  });

  // ── Dados derivados ─────────────────────────────────────────────────
  const filteredServices = useMemo(() => {
    if (!graph?.services) return [];
    if (!searchTerm) return graph.services;
    const term = searchTerm.toLowerCase();
    return graph.services.filter(
      (s) => s.name.toLowerCase().includes(term) || s.teamName.toLowerCase().includes(term) || s.domain.toLowerCase().includes(term)
    );
  }, [graph?.services, searchTerm]);

  const filteredApis = useMemo(() => {
    if (!graph?.apis) return [];
    if (!searchTerm) return graph.apis;
    const term = searchTerm.toLowerCase();
    return graph.apis.filter(
      (a) => a.name.toLowerCase().includes(term) || a.routePattern.toLowerCase().includes(term)
    );
  }, [graph?.apis, searchTerm]);

  const graphStats = useMemo(() => {
    if (!graph) return { services: 0, apis: 0, edges: 0, domains: 0 };
    const domains = new Set(graph.services.map((s) => s.domain)).size;
    const edges = graph.apis.reduce((sum, a) => sum + (a.consumers?.length ?? 0), 0);
    return { services: graph.services.length, apis: graph.apis.length, edges, domains };
  }, [graph]);

  const selectNodeForImpact = useCallback((nodeId: string) => {
    setSelectedNodeId(nodeId);
    setTab('impact');
  }, []);

  const snapshots: GraphSnapshotSummary[] = snapshotsData?.items ?? [];

  const tabs: { key: Tab; label: string; icon: React.ReactNode }[] = [
    { key: 'overview', label: t('serviceCatalog.tabs.overview'), icon: <BarChart3 size={14} /> },
    { key: 'graph', label: t('serviceCatalog.tabs.graph'), icon: <GitBranch size={14} /> },
    { key: 'services', label: t('serviceCatalog.tabs.services'), icon: <Server size={14} /> },
    { key: 'apis', label: t('serviceCatalog.tabs.apis'), icon: <Globe size={14} /> },
    { key: 'impact', label: t('serviceCatalog.tabs.impact'), icon: <Zap size={14} /> },
    { key: 'temporal', label: t('serviceCatalog.tabs.temporal'), icon: <Clock size={14} /> },
  ];

  return (
    <PageContainer>
      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="services" />

      <PageHeader
        title={t('serviceCatalog.title')}
        subtitle={t('serviceCatalog.subtitle')}
        actions={
          <div className="flex gap-2">
            <Button variant="secondary" onClick={() => setShowServiceForm((v) => !v)}>
              <Plus size={16} /> {t('serviceCatalog.registerService')}
            </Button>
            <Button onClick={() => setShowApiForm((v) => !v)}>
              <Plus size={16} /> {t('serviceCatalog.registerApi')}
            </Button>
          </div>
        }
      />

      {/* ── Estatísticas resumidas ──────────────────────────────────── */}
      <StatsGrid columns={4}>
        {[
          { label: t('serviceCatalog.stats.services'), value: graphStats.services, icon: <Server size={18} />, color: 'text-blue-500' },
          { label: t('serviceCatalog.stats.apis'), value: graphStats.apis, icon: <Globe size={18} />, color: 'text-emerald-500' },
          { label: t('serviceCatalog.stats.edges'), value: graphStats.edges, icon: <GitBranch size={18} />, color: 'text-purple-500' },
          { label: t('serviceCatalog.stats.domains'), value: graphStats.domains, icon: <Layers size={18} />, color: 'text-amber-500' },
        ].map((stat) => (
          <Card key={stat.label}>
            <CardBody className="flex items-center gap-3">
              <div className={`${stat.color}`}>{stat.icon}</div>
              <div>
                <p className="text-2xl font-bold text-heading">{stat.value}</p>
                <p className="text-xs text-muted">{stat.label}</p>
              </div>
            </CardBody>
          </Card>
        ))}
      </StatsGrid>

      {/* ── Formulário de registro de serviço ───────────────────────── */}
      {showServiceForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('serviceCatalog.registerServiceTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); registerService.mutate(serviceForm); }}
              className="grid grid-cols-3 gap-4"
            >
              {([
                { field: 'name' as const, key: 'serviceCatalog.name' },
                { field: 'team' as const, key: 'serviceCatalog.team' },
                { field: 'description' as const, key: 'serviceCatalog.description' },
              ]).map(({ field, key }) => (
                <div key={field}>
                  <label className="block text-sm font-medium text-body mb-1">{t(key)}</label>
                  <input
                    type="text"
                    value={serviceForm[field]}
                    onChange={(e) => setServiceForm((f) => ({ ...f, [field]: e.target.value }))}
                    required={field !== 'description'}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
              ))}
              <div className="col-span-3 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowServiceForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={registerService.isPending}>{t('serviceCatalog.register')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* ── Formulário de registro de API ────────────────────────────── */}
      {showApiForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('serviceCatalog.registerApiTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); registerApi.mutate(apiForm); }}
              className="grid grid-cols-2 gap-4"
            >
              {([
                { field: 'name' as const, label: t('serviceCatalog.name') },
                { field: 'baseUrl' as const, label: t('serviceCatalog.baseUrl') },
                { field: 'ownerServiceId' as const, label: t('serviceCatalog.ownerServiceId') },
                { field: 'description' as const, label: t('serviceCatalog.description') },
              ]).map(({ field, label }) => (
                <div key={field}>
                  <label className="block text-sm font-medium text-body mb-1">{label}</label>
                  <input
                    type="text"
                    value={apiForm[field]}
                    onChange={(e) => setApiForm((f) => ({ ...f, [field]: e.target.value }))}
                    required={field !== 'description'}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
              ))}
              <div className="col-span-2 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowApiForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={registerApi.isPending}>{t('serviceCatalog.register')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* ── Busca global ────────────────────────────────────────────── */}
      <div className="flex items-center gap-4 mb-4">
        <div className="relative flex-1 max-w-md">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            placeholder={t('serviceCatalog.searchPlaceholder')}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-9 pr-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
          />
          {searchTerm && (
            <button onClick={() => setSearchTerm('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-muted hover:text-body">
              <X size={14} />
            </button>
          )}
        </div>

        {/* ── Abas de navegação ──────────────────────────────────────── */}
        <div className="flex gap-1 bg-elevated rounded-lg p-1">
          {tabs.map((tabItem) => (
            <button
              key={tabItem.key}
              onClick={() => setTab(tabItem.key)}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                tab === tabItem.key ? 'bg-card shadow text-heading' : 'text-muted hover:text-body'
              }`}
            >
              {tabItem.icon} {tabItem.label}
            </button>
          ))}
        </div>
      </div>

      {/* ── Conteúdo das abas ──────────────────────────────────────── */}
      {isGraphError ? (
        <PageErrorState />
      ) : isLoading ? (
        <PageLoadingState />
      ) : (
        <>
          {/* ── Aba: Visão Operacional ──────────────────────────────── */}
          {tab === 'overview' && (
            <ServiceCatalogOverviewTab
              graph={graph}
              healthData={healthData}
              onSelectNode={(id) => { selectNodeForImpact(id); setSelectedDetailNode(id); }}
            />
          )}

          {/* ── Aba: Grafo Visual ──────────────────────────────────── */}
          {tab === 'graph' && graph && (
            <div className="relative">
              <GraphVisualization graph={graph} onSelectNode={(id) => { selectNodeForImpact(id); setSelectedDetailNode(id); }} />
              {selectedDetailNode && (
                <ServiceDetailPanel
                  graph={graph}
                  nodeId={selectedDetailNode}
                  healthData={healthData ?? null}
                  onClose={() => setSelectedDetailNode(null)}
                />
              )}
            </div>
          )}

          {/* ── Aba: Serviços ──────────────────────────────────────── */}
          {tab === 'services' && (
            <ServiceCatalogServicesTab
              filteredServices={filteredServices}
              onSelectNode={selectNodeForImpact}
            />
          )}

          {/* ── Aba: APIs ──────────────────────────────────────────── */}
          {tab === 'apis' && (
            <Card>
              <CardBody className="p-0">
                {!filteredApis.length ? (
                  <p className="px-6 py-12 text-sm text-muted text-center">{t('serviceCatalog.noApis')}</p>
                ) : (
                  <ul className="divide-y divide-edge">
                    {filteredApis.map((api) => (
                      <li key={api.apiAssetId} className="px-6 py-4 flex items-center gap-4 hover:bg-hover transition-colors cursor-pointer" role="button" tabIndex={0} onClick={() => selectNodeForImpact(api.apiAssetId)} onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); selectNodeForImpact(api.apiAssetId); } }}>
                        <div className="w-10 h-10 rounded-lg bg-info/15 flex items-center justify-center text-info">
                          <Globe size={18} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-medium text-heading">{api.name}</p>
                          <p className="text-xs text-muted font-mono">{api.routePattern}</p>
                        </div>
                        <div className="text-right">
                          <Badge variant={api.visibility === 'Public' ? 'success' : 'default'}>{api.visibility}</Badge>
                          <p className="text-xs text-muted mt-1">v{api.version} · {api.consumers?.length ?? 0} {t('serviceCatalog.consumers')}</p>
                        </div>
                        <ChevronRight size={16} className="text-muted" />
                      </li>
                    ))}
                  </ul>
                )}
              </CardBody>
            </Card>
          )}

          {/* ── Aba: Propagação de Impacto ──────────────────────────── */}
          {tab === 'impact' && (
            <ImpactPanel
              graph={graph}
              selectedNodeId={selectedNodeId}
              impactResult={impactResult ?? null}
              impactLoading={impactLoading}
              impactDepth={impactDepth}
              onSelectNode={setSelectedNodeId}
              onChangeDepth={setImpactDepth}
            />
          )}

          {/* ── Aba: Diff Temporal ──────────────────────────────────── */}
          {tab === 'temporal' && (
            <TemporalPanel
              snapshots={snapshots}
              selectedFrom={selectedFromSnapshot}
              selectedTo={selectedToSnapshot}
              diffResult={diffResult ?? null}
              diffLoading={diffLoading}
              onSelectFrom={setSelectedFromSnapshot}
              onSelectTo={setSelectedToSnapshot}
              onCreateSnapshot={() => {
                const timestamp = new Date().toISOString().slice(0, 16); // YYYY-MM-DDTHH:mm
                createSnapshot.mutate(`Snapshot ${timestamp}`);
              }}
              createSnapshotPending={createSnapshot.isPending}
            />
          )}
        </>
      )}
    </PageContainer>
  );
}

// ═══════════════════════════════════════════════════════════════════════
// Componente: Visualização do grafo em layout visual
// ═══════════════════════════════════════════════════════════════════════

/** Componente de visualização do grafo como diagrama de dependências. */
function GraphVisualization({ graph, onSelectNode }: { graph: AssetGraph; onSelectNode: (id: string) => void }) {
  const { t } = useTranslation();

  if (!graph.services.length && !graph.apis.length) {
    return (
      <Card>
        <CardBody className="py-12 text-center">
          <GitBranch size={40} className="mx-auto text-muted mb-4" />
          <p className="text-muted">{t('serviceCatalog.emptyGraph')}</p>
        </CardBody>
      </Card>
    );
  }

  /** Agrupa APIs pelo serviço proprietário para layout hierárquico. */
  const serviceApiMap = new Map<string, { service: ServiceNode; apis: ApiNode[] }>();
  for (const svc of graph.services) {
    serviceApiMap.set(svc.serviceAssetId, { service: svc, apis: [] });
  }
  for (const api of graph.apis) {
    const entry = serviceApiMap.get(api.ownerServiceAssetId);
    if (entry) entry.apis.push(api);
  }

  /** Coleta todas as arestas de consumo cross-service para exibição na tabela. */
  const crossServiceEdges: { fromApi: string; toConsumer: string; confidence: number }[] = [];
  for (const api of graph.apis) {
    for (const consumer of api.consumers ?? []) {
      crossServiceEdges.push({
        fromApi: api.apiAssetId,
        toConsumer: consumer.consumerName,
        confidence: consumer.confidenceScore,
      });
    }
  }

  return (
    <div className="space-y-4">
      {/* ── Legenda ───────────────────────────────────────────── */}
      <Card>
        <CardBody className="flex items-center gap-6 py-2">
          <span className="text-xs font-medium text-muted">{t('serviceCatalog.legend')}:</span>
          <span className="flex items-center gap-1.5 text-xs">
            <span className={`w-3 h-3 rounded ${nodeColors.service.bg} ${nodeColors.service.border} border`} />
            {t('serviceCatalog.legendService')}
          </span>
          <span className="flex items-center gap-1.5 text-xs">
            <span className={`w-3 h-3 rounded ${nodeColors.api.bg} ${nodeColors.api.border} border`} />
            {t('serviceCatalog.legendApi')}
          </span>
          <span className="flex items-center gap-1.5 text-xs">
            <ArrowRight size={12} className="text-muted" />
            {t('serviceCatalog.legendDependency')}
          </span>
        </CardBody>
      </Card>

      {/* ── Grafo por domínio (agrupado por serviço) ──────────── */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        {Array.from(serviceApiMap.values()).map(({ service, apis }) => (
          <Card key={service.serviceAssetId} className="overflow-hidden">
            <CardHeader className={`${nodeColors.service.bg} cursor-pointer`} role="button" tabIndex={0} onClick={() => onSelectNode(service.serviceAssetId)} onKeyDown={(e: React.KeyboardEvent) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSelectNode(service.serviceAssetId); } }}>
              <div className="flex items-center gap-2">
                <Server size={16} className={nodeColors.service.text} />
                <div>
                  <p className={`font-semibold text-sm ${nodeColors.service.text}`}>{service.name}</p>
                  <p className="text-xs text-muted">{service.teamName} · {service.domain}</p>
                </div>
              </div>
            </CardHeader>
            <CardBody className="p-3 space-y-2">
              {apis.length === 0 ? (
                <p className="text-xs text-muted italic">{t('serviceCatalog.noApisForService')}</p>
              ) : (
                apis.map((api) => (
                  <div
                    key={api.apiAssetId}
                    className={`${nodeColors.api.bg} rounded-md p-2 border ${nodeColors.api.border} cursor-pointer hover:shadow-sm transition-shadow`}
                    onClick={() => onSelectNode(api.apiAssetId)}
                  >
                    <div className="flex items-center justify-between">
                      <span className={`text-xs font-medium ${nodeColors.api.text}`}>{api.name}</span>
                      <Badge variant="default">v{api.version}</Badge>
                    </div>
                    <p className="text-xs text-muted font-mono mt-0.5">{api.routePattern}</p>
                    {(api.consumers?.length ?? 0) > 0 && (
                      <div className="mt-1.5 flex flex-wrap gap-1">
                        {api.consumers.map((c) => (
                          <span
                            key={c.relationshipId}
                            className="inline-flex items-center gap-1 text-xs bg-purple-50 dark:bg-purple-900/20 text-purple-700 dark:text-purple-300 rounded px-1.5 py-0.5"
                          >
                            <ArrowRight size={10} />
                            {c.consumerName}
                            <span className="text-[10px] opacity-60">({Math.round(c.confidenceScore * 100)}%)</span>
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                ))
              )}
            </CardBody>
          </Card>
        ))}
      </div>

      {/* ── Resumo de relações cross-service ──────────────────── */}
      {crossServiceEdges.length > 0 && (
        <Card>
          <CardHeader>
            <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.crossServiceDependencies')}</h3>
          </CardHeader>
          <CardBody className="p-0">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.sourceApi')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.consumerService')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.confidence')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {crossServiceEdges.slice(0, 20).map((edge) => (
                  <tr key={`${edge.fromApi}-${edge.toConsumer}`} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-2 font-mono text-xs text-body">{edge.fromApi.slice(0, 8)}…</td>
                    <td className="px-4 py-2 text-body">{edge.toConsumer}</td>
                    <td className="px-4 py-2">
                      <Badge variant={confidenceVariant(edge.confidence)}>
                        {Math.round(edge.confidence * 100)}%
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════════
// Componente: Painel de propagação de impacto (Blast Radius)
// ═══════════════════════════════════════════════════════════════════════

interface ImpactPanelProps {
  graph: AssetGraph | undefined;
  selectedNodeId: string | null;
  impactResult: ImpactPropagationResult | null;
  impactLoading: boolean;
  impactDepth: number;
  onSelectNode: (id: string) => void;
  onChangeDepth: (depth: number) => void;
}

/** Painel de análise de propagação de impacto (blast radius). */
function ImpactPanel({ graph, selectedNodeId, impactResult, impactLoading, impactDepth, onSelectNode, onChangeDepth }: ImpactPanelProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      {/* ── Seletor de nó e profundidade ────────────────────── */}
      <Card>
        <CardBody>
          <div className="flex items-end gap-4">
            <div className="flex-1">
              <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.impact.selectNode')}</label>
              <select
                value={selectedNodeId ?? ''}
                onChange={(e) => onSelectNode(e.target.value)}
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
              >
                <option value="">{t('serviceCatalog.impact.selectNodePlaceholder')}</option>
                {graph?.apis?.map((api) => (
                  <option key={api.apiAssetId} value={api.apiAssetId}>
                    API: {api.name} ({api.routePattern})
                  </option>
                ))}
                {graph?.services?.map((svc) => (
                  <option key={svc.serviceAssetId} value={svc.serviceAssetId}>
                    Service: {svc.name} ({svc.domain})
                  </option>
                ))}
              </select>
            </div>
            <div className="w-32">
              <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.impact.maxDepth')}</label>
              <select
                value={impactDepth}
                onChange={(e) => onChangeDepth(Number(e.target.value))}
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
              >
                {[1, 2, 3, 4, 5].map((d) => (
                  <option key={d} value={d}>{d}</option>
                ))}
              </select>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* ── Estado sem seleção ───────────────────────────────── */}
      {!selectedNodeId && (
        <Card>
          <CardBody className="py-12 text-center">
            <Zap size={40} className="mx-auto text-muted mb-4" />
            <p className="text-muted">{t('serviceCatalog.impact.selectNodeHint')}</p>
          </CardBody>
        </Card>
      )}

      {/* ── Carregando ──────────────────────────────────────── */}
      {selectedNodeId && impactLoading && (
        <div className="flex items-center justify-center py-12">
          <RefreshCw size={20} className="animate-spin text-muted" />
          <span className="ml-2 text-muted">{t('serviceCatalog.impact.calculating')}</span>
        </div>
      )}

      {/* ── Resultado do impacto ────────────────────────────── */}
      {impactResult && (
        <>
          <div className="grid grid-cols-3 gap-4">
            <Card>
              <CardBody className="text-center">
                <p className="text-3xl font-bold text-heading">{impactResult.directCount + impactResult.transitiveCount}</p>
                <p className="text-xs text-muted">{t('serviceCatalog.impact.totalImpacted')}</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <p className="text-3xl font-bold text-amber-600">{impactResult.directCount}</p>
                <p className="text-xs text-muted">{t('serviceCatalog.impact.directConsumers')}</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <p className="text-3xl font-bold text-red-600">{impactResult.transitiveCount}</p>
                <p className="text-xs text-muted">{t('serviceCatalog.impact.transitiveConsumers')}</p>
              </CardBody>
            </Card>
          </div>

          {impactResult.impactedNodes.length > 0 && (
            <Card>
              <CardHeader>
                <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.impact.impactedNodes')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.impact.node')}</th>
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.impact.depth')}</th>
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.confidence')}</th>
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.impact.path')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {impactResult.impactedNodes.map((node) => (
                      <tr key={node.nodeId} className="hover:bg-hover transition-colors">
                        <td className="px-4 py-2 text-body font-medium">
                          <div className="flex items-center gap-2">
                            {node.depth === 1 ? (
                              <AlertTriangle size={14} className="text-amber-500" />
                            ) : (
                              <Shield size={14} className="text-red-400" />
                            )}
                            {node.nodeName}
                          </div>
                        </td>
                        <td className="px-4 py-2">
                          <Badge variant={node.depth === 1 ? 'warning' : 'danger'}>{node.depth}</Badge>
                        </td>
                        <td className="px-4 py-2">
                          <Badge variant={confidenceVariant(node.confidenceScore)}>
                            {Math.round(node.confidenceScore * 100)}%
                          </Badge>
                        </td>
                        <td className="px-4 py-2 text-xs text-muted font-mono">{node.impactPath}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </CardBody>
            </Card>
          )}
        </>
      )}
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════════
// Componente: Painel de diff temporal
// ═══════════════════════════════════════════════════════════════════════

interface TemporalPanelProps {
  snapshots: GraphSnapshotSummary[];
  selectedFrom: string;
  selectedTo: string;
  diffResult: TemporalDiffResult | null;
  diffLoading: boolean;
  onSelectFrom: (id: string) => void;
  onSelectTo: (id: string) => void;
  onCreateSnapshot: () => void;
  createSnapshotPending: boolean;
}

/** Painel de comparação temporal entre snapshots do grafo. */
function TemporalPanel({
  snapshots, selectedFrom, selectedTo, diffResult, diffLoading,
  onSelectFrom, onSelectTo, onCreateSnapshot, createSnapshotPending,
}: TemporalPanelProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      {/* ── Controles de snapshot ────────────────────────────── */}
      <Card>
        <CardBody>
          <div className="flex items-end gap-4">
            <div className="flex-1">
              <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.temporal.fromSnapshot')}</label>
              <select
                value={selectedFrom}
                onChange={(e) => onSelectFrom(e.target.value)}
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
              >
                <option value="">{t('serviceCatalog.temporal.selectSnapshot')}</option>
                {snapshots.map((s) => (
                  <option key={s.snapshotId} value={s.snapshotId}>
                    {s.label} — {new Date(s.capturedAt).toLocaleDateString()} ({s.nodeCount} {t('serviceCatalog.temporal.nodes')}, {s.edgeCount} {t('serviceCatalog.temporal.edges')})
                  </option>
                ))}
              </select>
            </div>
            <div className="flex-1">
              <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.temporal.toSnapshot')}</label>
              <select
                value={selectedTo}
                onChange={(e) => onSelectTo(e.target.value)}
                className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent"
              >
                <option value="">{t('serviceCatalog.temporal.selectSnapshot')}</option>
                {snapshots.map((s) => (
                  <option key={s.snapshotId} value={s.snapshotId}>
                    {s.label} — {new Date(s.capturedAt).toLocaleDateString()} ({s.nodeCount} {t('serviceCatalog.temporal.nodes')}, {s.edgeCount} {t('serviceCatalog.temporal.edges')})
                  </option>
                ))}
              </select>
            </div>
            <Button
              variant="secondary"
              onClick={onCreateSnapshot}
              loading={createSnapshotPending}
            >
              <Clock size={16} /> {t('serviceCatalog.temporal.createSnapshot')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {/* ── Estado sem snapshots ─────────────────────────────── */}
      {snapshots.length === 0 && (
        <Card>
          <CardBody className="py-12 text-center">
            <Clock size={40} className="mx-auto text-muted mb-4" />
            <p className="text-muted">{t('serviceCatalog.temporal.noSnapshots')}</p>
            <p className="text-xs text-muted mt-2">{t('serviceCatalog.temporal.createFirst')}</p>
          </CardBody>
        </Card>
      )}

      {/* ── Lista de snapshots ──────────────────────────────── */}
      {snapshots.length > 0 && (
        <Card>
          <CardHeader>
            <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.temporal.snapshotHistory')}</h3>
          </CardHeader>
          <CardBody className="p-0">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.label')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.capturedAt')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.nodes')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.edges')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.createdBy')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {snapshots.map((s) => (
                  <tr key={s.snapshotId} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-2 text-body font-medium">{s.label}</td>
                    <td className="px-4 py-2 text-body">{new Date(s.capturedAt).toLocaleString()}</td>
                    <td className="px-4 py-2 text-body">{s.nodeCount}</td>
                    <td className="px-4 py-2 text-body">{s.edgeCount}</td>
                    <td className="px-4 py-2 text-xs text-muted">{s.createdBy}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}

      {/* ── Carregando diff ─────────────────────────────────── */}
      {diffLoading && (
        <div className="flex items-center justify-center py-8">
          <RefreshCw size={20} className="animate-spin text-muted" />
          <span className="ml-2 text-muted">{t('serviceCatalog.temporal.calculatingDiff')}</span>
        </div>
      )}

      {/* ── Resultado do diff temporal ──────────────────────── */}
      {diffResult && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-emerald-600">+{diffResult.addedNodesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.addedNodes')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-red-600">-{diffResult.removedNodesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.removedNodes')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-emerald-600">+{diffResult.addedEdgesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.addedEdges')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-red-600">-{diffResult.removedEdgesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.removedEdges')}</p>
            </CardBody>
          </Card>
        </div>
      )}
    </div>
  );
}

// ═══════════════════════════════════════════════════════════════════════
// Componente: Painel de detalhe de serviço/API com contexto operacional
// ═══════════════════════════════════════════════════════════════════════

/** Variantes de badge para status de saúde dos nós. */
const healthBadgeVariant = (status: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (status.toLowerCase()) {
    case 'healthy': return 'success';
    case 'degraded': return 'warning';
    case 'unhealthy': return 'danger';
    default: return 'default';
  }
};

/** Painel lateral de detalhe que mostra contexto operacional de um serviço ou API selecionado. */
function ServiceDetailPanel({
  graph,
  nodeId,
  healthData,
  onClose,
}: {
  graph: AssetGraph;
  nodeId: string;
  healthData: NodeHealthResult | null;
  onClose: () => void;
}) {
  const { t } = useTranslation();

  const service = graph.services.find((s) => s.serviceAssetId === nodeId);
  const api = graph.apis.find((a) => a.apiAssetId === nodeId);
  const nodeName = service?.name ?? api?.name ?? nodeId;
  const nodeHealth = healthData?.items?.find((h) => h.nodeId === nodeId);
  const healthStatus = nodeHealth?.status ?? 'Unknown';

  const consumerCount = api?.consumers?.length ?? 0;
  const dependencyCount = service
    ? graph.apis.filter((a) => a.consumers?.some((c) => c.consumerName === nodeId)).length
    : 0;

  return (
    <div className="absolute top-0 right-0 w-80 z-10 animate-fade-in">
      <Card className="shadow-lg border-edge">
        <CardHeader className="flex items-center justify-between">
          <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.detail.title')}</h3>
          <button onClick={onClose} className="text-muted hover:text-body transition-colors" aria-label={t('serviceCatalog.detail.close')}>
            <X size={16} />
          </button>
        </CardHeader>
        <CardBody className="space-y-4">
          {/* Nome e saúde */}
          <div>
            <p className="font-medium text-heading">{nodeName}</p>
            <div className="flex items-center gap-2 mt-1">
              <Badge variant={healthBadgeVariant(healthStatus)}>
                {t(`serviceCatalog.overview.${healthStatus.toLowerCase()}`)}
              </Badge>
              {nodeHealth && <span className="text-xs text-muted">{t('serviceCatalog.overview.healthScore')}: {nodeHealth.score.toFixed(2)}</span>}
            </div>
          </div>

          {/* Metadados do serviço */}
          {service && (
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted">{t('serviceCatalog.detail.domain')}</span>
                <span className="text-heading">{service.domain}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted">{t('serviceCatalog.detail.team')}</span>
                <span className="text-heading">{service.teamName}</span>
              </div>
            </div>
          )}

          {/* Metadados da API */}
          {api && (
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted">{t('serviceCatalog.detail.version')}</span>
                <span className="text-heading">v{api.version}</span>
              </div>
            </div>
          )}

          {/* Contadores */}
          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-md bg-elevated p-3 text-center">
              <p className="text-lg font-bold text-heading">{consumerCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.detail.consumerCount')}</p>
            </div>
            <div className="rounded-md bg-elevated p-3 text-center">
              <p className="text-lg font-bold text-heading">{dependencyCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.detail.dependencyCount')}</p>
            </div>
          </div>

          {/* Proveniência dos dados */}
          <div>
            <p className="text-xs font-medium text-heading mb-2">{t('serviceCatalog.detail.dataProvenance')}</p>
            <div className="space-y-1 text-xs text-muted">
              <div className="flex justify-between">
                <span>{t('serviceCatalog.overview.provenance')}</span>
                <Badge variant="default">{t('serviceCatalog.overview.catalogImport')}</Badge>
              </div>
              <div className="flex justify-between">
                <span>{t('serviceCatalog.overview.confidence')}</span>
                <span>—</span>
              </div>
              <div className="flex justify-between">
                <span>{t('serviceCatalog.overview.freshness')}</span>
                <span>—</span>
              </div>
            </div>
          </div>

          {/* Issues críticas */}
          <div>
            <p className="text-xs font-medium text-heading mb-1">{t('serviceCatalog.detail.criticalIssues')}</p>
            <p className="text-xs text-muted">{t('serviceCatalog.detail.noCriticalIssues')}</p>
          </div>

          <Button variant="secondary" className="w-full" onClick={onClose}>
            {t('serviceCatalog.detail.close')}
          </Button>
        </CardBody>
      </Card>
    </div>
  );
}
