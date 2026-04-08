import * as React from 'react';
import { useState, useRef, useEffect, useCallback, type KeyboardEvent, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import {
  Bot,
  Send,
  MessageSquare,
  Plus,
  Shield,
  Cpu,
  Server,
  FileText,
  AlertTriangle,
  GitBranch,
  BookOpen,
  Info,
  ChevronDown,
  ChevronUp,
  Sparkles,
  Database,
  Eye,
  Link2,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Inbox,
  Globe,
  Lock,
  PanelLeftOpen,
  PanelLeftClose,
  LogOut,
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';
import { useAuth } from '../../../contexts/AuthContext';
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
  responseState?: string;
  isDegraded?: boolean;
  degradedReason?: string | null;
  timestamp: string;
}

// ── API Response Types ──────────────────────────────────────────────────

interface AvailableModelItem {
  modelId: string;
  name: string;
  displayName: string;
  provider: string;
  modelType: string;
  isInternal: boolean;
  isExternal: boolean;
  status: string;
  capabilities: string;
  isDefault: boolean;
  slug: string | null;
  contextWindow: number | null;
}

interface AvailableModelsResponse {
  internalModels: AvailableModelItem[];
  externalModels: AvailableModelItem[];
  allowExternalModels: boolean;
  appliedPolicyName: string | null;
  totalCount: number;
}

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
  responseState?: string;
  isDegraded?: boolean;
  degradedReason?: string | null;
}

interface ConversationDetailApiResponse {
  conversationId: string;
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
  messages: MessageApiItem[];
}

const contextScopes = ['Services', 'Contracts', 'Incidents', 'Changes', 'Runbooks'] as const;
const conversationSearchParam = 'conversation';

function normalizeContextScope(scope: string) {
  return contextScopes.find(candidate => candidate.toLowerCase() === scope.toLowerCase()) ?? scope;
}

function mapMessage(item: MessageApiItem): ChatMessage {
  const isAssistant = item.role === 'assistant';
  const isDegraded = item.isDegraded ?? false;

  return {
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
    responseState: item.responseState ?? (isAssistant ? (isDegraded ? 'Degraded' : 'Completed') : 'Completed'),
    isDegraded,
    degradedReason: item.degradedReason ?? null,
    useCaseType: 'General',
    routingPath: isDegraded ? 'ProviderUnavailable' : item.isInternalModel ? 'InternalOnly' : 'ExternalEscalation',
    confidenceLevel: isDegraded ? 'Low' : item.isInternalModel ? 'High' : 'Medium',
    costClass: isDegraded ? 'none' : item.isInternalModel ? 'low' : 'medium',
  };
}

function getProblemStatus(error: unknown): number | null {
  if (typeof error !== 'object' || error === null || !('response' in error)) {
    return null;
  }

  const response = (error as { response?: { status?: number } }).response;
  return typeof response?.status === 'number' ? response.status : null;
}

/** Props do AiCopilotPage. */
export interface AiCopilotPageProps {
  /** Modo full-screen sem PageContainer — usado pelo AiOnlyShell. Se não definido, auto-detecta pela persona AiUser. */
  fullScreen?: boolean;
}

/**
 * Página do AI Assistant com layout inspirado no Microsoft Copilot.
 *
 * Experiência centrada com:
 * - Área principal de chat com mensagens centradas
 * - Painel lateral de conversas recolhível
 * - Greeting hero quando não há mensagens
 * - Suggestion chips contextualizados por persona
 * - Input centralizado na parte inferior
 * - Opção full-screen para utilizadores AiUser (sem sidebar do sistema)
 */
export function AiCopilotPage({ fullScreen: fullScreenProp }: AiCopilotPageProps) {
  const { t } = useTranslation();
  const { persona, config } = usePersona();
  const { user, logout } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  // Auto-detect full-screen mode for AiUser persona
  const fullScreen = fullScreenProp ?? persona === 'AiUser';

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
  const [sidebarOpen, setSidebarOpen] = useState(false);

  // ── Model selection state ─────────────────────────────────────────────
  const [availableModels, setAvailableModels] = useState<AvailableModelsResponse | null>(null);
  const [selectedModelId, setSelectedModelId] = useState<string | null>(null);
  const [isModelSelectorOpen, setIsModelSelectorOpen] = useState(false);
  const modelSelectorRef = useRef<HTMLDivElement>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
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
      if (requestId !== conversationLoadRequestRef.current) return;

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
      if (requestId !== messageLoadRequestRef.current) return;

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

  // ── Load available models ─────────────────────────────────────────────
  useEffect(() => {
    aiGovernanceApi.listAvailableModels()
      .then((data: AvailableModelsResponse) => {
        setAvailableModels(data);
        const allModels = [...(data.internalModels || []), ...(data.externalModels || [])];
        const defaultModel = allModels.find(m => m.isDefault);
        if (defaultModel && !selectedModelId) {
          setSelectedModelId(defaultModel.modelId);
        }
      })
      .catch(() => {
        // Available models not loaded
      });
  // eslint-disable-next-line react-hooks/exhaustive-deps -- only run on mount
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
    setSidebarOpen(false);
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

  const hasMessages = messages.length > 0;
  const showHero = !selectedConversation || (!isLoadingMessages && !messagesError && messages.length === 0);

  return (
    <div className={`flex h-full ${fullScreen ? 'h-screen' : ''}`}>
      {/* ── Conversations sidebar (slide-over) ──────────────────────────── */}
      {sidebarOpen && (
        <>
          <div
            className="fixed inset-0 bg-black/30 z-40 lg:hidden"
            onClick={() => setSidebarOpen(false)}
          />
          <div className="fixed left-0 top-0 bottom-0 w-[320px] z-50 bg-card border-r border-edge flex flex-col shadow-lg animate-fade-in lg:relative lg:shadow-none lg:z-auto">
            <div className="px-4 py-4 border-b border-edge flex items-center justify-between">
              <h2 className="text-sm font-semibold text-heading">{t('aiHub.conversations')}</h2>
              <div className="flex items-center gap-2">
                <Button variant="ghost" size="sm" onClick={handleNewConversation}>
                  <Plus size={16} />
                </Button>
                <button onClick={() => setSidebarOpen(false)} className="text-muted hover:text-body transition-colors">
                  <PanelLeftClose size={16} />
                </button>
              </div>
            </div>
            <div className="flex-1 overflow-y-auto">
              {isLoadingConversations && (
                <div className="flex items-center justify-center py-8">
                  <Loader2 size={20} className="animate-spin text-muted" />
                </div>
              )}
              {conversationsError && !isLoadingConversations && (
                <div className="px-4 py-6 text-center">
                  <AlertCircle size={24} className="text-warning mx-auto mb-2" />
                  <p className="text-sm text-muted">{conversationsError}</p>
                  <Button variant="ghost" size="sm" className="mt-2" onClick={() => void loadConversations()}>
                    {t('common.retry')}
                  </Button>
                </div>
              )}
              {!isLoadingConversations && !conversationsError && conversations.length === 0 && (
                <div className="px-4 py-8 text-center">
                  <Inbox size={32} className="text-muted mx-auto mb-3" />
                  <p className="text-sm text-heading mb-1">{t('aiHub.noConversations')}</p>
                  <p className="text-xs text-muted mb-4">{t('aiHub.startNewConversation')}</p>
                </div>
              )}
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
                    <span>{conv.messageCount} {t('aiHub.messages')}</span>
                    {conv.lastModelUsed && (
                      <>
                        <span>·</span>
                        <Cpu size={10} />
                      </>
                    )}
                  </div>
                </button>
              ))}
            </div>
          </div>
        </>
      )}

      {/* ── Main chat area ───────────────────────────────────────────────── */}
      <div className="flex-1 flex flex-col min-w-0 relative">
        {/* ── Top bar ──────────────────────────────────────────────────── */}
        <div className="flex items-center justify-between px-4 sm:px-6 py-3 border-b border-edge bg-card/80 backdrop-blur-sm">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setSidebarOpen(prev => !prev)}
              className="p-1.5 rounded-lg text-muted hover:text-body hover:bg-hover transition-colors"
              title={t('aiHub.copilot.toggleHistory')}
            >
              <PanelLeftOpen size={18} />
            </button>
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-full bg-gradient-to-br from-accent to-blue flex items-center justify-center">
                <Bot size={16} className="text-white" />
              </div>
              <div>
                <h1 className="text-sm font-semibold text-heading">{t('aiHub.copilot.title')}</h1>
                {selectedConv && (
                  <p className="text-[11px] text-muted truncate max-w-[200px]">{selectedConv.title}</p>
                )}
              </div>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {backendConnected === true && (
              <span className="hidden sm:inline-flex items-center gap-1 text-[10px] text-success">
                <CheckCircle2 size={10} />
                {providerStatus}
              </span>
            )}
            {backendConnected === false && (
              <span className="hidden sm:inline-flex items-center gap-1 text-[10px] text-warning">
                <AlertCircle size={10} />
                {providerStatus}
              </span>
            )}

            {/* Model Selector */}
            {availableModels && (
              <div className="relative" ref={modelSelectorRef}>
                <button
                  onClick={() => setIsModelSelectorOpen(prev => !prev)}
                  className="inline-flex items-center gap-1.5 px-2 py-1.5 rounded-md border border-edge text-xs font-medium text-body hover:bg-hover transition-colors"
                >
                  {(() => {
                    const allModels = [...(availableModels.internalModels || []), ...(availableModels.externalModels || [])];
                    const selected = allModels.find(m => m.modelId === selectedModelId);
                    if (selected) {
                      return (
                        <>
                          {selected.isInternal ? <Shield size={12} className="text-success" /> : <Globe size={12} className="text-warning" />}
                          <span className="max-w-[100px] truncate hidden sm:inline">{selected.displayName}</span>
                        </>
                      );
                    }
                    return <Cpu size={12} className="text-muted" />;
                  })()}
                  {isModelSelectorOpen ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
                </button>

                {isModelSelectorOpen && (
                  <div className="absolute right-0 top-full mt-1 w-[300px] bg-card border border-edge rounded-lg shadow-lg z-50 max-h-[400px] overflow-y-auto">
                    {availableModels.appliedPolicyName && (
                      <div className="px-3 py-2 border-b border-edge flex items-center gap-1.5 text-[10px] text-muted">
                        <Lock size={10} />
                        {t('aiHub.policyApplied')}: {availableModels.appliedPolicyName}
                      </div>
                    )}
                    {availableModels.internalModels.length > 0 && (
                      <div>
                        <div className="px-3 py-2 border-b border-edge flex items-center gap-1.5">
                          <Shield size={12} className="text-success" />
                          <span className="text-xs font-semibold text-heading">{t('aiHub.internalModels')}</span>
                        </div>
                        {availableModels.internalModels.map(model => (
                          <button
                            key={model.modelId}
                            onClick={() => { setSelectedModelId(model.modelId); setIsModelSelectorOpen(false); }}
                            className={`w-full text-left px-3 py-2 hover:bg-hover transition-colors ${
                              selectedModelId === model.modelId ? 'bg-accent/10 border-l-2 border-accent' : ''
                            }`}
                          >
                            <span className="text-xs font-medium text-heading">{model.displayName}</span>
                            <div className="flex items-center gap-2 mt-0.5">
                              <span className="text-[10px] text-muted">{model.provider}</span>
                              {model.contextWindow && (
                                <span className="text-[10px] text-muted">· {(model.contextWindow / 1000).toFixed(0)}k</span>
                              )}
                            </div>
                          </button>
                        ))}
                      </div>
                    )}
                    {availableModels.allowExternalModels && availableModels.externalModels.length > 0 && (
                      <div>
                        <div className="px-3 py-2 border-b border-t border-edge flex items-center gap-1.5">
                          <Globe size={12} className="text-warning" />
                          <span className="text-xs font-semibold text-heading">{t('aiHub.externalModels')}</span>
                        </div>
                        {availableModels.externalModels.map(model => (
                          <button
                            key={model.modelId}
                            onClick={() => { setSelectedModelId(model.modelId); setIsModelSelectorOpen(false); }}
                            className={`w-full text-left px-3 py-2 hover:bg-hover transition-colors ${
                              selectedModelId === model.modelId ? 'bg-accent/10 border-l-2 border-accent' : ''
                            }`}
                          >
                            <span className="text-xs font-medium text-heading">{model.displayName}</span>
                            <div className="flex items-center gap-2 mt-0.5">
                              <span className="text-[10px] text-muted">{model.provider}</span>
                            </div>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            )}

            <Button variant="ghost" size="sm" onClick={handleNewConversation} title={t('aiHub.newConversation')}>
              <Plus size={16} />
            </Button>

            {/* Logout for AI-only users */}
            {fullScreen && (
              <button
                onClick={() => logout()}
                className="p-1.5 rounded-lg text-muted hover:text-body hover:bg-hover transition-colors"
                title={t('auth.logout')}
              >
                <LogOut size={16} />
              </button>
            )}
          </div>
        </div>

        {/* ── Messages area ────────────────────────────────────────────── */}
        <div className="flex-1 overflow-y-auto">
          <div className="max-w-3xl mx-auto px-4 sm:px-6 py-6">
            {/* ── Loading messages ─────────────────────────────────────── */}
            {isLoadingMessages && (
              <div className="flex items-center justify-center py-12">
                <Loader2 size={24} className="animate-spin text-muted mr-2" />
                <span className="text-sm text-muted">{t('aiHub.loadingMessages')}</span>
              </div>
            )}

            {/* ── Error state ──────────────────────────────────────────── */}
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

            {/* ── Hero / Welcome screen (Copilot-style) ───────────────── */}
            {showHero && !isLoadingMessages && !messagesError && (
              <div className="flex flex-col items-center justify-center min-h-[50vh] text-center animate-fade-in">
                <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-accent to-blue flex items-center justify-center mb-6 shadow-lg">
                  <Bot size={32} className="text-white" />
                </div>
                <h2 className="text-2xl font-semibold text-heading mb-2">
                  {t('aiHub.copilot.greeting', { name: user?.firstName || user?.fullName?.split(' ')[0] || '' })}
                </h2>
                <p className="text-base text-muted mb-8 max-w-md">
                  {t('aiHub.copilot.subtitle')}
                </p>

                {/* ── Suggestion chips ─────────────────────────────────── */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full max-w-lg">
                  {config.aiSuggestedPromptKeys.map((promptKey, idx) => (
                    <button
                      key={idx}
                      onClick={() => {
                        setInputValue(t(promptKey));
                        inputRef.current?.focus();
                      }}
                      className="group text-left px-4 py-3 rounded-xl border border-edge bg-card hover:bg-hover hover:border-accent/30 transition-all duration-200 shadow-sm hover:shadow-md"
                    >
                      <div className="flex items-start gap-2.5">
                        <Sparkles size={16} className="text-accent shrink-0 mt-0.5 group-hover:scale-110 transition-transform" />
                        <span className="text-sm text-body leading-relaxed">{t(promptKey)}</span>
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* ── Messages ─────────────────────────────────────────────── */}
            {!isLoadingMessages && !messagesError && hasMessages && (
              <div className="space-y-6">
                {messages.map(msg => (
                  <div key={msg.id} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                    <div className={`max-w-[85%] ${msg.role === 'user' ? '' : 'flex gap-3'}`}>
                      {/* Avatar for assistant */}
                      {msg.role === 'assistant' && (
                        <div className="w-7 h-7 rounded-full bg-gradient-to-br from-accent to-blue flex items-center justify-center shrink-0 mt-1">
                          <Bot size={14} className="text-white" />
                        </div>
                      )}

                      <div className={`rounded-2xl px-4 py-3 ${
                        msg.role === 'assistant'
                          ? 'bg-elevated'
                          : 'bg-accent text-white'
                      }`}>
                        {/* Header badges for assistant */}
                        {msg.role === 'assistant' && (
                          <div className="flex items-center gap-1.5 mb-2 flex-wrap">
                            {msg.responseState === 'Degraded' && (
                              <Badge variant="warning">
                                <AlertCircle size={10} className="mr-0.5" />
                                {t('aiHub.responseStateDegraded')}
                              </Badge>
                            )}
                            {msg.modelName && (
                              <Badge variant={msg.isInternalModel ? 'info' : 'warning'}>
                                <Cpu size={10} className="mr-0.5" />
                                {msg.modelName}
                              </Badge>
                            )}
                            {msg.confidenceLevel && (
                              <Badge
                                variant={msg.confidenceLevel === 'High' ? 'success' : msg.confidenceLevel === 'Medium' ? 'warning' : 'default'}
                              >
                                <CheckCircle2 size={10} className="mr-0.5" />
                                {msg.confidenceLevel === 'High'
                                  ? t('aiHub.trustGrounded')
                                  : msg.confidenceLevel === 'Medium'
                                    ? t('aiHub.trustPartialContext')
                                    : t('aiHub.trustLimitedContext')}
                              </Badge>
                            )}
                            <span className="text-[10px] text-faded ml-auto">{formatTime(msg.timestamp)}</span>
                          </div>
                        )}

                        {/* User timestamp */}
                        {msg.role === 'user' && (
                          <div className="flex items-center gap-2 mb-1 justify-end">
                            <span className="text-[10px] text-white/70">{formatTime(msg.timestamp)}</span>
                          </div>
                        )}

                        {/* Content */}
                        <p className={`text-sm whitespace-pre-wrap leading-relaxed ${
                          msg.role === 'user' ? 'text-white' : 'text-body'
                        }`}>{msg.content}</p>

                        {/* Grounding Sources */}
                        {msg.groundingSources && msg.groundingSources.length > 0 && (
                          <div className="mt-2 flex items-center gap-1 flex-wrap">
                            <Database size={12} className="text-muted shrink-0" />
                            <span className="text-xs text-muted">{t('aiHub.groundingSources')}:</span>
                            {msg.groundingSources.map(src => (
                              <Badge key={src} variant="default">{src}</Badge>
                            ))}
                          </div>
                        )}

                        {/* Context References */}
                        {msg.contextReferences && msg.contextReferences.length > 0 && (
                          <div className="mt-1.5 flex items-center gap-1 flex-wrap">
                            <Link2 size={12} className="text-muted shrink-0" />
                            <span className="text-xs text-muted">{t('aiHub.contextRefs')}:</span>
                            {msg.contextReferences.map(ref => (
                              <span key={ref} className="text-xs text-accent bg-accent/10 px-1.5 py-0.5 rounded">{ref}</span>
                            ))}
                          </div>
                        )}

                        {/* Metadata toggle */}
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
                                  <span className="text-muted">{t('aiHub.metaResponseState')}:</span>
                                  <span className="text-body">{msg.responseState ?? t('aiHub.responseStateCompleted')}</span>
                                  <span className="text-muted">{t('aiHub.metaModel')}:</span>
                                  <span className="text-body">{msg.modelName ?? '—'}</span>
                                  <span className="text-muted">{t('aiHub.metaProvider')}:</span>
                                  <span className="text-body">{msg.provider ?? '—'}</span>
                                  <span className="text-muted">{t('aiHub.metaModelType')}:</span>
                                  <span className="text-body">
                                    {msg.isInternalModel
                                      ? <span className="text-success">{t('aiHub.internalLabel')}</span>
                                      : <span className="text-warning">{t('aiHub.externalLabel')}</span>}
                                  </span>
                                  <span className="text-muted">{t('aiHub.metaPromptTokens')}:</span>
                                  <span className="text-body">{msg.promptTokens ?? 0}</span>
                                  <span className="text-muted">{t('aiHub.metaCompletionTokens')}:</span>
                                  <span className="text-body">{msg.completionTokens ?? 0}</span>
                                  <span className="text-muted">{t('aiHub.metaPolicy')}:</span>
                                  <span className="text-body">{msg.appliedPolicyName ?? t('aiHub.metaNoneApplied')}</span>
                                  <span className="text-muted">{t('aiHub.metaCorrelation')}:</span>
                                  <span className="text-body font-mono text-[10px]">{msg.correlationId}</span>
                                  <span className="text-muted">{t('aiHub.metaConfidence')}:</span>
                                  <span className="text-body">{msg.confidenceLevel ?? '—'}</span>
                                  <span className="text-muted">{t('aiHub.metaCostClass')}:</span>
                                  <span className="text-body">{msg.costClass ?? '—'}</span>
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
                  </div>
                ))}

                {/* Typing indicator */}
                {isTyping && (
                  <div className="flex justify-start">
                    <div className="flex gap-3">
                      <div className="w-7 h-7 rounded-full bg-gradient-to-br from-accent to-blue flex items-center justify-center shrink-0">
                        <Bot size={14} className="text-white" />
                      </div>
                      <div className="bg-elevated rounded-2xl px-4 py-3 flex items-center gap-2">
                        <span className="text-xs text-muted animate-pulse">{t('aiHub.typing')}</span>
                        <span className="flex gap-1">
                          <span className="w-1.5 h-1.5 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                          <span className="w-1.5 h-1.5 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                          <span className="w-1.5 h-1.5 bg-accent/60 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                        </span>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>
        </div>

        {/* ── Bottom input area ─────────────────────────────────────────── */}
        <div className="border-t border-edge bg-card/80 backdrop-blur-sm">
          <div className="max-w-3xl mx-auto px-4 sm:px-6 py-4">
            {/* Context scope chips */}
            <div className="flex items-center gap-2 flex-wrap mb-3">
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

            {/* Input */}
            <div className="flex items-end gap-3">
              <div className="flex-1 relative">
                <textarea
                  ref={inputRef}
                  value={inputValue}
                  onChange={e => setInputValue(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder={t('aiHub.copilot.inputPlaceholder')}
                  rows={1}
                  className="w-full bg-elevated border border-edge rounded-2xl px-4 py-3 pr-12 text-sm text-body placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent resize-none overflow-hidden"
                  disabled={isTyping}
                  style={{ minHeight: '44px', maxHeight: '120px' }}
                  onInput={e => {
                    const target = e.target as HTMLTextAreaElement;
                    target.style.height = 'auto';
                    target.style.height = `${Math.min(target.scrollHeight, 120)}px`;
                  }}
                />
                <button
                  className={`absolute right-2 bottom-2 p-1.5 rounded-full transition-colors ${
                    inputValue.trim() && !isTyping
                      ? 'bg-accent text-white hover:bg-accent/90'
                      : 'bg-elevated text-muted cursor-not-allowed'
                  }`}
                  disabled={!inputValue.trim() || isTyping}
                  onClick={handleSendMessage}
                >
                  <Send size={16} />
                </button>
              </div>
            </div>

            {/* Footer info */}
            <div className="flex items-center gap-2 mt-2">
              {selectedModelId && availableModels && (() => {
                const allModels = [...(availableModels.internalModels || []), ...(availableModels.externalModels || [])];
                const model = allModels.find(m => m.modelId === selectedModelId);
                if (!model) return null;
                return (
                  <span className="inline-flex items-center gap-1 text-[10px] text-muted">
                    {model.isInternal ? <Shield size={9} className="text-success" /> : <Globe size={9} className="text-warning" />}
                    {model.displayName}
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
      </div>
    </div>
  );
}
