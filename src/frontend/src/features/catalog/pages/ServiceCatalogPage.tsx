import { useState, useMemo, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Plus,
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
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { OnboardingHints } from '../../../components/OnboardingHints';
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

  const [serviceForm, setServiceForm] = useState({
    name: '', team: '', description: '', domain: '',
    serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
    technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
  });
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
      setServiceForm({
        name: '', team: '', description: '', domain: '',
        serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
        technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
      });
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

      {/* ── Formulário de registro de serviço ───────────────────────── */}
      {showServiceForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-heading">{t('serviceCatalog.registerServiceTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); registerService.mutate(serviceForm); }}
              className="space-y-4"
            >
              {/* ── Basic Information ── */}
              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.basicInfo', 'Basic Information')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('serviceCatalog.name')} <span className="text-danger">*</span>
                    </label>
                    <input type="text" value={serviceForm.name}
                      onChange={(e) => setServiceForm((f) => ({ ...f, name: e.target.value }))}
                      required placeholder={t('serviceCatalog.namePlaceholder', 'e.g., payment-service')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('serviceCatalog.domain', 'Domain')} <span className="text-danger">*</span>
                    </label>
                    <input type="text" value={serviceForm.domain}
                      onChange={(e) => setServiceForm((f) => ({ ...f, domain: e.target.value }))}
                      required placeholder={t('serviceCatalog.domainPlaceholder', 'e.g., payments, identity, orders')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('serviceCatalog.team')} <span className="text-danger">*</span>
                    </label>
                    <input type="text" value={serviceForm.team}
                      onChange={(e) => setServiceForm((f) => ({ ...f, team: e.target.value }))}
                      required placeholder={t('serviceCatalog.teamPlaceholder', 'e.g., platform-team')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                </div>
              </div>

              {/* ── Service Type & Classification ── */}
              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.classification', 'Classification')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.serviceType', 'Service Type')}</label>
                    <select value={serviceForm.serviceType}
                      onChange={(e) => setServiceForm((f) => ({ ...f, serviceType: e.target.value }))}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors">
                      <optgroup label={t('serviceCatalog.typeGroupModern', 'Modern Services')}>
                        <option value="RestApi">{t('serviceCatalog.typeRestApi', 'REST API')}</option>
                        <option value="GraphqlApi">{t('serviceCatalog.typeGraphqlApi', 'GraphQL API')}</option>
                        <option value="GrpcService">{t('serviceCatalog.typeGrpcService', 'gRPC Service')}</option>
                        <option value="SoapService">{t('serviceCatalog.typeSoapService', 'SOAP Service')}</option>
                        <option value="KafkaProducer">{t('serviceCatalog.typeKafkaProducer', 'Kafka Producer')}</option>
                        <option value="KafkaConsumer">{t('serviceCatalog.typeKafkaConsumer', 'Kafka Consumer')}</option>
                        <option value="BackgroundService">{t('serviceCatalog.typeBackgroundService', 'Background Service')}</option>
                        <option value="ScheduledProcess">{t('serviceCatalog.typeScheduledProcess', 'Scheduled Process')}</option>
                        <option value="Gateway">{t('serviceCatalog.typeGateway', 'API Gateway')}</option>
                      </optgroup>
                      <optgroup label={t('serviceCatalog.typeGroupPlatform', 'Platform & Integration')}>
                        <option value="IntegrationComponent">{t('serviceCatalog.typeIntegrationComponent', 'Integration Component')}</option>
                        <option value="SharedPlatformService">{t('serviceCatalog.typeSharedPlatformService', 'Shared Platform Service')}</option>
                        <option value="Framework">{t('serviceCatalog.typeFramework', 'Framework / SDK')}</option>
                        <option value="ThirdParty">{t('serviceCatalog.typeThirdParty', 'Third-Party Service')}</option>
                        <option value="LegacySystem">{t('serviceCatalog.typeLegacySystem', 'Legacy System')}</option>
                      </optgroup>
                      <optgroup label={t('serviceCatalog.typeGroupMainframe', 'Mainframe')}>
                        <option value="CobolProgram">{t('serviceCatalog.typeCobolProgram', 'COBOL Program')}</option>
                        <option value="CicsTransaction">{t('serviceCatalog.typeCicsTransaction', 'CICS Transaction')}</option>
                        <option value="ImsTransaction">{t('serviceCatalog.typeImsTransaction', 'IMS Transaction')}</option>
                        <option value="BatchJob">{t('serviceCatalog.typeBatchJob', 'Batch Job')}</option>
                        <option value="MainframeSystem">{t('serviceCatalog.typeMainframeSystem', 'Mainframe System')}</option>
                        <option value="MqQueueManager">{t('serviceCatalog.typeMqQueueManager', 'MQ Queue Manager')}</option>
                        <option value="ZosConnectApi">{t('serviceCatalog.typeZosConnectApi', 'z/OS Connect API')}</option>
                      </optgroup>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.criticality', 'Criticality')}</label>
                    <select value={serviceForm.criticality}
                      onChange={(e) => setServiceForm((f) => ({ ...f, criticality: e.target.value }))}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors">
                      <option value="Low">{t('serviceCatalog.criticalityLow', 'Low')}</option>
                      <option value="Medium">{t('serviceCatalog.criticalityMedium', 'Medium')}</option>
                      <option value="High">{t('serviceCatalog.criticalityHigh', 'High')}</option>
                      <option value="Critical">{t('serviceCatalog.criticalityCritical', 'Critical')}</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.exposure', 'Exposure')}</label>
                    <select value={serviceForm.exposureType}
                      onChange={(e) => setServiceForm((f) => ({ ...f, exposureType: e.target.value }))}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors">
                      <option value="Internal">{t('serviceCatalog.exposureInternal', 'Internal')}</option>
                      <option value="Partner">{t('serviceCatalog.exposurePartner', 'Partner')}</option>
                      <option value="External">{t('serviceCatalog.exposureExternal', 'External / Public')}</option>
                    </select>
                  </div>
                </div>
              </div>

              {/* ── Description ── */}
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.description')}</label>
                <textarea value={serviceForm.description}
                  onChange={(e) => setServiceForm((f) => ({ ...f, description: e.target.value }))}
                  rows={2} placeholder={t('serviceCatalog.descriptionPlaceholder', 'Describe the purpose and responsibilities of this service...')}
                  className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors resize-none" />
              </div>

              {/* ── Ownership ── */}
              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.ownership', 'Ownership')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.technicalOwner', 'Technical Owner')}</label>
                    <input type="text" value={serviceForm.technicalOwner}
                      onChange={(e) => setServiceForm((f) => ({ ...f, technicalOwner: e.target.value }))}
                      placeholder={t('serviceCatalog.technicalOwnerPlaceholder', 'e.g., john.smith@company.com')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.businessOwner', 'Business Owner')}</label>
                    <input type="text" value={serviceForm.businessOwner}
                      onChange={(e) => setServiceForm((f) => ({ ...f, businessOwner: e.target.value }))}
                      placeholder={t('serviceCatalog.businessOwnerPlaceholder', 'e.g., Product Manager Name')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors" />
                  </div>
                </div>
              </div>

              {/* ── References ── */}
              <div>
                <h3 className="text-xs font-semibold text-muted uppercase tracking-wider mb-2">{t('serviceCatalog.references', 'References')}</h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.documentationUrl', 'Documentation URL')}</label>
                    <input type="url" value={serviceForm.documentationUrl}
                      onChange={(e) => setServiceForm((f) => ({ ...f, documentationUrl: e.target.value }))}
                      placeholder={t('catalog.detail.placeholder.documentationUrl', 'https://docs.company.com/payment-service')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono" />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.repositoryUrl', 'Repository URL')}</label>
                    <input type="url" value={serviceForm.repositoryUrl}
                      onChange={(e) => setServiceForm((f) => ({ ...f, repositoryUrl: e.target.value }))}
                      placeholder={t('catalog.detail.placeholder.repositoryUrl', 'https://github.com/org/payment-service')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono" />
                  </div>
                </div>
              </div>

              <div className="flex gap-2 justify-end pt-2 border-t border-edge">
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
