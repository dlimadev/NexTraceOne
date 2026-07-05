import { useState, useMemo, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Server,
  Globe,
  Zap,
  Clock,
  Layers,
  GitBranch,
  BarChart3,
  Compass,
  PlusCircle,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { Button } from '../../../components/Button';
import { StatCard } from '../../../components/StatCard';
import { Tabs, TabPanel } from '../../../components/Tabs';
import { serviceCatalogApi } from '../api';
import { ServiceCatalogOverviewTab } from '../components/ServiceCatalogOverviewTab';
import { DependencyGraph } from '../components/DependencyGraph';
import { ServiceBrowseSurface } from '../browse/ServiceBrowseSurface';
import { PageContainer, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import type { GraphSnapshotSummary } from '../../../types';
import { ImpactPanel } from './ImpactPanel';
import { TemporalPanel } from './TemporalPanel';
import { ServiceDetailPanel } from './ServiceDetailPanel';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { queryKeys } from '../../../shared/api/queryKeys';

type Segment = 'browse' | 'explore';
type Tab = 'overview' | 'graph' | 'impact' | 'temporal';

export function ServiceCatalogPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { activeEnvironmentId } = useEnvironment();
  const [segment, setSegment] = useState<Segment>('browse');
  const [tab, setTab] = useState<Tab>('overview');
  const [selectedDetailNode, setSelectedDetailNode] = useState<string | null>(null);
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

  // ── Dados derivados ─────────────────────────────────────────────────
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

  // ── Handlers do Browse ──────────────────────────────────────────────
  // Rota de detalhe de serviço existe (/services/:serviceId → ServiceDetailPage).
  const handleOpenService = useCallback((id: string) => {
    navigate(`/services/${id}`);
  }, [navigate]);

  // Não existe rota dedicada de detalhe de API/interface; fallback in-page para
  // o segmento Explorar com o nó selecionado (aba de propagação de impacto).
  const handleOpenApi = useCallback((id: string) => {
    setSegment('explore');
    selectNodeForImpact(id);
  }, [selectNodeForImpact]);

  // Não existe rota de contrato indexada por apiId (/contracts/:contractVersionId
  // espera um contractVersionId); reencaminha para o fallback de API.
  const handleViewContract = useCallback((apiId: string) => {
    handleOpenApi(apiId);
  }, [handleOpenApi]);

  // ── Segmento de topo (Browse | Explorar) ────────────────────────────
  const segmentItems = [
    { id: 'browse',  label: t('serviceCatalog.browse.segment.browse'),  icon: <Compass size={14} /> },
    { id: 'explore', label: t('serviceCatalog.browse.segment.explore'), icon: <BarChart3 size={14} /> },
  ];

  // ── Definição das abas de análise (Explorar) ────────────────────────
  const tabItems = [
    { id: 'overview', label: t('serviceCatalog.tabs.overview'), icon: <BarChart3 size={14} /> },
    { id: 'graph',    label: t('serviceCatalog.tabs.graph'),    icon: <GitBranch size={14} /> },
    { id: 'impact',   label: t('serviceCatalog.tabs.impact'),   icon: <Zap size={14} /> },
    { id: 'temporal', label: t('serviceCatalog.tabs.temporal'), icon: <Clock size={14} /> },
  ];

  return (
    <PageContainer>

      {/* ── Cabeçalho com CTA secundária no header-right ─────────────── */}
      <PageHeader
        title={t('serviceCatalog.title')}
        subtitle={t('serviceCatalog.subtitle')}
        actions={
          <Button
            variant="ghost"
            size="sm"
            icon={<PlusCircle size={15} />}
          >
            {t('serviceCatalog.registerService')}
          </Button>
        }
      />

      {/* ── Segmento de topo: Browse | Explorar ────────────────────── */}
      <div className="mb-4">
        <Tabs
          id="catalog-segment"
          items={segmentItems}
          activeId={segment}
          onChange={(id) => setSegment(id as Segment)}
          variant="pill"
          size="sm"
        />
      </div>

      {/* ── Segmento: Browse (descoberta orientada ao consumidor) ───── */}
      <TabPanel tabsId="catalog-segment" tabId="browse" active={segment === 'browse'}>
        {isGraphError ? (
          <PageErrorState />
        ) : (
          <ServiceBrowseSurface
            graph={graph}
            loading={isLoading}
            onOpenService={handleOpenService}
            onOpenApi={handleOpenApi}
            onViewContract={handleViewContract}
          />
        )}
      </TabPanel>

      {/* ── Segmento: Explorar (análise para arquitetos) ───────────── */}
      <TabPanel tabsId="catalog-segment" tabId="explore" active={segment === 'explore'}>

        {/* ── KPIs com StatCard ──────────────────────────────────── */}
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

        {/* ── Sub-abas de análise ────────────────────────────────── */}
        <div className="mb-4">
          <Tabs
            id="service-catalog-tabs"
            items={tabItems}
            activeId={tab}
            onChange={(id) => setTab(id as Tab)}
            variant="pill"
            size="sm"
          />
        </div>

        {/* ── Conteúdo das abas de análise ───────────────────────── */}
        {isGraphError ? (
          <PageErrorState />
        ) : isLoading ? (
          <PageLoadingState />
        ) : (
          <>
            {/* ── Aba: Visão Operacional ──────────────────────────── */}
            <TabPanel tabsId="service-catalog-tabs" tabId="overview" active={tab === 'overview'}>
              <ServiceCatalogOverviewTab
                graph={graph}
                healthData={healthData}
                onSelectNode={(id) => { selectNodeForImpact(id); setSelectedDetailNode(id); }}
              />
            </TabPanel>

            {/* ── Aba: Grafo Visual ──────────────────────────────── */}
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

            {/* ── Aba: Propagação de Impacto ──────────────────────── */}
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

            {/* ── Aba: Diff Temporal ──────────────────────────────── */}
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
      </TabPanel>
    </PageContainer>
  );
}
