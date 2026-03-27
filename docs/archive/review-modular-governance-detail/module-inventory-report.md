# Inventário de Módulos — Backend e Frontend — NexTraceOne

> **Data:** 2026-03-24  
> **Tipo:** Auditoria Estrutural — Parte 2  
> **Fonte de verdade:** Código do repositório

---

## Resumo

| Métrica | Backend | Frontend |
|---------|---------|----------|
| Módulos/Features | 9 | 13 (+1 shared) |
| Projetos C# / Páginas | 71 .csproj | 105 páginas |
| DbContexts | 16+ | — |
| Endpoints | 200+ | — |
| Hooks de API | — | 18+ |
| Locales | — | 4 (en, es, pt-BR, pt-PT) |
| Testes | 1709+ backend | 398+ frontend |

---

## PARTE A — Módulos Backend

### Arquitetura Geral

- **Framework:** .NET (C#), Modular Monolith
- **Padrão:** DDD + CQRS com MediatR
- **Camadas por módulo:** Domain, Application, Infrastructure, API, Contracts
- **Hosts:** ApiHost, BackgroundWorkers, Ingestion.Api
- **Building Blocks:** 5 bibliotecas partilhadas (Core, Application, Infrastructure, Observability, Security)

---

### 1. Identity & Access (`src/modules/identityaccess/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Autenticação, autorização, gestão de utilizadores, sessões, delegações |
| **Entidades** | Role, TenantMembership, ExternalIdentity, SsoGroupMapping, SecurityEvent, JitAccessRequest, EnvironmentPolicy |
| **DbContexts** | IdentityDbContext |
| **Migrations** | InitialCreate, AddIsPrimaryProductionToEnvironment |
| **Endpoints** | 11 módulos: auth (login, federated, OIDC, refresh, logout), roles, users, tenants, environments, sessions, JIT, break-glass, delegations, access-reviews, runtime-context |
| **Estado** | **Ativo** — módulo fundacional completo com persistência real |
| **Documentação** | Parcial — coberto em assessment/ e security/ |
| **Testes** | 186+ (conforme REBASELINE.md) |

---

### 2. Catalog (`src/modules/catalog/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Catálogo de serviços, APIs, contratos, portal do developer, Source of Truth |
| **Subdomínios** | Graph (serviços/APIs/dependências), Contracts (drafts/reviews/scorecards), Portal (pesquisa/playground), SourceOfTruth (referências) |
| **Entidades** | ServiceAsset, ApiAsset, ConsumerAsset, ConsumerRelationship, GraphSnapshot, NodeHealthRecord, ContractDraft, ContractReview, ContractScorecard, SpectralRuleset, SavedSearch, PlaygroundSession, LinkedReference |
| **DbContexts** | CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext |
| **Migrations** | InitialCreate (por database) |
| **Endpoints** | 4 módulos: catalog (services, APIs, graph, impact), contracts (drafts, reviews), developerportal, truth (source of truth) |
| **Estado** | **Ativo** — módulo central com maior maturidade |
| **Documentação** | Boa — assessment/, user-guide/service-catalog.md |
| **Testes** | 430+ (conforme REBASELINE.md) |

---

### 3. Change Governance (`src/modules/changegovernance/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Inteligência de mudanças, workflows, promoções, governance de rulesets |
| **Subdomínios** | ChangeIntelligence (releases, eventos, blast radius), RulesetGovernance (linting), Workflow (templates, aprovações, SLA), Promotion (requests, gates) |
| **Entidades** | Release, ChangeEvent, BlastRadiusReport, RollbackAssessment, Ruleset, RulesetBinding, WorkflowTemplate, WorkflowInstance, PromotionRequest, PromotionGate |
| **DbContexts** | ChangeIntelligenceDbContext, RulesetGovernanceDbContext, WorkflowDbContext, PromotionDbContext |
| **Migrations** | InitialCreate (por database) |
| **Endpoints** | 4 módulos: changes (analysis, deployments, freeze, confidence, releases), rulesets, workflows (approvals, templates, evidence), promotions |
| **Estado** | **Ativo** — pilar central do produto |
| **Documentação** | Boa — CHANGE-CONFIDENCE.md, user-guide/change-governance.md |
| **Testes** | 179+ (conforme REBASELINE.md) |

---

### 4. Operational Intelligence (`src/modules/operationalintelligence/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Incidentes, fiabilidade, automação, custos, runtime |
| **Subdomínios** | Cost (registos, trends, otimização), Incidents (incidentes, runbooks, mitigação), Reliability (snapshots), Automation (workflows, auditoria), Runtime (baselines, drift, profiles) |
| **Entidades** | CostRecord, CostSnapshot, IncidentRecord, RunbookRecord, MitigationWorkflowRecord, ReliabilitySnapshot, AutomationWorkflowRecord, RuntimeSnapshot, RuntimeBaseline, DriftFinding |
| **DbContexts** | CostIntelligenceDbContext, IncidentDbContext, ReliabilityDbContext, AutomationDbContext, RuntimeIntelligenceDbContext |
| **Migrations** | InitialCreate (por database) |
| **Endpoints** | 5 módulos: cost (summary, by-service, trends), incidents (CRUD, runbooks, mitigation), reliability (metrics, SLAs), automation (CRUD), runtime (baselines, drift, profiles) |
| **Estado** | **Ativo** — módulo grande com 5 subdomínios |
| **Documentação** | Parcial — docs/reliability/, user-guide/operations.md |
| **Testes** | 164+ (conforme REBASELINE.md) |

---

### 5. AI Knowledge (`src/modules/aiknowledge/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | IA externa, governança de modelos, orquestração, assistente, agentes |
| **Subdomínios** | ExternalAI (providers, policies, consultas), Governance (modelos, quotas, routing, políticas), Runtime (execução), Orchestration (conversas, knowledge, agentes) |
| **Entidades** | ExternalAiProvider, AiProvider, AiSource, AIAccessPolicy, AiTokenQuotaPolicy, AiContext, AiConversation, KnowledgeCaptureEntry, GeneratedTestArtifact |
| **DbContexts** | ExternalAiDbContext, AiGovernanceDbContext, AiOrchestrationDbContext |
| **Migrations** | InitialCreate + expansões |
| **Endpoints** | 3 módulos: externalai (knowledge, query), ai (providers, models, sources, policies, budgets, audit, routing, IDE), aiorchestration (conversations, agents, assistant, knowledge) |
| **Estado** | **Parcial** — ~20-25% maturidade conforme AI-LOCAL-IMPLEMENTATION-AUDIT.md; governança funcional, orquestração com stubs |
| **Documentação** | Boa — 6+ documentos dedicados (AI-ARCHITECTURE.md, AI-GOVERNANCE.md, etc.) |
| **Testes** | 5+ testes reais (em crescimento) |

---

### 6. Governance (`src/modules/governance/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Governança organizacional, compliance, risk, FinOps, integrações, analytics, plataforma |
| **Entidades** | GovernanceDomain, GovernancePack, Team, GovernanceWaiver, IngestionSource, IngestionExecution, IntegrationConnector |
| **Enums** | 45+ (ComplianceStatus, GovernanceMaturity, RiskLevel, WaiverStatus, etc.) |
| **DbContexts** | GovernanceDbContext |
| **Migrations** | InitialCreate, Phase5Enrichment, AddLastProcessedAt |
| **Endpoints** | 20+ módulos: domains, teams, executive (overview, risk, maturity, benchmarking), governance/packs, platform (health, jobs, queues), integrations, analytics, finops, compliance, risk, controls, waivers, evidence, reports, admin, policies, onboarding, context |
| **Estado** | **Ativo** — módulo muito grande que serve como "catch-all" para funcionalidades transversais |
| **Documentação** | Parcial — assessment/, docs/governance/ |
| **Testes** | Variáveis por sub-feature |

> **Observação crítica:** O módulo Governance é excessivamente amplo. Contém responsabilidades de Integrations, Analytics, FinOps, Platform Operations, Onboarding e Delegated Admin que poderiam ser bounded contexts separados.

---

### 7. Configuration (`src/modules/configuration/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Gestão de configuração centralizada do sistema |
| **Entidades** | ConfigurationEntry, ConfigurationDefinition, ConfigurationAuditEntry |
| **DbContexts** | ConfigurationDbContext |
| **Endpoints** | /api/v1/configuration |
| **Estado** | **Ativo** — ~345 definições de configuração com 251 testes |
| **Documentação** | Parcial — coberto em docs/execution/CONFIGURATION-* (35 ficheiros) mas sem documento unificado |
| **Seed Data** | Extenso — 8 fases de seeding com definições por domínio |

---

### 8. Audit & Compliance (`src/modules/auditcompliance/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Auditoria, hash chain, compliance, campanhas |
| **Entidades** | AuditCampaign, AuditChainLink, AuditEvent, CompliancePolicy, ComplianceResult, RetentionPolicy |
| **DbContexts** | AuditDbContext |
| **Migrations** | InitialCreate, Phase3ComplianceDomain |
| **Endpoints** | audit (events, trail, search, verify-chain, report, compliance, campaigns) |
| **Estado** | **Ativo** — funcionalidade de auditoria com hash chain |
| **Documentação** | Parcial |
| **Testes** | Variáveis |

---

### 9. Notifications (`src/modules/notifications/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Centro de notificações, preferências, entrega |
| **Entidades** | Notification, NotificationDelivery, NotificationPreference |
| **DbContexts** | NotificationDbContext |
| **Endpoints** | /api/v1/notifications |
| **Estado** | **Ativo** — funcionalidade base implementada |
| **Documentação** | Boa — 12+ documentos em docs/execution/NOTIFICATIONS-* |
| **Testes** | Variáveis |

---

### Building Blocks (5 projetos)

| Projeto | Propósito |
|---------|-----------|
| `BuildingBlocks.Core` | Entidades base, Result<T>, especificações, erros |
| `BuildingBlocks.Application` | MediatR, CQRS, localização de erros |
| `BuildingBlocks.Infrastructure` | RepositoryBase, DbContextBase, OutboxEventBus, TenantRlsInterceptor, AuditInterceptor, EncryptedStringConverter |
| `BuildingBlocks.Security` | PermissionAuthorizationHandler, AesGcmEncryptor, AssemblyIntegrityChecker, CSRF |
| `BuildingBlocks.Observability` | Serilog, health checks, tracing distribuído |

### Platform Hosts (3 projetos)

| Projeto | Propósito |
|---------|-----------|
| `NexTraceOne.ApiHost` | Gateway REST principal — registra todos os 9 módulos |
| `NexTraceOne.BackgroundWorkers` | Processamento de jobs em background |
| `NexTraceOne.Ingestion.Api` | API dedicada de ingestão de dados |

---

## PARTE B — Módulos Frontend

### Arquitetura Geral

- **Framework:** React 19 + Vite 7 + TypeScript 5.9
- **State Management:** React Query (@tanstack/react-query)
- **Routing:** React Router DOM
- **i18n:** 4 locales (en, es, pt-BR, pt-PT), ~639 KB total
- **Design System:** Tailwind CSS com tokens customizados (--nto-*)
- **Auth:** ProtectedRoute com permissões server-side

---

### 1. AI Hub (`src/frontend/src/features/ai-hub/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 11: AiAssistant (483 linhas), AiAgents, AgentDetail, AiAnalysis, AiAudit, AiPolicies, AiRouting, IdeIntegrations, ModelRegistry, TokenBudget, AiIntegrationsConfiguration |
| **Rotas** | `/ai/assistant`, `/ai/agents`, `/ai/agents/:agentId`, `/ai/models`, `/ai/policies`, `/ai/routing`, `/ai/ide`, `/ai/budgets`, `/ai/audit`, `/ai/analysis` |
| **Menu** | 9 itens na secção "AI Hub" |
| **Estado** | **Parcial** — UI extensiva, backend com stubs; AiAssistant é a página mais complexa |
| **i18n** | Sim |
| **Hooks** | Não identificados hooks dedicados (provavelmente usa API direta) |

---

### 2. Catalog (`src/frontend/src/features/catalog/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 12: ServiceCatalog (1010 linhas), ServiceCatalogList, ServiceDetail, SourceOfTruthExplorer, ServiceSourceOfTruth, ContractSourceOfTruth, DeveloperPortal, GlobalSearch, ContractDetail, ContractList, Contracts, CatalogContractsConfiguration |
| **Rotas** | `/services`, `/services/:serviceId`, `/source-of-truth`, `/portal`, `/search` |
| **Menu** | 4 itens em "Services" + "Knowledge" |
| **Estado** | **Ativo** — módulo mais maduro; ServiceCatalogPage é a página mais longa |
| **i18n** | Sim |
| **Observação** | ContractDetailPage, ContractListPage e ContractsPage existem mas **não estão roteadas no App.tsx** (órfãs) |

---

### 3. Contracts (`src/frontend/src/features/contracts/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 8: ContractCatalog, CreateService, DraftStudio, ContractWorkspace, ContractGovernance, SpectralRulesetManager, CanonicalEntityCatalog, ContractPortal |
| **Rotas definidas** | `/contracts`, `/contracts/new`, `/contracts/studio`, `/contracts/studio/:draftId`, `/contracts/:contractVersionId` |
| **Rotas NO MENU mas SEM rota** | `/contracts/governance`, `/contracts/spectral`, `/contracts/canonical` |
| **Menu** | 6 itens em "Contracts" (3 sem rota real) |
| **Estado** | **Parcial** — 3 páginas no menu sem route no App.tsx; ContractPortalPage órfã |
| **i18n** | Sim |
| **Hooks** | 12 hooks dedicados (useContractList, useContractDetail, useDraftWorkflow, etc.) |

> **⚠️ Problema crítico:** 3 itens de menu apontam para rotas inexistentes (`/contracts/governance`, `/contracts/spectral`, `/contracts/canonical`). Clicando, o utilizador é redirecionado para a home.

---

### 4. Change Governance (`src/frontend/src/features/change-governance/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 6: ChangeCatalog, ChangeDetail, Releases, Workflow, Promotion, WorkflowConfiguration |
| **Rotas** | `/changes`, `/changes/:changeId`, `/releases`, `/workflow`, `/promotion` |
| **Menu** | 4 itens em "Changes" |
| **Estado** | **Ativo** — todas as rotas funcionais |
| **i18n** | Sim |

---

### 5. Operations (`src/frontend/src/features/operations/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 10: Incidents, IncidentDetail, Runbooks, TeamReliability, ServiceReliabilityDetail, AutomationWorkflows, AutomationAdmin, AutomationWorkflowDetail, EnvironmentComparison, PlatformOperations |
| **Rotas** | `/operations/incidents`, `/operations/incidents/:incidentId`, `/operations/runbooks`, `/operations/reliability`, `/operations/reliability/:serviceId`, `/operations/automation`, `/operations/automation/admin`, `/operations/automation/:workflowId`, `/operations/runtime-comparison`, `/platform/operations` |
| **Menu** | 5 itens em "Operations" |
| **Estado** | **Ativo** — funcionalidade completa |
| **i18n** | Sim |

---

### 6. Governance (`src/frontend/src/features/governance/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 22: ExecutiveOverview, ExecutiveDrillDown, ExecutiveFinOps, Reports, Compliance, RiskCenter, RiskHeatmap, FinOps, ServiceFinOps, TeamFinOps, DomainFinOps, PolicyCatalog, EnterpriseControls, EvidencePackages, MaturityScorecards, Benchmarking, TeamsOverview, TeamDetail, DomainsOverview, DomainDetail, GovernancePacksOverview, GovernancePackDetail, Waivers, DelegatedAdmin, GovernanceConfiguration |
| **Rotas** | 24 rotas sob `/governance/` |
| **Menu** | 7 itens em "Governance" + 2 em "Organization" |
| **Estado** | **Ativo** — módulo maior do frontend |
| **i18n** | Sim |

---

### 7. Identity & Access (`src/frontend/src/features/identity-access/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 15: Login, ForgotPassword, ResetPassword, Activation, Mfa, Invitation, TenantSelection, Users, Environments, BreakGlass, JitAccess, Delegation, AccessReview, MySessions, Unauthorized |
| **Rotas** | Públicas: `/login`, `/forgot-password`, `/reset-password`, `/activate`, `/mfa`, `/invitation`, `/select-tenant`; Protegidas: `/users`, `/environments`, `/break-glass`, `/jit-access`, `/delegations`, `/access-reviews`, `/my-sessions` |
| **Menu** | 7 itens em "Administration" |
| **Estado** | **Ativo** — módulo fundacional completo |
| **i18n** | Sim |

---

### 8. Integrations (`src/frontend/src/features/integrations/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 4: IntegrationHub, ConnectorDetail, IngestionExecutions, IngestionFreshness |
| **Rotas** | `/integrations`, `/integrations/:connectorId`, `/integrations/executions`, `/integrations/freshness` |
| **Menu** | 1 item em "Integrations" |
| **Estado** | **Ativo** — funcionalidade base |
| **i18n** | Sim |

---

### 9. Configuration (`src/frontend/src/features/configuration/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 2: ConfigurationAdmin, AdvancedConfigurationConsole (6 tabs: explorer, diff, import/export, rollback, history, health) |
| **Rotas** | `/platform/configuration`, `/platform/configuration/advanced` |
| **Menu** | 1 item em "Administration" (com sub-rotas para configuração por domínio) |
| **Estado** | **Ativo** — consolida 345+ definições |
| **i18n** | Sim |
| **Hooks** | useConfiguration (React Query factory) |

---

### 10. Notifications (`src/frontend/src/features/notifications/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 3: NotificationCenter, NotificationPreferences, NotificationConfiguration |
| **Rotas** | `/notifications`, `/notifications/preferences`, `/platform/configuration/notifications` |
| **Menu** | Acesso via bell icon e admin config |
| **Estado** | **Ativo** |
| **i18n** | Sim |
| **Hooks** | 4 hooks dedicados (useNotifications, useNotificationList, useNotificationPreferences, useNotificationHelpers) |

---

### 11. Audit & Compliance (`src/frontend/src/features/audit-compliance/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 1: AuditPage |
| **Rotas** | `/audit` |
| **Menu** | 1 item em "Administration" |
| **Estado** | **Ativo** — página única |
| **i18n** | Sim |

---

### 12. Product Analytics (`src/frontend/src/features/product-analytics/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Páginas** | 5: ProductAnalyticsOverview, ModuleAdoption, PersonaUsage, JourneyFunnel, ValueTracking |
| **Rotas** | `/analytics`, `/analytics/adoption`, `/analytics/personas`, `/analytics/journeys`, `/analytics/value` |
| **Menu** | 1 item em "Analytics" |
| **Estado** | **Parcial** — possivelmente preview |
| **i18n** | Sim |

---

### 13. Shared (`src/frontend/src/features/shared/`)

| Propriedade | Detalhe |
|-------------|---------|
| **Propósito** | Componentes e utilitários partilhados entre features |
| **Estado** | **Ativo** |

---

## PARTE C — Cruzamento Frontend × Backend

### Frontend sem Backend Correspondente

| Feature Frontend | Observação |
|-----------------|------------|
| Product Analytics | Endpoints analytics existem no módulo Governance mas são limitados |
| Contracts — ContractGovernance | Página existe, rota não existe, backend parcial |
| Contracts — SpectralRulesetManager | Página existe, rota não existe, backend parcial |
| Contracts — CanonicalEntityCatalog | Página existe, rota não existe, sem evidência backend |

### Backend sem Frontend Correspondente

| Módulo Backend | Observação |
|----------------|------------|
| Governance — Onboarding endpoints | `/api/v1/onboarding/*` sem UI dedicada |
| Governance — Controls | `/api/v1/controls/summary` — UI existe (EnterpriseControlsPage) mas alinhamento incerto |
| ChangeGovernance — Rulesets | `/api/v1/rulesets` sem UI dedicada além de SpectralRulesetManager (não roteada) |

### Código sem Documentação

| Componente | Observação |
|-----------|------------|
| Product Analytics (5 páginas) | Zero documentação dedicada |
| Configuration (345 definições) | Apenas documentação fragmentada em execution/ |
| Integrations Hub (4 páginas) | Zero documentação dedicada |
| Building Blocks (5 bibliotecas) | Zero documentação dedicada |
| Platform Hosts (3 projetos) | Apenas LOCAL-SETUP.md parcial |

### Documentação sem Código Correspondente

| Documento | Observação |
|-----------|------------|
| CONTRACT-STUDIO-VISION.md | Visão expandida que excede a implementação atual do DraftStudioPage |
| AI-DEVELOPER-EXPERIENCE.md | IDE integrations existem como página mas sem implementação real de extensões |
| DEPLOYMENT-ARCHITECTURE.md | Descreve deployment que pode não estar 100% implementado |
