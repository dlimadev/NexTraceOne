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
  Tag,
  Sparkles,
  Database,
  Eye,
  Link2,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Inbox,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { PageContainer } from '../../../components/shell';
import { aiGovernanceApi } from '../api/aiGovernance';

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
  clientType?: string;
  createdBy?: string;
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

// ── API Response Types ──────────────────────────────────────────────────

interface ConversationApiItem {
  id: string;
  title: string;
  persona: string;
  clientType: string;
  defaultContextScope: string;
  lastModelUsed: string | null;
  createdBy: string;
  messageCount: number;
  tags: string;
  isActive: boolean;
  lastMessageAt: string | null;
}

interface MessageApiItem {
  messageId: string;
  conversationId: string;
  role: string;
  content: string;
  modelName: string | null;
  provider: string | null;
  isInternalModel: boolean;
  promptTokens: number;
  completionTokens: number;
  appliedPolicyName: string | null;
  groundingSources: string[];
  contextReferences: string[];
  correlationId: string;
  timestamp: string;
}

const contextScopes = ['Services', 'Contracts', 'Incidents', 'Changes', 'Runbooks'] as const;

/**
 * Página madura do AI Assistant — assistente IA contextualizado, governado e explicável.
 * A experiência adapta-se à persona do utilizador: contextos padrão,
 * prompts sugeridos, metadata de resposta e explicabilidade variam por perfil.
 *
 * Esta versão usa exclusivamente APIs reais para listagem de conversas e mensagens.
 *
 * @see docs/AI-ASSISTED-OPERATIONS.md
 * @see docs/PERSONA-UX-MAPPING.md — secção de IA por persona
 */
export function AiAssistantPage() {
  const { t } = useTranslation();
  const { persona, config } = usePersona();

  // ── State ─────────────────────────────────────────────────────────────
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<string | null>(null);
  const [activeContexts, setActiveContexts] = useState<string[]>(config.aiContextScopes);
  const [inputValue, setInputValue] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [expandedMeta, setExpandedMeta] = useState<string | null>(null);
  const [isTyping, setIsTyping] = useState(false);
  const [backendConnected, setBackendConnected] = useState<boolean | null>(null);
  const [providerStatus, setProviderStatus] = useState<string>('');
  const [isLoadingConversations, setIsLoadingConversations] = useState(true);
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [conversationsError, setConversationsError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [messages, scrollToBottom]);

  // ── Load conversations from backend ───────────────────────────────────
  const loadConversations = useCallback(async () => {
    setIsLoadingConversations(true);
    setConversationsError(null);
    try {
      const data = await aiGovernanceApi.listConversations({ pageSize: 50 });
      const items: Conversation[] = (data.items || []).map((item: ConversationApiItem) => ({
        id: item.id,
        title: item.title,
        persona: item.persona,
        messageCount: item.messageCount,
        isActive: item.isActive,
        lastMessageAt: item.lastMessageAt,
        lastModelUsed: item.lastModelUsed,
        tags: item.tags ?? '',
        defaultContextScope: item.defaultContextScope ?? '',
        clientType: item.clientType,
        createdBy: item.createdBy,
      }));
      setConversations(items);
      // Auto-select first conversation if available
      if (items.length > 0 && selectedConversation === null) {
        const firstItem = items[0];
        if (firstItem) {
          setSelectedConversation(firstItem.id);
        }
      }
    } catch (err) {
      console.error('Failed to load conversations:', err);
      setConversationsError(t('aiHub.errorLoadingConversations'));
    } finally {
      setIsLoadingConversations(false);
    }
  }, [selectedConversation, t]);

  // ── Load messages for selected conversation ───────────────────────────
  const loadMessages = useCallback(async (conversationId: string) => {
    setIsLoadingMessages(true);
    try {
      const data = await aiGovernanceApi.listMessages(conversationId, { pageSize: 100 });
      const items: ChatMessage[] = (data.items || []).map((item: MessageApiItem) => ({
        id: item.messageId,
        role: item.role as 'user' | 'assistant',
        content: item.content,
        modelName: item.modelName,
        provider: item.provider,
        isInternalModel: item.isInternalModel,
        promptTokens: item.promptTokens,
        completionTokens: item.completionTokens,
        appliedPolicyName: item.appliedPolicyName,
        groundingSources: item.groundingSources ?? [],
        contextReferences: item.contextReferences ?? [],
        correlationId: item.correlationId,
        timestamp: item.timestamp,
        // Derived fields for UI display
        useCaseType: 'General',
        routingPath: item.isInternalModel ? 'InternalOnly' : 'ExternalEscalation',
        confidenceLevel: item.isInternalModel ? 'High' : 'Medium',
        costClass: item.isInternalModel ? 'low' : 'medium',
      }));
      setMessages(items);
    } catch (err) {
      console.error('Failed to load messages:', err);
      setMessages([]);
    } finally {
      setIsLoadingMessages(false);
    }
  }, []);

  // ── Initial load ──────────────────────────────────────────────────────
  useEffect(() => {
    loadConversations();
  }, [loadConversations]);

  // ── Load messages when conversation changes ───────────────────────────
  useEffect(() => {
    if (selectedConversation) {
      loadMessages(selectedConversation);
    } else {
      setMessages([]);
    }
  }, [selectedConversation, loadMessages]);

  // ── Check provider health ─────────────────────────────────────────────
  useEffect(() => {
    aiGovernanceApi.checkProvidersHealth()
      .then((data: { allHealthy: boolean; items: Array<{ providerId: string; isHealthy: boolean; message?: string }> }) => {
        setBackendConnected(true);
        const healthyCount = data.items?.filter((p: { isHealthy: boolean }) => p.isHealthy).length || 0;
        setProviderStatus(`${healthyCount}/${data.items?.length || 0} providers healthy`);
      })
      .catch(() => {
        setBackendConnected(false);
        setProviderStatus('Backend unavailable');
      });
  }, []);

  const handleSelectConversation = (convId: string) => {
    setSelectedConversation(convId);
    setExpandedMeta(null);
  };

  const handleNewConversation = async () => {
    try {
      const response = await aiGovernanceApi.createConversation({
        title: t('aiHub.newConversationTitle'),
        persona: persona,
        clientType: 'Web',
        defaultContextScope: activeContexts.join(',').toLowerCase(),
      });
      const newConv: Conversation = {
        id: response.conversationId,
        title: response.title,
        persona: response.persona,
        messageCount: 0,
        isActive: response.isActive,
        lastMessageAt: null,
        lastModelUsed: null,
        tags: '',
        defaultContextScope: response.defaultContextScope,
        clientType: response.clientType,
      };
      setConversations(prev => [newConv, ...prev]);
      setSelectedConversation(response.conversationId);
      setMessages([]);
    } catch (err) {
      console.error('Failed to create conversation:', err);
      setConversationsError(t('common.errorLoading'));
    }
  };

  const handleSendMessage = async () => {
    if (!inputValue.trim() || isTyping) return;

    const userMsg: ChatMessage = {
      id: `u-${Date.now()}`,
      role: 'user',
      content: inputValue.trim(),
      timestamp: new Date().toISOString(),
    };
    setMessages(prev => [...prev, userMsg]);
    setInputValue('');
    setIsTyping(true);

    try {
      const response = await aiGovernanceApi.sendMessage({
        message: userMsg.content,
        conversationId: selectedConversation || undefined,
        contextScope: activeContexts.join(','),
        persona,
        clientType: 'Web',
      });

      const assistantMsg: ChatMessage = {
        id: `a-${Date.now()}`,
        role: 'assistant',
        content: response.assistantResponse || response.message || response.content,
        modelName: response.modelUsed || response.modelName || response.modelId,
        provider: response.provider || response.providerId,
        isInternalModel: response.isInternalModel,
        promptTokens: response.promptTokens,
        completionTokens: response.completionTokens,
        correlationId: response.correlationId,
        useCaseType: response.useCaseType || 'General',
        routingPath: response.routingPath || (response.isInternalModel ? 'InternalOnly' : 'ExternalEscalation'),
        confidenceLevel: response.confidenceLevel || 'Unknown',
        costClass: response.costClass || (response.isInternalModel ? 'low' : 'medium'),
        routingRationale: response.routingRationale || `Routed to ${response.provider || response.providerId}/${response.modelUsed || response.modelName || response.modelId}.`,
        sourceWeightingSummary: response.sourceWeightingSummary || '',
        escalationReason: response.escalationReason || 'None',
        timestamp: new Date().toISOString(),
      };
      setMessages(prev => [...prev, assistantMsg]);

      if (response.conversationId && !selectedConversation) {
        setSelectedConversation(response.conversationId);
      }
    } catch (error: unknown) {
      const errorMessage = error instanceof Error ? error.message : t('common.errorLoading');
      const assistantMsg: ChatMessage = {
        id: `a-${Date.now()}`,
        role: 'assistant',
        content: `${t('common.errorLoading')} ${errorMessage}`,
        modelName: null,
        provider: null,
        isInternalModel: false,
        promptTokens: 0,
        completionTokens: 0,
        appliedPolicyName: null,
        groundingSources: [],
        contextReferences: [],
        correlationId: `provider-unavailable-${Date.now()}`,
        useCaseType: 'General',
        routingPath: 'ProviderUnavailable',
        confidenceLevel: 'Unknown',
        costClass: 'none',
        routingRationale: 'Provider unavailable. No silent mock was used.',
        sourceWeightingSummary: '',
        escalationReason: 'ProviderUnavailable',
        timestamp: new Date().toISOString(),
      };
      setMessages(prev => [...prev, assistantMsg]);
    } finally {
      setIsTyping(false);
    }
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

  const selectedConv = conversations.find(c => c.id === selectedConversation);

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
            {/* ── Loading state ─────────────────────────────────────────── */}
            {isLoadingConversations && (
              <div className="flex items-center justify-center py-8">
                <Loader2 size={20} className="animate-spin text-muted" />
              </div>
            )}

            {/* ── Error state ───────────────────────────────────────────── */}
            {conversationsError && !isLoadingConversations && (
              <div className="px-4 py-6 text-center">
                <AlertCircle size={24} className="text-warning mx-auto mb-2" />
                <p className="text-sm text-muted">{conversationsError}</p>
                <Button variant="ghost" size="sm" className="mt-2" onClick={loadConversations}>
                  {t('common.retry')}
                </Button>
              </div>
            )}

            {/* ── Empty state ───────────────────────────────────────────── */}
            {!isLoadingConversations && !conversationsError && conversations.length === 0 && (
              <div className="px-4 py-8 text-center">
                <Inbox size={32} className="text-muted mx-auto mb-3" />
                <p className="text-sm text-heading mb-1">{t('aiHub.noConversations')}</p>
                <p className="text-xs text-muted mb-4">{t('aiHub.startNewConversation')}</p>
                <Button variant="primary" size="sm" onClick={handleNewConversation}>
                  <Plus size={14} className="mr-1" />
                  {t('aiHub.newConversation')}
                </Button>
              </div>
            )}

            {/* ── Conversations list ────────────────────────────────────── */}
            {!isLoadingConversations && !conversationsError && conversations.map(conv => (
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
              {backendConnected === true && (
                <Badge variant="success">
                  <CheckCircle2 size={10} className="mr-0.5" />
                  {providerStatus}
                </Badge>
              )}
              {backendConnected === false && (
                <Badge variant="warning">
                  <AlertCircle size={10} className="mr-0.5" />
                  {providerStatus}
                </Badge>
              )}
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
            {/* ── Loading messages state ──────────────────────────────── */}
            {isLoadingMessages && (
              <div className="flex items-center justify-center py-8">
                <Loader2 size={20} className="animate-spin text-muted mr-2" />
                <span className="text-sm text-muted">{t('aiHub.loadingMessages')}</span>
              </div>
            )}

            {/* ── No conversation selected ────────────────────────────── */}
            {!selectedConversation && !isLoadingMessages && (
              <div className="flex flex-col items-center justify-center h-full text-center">
                <Bot size={48} className="text-muted mb-4" />
                <p className="text-heading font-medium mb-2">{t('aiHub.selectConversation')}</p>
                <p className="text-sm text-muted mb-4">{t('aiHub.selectConversationHint')}</p>
                <Button variant="primary" size="sm" onClick={handleNewConversation}>
                  <Plus size={14} className="mr-1" />
                  {t('aiHub.newConversation')}
                </Button>
              </div>
            )}

            {/* ── Empty conversation ──────────────────────────────────── */}
            {selectedConversation && !isLoadingMessages && messages.length === 0 && (
              <div className="flex flex-col items-center justify-center h-full text-center">
                <Sparkles size={32} className="text-accent mb-3" />
                <p className="text-heading font-medium mb-1">{t('aiHub.emptyConversation')}</p>
                <p className="text-sm text-muted">{t('aiHub.startTyping')}</p>
              </div>
            )}

            {/* ── Messages list ───────────────────────────────────────── */}
            {!isLoadingMessages && messages.map(msg => (
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
