import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Bot,
  ArrowLeft,
  Play,
  FileEdit,
  Shield,
  Users,
  User,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Ban,
  Archive,
  Eye,
  Cpu,
  Globe,
  Lock,
  Clock,
  Hash,
  Sparkles,
  Save,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { aiGovernanceApi } from '../api/aiGovernance';

// ── Types ───────────────────────────────────────────────────────────────

interface AgentDetail {
  agentId: string;
  name: string;
  displayName: string;
  slug: string;
  description: string;
  category: string;
  systemPrompt: string;
  capabilities: string;
  targetPersona: string;
  icon: string;
  preferredModelId: string | null;
  isOfficial: boolean;
  isActive: boolean;
  ownershipType: string;
  visibility: string;
  publicationStatus: string;
  ownerId: string;
  ownerTeamId: string;
  allowedModelIds: string;
  allowedTools: string;
  objective: string;
  inputSchema: string;
  outputSchema: string;
  allowModelOverride: boolean;
  version: number;
  executionCount: number;
  sortOrder: number;
  createdAt: string;
  updatedAt: string | null;
}

interface ExecutionSummary {
  executionId: string;
  status: string;
  modelUsed: string;
  durationMs: number;
  totalTokens: number;
  artifactCount: number;
  startedAt: string;
}

/**
 * Página de detalhe de um AI Agent — visualização, edição, execução e histórico.
 */
export function AgentDetailPage() {
  const { t } = useTranslation();
  const { agentId } = useParams<{ agentId: string }>();
  const navigate = useNavigate();

  const [agent, setAgent] = useState<AgentDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'definition' | 'edit'>('overview');

  // ── Edit state ──────────────────────────────────────────────────────
  const [editForm, setEditForm] = useState({
    displayName: '',
    description: '',
    systemPrompt: '',
    objective: '',
    capabilities: '',
    targetPersona: '',
    visibility: '',
    allowModelOverride: false,
  });
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);

  // ── Execute state ─────────────────────────────────────────────────
  const [isExecuteOpen, setIsExecuteOpen] = useState(false);
  const [executeInput, setExecuteInput] = useState('');
  const [isExecuting, setIsExecuting] = useState(false);
  const [executeError, setExecuteError] = useState<string | null>(null);
  const [executeResult, setExecuteResult] = useState<{
    executionId: string;
    status: string;
    output: string;
    modelUsed: string;
    providerUsed: string;
    promptTokens: number;
    completionTokens: number;
    totalTokens: number;
    durationMs: number;
    artifacts: Array<{
      artifactId: string;
      title: string;
      artifactType: string;
      content: string;
      reviewStatus: string;
    }>;
  } | null>(null);

  const loadAgent = useCallback(async () => {
    if (!agentId) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await aiGovernanceApi.getAgent(agentId);
      setAgent(data);
      setEditForm({
        displayName: data.displayName,
        description: data.description,
        systemPrompt: data.systemPrompt,
        objective: data.objective || '',
        capabilities: data.capabilities || '',
        targetPersona: data.targetPersona || '',
        visibility: data.visibility,
        allowModelOverride: data.allowModelOverride,
      });
    } catch {
      setError(t('agents.detailLoadError'));
    } finally {
      setIsLoading(false);
    }
  }, [agentId, t]);

  useEffect(() => {
    void loadAgent();
  }, [loadAgent]);

  const handleSave = async () => {
    if (!agentId) return;
    setIsSaving(true);
    setSaveError(null);
    try {
      await aiGovernanceApi.updateAgent(agentId, editForm);
      await loadAgent();
      setActiveTab('overview');
    } catch {
      setSaveError(t('agents.updateError'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleExecute = async () => {
    if (!agentId || !executeInput.trim()) return;
    setIsExecuting(true);
    setExecuteError(null);
    setExecuteResult(null);
    try {
      const data = await aiGovernanceApi.executeAgent(agentId, { input: executeInput.trim() });
      setExecuteResult(data);
    } catch {
      setExecuteError(t('agents.executeError'));
    } finally {
      setIsExecuting(false);
    }
  };

  const handleReviewArtifact = async (artifactId: string, decision: string) => {
    try {
      await aiGovernanceApi.reviewArtifact(artifactId, { decision });
      if (executeResult) {
        setExecuteResult({
          ...executeResult,
          artifacts: executeResult.artifacts.map(a =>
            a.artifactId === artifactId ? { ...a, reviewStatus: decision === 'Approve' ? 'Approved' : 'Rejected' } : a,
          ),
        });
      }
    } catch {
      // silent
    }
  };

  if (isLoading) {
    return (
      <PageContainer>
        <div className="flex items-center justify-center py-24">
          <Loader2 size={24} className="animate-spin text-muted" />
        </div>
      </PageContainer>
    );
  }

  if (error || !agent) {
    return (
      <PageContainer>
        <div className="text-center py-16">
          <AlertCircle size={32} className="text-warning mx-auto mb-3" />
          <p className="text-sm text-muted">{error || t('agents.detailNotFound')}</p>
          <Button variant="ghost" size="sm" className="mt-3" onClick={() => navigate('/ai/agents')}>
            <ArrowLeft size={14} className="mr-1" /> {t('agents.backToList')}
          </Button>
        </div>
      </PageContainer>
    );
  }

  const isSystem = agent.ownershipType === 'System';
  const isPublished = agent.publicationStatus === 'Published' || agent.publicationStatus === 'Active';

  return (
    <PageContainer>
      <div className="space-y-6">
        {/* Back + Header */}
        <div>
          <button onClick={() => navigate('/ai/agents')} className="text-xs text-muted hover:text-body flex items-center gap-1 mb-3">
            <ArrowLeft size={12} /> {t('agents.backToList')}
          </button>
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-accent/20 flex items-center justify-center text-lg">
                {agent.icon || <Bot size={20} className="text-accent" />}
              </div>
              <div>
                <h1 className="text-xl font-bold text-heading">{agent.displayName}</h1>
                <div className="flex items-center gap-2 mt-0.5">
                  <span className="text-xs text-muted">{agent.slug}</span>
                  <span className="text-xs text-muted">·</span>
                  <span className="text-xs text-muted">v{agent.version}</span>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-2">
              {!isSystem && (
                <Button variant="ghost" size="sm" onClick={() => setActiveTab('edit')}>
                  <FileEdit size={14} className="mr-1" /> {t('agents.edit')}
                </Button>
              )}
              {agent.isActive && isPublished && (
                <Button variant="primary" size="sm" onClick={() => setIsExecuteOpen(true)}>
                  <Play size={14} className="mr-1" /> {t('agents.execute')}
                </Button>
              )}
            </div>
          </div>
        </div>

        {/* Status badges */}
        <div className="flex items-center gap-2 flex-wrap">
          <Badge variant={isPublished ? 'success' : 'default'}>
            {t(`agents.status.${agent.publicationStatus}`)}
          </Badge>
          <Badge variant="default">
            {isSystem ? <Shield size={10} className="mr-0.5" /> : agent.ownershipType === 'Tenant' ? <Users size={10} className="mr-0.5" /> : <User size={10} className="mr-0.5" />}
            {t(`agents.ownership.${agent.ownershipType}`)}
          </Badge>
          <Badge variant="default">
            {agent.visibility === 'Private' ? <Lock size={10} className="mr-0.5" /> : agent.visibility === 'Team' ? <Users size={10} className="mr-0.5" /> : <Globe size={10} className="mr-0.5" />}
            {t(`agents.visibility.${agent.visibility}`)}
          </Badge>
          <Badge variant="info">{t(`agents.category.${agent.category}`)}</Badge>
          {agent.isActive ? (
            <Badge variant="success"><CheckCircle2 size={10} className="mr-0.5" /> {t('agents.active')}</Badge>
          ) : (
            <Badge variant="warning"><Ban size={10} className="mr-0.5" /> {t('agents.inactive')}</Badge>
          )}
        </div>

        {/* Tabs */}
        <div className="border-b border-edge flex items-center gap-4">
          {(['overview', 'definition', ...(isSystem ? [] : ['edit'])] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab as 'overview' | 'definition' | 'edit')}
              className={`pb-2 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab ? 'border-accent text-accent' : 'border-transparent text-muted hover:text-body'
              }`}
            >
              {t(`agents.tab.${tab}`)}
            </button>
          ))}
        </div>

        {/* Overview Tab */}
        {activeTab === 'overview' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 space-y-4">
              <div className="bg-card border border-edge rounded-lg p-4">
                <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.descriptionLabel')}</h3>
                <p className="text-sm text-body">{agent.description}</p>
              </div>
              {agent.objective && (
                <div className="bg-card border border-edge rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.objectiveLabel')}</h3>
                  <p className="text-sm text-body">{agent.objective}</p>
                </div>
              )}
              {agent.capabilities && (
                <div className="bg-card border border-edge rounded-lg p-4">
                  <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.capabilitiesLabel')}</h3>
                  <div className="flex items-center gap-1.5 flex-wrap">
                    {agent.capabilities.split(',').map(c => (
                      <Badge key={c} variant="default">{c.trim()}</Badge>
                    ))}
                  </div>
                </div>
              )}
            </div>
            <div className="space-y-4">
              <div className="bg-card border border-edge rounded-lg p-4 space-y-3">
                <h3 className="text-sm font-semibold text-heading">{t('agents.statsLabel')}</h3>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted">{t('agents.totalExecutions')}</span>
                  <span className="text-heading font-medium">{agent.executionCount.toLocaleString()}</span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted">{t('agents.versionLabel')}</span>
                  <span className="text-heading font-medium">v{agent.version}</span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted">{t('agents.modelOverride')}</span>
                  <span className="text-heading font-medium">{agent.allowModelOverride ? t('common.yes') : t('common.no')}</span>
                </div>
                {agent.targetPersona && (
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted">{t('agents.targetPersonaLabel')}</span>
                    <span className="text-heading font-medium">{agent.targetPersona}</span>
                  </div>
                )}
              </div>
              <div className="bg-card border border-edge rounded-lg p-4 space-y-2">
                <h3 className="text-sm font-semibold text-heading">{t('agents.metadataLabel')}</h3>
                <div className="text-xs text-muted">
                  <div className="flex items-center gap-1"><Clock size={10} /> {t('agents.createdAt')}: {new Date(agent.createdAt).toLocaleDateString()}</div>
                  {agent.updatedAt && (
                    <div className="flex items-center gap-1 mt-1"><Clock size={10} /> {t('agents.updatedAt')}: {new Date(agent.updatedAt).toLocaleDateString()}</div>
                  )}
                  <div className="flex items-center gap-1 mt-1"><Hash size={10} /> {t('agents.ownerId')}: {agent.ownerId}</div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Definition Tab */}
        {activeTab === 'definition' && (
          <div className="space-y-4">
            <div className="bg-card border border-edge rounded-lg p-4">
              <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.systemPromptLabel')}</h3>
              <div className="rounded border border-edge bg-elevated p-3 text-xs text-body whitespace-pre-wrap font-mono max-h-[400px] overflow-y-auto">
                {agent.systemPrompt}
              </div>
            </div>
            {agent.inputSchema && (
              <div className="bg-card border border-edge rounded-lg p-4">
                <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.inputSchemaLabel')}</h3>
                <div className="rounded border border-edge bg-elevated p-3 text-xs text-body whitespace-pre-wrap font-mono max-h-[200px] overflow-y-auto">
                  {agent.inputSchema}
                </div>
              </div>
            )}
            {agent.outputSchema && (
              <div className="bg-card border border-edge rounded-lg p-4">
                <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.outputSchemaLabel')}</h3>
                <div className="rounded border border-edge bg-elevated p-3 text-xs text-body whitespace-pre-wrap font-mono max-h-[200px] overflow-y-auto">
                  {agent.outputSchema}
                </div>
              </div>
            )}
            {agent.allowedModelIds && (
              <div className="bg-card border border-edge rounded-lg p-4">
                <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.allowedModelsLabel')}</h3>
                <div className="flex items-center gap-1.5 flex-wrap">
                  {agent.allowedModelIds.split(',').map(id => (
                    <Badge key={id} variant="default"><Cpu size={10} className="mr-0.5" />{id.trim()}</Badge>
                  ))}
                </div>
              </div>
            )}
            {agent.allowedTools && (
              <div className="bg-card border border-edge rounded-lg p-4">
                <h3 className="text-sm font-semibold text-heading mb-2">{t('agents.allowedToolsLabel')}</h3>
                <div className="flex items-center gap-1.5 flex-wrap">
                  {agent.allowedTools.split(',').map(tool => (
                    <Badge key={tool} variant="default">{tool.trim()}</Badge>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Edit Tab */}
        {activeTab === 'edit' && !isSystem && (
          <div className="space-y-4 max-w-2xl">
            {saveError && (
              <div className="p-3 rounded-md bg-red-500/10 border border-red-500/30 text-sm text-red-400 flex items-center gap-2">
                <AlertCircle size={16} /> {saveError}
              </div>
            )}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldDisplayName')}</label>
                <input
                  type="text"
                  value={editForm.displayName}
                  onChange={e => setEditForm(f => ({ ...f, displayName: e.target.value }))}
                  className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldVisibility')}</label>
                <select
                  value={editForm.visibility}
                  onChange={e => setEditForm(f => ({ ...f, visibility: e.target.value }))}
                  className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent"
                >
                  <option value="Private">{t('agents.visibility.Private')}</option>
                  <option value="Team">{t('agents.visibility.Team')}</option>
                  <option value="Tenant">{t('agents.visibility.Tenant')}</option>
                </select>
              </div>
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldDescription')}</label>
              <textarea
                value={editForm.description}
                onChange={e => setEditForm(f => ({ ...f, description: e.target.value }))}
                rows={3}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent resize-none"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldObjective')}</label>
              <input
                type="text"
                value={editForm.objective}
                onChange={e => setEditForm(f => ({ ...f, objective: e.target.value }))}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-muted mb-1">{t('agents.fieldSystemPrompt')}</label>
              <textarea
                value={editForm.systemPrompt}
                onChange={e => setEditForm(f => ({ ...f, systemPrompt: e.target.value }))}
                rows={8}
                className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body focus:outline-none focus:ring-1 focus:ring-accent resize-none font-mono text-xs"
              />
            </div>
            <div className="flex items-center gap-3">
              <label className="flex items-center gap-2 text-sm text-body cursor-pointer">
                <input
                  type="checkbox"
                  checked={editForm.allowModelOverride}
                  onChange={e => setEditForm(f => ({ ...f, allowModelOverride: e.target.checked }))}
                  className="rounded border-edge"
                />
                {t('agents.allowModelOverrideLabel')}
              </label>
            </div>
            <div className="flex justify-end gap-3 pt-2">
              <Button variant="ghost" size="sm" onClick={() => setActiveTab('overview')} disabled={isSaving}>
                {t('common.cancel')}
              </Button>
              <Button variant="primary" size="sm" onClick={handleSave} disabled={isSaving}>
                {isSaving ? <Loader2 size={14} className="animate-spin mr-1" /> : <Save size={14} className="mr-1" />}
                {t('agents.saveChanges')}
              </Button>
            </div>
          </div>
        )}

        {/* Execute Section */}
        {isExecuteOpen && (
          <div className="bg-card border border-accent/30 rounded-lg p-4 space-y-4">
            <h3 className="text-sm font-semibold text-heading flex items-center gap-2">
              <Play size={14} className="text-accent" />
              {t('agents.executeTitle')} — {agent.displayName}
            </h3>
            {executeError && (
              <div className="p-3 rounded-md bg-red-500/10 border border-red-500/30 text-sm text-red-400 flex items-center gap-2">
                <AlertCircle size={16} /> {executeError}
              </div>
            )}
            {!executeResult && (
              <>
                <textarea
                  value={executeInput}
                  onChange={e => setExecuteInput(e.target.value)}
                  rows={4}
                  placeholder={t('agents.inputPlaceholder')}
                  className="w-full rounded-md border border-edge bg-elevated px-3 py-2 text-sm text-body placeholder-muted focus:outline-none focus:ring-1 focus:ring-accent resize-none"
                />
                <div className="flex justify-end gap-2">
                  <Button variant="ghost" size="sm" onClick={() => { setIsExecuteOpen(false); setExecuteInput(''); }}>
                    {t('common.cancel')}
                  </Button>
                  <Button variant="primary" size="sm" onClick={handleExecute} disabled={isExecuting || !executeInput.trim()}>
                    {isExecuting ? <Loader2 size={14} className="animate-spin mr-1" /> : <Play size={14} className="mr-1" />}
                    {t('agents.executeButton')}
                  </Button>
                </div>
              </>
            )}
            {executeResult && (
              <div className="space-y-4">
                <div className="flex items-center gap-3 flex-wrap">
                  <Badge variant={executeResult.status === 'Completed' ? 'success' : 'warning'}>{executeResult.status}</Badge>
                  <span className="text-xs text-muted"><Cpu size={10} className="inline mr-0.5" /> {executeResult.modelUsed}</span>
                  <span className="text-xs text-muted">{executeResult.durationMs}ms</span>
                  <span className="text-xs text-muted">{executeResult.totalTokens} {t('agents.tokens')}</span>
                </div>
                <div className="rounded border border-edge bg-elevated p-3 text-xs text-body whitespace-pre-wrap font-mono max-h-[300px] overflow-y-auto">
                  {executeResult.output}
                </div>
                {executeResult.artifacts.length > 0 && (
                  <div>
                    <h4 className="text-xs font-semibold text-heading mb-2">{t('agents.artifacts')} ({executeResult.artifacts.length})</h4>
                    {executeResult.artifacts.map(art => (
                      <div key={art.artifactId} className="rounded border border-edge bg-elevated p-3 mb-2">
                        <div className="flex items-center justify-between mb-2">
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium text-heading">{art.title}</span>
                            <Badge variant="default">{art.artifactType}</Badge>
                            <Badge variant={art.reviewStatus === 'Approved' ? 'success' : art.reviewStatus === 'Rejected' ? 'warning' : 'default'}>
                              {art.reviewStatus}
                            </Badge>
                          </div>
                          {art.reviewStatus === 'Pending' && (
                            <div className="flex items-center gap-1">
                              <Button variant="ghost" size="sm" onClick={() => handleReviewArtifact(art.artifactId, 'Approve')}>
                                <CheckCircle2 size={12} className="text-green-400 mr-0.5" /> {t('agents.approve')}
                              </Button>
                              <Button variant="ghost" size="sm" onClick={() => handleReviewArtifact(art.artifactId, 'Reject')}>
                                <Ban size={12} className="text-red-400 mr-0.5" /> {t('agents.reject')}
                              </Button>
                            </div>
                          )}
                        </div>
                        <div className="rounded border border-edge bg-card p-2 text-xs text-body whitespace-pre-wrap font-mono max-h-[200px] overflow-y-auto">
                          {art.content}
                        </div>
                      </div>
                    ))}
                  </div>
                )}
                <div className="flex justify-end">
                  <Button variant="ghost" size="sm" onClick={() => { setIsExecuteOpen(false); setExecuteResult(null); setExecuteInput(''); }}>
                    {t('common.close')}
                  </Button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </PageContainer>
  );
}
