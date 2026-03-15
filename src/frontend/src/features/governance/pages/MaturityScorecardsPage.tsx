import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Award } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { MaturityScorecardsResponse, MaturityLevelType } from '../../../types';

type MaturityDimension = 'team' | 'domain';

/**
 * Dados simulados dos scorecards de maturidade — alinhados com o backend GetMaturityScorecards.
 * Em produção, virão da API /api/v1/governance/executive/maturity.
 */
const mockScorecards: MaturityScorecardsResponse = {
  dimension: 'team',
  scorecards: [
    {
      groupId: 'team-payment-squad',
      groupName: 'Payment Squad',
      overallMaturity: 'Managed',
      dimensions: [
        { dimension: 'ownership', level: 'Optimizing', score: 95, maxScore: 100, explanation: 'All services have defined owners and escalation paths' },
        { dimension: 'contract', level: 'Managed', score: 82, maxScore: 100, explanation: 'Versioned contracts with validation pipeline' },
        { dimension: 'documentation', level: 'Defined', score: 68, maxScore: 100, explanation: 'Technical docs present but operational runbooks incomplete' },
        { dimension: 'runbook', level: 'Developing', score: 45, maxScore: 100, explanation: 'Runbooks exist for critical paths only' },
        { dimension: 'dependencyMapping', level: 'Managed', score: 80, maxScore: 100, explanation: 'Dependencies mapped with topology visualization' },
        { dimension: 'changeValidation', level: 'Managed', score: 78, maxScore: 100, explanation: 'Pre-deploy validation in CI/CD pipeline' },
        { dimension: 'operationalReadiness', level: 'Defined', score: 65, maxScore: 100, explanation: 'Monitoring configured but alerting gaps remain' },
        { dimension: 'aiGovernance', level: 'Initial', score: 20, maxScore: 100, explanation: 'No AI governance policies defined' },
      ],
    },
    {
      groupId: 'team-order-squad',
      groupName: 'Order Squad',
      overallMaturity: 'Defined',
      dimensions: [
        { dimension: 'ownership', level: 'Managed', score: 85, maxScore: 100, explanation: 'Ownership defined for most services' },
        { dimension: 'contract', level: 'Developing', score: 50, maxScore: 100, explanation: 'Contracts exist but versioning inconsistent' },
        { dimension: 'documentation', level: 'Defined', score: 60, maxScore: 100, explanation: 'Basic documentation available for core services' },
        { dimension: 'runbook', level: 'Initial', score: 25, maxScore: 100, explanation: 'Minimal runbook coverage' },
        { dimension: 'dependencyMapping', level: 'Defined', score: 62, maxScore: 100, explanation: 'Core dependencies mapped, edge cases missing' },
        { dimension: 'changeValidation', level: 'Defined', score: 60, maxScore: 100, explanation: 'Basic validation checks in place' },
        { dimension: 'operationalReadiness', level: 'Developing', score: 48, maxScore: 100, explanation: 'Partial monitoring setup' },
        { dimension: 'aiGovernance', level: 'Initial', score: 15, maxScore: 100, explanation: 'AI tools used ad-hoc without governance' },
      ],
    },
    {
      groupId: 'team-platform-squad',
      groupName: 'Platform Squad',
      overallMaturity: 'Developing',
      dimensions: [
        { dimension: 'ownership', level: 'Defined', score: 70, maxScore: 100, explanation: 'Shared services ownership partially defined' },
        { dimension: 'contract', level: 'Defined', score: 65, maxScore: 100, explanation: 'Internal contracts documented but not validated' },
        { dimension: 'documentation', level: 'Developing', score: 40, maxScore: 100, explanation: 'Documentation sparse for shared infrastructure' },
        { dimension: 'runbook', level: 'Developing', score: 35, maxScore: 100, explanation: 'Runbooks in progress for critical systems' },
        { dimension: 'dependencyMapping', level: 'Managed', score: 75, maxScore: 100, explanation: 'Platform dependencies well mapped' },
        { dimension: 'changeValidation', level: 'Managed', score: 80, maxScore: 100, explanation: 'Robust validation for platform changes' },
        { dimension: 'operationalReadiness', level: 'Defined', score: 60, maxScore: 100, explanation: 'Good monitoring but alert fatigue issues' },
        { dimension: 'aiGovernance', level: 'Developing', score: 35, maxScore: 100, explanation: 'Basic AI usage policies being drafted' },
      ],
    },
    {
      groupId: 'team-identity-squad',
      groupName: 'Identity Squad',
      overallMaturity: 'Optimizing',
      dimensions: [
        { dimension: 'ownership', level: 'Optimizing', score: 98, maxScore: 100, explanation: 'Full ownership model with clear escalation' },
        { dimension: 'contract', level: 'Optimizing', score: 92, maxScore: 100, explanation: 'All contracts versioned with compatibility checks' },
        { dimension: 'documentation', level: 'Managed', score: 85, maxScore: 100, explanation: 'Comprehensive technical and operational docs' },
        { dimension: 'runbook', level: 'Managed', score: 80, maxScore: 100, explanation: 'Runbooks for all critical and secondary paths' },
        { dimension: 'dependencyMapping', level: 'Optimizing', score: 95, maxScore: 100, explanation: 'Full topology with impact analysis' },
        { dimension: 'changeValidation', level: 'Optimizing', score: 90, maxScore: 100, explanation: 'Automated validation with blast radius analysis' },
        { dimension: 'operationalReadiness', level: 'Optimizing', score: 92, maxScore: 100, explanation: 'Full observability with proactive alerting' },
        { dimension: 'aiGovernance', level: 'Defined', score: 60, maxScore: 100, explanation: 'AI policies defined and partially enforced' },
      ],
    },
    {
      groupId: 'team-data-squad',
      groupName: 'Data Squad',
      overallMaturity: 'Defined',
      dimensions: [
        { dimension: 'ownership', level: 'Managed', score: 82, maxScore: 100, explanation: 'Data pipeline ownership well defined' },
        { dimension: 'contract', level: 'Defined', score: 55, maxScore: 100, explanation: 'Schema contracts exist but event contracts lacking' },
        { dimension: 'documentation', level: 'Managed', score: 78, maxScore: 100, explanation: 'Good documentation for data models and pipelines' },
        { dimension: 'runbook', level: 'Defined', score: 58, maxScore: 100, explanation: 'Runbooks for main pipelines only' },
        { dimension: 'dependencyMapping', level: 'Defined', score: 65, maxScore: 100, explanation: 'Data flow dependencies partially mapped' },
        { dimension: 'changeValidation', level: 'Developing', score: 42, maxScore: 100, explanation: 'Limited validation for schema changes' },
        { dimension: 'operationalReadiness', level: 'Defined', score: 62, maxScore: 100, explanation: 'Pipeline monitoring in place' },
        { dimension: 'aiGovernance', level: 'Managed', score: 75, maxScore: 100, explanation: 'AI model governance integrated with data platform' },
      ],
    },
  ],
  generatedAt: new Date().toISOString(),
};

/** Mapeia MaturityLevelType para variante do Badge. */
const maturityBadgeVariant = (level: MaturityLevelType): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (level) {
    case 'Optimizing': return 'success';
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
};

/** Mapeia score para classe de cor da barra de progresso. */
const scoreBarColor = (score: number): string => {
  if (score >= 80) return 'bg-emerald-500';
  if (score >= 60) return 'bg-amber-500';
  if (score >= 40) return 'bg-orange-500';
  return 'bg-critical';
};

/**
 * Página de Scorecards de Maturidade — avaliação multidimensional de maturidade por grupo.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function MaturityScorecardsPage() {
  const { t } = useTranslation();
  const [dimension, setDimension] = useState<MaturityDimension>('team');

  const d = mockScorecards;
  const dimensions: MaturityDimension[] = ['team', 'domain'];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.executive.maturityTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.executive.maturitySubtitle')}</p>
      </div>

      {/* Dimension Selector */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        {dimensions.map(dim => (
          <button
            key={dim}
            onClick={() => setDimension(dim)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              dimension === dim
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(`governance.executive.dimensionGroup.${dim}`)}
          </button>
        ))}
      </div>

      {/* Scorecards */}
      <div className="space-y-6">
        {d.scorecards.map(sc => (
          <Card key={sc.groupId}>
            <CardHeader>
              <div className="flex items-center gap-3">
                <Award size={16} className="text-accent" />
                <span className="text-sm font-semibold text-heading">{sc.groupName}</span>
                <Badge variant={maturityBadgeVariant(sc.overallMaturity)}>
                  {t(`governance.executive.maturityLevel.${sc.overallMaturity}`)}
                </Badge>
              </div>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                {sc.dimensions.map(dim => (
                  <div key={dim.dimension} className="bg-surface/50 rounded-md p-3 border border-edge/50">
                    <div className="flex items-center justify-between mb-1">
                      <p className="text-xs font-medium text-heading">
                        {t(`governance.executive.dim.${dim.dimension}`)}
                      </p>
                      <Badge variant={maturityBadgeVariant(dim.level)}>
                        {t(`governance.executive.maturityLevel.${dim.level}`)}
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-xs text-muted">
                        {dim.score}/{dim.maxScore}
                      </span>
                    </div>
                    <div className="w-full bg-surface rounded-full h-1.5 mb-2">
                      <div
                        className={`${scoreBarColor(dim.score)} rounded-full h-1.5 transition-all`}
                        style={{ width: `${(dim.score / dim.maxScore) * 100}%` }}
                      />
                    </div>
                    <p className="text-xs text-muted">{dim.explanation}</p>
                  </div>
                ))}
              </div>
            </CardBody>
          </Card>
        ))}
      </div>
    </div>
  );
}
