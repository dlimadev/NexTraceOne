import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import {
  Bot,
  Play,
  Zap,
  User,
  ChevronRight,
  Loader2,
  AlertCircle,
  CheckCircle2,
  Clock,
  FileText,
  ExternalLink,
} from 'lucide-react';
import { cn } from '../../../../lib/cn';
import { Badge } from '../../../../components/Badge';
import { Button } from '../../../../components/Button';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { aiGovernanceApi } from '../../../ai-hub/api/aiGovernance';
import type { StudioContract } from '../studioTypes';

// ── Types ────────────────────────────────────────────────────────────────────

interface AgentItem {
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
}

interface ArtifactItem {
  artifactId: string;
  artifactType: string;
  title: string;
  format: string;
}

interface ExecutionResult {
  executionId: string;
  agentId: string;
  agentName: string;
  status: string;
  output: string;
  promptTokens: number;
  completionTokens: number;
  durationMs: number;
  artifacts: ArtifactItem[];
}

interface AiAgentsSectionProps {
  contract: StudioContract;
  className?: string;
}

// ── Constants ────────────────────────────────────────────────────────────────

/** Maximum number of capability tags displayed per agent card. */
const MAX_DISPLAYED_CAPABILITIES = 3;

/**
 * Derivação do contexto de módulo a partir do protocolo e tipo de serviço do contrato.
 */
function deriveModuleContext(protocol: string, serviceType: string): string {
  const p = (protocol ?? '').toLowerCase();
  const s = (serviceType ?? '').toLowerCase();

  if (p === 'wsdl' || s === 'soap') return 'soap';
  if (p === 'asyncapi' || s === 'kafkaproducer' || s === 'kafkaconsumer' || s === 'event')
    return 'kafka';
  if (p === 'openapi' || p === 'swagger' || s === 'restapi') return 'rest-api';
  return 'rest-api';
}

/**
 * Secção de AI Agents do studio de contratos.
 * Exibe agents recomendados para o contexto do contrato actual
 * e permite executar agents com input contextual.
 */
export function AiAgentsSection({ contract, className }: AiAgentsSectionProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const moduleContext = deriveModuleContext(contract.protocol, contract.serviceType);

  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [selectedAgent, setSelectedAgent] = useState<AgentItem | null>(null);
  const [agentInput, setAgentInput] = useState('');
  const [isExecuting, setIsExecuting] = useState(false);
  const [executionResult, setExecutionResult] = useState<ExecutionResult | null>(null);
  const [executionError, setExecutionError] = useState<string | null>(null);

  // ── Load contextual agents ──────────────────────────────────────────────

  useEffect(() => {
    setIsLoading(true);
    setLoadError(null);

    aiGovernanceApi.listAgentsByContext(moduleContext)
      .then((data: { items: AgentItem[]; totalCount: number }) => {
        setAgents(data.items ?? []);
      })
      .catch(() => {
        setLoadError(t('contracts.workspace.aiAgentsLoadError', 'Failed to load AI agents.'));
      })
      .finally(() => setIsLoading(false));
  }, [moduleContext, t]);

  // ── Execute agent ────────────────────────────────────────────────────────

  const handleExecute = useCallback(async () => {
    if (!selectedAgent || !agentInput.trim()) return;

    setIsExecuting(true);
    setExecutionError(null);
    setExecutionResult(null);

    // Build contextual input enriched with contract info.
    // Note: these field labels are intentionally in English — they are part
    // of the AI model prompt, not user-visible UI strings.
    const contextualInput = `Contract: ${contract.friendlyName} (${contract.technicalName})
Protocol: ${contract.protocol}
Service Type: ${contract.serviceType}
Version: ${contract.semVer}
Description: ${contract.functionalDescription || '(none)'}

---

${agentInput.trim()}`;

    try {
      const result = await aiGovernanceApi.executeAgent(selectedAgent.agentId, {
        input: contextualInput,
        contextJson: JSON.stringify({
          contractId: contract.id,
          contractName: contract.technicalName,
          protocol: contract.protocol,
          serviceType: contract.serviceType,
          moduleContext,
        }),
      });

      setExecutionResult(result as ExecutionResult);
    } catch {
      setExecutionError(t('contracts.workspace.aiAgentsExecuteError', 'Agent execution failed. Please try again.'));
    } finally {
      setIsExecuting(false);
    }
  }, [selectedAgent, agentInput, contract, moduleContext, t]);

  const handleClearExecution = useCallback(() => {
    setExecutionResult(null);
    setExecutionError(null);
    setAgentInput('');
  }, []);

  // ── Render loading state ──────────────────────────────────────────────────

  if (isLoading) {
    return (
      <div className={cn('space-y-4', className)}>
        <div className="flex items-center justify-center py-12">
          <Loader2 size={20} className="animate-spin text-accent mr-2" />
          <span className="text-sm text-muted">{t('contracts.workspace.aiAgentsLoading', 'Loading AI agents...')}</span>
        </div>
      </div>
    );
  }

  // ── Render error state ────────────────────────────────────────────────────

  if (loadError) {
    return (
      <div className={cn('space-y-4', className)}>
        <div className="flex items-center gap-2 p-4 rounded-lg bg-danger/10 border border-danger/20">
          <AlertCircle size={16} className="text-danger shrink-0" />
          <span className="text-sm text-danger">{loadError}</span>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('space-y-6', className)}>
      {/* ── Header ─────────────────────────────────────────────────────── */}
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-sm font-semibold text-heading">
            {t('contracts.workspace.aiAgentsTitle', 'AI Agents')}
          </h2>
          <p className="text-xs text-muted mt-1">
            {t('contracts.workspace.aiAgentsSubtitle', {
              context: moduleContext,
              defaultValue: `Recommended agents for ${moduleContext} contracts. Select an agent and provide context to generate governed artifacts.`,
            })}
          </p>
        </div>
        <button
          type="button"
          onClick={() => navigate('/ai/assistant')}
          className="text-xs text-accent hover:underline flex items-center gap-1"
        >
          {t('contracts.workspace.aiAgentsOpenAssistant', 'Open AI Assistant')}
          <ExternalLink size={10} />
        </button>
      </div>

      {agents.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <Bot size={32} className="text-muted/40 mb-3" />
          <p className="text-sm text-muted">
            {t('contracts.workspace.aiAgentsEmpty', 'No AI agents available for this contract type.')}
          </p>
        </div>
      ) : (
        <>
          {/* ── Agent cards ─────────────────────────────────────────────── */}
          <div className="grid grid-cols-1 gap-3">
            {agents.map((agent) => (
              <AgentCard
                key={agent.agentId}
                agent={agent}
                isSelected={selectedAgent?.agentId === agent.agentId}
                onSelect={() => {
                  setSelectedAgent(agent);
                  setExecutionResult(null);
                  setExecutionError(null);
                  setAgentInput('');
                }}
              />
            ))}
          </div>

          {/* ── Execution panel ──────────────────────────────────────────── */}
          {selectedAgent && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <span className="text-base">{selectedAgent.icon || '🤖'}</span>
                  <span className="text-sm font-medium text-heading">{selectedAgent.displayName}</span>
                  {selectedAgent.isOfficial && (
                    <Badge variant="info">
                      <Zap size={8} className="mr-0.5" />
                      {t('aiHub.officialAgent', 'Official')}
                    </Badge>
                  )}
                </div>
              </CardHeader>
              <CardBody>
                <div className="space-y-3">
                  <div>
                    <label
                      htmlFor="agent-input"
                      className="block text-xs font-medium text-muted mb-1.5"
                    >
                      {t('contracts.workspace.aiAgentsInputLabel', 'Describe what you need')}
                    </label>
                    <textarea
                      id="agent-input"
                      value={agentInput}
                      onChange={(e) => setAgentInput(e.target.value)}
                      placeholder={t('contracts.workspace.aiAgentsInputPlaceholder', 'e.g. Generate a complete OpenAPI spec for this payment API with authentication endpoints...')}
                      rows={4}
                      className="w-full px-3 py-2 text-sm bg-elevated border border-edge rounded-lg text-body placeholder:text-muted/50 resize-none focus:outline-none focus:ring-1 focus:ring-accent/50 focus:border-accent/50"
                    />
                  </div>

                  <div className="flex items-center justify-between">
                    <p className="text-[10px] text-muted">
                      {t('contracts.workspace.aiAgentsContextNote', 'Contract context is automatically included.')}
                    </p>
                    <div className="flex items-center gap-2">
                      {executionResult && (
                        <button
                          type="button"
                          onClick={handleClearExecution}
                          className="text-xs text-muted hover:text-body"
                        >
                          {t('common.clear', 'Clear')}
                        </button>
                      )}
                      <Button
                        type="button"
                        size="sm"
                        onClick={handleExecute}
                        disabled={!agentInput.trim() || isExecuting}
                      >
                        {isExecuting ? (
                          <>
                            <Loader2 size={12} className="animate-spin mr-1" />
                            {t('contracts.workspace.aiAgentsRunning', 'Running...')}
                          </>
                        ) : (
                          <>
                            <Play size={12} className="mr-1" />
                            {t('contracts.workspace.aiAgentsRun', 'Run Agent')}
                          </>
                        )}
                      </Button>
                    </div>
                  </div>

                  {/* Execution error */}
                  {executionError && (
                    <div className="flex items-start gap-2 p-3 rounded-lg bg-danger/10 border border-danger/20">
                      <AlertCircle size={14} className="text-danger shrink-0 mt-0.5" />
                      <span className="text-xs text-danger">{executionError}</span>
                    </div>
                  )}

                  {/* Execution result */}
                  {executionResult && (
                    <ExecutionResultPanel result={executionResult} />
                  )}
                </div>
              </CardBody>
            </Card>
          )}
        </>
      )}
    </div>
  );
}

// ── Sub-components ───────────────────────────────────────────────────────────

interface AgentCardProps {
  agent: AgentItem;
  isSelected: boolean;
  onSelect: () => void;
}

function AgentCard({ agent, isSelected, onSelect }: AgentCardProps) {
  const { t } = useTranslation();

  return (
    <button
      type="button"
      onClick={onSelect}
      className={cn(
        'w-full text-left px-4 py-3 rounded-lg border transition-all',
        isSelected
          ? 'border-accent bg-accent/5 ring-1 ring-accent/20'
          : 'border-edge bg-card hover:bg-hover hover:border-edge/80',
      )}
    >
      <div className="flex items-start gap-3">
        <span className="text-xl leading-none mt-0.5">{agent.icon || '🤖'}</span>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className="text-sm font-medium text-heading">{agent.displayName}</span>
            {agent.isOfficial && (
              <Badge variant="info">
                <Zap size={8} className="mr-0.5" />
                {t('aiHub.officialAgent', 'Official')}
              </Badge>
            )}
            <Badge variant="default">{agent.category}</Badge>
          </div>
          <p className="text-xs text-muted line-clamp-2">{agent.description}</p>
          {(agent.targetPersona || agent.capabilities) && (
            <div className="flex items-center gap-2 mt-1.5 flex-wrap">
              {agent.targetPersona && (
                <span className="text-[10px] text-muted flex items-center gap-0.5">
                  <User size={8} />
                  {agent.targetPersona}
                </span>
              )}
              {agent.capabilities && (() => {
                const caps = agent.capabilities.split(',').map(c => c.trim()).filter(Boolean);
                return (
                  <>
                    {caps.slice(0, MAX_DISPLAYED_CAPABILITIES).map(cap => (
                      <span
                        key={cap}
                        className="inline-flex items-center px-1.5 py-0.5 rounded text-[9px] bg-elevated text-muted"
                      >
                        {cap}
                      </span>
                    ))}
                    {caps.length > MAX_DISPLAYED_CAPABILITIES && (
                      <span className="text-[9px] text-muted">+{caps.length - MAX_DISPLAYED_CAPABILITIES}</span>
                    )}
                  </>
                );
              })()}
            </div>
          )}
        </div>
        <ChevronRight size={14} className={cn(
          'shrink-0 mt-1 transition-colors',
          isSelected ? 'text-accent' : 'text-muted/40',
        )} />
      </div>
    </button>
  );
}

interface ExecutionResultPanelProps {
  result: ExecutionResult;
}

function ExecutionResultPanel({ result }: ExecutionResultPanelProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-3">
      {/* Status bar */}
      <div className="flex items-center gap-2 p-2 rounded-md bg-mint/10 border border-mint/20">
        <CheckCircle2 size={13} className="text-mint shrink-0" />
        <span className="text-xs text-mint font-medium">
          {t('contracts.workspace.aiAgentsExecutionComplete', 'Agent completed')}
        </span>
        <span className="text-[10px] text-muted ml-auto flex items-center gap-1">
          <Clock size={9} />
          {result.durationMs}ms
        </span>
      </div>

      {/* Artifacts */}
      {result.artifacts.length > 0 && (
        <div className="space-y-1.5">
          <p className="text-[10px] font-semibold text-muted uppercase tracking-wider">
            {t('contracts.workspace.aiAgentsArtifacts', 'Generated Artifacts')}
          </p>
          {result.artifacts.map((artifact) => (
            <div
              key={artifact.artifactId}
              className="flex items-center gap-2 p-2 rounded-md bg-elevated border border-edge"
            >
              <FileText size={12} className="text-accent shrink-0" />
              <div className="flex-1 min-w-0">
                <span className="text-xs text-body truncate block">{artifact.title}</span>
                <span className="text-[10px] text-muted">{artifact.artifactType} · {artifact.format}</span>
              </div>
              <Badge variant="default">
                {t('contracts.workspace.aiAgentsArtifactPending', 'Pending review')}
              </Badge>
            </div>
          ))}
        </div>
      )}

      {/* Output */}
      <div className="space-y-1.5">
        <p className="text-[10px] font-semibold text-muted uppercase tracking-wider">
          {t('contracts.workspace.aiAgentsOutput', 'Output')}
        </p>
        <div className="bg-code rounded-lg border border-edge p-3 max-h-80 overflow-y-auto">
          <pre className="text-[11px] text-body whitespace-pre-wrap font-mono leading-relaxed">
            {result.output}
          </pre>
        </div>
      </div>

      {/* Token usage */}
      <p className="text-[10px] text-muted text-right">
        {t('contracts.workspace.aiAgentsTokens', '{{prompt}} prompt + {{completion}} completion tokens', {
          prompt: result.promptTokens,
          completion: result.completionTokens,
        })}
      </p>
    </div>
  );
}
