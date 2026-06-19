import { useState, useMemo, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Server,
  Globe,
  Zap,
  Clock,
  ChevronRight,
  Layers,
  GitBranch,
  BarChart3,
  PlusCircle,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { EmptyState } from '../../../components/EmptyState';
import { StatCard } from '../../../components/StatCard';
import { Tabs, TabPanel } from '../../../components/Tabs';
import { SearchInput } from '../../../components/SearchInput';
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

  // ── Definição das abas para o componente Tabs ───────────────────────
  const tabItems = [
    { id: 'overview', label: t('serviceCatalog.tabs.overview'), icon: <BarChart3 size={14} /> },
    { id: 'graph',    label: t('serviceCatalog.tabs.graph'),    icon: <GitBranch size={14} /> },
    { id: 'services', label: t('serviceCatalog.tabs.services'), icon: <Server size={14} /> },
    { id: 'apis',     label: t('serviceCatalog.tabs.apis'),     icon: <Globe size={14} /> },
    { id: 'impact',   label: t('serviceCatalog.tabs.impact'),   icon: <Zap size={14} /> },
    { id: 'temporal', label: t('serviceCatalog.tabs.temporal'), icon: <Clock size={14} /> },
  ];

  return (
    <PageContainer>

      {/* ── Cabeçalho com CTA principal no header-right ─────────────── */}
      <PageHeader
        title={t('serviceCatalog.title')}
        subtitle={t('serviceCatalog.subtitle')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<PlusCircle size={15} />}
          >
            {t('serviceCatalog.registerService')}
          </Button>
        }
      />

      {/* ── KPIs com StatCard ──────────────────────────────────────── */}
      <StatsGrid columns={4}>
        <StatCard
          title={t('serviceCatalog.stats.services')}
          value={graphStats.services}
          icon={<Server size={18} />}
          color="text-info"
        />
        <StatCard
          title={t('serviceCatalog.stats.apis')}
          value={graphStats.apis}
          icon={<Globe size={18} />}
          color="text-success"
        />
        <StatCard
          title={t('serviceCatalog.stats.edges')}
          value={graphStats.edges}
          icon={<GitBranch size={18} />}
          color="text-info"
        />
        <StatCard
          title={t('serviceCatalog.stats.domains')}
          value={graphStats.domains}
          icon={<Layers size={18} />}
          color="text-warning"
        />
      </StatsGrid>

      {/* ── Barra de filtros: busca + abas lado a lado ─────────────── */}
      <div className="flex flex-col sm:flex-row sm:items-center gap-3 mb-4">
        <SearchInput
          size="sm"
          placeholder={t('serviceCatalog.searchPlaceholder')}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="max-w-sm"
          aria-label={t('serviceCatalog.searchPlaceholder')}
        />

        {/* ── Abas de navegação (pill variant) ──────────────────────── */}
        <Tabs
          id="service-catalog-tabs"
          items={tabItems}
          activeId={tab}
          onChange={(id) => setTab(id as Tab)}
          variant="pill"
          size="sm"
        />
      </div>

      {/* ── Conteúdo das abas ──────────────────────────────────────── */}
      {isGraphError ? (
        <PageErrorState />
      ) : isLoading ? (
        <PageLoadingState />
      ) : (
        <>
          {/* ── Aba: Visão Operacional ──────────────────────────────── */}
          <TabPanel tabsId="service-catalog-tabs" tabId="overview" active={tab === 'overview'}>
            <ServiceCatalogOverviewTab
              graph={graph}
              healthData={healthData}
              onSelectNode={(id) => { selectNodeForImpact(id); setSelectedDetailNode(id); }}
            />
          </TabPanel>

          {/* ── Aba: Grafo Visual ──────────────────────────────────── */}
          <TabPanel tabsId="service-catalog-tabs" tabId="graph" active={tab === 'graph'}>
            {graph && (
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
          </TabPanel>

          {/* ── Aba: Serviços ──────────────────────────────────────── */}
          <TabPanel tabsId="service-catalog-tabs" tabId="services" active={tab === 'services'}>
            <ServiceCatalogServicesTab
              filteredServices={filteredServices}
              onSelectNode={selectNodeForImpact}
            />
          </TabPanel>

          {/* ── Aba: APIs ──────────────────────────────────────────── */}
          <TabPanel tabsId="service-catalog-tabs" tabId="apis" active={tab === 'apis'}>
            <Card>
              <CardBody className="p-0">
                {!filteredApis.length ? (
                  <EmptyState
                    title={t('serviceCatalog.noApis')}
                    size="compact"
                  />
                ) : (
                  <ul className="divide-y divide-edge">
                    {filteredApis.map((api) => (
                      <li
                        key={api.apiAssetId}
                        className="px-6 py-4 flex items-center gap-4 hover:bg-hover transition-colors cursor-pointer"
                        role="button"
                        tabIndex={0}
                        onClick={() => selectNodeForImpact(api.apiAssetId)}
                        onKeyDown={(e) => {
                          if (e.key === ' ') { e.preventDefault(); }
                          if (e.key === 'Enter' || e.key === ' ') { selectNodeForImpact(api.apiAssetId); }
                        }}
                      >
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
          </TabPanel>

          {/* ── Aba: Propagação de Impacto ──────────────────────────── */}
          <TabPanel tabsId="service-catalog-tabs" tabId="impact" active={tab === 'impact'}>
            <ImpactPanel
              graph={graph}
              selectedNodeId={selectedNodeId}
              impactResult={impactResult ?? null}
              impactLoading={impactLoading}
              impactDepth={impactDepth}
              onSelectNode={setSelectedNodeId}
              onChangeDepth={setImpactDepth}
            />
          </TabPanel>

          {/* ── Aba: Diff Temporal ──────────────────────────────────── */}
          <TabPanel tabsId="service-catalog-tabs" tabId="temporal" active={tab === 'temporal'}>
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
          </TabPanel>
        </>
      )}
    </PageContainer>
  );
}
