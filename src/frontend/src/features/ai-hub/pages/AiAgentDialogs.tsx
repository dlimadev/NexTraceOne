import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlertCircle, CheckCircle2, Ban, Loader2, Play, Cpu,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { aiGovernanceApi } from '../api/aiGovernance';
import { humanizeEnumValue } from './AiAgentTypes';
import type {
  CreateAgentDialogProps, ExecuteAgentDialogProps,
  ExecutionResult, ArtifactResult,
} from './AiAgentTypes';

// ── Create Agent Dialog ───────────────────────────────────────────────────────

/**
 * Diálogo de criação de AI Agent customizado pelo utilizador.
 */
export function CreateAgentDialog({
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
                placeholder={t('aiHub.agentPlaceholder.agentName', 'my-custom-agent')}
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

// ── Execute Agent Dialog ──────────────────────────────────────────────────────

/**
 * Diálogo de execução governada de um AI Agent.
 */
export function ExecuteAgentDialog({ isOpen, agent, onClose }: ExecuteAgentDialogProps) {
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
          artifacts: result.artifacts.map((a: ArtifactResult) =>
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
                    {result.artifacts.map((artifact: ArtifactResult) => (
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
