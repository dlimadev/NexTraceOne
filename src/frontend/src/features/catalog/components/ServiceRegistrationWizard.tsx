/**
 * Wizard multi-etapas para registo de serviço no catálogo.
 *
 * Step 1: Identidade (name, domain)
 * Step 2: Classificação (serviceType, criticality, exposureType) + campos condicionais
 * Step 3: Ownership (team, technicalOwner, businessOwner)
 * Step 4: Referências (description, documentationUrl, repositoryUrl)
 * Step 5: Confirmação (resumo)
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Check, X } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';

export interface ServiceFormData {
  name: string;
  domain: string;
  team: string;
  description: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  technicalOwner: string;
  businessOwner: string;
  documentationUrl: string;
  repositoryUrl: string;
}

interface ServiceRegistrationWizardProps {
  onSubmit: (data: ServiceFormData) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
}

const INITIAL_FORM: ServiceFormData = {
  name: '', domain: '', team: '', description: '',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  technicalOwner: '', businessOwner: '', documentationUrl: '', repositoryUrl: '',
};

const TOTAL_STEPS = 5;

const inputClass = 'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';
const selectClass = inputClass;
const monoClass = `${inputClass} font-mono`;

export function ServiceRegistrationWizard({ onSubmit, onCancel, isSubmitting }: ServiceRegistrationWizardProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<ServiceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const set = useCallback((field: keyof ServiceFormData, value: string) => {
    setForm((f) => ({ ...f, [field]: value }));
    setErrors((e) => ({ ...e, [field]: '' }));
  }, []);

  // ── Validation per step ─────────────────────────────────────────
  const validateStep = useCallback((s: number): boolean => {
    const errs: Record<string, string> = {};
    if (s === 1) {
      if (!form.name.trim()) errs.name = t('serviceCatalog.wizard.nameRequired', 'Name is required');
      if (!form.domain.trim()) errs.domain = t('serviceCatalog.wizard.domainRequired', 'Domain is required');
    }
    if (s === 3) {
      if (!form.team.trim()) errs.team = t('serviceCatalog.wizard.teamRequired', 'Team is required');
    }
    setErrors(errs);
    return Object.keys(errs).length === 0;
  }, [form, t]);

  const next = useCallback(() => {
    if (validateStep(step)) setStep((s) => Math.min(s + 1, TOTAL_STEPS));
  }, [step, validateStep]);

  const back = useCallback(() => setStep((s) => Math.max(s - 1, 1)), []);

  const handleSubmit = useCallback(() => {
    onSubmit(form);
  }, [form, onSubmit]);

  // ── Step labels ─────────────────────────────────────────────────
  const stepLabels = [
    t('serviceCatalog.wizard.step1', 'Identity'),
    t('serviceCatalog.wizard.step2', 'Classification'),
    t('serviceCatalog.wizard.step3', 'Ownership'),
    t('serviceCatalog.wizard.step4', 'References'),
    t('serviceCatalog.wizard.step5', 'Confirmation'),
  ];

  return (
    <Card className="mb-6">
      <CardHeader>
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-heading">{t('serviceCatalog.registerServiceTitle')}</h2>
          <button type="button" onClick={onCancel} className="text-muted hover:text-heading transition-colors">
            <X size={18} />
          </button>
        </div>
      </CardHeader>
      <CardBody>
        {/* ── Stepper ── */}
        <div className="flex items-center gap-1 mb-6">
          {stepLabels.map((label, i) => {
            const stepNum = i + 1;
            const isActive = stepNum === step;
            const isDone = stepNum < step;
            return (
              <div key={stepNum} className="flex items-center gap-1 flex-1">
                <div className={`flex items-center justify-center w-7 h-7 rounded-full text-xs font-bold shrink-0
                  ${isDone ? 'bg-mint text-white' : isActive ? 'bg-accent text-white' : 'bg-surface border border-edge text-muted'}`}>
                  {isDone ? <Check size={14} /> : stepNum}
                </div>
                <span className={`text-xs truncate ${isActive ? 'text-heading font-medium' : 'text-muted'}`}>{label}</span>
                {i < stepLabels.length - 1 && <div className={`h-px flex-1 ${isDone ? 'bg-mint' : 'bg-edge'}`} />}
              </div>
            );
          })}
        </div>

        {/* ── Step 1: Identity ── */}
        {step === 1 && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.name')} <span className="text-danger">*</span></label>
                <input type="text" value={form.name} onChange={(e) => set('name', e.target.value)}
                  placeholder={t('serviceCatalog.namePlaceholder', 'e.g., payment-service')} className={monoClass} />
                {errors.name && <p className="text-xs text-danger mt-1">{errors.name}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.domain', 'Domain')} <span className="text-danger">*</span></label>
                <input type="text" value={form.domain} onChange={(e) => set('domain', e.target.value)}
                  placeholder={t('serviceCatalog.domainPlaceholder', 'e.g., payments, identity')} className={inputClass} />
                {errors.domain && <p className="text-xs text-danger mt-1">{errors.domain}</p>}
              </div>
            </div>
          </div>
        )}

        {/* ── Step 2: Classification ── */}
        {step === 2 && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.serviceType', 'Service Type')}</label>
                <select value={form.serviceType} onChange={(e) => set('serviceType', e.target.value)} className={selectClass}>
                  <optgroup label={t('serviceCatalog.typeGroupModern', 'Modern Services')}>
                    <option value="RestApi">REST API</option>
                    <option value="GraphqlApi">GraphQL API</option>
                    <option value="GrpcService">gRPC Service</option>
                    <option value="SoapService">SOAP Service</option>
                    <option value="KafkaProducer">Kafka Producer</option>
                    <option value="KafkaConsumer">Kafka Consumer</option>
                    <option value="BackgroundService">Background Service</option>
                    <option value="ScheduledProcess">Scheduled Process</option>
                    <option value="Gateway">API Gateway</option>
                  </optgroup>
                  <optgroup label={t('serviceCatalog.typeGroupPlatform', 'Platform & Integration')}>
                    <option value="IntegrationComponent">Integration Component</option>
                    <option value="SharedPlatformService">Shared Platform Service</option>
                    <option value="Framework">Framework / SDK</option>
                    <option value="ThirdParty">Third-Party Service</option>
                    <option value="LegacySystem">Legacy System</option>
                  </optgroup>
                  <optgroup label={t('serviceCatalog.typeGroupMainframe', 'Mainframe')}>
                    <option value="CobolProgram">COBOL Program</option>
                    <option value="CicsTransaction">CICS Transaction</option>
                    <option value="ImsTransaction">IMS Transaction</option>
                    <option value="BatchJob">Batch Job</option>
                    <option value="MainframeSystem">Mainframe System</option>
                    <option value="MqQueueManager">MQ Queue Manager</option>
                    <option value="ZosConnectApi">z/OS Connect API</option>
                  </optgroup>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.criticality', 'Criticality')}</label>
                <select value={form.criticality} onChange={(e) => set('criticality', e.target.value)} className={selectClass}>
                  <option value="Low">{t('serviceCatalog.criticalityLow', 'Low')}</option>
                  <option value="Medium">{t('serviceCatalog.criticalityMedium', 'Medium')}</option>
                  <option value="High">{t('serviceCatalog.criticalityHigh', 'High')}</option>
                  <option value="Critical">{t('serviceCatalog.criticalityCritical', 'Critical')}</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.exposure', 'Exposure')}</label>
                <select value={form.exposureType} onChange={(e) => set('exposureType', e.target.value)} className={selectClass}>
                  <option value="Internal">{t('serviceCatalog.exposureInternal', 'Internal')}</option>
                  <option value="Partner">{t('serviceCatalog.exposurePartner', 'Partner')}</option>
                  <option value="External">{t('serviceCatalog.exposureExternal', 'External / Public')}</option>
                </select>
              </div>
            </div>
            {/* ── Conditional fields by ServiceType ── */}
            <ConditionalTypeFields serviceType={form.serviceType} />
          </div>
        )}

        {/* ── Step 3: Ownership ── */}
        {step === 3 && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.team')} <span className="text-danger">*</span></label>
                <input type="text" value={form.team} onChange={(e) => set('team', e.target.value)}
                  placeholder={t('serviceCatalog.teamPlaceholder', 'e.g., platform-team')} className={inputClass} />
                {errors.team && <p className="text-xs text-danger mt-1">{errors.team}</p>}
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.technicalOwner', 'Technical Owner')}</label>
                <input type="text" value={form.technicalOwner} onChange={(e) => set('technicalOwner', e.target.value)}
                  placeholder={t('serviceCatalog.technicalOwnerPlaceholder', 'e.g., john.smith@company.com')} className={inputClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.businessOwner', 'Business Owner')}</label>
                <input type="text" value={form.businessOwner} onChange={(e) => set('businessOwner', e.target.value)}
                  placeholder={t('serviceCatalog.businessOwnerPlaceholder', 'e.g., Product Manager')} className={inputClass} />
              </div>
            </div>
          </div>
        )}

        {/* ── Step 4: References ── */}
        {step === 4 && (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.description')}</label>
              <textarea value={form.description} onChange={(e) => set('description', e.target.value)} rows={3}
                placeholder={t('serviceCatalog.descriptionPlaceholder', 'Describe the purpose and responsibilities of this service...')}
                className={`${inputClass} resize-none`} />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.documentationUrl', 'Documentation URL')}</label>
                <input type="url" value={form.documentationUrl} onChange={(e) => set('documentationUrl', e.target.value)}
                  placeholder={t('catalog.registration.placeholder.documentationUrl', 'https://docs.example.com/service')} className={monoClass} />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">{t('serviceCatalog.repositoryUrl', 'Repository URL')}</label>
                <input type="url" value={form.repositoryUrl} onChange={(e) => set('repositoryUrl', e.target.value)}
                  placeholder={t('catalog.registration.placeholder.repositoryUrl', 'https://github.com/org/repo')} className={monoClass} />
              </div>
            </div>
          </div>
        )}

        {/* ── Step 5: Confirmation ── */}
        {step === 5 && (
          <div className="space-y-3">
            <h3 className="text-sm font-semibold text-heading">{t('serviceCatalog.wizard.summaryTitle', 'Service Summary')}</h3>
            <div className="rounded-lg bg-surface border border-edge p-4 space-y-2 text-sm">
              <SummaryRow label={t('serviceCatalog.name')} value={form.name} mono />
              <SummaryRow label={t('serviceCatalog.domain', 'Domain')} value={form.domain} />
              <SummaryRow label={t('serviceCatalog.team')} value={form.team} />
              <SummaryRow label={t('serviceCatalog.serviceType', 'Service Type')} value={form.serviceType} />
              <SummaryRow label={t('serviceCatalog.criticality', 'Criticality')} value={form.criticality} />
              <SummaryRow label={t('serviceCatalog.exposure', 'Exposure')} value={form.exposureType} />
              {form.description && <SummaryRow label={t('serviceCatalog.description')} value={form.description} />}
              {form.technicalOwner && <SummaryRow label={t('serviceCatalog.technicalOwner', 'Technical Owner')} value={form.technicalOwner} />}
              {form.businessOwner && <SummaryRow label={t('serviceCatalog.businessOwner', 'Business Owner')} value={form.businessOwner} />}
              {form.documentationUrl && <SummaryRow label={t('serviceCatalog.documentationUrl', 'Docs')} value={form.documentationUrl} mono />}
              {form.repositoryUrl && <SummaryRow label={t('serviceCatalog.repositoryUrl', 'Repo')} value={form.repositoryUrl} mono />}
            </div>
          </div>
        )}

        {/* ── Navigation buttons ── */}
        <div className="flex justify-between pt-4 mt-4 border-t border-edge">
          <div>
            {step > 1 && (
              <Button variant="secondary" type="button" onClick={back}>
                <ChevronLeft size={14} className="mr-1" /> {t('serviceCatalog.wizard.back', 'Back')}
              </Button>
            )}
          </div>
          <div className="flex gap-2">
            <Button variant="secondary" type="button" onClick={onCancel}>{t('common.cancel')}</Button>
            {step < TOTAL_STEPS ? (
              <Button type="button" onClick={next}>
                {t('serviceCatalog.wizard.next', 'Next')} <ChevronRight size={14} className="ml-1" />
              </Button>
            ) : (
              <Button type="button" onClick={handleSubmit} loading={isSubmitting}>
                <Check size={14} className="mr-1" /> {t('serviceCatalog.wizard.register', 'Register Service')}
              </Button>
            )}
          </div>
        </div>
      </CardBody>
    </Card>
  );
}

// ── Helper: Summary row ───────────────────────────────────────────
function SummaryRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex gap-2">
      <span className="text-muted w-32 shrink-0">{label}:</span>
      <span className={`text-heading ${mono ? 'font-mono' : ''}`}>{value}</span>
    </div>
  );
}

// ── Conditional fields by ServiceType ─────────────────────────────
function ConditionalTypeFields({ serviceType }: { serviceType: string }) {
  const { t } = useTranslation();
  
  // These are informational hint fields (metadata not yet persisted by backend)
  const fields: Record<string, { label: string; placeholder: string }[]> = {
    RestApi: [
      { label: t('serviceCatalog.typeMetadata.baseUrl', 'Base URL'), placeholder: 'https://api.example.com/v1' },
    ],
    GrpcService: [
      { label: t('serviceCatalog.typeMetadata.protoFileUrl', 'Proto File URL'), placeholder: 'https://repo.example.com/protos/service.proto' },
    ],
    KafkaProducer: [
      { label: t('serviceCatalog.typeMetadata.brokerCluster', 'Broker Cluster'), placeholder: 'kafka-prod-01' },
      { label: t('serviceCatalog.typeMetadata.topics', 'Topics'), placeholder: 'orders.created, orders.updated' },
    ],
    KafkaConsumer: [
      { label: t('serviceCatalog.typeMetadata.brokerCluster', 'Broker Cluster'), placeholder: 'kafka-prod-01' },
      { label: t('serviceCatalog.typeMetadata.topics', 'Topics'), placeholder: 'orders.created' },
    ],
    BackgroundService: [
      { label: t('serviceCatalog.typeMetadata.schedule', 'Schedule (cron)'), placeholder: '0 */5 * * *' },
      { label: t('serviceCatalog.typeMetadata.healthCheckUrl', 'Health Check URL'), placeholder: 'https://service.internal/health' },
    ],
    Gateway: [
      { label: t('serviceCatalog.typeMetadata.upstreamServices', 'Upstream Services'), placeholder: 'auth-service, user-service' },
    ],
    Framework: [
      { label: t('serviceCatalog.typeMetadata.language', 'Language'), placeholder: 'C#, TypeScript, Java' },
      { label: t('serviceCatalog.typeMetadata.packageManager', 'Package Manager'), placeholder: 'NuGet, npm, Maven' },
      { label: t('serviceCatalog.typeMetadata.artifactRegistry', 'Artifact Registry URL'), placeholder: 'https://nuget.company.com' },
      { label: t('serviceCatalog.typeMetadata.sdkVersion', 'SDK Version'), placeholder: '3.2.1' },
    ],
  };

  const typeFields = fields[serviceType];
  if (!typeFields) return null;

  return (
    <div className="mt-3 pt-3 border-t border-edge/50">
      <p className="text-xs text-muted mb-2">{t('serviceCatalog.wizard.conditionalHint', 'Additional context for this service type (optional):')}</p>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {typeFields.map((f) => (
          <div key={f.label}>
            <label className="block text-xs font-medium text-muted mb-1">{f.label}</label>
            <input type="text" placeholder={f.placeholder}
              className="w-full rounded-md bg-canvas border border-edge/60 px-3 py-1.5 text-xs text-heading placeholder:text-muted/60 focus:outline-none focus:ring-1 focus:ring-accent transition-colors" />
          </div>
        ))}
      </div>
    </div>
  );
}
