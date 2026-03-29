import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  Globe,
  Server,
  Zap,
  Cog,
  Database,
  Columns,
  Upload,
  Sparkles,
  ArrowLeft,
  ArrowRight,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';
import { SERVICE_TYPES, PROTOCOL_BY_TYPE, type ServiceTypeValue } from '../shared/constants';
import { contractStudioApi } from '../api/contractStudio';
import type { ContractType, ContractProtocol, ServiceListItem } from '../types';
import { useAuth } from '../../../contexts/AuthContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';

const TYPE_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
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

type Step = 'type' | 'mode' | 'details';

/**
 * Página de criação de novo serviço/contrato.
 * Fluxo em 3 passos: escolha de tipo → modo de criação → detalhes.
 * Para contratos SOAP com modo import, usa o endpoint dedicado createSoapDraft
 * que popula o SoapDraftMetadata com os campos específicos do serviço.
 */
export function CreateServicePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user } = useAuth();
  const currentActor = user?.email || user?.fullName || user?.id || 'system';

  const [step, setStep] = useState<Step>('type');
  const [selectedType, setSelectedType] = useState<ServiceTypeValue | null>(null);
  const [selectedMode, setSelectedMode] = useState<CreationMode | null>(null);
  const [selectedProtocol, setSelectedProtocol] = useState<ContractProtocol | ''>('');
  const [linkedServiceId, setLinkedServiceId] = useState('');

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [importContent, setImportContent] = useState('');
  const [aiPrompt, setAiPrompt] = useState('');

  // SOAP-specific fields for import/create flows
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

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!selectedType || !selectedProtocol) throw new Error('Missing required fields');

      // AI Generation mode uses dedicated generateFromAi endpoint
      if (selectedMode === 'ai') {
        const aiDraft = await contractStudioApi.generateFromAi({
          title,
          author: currentActor,
          contractType: selectedType as ContractType,
          protocol: selectedProtocol as ContractProtocol,
          prompt: aiPrompt,
          serviceId: linkedServiceId || undefined,
        });

        return { draftId: aiDraft.draftId };
      }

      // SOAP type uses dedicated createSoapDraft to populate SoapDraftMetadata
      if (isSoapType) {
        const soapDraft = await contractStudioApi.createSoapDraft({
          title,
          author: currentActor,
          serviceName: soapServiceName || title,
          targetNamespace: soapTargetNamespace || 'http://example.com/service',
          soapVersion,
          serviceId: linkedServiceId || undefined,
          description,
          endpointUrl: soapEndpointUrl || undefined,
        });

        // If import mode and WSDL content provided, update draft spec content
        if (selectedMode === 'import' && importContent.trim()) {
          await contractStudioApi.updateContent(soapDraft.draftId, {
            specContent: importContent,
            format: 'xml',
            editedBy: currentActor,
          });
        }

        return { draftId: soapDraft.draftId };
      }

      // Event type uses dedicated createEventDraft to populate EventDraftMetadata
      if (isEventType) {
        const eventDraft = await contractStudioApi.createEventDraft({
          title,
          author: currentActor,
          asyncApiVersion,
          serviceId: linkedServiceId || undefined,
          description,
          defaultContentType,
        });

        // If import mode and AsyncAPI content provided, update draft spec content
        if (selectedMode === 'import' && importContent.trim()) {
          await contractStudioApi.updateContent(eventDraft.draftId, {
            specContent: importContent,
            format: 'json',
            editedBy: currentActor,
          });
        }

        return { draftId: eventDraft.draftId };
      }

      // Background Service type uses dedicated createBackgroundServiceDraft to populate BackgroundServiceDraftMetadata
      if (isBackgroundServiceType) {
        const bgDraft = await contractStudioApi.createBackgroundServiceDraft({
          title,
          author: currentActor,
          serviceName: bgServiceName || title,
          category: bgCategory,
          triggerType: bgTriggerType,
          serviceId: linkedServiceId || undefined,
          description,
          scheduleExpression: bgScheduleExpression || undefined,
        });

        return { draftId: bgDraft.draftId };
      }

      // Generic draft creation for other types
      const createdDraft = await contractStudioApi.createDraft({
        title,
        author: currentActor,
        contractType: selectedType as ContractType,
        protocol: selectedProtocol as ContractProtocol,
        serviceId: linkedServiceId || undefined,
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
  const availableServices = servicesQuery.data?.items ?? [];

  const canProceedToMode = !!selectedType;
  const canProceedToDetails = !!selectedMode;
  const canCreate = !!title && !!selectedProtocol && (() => {
    if (selectedMode === 'ai') return !!aiPrompt.trim();
    if (selectedMode === 'import') return !!importContent.trim();
    return true;
  })();

  return (
    <PageContainer className="max-w-4xl">
      {/* Header */}
      <div className="flex items-center gap-3 mb-8">
        <button
          onClick={() => step === 'type' ? navigate('/contracts') : setStep(step === 'details' ? 'mode' : 'type')}
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
        {(['type', 'mode', 'details'] as Step[]).map((s, i) => (
          <div key={s} className="flex items-center gap-2">
            <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-medium
              ${step === s ? 'bg-accent text-white' :
                i < ['type', 'mode', 'details'].indexOf(step) ? 'bg-emerald-900/40 text-emerald-300' :
                'bg-elevated text-muted'}`}
            >
              {i + 1}
            </div>
            <span className={`text-xs ${step === s ? 'text-heading font-medium' : 'text-muted'}`}>
              {t(`contracts.create.step${s.charAt(0).toUpperCase() + s.slice(1)}`, s)}
            </span>
            {i < 2 && <div className="w-8 h-px bg-edge" />}
          </div>
        ))}
      </div>

      {/* Step 1: Type selection */}
      {step === 'type' && (
        <div className="space-y-4">
          <h2 className="text-sm font-semibold text-heading mb-4">
            {t('contracts.create.selectType', 'What type of service?')}
          </h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {SERVICE_TYPES.map((st) => {
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

          <div className="flex justify-end mt-6">
            <button
              onClick={() => setStep('mode')}
              disabled={!canProceedToMode}
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

              {/* Linked Service */}
              <div>
                <label className="block text-xs font-medium text-heading mb-1">
                  {t('contracts.create.linkedService', 'Linked Service')}
                </label>
                <select
                  value={linkedServiceId}
                  onChange={(e) => setLinkedServiceId(e.target.value)}
                  className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body focus:outline-none focus:ring-1 focus:ring-accent"
                >
                  <option value="">{t('contracts.create.linkedServiceOptional', 'No linked service yet')}</option>
                  {availableServices.map((service: ServiceListItem) => (
                    <option key={service.serviceId} value={service.serviceId}>
                      {service.displayName} · {service.domain} · {service.teamName}
                    </option>
                  ))}
                </select>
                <p className="mt-1 text-[10px] text-muted">
                  {t('contracts.create.linkedServiceHint', 'Linking a real catalog service enables publish and workspace enrichment without artificial fallback.')}
                </p>
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

              {/* Import content (when mode is 'import') */}
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

              {/* AI prompt (when mode is 'ai') */}
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
                      placeholder="http://example.com/service"
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
                        placeholder="http://example.com/service"
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
                        <option value="Job">Job</option>
                        <option value="Worker">Worker</option>
                        <option value="Scheduler">Scheduler</option>
                        <option value="Processor">Processor</option>
                        <option value="Exporter">Exporter</option>
                        <option value="Notifier">Notifier</option>
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
                        <option value="OnDemand">On Demand</option>
                        <option value="Cron">Cron</option>
                        <option value="Interval">Interval</option>
                        <option value="EventTriggered">Event Triggered</option>
                        <option value="Continuous">Continuous</option>
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
            <div className="text-xs text-red-400 bg-red-900/10 border border-red-700/20 rounded-md px-3 py-2">
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
