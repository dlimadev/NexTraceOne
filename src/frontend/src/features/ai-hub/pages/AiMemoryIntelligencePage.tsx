import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Brain, TrendingUp, Award, Layers, Link2, BarChart3 } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { PageErrorState } from '../../../components/PageErrorState';
import { useAuth } from '../../../contexts/AuthContext';
import { aiGovernanceApi } from '../api';

// ── Types ────────────────────────────────────────────────────────────────────

interface NodeTypeBreakdown {
  nodeType: string;
  count: number;
  freshCount: number;
  staleCount: number;
  linkedCount: number;
  averageRelevance: number;
}

interface MemoryHealthReport {
  tenantId: string;
  totalNodes: number;
  freshNodes: number;
  staleNodes: number;
  linkedNodes: number;
  freshnessRatePct: number;
  connectivityRatePct: number;
  averageRelevanceScore: number;
  memoryHealthTier: string;
  nodeTypeBreakdown: NodeTypeBreakdown[];
  recentNodeTitles: string[];
  lookbackDays: number;
}

interface AgentBenchmarkItem {
  agentId: string;
  agentName: string;
  totalExecutions: number;
  accuracyRate: number;
  averageRating: number;
  feedbackCoveragePct: number;
  rlCyclesCompleted: number;
  benchmarkScore: number;
  benchmarkTier: string;
}

interface BenchmarkReport {
  tenantId: string;
  totalAgentsEvaluated: number;
  qualifiedAgents: number;
  agentBenchmarks: AgentBenchmarkItem[];
  tierSummary: Record<string, number>;
  topPerformerName: string | null;
  averageBenchmarkScore: number;
}

interface MaturityDimension {
  dimensionName: string;
  actualValue: number;
  maxObserved: number;
  scorePct: number;
  description: string;
}

interface MaturityReport {
  tenantId: string;
  maturityScore: number;
  maturityLevel: string;
  totalActiveAgents: number;
  totalAgents: number;
  totalPublishedSkills: number;
  totalSkills: number;
  organizationalMemoryNodes: number;
  feedbackLoopScore: number;
  rlAdoptionPct: number;
  hasPioneerAdoption: boolean;
  maturityDimensions: MaturityDimension[];
  lookbackDays: number;
}

type ActiveTab = 'memory' | 'benchmark' | 'maturity';

// ── Helpers ──────────────────────────────────────────────────────────────────

function tierBadgeVariant(tier: string): 'success' | 'warning' | 'info' | 'danger' | 'default' {
  switch (tier) {
    case 'Thriving':
    case 'Champion':
    case 'Innovating': return 'success';
    case 'Active':
    case 'HighPerformer':
    case 'Scaling': return 'info';
    case 'Building':
    case 'Adopting':
    case 'Exploring': return 'warning';
    case 'Empty':
    case 'Underperforming':
    case 'Initiating': return 'danger';
    default: return 'default';
  }
}

function maturityScoreColor(score: number): string {
  if (score >= 80) return 'text-success';
  if (score >= 60) return 'text-info';
  if (score >= 40) return 'text-warning';
  return 'text-danger';
}

// ── Sub-panels ───────────────────────────────────────────────────────────────

function MemoryHealthPanel({ tenantId }: { tenantId: string }) {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-intelligence', 'memory-health', tenantId],
    queryFn: () => aiGovernanceApi.getOrganizationalMemoryHealthReport({ tenantId }),
    staleTime: 60_000,
  });

  const report = data as MemoryHealthReport | undefined;

  if (isLoading) return <CardListSkeleton count={4} />;
  if (isError) return <PageErrorState onRetry={refetch} />;
  if (!report) return null;

  return (
    <div className="space-y-6">
      {/* KPI row */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          title={t('organizationalMemoryHealth.totalNodes')}
          value={report.totalNodes}
          icon={<Brain size={16} />}
        />
        <StatCard
          title={t('organizationalMemoryHealth.freshNodes')}
          value={report.freshNodes}
          icon={<TrendingUp size={16} />}
        />
        <StatCard
          title={t('organizationalMemoryHealth.freshnessRate')}
          value={`${report.freshnessRatePct}%`}
          icon={<BarChart3 size={16} />}
        />
        <StatCard
          title={t('organizationalMemoryHealth.connectivityRate')}
          value={`${report.connectivityRatePct}%`}
          icon={<Link2 size={16} />}
        />
      </div>

      {/* Tier + avg relevance */}
      <Card>
        <CardBody>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs text-muted mb-1">{t('organizationalMemoryHealth.memoryHealthTier')}</p>
              <Badge variant={tierBadgeVariant(report.memoryHealthTier)}>
                {t(`organizationalMemoryHealth.tiers.${report.memoryHealthTier}`, { defaultValue: report.memoryHealthTier })}
              </Badge>
            </div>
            <div className="text-right">
              <p className="text-xs text-muted mb-1">{t('organizationalMemoryHealth.avgRelevance')}</p>
              <span className="text-lg font-semibold text-heading">{report.averageRelevanceScore.toFixed(2)}</span>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Node type breakdown */}
      <Card>
        <CardBody>
          <h3 className="text-sm font-medium text-heading mb-3">{t('organizationalMemoryHealth.nodeTypeBreakdown')}</h3>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-muted text-left">
                  <th className="pb-2 pr-4">Type</th>
                  <th className="pb-2 pr-4 text-right">Count</th>
                  <th className="pb-2 pr-4 text-right">{t('organizationalMemoryHealth.freshNodes')}</th>
                  <th className="pb-2 pr-4 text-right">{t('organizationalMemoryHealth.linkedNodes')}</th>
                  <th className="pb-2 text-right">{t('organizationalMemoryHealth.avgRelevance')}</th>
                </tr>
              </thead>
              <tbody>
                {report.nodeTypeBreakdown.map(row => (
                  <tr key={row.nodeType} className="border-b border-edge last:border-0">
                    <td className="py-2 pr-4 text-heading">
                      {t(`organizationalMemoryHealth.nodeTypes.${row.nodeType}`, { defaultValue: row.nodeType })}
                    </td>
                    <td className="py-2 pr-4 text-right">{row.count}</td>
                    <td className="py-2 pr-4 text-right text-success">{row.freshCount}</td>
                    <td className="py-2 pr-4 text-right text-info">{row.linkedCount}</td>
                    <td className="py-2 text-right">{row.averageRelevance.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardBody>
      </Card>

      {/* Recent nodes */}
      {report.recentNodeTitles.length > 0 && (
        <Card>
          <CardBody>
            <h3 className="text-sm font-medium text-heading mb-3">{t('organizationalMemoryHealth.recentNodes')}</h3>
            <ul className="space-y-1">
              {report.recentNodeTitles.map((title, i) => (
                <li key={i} className="text-sm text-muted flex items-center gap-2">
                  <span className="w-1.5 h-1.5 rounded-full bg-accent flex-shrink-0" />
                  {title}
                </li>
              ))}
            </ul>
          </CardBody>
        </Card>
      )}
    </div>
  );
}

function AgentBenchmarkPanel({ tenantId }: { tenantId: string }) {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-intelligence', 'agent-benchmark', tenantId],
    queryFn: () => aiGovernanceApi.getAgentPerformanceBenchmarkReport({ tenantId }),
    staleTime: 60_000,
  });

  const report = data as BenchmarkReport | undefined;

  if (isLoading) return <CardListSkeleton count={4} />;
  if (isError) return <PageErrorState onRetry={refetch} />;
  if (!report) return null;

  const tierColors: Record<string, string> = {
    Champion: 'text-success',
    HighPerformer: 'text-info',
    Active: 'text-heading',
    Developing: 'text-warning',
    Underperforming: 'text-danger',
  };

  return (
    <div className="space-y-6">
      {/* KPIs */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          title={t('agentPerformanceBenchmark.totalAgents')}
          value={report.totalAgentsEvaluated}
          icon={<Brain size={16} />}
        />
        <StatCard
          title={t('agentPerformanceBenchmark.qualifiedAgents')}
          value={report.qualifiedAgents}
          icon={<BarChart3 size={16} />}
        />
        <StatCard
          title={t('agentPerformanceBenchmark.avgBenchmarkScore')}
          value={report.averageBenchmarkScore.toFixed(3)}
          icon={<Award size={16} />}
        />
        <StatCard
          title={t('agentPerformanceBenchmark.topPerformer')}
          value={report.topPerformerName ?? '—'}
          icon={<TrendingUp size={16} />}
        />
      </div>

      {/* Tier distribution */}
      <Card>
        <CardBody>
          <h3 className="text-sm font-medium text-heading mb-3">{t('agentPerformanceBenchmark.tierSummary')}</h3>
          <div className="flex flex-wrap gap-3">
            {Object.entries(report.tierSummary).map(([tier, count]) => (
              <div key={tier} className="flex items-center gap-2 px-3 py-1.5 rounded border border-edge bg-surface">
                <span className={`text-sm font-medium ${tierColors[tier] ?? ''}`}>
                  {t(`agentPerformanceBenchmark.tiers.${tier}`, { defaultValue: tier })}
                </span>
                <Badge variant={tierBadgeVariant(tier)}>{count}</Badge>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>

      {/* Agent table */}
      {report.agentBenchmarks.length > 0 && (
        <Card>
          <CardBody>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-muted text-left">
                    <th className="pb-2 pr-4">Agent</th>
                    <th className="pb-2 pr-4 text-right">{t('agentPerformanceBenchmark.benchmarkScore')}</th>
                    <th className="pb-2 pr-4 text-right">{t('agentPerformanceBenchmark.accuracyRate')}</th>
                    <th className="pb-2 pr-4 text-right">{t('agentPerformanceBenchmark.avgRating')}</th>
                    <th className="pb-2 pr-4 text-right">{t('agentPerformanceBenchmark.feedbackCoverage')}</th>
                    <th className="pb-2 text-right">{t('agentPerformanceBenchmark.benchmarkTier')}</th>
                  </tr>
                </thead>
                <tbody>
                  {report.agentBenchmarks.map(agent => (
                    <tr key={agent.agentId} className="border-b border-edge last:border-0">
                      <td className="py-2 pr-4 text-heading font-medium">{agent.agentName}</td>
                      <td className="py-2 pr-4 text-right font-mono">{agent.benchmarkScore.toFixed(3)}</td>
                      <td className="py-2 pr-4 text-right">{(agent.accuracyRate * 100).toFixed(1)}%</td>
                      <td className="py-2 pr-4 text-right">{agent.averageRating.toFixed(1)}</td>
                      <td className="py-2 pr-4 text-right">{agent.feedbackCoveragePct.toFixed(1)}%</td>
                      <td className="py-2 text-right">
                        <Badge variant={tierBadgeVariant(agent.benchmarkTier)}>
                          {t(`agentPerformanceBenchmark.tiers.${agent.benchmarkTier}`, { defaultValue: agent.benchmarkTier })}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardBody>
        </Card>
      )}
    </div>
  );
}

function MaturityPanel({ tenantId }: { tenantId: string }) {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['ai-intelligence', 'capability-maturity', tenantId],
    queryFn: () => aiGovernanceApi.getAiCapabilityMaturityReport({ tenantId }),
    staleTime: 60_000,
  });

  const report = data as MaturityReport | undefined;

  if (isLoading) return <CardListSkeleton count={4} />;
  if (isError) return <PageErrorState onRetry={refetch} />;
  if (!report) return null;

  return (
    <div className="space-y-6">
      {/* KPIs */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard
          title={t('aiCapabilityMaturity.maturityScore')}
          value={`${report.maturityScore}/100`}
          icon={<Layers size={16} />}
        />
        <StatCard
          title={t('aiCapabilityMaturity.activeAgents')}
          value={`${report.totalActiveAgents}/${report.totalAgents}`}
          icon={<Brain size={16} />}
        />
        <StatCard
          title={t('aiCapabilityMaturity.publishedSkills')}
          value={`${report.totalPublishedSkills}/${report.totalSkills}`}
          icon={<BarChart3 size={16} />}
        />
        <StatCard
          title={t('aiCapabilityMaturity.memoryNodes')}
          value={report.organizationalMemoryNodes}
          icon={<Link2 size={16} />}
        />
      </div>

      {/* Maturity level */}
      <Card>
        <CardBody>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-xs text-muted mb-1">{t('aiCapabilityMaturity.maturityLevel')}</p>
              <Badge variant={tierBadgeVariant(report.maturityLevel)}>
                {t(`aiCapabilityMaturity.levels.${report.maturityLevel}`, { defaultValue: report.maturityLevel })}
              </Badge>
            </div>
            <div className="text-right">
              <p className={`text-3xl font-bold ${maturityScoreColor(report.maturityScore)}`}>
                {report.maturityScore}
              </p>
              <p className="text-xs text-muted">/ 100</p>
            </div>
          </div>
          <div className="mt-4 flex flex-wrap gap-4 text-sm">
            <span className="text-muted">
              {t('aiCapabilityMaturity.feedbackLoopScore')}:{' '}
              <span className="text-heading">{(report.feedbackLoopScore * 100).toFixed(1)}%</span>
            </span>
            <span className="text-muted">
              {t('aiCapabilityMaturity.rlAdoption')}:{' '}
              <span className="text-heading">{report.rlAdoptionPct.toFixed(1)}%</span>
            </span>
            {report.hasPioneerAdoption && (
              <Badge variant="success">{t('aiCapabilityMaturity.pioneerAdoption')}</Badge>
            )}
          </div>
        </CardBody>
      </Card>

      {/* Dimension breakdown */}
      <Card>
        <CardBody>
          <h3 className="text-sm font-medium text-heading mb-3">{t('aiCapabilityMaturity.dimensions')}</h3>
          <div className="space-y-3">
            {report.maturityDimensions.map(dim => (
              <div key={dim.dimensionName}>
                <div className="flex justify-between text-sm mb-1">
                  <span className="text-heading">{dim.dimensionName}</span>
                  <span className="text-muted">{dim.scorePct.toFixed(1)}%</span>
                </div>
                <div className="w-full h-2 rounded-full bg-surface">
                  <div
                    className="h-2 rounded-full bg-accent"
                    style={{ width: `${Math.min(dim.scorePct, 100)}%` }}
                  />
                </div>
                <p className="text-xs text-faded mt-0.5">{dim.description}</p>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}

// ── Main page ────────────────────────────────────────────────────────────────

export function AiMemoryIntelligencePage() {
  const { t } = useTranslation();
  const { tenantId } = useAuth();
  const [activeTab, setActiveTab] = useState<ActiveTab>('memory');

  if (!tenantId) {
    return (
      <PageContainer>
        <div className="rounded-lg border border-edge bg-elevated p-8 text-center">
          <p className="text-sm text-muted">{t('common.noTenantContext', { defaultValue: 'No tenant context available.' })}</p>
        </div>
      </PageContainer>
    );
  }

  const tabs: Array<{ key: ActiveTab; label: string; icon: React.ReactNode }> = [
    { key: 'memory', label: t('organizationalMemoryHealth.pageTitle'), icon: <Brain size={14} /> },
    { key: 'benchmark', label: t('agentPerformanceBenchmark.pageTitle'), icon: <Award size={14} /> },
    { key: 'maturity', label: t('aiCapabilityMaturity.pageTitle'), icon: <Layers size={14} /> },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('organizationalMemoryHealth.pageTitle')}
        subtitle={t('organizationalMemoryHealth.pageSubtitle')}
      />

      {/* Tabs */}
      <div className="flex gap-1 mb-6 border-b border-edge">
        {tabs.map(tab => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={[
              'flex items-center gap-1.5 px-4 py-2 text-sm font-medium transition-colors',
              activeTab === tab.key
                ? 'text-accent border-b-2 border-accent -mb-px'
                : 'text-muted hover:text-heading',
            ].join(' ')}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {/* Panel */}
      {activeTab === 'memory' && <MemoryHealthPanel tenantId={tenantId} />}
      {activeTab === 'benchmark' && <AgentBenchmarkPanel tenantId={tenantId} />}
      {activeTab === 'maturity' && <MaturityPanel tenantId={tenantId} />}
    </PageContainer>
  );
}
