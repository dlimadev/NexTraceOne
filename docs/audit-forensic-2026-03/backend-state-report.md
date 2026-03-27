# Relatório de Estado do Backend — NexTraceOne
**Auditoria Forense | Março 2026**

---

## 1. Visão Geral

| Métrica | Valor |
|---|---|
| Total de projetos .csproj | 59 |
| Total de arquivos .cs | ~1.866 |
| Módulos de domínio | 12 |
| Projetos por módulo (camadas) | 5 (API, Application, Domain, Infrastructure, Contracts) |
| Building Blocks | 5 projetos compartilhados |
| Platform hosts | 3 (ApiHost, Ingestion.Api, BackgroundWorkers) |
| DbContexts | 24 |
| Migrações geradas | 46 |
| DbContexts com migrações | ~15 confirmados |
| DbContexts sem migrações | ~9 (Knowledge, ProductAnalytics, Integrations, e parcialmente OperationalIntelligence) |

---

## 2. Building Blocks — Estado

### `NexTraceOne.BuildingBlocks.Core`
**Status: READY**

- Primitivos de domínio: `AggregateRoot`, `Entity`, `ValueObject`, `DomainEvent`
- `Result<T>` pattern implementado
- Guards (guard clauses)
- Strongly typed IDs
- Enums e atributos de domínio

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Core/`

### `NexTraceOne.BuildingBlocks.Application`
**Status: READY**

- Abstrações CQRS: `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`
- Behaviors MediatR: validação (FluentValidation), logging, correlação, paginação
- Context: `ICurrentUser`, `ICurrentTenant` — injeção presente em todos os handlers
- i18n/localização: `ILocalizationService`
- Integração: contratos de eventos de integração

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/`

### `NexTraceOne.BuildingBlocks.Infrastructure`
**Status: READY**

- `NexTraceDbContextBase` — base para todos os DbContexts com tenant isolation
- `TenantRlsInterceptor` — PostgreSQL Row-Level Security por SET config
- `AuditInterceptor` — campos CreatedAt/UpdatedAt automáticos
- Outbox pattern — parcialmente processado (apenas IdentityDbContext ativo)
- Health checks: `DbContextConnectivityHealthCheck`

**Gap crítico:** O outbox só processa eventos do IdentityDbContext. Os outros 23 DbContexts têm tabelas de outbox mas sem processamento ativo.

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs`

### `NexTraceOne.BuildingBlocks.Security`
**Status: READY**

- JWT com validação obrigatória no startup (falha se chave ausente)
- API Key authentication dual-scheme
- CORS com validação de wildcard e configuração obrigatória em produção
- Rate limiting em 6 policies por risco de endpoint
- TenantResolutionMiddleware após UseAuthentication (ordem correta)
- AES-256-GCM encryption com chave obrigatória por env var
- AssemblyIntegrityChecker no startup
- Permission-based authorization (dynamic policy provider)

**Evidência:** `src/building-blocks/NexTraceOne.BuildingBlocks.Security/`

### `NexTraceOne.BuildingBlocks.Observability`
**Status: PARTIAL**

- OpenTelemetry configurado para `localhost:4317`
- Serilog com structured logging
- Health checks
- Métricas e alertas

**Gap:** Configuração aponta para localhost em produção — requer configuração por ambiente.

---

## 3. Módulos de Domínio — Análise Individual

### Catalog — ALTA maturidade ✅
**Status: READY**

**DbContexts:** ContractsDbContext, CatalogGraphDbContext, DeveloperPortalDbContext (3 DbContexts, 4 migrações)

**Features implementadas (84 total, 91.7% real):**
- Graph: RegisterServiceAsset, ImportFromBackstage, ListServices, GetAssetGraph, CreateGraphSnapshot (27 features, 100% real)
- Contracts: CreateContractVersion, CreateDraft, PublishDraft, SignContractVersion, GenerateScorecard, EvaluateContractRules, ComputeSemanticDiff, EvaluateCompatibility (35 features, 100% real)
- Portal: RecordAnalyticsEvent, CreateSubscription, ExecutePlayground, GenerateCode, GlobalSearch (22 features, 68% real)

**Stubs intencionais (7):** SearchCatalog, RenderOpenApiContract, GetApiHealth, GetMyApis, GetAssetTimeline, GetApisIConsume, GetApiDetail — aguardam integração cross-module

**Evidência:** `src/modules/catalog/`

---

### Change Governance — ALTA maturidade ✅
**Status: READY**

**DbContexts:** ChangeIntelligenceDbContext, WorkflowDbContext, PromotionDbContext, RulesetGovernanceDbContext (4 DbContexts, 4 migrações)

**Features implementadas (50+, 100% real):**
- ChangeIntelligence: releases, blast radius, change scores, freeze windows, rollback assessments
- Workflow: templates, instâncias, stages, approval decisions, evidence packs, SLA policies
- Promotion: environments, promotion requests, gates, gate evaluations
- RulesetGovernance: rulesets, bindings, lint results (Spectral)

**Endpoints:** FreezeEndpoints, TraceCorrelationEndpoints, ChangeConfidenceEndpoints, AnalysisEndpoints, ReleaseQueryEndpoints, DeploymentEndpoints, IntelligenceEndpoints, ApprovalEndpoints, EvidencePackEndpoints, StatusEndpoints, TemplateEndpoints

**Evidência:** `src/modules/changegovernance/`

---

### Identity Access — ALTA maturidade ✅
**Status: READY**

**DbContexts:** IdentityDbContext (1 DbContext, 1 migração)

**Features implementadas (35, 100% real):**
- Auth JWT, RBAC, sessões, multi-tenancy com RLS
- JIT access, break glass, access reviews, delegações
- Environments, tenants, users, roles, permissions

**Endpoints:** AuthEndpoints, UserEndpoints, TenantEndpoints, RolePermissionEndpoints, JitAccessEndpoints, AccessReviewEndpoints, BreakGlassEndpoints, DelegationEndpoints, EnvironmentEndpoints, SecurityEventsEndpoints, CookieSessionEndpoints, RuntimeContextEndpoints

**Evidência:** `src/modules/identityaccess/`

---

### Audit Compliance — ALTA maturidade ✅
**Status: READY**

**DbContexts:** AuditDbContext (1 DbContext, 2 migrações — inclui `20260327103000_P7_4_AuditCorrelationId`)

**Features implementadas (7, 100% real):**
- RecordAuditEvent, GetAuditTrail, VerifyChainIntegrity, SearchAuditLog
- Hash chain SHA-256 para imutabilidade auditável
- AuditChainLink com cascade delete controlado

**Evidência:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure/Persistence/`

---

### Operational Intelligence — BAIXA maturidade ⚠️
**Status: PARTIAL/MOCK**

**DbContexts:** IncidentDbContext, AutomationDbContext, ReliabilityDbContext, RuntimeIntelligenceDbContext, CostIntelligenceDbContext (5 DbContexts)

**Migrações:** IncidentDbContext tem migração; AutomationDbContext tem migração; ReliabilityDbContext tem migração; RuntimeIntelligenceDbContext e CostIntelligenceDbContext têm ModelSnapshot mas sem confirmação de migração executável

**Features por área:**
- Incidents (17): **PARTIAL/SIM** — EfIncidentStore (678 linhas) com 5 DbSets, seed data SQL; correlação incident↔change é seed data estático
- Automation (10): **MOCK** — catálogo estático, workflows não persistidos
- Reliability (7): **MOCK** — 8 serviços hardcoded, sem integração cross-module
- Runtime Intelligence (8+): **PARTIAL** — RuntimeIntelligenceDbContext existe, repositórios EF Core presentes; IRuntimeIntelligenceModule = PLAN
- Cost Intelligence (8+): **PARTIAL** — CostIntelligenceDbContext existe; ICostIntelligenceModule = PLAN

**Gap crítico:** Correlação dinâmica incident↔change é ZERO. `CreateMitigationWorkflow` existe mas não persiste. `GetMitigationHistory` retorna dados fixos.

**Evidência:** `src/modules/operationalintelligence/`, `docs/REBASELINE.md` §Operational Intelligence, `docs/CORE-FLOW-GAPS.md` §Fluxo 3

---

### AI Knowledge — MÉDIA maturidade ⚠️
**Status: PARTIAL**

**DbContexts:** AiGovernanceDbContext (com migrações), AiOrchestrationDbContext (com ModelSnapshot), ExternalAiDbContext (com ModelSnapshot)

**Migrações confirmadas:** AiGovernanceDbContext — outras têm snapshot mas migrações não confirmadas

**Features por área:**
- AI Governance (28): **PARTIAL** — funcional com repositórios EF Core (modelos, políticas, budgets, model registry)
- ExternalAI (8): **STUB** — TODO em todos os handlers; `IExternalAiModule = PLAN` (empty interface)
- AI Orchestration: **PARTIAL** — DbContext existe; `IAiOrchestrationModule = PLAN` (empty interface)

**AI Chat (local):**
- Ollama integrado (`localhost:11434`, `qwen3.5:9b`)
- OpenAI configurado mas desabilitado por padrão
- SendAssistantMessage retorna respostas hardcoded — **sem LLM real no fluxo end-to-end**

**Evidência:** `src/modules/aiknowledge/`, `docs/CORE-FLOW-GAPS.md` §Fluxo 4, `docs/IMPLEMENTATION-STATUS.md` §AI

---

### Governance — BAIXA maturidade ⚠️
**Status: MOCK (intencional por design)**

**DbContexts:** GovernanceDbContext existe mas sem persistência própria por design

**Features:** 74 handlers retornam `IsSimulated: true` e `DataSource = "demo"`:
- Teams, Domains, Governance Packs, Evidence, Policies, FinOps, Reports, Compliance, Executive views — todos mock
- Única exceção: ICatalogGraphModule é chamado para ServiceCount real em Teams/Domains

**Justificativa do design:** "Fase atual: sem persistência própria — agrega dados de outros módulos"

**Risco:** Toda a camada de Governance como entregável de produto é vazia. Qualquer demo desta área é falsa.

**Evidência:** `src/modules/governance/`, `docs/IMPLEMENTATION-STATUS.md` §Governance

---

### Notifications — PARTIAL
**Status: PARTIAL**

**DbContexts:** NotificationsDbContext (com 2+ migrações — `20260327082159`, `20260327092812`)

**Features:** Delivery channels, preferences, templates — existência confirmada; cobertura funcional end-to-end não auditada

**Evidência:** `src/modules/notifications/`

---

### Configuration — PARTIAL
**Status: PARTIAL**

**DbContexts:** ConfigurationDbContext (com migração)

**Features:** Feature flags (database-driven com override por tenant), ConfigurationDefinitionSeeder, settings por tenant/ambiente

**Evidência:** `src/modules/configuration/`, `docs/IMPLEMENTATION-STATUS.md` §Foundation

---

### Integrations — INCOMPLETE
**Status: INCOMPLETE**

**DbContexts:** IntegrationsDbContext (sem migração confirmada)

**Features:** Conectores como stubs; sem processamento real de dados externos. 5 endpoints de ingestão com `processingStatus: "metadata_recorded"` — payload não processado.

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Ingestion

---

### Knowledge — INCOMPLETE
**Status: INCOMPLETE**

**DbContexts:** KnowledgeDbContext (sem migrações geradas)

**Features:** Operational notes e changelog existem via eventos de domínio; Knowledge Hub sem migração deployável

**Evidência:** `src/modules/knowledge/`

---

### Product Analytics — MOCK
**Status: MOCK**

**DbContexts:** ProductAnalyticsDbContext (sem migrações confirmadas)

**Features:** 100% mock — depende de event tracking real que não existe

**Evidência:** `docs/IMPLEMENTATION-STATUS.md` §Product Analytics

---

## 4. Platform Hosts

### `NexTraceOne.ApiHost`
**Status: READY**

- Orquestra todos os módulos via DI
- Middleware pipeline em ordem correta (CORS → RateLimit → SecurityHeaders → CSRF → Auth → TenantResolution → EnvironmentResolution → Authorization)
- Seed data restrito a Development/Staging
- appsettings.json com defaults seguros
- IntegrityChecker no startup

### `NexTraceOne.Ingestion.Api`
**Status: PARTIAL**

- 5 endpoints de ingestão existem
- Dados ingeridos são registados como metadata, não processados

### `NexTraceOne.BackgroundWorkers`
**Status: PARTIAL**

- Projeto existe; cobertura de jobs Quartz não auditada em detalhe

---

## 5. Problemas de Código a Corrigir

| Problema | Localização | Severidade |
|---|---|---|
| Outbox não processado em 23 DbContexts | BuildingBlocks.Infrastructure | Alta |
| 8 cross-module interfaces sem implementação | IMPLEMENTATION-STATUS.md §Cross-Module | Alta |
| 516 warnings CS8632 nullable | CI/CD logs | Média |
| InMemoryIncidentStore (resíduo?) | Verificar se ainda existe após EfIncidentStore | Média |
| IRuntimeIntelligenceModule/ICostIntelligenceModule: PLAN | Operational Intelligence | Alta |
| IAiOrchestrationModule/IExternalAiModule: PLAN (empty) | AIKnowledge | Alta |
| SendAssistantMessage retorna hardcoded | AIKnowledge ExternalAI | Alta |

---

## 6. Qualidade de Código — Verificação de Padrões

| Padrão | Estado | Observação |
|---|---|---|
| `sealed` para classes finais | Parcial | Não auditado sistematicamente |
| `CancellationToken` em async | Parcial — presente em Building Blocks | Não verificado em todos os handlers |
| `Result<T>` para falhas | Sim — presente no Core | Usado em responses |
| Guard clauses | Sim — BuildingBlocks.Core/Guards/ | Presente |
| Strongly typed IDs | Sim — BuildingBlocks.Core/StronglyTypedIds/ | Presente |
| `DateTime.Now` proibido | Não verificado sistematicamente | Usar `DateTime.UtcNow` |
| Logging estruturado | Sim — Serilog com propriedades | Presente |
| DbContext isolado por módulo | Sim | Sem cross-context references detectadas |
| Bounded contexts preservados | Sim | 12 módulos isolados |
| Controllers com lógica leve | Sim — Minimal API Endpoints | Pattern correto |
| Domain separado de infra | Sim | 5 camadas por módulo |
