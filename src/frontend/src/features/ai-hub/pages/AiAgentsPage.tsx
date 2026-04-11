import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { EmptyState } from '../../../components/EmptyState';
import {
  Bot,
  Plus,
  Search,
  Shield,
  Users,
  User,
  Play,
  Eye,
  Loader2,
  AlertCircle,
  Inbox,
  CheckCircle2,
  Ban,
  Sparkles,
  Cpu,
  Globe,
  Lock,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageErrorState } from '../../../components/PageErrorState';
import { CardListSkeleton } from '../../../components/CardListSkeleton';
import { useToast } from '../../../components/Toast';
import { aiGovernanceApi } from '../api/aiGovernance';

// ── Types ───────────────────────────────────────────────────────────────

interface AgentListItem {
  agentId: string;
  name: string;
  displayName: string;
  slug: string;
  description: string;
  category: string;
  isOfficial: boolean;
  isActive: boolean;
  capabilities: string;
  targetPersona: string;
  icon: string;
  preferredModelId: string | null;
  ownershipType: string;
  visibility: string;
  publicationStatus: string;
  version: number;
  executionCount: number;
}

interface AgentsResponse {
  items: AgentListItem[];
  totalCount: number;
}

const FALLBACK_AGENT_CATEGORIES = [
  'General',
  'ApiDesign',
  'TestGeneration',
  'EventDesign',
  'Documentation',
  'Analysis',
  'CodeReview',
  'Security',
  'Operations',
];

function humanizeEnumValue(value: string): string {
  return value
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .trim();
}

// ── Create Agent Dialog ─────────────────────────────────────────────────

interface CreateAgentDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
  categories: string[];
  defaultCategory: string;
}

function CreateAgentDialog({
  isOpen,
  onClose,
  onCreated,
  categories,
  defaultCategory,
}: CreateAgentDialogProps) {
  const { t } = useTranslation();
  const [name, setName] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState(defaultCategory);
  const [systemPrompt, setSystemPrompt] = useState('');
  const [objective, setObjective] = useState('');
  const [visibility, setVisibility] = useState('Team');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!categories.length) return;
    if (!categories.includes(category)) {
      setCategory(defaultCategory);
    }
  }, [categories, category, defaultCategory]);

  const handleSubmit = async () => {
    if (!name.trim() || !displayName.trim() || !systemPrompt.trim()) return;
    setIsSubmitting(true);
    setError(null);
    try {
      await aiGovernanceApi.createAgent({
        name: name.trim(),
        displayName: displayName.trim(),
        description: description.trim(),
        category,
        systemPrompt: systemPrompt.trim(),
        objective: objective.trim() || undefined,
        ownershipType: 'Tenant',
        visibility,
      });
      onCreated();
      onClose();
      setName('');
      setDisplayName('');
      setDescription('');
      setCategory(defaultCategory);
      setSystemPrompt('');
      setObjective('');
      setVisibility('Team');
    } catch {
      setError(t('agents.createError'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-card border border-edge rounded-lg shadow-lg w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div className="px-6 py-4 border-b border-edge flex items-center justify-between">
          <h2 className="text-lg font-semibold text-heading">{t('agents.createTitle')}</h2>
          <Button variant="ghost" size="sm" onClick={onClose}>&times;</Button>
        </div>
        <div className="px-6 py-4 space-y-4">
          {error && (
            <div className="p-3 rounded-md bg-critical/15 border border-critical/25 text-sm text-critical flex items-center gap-2">
              <AlertCircle size={16} /> {error}
            </div>
          )}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldName')}</label>
              <input
                type="text"
                value={name}
                onChange={e => setName(e.target.value)}
                placeholder={t('aiHub.agents.placeholder.agentName', 'my-custom-agent')}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldDisplayName')}</label>
              <input
                type="text"
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                placeholder={t('agents.displayNamePlaceholder')}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldDescription')}</label>
            <textarea
              value={description}
              onChange={e => setDescription(e.target.value)}
              rows={2}
              placeholder={t('agents.descriptionPlaceholder')}
              className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent resize-none"
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldCategory')}</label>
              <select
                value={category}
                onChange={e => setCategory(e.target.value)}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent"
              >
                {categories.map(c => (
                  <option key={c} value={c}>{t(`agents.category.${c}`) || humanizeEnumValue(c)}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldVisibility')}</label>
              <select
                value={visibility}
                onChange={e => setVisibility(e.target.value)}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent"
              >
                <option value="Private">{t('agents.visibility.Private')}</option>
                <option value="Team">{t('agents.visibility.Team')}</option>
                <option value="Tenant">{t('agents.visibility.Tenant')}</option>
              </select>
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldObjective')}</label>
            <input
              type="text"
              value={objective}
              onChange={e => setObjective(e.target.value)}
              placeholder={t('agents.objectivePlaceholder')}
              className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldSystemPrompt')}</label>
            <textarea
              value={systemPrompt}
              onChange={e => setSystemPrompt(e.target.value)}
              rows={6}
              placeholder={t('agents.systemPromptPlaceholder')}
              className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent resize-none font-mono text-xs"
            />
          </div>
        </div>
        <div className="px-6 py-4 border-t border-edge flex justify-end gap-3">
          <Button variant="ghost" size="sm" onClick={onClose} disabled={isSubmitting}>
            {t('common.cancel')}
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={handleSubmit}
            disabled={isSubmitting || !name.trim() || !displayName.trim() || !systemPrompt.trim()}
          >
            {isSubmitting && <Loader2 size={14} className="animate-spin mr-1" />}
            {t('agents.createButton')}
          </Button>
        </div>
      </div>
    </div>
  );
}

// ── Execute Agent Dialog ────────────────────────────────────────────────

interface ExecuteAgentDialogProps {
  isOpen: boolean;
  agent: AgentListItem | null;
  onClose: () => void;
}

interface ExecutionResult {
  executionId: string;
  agentId: string;
  status: string;
  output: string;
  modelUsed: string;
  providerUsed: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  durationMs: number;
  artifacts: ArtifactResult[];
}

interface ArtifactResult {
  artifactId: string;
  title: string;
  artifactType: string;
  format: string;
  content: string;
  reviewStatus: string;
}

function ExecuteAgentDialog({ isOpen, agent, onClose }: ExecuteAgentDialogProps) {
  const { t } = useTranslation();
  const [input, setInput] = useState('');
  const [isExecuting, setIsExecuting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<ExecutionResult | null>(null);
  const [reviewingArtifact, setReviewingArtifact] = useState<string | null>(null);

  const handleExecute = async () => {
    if (!input.trim() || !agent) return;
    setIsExecuting(true);
    setError(null);
    setResult(null);
    try {
      const data = await aiGovernanceApi.executeAgent(agent.agentId, { input: input.trim() });
      setResult(data);
    } catch {
      setError(t('agents.executeError'));
    } finally {
      setIsExecuting(false);
    }
  };

  const handleReview = async (artifactId: string, decision: string) => {
    setReviewingArtifact(artifactId);
    try {
      await aiGovernanceApi.reviewArtifact(artifactId, { decision });
      if (result) {
        setResult({
          ...result,
          artifacts: result.artifacts.map(a =>
            a.artifactId === artifactId ? { ...a, reviewStatus: decision === 'Approve' ? 'Approved' : 'Rejected' } : a,
          ),
        });
      }
    } catch {
      // Review failed silently
    } finally {
      setReviewingArtifact(null);
    }
  };

  const handleClose = () => {
    setInput('');
    setResult(null);
    setError(null);
    onClose();
  };

  if (!isOpen || !agent) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-card border border-edge rounded-lg shadow-lg w-full max-w-3xl max-h-[90vh] overflow-y-auto">
        <div className="px-6 py-4 border-b border-edge">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center">
              <Play size={16} className="text-accent" />
            </div>
            <div>
              <h2 className="text-lg font-semibold text-heading">{t('agents.executeTitle')}</h2>
              <p className="text-xs text-muted">{agent.displayName}</p>
            </div>
          </div>
        </div>
        <div className="px-6 py-4 space-y-4">
          {error && (
            <div className="p-3 rounded-md bg-critical/15 border border-critical/25 text-sm text-critical flex items-center gap-2">
              <AlertCircle size={16} /> {error}
            </div>
          )}

          {!result && (
            <>
              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('agents.inputLabel')}</label>
                <textarea
                  value={input}
                  onChange={e => setInput(e.target.value)}
                  rows={5}
                  placeholder={t('agents.inputPlaceholder')}
                  className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                />
              </div>
              <div className="flex justify-end gap-3">
                <Button variant="ghost" size="sm" onClick={handleClose} disabled={isExecuting}>
                  {t('common.cancel')}
                </Button>
                <Button variant="primary" size="sm" onClick={handleExecute} disabled={isExecuting || !input.trim()}>
                  {isExecuting ? <Loader2 size={14} className="animate-spin mr-1" /> : <Play size={14} className="mr-1" />}
                  {t('agents.executeButton')}
                </Button>
              </div>
            </>
          )}

          {result && (
            <div className="space-y-4">
              {/* Execution metadata */}
              <div className="flex items-center gap-3 flex-wrap">
                <Badge variant={result.status === 'Completed' ? 'success' : 'warning'}>
                  {result.status === 'Completed' ? <CheckCircle2 size={10} className="mr-0.5" /> : <AlertCircle size={10} className="mr-0.5" />}
                  {result.status}
                </Badge>
                <span className="text-xs text-muted">
                  <Cpu size={10} className="inline mr-0.5" /> {result.modelUsed}
                </span>
                <span className="text-xs text-muted">{result.durationMs}ms</span>
                <span className="text-xs text-muted">
                  {result.totalTokens} {t('agents.tokens')}
                </span>
              </div>

              {/* Output */}
              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('agents.outputLabel')}</label>
                <div className="rounded-md border border-edge bg-elevated p-3 text-sm text-body whitespace-pre-wrap max-h-[300px] overflow-y-auto font-mono text-xs">
                  {result.output}
                </div>
              </div>

              {/* Artifacts */}
              {result.artifacts.length > 0 && (
                <div>
                  <label className="block text-xs font-medium text-muted mb-2">{t('agents.artifacts')} ({result.artifacts.length})</label>
                  <div className="space-y-3">
                    {result.artifacts.map(artifact => (
                      <div key={artifact.artifactId} className="rounded-md border border-edge bg-elevated p-3">
                        <div className="flex items-center justify-between mb-2">
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium text-heading">{artifact.title}</span>
                            <Badge variant="default">{artifact.artifactType}</Badge>
                            <Badge variant={artifact.reviewStatus === 'Approved' ? 'success' : artifact.reviewStatus === 'Rejected' ? 'warning' : 'default'}>
                              {artifact.reviewStatus}
                            </Badge>
                          </div>
                          {artifact.reviewStatus === 'Pending' && (
                            <div className="flex items-center gap-1">
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleReview(artifact.artifactId, 'Approve')}
                                disabled={reviewingArtifact === artifact.artifactId}
                              >
                                <CheckCircle2 size={14} className="text-success mr-1" />
                                {t('agents.approve')}
                              </Button>
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleReview(artifact.artifactId, 'Reject')}
                                disabled={reviewingArtifact === artifact.artifactId}
                              >
                                <Ban size={14} className="text-critical mr-1" />
                                {t('agents.reject')}
                              </Button>
                            </div>
                          )}
                        </div>
                        <div className="rounded border border-edge bg-card p-2 text-xs text-body whitespace-pre-wrap max-h-[200px] overflow-y-auto font-mono">
                          {artifact.content}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="flex justify-end">
                <Button variant="ghost" size="sm" onClick={handleClose}>
                  {t('common.close')}
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Helper functions ────────────────────────────────────────────────────

function ownershipIcon(type: string) {
  switch (type) {
    case 'System': return <Shield size={12} className="text-accent" />;
    case 'Tenant': return <Users size={12} className="text-info" />;
    case 'User': return <User size={12} className="text-success" />;
    default: return <Bot size={12} className="text-muted" />;
  }
}

function visibilityIcon(vis: string) {
  switch (vis) {
    case 'Private': return <Lock size={12} className="text-warning" />;
    case 'Team': return <Users size={12} className="text-info" />;
    case 'Tenant': return <Globe size={12} className="text-success" />;
    default: return null;
  }
}

function statusVariant(status: string): 'success' | 'warning' | 'default' | 'info' {
  switch (status) {
    case 'Published':
    case 'Active': return 'success';
    case 'Draft': return 'default';
    case 'PendingReview': return 'info';
    case 'Archived': return 'warning';
    case 'Blocked': return 'warning';
    default: return 'default';
  }
}

// ── Main Page ───────────────────────────────────────────────────────────

/**
 * Página de gestão de AI Agents — catálogo oficial, agents de utilizador,
 * execução governada e revisão de artefactos.
 *
 * @see docs/AI-ARCHITECTURE.md
 */
export function AiAgentsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { toastSuccess } = useToast();

  const [searchTerm, setSearchTerm] = useState('');
  const [filter, setFilter] = useState<'all' | 'official' | 'custom'>('all');
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [executeAgent, setExecuteAgent] = useState<AgentListItem | null>(null);

  const isOfficial = filter === 'official' ? true : filter === 'custom' ? false : undefined;

  const agentsQuery = useQuery({
    queryKey: ['ai-agents', isOfficial],
    queryFn: () => aiGovernanceApi.listAgents({ isOfficial }) as Promise<AgentsResponse>,
    staleTime: 20_000,
  });

  const agentCategoriesQuery = useQuery({
    queryKey: ['ai-agent-categories'],
    queryFn: () => aiGovernanceApi.listAgentCategories(),
    staleTime: 5 * 60_000,
  });

  const agents = useMemo(() => agentsQuery.data?.items ?? [], [agentsQuery.data?.items]);

  const availableCategories = useMemo(() => {
    const fromEndpoint = agentCategoriesQuery.data?.items ?? [];
    const fromAgents = agents.map((agent) => agent.category);
    const merged = [...fromEndpoint, ...fromAgents, ...FALLBACK_AGENT_CATEGORIES]
      .filter((value): value is string => Boolean(value && value.trim()));
    return Array.from(new Set(merged));
  }, [agentCategoriesQuery.data?.items, agents]);

  const defaultCategory = availableCategories[0] ?? 'General';

  const handleAgentCreated = async () => {
    await agentsQuery.refetch();
    toastSuccess(t('agents.createSuccess'));
  };

  const filteredAgents = agents.filter(a => {
    if (!searchTerm) return true;
    const term = searchTerm.toLowerCase();
    return (
      a.displayName.toLowerCase().includes(term) ||
      a.name.toLowerCase().includes(term) ||
      a.description.toLowerCase().includes(term) ||
      a.category.toLowerCase().includes(term)
    );
  });

  const officialAgents = filteredAgents.filter(a => a.ownershipType === 'System');
  const customAgents = filteredAgents.filter(a => a.ownershipType !== 'System');

  if (agentsQuery.isError) {
    return <PageContainer><PageErrorState /></PageContainer>;
  }

  return (
    <PageContainer>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
              <Bot size={24} className="text-accent" />
              {t('agents.title')}
            </h1>
            <p className="text-sm text-muted mt-1">{t('agents.subtitle')}</p>
          </div>
          <Button variant="primary" size="sm" onClick={() => setIsCreateOpen(true)}>
            <Plus size={14} className="mr-1" />
            {t('agents.createAgent')}
          </Button>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3">
          <div className="relative flex-1 max-w-sm">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
              placeholder={t('agents.searchPlaceholder')}
              aria-label={t('agents.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 rounded-md border border-edge bg-elevated text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          <div className="flex items-center gap-1 rounded-md border border-edge bg-elevated p-0.5" role="group" aria-label={t('aiHub.filterByStatus')}>
            {(['all', 'official', 'custom'] as const).map(f => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                aria-pressed={filter === f}
                className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
                  filter === f ? 'bg-accent text-white' : 'text-muted hover:text-body'
                }`}
              >
                {t(`agents.filter.${f}`)}
              </button>
            ))}
          </div>
        </div>

        {/* Loading */}
        {agentsQuery.isLoading && (
          <CardListSkeleton count={4} showStats={false} />
        )}

        {/* Error */}
        {agentsQuery.isError && !agentsQuery.isLoading && (
          <div className="text-center py-12">
            <AlertCircle size={32} className="text-warning mx-auto mb-3" />
            <p className="text-sm text-muted">{t('agents.loadError')}</p>
            <Button variant="ghost" size="sm" className="mt-3" onClick={() => agentsQuery.refetch()}>
              {t('common.retry')}
            </Button>
          </div>
        )}

        {/* Empty */}
        {!agentsQuery.isLoading && !agentsQuery.isError && filteredAgents.length === 0 && (
          <EmptyState
            icon={<Inbox size={20} />}
            title={t('agents.emptyTitle', 'No agents found')}
            description={t('agents.emptyDescription', 'Try adjusting your filters or create a new agent.')}
          />
        )}

        {/* Official Agents */}
        {!agentsQuery.isLoading && !agentsQuery.isError && officialAgents.length > 0 && (
          <div>
            <h2 className="text-sm font-semibold text-heading mb-3 flex items-center gap-2">
              <Shield size={14} className="text-accent" />
              {t('agents.officialSection')} ({officialAgents.length})
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {officialAgents.map(agent => (
                <AgentCard
                  key={agent.agentId}
                  agent={agent}
                  onView={() => navigate(`/ai/agents/${agent.agentId}`)}
                  onExecute={() => setExecuteAgent(agent)}
                  t={t}
                />
              ))}
            </div>
          </div>
        )}

        {/* Custom Agents */}
        {!agentsQuery.isLoading && !agentsQuery.isError && customAgents.length > 0 && (
          <div>
            <h2 className="text-sm font-semibold text-heading mb-3 flex items-center gap-2">
              <Sparkles size={14} className="text-info" />
              {t('agents.customSection')} ({customAgents.length})
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {customAgents.map(agent => (
                <AgentCard
                  key={agent.agentId}
                  agent={agent}
                  onView={() => navigate(`/ai/agents/${agent.agentId}`)}
                  onExecute={() => setExecuteAgent(agent)}
                  t={t}
                />
              ))}
            </div>
          </div>
        )}

        {/* Governance Notice */}
        <div className="mt-6 px-4 py-3 rounded-md bg-accent/5 border border-accent/20 text-xs text-muted flex items-center gap-2">
          <Shield size={14} className="text-accent shrink-0" />
          {t('agents.governanceNotice')}
        </div>
      </div>

      {/* Dialogs */}
      <CreateAgentDialog
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onCreated={handleAgentCreated}
        categories={availableCategories}
        defaultCategory={defaultCategory}
      />
      <ExecuteAgentDialog
        isOpen={!!executeAgent}
        agent={executeAgent}
        onClose={() => setExecuteAgent(null)}
      />
    </PageContainer>
  );
}

// ── Agent Card Component ────────────────────────────────────────────────

interface AgentCardProps {
  agent: AgentListItem;
  onView: () => void;
  onExecute: () => void;
  t: (key: string) => string;
}

function AgentCard({ agent, onView, onExecute, t }: AgentCardProps) {
  return (
    <div className="bg-card border border-edge rounded-lg p-4 hover:border-accent/30 transition-colors">
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-lg bg-accent/20 flex items-center justify-center text-sm">
            {agent.icon || <Bot size={16} className="text-accent" />}
          </div>
          <div>
            <h3 className="text-sm font-semibold text-heading leading-tight">{agent.displayName}</h3>
            <span className="text-[10px] text-muted">{agent.slug}</span>
          </div>
        </div>
        <Badge variant={statusVariant(agent.publicationStatus)}>
          {t(`agents.status.${agent.publicationStatus}`)}
        </Badge>
      </div>

      <p className="text-xs text-muted line-clamp-2 mb-3">{agent.description}</p>

      <div className="flex items-center gap-2 flex-wrap mb-3">
        <Badge variant="default">
          {ownershipIcon(agent.ownershipType)}
          <span className="ml-0.5">{t(`agents.ownership.${agent.ownershipType}`)}</span>
        </Badge>
        <Badge variant="default">
          {visibilityIcon(agent.visibility)}
          <span className="ml-0.5">{t(`agents.visibility.${agent.visibility}`)}</span>
        </Badge>
        <Badge variant="info">{t(`agents.category.${agent.category}`) || humanizeEnumValue(agent.category)}</Badge>
      </div>

      <div className="flex items-center justify-between text-[10px] text-muted mb-3">
        <span>v{agent.version}</span>
        <span>{agent.executionCount} {t('agents.executions')}</span>
      </div>

      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" onClick={onView} className="flex-1">
          <Eye size={12} className="mr-1" /> {t('agents.view')}
        </Button>
        {agent.isActive && (agent.publicationStatus === 'Published' || agent.publicationStatus === 'Active') && (
          <Button variant="primary" size="sm" onClick={onExecute} className="flex-1">
            <Play size={12} className="mr-1" /> {t('agents.execute')}
          </Button>
        )}
      </div>
    </div>
  );
}
