import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Route,
  Database,
  Shield,
  ChevronDown,
  ChevronUp,
  Eye,
  AlertTriangle,
  CheckCircle2,
  Cpu,
  Layers,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';

/* ------------------------------------------------------------------ */
/*  Tipos locais                                                       */
/* ------------------------------------------------------------------ */

interface RoutingStrategy {
  id: string;
  name: string;
  description: string;
  targetPersona: string;
  targetUseCase: string;
  targetClientType: string;
  preferredPath: string;
  maxSensitivityLevel: number;
  allowExternalEscalation: boolean;
  isActive: boolean;
  priority: number;
}

interface SourceWeight {
  sourceType: string;
  weight: number;
  relevance: 'Primary' | 'Secondary' | 'Supplementary';
}

interface UseCaseWeights {
  useCaseType: string;
  sources: SourceWeight[];
}

/* ------------------------------------------------------------------ */
/*  Dados mock                                                         */
/* ------------------------------------------------------------------ */

const mockStrategies: RoutingStrategy[] = [
  { id: '1', name: 'Engineer Default', description: 'Internal-first routing for engineering personas', targetPersona: 'Engineer', targetUseCase: '*', targetClientType: '*', preferredPath: 'InternalOnly', maxSensitivityLevel: 3, allowExternalEscalation: false, isActive: true, priority: 1 },
  { id: '2', name: 'Architect Advanced', description: 'Allows advanced model usage for architects when policy permits', targetPersona: 'Architect', targetUseCase: 'ContractGeneration', targetClientType: '*', preferredPath: 'InternalPreferred', maxSensitivityLevel: 4, allowExternalEscalation: true, isActive: true, priority: 2 },
  { id: '3', name: 'Executive Summary', description: 'Cost-efficient routing for executive summaries', targetPersona: 'Executive', targetUseCase: 'ExecutiveSummary', targetClientType: 'Web', preferredPath: 'InternalOnly', maxSensitivityLevel: 2, allowExternalEscalation: false, isActive: true, priority: 1 },
  { id: '4', name: 'Operations Incident', description: 'Enriched routing for incident investigation with full operational context', targetPersona: '*', targetUseCase: 'IncidentExplanation', targetClientType: '*', preferredPath: 'InternalPreferred', maxSensitivityLevel: 4, allowExternalEscalation: true, isActive: true, priority: 1 },
  { id: '5', name: 'Auditor Visibility', description: 'Read-only routing with full audit trail for compliance personas', targetPersona: 'Auditor', targetUseCase: '*', targetClientType: '*', preferredPath: 'InternalOnly', maxSensitivityLevel: 2, allowExternalEscalation: false, isActive: true, priority: 3 },
];

const mockSourceWeights: UseCaseWeights[] = [
  { useCaseType: 'ServiceLookup', sources: [{ sourceType: 'Service', weight: 60, relevance: 'Primary' }, { sourceType: 'Contract', weight: 25, relevance: 'Secondary' }, { sourceType: 'Documentation', weight: 15, relevance: 'Supplementary' }] },
  { useCaseType: 'ContractExplanation', sources: [{ sourceType: 'Contract', weight: 55, relevance: 'Primary' }, { sourceType: 'Service', weight: 25, relevance: 'Secondary' }, { sourceType: 'SourceOfTruth', weight: 20, relevance: 'Secondary' }] },
  { useCaseType: 'IncidentExplanation', sources: [{ sourceType: 'Incident', weight: 40, relevance: 'Primary' }, { sourceType: 'Change', weight: 25, relevance: 'Secondary' }, { sourceType: 'Runbook', weight: 20, relevance: 'Secondary' }, { sourceType: 'TelemetrySummary', weight: 15, relevance: 'Supplementary' }] },
  { useCaseType: 'ChangeAnalysis', sources: [{ sourceType: 'Change', weight: 45, relevance: 'Primary' }, { sourceType: 'Service', weight: 25, relevance: 'Secondary' }, { sourceType: 'Incident', weight: 20, relevance: 'Secondary' }, { sourceType: 'TelemetrySummary', weight: 10, relevance: 'Supplementary' }] },
  { useCaseType: 'ExecutiveSummary', sources: [{ sourceType: 'SourceOfTruth', weight: 40, relevance: 'Primary' }, { sourceType: 'Service', weight: 30, relevance: 'Secondary' }, { sourceType: 'TelemetrySummary', weight: 30, relevance: 'Secondary' }] },
  { useCaseType: 'MitigationGuidance', sources: [{ sourceType: 'Runbook', weight: 40, relevance: 'Primary' }, { sourceType: 'Incident', weight: 30, relevance: 'Primary' }, { sourceType: 'Service', weight: 15, relevance: 'Secondary' }, { sourceType: 'TelemetrySummary', weight: 15, relevance: 'Supplementary' }] },
];

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

type Tab = 'strategies' | 'weights';

const pathBadgeVariant = (path: string): 'info' | 'warning' => {
  return path === 'InternalOnly' ? 'info' : 'warning';
};

const sensitivityLabel = (level: number): string => {
  if (level <= 2) return 'Low';
  if (level === 3) return 'Medium';
  return 'High';
};

const sensitivityVariant = (level: number): 'success' | 'warning' | 'danger' => {
  if (level <= 2) return 'success';
  if (level === 3) return 'warning';
  return 'danger';
};

const relevanceColor = (relevance: string): string => {
  switch (relevance) {
    case 'Primary': return 'bg-accent';
    case 'Secondary': return 'bg-info';
    case 'Supplementary': return 'bg-muted';
    default: return 'bg-muted';
  }
};

/**
 * Página de AI Routing Strategies — visibilidade sobre estratégias de
 * encaminhamento e pesos de knowledge sources por caso de uso.
 * Acessível a Platform Admins e Auditors no módulo AI Hub.
 */
export function AiRoutingPage() {
  const { t } = useTranslation();
  const { persona } = usePersona();
  const [activeTab, setActiveTab] = useState<Tab>('strategies');
  const [expandedStrategy, setExpandedStrategy] = useState<string | null>(null);

  const tabs: { key: Tab; label: string; icon: React.ReactNode }[] = [
    { key: 'strategies', label: t('aiHub.routingStrategiesTab'), icon: <Route size={16} /> },
    { key: 'weights', label: t('aiHub.routingSourceWeightsTab'), icon: <Layers size={16} /> },
  ];

  const toggleExpand = (id: string) => {
    setExpandedStrategy(prev => (prev === id ? null : id));
  };

  const formatTarget = (value: string): string => {
    return value === '*' ? t('aiHub.routingWildcard') : value;
  };

  /* ---------------------------------------------------------------- */
  /*  Render — Routing Strategies                                      */
  /* ---------------------------------------------------------------- */

  const renderStrategies = () => (
    <div className="grid grid-cols-1 gap-4">
      {mockStrategies.map(strategy => {
        const isExpanded = expandedStrategy === strategy.id;
        return (
          <Card key={strategy.id}>
            <CardBody>
              {/* Header row */}
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-4 min-w-0">
                  <div className="shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-accent/15 text-accent">
                    <Route size={20} />
                  </div>
                  <div className="min-w-0">
                    <div className="flex items-center gap-2 mb-1 flex-wrap">
                      <h3 className="text-sm font-semibold text-heading truncate">{strategy.name}</h3>
                      <Badge variant={strategy.isActive ? 'success' : 'default'}>
                        {strategy.isActive ? t('aiHub.routingActive') : t('aiHub.routingInactive')}
                      </Badge>
                      <Badge variant={pathBadgeVariant(strategy.preferredPath)}>
                        {strategy.preferredPath}
                      </Badge>
                      <Badge variant={sensitivityVariant(strategy.maxSensitivityLevel)}>
                        {t('aiHub.routingSensitivity')}: {sensitivityLabel(strategy.maxSensitivityLevel)}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted">{strategy.description}</p>
                  </div>
                </div>

                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => toggleExpand(strategy.id)}
                  aria-label={isExpanded ? 'Collapse' : 'Expand'}
                >
                  {isExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                </Button>
              </div>

              {/* Expanded detail */}
              {isExpanded && (
                <div className="mt-4 pt-4 border-t border-edge grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 text-xs">
                  <div>
                    <span className="text-muted">{t('aiHub.routingTargetPersona')}</span>
                    <p className="text-body font-medium mt-0.5">{formatTarget(strategy.targetPersona)}</p>
                  </div>
                  <div>
                    <span className="text-muted">{t('aiHub.routingTargetUseCase')}</span>
                    <p className="text-body font-medium mt-0.5">{formatTarget(strategy.targetUseCase)}</p>
                  </div>
                  <div>
                    <span className="text-muted">{t('aiHub.routingTargetClient')}</span>
                    <p className="text-body font-medium mt-0.5">{formatTarget(strategy.targetClientType)}</p>
                  </div>
                  <div>
                    <span className="text-muted">{t('aiHub.routingPreferredPath')}</span>
                    <p className="text-body font-medium mt-0.5">{strategy.preferredPath}</p>
                  </div>
                  <div>
                    <span className="text-muted">{t('aiHub.routingSensitivity')}</span>
                    <p className="text-body font-medium mt-0.5">
                      {strategy.maxSensitivityLevel} — {sensitivityLabel(strategy.maxSensitivityLevel)}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('aiHub.routingEscalationAllowed')}</span>
                    <p className="mt-0.5">
                      <Badge variant={strategy.allowExternalEscalation ? 'warning' : 'info'}>
                        {strategy.allowExternalEscalation
                          ? t('aiHub.routingYes')
                          : t('aiHub.routingNo')}
                      </Badge>
                    </p>
                  </div>
                  <div>
                    <span className="text-muted">{t('aiHub.routingPriority')}</span>
                    <p className="text-body font-medium mt-0.5">{strategy.priority}</p>
                  </div>
                </div>
              )}
            </CardBody>
          </Card>
        );
      })}
    </div>
  );

  /* ---------------------------------------------------------------- */
  /*  Render — Source Weights                                           */
  /* ---------------------------------------------------------------- */

  const renderSourceWeights = () => (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
      {mockSourceWeights.map(uc => (
        <Card key={uc.useCaseType}>
          <CardBody>
            <div className="flex items-center gap-3 mb-4">
              <div className="shrink-0 w-9 h-9 rounded-lg flex items-center justify-center bg-info/15 text-info">
                <Database size={18} />
              </div>
              <h3 className="text-sm font-semibold text-heading">{uc.useCaseType}</h3>
            </div>

            <div className="space-y-3">
              {uc.sources.map(src => {
                const relevanceKey =
                  src.relevance === 'Primary'
                    ? 'aiHub.routingWeightPrimary'
                    : src.relevance === 'Secondary'
                      ? 'aiHub.routingWeightSecondary'
                      : 'aiHub.routingWeightSupplementary';

                return (
                  <div key={src.sourceType}>
                    <div className="flex items-center justify-between mb-1">
                      <span className="text-xs text-body font-medium">{src.sourceType}</span>
                      <div className="flex items-center gap-2">
                        <Badge
                          variant={
                            src.relevance === 'Primary'
                              ? 'success'
                              : src.relevance === 'Secondary'
                                ? 'info'
                                : 'default'
                          }
                        >
                          {t(relevanceKey)}
                        </Badge>
                        <span className="text-xs text-muted font-semibold">{src.weight}%</span>
                      </div>
                    </div>
                    {/* Progress bar */}
                    <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all ${relevanceColor(src.relevance)}`}
                        style={{ width: `${src.weight}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          </CardBody>
        </Card>
      ))}
    </div>
  );

  /* ---------------------------------------------------------------- */
  /*  Render principal                                                 */
  /* ---------------------------------------------------------------- */

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Page header */}
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('aiHub.routingTitle')}</h1>
          <p className="text-muted mt-1">{t('aiHub.routingSubtitle')}</p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="info">
            <Eye size={12} className="mr-1 inline" />
            {persona}
          </Badge>
        </div>
      </div>

      {/* Governance notice */}
      <div className="mb-6 flex items-center gap-3 rounded-lg border border-edge bg-elevated px-4 py-3">
        <Shield size={18} className="shrink-0 text-warning" />
        <p className="text-xs text-muted">{t('aiHub.routingGovernanceNotice')}</p>
      </div>

      {/* Stat overview */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <Route size={18} className="text-accent" />
          <div>
            <p className="text-lg font-bold text-heading">{mockStrategies.length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingStrategiesTab')}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <CheckCircle2 size={18} className="text-success" />
          <div>
            <p className="text-lg font-bold text-heading">{mockStrategies.filter(s => s.isActive).length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingActive')}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <AlertTriangle size={18} className="text-warning" />
          <div>
            <p className="text-lg font-bold text-heading">{mockStrategies.filter(s => s.allowExternalEscalation).length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingEscalationAllowed')}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <Cpu size={18} className="text-info" />
          <div>
            <p className="text-lg font-bold text-heading">{mockSourceWeights.length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingSourceWeightsTitle')}</p>
          </div>
        </div>
      </div>

      {/* Tab bar */}
      <div className="flex items-center gap-1 mb-6">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-1.5 px-4 py-2 rounded-md text-xs font-medium transition-colors ${
              activeTab === tab.key
                ? 'bg-accent text-heading'
                : 'bg-elevated text-muted hover:text-body'
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      {activeTab === 'strategies' ? renderStrategies() : renderSourceWeights()}
    </div>
  );
}
