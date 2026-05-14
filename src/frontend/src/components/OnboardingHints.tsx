import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Layers,
  GitBranch,
  Activity,
  FileText,
  PenTool,
  GitCompare,
  Rocket,
  Target,
  ShieldCheck,
  AlertTriangle,
  BookOpen,
  BarChart3,
  X,
  Bot,
  Database,
  Plug,
  Shield,
  ClipboardList,
  Globe,
  Scale,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

interface OnboardingHint {
  id: string;
  icon: LucideIcon;
  titleKey: string;
  descriptionKey: string;
  actionRoute?: string;
  actionLabelKey?: string;
}

interface OnboardingHintsProps {
  module: string;
}

const STORAGE_PREFIX = 'nex:onboarding:dismissed:';

/**
 * Definição de hints por módulo.
 * Cada módulo apresenta até 3 dicas contextuais para orientar o utilizador.
 */
const hintsByModule: Record<string, OnboardingHint[]> = {
  services: [
    {
      id: 'services-catalog',
      icon: Layers,
      titleKey: 'onboarding.services.hint1Title',
      descriptionKey: 'onboarding.services.hint1Desc',
      actionRoute: '/services',
      actionLabelKey: 'common.viewAll',
    },
    {
      id: 'services-graph',
      icon: GitBranch,
      titleKey: 'onboarding.services.hint2Title',
      descriptionKey: 'onboarding.services.hint2Desc',
      actionRoute: '/services/graph',
      actionLabelKey: 'common.view',
    },
    {
      id: 'services-health',
      icon: Activity,
      titleKey: 'onboarding.services.hint3Title',
      descriptionKey: 'onboarding.services.hint3Desc',
    },
  ],
  contracts: [
    {
      id: 'contracts-browse',
      icon: FileText,
      titleKey: 'onboarding.contracts.hint1Title',
      descriptionKey: 'onboarding.contracts.hint1Desc',
      actionRoute: '/contracts',
      actionLabelKey: 'common.viewAll',
    },
    {
      id: 'contracts-studio',
      icon: PenTool,
      titleKey: 'onboarding.contracts.hint2Title',
      descriptionKey: 'onboarding.contracts.hint2Desc',
      actionRoute: '/contracts/studio',
      actionLabelKey: 'common.open',
    },
    {
      id: 'contracts-versions',
      icon: GitCompare,
      titleKey: 'onboarding.contracts.hint3Title',
      descriptionKey: 'onboarding.contracts.hint3Desc',
    },
  ],
  changes: [
    {
      id: 'changes-track',
      icon: Rocket,
      titleKey: 'onboarding.changes.hint1Title',
      descriptionKey: 'onboarding.changes.hint1Desc',
    },
    {
      id: 'changes-blast',
      icon: Target,
      titleKey: 'onboarding.changes.hint2Title',
      descriptionKey: 'onboarding.changes.hint2Desc',
    },
    {
      id: 'changes-confidence',
      icon: ShieldCheck,
      titleKey: 'onboarding.changes.hint3Title',
      descriptionKey: 'onboarding.changes.hint3Desc',
    },
  ],
  operations: [
    {
      id: 'operations-incidents',
      icon: AlertTriangle,
      titleKey: 'onboarding.operations.hint1Title',
      descriptionKey: 'onboarding.operations.hint1Desc',
      actionRoute: '/operations/incidents',
      actionLabelKey: 'common.view',
    },
    {
      id: 'operations-runbooks',
      icon: BookOpen,
      titleKey: 'onboarding.operations.hint2Title',
      descriptionKey: 'onboarding.operations.hint2Desc',
      actionRoute: '/operations/runbooks',
      actionLabelKey: 'common.view',
    },
    {
      id: 'operations-reliability',
      icon: BarChart3,
      titleKey: 'onboarding.operations.hint3Title',
      descriptionKey: 'onboarding.operations.hint3Desc',
    },
  ],
  aiHub: [
    {
      id: 'aihub-assistant',
      icon: Bot,
      titleKey: 'onboarding.aiHub.hint1Title',
      descriptionKey: 'onboarding.aiHub.hint1Desc',
      actionRoute: '/ai/assistant',
      actionLabelKey: 'common.open',
    },
    {
      id: 'aihub-models',
      icon: Database,
      titleKey: 'onboarding.aiHub.hint2Title',
      descriptionKey: 'onboarding.aiHub.hint2Desc',
      actionRoute: '/ai/models',
      actionLabelKey: 'common.view',
    },
    {
      id: 'aihub-ide',
      icon: Plug,
      titleKey: 'onboarding.aiHub.hint3Title',
      descriptionKey: 'onboarding.aiHub.hint3Desc',
      actionRoute: '/ai/ide',
      actionLabelKey: 'common.explore',
    },
  ],
  governance: [
    {
      id: 'governance-reports',
      icon: BarChart3,
      titleKey: 'onboarding.governance.hint1Title',
      descriptionKey: 'onboarding.governance.hint1Desc',
      actionRoute: '/governance/reports',
      actionLabelKey: 'common.view',
    },
    {
      id: 'governance-risk',
      icon: Shield,
      titleKey: 'onboarding.governance.hint2Title',
      descriptionKey: 'onboarding.governance.hint2Desc',
      actionRoute: '/governance/risk',
      actionLabelKey: 'common.view',
    },
    {
      id: 'governance-compliance',
      icon: Scale,
      titleKey: 'onboarding.governance.hint3Title',
      descriptionKey: 'onboarding.governance.hint3Desc',
      actionRoute: '/governance/compliance',
      actionLabelKey: 'common.view',
    },
  ],
  knowledge: [
    {
      id: 'knowledge-sot',
      icon: Globe,
      titleKey: 'onboarding.knowledge.hint1Title',
      descriptionKey: 'onboarding.knowledge.hint1Desc',
      actionRoute: '/source-of-truth',
      actionLabelKey: 'common.explore',
    },
    {
      id: 'knowledge-audit',
      icon: ClipboardList,
      titleKey: 'onboarding.knowledge.hint2Title',
      descriptionKey: 'onboarding.knowledge.hint2Desc',
      actionRoute: '/audit',
      actionLabelKey: 'common.view',
    },
  ],
};

function getDismissedIds(): Set<string> {
  try {
    const raw = sessionStorage.getItem(`${STORAGE_PREFIX}ids`);
    return raw ? new Set<string>(JSON.parse(raw) as string[]) : new Set<string>();
  } catch {
    return new Set<string>();
  }
}

function persistDismissedId(id: string): void {
  const dismissed = getDismissedIds();
  dismissed.add(id);
  sessionStorage.setItem(`${STORAGE_PREFIX}ids`, JSON.stringify([...dismissed]));
}

/**
 * Componente de dicas contextuais por módulo.
 * Exibe até 3 hints sobre o que o utilizador pode fazer na página atual.
 * As dicas podem ser descartadas (estado guardado em sessionStorage).
 */
export function OnboardingHints({ module }: OnboardingHintsProps) {
  const { t } = useTranslation();
  const [dismissed, setDismissed] = useState<Set<string>>(() => getDismissedIds());

  useEffect(() => {
     
    setDismissed(getDismissedIds());
  }, [module]);

  const allHints = hintsByModule[module] ?? [];
  const visibleHints = allHints.filter((h) => !dismissed.has(h.id)).slice(0, 3);

  if (visibleHints.length === 0) return null;

  const handleDismiss = (id: string) => {
    persistDismissedId(id);
    setDismissed((prev) => new Set(prev).add(id));
  };

  return (
    <section aria-label={t('onboarding.hintsTitle')} className="px-6 pt-4">
      <h2 className="text-sm font-medium text-heading mb-3">
        {t('onboarding.hintsTitle')}
      </h2>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {visibleHints.map((hint) => {
          const Icon = hint.icon;
          return (
            <div
              key={hint.id}
              className="bg-elevated/50 border border-edge/50 rounded-lg p-4 flex flex-col gap-2 relative group"
            >
              <button
                type="button"
                onClick={() => handleDismiss(hint.id)}
                className="absolute top-2 right-2 text-muted hover:text-heading opacity-0 group-hover:opacity-100 transition-opacity"
                aria-label={t('onboarding.dismiss')}
              >
                <X className="h-3.5 w-3.5" />
              </button>

              <div className="flex items-center gap-2">
                <Icon className="h-4 w-4 text-accent" aria-hidden="true" />
                <span className="text-sm font-medium text-heading">
                  {t(hint.titleKey)}
                </span>
              </div>

              <p className="text-xs text-muted leading-relaxed">
                {t(hint.descriptionKey)}
              </p>

              {hint.actionRoute && hint.actionLabelKey && (
                <Link
                  to={hint.actionRoute}
                  className="text-xs text-accent hover:text-accent/80 transition-colors mt-auto self-start"
                >
                  {t(hint.actionLabelKey)} →
                </Link>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}
