// ── Types ───────────────────────────────────────────────────────────────

export interface Conversation {
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

export interface ChatMessage {
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

export interface AvailableModelItem {
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

export interface AvailableModelsResponse {
  internalModels: AvailableModelItem[];
  externalModels: AvailableModelItem[];
  allowExternalModels: boolean;
  appliedPolicyName: string | null;
  totalCount: number;
}

export interface AgentItem {
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

export interface AgentsResponse {
  items: AgentItem[];
  totalCount: number;
}

export interface ConversationApiItem {
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

export interface MessageApiItem {
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

export interface ConversationDetailApiResponse {
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

export const contextScopes = ['Services', 'Contracts', 'Incidents', 'Changes', 'Runbooks'] as const;
export const conversationSearchParam = 'conversation';

export function normalizeContextScope(scope: string) {
  return contextScopes.find(candidate => candidate.toLowerCase() === scope.toLowerCase()) ?? scope;
}

export function mapMessage(item: MessageApiItem): ChatMessage {
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

export function getProblemStatus(error: unknown): number | null {
  if (typeof error !== 'object' || error === null || !('response' in error)) {
    return null;
  }

  const response = (error as { response?: { status?: number } }).response;
  return typeof response?.status === 'number' ? response.status : null;
}
