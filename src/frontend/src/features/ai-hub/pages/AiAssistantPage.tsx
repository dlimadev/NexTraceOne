import { useState } from 'react';
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
} from 'lucide-react';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { usePersona } from '../../../contexts/PersonaContext';

interface Conversation {
  id: string;
  topic: string;
  service: string;
  startedAt: string;
  turnCount: number;
  status: string;
}

interface ChatMessage {
  role: 'assistant' | 'user';
  content: string;
  model?: string;
  isInternal?: boolean;
  groundingSources?: string[];
}

const mockConversations: Conversation[] = [
  { id: '1', topic: 'Payment API issues', service: 'payment-service', startedAt: '2026-03-15T10:00:00Z', turnCount: 5, status: 'Active' },
  { id: '2', topic: 'Contract compatibility check', service: 'order-service', startedAt: '2026-03-14T14:30:00Z', turnCount: 3, status: 'Completed' },
  { id: '3', topic: 'Incident correlation analysis', service: 'notification-service', startedAt: '2026-03-13T09:00:00Z', turnCount: 8, status: 'Completed' },
];

const mockMessages: ChatMessage[] = [
  {
    role: 'assistant',
    content: 'Welcome! I\'m the NexTraceOne AI Assistant. I can help you investigate production issues, analyze contracts, correlate incidents, and provide operational insights. What would you like to explore?',
    model: 'NexTrace-Internal-v1',
    isInternal: true,
    groundingSources: ['Service Catalog', 'Contract Registry'],
  },
];

const contextScopes = ['Services', 'Contracts', 'Incidents', 'Changes', 'Runbooks'] as const;

/**
 * Página do AI Assistant — assistente IA contextualizado estilo Copilot.
 * A experiência adapta-se à persona do utilizador: contextos padrão,
 * prompts sugeridos e escopo de IA variam por perfil.
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

  const toggleContext = (ctx: string) => {
    setActiveContexts(prev =>
      prev.includes(ctx) ? prev.filter(c => c !== ctx) : [...prev, ctx],
    );
  };

  const contextIcons: Record<string, React.ReactNode> = {
    Services: <Server size={14} />,
    Contracts: <FileText size={14} />,
    Incidents: <AlertTriangle size={14} />,
    Changes: <GitBranch size={14} />,
    Runbooks: <BookOpen size={14} />,
  };

  return (
    <div className="p-6 lg:p-8 animate-fade-in h-[calc(100vh-4rem)]">
      <div className="flex h-full gap-4">
        {/* Sidebar — lista de conversas */}
        <div className="w-[280px] shrink-0 bg-card rounded-lg border border-edge flex flex-col">
          <div className="px-4 py-3 border-b border-edge flex items-center justify-between">
            <h2 className="text-sm font-semibold text-heading">{t('aiHub.conversations')}</h2>
            <Button variant="ghost" size="sm">
              <Plus size={16} />
            </Button>
          </div>
          <div className="flex-1 overflow-y-auto">
            {mockConversations.map(conv => (
              <button
                key={conv.id}
                onClick={() => setSelectedConversation(conv.id)}
                className={`w-full text-left px-4 py-3 border-b border-edge transition-colors ${
                  selectedConversation === conv.id ? 'bg-hover' : 'hover:bg-hover'
                }`}
              >
                <div className="flex items-center gap-2 mb-1">
                  <MessageSquare size={14} className="text-muted shrink-0" />
                  <span className="text-sm font-medium text-heading truncate">{conv.topic}</span>
                </div>
                <div className="flex items-center gap-2 text-xs text-muted">
                  <span>{conv.service}</span>
                  <span>·</span>
                  <span>{conv.turnCount} {t('aiHub.turns')}</span>
                </div>
                <div className="mt-1">
                  <Badge variant={conv.status === 'Active' ? 'success' : 'default'}>
                    {conv.status === 'Active' ? t('aiHub.statusActive') : t('aiHub.statusCompleted')}
                  </Badge>
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Área principal de chat */}
        <div className="flex-1 bg-card rounded-lg border border-edge flex flex-col min-w-0">
          {/* Cabeçalho */}
          <div className="px-6 py-3 border-b border-edge flex items-center justify-between">
            <div className="flex items-center gap-3">
              <Bot size={20} className="text-accent" />
              <h1 className="text-base font-semibold text-heading">{t('aiHub.assistantTitle')}</h1>
            </div>
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-1.5">
                <User size={14} className="text-muted" />
                <span className="text-xs text-muted">{t('aiHub.persona')}: {t(`persona.${persona}.label`)}</span>
              </div>
              <Badge variant="info">
                <div className="flex items-center gap-1">
                  <Shield size={12} />
                  {t('aiHub.internalAi')}
                </div>
              </Badge>
            </div>
          </div>

          {/* Mensagens */}
          <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">
            {mockMessages.map((msg, idx) => (
              <div key={idx} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div className={`max-w-[70%] rounded-lg px-4 py-3 ${
                  msg.role === 'assistant' ? 'bg-elevated' : 'bg-accent/20'
                }`}>
                  {msg.role === 'assistant' && (
                    <div className="flex items-center gap-2 mb-2">
                      <Bot size={14} className="text-accent" />
                      <span className="text-xs font-medium text-accent">{t('aiHub.assistant')}</span>
                      {msg.model && (
                        <Badge variant={msg.isInternal ? 'info' : 'warning'}>
                          <div className="flex items-center gap-1">
                            <Cpu size={10} />
                            {msg.model}
                          </div>
                        </Badge>
                      )}
                    </div>
                  )}
                  <p className="text-sm text-body">{msg.content}</p>
                  {msg.groundingSources && msg.groundingSources.length > 0 && (
                    <div className="mt-2 flex items-center gap-1 flex-wrap">
                      <span className="text-xs text-muted">{t('aiHub.groundingSources')}:</span>
                      {msg.groundingSources.map(src => (
                        <Badge key={src} variant="default">{src}</Badge>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            ))}

            {/* Prompts sugeridos — adaptados à persona */}
            <div className="pt-4">
              <p className="text-xs text-muted mb-3">{t('aiHub.suggestedPrompts')}</p>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                {config.aiSuggestedPromptKeys.map((promptKey, idx) => (
                  <button
                    key={idx}
                    onClick={() => setInputValue(t(promptKey))}
                    className="text-left px-3 py-2 rounded-md border border-edge text-sm text-body hover:bg-hover transition-colors"
                  >
                    {t(promptKey)}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Seletor de contexto */}
          <div className="px-6 py-2 border-t border-edge">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-xs text-muted">{t('aiHub.contextScope')}:</span>
              {contextScopes.map(ctx => (
                <button
                  key={ctx}
                  onClick={() => toggleContext(ctx)}
                  className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
                    activeContexts.includes(ctx)
                      ? 'bg-accent/20 text-accent'
                      : 'bg-elevated text-muted hover:text-body'
                  }`}
                >
                  {contextIcons[ctx]}
                  {t(`aiHub.context${ctx}`)}
                </button>
              ))}
            </div>
          </div>

          {/* Campo de entrada */}
          <div className="px-6 py-4 border-t border-edge">
            <div className="flex items-center gap-3">
              <input
                type="text"
                value={inputValue}
                onChange={e => setInputValue(e.target.value)}
                placeholder={t('aiHub.inputPlaceholder')}
                className="flex-1 bg-elevated border border-edge rounded-lg px-4 py-2.5 text-sm text-body placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
              />
              <Button variant="primary" size="md" disabled={!inputValue.trim()}>
                <Send size={16} />
                {t('aiHub.send')}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
