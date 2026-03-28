# Relatório de Estado do Frontend — NexTraceOne
**Auditoria Forense | 28 de Março de 2026**

---

## Objetivo da Área no Contexto do Produto

O frontend é a superfície operacional e de governança do NexTraceOne — deve traduzir a visão de produto para Engineer, Tech Lead, Architect, Executive, Platform Admin e Auditor com contexto de ambiente, ownership, contratos e mudanças.

---

## Stack Real vs. Stack Alvo (CLAUDE.md)

| Componente | Alvo (CLAUDE.md) | Real (package.json) | Status |
|---|---|---|---|
| React | 18 | **19.2.0** | ⚠️ DIVERGE |
| Router | TanStack Router | **react-router-dom v7.13.1** | ❌ DIVERGE |
| State management | Zustand | **Não presente** | ❌ AUSENTE |
| UI Components | Radix UI | **Não presente** | ❌ AUSENTE |
| Charts | Apache ECharts | **Não presente** | ❌ AUSENTE |
| Data fetching | TanStack Query | ✅ @tanstack/react-query v5.90 | ✅ OK |
| Forms | — | react-hook-form v7 + zod v4 | OK |
| Styling | Tailwind CSS | ✅ tailwindcss v4 (via @tailwindcss/vite) | ✅ OK |
| HTTP client | — | axios v1.13 | OK |
| i18n | i18n | ✅ i18next v25 + react-i18next v16 | ✅ OK |
| Bundler | Vite | ✅ @vitejs/plugin-react | ✅ OK |
| TypeScript | TypeScript | ✅ | ✅ OK |
| E2E testing | Playwright | ✅ @playwright/test | ✅ OK |
| Unit testing | Vitest | ✅ (via @vitest/coverage-v8) | ✅ OK |
| MSW (mock service worker) | — | ✅ msw | Presente para testes |

**Impacto do desvio:** A ausência de Radix UI significa que o design system usa componentes próprios (confirmado: `src/components/` com 50+ componentes customizados). A ausência de Apache ECharts implica que gráficos usam outra biblioteca ou são placeholders. A ausência de Zustand implica que estado global é gerido apenas com React Context ou TanStack Query.

---

## Estrutura de Ficheiros

| Área | Ficheiros |
|---|---|
| `src/features/` (total) | 235+ TypeScript/TSX |
| `src/components/` (shared) | 50+ componentes |
| `src/routes/` | 7 ficheiros de rotas |
| `src/api/` | `client.ts` + `index.ts` |
| `src/locales/` | i18n translations |
| `src/auth/` | auth flow |
| `src/contexts/` | React contexts |
| `src/hooks/` | custom hooks |
| `src/__tests__/` | testes unitários |
| `src/shared/design-system/` | foundations.ts |
| `e2e/` | specs Playwright mock |
| `e2e-real/` | specs real-environment |

---

## Features e Páginas — Estado Detalhado

### Catalog (`src/features/catalog/`)
**Rotas:** `catalogRoutes.tsx`

| Página | Status | Evidência |
|---|---|---|
| ServiceCatalogListPage | ✅ REAL | Conectada à API real de serviços |
| ServiceCatalogPage | ✅ REAL | — |
| ServiceDetailPage | ✅ REAL | — |
| ContractsPage | ✅ REAL | — |
| ContractListPage | ✅ REAL | — |
| ContractDetailPage | ✅ REAL | — |
| SourceOfTruthExplorerPage | ✅ REAL | — |
| ServiceSourceOfTruthPage | ✅ REAL | — |
| ContractSourceOfTruthPage | ✅ REAL | — |
| CatalogContractsConfigurationPage | ✅ REAL | — |
| DeveloperPortalPage | ⚠️ PARCIAL | 7 endpoints backend stub |
| GlobalSearchPage | ⚠️ PARCIAL | GlobalSearch real; SearchCatalog stub |

**Status geral:** READY para 10/12 páginas; 2 parciais por stubs do backend

---

### Change Governance (`src/features/change-governance/`)
**Rotas:** `changesRoutes.tsx`

| Página | Status | Evidência |
|---|---|---|
| ChangeCatalogPage | ✅ REAL | — |
| ChangeDetailPage | ✅ REAL | — |
| ReleasesPage | ✅ REAL | — |
| WorkflowPage | ✅ REAL | — |
| WorkflowConfigurationPage | ✅ REAL | — |
| PromotionPage | ✅ REAL | Testado em `__tests__/pages/PromotionPage.test.tsx` |

**Status geral:** READY — módulo mais completo no frontend

---

### Operations (`src/features/operations/`)
**Rotas:** `operationsRoutes.tsx`

| Página | Status | Evidência |
|---|---|---|
| IncidentsPage | ❌ MOCK | `mockIncidents` hardcoded inline — confirmado |
| RunbooksPage | ❌ MOCK | 3 runbooks hardcoded |
| MitigationPage | ❌ MOCK | Sem conexão à API real |

**Status geral:** BROKEN — 100% mock, sem conexão ao backend real

---

### AI Hub (`src/features/ai-hub/`)
**Rotas:** `aiHubRoutes.tsx`

| Componente/Página | Status | Evidência |
|---|---|---|
| AssistantPanel.tsx | ❌ MOCK | `mockConversations` hardcoded — confirmado |
| AI Governance pages | ✅ REAL | Model registry, policies, budgets conectados |
| AI Agents pages | ⚠️ PARCIAL | Dados parcialmente reais |

**Status geral:** PARTIAL — governance real; assistant 100% mock

---

### Governance (`src/features/governance/`)
**Rotas:** `governanceRoutes.tsx`

| Página | Status | Evidência |
|---|---|---|
| GovernancePacksOverviewPage | 🟡 MOCK | Dados simulados do backend |
| GovernancePackDetailPage | 🟡 MOCK | — |
| DomainsOverviewPage | 🟡 MOCK | — |
| DomainDetailPage | 🟡 MOCK | — |
| TeamsOverviewPage | 🟡 MOCK | — |
| ReportsPage | 🟡 MOCK | — |
| RiskCenterPage | 🟡 MOCK | — |
| RiskHeatmapPage | 🟡 MOCK | — |
| CompliancePage | 🟡 MOCK | — |
| WaiversPage | 🟡 MOCK | — |
| EvidencePackagesPage | 🟡 MOCK | — |
| PolicyCatalogPage | 🟡 MOCK | — |
| ExecutiveOverviewPage | 🟡 MOCK | — |
| ExecutiveDrillDownPage | 🟡 MOCK | — |
| ExecutiveFinOpsPage | 🟡 MOCK | — |
| ServiceFinOpsPage | 🟡 MOCK | — |
| DomainFinOpsPage | 🟡 MOCK | — |
| BenchmarkingPage | 🟡 MOCK | — |
| DelegatedAdminPage | 🟡 MOCK | — |
| GovernanceConfigurationPage | ⚠️ PARCIAL | — |
| EnterpriseControlsPage | 🟡 MOCK | — |

**Status geral:** MOCK — 20+ páginas que consomem dados simulados do backend

---

### Identity Access (`src/features/identity-access/`)

| Componente/Página | Status |
|---|---|
| Users, Roles, Delegations, Environments | ✅ REAL |
| Login, OIDC flow | ✅ REAL |
| MFA, JIT, Break Glass | ✅ REAL |

**Status geral:** READY

---

### Contracts (`src/features/contracts/`)
**Rotas:** `contractsRoutes.tsx`

| Área | Status |
|---|---|
| Contract catalog, list, detail | ✅ REAL |
| Contract Studio workspace | ⚠️ PARCIAL |
| Contract governance | ✅ REAL |
| Publication workflow | ✅ REAL |

**Status geral:** READY para 80%; Contract Studio precisa polish

---

### Configuration (`src/features/configuration/`)

| Área | Status |
|---|---|
| Feature flags, settings | ✅ REAL |
| Tenant configuration | ✅ REAL |

**Status geral:** READY

---

### Audit Compliance (`src/features/audit-compliance/`)

| Área | Status |
|---|---|
| Audit trail, campaigns | ✅ REAL |

**Status geral:** READY

---

### Integrations (`src/features/integrations/`)

| Área | Status |
|---|---|
| Integration list, configuration | ⚠️ PARCIAL |

**Status geral:** PARCIAL — conectores backend são stubs

---

### Notifications (`src/features/notifications/`)

| Área | Status |
|---|---|
| Notification preferences, templates | ✅ REAL |

**Status geral:** READY (cobertura E2E não validada)

---

### Operations → Product Analytics (`src/features/product-analytics/`)

**Status:** MOCK — dados simulados

---

### Dashboard (`src/features/shared/pages/DashboardPage.tsx`)

Dashboard principal. Conecta a múltiplos módulos. Estado: PARCIAL — dados de módulos mock aparecem simulados.

---

## API Integration (`src/api/`)

- `client.ts`: cliente axios configurado com base URL e interceptors de auth
- `index.ts`: ponto de exportação
- Cada feature tem sua própria pasta `api/` com hooks TanStack Query

**Avaliação:** Integração real confirmada para Catalog, ChangeGovernance, IdentityAccess, AuditCompliance. Mock/placeholder para Operations (incidents), AI Hub (assistant), e módulos governance.

---

## i18n — Estado

- Framework: i18next + react-i18next (confirmado)
- 4 locales documentados
- 41 namespaces documentados
- `src/locales/en.json` — ficheiro principal confirmado
- Avaliação: i18n presente e aplicado nas áreas core; áreas mock podem ter cobertura parcial

**Sem textos hardcoded identificados nas áreas core.** Áreas mock (Governance, Operations) devem ser verificadas no momento de substituição.

---

## Auth Flow (`src/auth/`)

- `AuthContext.tsx` testado em `__tests__/contexts/AuthContext.test.tsx`
- `ProtectedRoute.tsx` testado em `__tests__/components/ProtectedRoute.test.tsx`
- `usePermissions.test.tsx` confirma lógica de permissões
- Deep-link preservation: verificar no momento de integração OIDC completa

---

## Componentes Shared (`src/components/`)

50+ componentes customizados confirmados com `wc -l`:
- `PageStateDisplay.tsx` (77 linhas) — estados de página
- `PasswordInput.tsx` (84 linhas)
- `PersonaQuickstart.tsx` (150 linhas) — onboarding por persona
- `QuickActions.tsx` (102 linhas) — ações contextuais
- `StatCard.tsx` (78 linhas) — cards de métricas
- `Tabs.tsx` (106 linhas)
- `TimelinePanel.tsx` (92 linhas)
- `Typography.tsx` (147 linhas)
- `DemoBanner.tsx` — **banner explícito para áreas de demonstração/simulação**
- `CommandPalette.tsx` — busca global
- `SearchInput.tsx`
- `ReleaseScopeGate.tsx` — gate de acesso por release scope

**Nota:** Sem Radix UI — todos os componentes são implementações customizadas. Risco de inconsistência com o alvo de design system definido.

---

## Anti-padrões Identificados

| Anti-padrão | Localização | Impacto |
|---|---|---|
| `mockIncidents` hardcoded inline | `src/features/operations/` | Fluxo 3 inoperante |
| `mockConversations` hardcoded | `src/features/ai-hub/components/AssistantPanel.tsx` | AI Assistant inoperante |
| Governance exibe dados simulados sem warning adequado | `src/features/governance/` | Expectativas falsas |
| React 19 em vez de React 18 (alvo CLAUDE.md) | `package.json` | Desvio de stack |
| react-router-dom em vez de TanStack Router | `package.json` | Desvio de stack |
| Ausência de Radix UI | `package.json` | Componentes customizados sem base validada |
| Ausência de Apache ECharts | `package.json` | Gráficos sem biblioteca definida pelo alvo |

---

## Resumo por Módulo

| Feature | Estado | Mock? |
|---|---|---|
| Catalog (serviços, contratos, source of truth) | ✅ READY | Não |
| Change Governance | ✅ READY | Não |
| Identity Access | ✅ READY | Não |
| Audit Compliance | ✅ READY | Não |
| Configuration | ✅ READY | Não |
| Notifications | ✅ READY | Não |
| Contracts workspace | ⚠️ PARCIAL | Parcialmente |
| AI Hub (governance) | ⚠️ PARCIAL | Sim (assistant) |
| Operations (incidents, runbooks) | ❌ BROKEN | Sim (100%) |
| Governance (reports, FinOps, risk) | 🟡 MOCK | Sim (100%) |
| Integrations | ⚠️ PARCIAL | Parcialmente |
| Product Analytics | 🟡 MOCK | Sim |

**Global:** ~89% de páginas conectadas a backend real; ~11% em mock inline

---

## Recomendações

1. **Crítico:** Conectar `IncidentsPage.tsx` à API real — remover `mockIncidents`
2. **Crítico:** Conectar `AssistantPanel.tsx` à API real de conversações
3. **Alta:** Substituir Governance pages para consumir dados reais quando backend for implementado
4. **Média:** Avaliar alinhamento da stack (TanStack Router, Zustand, Radix UI, ECharts)
5. **Média:** Padronizar loading, error e empty states em todas as páginas
6. **Baixa:** Garantir i18n completo nas áreas que serão substituídas (Governance, Operations)

---

*Data: 28 de Março de 2026*
