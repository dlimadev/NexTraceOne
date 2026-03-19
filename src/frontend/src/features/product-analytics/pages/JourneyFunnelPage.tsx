import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  ArrowRight,
  Clock,
  AlertTriangle,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';

/**
 * Página de jornadas e funis do produto.
 *
 * Mostra jornadas-chave com steps, taxas de conclusão, pontos de abandono
 * e tempo médio. Permite identificar onde os utilizadores não completam fluxos importantes.
 *
 * @see docs/PRODUCT-VISION.md — jornadas de valor do produto
 */

/* ── Dados de demonstração (MVP) ── */

const mockJourneys = [
  {
    journeyId: 'search_to_entity',
    journeyName: 'Search to Entity View',
    completionRate: 61.8,
    avgDurationMinutes: 4.2,
    biggestDropOff: 'results_displayed → result_clicked',
    steps: [
      { stepId: 'search_executed', stepName: 'Search Executed', completionPercent: 100.0 },
      { stepId: 'results_displayed', stepName: 'Results Displayed', completionPercent: 87.2 },
      { stepId: 'result_clicked', stepName: 'Result Clicked', completionPercent: 64.5 },
      { stepId: 'entity_viewed', stepName: 'Entity Viewed', completionPercent: 61.8 },
    ],
  },
  {
    journeyId: 'ai_prompt_to_action',
    journeyName: 'AI Prompt to Useful Action',
    completionRate: 48.6,
    avgDurationMinutes: 6.8,
    biggestDropOff: 'response_received → response_used',
    steps: [
      { stepId: 'assistant_opened', stepName: 'Assistant Opened', completionPercent: 100.0 },
      { stepId: 'prompt_submitted', stepName: 'Prompt Submitted', completionPercent: 82.4 },
      { stepId: 'response_received', stepName: 'Response Received', completionPercent: 80.1 },
      { stepId: 'response_used', stepName: 'Response Used', completionPercent: 48.6 },
    ],
  },
  {
    journeyId: 'contract_draft_to_publish',
    journeyName: 'Contract Draft to Publication',
    completionRate: 34.8,
    avgDurationMinutes: 48.5,
    biggestDropOff: 'draft_validated → review_submitted',
    steps: [
      { stepId: 'studio_opened', stepName: 'Studio Opened', completionPercent: 100.0 },
      { stepId: 'draft_created', stepName: 'Draft Created', completionPercent: 72.3 },
      { stepId: 'draft_validated', stepName: 'Draft Validated', completionPercent: 58.1 },
      { stepId: 'review_submitted', stepName: 'Review Submitted', completionPercent: 41.2 },
      { stepId: 'contract_published', stepName: 'Contract Published', completionPercent: 34.8 },
    ],
  },
  {
    journeyId: 'incident_to_mitigation',
    journeyName: 'Incident to Mitigation Completion',
    completionRate: 42.3,
    avgDurationMinutes: 125.0,
    biggestDropOff: 'cause_identified → mitigation_started',
    steps: [
      { stepId: 'incident_opened', stepName: 'Incident Opened', completionPercent: 100.0 },
      { stepId: 'investigation_started', stepName: 'Investigation Started', completionPercent: 91.2 },
      { stepId: 'cause_identified', stepName: 'Cause Identified', completionPercent: 68.4 },
      { stepId: 'mitigation_started', stepName: 'Mitigation Started', completionPercent: 55.7 },
      { stepId: 'mitigation_completed', stepName: 'Mitigation Completed', completionPercent: 42.3 },
    ],
  },
  {
    journeyId: 'onboarding_to_first_action',
    journeyName: 'Onboarding to First Meaningful Action',
    completionRate: 62.4,
    avgDurationMinutes: 18.5,
    biggestDropOff: 'first_search → first_meaningful_action',
    steps: [
      { stepId: 'first_login', stepName: 'First Login', completionPercent: 100.0 },
      { stepId: 'persona_selected', stepName: 'Persona Selected', completionPercent: 94.5 },
      { stepId: 'dashboard_viewed', stepName: 'Dashboard Viewed', completionPercent: 92.1 },
      { stepId: 'first_search', stepName: 'First Search', completionPercent: 78.3 },
      { stepId: 'first_meaningful_action', stepName: 'First Meaningful Action', completionPercent: 62.4 },
    ],
  },
];

function completionColor(rate: number): string {
  if (rate >= 60) return 'text-emerald-400';
  if (rate >= 40) return 'text-accent';
  if (rate >= 25) return 'text-amber-400';
  return 'text-red-400';
}

function barColor(percent: number): string {
  if (percent >= 80) return 'bg-emerald-500';
  if (percent >= 50) return 'bg-accent';
  if (percent >= 30) return 'bg-amber-500';
  return 'bg-red-500';
}

export function JourneyFunnelPage() {
  const { t } = useTranslation();
  const [selectedJourney, setSelectedJourney] = useState<string | null>(null);

  const journeys = selectedJourney
    ? mockJourneys.filter((j) => j.journeyId === selectedJourney)
    : mockJourneys;

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-white">{t('analytics.journey.title')}</h1>
        <p className="text-zinc-400 mt-1">{t('analytics.journey.subtitle')}</p>
      </div>

      {/* Journey filter */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          onClick={() => setSelectedJourney(null)}
          className={`px-3 py-1.5 rounded-lg text-sm transition ${!selectedJourney ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-zinc-800 text-zinc-400 border border-zinc-700 hover:border-zinc-600'}`}
        >
          {t('analytics.journey.all')}
        </button>
        {mockJourneys.map((j) => (
          <button
            key={j.journeyId}
            onClick={() => setSelectedJourney(j.journeyId)}
            className={`px-3 py-1.5 rounded-lg text-sm transition ${selectedJourney === j.journeyId ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-zinc-800 text-zinc-400 border border-zinc-700 hover:border-zinc-600'}`}
          >
            {j.journeyName}
          </button>
        ))}
      </div>

      {/* Journey cards */}
      <div className="space-y-6">
        {journeys.map((j) => (
          <Card key={j.journeyId}>
            <CardHeader>
              <div className="flex items-center justify-between">
                <span className="font-semibold text-white">{j.journeyName}</span>
                <div className="flex items-center gap-4 text-sm">
                  <span className={`font-medium ${completionColor(j.completionRate)}`}>
                    {j.completionRate}% {t('analytics.journey.completion')}
                  </span>
                  <span className="text-zinc-400 flex items-center gap-1">
                    <Clock size={14} />
                    {j.avgDurationMinutes} {t('analytics.minutes')}
                  </span>
                </div>
              </div>
            </CardHeader>
            <CardBody>
              {/* Funnel visualization */}
              <div className="space-y-3 mb-4">
                {j.steps.map((step, idx) => {
                  const previousStep = idx > 0 ? j.steps[idx - 1] : undefined;
                  const prevPercent = previousStep?.completionPercent ?? 100;
                  const dropOff = prevPercent - step.completionPercent;

                  return (
                    <div key={step.stepId}>
                      <div className="flex items-center justify-between mb-1">
                        <div className="flex items-center gap-2">
                          {idx > 0 && <ArrowRight size={12} className="text-zinc-600" />}
                          <span className="text-sm text-zinc-300">{step.stepName}</span>
                        </div>
                        <div className="flex items-center gap-3 text-sm">
                          <span className="text-white font-medium">{step.completionPercent}%</span>
                          {dropOff > 5 && (
                            <span className="text-red-400 text-xs">-{dropOff.toFixed(1)}%</span>
                          )}
                        </div>
                      </div>
                      <div className="w-full h-2 rounded-full bg-zinc-800 overflow-hidden">
                        <div
                          className={`h-full rounded-full ${barColor(step.completionPercent)} transition-all`}
                          style={{ width: `${step.completionPercent}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Drop-off insight */}
              <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-amber-500/10 border border-amber-500/20">
                <AlertTriangle size={14} className="text-amber-400 flex-shrink-0" />
                <span className="text-sm text-amber-300">
                  {t('analytics.journey.biggestDropOff')}: {j.biggestDropOff}
                </span>
              </div>
            </CardBody>
          </Card>
        ))}
      </div>
    </PageContainer>
  );
}
