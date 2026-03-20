import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
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
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Loader } from '../../../components/Loader';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { aiGovernanceApi } from '../api';

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
  createdAt?: string;
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
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

type Tab = 'strategies' | 'weights';

const pathBadgeVariant = (path: string): 'info' | 'warning' => {
  return path === 'InternalOnly' ? 'info' : 'warning';
};

const sensitivityLabel = (t: (key: string) => string, level: number): string => {
  if (level <= 2) return t('aiHub.sensitivityLow');
  if (level === 3) return t('aiHub.sensitivityMedium');
  return t('aiHub.sensitivityHigh');
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

  const {
    data: strategiesData,
    isLoading: isLoadingStrategies,
    isError: isStrategiesError,
    refetch: refetchStrategies,
  } = useQuery({
    queryKey: ['ai-governance', 'routing', 'strategies'],
    queryFn: () => aiGovernanceApi.listRoutingStrategies(),
    staleTime: 30_000,
  });

  const {
    data: weightsData,
    isLoading: isLoadingWeights,
    isError: isWeightsError,
    refetch: refetchWeights,
  } = useQuery({
    queryKey: ['ai-governance', 'knowledge-sources', 'weights'],
    queryFn: () => aiGovernanceApi.listKnowledgeSourceWeights(),
    staleTime: 30_000,
  });

  const strategies: RoutingStrategy[] = useMemo(() => {
    const items = (strategiesData?.items ?? []) as Array<{
      strategyId: string;
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
      createdAt: string;
    }>;

    return items.map((s) => ({
      id: s.strategyId,
      name: s.name,
      description: s.description,
      targetPersona: s.targetPersona,
      targetUseCase: s.targetUseCase,
      targetClientType: s.targetClientType,
      preferredPath: s.preferredPath,
      maxSensitivityLevel: s.maxSensitivityLevel,
      allowExternalEscalation: s.allowExternalEscalation,
      isActive: s.isActive,
      priority: s.priority,
      createdAt: s.createdAt,
    }));
  }, [strategiesData]);

  const sourceWeights: UseCaseWeights[] = useMemo(() => {
    const items = (weightsData?.items ?? []) as Array<{
      sourceType: string;
      useCaseType: string;
      relevance: 'Primary' | 'Secondary' | 'Supplementary';
      weight: number;
    }>;

    const byUseCase = new Map<string, SourceWeight[]>();
    for (const item of items) {
      const list = byUseCase.get(item.useCaseType) ?? [];
      list.push({
        sourceType: item.sourceType,
        relevance: item.relevance,
        weight: item.weight,
      });
      byUseCase.set(item.useCaseType, list);
    }

    return Array.from(byUseCase.entries()).map(([useCaseType, sources]) => ({
      useCaseType,
      sources: sources.sort((a, b) => b.weight - a.weight),
    }));
  }, [weightsData]);

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
      {strategies.map(strategy => {
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
                        {t('aiHub.routingSensitivity')}: {sensitivityLabel(t, strategy.maxSensitivityLevel)}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted">{strategy.description}</p>
                  </div>
                </div>

                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => toggleExpand(strategy.id)}
                  aria-label={isExpanded ? t('common.collapse') : t('common.expand')}
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
                      {strategy.maxSensitivityLevel} — {sensitivityLabel(t, strategy.maxSensitivityLevel)}
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
      {sourceWeights.map(uc => (
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
    <PageContainer>
      <PageHeader
        title={t('aiHub.routingTitle')}
        subtitle={t('aiHub.routingSubtitle')}
        actions={
          <div className="flex items-center gap-2">
            <Badge variant="info">
              <Eye size={12} className="mr-1 inline" />
              {persona}
            </Badge>
          </div>
        }
      />

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
            <p className="text-lg font-bold text-heading">{strategies.length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingStrategiesTab')}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <CheckCircle2 size={18} className="text-success" />
          <div>
            <p className="text-lg font-bold text-heading">{strategies.filter(s => s.isActive).length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingActive')}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <AlertTriangle size={18} className="text-warning" />
          <div>
            <p className="text-lg font-bold text-heading">{strategies.filter(s => s.allowExternalEscalation).length}</p>
            <p className="text-xs text-muted">{t('aiHub.routingEscalationAllowed')}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 rounded-lg border border-edge bg-card px-4 py-3">
          <Cpu size={18} className="text-info" />
          <div>
            <p className="text-lg font-bold text-heading">{sourceWeights.length}</p>
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
      {(isLoadingStrategies || isLoadingWeights) && (
        <Card>
          <CardBody className="flex justify-center py-16">
            <Loader size="lg" />
          </CardBody>
        </Card>
      )}

      {(isStrategiesError || isWeightsError) && (
        <PageErrorState
          action={(
            <Button
              variant="secondary"
              size="sm"
              onClick={() => {
                refetchStrategies();
                refetchWeights();
              }}
            >
              {t('common.retry', 'Retry')}
            </Button>
          )}
        />
      )}

      {!isLoadingStrategies && !isLoadingWeights && !isStrategiesError && !isWeightsError && (
        activeTab === 'strategies'
          ? (strategies.length > 0 ? renderStrategies() : (
            <EmptyState
              title={t('aiHub.noRoutingStrategiesFound', 'No routing strategies found')}
              size="compact"
            />
          ))
          : (sourceWeights.length > 0 ? renderSourceWeights() : (
            <EmptyState
              title={t('aiHub.noRoutingWeightsFound', 'No source weights found')}
              size="compact"
            />
          ))
      )}
    </PageContainer>
  );
}
