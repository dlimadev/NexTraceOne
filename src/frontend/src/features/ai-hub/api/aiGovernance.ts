import client from '../../../api/client';
import { getAccessToken, getCsrfToken, getTenantId, getEnvironmentId } from '../../../utils/tokenStorage';
import type { AgentsResponse } from './AiAgentApiTypes';

/** Payload reutilizável para envio de mensagens ao assistente de IA. */
export type SendMessagePayload = {
  conversationId?: string;
  message: string;
  contextScope?: string;
  persona?: string;
  preferredModelId?: string;
  clientType?: string;
  serviceId?: string;
  contractId?: string;
  incidentId?: string;
  changeId?: string;
  teamId?: string;
  domainId?: string;
  contextBundle?: string;
};

export const aiGovernanceApi = {
  listModels: (params?: { provider?: string; modelType?: string; status?: string; isInternal?: boolean }) =>
    client.get('/ai/models', { params }).then(r => r.data),
  getModel: (modelId: string) =>
    client.get(`/ai/models/${modelId}`).then(r => r.data),
  registerModel: (data: unknown) =>
    client.post('/ai/models', data).then(r => r.data),
  updateModel: (modelId: string, data: unknown) =>
    client.patch(`/ai/models/${modelId}`, data).then(r => r.data),
  listPolicies: (params?: { scope?: string; isActive?: boolean }) =>
    client.get('/ai/policies', { params }).then(r => r.data),
  createPolicy: (data: unknown) =>
    client.post('/ai/policies', data).then(r => r.data),
  updatePolicy: (policyId: string, data: unknown) =>
    client.patch(`/ai/policies/${policyId}`, data).then(r => r.data),
  listBudgets: (params?: { scope?: string; isActive?: boolean }) =>
    client.get('/ai/budgets', { params }).then(r => r.data),
  updateBudget: (budgetId: string, data: unknown) =>
    client.patch(`/ai/budgets/${budgetId}`, data).then(r => r.data),
  listAuditEntries: (params?: Record<string, unknown>) =>
    client.get('/ai/audit', { params }).then(r => r.data),

  // ── IDE Integrations ──────────────────────────────────────────────
  getIdeSummary: () =>
    client.get('/ai/ide/summary').then(r => r.data),
  listIdeClients: (params?: { userId?: string; clientType?: string; isActive?: boolean; pageSize?: number }) =>
    client.get('/ai/ide/clients', { params }).then(r => r.data),
  registerIdeClient: (data: unknown) =>
    client.post('/ai/ide/clients/register', data).then(r => r.data),
  listIdeCapabilityPolicies: (params?: { clientType?: string; isActive?: boolean; pageSize?: number }) =>
    client.get('/ai/ide/policies', { params }).then(r => r.data),
  getIdeCapabilities: (params: { clientType: string; persona?: string | null }) =>
    client.get('/ai/ide/capabilities', { params }).then(r => r.data),

  // ── IDE Query Sessions (audit/history) ────────────────────────────
  submitIdeQuery: (data: {
    clientType?: string;
    clientVersion?: string;
    queryType?: string;
    queryText: string;
    context?: string;
    modelPreference?: string;
    serviceContext?: string;
    persona?: string;
  }) => client.post('/ai/ide/query', data).then(r => r.data),
  getIdeQuerySession: (sessionId: string) =>
    client.get(`/ai/ide/query/${sessionId}`).then(r => r.data),
  listIdeQuerySessions: (params?: { clientType?: string; status?: string }) =>
    client.get('/ai/ide/query', { params }).then(r => r.data),

  // ── Token usage (runtime) ─────────────────────────────────────────
  getTokenUsage: (params: { userId?: string; tenantId?: string }) =>
    client.get('/ai/token-usage', { params }).then(r => r.data),
  listKnowledgeSources: (params?: { sourceType?: string; isActive?: boolean }) =>
    client.get('/ai/knowledge-sources', { params }).then(r => r.data),
  sendMessage: (data: SendMessagePayload) =>
    client.post('/ai/assistant/chat', data).then(r => r.data),

  /**
   * Envia uma mensagem ao assistente via SSE streaming.
   * Consome o endpoint POST /api/v1/ai/assistant/chat/stream com ReadableStream.
   * Retorna um AbortController para cancelamento (ex: navegação).
   */
  sendMessageStreaming: (
    data: SendMessagePayload,
    onChunk: (text: string) => void,
    onComplete: (metadata?: unknown) => void,
    onError: (err: Error) => void,
  ): AbortController => {
    const controller = new AbortController();

    const run = async () => {
      try {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
        };
        const token = getAccessToken();
        if (token) headers['Authorization'] = `Bearer ${token}`;
        const tenantId = getTenantId();
        if (tenantId) headers['X-Tenant-Id'] = tenantId;
        const envId = getEnvironmentId();
        if (envId) headers['X-Environment-Id'] = envId;
        const csrf = getCsrfToken();
        if (csrf) headers['X-CSRF-Token'] = csrf;

        const response = await fetch('/api/v1/ai/assistant/chat/stream', {
          method: 'POST',
          headers,
          body: JSON.stringify(data),
          signal: controller.signal,
        });

        if (!response.ok || !response.body) {
          onError(new Error(`Stream request failed: ${response.status}`));
          return;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';
        let finalMetadata: unknown;

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() ?? '';

          for (const line of lines) {
            if (!line.startsWith('data: ')) continue;
            const payload = line.slice(6).trim();
            if (payload === '[DONE]') {
              onComplete(finalMetadata);
              return;
            }
            try {
              const parsed = JSON.parse(payload) as { text?: string; metadata?: unknown; done?: boolean };
              if (parsed.text) onChunk(parsed.text);
              if (parsed.metadata) finalMetadata = parsed.metadata;
              if (parsed.done) {
                onComplete(finalMetadata);
                return;
              }
            } catch {
              // linha SSE não-JSON — ignorar
            }
          }
        }

        onComplete(finalMetadata);
      } catch (err: unknown) {
        if (err instanceof DOMException && err.name === 'AbortError') return;
        onError(err instanceof Error ? err : new Error(String(err)));
      }
    };

    void run();
    return controller;
  },

  /** Stub: marca onboarding como completo para o utilizador no servidor. */
  completeOnboarding: (sessionId: string): Promise<void> => {
    // TODO: Implementar chamada API real quando backend estiver pronto
    void sessionId; // Suppress unused parameter warning
    return Promise.resolve();
  },
  listConversations: (params?: { userId?: string; pageSize?: number }) =>
    client.get('/ai/assistant/conversations', { params }).then(r => r.data),
  getConversation: (conversationId: string, params?: { messagePageSize?: number }) =>
    client.get(`/ai/assistant/conversations/${conversationId}`, { params }).then(r => r.data),
  createConversation: (data: {
    title: string;
    persona?: string;
    clientType?: string;
    defaultContextScope?: string;
    tags?: string;
    serviceId?: string;
    contractId?: string;
    incidentId?: string;
    changeId?: string;
    teamId?: string;
  }) =>
    client.post('/ai/assistant/conversations', data).then(r => r.data),
  updateConversation: (conversationId: string, data: { title?: string; tags?: string; archive?: boolean }) =>
    client.patch(`/ai/assistant/conversations/${conversationId}`, data).then(r => r.data),
  listMessages: (conversationId: string, params?: { pageSize?: number }) =>
    client.get(`/ai/assistant/conversations/${conversationId}/messages`, { params }).then(r => r.data),
  listSuggestedPrompts: (params?: { persona?: string; category?: string }) =>
    client.get('/ai/assistant/prompts', { params }).then(r => r.data),

  // ── AI Routing & Enrichment ────────────────────────────────────────
  planExecution: (data: {
    inputQuery: string;
    persona?: string;
    contextScope?: string;
    clientType: string;
    preferredModelId?: string;
    serviceId?: string;
    contractId?: string;
    incidentId?: string;
    changeId?: string;
  }) =>
    client.post('/ai/assistant/plan-execution', data).then(r => r.data),
  listRoutingStrategies: (params?: { isActive?: boolean }) =>
    client.get('/ai/routing/strategies', { params }).then(r => r.data),
  getRoutingDecision: (decisionId: string) =>
    client.get(`/ai/routing/decisions/${decisionId}`).then(r => r.data),
  listKnowledgeSourceWeights: (params?: { useCaseType?: string }) =>
    client.get('/ai/knowledge-sources/weights', { params }).then(r => r.data),
  enrichContext: (data: {
    inputQuery: string;
    persona?: string;
    useCaseType?: string;
    targetScope?: string;
    serviceId?: string;
    contractId?: string;
    incidentId?: string;
    changeId?: string;
  }) =>
    client.post('/ai/context/enrich', data).then(r => r.data),

  // ── AI Runtime ────────────────────────────────────────────────────
  chat: (data: {
    message: string;
    conversationId?: string;
    preferredModelId?: string;
    systemPrompt?: string;
    temperature?: number;
    maxTokens?: number;
  }) =>
    client.post('/ai/chat', data).then(r => r.data),
  listProviders: () =>
    client.get('/ai/providers').then(r => r.data),
  checkProvidersHealth: () =>
    client.get('/ai/providers/health').then(r => r.data),

  // ── Available Models (per-user authorization) ──────────────────────
  listAvailableModels: () =>
    client.get('/ai/models/available').then(r => r.data),

  // ── AI Agents ─────────────────────────────────────────────────────
  listAgents: async (params?: { isOfficial?: boolean }): Promise<AgentsResponse> => {
    const response = await client.get<AgentsResponse>('/ai/agents', { params });
    return response.data;
  },
  listAgentCategories: () =>
    client.get<{ items: string[] }>('/ai/agents/categories').then(r => r.data),
  listAgentsByContext: (context: string) =>
    client.get('/ai/agents/by-context', { params: { context } }).then(r => r.data),
  getAgent: (agentId: string) =>
    client.get(`/ai/agents/${agentId}`).then(r => r.data),
  createAgent: (data: {
    name: string;
    displayName: string;
    description: string;
    category: string;
    systemPrompt: string;
    objective?: string;
    ownershipType: string;
    visibility: string;
    capabilities?: string;
    targetPersona?: string;
    icon?: string;
    preferredModelId?: string | null;
    allowedModelIds?: string;
    allowedTools?: string;
    inputSchema?: string;
    outputSchema?: string;
    allowModelOverride?: boolean;
    sortOrder?: number;
  }) =>
    client.post('/ai/agents', data).then(r => r.data),
  updateAgent: (agentId: string, data: {
    displayName?: string;
    description?: string;
    systemPrompt?: string;
    objective?: string;
    capabilities?: string;
    targetPersona?: string;
    icon?: string;
    preferredModelId?: string | null;
    allowedModelIds?: string;
    allowedTools?: string;
    inputSchema?: string;
    outputSchema?: string;
    visibility?: string;
    allowModelOverride?: boolean;
    sortOrder?: number;
  }) =>
    client.put(`/ai/agents/${agentId}`, data).then(r => r.data),
  executeAgent: (agentId: string, data: {
    input: string;
    modelIdOverride?: string | null;
    contextJson?: string;
  }) =>
    client.post(`/ai/agents/${agentId}/execute`, data).then(r => r.data),
  getAgentExecution: (executionId: string) =>
    client.get(`/ai/agent-executions/${executionId}`).then(r => r.data),
  reviewArtifact: (artifactId: string, data: {
    decision: string;
    notes?: string;
  }) =>
    client.post(`/ai/artifacts/${artifactId}/review`, data).then(r => r.data),

  // ── AI Environment Analysis (Phase 7) ────────────────────────────────
  analyzeNonProdEnvironment: (data: {
    tenantId: string;
    environmentId: string;
    environmentName: string;
    environmentProfile: string;
    serviceFilter?: string[] | null;
    observationWindowDays: number;
    preferredProvider?: string | null;
  }) =>
    client.post('/aiorchestration/analysis/non-prod', data).then(r => r.data),

  compareEnvironments: (data: {
    tenantId: string;
    subjectEnvironmentId: string;
    subjectEnvironmentName: string;
    subjectEnvironmentProfile: string;
    referenceEnvironmentId: string;
    referenceEnvironmentName: string;
    referenceEnvironmentProfile: string;
    serviceFilter?: string[] | null;
    comparisonDimensions?: string[] | null;
    preferredProvider?: string | null;
  }) =>
    client.post('/aiorchestration/analysis/compare-environments', data).then(r => r.data),

  assessPromotionReadiness: (data: {
    tenantId: string;
    sourceEnvironmentId: string;
    sourceEnvironmentName: string;
    targetEnvironmentId: string;
    targetEnvironmentName: string;
    serviceName: string;
    version: string;
    releaseId?: string | null;
    observationWindowDays: number;
    preferredProvider?: string | null;
  }) =>
    client.post('/aiorchestration/analysis/promotion-readiness', data).then(r => r.data),

  // ── MCP Server ────────────────────────────────────────────────────────
  /** Retorna metadados do servidor MCP nativo: versão do protocolo, capacidades e tool count. */
  getMcpServerInfo: () =>
    client.get('/ai/mcp').then(r => r.data),

  /** Lista todas as tools disponíveis no servidor MCP no formato JSON Schema. */
  listMcpTools: (params?: { category?: string }) =>
    client.get('/ai/mcp/tools', { params }).then(r => r.data),

  // ── Wave BD: AI Organizational Intelligence & Memory Analytics ────────
  getOrganizationalMemoryHealthReport: (params: {
    tenantId: string;
    lookbackDays?: number;
    staleThresholdDays?: number;
  }) =>
    client.get('/ai/intelligence/memory-health', { params }).then(r => r.data),

  getAgentPerformanceBenchmarkReport: (params: {
    tenantId: string;
    minExecutions?: number;
  }) =>
    client.get('/ai/intelligence/agent-benchmark', { params }).then(r => r.data),

  getAiCapabilityMaturityReport: (params: {
    tenantId: string;
    lookbackDays?: number;
    pioneerThresholdPct?: number;
    minTeamExecutions?: number;
  }) =>
    client.get('/ai/intelligence/capability-maturity', { params }).then(r => r.data),
};
