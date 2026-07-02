import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import {
  Fingerprint, LayoutGrid, Users, Link as LinkIcon, ClipboardCheck,
} from 'lucide-react';
import { WizardOverlay } from './WizardOverlay';
import { ServiceTypeIconPicker } from './ServiceTypeIconPicker';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Select } from '../../../components/Select';
import { serviceCatalogApi } from '../api';

const labelClass = 'block text-sm font-medium text-body mb-1';

interface ServiceRegistrationOverlayProps {
  onClose: () => void;
  onSuccess: (serviceId: string) => void;
}

interface ServiceFormData {
  name: string;
  domain: string;
  subDomain: string;
  capability: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  dataClassification: string;
  infrastructureProvider: string;
  team: string;
  technicalOwner: string;
  businessOwner: string;
  productOwner: string;
  contactChannel: string;
  description: string;
  documentationUrl: string;
  repositoryUrl: string;
}

const INITIAL_FORM: ServiceFormData = {
  name: '', domain: '', subDomain: '', capability: '',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  dataClassification: '', infrastructureProvider: '',
  team: '', technicalOwner: '', businessOwner: '', productOwner: '', contactChannel: '',
  description: '', documentationUrl: '', repositoryUrl: '',
};

const STEPS = [
  { id: 'identity',       labelKey: 'catalog.registration.step.identity',       icon: Fingerprint },
  { id: 'classification', labelKey: 'catalog.registration.step.classification', icon: LayoutGrid },
  { id: 'ownership',      labelKey: 'catalog.registration.step.ownership',      icon: Users },
  { id: 'references',     labelKey: 'catalog.registration.step.references',     icon: LinkIcon },
  { id: 'review',         labelKey: 'catalog.registration.step.review',         icon: ClipboardCheck },
];

/** Overlay de 5 passos para registrar um novo serviço no catálogo. */
export function ServiceRegistrationOverlay({ onClose, onSuccess }: ServiceRegistrationOverlayProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<ServiceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Partial<Record<keyof ServiceFormData, string>>>({});

  const set = (key: keyof ServiceFormData, value: string) =>
    setForm((f) => ({ ...f, [key]: value }));
  const clearError = (key: keyof ServiceFormData) =>
    setErrors((e) => { const n = { ...e }; delete n[key]; return n; });

  const mutation = useMutation({
    mutationFn: () => serviceCatalogApi.registerService({
      name: form.name, domain: form.domain, team: form.team,
      description: form.description || undefined,
      serviceType: form.serviceType || undefined,
      criticality: form.criticality || undefined,
      exposureType: form.exposureType || undefined,
      technicalOwner: form.technicalOwner || undefined,
      businessOwner: form.businessOwner || undefined,
      documentationUrl: form.documentationUrl || undefined,
      repositoryUrl: form.repositoryUrl || undefined,
    }),
    onSuccess: (data) => {
      onSuccess(data.id);
    },
    onError: () => {
      toast.error(t('common.errorSaving'));
    },
  });

  function validate(): boolean {
    const errs: typeof errors = {};
    if (step === 1) {
      if (!form.name.trim()) errs.name = t('serviceCatalog.name') + ' ' + t('common.isRequired', { defaultValue: 'is required' });
      if (!form.domain.trim()) errs.domain = t('serviceCatalog.domain', { defaultValue: 'Domain' }) + ' ' + t('common.isRequired', { defaultValue: 'is required' });
    }
    if (step === 3) {
      if (!form.team.trim()) errs.team = t('serviceCatalog.team') + ' ' + t('common.isRequired', { defaultValue: 'is required' });
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }

  function handleNext() {
    if (!validate()) return;
    setStep((s) => s + 1);
  }

  function handleBack() {
    setStep((s) => Math.max(1, s - 1));
    setErrors({});
  }

  function renderStep() {
    switch (step) {
      case 1:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.name')} <span className="text-danger">*</span></label>
              <TextField
                type="text"
                value={form.name}
                onChange={(e) => { set('name', e.target.value); clearError('name'); }}
                placeholder="e.g., payment-service"
              />
              {errors.name && <p className="mt-1 text-xs text-danger">{errors.name}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.domain', { defaultValue: 'Domain' })} <span className="text-danger">*</span></label>
              <TextField
                type="text"
                value={form.domain}
                onChange={(e) => { set('domain', e.target.value); clearError('domain'); }}
                placeholder="e.g., payments, identity, orders"
              />
              {errors.domain && <p className="mt-1 text-xs text-danger">{errors.domain}</p>}
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.subDomain', { defaultValue: 'Sub-Domain' })}</label>
              <TextField type="text" value={form.subDomain}
                onChange={(e) => set('subDomain', e.target.value)} placeholder="e.g., billing" />
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.capability', { defaultValue: 'Capability' })}</label>
              <TextField type="text" value={form.capability}
                onChange={(e) => set('capability', e.target.value)} placeholder="e.g., payment-processing" />
            </div>
          </div>
        );

      case 2:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.serviceType', { defaultValue: 'Service Type' })}</label>
              <ServiceTypeIconPicker value={form.serviceType} onChange={(v) => set('serviceType', v)} mode="service" />
            </div>
            <div className="grid grid-cols-2 gap-4 mt-4">
              <div>
                <label className={labelClass}>{t('serviceCatalog.criticality', { defaultValue: 'Criticality' })}</label>
                <Select
                  value={form.criticality}
                  onChange={(e) => set('criticality', e.target.value)}
                  options={[
                    { value: 'Critical', label: t('catalog.badges.criticality.Critical') },
                    { value: 'High', label: t('catalog.badges.criticality.High') },
                    { value: 'Medium', label: t('catalog.badges.criticality.Medium') },
                    { value: 'Low', label: t('catalog.badges.criticality.Low') },
                  ]}
                />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.exposureType', { defaultValue: 'Exposure' })}</label>
                <Select
                  value={form.exposureType}
                  onChange={(e) => set('exposureType', e.target.value)}
                  options={[
                    { value: 'Internal', label: t('catalog.badges.exposure.Internal') },
                    { value: 'External', label: t('catalog.badges.exposure.External') },
                    { value: 'Partner', label: t('catalog.badges.exposure.Partner') },
                  ]}
                />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.dataClassification', { defaultValue: 'Data Classification' })}</label>
                <TextField type="text" value={form.dataClassification}
                  onChange={(e) => set('dataClassification', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.infrastructureProvider', { defaultValue: 'Infrastructure' })}</label>
                <TextField type="text" value={form.infrastructureProvider}
                  onChange={(e) => set('infrastructureProvider', e.target.value)} />
              </div>
            </div>
          </div>
        );

      case 3:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.team')} <span className="text-danger">*</span></label>
              <TextField
                type="text"
                value={form.team}
                onChange={(e) => { set('team', e.target.value); clearError('team'); }}
                placeholder="e.g., platform-team"
              />
              {errors.team && <p className="mt-1 text-xs text-danger">{errors.team}</p>}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className={labelClass}>{t('serviceCatalog.technicalOwner', { defaultValue: 'Technical Owner' })}</label>
                <TextField type="text" value={form.technicalOwner}
                  onChange={(e) => set('technicalOwner', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.businessOwner', { defaultValue: 'Business Owner' })}</label>
                <TextField type="text" value={form.businessOwner}
                  onChange={(e) => set('businessOwner', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.productOwner', { defaultValue: 'Product Owner' })}</label>
                <TextField type="text" value={form.productOwner}
                  onChange={(e) => set('productOwner', e.target.value)} />
              </div>
              <div>
                <label className={labelClass}>{t('serviceCatalog.contactChannel', { defaultValue: 'Contact Channel' })}</label>
                <TextField type="text" value={form.contactChannel}
                  onChange={(e) => set('contactChannel', e.target.value)} placeholder="#slack-channel" />
              </div>
            </div>
          </div>
        );

      case 4:
        return (
          <div className="space-y-4">
            <div>
              <label className={labelClass}>{t('serviceCatalog.description', { defaultValue: 'Description' })}</label>
              <TextArea
                textareaClassName="resize-none"
                rows={3}
                value={form.description}
                onChange={(e) => set('description', e.target.value)}
              />
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.documentationUrl', { defaultValue: 'Documentation URL' })}</label>
              <TextField type="url" value={form.documentationUrl}
                onChange={(e) => set('documentationUrl', e.target.value)} placeholder="https://docs.example.com" />
            </div>
            <div>
              <label className={labelClass}>{t('serviceCatalog.repositoryUrl', { defaultValue: 'Repository URL' })}</label>
              <TextField type="url" value={form.repositoryUrl}
                onChange={(e) => set('repositoryUrl', e.target.value)} placeholder="https://github.com/org/repo" />
            </div>
          </div>
        );

      case 5:
        return (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-heading mb-3">{t('catalog.registration.step.review')}</h3>
            {([
              ['serviceCatalog.name', form.name],
              ['serviceCatalog.domain', form.domain],
              ['serviceCatalog.team', form.team],
              ['serviceCatalog.serviceType', form.serviceType],
              ['serviceCatalog.criticality', form.criticality],
              ['serviceCatalog.exposureType', form.exposureType],
              ['serviceCatalog.technicalOwner', form.technicalOwner],
            ] as [string, string][]).map(([key, value]) =>
              value ? (
                <div key={key} className="flex justify-between text-sm">
                  <span className="text-muted">{t(key, { defaultValue: key })}</span>
                  <span className="font-medium text-heading">{value}</span>
                </div>
              ) : null
            )}
          </div>
        );

      default:
        return null;
    }
  }

  return (
    <WizardOverlay
      title={t('catalog.registration.title')}
      headerIcon={<Fingerprint size={20} />}
      steps={STEPS}
      currentStep={step}
      onClose={onClose}
      onBack={handleBack}
      onNext={handleNext}
      onSubmit={() => mutation.mutate()}
      isSubmitting={mutation.isPending}
      isLastStep={step === STEPS.length}
    >
      {renderStep()}
    </WizardOverlay>
  );
}
