import type React from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, ArrowRight, Check, X } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { Button } from '../../../shared/ui';
import { cn } from '../../../lib/cn';

export type OnboardStep = 'identity' | 'interface' | 'contract' | 'review';
export interface OnboardStepMeta { id: OnboardStep; label: string; optional?: boolean; }

interface OnboardWizardShellProps {
  title: string;
  steps: OnboardStepMeta[];
  activeStep: OnboardStep;
  preview: React.ReactNode;
  children: React.ReactNode;
  canGoNext: boolean;
  isFirstStep: boolean;
  isLastStep: boolean;
  canSkip: boolean;
  pending: boolean;
  onBack: () => void;
  onNext: () => void;
  onSkip: () => void;
  onCancel: () => void;
}

/** Shell presentacional do wizard de onboarding: rail de progresso + preview + conteúdo + footer. */
export function OnboardWizardShell({
  title, steps, activeStep, preview, children,
  canGoNext, isFirstStep, isLastStep, canSkip, pending,
  onBack, onNext, onSkip, onCancel,
}: OnboardWizardShellProps) {
  const { t } = useTranslation();
  const activeIndex = steps.findIndex((s) => s.id === activeStep);

  return (
    <PageContainer className="animate-fade-in">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-lg font-bold text-heading">{title}</h1>
        <Button variant="ghost" size="sm" icon={<X size={14} />} onClick={onCancel}>
          {t('common.cancel', 'Cancel')}
        </Button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[300px_minmax(0,1fr)] gap-6 items-start">
        <div className="lg:sticky lg:top-4 space-y-4">
          {preview}
          <ol className="rounded-2xl border border-edge bg-card p-3 space-y-1">
            {steps.map((s, idx) => (
              <li key={s.id} className={cn(
                'flex items-center gap-2 rounded-lg px-2.5 py-2 text-sm',
                s.id === activeStep ? 'bg-accent/10 text-heading' : 'text-muted',
              )}>
                <span className={cn(
                  'w-5 h-5 rounded-full text-[11px] flex items-center justify-center font-bold shrink-0',
                  idx < activeIndex ? 'bg-success text-white'
                    : s.id === activeStep ? 'bg-accent text-on-accent' : 'bg-elevated text-muted',
                )}>
                  {idx < activeIndex ? <Check size={12} /> : idx + 1}
                </span>
                <span className="truncate">{s.label}</span>
                {s.optional && (
                  <span className="ml-auto text-[10px] text-muted">{t('onboard.optional', 'Optional')}</span>
                )}
              </li>
            ))}
          </ol>
        </div>

        <div className="min-w-0">
          <div className="bg-card border border-edge rounded-xl p-5">
            {children}
            <div className="flex items-center justify-between pt-4 mt-4 border-t border-edge">
              <Button variant="ghost" size="sm" icon={<ArrowLeft size={14} />} onClick={onBack} disabled={isFirstStep}>
                {t('common.back', 'Back')}
              </Button>
              <div className="flex items-center gap-2">
                {canSkip && (
                  <Button variant="ghost" size="sm" onClick={onSkip}>
                    {t('onboard.skip', 'Skip this step')}
                  </Button>
                )}
                <Button
                  variant="primary"
                  size="sm"
                  onClick={onNext}
                  disabled={!canGoNext}
                  loading={pending}
                  icon={isLastStep ? <Check size={14} /> : undefined}
                >
                  {isLastStep ? t('onboard.finish', 'Finish') : t('common.next', 'Next')}
                  {!isLastStep && <ArrowRight size={14} />}
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </PageContainer>
  );
}
