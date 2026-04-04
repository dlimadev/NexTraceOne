/**
 * NexTraceOne — Query Key Factory
 *
 * Padroniza todas as query keys da aplicação num único lugar.
 * Elimina strings soltas espalhadas pelos componentes e garante
 * invalidação consistente em mutations.
 *
 * Padrão: domínio → escopo → parâmetros
 *
 * @example
 * // Usar em useQuery:
 * useQuery({ queryKey: queryKeys.incidents.list(params), queryFn: ... })
 *
 * // Invalidar após mutation:
 * queryClient.invalidateQueries({ queryKey: queryKeys.incidents.all })
 *
 * @see https://tkdodo.eu/blog/effective-react-query-keys
 */
export const queryKeys = {
  // ── Catalog ──
  catalog: {
    all: ['catalog'] as const,
    graph: () => [...queryKeys.catalog.all, 'graph'] as const,
    services: {
      all: () => [...queryKeys.catalog.all, 'services'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.catalog.services.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.catalog.services.all(), 'detail', id] as const,
      summary: () => [...queryKeys.catalog.services.all(), 'summary'] as const,
    },
  },

  // ── Contracts ──
  contracts: {
    all: ['contracts'] as const,
    list: (params?: Record<string, unknown>) => [...queryKeys.contracts.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.contracts.all, 'detail', id] as const,
    summary: () => [...queryKeys.contracts.all, 'summary'] as const,
  },

  // ── Change Governance ──
  changes: {
    all: ['changes'] as const,
    list: (params?: Record<string, unknown>) => [...queryKeys.changes.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.changes.all, 'detail', id] as const,
    summary: () => [...queryKeys.changes.all, 'summary'] as const,
    dora: (params?: Record<string, unknown>) => [...queryKeys.changes.all, 'dora', params] as const,
  },

  // ── Operations / Incidents ──
  incidents: {
    all: ['incidents'] as const,
    list: (params?: Record<string, unknown>) => [...queryKeys.incidents.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.incidents.all, 'detail', id] as const,
    summary: () => [...queryKeys.incidents.all, 'summary'] as const,
  },

  // ── Governance ──
  governance: {
    all: ['governance'] as const,
    reports: () => [...queryKeys.governance.all, 'reports'] as const,
    risk: () => [...queryKeys.governance.all, 'risk'] as const,
    compliance: () => [...queryKeys.governance.all, 'compliance'] as const,
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
      summary: (params?: Record<string, unknown>) => [...queryKeys.governance.finops.all(), 'summary', params] as const,
      service: (id: string) => [...queryKeys.governance.finops.all(), 'service', id] as const,
      team: (id: string) => [...queryKeys.governance.finops.all(), 'team', id] as const,
      domain: (id: string) => [...queryKeys.governance.finops.all(), 'domain', id] as const,
      trends: (params?: Record<string, unknown>) => [...queryKeys.governance.finops.all(), 'trends', params] as const,
    },
    evidence: {
      all: () => [...queryKeys.governance.all, 'evidence'] as const,
      list: (params?: Record<string, unknown>) => [...queryKeys.governance.evidence.all(), 'list', params] as const,
      detail: (id: string) => [...queryKeys.governance.evidence.all(), 'detail', id] as const,
    },
    executive: {
      all: () => [...queryKeys.governance.all, 'executive'] as const,
      drillDown: (entityType: string, entityId: string) => [...queryKeys.governance.executive.all(), 'drillDown', entityType, entityId] as const,
    },
  },

  // ── Reliability ──
  reliability: {
    all: ['reliability'] as const,
    list: (params?: Record<string, unknown>) => [...queryKeys.reliability.all, 'list', params] as const,
    detail: (id: string) => [...queryKeys.reliability.all, 'detail', id] as const,
    teamSummary: (teamId: string) => [...queryKeys.reliability.all, 'teamSummary', teamId] as const,
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
