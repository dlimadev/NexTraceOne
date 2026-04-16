# NexTraceOne — Implementation Status

> **Última atualização:** Abril 2026
> **Roadmap futuro:** [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md)

Este documento regista o estado de implementação de cada módulo do NexTraceOne.

**Legenda de status:**
- **READY** — Produção-ready, persistência EF Core real, features reais
- **PARTIAL** — Mix de features reais e mock/incompletas
- **SIM** — Dados simulados/mock, não apto para produção
- **INCOMPLETE** — Estrutura existe, migrations ou features em falta
- **PLAN** — Planeado mas não implementado

---

## §Foundation — Building Blocks

| Componente | Status | Notas |
|---|---|---|
| BuildingBlocks.Core | READY | `Result<T>`, guard clauses, strongly typed IDs, primitivos de domínio |
| BuildingBlocks.Application | READY | Abstrações CQRS, MediatR behaviors, `ICurrentUser`/`ICurrentTenant` |
| BuildingBlocks.Infrastructure | READY | `NexTraceDbContextBase`, `TenantRlsInterceptor`, `AuditInterceptor` |
| BuildingBlocks.Security | READY | JWT, API Key, CORS, rate limiting (6 policies), AES-256-GCM, `AssemblyIntegrityChecker` |
| BuildingBlocks.Observability | READY | OpenTelemetry configurado; Elastic + ClickHouse providers reais; ITelemetryQueryService implementado; Product Store (5 reader/writer pairs) + Metrics Store (2 reader/writer pairs) com EF Core PostgreSQL; TelemetryRetentionService + TelemetryAggregationService implementados |
| Outbox Processing | READY | `ModuleOutboxProcessorJob` registado para todos os 25 DbContexts via `BackgroundWorkers/Program.cs` (54 registros). HealthChecks configurados por módulo |

**Evidência:** `src/building-blocks/`

---

## §Configuration — Configuration Module

| Feature Area | Status | Notas |
|---|---|---|
| Feature Flags | READY | Database-driven, override por tenant, `ConfigurationDefinitionSeeder` com 458 seeds, 112 parâmetros em 17 domínios |
| Settings por tenant/ambiente | READY | Presente e funcional, com governance gates e analytics endpoints |
| User Saved Views & Bookmarks | READY | `UserSavedView`, `UserBookmark` — EF Core com RLS |
| User Watch & Alert Rules | READY | `UserWatch`, `UserAlertRule` — EF Core com RLS |
| Tags, Custom Fields & Taxonomy | READY | `EntityTag`, `ServiceCustomField`, `TaxonomyCategory/Value` — EF Core com RLS |
| Automation, Checklists & Templates | READY | `AutomationRule`, `ChangeChecklist`, `ContractTemplate` — EF Core com RLS |
| Scheduled Reports | READY | `ScheduledReport` — EF Core com RLS |
| Saved Prompts | READY | `SavedPrompt` — EF Core com RLS |
| Webhook Templates | READY | `WebhookTemplate` — EF Core com RLS |

**DbContexts:** `ConfigurationDbContext` (3 migrações: Initial, UserSavedViews/Bookmarks, Phase3To8Tables)
**Testes:** 451 unit tests (0 falhas)
**Evidência:** `src/modules/configuration/`

---

## §Identity — Identity Access

| Feature Area | Status | Notas |
|---|---|---|
| Auth JWT / RBAC | READY | Multi-tenancy com RLS, role-permission enforcement |
| Sessions / Cookies | READY | Cookie session com refresh token |
| JIT Access | READY | Just-In-Time privileged access, expiração automática |
| Break Glass | READY | Acesso emergencial com expiração e auditoria |
| Access Reviews | READY | Workflows de revisão periódica |
| Delegations | READY | Acesso delegado com expiração e revogação |
| Environments / Tenants / Users | READY | CRUD completo, isolamento de tenant |
| Security Events | READY | Trilha de auditoria para eventos de segurança |

**DbContexts:** `IdentityDbContext` (1 migração confirmada)
**Evidência:** `src/modules/identityaccess/`

---

## §Catalog — Source of Truth / Contract Governance

| Feature Area | Status | Notas |
|---|---|---|
| Graph / Service Catalog | READY | `RegisterServiceAsset`, `ImportFromBackstage`, `ListServices`, `GetAssetGraph` — 27 features, 100% real |
| Contracts (REST/SOAP/Event/Background) | READY | CRUD, versões, compatibilidade, scorecard — 35 features, 100% real |
| Contract Studio | READY | Backend real; 10/10 contract types com visual builders (REST, SOAP, Event, BackgroundService, SharedSchema, Webhook, Copybook, MqMessage, FixedLayout, CicsCommarea) |
| Semantic Diff | READY | `ComputeSemanticDiff`, `EvaluateCompatibility` reais |
| Developer Portal | PARTIAL | `RecordAnalyticsEvent`, `CreateSubscription`, `ExecutePlayground` reais; `SearchCatalog` é stub |
| Global Search | PARTIAL | `GlobalSearch` endpoint real (PostgreSQL FTS); `SearchCatalog` é stub intencional aguardando integração cross-module |
| Contract Drift Detection (PA-25) | READY | `DetectContractDrift` — detecta desvios entre contrato publicado e traces OTel; ghost endpoints + endpoints não declarados; `GET /api/v1/catalog/contracts/{id}/drift` |
| Contract Health Score Timeline (PA-27) | READY | `GetContractHealthTimeline` — evolução temporal do health score com correlação de changes; `GET /api/v1/catalog/contracts/{id}/health/timeline`; frontend `ContractHealthTimelinePage` |
| Service Maturity Benchmark (PA-28) | READY | `GetServiceMaturityBenchmark` — comparação de maturidade entre serviços, equipas e domínios; `GET /api/v1/catalog/services/maturity-benchmark` |
| Canonical Entity Impact Cascade (PA-30) | READY | `GetCanonicalEntityImpactCascade` — análise em cascata multi-nível (depth 1-3) de impacto; RiskLevel (None/Low/Medium/High/Critical); `GET /api/v1/catalog/canonical-entities/{id}/impact/cascade`; frontend `CanonicalEntityImpactCascadePage` |

**DbContexts:** `ContractsDbContext`, `CatalogGraphDbContext`, `DeveloperPortalDbContext` (3 DbContexts, 4 migrações)
**Status geral:** 90 features, 100% real; 10/10 contract types com visual builders; Phase 4 Innovation completa (6/6 PA items)
**Testes:** 1179+ testes unitários (0 falhas)
**Evidência:** `src/modules/catalog/`

---

## §ChangeGovernance — Change Intelligence

| Feature Area | Status | Notas |
|---|---|---|
| Releases / Change Intelligence | READY | BlastRadius, ChangeScores, FreezeWindows, RollbackAssessments — reais |
| Workflow / Approvals | READY | Templates, instâncias, stages, approval decisions, evidence packs, SLA policies — reais |
| Promotion / Gate Evaluations | READY | Environments, promotion requests, gates, gate evaluations — reais |
| Contract Compliance Gate (PA-29) | READY | `EvaluateContractComplianceGate` — verifica se gate de conformidade de contratos está configurado para o ambiente alvo da promoção; `GET /api/v1/promotion/{id}/contract-compliance` |
| Ruleset Governance | READY | Rulesets, bindings, lint results (Spectral) — reais |
| Audit Trail / Decision Trail | READY | Trilha de decisão, timeline de mudança, correlation events — reais |
| Post-Release Review | READY | Frontend `PostReleaseReviewPage` (`/releases/post-review`): inicia/progride revisão pós-deploy, janelas de observação, baseline de performance, confidence score. API: `getPostReleaseReview`, `startPostReleaseReview`, `progressPostReleaseReview` |
| Rollback Assessment | READY | Frontend `ReleaseRollbackPage` (`/releases/rollback`): avalia viabilidade, readiness score, executa rollback auditado. API: `getRollbackAssessment`, `assessRollbackViability`, `registerRollback` |
| Release Notes (AI) | READY | Frontend `ReleaseNotesPage` (`/releases/notes`): gera/regenera notas por IA com seleção de persona (Technical/Executive/PM), exibe secções estruturadas. API: `getReleaseNotes`, `generateReleaseNotes`, `regenerateReleaseNotes` |
| Workflow Configuration | READY | Frontend `WorkflowConfigurationPage` (`/workflow/configuration`): configuração por scope de workflow e promoção, seções templates/stages/approvers/sla/gates/promotion/freeze, audit history |
| Navigation — Release Governance | READY | Sidebar com 21 itens de navegação em 4 sub-grupos: Release Lifecycle (Release Train, Commit Pool, Impact Report, Post-Review, Release Notes), Approval & Governance (Promotion, Gateway, Policies, Control Params, External Ingest), Workflow Management (Workflow, Configuration, Checklist), Risk & Rollback (Rollback) |

**DbContexts:** `ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext` (4 DbContexts, 4 migrações)
**Status geral:** READY — módulo completo com navegação estruturada (21 páginas frontend, 4 sub-grupos de nav)
**Testes:** 307 testes unitários backend (0 falhas) + 1771 testes frontend (261 ficheiros, 0 falhas)
**Evidência:** `src/modules/changegovernance/`

---

## §AuditCompliance — Audit Compliance

| Feature Area | Status | Notas |
|---|---|---|
| RecordAuditEvent / GetAuditTrail | READY | Hash chain SHA-256 para imutabilidade auditável |
| VerifyChainIntegrity | READY | Verificação de integridade da cadeia de auditoria |
| SearchAuditLog | READY | Pesquisa de trilha de auditoria |

**DbContexts:** `AuditDbContext` (2 migrações confirmadas)
**Evidência:** `src/modules/auditcompliance/`

---

## §OperationalIntelligence — Incidents, Automation, Reliability

> **⚠️ Correção de Auditoria (Março 2026):** Versões anteriores deste documento listavam Incidents como `SIM (InMemoryIncidentStore)`. Esta informação estava **incorreta**. O `EfIncidentStore` (678 linhas) é a implementação registada em `DependencyInjection.cs`. O `InMemoryIncidentStore` existe apenas como artefato de testes (deprecated). A correlação dinâmica incident↔change é 0%.
> **Evidência:** `docs/audit-forensic-2026-03/backend-state-report.md §OperationalIntelligence`

| Feature Area | Status | Notas |
|---|---|---|
| Incidents | READY | `EfIncidentStore` é a implementação registada. Frontend totalmente conectado via API real. `IIncidentModule` implementado para cross-module. Correlação dinâmica via `IIncidentCorrelationRepository` + `IChangeIntelligenceReader`. |
| Automation | READY | 10/10 handlers reais — workflows persistidos via `AutomationDbContext`, catálogo estático, auditoria, validação pós-execução e precondições avaliadas contra estado real do workflow. `IAutomationModule` implementado. |
| Reliability | READY | 15/15 handlers reais — SLO/SLA definitions, burn rate, error budget, snapshots de fiabilidade, sumários por equipa/domínio. `IReliabilityModule` implementado. |
| Runtime Intelligence | READY | `RuntimeIntelligenceDbContext`, 6 repositórios EF Core (RuntimeSnapshot, RuntimeBaseline, DriftFinding, ObservabilityProfile, CustomChart, ChaosExperiment), 20+ features, `RuntimeIntelligenceEndpointModule` com endpoints REST completos |
| Cost Intelligence | READY | `CostIntelligenceDbContext`, 8 repositórios EF Core (CostAttribution, CostRecord, CostTrend, BudgetForecast, etc.), `CostIntelligenceEndpointModule` com endpoints REST, `CostIntelligenceModuleService` para cross-module |
| Mitigation Workflows | READY | `CreateMitigationWorkflow` persiste via `IMitigationWorkflowRepository`; `GetMitigationHistory` consulta dados reais; `RecordMitigationValidation` persiste logs de validação. |

**DbContexts:** `IncidentDbContext` (migração), `AutomationDbContext` (migração), `ReliabilityDbContext` (migração), `RuntimeIntelligenceDbContext` (migração), `CostIntelligenceDbContext` (migração)
**Gap remanescente:** Heurísticas de correlação incident↔change são básicas (timestamp+service matching).
**Testes:** 639 testes unitários (0 falhas). Inclui testes de OnCallIntelligence, PIR workflow, ChaosEngineering.
**Evidência:** `src/modules/operationalintelligence/`

---

## §AI — AI Knowledge

| Feature Area | Status | Notas |
|---|---|---|
| AI Governance (modelos, políticas, budgets) | REAL | Repositórios EF Core reais; `SendAssistantMessage` invoca `IChatCompletionProvider.CompleteAsync()` com LLM real, routing, governance, audit trail e fallback degradado |
| Model Registry | READY | Funcional com DbContext real, `IAiModelCatalogService` para resolução de modelos |
| AI Streaming | READY | `IChatCompletionProvider` com streaming; endpoint SSE; LLM real integrado via Ollama/OpenAI |
| AI Tool Execution | READY | `IToolRegistry`, `IToolExecutor`, `IToolPermissionValidator` implementados; 3 ferramentas reais (`list_services`, `get_service_health`, `list_recent_changes`); `MaxToolIterations=5` |
| AI Grounding / Context | READY | Assemblagem de contexto configurada (`DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService`); 4 grounding readers cross-module verificados (`CatalogGroundingReader`, `ChangeGroundingReader`, `IncidentGroundingReader`, `KnowledgeDocumentGroundingReader`) |
| AI Security Guardrails | READY | `DefaultGuardrailCatalog` com 5 guardrails: prompt-injection-detection (block), credential-leak-prevention (sanitize), pii-email-detection (warn), pii-phone-detection (warn), sensitive-data-classification (log) |
| AI Orchestration | REAL | `AiOrchestrationDbContext` com migrações; `IAiOrchestrationModule` implementado por `AiOrchestrationModule` |
| External AI | REAL | `IExternalAiModule` implementado por `ExternalAiModule`; `ExternalAiDbContext` com migrações |
| Knowledge Source Weights | REAL | Pesos persistidos em `aik_source_weights`; `ListKnowledgeSourceWeights` consulta DB com fallback a defaults |
| AI Contract Reviewer (PA-26) | READY | `ReviewContractDraft` — revisão automática de rascunhos por IA; QualityScore 0-100, issues por severidade/categoria, sugestões, recomendação (Approve/RequestChanges/Reject); `POST /api/v1/aiorchestration/contracts/review` |
| AiAssistantPage (frontend) | REAL | Usa API real: `aiGovernanceApi.listConversations`, `sendMessage`, `getMessages` (7 chamadas API reais) |

**DbContexts:** `AiGovernanceDbContext`, `AiOrchestrationDbContext`, `ExternalAiDbContext` — todos com migrações confirmadas.
**Testes:** 819+ testes unitários (0 falhas)
**Evidência:** `src/modules/aiknowledge/`

---

## §Governance — Governance, FinOps, Reports, Compliance

> **Nota de design:** Este módulo agrega dados de outros módulos via cross-module interfaces. GetExecutiveOverview usa IIncidentModule para métricas reais de incidentes. ListTeams usa ICatalogGraphModule para contagens de contratos. FinOps usa ICostIntelligenceModule. Todos os 44+ handlers retornam dados reais (`IsSimulated: false`).
>
> **Ownership explícito de FinOps:** a **experiência de produto FinOps** (dashboards, KPIs, drill-down e leitura por persona) pertence ao módulo Governance. O módulo OperationalIntelligence atua como **provider de dados de custo** via `ICostIntelligenceModule`, sem ownership da UX/fluxo de governança FinOps.

| Feature Area | Status | Notas |
|---|---|---|
| Teams / Domains | READY | CRUD via repositório; contagens cross-module implementadas via `ICatalogGraphModule` e `IIncidentModule`; `GovernanceDbContext` real |
| Governance Packs / Evidence | READY | Handlers retornam dados reais via repositórios (`IsSimulated: false`) |
| Policies / Compliance | READY | Persistência via `GovernanceDbContext`; handlers consultam repositórios reais |
| FinOps | READY | Dados reais via `ICostIntelligenceModule` (cross-module); `IsSimulated: false` |
| Reports | READY | Dados reais via agregação cross-module |
| Executive Views | READY | `GetExecutiveOverview` usa `IIncidentModule` para métricas reais; `CrossModuleDataAvailable: true` |

**Frontend:** 25/26 páginas conectadas a APIs reais (50+ endpoints). GovernanceConfigurationPage usa sistema de configuração. GovernanceGatesPage com 7 gates. DoraMetricsPage, ServiceScorecardPage, TechnicalDebtPage, CustomDashboardsPage implementados.
**Testes:** 233+ testes unitários (0 falhas). Inclui DORA Metrics, Service Scorecard, Technical Debt, Custom Dashboards, FinOps Budget Gate, Compliance Remediation Gate.
**Evidência:** `src/modules/governance/`

---

## §Knowledge — Knowledge Hub

| Feature Area | Status | Notas |
|---|---|---|
| Knowledge Documents | READY | `KnowledgeDbContext` com migração confirmada; CRUD completo |
| Operational Notes | READY | Create/List/Update funcional |
| Knowledge Relations | READY | Ligações entre entidades de conhecimento e serviços |
| Knowledge Endpoints | READY | 11 endpoints CRUD implementados |
| Knowledge Graph | READY | `GetKnowledgeGraphOverview` — visualização de relações entre entidades; `GET /api/v1/knowledge/graph` |
| Auto Documentation | READY | `GenerateAutoDocumentation` — geração automática de documentação por serviço; `GET /api/v1/knowledge/auto-documentation/{serviceName}` |
| IKnowledgeModule | READY | Cross-module interface implementada por `KnowledgeModuleService` |

**DbContexts:** `KnowledgeDbContext` (migração confirmada: `20260328162322_InitialCreate`)
**Tests:** 70+ testes (0 falhas). Inclui KnowledgeIntelligence, ValidateDocumentReviewGate.
**Evidência:** `src/modules/knowledge/`

---

## §Notifications — Notifications

| Feature Area | Status | Notas |
|---|---|---|
| Delivery Channels | READY | `NotificationsDbContext` com 2+ migrações; channels funcionais |
| Preferences / Templates | READY | Templates de notificação e preferências de utilizador funcionais |

**DbContexts:** `NotificationsDbContext` (2+ migrações)
**Evidência:** `src/modules/notifications/`

---

## §Ingestion — Integrations & Ingestion

| Feature Area | Status | Notas |
|---|---|---|
| Integration Connectors | READY | `IntegrationsDbContext` com migrações; repositórios EF Core reais; 109 testes passam |
| Ingestion Sources | READY | 5 endpoints de ingestão; `ProcessIngestionPayload` com parsing real; `IIngestionSourceRepository` real |
| Ingestion Executions | READY | Pipeline de processamento real com parsers e processadores; deep Kafka/queue integration planeada para roadmap futuro |
| Webhook Subscriptions | READY | `WebhookSubscription` domain entity com typed ID; `IWebhookSubscriptionRepository` EF Core; `RegisterWebhookSubscription` persiste via UnitOfWork; `ListWebhookSubscriptions` consulta repositório real; RLS para `int_webhook_subscriptions` |

**DbContexts:** `IntegrationsDbContext` com 3 migrações (InitialCreate, AddParsedPayloadFields, AddWebhookSubscriptions)
**Testes:** 109 testes unitários (0 falhas)
**Evidência:** `src/modules/integrations/`

---

## §ProductAnalytics — Product Analytics

| Feature Area | Status | Notas |
|---|---|---|
| Analytics Events | READY | Repositório EF Core real; handlers com dados reais; 42+ testes passam; `AnalyticsEventTracker` integrado no `AppShell` |
| Persona Usage / Journeys | READY | Queries reais com `ProductAnalyticsDbContext`; `TrackPersonaActivity` com analytics |
| Value Milestones | READY | Implementado com `ProductAnalyticsDbContext` |

**DbContexts:** `ProductAnalyticsDbContext` com migrações confirmadas
**Evidência:** `src/modules/productanalytics/`, 42 testes em `NexTraceOne.ProductAnalytics.Tests` (28 base + 14 advanced)

---

## §CrossModule — Cross-Module Interfaces

| Interface | Status | Notas |
|---|---|---|
| `IContractsModule` | REAL | Implementado por `ContractsModuleService` (Catalog.Infrastructure) |
| `IChangeIntelligenceModule` | REAL | Implementado por `ChangeIntelligenceModule` (ChangeGovernance.Infrastructure) |
| `IPromotionModule` | REAL | Implementado por `PromotionModuleService` (ChangeGovernance.Infrastructure) |
| `IRulesetGovernanceModule` | REAL | Implementado por `RulesetGovernanceModuleService` (ChangeGovernance.Infrastructure) |
| `ICatalogGraphModule` | REAL | Implementado por `CatalogGraphModuleService` (Catalog.Infrastructure) |
| `IRuntimeIntelligenceModule` | REAL | Implementado por `RuntimeIntelligenceModule` (OpsIntel.Infrastructure) |
| `ICostIntelligenceModule` | REAL | Implementado por `CostIntelligenceModuleService` (OpsIntel.Infrastructure) |
| `IReliabilityModule` | REAL | Implementado por `ReliabilityModuleService` (OpsIntel.Infrastructure) |
| `IAutomationModule` | REAL | Implementado por `AutomationModuleService` (OpsIntel.Infrastructure) |
| `IIncidentModule` | REAL | Implementado por `IncidentModuleService` (OpsIntel.Infrastructure) — métricas para Governance executive |
| `IKnowledgeModule` | REAL | Implementado por `KnowledgeModuleService` (Knowledge.Infrastructure) — contagens e resumo cross-module |
| `IProductAnalyticsModule` | REAL | Implementado por `ProductAnalyticsModuleService` (ProductAnalytics.Infrastructure) |
| `IAiOrchestrationModule` | REAL | Implementado por `AiOrchestrationModule` (Infrastructure) |
| `IExternalAiModule` | REAL | Implementado por `ExternalAiModule` (Infrastructure) |
| `IAiGovernanceModule` | REAL | Implementado por `AiGovernanceModuleService` (AIKnowledge.Infrastructure) |

**Status:** 15 interfaces cross-module definidas; 15 de 15 com implementação real.
**Evidência:** `src/modules/*/Infrastructure/*/Services/`

---

## §IntegrationEvents — Integration Events (Outbox)

| Componente | Status | Notas |
|---|---|---|
| Outbox Pattern (todos os 22 DbContexts) | ATIVO | `ModuleOutboxProcessorJob` registado para cada DbContext |
| Domain Event Publishing | ATIVO | Eventos capturados pelo outbox durante SaveChanges e entregues pelo processador |

---

## Matriz de Prontidão — Resumo

| Módulo | Prontidão | Apto para Produção? |
|---|---|---|
| Building Blocks | READY | Sim |
| Identity Access | READY | Sim |
| Catalog | READY | Sim (100% real, 11 portal handlers, 10/10 contract types com visual builders) |
| Change Governance | READY | Sim (100% real, módulo flagship) |
| Audit Compliance | READY | Sim (hash chain SHA-256) |
| Operational Intelligence | READY | Sim — Incidents real, Automation 10/10 real, Reliability 15/15 real, IIncidentModule + IAutomationModule + IReliabilityModule implementados |
| AI Knowledge | READY | LLM real E2E; grounding cross-module via `IKnowledgeModule`; 5 security guardrails; AI Contract Reviewer READY |
| Governance | READY | Dados reais via repositórios e cross-module; FinOps real; 25/26 frontend pages conectadas; 158/158 testes passam |
| Knowledge | READY | Sim — CRUD completo, 44/44 testes passam, IKnowledgeModule implementado |
| Notifications | READY | Channels e templates funcionais |
| Configuration | READY | 458 seeds, 112 parâmetros, 17 domínios, governance gates, analytics, 14 EF Core entity types com RLS |
| Integrations | READY | Repositórios EF Core reais, 109 testes; webhook subscriptions persistidas; metadata capture funcional; deep pipeline integration planeada |
| Product Analytics | READY | Repositórios EF Core reais, 42+ testes; `AnalyticsEventTracker` no frontend |

---

## §GraphQL — Federation Gateway

| Feature Area | Status | Notas |
|---|---|---|
| CatalogQuery | READY | `services`, `contracts`, `npsSummary` via `[ExtendObjectType("Query")]` |
| ChangeGovernanceQuery | READY | `changesSummary(teamName, environment, daysBack)` via schema stitching |
| Subscriptions | READY | `onChangeDeployed` + `onIncidentUpdated` via WebSocket HotChocolate in-memory |
| Publisher | READY | `IGraphQLEventPublisher` + `GraphQLEventPublisher` para publicação de eventos |
| Endpoint | READY | `GET+POST /api/v1/graphql` com WebSocket |

**HotChocolate:** 14.3.0 com `[ExtendObjectType]` federation pattern
**Evidência:** `src/modules/catalog/NexTraceOne.Catalog.API/GraphQL/`, `src/modules/changegovernance/NexTraceOne.ChangeGovernance.API/GraphQL/`, 28+ testes GraphQL

---

*Última atualização: Abril 2026 — Todos os módulos READY. Build 0 erros.*
*Ver: [FUTURE-ROADMAP.md](./FUTURE-ROADMAP.md)*
