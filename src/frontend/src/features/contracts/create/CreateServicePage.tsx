import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  Globe,
  Server,
  Zap,
  Cog,
  Database,
  Columns,
  Upload,
  LayoutTemplate,
  Copy,
  Code,
  ArrowLeft,
  ArrowRight,
  Sparkles,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { SERVICE_TYPES, PROTOCOL_BY_TYPE, type ServiceTypeValue } from '../shared/constants';
import { contractStudioApi } from '../api/contractStudio';
import type { ContractType, ContractProtocol } from '../types';

const TYPE_ICONS: Record<string, React.ComponentType<{ size?: number; className?: string }>> = {
  RestApi: Globe,
  Soap: Server,
  Event: Zap,
  BackgroundService: Cog,
  SharedSchema: Database,
};

type CreationMode = 'visual' | 'import' | 'template' | 'clone' | 'source' | 'ai';

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
  { id: 'template', labelKey: 'contracts.create.modeTemplate', descriptionKey: 'contracts.create.modeTemplateDesc', Icon: LayoutTemplate },
  { id: 'clone', labelKey: 'contracts.create.modeClone', descriptionKey: 'contracts.create.modeCloneDesc', Icon: Copy },
  { id: 'source', labelKey: 'contracts.create.modeSource', descriptionKey: 'contracts.create.modeSourceDesc', Icon: Code },
];

type Step = 'type' | 'mode' | 'details';

/**
 * Página de criação de novo serviço/contrato.
 * Fluxo em 3 passos: escolha de tipo → modo de criação → detalhes.
 */
export function CreateServicePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [step, setStep] = useState<Step>('type');
  const [selectedType, setSelectedType] = useState<ServiceTypeValue | null>(null);
  const [selectedMode, setSelectedMode] = useState<CreationMode | null>(null);
  const [selectedProtocol, setSelectedProtocol] = useState<ContractProtocol | ''>('');

  // Form fields for details step
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [aiPrompt, setAiPrompt] = useState('');
  const [importContent, setImportContent] = useState('');

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!selectedType || !selectedProtocol) throw new Error('Missing required fields');

      if (selectedMode === 'ai') {
        return contractStudioApi.generateFromAi({
          title,
          author: 'current-user',
          contractType: selectedType as ContractType,
          protocol: selectedProtocol as ContractProtocol,
          prompt: aiPrompt,
        });
      }

      return contractStudioApi.createDraft({
        title,
        author: 'current-user',
        contractType: selectedType as ContractType,
        protocol: selectedProtocol as ContractProtocol,
        description,
      });
    },
    onSuccess: (data) => {
      navigate(`/contracts/studio/${data.draftId}`);
    },
  });

  const protocols = selectedType ? PROTOCOL_BY_TYPE[selectedType] : [];

  const canProceedToMode = !!selectedType;
  const canProceedToDetails = !!selectedMode;
  const canCreate = !!title && !!selectedProtocol && (selectedMode !== 'ai' || !!aiPrompt);

  return (
    <div className="max-w-4xl mx-auto py-8 px-6">
      {/* Header */}
      <div className="flex items-center gap-3 mb-8">
        <button
          onClick={() => step === 'type' ? navigate('/contracts') : setStep(step === 'details' ? 'mode' : 'type')}
          className="text-muted hover:text-heading transition-colors"
        >
          <ArrowLeft size={18} />
        </button>
        <div>
          <h1 className="text-lg font-semibold text-heading">
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
                    if (protos.length === 1) setSelectedProtocol(protos[0]);
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
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
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

              {protocols.length === 1 && (
                <div className="text-xs text-muted">
                  {t('contracts.protocol', 'Protocol')}: <span className="text-heading font-medium">{t(`contracts.protocols.${protocols[0]}`, protocols[0])}</span>
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
                    rows={4}
                    placeholder={t('contracts.create.aiPromptPlaceholder', 'Describe the API you want to generate. Include endpoints, operations, data models...')}
                    className="w-full text-sm bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                  />
                </div>
              )}

              {/* Import content (when mode is 'import') */}
              {selectedMode === 'import' && (
                <div>
                  <label className="block text-xs font-medium text-heading mb-1">
                    {t('contracts.create.importContent', 'Specification Content')}
                  </label>
                  <textarea
                    value={importContent}
                    onChange={(e) => setImportContent(e.target.value)}
                    rows={8}
                    placeholder={t('contracts.specContentPlaceholder', 'Paste your specification here (JSON/YAML/XML)...')}
                    className="w-full text-xs font-mono bg-elevated border border-edge rounded-md px-3 py-2 text-body placeholder:text-muted/40 focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                  />
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
                ? t('common.loading', 'Creating...')
                : t('common.create', 'Create')}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
