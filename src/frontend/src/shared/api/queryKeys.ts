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
    finops: () => [...queryKeys.governance.all, 'finops'] as const,
  },

  // ── AI Hub ──
  ai: {
    all: ['ai'] as const,
    models: () => [...queryKeys.ai.all, 'models'] as const,
    policies: () => [...queryKeys.ai.all, 'policies'] as const,
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
