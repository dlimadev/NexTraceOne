# ADR-005: React 19 Frontend Stack

## Status

Accepted

## Date

2026-01-20

## Context

NexTraceOne requires an enterprise-grade frontend with:

- 113+ routes across 12 bounded context modules
- Persona-aware layouts (Engineer, Tech Lead, Architect, Executive, Admin, Auditor)
- Multi-locale support (en, pt-BR, pt-PT, es)
- Rich data visualization (charts, topology maps, code editors)
- Real-time updates for operational intelligence dashboards

The frontend must support self-hosted deployments on customer infrastructure.

## Decision

We chose the following frontend stack:

| Component | Choice | Rationale |
|-----------|--------|-----------|
| **Framework** | React 19 | Mature ecosystem, large talent pool, excellent TypeScript support |
| **Build Tool** | Vite 7 | Fast HMR, native ESM, excellent DX |
| **Routing** | react-router-dom v7 | Mature, well-documented, supports nested layouts |
| **Data Fetching** | TanStack Query v5 | Server state management with caching, retries, optimistic updates |
| **Styling** | Tailwind CSS 4 | Utility-first, design system friendly, tree-shakeable |
| **UI Primitives** | Radix UI | Accessible, unstyled primitives for custom design system |
| **Charts** | Apache ECharts | Rich visualization library with good React bindings |
| **Forms** | react-hook-form + zod | Performant forms with runtime validation |
| **i18n** | i18next | Battle-tested i18n with React integration |
| **Testing** | Vitest + Playwright | Fast unit tests + reliable E2E tests |

## Consequences

### Positive

- Large React ecosystem provides libraries for every need.
- Vite provides fast development experience and optimized production builds.
- TanStack Query eliminates most client-side state management complexity.
- Tailwind CSS ensures visual consistency across all 113 routes.
- i18next supports the 4-locale requirement with lazy loading.

### Negative

- React 19 is relatively new — some libraries may not be fully compatible.
- No built-in SSR (not needed for this SPA use case).
- Tailwind utility classes can be verbose in templates.

### Alternatives Considered

- **Angular**: Rejected due to smaller talent pool and heavier framework weight.
- **TanStack Router**: Evaluated but react-router-dom v7 was chosen for maturity and ecosystem familiarity.
- **Next.js**: Rejected — SSR is unnecessary for this enterprise SPA, and self-hosted deployment is simpler without it.
