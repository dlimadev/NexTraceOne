import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Bot, Sparkles, Users, CheckCircle2, ChevronRight, ChevronLeft } from 'lucide-react';
import { Modal } from '../../../components/Modal';
import { Button } from '../../../components/Button';
import { aiGovernanceApi } from '../api/aiGovernance';

/** Personas disponíveis para selecção no onboarding. */
const PERSONAS = ['Engineer', 'TechLead', 'Architect', 'Product', 'Executive'] as const;
type OnboardingPersona = (typeof PERSONAS)[number];

interface AiOnboardingModalProps {
  open: boolean;
  onFinish: () => void;
}

const TOTAL_STEPS = 4;

/**
 * Modal de onboarding em 4 passos para o AI Assistant.
 * Guia o utilizador pela seleção de persona, introdução ao assistente,
 * descoberta dos AI Agents e CTA para iniciar.
 */
export function AiOnboardingModal({ open, onFinish }: AiOnboardingModalProps) {
  const { t } = useTranslation();
  const [step, setStep] = useState(1);
  const [selectedPersona, setSelectedPersona] = useState<OnboardingPersona | null>(null);
  const [isFinishing, setIsFinishing] = useState(false);

  const handleFinish = async () => {
    setIsFinishing(true);
    try {
      await aiGovernanceApi.completeOnboarding(`onboarding-${Date.now()}`);
    } finally {
      setIsFinishing(false);
      onFinish();
    }
  };

  const handleClose = () => {
    onFinish();
  };

  const stepContent: Record<number, React.ReactNode> = {
    1: (
      <div className="space-y-4">
        <div className="flex items-center gap-3 mb-2">
          <Bot size={28} className="text-accent" />
          <h2 className="text-lg font-semibold text-heading">{t('aiHub.onboarding.step1.title')}</h2>
        </div>
        <p className="text-sm text-muted">{t('aiHub.onboarding.step1.description')}</p>
        <div className="space-y-2 mt-4">
          <p className="text-xs font-medium text-muted uppercase tracking-wide">{t('aiHub.onboarding.step1.personaPrompt')}</p>
          <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
            {PERSONAS.map((p) => (
              <button
                key={p}
                onClick={() => setSelectedPersona(p)}
                className={`px-3 py-2 rounded-lg text-sm font-medium border transition-colors ${
                  selectedPersona === p
                    ? 'border-accent bg-accent/10 text-accent'
                    : 'border-edge bg-elevated text-body hover:border-accent/60'
                }`}
              >
                {t(`aiHub.onboarding.personas.${p}`)}
              </button>
            ))}
          </div>
        </div>
      </div>
    ),
    2: (
      <div className="space-y-4">
        <div className="flex items-center gap-3 mb-2">
          <Sparkles size={28} className="text-accent" />
          <h2 className="text-lg font-semibold text-heading">{t('aiHub.onboarding.step2.title')}</h2>
        </div>
        <p className="text-sm text-muted">{t('aiHub.onboarding.step2.description')}</p>
        <div className="mt-4 p-3 bg-elevated border border-edge rounded-lg">
          <p className="text-xs text-muted mb-1">{t('aiHub.onboarding.step2.sampleLabel')}</p>
          <p className="text-sm text-accent italic">{t('aiHub.onboarding.step2.samplePrompt')}</p>
        </div>
      </div>
    ),
    3: (
      <div className="space-y-4">
        <div className="flex items-center gap-3 mb-2">
          <Users size={28} className="text-accent" />
          <h2 className="text-lg font-semibold text-heading">{t('aiHub.onboarding.step3.title')}</h2>
        </div>
        <p className="text-sm text-muted">{t('aiHub.onboarding.step3.description')}</p>
        <ul className="mt-3 space-y-2">
          {['contractAgent', 'incidentAgent', 'testAgent'].map((key) => (
            <li key={key} className="flex items-center gap-2 text-sm text-body">
              <Bot size={14} className="text-accent flex-shrink-0" />
              {t(`aiHub.onboarding.step3.agents.${key}`)}
            </li>
          ))}
        </ul>
      </div>
    ),
    4: (
      <div className="space-y-4 text-center">
        <CheckCircle2 size={48} className="text-success mx-auto" />
        <h2 className="text-lg font-semibold text-heading">{t('aiHub.onboarding.step4.title')}</h2>
        <p className="text-sm text-muted">{t('aiHub.onboarding.step4.description')}</p>
      </div>
    ),
  };

  const footer = (
    <div className="flex items-center justify-between w-full">
      <div className="flex gap-1">
        {Array.from({ length: TOTAL_STEPS }, (_, i) => (
          <span
            key={i}
            className={`w-2 h-2 rounded-full transition-colors ${i + 1 === step ? 'bg-accent' : 'bg-edge'}`}
          />
        ))}
      </div>
      <div className="flex gap-2">
        {step > 1 && (
          <Button variant="secondary" size="sm" onClick={() => setStep(s => s - 1)}>
            <ChevronLeft size={14} />
            {t('aiHub.onboarding.back')}
          </Button>
        )}
        {step < TOTAL_STEPS ? (
          <Button
            variant="primary"
            size="sm"
            onClick={() => setStep(s => s + 1)}
            disabled={step === 1 && selectedPersona === null}
          >
            {t('aiHub.onboarding.next')}
            <ChevronRight size={14} />
          </Button>
        ) : (
          <Button variant="primary" size="sm" onClick={() => void handleFinish()} loading={isFinishing}>
            {t('aiHub.onboarding.finish')}
          </Button>
        )}
      </div>
    </div>
  );

  return (
    <Modal
      open={open}
      onClose={handleClose}
      size="md"
      footer={footer}
    >
      {stepContent[step] ?? null}
    </Modal>
  );
}
