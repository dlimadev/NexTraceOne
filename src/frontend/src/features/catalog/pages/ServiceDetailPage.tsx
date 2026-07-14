import { useState, useCallback, useMemo } from 'react';
import type { TFunction } from 'i18next';
import { useParams, Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Shield,
  Users,
  Globe,
  FileText,
  ExternalLink,
  Eye,
  Lock,
  GitCommit,
  AlertTriangle,
  Server,
  Plus,
  Info,
  Activity,
  Award,
  Download,
  Pencil,
  X,
  Check,
  ChevronLeft,
  ChevronRight,
  GitBranch,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Tabs } from '../../../components/Tabs';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { serviceCatalogApi } from '../api';
import { contractsApi } from '../api/contracts';
import { AssistantPanel } from '../../ai-hub/components/AssistantPanel';
import { ServiceLinksSection } from '../components/ServiceLinksSection';
import { ServiceLifecyclePanel } from '../components/ServiceLifecyclePanel';
import type { Criticality, LifecycleStatus, ServiceApiSummary, ServiceContractItem, ServiceDetail } from '../../../types';
import type { ServiceType } from '../../../types';
import { PageContainer, PageSection, TableWrapper } from '../../../components/shell';
import { isRouteAvailableInFinalProductionScope } from '../../../releaseScope';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { supportsContracts } from '../../contracts/shared/serviceContractPolicy';
import { ServiceInterfacesTab } from '../components/ServiceInterfacesTab';
import { ServiceSetupChecklist } from '../components/ServiceSetupChecklist';
import { ServiceObservabilityTab } from '../components/ServiceObservabilityTab';
import { ServiceReliabilityTab } from '../components/ServiceReliabilityTab';
import { ServiceIncidentsTab } from '../components/ServiceIncidentsTab';
import { ServiceScoreTab } from '../components/ServiceScoreTab';
import { Button, TextField, TextArea, Select } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { ServiceContractDrawer, type ContractDrawerState } from '../components/ServiceContractDrawer';

// ── Helpers de variante de badge ─────────────────────────────────────────────

/** Mapeia criticidade para variante do Badge. */
const criticalityBadgeVariant = (level: Criticality): 'danger' | 'warning' | 'default' => {
  switch (level) {
    case 'Critical': return 'danger';
    case 'High': return 'warning';
    case 'Medium': return 'warning';
    case 'Low': return 'default';
    default: return 'default';
  }
};

/** Mapeia ciclo de vida para variante do Badge. */
const lifecycleBadgeVariant = (status: LifecycleStatus): 'success' | 'info' | 'warning' | 'default' => {
  switch (status) {
    case 'Active': return 'success';
    case 'Planning': case 'Development': case 'Staging': return 'info';
    case 'Deprecating': case 'Deprecated': return 'warning';
    case 'Retired': return 'default';
    default: return 'default';
  }
};

/** Mapeia protocolo de contrato para variante do Badge. */
const protocolBadgeVariant = (protocol: string): 'success' | 'info' | 'warning' | 'default' => {
  switch (protocol) {
    case 'OpenApi': case 'Swagger': return 'success';
    case 'AsyncApi': case 'Wsdl': return 'info';
    case 'Protobuf': case 'GraphQl': return 'warning';
    default: return 'default';
  }
};

/** Mapeia ciclo de vida de contrato para variante do Badge. */
const contractLifecycleBadgeVariant = (state: string): 'success' | 'info' | 'warning' | 'danger' | 'default' => {
  switch (state) {
    case 'Approved': return 'success';
    case 'InReview': return 'info';
    case 'Locked': return 'info';
    case 'Draft': return 'default';
    case 'Deprecated': return 'warning';
    case 'Sunset': case 'Retired': return 'danger';
    default: return 'default';
  }
};

// ── Tipos internos ────────────────────────────────────────────────────────────

/** Modo do workspace: consulta, edição ou criação. */
type WorkspaceMode = 'view' | 'edit' | 'create';

/** Tabs do formulário de preenchimento (edição/criação). */
type FormTab = 'identity' | 'classification' | 'ownership' | 'references' | 'confirm';

const FORM_TABS: FormTab[] = ['identity', 'classification', 'ownership', 'references', 'confirm'];

/** Estado de formulário completo para edição/criação. */
interface EditFormState {
  name: string;
  displayName: string;
  domain: string;
  subDomain: string;
  capability: string;
  description: string;
  serviceType: string;
  criticality: string;
  lifecycleStatus: string;
  exposureType: string;
  dataClassification: string;
  regulatoryScope: string;
  infrastructureProvider: string;
  runtimeLanguage: string;
  teamName: string;
  technicalOwner: string;
  businessOwner: string;
  productOwner: string;
  contactChannel: string;
  documentationUrl: string;
  repositoryUrl: string;
  systemArea: string;
}

const EMPTY_FORM: EditFormState = {
  name: '', displayName: '', domain: '', subDomain: '', capability: '',
  description: '', serviceType: 'RestApi', criticality: 'Medium',
  lifecycleStatus: 'Planning', exposureType: 'Internal',
  dataClassification: 'Internal', regulatoryScope: 'None',
  infrastructureProvider: '', runtimeLanguage: '',
  teamName: '', technicalOwner: '', businessOwner: '', productOwner: '',
  contactChannel: '', documentationUrl: '', repositoryUrl: '', systemArea: '',
};

/** Inicializa o formulário a partir dos dados do serviço carregado. */
function formFromDetail(svc: ServiceDetail): EditFormState {
  return {
    name: svc.name ?? '',
    displayName: svc.displayName ?? '',
    domain: svc.domain ?? '',
    subDomain: svc.subDomain ?? '',
    capability: svc.capability ?? '',
    description: svc.description ?? '',
    serviceType: svc.serviceType ?? 'RestApi',
    criticality: svc.criticality ?? 'Medium',
    lifecycleStatus: svc.lifecycleStatus ?? 'Active',
    exposureType: svc.exposureType ?? 'Internal',
    dataClassification: svc.dataClassification ?? 'Internal',
    regulatoryScope: svc.regulatoryScope ?? 'None',
    infrastructureProvider: svc.infrastructureProvider ?? '',
    runtimeLanguage: svc.runtimeLanguage ?? '',
    teamName: svc.teamName ?? '',
    technicalOwner: svc.technicalOwner ?? '',
    businessOwner: svc.businessOwner ?? '',
    productOwner: svc.productOwner ?? '',
    contactChannel: svc.contactChannel ?? '',
    documentationUrl: svc.documentationUrl ?? '',
    repositoryUrl: svc.repositoryUrl ?? '',
    systemArea: svc.systemArea ?? '',
  };
}

// ── Opções de select ──────────────────────────────────────────────────────────

const SERVICE_TYPE_OPTIONS = [
  { value: 'RestApi', label: 'REST API' },
  { value: 'GraphqlApi', label: 'GraphQL API' },
  { value: 'GrpcService', label: 'gRPC Service' },
  { value: 'SoapService', label: 'SOAP Service' },
  { value: 'KafkaProducer', label: 'Kafka Producer' },
  { value: 'KafkaConsumer', label: 'Kafka Consumer' },
  { value: 'BackgroundService', label: 'Background Service' },
  { value: 'ScheduledProcess', label: 'Scheduled Process' },
  { value: 'Gateway', label: 'API Gateway' },
  { value: 'IntegrationComponent', label: 'Integration Component' },
  { value: 'SharedPlatformService', label: 'Shared Platform Service' },
  { value: 'Framework', label: 'Framework / SDK' },
  { value: 'ThirdParty', label: 'Third-Party Service' },
  { value: 'LegacySystem', label: 'Legacy System' },
  { value: 'BatchJob', label: 'Batch Job' },
];

const CRITICALITY_OPTIONS = [
  { value: 'Low', label: 'Low' },
  { value: 'Medium', label: 'Medium' },
  { value: 'High', label: 'High' },
  { value: 'Critical', label: 'Critical' },
];

const LIFECYCLE_OPTIONS = [
  { value: 'Planning', label: 'Planning' },
  { value: 'Development', label: 'Development' },
  { value: 'Staging', label: 'Staging' },
  { value: 'Active', label: 'Active' },
  { value: 'Deprecating', label: 'Deprecating' },
  { value: 'Deprecated', label: 'Deprecated' },
  { value: 'Retired', label: 'Retired' },
];

const EXPOSURE_OPTIONS = [
  { value: 'Internal', label: 'Internal' },
  { value: 'Partner', label: 'Partner' },
  { value: 'External', label: 'External / Public' },
];

const DATA_CLASS_OPTIONS = [
  { value: 'Public', label: 'Public' },
  { value: 'Internal', label: 'Internal' },
  { value: 'Confidential', label: 'Confidential' },
  { value: 'Restricted', label: 'Restricted' },
];

const REGULATORY_OPTIONS = [
  { value: 'None', label: 'None' },
  { value: 'PCI-DSS', label: 'PCI-DSS' },
  { value: 'LGPD', label: 'LGPD' },
  { value: 'GDPR', label: 'GDPR' },
  { value: 'HIPAA', label: 'HIPAA' },
];

// ── Tabs de conteúdo para modo view ──────────────────────────────────────────

type ServiceTab = 'overview' | 'apis' | 'contracts' | 'interfaces' | 'observability' | 'reliability' | 'incidents' | 'score';

// ── Componente principal ──────────────────────────────────────────────────────

/**
 * Página de detalhe/edição/criação de serviço — Service Workspace v5.
 *
 * Layout: 2 colunas fixas.
 * ESQUERDA (~300px, sticky): cartão de identidade/resumo do serviço, atualiza ao vivo em edição/criação.
 * DIREITA:
 *   - Modo view: seções de conteúdo empilhadas, sem tabs de top-level.
 *   - Modo edit/create: tabs de preenchimento (Identity · Classification · Ownership · References · Confirm).
 *
 * Criar = navega para /services/new (ou ?mode=create em detalhe) com rascunho vazio.
 */
export function ServiceDetailPage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId?: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { activeEnvironment } = useEnvironment();

  // Determina modo inicial (create quando não há serviceId ou quando searchParams indica)
  const isCreateRoute = !serviceId || serviceId === 'new';
  const [mode, setMode] = useState<WorkspaceMode>(isCreateRoute ? 'create' : 'view');
  const [activeFormTab, setActiveFormTab] = useState<FormTab>('identity');
  const [activeViewTab, setActiveViewTab] = useState<ServiceTab>('overview');
  const [editForm, setEditForm] = useState<EditFormState>(EMPTY_FORM);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [contractDrawer, setContractDrawer] = useState<ContractDrawerState>({ mode: 'closed' });

  const closeContractDrawer = useCallback(() => {
    setContractDrawer({ mode: 'closed' });
    queryClient.invalidateQueries({ queryKey: ['catalog-service-contracts', serviceId] });
  }, [queryClient, serviceId]);

  // ── Carregamento de dados (apenas em view/edit de serviço existente) ─────────
  const { data: service, isLoading, isError } = useQuery({
    queryKey: ['catalog-service-detail', serviceId],
    queryFn: () => serviceCatalogApi.getServiceDetail(serviceId!),
    enabled: !!serviceId && !isCreateRoute,
  });

  const { data: serviceContracts } = useQuery({
    queryKey: ['catalog-service-contracts', serviceId],
    queryFn: () => contractsApi.listContractsByService(serviceId!),
    enabled: !!serviceId && !isCreateRoute,
  });

  // Scorecard de maturidade — alimenta o mini health strip (honest-null se indisponível).
  const { data: maturity } = useQuery({
    queryKey: ['catalog-service-maturity', serviceId],
    queryFn: () => serviceCatalogApi.getServiceMaturity(serviceId!),
    enabled: !!serviceId && !isCreateRoute,
  });

  const contracts = serviceContracts?.contracts ?? serviceContracts?.items ?? [];

  // ── Mutations ─────────────────────────────────────────────────────────────────

  /** Atualiza classificação e detalhes de serviço existente. */
  const updateMutation = useMutation({
    mutationFn: (data: Parameters<typeof serviceCatalogApi.updateService>[1]) =>
      serviceCatalogApi.updateService(serviceId!, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-service-detail', serviceId] });
      setMode('view');
      setSaveError(null);
    },
    onError: () => {
      setSaveError(t('common.error', 'Erro ao salvar. Tente novamente.'));
    },
  });

  /** Registra novo serviço no catálogo. */
  const createMutation = useMutation({
    mutationFn: (data: Parameters<typeof serviceCatalogApi.registerService>[0]) =>
      serviceCatalogApi.registerService(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['catalog-services'] });
      setSaveError(null);
      // Navega para o detalhe do serviço recém-criado
      if (result?.id) {
        navigate(`/services/${result.id}`);
      } else {
        navigate('/services');
      }
    },
    onError: () => {
      setSaveError(t('common.error', 'Erro ao criar serviço. Tente novamente.'));
    },
  });

  // ── Transição de modo ─────────────────────────────────────────────────────────

  /** Entra em modo edição inicializando o formulário com os dados atuais. */
  const enterEditMode = useCallback(() => {
    if (service) {
      setEditForm(formFromDetail(service));
    }
    setSaveError(null);
    setActiveFormTab('identity');
    setMode('edit');
  }, [service]);

  /** Entra em modo edição posicionado na tab que preenche a lacuna do checklist. */
  const handleEditField = useCallback((tab: FormTab) => {
    enterEditMode();
    setActiveFormTab(tab);
  }, [enterEditMode]);

  /** Cancela edição e volta para view (ou navega de volta em criação). */
  const cancelEdit = useCallback(() => {
    setSaveError(null);
    if (mode === 'create') {
      navigate('/services');
    } else {
      setMode('view');
    }
  }, [mode, navigate]);

  /** Salva edição ou criação. */
  const handleSave = useCallback(() => {
    setSaveError(null);
    if (mode === 'create') {
      createMutation.mutate({
        name: editForm.name,
        team: editForm.teamName,
        description: editForm.description,
        domain: editForm.domain,
        serviceType: editForm.serviceType,
        criticality: editForm.criticality,
        exposureType: editForm.exposureType,
        technicalOwner: editForm.technicalOwner,
        businessOwner: editForm.businessOwner,
        documentationUrl: editForm.documentationUrl,
        repositoryUrl: editForm.repositoryUrl,
      });
    } else {
      updateMutation.mutate({
        displayName: editForm.displayName || editForm.name,
        description: editForm.description,
        serviceType: editForm.serviceType,
        systemArea: editForm.systemArea,
        criticality: editForm.criticality,
        lifecycleStatus: editForm.lifecycleStatus,
        exposureType: editForm.exposureType,
        documentationUrl: editForm.documentationUrl,
        repositoryUrl: editForm.repositoryUrl,
      });
    }
  }, [mode, editForm, createMutation, updateMutation]);

  /** Atualiza um campo do formulário. */
  const setField = useCallback(<K extends keyof EditFormState>(key: K) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
      setEditForm((prev) => ({ ...prev, [key]: e.target.value }));
    }, []);

  // ── Navegação entre tabs do formulário ───────────────────────────────────────
  const formTabIndex = FORM_TABS.indexOf(activeFormTab);

  const goNextFormTab = useCallback(() => {
    const next = FORM_TABS[Math.min(formTabIndex + 1, FORM_TABS.length - 1)];
    if (next) setActiveFormTab(next);
  }, [formTabIndex]);

  const goPrevFormTab = useCallback(() => {
    const prev = FORM_TABS[Math.max(formTabIndex - 1, 0)];
    if (prev) setActiveFormTab(prev);
  }, [formTabIndex]);

  // ── Dados derivados para o cartão de resumo ───────────────────────────────────

  /** Dados exibidos no cartão de resumo — refletem o formulário em edição/criação. */
  const summaryData = useMemo(() => {
    if (mode === 'view' && service) {
      return {
        name: service.displayName || service.name,
        domain: service.domain,
        subDomain: service.subDomain,
        serviceType: service.serviceType,
        criticality: service.criticality,
        exposureType: service.exposureType,
        regulatoryScope: service.regulatoryScope,
        lifecycleStatus: service.lifecycleStatus,
        teamName: service.teamName,
        technicalOwner: service.technicalOwner,
        dependencyCount: service.apis?.length ?? 0,
        contractCount: contracts.length,
        maturityLevel: maturity?.level,
        sloTarget: service.sloTarget,
      };
    }
    // Em edit/create: reflete o formulário ao vivo
    return {
      name: editForm.name || (mode === 'create' ? 'novo-serviço' : service?.name ?? ''),
      domain: editForm.domain,
      subDomain: editForm.subDomain,
      serviceType: editForm.serviceType,
      criticality: editForm.criticality as Criticality,
      exposureType: editForm.exposureType,
      regulatoryScope: editForm.regulatoryScope,
      lifecycleStatus: mode === 'create' ? 'Planning' : (editForm.lifecycleStatus as LifecycleStatus),
      teamName: editForm.teamName,
      technicalOwner: editForm.technicalOwner,
      dependencyCount: service?.apis?.length ?? 0,
      contractCount: contracts.length,
      maturityLevel: maturity?.level,
      sloTarget: service?.sloTarget,
    };
  }, [mode, service, editForm, contracts.length, maturity]);

  // ── Loading / Error ───────────────────────────────────────────────────────────

  if (isLoading && !isCreateRoute) {
    return (
      <PageContainer>
        <PageLoadingState size="lg" />
      </PageContainer>
    );
  }

  if ((isError || (!service && !isCreateRoute))) {
    return (
      <PageContainer>
        <PageErrorState
          message={t('common.noResults')}
          action={
            <Link to="/services" className="text-sm text-accent hover:underline">
              {t('common.back')}
            </Link>
          }
        />
      </PageContainer>
    );
  }

  // ── Tabs de conteúdo (modo view) ──────────────────────────────────────────────

  const viewTabItems = [
    { id: 'overview', label: t('catalog.detail.overview'), icon: <FileText size={14} /> },
    {
      id: 'apis',
      label: `${t('catalog.detail.apis')} (${service?.apiCount ?? service?.apis?.length ?? 0})`,
      icon: <Globe size={14} />,
    },
    {
      id: 'contracts',
      label: `${t('catalog.detail.contracts')} (${serviceContracts?.totalCount ?? contracts.length})`,
      icon: <FileText size={14} />,
    },
    { id: 'interfaces', label: t('serviceDetail.tabInterfaces', 'Interfaces'), icon: <Server size={14} /> },
    { id: 'observability', label: t('serviceDetail.tabObservability', 'Observability'), icon: <Activity size={14} /> },
    { id: 'reliability', label: t('serviceDetail.tabReliability', 'Reliability & SLOs'), icon: <Shield size={14} /> },
    { id: 'incidents', label: t('serviceDetail.tabIncidents', 'Incidents'), icon: <AlertTriangle size={14} /> },
    { id: 'score', label: t('serviceDetail.tabScore', 'Score'), icon: <Award size={14} /> },
  ];

  // ── Render ────────────────────────────────────────────────────────────────────

  return (
    <PageContainer className="animate-fade-in">
      {/* Breadcrumb / volta ao catálogo */}
      <div className="flex items-center justify-between mb-4">
        <Link to="/services" className="inline-flex items-center gap-1.5 text-sm text-muted hover:text-heading transition-colors">
          <ChevronLeft size={14} />
          {t('catalog.detail.backToCatalog', 'Service Catalog')}
        </Link>
        {/* Botão de criar novo serviço — apenas em modo view de serviço existente */}
        {mode === 'view' && !isCreateRoute && (
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={() => navigate('/services/new')}
          >
            {t('serviceCatalog.registerService', '+ Novo serviço')}
          </Button>
        )}
      </div>

      {/* ── Workspace: 2 colunas ─────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">

        {/* ── ESQUERDA: cartão de identidade (sticky) ──────────────────────── */}
        <div className="lg:sticky lg:top-4">
          <ServiceIdentityCard
            mode={mode}
            summaryData={summaryData}
            t={t}
            criticalityBadgeVariant={criticalityBadgeVariant}
            lifecycleBadgeVariant={lifecycleBadgeVariant}
          />
        </div>

        {/* ── DIREITA: conteúdo por modo ────────────────────────────────────── */}
        <div className="min-w-0">
          {/* Cabeçalho da coluna direita */}
          <div className="flex items-center justify-between mb-4">
            <h1 className="text-lg font-bold text-heading">
              {mode === 'view'
                ? t('serviceDetail.title', 'Visão geral')
                : mode === 'create'
                  ? t('serviceDetail.createTitle', 'Novo serviço')
                  : t('serviceDetail.editTitle', 'Editar serviço')}
            </h1>
            <div className="flex items-center gap-2">
              {mode === 'view' && !isCreateRoute && (
                <>
                  <Button variant="outline" size="sm" icon={<Download size={14} />}>
                    {t('common.export', 'Export')}
                  </Button>
                  <Button variant="primary" size="sm" icon={<Pencil size={14} />} onClick={enterEditMode}>
                    {t('common.edit', 'Editar')}
                  </Button>
                </>
              )}
              {(mode === 'edit' || mode === 'create') && (
                <>
                  <Button variant="ghost" size="sm" icon={<X size={14} />} onClick={cancelEdit}>
                    {t('common.cancel', 'Cancelar')}
                  </Button>
                  <Button
                    variant="primary"
                    size="sm"
                    icon={<Check size={14} />}
                    onClick={handleSave}
                    loading={updateMutation.isPending || createMutation.isPending}
                  >
                    {t('common.save', 'Salvar')}
                  </Button>
                </>
              )}
            </div>
          </div>

          {/* Mensagem de erro de salvamento */}
          {saveError && (
            <div className="mb-4 rounded-lg border border-critical/30 bg-critical/10 px-4 py-3 text-sm text-critical">
              {saveError}
            </div>
          )}

          {/* ── MODO VIEW: seções sem tabs de primeiro nível ─────────────── */}
          {mode === 'view' && service && (
            <ViewContent
              service={service}
              serviceId={serviceId!}
              contracts={contracts}
              serviceContracts={serviceContracts}
              activeViewTab={activeViewTab}
              setActiveViewTab={setActiveViewTab}
              viewTabItems={viewTabItems}
              activeEnvironment={activeEnvironment}
              criticalityBadgeVariant={criticalityBadgeVariant}
              lifecycleBadgeVariant={lifecycleBadgeVariant}
              protocolBadgeVariant={protocolBadgeVariant}
              contractLifecycleBadgeVariant={contractLifecycleBadgeVariant}
              navigate={navigate}
              onEditField={handleEditField}
              onCreateContract={() => setContractDrawer({ mode: 'create' })}
              onViewContract={(contractVersionId) => setContractDrawer({ mode: 'view', contractVersionId })}
              t={t}
            />
          )}

          {/* ── MODO EDIT / CREATE: tabs de preenchimento ────────────────── */}
          {(mode === 'edit' || mode === 'create') && (
            <EditTabsContent
              form={editForm}
              setField={setField}
              activeFormTab={activeFormTab}
              setActiveFormTab={setActiveFormTab}
              formTabIndex={formTabIndex}
              goNextFormTab={goNextFormTab}
              goPrevFormTab={goPrevFormTab}
              onSave={handleSave}
              isSaving={updateMutation.isPending || createMutation.isPending}
              t={t}
            />
          )}
        </div>
      </div>

      {/* AssistantPanel — apenas em modo view de serviço existente */}
      {mode === 'view' && service && !isCreateRoute && (
        <div className="mt-6">
          <AssistantPanel
            contextType="service"
            contextId={serviceId!}
            contextSummary={{
              name: service.displayName || service.name,
              description: service.description,
              status: service.lifecycleStatus,
              additionalInfo: {
                ...(service.criticality ? { criticality: service.criticality } : {}),
                ...(service.teamName ? { team: service.teamName } : {}),
                ...(service.domain ? { domain: service.domain } : {}),
              },
            }}
            contextData={{
              entityType: 'service',
              entityName: service.displayName || service.name,
              entityStatus: service.lifecycleStatus,
              entityDescription: service.description,
              properties: {
                ...(service.criticality ? { criticality: service.criticality } : {}),
                ...(service.teamName ? { team: service.teamName } : {}),
                ...(service.domain ? { domain: service.domain } : {}),
                ...(service.serviceType ? { serviceType: service.serviceType } : {}),
                ...(service.technicalOwner ? { technicalOwner: service.technicalOwner } : {}),
                ...(service.exposureType ? { exposure: service.exposureType } : {}),
                ...(service.apiCount != null ? { apiCount: String(service.apiCount) } : {}),
                ...(service.totalConsumers != null ? { consumers: String(service.totalConsumers) } : {}),
              },
              relations: contracts.map((c: ServiceContractItem) => ({
                relationType: 'Contracts',
                entityType: 'contract',
                name: c.apiName || c.versionId || c.contractVersionId,
                status: c.lifecycleState,
                properties: {
                  ...(c.protocol ? { protocol: c.protocol } : {}),
                  ...(c.semVer || c.version ? { version: c.semVer ?? c.version } : {}),
                },
              })),
              caveats: [
                ...(!contracts.length ? [t('assistantPanel.contextCaveats.noContractsLoaded')] : []),
              ].filter(Boolean),
            }}
            activeEnvironmentId={activeEnvironment?.id}
            activeEnvironmentName={activeEnvironment?.name}
            isNonProductionEnvironment={activeEnvironment ? !activeEnvironment.isProductionLike : false}
          />
        </div>
      )}

      {!isCreateRoute && serviceId && (
        <ServiceContractDrawer
          state={contractDrawer}
          onClose={closeContractDrawer}
          onModeChange={setContractDrawer}
          serviceId={serviceId}
        />
      )}
    </PageContainer>
  );
}

// ── Sub-componente: Cartão de Identidade (esquerda, sticky) ──────────────────

interface SummaryData {
  name: string;
  domain: string;
  subDomain?: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  regulatoryScope?: string;
  lifecycleStatus: string;
  teamName: string;
  technicalOwner: string;
  dependencyCount: number;
  contractCount: number;
  /** Nível de maturidade (scorecard) — honest-null se indisponível. */
  maturityLevel?: string;
  /** Alvo de SLO do serviço — honest-null se indisponível. */
  sloTarget?: string;
}

interface ServiceIdentityCardProps {
  mode: WorkspaceMode;
  summaryData: SummaryData;
  t: TFunction;
  criticalityBadgeVariant: (c: Criticality) => 'danger' | 'warning' | 'default';
  lifecycleBadgeVariant: (s: LifecycleStatus) => 'success' | 'info' | 'warning' | 'default';
}

/**
 * Cartão de resumo persistente à esquerda do workspace.
 * Em modo edição/criação reflete os valores do formulário ao vivo.
 */
function ServiceIdentityCard({ mode, summaryData, t, criticalityBadgeVariant, lifecycleBadgeVariant }: ServiceIdentityCardProps) {
  const initial = summaryData.name.trim().charAt(0).toUpperCase() || (mode === 'create' ? '?' : '?');
  const isDraft = mode === 'create' || summaryData.lifecycleStatus === 'Planning';
  const lsVariant = lifecycleBadgeVariant(summaryData.lifecycleStatus as LifecycleStatus);

  return (
    <div className="rounded-2xl border border-edge bg-card overflow-hidden shadow-sm">
      {/* Topo com gradiente e avatar */}
      <div className="bg-gradient-to-b from-accent/10 to-transparent p-4">
        <div className="flex items-center gap-3">
          {/* Avatar com inicial */}
          <div className={cn(
            'flex items-center justify-center w-11 h-11 rounded-xl font-bold text-lg shrink-0',
            mode === 'create' && !summaryData.name
              ? 'bg-accent/20 text-accent'
              : 'bg-accent text-on-accent',
          )}>
            {initial}
          </div>
          <div className="min-w-0">
            {/* Nome em mono */}
            <p className={cn(
              'font-mono text-sm font-semibold truncate',
              !summaryData.name ? 'text-muted' : 'text-heading',
            )}>
              {summaryData.name || 'novo-serviço'}
            </p>
            {/* Domínio e subdomínio */}
            <p className="text-xs text-muted truncate mt-0.5">
              {summaryData.domain || '—'}
              {summaryData.subDomain && ` · ${summaryData.subDomain}`}
            </p>
          </div>
          {/* Badge de status */}
          {isDraft && mode === 'create' ? (
            <Badge variant="warning" size="sm" className="shrink-0 ml-auto">{t('serviceDetail.draftBadge', 'Draft')}</Badge>
          ) : (
            <Badge variant={lsVariant} size="sm" className="shrink-0 ml-auto">
              {summaryData.lifecycleStatus}
            </Badge>
          )}
        </div>

        {/* Chips de classificação */}
        <div className="flex flex-wrap gap-1.5 mt-3">
          {summaryData.serviceType && (
            <Badge variant="primary" size="sm">{summaryData.serviceType}</Badge>
          )}
          {summaryData.criticality && (
            <Badge variant={criticalityBadgeVariant(summaryData.criticality as Criticality)} size="sm">
              {summaryData.criticality}
            </Badge>
          )}
          {summaryData.exposureType && (
            <Badge variant="default" size="sm">
              {summaryData.exposureType === 'External' ? '🌐' : '🏠'} {summaryData.exposureType}
            </Badge>
          )}
          {summaryData.regulatoryScope && summaryData.regulatoryScope !== 'None' && (
            <Badge variant="warning" size="sm">⚠ {summaryData.regulatoryScope}</Badge>
          )}
        </div>
      </div>

      {/* Mini health strip — apenas sinais com dados reais (honest-null) */}
      <HealthStrip
        signals={[
          summaryData.maturityLevel
            ? { key: 'maturity', label: t('serviceDetail.health.maturity', 'Maturity'), value: summaryData.maturityLevel }
            : null,
          summaryData.sloTarget
            ? { key: 'slo', label: t('serviceDetail.health.slo', 'SLO'), value: summaryData.sloTarget, mono: true }
            : null,
        ]}
      />

      {/* Meta rows */}
      <div className="px-4 py-2 divide-y divide-edge/60">
        <MetaRow label={t('catalog.detail.team', 'Time')} value={summaryData.teamName || '—'} />
        <MetaRow label={t('catalog.detail.technicalOwner', 'Tech owner')} value={summaryData.technicalOwner || '—'} />
        <MetaRow label={t('catalog.detail.dependencies', 'Dependências')} value={String(summaryData.dependencyCount)} mono />
        <MetaRow label={t('catalog.detail.contracts', 'Contratos')} value={String(summaryData.contractCount)} mono />
      </div>

      {/* Nota em modo edição/criação */}
      {(mode === 'edit' || mode === 'create') && (
        <p className="text-[11px] text-muted text-center py-2 px-4 border-t border-edge">
          {t('serviceDetail.livePreviewHint', 'Resumo atualiza ao vivo')}
        </p>
      )}
    </div>
  );
}

/** Sinal individual do mini health strip. */
interface HealthSignal {
  key: string;
  label: string;
  value: string;
  mono?: boolean;
}

/**
 * Mini health strip do cartão de identidade.
 * Renderiza apenas os sinais com dados reais (honest-null) e distribui as
 * colunas dinamicamente; oculta-se por completo quando não há sinais.
 */
function HealthStrip({ signals }: { signals: (HealthSignal | null)[] }) {
  const present = signals.filter((s): s is HealthSignal => s !== null);
  if (present.length === 0) return null;

  return (
    <div
      className="grid gap-px bg-edge border-t border-b border-edge"
      style={{ gridTemplateColumns: `repeat(${present.length}, minmax(0, 1fr))` }}
    >
      {present.map((s) => (
        <div key={s.key} className="bg-deep text-center py-3">
          <p className={cn('text-sm font-bold text-heading', s.mono && 'font-mono')}>{s.value}</p>
          <p className="text-[10px] text-muted mt-0.5">{s.label}</p>
        </div>
      ))}
    </div>
  );
}

/** Linha de meta-dado no cartão de identidade. */
function MetaRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex items-center justify-between py-2 text-xs">
      <span className="text-muted">{label}</span>
      <span className={cn('text-heading font-medium', mono && 'font-mono')}>{value}</span>
    </div>
  );
}

// ── Sub-componente: Conteúdo em modo View ─────────────────────────────────────

interface ViewContentProps {
  service: ServiceDetail;
  serviceId: string;
  contracts: ServiceContractItem[];
  serviceContracts: { totalCount?: number; contracts?: ServiceContractItem[]; items?: ServiceContractItem[] } | undefined;
  activeViewTab: ServiceTab;
  setActiveViewTab: (tab: ServiceTab) => void;
  viewTabItems: Array<{ id: string; label: string; icon: React.ReactNode }>;
  activeEnvironment: { id?: string; name?: string; isProductionLike?: boolean } | null | undefined;
  criticalityBadgeVariant: (c: Criticality) => 'danger' | 'warning' | 'default';
  lifecycleBadgeVariant: (s: LifecycleStatus) => 'success' | 'info' | 'warning' | 'default';
  protocolBadgeVariant: (p: string) => 'success' | 'info' | 'warning' | 'default';
  contractLifecycleBadgeVariant: (s: string) => 'success' | 'info' | 'warning' | 'danger' | 'default';
  navigate: (path: string) => void;
  onEditField: (tab: FormTab) => void;
  onCreateContract: () => void;
  onViewContract: (contractVersionId: string) => void;
  t: TFunction;
}

/**
 * Conteúdo do lado direito em modo de consulta.
 * Mostra uma stat strip, depois seções empilhadas (sem tabs de primeiro nível),
 * seguidas de tabs de conteúdo secundário (APIs, contratos, etc.).
 */
function ViewContent({
  service,
  serviceId,
  contracts,
  serviceContracts,
  activeViewTab,
  setActiveViewTab,
  viewTabItems,
  activeEnvironment,
  criticalityBadgeVariant,
  lifecycleBadgeVariant,
  protocolBadgeVariant,
  contractLifecycleBadgeVariant,
  navigate,
  onEditField,
  onCreateContract,
  onViewContract,
  t,
}: ViewContentProps) {
  return (
    <>
      {/* Mini stat strip */}
      <div className="grid grid-cols-3 gap-3 mb-5">
        <StatStrip label={t('catalog.detail.sloTarget', 'SLO')} value={service.sloTarget ?? '—'} />
        <StatStrip label={t('catalog.detail.apis', 'APIs')} value={String(service.apiCount ?? 0)} />
        <StatStrip label={t('catalog.detail.contracts', 'Contratos')} value={String(serviceContracts?.totalCount ?? contracts.length)} />
      </div>

      {/* Checklist de setup guiado — do Planning ao Active */}
      <ServiceSetupChecklist
        service={service}
        contractCount={serviceContracts?.totalCount ?? contracts.length}
        lifecycleStatus={service.lifecycleStatus}
        onEditOwnership={() => onEditField('ownership')}
        onEditReferences={() => onEditField('references')}
        onAddInterface={() => navigate(`/services/${serviceId}/interfaces/new`)}
        onAddContract={onCreateContract}
      />

      {/* Seção: Identidade & Classificação */}
      <SectionBlock title={t('catalog.detail.identityAndClassification', 'Identidade & Classificação')}>
        <Card>
          <CardBody>
            <dl className="divide-y divide-edge/60 text-sm">
              <FieldRow label={t('catalog.columns.domain', 'Domínio')} value={`${service.domain}${service.subDomain ? ` / ${service.subDomain}` : ''}`} />
              <FieldRow label={t('catalog.columns.serviceType', 'Tipo')}>
                <Badge variant="primary" size="sm">{t(`catalog.badges.type.${service.serviceType}`, service.serviceType)}</Badge>
              </FieldRow>
              <FieldRow label={t('catalog.detail.criticality', 'Criticidade')}>
                <Badge variant={criticalityBadgeVariant(service.criticality)} size="sm">
                  {t(`catalog.badges.criticality.${service.criticality}`, service.criticality)}
                </Badge>
              </FieldRow>
              <FieldRow label={t('catalog.detail.lifecycleStatus', 'Ciclo de vida')}>
                <Badge variant={lifecycleBadgeVariant(service.lifecycleStatus)} size="sm">
                  {t(`catalog.badges.lifecycle.${service.lifecycleStatus}`, service.lifecycleStatus)}
                </Badge>
              </FieldRow>
              <FieldRow
                label={t('catalog.detail.exposureType', 'Exposição')}
                value={`${t(`catalog.badges.exposure.${service.exposureType}`, service.exposureType)}${service.regulatoryScope && service.regulatoryScope !== 'None' ? ` · ${service.regulatoryScope}` : ''}`}
              />
              {service.capability && <FieldRow label={t('catalog.detail.capability', 'Capability')} value={service.capability} />}
              {service.dataClassification && <FieldRow label={t('catalog.detail.dataClassification', 'Classificação')} value={service.dataClassification} />}
            </dl>
          </CardBody>
        </Card>
      </SectionBlock>

      {/* Seção: Ownership */}
      <SectionBlock title={t('catalog.detail.ownership', 'Ownership')}>
        <Card>
          <CardBody>
            <dl className="divide-y divide-edge/60 text-sm">
              <FieldRow label={t('catalog.detail.team', 'Time')} value={service.teamName} />
              <FieldRow label={t('catalog.detail.technicalOwner', 'Tech Owner')} value={service.technicalOwner} />
              {service.businessOwner && <FieldRow label={t('catalog.detail.businessOwner', 'Business Owner')} value={service.businessOwner} />}
              {service.contactChannel && (
                <FieldRow label={t('catalog.detail.contactChannel', 'Contato')}>
                  <span className="text-accent">{service.contactChannel}</span>
                </FieldRow>
              )}
            </dl>
          </CardBody>
        </Card>
      </SectionBlock>

      {/* Seção: Dependências */}
      <SectionBlock
        title={`${t('catalog.detail.dependencies', 'Dependências')} (${service.apiCount ?? 0})`}
        action={
          <Link
            to={`/services/${serviceId}/interfaces`}
            className="text-xs text-accent hover:underline flex items-center gap-1"
          >
            <ExternalLink size={11} />
            {t('catalog.detail.viewGraph', 'Ver grafo')}
          </Link>
        }
      >
        <Card>
          <CardBody>
            {(service.apis?.length ?? 0) === 0 ? (
              <p className="text-sm text-muted py-2">{t('catalog.detail.noApis', 'Nenhuma dependência registrada')}</p>
            ) : (
              <ul className="divide-y divide-edge/60">
                {service.apis.slice(0, 5).map((api: ServiceApiSummary) => (
                  <li key={api.apiId} className="flex items-center gap-2 py-2.5 text-sm">
                    <span className={cn('w-2 h-2 rounded-full shrink-0', api.isDecommissioned ? 'bg-critical' : 'bg-success')} />
                    <span className="font-mono text-heading text-xs">{api.name}</span>
                    <span className="text-muted text-xs ml-auto">{api.routePattern}</span>
                  </li>
                ))}
                {service.apis.length > 5 && (
                  <li className="py-2 text-xs text-muted">
                    + {service.apis.length - 5} {t('common.more', 'mais')}
                  </li>
                )}
              </ul>
            )}
          </CardBody>
        </Card>
      </SectionBlock>

      {/* Seção: Contratos */}
      <SectionBlock title={`${t('catalog.detail.contracts', 'Contratos')} (${serviceContracts?.totalCount ?? contracts.length})`}>
        <Card>
          <CardBody>
            {contracts.length === 0 ? (
              <p className="text-sm text-muted py-2">{t('catalog.detail.noContracts', 'Nenhum contrato publicado')}</p>
            ) : (
              <ul className="divide-y divide-edge/60">
                {contracts.slice(0, 5).map((c: ServiceContractItem) => (
                  <li key={c.versionId ?? c.contractVersionId} className="flex items-center gap-2 py-2.5 text-sm">
                    <Badge variant={protocolBadgeVariant(c.protocol)} size="sm">{c.protocol}</Badge>
                    <span className="font-mono text-heading text-xs">{c.apiName ?? '—'}</span>
                    <Badge variant={contractLifecycleBadgeVariant(c.lifecycleState)} size="sm" className="ml-auto">
                      {c.lifecycleState}
                    </Badge>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </SectionBlock>

      {/* Seção: Links & Referências */}
      {service.documentationUrl || service.repositoryUrl || service.gitRepository ? (
        <SectionBlock title={t('catalog.detail.references', 'Referências')}>
          <Card>
            <CardBody>
              <dl className="divide-y divide-edge/60 text-sm">
                {service.documentationUrl && (
                  <FieldRow label={t('catalog.detail.documentation', 'Documentação')}>
                    <a href={service.documentationUrl} target="_blank" rel="noopener noreferrer" className="text-accent hover:underline flex items-center gap-1 text-xs font-mono">
                      {service.documentationUrl} <ExternalLink size={10} />
                    </a>
                  </FieldRow>
                )}
                {(service.repositoryUrl || service.gitRepository) && (
                  <FieldRow label={t('catalog.detail.gitRepository', 'Repositório')}>
                    <a href={service.repositoryUrl ?? service.gitRepository ?? '#'} target="_blank" rel="noopener noreferrer" className="text-accent hover:underline flex items-center gap-1 text-xs font-mono">
                      <GitBranch size={10} />
                      {service.repositoryUrl ?? service.gitRepository}
                    </a>
                  </FieldRow>
                )}
                {service.ciPipelineUrl && (
                  <FieldRow label={t('catalog.detail.ciPipelineUrl', 'CI/CD')}>
                    <a href={service.ciPipelineUrl} target="_blank" rel="noopener noreferrer" className="text-accent hover:underline flex items-center gap-1 text-xs font-mono">
                      {service.ciPipelineUrl} <ExternalLink size={10} />
                    </a>
                  </FieldRow>
                )}
              </dl>
            </CardBody>
          </Card>
        </SectionBlock>
      ) : null}

      {/* Separador para conteúdo secundário (tabs avançadas) */}
      <div className="mt-8 border-t border-edge pt-6">
        <Tabs
          items={viewTabItems}
          activeId={activeViewTab}
          onChange={(id) => setActiveViewTab(id as ServiceTab)}
          className="mb-6"
        />

        {/* Conteúdo da tab de view secundária */}
        {activeViewTab === 'overview' && (
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <GitCommit size={16} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('catalog.detail.recentChanges', 'Mudanças recentes')}</h2>
              </div>
            </CardHeader>
            <CardBody>
              <p className="text-xs text-muted mb-3">{t('catalog.detail.recentChangesDescription')}</p>
              <div className="flex flex-wrap items-center gap-4">
                <Link
                  to={`/changes?serviceName=${encodeURIComponent(service.name)}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('catalog.detail.viewChange')}
                </Link>
                <Link
                  to={`/services/scorecards?serviceName=${encodeURIComponent(service.name)}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('serviceDetail.viewScorecard', 'View scorecard')}
                </Link>
                <Link
                  to={`/source-of-truth/services/${serviceId}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('serviceDetail.viewSourceOfTruth', 'View source of truth')}
                </Link>
              </div>
            </CardBody>
          </Card>
        )}

        {activeViewTab === 'apis' && (
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Globe size={16} className="text-accent" />
                <h2 className="text-base font-semibold text-heading">{t('catalog.detail.apis')}</h2>
              </div>
            </CardHeader>
            <CardBody className="p-0">
              {service.apis.length === 0 ? (
                <div className="py-10 text-center">
                  <p className="text-sm text-muted">{t('catalog.detail.noApis')}</p>
                </div>
              ) : (
                <TableWrapper>
                  <table className="w-full text-sm">
                    <thead className="sticky top-0 z-10 bg-panel">
                      <tr className="border-b border-edge text-left">
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.columns.name')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.routePattern')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.version')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.visibility')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.consumers')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.status')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge">
                      {service.apis.map((api: ServiceApiSummary) => (
                        <tr key={api.apiId} className="hover:bg-elevated/50 transition-colors">
                          <td className="px-4 py-3 font-medium text-heading">{api.name}</td>
                          <td className="px-4 py-3 text-muted font-mono text-xs">{api.routePattern}</td>
                          <td className="px-4 py-3 text-muted">{api.version}</td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center gap-1 text-xs text-muted">
                              <Eye size={12} />
                              {api.visibility}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-muted">{api.consumerCount}</td>
                          <td className="px-4 py-3">
                            {api.isDecommissioned
                              ? <Badge variant="danger" size="sm">{t('catalog.detail.decommissioned')}</Badge>
                              : <Badge variant="success" size="sm">{t('catalog.detail.active')}</Badge>}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </TableWrapper>
              )}
            </CardBody>
          </Card>
        )}

        {activeViewTab === 'contracts' && (
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileText size={16} className="text-accent" />
                  <h2 className="text-base font-semibold text-heading">{t('catalog.detail.contracts')}</h2>
                </div>
                {service.serviceType && supportsContracts(service.serviceType as ServiceType) ? (
                  <Button
                    variant="ghost"
                    size="xs"
                    onClick={onCreateContract}
                    icon={<Plus size={13} />}
                    className="text-accent border border-accent/30 hover:text-accent/80 hover:bg-transparent"
                  >
                    {t('catalog.services.addContract', 'Add Contract')}
                  </Button>
                ) : (
                  <div className="flex items-center gap-1 text-xs text-muted border border-edge rounded px-2.5 py-1">
                    <Info size={12} />
                    {t('catalog.services.noContractsForType', 'No public contracts for this type')}
                  </div>
                )}
              </div>
            </CardHeader>
            <CardBody className="p-0">
              {contracts.length === 0 ? (
                <div className="py-10 text-center">
                  <p className="text-sm text-muted">{t('catalog.detail.noContracts')}</p>
                </div>
              ) : (
                <TableWrapper>
                  <table className="w-full text-sm">
                    <thead className="sticky top-0 z-10 bg-panel">
                      <tr className="border-b border-edge text-left">
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.columns.name')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.detail.version')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('contractGov.columns.protocol')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('contractGov.columns.lifecycle')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('contractGov.columns.locked')}</th>
                        <th scope="col" className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">{t('catalog.columns.actions')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge">
                      {contracts.map((contract: ServiceContractItem) => (
                        <tr key={contract.versionId ?? contract.contractVersionId} className="hover:bg-elevated/50 transition-colors">
                          <td className="px-4 py-3">
                            <span className="font-medium text-heading">{contract.apiName ?? '—'}</span>
                            <span className="block text-xs text-muted font-mono">{contract.apiRoutePattern ?? '—'}</span>
                          </td>
                          <td className="px-4 py-3 text-muted font-mono text-xs">v{contract.semVer ?? contract.version}</td>
                          <td className="px-4 py-3">
                            <Badge variant={protocolBadgeVariant(contract.protocol)} size="sm">
                              {t(`contractGov.badges.protocols.${contract.protocol}`, contract.protocol)}
                            </Badge>
                          </td>
                          <td className="px-4 py-3">
                            <Badge variant={contractLifecycleBadgeVariant(contract.lifecycleState)} size="sm">
                              {t(`contractGov.badges.lifecycle.${contract.lifecycleState}`, contract.lifecycleState)}
                            </Badge>
                          </td>
                          <td className="px-4 py-3">
                            {contract.isLocked
                              ? <Lock size={14} className="text-info" />
                              : <span className="text-xs text-muted">—</span>}
                          </td>
                          <td className="px-4 py-3">
                            <button
                              type="button"
                              onClick={() => onViewContract(contract.versionId ?? contract.contractVersionId)}
                              className="text-xs text-accent hover:underline"
                            >
                              {t('catalog.detail.viewContract')}
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </TableWrapper>
              )}
            </CardBody>
          </Card>
        )}

        {activeViewTab === 'interfaces' && <ServiceInterfacesTab serviceId={serviceId} />}
        {activeViewTab === 'observability' && <ServiceObservabilityTab serviceId={serviceId} serviceName={service.name} />}
        {activeViewTab === 'reliability' && <ServiceReliabilityTab serviceId={serviceId} />}
        {activeViewTab === 'incidents' && <ServiceIncidentsTab serviceId={serviceId} />}
        {activeViewTab === 'score' && <ServiceScoreTab serviceId={serviceId} />}
      </div>

      {/* Panels laterais adicionais — lifecycle e links */}
      <div className="mt-6 grid grid-cols-1 gap-4">
        <ServiceLifecyclePanel
          serviceId={serviceId}
          serviceName={service.displayName || service.name}
          currentStatus={service.lifecycleStatus}
        />
        <ServiceLinksSection serviceId={serviceId} />
      </div>
    </>
  );
}

// ── Sub-componente: Formulário com tabs (modo edit / create) ──────────────────

interface EditTabsContentProps {
  form: EditFormState;
  setField: <K extends keyof EditFormState>(key: K) => (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => void;
  activeFormTab: FormTab;
  setActiveFormTab: (tab: FormTab) => void;
  formTabIndex: number;
  goNextFormTab: () => void;
  goPrevFormTab: () => void;
  onSave: () => void;
  isSaving: boolean;
  t: TFunction;
}

/**
 * Formulário de preenchimento em tabs — edição e criação.
 * Tabs: Identity · Classification · Ownership · References · Confirm.
 * Navegação livre (clicar na tab vai direto) + Anterior/Próximo.
 */
function EditTabsContent({
  form,
  setField,
  activeFormTab,
  setActiveFormTab,
  formTabIndex,
  goNextFormTab,
  goPrevFormTab,
  onSave,
  isSaving,
  t,
}: EditTabsContentProps) {
  const TAB_LABELS: Record<FormTab, string> = {
    identity: t('serviceCatalog.wizard.step1', 'Identity'),
    classification: t('serviceCatalog.wizard.step2', 'Classification'),
    ownership: t('serviceCatalog.wizard.step3', 'Ownership'),
    references: t('serviceCatalog.wizard.step4', 'References'),
    confirm: t('serviceCatalog.wizard.step5', 'Confirm'),
  };

  const isLastTab = formTabIndex === FORM_TABS.length - 1;
  const isFirstTab = formTabIndex === 0;

  return (
    <div>
      {/* Tabs de preenchimento — navegação livre */}
      <div className="flex gap-0.5 border-b border-edge overflow-x-auto">
        {FORM_TABS.map((tab, idx) => (
          <Button
            key={tab}
            variant="ghost"
            size="md"
            onClick={() => setActiveFormTab(tab)}
            className={cn(
              'whitespace-nowrap border-b-2 rounded-none hover:bg-transparent',
              activeFormTab === tab
                ? 'text-accent border-accent hover:text-accent'
                : 'text-muted border-transparent hover:text-heading',
            )}
          >
            <span className={cn(
              'w-5 h-5 rounded-full text-[11px] flex items-center justify-center font-bold',
              activeFormTab === tab ? 'bg-accent text-on-accent' : 'bg-elevated text-muted',
            )}>
              {idx + 1}
            </span>
            {TAB_LABELS[tab]}
          </Button>
        ))}
      </div>

      {/* Conteúdo de cada tab */}
      <div className="bg-card border border-edge border-t-0 rounded-b-xl p-5">

        {/* Tab 1: Identity */}
        {activeFormTab === 'identity' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <TextField
              label={`${t('serviceCatalog.name', 'Name')} *`}
              value={form.name}
              onChange={setField('name')}
              placeholder={t('serviceCatalog.namePlaceholder', 'e.g., payment-service')}
              className="font-mono"
            />
            <TextField
              label={t('serviceCatalog.wizard.displayName', 'Display Name')}
              value={form.displayName}
              onChange={setField('displayName')}
              placeholder={t('serviceCatalog.wizard.displayNamePlaceholder', 'e.g., Payment Service')}
            />
            <TextField
              label={`${t('serviceCatalog.domain', 'Domain')} *`}
              value={form.domain}
              onChange={setField('domain')}
              placeholder={t('serviceCatalog.domainPlaceholder', 'e.g., payments, identity')}
            />
            <TextField
              label={t('serviceCatalog.wizard.subDomain', 'Sub-Domain')}
              value={form.subDomain}
              onChange={setField('subDomain')}
              placeholder={t('serviceCatalog.wizard.subDomainPlaceholder', 'fraud, payments-core')}
            />
            <TextField
              label={t('serviceCatalog.wizard.capability', 'Business Capability')}
              value={form.capability}
              onChange={setField('capability')}
              placeholder={t('serviceCatalog.wizard.capabilityPlaceholder', 'Payment Processing')}
            />
            <TextField
              label={t('catalog.detail.systemArea', 'System Area')}
              value={form.systemArea}
              onChange={setField('systemArea')}
              placeholder={t('catalog.detail.systemAreaPlaceholder', 'Core Banking, Payments')}
            />
          </div>
        )}

        {/* Tab 2: Classification */}
        {activeFormTab === 'classification' && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Select
                label={t('serviceCatalog.serviceType', 'Service Type')}
                value={form.serviceType}
                onChange={setField('serviceType')}
                options={SERVICE_TYPE_OPTIONS}
              />
              <Select
                label={t('serviceCatalog.criticality', 'Criticality')}
                value={form.criticality}
                onChange={setField('criticality')}
                options={CRITICALITY_OPTIONS}
              />
              <Select
                label={t('serviceCatalog.exposure', 'Exposure')}
                value={form.exposureType}
                onChange={setField('exposureType')}
                options={EXPOSURE_OPTIONS}
              />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Select
                label={t('catalog.detail.lifecycleStatus', 'Lifecycle Status')}
                value={form.lifecycleStatus}
                onChange={setField('lifecycleStatus')}
                options={LIFECYCLE_OPTIONS}
              />
              <Select
                label={t('serviceCatalog.wizard.dataClassification', 'Data Classification')}
                value={form.dataClassification}
                onChange={setField('dataClassification')}
                options={DATA_CLASS_OPTIONS}
              />
              <Select
                label={t('serviceCatalog.wizard.regulatoryScope', 'Regulatory Scope')}
                value={form.regulatoryScope}
                onChange={setField('regulatoryScope')}
                options={REGULATORY_OPTIONS}
              />
              <TextField
                label={t('serviceCatalog.wizard.infrastructureProvider', 'Infrastructure Provider')}
                value={form.infrastructureProvider}
                onChange={setField('infrastructureProvider')}
                placeholder={t('serviceCatalog.wizard.infrastructureProviderPlaceholder', 'Kubernetes, IIS, VM')}
              />
              <TextField
                label={t('serviceCatalog.wizard.runtimeLanguage', 'Runtime Language')}
                value={form.runtimeLanguage}
                onChange={setField('runtimeLanguage')}
                placeholder={t('serviceCatalog.wizard.runtimeLanguagePlaceholder', 'C#, Java, Python')}
              />
            </div>
          </div>
        )}

        {/* Tab 3: Ownership */}
        {activeFormTab === 'ownership' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <TextField
              label={`${t('serviceCatalog.team', 'Team')} *`}
              value={form.teamName}
              onChange={setField('teamName')}
              placeholder={t('serviceCatalog.teamPlaceholder', 'e.g., platform-team')}
            />
            <TextField
              label={t('serviceCatalog.technicalOwner', 'Technical Owner')}
              value={form.technicalOwner}
              onChange={setField('technicalOwner')}
              placeholder={t('serviceCatalog.technicalOwnerPlaceholder', 'e.g., john.smith@company.com')}
            />
            <TextField
              label={t('serviceCatalog.businessOwner', 'Business Owner')}
              value={form.businessOwner}
              onChange={setField('businessOwner')}
              placeholder={t('serviceCatalog.businessOwnerPlaceholder', 'e.g., Product Manager')}
            />
            <TextField
              label={t('serviceCatalog.wizard.productOwner', 'Product Owner')}
              value={form.productOwner}
              onChange={setField('productOwner')}
              placeholder={t('serviceCatalog.wizard.productOwnerPlaceholder', 'jane.doe@company.com')}
            />
            <TextField
              label={t('serviceCatalog.wizard.contactChannel', 'Contact Channel')}
              value={form.contactChannel}
              onChange={setField('contactChannel')}
              placeholder={t('serviceCatalog.wizard.contactChannelPlaceholder', '#payments-support')}
            />
          </div>
        )}

        {/* Tab 4: References */}
        {activeFormTab === 'references' && (
          <div className="space-y-4">
            <TextArea
              label={t('serviceCatalog.description', 'Description')}
              value={form.description}
              onChange={setField('description')}
              rows={3}
              placeholder={t('serviceCatalog.descriptionPlaceholder', 'Descreva o propósito e responsabilidades deste serviço...')}
            />
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <TextField
                label={t('serviceCatalog.documentationUrl', 'Documentation URL')}
                type="url"
                value={form.documentationUrl}
                onChange={setField('documentationUrl')}
                placeholder={t('catalog.registration.placeholder.documentationUrl', 'https://docs.example.com/service')}
                className="font-mono"
              />
              <TextField
                label={t('serviceCatalog.repositoryUrl', 'Repository URL')}
                type="url"
                value={form.repositoryUrl}
                onChange={setField('repositoryUrl')}
                placeholder={t('catalog.registration.placeholder.repositoryUrl', 'https://github.com/org/repo')}
                className="font-mono"
              />
            </div>
          </div>
        )}

        {/* Tab 5: Confirm */}
        {activeFormTab === 'confirm' && (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-heading">
              {t('serviceCatalog.wizard.summaryTitle', 'Revisão & confirmação')}
            </h3>
            <p className="text-sm text-muted">
              {t('serviceCatalog.wizard.summaryHint', 'Confira o resumo à esquerda (atualiza ao vivo) e confirme.')}
            </p>
            <div className="rounded-xl bg-elevated border border-edge p-4 space-y-2 text-sm">
              <ConfirmRow label={t('serviceCatalog.name', 'Name')} value={form.name} mono />
              <ConfirmRow label={t('serviceCatalog.domain', 'Domain')} value={form.domain} />
              <ConfirmRow label={t('serviceCatalog.team', 'Team')} value={form.teamName} />
              <ConfirmRow label={t('serviceCatalog.serviceType', 'Service Type')} value={form.serviceType} />
              <ConfirmRow label={t('serviceCatalog.criticality', 'Criticality')} value={form.criticality} />
              <ConfirmRow label={t('serviceCatalog.exposure', 'Exposure')} value={form.exposureType} />
              {form.regulatoryScope !== 'None' && (
                <ConfirmRow label={t('serviceCatalog.wizard.regulatoryScope', 'Regulatory Scope')} value={form.regulatoryScope} />
              )}
            </div>
          </div>
        )}

        {/* Rodapé de navegação */}
        <div className="flex items-center justify-between mt-6 pt-4 border-t border-edge">
          <Button
            variant="ghost"
            size="sm"
            icon={<ChevronLeft size={14} />}
            onClick={goPrevFormTab}
            disabled={isFirstTab}
          >
            {t('serviceCatalog.wizard.back', 'Anterior')}
          </Button>
          <span className="text-xs text-muted">
            {t('serviceDetail.freeTabNav', 'Navegue livre pelas tabs')}
          </span>
          {isLastTab ? (
            <Button
              variant="primary"
              size="sm"
              icon={<Check size={14} />}
              onClick={onSave}
              loading={isSaving}
            >
              {t('serviceCatalog.wizard.register', 'Salvar')}
            </Button>
          ) : (
            <Button
              variant="primary"
              size="sm"
              icon={<ChevronRight size={14} />}
              onClick={goNextFormTab}
            >
              {t('serviceCatalog.wizard.next', 'Próximo')}
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Utilitários internos ──────────────────────────────────────────────────────

/** Bloco de seção com título e linha divisória. */
function SectionBlock({
  title,
  children,
  action,
}: {
  title: string;
  children: React.ReactNode;
  action?: React.ReactNode;
}) {
  return (
    <div className="mt-5">
      <div className="flex items-center gap-2 mb-2">
        <h2 className="text-xs font-bold text-heading uppercase tracking-wide whitespace-nowrap">{title}</h2>
        <div className="flex-1 h-px bg-edge" />
        {action && <div className="shrink-0">{action}</div>}
      </div>
      {children}
    </div>
  );
}

/** Linha de campo label + valor (ou children) no card de seção. */
function FieldRow({
  label,
  value,
  children,
}: {
  label: string;
  value?: string;
  children?: React.ReactNode;
}) {
  return (
    <div className="flex items-center gap-4 py-2.5">
      <dt className="text-xs text-muted w-36 shrink-0">{label}</dt>
      <dd className="text-sm text-heading flex-1">
        {children ?? value ?? '—'}
      </dd>
    </div>
  );
}

/** Cartão de estatística na mini strip do modo view. */
function StatStrip({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-card border border-edge rounded-xl px-4 py-3">
      <p className="text-[10px] text-muted font-semibold uppercase tracking-wide">{label}</p>
      <p className="font-mono text-base font-bold text-heading mt-1">{value}</p>
    </div>
  );
}

/** Linha de confirmação no passo de revisão. */
function ConfirmRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex gap-2">
      <span className="text-muted w-32 shrink-0">{label}:</span>
      <span className={cn('text-heading', mono && 'font-mono')}>{value || '—'}</span>
    </div>
  );
}
