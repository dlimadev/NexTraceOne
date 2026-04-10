# Plano de Validação Completa — NexTraceOne

> **Objetivo**: Validar módulo a módulo, camada a camada, todo o fluxo funcional do NexTraceOne — frontend, backend, database, testes e documentação — identificando bugs, gaps, implementações incompletas ou parciais.

> **Estratégia**: Cada módulo será validado de forma independente e completa, seguindo a mesma checklist estruturada. Ao final, uma validação cross-module garante integridade entre bounded contexts.

---

## Índice

1. [Inventário do Sistema](#1-inventário-do-sistema)
2. [Metodologia de Validação](#2-metodologia-de-validação)
3. [Módulo 1 — IdentityAccess (Foundation)](#módulo-1--identityaccess-foundation)
4. [Módulo 2 — Catalog (Services & Contracts)](#módulo-2--catalog-services--contracts)
5. [Módulo 3 — ChangeGovernance (Changes)](#módulo-3--changegovernance-changes)
6. [Módulo 4 — OperationalIntelligence (Operations)](#módulo-4--operationalintelligence-operations)
7. [Módulo 5 — AIKnowledge (AI)](#módulo-5--aiknowledge-ai)
8. [Módulo 6 — Governance (Governance)](#módulo-6--governance-governance)
9. [Módulo 7 — Knowledge (Knowledge Hub)](#módulo-7--knowledge-knowledge-hub)
10. [Módulo 8 — Notifications](#módulo-8--notifications)
11. [Módulo 9 — Integrations](#módulo-9--integrations)
12. [Módulo 10 — AuditCompliance](#módulo-10--auditcompliance)
13. [Módulo 11 — Configuration](#módulo-11--configuration)
14. [Módulo 12 — ProductAnalytics](#módulo-12--productanalytics)
15. [Building Blocks (Cross-cutting)](#building-blocks-cross-cutting)
16. [Platform (ApiHost / Workers / Ingestion)](#platform-apihost--workers--ingestion)
17. [Frontend Global](#frontend-global)
18. [Infraestrutura (DB / RLS / Seed / Docker)](#infraestrutura-db--rls--seed--docker)
19. [Validação Cross-Module](#validação-cross-module)
20. [Documentação Global](#documentação-global)
21. [Priorização e Ordem de Execução](#priorização-e-ordem-de-execução)

---

## 1. Inventário do Sistema

### Backend — 12 Módulos

| # | Módulo | DbContexts | DbSets | Endpoints | Features | Testes |
|---|--------|-----------|--------|-----------|----------|--------|
| 1 | IdentityAccess | 1 | 23 | 13 | 38 | ~58 |
| 2 | Catalog | 6 | ~66 | ~20 | ~225 | ~158 |
| 3 | ChangeGovernance | 4 | ~29 | ~17 | ~91 | ~40 |
| 4 | OperationalIntelligence | 6 | ~52 | ~11 | ~80+ | ~75 |
| 5 | AIKnowledge | 3 | ~49 | ~5 | ~106 | ~87 |
| 6 | Governance | 1 | 22 | ~21 | ~40+ | ~36 |
| 7 | Knowledge | 1 | 4 | 1 | ~18 | ~16 |
| 8 | Notifications | 1 | 6 | 2 | ~17 | ~50 |
| 9 | Integrations | 1 | 4 | 2 | ~13 | ~16 |
| 10 | AuditCompliance | 1 | 6 | 1 | ~23 | ~15 |
| 11 | Configuration | 1 | 20 | 17 | ~57 | ~30 |
| 12 | ProductAnalytics | 1 | 1 | 1 | ~9 | ~4 |

### Frontend

| Área | Contagem |
|------|----------|
| Feature modules | 16 |
| Componentes globais | 54+ |
| Shell components | 15+ |
| Rotas/Páginas (ficheiros de rota) | 8 |
| E2E specs | 16 |
| Idiomas i18n | 4 (en, es, pt-BR, pt-PT) |
| Contextos React | 4 (Auth, Environment, Persona, Theme) |

### Infraestrutura

| Área | Ficheiros |
|------|-----------|
| RLS (apply-rls.sql) | ~100 tabelas |
| Seed production | seed_production.sql |
| Seed development | seed_development.sql |
| Docker Compose | 3 ficheiros |
| Platform projects | 3 (ApiHost, Workers, Ingestion) |
| Building Blocks | 5 |

---

## 2. Metodologia de Validação

### Para cada módulo, validar 7 camadas:

```
┌─────────────────────────────────────────────┐
│ 1. DOMAIN — Entidades, Enums, Value Objects │
│ 2. APPLICATION — Features (Commands/Queries)│
│ 3. INFRASTRUCTURE — Persistence, Services   │
│ 4. API — Endpoints, Contratos, Validações   │
│ 5. FRONTEND — Páginas, Componentes, i18n    │
│ 6. DATABASE — Migrações, RLS, Seed          │
│ 7. TESTES — Cobertura, Qualidade, Gaps      │
└─────────────────────────────────────────────┘
```

### Critérios de validação por camada:

#### Domain
- [ ] Entidades têm validação de invariantes
- [ ] Strongly-typed IDs onde aplicável
- [ ] Enums documentados e completos
- [ ] Value Objects com igualdade implementada
- [ ] Domain Events definidos para operações relevantes
- [ ] Errors/Exceptions tipados e com mensagens claras
- [ ] Sem dependência de infraestrutura

#### Application
- [ ] Cada feature tem Command/Query + Handler + Validator
- [ ] FluentValidation em todos os commands
- [ ] CancellationToken propagado em todas operações async
- [ ] Result<T> para falhas controladas
- [ ] Guard clauses no início dos handlers
- [ ] Sem lógica de infraestrutura no handler
- [ ] Logging estruturado em operações importantes
- [ ] DTOs de resposta claros e estáveis

#### Infrastructure
- [ ] DbContext com todas as entidades mapeadas
- [ ] Entity configurations completas (índices, constraints, lengths)
- [ ] Repositories implementam interfaces do Application
- [ ] Migrations existem e são sequenciais
- [ ] Outbox pattern integrado
- [ ] Auditoria integrada via NexTraceDbContextBase
- [ ] Services externos abstraídos via interfaces

#### API
- [ ] Todos os features têm endpoint correspondente
- [ ] Endpoints usam DTOs adequados (não expõem entidades)
- [ ] Validação de input no endpoint
- [ ] Status codes HTTP corretos
- [ ] Autorização aplicada (permissões, tenant, ambiente)
- [ ] Rate limiting onde necessário
- [ ] Documentação OpenAPI/Swagger correcta

#### Frontend
- [ ] Página existe para cada feature principal
- [ ] Componentes usam design system consistente
- [ ] i18n completo (sem textos hardcoded)
- [ ] Estados de loading, erro e vazio implementados
- [ ] Chamadas API usam TanStack Query com query keys centralizadas
- [ ] Persona awareness (conteúdo adapta-se ao papel)
- [ ] Responsividade real
- [ ] Navegação consistente (breadcrumbs, tabs, menu)
- [ ] Sem GUIDs expostos ao utilizador final
- [ ] Segurança frontend (sem dangerouslySetInnerHTML não sanitizado)

#### Database
- [ ] Migrações cobrem todas as entidades do DbContext
- [ ] RLS aplicado em todas as tabelas do módulo
- [ ] Seed data existe para dados de referência
- [ ] Índices adequados para queries frequentes
- [ ] Foreign keys e constraints definidos
- [ ] Prefixo de tabela correto (iam_, cat_, chg_, etc.)

#### Testes
- [ ] Testes unitários para domain (entidades, value objects)
- [ ] Testes de application (handlers, validators)
- [ ] Testes de infrastructure (repositories, services)
- [ ] Testes E2E para fluxos críticos
- [ ] Todos os testes passam (0 failures)
- [ ] Cobertura adequada vs número de features
- [ ] Testes não dependem de estado externo

---

## Módulo 1 — IdentityAccess (Foundation)

**Pilar**: Foundation / Security  
**Personas impactadas**: Todas  
**Prioridade**: 🔴 Crítica (base de todo o sistema)

### Backend

#### 1.1 Domain
- [ ] Validar entidades: User, Tenant, Role, Permission, Session, TenantMembership
- [ ] Validar entidades SSO: ExternalIdentity, SsoGroupMapping
- [ ] Validar entidades privileged access: BreakGlassRequest, JitAccessRequest, Delegation
- [ ] Validar entidades access review: AccessReviewCampaign, AccessReviewItem
- [ ] Validar entidade: SecurityEvent
- [ ] Validar entidades environment: Environment, EnvironmentAccess
- [ ] Validar entidades authorization: RolePermission, ModuleAccessPolicy, UserRoleAssignment
- [ ] Value Objects: Email, HashedPassword, MfaPolicy, AuthenticationPolicy, SessionPolicy
- [ ] Domain Events para criação/alteração de user, role, tenant, sessão
- [ ] Errors tipados para cada subdomain

#### 1.2 Application (38 features)
- [ ] **Auth**: LocalLogin, FederatedLogin, StartOidcLogin, OidcCallback, Logout, RefreshToken, VerifyMfaChallenge
- [ ] **User Management**: CreateUser, ActivateUser, DeactivateUser, GetCurrentUser, GetUserProfile, ListTenantUsers
- [ ] **Roles & Permissions**: CreateRole, ListRoles, UpdateRole, DeleteRole, ListPermissions, SeedDefaultRolePermissions
- [ ] **Sessions**: ListActiveSessions, RevokeSession
- [ ] **Tenants**: ListMyTenants, SelectTenant
- [ ] **Environments**: CreateEnvironment, UpdateEnvironment, ListEnvironments, GetPrimaryProductionEnvironment, SetPrimaryProductionEnvironment
- [ ] **Access Control**: GrantEnvironmentAccess, AssignRole
- [ ] **Privileged Access**: RequestBreakGlass, RevokeBreakGlass, ListBreakGlassRequests, RequestJitAccess, DecideJitAccess
- [ ] **Delegation**: CreateDelegation, ListDelegations, RevokeDelegation
- [ ] **Access Review**: StartAccessReviewCampaign, GetAccessReviewCampaign, ListAccessReviewCampaigns, DecideAccessReviewItem
- [ ] **Security**: ListSecurityEvents, SeedDefaultModuleAccessPolicies, ChangePassword
- [ ] Validar FluentValidation em todos os commands
- [ ] Validar CancellationToken em todos os handlers

#### 1.3 Infrastructure
- [ ] IdentityDbContext — 23 DbSets todos mapeados correctamente
- [ ] Entity Configurations (17 ficheiros) — verificar completude
- [ ] Migrations sequenciais e sem conflitos
- [ ] Repositories cobrem todas as abstractions
- [ ] SecurityEventAuditBehavior pipeline funcional

#### 1.4 API (13 endpoints)
- [ ] AuthEndpoints — login, logout, refresh, MFA
- [ ] UserEndpoints — CRUD, ativar, desativar
- [ ] TenantEndpoints — gestão de tenant
- [ ] EnvironmentEndpoints — CRUD ambientes
- [ ] RolePermissionEndpoints — CRUD roles
- [ ] BreakGlassEndpoints — request, revoke, list
- [ ] JitAccessEndpoints — request, decide
- [ ] DelegationEndpoints — create, list, revoke
- [ ] AccessReviewEndpoints — campaign, decide
- [ ] SecurityEventsEndpoints — list events
- [ ] RuntimeContextEndpoints — contexto atual
- [ ] CookieSessionEndpoints — gestão sessão
- [ ] Verificar que todos os endpoints exigem autenticação (exceto login)
- [ ] Verificar tenant isolation em todos os endpoints

#### 1.5 Frontend
- [ ] Páginas: Login, Users, Environments, BreakGlass, JIT Access, Delegation, AccessReview, MySessions
- [ ] AuthContext funcional (login, logout, refresh)
- [ ] EnvironmentContext funcional (switch de ambiente)
- [ ] PersonaContext funcional (adaptação por papel)
- [ ] i18n completo em todas as telas de identity
- [ ] Fluxo completo de login → seleção de tenant → dashboard
- [ ] Gestão de roles e permissões funcional na UI
- [ ] Estados de erro e loading em todos os formulários

#### 1.6 Database
- [ ] Prefixo `iam_` em todas as tabelas
- [ ] RLS em todas as 23 tabelas (verificar apply-rls.sql)
- [ ] Seed production: PlatformAdmin role + 93 permissões
- [ ] Seed development: 6 roles adicionais + 7 utilizadores de teste
- [ ] Índices em campos de pesquisa frequente (email, tenantId)

#### 1.7 Testes (~58 ficheiros)
- [ ] Executar: `dotnet test tests/modules/identityaccess/`
- [ ] Verificar 0 failures
- [ ] Domain tests cobrem entidades e value objects
- [ ] Application tests cobrem features críticas (login, roles, environments)
- [ ] Infrastructure tests cobrem services (TotpVerifier, PermissionResolver, etc.)
- [ ] Gap analysis: features sem teste correspondente

---

## Módulo 2 — Catalog (Services & Contracts)

**Pilar**: Service Governance + Contract Governance  
**Personas impactadas**: Engineer, Tech Lead, Architect  
**Prioridade**: 🔴 Crítica (core do produto)

### Backend

#### 2.1 Domain (8 subdomains)
- [ ] **Contracts**: Entidades (ContractVersion, Draft, Review, Example, etc.), Enums, ValueObjects, Services
- [ ] **DependencyGovernance**: ServiceDependencyProfile, PackageDependency
- [ ] **DeveloperExperience**: DeveloperSurvey
- [ ] **Graph**: ApiAsset, ServiceAsset, ConsumerRelationship, DiscoveryRun, etc.
- [ ] **LegacyAssets**: MainframeSystem, CobolProgram, Copybook, CicsTransaction, etc.
- [ ] **Portal**: Subscription, PlaygroundSession, CodeGenerationRecord, etc.
- [ ] **SourceOfTruth**: entidades de service registry
- [ ] **Templates**: ServiceTemplate
- [ ] Verificar domain events em operações de contrato (create, sign, publish)
- [ ] Validar strongly-typed IDs

#### 2.2 Application (~225 features)
- [ ] **Contract CRUD**: Create, List, Get, Update, Delete para REST, SOAP, Event, BackgroundService
- [ ] **Contract Studio**: Draft workflows, review, approval, sign
- [ ] **Versioning**: CreateVersion, ComputeDiff, SemanticDiff
- [ ] **Validation**: ValidateContractIntegrity, EvaluateContractRules, LintContract
- [ ] **Publication**: Publish, PublishToMarketplace, CreateListing
- [ ] **Health**: ComputeHealthScore, GetContractScorecard
- [ ] **Compliance**: CreateComplianceGate, EvaluateContractCompliance
- [ ] **Pipeline**: ExecuteContractPipeline, PipelineExecution
- [ ] **Negotiation**: ContractNegotiation, AddNegotiationComment
- [ ] **Impact**: SimulateDependencyImpact, ImpactSimulation
- [ ] **Schema Evolution**: SchemaEvolutionAdvice
- [ ] **Graph**: Service discovery, topology, dependencies
- [ ] **Legacy Assets**: CICS, IMS, Copybook, DB2, ZosConnect, COBOL
- [ ] **Portal**: Developer portal, playground, code generation, API keys
- [ ] **Source of Truth**: Service registry operations
- [ ] **Templates**: Service template management

#### 2.3 Infrastructure (6 DbContexts)
- [ ] ContractsDbContext — ~31 DbSets
- [ ] CatalogGraphDbContext — 16 DbSets
- [ ] LegacyAssetsDbContext — 14 DbSets
- [ ] DeveloperPortalDbContext — 8 DbSets
- [ ] DependencyGovernanceDbContext — 2 DbSets
- [ ] TemplatesDbContext — 1 DbSet
- [ ] DeveloperExperienceDbContext — 1 DbSet
- [ ] Verificar migrations para cada DbContext
- [ ] Verificar entity configurations completas

#### 2.4 API (~20 endpoint modules)
- [ ] ContractsEndpointModule — CRUD contratos
- [ ] ContractStudioEndpointModule — workflow de criação
- [ ] SoapContractEndpointModule — contratos SOAP
- [ ] EventContractEndpointModule — contratos de eventos
- [ ] BackgroundServiceContractEndpointModule — background services
- [ ] PublicationCenterEndpointModule — publicação
- [ ] DeveloperPortalEndpointModule — portal developer
- [ ] ContractPipelineEndpointModule — pipeline
- [ ] ScaffoldExportEndpointModule — scaffold/export
- [ ] SourceOfTruthEndpointModule — source of truth
- [ ] ServiceTemplateEndpointModule — templates
- [ ] DeveloperSurveyEndpointModule — surveys
- [ ] DeveloperExperienceEndpointModule — DX
- [ ] Legacy endpoints: CICS, Mainframe, IMS, Copybook, DB2, ZosConnect, Cobol
- [ ] GraphQL endpoints para graph/topology

#### 2.5 Frontend
- [ ] Service Catalog — listagem, detalhe, pesquisa
- [ ] Contract Studio — criação, edição, diff
- [ ] Contract Workspace — visualização completa do contrato
- [ ] SOAP, Event, Background contract editors
- [ ] Publication center — publicar, listar publicações
- [ ] Developer Portal — documentação, playground
- [ ] Legacy Assets — gestão de assets mainframe
- [ ] Graph/Topology — visualização de dependências
- [ ] Health Dashboard — saúde dos contratos
- [ ] Marketplace — listagem, reviews
- [ ] i18n completo em todas as telas
- [ ] Persona awareness (Engineer vs Architect vs Tech Lead)

#### 2.6 Database
- [ ] Prefixo `cat_` em todas as tabelas
- [ ] RLS em todas as tabelas do módulo
- [ ] Migrations sequenciais por cada DbContext
- [ ] Verificar innovative ideas DbSets: SemanticDiffResults, ContractComplianceGates, ContractComplianceResults, ContractListings, MarketplaceReviews, ImpactSimulations, SchemaEvolutionAdvices, PipelineExecutions, ContractNegotiations, NegotiationComments

#### 2.7 Testes (~158 ficheiros, ~1441 testes)
- [ ] Executar: `dotnet test tests/modules/catalog/`
- [ ] Verificar 0 failures
- [ ] Coverage por subdomain (Contracts, Graph, Legacy, Portal, SourceOfTruth, Templates, DeveloperExperience)
- [ ] Gap analysis: features das Innovative Ideas com testes

---

## Módulo 3 — ChangeGovernance (Changes)

**Pilar**: Change Intelligence & Production Change Confidence  
**Personas impactadas**: Engineer, Tech Lead, Product  
**Prioridade**: 🔴 Crítica (confiança em mudanças)

### Backend

#### 3.1 Domain (4 subdomains)
- [ ] **ChangeIntelligence**: Release, ChangeEvent, ChangeScore, BlastRadiusReport, FreezeWindow, etc.
- [ ] **Promotion**: DeploymentEnvironment, PromotionRequest, PromotionGate, GateEvaluation
- [ ] **Workflow**: WorkflowTemplate, WorkflowInstance, WorkflowStage, EvidencePack, SlaPolicy
- [ ] **RulesetGovernance**: Ruleset, RulesetBinding, LintResult
- [ ] Validar domain events para deploy, promotion, approval

#### 3.2 Application (~91 features)
- [ ] **ChangeIntelligence (51)**: CalculateBlastRadius, ComputeChangeScore, GenerateReleaseNotes, GetChangeConfidenceTimeline, NotifyDeployment, RecordConfidenceEvent, etc.
- [ ] **Promotion (11)**: CreatePromotionRequest, ApprovePromotion, EvaluatePromotionGates, EvaluateContractComplianceGate, etc.
- [ ] **Workflow (15)**: InitiateWorkflow, ApproveStage, GenerateEvidencePack, ExportEvidencePackPdf, etc.
- [ ] **RulesetGovernance (9)**: UploadRuleset, ExecuteLintForRelease, ComputeRulesetScore, etc.
- [ ] Validar todos os handlers têm validators

#### 3.3 Infrastructure (4 DbContexts)
- [ ] ChangeIntelligenceDbContext — 16 DbSets
- [ ] WorkflowDbContext — 6 DbSets
- [ ] PromotionDbContext — 4 DbSets
- [ ] RulesetGovernanceDbContext — 3 DbSets
- [ ] Verificar migrations e configurations

#### 3.4 API (~17 endpoints)
- [ ] ChangeIntelligence endpoints (8): Analysis, Confidence, Deployment, Freeze, Intelligence, Release, TraceCorrelation
- [ ] Workflow endpoints (5): Approval, Evidence, Status, Template
- [ ] Promotion endpoints
- [ ] RulesetGovernance endpoints
- [ ] GraphQL integration

#### 3.5 Frontend
- [ ] Change Intelligence dashboard — timeline, scores, releases
- [ ] Change Confidence visualization
- [ ] Blast Radius view
- [ ] Promotion governance — gates, approvals
- [ ] Workflow — templates, instâncias, stages
- [ ] Evidence Pack — geração, exportação PDF
- [ ] Release Calendar
- [ ] Freeze Windows — criação, listagem
- [ ] Ruleset governance — upload, lint, score
- [ ] DORA Metrics
- [ ] i18n completo
- [ ] Persona awareness

#### 3.6 Database
- [ ] Prefixo `chg_` em todas as tabelas
- [ ] RLS: chg_blast_radius_reports, chg_promotion_gates, chg_promotion_gate_evaluations + todas as demais
- [ ] Migrations por cada DbContext

#### 3.7 Testes (~40 ficheiros)
- [ ] Executar: `dotnet test tests/modules/changegovernance/`
- [ ] Verificar 0 failures
- [ ] Gap analysis: 91 features vs ~40 ficheiros de teste (cobertura adequada?)
- [ ] Verificar testes para ChangeIntelligence (23), Promotion (5), Workflow (5), RulesetGovernance (4)

---

## Módulo 4 — OperationalIntelligence (Operations)

**Pilar**: Operational Reliability + Operational Consistency  
**Personas impactadas**: Engineer, Tech Lead, Platform Admin  
**Prioridade**: 🟠 Alta

### Backend

#### 4.1 Domain (5 subdomains)
- [ ] **Incidents**: IncidentRecord, MitigationWorkflow, Runbook, ChangeCorrelation, PostIncidentReview, IncidentNarrative
- [ ] **Reliability**: ReliabilitySnapshot, SloDefinition, SlaDefinition, ErrorBudget, BurnRate, ServiceFailurePrediction, CapacityForecast, IncidentPredictionPattern, HealingRecommendation
- [ ] **Runtime**: RuntimeSnapshot, DriftFinding, ObservabilityProfile, CustomChart, ChaosExperiment, AnomalyNarrative, EnvironmentDriftReport, OperationalPlaybook, PlaybookExecution, ResilienceReport
- [ ] **Cost**: CostSnapshot, CostAttribution, CostTrend, ServiceCostProfile, BudgetForecast, EfficiencyRecommendation
- [ ] **Automation**: AutomationWorkflow, ValidationRecord, AuditRecord

#### 4.2 Application (~80+ features)
- [ ] **Incidents**: RegisterIncident, EscalateIncident, ResolveIncident, CorrelateWithChange, CreateMitigationWorkflow, CreateRunbook, PerformPostIncidentReview, GenerateIncidentNarrative
- [ ] **Reliability**: CreateSlo, CreateSla, CalculateErrorBudget, PredictServiceFailure, ForecastCapacity, GenerateHealingRecommendation
- [ ] **Runtime**: RecordRuntimeSnapshot, DetectDrift, CreateCustomChart, RunChaosExperiment, GenerateAnomalyNarrative, DetectEnvironmentDrift, ExecutePlaybook, AssessResilience
- [ ] **Cost**: ImportCostData, AttributeCost, AnalyzeCostTrend, ForecastBudget, RecommendEfficiency
- [ ] **Automation**: CreateWorkflow, RunValidation
- [ ] **Telemetry**: ServiceMetrics, DependencyMetrics, Topology, Anomalies

#### 4.3 Infrastructure (6 DbContexts)
- [ ] IncidentDbContext — 8 DbSets
- [ ] ReliabilityDbContext — 9 DbSets
- [ ] RuntimeIntelligenceDbContext — 11 DbSets
- [ ] CostIntelligenceDbContext — 8 DbSets
- [ ] AutomationDbContext — 3 DbSets
- [ ] TelemetryStoreDbContext — 7 DbSets
- [ ] Verificar migrations e configurations

#### 4.4 API (~11 endpoints)
- [ ] IncidentEndpointModule — CRUD incidentes
- [ ] MitigationEndpointModule — mitigação
- [ ] RunbookEndpointModule — runbooks
- [ ] PostIncidentReviewEndpointModule — PIR
- [ ] ReliabilityEndpointModule — SLO/SLA/error budget
- [ ] RuntimeIntelligenceEndpointModule — runtime insights
- [ ] CustomChartEndpointModule — charts personalizados
- [ ] TelemetryEndpointModule — telemetria
- [ ] CostIntelligenceEndpointModule — custos
- [ ] AutomationEndpointModule — automação
- [ ] PredictiveIntelligenceEndpointModule — previsões

#### 4.5 Frontend
- [ ] Incident management — lista, detalhe, criação, timeline
- [ ] Mitigation workflow — steps, validação
- [ ] Runbooks — lista, detalhe, execução
- [ ] Reliability — SLO/SLA dashboards, error budgets
- [ ] Runtime Intelligence — anomalias, drift
- [ ] Custom Charts — criação e visualização
- [ ] Cost Intelligence — dashboards de custo
- [ ] Automation — workflows
- [ ] Telemetry views
- [ ] Predictive Intelligence — previsões
- [ ] i18n completo
- [ ] Apache ECharts para gráficos

#### 4.6 Database
- [ ] Prefixo `ops_` em todas as tabelas
- [ ] RLS em todas as tabelas (incluindo innovative ideas: ops_reliability_healing_recommendations, ops_operational_playbooks, ops_playbook_executions, ops_resilience_reports)
- [ ] Migrations por cada DbContext

#### 4.7 Testes (~75 ficheiros, ~851 testes)
- [ ] Executar: `dotnet test tests/modules/operationalintelligence/`
- [ ] Verificar 0 failures
- [ ] Coverage por subdomain
- [ ] Gap analysis

---

## Módulo 5 — AIKnowledge (AI)

**Pilar**: AI-assisted Operations & Engineering + AI Governance  
**Personas impactadas**: Engineer, Tech Lead, Platform Admin  
**Prioridade**: 🟠 Alta

### Backend

#### 5.1 Domain (3 subdomains)
- [ ] **Governance**: AccessPolicy, Model, Budget, Conversation, Message, Agent, Guardrail, Evaluation, Feedback, PromptTemplate, ToolDefinition, OnboardingSession, IdeQuerySession, etc.
- [ ] **ExternalAI**: Provider, Policy, Consultation, KnowledgeCapture
- [ ] **Orchestration**: Context, Conversation, TestArtifact, KnowledgeCaptureEntry

#### 5.2 Application (~106 features)
- [ ] **Governance (68)**: CreateAgent, ExecuteAgent, RegisterModel, CreatePolicy, UpdateBudget, SendAssistantMessage, SubmitAiFeedback, CreateGuardrail, SubmitEvaluation, CreatePromptTemplate, CreateToolDefinition, RegisterIdeClient, SubmitIdeQuery, StartOnboardingSession, ListKnowledgeSources, ListAuditEntries, etc.
- [ ] **ExternalAI (8)**: QueryExternalAI, CaptureResponse, ApproveKnowledgeCapture, ConfigureExternalAIPolicy, etc.
- [ ] **Orchestration (18)**: AskCatalogQuestion, ClassifyChangeWithAI, SuggestSemanticVersion, AnalyzeNonProdEnvironment, CompareEnvironments, GenerateTestScenarios, EvaluateArchitectureFitness, ReviewContractDraft, etc.
- [ ] **Runtime (12)**: ActivateModel, ExecuteAiChat, GetTokenUsage, SearchData, SearchDocuments, SearchTelemetry, etc.
- [ ] Verificar governança: políticas, budgets, auditoria em cada feature

#### 5.3 Infrastructure (3 DbContexts)
- [ ] AiGovernanceDbContext — ~41 DbSets
- [ ] ExternalAiDbContext — 4 DbSets
- [ ] AiOrchestrationDbContext — 4 DbSets
- [ ] Verificar migrations e configurations

#### 5.4 API (~5 endpoint modules)
- [ ] AiGovernanceEndpointModule — governance CRUD
- [ ] AiIdeEndpointModule — IDE integration
- [ ] ExternalAiEndpointModule — external AI
- [ ] AiOrchestrationEndpointModule — orchestration
- [ ] AiRuntimeEndpointModule — runtime execution

#### 5.5 Frontend
- [ ] AI Assistant — chat interface
- [ ] AI Agents — lista, execução, artefactos
- [ ] Model Registry — modelos, providers
- [ ] AI Policies — governance, budgets
- [ ] AI Knowledge Sources — fontes de conhecimento
- [ ] AI Audit — usage, logs
- [ ] IDE Extensions — gestão de clientes IDE
- [ ] Guardrails — proteções
- [ ] Prompt Templates — templates
- [ ] Tool Definitions — tools
- [ ] External AI — providers, policies
- [ ] Onboarding — sessões de onboarding
- [ ] i18n completo
- [ ] Verificar que nenhuma feature é "chat genérico" sem governança

#### 5.6 Database
- [ ] Prefixo `ai_` em todas as tabelas
- [ ] RLS em todas as tabelas (incluindo ai_onboarding_sessions, ai_ide_query_sessions)
- [ ] Migrations por cada DbContext

#### 5.7 Testes (~87 ficheiros, ~982 testes)
- [ ] Executar: `dotnet test tests/modules/aiknowledge/`
- [ ] Verificar 0 failures
- [ ] Coverage: Governance (41), ExternalAI (9), Orchestration (16), Runtime (20)
- [ ] Gap analysis

---

## Módulo 6 — Governance (Governance)

**Pilar**: Service Governance + FinOps  
**Personas impactadas**: Tech Lead, Architect, Executive, Auditor  
**Prioridade**: 🟠 Alta

### Backend

#### 6.1 Domain
- [ ] Entidades: Team, GovernanceDomain, GovernancePack, GovernanceWaiver, DelegatedAdministration, EvidencePackage, ComplianceGap, PolicyAsCodeDefinition, SecurityScanResult, SecurityFinding, CustomDashboard, TechnicalDebtItem, ServiceMaturityAssessment, TeamHealthSnapshot, ChangeCostImpact, ExecutiveBriefing, CostAttribution, LicenseComplianceReport
- [ ] SecurityGate subdomain

#### 6.2 Application (~40+ features)
- [ ] Teams management
- [ ] Domains management
- [ ] Governance Packs & Versions
- [ ] Waivers
- [ ] Delegated Administration
- [ ] Evidence Packages
- [ ] Compliance Gaps
- [ ] Policy as Code
- [ ] Security Scanning
- [ ] Custom Dashboards
- [ ] Technical Debt
- [ ] Service Maturity Assessment
- [ ] Team Health Snapshots
- [ ] Change Cost Impact
- [ ] Executive Briefings
- [ ] Cost Attribution
- [ ] License Compliance
- [ ] FinOps features
- [ ] Enterprise Controls

#### 6.3 Infrastructure (1 DbContext)
- [ ] GovernanceDbContext — 22 DbSets
- [ ] Verificar migrations e configurations

#### 6.4 API (~21 endpoint modules)
- [ ] PlatformStatusEndpointModule
- [ ] GovernanceRiskEndpointModule
- [ ] GovernanceGatesEndpointModule
- [ ] GovernanceWaiversEndpointModule
- [ ] PolicyAsCodeEndpointModule
- [ ] GovernanceReportsEndpointModule
- [ ] GovernanceComplianceEndpointModule
- [ ] DelegatedAdminEndpointModule
- [ ] DashboardsAndDebtEndpointModule
- [ ] SecurityGateEndpointModule
- [ ] ComplianceChecksEndpointModule
- [ ] PolicyCatalogEndpointModule
- [ ] ScopedContextEndpointModule
- [ ] DomainEndpointModule
- [ ] GovernancePacksEndpointModule
- [ ] OnboardingEndpointModule
- [ ] GovernanceFinOpsEndpointModule
- [ ] EnterpriseControlsEndpointModule
- [ ] EvidencePackagesEndpointModule
- [ ] TeamEndpointModule
- [ ] Verificar que cada endpoint tem autorização adequada

#### 6.5 Frontend
- [ ] Teams — gestão de equipas
- [ ] Domains — domínios de governança
- [ ] Governance Packs — regras, versões
- [ ] Compliance — gaps, checks
- [ ] Security — scans, findings
- [ ] FinOps — custo, atribuição, briefings executivos
- [ ] Risk Center — visão de risco
- [ ] Executive Views — dashboards executivos
- [ ] Reports — relatórios
- [ ] Service Maturity — avaliação de maturidade
- [ ] Technical Debt — itens de dívida técnica
- [ ] i18n completo

#### 6.6 Database
- [ ] Prefixo `gov_` em todas as tabelas
- [ ] RLS em todas as tabelas (incluindo gov_team_health_snapshots, gov_change_cost_impacts, gov_executive_briefings, gov_cost_attributions, gov_license_compliance_reports)
- [ ] Migrations

#### 6.7 Testes (~36 ficheiros, ~413 testes)
- [ ] Executar: `dotnet test tests/modules/governance/`
- [ ] Verificar 0 failures
- [ ] Gap analysis

---

## Módulo 7 — Knowledge (Knowledge Hub)

**Pilar**: Source of Truth & Operational Knowledge  
**Personas impactadas**: Engineer, Tech Lead  
**Prioridade**: 🟡 Média

### Backend

#### 7.1 Domain
- [ ] Entidades: KnowledgeDocument, OperationalNote, KnowledgeRelation, KnowledgeGraphSnapshot
- [ ] Enums completos

#### 7.2 Application (~18 features)
- [ ] CreateDocument, UpdateDocument, ListDocuments, GetDocument
- [ ] CreateOperationalNote, ListOperationalNotes
- [ ] CreateKnowledgeRelation, ListRelations
- [ ] CreateKnowledgeGraphSnapshot
- [ ] Search features
- [ ] Validar completude de CRUD para cada entidade

#### 7.3 Infrastructure (1 DbContext)
- [ ] KnowledgeDbContext — 4 DbSets
- [ ] Migrations e configurations
- [ ] RunbookKnowledgeLinkingService

#### 7.4 API (1 endpoint module)
- [ ] KnowledgeEndpointModule — verificar se cobre todas as features

#### 7.5 Frontend
- [ ] Knowledge Hub — documentos, notas operacionais
- [ ] Knowledge Relations — relações entre entidades
- [ ] Knowledge Graph — visualização
- [ ] Search — pesquisa de conhecimento
- [ ] i18n completo

#### 7.6 Database
- [ ] Prefixo de tabelas correto
- [ ] RLS aplicado
- [ ] Migrations

#### 7.7 Testes (~16 ficheiros)
- [ ] Executar: `dotnet test tests/modules/knowledge/`
- [ ] Verificar 0 failures
- [ ] Gap analysis: 18 features vs 16 testes

---

## Módulo 8 — Notifications

**Pilar**: Operational Consistency  
**Personas impactadas**: Todas  
**Prioridade**: 🟡 Média

### Backend

#### 8.1 Domain
- [ ] Entidades: Notification, NotificationDelivery, NotificationPreference, NotificationTemplate, DeliveryChannelConfiguration, SmtpConfiguration
- [ ] Events, StronglyTypedIds

#### 8.2 Application (~17 features)
- [ ] Notification center operations
- [ ] Delivery configuration
- [ ] Preferences management
- [ ] Template management
- [ ] SMTP configuration
- [ ] Notification routing engine

#### 8.3 Infrastructure
- [ ] NotificationsDbContext — 6 DbSets
- [ ] Engine, EventHandlers, ExternalDelivery, Governance, Intelligence, Preferences, Routing subsystems
- [ ] Migrations e configurations

#### 8.4 API (2 endpoints)
- [ ] NotificationCenterEndpointModule
- [ ] NotificationConfigurationEndpointModule

#### 8.5 Frontend
- [ ] Notification center — lista, mark as read
- [ ] Notification preferences — configuração por canal
- [ ] Notification analytics
- [ ] i18n completo

#### 8.6 Database
- [ ] RLS em todas as tabelas
- [ ] Migrations

#### 8.7 Testes (~50 ficheiros)
- [ ] Executar: `dotnet test tests/modules/notifications/`
- [ ] Verificar 0 failures

---

## Módulo 9 — Integrations

**Pilar**: Source of Truth (ingestão externa)  
**Personas impactadas**: Platform Admin, Engineer  
**Prioridade**: 🟡 Média

### Backend

#### 9.1 Domain
- [ ] Entidades: IntegrationConnector, IngestionSource, IngestionExecution, WebhookSubscription
- [ ] LegacyTelemetry subdomain

#### 9.2 Application (~13 features + LegacyTelemetry)
- [ ] CRUD IntegrationConnector
- [ ] CRUD IngestionSource
- [ ] Run Ingestion
- [ ] CRUD WebhookSubscription
- [ ] LegacyTelemetry: parsing, ingestão

#### 9.3 Infrastructure
- [ ] IntegrationsDbContext — 4 DbSets
- [ ] LegacyTelemetry subsystem
- [ ] Migrations e configurations

#### 9.4 API (2 endpoints)
- [ ] IntegrationHubEndpointModule
- [ ] LegacyTelemetryEndpointModule

#### 9.5 Frontend
- [ ] Integrations hub — conectores, fontes
- [ ] Webhook management
- [ ] Legacy telemetry
- [ ] i18n completo

#### 9.6 Database
- [ ] RLS em todas as tabelas
- [ ] Migrations

#### 9.7 Testes (~16 ficheiros)
- [ ] Executar: `dotnet test tests/modules/integrations/`
- [ ] Verificar 0 failures

---

## Módulo 10 — AuditCompliance

**Pilar**: Source of Truth (auditoria e compliance)  
**Personas impactadas**: Auditor, Platform Admin  
**Prioridade**: 🟡 Média

### Backend

#### 10.1 Domain
- [ ] Entidades: AuditEvent, AuditChainLink, RetentionPolicy, CompliancePolicy, AuditCampaign, ComplianceResult
- [ ] Audit domain ports e events

#### 10.2 Application (~23 features)
- [ ] Audit events management
- [ ] Chain link integrity
- [ ] Retention policies
- [ ] Compliance policies
- [ ] Audit campaigns
- [ ] Compliance checks

#### 10.3 Infrastructure
- [ ] AuditDbContext — 6 DbSets
- [ ] Retention subsystem
- [ ] Migrations e configurations

#### 10.4 API (1 endpoint)
- [ ] AuditEndpointModule — verificar se cobre todas as features

#### 10.5 Frontend
- [ ] Audit trail — eventos, pesquisa, filtros
- [ ] Compliance — políticas, resultados
- [ ] Campaigns — gestão de campanhas
- [ ] Retention — políticas de retenção
- [ ] i18n completo

#### 10.6 Database
- [ ] Prefixo `aud_` em todas as tabelas
- [ ] RLS
- [ ] Migrations

#### 10.7 Testes (~15 ficheiros)
- [ ] Executar: `dotnet test tests/modules/auditcompliance/`
- [ ] Verificar 0 failures
- [ ] Gap analysis: 23 features vs 15 testes

---

## Módulo 11 — Configuration

**Pilar**: Operational Consistency + Foundation  
**Personas impactadas**: Platform Admin, Engineer  
**Prioridade**: 🟡 Média

### Backend

#### 11.1 Domain
- [ ] Entidades: ConfigurationDefinition, ConfigurationEntry, ConfigurationAuditEntry, ConfigurationModule, FeatureFlagDefinition, FeatureFlagEntry, UserSavedView, UserBookmark, UserWatch, UserAlertRule, EntityTag, ServiceCustomField, TaxonomyCategory, TaxonomyValue, AutomationRule, ChangeChecklist, ContractTemplate, ScheduledReport, SavedPrompt, WebhookTemplate

#### 11.2 Application (~57 features)
- [ ] Configuration CRUD
- [ ] Feature flags CRUD
- [ ] User preferences (saved views, bookmarks, watches)
- [ ] Alert rules
- [ ] Entity tags
- [ ] Custom fields
- [ ] Taxonomies
- [ ] Automation rules
- [ ] Change checklists
- [ ] Contract templates
- [ ] Scheduled reports
- [ ] Saved prompts
- [ ] Webhook templates
- [ ] Export

#### 11.3 Infrastructure
- [ ] ConfigurationDbContext — 20 DbSets
- [ ] Seed data
- [ ] Migrations e configurations

#### 11.4 API (17 endpoint modules)
- [ ] Verificar que cada area funcional tem endpoint dedicado
- [ ] ConfigurationEndpointModule, AlertRulesEndpointModule, AutomationRulesEndpointModule, etc.

#### 11.5 Frontend
- [ ] Configuration admin — definições, módulos
- [ ] Feature flags — gestão
- [ ] User preferences — views, bookmarks, watches
- [ ] Alert rules — criação, gestão
- [ ] Tags — gestão de tags
- [ ] Taxonomies — categorias, valores
- [ ] i18n completo

#### 11.6 Database
- [ ] Prefixo `cfg_` em todas as tabelas
- [ ] RLS
- [ ] Migrations

#### 11.7 Testes (~30 ficheiros)
- [ ] Executar: `dotnet test tests/modules/configuration/`
- [ ] Verificar 0 failures

---

## Módulo 12 — ProductAnalytics

**Pilar**: Operational Intelligence  
**Personas impactadas**: Product, Executive  
**Prioridade**: 🟢 Normal

### Backend

#### 12.1 Domain
- [ ] Entidade: AnalyticsEvent

#### 12.2 Application (~9 features)
- [ ] Track analytics events
- [ ] Query analytics
- [ ] Aggregate analytics

#### 12.3 Infrastructure
- [ ] ProductAnalyticsDbContext — 1 DbSet
- [ ] Migrations e configurations

#### 12.4 API (1 endpoint)
- [ ] ProductAnalyticsEndpointModule

#### 12.5 Frontend
- [ ] Analytics tracker component
- [ ] Analytics dashboards (se existirem)

#### 12.6 Database
- [ ] RLS
- [ ] Migrations

#### 12.7 Testes (~4 ficheiros)
- [ ] Executar: `dotnet test tests/modules/productanalytics/`
- [ ] Verificar 0 failures
- [ ] Gap analysis: 9 features vs 4 testes

---

## Building Blocks (Cross-cutting)

### Validações

#### Core
- [ ] Base entities, value objects, domain events
- [ ] Strongly-typed ID infrastructure
- [ ] Result<T> pattern
- [ ] Guard clauses utilities
- [ ] Testes: `dotnet test tests/building-blocks/NexTraceOne.BuildingBlocks.Core.Tests/`

#### Application
- [ ] MediatR pipeline behaviors
- [ ] Validation behavior
- [ ] Logging behavior
- [ ] Transaction behavior
- [ ] Testes: `dotnet test tests/building-blocks/NexTraceOne.BuildingBlocks.Application.Tests/`

#### Infrastructure
- [ ] NexTraceDbContextBase (RLS, Outbox, Audit)
- [ ] Repository base classes
- [ ] Unit of Work
- [ ] Testes: `dotnet test tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/`

#### Observability
- [ ] Serilog configuration
- [ ] OpenTelemetry integration
- [ ] Health checks
- [ ] Testes: `dotnet test tests/building-blocks/NexTraceOne.BuildingBlocks.Observability.Tests/`

#### Security
- [ ] JWT authentication
- [ ] Authorization policies
- [ ] RLS integration
- [ ] Tenant resolution
- [ ] Encryption utilities
- [ ] Testes: `dotnet test tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/`

---

## Platform (ApiHost / Workers / Ingestion)

### ApiHost
- [ ] Todos os 12 módulos registados correctamente
- [ ] OpenAPI/Swagger (Scalar) funcional
- [ ] CORS configurado
- [ ] Middleware pipeline correcto (auth → tenant → RLS)
- [ ] Health check endpoint
- [ ] Seed data loading funcional

### BackgroundWorkers
- [ ] Quartz scheduler configurado
- [ ] Todos os event handlers registados
- [ ] Jobs programados funcionais
- [ ] Logging adequado

### Ingestion API
- [ ] Endpoints de ingestão para ChangeGovernance, Catalog, OperationalIntelligence, Governance, Integrations
- [ ] Autorização adequada
- [ ] Rate limiting

---

## Frontend Global

### Design System
- [ ] Tokens e foundations definidos
- [ ] 54+ componentes globais consistentes
- [ ] Shell components funcionais (AppShell, Sidebar, Topbar)
- [ ] ThemeToggle (dark/light mode)
- [ ] Responsividade em todos os componentes

### Routing
- [ ] 8 ficheiros de rota cobrem todos os módulos
- [ ] Lazy loading funcional
- [ ] Deep-link preservation no login

### i18n
- [ ] 4 idiomas: en, es, pt-BR, pt-PT
- [ ] Nenhum texto hardcoded (verificar exaustivamente)
- [ ] Fallback para inglês funcional
- [ ] XSS protection via escapeValue

### API Client
- [ ] Axios client com interceptors correctos
- [ ] Token refresh logic funcional
- [ ] Tenant ID injection
- [ ] CSRF handling
- [ ] Query keys centralizadas (TanStack Query)

### Contextos
- [ ] AuthContext — login, logout, estado de autenticação
- [ ] EnvironmentContext — seleção de ambiente
- [ ] PersonaContext — adaptação por papel
- [ ] ThemeContext — tema visual

### E2E Tests (16 specs)
- [ ] Executar: Playwright E2E
- [ ] app.spec.ts — bootstrap
- [ ] login-form-flows.spec.ts — autenticação
- [ ] contracts.spec.ts — contratos CRUD
- [ ] contract-wizard-flows.spec.ts — wizard
- [ ] contract-approval-flows.spec.ts — aprovação
- [ ] contract-versioning-flows.spec.ts — versionamento
- [ ] contract-deprecation-flows.spec.ts — deprecação
- [ ] contract-health-dashboard.spec.ts — health
- [ ] change-confidence.spec.ts — change confidence
- [ ] governance-business-flows.spec.ts — governance
- [ ] governance-finops.spec.ts — FinOps
- [ ] incident-business-flows.spec.ts — incidentes
- [ ] incidents.spec.ts — CRUD incidentes
- [ ] service-catalog.spec.ts — catálogo
- [ ] modules.spec.ts — módulos
- [ ] refresh-token.spec.ts — refresh token

### Segurança Frontend
- [ ] Sem `dangerouslySetInnerHTML` com conteúdo não sanitizado
- [ ] Token storage seguro (sessionStorage)
- [ ] URLs e redirects validados
- [ ] sanitize.ts usado onde necessário
- [ ] Sem credenciais expostas

---

## Infraestrutura (DB / RLS / Seed / Docker)

### PostgreSQL
- [ ] init-databases.sql — criação correcta do DB `nextraceone`
- [ ] Prefixos de tabela por módulo (iam_, cat_, chg_, ops_, ai_, gov_, cfg_, aud_, etc.)

### RLS (Row-Level Security)
- [ ] apply-rls.sql cobre ~100 tabelas
- [ ] Função `get_current_tenant_id()` funcional
- [ ] Todas as tabelas de todos os módulos cobertas
- [ ] Innovative Ideas tables incluídas (Wave A-G)
- [ ] Verificar que nenhuma tabela nova ficou sem RLS

### Seed Data
- [ ] seed_production.sql — idempotente, PlatformAdmin role
- [ ] seed_development.sql — 7 utilizadores de teste, 6 roles adicionais
- [ ] Ordem de execução: production → development

### Docker
- [ ] docker-compose.yml — funcional para desenvolvimento
- [ ] docker-compose.override.yml — overrides de desenvolvimento
- [ ] docker-compose.production.yml — configuração de produção
- [ ] Dockerfile.apihost — build funcional
- [ ] Dockerfile.frontend — build funcional
- [ ] Dockerfile.ingestion — build funcional
- [ ] Dockerfile.workers — build funcional

---

## Validação Cross-Module

### Integridade entre módulos
- [ ] IdentityAccess → todos os módulos (autenticação/autorização)
- [ ] Catalog → ChangeGovernance (contratos e mudanças)
- [ ] Catalog → OperationalIntelligence (serviços e incidentes)
- [ ] ChangeGovernance → OperationalIntelligence (mudanças e incidentes)
- [ ] AIKnowledge → Catalog (AI assistida para contratos)
- [ ] AIKnowledge → ChangeGovernance (AI para change analysis)
- [ ] Knowledge → Catalog (documentação de serviços)
- [ ] Knowledge → OperationalIntelligence (runbooks)
- [ ] Governance → Catalog (compliance de serviços)
- [ ] Governance → ChangeGovernance (governance de mudanças)
- [ ] AuditCompliance → todos os módulos (trilha de auditoria)
- [ ] Notifications → todos os módulos (notificações de eventos)
- [ ] Configuration → todos os módulos (feature flags, configurações)

### Outbox Pattern
- [ ] Cada DbContext tem Outbox table
- [ ] BackgroundWorkers processa mensagens do Outbox
- [ ] Eventos cross-module são entregues

### Tenant Isolation
- [ ] Todos os endpoints filtram por tenant
- [ ] RLS activo em todas as tabelas
- [ ] Nenhum dado vaza entre tenants

### Persona Awareness
- [ ] Dashboard adapta-se ao papel
- [ ] Menu ordem reflete persona
- [ ] Relatórios adequados por persona
- [ ] Métricas priorizadas por papel

---

## Documentação Global

### Verificar atualização e completude de:
- [ ] PRODUCT-VISION.md — alinhado com estado actual
- [ ] ARCHITECTURE-OVERVIEW.md — reflecte arquitectura real
- [ ] MODULES-AND-PAGES.md — lista todos os módulos e páginas
- [ ] IMPLEMENTATION-STATUS.md — estado real de implementação
- [ ] FRONTEND-ARCHITECTURE.md — reflecte frontend actual
- [ ] DATA-ARCHITECTURE.md — reflecte modelo de dados actual
- [ ] SECURITY-ARCHITECTURE.md — reflecte security actual
- [ ] AI-ARCHITECTURE.md — reflecte AI actual
- [ ] OBSERVABILITY-STRATEGY.md — reflecte observabilidade
- [ ] DESIGN-SYSTEM.md — reflecte design system actual
- [ ] I18N-STRATEGY.md — reflecte i18n actual
- [ ] PERSONA-MATRIX.md — reflecte personas
- [ ] DOMAIN-BOUNDARIES.md — reflecte bounded contexts
- [ ] DEVELOPMENT-PLAN-INNOVATIVE-IDEAS.md — status actual das 29 ideias
- [ ] DOCUMENTATION-INDEX.md — índice completo e actualizado
- [ ] LOCAL-SETUP.md — instruções de setup funcionais
- [ ] ENVIRONMENT-VARIABLES.md — todas as variáveis documentadas
- [ ] ADRs (Architecture Decision Records) — actualizados

---

## Priorização e Ordem de Execução

### Fase 1 — Foundation & Core (Semana 1)
1. **Building Blocks** — validar base transversal
2. **IdentityAccess** — validar autenticação, autorização, tenants
3. **Infrastructure** — DB, RLS, Seed, Docker

### Fase 2 — Core do Produto (Semana 2-3)
4. **Catalog** — validar serviços e contratos (módulo maior)
5. **ChangeGovernance** — validar mudanças, promoções, workflows
6. **OperationalIntelligence** — validar incidentes, reliability, runtime

### Fase 3 — Inteligência & Governança (Semana 4)
7. **AIKnowledge** — validar AI governance, agents, orchestration
8. **Governance** — validar governance, compliance, FinOps
9. **Knowledge** — validar knowledge hub

### Fase 4 — Suporte & Cross-cutting (Semana 5)
10. **Notifications** — validar notificações
11. **Configuration** — validar configurações, feature flags
12. **AuditCompliance** — validar auditoria
13. **Integrations** — validar integrações
14. **ProductAnalytics** — validar analytics

### Fase 5 — Frontend & Cross-Module (Semana 6)
15. **Frontend Global** — design system, routing, i18n, segurança
16. **E2E Tests** — executar e validar todos os specs
17. **Validação Cross-Module** — integridade entre módulos
18. **Platform** — ApiHost, Workers, Ingestion

### Fase 6 — Documentação & Wrap-up (Semana 7)
19. **Documentação** — verificar todos os .md files
20. **Relatório Final** — consolidar bugs, gaps e plano de correção

---

## Modelo de Relatório por Módulo

Ao concluir a validação de cada módulo, gerar relatório com:

```markdown
## Relatório de Validação — [Nome do Módulo]

### Estado Geral: 🟢 OK | 🟡 Parcial | 🔴 Problemas Críticos

### Bugs Encontrados
| # | Severidade | Descrição | Ficheiro | Linha |
|---|-----------|-----------|---------|-------|

### Gaps Identificados
| # | Tipo | Descrição | Impacto |
|---|------|-----------|---------|

### Implementação Incompleta
| # | Feature | Estado | % Completa | Próximo Passo |
|---|---------|--------|-----------|---------------|

### Testes
| Métrica | Valor |
|---------|-------|
| Total testes | |
| Testes passam | |
| Testes falham | |
| Features sem teste | |

### i18n
| Idioma | Keys usadas | Keys em falta |
|--------|-------------|---------------|

### Recomendações
1. ...
2. ...
```

---

> **Nota**: Este plano deve ser executado de forma iterativa. Cada módulo validado pode revelar gaps que impactam outros módulos. Manter um registo centralizado de todos os findings para priorização final.
