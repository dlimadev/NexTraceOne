import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Server,
  Database,
  Shield,
  BrainCircuit,
  Globe,
  Users,
  CheckCircle2,
  ArrowRight,
  ArrowLeft,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';

// ─── Wizard step definitions ──────────────────────────────────────────────────

type StepStatus = 'pending' | 'active' | 'completed' | 'skipped';

interface WizardStep {
  id: string;
  icon: React.ReactNode;
  required: boolean;
}

const STEPS: WizardStep[] = [
  { id: 'welcome', icon: <Server size={20} />, required: true },
  { id: 'database', icon: <Database size={20} />, required: true },
  { id: 'security', icon: <Shield size={20} />, required: true },
  { id: 'organization', icon: <Users size={20} />, required: true },
  { id: 'ai', icon: <BrainCircuit size={20} />, required: false },
  { id: 'network', icon: <Globe size={20} />, required: false },
  { id: 'review', icon: <CheckCircle2 size={20} />, required: true },
];

// ─── Helper components ────────────────────────────────────────────────────────

interface StepperItemProps {
  label: string;
  index: number;
  status: StepStatus;
  icon: React.ReactNode;
}

function StepperItem({ label, index, status, icon }: StepperItemProps) {
  const base = 'flex flex-col items-center gap-1 text-xs font-medium';
  const colors = {
    pending: 'text-muted',
    active: 'text-accent',
    completed: 'text-success',
    skipped: 'text-muted/50',
  };
  const circleBg = {
    pending: 'bg-surface border border-border text-muted',
    active: 'bg-accent text-white border border-accent',
    completed: 'bg-success/10 border border-success text-success',
    skipped: 'bg-surface border border-border/40 text-muted/40',
  };

  return (
    <div className={`${base} ${colors[status]}`}>
      <div className={`flex items-center justify-center w-8 h-8 rounded-full text-sm ${circleBg[status]}`}>
        {status === 'completed' ? <CheckCircle2 size={15} /> : status === 'active' ? icon : index + 1}
      </div>
      <span className="hidden sm:block max-w-[70px] text-center leading-tight">{label}</span>
    </div>
  );
}

// ─── Step content panels ──────────────────────────────────────────────────────

interface FormFieldProps {
  label: string;
  hint?: string;
  children: React.ReactNode;
}

function FormField({ label, hint, children }: FormFieldProps) {
  return (
    <div className="space-y-1.5">
      <label className="text-sm font-medium text-heading">{label}</label>
      {children}
      {hint && <p className="text-xs text-muted">{hint}</p>}
    </div>
  );
}

interface InputFieldProps {
  placeholder?: string;
  type?: string;
  value: string;
  onChange: (v: string) => void;
}

function InputField({ placeholder, type = 'text', value, onChange }: InputFieldProps) {
  return (
    <input
      type={type}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      className="w-full px-3 py-2 bg-surface border border-border rounded-lg text-sm text-heading placeholder:text-muted/50 focus:outline-none focus:ring-2 focus:ring-accent/40"
    />
  );
}

// ─── Step panels ──────────────────────────────────────────────────────────────

interface StepPanelProps {
  stepId: string;
  t: (key: string, opts?: Record<string, unknown>) => string;
  formData: Record<string, string>;
  onFormChange: (key: string, value: string) => void;
}

function StepPanel({ stepId, t, formData, onFormChange }: StepPanelProps) {
  switch (stepId) {
    case 'welcome':
      return (
        <div className="space-y-4">
          <div className="bg-accent/5 border border-accent/20 rounded-xl p-4">
            <p className="text-sm text-muted">{t('setup.welcome.introText')}</p>
          </div>
          <ul className="space-y-2">
            {['database', 'security', 'organization', 'ai', 'network'].map((item) => (
              <li key={item} className="flex items-center gap-2 text-sm text-muted">
                <CheckCircle2 size={14} className="text-success shrink-0" />
                {t(`setup.welcome.feature_${item}`)}
              </li>
            ))}
          </ul>
          <div className="bg-warning/10 border border-warning/20 rounded-lg p-3 flex items-start gap-2">
            <AlertTriangle size={14} className="text-warning mt-0.5 shrink-0" />
            <p className="text-xs text-muted">{t('setup.welcome.prerequisite')}</p>
          </div>
        </div>
      );

    case 'database':
      return (
        <div className="space-y-4">
          <FormField label={t('setup.database.hostLabel')} hint={t('setup.database.hostHint')}>
            <InputField
              placeholder="localhost"
              value={formData['db_host'] ?? ''}
              onChange={(v) => onFormChange('db_host', v)}
            />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label={t('setup.database.portLabel')}>
              <InputField
                placeholder="5432"
                value={formData['db_port'] ?? '5432'}
                onChange={(v) => onFormChange('db_port', v)}
              />
            </FormField>
            <FormField label={t('setup.database.nameLabel')}>
              <InputField
                placeholder="nextraceone"
                value={formData['db_name'] ?? 'nextraceone'}
                onChange={(v) => onFormChange('db_name', v)}
              />
            </FormField>
          </div>
          <FormField label={t('setup.database.usernameLabel')}>
            <InputField
              placeholder="nextraceone"
              value={formData['db_user'] ?? ''}
              onChange={(v) => onFormChange('db_user', v)}
            />
          </FormField>
          <FormField label={t('setup.database.passwordLabel')}>
            <InputField
              type="password"
              value={formData['db_password'] ?? ''}
              onChange={(v) => onFormChange('db_password', v)}
            />
          </FormField>
        </div>
      );

    case 'security':
      return (
        <div className="space-y-4">
          <FormField label={t('setup.security.jwtLabel')} hint={t('setup.security.jwtHint')}>
            <InputField
              type="password"
              value={formData['jwt_secret'] ?? ''}
              onChange={(v) => onFormChange('jwt_secret', v)}
            />
          </FormField>
          <FormField label={t('setup.security.encKeyLabel')} hint={t('setup.security.encKeyHint')}>
            <InputField
              type="password"
              value={formData['enc_key'] ?? ''}
              onChange={(v) => onFormChange('enc_key', v)}
            />
          </FormField>
          <FormField label={t('setup.security.corsLabel')} hint={t('setup.security.corsHint')}>
            <InputField
              placeholder="https://nextraceone.acme.com"
              value={formData['cors_origin'] ?? ''}
              onChange={(v) => onFormChange('cors_origin', v)}
            />
          </FormField>
        </div>
      );

    case 'organization':
      return (
        <div className="space-y-4">
          <FormField label={t('setup.organization.nameLabel')}>
            <InputField
              placeholder="Acme Corp"
              value={formData['org_name'] ?? ''}
              onChange={(v) => onFormChange('org_name', v)}
            />
          </FormField>
          <FormField label={t('setup.organization.adminEmailLabel')}>
            <InputField
              type="email"
              placeholder="admin@acme.com"
              value={formData['admin_email'] ?? ''}
              onChange={(v) => onFormChange('admin_email', v)}
            />
          </FormField>
          <FormField label={t('setup.organization.adminPasswordLabel')}>
            <InputField
              type="password"
              value={formData['admin_password'] ?? ''}
              onChange={(v) => onFormChange('admin_password', v)}
            />
          </FormField>
        </div>
      );

    case 'ai':
      return (
        <div className="space-y-4">
          <div className="bg-accent/5 border border-accent/20 rounded-xl p-3">
            <p className="text-xs text-muted">{t('setup.ai.intro')}</p>
          </div>
          <FormField label={t('setup.ai.ollamaUrlLabel')} hint={t('setup.ai.ollamaUrlHint')}>
            <InputField
              placeholder="http://localhost:11434"
              value={formData['ollama_url'] ?? 'http://localhost:11434'}
              onChange={(v) => onFormChange('ollama_url', v)}
            />
          </FormField>
          <FormField label={t('setup.ai.defaultModelLabel')}>
            <InputField
              placeholder="qwen3.5:9b"
              value={formData['ollama_model'] ?? 'qwen3.5:9b'}
              onChange={(v) => onFormChange('ollama_model', v)}
            />
          </FormField>
        </div>
      );

    case 'network':
      return (
        <div className="space-y-4">
          <FormField label={t('setup.network.smtpHostLabel')} hint={t('setup.network.smtpHostHint')}>
            <InputField
              placeholder="smtp.acme.com"
              value={formData['smtp_host'] ?? ''}
              onChange={(v) => onFormChange('smtp_host', v)}
            />
          </FormField>
          <div className="grid grid-cols-2 gap-3">
            <FormField label={t('setup.network.smtpPortLabel')}>
              <InputField
                placeholder="587"
                value={formData['smtp_port'] ?? '587'}
                onChange={(v) => onFormChange('smtp_port', v)}
              />
            </FormField>
            <FormField label={t('setup.network.smtpFromLabel')}>
              <InputField
                type="email"
                placeholder="noreply@acme.com"
                value={formData['smtp_from'] ?? ''}
                onChange={(v) => onFormChange('smtp_from', v)}
              />
            </FormField>
          </div>
        </div>
      );

    case 'review':
      return (
        <div className="space-y-3">
          <p className="text-sm text-muted">{t('setup.review.intro')}</p>
          {[
            { label: t('setup.review.database'), value: formData['db_host'] ? `${formData['db_host']}:${formData['db_port'] ?? '5432'}` : '—' },
            { label: t('setup.review.organization'), value: formData['org_name'] ?? '—' },
            { label: t('setup.review.adminEmail'), value: formData['admin_email'] ?? '—' },
            { label: t('setup.review.ai'), value: formData['ollama_url'] ?? t('setup.review.notConfigured') },
            { label: t('setup.review.smtp'), value: formData['smtp_host'] ?? t('setup.review.notConfigured') },
          ].map(({ label, value }) => (
            <div key={label} className="flex items-center justify-between py-2 border-b border-border last:border-0">
              <span className="text-sm text-muted">{label}</span>
              <span className="text-sm font-medium text-heading">{value}</span>
            </div>
          ))}
          <div className="bg-warning/10 border border-warning/20 rounded-lg p-3 mt-2 flex items-start gap-2">
            <AlertTriangle size={14} className="text-warning mt-0.5 shrink-0" />
            <p className="text-xs text-muted">{t('setup.review.appsettingsNote')}</p>
          </div>
        </div>
      );

    default:
      return null;
  }
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export function SetupWizardPage() {
  const { t } = useTranslation();
  const [currentStep, setCurrentStep] = useState(0);
  const [formData, setFormData] = useState<Record<string, string>>({});
  const [completed, setCompleted] = useState(false);

  const step = STEPS[currentStep];
  const isFirst = currentStep === 0;
  const isLast = currentStep === STEPS.length - 1;

  const getStepStatus = (idx: number): StepStatus => {
    if (idx < currentStep) return 'completed';
    if (idx === currentStep) return 'active';
    return 'pending';
  };

  const handleNext = () => {
    if (isLast) {
      setCompleted(true);
    } else {
      setCurrentStep((prev) => prev + 1);
    }
  };

  const handleBack = () => {
    if (!isFirst) setCurrentStep((prev) => prev - 1);
  };

  const handleSkip = () => {
    setCurrentStep((prev) => prev + 1);
  };

  const handleFormChange = (key: string, value: string) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  // Completion screen
  if (completed) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center py-12 px-4">
        <div className="text-center max-w-md">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-success/10 rounded-full mb-4">
            <CheckCircle2 size={32} className="text-success" />
          </div>
          <h1 className="text-2xl font-bold text-heading mb-2">{t('setup.completed.title')}</h1>
          <p className="text-muted text-sm mb-6">{t('setup.completed.subtitle')}</p>
          <Button variant="primary" onClick={() => { window.location.href = '/preflight'; }}>
            {t('setup.completed.runPreflight')}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background flex flex-col items-center justify-center py-12 px-4">
      {/* Header */}
      <div className="mb-8 text-center">
        <div className="inline-flex items-center justify-center w-12 h-12 bg-accent/10 rounded-xl mb-3">
          <Server size={22} className="text-accent" />
        </div>
        <h1 className="text-xl font-bold text-heading">{t('setup.title')}</h1>
        <p className="text-sm text-muted mt-1">{t('setup.subtitle')}</p>
      </div>

      <div className="w-full max-w-xl space-y-6">
        {/* Stepper */}
        <div className="flex items-center justify-between px-2">
          {STEPS.map((s, idx) => (
            <div key={s.id} className="flex items-center flex-1">
              <StepperItem
                label={t(`setup.${s.id}.stepLabel`)}
                index={idx}
                status={getStepStatus(idx)}
                icon={s.icon}
              />
              {idx < STEPS.length - 1 && (
                <div className={`flex-1 h-px mx-2 ${idx < currentStep ? 'bg-success/40' : 'bg-border'}`} />
              )}
            </div>
          ))}
        </div>

        {/* Step card */}
        <Card>
          <CardBody>
            <div className="flex items-center gap-3 mb-5">
              <div className="flex items-center justify-center w-9 h-9 bg-accent/10 rounded-lg text-accent">
                {step.icon}
              </div>
              <div>
                <h2 className="font-semibold text-heading text-sm">{t(`setup.${step.id}.title`)}</h2>
                <p className="text-xs text-muted">{t(`setup.${step.id}.description`)}</p>
              </div>
              {!step.required && (
                <Badge variant="default" className="ml-auto">{t('setup.optional')}</Badge>
              )}
            </div>

            <StepPanel
              stepId={step.id}
              t={t}
              formData={formData}
              onFormChange={handleFormChange}
            />
          </CardBody>
        </Card>

        {/* Navigation */}
        <div className="flex items-center justify-between">
          <Button
            variant="ghost"
            size="sm"
            onClick={handleBack}
            disabled={isFirst}
          >
            <ArrowLeft size={14} />
            {t('setup.back')}
          </Button>

          <div className="flex items-center gap-2">
            {!step.required && !isLast && (
              <Button variant="ghost" size="sm" onClick={handleSkip}>
                {t('setup.skip')}
              </Button>
            )}
            <Button variant="primary" size="sm" onClick={handleNext}>
              {isLast ? t('setup.finish') : t('setup.next')}
              {!isLast && <ArrowRight size={14} />}
            </Button>
          </div>
        </div>

        {/* Progress indicator */}
        <p className="text-xs text-muted text-center">
          {t('setup.progress', { current: currentStep + 1, total: STEPS.length })}
        </p>
      </div>
    </div>
  );
}
