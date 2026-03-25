# Relatório de Estado do Frontend — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o frontend React com foco em: integração real com backend, i18n, personas, autenticação, autorização, estados de UI, mocks, placeholders e desvios do target técnico.

---

## 2. Stack Técnica — Estado Real vs Target

| Target | Real | Estado |
|--------|------|--------|
| React 18 | React 19.2.0 | DESVIO — upgrade, não downgrade |
| TanStack Router | React Router DOM 7.13.1 | DESVIO — funcionalmente adequado |
| TanStack Query | TanStack Query 5.90.21 | CONFORME |
| Radix UI | Componentes customizados | DESVIO — acessibilidade pode ser limitada |
| Zustand | React Context + TanStack Query | DESVIO — padrão alternativo adequado |
| Apache ECharts | Sem biblioteca de gráficos | DESVIO — dashboards sem gráficos ricos |
| Tailwind CSS | Tailwind CSS 4.2.1 | CONFORME |
| TypeScript | TypeScript 5.9.3 | CONFORME |
| Vite | Vite 7.3.1 | CONFORME |
| Playwright | Playwright 1.58.2 | CONFORME |

**Evidência:** `src/frontend/package.json`

---

## 3. Arquitectura e Routing

**Ficheiro:** `src/frontend/src/App.tsx`

- React Router DOM v7 com `BrowserRouter`
- Lazy code splitting em todos os módulos
- `ProtectedRoute` wrapper com verificação de permissão
- `AppShell` como layout wrapper de rotas autenticadas
- 34 route prefixes em `finalProductionIncludedRoutePrefixes`
- 0 routes excluídas em `finalProductionExcludedRoutePrefixes`

**Estado:** READY

---

## 4. Autenticação e Sessão

**Ficheiros:** `src/frontend/src/contexts/AuthContext.tsx`, `src/frontend/src/api/client.ts`

### Implementação verificada:
- Bearer token em `sessionStorage` (não `localStorage`)
- Refresh tokens em memória (não persistido)
- CSRF token em `sessionStorage`
- Interceptor Axios com injeção automática de token
- Fluxo de refresh automático antes de expiração
- Header `X-Tenant-Id` e `X-Environment-Id` em todos os pedidos
- `withCredentials: true` para cookies
- Evento `auth:session-expired` para logout em cascata
- Redirect para login com deep-link preservation

**Estado:** READY — secure token handling sem antipadrões

---

## 5. Sistema de Permissões e Personas

**Ficheiros:** `src/frontend/src/auth/permissions.ts`, `src/frontend/src/auth/persona.ts`

### Permissões (118+ granulares):
- Identity & Access: users.read, users.write, roles.*, sessions.*
- Catalog: assets.read, assets.write, serviceregistration.*
- Contracts: contracts.read, contracts.write, contracts.import
- Changes: changes.*, workflows.*, promotions.*
- Operations: incidents.*, runbooks.*, reliability.*, automation.*
- AI Hub: ai.assistant.*, ai.models.*, ai.policies.*, ai.ide.*, ai.governance.*
- Governance: reports.*, risk.*, compliance.*, finops.*, teams.*, domains.*, waivers.*
- Audit: audit.read, integrations.*

### Personas (7 oficiais):
- `Engineer` — foco em serviços, contratos, mudanças
- `TechLead` — foco em governança de equipa, fiabilidade
- `Architect` — foco em dependências, topologia, IA
- `Product` — foco em analytics, roadmap, alinhamento
- `Executive` — foco em overview, FinOps, compliance
- `PlatformAdmin` — foco em configuração, identidade, infra
- `Auditor` — foco em audit trail, compliance, evidências

**Cada persona tem:**
- `sectionOrder` — ordenação das secções do sidebar
- `homeWidgets` — widgets específicos no dashboard
- `quickActions` — acções rápidas contextuais
- `aiScope` — escopo de contexto IA
- `aiSuggestedPrompts` — prompts sugeridos por persona

**Estado:** READY

---

## 6. Feature Modules — Auditoria Detalhada

### 6.1 Contracts — READY

**Path:** `src/frontend/src/features/contracts/`

**Integração real verificada:**
- `contractsApi.listContracts()` — GET `/contracts`
- `contractsApi.importContract()`, `createVersion()`, `computeDiff()`, `getClassification()`
- `contractsApi.suggestVersion()`, `getHistory()`, `getDetail()`
- Studio com workflow Draft→InReview→Approved→Locked→Deprecated
- Spectral ruleset management real
- Canonical Entity Catalog real

**Decisão de produto:** Contract Studio real com versioning, diff e governance

---

### 6.2 Change Governance — READY

**Path:** `src/frontend/src/features/change-governance/`

**Integração real verificada:**
- `changeIntelligenceApi.listReleases(apiAssetId, page, pageSize)`
- `changeIntelligenceApi.getIntelligenceSummary(releaseId)` — retorna `ScoreDto`, `BlastRadiusDto`, `MarkerDto[]`, `TimelineEventDto[]`
- `changeIntelligenceApi.checkFreezeConflict(date, environment)`
- Pages: Releases, Change Catalog, Change Detail, Workflow, Promotion

**Persona principal:** Engineer, Tech Lead, Architect
**Decisão:** Avaliação de risco e confiança em mudanças de produção

---

### 6.3 AI Hub — PARTIAL

**Path:** `src/frontend/src/features/ai-hub/`

**Integração real verificada (12 pages):**
- Chat: `aiGovernanceApi.sendMessage()`, `createConversation()`, `listConversations()`
- Models: `listModels()`, `registerModel()`, `updateModel()`
- Policies: `listPolicies()`, `createPolicy()`, `updatePolicy()`
- Budgets: `listBudgets()`, `updateBudget()`
- Audit: `listAuditEntries()`
- IDE: `getIdeSummary()`, `listIdeClients()`, `registerIdeClient()`
- Agents: `listAgents()`, `executeAgent()`

**Problema crítico encontrado:**
`AssistantPanel.tsx` — componente reutilizado em 4 páginas de detalhe (Service, Contract, Change, Incident) contém função `buildGroundedContent()` que gera respostas mock sem chamar o endpoint real de chat.

**Estado:** PARTIAL — chat standalone real; AssistantPanel em contextos de detalhe usa mock

---

### 6.4 Service Catalog — READY

**Path:** `src/frontend/src/features/catalog/`

**Integração real verificada:**
- `serviceCatalogApi.getGraph()` — retorna `AssetGraph` com `services[]`, `apis[]`, `dependencies`
- `serviceCatalogApi.getImpactPropagation(nodeId, depth)` — blast radius
- `serviceCatalogApi.listSnapshots(limit)` — temporal analysis
- `serviceCatalogApi.getTemporalDiff(fromSnapshotId, toSnapshotId)` — change tracking
- `serviceCatalogApi.getNodeHealth(nodeType)` — health metrics

**Persona principal:** Architect, Engineer, Tech Lead

---

### 6.5 Operations — READY

**Path:** `src/frontend/src/features/operations/`

**Integração real verificada:**
- `incidentsApi.listIncidents(filters)` com paginação
- `incidentsApi.getIncidentSummary()` — totalOpen, totalByStatus
- `incidentsApi.getDetail(incidentId)` com `correlatedChanges`
- Runbooks management real
- Team Reliability real
- Automation Workflows real
- Environment Comparison real

**Persona principal:** Engineer, Platform Admin

---

### 6.6 Governance — READY

**Path:** `src/frontend/src/features/governance/`

**Integração real verificada:**
- `organizationGovernanceApi.getExecutiveOverview()` — `ExecutiveOverviewResponse` com KPIs
- `listTeams()`, `getTeamDetail()`, `createTeam()`, `updateTeam()`
- `listDomains()`, `getDomainDetail()`, `createDomain()`
- `listGovernancePacks()`, `getGovernancePackDetail()`
- `listWaivers()`, `createWaiver()`, `approveWaiver()`, `rejectWaiver()`
- FinOps pages com API calls reais

**Persona principal:** Executive, Product, Auditor

---

### 6.7 Identity Access — READY

**Path:** `src/frontend/src/features/identity-access/`

**Páginas verificadas:** Login, Tenant Selection, Users, Environments, Break Glass, JIT Access, Delegations, Access Reviews, My Sessions

**Estado:** READY — fluxo auth completo com MFA, multi-tenant, break glass, JIT

---

### 6.8 Integrations — READY

**Path:** `src/frontend/src/features/integrations/`

**Páginas:** Integration Hub, Connector Detail, Ingestion Executions, Ingestion Freshness

**Estado:** READY — connector management funcional

---

### 6.9 Product Analytics — PARTIAL

**Path:** `src/frontend/src/features/product-analytics/`

**Páginas:** Overview, Module Adoption, Persona Usage, Journey Funnel, Value Tracking

**Estado:** PARTIAL — UI existe; pipeline analítico backend não verificado

---

### 6.10 Audit Compliance — READY

**Path:** `src/frontend/src/features/audit-compliance/`

**Estado:** READY — audit trail viewing com filtros e export

---

### 6.11 Notifications — READY

**Path:** `src/frontend/src/features/notifications/`

**Estado:** READY — notification center, preferences, admin config

---

### 6.12 Configuration — READY

**Path:** `src/frontend/src/features/configuration/`

**Estado:** READY — config admin e console avançado

---

### 6.13 Operational Intelligence — PARTIAL

**Path:** `src/frontend/src/features/operational-intelligence/`

**Estado:** PARTIAL — configuração de FinOps e anomaly detection; pipeline backend não verificado

---

### 6.14 Shared/Pages — READY

**Path:** `src/frontend/src/features/shared/`

**Estado:** READY — páginas 404, 403, loading, error

---

## 7. Componentes UI — Auditoria

### 7.1 Shell

**Ficheiro:** `src/frontend/src/components/shell/AppSidebar.tsx`

- 12 secções de navegação
- Persona-aware via `PersonaContext.sectionOrder`
- Permission-gated via `can()` hook
- Collapsible, responsivo, sticky mobile
- `EnvironmentBanner` mostra ambiente activo
- `ContextStrip` mostra serviço/contrato/mudança activo

**Estado:** READY

### 7.2 Estados de UI

Verificados e implementados:
- `EmptyState.tsx` — estado vazio com i18n
- `ErrorState.tsx`, `PageErrorState.tsx` — estados de erro
- `PageLoadingState.tsx`, `Skeleton.tsx` — estados de carregamento
- `InlineMessage.tsx` — mensagens inline

**Estado:** READY

### 7.3 DemoBanner e ReleaseScopeGate

- `DemoBanner.tsx` — componente existe mas não foi encontrado como problema generalizado
- `ReleaseScopeGate.tsx` — gate por escopo de release; 0 rotas excluídas actualmente

---

## 8. Internacionalização (i18n)

**Ficheiro:** `src/frontend/src/i18n.ts`, `src/frontend/src/locales/`

| Idioma | Chaves | Estado |
|--------|--------|--------|
| Inglês (en) | 4.814 | COMPLETO |
| Português Brasil (pt-BR) | 3.096 | PARCIAL (~64%) |
| Português Portugal (pt-PT) | 4.033 | PARCIAL (~84%) |
| Espanhol (es) | 3.812 | PARCIAL (~79%) |

**Framework:** i18next 25.8.18 + react-i18next 16.5.8
**Segurança:** XSS protection habilitado (`escapeValue: true`)
**Fallback:** Inglês para chaves em falta

**Lacuna:** pt-BR tem apenas 64% das chaves em inglês — risco de fallback visível

---

## 9. Gráficos e Visualizações

**Estado: AUSENTE**

O produto não usa nenhuma biblioteca de gráficos. Dashboards executive, FinOps, e analytics usam:
- `StatCard` com valores numéricos
- `Badge` com estados
- HTML/CSS para barras simples

**Impacto:** Dashboards de executive e FinOps não têm gráficos de tendência, distribuição ou séries temporais.

**Recomendação:** Integrar Apache ECharts (conforme target) para dashboards de executive, FinOps, reliability e change intelligence.

---

## 10. Segurança do Frontend

| Aspecto | Estado | Evidência |
|---------|--------|-----------|
| Tokens em sessionStorage | CUMPRIDO | `src/frontend/src/api/client.ts` |
| Sem localStorage para segredos | CUMPRIDO | Verificado |
| CSRF token | CUMPRIDO | Double-submit cookie |
| Bearer token automático | CUMPRIDO | Axios interceptor |
| Tenant/Environment headers | CUMPRIDO | X-Tenant-Id, X-Environment-Id |
| Backend como autoridade | CUMPRIDO | ProtectedRoute espera perfil do servidor |
| dangerouslySetInnerHTML | NÃO ENCONTRADO | Verificado |
| Exposição de segredos | NÃO ENCONTRADO | Sem chaves em código frontend |

---

## 11. Resumo por Módulo

| Módulo | Persona Principal | Integração Real | Estado | Gaps |
|--------|------------------|----------------|--------|------|
| Contracts | Architect, Engineer | SIM | READY | Sem gráficos |
| Change Governance | Engineer, TechLead | SIM | READY | Sem calendario visual |
| AI Hub | Todos | PARCIAL | PARTIAL | AssistantPanel mock |
| Service Catalog | Architect, Engineer | SIM | READY | Sem gráficos topology |
| Operations | Engineer, PlatformAdmin | SIM | READY | SLO UI limitado |
| Governance | Executive, Auditor | SIM | READY | Sem gráficos FinOps |
| Identity Access | PlatformAdmin | SIM | READY | — |
| Audit Compliance | Auditor | SIM | READY | — |
| Notifications | Todos | SIM | READY | — |
| Configuration | PlatformAdmin | SIM | READY | — |
| Product Analytics | Product | PARCIAL | PARTIAL | Pipeline backend |
| Operational Intelligence | Engineer, PlatformAdmin | PARCIAL | PARTIAL | Analytics backend |

---

## 12. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P0 | Remover mock response generator de AssistantPanel.tsx |
| P1 | Integrar Apache ECharts para dashboards com gráficos |
| P1 | Completar tradução pt-BR (3.096 → 4.814 chaves) |
| P2 | Adicionar visualização de calendário de releases (FreezeWindow) |
| P2 | Completar analytics de Product Analytics com dados reais |
| P3 | Migrar de React Router DOM para TanStack Router |
| P3 | Avaliar adopção de Radix UI para acessibilidade |
