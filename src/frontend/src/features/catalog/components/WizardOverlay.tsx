import { useEffect } from 'react';
import type { ReactNode } from 'react';
import { X, Check } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import type { LucideIcon } from 'lucide-react';
import { Button } from '../../../components/Button';

export interface WizardStep {
  id: string;
  /** i18n key */
  labelKey: string;
  icon: LucideIcon;
}

export interface WizardOverlayProps {
  title: string;
  headerIcon: ReactNode;
  steps: WizardStep[];
  currentStep: number; // 1-based
  onClose: () => void;
  onBack: () => void;
  onNext: () => void;
  onSubmit: () => void;
  isSubmitting?: boolean;
  isNextDisabled?: boolean;
  isLastStep: boolean;
  children: ReactNode;
}

/**
 * Overlay full-screen reutilizável para wizards multi-step.
 * Layout: backdrop + painel centrado com stepper lateral 200px.
 */
export function WizardOverlay({
  title,
  headerIcon,
  steps,
  currentStep,
  onClose,
  onBack,
  onNext,
  onSubmit,
  isSubmitting = false,
  isNextDisabled = false,
  isLastStep,
  children,
}: WizardOverlayProps) {
  const { t } = useTranslation();

  // Fecha com Escape + trava scroll do body
  useEffect(() => {
    document.body.style.overflow = 'hidden';
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = '';
    };
  }, [onClose]);

  const stepLabel = steps[currentStep - 1]
    ? t(steps[currentStep - 1].labelKey, steps[currentStep - 1].id)
    : '';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-black/60"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Panel */}
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="wizard-title"
        className="relative z-10 w-full max-w-3xl mx-4 bg-panel border border-edge rounded-md shadow-2xl flex flex-col max-h-[90vh] animate-fade-in"
      >

        {/* Header */}
        <div className="flex items-center gap-3 px-6 py-4 border-b border-edge shrink-0">
          <div className="text-accent">{headerIcon}</div>
          <div className="flex-1 min-w-0">
            <h2 id="wizard-title" className="text-base font-semibold text-heading truncate">{title}</h2>
            <p className="text-xs text-muted">
              {t('catalog.wizard.stepOf', { current: currentStep, total: steps.length })} — {stepLabel}
            </p>
          </div>
          <button
            onClick={onClose}
            aria-label={t('catalog.wizard.close')}
            className="text-muted hover:text-heading transition-colors p-1 rounded"
          >
            <X size={18} />
          </button>
        </div>

        {/* Body */}
        <div className="flex flex-1 min-h-0">
          {/* Stepper sidebar */}
          <div className="w-[200px] shrink-0 border-r border-edge bg-canvas/50 px-4 py-5 flex flex-col gap-1">
            {steps.map((step, idx) => {
              const stepNum = idx + 1;
              const isDone = stepNum < currentStep;
              const isActive = stepNum === currentStep;
              const StepIcon = step.icon;

              return (
                <div
                  key={step.id}
                  className={`flex items-center gap-2.5 px-3 py-2 rounded-md text-xs transition-colors ${
                    isActive
                      ? 'bg-accent/15 text-accent font-semibold'
                      : isDone
                        ? 'text-success'
                        : 'text-muted'
                  }`}
                >
                  <div
                    className={`w-5 h-5 rounded-full flex items-center justify-center shrink-0 text-[10px] border transition-colors ${
                      isActive
                        ? 'bg-accent border-accent text-white'
                        : isDone
                          ? 'bg-success/20 border-success text-success'
                          : 'border-edge text-muted'
                    }`}
                  >
                    {isDone ? <Check size={10} /> : <StepIcon size={10} />}
                  </div>
                  <span className="truncate">{t(step.labelKey, step.id)}</span>
                </div>
              );
            })}
          </div>

          {/* Content area */}
          <div className="flex-1 overflow-y-auto px-6 py-5 min-w-0">
            {children}
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between px-6 py-4 border-t border-edge shrink-0">
          <Button
            variant="secondary"
            onClick={onBack}
            disabled={currentStep === 1}
          >
            {t('catalog.wizard.back')}
          </Button>

          {isLastStep ? (
            <Button onClick={onSubmit} loading={isSubmitting} disabled={isSubmitting}>
              {isSubmitting ? t('catalog.wizard.submitting') : t('catalog.wizard.submit')}
            </Button>
          ) : (
            <Button onClick={onNext} disabled={isNextDisabled}>
              {t('catalog.wizard.next')}
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
