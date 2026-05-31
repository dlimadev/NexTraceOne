import { useState, useMemo } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Globe,
  Server,
  Zap,
  Cog,
  Database,
  FileCode,
  MessageSquare,
  Terminal,
  Webhook,
  AlignJustify,
  Columns,
  Upload,
  Sparkles,
  ArrowLeft,
  ArrowRight,
  Lock,
  Info,
  Search,
  Check,
  ChevronRight,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import {
  SERVICE_TYPES as CONTRACT_TYPES,
  PROTOCOL_BY_TYPE,
  PROTOCOL_COLORS,
  type ContractTypeValue,
} from '../shared/constants';
import { supportsContracts, allowedContractTypes } from '../shared/serviceContractPolicy';
import { contractStudioApi } from '../api/contractStudio';
import type { ContractType, ContractProtocol, ServiceListItem } from '../types';
import type { ServiceType } from '../../../types';
import { useAuth } from '../../../contexts/AuthContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';

// ── Icon map ──────────────────────────────────────────────────────────────────

const TYPE_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
  Copybook: FileCode,
  MqMessage: MessageSquare,
  FixedLayout: AlignJustify,
  CicsCommarea: Terminal,
  Webhook: Webhook,
};

// ── Creation modes ────────────────────────────────────────────────────────────

type CreationMode = 'visual' | 'import' | 'ai';

interface CreationModeOption {
  id: CreationMode;
  labelKey: string;
  descriptionKey: string;
  Icon: React.ComponentType<{ size?: number; className?: string }>;
}

const CREATION_MODES: CreationModeOption[] = [
  {
    id: 'visual',
    labelKey: 'contracts.create.modeVisual',
    descriptionKey: 'contracts.create.modeVisualDesc',
    Icon: Columns,
  },
  {
    id: 'import',
    labelKey: 'contracts.create.modeImport',
    descriptionKey: 'contracts.create.modeImportDesc',
    Icon: Upload,
  },
  {
    id: 'ai',
    labelKey: 'contracts.create.modeAi',
    descriptionKey: 'contracts.create.modeAiDesc',
    Icon: Sparkles,
  },
];

// ── Step type: 3 steps (was 4) ─────────────────────────────────────────────
// service → configure (type + mode combined) → details

type Step = 'service' | 'configure' | 'details';

/**
 * Página de criação de novo contrato.
 * Fluxo: service (se não pré-preenchido) → configure (tipo + modo) → details.
 * Reduz de 4 para 3 passos ao combinar seleção de tipo e modo no mesmo passo.
 */
export function CreateContractPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const currentActor = user?.email || user?.fullName || user?.id || 'system';

  const prefilledServiceId = searchParams.get('serviceId') ?? '';
  const hasPrefilledService = prefilledServiceId.length > 0;

  const [step, setStep] = useState<Step>(hasPrefilledService ? 'configure' : 'service');
  const [selectedType, setSelectedType] = useState<ContractTypeValue | null>(null);
  const [selectedMode, setSelectedMode] = useState<CreationMode | null>(null);
  const [selectedProtocol, setSelectedProtocol] = useState<ContractProtocol | ''>('');
  const [linkedServiceId, setLinkedServiceId] = useState(prefilledServiceId);
  const [selectedServiceType, setSelectedServiceType] = useState<ServiceType | null>(null);
  const [serviceSearch, setServiceSearch] = useState('');

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [importContent, setImportContent] = useState('');
  const [aiPrompt, setAiPrompt] = useState('');

  // SOAP-specific fields
  const [soapServiceName, setSoapServiceName] = useState('');
  const [soapTargetNamespace, setSoapTargetNamespace] = useState('http://example.com/service');
  const [soapVersion, setSoapVersion] = useState<'1.1' | '1.2'>('1.1');
  const [soapEndpointUrl, setSoapEndpointUrl] = useState('');

  // Event/AsyncAPI-specific fields
  const [asyncApiVersion, setAsyncApiVersion] = useState('2.6.0');
  const [defaultContentType, setDefaultContentType] = useState('application/json');

  // Background Service-specific fields
  const [bgServiceName, setBgServiceName] = useState('');
  const [bgCategory, setBgCategory] = useState('Job');
  const [bgTriggerType, setBgTriggerType] = useState('OnDemand');
  const [bgScheduleExpression, setBgScheduleExpression] = useState('');

  const isSoapType = selectedType === 'Soap';
  const isEventType = selectedType === 'Event';
  const isBackgroundServiceType = selectedType === 'BackgroundService';

  // ── Queries ─────────────────────────────────────────────────────────────────

  const servicesQuery = useQuery({
    queryKey: ['catalog-services-for-contracts'],
    queryFn: () => serviceCatalogApi.listServices(),
  });

  const prefilledServiceQuery = useQuery({
    queryKey: ['catalog-service-for-contract-create', prefilledServiceId],
    queryFn: () => serviceCatalogApi.listServices(),
    enabled: hasPrefilledService,
    select: (data) => data?.items?.find((s) => s.serviceId === prefilledServiceId) ?? null,
  });

  const effectiveServiceType: ServiceType | null =
    selectedServiceType ?? (prefilledServiceQuery.data?.serviceType ?? null);

  const filteredContractTypes = effectiveServiceType
    ? CONTRACT_TYPES.filter((ct) =>
        allowedContractTypes(effectiveServiceType).includes(ct.value as ContractTypeValue),
      )
    : CONTRACT_TYPES;

  const serviceSupportsContracts = effectiveServiceType ? supportsContracts(effectiveServiceType) : true;

  const availableServices = servicesQuery.data?.items ?? [];

  const prefilledService = prefilledServiceQuery.data ?? null;
  const selectedServiceDisplay =
    prefilledService ??
    (linkedServiceId ? availableServices.find((s) => s.serviceId === linkedServiceId) ?? null : null);

  const filteredServices = useMemo(() => {
    const q = serviceSearch.toLowerCase();
    if (!q) return availableServices;
    return availableServices.filter(
      (svc) =>
        svc.displayName.toLowerCase().includes(q) ||
        svc.domain.toLowerCase().includes(q) ||
        svc.teamName.toLowerCase().includes(q),
    );
  }, [availableServices, serviceSearch]);

  // ── Mutation ─────────────────────────────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!selectedType || !selectedProtocol || !linkedServiceId) {
        throw new Error(t('contracts.create.missingRequiredFields', 'Missing required fields'));
      }

      if (selectedMode === 'ai') {
        const aiDraft = await contractStudioApi.generateFromAi({
          title,
          author: currentActor,
          contractType: selectedType as ContractType,
          protocol: selectedProtocol as ContractProtocol,
          prompt: aiPrompt,
          serviceId: linkedServiceId,
        });
        return { draftId: aiDraft.draftId };
      }

      if (isSoapType) {
        const soapDraft = await contractStudioApi.createSoapDraft({
          title,
          author: currentActor,
          serviceName: soapServiceName || title,
          targetNamespace: soapTargetNamespace || 'http://example.com/service',
          soapVersion,
          serviceId: linkedServiceId,
          description,
          endpointUrl: soapEndpointUrl || undefined,
        });
        if (selectedMode === 'import' && importContent.trim()) {
          await contractStudioApi.updateContent(soapDraft.draftId, {
            specContent: importContent,
            format: 'xml',
            editedBy: currentActor,
          });
        }
        return { draftId: soapDraft.draftId };
      }

      if (isEventType) {
        const eventDraft = await contractStudioApi.createEventDraft({
          title,
          author: currentActor,
          asyncApiVersion,
          serviceId: linkedServiceId,
          description,
          defaultContentType,
        });
        if (selectedMode === 'import' && importContent.trim()) {
          await contractStudioApi.updateContent(eventDraft.draftId, {
            specContent: importContent,
            format: 'json',
            editedBy: currentActor,
          });
        }
        return { draftId: eventDraft.draftId };
      }

      if (isBackgroundServiceType) {
        const bgDraft = await contractStudioApi.createBackgroundServiceDraft({
          title,
          author: currentActor,
          serviceName: bgServiceName || title,
          category: bgCategory,
          triggerType: bgTriggerType,
          serviceId: linkedServiceId,
          description,
          scheduleExpression: bgScheduleExpression || undefined,
        });
        return { draftId: bgDraft.draftId };
      }

      const createdDraft = await contractStudioApi.createDraft({
        title,
        author: currentActor,
        contractType: selectedType as ContractType,
        protocol: selectedProtocol as ContractProtocol,
        serviceId: linkedServiceId,
        description,
      });
      if (selectedMode === 'import' && importContent.trim()) {
        await contractStudioApi.updateContent(createdDraft.draftId, {
          specContent: importContent,
          format: 'yaml',
          editedBy: currentActor,
        });
      }
      return createdDraft;
    },
    onSuccess: (data) => {
      navigate(`/contracts/studio/${data.draftId}`);
    },
  });

  // ── Derived state ─────────────────────────────────────────────────────────

  const protocols = selectedType ? PROTOCOL_BY_TYPE[selectedType] : [];

  const canProceedFromService = linkedServiceId.length > 0 && serviceSupportsContracts;
  const canProceedFromConfigure = !!selectedType && !!selectedMode;
  const canCreate =
    !!title &&
    !!selectedProtocol &&
    !!linkedServiceId &&
    (() => {
      if (selectedMode === 'ai') return !!aiPrompt.trim();
      if (selectedMode === 'import') return !!importContent.trim();
      return true;
    })();

  // ── Navigation ──────────────────────────────────────────────────────────────

  const handleBack = () => {
    if (step === 'service') navigate('/contracts');
    else if (step === 'configure') {
      if (hasPrefilledService) navigate('/contracts');
      else setStep('service');
    } else if (step === 'details') setStep('configure');
  };

  const allSteps: Step[] = hasPrefilledService
    ? ['configure', 'details']
    : ['service', 'configure', 'details'];

  const stepIndex = allSteps.indexOf(step);

  const STEP_LABELS: Record<Step, string> = {
    service: t('contracts.create.stepService', 'Service'),
    configure: t('contracts.create.stepConfigure', 'Contract'),
    details: t('contracts.create.stepDetails', 'Details'),
  };

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <PageContainer className="max-w-3xl">
      {/* Header */}
      <div className="flex items-center gap-3 mb-6">
        <button
          onClick={handleBack}
          className="flex items-center justify-center w-8 h-8 rounded-md text-muted hover:text-heading hover:bg-elevated transition-colors"
        >
          <ArrowLeft size={16} />
        </button>
        <div>
          <h1 className="text-xl font-bold text-heading">
            {t('contracts.create.title', 'Create Service Contract')}
          </h1>
          <p className="text-xs text-muted">
            {t('contracts.create.subtitle', 'Define a new service and its contract specification')}
          </p>
        </div>
      </div>

      {/* Step indicator */}
      <div className="flex items-center gap-0 mb-8">
        {allSteps.map((s, i) => {
          const isActive = step === s;
          const isComplete = i < stepIndex;
          return (
            <div key={s} className="flex items-center">
              <div
                className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium transition-all
                  ${isActive
                    ? 'bg-accent/15 text-accent border border-accent/30'
                    : isComplete
                      ? 'bg-success/10 text-success border border-success/20'
                      : 'bg-elevated text-muted border border-edge'}`}
              >
                <span
                  className={`flex items-center justify-center w-4 h-4 rounded-full text-[10px] font-bold shrink-0
                    ${isActive
                      ? 'bg-accent text-white'
                      : isComplete
                        ? 'bg-success text-white'
                        : 'bg-edge text-muted'}`}
                >
                  {isComplete ? <Check size={8} /> : i + 1}
                </span>
                {STEP_LABELS[s]}
              </div>
              {i < allSteps.length - 1 && <div className="w-6 h-px bg-edge mx-1" />}
            </div>
          );
        })}
      </div>

      {/* ── Step: Service ─────────────────────────────────────────────────────── */}
      {step === 'service' && (
        <div className="space-y-4">
          <div>
            <h2 className="text-base font-semibold text-heading mb-1">
              {t('contracts.create.selectService', 'Select the service for this contract')}
            </h2>
            <p className="text-xs text-muted">
              {t(
                'contracts.create.linkedServiceHint',
                'Contracts are always bound to a catalog service. Link the right service to enable publishing and workspace enrichment.',
              )}
            </p>
          </div>

          {/* Search */}
          <div className="relative">
            <Search
              size={14}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-muted pointer-events-none"
            />
            <input
              type="text"
              value={serviceSearch}
              onChange={(e) => setServiceSearch(e.target.value)}
              placeholder={t(
                'contracts.create.searchServices',
                'Search by name, domain or team...',
              )}
              className="w-full pl-9 pr-4 py-2.5 text-sm bg-elevated border border-edge rounded-lg text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent focus:border-accent transition-colors"
            />
          </div>

          {/* Service cards */}
          {servicesQuery.isLoading ? (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="h-[76px] rounded-lg bg-elevated animate-pulse border border-edge" />
              ))}
            </div>
          ) : filteredServices.length === 0 ? (
            <div className="py-10 text-center text-sm text-muted border border-edge rounded-lg bg-elevated/20">
              {serviceSearch
                ? t('contracts.create.noServicesMatch', 'No services match your search')
                : t('contracts.create.noServices', 'No services available')}
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 max-h-[460px] overflow-y-auto pr-1">
              {filteredServices.map((svc: ServiceListItem) => {
                const isSelected = linkedServiceId === svc.serviceId;
                const svcType = svc.serviceType as ServiceType;
                const supported = supportsContracts(svcType);
                const typeCount = allowedContractTypes(svcType).length;

                return (
                  <button
                    key={svc.serviceId}
                    onClick={() => {
                      if (!supported) return;
                      setLinkedServiceId(svc.serviceId);
                      setSelectedServiceType(svcType);
                      setSelectedType(null);
                      setSelectedMode(null);
                      setSelectedProtocol('');
                    }}
                    disabled={!supported}
                    className={`text-left rounded-lg border p-3.5 transition-all
                      ${isSelected
                        ? 'border-accent bg-accent/5 ring-1 ring-accent/20'
                        : !supported
                          ? 'border-edge bg-elevated/20 opacity-50 cursor-not-allowed'
                          : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2">
                          {isSelected && (
                            <span className="w-4 h-4 rounded-full bg-accent flex items-center justify-center shrink-0">
                              <Check size={9} className="text-white" />
                            </span>
                          )}
                          <span className="text-sm font-semibold text-heading truncate">
                            {svc.displayName}
                          </span>
                        </div>
                        <p className="text-xs text-muted mt-0.5 truncate">
                          {svc.teamName} · {svc.domain}
                        </p>
                      </div>
                      <span className="text-[10px] font-medium px-1.5 py-0.5 rounded bg-elevated text-muted border border-edge shrink-0 whitespace-nowrap">
                        {svc.serviceType}
                      </span>
                    </div>

                    <p
                      className={`text-[10px] mt-2 ${supported ? 'text-muted/70' : 'text-warning/80'}`}
                    >
                      {supported
                        ? typeCount === 1
                          ? t(
                              'contracts.create.oneTypeAvailable',
                              '1 contract type available',
                            )
                          : t(
                              'contracts.create.nTypesAvailable',
                              '{{n}} contract types available',
                              { n: typeCount },
                            )
                        : t(
                            'contracts.create.noTypesAvailable',
                            'No contract types for this service',
                          )}
                    </p>
                  </button>
                );
              })}
            </div>
          )}

          <div className="flex justify-end pt-2">
            <button
              onClick={() => setStep('configure')}
              disabled={!canProceedFromService}
              className="inline-flex items-center gap-1.5 px-5 py-2 text-sm font-medium rounded-lg bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {t('common.next', 'Next')} <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* ── Step: Configure (type + mode) ─────────────────────────────────────── */}
      {step === 'configure' && (
        <div className="space-y-6">
          {/* Locked service banner */}
          {selectedServiceDisplay && (
            <div className="flex items-center gap-2.5 px-4 py-2.5 bg-elevated rounded-lg border border-edge text-xs">
              <Lock size={11} className="text-muted shrink-0" />
              <span className="text-muted shrink-0">
                {t('contracts.create.linkedService', 'Service')}
              </span>
              <span className="text-edge">·</span>
              <span className="text-heading font-semibold truncate">
                {selectedServiceDisplay.displayName}
              </span>
              <span className="text-muted truncate hidden sm:block">
                {selectedServiceDisplay.domain}
              </span>
              <span className="text-[10px] px-1.5 py-0.5 rounded bg-card border border-edge text-muted shrink-0 ml-auto">
                {selectedServiceDisplay.serviceType}
              </span>
            </div>
          )}

          {effectiveServiceType && !serviceSupportsContracts ? (
            <div className="flex items-start gap-2.5 text-xs text-warning bg-warning/10 border border-warning/20 rounded-lg px-4 py-3">
              <Info size={14} className="shrink-0 mt-0.5" />
              <span>
                {t(
                  'contracts.create.serviceTypeNoContracts',
                  'This service type ({{type}}) does not expose public interface contracts.',
                  { type: effectiveServiceType },
                )}
              </span>
            </div>
          ) : (
            <>
              {/* ── Contract type ─── */}
              <div>
                <h2 className="text-sm font-semibold text-heading mb-0.5">
                  {t('contracts.create.selectType', 'What type of contract?')}
                </h2>
                <p className="text-xs text-muted mb-4">
                  {t(
                    'contracts.create.selectTypeHint',
                    "Choose the contract type that matches this service's interface style.",
                  )}
                </p>

                <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                  {filteredContractTypes.map((ct) => {
                    const Icon = TYPE_ICONS[ct.value] ?? Globe;
                    const isSelected = selectedType === ct.value;
                    const ctProtocols = PROTOCOL_BY_TYPE[ct.value as ContractTypeValue];

                    return (
                      <button
                        key={ct.value}
                        onClick={() => {
                          setSelectedType(ct.value as ContractTypeValue);
                          const protos = PROTOCOL_BY_TYPE[ct.value as ContractTypeValue];
                          if (protos.length === 1 && protos[0]) setSelectedProtocol(protos[0]);
                          else setSelectedProtocol('');
                          // Reset mode when type changes so user re-confirms
                          setSelectedMode(null);
                        }}
                        className={`text-left rounded-xl border p-4 transition-all
                          ${isSelected
                            ? 'border-accent bg-accent/5 ring-1 ring-accent/30'
                            : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
                      >
                        <div
                          className={`w-9 h-9 rounded-lg flex items-center justify-center mb-3
                            ${isSelected ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted'}`}
                        >
                          <Icon size={18} />
                        </div>
                        <p
                          className={`text-sm font-semibold mb-2 ${isSelected ? 'text-accent' : 'text-heading'}`}
                        >
                          {t(ct.labelKey, ct.value)}
                        </p>
                        {ctProtocols.length > 0 ? (
                          <div className="flex flex-wrap gap-1">
                            {ctProtocols.map((p) => (
                              <span
                                key={p}
                                className={`text-[10px] px-1.5 py-0.5 rounded font-medium ${PROTOCOL_COLORS[p] ?? 'bg-muted/15 text-muted border border-muted/25'}`}
                              >
                                {p}
                              </span>
                            ))}
                          </div>
                        ) : (
                          <span className="text-[10px] text-muted/60 italic">
                            {t('contracts.create.noProtocol', 'No protocol')}
                          </span>
                        )}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* ── Creation mode — appears after type is selected ─── */}
              {selectedType && (
                <div>
                  <div className="h-px bg-edge mb-6" />
                  <h2 className="text-sm font-semibold text-heading mb-0.5">
                    {t('contracts.create.selectMode', 'How do you want to create it?')}
                  </h2>
                  <p className="text-xs text-muted mb-4">
                    {t(
                      'contracts.create.selectModeHint',
                      "Choose how you'd like to define the contract specification.",
                    )}
                  </p>

                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                    {CREATION_MODES.map((mode) => {
                      const isSelected = selectedMode === mode.id;
                      return (
                        <button
                          key={mode.id}
                          onClick={() => setSelectedMode(mode.id)}
                          className={`text-left rounded-xl border p-4 transition-all
                            ${isSelected
                              ? 'border-accent bg-accent/5 ring-1 ring-accent/30'
                              : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
                        >
                          <div
                            className={`w-9 h-9 rounded-lg flex items-center justify-center mb-3
                              ${isSelected ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted'}`}
                          >
                            <mode.Icon size={18} />
                          </div>
                          <p
                            className={`text-sm font-semibold mb-1 ${isSelected ? 'text-accent' : 'text-heading'}`}
                          >
                            {t(mode.labelKey, mode.id)}
                          </p>
                          <p className="text-xs text-muted leading-relaxed">
                            {t(mode.descriptionKey, `Create using ${mode.id}`)}
                          </p>
                        </button>
                      );
                    })}
                  </div>
                </div>
              )}
            </>
          )}

          <div className="flex justify-between pt-2">
            <button
              onClick={handleBack}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-elevated text-muted hover:text-heading transition-colors"
            >
              <ArrowLeft size={14} /> {t('common.back', 'Back')}
            </button>
            <button
              onClick={() => setStep('details')}
              disabled={!canProceedFromConfigure || !serviceSupportsContracts}
              className="inline-flex items-center gap-1.5 px-5 py-2 text-sm font-medium rounded-lg bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {t('common.next', 'Next')} <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* ── Step: Details ─────────────────────────────────────────────────────── */}
      {step === 'details' && (
        <div className="space-y-5">
          {/* Context breadcrumb — shows the full chain of decisions */}
          {selectedServiceDisplay && selectedType && selectedMode && (
            <div className="flex items-center flex-wrap gap-1.5 px-4 py-2.5 bg-elevated/50 rounded-lg border border-edge text-xs">
              <span className="text-heading font-semibold">
                {selectedServiceDisplay.displayName}
              </span>
              <ChevronRight size={11} className="text-muted shrink-0" />
              <span className="text-heading font-medium">
                {t(`contracts.contractTypes.${selectedType}`, selectedType)}
              </span>
              <ChevronRight size={11} className="text-muted shrink-0" />
              <span className="text-heading font-medium">
                {t(
                  `contracts.create.mode${selectedMode.charAt(0).toUpperCase() + selectedMode.slice(1)}`,
                  selectedMode,
                )}
              </span>
              {protocols.length === 1 && protocols[0] && (
                <>
                  <ChevronRight size={11} className="text-muted shrink-0" />
                  <span
                    className={`text-[10px] px-1.5 py-0.5 rounded font-medium ${PROTOCOL_COLORS[protocols[0]] ?? 'bg-muted/15 text-muted border border-muted/25'}`}
                  >
                    {protocols[0]}
                  </span>
                </>
              )}
            </div>
          )}

          {/* Main form card — divided sections */}
          <div className="rounded-xl border border-edge bg-card divide-y divide-edge">
            {/* Name */}
            <div className="p-4">
              <label className="block text-xs font-medium text-heading mb-1.5">
                {t('contracts.create.name', 'Name')}{' '}
                <span className="text-accent">*</span>
              </label>
              <input
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder={t('contracts.create.namePlaceholder', 'e.g., User Management API')}
                className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent focus:border-accent transition-colors"
              />
            </div>

            {/* Description */}
            <div className="p-4">
              <label className="block text-xs font-medium text-heading mb-1.5">
                {t('contracts.create.description', 'Description')}
                <span className="ml-1.5 text-muted font-normal text-[10px]">
                  {t('common.optional', 'optional')}
                </span>
              </label>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={3}
                placeholder={t(
                  'contracts.create.descriptionPlaceholder',
                  'Describe what this service does...',
                )}
                className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent focus:border-accent transition-colors resize-none"
              />
            </div>

            {/* Protocol — only when multiple choices */}
            {protocols.length > 1 && (
              <div className="p-4">
                <label className="block text-xs font-medium text-heading mb-1.5">
                  {t('contracts.selectProtocol', 'Protocol')}{' '}
                  <span className="text-accent">*</span>
                </label>
                <select
                  value={selectedProtocol}
                  onChange={(e) => setSelectedProtocol(e.target.value as ContractProtocol)}
                  className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent focus:border-accent transition-colors"
                >
                  <option value="">{t('contracts.selectProtocol', 'Select Protocol')}</option>
                  {protocols.map((p) => (
                    <option key={p} value={p}>
                      {t(`contracts.protocols.${p}`, p)}
                    </option>
                  ))}
                </select>
              </div>
            )}

            {/* Import spec content */}
            {selectedMode === 'import' && (
              <div className="p-4">
                <label className="block text-xs font-medium text-heading mb-1.5">
                  {t('contracts.create.importContent', 'Specification Content')}{' '}
                  <span className="text-accent">*</span>
                  {isSoapType && (
                    <span className="ml-1.5 text-muted font-normal text-[10px]">
                      {t('contracts.create.wsdlXmlHint', 'WSDL XML')}
                    </span>
                  )}
                  {isEventType && (
                    <span className="ml-1.5 text-muted font-normal text-[10px]">
                      {t('contracts.create.asyncApiJsonHint', 'AsyncAPI JSON')}
                    </span>
                  )}
                </label>
                <textarea
                  value={importContent}
                  onChange={(e) => setImportContent(e.target.value)}
                  rows={10}
                  placeholder={
                    isSoapType
                      ? t(
                          'contracts.create.wsdlPlaceholder',
                          'Paste your WSDL XML here (<?xml version="1.0"?><definitions ...>)...',
                        )
                      : isEventType
                        ? t(
                            'contracts.create.asyncApiPlaceholder',
                            'Paste your AsyncAPI JSON here ({"asyncapi":"2.6.0","info":{"title":"..."},...})...',
                          )
                        : t(
                            'contracts.specContentPlaceholder',
                            'Paste your specification here (JSON/YAML/XML)...',
                          )
                  }
                  className="w-full text-xs font-mono bg-elevated border border-edge rounded-lg px-3 py-2.5 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent focus:border-accent transition-colors resize-none"
                />
              </div>
            )}

            {/* AI prompt */}
            {selectedMode === 'ai' && (
              <div className="p-4">
                <label className="block text-xs font-medium text-heading mb-1.5">
                  {t('contracts.create.aiPrompt', 'AI Prompt')}{' '}
                  <span className="text-accent">*</span>
                </label>
                <textarea
                  value={aiPrompt}
                  onChange={(e) => setAiPrompt(e.target.value)}
                  rows={6}
                  placeholder={t(
                    'contracts.create.aiPromptPlaceholder',
                    'Describe the API you want to generate. Include endpoints, operations, data models...',
                  )}
                  className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent focus:border-accent transition-colors resize-none"
                />
                <p className="mt-1.5 text-[10px] text-muted">
                  {t(
                    'contracts.create.aiPromptHint',
                    'The AI will generate a specification draft based on your description. You can refine it in the studio after creation.',
                  )}
                </p>
              </div>
            )}

            {/* SOAP-specific metadata */}
            {isSoapType && (
              <div className="p-4 space-y-3">
                <p className="text-[10px] text-muted font-semibold uppercase tracking-wider">
                  {t('contracts.create.soapMetadata', 'SOAP Service Metadata')}
                </p>

                <div>
                  <label className="block text-xs font-medium text-heading mb-1.5">
                    {t('contracts.create.soapServiceName', 'Service Name')}
                  </label>
                  <input
                    type="text"
                    value={soapServiceName}
                    onChange={(e) => setSoapServiceName(e.target.value)}
                    placeholder={t(
                      'contracts.create.soapServiceNamePlaceholder',
                      'e.g., UserService',
                    )}
                    className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                  />
                </div>

                <div>
                  <label className="block text-xs font-medium text-heading mb-1.5">
                    {t('contracts.create.soapTargetNamespace', 'Target Namespace')}
                  </label>
                  <input
                    type="text"
                    value={soapTargetNamespace}
                    onChange={(e) => setSoapTargetNamespace(e.target.value)}
                    placeholder="http://example.com/service"
                    className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                  />
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.soapVersion', 'SOAP Version')}
                    </label>
                    <select
                      value={soapVersion}
                      onChange={(e) => setSoapVersion(e.target.value as '1.1' | '1.2')}
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    >
                      <option value="1.1">SOAP 1.1</option>
                      <option value="1.2">SOAP 1.2</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.soapEndpointUrl', 'Endpoint URL')}
                      <span className="ml-1 text-muted font-normal text-[10px]">
                        {t('common.optional', '(optional)')}
                      </span>
                    </label>
                    <input
                      type="text"
                      value={soapEndpointUrl}
                      onChange={(e) => setSoapEndpointUrl(e.target.value)}
                      placeholder="http://example.com/service"
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    />
                  </div>
                </div>
              </div>
            )}

            {/* Event/AsyncAPI metadata */}
            {isEventType && (
              <div className="p-4 space-y-3">
                <p className="text-[10px] text-muted font-semibold uppercase tracking-wider">
                  {t('contracts.create.asyncApiMetadata', 'AsyncAPI Event Metadata')}
                </p>
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.asyncApiVersion', 'AsyncAPI Version')}
                    </label>
                    <select
                      value={asyncApiVersion}
                      onChange={(e) => setAsyncApiVersion(e.target.value)}
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    >
                      <option value="2.6.0">AsyncAPI 2.6.0</option>
                      <option value="3.0.0">AsyncAPI 3.0.0</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.defaultContentType', 'Default Content Type')}
                    </label>
                    <select
                      value={defaultContentType}
                      onChange={(e) => setDefaultContentType(e.target.value)}
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    >
                      <option value="application/json">application/json</option>
                      <option value="application/avro">application/avro</option>
                      <option value="application/protobuf">application/protobuf</option>
                    </select>
                  </div>
                </div>
              </div>
            )}

            {/* Background Service metadata */}
            {isBackgroundServiceType && (
              <div className="p-4 space-y-3">
                <p className="text-[10px] text-muted font-semibold uppercase tracking-wider">
                  {t('contracts.create.bgServiceMetadata', 'Background Service Metadata')}
                </p>

                <div>
                  <label className="block text-xs font-medium text-heading mb-1.5">
                    {t('contracts.create.bgServiceName', 'Service / Job Name')}{' '}
                    <span className="text-accent">*</span>
                  </label>
                  <input
                    type="text"
                    value={bgServiceName}
                    onChange={(e) => setBgServiceName(e.target.value)}
                    placeholder={t(
                      'contracts.create.bgServiceNamePlaceholder',
                      'e.g., OrderExpirationJob, ReportGeneratorWorker',
                    )}
                    className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                  />
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.bgCategory', 'Category')}
                    </label>
                    <select
                      value={bgCategory}
                      onChange={(e) => setBgCategory(e.target.value)}
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    >
                      <option value="Job">{t('contracts.create.bgCategoryJob', 'Job')}</option>
                      <option value="Worker">
                        {t('contracts.create.bgCategoryWorker', 'Worker')}
                      </option>
                      <option value="Scheduler">
                        {t('contracts.create.bgCategoryScheduler', 'Scheduler')}
                      </option>
                      <option value="Processor">
                        {t('contracts.create.bgCategoryProcessor', 'Processor')}
                      </option>
                      <option value="Exporter">
                        {t('contracts.create.bgCategoryExporter', 'Exporter')}
                      </option>
                      <option value="Notifier">
                        {t('contracts.create.bgCategoryNotifier', 'Notifier')}
                      </option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.bgTriggerType', 'Trigger Type')}
                    </label>
                    <select
                      value={bgTriggerType}
                      onChange={(e) => setBgTriggerType(e.target.value)}
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    >
                      <option value="OnDemand">
                        {t('contracts.create.bgTriggerOnDemand', 'On Demand')}
                      </option>
                      <option value="Cron">{t('contracts.create.bgTriggerCron', 'Cron')}</option>
                      <option value="Interval">
                        {t('contracts.create.bgTriggerInterval', 'Interval')}
                      </option>
                      <option value="EventTriggered">
                        {t('contracts.create.bgTriggerEventTriggered', 'Event Triggered')}
                      </option>
                      <option value="Continuous">
                        {t('contracts.create.bgTriggerContinuous', 'Continuous')}
                      </option>
                    </select>
                  </div>
                </div>

                {(bgTriggerType === 'Cron' || bgTriggerType === 'Interval') && (
                  <div>
                    <label className="block text-xs font-medium text-heading mb-1.5">
                      {t('contracts.create.bgScheduleExpression', 'Schedule Expression')}
                    </label>
                    <input
                      type="text"
                      value={bgScheduleExpression}
                      onChange={(e) => setBgScheduleExpression(e.target.value)}
                      placeholder={
                        bgTriggerType === 'Cron'
                          ? t(
                              'contracts.create.bgCronPlaceholder',
                              'e.g., 0 * * * * (every hour)',
                            )
                          : t(
                              'contracts.create.bgIntervalPlaceholder',
                              'e.g., PT5M (ISO 8601 interval)',
                            )
                      }
                      className="w-full text-sm bg-elevated border border-edge rounded-lg px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent transition-colors"
                    />
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Error */}
          {createMutation.isError && (
            <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-lg px-4 py-3">
              {t('contracts.errors.createVersionFailed', 'Failed to create contract')}
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-between pt-1">
            <button
              onClick={() => setStep('configure')}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg bg-elevated text-muted hover:text-heading transition-colors"
            >
              <ArrowLeft size={14} /> {t('common.back', 'Back')}
            </button>
            <button
              onClick={() => createMutation.mutate()}
              disabled={!canCreate || createMutation.isPending}
              className="inline-flex items-center gap-1.5 px-6 py-2 text-sm font-medium rounded-lg bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {createMutation.isPending
                ? t('contracts.create.creatingDraft', 'Creating draft...')
                : t('contracts.create.createDraft', 'Create Draft')}
              {!createMutation.isPending && <ArrowRight size={14} />}
            </button>
          </div>
        </div>
      )}
    </PageContainer>
  );
}
