import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plug, Settings2, ClipboardCheck } from 'lucide-react';
import { WizardOverlay } from './WizardOverlay';
import { ServiceTypeIconPicker } from './ServiceTypeIconPicker';
import { serviceCatalogApi } from '../api';
import type { InterfaceType } from '../../../types';

const inputClass =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const selectClass = inputClass;
const labelClass = 'block text-sm font-medium text-body mb-1';

interface ServiceInterfaceOverlayProps {
  serviceId: string;
  serviceName: string;
  onClose: () => void;
  onSuccess: () => void;
}

interface InterfaceFormData {
  name: string;
  interfaceType: string;
  exposureScope: string;
  basePath: string;
  topicName: string;
  wsdlNamespace: string;
  grpcServiceName: string;
  scheduleCron: string;
  documentationUrl: string;
  requiresContract: boolean;
}

const INITIAL_FORM: InterfaceFormData = {
  name: '', interfaceType: 'RestApi', exposureScope: 'Internal',
  basePath: '', topicName: '', wsdlNamespace: '',
  grpcServiceName: '', scheduleCron: '', documentationUrl: '', requiresContract: false,
};

const STEPS = [
  { id: 'type',    labelKey: 'catalog.interface.step.type',    icon: Plug },
  { id: 'details', labelKey: 'catalog.interface.step.details', icon: Settings2 },
  { id: 'review',  labelKey: 'catalog.interface.step.review',  icon: ClipboardCheck },
];

const TYPES_WITH_BASE_PATH: string[] = ['RestApi', 'SoapService', 'ZosConnectApi', 'GraphqlApi'];
const TYPES_WITH_WSDL:      string[] = ['SoapService'];
const TYPES_WITH_TOPIC:     string[] = ['KafkaProducer', 'KafkaConsumer', 'MqQueue'];
const TYPES_WITH_GRPC:      string[] = ['GrpcService'];
const TYPES_WITH_CRON:      string[] = ['BackgroundWorker', 'ScheduledJob'];

export function ServiceInterfaceOverlay({ serviceId, serviceName, onClose, onSuccess }: ServiceInterfaceOverlayProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<InterfaceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Partial<Record<keyof InterfaceFormData, string>>>({});

  const set = (key: keyof InterfaceFormData, value: string | boolean) =>
    setForm((f) => ({ ...f, [key]: value }));

  const clearError = (key: keyof InterfaceFormData) =>
    setErrors((e) => { const n = { ...e }; delete n[key]; return n; });

  const mutation = useMutation({
    mutationFn: () => serviceCatalogApi.createServiceInterface({
      serviceAssetId: serviceId,
      name: form.name,
      interfaceType: form.interfaceType as InterfaceType,
      exposureScope: form.exposureScope || undefined,
      basePath: form.basePath || undefined,
      topicName: form.topicName || undefined,
      wsdlNamespace: form.wsdlNamespace || undefined,
      grpcServiceName: form.grpcServiceName || undefined,
      scheduleCron: form.scheduleCron || undefined,
      documentationUrl: form.documentationUrl || undefined,
      requiresContract: form.requiresContract,
    }),
    onSuccess: () => onSuccess(),
    onError: () => toast.error(t('common.errorSaving')),
  });

  function validate(): boolean {
    const errs: typeof errors = {};
    if (step === 1 && !form.name.trim()) {
      errs.name = t('serviceCatalog.name') + ' ' + t('common.isRequired', { defaultValue: 'is required' });
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  function handleNext() {
    if (!validate()) return;
    setStep((s) => s + 1);
  }

  function renderStep() {
    switch (step) {
      case 1:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('catalog.interface.step.type')}</label>
              <ServiceTypeIconPicker value={form.interfaceType} onChange={(v) => set('interfaceType', v)} mode="interface" />
            </div>
            <div className="mt-4">
              <label className={labelClass}>{t('serviceCatalog.name')} <span className="text-danger">*</span></label>
              <input
                type="text"
                className={inputClass}
                value={form.name}
                onChange={(e) => { set('name', e.target.value); clearError('name'); }}
                placeholder="Interface name, e.g., POST /payments"
              />
              {errors.name && <p className="mt-1 text-xs text-danger">{errors.name}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('catalog.interface.exposureScope', { defaultValue: 'Exposure Scope' })}</label>
              <select className={selectClass} value={form.exposureScope} onChange={(e) => set('exposureScope', e.target.value)}>
                <option value="Internal">{t('catalog.badges.exposure.Internal')}</option>
                <option value="External">{t('catalog.badges.exposure.External')}</option>
                <option value="Partner">{t('catalog.badges.exposure.Partner')}</option>
              </select>
            </div>
          </div>
        );

      case 2: {
        const ifType = form.interfaceType;
        return (
          <div className="space-y-4">
            {TYPES_WITH_BASE_PATH.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.basePath', { defaultValue: 'Base Path' })}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.basePath}
                  onChange={(e) => set('basePath', e.target.value)} placeholder="/api/v1" />
              </div>
            )}
            {TYPES_WITH_WSDL.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.wsdlNamespace', { defaultValue: 'WSDL Namespace' })}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.wsdlNamespace}
                  onChange={(e) => set('wsdlNamespace', e.target.value)} placeholder="http://example.com/service" />
              </div>
            )}
            {TYPES_WITH_TOPIC.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.topicName', { defaultValue: 'Topic Name' })}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.topicName}
                  onChange={(e) => set('topicName', e.target.value)} placeholder="topic.name.v1" />
              </div>
            )}
            {TYPES_WITH_GRPC.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.grpcServiceName', { defaultValue: 'gRPC Service Name' })}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.grpcServiceName}
                  onChange={(e) => set('grpcServiceName', e.target.value)} placeholder="PaymentService" />
              </div>
            )}
            {TYPES_WITH_CRON.includes(ifType) && (
              <div>
                <label className={labelClass}>{t('catalog.interface.scheduleCron', { defaultValue: 'Schedule (cron)' })}</label>
                <input type="text" className={`${inputClass} font-mono`} value={form.scheduleCron}
                  onChange={(e) => set('scheduleCron', e.target.value)} placeholder="0 */6 * * *" />
              </div>
            )}
            <div>
              <label className={labelClass}>{t('serviceCatalog.documentationUrl', { defaultValue: 'Documentation URL' })}</label>
              <input type="url" className={inputClass} value={form.documentationUrl}
                onChange={(e) => set('documentationUrl', e.target.value)} />
            </div>
            <div className="flex items-center gap-2">
              <input type="checkbox" id="requiresContract" checked={form.requiresContract}
                onChange={(e) => set('requiresContract', e.target.checked)}
                className="rounded border-edge text-accent" />
              <label htmlFor="requiresContract" className="text-sm text-body">
                {t('catalog.interface.requiresContract', { defaultValue: 'Requires contract' })}
              </label>
            </div>
          </div>
        );
      }

      case 3:
        return (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-heading mb-3">{t('catalog.interface.step.review')}</h3>
            {([
              [t('serviceCatalog.name'), form.name],
              [t('catalog.interface.step.type'), form.interfaceType],
              [t('catalog.interface.exposureScope', { defaultValue: 'Exposure Scope' }), form.exposureScope],
              ...(form.basePath ? [[t('catalog.interface.basePath', { defaultValue: 'Base Path' }), form.basePath]] : []),
              ...(form.topicName ? [[t('catalog.interface.topicName', { defaultValue: 'Topic' }), form.topicName]] : []),
              ...(form.grpcServiceName ? [[t('catalog.interface.grpcServiceName', { defaultValue: 'gRPC' }), form.grpcServiceName]] : []),
              ...(form.wsdlNamespace ? [[t('catalog.interface.wsdlNamespace', { defaultValue: 'WSDL Namespace' }), form.wsdlNamespace]] : []),
              ...(form.scheduleCron ? [[t('catalog.interface.scheduleCron', { defaultValue: 'Schedule (cron)' }), form.scheduleCron]] : []),
            ] as [string, string][]).map(([label, value]) => (
              <div key={label} className="flex justify-between text-sm">
                <span className="text-muted">{label}</span>
                <span className="font-medium text-heading font-mono">{value}</span>
              </div>
            ))}
            {form.requiresContract && (
              <div className="flex justify-between text-sm">
                <span className="text-muted">{t('catalog.interface.requiresContract', { defaultValue: 'Requires contract' })}</span>
                <span className="font-medium text-success">✓</span>
              </div>
            )}
            <div className="text-xs text-muted mt-2">
              {t('catalog.interface.service', { defaultValue: 'Service' })}: <span className="text-heading">{serviceName}</span>
            </div>
          </div>
        );

      default:
        return null;
    }
  }

  return (
    <WizardOverlay
      title={t('catalog.interface.title')}
      headerIcon={<Plug size={20} />}
      steps={STEPS}
      currentStep={step}
      onClose={onClose}
      onBack={() => { setStep((s) => Math.max(1, s - 1)); setErrors({}); }}
      onNext={handleNext}
      onSubmit={() => mutation.mutate()}
      isSubmitting={mutation.isPending}
      isLastStep={step === STEPS.length}
    >
      {renderStep()}
    </WizardOverlay>
  );
}
