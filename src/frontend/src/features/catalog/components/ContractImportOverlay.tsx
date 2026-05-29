import { useState, useCallback } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Layers, FileCode, Tag } from 'lucide-react';
import { WizardOverlay } from './WizardOverlay';
import { contractsApi } from '../../contracts/api/contracts';
import type { ContractProtocol } from '../../../types';

const inputClass =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const labelClass = 'block text-sm font-medium text-body mb-1';

interface ContractImportOverlayProps {
  preselectedApiAssetId?: string;
  preselectedApiAssetName?: string;
  onClose: () => void;
  onSuccess: () => void;
}

const STEPS = [
  { id: 'interface', labelKey: 'catalog.contract.step.interface', icon: Layers },
  { id: 'spec',      labelKey: 'catalog.contract.step.spec',      icon: FileCode },
  { id: 'version',   labelKey: 'catalog.contract.step.version',   icon: Tag },
];

type SpecTab = 'upload' | 'url' | 'editor';

/**
 * Detecta o protocolo de uma spec a partir dos primeiros 2KB do conteúdo.
 */
function detectProtocol(content: string): ContractProtocol | null {
  const sample = content.slice(0, 2048);
  if (/openapi[:\s"']/i.test(sample)) return 'OpenApi';
  if (/asyncapi[:\s"']/i.test(sample)) return 'AsyncApi';
  if (/syntax\s*=\s*["']proto/i.test(sample)) return 'Protobuf';
  if (/<definitions|<wsdl:/i.test(sample)) return 'Wsdl';
  if (/"swagger"\s*:/i.test(sample)) return 'Swagger';
  if (/type\s+Query\s*\{|schema\s*\{/i.test(sample)) return 'GraphQl';
  return null;
}

/** Overlay de 3 passos para importar uma versão de contrato. */
export function ContractImportOverlay({
  preselectedApiAssetId,
  preselectedApiAssetName,
  onClose,
  onSuccess,
}: ContractImportOverlayProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [apiAssetId, setApiAssetId] = useState(preselectedApiAssetId ?? '');
  const [specContent, setSpecContent] = useState('');
  const [specTab, setSpecTab] = useState<SpecTab>('upload');
  const [specUrl, setSpecUrl] = useState('');
  const [detectedProtocol, setDetectedProtocol] = useState<ContractProtocol | null>(null);
  const [version, setVersion] = useState('');
  const [protocol, setProtocol] = useState<ContractProtocol>('OpenApi');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mutation = useMutation({
    mutationFn: () =>
      contractsApi.importContract({
        apiAssetId,
        content: specTab === 'url' ? specUrl : specContent,
        version,
        protocol: detectedProtocol ?? protocol,
      }),
    onSuccess: () => onSuccess(),
    onError: () => toast.error(t('common.errorSaving')),
  });

  const handleContentChange = useCallback((content: string) => {
    setSpecContent(content);
    const detected = detectProtocol(content);
    setDetectedProtocol(detected);
    if (detected) setProtocol(detected);
  }, []);

  function validate(): boolean {
    const errs: Record<string, string> = {};
    if (step === 1 && !apiAssetId.trim()) {
      errs.apiAssetId =
        t('contracts.apiAssetId', 'API Asset ID') +
        ' ' +
        t('common.isRequired', 'is required');
    }
    if (step === 2) {
      const hasContent = specTab === 'url' ? specUrl.trim() !== '' : specContent.trim() !== '';
      if (!hasContent) {
        errs.spec =
          t('catalog.contract.step.spec', 'Spec') +
          ' ' +
          t('common.isRequired', 'is required');
      }
    }
    if (step === 3 && !version.trim()) {
      errs.version =
        t('contracts.version', 'Version') +
        ' ' +
        t('common.isRequired', 'is required');
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  function handleNext() {
    if (!validate()) return;
    setStep((s) => s + 1);
  }

  async function handleFileDrop(file: File) {
    const text = await file.text();
    handleContentChange(text);
  }

  function renderStep() {
    switch (step) {
      case 1:
        if (preselectedApiAssetId) {
          return (
            <div className="space-y-3">
              <p className="text-sm text-muted">
                {t('contracts.apiAssetId', 'API Asset ID')}
              </p>
              <div className="px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading font-mono">
                {preselectedApiAssetName ?? preselectedApiAssetId}
              </div>
              <p className="text-xs text-muted font-mono">{preselectedApiAssetId}</p>
            </div>
          );
        }
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>
                {t('contracts.apiAssetId', 'API Asset ID')}{' '}
                <span className="text-danger">*</span>
              </label>
              <input
                type="text"
                className={`${inputClass} font-mono`}
                value={apiAssetId}
                onChange={(e) => {
                  setApiAssetId(e.target.value);
                  setErrors({});
                }}
                placeholder="uuid — Asset ID da interface"
              />
              {errors.apiAssetId && (
                <p className="mt-1 text-xs text-danger">{errors.apiAssetId}</p>
              )}
            </div>
          </div>
        );

      case 2:
        return (
          <div className="space-y-4">
            {/* Protocol badge */}
            {detectedProtocol ? (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-success/15 text-success border border-success/30">
                {t('catalog.contract.protocolDetected', { protocol: detectedProtocol, defaultValue: '{{protocol}} detected' })}
              </span>
            ) : specContent ? (
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-warning/15 text-warning border border-warning/30">
                {t('catalog.contract.protocolUnknown', {
                  defaultValue: 'Select manually',
                })}
              </span>
            ) : null}

            {/* Tabs */}
            <div className="flex gap-0.5 bg-canvas rounded-md p-0.5 w-fit border border-edge">
              {(['upload', 'url', 'editor'] as SpecTab[]).map((tab) => (
                <button
                  key={tab}
                  type="button"
                  onClick={() => setSpecTab(tab)}
                  className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                    specTab === tab
                      ? 'bg-accent/20 text-accent'
                      : 'text-muted hover:text-heading'
                  }`}
                >
                  {t(`catalog.contract.tab.${tab}`)}
                </button>
              ))}
            </div>

            {specTab === 'upload' && (
              <div
                onDragOver={(e) => e.preventDefault()}
                onDrop={(e) => {
                  e.preventDefault();
                  const file = e.dataTransfer.files[0];
                  if (file) void handleFileDrop(file);
                }}
                className="border-2 border-dashed border-accent/30 rounded-lg p-8 text-center bg-accent/5 hover:bg-accent/10 transition-colors cursor-pointer"
                onClick={() => {
                  const input = document.createElement('input');
                  input.type = 'file';
                  input.accept = '.yaml,.yml,.json,.xml,.proto,.wsdl';
                  input.onchange = (e) => {
                    const file = (e.target as HTMLInputElement).files?.[0];
                    if (file) void handleFileDrop(file);
                  };
                  input.click();
                }}
              >
                <p className="text-sm text-muted">
                  {t('catalog.contract.dropZone.hint', {
                    defaultValue: 'Drop openapi.yaml or click to browse',
                  })}
                </p>
                <p className="text-xs text-muted/60 mt-1">
                  {t('catalog.contract.dropZone.formats', {
                    defaultValue: 'YAML · JSON · WSDL · Proto',
                  })}
                </p>
                {specContent && (
                  <p className="text-xs text-success mt-2">
                    ✓ {specContent.length}{' '}
                    {t('common.characters', { defaultValue: 'chars' })}
                  </p>
                )}
              </div>
            )}

            {specTab === 'url' && (
              <div>
                <label className={labelClass}>
                  {t('contracts.specUrl', { defaultValue: 'Specification URL' })}
                </label>
                <input
                  type="url"
                  className={inputClass}
                  value={specUrl}
                  onChange={(e) => setSpecUrl(e.target.value)}
                  placeholder="https://api.example.com/openapi.yaml"
                />
              </div>
            )}

            {specTab === 'editor' && (
              <textarea
                className={`${inputClass} font-mono resize-none`}
                rows={12}
                onChange={(e) => handleContentChange(e.target.value)}
                placeholder={'openapi: "3.0.0"\ninfo:\n  title: My API\n  version: 1.0.0'}
              />
            )}

            {errors.spec && (
              <p className="mt-1 text-xs text-danger">{errors.spec}</p>
            )}
          </div>
        );

      case 3:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>
                {t('contracts.version', { defaultValue: 'Version' })}{' '}
                <span className="text-danger">*</span>
              </label>
              <input
                type="text"
                className={`${inputClass} font-mono`}
                value={version}
                onChange={(e) => {
                  setVersion(e.target.value);
                  setErrors({});
                }}
                placeholder="1.0.0"
              />
              {errors.version && (
                <p className="mt-1 text-xs text-danger">{errors.version}</p>
              )}
            </div>
            <div>
              <label className={labelClass}>
                {t('contracts.protocol', { defaultValue: 'Protocol' })}
              </label>
              <select
                className={inputClass}
                value={detectedProtocol ?? protocol}
                onChange={(e) => {
                  setProtocol(e.target.value as ContractProtocol);
                  setDetectedProtocol(null);
                }}
              >
                {(['OpenApi', 'Swagger', 'AsyncApi', 'Wsdl', 'Protobuf', 'GraphQl', 'WorkerService'] as ContractProtocol[]).map(
                  (p) => (
                    <option key={p} value={p}>
                      {t(`contracts.protocols.${p}`, { defaultValue: p })}
                    </option>
                  )
                )}
              </select>
            </div>
          </div>
        );

      default:
        return null;
    }
  }

  return (
    <WizardOverlay
      title={t('catalog.contract.title')}
      headerIcon={<FileCode size={20} />}
      steps={STEPS}
      currentStep={step}
      onClose={onClose}
      onBack={() => {
        setStep((s) => Math.max(1, s - 1));
        setErrors({});
      }}
      onNext={handleNext}
      onSubmit={() => mutation.mutate()}
      isSubmitting={mutation.isPending}
      isLastStep={step === STEPS.length}
    >
      {renderStep()}
    </WizardOverlay>
  );
}
