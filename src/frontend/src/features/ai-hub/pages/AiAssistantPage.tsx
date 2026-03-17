import { useState, useRef, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Bot,
  Send,
  MessageSquare,
  Plus,
  Shield,
  Cpu,
  User,
  Server,
  FileText,
  AlertTriangle,
  GitBranch,
  BookOpen,
  Info,
  ChevronDown,
  ChevronUp,
  Archive,
  Tag,
  Sparkles,
  Database,
  Eye,
  Link2,
  CheckCircle2,
  AlertCircle,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { PageContainer } from '../../../components/shell';

// ── Types ───────────────────────────────────────────────────────────────

interface Conversation {
  id: string;
  title: string;
  persona: string;
  messageCount: number;
  isActive: boolean;
  lastMessageAt: string | null;
  lastModelUsed: string | null;
  tags: string;
  defaultContextScope: string;
}

interface ChatMessage {
  id: string;
  role: 'assistant' | 'user';
  content: string;
  modelName?: string | null;
  provider?: string | null;
  isInternalModel?: boolean;
  promptTokens?: number;
  completionTokens?: number;
  appliedPolicyName?: string | null;
  groundingSources?: string[];
  contextReferences?: string[];
  correlationId?: string;
  useCaseType?: string;
  routingPath?: string;
  confidenceLevel?: string;
  costClass?: string;
  routingRationale?: string;
  sourceWeightingSummary?: string;
  escalationReason?: string;
  timestamp: string;
}

interface SuggestedPrompt {
  prompt: string;
  category: string;
  personas: string[];
  scopeHint: string | null;
  relevance: string;
}

// ── Mock Data (simulates backend) ───────────────────────────────────────

const mockConversations: Conversation[] = [
  {
    id: '1',
    title: 'Payment API latency investigation',
    persona: 'Engineer',
    messageCount: 4,
    isActive: true,
    lastMessageAt: '2026-03-15T10:30:00Z',
    lastModelUsed: 'NexTrace-Internal-v1',
    tags: 'troubleshooting,payment',
    defaultContextScope: 'services,incidents',
  },
  {
    id: '2',
    title: 'Contract compatibility check — order-service v3',
    persona: 'Architect',
    messageCount: 6,
    isActive: true,
    lastMessageAt: '2026-03-14T14:30:00Z',
    lastModelUsed: 'NexTrace-Internal-v1',
    tags: 'contracts',
    defaultContextScope: 'contracts,services',
  },
  {
    id: '3',
    title: 'Incident correlation — notification failures',
    persona: 'TechLead',
    messageCount: 8,
    isActive: false,
    lastMessageAt: '2026-03-13T09:00:00Z',
    lastModelUsed: 'NexTrace-Internal-v1',
    tags: 'incident,correlation',
    defaultContextScope: 'incidents,changes',
  },
];

const mockMessagesMap: Record<string, ChatMessage[]> = {
  '1': [
    {
      id: 'w1',
      role: 'assistant',
      content:
        "Welcome! I'm the NexTraceOne AI Assistant. I can help you investigate production issues, analyze contracts, correlate incidents, and provide operational insights. What would you like to explore?",
      modelName: 'NexTrace-Internal-v1',
      provider: 'Internal',
      isInternalModel: true,
      promptTokens: 0,
      completionTokens: 42,
      groundingSources: ['Service Catalog', 'Contract Registry'],
      contextReferences: [],
      correlationId: 'init-001',
      useCaseType: 'General',
      routingPath: 'InternalOnly',
      confidenceLevel: 'High',
      costClass: 'low',
      routingRationale: 'Default welcome message served by internal model with full catalog access.',
      sourceWeightingSummary: 'ServiceCatalog:50%,ContractRegistry:50%',
      escalationReason: 'None',
      timestamp: '2026-03-15T10:00:00Z',
    },
    {
      id: 'u1',
      role: 'user',
      content: 'What issues are affecting the payment API right now?',
      timestamp: '2026-03-15T10:05:00Z',
    },
    {
      id: 'a1',
      role: 'assistant',
      content:
        'Based on the Service Catalog and Incident History, the payment-service is currently experiencing elevated latency (p99 > 2s) since 09:45 UTC. There is an active incident INC-2847 correlated with a deployment change CHG-1923 from 09:30 UTC. The blast radius includes order-service and notification-service as downstream consumers.',
      modelName: 'NexTrace-Internal-v1',
      provider: 'Internal',
      isInternalModel: true,
      promptTokens: 156,
      completionTokens: 89,
      appliedPolicyName: 'Default Internal Policy',
      groundingSources: ['Service Catalog', 'Incident History', 'Change Intelligence'],
      contextReferences: ['service:payment-service', 'incident:INC-2847', 'change:CHG-1923'],
      correlationId: 'resp-002',
      useCaseType: 'IncidentExplanation',
      routingPath: 'InternalOnly',
      confidenceLevel: 'High',
      costClass: 'low',
      routingRationale: 'Incident data fully available internally; no external escalation needed.',
      sourceWeightingSummary: 'Incident:40%,Change:25%,Runbook:20%,TelemetrySummary:15%',
      escalationReason: 'None',
      timestamp: '2026-03-15T10:05:02Z',
    },
    {
      id: 'u2',
      role: 'user',
      content: 'Is there a runbook for this type of issue?',
      timestamp: '2026-03-15T10:10:00Z',
    },
    {
      id: 'a2',
      role: 'assistant',
      content:
        'Yes, there is a runbook RB-PAY-003 "Payment Service Latency Escalation" that covers this scenario. Key steps: (1) Verify deployment rollback eligibility for CHG-1923, (2) Check database connection pool saturation, (3) Enable circuit breaker on payment-gateway dependency. The runbook was last updated 2 weeks ago and has been used 3 times in the last quarter.',
      modelName: 'NexTrace-Internal-v1',
      provider: 'Internal',
      isInternalModel: true,
      promptTokens: 203,
      completionTokens: 112,
      appliedPolicyName: 'Default Internal Policy',
      groundingSources: ['Runbook Library', 'Service Catalog', 'Change Intelligence'],
      contextReferences: ['runbook:RB-PAY-003', 'change:CHG-1923'],
      correlationId: 'resp-003',
      useCaseType: 'MitigationGuidance',
      routingPath: 'InternalOnly',
      confidenceLevel: 'High',
      costClass: 'low',
      routingRationale: 'Runbook and change data available internally; mitigation steps grounded in verified sources.',
      sourceWeightingSummary: 'Runbook:45%,Change:30%,ServiceCatalog:25%',
      escalationReason: 'None',
      timestamp: '2026-03-15T10:10:03Z',
    },
  ],
  '2': [
    {
      id: 'w2',
      role: 'assistant',
      content: "Welcome! I'm ready to help you analyze contract compatibility. What would you like to check?",
      modelName: 'NexTrace-Internal-v1',
      provider: 'Internal',
      isInternalModel: true,
      groundingSources: ['Contract Registry'],
      contextReferences: [],
      correlationId: 'init-002',
      useCaseType: 'ContractExplanation',
      routingPath: 'InternalOnly',
      confidenceLevel: 'High',
      costClass: 'low',
      routingRationale: 'Contract analysis scoped to internal registry; no external model required.',
      sourceWeightingSummary: 'ContractRegistry:100%',
      escalationReason: 'None',
      timestamp: '2026-03-14T14:00:00Z',
    },
  ],
  '3': [
    {
      id: 'w3',
      role: 'assistant',
      content: "Welcome! I'm ready to help you correlate incidents. What would you like to investigate?",
      modelName: 'NexTrace-Internal-v1',
      provider: 'Internal',
      isInternalModel: true,
      groundingSources: ['Incident History', 'Change Intelligence'],
      contextReferences: [],
      correlationId: 'init-003',
      useCaseType: 'IncidentExplanation',
      routingPath: 'InternalOnly',
      confidenceLevel: 'Medium',
      costClass: 'low',
      routingRationale: 'Incident correlation initiated with partial context; awaiting user query for full grounding.',
      sourceWeightingSummary: 'Incident:50%,Change:50%',
      escalationReason: 'None',
      timestamp: '2026-03-13T09:00:00Z',
    },
  ],
};

const contextScopes = ['Services', 'Contracts', 'Incidents', 'Changes', 'Runbooks'] as const;

/**
 * Página madura do AI Assistant — assistente IA contextualizado, governado e explicável.
 * A experiência adapta-se à persona do utilizador: contextos padrão,
 * prompts sugeridos, metadata de resposta e explicabilidade variam por perfil.
 *
 * @see docs/AI-ASSISTED-OPERATIONS.md
 * @see docs/PERSONA-UX-MAPPING.md — secção de IA por persona
 */
export function AiAssistantPage() {
  const { t } = useTranslation();
  const { persona, config } = usePersona();
  const [selectedConversation, setSelectedConversation] = useState<string>('1');
  const [activeContexts, setActiveContexts] = useState<string[]>(config.aiContextScopes);
  const [inputValue, setInputValue] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>(mockMessagesMap['1'] || []);
  const [expandedMeta, setExpandedMeta] = useState<string | null>(null);
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [messages, scrollToBottom]);

  const handleSelectConversation = (convId: string) => {
    setSelectedConversation(convId);
    setMessages(mockMessagesMap[convId] || []);
    setExpandedMeta(null);
  };

  const handleNewConversation = () => {
    const newId = `new-${Date.now()}`;
    setSelectedConversation(newId);
    setMessages([
      {
        id: `w-${newId}`,
        role: 'assistant',
        content: t('aiHub.welcomeMessage'),
        modelName: 'NexTrace-Internal-v1',
        provider: 'Internal',
        isInternalModel: true,
        groundingSources: ['Service Catalog', 'Contract Registry'],
        contextReferences: [],
        correlationId: `init-${newId}`,
        timestamp: new Date().toISOString(),
      },
    ]);
    setExpandedMeta(null);
  };

  const handleSendMessage = () => {
    if (!inputValue.trim()) return;

    const userMsg: ChatMessage = {
      id: `u-${Date.now()}`,
      role: 'user',
      content: inputValue,
      timestamp: new Date().toISOString(),
    };
    setMessages(prev => [...prev, userMsg]);
    setInputValue('');
    setIsTyping(true);

    // Simulate AI response with delay
    setTimeout(() => {
      const assistantMsg: ChatMessage = {
        id: `a-${Date.now()}`,
        role: 'assistant',
        content: `${t('aiHub.contextualResponsePrefix')}: "${inputValue.length > 80 ? inputValue.slice(0, 77) + '...' : inputValue}". ${t('aiHub.groundingDevelopment')}`,
        modelName: 'NexTrace-Internal-v1',
        provider: 'Internal',
        isInternalModel: true,
        promptTokens: Math.floor(inputValue.length / 4),
        completionTokens: 45,
        appliedPolicyName: 'Default Internal Policy',
        groundingSources: activeContexts.map(ctx => {
          const srcMap: Record<string, string> = {
            services: 'Service Catalog',
            contracts: 'Contract Registry',
            incidents: 'Incident History',
            changes: 'Change Intelligence',
            runbooks: 'Runbook Library',
          };
          return srcMap[ctx.toLowerCase()] || ctx;
        }),
        contextReferences: [],
        correlationId: `resp-${Date.now()}`,
        useCaseType: 'General',
        routingPath: 'InternalOnly',
        confidenceLevel: 'Medium',
        costClass: 'low',
        routingRationale: 'Default internal routing applied for general query.',
        sourceWeightingSummary: activeContexts.map(c => `${c}:25%`).join(','),
        escalationReason: 'None',
        timestamp: new Date().toISOString(),
      };
      setMessages(prev => [...prev, assistantMsg]);
      setIsTyping(false);
    }, 1200);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const toggleContext = (ctx: string) => {
    setActiveContexts(prev => (prev.includes(ctx) ? prev.filter(c => c !== ctx) : [...prev, ctx]));
  };

  const toggleMeta = (msgId: string) => {
    setExpandedMeta(prev => (prev === msgId ? null : msgId));
  };

  const selectedConv = mockConversations.find(c => c.id === selectedConversation);

  const contextIcons: Record<string, React.ReactNode> = {
    Services: <Server size={14} />,
    Contracts: <FileText size={14} />,
    Incidents: <AlertTriangle size={14} />,
    Changes: <GitBranch size={14} />,
    Runbooks: <BookOpen size={14} />,
  };

  const formatTime = (ts: string) => {
    try {
      return new Date(ts).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return '';
    }
  };

  return (
    <PageContainer>
      <div className="flex h-full gap-4">
        {/* ── Sidebar — lista de conversas ────────────────────────────── */}
        <div className="w-[300px] shrink-0 bg-card rounded-lg border border-edge flex flex-col">
          <div className="px-4 py-3 border-b border-edge flex items-center justify-between">
            <h2 className="text-sm font-semibold text-heading">{t('aiHub.conversations')}</h2>
            <Button variant="ghost" size="sm" onClick={handleNewConversation}>
              <Plus size={16} />
            </Button>
          </div>
          <div className="flex-1 overflow-y-auto">
            {mockConversations.map(conv => (
              <button
                key={conv.id}
                onClick={() => handleSelectConversation(conv.id)}
                className={`w-full text-left px-4 py-3 border-b border-edge transition-colors ${
                  selectedConversation === conv.id ? 'bg-hover' : 'hover:bg-hover'
                }`}
              >
                <div className="flex items-center gap-2 mb-1">
                  <MessageSquare size={14} className="text-muted shrink-0" />
                  <span className="text-sm font-medium text-heading truncate">{conv.title}</span>
                </div>
                <div className="flex items-center gap-2 text-xs text-muted mt-1">
                  <span>
                    {conv.messageCount} {t('aiHub.messages')}
                  </span>
                  <span>·</span>
                  <span>{conv.persona}</span>
                </div>
                <div className="mt-1.5 flex items-center gap-1.5 flex-wrap">
                  <Badge variant={conv.isActive ? 'success' : 'default'}>
                    {conv.isActive ? t('aiHub.statusActive') : t('aiHub.statusArchived')}
                  </Badge>
                  {conv.lastModelUsed && (
                    <Badge variant="info">
                      <Cpu size={10} className="mr-0.5" />
                      {t('aiHub.internalLabel')}
                    </Badge>
                  )}
                </div>
                {conv.tags && (
                  <div className="mt-1.5 flex items-center gap-1 flex-wrap">
                    {conv.tags.split(',').map(tag => (
                      <span key={tag} className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-[10px] bg-elevated text-muted">
                        <Tag size={8} />
                        {tag.trim()}
                      </span>
                    ))}
                  </div>
                )}
              </button>
            ))}
          </div>
        </div>

        {/* ── Área principal de chat ──────────────────────────────────── */}
        <div className="flex-1 bg-card rounded-lg border border-edge flex flex-col min-w-0">
          {/* ── Cabeçalho ──────────────────────────────────────────────── */}
          <div className="px-6 py-3 border-b border-edge flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Bot size={20} className="text-accent" />
              <div>
                <h1 className="text-base font-semibold text-heading">{t('aiHub.assistantTitle')}</h1>
                {selectedConv && (
                  <p className="text-xs text-muted truncate max-w-[300px]">{selectedConv.title}</p>
                )}
              </div>
            </div>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-1.5">
                <User size={14} className="text-muted" />
                <span className="text-xs text-muted">
                  {t('aiHub.persona')}: {t(`persona.${persona}.label`)}
                </span>
              </div>
              <Badge variant="info">
                <div className="flex items-center gap-1">
                  <Shield size={12} />
                  {t('aiHub.internalAi')}
                </div>
              </Badge>
            </div>
          </div>

          {/* ── Mensagens ──────────────────────────────────────────────── */}
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
            {messages.map(msg => (
              <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[75%] rounded-lg px-4 py-3 ${
                    msg.role === 'assistant' ? 'bg-elevated' : 'bg-accent/20'
                  }`}
                >
                  {/* ── Header ────────────────────────────────────────── */}
                  {msg.role === 'assistant' && (
                    <div className="flex items-center gap-2 mb-2 flex-wrap">
                      <Bot size={14} className="text-accent" />
                      <span className="text-xs font-medium text-accent">{t('aiHub.assistant')}</span>
                      {msg.modelName && (
                        <Badge variant={msg.isInternalModel ? 'info' : 'warning'}>
                          <div className="flex items-center gap-1">
                            <Cpu size={10} />
                            {msg.modelName}
                          </div>
                        </Badge>
                      )}
                      {msg.confidenceLevel && (
                        <Badge
                          variant={
                            msg.confidenceLevel === 'High'
                              ? 'success'
                              : msg.confidenceLevel === 'Medium'
                                ? 'warning'
                                : 'default'
                          }
                        >
                          <div className="flex items-center gap-1">
                            <CheckCircle2 size={10} aria-hidden="true" />
                            {msg.confidenceLevel === 'High'
                              ? t('aiHub.trustGrounded')
                              : msg.confidenceLevel === 'Medium'
                                ? t('aiHub.trustPartialContext')
                                : t('aiHub.trustLimitedContext')}
                          </div>
                        </Badge>
                      )}
                      {msg.isInternalModel && (
                        <Badge variant="info">
                          <div className="flex items-center gap-1">
                            <Shield size={10} />
                            {t('aiHub.trustInternalOnly')}
                          </div>
                        </Badge>
                      )}
                      {msg.escalationReason && msg.escalationReason !== 'None' && (
                        <Badge variant="warning">
                          <div className="flex items-center gap-1">
                            <AlertCircle size={10} aria-hidden="true" />
                            {t('aiHub.trustExternalUsed')}
                          </div>
                        </Badge>
                      )}
                      <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
                    </div>
                  )}
                  {msg.role === 'user' && (
                    <div className="flex items-center gap-2 mb-1 justify-end">
                      <span className="text-[10px] text-faded">{formatTime(msg.timestamp)}</span>
                      <span className="text-xs font-medium text-body">{t('aiHub.you')}</span>
                    </div>
                  )}

                  {/* ── Content ──────────────────────────────────────── */}
                  <p className="text-sm text-body whitespace-pre-wrap">{msg.content}</p>

                  {/* ── Grounding Sources ────────────────────────────── */}
                  {msg.groundingSources && msg.groundingSources.length > 0 && (
                    <div className="mt-2 flex items-center gap-1 flex-wrap">
                      <Database size={12} className="text-muted shrink-0" />
                      <span className="text-xs text-muted">{t('aiHub.groundingSources')}:</span>
                      {msg.groundingSources.map(src => (
                        <Badge key={src} variant="default">
                          {src}
                        </Badge>
                      ))}
                    </div>
                  )}

                  {/* ── Context References ───────────────────────────── */}
                  {msg.contextReferences && msg.contextReferences.length > 0 && (
                    <div className="mt-1.5 flex items-center gap-1 flex-wrap">
                      <Link2 size={12} className="text-muted shrink-0" />
                      <span className="text-xs text-muted">{t('aiHub.contextRefs')}:</span>
                      {msg.contextReferences.map(ref => (
                        <span key={ref} className="text-xs text-accent bg-accent/10 px-1.5 py-0.5 rounded">
                          {ref}
                        </span>
                      ))}
                    </div>
                  )}

                  {/* ── Metadata toggle ──────────────────────────────── */}
                  {msg.role === 'assistant' && msg.correlationId && (
                    <div className="mt-2">
                      <button
                        onClick={() => toggleMeta(msg.id)}
                        className="flex items-center gap-1 text-xs text-muted hover:text-body transition-colors"
                      >
                        <Eye size={12} />
                        {t('aiHub.responseMetadata')}
                        {expandedMeta === msg.id ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                      </button>

                      {expandedMeta === msg.id && (
                        <div className="mt-2 p-3 rounded-md bg-canvas border border-edge text-xs space-y-1.5">
                          <div className="grid grid-cols-2 gap-x-4 gap-y-1">
                            <span className="text-muted">{t('aiHub.metaModel')}:</span>
                            <span className="text-body">{msg.modelName ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaProvider')}:</span>
                            <span className="text-body">{msg.provider ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaModelType')}:</span>
                            <span className="text-body">
                              {msg.isInternalModel ? (
                                <span className="text-success">{t('aiHub.internalLabel')}</span>
                              ) : (
                                <span className="text-warning">{t('aiHub.externalLabel')}</span>
                              )}
                            </span>
                            <span className="text-muted">{t('aiHub.metaPromptTokens')}:</span>
                            <span className="text-body">{msg.promptTokens ?? 0}</span>
                            <span className="text-muted">{t('aiHub.metaCompletionTokens')}:</span>
                            <span className="text-body">{msg.completionTokens ?? 0}</span>
                            <span className="text-muted">{t('aiHub.metaPolicy')}:</span>
                            <span className="text-body">{msg.appliedPolicyName ?? t('aiHub.metaNoneApplied')}</span>
                            <span className="text-muted">{t('aiHub.metaCorrelation')}:</span>
                            <span className="text-body font-mono text-[10px]">{msg.correlationId}</span>
                            <span className="text-muted">{t('aiHub.metaUseCase')}:</span>
                            <span className="text-body">{msg.useCaseType ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaRoutingPath')}:</span>
                            <span className="text-body">{msg.routingPath ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaConfidence')}:</span>
                            <span className="text-body">{msg.confidenceLevel ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaCostClass')}:</span>
                            <span className="text-body">{msg.costClass ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaSourceWeights')}:</span>
                            <span className="text-body">{msg.sourceWeightingSummary ?? '—'}</span>
                            <span className="text-muted">{t('aiHub.metaEscalation')}:</span>
                            <span className="text-body">{msg.escalationReason ?? '—'}</span>
                          </div>
                          {msg.routingRationale && (
                            <div className="mt-2 pt-2 border-t border-edge">
                              <span className="text-muted">{t('aiHub.metaRoutingRationale')}:</span>
                              <p className="text-body mt-0.5">{msg.routingRationale}</p>
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            ))}

            {/* ── Typing indicator ─────────────────────────────────────── */}
            {isTyping && (
              <div className="flex justify-start">
                <div className="bg-elevated rounded-lg px-4 py-3 flex items-center gap-2">
                  <Bot size={14} className="text-accent" />
                  <span className="text-xs text-muted animate-pulse">{t('aiHub.typing')}</span>
                  <span className="flex gap-1">
                    <span className="w-1.5 h-1.5 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                    <span className="w-1.5 h-1.5 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                    <span className="w-1.5 h-1.5 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                  </span>
                </div>
              </div>
            )}

            {/* ── Suggested Prompts (shown only when conversation has few messages) ── */}
            {messages.length <= 2 && (
              <div className="pt-4">
                <div className="flex items-center gap-2 mb-2">
                  <Sparkles size={14} className="text-accent" />
                  <p className="text-xs font-medium text-heading">{t('aiHub.suggestedPrompts')}</p>
                </div>
                <p className="text-xs text-muted mb-3">{t('productPolish.aiAssistantPersonaHint')}</p>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                  {config.aiSuggestedPromptKeys.map((promptKey, idx) => (
                    <button
                      key={idx}
                      onClick={() => setInputValue(t(promptKey))}
                      className="text-left px-3 py-2 rounded-md border border-edge text-sm text-body hover:bg-hover hover:border-accent/30 transition-colors"
                    >
                      {t(promptKey)}
                    </button>
                  ))}
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* ── Context scope selector ─────────────────────────────────── */}
          <div className="px-6 py-2 border-t border-edge">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-xs text-muted">{t('aiHub.contextScope')}:</span>
              {contextScopes.map(ctx => (
                <button
                  key={ctx}
                  onClick={() => toggleContext(ctx)}
                  className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
                    activeContexts.includes(ctx) ? 'bg-accent/20 text-accent' : 'bg-elevated text-muted hover:text-body'
                  }`}
                >
                  {contextIcons[ctx]}
                  {t(`aiHub.context${ctx}`)}
                </button>
              ))}
            </div>
          </div>

          {/* ── Input field ────────────────────────────────────────────── */}
          <div className="px-6 py-4 border-t border-edge">
            <div className="flex items-center gap-3">
              <input
                type="text"
                value={inputValue}
                onChange={e => setInputValue(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={t('productPolish.aiAssistantHint')}
                className="flex-1 bg-elevated border border-edge rounded-lg px-4 py-2.5 text-sm text-body placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
                disabled={isTyping}
              />
              <Button variant="primary" size="md" disabled={!inputValue.trim() || isTyping} onClick={handleSendMessage}>
                <Send size={16} />
                {t('aiHub.send')}
              </Button>
            </div>
            <div className="flex items-center gap-2 mt-2 text-[10px] text-faded">
              <Info size={10} />
              <span>{t('aiHub.governanceNotice')}</span>
            </div>
          </div>
        </div>
      </div>
    </PageContainer>
  );
}
