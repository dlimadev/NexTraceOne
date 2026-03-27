# Relatório de Estado do Frontend — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Visão Geral

| Métrica | Valor |
|---|---|
| Total de páginas | 82 |
| Páginas conectadas ao backend real | 73 (89%) |
| Páginas com mock inline | 9 (11%) |
| Arquivos .tsx | ~335 |
| Arquivos .ts (não-test) | ~110 |
| Testes unitários frontend | ~264 (passando) |
| Testes E2E (Playwright) | 8 specs + 5 real-env |
| Locales i18n | 4 (en, es, pt-BR, pt-PT) |
| Namespaces i18n | 41 |
| Feature modules | 12 |

---

## 2. Arquitetura Frontend

### Stack
- React 18 + TypeScript + Vite
- TanStack Router (roteamento type-safe)
- TanStack Query (`useQuery`, `useMutation`) para estado de servidor
- Axios via `src/api/client.ts` com JWT auth e tenant headers
- i18n via `i18next` com 4 locales e 41 namespaces
- Tailwind CSS + Radix UI
- Playwright para E2E

### API Client
`src/api/client.ts` — centralizado com:
- JWT Authentication (Bearer token)
- Headers: `X-Tenant-Id`, `X-Environment-Id`, `X-Correlation-Id`
- Refresh token automático
- Axios interceptors

### Autenticação e Guards
- `AuthContext` com estado de autenticação global
- Guardas de rota presentes
- Deep-link preservation após login
- `TenantSelectionPage` para fluxo multi-tenant

---

## 3. Estado por Feature Module

### Catalog (11 páginas) — PARTIAL/READY
**Status: CONNECTED — 89% real**

| Página | Integração | Status |
|---|---|---|
| ServiceCatalogListPage | API real | READY |
| ServiceCatalogPage | API real | READY |
| ServiceDetailPage | API real | READY |
| ContractsPage | API real | READY |
| ContractListPage | API real | READY |
| ContractDetailPage | API real | READY |
| SourceOfTruthExplorerPage | API real | READY |
| ServiceSourceOfTruthPage | API real | READY |
| ContractSourceOfTruthPage | API real | READY |
| DeveloperPortalPage | API parcial (7 stubs backend) | PARTIAL |
| CatalogContractsConfigurationPage | API real | READY |
| GlobalSearchPage | API real (SearchCatalog é stub backend) | PARTIAL |

**Evidência:** `src/frontend/src/features/catalog/pages/`

---

### Change Governance (5 páginas) — READY
**Status: FULLY CONNECTED**

| Página | Integração | Status |
|---|---|---|
| ChangeCatalogPage | API real | READY |
| ChangeDetailPage | API real | READY |
| WorkflowPage | API real | READY |
| WorkflowConfigurationPage | API real | READY |
| ReleasesPage | API real | READY |
| PromotionPage | API real | READY |

**Evidência:** `src/frontend/src/features/change-governance/pages/`

---

### Operations / Incidents (9 páginas) — MOCK
**Status: MOCK INLINE — gap crítico**

- `IncidentsPage.tsx` usa `mockIncidents` hardcoded inline com comentário: *"Dados simulados — em produção, virão da API /api/v1/incidents"*
- `IncidentDetailPage.tsx` — dados estáticos
- Mitigação, runbooks, validação pós-ação: stubs visuais sem API real

**Risco:** O fluxo central de incidents não funciona no frontend. A correlação incident↔change é estática.

**Evidência:** `src/frontend/src/features/operations/`

---

### Governance (25 páginas) — CONNECTED (backend mock)
**Status: CONNECTED to mock backend**

25 páginas conectadas ao backend via API real. O backend retorna `IsSimulated: true` com dados fabricados. O frontend exibe `DemoBanner` quando recebe `IsSimulated: true`.

| Área | Páginas | Status Backend |
|---|---|---|
| Teams & Domains | TeamsOverviewPage, DomainsOverviewPage, TeamDetailPage, DomainDetailPage | MOCK |
| FinOps | FinOpsPage, TeamFinOpsPage, DomainFinOpsPage, ServiceFinOpsPage, ExecutiveFinOpsPage | MOCK |
| Executive | ExecutiveOverviewPage, ExecutiveDrillDownPage | MOCK |
| Compliance | CompliancePage, EnterpriseControlsPage, RiskHeatmapPage | MOCK |
| Governance Packs | GovernancePacksOverviewPage, GovernancePackDetailPage | MOCK |
| Policies | PolicyCatalogPage | MOCK |
| Reports | ReportsPage, MaturityScorecardsPage, BenchmarkingPage | MOCK |
| Waivers | WaiversPage | MOCK |
| Evidence | EvidencePackagesPage | Parcial (backend real) |

**Evidência:** `src/frontend/src/features/governance/pages/`, `docs/IMPLEMENTATION-STATUS.md` §Governance

---

### AI Hub (10 páginas) — PARTIAL
**Status: CONNECTED (parcial)**

| Página | Integração | Status |
|---|---|---|
| AiAssistantPage | `mockConversations` hardcoded | MOCK |
| AssistantPanel | API com fallback mock | PARTIAL |
| AiAnalysisPage | API real | PARTIAL |
| ModelRegistryPage | API parcial (dados mock no backend) | PARTIAL |
| AiPoliciesPage | API real | PARTIAL |
| TokenBudgetPage | API real (dados mock no backend) | PARTIAL |
| AiAuditPage | API real | PARTIAL |
| AiAgentsPage | API real | PARTIAL |
| AgentDetailPage | API real | PARTIAL |
| AiRoutingPage | API real | PARTIAL |
| AiIntegrationsConfigurationPage | API real | PARTIAL |
| IdeIntegrationsPage | API real | PARTIAL |

**Gap crítico:** AiAssistantPage usa `mockConversations` — o fluxo de assistente não funciona.

**Evidência:** `src/frontend/src/features/ai-hub/pages/`, `docs/CORE-FLOW-GAPS.md` §Fluxo 4

---

### Identity Access (9 páginas) — READY
**Status: FULLY CONNECTED**

- LoginPage, UserManagement, RolePermissions, TenantSelection, AccessReview, BreakGlass, JitAccess, DelegatedAdmin, DelegationPage
- Todos conectados ao backend real

**Evidência:** `src/frontend/src/features/identity-access/`

---

### Audit Compliance (1 página) — READY
**Status: FULLY CONNECTED**

- AuditPage conectada ao backend real

**Evidência:** `src/frontend/src/features/audit-compliance/`

---

### Integrations (4 páginas) — PARTIAL
**Status: CONNECTED (backend stubs)**

- ConnectorDetailPage, IngestionExecutionsPage e páginas relacionadas
- Conectados ao backend; backend retorna stubs/metadata-only

**Evidência:** `src/frontend/src/features/integrations/`

---

### Product Analytics (5 páginas) — MOCK
**Status: CONNECTED (backend 100% mock)**

- ValueTrackingPage e demais conectados ao backend que retorna dados simulados

**Evidência:** `src/frontend/src/features/product-analytics/`

---

### Dashboard (shared) — PARTIAL
**Status: PARTIAL**

- `DashboardPage.tsx` em `src/frontend/src/features/shared/pages/`
- Dashboard sem semântica clara de persona — parece genérico
- Não reflete ownership, ambiente, serviços do utilizador autenticado de forma clara

---

## 4. Avaliação de i18n

**Status: COMPLETO nas áreas core**

| Verificação | Estado |
|---|---|
| Títulos de página | Sim — via `t()` |
| Labels e placeholders | Sim — via `t()` |
| Botões e tooltips | Sim — via `t()` |
| Empty states | Parcial — padrão não uniforme |
| Loading states | Parcial — só ServiceCatalogPage tem loading real padronizado |
| Error states | Parcial — 96% das páginas sem error boundary por secção |
| Mensagens de backend | Sim — contrato `code`/`messageKey`/`correlationId` presente |

**4 locales ativos:** `en`, `es`, `pt-BR`, `pt-PT`
**41 namespaces** cobrindo todos os módulos core

**Evidência:** `src/frontend/src/i18n.ts`, `docs/REBASELINE.md` §i18n

---

## 5. Problemas de UX a Endereçar

| Problema | Localização | Impacto |
|---|---|---|
| Dashboard genérico sem semântica de persona | DashboardPage.tsx | UX não reflete papel do utilizador |
| IncidentsPage 100% mock | operations/ | Fluxo central indisponível |
| AiAssistantPage 100% mock conversations | ai-hub/AiAssistantPage.tsx | Fluxo AI quebrado |
| 83% das páginas sem EmptyState padronizado | Geral | UX inconsistente |
| 96% das páginas sem error states por secção | Geral | Erros silenciosos |
| Loading states não padronizados | Geral (ServiceCatalogPage é exceção) | UX inconsistente |
| Governance/FinOps 25 páginas com DemoBanner | governance/ | Experiência de demo explícita |

---

## 6. Integração Real vs. Mock — Resumo

| Feature Module | API Real? | Backend Real? | Estado Final |
|---|---|---|---|
| Catalog | Sim | Sim (91.7%) | READY com gaps pontuais |
| Change Governance | Sim | Sim (100%) | READY |
| Identity Access | Sim | Sim (100%) | READY |
| Audit Compliance | Sim | Sim (100%) | READY |
| Operations/Incidents | Não (mock inline) | Parcial (seed estático) | MOCK/BROKEN |
| Governance | Sim | Não (IsSimulated) | CONNECTED/MOCK |
| AI Hub | Parcial (Assistant mock) | Parcial (ExternalAI stubs) | PARTIAL |
| Integrations | Sim | Parcial (stubs) | PARTIAL |
| Product Analytics | Sim | Não (mock) | CONNECTED/MOCK |
| Configuration | Sim | Sim | READY |
| Notifications | Sim | Parcial | PARTIAL |

---

## 7. Avaliação de Maturidade por Persona

| Persona | Áreas Funcionais no Frontend | Lacunas |
|---|---|---|
| Engineer | Catalog, Change, Contract Studio, AI Hub (parcial) | AI Assistant não funciona |
| Tech Lead | Change Intelligence, Blast Radius, Workflow | Incidents mock |
| Platform Admin | Identity, Config, Audit, Environments | Funcional |
| Auditor | Audit Trail, Security Events | Funcional |
| Architect | Service Topology, Source of Truth | Developer Portal parcial |
| Product | Governance, Maturity, Benchmarking | Todos mock |
| Executive | Executive Overview, FinOps, Reports | Todos mock |

---

## 8. Testes Frontend

| Tipo | Quantidade | Estado |
|---|---|---|
| Testes unitários (Vitest) | ~264 | Passando |
| Testes E2E Playwright | 8 specs | Cobrem catalog, changes, incidents (mock), AI |
| Testes real-env | 5 (e2e-real/) | Ambiente real; configuração separada |
| Testes de componentes | ~15 | Presentes |

**Gap:** E2E de incidents e AI não validam fluxo real (usam fixtures estáticas).

**Evidência:** `src/frontend/e2e/`, `src/frontend/src/__tests__/`
