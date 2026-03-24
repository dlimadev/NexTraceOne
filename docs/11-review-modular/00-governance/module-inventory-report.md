# NexTraceOne — Inventário de Módulos Backend e Frontend

**Data:** 2026-03-24
**Fonte:** Código-fonte real do repositório
**Nota:** Este inventário baseia-se exclusivamente no código existente, não na documentação.

---

## PARTE 1 — Backend

### Visão Geral

O backend é uma aplicação C#/.NET organizada em solução única (`NexTraceOne.sln`) com 9 módulos de domínio, 5 building blocks transversais e 3 projetos de plataforma.

```
src/
├── building-blocks/          # 5 projetos de infraestrutura transversal
├── modules/                  # 9 módulos de domínio
│   ├── aiknowledge/
│   ├── auditcompliance/
│   ├── catalog/
│   ├── changegovernance/
│   ├── configuration/
│   ├── governance/
│   ├── identityaccess/
│   ├── notifications/
│   └── operationalintelligence/
└── platform/                 # 3 projetos de host/plataforma
    ├── NexTraceOne.ApiHost/
    ├── NexTraceOne.BackgroundWorkers/
    └── NexTraceOne.Ingestion.Api/
```

---

### Building Blocks (5 projetos)

| Projeto | Propósito |
|---------|-----------|
| `NexTraceOne.BuildingBlocks.Application` | Interfaces e abstrações de aplicação (CQRS, mediator) |
| `NexTraceOne.BuildingBlocks.Core` | Entidades base, value objects, domain events |
| `NexTraceOne.BuildingBlocks.Infrastructure` | DbContextBase, OutboxPublisher, health checks |
| `NexTraceOne.BuildingBlocks.Observability` | OpenTelemetry, métricas, rastreamento distribuído |
| `NexTraceOne.BuildingBlocks.Security` | Autenticação, autorização, JWT, rate limiting |

---

### Módulo 1: AIKnowledge

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/aiknowledge/` |
| **Propósito** | Governança de IA, orquestração de modelos, integração com provedores externos, execução de agentes |
| **Estado** | Ativo |
| **DbContexts** | `ExternalAiDbContext`, `AiGovernanceDbContext`, `AiOrchestrationDbContext` |
| **Migrations** | 7 (mais recente: `20260323201957_SeparateSharedEntityOwnership`) |
| **Subdomínios** | ExternalAI, Governance, Orchestration, Runtime |

**Endpoints expostos:**
- `ExternalAiEndpointModule` — integração com provedores externos (OpenAI, Anthropic, etc.)
- `AiGovernanceEndpointModule` — políticas, orçamentos, regras de uso
- `AiIdeEndpointModule` — integração IDE
- `AiOrchestrationEndpointModule` — orquestração de agentes
- `AiRuntimeEndpointModule` — execução e análise em runtime

**Entidades principais (Domain):**
- Provider, Model, Agent (core entities)
- Policies, Budgets, RoutingRules

**Documentação associada:**
- `docs/AI-ARCHITECTURE.md`, `docs/AI-GOVERNANCE.md`, `docs/aiknowledge/`, `docs/architecture/phase-7/phase-7-ai-capability-architecture.md`
- **Estado documental:** Espalhado em múltiplos ficheiros, sem documento único consolidado

---

### Módulo 2: AuditCompliance

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/auditcompliance/` |
| **Propósito** | Registo de auditoria e conformidade regulatória |
| **Estado** | Parcial/Mínimo |
| **DbContexts** | `AuditDbContext` |
| **Migrations** | 2 (Phase 3 Compliance Domain) |
| **Subdomínios** | Audit |

**Endpoints expostos:**
- `AuditEndpointModule` — 1 módulo de endpoint

**Documentação associada:**
- Sem documentação dedicada de módulo
- Referenciado em `docs/assessment/08-SECURITY-AUDIT.md`

**Observação:** Módulo mínimo — backend com 1 endpoint e frontend com 1 página (`/audit`). Escopo real não é claro.

---

### Módulo 3: Catalog

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/catalog/` |
| **Propósito** | Catálogo de serviços, contratos de API, grafo de dependências, portal de desenvolvedor, source of truth |
| **Estado** | Ativo e rico |
| **DbContexts** | `ContractsDbContext`, `CatalogGraphDbContext`, `DeveloperPortalDbContext` |
| **Migrations** | 3 (1 por DbContext) |
| **Subdomínios** | Contracts, Graph, Portal, SourceOfTruth |

**Endpoints expostos:**
- `ContractStudioEndpointModule` — edição visual de contratos
- `ContractsEndpointModule` — CRUD de versões de contratos
- `ServiceCatalogEndpointModule` — catálogo de serviços e grafo
- `DeveloperPortalEndpointModule` — portal de desenvolvedores
- `SourceOfTruthEndpointModule` — source of truth explorador

**Entidades principais:**
- *Contracts:* `ContractVersion`, `ContractDraft`, `ContractReview`, `ContractEvidencePack`, `SpectralRuleset`
- *Graph:* `ServiceAsset`, `ApiAsset`, `ConsumerAsset`, `SavedGraphView`, `GraphSnapshot`
- *Portal:* `Subscription`, `CodeGeneration`, `PlaygroundSession`, `PortalAnalytics`

**Serviços de domínio:**
- `OpenApiDiffCalculator`, `ContractRuleEngine`, `ContractScorecardCalculator`, `CanonicalModelBuilder`

**Application Features:** 30+ features em Contracts, 20+ em Graph, 17+ em Portal, 5 em SourceOfTruth

**Documentação associada:**
- `docs/SERVICE-CONTRACT-GOVERNANCE.md`, `docs/CONTRACT-STUDIO-VISION.md`, `docs/SOURCE-OF-TRUTH-STRATEGY.md`

---

### Módulo 4: ChangeGovernance

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/changegovernance/` |
| **Propósito** | Inteligência de mudanças, promoção de versões, workflows, rulesets de governança |
| **Estado** | Ativo |
| **DbContexts** | `ChangeIntelligenceDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext`, `WorkflowDbContext` |
| **Migrations** | 4 (1 por DbContext, última: `20260321160240_InitialCreate`) |
| **Subdomínios** | ChangeIntelligence, Promotion, RulesetGovernance, Workflow |

**Endpoints expostos:**
- `ChangeIntelligenceEndpointModule` com sub-endpoints:
  - `AnalysisEndpoints` — análise de mudanças
  - `ChangeConfidenceEndpoints` — confiança de mudança
  - `DeploymentEndpoints` — deployments
  - `FreezeEndpoints` — janelas de freeze
  - `IntelligenceEndpoints` — inteligência geral
  - `ReleaseQueryEndpoints` — consulta de releases
- `PromotionEndpointModule` — promoção entre ambientes
- `RulesetGovernanceEndpointModule` — gestão de rulesets

**Documentação associada:**
- `docs/CHANGE-CONFIDENCE.md`, `docs/execution/CONFIGURATION-CHANGE-TYPES-*`

---

### Módulo 5: Configuration

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/configuration/` |
| **Propósito** | Configuração global da plataforma por instância/tenant |
| **Estado** | Ativo |
| **DbContexts** | `ConfigurationDbContext` |
| **Migrations** | A confirmar |
| **Subdomínios** | Configuration |

**Endpoints expostos:**
- `ConfigurationEndpointModule` — leitura e escrita de configurações

**Documentação associada:**
- Séries `docs/execution/CONFIGURATION-*` (30+ ficheiros)
- Sem documento arquitetural dedicado ao módulo

---

### Módulo 6: Governance

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/governance/` |
| **Propósito** | Governança corporativa, compliance, risco, FinOps, políticas, equipes, domínios |
| **Estado** | Ativo e mais rico do backend |
| **DbContexts** | `GovernanceDbContext` |
| **Migrations** | 3 (Phase 5 Enrichment, LastProcessedAt, StandardizeTenantId) |
| **Subdomínios** | 17 áreas funcionais |

**Endpoints expostos (17 módulos):**
- `ComplianceChecksEndpointModule`
- `DelegatedAdminEndpointModule`
- `DomainEndpointModule`
- `EnterpriseControlsEndpointModule`
- `EvidencePackagesEndpointModule`
- `ExecutiveOverviewEndpointModule`
- `GovernanceComplianceEndpointModule`
- `GovernanceFinOpsEndpointModule`
- `GovernancePacksEndpointModule`
- `GovernanceReportsEndpointModule`
- `GovernanceRiskEndpointModule`
- `GovernanceWaiversEndpointModule`
- `IntegrationHubEndpointModule`
- `OnboardingEndpointModule`
- `PlatformStatusEndpointModule`
- `PolicyCatalogEndpointModule`
- `ProductAnalyticsEndpointModule`
- `ScopedContextEndpointModule`
- `TeamEndpointModule`

**Documentação associada:**
- `docs/governance/PHASE-5-GOVERNANCE-ENRICHMENT.md`
- Sem documento arquitetural consolidado

---

### Módulo 7: IdentityAccess

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/identityaccess/` |
| **Propósito** | Identidade, autenticação, autorização, tenancy, sessões, delegação, JIT |
| **Estado** | Ativo e completo |
| **DbContexts** | `IdentityDbContext` |
| **Migrations** | 2 |
| **Subdomínios** | Auth, Users, Tenants, Sessions, Delegation, JIT, AccessReview |

**Endpoints expostos (11 ficheiros):**
- `AccessReviewEndpoints`
- `AuthEndpoints` — login, logout, MFA
- `BreakGlassEndpoints` — acesso de emergência
- `CookieSessionEndpoints`
- `DelegationEndpoints`
- `EnvironmentEndpoints`
- `IdentityEndpointModule`
- `JitAccessEndpoints` — Just-In-Time access
- `RolePermissionEndpoints`
- `RuntimeContextEndpoints`
- `TenantEndpoints`
- `UserEndpoints`

**Documentação associada:**
- `docs/SECURITY-ARCHITECTURE.md`, `docs/security/BACKEND-ENDPOINT-AUTH-AUDIT.md`

---

### Módulo 8: Notifications

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/notifications/` |
| **Propósito** | Centro de notificações, entrega de alertas, preferências, canais |
| **Estado** | Ativo |
| **DbContexts** | `NotificationsDbContext` |
| **Migrations** | A confirmar |
| **Subdomínios** | NotificationCenter |

**Endpoints expostos:**
- `NotificationCenterEndpointModule`

**Documentação associada:**
- Série `docs/execution/NOTIFICATIONS-*` (25+ ficheiros)
- Sem documento arquitetural de módulo dedicado

---

### Módulo 9: OperationalIntelligence

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/modules/operationalintelligence/` |
| **Propósito** | Incidentes, confiabilidade, automação de workflows, inteligência de runtime, custo/FinOps, runbooks |
| **Estado** | Ativo — módulo mais complexo por número de DbContexts |
| **DbContexts** | `AutomationDbContext`, `CostIntelligenceDbContext`, `IncidentDbContext`, `ReliabilityDbContext`, `RuntimeIntelligenceDbContext` |
| **Migrations** | 10+ (2 por DbContext mínimo, CostIntelligence tem adicional `AddCostImportPipeline`) |
| **Subdomínios** | Automation, CostIntelligence, Incidents, Mitigation, Reliability, Runtime |

**Endpoints expostos (7 módulos):**
- `AutomationEndpointModule`
- `CostIntelligenceEndpointModule`
- `IncidentEndpointModule`
- `MitigationEndpointModule`
- `RunbookEndpointModule`
- `ReliabilityEndpointModule`
- `RuntimeIntelligenceEndpointModule`

**Documentação associada:**
- `docs/reliability/` (4 ficheiros), `docs/runbooks/` (11 ficheiros)
- **Problema:** Frontend tem apenas 1 página para este módulo (`OperationsFinOpsConfigurationPage`). O módulo Operations no frontend (`src/features/operations/`) com 8 páginas é distinto e consome este backend.

---

### Plataforma (3 projetos)

| Projeto | Propósito |
|---------|-----------|
| `NexTraceOne.ApiHost` | Host principal da API — agrega todos os módulos e expõe os endpoints |
| `NexTraceOne.BackgroundWorkers` | Workers de background — outbox, jobs recorrentes |
| `NexTraceOne.Ingestion.Api` | API de ingestão de dados de observabilidade (OTel, traces, etc.) |

---

### Resumo Backend

| Módulo | DbContexts | Endpoints Modules | Migrations | Estado |
|--------|------------|-------------------|------------|--------|
| AIKnowledge | 3 | 5 | 7 | Ativo |
| AuditCompliance | 1 | 1 | 2 | Parcial |
| Catalog | 3 | 5 | 3 | Ativo |
| ChangeGovernance | 4 | 9 | 4 | Ativo |
| Configuration | 1 | 1 | ? | Ativo |
| Governance | 1 | 17 | 3 | Ativo |
| IdentityAccess | 1 | 11 | 2 | Ativo |
| Notifications | 1 | 1 | ? | Ativo |
| OperationalIntelligence | 5 | 7 | 10+ | Ativo |
| **Total** | **20** | **57** | **~50** | — |

---

## PARTE 2 — Frontend

### Visão Geral

O frontend é uma aplicação React 19 com React Router 7, organizada por features/módulos em `src/features/`. Cada módulo contém `pages/`, `components/`, `api/` e hooks específicos.

```
src/frontend/src/
├── features/              # 15 módulos de feature
├── components/shell/      # Shell de navegação (sidebar, topbar, etc.)
├── components/            # Componentes partilhados
├── contexts/              # AuthContext, EnvironmentContext, PersonaContext
├── hooks/                 # Hooks partilhados
├── locales/               # en.json, es.json, pt-BR.json, pt-PT.json
├── shared/                # design-system, ui, lib, tokens, api
├── auth/                  # permissions, persona
├── utils/                 # navigation.ts (safe redirect)
├── App.tsx                # Router e lazy imports de todas as páginas
└── main.tsx               # Entry point
```

---

### Módulo FE 1: ai-hub

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/ai-hub/` |
| **Propósito** | Interface de gestão do AI Hub — assistente, agentes, modelos, políticas, routing, IDE, budgets, audit |
| **Páginas** | 10 |
| **Rotas registadas** | 9 + 1 detail |
| **Backend correspondente** | AIKnowledge (ExternalAI, Governance, Orchestration, Runtime) |
| **Estado** | Parcial — alguns handlers podem ser SIM |

**Páginas:**
- `AiAssistantPage` → `/ai/assistant`
- `AiAgentsPage` → `/ai/agents`
- `AgentDetailPage` → `/ai/agents/:agentId`
- `ModelRegistryPage` → `/ai/models`
- `AiPoliciesPage` → `/ai/policies`
- `AiRoutingPage` → `/ai/routing`
- `IdeIntegrationsPage` → `/ai/ide`
- `TokenBudgetPage` → `/ai/budgets`
- `AiAuditPage` → `/ai/audit`
- `AiAnalysisPage` → `/ai/analysis`
- `AiIntegrationsConfigurationPage` → `/platform/configuration/ai-integrations` *(página de config admin)*

---

### Módulo FE 2: audit-compliance

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/audit-compliance/` |
| **Propósito** | Página de auditoria e conformidade |
| **Páginas** | 1 |
| **Rotas registadas** | 1 |
| **Backend correspondente** | AuditCompliance |
| **Estado** | Mínimo |

**Páginas:**
- `AuditPage` → `/audit`

---

### Módulo FE 3: catalog

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/catalog/` |
| **Propósito** | Catálogo de serviços, source of truth, developer portal, busca global |
| **Páginas** | 9+ |
| **Backend correspondente** | Catalog (Graph, Portal, SourceOfTruth) |
| **Estado** | Ativo |

**Páginas:**
- `ServiceCatalogListPage` → `/services`
- `ServiceCatalogPage` → `/services/graph`
- `ServiceDetailPage` → `/services/:serviceId`
- `SourceOfTruthExplorerPage` → `/source-of-truth`
- `ServiceSourceOfTruthPage` → `/source-of-truth/services/:serviceId`
- `ContractSourceOfTruthPage` → `/source-of-truth/contracts/:contractVersionId`
- `GlobalSearchPage` → `/search`
- `DeveloperPortalPage` → `/portal/*`
- `CatalogContractsConfigurationPage` → `/platform/configuration/catalog-contracts`

---

### Módulo FE 4: change-governance

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/change-governance/` |
| **Propósito** | Mudanças, releases, workflows, promoção |
| **Páginas** | 6 |
| **Backend correspondente** | ChangeGovernance |
| **Estado** | Ativo |

**Páginas:**
- `ChangeCatalogPage` → `/changes`
- `ChangeDetailPage` → `/changes/:changeId`
- `ReleasesPage` → `/releases`
- `WorkflowPage` → `/workflow`
- `PromotionPage` → `/promotion`
- `WorkflowConfigurationPage` → `/platform/configuration/workflows`

---

### Módulo FE 5: configuration

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/configuration/` |
| **Propósito** | Consola de administração e configuração avançada |
| **Páginas** | 2 |
| **Backend correspondente** | Configuration |
| **Estado** | Ativo |

**Páginas:**
- `ConfigurationAdminPage` → `/platform/configuration`
- `AdvancedConfigurationConsolePage` → `/platform/configuration/advanced`

---

### Módulo FE 6: contracts

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/contracts/` |
| **Propósito** | Contratos de API — catálogo, studio, criação, workspace, governance, spectral, canonical, portal |
| **Páginas** | 8 (mas 4 sem rota em App.tsx) |
| **Backend correspondente** | Catalog (Contracts, ContractStudio) |
| **Estado** | Parcialmente funcional — 4 páginas órfãs |

**Páginas COM rota:**
- `ContractCatalogPage` → `/contracts`
- `CreateServicePage` → `/contracts/new`
- `DraftStudioPage` → `/contracts/studio/:draftId`
- `ContractWorkspacePage` → `/contracts/:contractVersionId`

**Páginas SEM rota (órfãs):**
- `ContractGovernancePage` → esperado `/contracts/governance` — **SEM ROTA**
- `SpectralRulesetManagerPage` → esperado `/contracts/spectral` — **SEM ROTA**
- `CanonicalEntityCatalogPage` → esperado `/contracts/canonical` — **SEM ROTA**
- `ContractPortalPage` → esperado `/contracts/portal` — **SEM ROTA**

---

### Módulo FE 7: governance

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/governance/` |
| **Propósito** | Governança corporativa — executive, reports, compliance, risk, finops, policies, packs, teams, domains, waivers, etc. |
| **Páginas** | 20 |
| **Backend correspondente** | Governance (17 endpoint modules) |
| **Estado** | Ativo e mais rico do frontend |

**Páginas:**
- `ExecutiveOverviewPage` → `/governance/executive`
- `ExecutiveDrillDownPage` → `/governance/executive/drilldown`
- `ExecutiveFinOpsPage` → `/governance/executive/finops`
- `ReportsPage` → `/governance/reports`
- `CompliancePage` → `/governance/compliance`
- `RiskCenterPage` → `/governance/risk`
- `RiskHeatmapPage` → `/governance/risk/heatmap`
- `FinOpsPage` → `/governance/finops`
- `ServiceFinOpsPage` → `/governance/finops/service/:serviceId`
- `TeamFinOpsPage` → `/governance/finops/team/:teamId`
- `DomainFinOpsPage` → `/governance/finops/domain/:domainId`
- `PolicyCatalogPage` → `/governance/policies`
- `EnterpriseControlsPage` → `/governance/controls`
- `EvidencePackagesPage` → `/governance/evidence`
- `MaturityScorecardsPage` → `/governance/maturity`
- `BenchmarkingPage` → `/governance/benchmarking`
- `TeamsOverviewPage` → `/governance/teams`
- `TeamDetailPage` → `/governance/teams/:teamId`
- `DomainsOverviewPage` → `/governance/domains`
- `DomainDetailPage` → `/governance/domains/:domainId`
- `GovernancePacksOverviewPage` → `/governance/packs`
- `GovernancePackDetailPage` → `/governance/packs/:packId`
- `WaiversPage` → `/governance/waivers`
- `DelegatedAdminPage` → `/governance/delegated-admin`
- `GovernanceConfigurationPage` → `/platform/configuration/governance`

---

### Módulo FE 8: identity-access

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/identity-access/` |
| **Propósito** | Autenticação, gestão de utilizadores, sessões, JIT, delegação, break glass |
| **Páginas** | 12+ |
| **Backend correspondente** | IdentityAccess |
| **Estado** | Ativo |

**Páginas (auth — eager load):**
- `LoginPage` → `/login`
- `ForgotPasswordPage` → `/forgot-password`
- `ResetPasswordPage` → `/reset-password`
- `ActivationPage` → `/activate`
- `MfaPage` → `/mfa`
- `InvitationPage` → `/invitation`
- `TenantSelectionPage` → `/select-tenant`

**Páginas (lazy):**
- `UsersPage` → `/users`
- `EnvironmentsPage` → `/environments` *(sem item de menu)*
- `BreakGlassPage` → `/break-glass`
- `JitAccessPage` → `/jit-access`
- `DelegationPage` → `/delegations`
- `AccessReviewPage` → `/access-reviews`
- `MySessionsPage` → `/my-sessions`
- `UnauthorizedPage` → `/unauthorized`

---

### Módulo FE 9: integrations

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/integrations/` |
| **Propósito** | Hub de integrações, execuções de ingestão, frescor de dados |
| **Páginas** | 4 |
| **Backend correspondente** | Governance (IntegrationHub), Ingestion.Api |
| **Estado** | Ativo |

**Páginas:**
- `IntegrationHubPage` → `/integrations`
- `ConnectorDetailPage` → `/integrations/:connectorId`
- `IngestionExecutionsPage` → `/integrations/executions`
- `IngestionFreshnessPage` → `/integrations/freshness`

---

### Módulo FE 10: notifications

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/notifications/` |
| **Propósito** | Centro de notificações, preferências, configuração |
| **Páginas** | 3 |
| **Backend correspondente** | Notifications |
| **Estado** | Ativo — mas sem item de menu no sidebar |

**Páginas:**
- `NotificationCenterPage` → `/notifications` *(sem item no sidebar)*
- `NotificationPreferencesPage` → `/notifications/preferences` *(sem item no sidebar)*
- `NotificationConfigurationPage` → `/platform/configuration/notifications`

---

### Módulo FE 11: operational-intelligence

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/operational-intelligence/` |
| **Propósito** | Configuração de FinOps e inteligência operacional |
| **Páginas** | 1 |
| **Backend correspondente** | OperationalIntelligence (CostIntelligence) |
| **Estado** | Mínimo — apenas 1 página de configuração |

**Páginas:**
- `OperationsFinOpsConfigurationPage` → `/platform/configuration/operations-finops`

**Nota:** O módulo backend `OperationalIntelligence` tem 5 DbContexts e 7 endpoint modules. A maioria do frontend correspondente está no módulo `operations`, não aqui.

---

### Módulo FE 12: operations

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/operations/` |
| **Propósito** | Incidentes, runbooks, reliability, automação, comparação de ambientes, operações de plataforma |
| **Páginas** | 8+ |
| **Backend correspondente** | OperationalIntelligence |
| **Estado** | Ativo |

**Páginas:**
- `IncidentsPage` → `/operations/incidents`
- `IncidentDetailPage` → `/operations/incidents/:incidentId`
- `RunbooksPage` → `/operations/runbooks`
- `TeamReliabilityPage` → `/operations/reliability`
- `ServiceReliabilityDetailPage` → `/operations/reliability/:serviceId`
- `AutomationWorkflowsPage` → `/operations/automation`
- `AutomationAdminPage` → `/operations/automation/admin`
- `AutomationWorkflowDetailPage` → `/operations/automation/:workflowId`
- `EnvironmentComparisonPage` → `/operations/runtime-comparison`
- `PlatformOperationsPage` → `/platform/operations`

---

### Módulo FE 13: product-analytics

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/product-analytics/` |
| **Propósito** | Analytics de produto — adoção, personas, jornadas, valor |
| **Páginas** | 5 |
| **Backend correspondente** | Governance (ProductAnalyticsEndpointModule) |
| **Estado** | Parcial |

**Páginas:**
- `ProductAnalyticsOverviewPage` → `/analytics`
- `ModuleAdoptionPage` → `/analytics/adoption`
- `PersonaUsagePage` → `/analytics/personas`
- `JourneyFunnelPage` → `/analytics/journeys`
- `ValueTrackingPage` → `/analytics/value`

---

### Módulo FE 14: shared

| Atributo | Detalhe |
|----------|---------|
| **Pasta** | `src/features/shared/` |
| **Propósito** | Página de dashboard inicial partilhada |
| **Páginas** | 1 |
| **Estado** | Ativo |

**Páginas:**
- `DashboardPage` → `/`

---

### Módulo FE 15: Shell e Componentes Transversais

| Componente | Localização | Propósito |
|-----------|-------------|-----------|
| `AppShell` | `components/shell/AppShell.tsx` | Layout base com sidebar e topbar |
| `AppSidebar` | `components/shell/AppSidebar.tsx` | Sidebar de navegação com 50 itens |
| `AppTopbar` | `components/shell/AppTopbar.tsx` | Barra superior |
| `AppTopbarSearch` | `components/shell/AppTopbarSearch.tsx` | Busca global |
| `WorkspaceSwitcher` | `components/shell/WorkspaceSwitcher.tsx` | Troca de workspace/tenant |
| `ContextStrip` | `components/shell/ContextStrip.tsx` | Strip de contexto de ambiente |
| `EnvironmentBanner` | `components/shell/EnvironmentBanner.tsx` | Banner de ambiente |
| `ProtectedRoute` | `components/ProtectedRoute.tsx` | Guarda de rota por permissão |
| `AuthContext` | `contexts/AuthContext.tsx` | Estado de autenticação |
| `EnvironmentContext` | `contexts/EnvironmentContext.tsx` | Contexto de ambiente/tenant |
| `PersonaContext` | `contexts/PersonaContext.tsx` | Contexto de persona/role |

---

## PARTE 3 — Cruzamento Frontend ↔ Backend

### Frontend sem Backend Claro

| Módulo FE | Situação |
|-----------|----------|
| `operational-intelligence` (1 página) | Apenas config page — o backend OperationalIntelligence é consumido pelo módulo `operations` |
| `product-analytics` | Consome `ProductAnalyticsEndpointModule` dentro de Governance — mas não está claro se usa dados reais |
| `shared/DashboardPage` | Dashboard raiz — não está claro quais endpoints agrega |

### Backend sem Frontend Correspondente Claro

| Módulo Backend | Situação |
|----------------|----------|
| `Configuration` | Configurações espalhadas em sub-páginas de `/platform/configuration/*` — sem módulo FE dedicado |
| `AuditCompliance` | Apenas 1 página frontend — backend provavelmente tem mais capacidades |
| Ingestion.Api | Plataforma de ingestão — não tem páginas de UI dedicadas (apenas `IngestionExecutionsPage` e `IngestionFreshnessPage` em integrations) |

### Código sem Documentação Módulo-a-Módulo

| Módulo | Estado Documental |
|--------|-------------------|
| `Notifications` | Documentação espalhada em 25+ guias de execução, sem doc arquitetural |
| `Configuration` | Sem documento de módulo dedicado |
| `AuditCompliance` | Sem documentação |
| `OperationalIntelligence` | Documentação de reliability existe mas não cobre Automation, Cost, Runtime |

---

## Totais do Inventário

| Dimensão | Qtd |
|----------|-----|
| Módulos backend | 9 |
| Building blocks | 5 |
| Projetos de plataforma | 3 |
| DbContexts totais | 20 |
| Endpoint modules | 57 |
| Migrations totais | ~50 |
| Módulos frontend | 15 |
| Páginas frontend com rota | ~80 |
| Páginas frontend sem rota | 4 |
| Locales i18n | 4 |
