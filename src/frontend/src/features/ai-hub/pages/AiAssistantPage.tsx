import { useState, useRef, useEffect, useCallback, type KeyboardEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import {
  Bot,
  Send,
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
  Sparkles,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Globe,
  Lock,
  Users,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { PageContainer } from '../../../components/shell';
import { aiGovernanceApi } from '../api/aiGovernance';
import { ChatSidebar } from './ChatSidebar';
import { ChatMessageItem } from './ChatMessageItem';
import { AgentsSidePanel } from './AgentsSidePanel';
import { SuggestedPrompts } from './SuggestedPrompts';
import type {
  Conversation,
  ChatMessage,
  AvailableModelsResponse,
  AgentItem,
  AgentsResponse,
  ConversationApiItem,
  MessageApiItem,
  ConversationDetailApiResponse,
} from './AiAssistantTypes';
import {
  contextScopes,
  conversationSearchParam,
  normalizeContextScope,
  mapMessage,
  getProblemStatus,
} from './AiAssistantTypes';

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
  const [searchParams, setSearchParams] = useSearchParams();

  // ── State ─────────────────────────────────────────────────────────────
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<string | null>(() => searchParams.get(conversationSearchParam));
  const [activeContexts, setActiveContexts] = useState<string[]>(() =>
    Array.from(new Set(config.aiContextScopes.map(normalizeContextScope))),
  );
  const [inputValue, setInputValue] = useState('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [expandedMeta, setExpandedMeta] = useState<string | null>(null);
  const [isTyping, setIsTyping] = useState(false);
  const [backendConnected, setBackendConnected] = useState<boolean | null>(null);
  const [providerStatus, setProviderStatus] = useState<string>('');
  const [isLoadingConversations, setIsLoadingConversations] = useState(true);
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [conversationsError, setConversationsError] = useState<string | null>(null);
  const [messagesError, setMessagesError] = useState<string | null>(null);

  // ── Model selection state ─────────────────────────────────────────────
  const [availableModels, setAvailableModels] = useState<AvailableModelsResponse | null>(null);
  const [selectedModelId, setSelectedModelId] = useState<string | null>(null);
  const [isModelSelectorOpen, setIsModelSelectorOpen] = useState(false);
  const modelSelectorRef = useRef<HTMLDivElement>(null);

  // ── Agents state ──────────────────────────────────────────────────────
  const [agents, setAgents] = useState<AgentItem[]>([]);
  const [isAgentsPanelOpen, setIsAgentsPanelOpen] = useState(false);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const conversationLoadRequestRef = useRef(0);
  const messageLoadRequestRef = useRef(0);

  const scrollToBottom = useCallback(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, []);

  const setSelectedConversationState = useCallback((conversationId: string | null) => {
    setSelectedConversation(conversationId);
    setExpandedMeta(null);

    const nextSearchParams = new URLSearchParams(searchParams);
    if (conversationId) {
      nextSearchParams.set(conversationSearchParam, conversationId);
    } else {
      nextSearchParams.delete(conversationSearchParam);
    }

    setSearchParams(nextSearchParams, { replace: true });
  }, [searchParams, setSearchParams]);

  useEffect(() => {
    scrollToBottom();
  }, [messages, scrollToBottom]);

  useEffect(() => {
    const conversationFromUrl = searchParams.get(conversationSearchParam);
    setSelectedConversation(current => current === conversationFromUrl ? current : conversationFromUrl);
  }, [searchParams]);

  // ── Load conversations from backend ───────────────────────────────────
  const loadConversations = useCallback(async (preferredConversationId?: string | null) => {
    const requestId = ++conversationLoadRequestRef.current;
    setIsLoadingConversations(true);
    setConversationsError(null);

    try {
      const data = await aiGovernanceApi.listConversations({ pageSize: 50 });
      if (requestId !== conversationLoadRequestRef.current) {
        return;
      }

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

      const requestedConversationId = preferredConversationId
        ?? searchParams.get(conversationSearchParam)
        ?? selectedConversation;

      if (requestedConversationId && items.some(item => item.id === requestedConversationId)) {
        if (selectedConversation !== requestedConversationId) {
          setSelectedConversationState(requestedConversationId);
        }
        return;
      }

      if (requestedConversationId && !items.some(item => item.id === requestedConversationId)) {
        setMessages([]);
        setMessagesError(t('aiHub.conversationNotFound'));
      }

      if (items.length > 0) {
        const firstConversation = items[0];
        if (firstConversation) {
          setSelectedConversationState(firstConversation.id);
        }
      } else {
        setSelectedConversationState(null);
      }
    } catch {
      setConversationsError(t('aiHub.errorLoadingConversations'));
    } finally {
      if (requestId === conversationLoadRequestRef.current) {
        setIsLoadingConversations(false);
      }
    }
  }, [searchParams, selectedConversation, setSelectedConversationState, t]);

  // ── Load messages for selected conversation ───────────────────────────
  const loadMessages = useCallback(async (conversationId: string) => {
    const requestId = ++messageLoadRequestRef.current;
    setIsLoadingMessages(true);
    setMessagesError(null);

    try {
      const data: ConversationDetailApiResponse = await aiGovernanceApi.getConversation(conversationId, { messagePageSize: 100 });
       if (requestId !== messageLoadRequestRef.current) {
        return;
      }

      const items: ChatMessage[] = (data.messages || []).map((item: MessageApiItem) => mapMessage(item));
      setMessages(items);
      setConversations(prev => prev.map(conv => (
        conv.id === conversationId
          ? {
              ...conv,
              title: data.title,
              persona: data.persona,
              messageCount: data.messageCount,
              isActive: data.isActive,
              lastMessageAt: data.lastMessageAt,
              lastModelUsed: data.lastModelUsed,
              tags: data.tags ?? '',
              defaultContextScope: data.defaultContextScope ?? '',
              clientType: data.clientType,
              createdBy: data.createdBy,
            }
          : conv
      )));
    } catch (err) {
      setMessages([]);
      setMessagesError(getProblemStatus(err) === 404 ? t('aiHub.conversationNotFound') : t('aiHub.errorLoadingMessages'));
    } finally {
      if (requestId === messageLoadRequestRef.current) {
        setIsLoadingMessages(false);
      }
    }
  }, [t]);

  // ── Initial load ──────────────────────────────────────────────────────
  useEffect(() => {
    void loadConversations();
  }, [loadConversations]);

  // ── Load messages when conversation changes ───────────────────────────
  useEffect(() => {
    if (selectedConversation) {
      void loadMessages(selectedConversation);
    } else {
      setMessages([]);
      setMessagesError(null);
    }
  }, [selectedConversation, loadMessages]);

  // ── Check provider health ─────────────────────────────────────────────
  useEffect(() => {
    aiGovernanceApi.checkProvidersHealth()
      .then((data: { allHealthy: boolean; items: Array<{ providerId: string; isHealthy: boolean; message?: string }> }) => {
        setBackendConnected(true);
        const healthyCount = data.items?.filter((p: { isHealthy: boolean }) => p.isHealthy).length || 0;
        setProviderStatus(t('aiHub.providersHealthy', { healthy: healthyCount, total: data.items?.length || 0 }));
      })
      .catch(() => {
        setBackendConnected(false);
        setProviderStatus(t('aiHub.backendUnavailable'));
      });
  }, [t]);

  // ── Load available models (filtered by user authorization) ────────────
  useEffect(() => {
    aiGovernanceApi.listAvailableModels()
      .then((data: AvailableModelsResponse) => {
        setAvailableModels(data);
        // Auto-select default model
        const allModels = [...(data.internalModels || []), ...(data.externalModels || [])];
        const defaultModel = allModels.find(m => m.isDefault);
        if (defaultModel && !selectedModelId) {
          setSelectedModelId(defaultModel.modelId);
        }
      })
      .catch(() => {
        // Available models not loaded — model selector won't show
      });
  // eslint-disable-next-line react-hooks/exhaustive-deps -- only run on mount; selectedModelId read is intentional guard
  }, []);

  // ── Load agents ───────────────────────────────────────────────────────
  useEffect(() => {
    aiGovernanceApi.listAgents()
      .then((data: AgentsResponse) => {
        setAgents(data.items || []);
      })
      .catch(() => {
        // Agents not loaded — agent panel won't show
      });
  }, []);

  // ── Close model selector on outside click ─────────────────────────────
  useEffect(() => {
    if (!isModelSelectorOpen) return;
    const handleClickOutside = (e: MouseEvent) => {
      if (modelSelectorRef.current && !modelSelectorRef.current.contains(e.target as Node)) {
        setIsModelSelectorOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isModelSelectorOpen]);

  const handleSelectConversation = (convId: string) => {
    setMessagesError(null);
    setSelectedConversationState(convId);
  };

  const handleNewConversation = async () => {
    try {
      setConversationsError(null);
      setMessagesError(null);
      const response = await aiGovernanceApi.createConversation({
        title: t('aiHub.newConversationTitle'),
        persona: persona,
        clientType: 'Web',
        defaultContextScope: activeContexts.join(',').toLowerCase(),
      });

      setMessages([]);
      setSelectedConversationState(response.conversationId);
      await loadConversations(response.conversationId);
    } catch {
      setConversationsError(t('common.errorLoading'));
    }
  };

  const handleSendMessage = async () => {
    const messageToSend = inputValue.trim();
    if (!messageToSend || isTyping) return;

    setInputValue('');
    setIsTyping(true);
    setMessagesError(null);

    try {
      const response = await aiGovernanceApi.sendMessage({
        message: messageToSend,
        conversationId: selectedConversation || undefined,
        contextScope: activeContexts.join(','),
        persona,
        clientType: 'Web',
        preferredModelId: selectedModelId || undefined,
      });

      const targetConversationId = response.conversationId || selectedConversation;
      if (targetConversationId) {
        setSelectedConversationState(targetConversationId);
        await loadMessages(targetConversationId);
        await loadConversations(targetConversationId);
      } else {
        await loadConversations();
      }
    } catch (error: unknown) {
      setInputValue(messageToSend);
      setMessagesError(getProblemStatus(error) === 404 ? t('aiHub.conversationNotFound') : t('aiHub.errorSendingMessage'));
    } finally {
      setIsTyping(false);
    }
  };

  const handleKeyDown = (e: KeyboardEvent) => {
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

  const contextIcons: Record<string, ReactNode> = {
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
        <ChatSidebar
          conversations={conversations}
          selectedConversation={selectedConversation}
          isLoadingConversations={isLoadingConversations}
          conversationsError={conversationsError}
          onNewConversation={handleNewConversation}
          onSelectConversation={handleSelectConversation}
          onRetry={() => void loadConversations()}
        />

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

              {/* ── Model Selector ────────────────────────────────────── */}
              {availableModels && (
                <div className="relative" ref={modelSelectorRef}>
                  <button
                    onClick={() => setIsModelSelectorOpen(prev => !prev)}
                    className="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-md border border-edge text-xs font-medium text-body hover:bg-hover transition-colors"
                  >
                    {(() => {
                      const allModels = [...(availableModels.internalModels || []), ...(availableModels.externalModels || [])];
                      const selected = allModels.find(m => m.modelId === selectedModelId);
                      if (selected) {
                        return (
                          <>
                            {selected.isInternal ? <Shield size={12} className="text-success" /> : <Globe size={12} className="text-warning" />}
                            <span className="max-w-[120px] truncate">{selected.displayName}</span>
                          </>
                        );
                      }
                      return (
                        <>
                          <Cpu size={12} className="text-muted" />
                          <span>{t('aiHub.selectModel')}</span>
                        </>
                      );
                    })()}
                    {isModelSelectorOpen ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                  </button>

                  {isModelSelectorOpen && (
                    <div className="absolute right-0 top-full mt-1 w-[320px] bg-card border border-edge rounded-lg shadow-lg z-50 max-h-[400px] overflow-y-auto">
                      {availableModels.appliedPolicyName && (
                        <div className="px-3 py-2 border-b border-edge flex items-center gap-1.5 text-[10px] text-muted">
                          <Lock size={10} />
                          {t('aiHub.policyApplied')}: {availableModels.appliedPolicyName}
                        </div>
                      )}

                      {/* Internal Models Group */}
                      {availableModels.internalModels.length > 0 && (
                        <div>
                          <div className="px-3 py-2 border-b border-edge flex items-center gap-1.5">
                            <Shield size={12} className="text-success" />
                            <span className="text-xs font-semibold text-heading">{t('aiHub.internalModels')}</span>
                            <Badge variant="success">{availableModels.internalModels.length}</Badge>
                          </div>
                          {availableModels.internalModels.map(model => (
                            <button
                              key={model.modelId}
                              onClick={() => { setSelectedModelId(model.modelId); setIsModelSelectorOpen(false); }}
                              className={`w-full text-left px-3 py-2 hover:bg-hover transition-colors flex items-center gap-2 ${
                                selectedModelId === model.modelId ? 'bg-accent/10 border-l-2 border-accent' : ''
                              }`}
                            >
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-1.5">
                                  <span className="text-xs font-medium text-heading truncate">{model.displayName}</span>
                                  {model.isDefault && <Badge variant="info">{t('aiHub.defaultModel')}</Badge>}
                                </div>
                                <div className="flex items-center gap-2 mt-0.5">
                                  <span className="text-[10px] text-muted">{model.provider}</span>
                                  {model.contextWindow && (
                                    <span className="text-[10px] text-muted">· {(model.contextWindow / 1000).toFixed(0)}k ctx</span>
                                  )}
                                  <span className="text-[10px] text-muted">· {model.modelType}</span>
                                </div>
                              </div>
                              <Badge variant={model.status === 'Active' ? 'success' : 'default'}>
                                {model.status === 'Active' ? <CheckCircle2 size={8} /> : <AlertCircle size={8} />}
                              </Badge>
                            </button>
                          ))}
                        </div>
                      )}

                      {/* External Models Group */}
                      {availableModels.allowExternalModels && availableModels.externalModels.length > 0 && (
                        <div>
                          <div className="px-3 py-2 border-b border-t border-edge flex items-center gap-1.5">
                            <Globe size={12} className="text-warning" />
                            <span className="text-xs font-semibold text-heading">{t('aiHub.externalModels')}</span>
                            <Badge variant="warning">{availableModels.externalModels.length}</Badge>
                          </div>
                          {availableModels.externalModels.map(model => (
                            <button
                              key={model.modelId}
                              onClick={() => { setSelectedModelId(model.modelId); setIsModelSelectorOpen(false); }}
                              className={`w-full text-left px-3 py-2 hover:bg-hover transition-colors flex items-center gap-2 ${
                                selectedModelId === model.modelId ? 'bg-accent/10 border-l-2 border-accent' : ''
                              }`}
                            >
                              <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-1.5">
                                  <span className="text-xs font-medium text-heading truncate">{model.displayName}</span>
                                  {model.isDefault && <Badge variant="info">{t('aiHub.defaultModel')}</Badge>}
                                </div>
                                <div className="flex items-center gap-2 mt-0.5">
                                  <span className="text-[10px] text-muted">{model.provider}</span>
                                  {model.contextWindow && (
                                    <span className="text-[10px] text-muted">· {(model.contextWindow / 1000).toFixed(0)}k ctx</span>
                                  )}
                                  <span className="text-[10px] text-muted">· {model.modelType}</span>
                                </div>
                              </div>
                              <Badge variant={model.status === 'Active' ? 'success' : 'default'}>
                                {model.status === 'Active' ? <CheckCircle2 size={8} /> : <AlertCircle size={8} />}
                              </Badge>
                            </button>
                          ))}
                        </div>
                      )}

                      {availableModels.totalCount === 0 && (
                        <div className="px-3 py-4 text-center text-xs text-muted">
                          {t('aiHub.noModelsAvailable')}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}

              {/* ── Agents Panel Toggle ───────────────────────────────── */}
              {agents.length > 0 && (
                <button
                  onClick={() => setIsAgentsPanelOpen(prev => !prev)}
                  className={`inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-md border text-xs font-medium transition-colors ${
                    isAgentsPanelOpen ? 'border-accent bg-accent/10 text-accent' : 'border-edge text-body hover:bg-hover'
                  }`}
                >
                  <Users size={12} />
                  {t('aiHub.agents')}
                  <Badge variant="default">{agents.length}</Badge>
                </button>
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

            {/* ── Message error state ─────────────────────────────────── */}
            {selectedConversation && !isLoadingMessages && messagesError && (
              <div className="px-4 py-6 text-center rounded-lg border border-edge bg-elevated/60">
                <AlertCircle size={24} className="text-warning mx-auto mb-2" />
                <p className="text-sm text-muted">{messagesError}</p>
                <Button
                  variant="ghost"
                  size="sm"
                  className="mt-2"
                  onClick={() => selectedConversation && loadMessages(selectedConversation)}
                >
                  {t('common.retry')}
                </Button>
              </div>
            )}

            {/* ── Empty conversation ──────────────────────────────────── */}
            {selectedConversation && !isLoadingMessages && !messagesError && messages.length === 0 && (
              <div className="flex flex-col items-center justify-center h-full text-center">
                <Sparkles size={32} className="text-accent mb-3" />
                <p className="text-heading font-medium mb-1">{t('aiHub.emptyConversation')}</p>
                <p className="text-sm text-muted">{t('aiHub.startTyping')}</p>
              </div>
            )}

            {/* ── Messages list ───────────────────────────────────────── */}
            {!isLoadingMessages && !messagesError && messages.map(msg => (
              <ChatMessageItem
                key={msg.id}
                message={msg}
                isExpanded={expandedMeta === msg.id}
                onToggleMeta={toggleMeta}
                formatTime={formatTime}
              />
            ))}

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
            <SuggestedPrompts
              prompts={config.aiSuggestedPromptKeys}
              onSelect={setInputValue}
              visible={messages.length <= 2 && !messagesError}
            />

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
                placeholder={t('aiHub.inputPlaceholder')}
                className="flex-1 bg-elevated border border-edge rounded-lg px-4 py-2.5 text-sm text-body placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
                disabled={isTyping}
              />
              <Button variant="primary" size="md" disabled={!inputValue.trim() || isTyping} onClick={handleSendMessage}>
                <Send size={16} />
                {t('aiHub.send')}
              </Button>
            </div>

            {/* ── Selected model indicator ─────────────────────────────── */}
            <div className="flex items-center gap-2 mt-2">
              {selectedModelId && availableModels && (() => {
                const allModels = [...(availableModels.internalModels || []), ...(availableModels.externalModels || [])];
                const model = allModels.find(m => m.modelId === selectedModelId);
                if (!model) return null;
                return (
                  <span className="inline-flex items-center gap-1 text-[10px] text-muted">
                    {model.isInternal ? <Shield size={9} className="text-success" /> : <Globe size={9} className="text-warning" />}
                    {t('aiHub.usingModel')}: {model.displayName}
                    {model.isInternal ? ` (${t('aiHub.internalLabel')})` : ` (${t('aiHub.externalLabel')})`}
                  </span>
                );
              })()}
              <span className="flex-1" />
              <span className="inline-flex items-center gap-1 text-[10px] text-faded">
                <Info size={10} />
                {t('aiHub.governanceNotice')}
              </span>
            </div>
          </div>
        </div>

        {/* ── Agents Panel (sidebar) ─────────────────────────────────── */}
        <AgentsSidePanel
          agents={agents}
          isOpen={isAgentsPanelOpen}
          onClose={() => setIsAgentsPanelOpen(false)}
        />
      </div>
    </PageContainer>
  );
}
