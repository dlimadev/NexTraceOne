import { useState } from 'react';
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
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';
import { SERVICE_TYPES as CONTRACT_TYPES, PROTOCOL_BY_TYPE, type ContractTypeValue } from '../shared/constants';
import { supportsContracts, allowedContractTypes } from '../shared/serviceContractPolicy';
import { contractStudioApi } from '../api/contractStudio';
import type { ContractType, ContractProtocol, ServiceListItem } from '../types';
import type { ServiceType } from '../../../types';
import { useAuth } from '../../../contexts/AuthContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';

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

type CreationMode = 'visual' | 'import' | 'ai';

interface CreationModeOption {
  id: CreationMode;
  labelKey: string;
  descriptionKey: string;
  Icon: React.ComponentType<{ size?: number; className?: string }>;
}

const CREATION_MODES: CreationModeOption[] = [
  { id: 'visual', labelKey: 'contracts.create.modeVisual', descriptionKey: 'contracts.create.modeVisualDesc', Icon: Columns },
  { id: 'import', labelKey: 'contracts.create.modeImport', descriptionKey: 'contracts.create.modeImportDesc', Icon: Upload },
  { id: 'ai', labelKey: 'contracts.create.modeAi', descriptionKey: 'contracts.create.modeAiDesc', Icon: Sparkles },
];

type Step = 'service' | 'type' | 'mode' | 'details';

/**
 * Página de criação de novo contrato.
 * Fluxo obrigatório: service (se não pré-preenchido) → tipo (filtrado por serviceType) → modo → detalhes.
 * O serviço é obrigatório; o tipo de contrato é filtrado pela política ServiceContractPolicy.
 */
export function CreateContractPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const currentActor = user?.email || user?.fullName || user?.id || 'system';

  const prefilledServiceId = searchParams.get('serviceId') ?? '';
  const hasPrefilledService = prefilledServiceId.length > 0;

  const [step, setStep] = useState<Step>(hasPrefilledService ? 'type' : 'service');
  const [selectedType, setSelectedType] = useState<ContractTypeValue | null>(null);
  const [selectedMode, setSelectedMode] = useState<CreationMode | null>(null);
  const [selectedProtocol, setSelectedProtocol] = useState<ContractProtocol | ''>('');
  const [linkedServiceId, setLinkedServiceId] = useState(prefilledServiceId);
  const [selectedServiceType, setSelectedServiceType] = useState<ServiceType | null>(null);

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

  const servicesQuery = useQuery({
    queryKey: ['catalog-services-for-contracts'],
    queryFn: () => serviceCatalogApi.listServices(),
  });

  // When service is pre-filled via URL param, load its data to get serviceType
  const prefilledServiceQuery = useQuery({
    queryKey: ['catalog-service-for-contract-create', prefilledServiceId],
    queryFn: () => serviceCatalogApi.listServices(),
    enabled: hasPrefilledService,
    select: (data) => data?.items?.find((s) => s.serviceId === prefilledServiceId) ?? null,
  });

  // Determine serviceType for the selected service
  const effectiveServiceType: ServiceType | null =
    selectedServiceType ??
    (prefilledServiceQuery.data?.serviceType ?? null);

  // Filtered contract types based on the selected service's serviceType
  const filteredContractTypes = effectiveServiceType
    ? CONTRACT_TYPES.filter((ct) =>
        allowedContractTypes(effectiveServiceType).includes(ct.value as ContractTypeValue),
      )
    : CONTRACT_TYPES;

  const serviceSupportsContracts = effectiveServiceType
    ? supportsContracts(effectiveServiceType)
    : true;

  const availableServices = servicesQuery.data?.items ?? [];

  const prefilledService = prefilledServiceQuery.data ?? null;
  const selectedServiceDisplay =
    prefilledService ??
    (linkedServiceId
      ? availableServices.find((s) => s.serviceId === linkedServiceId) ?? null
      : null);

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

  const protocols = selectedType ? PROTOCOL_BY_TYPE[selectedType] : [];

  const canProceedFromService = linkedServiceId.length > 0;
  const canProceedToMode = !!selectedType;
  const canProceedToDetails = !!selectedMode;
  const canCreate = !!title && !!selectedProtocol && !!linkedServiceId && (() => {
    if (selectedMode === 'ai') return !!aiPrompt.trim();
    if (selectedMode === 'import') return !!importContent.trim();
    return true;
  })();

  // Back navigation
  const handleBack = () => {
    if (step === 'service') navigate('/contracts');
    else if (step === 'type') setStep(hasPrefilledService ? ('type' as Step) : 'service');
    else if (step === 'mode') setStep('type');
    else if (step === 'details') setStep('mode');
  };

  const allSteps: Step[] = hasPrefilledService
    ? ['type', 'mode', 'details']
    : ['service', 'type', 'mode', 'details'];

  return (
    <PageContainer className="max-w-4xl">
      {/* Header */}
      <div className="flex items-center gap-3 mb-8">
        <button
          onClick={handleBack}
          className="text-muted hover:text-heading transition-colors"
        >
          <ArrowLeft size={18} />
        </button>
        <div>
          <h1 className="text-xl sm:text-2xl font-bold text-heading">
            {t('contracts.create.title', 'Create Service Contract')}
          </h1>
          <p className="text-xs text-muted">
            {t('contracts.create.subtitle', 'Define a new service and its contract specification')}
          </p>
        </div>
      </div>

      {/* Step indicators */}
      <div className="flex items-center gap-2 mb-8">
        {allSteps.map((s, i) => (
          <div key={s} className="flex items-center gap-2">
            <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-medium
              ${step === s ? 'bg-accent text-white' :
                i < allSteps.indexOf(step) ? 'bg-success/15 text-success' :
                'bg-elevated text-muted'}`}
            >
              {i + 1}
            </div>
            <span className={`text-xs ${step === s ? 'text-heading font-medium' : 'text-muted'}`}>
              {t(`contracts.create.step${s.charAt(0).toUpperCase() + s.slice(1)}`, s)}
            </span>
            {i < allSteps.length - 1 && <div className="w-8 h-px bg-edge" />}
          </div>
        ))}
      </div>

      {/* Step 0 (when no prefilled service): Service selection */}
      {step === 'service' && (
        <div className="space-y-4">
          <h2 className="text-sm font-semibold text-heading mb-4">
            {t('contracts.create.selectService', 'Select the service for this contract')} *
          </h2>
          <Card>
            <CardBody>
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.create.linkedService', 'Linked Service')} *
                </label>
                {servicesQuery.isLoading ? (
                  <div className="text-xs text-muted py-2">
                    {t('common.loading', 'Loading...')}
                  </div>
                ) : (
                  <select
                    value={linkedServiceId}
                    onChange={(e) => {
                      const svcId = e.target.value;
                      setLinkedServiceId(svcId);
                      const svc = availableServices.find((s) => s.serviceId === svcId);
                      const svcType = (svc?.serviceType as ServiceType) ?? null;
                      setSelectedServiceType(svcType);
                      // Reset downstream selections when service changes
                      setSelectedType(null);
                      setSelectedMode(null);
                      setSelectedProtocol('');
                    }}
                    className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                  >
                    <option value="">{t('contracts.create.selectServicePlaceholder', 'Select a service...')}</option>
                    {availableServices.map((service: ServiceListItem) => (
                      <option key={service.serviceId} value={service.serviceId}>
                        {service.displayName} · {service.domain} · {service.teamName}
                      </option>
                    ))}
                  </select>
                )}
                <p className="mt-1 text-[10px] text-muted">
                  {t('contracts.create.linkedServiceHint', 'Linking a real catalog service enables publish and workspace enrichment without artificial fallback.')}
                </p>
              </div>

              {/* No contracts support message */}
              {selectedServiceType && !serviceSupportsContracts && (
                <div className="mt-4 flex items-start gap-2 text-xs text-warning bg-warning/10 border border-warning/20 rounded-md px-3 py-2">
                  <Info size={14} className="shrink-0 mt-0.5" />
                  <span>
                    {t(
                      'contracts.create.serviceTypeNoContracts',
                      'This service type ({{type}}) does not expose public interface contracts.',
                      { type: selectedServiceType },
                    )}
                  </span>
                </div>
              )}
            </CardBody>
          </Card>

          <div className="flex justify-end mt-6">
            <button
              onClick={() => setStep('type')}
              disabled={!canProceedFromService || !serviceSupportsContracts}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {t('common.next', 'Next')} <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* Step 1: Type selection */}
      {step === 'type' && (
        <div className="space-y-4">
          {/* Locked service banner */}
          {selectedServiceDisplay && (
            <div className="flex items-center gap-2 px-3 py-2 bg-elevated rounded-md border border-edge text-xs">
              <Lock size={12} className="text-muted shrink-0" />
              <span className="text-muted">{t('contracts.create.linkedService', 'Linked Service')}:</span>
              <span className="text-heading font-medium">{selectedServiceDisplay.displayName}</span>
              <span className="text-muted">· {selectedServiceDisplay.domain}</span>
            </div>
          )}

          <h2 className="text-sm font-semibold text-heading mb-4">
            {t('contracts.create.selectType', 'What type of contract?')}
          </h2>

          {/* No contracts support message for pre-filled service */}
          {effectiveServiceType && !serviceSupportsContracts ? (
            <div className="flex items-start gap-2 text-xs text-warning bg-warning/10 border border-warning/20 rounded-md px-3 py-3">
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
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {filteredContractTypes.map((st) => {
                const Icon = TYPE_ICONS[st.value] ?? Globe;
                const isSelected = selectedType === st.value;

                return (
                  <button
                    key={st.value}
                    onClick={() => {
                      setSelectedType(st.value);
                      const protos = PROTOCOL_BY_TYPE[st.value];
                      const singleProtocol = protos[0];
                      if (protos.length === 1 && singleProtocol) setSelectedProtocol(singleProtocol);
                      else setSelectedProtocol('');
                    }}
                    className={`text-left rounded-lg border p-4 transition-all
                      ${isSelected
                        ? 'border-accent bg-accent/5 ring-1 ring-accent/30'
                        : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
                  >
                    <div className="flex items-center gap-3 mb-2">
                      <div className={`w-9 h-9 rounded-lg flex items-center justify-center
                        ${isSelected ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted'}`}>
                        <Icon size={18} />
                      </div>
                      <h3 className="text-sm font-medium text-heading">
                        {t(st.labelKey, st.value)}
                      </h3>
                    </div>
                    <p className="text-xs text-muted">
                      {t(`contracts.create.typeDesc${st.value}`, `Create a ${st.value} contract`)}
                    </p>
                  </button>
                );
              })}
            </div>
          )}

          <div className="flex justify-between mt-6">
            <button
              onClick={() => setStep(hasPrefilledService ? 'service' : 'service')}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
            >
              <ArrowLeft size={14} /> {t('common.back', 'Back')}
            </button>
            <button
              onClick={() => setStep('mode')}
              disabled={!canProceedToMode || !serviceSupportsContracts}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {t('common.next', 'Next')} <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* Step 2: Creation mode */}
      {step === 'mode' && (
        <div className="space-y-4">
          <h2 className="text-sm font-semibold text-heading mb-4">
            {t('contracts.create.selectMode', 'How do you want to create it?')}
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {CREATION_MODES.map((mode) => {
              const isSelected = selectedMode === mode.id;

              return (
                <button
                  key={mode.id}
                  onClick={() => setSelectedMode(mode.id)}
                  className={`text-left rounded-lg border p-4 transition-all
                    ${isSelected
                      ? 'border-accent bg-accent/5 ring-1 ring-accent/30'
                      : 'border-edge bg-card hover:border-accent/30 hover:bg-elevated/30'}`}
                >
                  <div className="flex items-center gap-3 mb-2">
                    <div className={`w-9 h-9 rounded-lg flex items-center justify-center
                      ${isSelected ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted'}`}>
                      <mode.Icon size={18} />
                    </div>
                    <h3 className="text-sm font-medium text-heading">
                      {t(mode.labelKey, mode.id)}
                    </h3>
                  </div>
                  <p className="text-xs text-muted">
                    {t(mode.descriptionKey, `Create using ${mode.id}`)}
                  </p>
                </button>
              );
            })}
          </div>

          <div className="flex justify-between mt-6">
            <button
              onClick={() => setStep('type')}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
            >
              <ArrowLeft size={14} /> {t('common.back', 'Back')}
            </button>
            <button
              onClick={() => setStep('details')}
              disabled={!canProceedToDetails}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {t('common.next', 'Next')} <ArrowRight size={14} />
            </button>
          </div>
        </div>
      )}

      {/* Step 3: Details */}
      {step === 'details' && (
        <div className="space-y-6">
          <h2 className="text-sm font-semibold text-heading mb-4">
            {t('contracts.create.details', 'Contract Details')}
          </h2>

          <Card>
            <CardBody className="space-y-4">
              {/* Title */}
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.create.name', 'Name')} *
                </label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder={t('contracts.create.namePlaceholder', 'e.g., User Management API')}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>

              {/* Description */}
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.create.description', 'Description')}
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={3}
                  placeholder={t('contracts.create.descriptionPlaceholder', 'Describe what this service does...')}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                />
              </div>

              {/* Linked Service — read-only locked display */}
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.create.linkedService', 'Linked Service')} *
                </label>
                {selectedServiceDisplay ? (
                  <div className="flex items-center gap-2 px-3 py-2 bg-elevated border border-edge rounded-md text-xs">
                    <Lock size={12} className="text-muted shrink-0" />
                    <span className="text-heading font-medium">{selectedServiceDisplay.displayName}</span>
                    <span className="text-muted">· {selectedServiceDisplay.domain} · {selectedServiceDisplay.teamName}</span>
                  </div>
                ) : (
                  <div className="text-xs text-muted italic px-1">
                    {t('contracts.create.noServiceSelected', 'No service selected')}
                  </div>
                )}
              </div>

              {/* Protocol selection */}
              {protocols.length > 1 && (
                <div>
                  <label className="block text-xs font-medium text-heading mb-1">
                    {t('contracts.selectProtocol', 'Protocol')} *
                  </label>
                  <select
                    value={selectedProtocol}
                    onChange={(e) => setSelectedProtocol(e.target.value as ContractProtocol)}
                    className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                  >
                    <option value="">{t('contracts.selectProtocol', 'Select Protocol')}</option>
                    {protocols.map((p) => (
                      <option key={p} value={p}>{t(`contracts.protocols.${p}`, p)}</option>
                    ))}
                  </select>
                </div>
              )}

              {protocols.length === 1 && protocols[0] && (
                <div className="text-xs text-muted">
                  {t('contracts.protocol', 'Protocol')}: <span className="text-heading font-medium">{t(`contracts.protocols.${protocols[0]}`, { defaultValue: protocols[0] })}</span>
                </div>
              )}

              {/* Import content */}
              {selectedMode === 'import' && (
                <div>
                  <label className="block text-xs font-medium text-heading mb-1">
                    {t('contracts.create.importContent', 'Specification Content')}
                    {isSoapType && (
                      <span className="ml-1 text-muted font-normal">
                        {t('contracts.create.wsdlXmlHint', '(WSDL XML)')}
                      </span>
                    )}
                    {isEventType && (
                      <span className="ml-1 text-muted font-normal">
                        {t('contracts.create.asyncApiJsonHint', '(AsyncAPI JSON)')}
                      </span>
                    )}
                  </label>
                  <textarea
                    value={importContent}
                    onChange={(e) => setImportContent(e.target.value)}
                    rows={8}
                    placeholder={isSoapType
                      ? t('contracts.create.wsdlPlaceholder', 'Paste your WSDL XML here (<?xml version="1.0"?><definitions ...>)...')
                      : isEventType
                        ? t('contracts.create.asyncApiPlaceholder', 'Paste your AsyncAPI JSON here ({"asyncapi":"2.6.0","info":{"title":"..."},...})...')
                        : t('contracts.specContentPlaceholder', 'Paste your specification here (JSON/YAML/XML)...')}
                    className="w-full text-xs font-mono bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                  />
                </div>
              )}

              {/* AI prompt */}
              {selectedMode === 'ai' && (
                <div>
                  <label className="block text-xs font-medium text-heading mb-1">
                    {t('contracts.create.aiPrompt', 'AI Prompt')} *
                  </label>
                  <textarea
                    value={aiPrompt}
                    onChange={(e) => setAiPrompt(e.target.value)}
                    rows={6}
                    placeholder={t('contracts.create.aiPromptPlaceholder', 'Describe the API you want to generate. Include endpoints, operations, data models...')}
                    className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                  />
                  <p className="mt-1 text-[10px] text-muted">
                    {t('contracts.create.aiPromptHint', 'The AI will generate a specification draft based on your description. You can refine it in the studio after creation.')}
                  </p>
                </div>
              )}

              {/* SOAP-specific metadata fields */}
              {isSoapType && (
                <div className="space-y-3 pt-1 border-t border-edge">
                  <p className="text-[10px] text-muted font-medium uppercase tracking-wider">
                    {t('contracts.create.soapMetadata', 'SOAP Service Metadata')}
                  </p>

                  <div>
                    <label className="block text-xs font-medium text-heading mb-1">
                      {t('contracts.create.soapServiceName', 'Service Name')}
                    </label>
                    <input
                      type="text"
                      value={soapServiceName}
                      onChange={(e) => setSoapServiceName(e.target.value)}
                      placeholder={t('contracts.create.soapServiceNamePlaceholder', 'e.g., UserService')}
                      className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
                    />
                  </div>

                  <div>
                    <label className="block text-xs font-medium text-heading mb-1">
                      {t('contracts.create.soapTargetNamespace', 'Target Namespace')}
                    </label>
                    <input
                      type="text"
                      value={soapTargetNamespace}
                      onChange={(e) => setSoapTargetNamespace(e.target.value)}
                      placeholder={t('contracts.create.placeholder.soapTargetNamespace', 'http://example.com/service')}
                      className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
                    />
                  </div>

                  <div className="flex gap-3">
                    <div className="flex-1">
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.soapVersion', 'SOAP Version')}
                      </label>
                      <select
                        value={soapVersion}
                        onChange={(e) => setSoapVersion(e.target.value as '1.1' | '1.2')}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                      >
                        <option value="1.1">SOAP 1.1</option>
                        <option value="1.2">SOAP 1.2</option>
                      </select>
                    </div>

                    <div className="flex-1">
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.soapEndpointUrl', 'Endpoint URL')}
                        <span className="ml-1 text-muted font-normal">{t('common.optional', '(optional)')}</span>
                      </label>
                      <input
                        type="text"
                        value={soapEndpointUrl}
                        onChange={(e) => setSoapEndpointUrl(e.target.value)}
                        placeholder={t('contracts.create.placeholder.soapEndpointUrl', 'http://example.com/service')}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
                      />
                    </div>
                  </div>
                </div>
              )}

              {/* Event/AsyncAPI-specific metadata fields */}
              {isEventType && (
                <div className="space-y-3 pt-1 border-t border-edge">
                  <p className="text-[10px] text-muted font-medium uppercase tracking-wider">
                    {t('contracts.create.asyncApiMetadata', 'AsyncAPI Event Metadata')}
                  </p>

                  <div className="flex gap-3">
                    <div className="flex-1">
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.asyncApiVersion', 'AsyncAPI Version')}
                      </label>
                      <select
                        value={asyncApiVersion}
                        onChange={(e) => setAsyncApiVersion(e.target.value)}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                      >
                        <option value="2.6.0">AsyncAPI 2.6.0</option>
                        <option value="3.0.0">AsyncAPI 3.0.0</option>
                      </select>
                    </div>

                    <div className="flex-1">
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.defaultContentType', 'Default Content Type')}
                      </label>
                      <select
                        value={defaultContentType}
                        onChange={(e) => setDefaultContentType(e.target.value)}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                      >
                        <option value="application/json">application/json</option>
                        <option value="application/avro">application/avro</option>
                        <option value="application/protobuf">application/protobuf</option>
                      </select>
                    </div>
                  </div>
                </div>
              )}

              {/* Background Service-specific metadata fields */}
              {isBackgroundServiceType && (
                <div className="space-y-3 pt-1 border-t border-edge">
                  <p className="text-[10px] text-muted font-medium uppercase tracking-wider">
                    {t('contracts.create.bgServiceMetadata', 'Background Service Metadata')}
                  </p>

                  <div>
                    <label className="block text-xs font-medium text-heading mb-1">
                      {t('contracts.create.bgServiceName', 'Service / Job Name')} *
                    </label>
                    <input
                      type="text"
                      value={bgServiceName}
                      onChange={(e) => setBgServiceName(e.target.value)}
                      placeholder={t('contracts.create.bgServiceNamePlaceholder', 'e.g., OrderExpirationJob, ReportGeneratorWorker')}
                      className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
                    />
                  </div>

                  <div className="flex gap-3">
                    <div className="flex-1">
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.bgCategory', 'Category')}
                      </label>
                      <select
                        value={bgCategory}
                        onChange={(e) => setBgCategory(e.target.value)}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                      >
                        <option value="Job">{t('contracts.create.bgCategoryJob', 'Job')}</option>
                        <option value="Worker">{t('contracts.create.bgCategoryWorker', 'Worker')}</option>
                        <option value="Scheduler">{t('contracts.create.bgCategoryScheduler', 'Scheduler')}</option>
                        <option value="Processor">{t('contracts.create.bgCategoryProcessor', 'Processor')}</option>
                        <option value="Exporter">{t('contracts.create.bgCategoryExporter', 'Exporter')}</option>
                        <option value="Notifier">{t('contracts.create.bgCategoryNotifier', 'Notifier')}</option>
                      </select>
                    </div>

                    <div className="flex-1">
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.bgTriggerType', 'Trigger Type')}
                      </label>
                      <select
                        value={bgTriggerType}
                        onChange={(e) => setBgTriggerType(e.target.value)}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                      >
                        <option value="OnDemand">{t('contracts.create.bgTriggerOnDemand', 'On Demand')}</option>
                        <option value="Cron">{t('contracts.create.bgTriggerCron', 'Cron')}</option>
                        <option value="Interval">{t('contracts.create.bgTriggerInterval', 'Interval')}</option>
                        <option value="EventTriggered">{t('contracts.create.bgTriggerEventTriggered', 'Event Triggered')}</option>
                        <option value="Continuous">{t('contracts.create.bgTriggerContinuous', 'Continuous')}</option>
                      </select>
                    </div>
                  </div>

                  {(bgTriggerType === 'Cron' || bgTriggerType === 'Interval') && (
                    <div>
                      <label className="block text-xs font-medium text-heading mb-1">
                        {t('contracts.create.bgScheduleExpression', 'Schedule Expression')}
                      </label>
                      <input
                        type="text"
                        value={bgScheduleExpression}
                        onChange={(e) => setBgScheduleExpression(e.target.value)}
                        placeholder={bgTriggerType === 'Cron'
                          ? t('contracts.create.bgCronPlaceholder', 'e.g., 0 * * * * (every hour)')
                          : t('contracts.create.bgIntervalPlaceholder', 'e.g., PT5M (ISO 8601 interval)')}
                        className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent"
                      />
                    </div>
                  )}
                </div>
              )}
            </CardBody>
          </Card>

          {createMutation.isError && (
            <div className="text-xs text-critical bg-critical/15 border border-critical/25 rounded-md px-3 py-2">
              {t('contracts.errors.createVersionFailed', 'Failed to create contract')}
            </div>
          )}

          <div className="flex justify-between">
            <button
              onClick={() => setStep('mode')}
              className="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md bg-elevated text-muted hover:text-heading transition-colors"
            >
              <ArrowLeft size={14} /> {t('common.back', 'Back')}
            </button>
            <button
              onClick={() => createMutation.mutate()}
              disabled={!canCreate || createMutation.isPending}
              className="inline-flex items-center gap-1.5 px-5 py-2 text-sm font-medium rounded-md bg-accent text-white hover:bg-accent/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              {createMutation.isPending
                ? t('contracts.create.creatingDraft', 'Creating draft...')
                : t('contracts.create.createDraft', 'Create Draft')}
            </button>
          </div>
        </div>
      )}
    </PageContainer>
  );
}

