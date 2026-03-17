import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Users,
  CheckCircle,
  AlertCircle,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { PageContainer } from '../../../components/shell';

/**
 * Página de uso por persona.
 *
 * Mostra perfil de uso para cada persona oficial do NexTraceOne:
 * módulos usados, ações principais, profundidade de adoção,
 * pontos de fricção comuns e marcos de valor atingidos.
 *
 * @see docs/PERSONA-MATRIX.md — definição oficial das personas
 */

/* ── Dados de demonstração (MVP) ── */

const mockPersonas = [
  {
    persona: 'Engineer',
    activeUsers: 98,
    totalActions: 4520,
    adoptionDepth: 85.2,
    topModules: [
      { module: 'Source of Truth', adoptionPercent: 92, actionCount: 1240 },
      { module: 'Contract Studio', adoptionPercent: 84, actionCount: 980 },
      { module: 'AI Assistant', adoptionPercent: 78, actionCount: 720 },
      { module: 'Search', adoptionPercent: 96, actionCount: 1580 },
    ],
    topActions: ['search_executed', 'entity_viewed', 'contract_draft_created', 'assistant_prompt_submitted'],
    frictionPoints: ['zero_result_search', 'empty_state_contracts'],
    milestones: ['FirstSearchSuccess', 'FirstContractDraftCreated', 'FirstAiUsefulInteraction'],
  },
  {
    persona: 'TechLead',
    activeUsers: 72,
    totalActions: 2840,
    adoptionDepth: 78.4,
    topModules: [
      { module: 'Change Intelligence', adoptionPercent: 88, actionCount: 680 },
      { module: 'Reliability', adoptionPercent: 82, actionCount: 520 },
      { module: 'Incidents', adoptionPercent: 76, actionCount: 440 },
      { module: 'Source of Truth', adoptionPercent: 70, actionCount: 380 },
    ],
    topActions: ['change_viewed', 'reliability_dashboard_viewed', 'incident_investigated', 'source_of_truth_queried'],
    frictionPoints: ['navigation_loop_reliability', 'aborted_journey_mitigation'],
    milestones: ['FirstIncidentInvestigation', 'FirstMitigationCompleted'],
  },
  {
    persona: 'Architect',
    activeUsers: 45,
    totalActions: 1620,
    adoptionDepth: 72.1,
    topModules: [
      { module: 'Contract Studio', adoptionPercent: 90, actionCount: 520 },
      { module: 'Source of Truth', adoptionPercent: 86, actionCount: 480 },
      { module: 'Governance', adoptionPercent: 62, actionCount: 220 },
      { module: 'Change Intelligence', adoptionPercent: 58, actionCount: 180 },
    ],
    topActions: ['contract_published', 'source_of_truth_queried', 'policy_viewed', 'change_viewed'],
    frictionPoints: ['empty_state_policies', 'late_discovery_evidence'],
    milestones: ['FirstContractPublished', 'FirstSourceOfTruthUsed'],
  },
  {
    persona: 'Product',
    activeUsers: 18,
    totalActions: 980,
    adoptionDepth: 68.5,
    topModules: [
      { module: 'Executive Views', adoptionPercent: 92, actionCount: 340 },
      { module: 'Governance', adoptionPercent: 78, actionCount: 280 },
      { module: 'FinOps', adoptionPercent: 65, actionCount: 180 },
      { module: 'Search', adoptionPercent: 88, actionCount: 180 },
    ],
    topActions: ['executive_overview_viewed', 'report_generated', 'search_executed'],
    frictionPoints: ['empty_state_reports'],
    milestones: ['FirstExecutiveOverviewConsumed', 'FirstReportGenerated'],
  },
  {
    persona: 'Executive',
    activeUsers: 12,
    totalActions: 420,
    adoptionDepth: 82.3,
    topModules: [
      { module: 'Executive Views', adoptionPercent: 96, actionCount: 280 },
      { module: 'FinOps', adoptionPercent: 42, actionCount: 80 },
      { module: 'Governance', adoptionPercent: 38, actionCount: 60 },
    ],
    topActions: ['executive_overview_viewed', 'report_generated'],
    frictionPoints: ['navigation_loop_reports'],
    milestones: ['FirstExecutiveOverviewConsumed'],
  },
  {
    persona: 'PlatformAdmin',
    activeUsers: 8,
    totalActions: 580,
    adoptionDepth: 71.8,
    topModules: [
      { module: 'Admin', adoptionPercent: 94, actionCount: 280 },
      { module: 'Integration Hub', adoptionPercent: 86, actionCount: 180 },
      { module: 'Governance', adoptionPercent: 72, actionCount: 120 },
    ],
    topActions: ['policy_viewed', 'connector_configured', 'user_managed'],
    frictionPoints: ['quota_exceeded', 'blocked_by_policy'],
    milestones: ['FirstSourceOfTruthUsed'],
  },
  {
    persona: 'Auditor',
    activeUsers: 6,
    totalActions: 320,
    adoptionDepth: 75.4,
    topModules: [
      { module: 'Governance', adoptionPercent: 92, actionCount: 180 },
      { module: 'Executive Views', adoptionPercent: 68, actionCount: 80 },
      { module: 'Source of Truth', adoptionPercent: 54, actionCount: 60 },
    ],
    topActions: ['evidence_package_exported', 'policy_viewed', 'compliance_check_viewed'],
    frictionPoints: ['empty_state_evidence'],
    milestones: ['FirstEvidenceExported'],
  },
];

function trendIcon(depth: number) {
  if (depth >= 80) return <TrendingUp size={14} className="text-emerald-400" />;
  if (depth >= 60) return <Minus size={14} className="text-zinc-400" />;
  return <TrendingDown size={14} className="text-amber-400" />;
}

function depthColor(depth: number): string {
  if (depth >= 80) return 'text-emerald-400';
  if (depth >= 60) return 'text-accent';
  if (depth >= 40) return 'text-amber-400';
  return 'text-red-400';
}

export function PersonaUsagePage() {
  const { t } = useTranslation();
  const [selectedPersona, setSelectedPersona] = useState<string | null>(null);

  const personas = selectedPersona
    ? mockPersonas.filter((p) => p.persona === selectedPersona)
    : mockPersonas;

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-white">{t('analytics.persona.title')}</h1>
        <p className="text-zinc-400 mt-1">{t('analytics.persona.subtitle')}</p>
      </div>

      {/* Persona filter */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button
          onClick={() => setSelectedPersona(null)}
          className={`px-3 py-1.5 rounded-lg text-sm transition ${!selectedPersona ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-zinc-800 text-zinc-400 border border-zinc-700 hover:border-zinc-600'}`}
        >
          {t('analytics.persona.all')}
        </button>
        {mockPersonas.map((p) => (
          <button
            key={p.persona}
            onClick={() => setSelectedPersona(p.persona)}
            className={`px-3 py-1.5 rounded-lg text-sm transition ${selectedPersona === p.persona ? 'bg-accent/20 text-accent border border-accent/40' : 'bg-zinc-800 text-zinc-400 border border-zinc-700 hover:border-zinc-600'}`}
          >
            {t(`analytics.persona.role.${p.persona}`)}
          </button>
        ))}
      </div>

      {/* Persona cards */}
      <div className="space-y-4">
        {personas.map((p) => (
          <Card key={p.persona}>
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Users size={18} className="text-accent" />
                  <span className="font-semibold text-white">{t(`analytics.persona.role.${p.persona}`)}</span>
                  {trendIcon(p.adoptionDepth)}
                </div>
                <div className="flex items-center gap-4 text-sm">
                  <span className="text-zinc-400">{p.activeUsers} {t('analytics.users')}</span>
                  <span className="text-zinc-400">{p.totalActions.toLocaleString()} {t('analytics.actions')}</span>
                  <span className={`font-medium ${depthColor(p.adoptionDepth)}`}>{p.adoptionDepth}% {t('analytics.persona.depth')}</span>
                </div>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Top modules */}
                <div>
                  <h4 className="text-xs text-zinc-500 uppercase tracking-wide mb-2">{t('analytics.persona.topModules')}</h4>
                  <div className="space-y-2">
                    {p.topModules.map((mod) => (
                      <div key={mod.module} className="flex items-center justify-between">
                        <span className="text-sm text-zinc-300">{mod.module}</span>
                        <div className="flex items-center gap-2">
                          <div className="w-16 h-1.5 rounded-full bg-zinc-800 overflow-hidden">
                            <div
                              className="h-full rounded-full bg-accent transition-all"
                              style={{ width: `${mod.adoptionPercent}%` }}
                            />
                          </div>
                          <span className="text-xs text-zinc-500">{mod.adoptionPercent}%</span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Milestones reached */}
                <div>
                  <h4 className="text-xs text-zinc-500 uppercase tracking-wide mb-2">{t('analytics.persona.milestonesReached')}</h4>
                  <div className="space-y-1">
                    {p.milestones.map((m) => (
                      <div key={m} className="flex items-center gap-2">
                        <CheckCircle size={14} className="text-emerald-400" />
                        <span className="text-sm text-zinc-300">{t(`analytics.milestone.${m}`)}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Friction points */}
                <div>
                  <h4 className="text-xs text-zinc-500 uppercase tracking-wide mb-2">{t('analytics.persona.frictionPoints')}</h4>
                  <div className="space-y-1">
                    {p.frictionPoints.map((f) => (
                      <div key={f} className="flex items-center gap-2">
                        <AlertCircle size={14} className="text-amber-400" />
                        <span className="text-sm text-zinc-300">{f.replace(/_/g, ' ')}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </CardBody>
          </Card>
        ))}
      </div>
    </PageContainer>
  );
}
