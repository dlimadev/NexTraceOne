/**
 * Wizard de registo de serviço no catálogo — jornada Betterstack (2 painéis).
 *
 * Layout: formulário (esquerda, stepper + steps com componentes do DS) +
 * painel de preview AO VIVO (direita) que monta o card do serviço em tempo real.
 *
 * Step 1: Identidade (name, domain, subDomain, capability)
 * Step 2: Classificação (serviceType, criticality, exposureType, dataClassification, regulatoryScope, infrastructureProvider, runtimeLanguage)
 * Step 3: Ownership (team, technicalOwner, businessOwner, productOwner, contactChannel)
 * Step 4: Referências (description, documentationUrl, repositoryUrl)
 * Step 5: Confirmação (revisão — apoiada no preview ao vivo)
 */
import { useState, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ChevronLeft, ChevronRight, Check, X, Boxes, ShieldAlert, Globe2 } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button, TextField, TextArea, Select, Badge } from '../../../shared/ui';
import { cn } from '../../../lib/cn';

export interface ServiceFormData {
  name: string;
  domain: string;
  subDomain: string;
  capability: string;
  team: string;
  description: string;
  serviceType: string;
  criticality: string;
  exposureType: string;
  technicalOwner: string;
  businessOwner: string;
  productOwner: string;
  contactChannel: string;
  documentationUrl: string;
  repositoryUrl: string;
  dataClassification: string;
  regulatoryScope: string;
  infrastructureProvider: string;
  runtimeLanguage: string;
}

interface ServiceRegistrationWizardProps {
  onSubmit: (data: ServiceFormData) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
}

const INITIAL_FORM: ServiceFormData = {
  name: '', domain: '', subDomain: '', capability: '', team: '', description: '',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  technicalOwner: '', businessOwner: '', productOwner: '', contactChannel: '',
  documentationUrl: '', repositoryUrl: '',
  dataClassification: 'Internal', regulatoryScope: 'None',
  infrastructureProvider: '', runtimeLanguage: '',
};

const TOTAL_STEPS = 5;

const criticalityBadge: Record<string, 'danger' | 'warning' | 'info' | 'default'> = {
  Critical: 'danger', High: 'warning', Medium: 'info', Low: 'default',
};

export function ServiceRegistrationWizard({ onSubmit, onCancel, isSubmitting }: ServiceRegistrationWizardProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState<ServiceFormData>(INITIAL_FORM);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const set = useCallback((field: keyof ServiceFormData) => (
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
      const value = e.target.value;
      setForm((f) => ({ ...f, [field]: value }));
      setErrors((er) => ({ ...er, [field]: '' }));
    }
  ), []);

  // ── Validação por step ──────────────────────────────────────────
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
  const handleSubmit = useCallback(() => { onSubmit(form); }, [form, onSubmit]);

  const stepLabels = [
    t('serviceCatalog.wizard.step1', 'Identity'),
    t('serviceCatalog.wizard.step2', 'Classification'),
    t('serviceCatalog.wizard.step3', 'Ownership'),
    t('serviceCatalog.wizard.step4', 'References'),
    t('serviceCatalog.wizard.step5', 'Confirmation'),
  ];

  // ── Opções de tipo de serviço (achatadas p/ DS Select) ──────────
  const serviceTypeOptions = useMemo(() => ([
    { value: 'RestApi', label: t('serviceCatalog.typeRestApi', 'REST API') },
    { value: 'GraphqlApi', label: t('serviceCatalog.typeGraphqlApi', 'GraphQL API') },
    { value: 'GrpcService', label: t('serviceCatalog.typeGrpcService', 'gRPC Service') },
    { value: 'SoapService', label: t('serviceCatalog.typeSoapService', 'SOAP Service') },
    { value: 'KafkaProducer', label: t('serviceCatalog.typeKafkaProducer', 'Kafka Producer') },
    { value: 'KafkaConsumer', label: t('serviceCatalog.typeKafkaConsumer', 'Kafka Consumer') },
    { value: 'BackgroundService', label: t('serviceCatalog.typeBackgroundService', 'Background Service') },
    { value: 'ScheduledProcess', label: t('serviceCatalog.typeScheduledProcess', 'Scheduled Process') },
    { value: 'Gateway', label: t('serviceCatalog.typeGateway', 'API Gateway') },
    { value: 'IntegrationComponent', label: t('serviceCatalog.typeIntegrationComponent', 'Integration Component') },
    { value: 'SharedPlatformService', label: t('serviceCatalog.typeSharedPlatformService', 'Shared Platform Service') },
    { value: 'Framework', label: t('serviceCatalog.typeFramework', 'Framework / SDK') },
    { value: 'ThirdParty', label: t('serviceCatalog.typeThirdParty', 'Third-Party Service') },
    { value: 'LegacySystem', label: t('serviceCatalog.typeLegacySystem', 'Legacy System') },
    { value: 'CobolProgram', label: t('serviceCatalog.typeCobolProgram', 'COBOL Program') },
    { value: 'CicsTransaction', label: t('serviceCatalog.typeCicsTransaction', 'CICS Transaction') },
    { value: 'ImsTransaction', label: t('serviceCatalog.typeImsTransaction', 'IMS Transaction') },
    { value: 'BatchJob', label: t('serviceCatalog.typeBatchJob', 'Batch Job') },
    { value: 'MainframeSystem', label: t('serviceCatalog.typeMainframeSystem', 'Mainframe System') },
    { value: 'MqQueueManager', label: t('serviceCatalog.typeMqQueueManager', 'MQ Queue Manager') },
    { value: 'ZosConnectApi', label: t('serviceCatalog.typeZosConnectApi', 'z/OS Connect API') },
  ]), [t]);

  return (
    <Card className="mb-6">
      <CardHeader>
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-heading">{t('serviceCatalog.registerServiceTitle')}</h2>
          <button type="button" onClick={onCancel} aria-label={t('common.close', 'Close')} className="text-muted hover:text-heading transition-colors">
            <X size={18} />
          </button>
        </div>
      </CardHeader>
      <CardBody>
        <div className="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_300px] gap-6">
          {/* ── Painel do formulário ── */}
          <div className="min-w-0">
            {/* Stepper */}
            <div className="flex items-center gap-1 mb-6">
              {stepLabels.map((label, i) => {
                const stepNum = i + 1;
                const isActive = stepNum === step;
                const isDone = stepNum < step;
                return (
                  <div key={stepNum} className="flex items-center gap-1.5 flex-1 min-w-0">
                    <div className={cn(
                      'flex items-center justify-center w-7 h-7 rounded-full text-xs font-bold shrink-0 transition-colors',
                      isDone ? 'bg-success text-on-accent' : isActive ? 'bg-accent text-on-accent' : 'bg-elevated border border-edge text-muted',
                    )}>
                      {isDone ? <Check size={14} /> : stepNum}
                    </div>
                    <span className={cn('text-xs truncate hidden sm:inline', isActive ? 'text-heading font-medium' : 'text-muted')}>{label}</span>
                    {i < stepLabels.length - 1 && <div className={cn('h-px flex-1', isDone ? 'bg-success' : 'bg-edge')} />}
                  </div>
                );
              })}
            </div>

            {/* Step 1: Identity */}
            {step === 1 && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <TextField
                  label={`${t('serviceCatalog.name')} *`}
                  value={form.name} onChange={set('name')}
                  placeholder={t('serviceCatalog.namePlaceholder', 'e.g., payment-service')}
                  error={errors.name} className="font-mono"
                />
                <TextField
                  label={`${t('serviceCatalog.domain', 'Domain')} *`}
                  value={form.domain} onChange={set('domain')}
                  placeholder={t('serviceCatalog.domainPlaceholder', 'e.g., payments, identity')}
                  error={errors.domain}
                />
                <TextField
                  label={t('serviceCatalog.wizard.subDomain', 'Sub-Domain')}
                  value={form.subDomain} onChange={set('subDomain')}
                  placeholder={t('serviceCatalog.wizard.subDomainPlaceholder', 'fraud, payments-core')}
                />
                <TextField
                  label={t('serviceCatalog.wizard.capability', 'Business Capability')}
                  value={form.capability} onChange={set('capability')}
                  placeholder={t('serviceCatalog.wizard.capabilityPlaceholder', 'Payment Processing, Identity Verification')}
                />
              </div>
            )}

            {/* Step 2: Classification */}
            {step === 2 && (
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Select label={t('serviceCatalog.serviceType', 'Service Type')} value={form.serviceType} onChange={set('serviceType')} options={serviceTypeOptions} />
                  <Select label={t('serviceCatalog.criticality', 'Criticality')} value={form.criticality} onChange={set('criticality')} options={[
                    { value: 'Low', label: t('serviceCatalog.criticalityLow', 'Low') },
                    { value: 'Medium', label: t('serviceCatalog.criticalityMedium', 'Medium') },
                    { value: 'High', label: t('serviceCatalog.criticalityHigh', 'High') },
                    { value: 'Critical', label: t('serviceCatalog.criticalityCritical', 'Critical') },
                  ]} />
                  <Select label={t('serviceCatalog.exposure', 'Exposure')} value={form.exposureType} onChange={set('exposureType')} options={[
                    { value: 'Internal', label: t('serviceCatalog.exposureInternal', 'Internal') },
                    { value: 'Partner', label: t('serviceCatalog.exposurePartner', 'Partner') },
                    { value: 'External', label: t('serviceCatalog.exposureExternal', 'External / Public') },
                  ]} />
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <Select label={t('serviceCatalog.wizard.dataClassification', 'Data Classification')} value={form.dataClassification} onChange={set('dataClassification')} options={[
                    { value: 'Public', label: 'Public' }, { value: 'Internal', label: 'Internal' },
                    { value: 'Confidential', label: 'Confidential' }, { value: 'Restricted', label: 'Restricted' },
                  ]} />
                  <Select label={t('serviceCatalog.wizard.regulatoryScope', 'Regulatory Scope')} value={form.regulatoryScope} onChange={set('regulatoryScope')} options={[
                    { value: 'None', label: 'None' }, { value: 'PCI-DSS', label: 'PCI-DSS' }, { value: 'LGPD', label: 'LGPD' },
                    { value: 'GDPR', label: 'GDPR' }, { value: 'HIPAA', label: 'HIPAA' },
                  ]} />
                  <TextField label={t('serviceCatalog.wizard.infrastructureProvider', 'Infrastructure Provider')} value={form.infrastructureProvider} onChange={set('infrastructureProvider')} placeholder={t('serviceCatalog.wizard.infrastructureProviderPlaceholder', 'Kubernetes, IIS, VM')} />
                  <TextField label={t('serviceCatalog.wizard.runtimeLanguage', 'Runtime Language')} value={form.runtimeLanguage} onChange={set('runtimeLanguage')} placeholder={t('serviceCatalog.wizard.runtimeLanguagePlaceholder', 'C#, Java, Python')} />
                </div>
              </div>
            )}

            {/* Step 3: Ownership */}
            {step === 3 && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <TextField label={`${t('serviceCatalog.team')} *`} value={form.team} onChange={set('team')} placeholder={t('serviceCatalog.teamPlaceholder', 'e.g., platform-team')} error={errors.team} />
                <TextField label={t('serviceCatalog.technicalOwner', 'Technical Owner')} value={form.technicalOwner} onChange={set('technicalOwner')} placeholder={t('serviceCatalog.technicalOwnerPlaceholder', 'e.g., john.smith@company.com')} />
                <TextField label={t('serviceCatalog.businessOwner', 'Business Owner')} value={form.businessOwner} onChange={set('businessOwner')} placeholder={t('serviceCatalog.businessOwnerPlaceholder', 'e.g., Product Manager')} />
                <TextField label={t('serviceCatalog.wizard.productOwner', 'Product Owner')} value={form.productOwner} onChange={set('productOwner')} placeholder={t('serviceCatalog.wizard.productOwnerPlaceholder', 'jane.doe@company.com')} />
                <TextField label={t('serviceCatalog.wizard.contactChannel', 'Contact Channel')} value={form.contactChannel} onChange={set('contactChannel')} placeholder={t('serviceCatalog.wizard.contactChannelPlaceholder', '#payments-support')} />
              </div>
            )}

            {/* Step 4: References */}
            {step === 4 && (
              <div className="space-y-4">
                <TextArea label={t('serviceCatalog.description')} value={form.description} onChange={set('description')} rows={3} placeholder={t('serviceCatalog.descriptionPlaceholder', 'Describe the purpose and responsibilities of this service...')} />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <TextField label={t('serviceCatalog.documentationUrl', 'Documentation URL')} type="url" value={form.documentationUrl} onChange={set('documentationUrl')} placeholder={t('catalog.registration.placeholder.documentationUrl', 'https://docs.example.com/service')} className="font-mono" />
                  <TextField label={t('serviceCatalog.repositoryUrl', 'Repository URL')} type="url" value={form.repositoryUrl} onChange={set('repositoryUrl')} placeholder={t('catalog.registration.placeholder.repositoryUrl', 'https://github.com/org/repo')} className="font-mono" />
                </div>
              </div>
            )}

            {/* Step 5: Confirmation */}
            {step === 5 && (
              <div className="space-y-3">
                <h3 className="text-sm font-semibold text-heading">{t('serviceCatalog.wizard.summaryTitle', 'Review & confirm')}</h3>
                <p className="text-sm text-muted">{t('serviceCatalog.wizard.summaryHint', 'Confira o card ao lado. Está como você quer? Registre o serviço.')}</p>
                <div className="rounded-xl bg-elevated border border-edge p-4 space-y-2 text-sm">
                  <SummaryRow label={t('serviceCatalog.name')} value={form.name} mono />
                  <SummaryRow label={t('serviceCatalog.domain', 'Domain')} value={form.domain} />
                  <SummaryRow label={t('serviceCatalog.team')} value={form.team} />
                  <SummaryRow label={t('serviceCatalog.serviceType', 'Service Type')} value={form.serviceType} />
                  <SummaryRow label={t('serviceCatalog.criticality', 'Criticality')} value={form.criticality} />
                  <SummaryRow label={t('serviceCatalog.exposure', 'Exposure')} value={form.exposureType} />
                  {form.regulatoryScope !== 'None' && <SummaryRow label={t('serviceCatalog.wizard.regulatoryScope', 'Regulatory Scope')} value={form.regulatoryScope} />}
                </div>
              </div>
            )}

            {/* Navegação */}
            <div className="flex justify-between pt-4 mt-6 border-t border-edge">
              <div>
                {step > 1 && (
                  <Button variant="outline" type="button" onClick={back} icon={<ChevronLeft size={14} />}>
                    {t('serviceCatalog.wizard.back', 'Back')}
                  </Button>
                )}
              </div>
              <div className="flex gap-2">
                <Button variant="ghost" type="button" onClick={onCancel}>{t('common.cancel')}</Button>
                {step < TOTAL_STEPS ? (
                  <Button variant="primary" type="button" onClick={next} icon={<ChevronRight size={14} />}>
                    {t('serviceCatalog.wizard.next', 'Next')}
                  </Button>
                ) : (
                  <Button variant="primary" type="button" onClick={handleSubmit} loading={isSubmitting} icon={<Check size={14} />}>
                    {t('serviceCatalog.wizard.register', 'Register Service')}
                  </Button>
                )}
              </div>
            </div>
          </div>

          {/* ── Painel de preview ao vivo ── */}
          <aside className="lg:border-l lg:border-edge lg:pl-6">
            <ServiceCardPreview form={form} step={step} />
          </aside>
        </div>
      </CardBody>
    </Card>
  );
}

// ── Preview ao vivo do card do serviço ────────────────────────────
function ServiceCardPreview({ form, step }: { form: ServiceFormData; step: number }) {
  const { t } = useTranslation();
  const initial = form.name.trim().charAt(0).toUpperCase() || '?';
  const filled = [form.name, form.domain, form.team].filter((v) => v.trim()).length;

  return (
    <div className="lg:sticky lg:top-4">
      <div className="flex items-center justify-between mb-3">
        <span className="type-overline text-muted">{t('serviceCatalog.wizard.livePreview', 'Live preview')}</span>
        <span className="flex items-center gap-1.5 text-[11px] text-success">
          <span className="w-1.5 h-1.5 rounded-full bg-success animate-pulse-soft" /> {t('serviceCatalog.wizard.live', 'Live')}
        </span>
      </div>

      <div className="rounded-xl border border-edge bg-card p-4 shadow-sm">
        <div className="flex items-start gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-accent-muted text-accent font-bold shrink-0">
            {initial}
          </div>
          <div className="min-w-0">
            <p className={cn('font-mono text-sm font-medium truncate', form.name ? 'text-heading' : 'text-faded')}>
              {form.name || t('serviceCatalog.wizard.previewNamePlaceholder', 'service-name')}
            </p>
            <p className="text-xs text-muted truncate">
              {form.domain || t('serviceCatalog.wizard.previewDomainPlaceholder', 'domain')}
              {form.subDomain && ` · ${form.subDomain}`}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap gap-1.5 mt-3">
          <Badge variant="primary" size="sm">{form.serviceType}</Badge>
          <Badge variant={criticalityBadge[form.criticality] ?? 'default'} size="sm">{form.criticality}</Badge>
          <Badge variant="default" size="sm" icon={<Globe2 size={10} />}>{form.exposureType}</Badge>
          {form.regulatoryScope !== 'None' && (
            <Badge variant="warning" size="sm" icon={<ShieldAlert size={10} />}>{form.regulatoryScope}</Badge>
          )}
        </div>

        {form.description && (
          <p className="text-xs text-muted mt-3 line-clamp-3">{form.description}</p>
        )}

        <div className="mt-3 pt-3 border-t border-edge/60 space-y-1 text-xs">
          {form.team && <PreviewMeta icon={<Boxes size={11} />} value={form.team} />}
          {form.technicalOwner && <PreviewMeta value={form.technicalOwner} />}
        </div>
      </div>

      {/* Progresso de campos essenciais */}
      <div className="mt-3">
        <div className="flex items-center justify-between text-[11px] text-muted mb-1">
          <span>{t('serviceCatalog.wizard.essentials', 'Essentials')}</span>
          <span className="font-mono">{filled}/3</span>
        </div>
        <div className="h-1.5 rounded-full bg-elevated overflow-hidden">
          <div className="h-full bg-accent transition-all" style={{ width: `${(filled / 3) * 100}%` }} />
        </div>
        <p className="text-[11px] text-faded mt-2">{t('serviceCatalog.wizard.stepOf', 'Step {{step}} of {{total}}', { step, total: TOTAL_STEPS })}</p>
      </div>
    </div>
  );
}

function PreviewMeta({ icon, value }: { icon?: React.ReactNode; value: string }) {
  return (
    <div className="flex items-center gap-1.5 text-muted">
      {icon && <span className="text-faded shrink-0">{icon}</span>}
      <span className="truncate">{value}</span>
    </div>
  );
}

// ── Helper: linha de resumo ───────────────────────────────────────
function SummaryRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div className="flex gap-2">
      <span className="text-muted w-32 shrink-0">{label}:</span>
      <span className={cn('text-heading', mono && 'font-mono')}>{value}</span>
    </div>
  );
}
