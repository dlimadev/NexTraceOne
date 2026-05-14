import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  Circle,
  ArrowRight,
  SkipForward,
  Loader2,
  Rocket,
  Signal,
  Layers,
  FileText,
  Gauge,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { apiClient } from '@/lib/api-client';
import { useToast } from '@/hooks/use-toast';

interface OnboardingStep {
  id: string;
  name: string;
  titleKey: string;
  descriptionKey: string;
  icon: LucideIcon;
  route?: string;
}

interface OnboardingStatus {
  progressId: string | null;
  currentStep: string;
  completedSteps: string[];
  isCompleted: boolean;
  isSkipped: boolean;
  completedAt: string | null;
}

const STEPS: OnboardingStep[] = [
  {
    id: 'Install',
    name: 'Install',
    titleKey: 'onboarding.steps.install.title',
    descriptionKey: 'onboarding.steps.install.description',
    icon: Rocket,
    route: '/services/register',
  },
  {
    id: 'FirstSignal',
    name: 'FirstSignal',
    titleKey: 'onboarding.steps.firstSignal.title',
    descriptionKey: 'onboarding.steps.firstSignal.description',
    icon: Signal,
    route: '/catalog',
  },
  {
    id: 'RegisterService',
    name: 'RegisterService',
    titleKey: 'onboarding.steps.registerService.title',
    descriptionKey: 'onboarding.steps.registerService.description',
    icon: Layers,
    route: '/services',
  },
  {
    id: 'AddContract',
    name: 'AddContract',
    titleKey: 'onboarding.steps.addContract.title',
    descriptionKey: 'onboarding.steps.addContract.description',
    icon: FileText,
    route: '/contracts/studio',
  },
  {
    id: 'SetupSlo',
    name: 'SetupSlo',
    titleKey: 'onboarding.steps.setupSlo.title',
    descriptionKey: 'onboarding.steps.setupSlo.description',
    icon: Gauge,
    route: '/slos',
  },
];

export function OnboardingWizardPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { toast } = useToast();
  const [status, setStatus] = useState<OnboardingStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [completingStep, setCompletingStep] = useState<string | null>(null);

  useEffect(() => {
    fetchStatus();
  }, []);

  const fetchStatus = async () => {
    try {
      const response = await apiClient.get('/onboarding/status');
      setStatus(response.data);
      
      // Se já está completo ou ignorado, redirecionar para dashboard
      if (response.data.isCompleted || response.data.isSkipped) {
        navigate('/');
      }
    } catch {
      // Erro tratado via toast - logging estruturado deve ser feito pelo backend
      toast({
        title: t('common.error'),
        description: t('onboarding.errors.fetchFailed'),
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCompleteStep = async (stepId: string) => {
    setCompletingStep(stepId);
    try {
      const response = await apiClient.post(`/onboarding/steps/${stepId}`);
      
      if (response.data.advanced) {
        toast({
          title: t('common.success'),
          description: t('onboarding.messages.stepCompleted', { step: stepId }),
        });
        
        // Refetch status to update UI
        await fetchStatus();
        
        // Se completou todos os passos, redirecionar
        if (response.data.isCompleted) {
          setTimeout(() => navigate('/'), 1500);
        }
      }
    } catch {
      // Erro tratado via toast - logging estruturado deve ser feito pelo backend
      toast({
        title: t('common.error'),
        description: t('onboarding.errors.stepFailed'),
        variant: 'destructive',
      });
    } finally {
      setCompletingStep(null);
    }
  };

  const handleSkipAll = async () => {
    try {
      // Marcar todos os passos como completados de uma vez
      for (const step of STEPS) {
        await apiClient.post(`/onboarding/steps/${step.id}`);
      }
      
      toast({
        title: t('common.success'),
        description: t('onboarding.messages.wizardSkipped'),
      });
      
      navigate('/');
    } catch {
      // Erro tratado via toast - logging estruturado deve ser feito pelo backend
      toast({
        title: t('common.error'),
        description: t('onboarding.errors.skipFailed'),
        variant: 'destructive',
      });
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!status) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Alert>
          <AlertTitle>{t('common.error')}</AlertTitle>
          <AlertDescription>{t('onboarding.errors.noStatus')}</AlertDescription>
        </Alert>
      </div>
    );
  }

  const currentStepIndex = STEPS.findIndex((s) => s.id === status.currentStep);
  const progressPercentage = ((currentStepIndex + 1) / STEPS.length) * 100;

  return (
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      <div className="mb-8">
        <h1 className="text-3xl font-bold">{t('onboarding.wizard.title')}</h1>
        <p className="text-muted-foreground mt-2">
          {t('onboarding.wizard.subtitle')}
        </p>
      </div>

      {/* Progress Bar */}
      <Card className="mb-6">
        <CardContent className="pt-6">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium">
              {t('onboarding.progress.label')}
            </span>
            <span className="text-sm text-muted-foreground">
              {Math.round(progressPercentage)}%
            </span>
          </div>
          <Progress value={progressPercentage} className="h-2" />
        </CardContent>
      </Card>

      {/* Steps */}
      <div className="space-y-4">
        {STEPS.map((step, index) => {
          const isCompleted = status.completedSteps.includes(step.id);
          const isCurrent = step.id === status.currentStep;
          const Icon = step.icon;

          return (
            <Card
              key={step.id}
              className={`transition-all ${
                isCurrent ? 'border-primary shadow-md' : ''
              } ${isCompleted ? 'bg-muted/50' : ''}`}
            >
              <CardHeader>
                <div className="flex items-start gap-4">
                  <div
                    className={`flex-shrink-0 w-12 h-12 rounded-full flex items-center justify-center ${
                      isCompleted
                        ? 'bg-green-500 text-white'
                        : isCurrent
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-muted text-muted-foreground'
                    }`}
                  >
                    {isCompleted ? (
                      <CheckCircle2 className="h-6 w-6" />
                    ) : (
                      <Icon className="h-6 w-6" />
                    )}
                  </div>
                  <div className="flex-1">
                    <CardTitle className="flex items-center gap-2">
                      {t(step.titleKey)}
                      {isCurrent && (
                        <span className="text-xs bg-primary text-primary-foreground px-2 py-1 rounded">
                          {t('onboarding.step.current')}
                        </span>
                      )}
                    </CardTitle>
                    <CardDescription className="mt-1">
                      {t(step.descriptionKey)}
                    </CardDescription>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex items-center gap-2">
                  {step.route && !isCompleted && (
                    <Button
                      variant="outline"
                      onClick={() => navigate(step.route!)}
                    >
                      {t('onboarding.actions.goToStep')}
                      <ArrowRight className="ml-2 h-4 w-4" />
                    </Button>
                  )}
                  {!isCompleted && (
                    <Button
                      onClick={() => handleCompleteStep(step.id)}
                      disabled={completingStep === step.id}
                    >
                      {completingStep === step.id ? (
                        <>
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                          {t('common.loading')}
                        </>
                      ) : (
                        <>
                          <CheckCircle2 className="mr-2 h-4 w-4" />
                          {t('onboarding.actions.markComplete')}
                        </>
                      )}
                    </Button>
                  )}
                  {isCompleted && (
                    <span className="text-sm text-green-600 font-medium">
                      {t('onboarding.step.completed')}
                    </span>
                  )}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      {/* Skip All Button */}
      <div className="mt-6 flex justify-end">
        <Button
          variant="ghost"
          onClick={handleSkipAll}
          disabled={completingStep !== null}
        >
          <SkipForward className="mr-2 h-4 w-4" />
          {t('onboarding.actions.skipAll')}
        </Button>
      </div>
    </div>
  );
}
