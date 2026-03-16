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
    teamId?: string;
    domainId?: string;
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
  }) =>
    client.post('/ai/context/enrich', data).then(r => r.data),
};
