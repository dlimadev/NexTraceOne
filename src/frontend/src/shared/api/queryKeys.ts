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
    models: () => [...queryKeys.ai.all, 'models'] as const,
    policies: () => [...queryKeys.ai.all, 'policies'] as const,
    conversations: () => [...queryKeys.ai.all, 'conversations'] as const,
    agents: () => [...queryKeys.ai.all, 'agents'] as const,
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
} as const;
