import client from '../../../api/client';

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

  // ── Token usage (runtime) ─────────────────────────────────────────
  getTokenUsage: (params: { userId?: string; tenantId?: string }) =>
    client.get('/ai/token-usage', { params }).then(r => r.data),
  listKnowledgeSources: (params?: { sourceType?: string; isActive?: boolean }) =>
    client.get('/ai/knowledge-sources', { params }).then(r => r.data),
  sendMessage: (data: {
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
  }) =>
    client.post('/ai/assistant/chat', data).then(r => r.data),
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
};
