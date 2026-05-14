import { useState, useMemo, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Server,
  Globe,
  Search,
  Zap,
  Clock,
  ChevronRight,
  Layers,
  GitBranch,
  X,
  BarChart3,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { serviceCatalogApi } from '../api';
import { ServiceCatalogOverviewTab } from '../components/ServiceCatalogOverviewTab';
import { ServiceCatalogServicesTab } from '../components/ServiceCatalogServicesTab';
import { DependencyGraph } from '../components/DependencyGraph';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type { GraphSnapshotSummary } from '../../../types';
import { ImpactPanel } from './ImpactPanel';
import { TemporalPanel } from './TemporalPanel';
import { ServiceDetailPanel } from './ServiceDetailPanel';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { queryKeys } from '../../../shared/api/queryKeys';

type Tab = 'overview' | 'services' | 'apis' | 'graph' | 'impact' | 'temporal';

export function ServiceCatalogPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { activeEnvironmentId } = useEnvironment();
  const [tab, setTab] = useState<Tab>('overview');
  const [selectedDetailNode, setSelectedDetailNode] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [impactDepth, setImpactDepth] = useState(3);
  const [selectedFromSnapshot, setSelectedFromSnapshot] = useState<string>('');
  const [selectedToSnapshot, setSelectedToSnapshot] = useState<string>('');

  // ── Queries principais ──────────────────────────────────────────────
  const { data: graph, isLoading, isError: isGraphError } = useQuery({
    queryKey: queryKeys.catalog.graph(activeEnvironmentId),
    queryFn: () => serviceCatalogApi.getGraph(),
    staleTime: 30_000,
  });

  const { data: impactResult, isLoading: impactLoading } = useQuery({
    queryKey: queryKeys.catalog.impact.propagation(selectedNodeId!, impactDepth, activeEnvironmentId),
    queryFn: () => serviceCatalogApi.getImpactPropagation(selectedNodeId!, impactDepth),
    enabled: !!selectedNodeId && tab === 'impact',
    staleTime: 15_000,
  });

  const { data: snapshotsData } = useQuery({
    queryKey: queryKeys.catalog.snapshots.all(activeEnvironmentId),
    queryFn: () => serviceCatalogApi.listSnapshots(20),
    enabled: tab === 'temporal',
    staleTime: 60_000,
  });

  const { data: diffResult, isLoading: diffLoading } = useQuery({
    queryKey: queryKeys.catalog.snapshots.diff(selectedFromSnapshot, selectedToSnapshot, activeEnvironmentId),
    queryFn: () => serviceCatalogApi.getTemporalDiff(selectedFromSnapshot, selectedToSnapshot),
    enabled: !!selectedFromSnapshot && !!selectedToSnapshot && selectedFromSnapshot !== selectedToSnapshot,
    staleTime: 30_000,
  });

  const { data: healthData } = useQuery({
    queryKey: queryKeys.catalog.nodeHealth.all('Health', activeEnvironmentId),
    queryFn: () => serviceCatalogApi.getNodeHealth('Health'),
    staleTime: 30_000,
  });

  // ── Mutations ───────────────────────────────────────────────────────
  const createSnapshot = useMutation({
    mutationFn: (label: string) => serviceCatalogApi.createSnapshot(label),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.catalog.snapshots.all(activeEnvironmentId) });
    },
  });

  const services = graph?.services;
  const apis = graph?.apis;

  // ── Dados derivados ─────────────────────────────────────────────────
  const filteredServices = useMemo(() => {
    if (!services) return [];
    if (!searchTerm) return services;
    const term = searchTerm.toLowerCase();
    return services.filter(
      (s) => s.name.toLowerCase().includes(term) || s.teamName.toLowerCase().includes(term) || s.domain.toLowerCase().includes(term)
    );
  }, [services, searchTerm]);

  const filteredApis = useMemo(() => {
    if (!apis) return [];
    if (!searchTerm) return apis;
    const term = searchTerm.toLowerCase();
    return apis.filter(
      (a) => a.name.toLowerCase().includes(term) || a.routePattern.toLowerCase().includes(term)
    );
  }, [apis, searchTerm]);

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

      <PageHeader
        title={t('serviceCatalog.title')}
        subtitle={t('serviceCatalog.subtitle')}
      />

      {/* ── Estatísticas resumidas ──────────────────────────────────── */}
      <StatsGrid columns={4}>
        {[
          { label: t('serviceCatalog.stats.services'), value: graphStats.services, icon: <Server size={18} />, color: 'text-info' },
          { label: t('serviceCatalog.stats.apis'), value: graphStats.apis, icon: <Globe size={18} />, color: 'text-success' },
          { label: t('serviceCatalog.stats.edges'), value: graphStats.edges, icon: <GitBranch size={18} />, color: 'text-info' },
          { label: t('serviceCatalog.stats.domains'), value: graphStats.domains, icon: <Layers size={18} />, color: 'text-warning' },
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
            <div className="relative space-y-3">
              <DependencyGraph
                graph={graph}
                selectedNodeId={selectedDetailNode}
                onSelectNode={(id) => { selectNodeForImpact(id); setSelectedDetailNode(id); }}
                height={560}
              />
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
                      <li key={api.apiAssetId} className="px-6 py-4 flex items-center gap-4 hover:bg-hover transition-colors cursor-pointer" role="button" tabIndex={0} onClick={() => selectNodeForImpact(api.apiAssetId)} onKeyDown={(e) => { if (e.key === ' ') { e.preventDefault(); } if (e.key === 'Enter' || e.key === ' ') { selectNodeForImpact(api.apiAssetId); } }}>
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
