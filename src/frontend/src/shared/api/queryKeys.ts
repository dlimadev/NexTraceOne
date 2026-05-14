/**
 * NexTraceOne — Query Key Factory
 *
 * Padroniza todas as query keys da aplicação num único lugar.
 * Elimina strings soltas espalhadas pelos componentes e garante
 * invalidação consistente em mutations.
 *
 * Padrão: domínio → escopo → parâmetros → [environmentId]
 *
 * REGRA DE AMBIENTE E TENANT:
 * - Queries que retornam dados ambiente-específicos DEVEM incluir `envId` na key.
 * - O `envId` é sempre o último elemento para permitir invalidação por prefixo:
 *     queryClient.invalidateQueries({ queryKey: queryKeys.incidents.all })
 *   invalida tanto com como sem envId.
 * - O componente QueryContextSync em App.tsx já invalida todas as queries ao
 *   trocar de ambiente/tenant — o envId na key garante cache separado por ambiente.
 *
 * @example
 * const { activeEnvironmentId } = useEnvironment();
 * useQuery({ queryKey: queryKeys.incidents.list(params, activeEnvironmentId), queryFn: ... })
 *
 * @see https://tkdodo.eu/blog/effective-react-query-keys
 * @see src/App.tsx QueryContextSync
 */
export const queryKeys = {
  // ── Catalog ──
  catalog: {
    all: ['catalog'] as const,
    graph: (envId?: string | null) => [...queryKeys.catalog.all, 'graph', envId] as const,
    services: {
      all: () => [...queryKeys.catalog.all, 'services'] as const,
      list: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.catalog.services.all(), 'list', params, envId] as const,
      detail: (id: string) => [...queryKeys.catalog.services.all(), 'detail', id] as const,
      summary: (envId?: string | null) => [...queryKeys.catalog.services.all(), 'summary', envId] as const,
      scorecard: (serviceName: string, environment: string) =>
        [...queryKeys.catalog.services.all(), 'scorecard', serviceName, environment] as const,
      discovery: (envId?: string | null) => [...queryKeys.catalog.services.all(), 'discovery', envId] as const,
      maturity: (envId?: string | null) => [...queryKeys.catalog.services.all(), 'maturity', envId] as const,
      dxScore: (envId?: string | null) => [...queryKeys.catalog.services.all(), 'dx-score', envId] as const,
      dependencyDashboard: (envId?: string | null) => [...queryKeys.catalog.services.all(), 'dependency-dashboard', envId] as const,
      licenseCompliance: (envId?: string | null) => [...queryKeys.catalog.services.all(), 'license-compliance', envId] as const,
    },
    templates: {
      all: () => [...queryKeys.catalog.all, 'templates'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.catalog.templates.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.catalog.templates.all(), 'detail', id] as const,
    },
    sourceOfTruth: {
      all: () => [...queryKeys.catalog.all, 'source-of-truth'] as const,
      service: (serviceId: string) => [...queryKeys.catalog.sourceOfTruth.all(), 'service', serviceId] as const,
      contract: (contractVersionId: string) => [...queryKeys.catalog.sourceOfTruth.all(), 'contract', contractVersionId] as const,
    },
    impact: {
      all: () => [...queryKeys.catalog.all, 'impact'] as const,
      propagation: (nodeId: string, depth: number, envId?: string | null) =>
        [...queryKeys.catalog.impact.all(), 'propagation', nodeId, depth, envId] as const,
    },
    snapshots: {
      all: (envId?: string | null) => [...queryKeys.catalog.all, 'snapshots', envId] as const,
      diff: (fromSnapshot: string, toSnapshot: string, envId?: string | null) =>
        [...queryKeys.catalog.snapshots.all(envId), 'diff', fromSnapshot, toSnapshot] as const,
    },
    nodeHealth: {
      all: (category: string, envId?: string | null) =>
        [...queryKeys.catalog.all, 'node-health', category, envId] as const,
    },
    contracts: {
      pipeline: (envId?: string | null) => [...queryKeys.catalog.all, 'contracts', 'pipeline', envId] as const,
      securityGate: (envId?: string | null) => [...queryKeys.catalog.all, 'security-gate', envId] as const,
    },
  },

  // ── Contracts ──
  contracts: {
    all: ['contracts'] as const,
    list: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.contracts.all, 'list', params, envId] as const,
    detail: (id: string) => [...queryKeys.contracts.all, 'detail', id] as const,
    summary: (envId?: string | null) => [...queryKeys.contracts.all, 'summary', envId] as const,
  },

  // ── Change Governance ──
  changes: {
    all: ['changes'] as const,
    list: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.changes.all, 'list', params, envId] as const,
    detail: (id: string) => [...queryKeys.changes.all, 'detail', id] as const,
    summary: (envId?: string | null) => [...queryKeys.changes.all, 'summary', envId] as const,
    dora: (params?: Record<string, unknown>) => [...queryKeys.changes.all, 'dora', params] as const,
  },

  // ── Operations / Incidents ──
  incidents: {
    all: ['incidents'] as const,
    list: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.incidents.all, 'list', params, envId] as const,
    detail: (id: string) => [...queryKeys.incidents.all, 'detail', id] as const,
    summary: (envId?: string | null) => [...queryKeys.incidents.all, 'summary', envId] as const,
  },
  
  // ── Operations / Runtime Intelligence ──
  runtime: {
    all: ['runtime'] as const,
    requestMetrics: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.runtime.all, 'request-metrics', params, envId] as const,
    errorAnalytics: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.runtime.all, 'error-analytics', params, envId] as const,
    userActivity: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.runtime.all, 'user-activity', params, envId] as const,
    systemHealth: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.runtime.all, 'system-health', params, envId] as const,
    reliability: {
      all: () => [...queryKeys.runtime.all, 'reliability'] as const,
      service: (serviceId: string, envId?: string | null) =>
        [...queryKeys.runtime.reliability.all(), 'service', serviceId, envId] as const,
    },
  },

  // ── Governance ──
  governance: {
    all: ['governance'] as const,
    reports: (envId?: string | null) => [...queryKeys.governance.all, 'reports', envId] as const,
    risk: (envId?: string | null) => [...queryKeys.governance.all, 'risk', envId] as const,
    compliance: (envId?: string | null) => [...queryKeys.governance.all, 'compliance', envId] as const,
    waivers: () => [...queryKeys.governance.all, 'waivers'] as const,
    teams: () => [...queryKeys.governance.all, 'teams'] as const,
    teamDetail: (id: string) => [...queryKeys.governance.all, 'teams', id] as const,
    delegations: () => [...queryKeys.governance.all, 'delegations'] as const,
    domains: () => [...queryKeys.governance.all, 'domains'] as const,
    domainDetail: (id: string) => [...queryKeys.governance.all, 'domains', id] as const,
    packs: () => [...queryKeys.governance.all, 'packs'] as const,
    packDetail: (id: string) => [...queryKeys.governance.all, 'packs', id] as const,
    finops: {
      all: () => [...queryKeys.governance.all, 'finops'] as const,
      summary: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'summary', params, envId] as const,
      service: (id: string, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'service', id, envId] as const,
      team: (id: string, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'team', id, envId] as const,
      domain: (id: string, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'domain', id, envId] as const,
      trends: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'trends', params, envId] as const,
      waste: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'waste', params, envId] as const,
      efficiency: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.governance.finops.all(), 'efficiency', params, envId] as const,
      configuration: () => [...queryKeys.governance.finops.all(), 'configuration'] as const,
      budgetApprovals: (status?: string, serviceName?: string) =>
        [...queryKeys.governance.finops.all(), 'budget-approvals', status, serviceName] as const,
    },
    evidence: {
      all: () => [...queryKeys.governance.all, 'evidence'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.governance.evidence.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.governance.evidence.all(), 'detail', id] as const,
    },
    executive: {
      all: () => [...queryKeys.governance.all, 'executive'] as const,
      controls: (envId?: string | null) => [...queryKeys.governance.executive.all(), 'controls', envId] as const,
      heatmap: (dimension: string, envId?: string | null) =>
        [...queryKeys.governance.executive.all(), 'heatmap', dimension, envId] as const,
      scorecards: (dimension: string, envId?: string | null) =>
        [...queryKeys.governance.executive.all(), 'scorecards', dimension, envId] as const,
      drillDown: (entityType: string, entityId: string) =>
        [...queryKeys.governance.executive.all(), 'drillDown', entityType, entityId] as const,
    },
  },

  // ── Reliability ──
  reliability: {
    all: ['reliability'] as const,
    list: (params?: Record<string, unknown>, envId?: string | null) =>
      [...queryKeys.reliability.all, 'list', params, envId] as const,
    detail: (id: string) => [...queryKeys.reliability.all, 'detail', id] as const,
    teamSummary: (teamId: string, envId?: string | null) =>
      [...queryKeys.reliability.all, 'teamSummary', teamId, envId] as const,
  },

  // ── Platform Operations ──
  platformOps: {
    all: ['platformOps'] as const,
    health: () => [...queryKeys.platformOps.all, 'health'] as const,
  },

  // ── Product Analytics ──
  analytics: {
    all: ['analytics'] as const,
    journeys: (params?: Record<string, unknown>) => [...queryKeys.analytics.all, 'journeys', params] as const,
    milestones: (params?: Record<string, unknown>) => [...queryKeys.analytics.all, 'milestones', params] as const,
    personas: (params?: Record<string, unknown>) => [...queryKeys.analytics.all, 'personas', params] as const,
  },

  // ── AI Hub ──
  ai: {
    all: ['ai'] as const,
    models: {
      all: () => [...queryKeys.ai.all, 'models'] as const,
      list: () => [...queryKeys.ai.models.all(), 'list'] as const,
      detail: (id: string) => [...queryKeys.ai.models.all(), 'detail', id] as const,
    },
    policies: {
      all: () => [...queryKeys.ai.all, 'policies'] as const,
      list: () => [...queryKeys.ai.policies.all(), 'list'] as const,
    },
    conversations: {
      all: () => [...queryKeys.ai.all, 'conversations'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.ai.conversations.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.ai.conversations.all(), 'detail', id] as const,
    },
    agents: {
      all: () => [...queryKeys.ai.all, 'agents'] as const,
      marketplace: () => [...queryKeys.ai.agents.all(), 'marketplace'] as const,
      detail: (id: string) => [...queryKeys.ai.agents.all(), 'detail', id] as const,
    },
    routing: {
      all: () => [...queryKeys.ai.all, 'routing'] as const,
      configuration: () => [...queryKeys.ai.routing.all(), 'configuration'] as const,
    },
    tokenBudget: {
      all: () => [...queryKeys.ai.all, 'token-budget'] as const,
      summary: () => [...queryKeys.ai.tokenBudget.all(), 'summary'] as const,
    },
    memory: {
      all: () => [...queryKeys.ai.all, 'memory'] as const,
      intelligence: () => [...queryKeys.ai.memory.all(), 'intelligence'] as const,
    },
    copilot: {
      all: () => [...queryKeys.ai.all, 'copilot'] as const,
      sessions: () => [...queryKeys.ai.copilot.all(), 'sessions'] as const,
    },
  },

  // ── Identity ──
  identity: {
    all: ['identity'] as const,
    users: {
      all: () => [...queryKeys.identity.all, 'users'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.identity.users.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.identity.users.all(), 'detail', id] as const,
    },
    sessions: () => [...queryKeys.identity.all, 'sessions'] as const,
  },

  // ── Audit & Compliance ──
  audit: {
    all: ['audit'] as const,
    events: {
      all: () => [...queryKeys.audit.all, 'events'] as const,
      list: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.audit.events.all(), 'list', params, envId] as const,
      trail: (resourceType: string, resourceId: string, envId?: string | null) =>
        [...queryKeys.audit.events.all(), 'trail', resourceType, resourceId, envId] as const,
    },
    integrity: (envId?: string | null) => [...queryKeys.audit.all, 'integrity', envId] as const,
    compliance: {
      all: () => [...queryKeys.audit.all, 'compliance'] as const,
      report: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.audit.compliance.all(), 'report', params, envId] as const,
      policies: (envId?: string | null) => [...queryKeys.audit.compliance.all(), 'policies', envId] as const,
      results: (params?: Record<string, unknown>, envId?: string | null) =>
        [...queryKeys.audit.compliance.all(), 'results', params, envId] as const,
    },
    retention: {
      all: () => [...queryKeys.audit.all, 'retention'] as const,
      policies: () => [...queryKeys.audit.retention.all(), 'policies'] as const,
    },
    campaigns: {
      all: () => [...queryKeys.audit.all, 'campaigns'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.audit.campaigns.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.audit.campaigns.all(), 'detail', id] as const,
    },
  },

  // ── Configuration ──
  configuration: {
    all: ['configuration'] as const,
    apiKeys: {
      all: () => [...queryKeys.configuration.all, 'api-keys'] as const,
      list: () => [...queryKeys.configuration.apiKeys.all(), 'list'] as const,
    },
    userPreferences: {
      all: () => [...queryKeys.configuration.all, 'user-preferences'] as const,
      get: (userId: string) => [...queryKeys.configuration.userPreferences.all(), 'get', userId] as const,
    },
    environment: {
      all: () => [...queryKeys.configuration.all, 'environment'] as const,
      list: () => [...queryKeys.configuration.environment.all(), 'list'] as const,
      detail: (id: string) => [...queryKeys.configuration.environment.all(), 'detail', id] as const,
    },
  },

  // ── Notifications ──
  notifications: {
    all: ['notifications'] as const,
    list: (params?: Record<string, unknown>) => [...queryKeys.notifications.all, 'list', params] as const,
    unread: () => [...queryKeys.notifications.all, 'unread'] as const,
    preferences: {
      all: () => [...queryKeys.notifications.all, 'preferences'] as const,
      get: (userId: string) => [...queryKeys.notifications.preferences.all(), 'get', userId] as const,
    },
  },

  // ── Integrations ──
  integrations: {
    all: ['integrations'] as const,
    list: (params?: Record<string, unknown>) => [...queryKeys.integrations.all, 'list', params] as const,
    providers: () => [...queryKeys.integrations.all, 'providers'] as const,
  },

} as const;
