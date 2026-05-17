# NexTraceOne — Análise Técnica Completa

> **Data:** 2026-05-17 | **Versão Analisada:** v1.0.0 | **Runtime:** .NET 10.0 | **DB:** PostgreSQL

---

## 1. O Que É o NexTraceOne

**NexTraceOne é uma Plataforma de Inteligência de Mudança multi-tenant, desenhada para SaaS**, que centraliza o ciclo de vida completo de serviços e APIs numa organização de engenharia. Funciona como uma camada de governança unificada entre equipes, serviços, ambientes e mudanças — com IA nativa, rastreabilidade end-to-end e conformidade regulatória integrada.

**Posicionamento de mercado:** concorre com Backstage (Spotify), Port, Cortex e OpsLevel, mas com diferencial em governança de mudanças com IA, blast radius analysis, e compliance multi-framework nativo.

### O que resolve na prática

| Problema | Solução NexTraceOne |
|---|---|
| "Quem é dono deste serviço?" | Catálogo de serviços com ownership, tiers e grafo de dependências |
| "Qual o impacto desta mudança?" | Blast Radius Analysis + Change Score (0–100) automático |
| "Esta release pode ir para produção?" | Promotion Gates + Workflow de aprovação com evidência |
| "Estamos a gastar quanto neste serviço?" | FinOps integrado com ingestion de billing cloud |
| "O nosso SLO está verde?" | SLO/SLI tracking com error budget e burn rate |
| "Estamos conformes com GDPR?" | Compliance multi-framework com evidence packs |
| "A IA está a ser usada responsavelmente?" | Guardrails, budgets de tokens e routing com políticas |
| "Onde foi auditado este acesso?" | Trilha imutável com blockchain-style chain link integrity |

---

## 2. Arquitetura

### 2.1 Padrão: Archon Pattern (Monólito Modular)

```
┌─────────────────────────────────────────────────────────┐
│                    Clientes HTTP/GraphQL/CLI             │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│               NexTraceOne.ApiHost                        │
│  Assembly Integrity → Serilog → Preflight (10 checks)   │
│  Middleware Pipeline (15 steps) → Rate Limiting (7 pol) │
│  JWT + API Key Auth → Multi-Tenancy → RLS               │
└──────────────┬──────────────────────────┬───────────────┘
               │                          │
┌──────────────▼──────────┐  ┌────────────▼──────────────┐
│   12 Módulos DDD        │  │  BackgroundWorkers         │
│   (Clean Architecture)  │  │  (13 Jobs Quartz.NET)      │
└──────────────┬──────────┘  └────────────┬──────────────┘
               │                          │
┌──────────────▼──────────────────────────▼───────────────┐
│              Building Blocks (5 projetos)                │
│  Core │ Application │ Infrastructure │ Security │ Obs.  │
└──────────────────────────┬──────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────┐
│  PostgreSQL (27 DbContexts, RLS, Outbox, JSONB)         │
│  Redis (cache) │ Kafka (opcional) │ ClickHouse (opt.)   │
└─────────────────────────────────────────────────────────┘
```

### 2.2 Estrutura de Pastas

```
NexTraceOne/
├── src/
│   ├── building-blocks/
│   │   ├── NexTraceOne.BuildingBlocks.Core
│   │   ├── NexTraceOne.BuildingBlocks.Application
│   │   ├── NexTraceOne.BuildingBlocks.Infrastructure
│   │   ├── NexTraceOne.BuildingBlocks.Security
│   │   └── NexTraceOne.BuildingBlocks.Observability
│   ├── platform/
│   │   ├── NexTraceOne.ApiHost          ← Entrada HTTP principal
│   │   ├── NexTraceOne.BackgroundWorkers ← 13 jobs Quartz
│   │   └── NexTraceOne.Ingestion.Api    ← Ingestão de telemetria
│   └── modules/                          ← 12 módulos DDD
│       ├── catalog/
│       ├── changegovernance/
│       ├── operationalintelligence/
│       ├── governance/
│       ├── identityaccess/
│       ├── aiknowledge/
│       ├── auditcompliance/
│       ├── integrations/
│       ├── notifications/
│       ├── configuration/
│       ├── knowledge/
│       └── productanalytics/
├── tools/
│   ├── NexTrace.Sdk        ← SDK net6.0 para clientes
│   └── NexTraceOne.CLI     ← CLI `nex` com System.CommandLine
└── tests/
    ├── building-blocks/
    ├── modules/
    └── platform/
        ├── NexTraceOne.E2E.Tests
        ├── NexTraceOne.IntegrationTests
        └── NexTraceOne.Selenium.Tests
```

### 2.3 Camadas por Módulo (Clean Architecture)

```
src/modules/<nome>/
├── NexTraceOne.<Nome>.Domain/          ← Entidades, AggregateRoots, ValueObjects, Eventos
├── NexTraceOne.<Nome>.Application/     ← Features (CQRS), Validators, Abstrações (IRepositories)
├── NexTraceOne.<Nome>.Infrastructure/  ← DbContext, Repositórios EF Core, DI
├── NexTraceOne.<Nome>.API/             ← Minimal API Endpoints
└── NexTraceOne.<Nome>.Contracts/       ← DTOs públicos, IXxxModule interface
```

### 2.4 Padrão CQRS por Feature

```csharp
// Ficheiro único: Features/CreateRelease/CreateRelease.cs
public static class CreateRelease
{
    public sealed record Command(string ServiceName, string Version, string Environment)
        : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Version).NotEmpty().Matches(@"^\d+\.\d+\.\d+");
        }
    }

    public sealed record Response(Guid ReleaseId, string Status);

    internal sealed class Handler(IReleaseRepository repo, IUnitOfWork uow)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var release = Release.Create(request.ServiceName, request.Version, request.Environment);
            repo.Add(release);
            await uow.CommitAsync(ct);
            return new Response(release.Id.Value, release.Status.ToString());
        }
    }
}
```

### 2.5 Pipeline MediatR (ordem de execução)

| # | Behavior | Função |
|---|---|---|
| 1 | `ValidationBehavior` | Executa todos os `IValidator<TRequest>` via FluentValidation; bloqueia handler se falhar |
| 2 | `ContextualLoggingBehavior` | Enriquece contexto (tenant, user, correlationId) |
| 3 | `TenantIsolationBehavior` | Valida `request.TenantId == currentTenant.Id`; rejeita se IPublicRequest não marcado |
| 4 | `TransactionBehavior` | Gerencia transação DbContext com savepoint |
| 5 | `PerformanceBehavior` | Loga Warning se duração > 500ms (configurável) |
| 6 | `LoggingBehavior` | Loga entrada/saída (sem serializar request — segurança) |

### 2.6 Result Pattern

```csharp
// Sem exceções para erros esperados
Result<ServiceDto> result = await sender.Send(query, ct);

// Mapeamento automático para HTTP
return result.ToHttpResult(localizer);
// NotFound → 404 | Validation/Business → 422 | Conflict → 409
// Unauthorized → 401 | Forbidden → 403 | Security → 500 (sem detalhe)
```

---

## 3. Tecnologias

### 3.1 Stack Principal

| Camada | Tecnologia | Versão |
|---|---|---|
| Runtime | .NET | 10.0 |
| Web Framework | ASP.NET Core (Minimal APIs) | 10.0.5 |
| ORM | Entity Framework Core + Npgsql | 10.0.4 / 10.0.2 |
| CQRS | MediatR | 14.1.0 |
| Validação | FluentValidation | 12.1.1 |
| Mapeamento | Mapster | 10.0.1 |
| GraphQL | HotChocolate Federation | 14.3.1 |
| Scheduler | Quartz.NET | 3.16.1 |
| Auth | JWT Bearer | 10.0.5 |
| Guard Clauses | Ardalis.GuardClauses | — |
| OpenAPI | Scalar.AspNetCore | — |

### 3.2 Infraestrutura

| Componente | Obrigatoriedade | Versão / Nota |
|---|---|---|
| PostgreSQL | Obrigatório (≥ 15) | Single DB, 27 DbContexts, RLS via `set_config` |
| Redis | Opcional | `StackExchange.Redis`; fallback in-process |
| Apache Kafka | Opcional | `Confluent.Kafka 2.8.0`; `NullKafkaEventProducer` padrão |
| ClickHouse | Opcional | `ClickHouse.Client 7.9.0`; desabilitado por padrão |
| Elasticsearch | Opcional | `NEST 7.17.5`; busca full-text |
| Qdrant | Opcional | `Qdrant.Client 1.12.0`; vector DB para RAG |
| Grafana Loki | Opcional | via Serilog sink |
| SMTP | Opcional | Notificações por email |
| Ollama | Opcional | Inferência de IA local |
| OpenTelemetry Collector | Opcional | OTLP exporter |

### 3.3 IA e Machine Learning

| Tecnologia | Versão | Uso |
|---|---|---|
| Semantic Kernel (Microsoft) | 1.76.0 | Orquestração de prompts e plugins de IA |
| Qdrant Client | 1.12.0 | Embeddings e RAG (Retrieval-Augmented Generation) |
| ML.NET Tokenizers | 2.0.0 | Processamento de texto e tokenização |
| Provedores externos | — | OpenAI, Anthropic, Google via routing dinâmico |

### 3.4 Observabilidade

| Tecnologia | Versão | Uso |
|---|---|---|
| OpenTelemetry | 1.12.0 | Traces, metrics, logs via OTLP |
| Serilog | 4.3.1 | Logging estruturado (Console, File, Loki, OTel) |
| Health Checks ASP.NET Core | — | `/health`, `/ready`, `/live` |

### 3.5 Testes

| Tipo | Tecnologia | Versão |
|---|---|---|
| Framework | xUnit | 2.9.3 |
| Assertions | FluentAssertions | 8.9.0 |
| Mocks | NSubstitute | 5.3.0 |
| Dados fictícios | Bogus | 35.6.5 |
| Containers | Testcontainers.PostgreSql | 4.11.0 |
| Reset BD | Respawn | — |
| E2E Browser | Selenium WebDriver | 4.43.0 |
| Load Testing | k6 (TypeScript) | — |

### 3.6 Ferramentas CLI

| Ferramenta | Tecnologia |
|---|---|
| CLI `nex` | System.CommandLine + Spectre.Console |
| SDK cliente | .NET 6.0 (retrocompatibilidade) |

---

## 4. Módulos de Negócio

> Cada módulo segue: Domain → Application → Infrastructure → API → Contracts

---

### 4.1 Módulo: Catalog

**Responsabilidade:** Catálogo centralizado de serviços, APIs, contratos e dependências. É a "fonte de verdade" sobre o que existe, quem é dono e como está a saúde dos contratos.

#### Subdomínios
- **Graph** — Grafo de dependências e snapshots de arquitetura
- **Contracts** — Ciclo de vida de contratos multi-protocolo
- **LegacyAssets** — Gestão de ativos legados
- **Portal** — Developer Portal
- **DeveloperExperience** — Métricas de DX
- **Templates** — Modelos de serviço
- **DependencyGovernance** — Governança de dependências
- **SourceOfTruth** — Metadados canônicos

#### Entidades de Domínio Principais

**ContractVersion** (Aggregate Root)
- Campos: `Id`, `ApiAssetId`, `ServiceName`, `Version` (semver), `Protocol` (OpenAPI, GraphQL, gRPC, AsyncAPI, SOAP, DataContract), `SpecContent` (max 1MB), `ChangeLevel` (None/Patch/Minor/Major/Breaking), `Status` (Draft/Published/Deprecated/Archived), `HasBreakingChanges`, `ConsumerCount`, `HealthScore`, `TenantId`, `RowVersion`
- Métodos: `Publish()`, `Deprecate()`, `Archive()`, `RecordConsumerRegistration()`, `UpdateHealthScore()`

**ContractDraft** (Aggregate Root)
- Campos: `ApiAssetId`, `DraftContent`, `Protocol`, `ValidatedAt`, `ValidationPassed`, `ValidationErrors` (JSONB)
- Métodos: `Validate()`, `Publish()`, `Discard()`

**ServiceAsset** (Aggregate Root)
- Campos: `Name`, `DisplayName`, `Team`, `Domain`, `Tier` (Critical/Standard/Internal), `IsDeprecated`, `Language`, `Repository`, `Documentation`, `SbomUri`, `TenantId`
- Métodos: `AssignOwnership()`, `Deprecate()`, `UpdateTier()`

**ContractHealthScore** (Entity)
- Campos: `ApiAssetId`, `OverallScore` (0–100), `ConsistencyScore`, `DocumentationScore`, `VersioningScore`, `BreakingChangeScore`, `ComputedAt`

**ContractConsumerInventory** (Entity)
- Rastreia quais serviços consomem qual contrato, com versão e ambiente

**ContractDeployment** (Entity)
- Associa contrato publicado a ambiente de deployment

**Outros:** GraphSnapshot, SavedView, OwnershipAudit, ServiceTier, DeprecationSchedule, ServiceLink, DeveloperSurvey, ProductivitySnapshot, DataContractSchema, EventContractDetail, SoapContractDetail, ContractVerification, ContractChangeLog, ContractArtifact, ContractComplianceResult

#### Features Principais (160+)

| Feature | Tipo | Descrição |
|---|---|---|
| `CreateContractDraft` | Command | Cria rascunho de contrato com spec e protocolo |
| `PublishContractDraft` | Command | Valida e publica rascunho como versão oficial |
| `ComputeSemanticDiff` | Command | Diff semântico entre duas versões (OpenAPI, GraphQL) |
| `GenerateContractScorecard` | Command | Calcula health score com 4 dimensões |
| `RegisterServiceAsset` | Command | Regista serviço no catálogo |
| `CreateGraphSnapshot` | Command | Snapshot do grafo de dependências atual |
| `GetAssetGraph` | Query | Grafo completo com serviços, dependências e saúde |
| `SearchContracts` | Query | Busca full-text em contratos com filtros |
| `GetBreakingChangeReport` | Query | Relatório de breaking changes num período |
| `BindContractToInterface` | Command | Vincula contrato publicado a interface de serviço |
| `GetContractConsumerInventory` | Query | Quem consome este contrato e em que versão |
| `ExportBackstage` | Command | Exporta catálogo para formato Backstage |
| `RunDiscovery` | Command | Descoberta automática de serviços e contratos |
| `GetDoraServiceMetrics` | Query | DORA metrics centradas no serviço |
| `ComputeContractComplianceResult` | Command | Avalia conformidade do contrato com políticas |

#### Endpoints HTTP (90+)

**Base:** `/api/v1/catalog`

| Método | Path | Auth | Descrição |
|---|---|---|---|
| `POST` | `/drafts` | `catalog:contracts:write` | Criar rascunho |
| `GET` | `/drafts/{draftId}` | `catalog:contracts:read` | Obter rascunho |
| `POST` | `/drafts/{draftId}/validate` | `catalog:contracts:write` | Validar spec |
| `POST` | `/drafts/{draftId}/publish` | `catalog:contracts:write` | Publicar |
| `GET` | `/contracts` | `catalog:contracts:read` | Listar contratos (paginado) |
| `GET` | `/contracts/{id}` | `catalog:contracts:read` | Detalhe de contrato |
| `GET` | `/contracts/{id}/versions` | `catalog:contracts:read` | Histórico de versões |
| `GET` | `/contracts/{id}/diff` | `catalog:contracts:read` | Diff semântico |
| `GET` | `/contracts/{id}/health` | `catalog:contracts:read` | Health score |
| `GET` | `/contracts/{id}/consumers` | `catalog:contracts:read` | Consumidores |
| `GET` | `/contracts/{id}/changelog` | `catalog:contracts:read` | Changelog |
| `POST` | `/services` | `catalog:assets:write` | Registar serviço |
| `GET` | `/services` | `catalog:assets:read` | Listar serviços |
| `GET` | `/services/{id}` | `catalog:assets:read` | Detalhe de serviço |
| `GET` | `/graph` | `catalog:graph:read` | Grafo completo |
| `POST` | `/graph/snapshots` | `catalog:graph:write` | Criar snapshot |
| `GET` | `/search` | `catalog:contracts:read` | Busca full-text |
| `POST` | `/discovery/run` | `catalog:assets:write` | Descoberta automática |
| `GET` | `/export/backstage` | `catalog:assets:read` | Exportar para Backstage |
| `GET` | `/health-scores` | `catalog:contracts:read` | Health scores agregados |
| `GET` | `/breaking-changes` | `catalog:contracts:read` | Relatório breaking changes |

#### Interface Pública

```csharp
public interface IContractsModule
{
    Task<ChangeLevel?> GetLatestChangeLevelAsync(Guid apiAssetId, CancellationToken ct);
    Task<bool> HasContractVersionAsync(Guid apiAssetId, CancellationToken ct);
    Task<decimal?> GetLatestOverallScoreAsync(Guid apiAssetId, CancellationToken ct);
    Task<bool> RequiresWorkflowApprovalAsync(Guid apiAssetId, CancellationToken ct);
    Task<IReadOnlyList<ContractBreakingChangeSummary>> GetRecentBreakingChangesAsync(
        Guid apiAssetId, int days, CancellationToken ct);
}
```

#### Eventos de Integração
- `BreakingChangeDetectedIntegrationEvent(apiAssetId, version, changeLevel, tenantId)`
- `ContractPublishedIntegrationEvent(contractId, protocol, serviceName, tenantId)`
- `ContractVerificationPassedIntegrationEvent(contractId, tenantId)`
- `ContractDeprecatedIntegrationEvent(contractId, successorId, tenantId)`

#### Configuração EF Core (tabelas)
Prefixo `ctr_` — ex: `ctr_contract_versions`, `ctr_contract_drafts`, `ctr_service_assets`, `ctr_sbom_records`, `ctr_data_contract_records`, `ctr_deprecation_schedules`, `ctr_feature_flag_records`

---

### 4.2 Módulo: Change Governance

**Responsabilidade:** Motor central de governança de mudanças. Avalia risco de releases, calcula blast radius, gere promoções entre ambientes, executa workflows de aprovação e mede métricas DORA.

#### Subdomínios
- **ChangeIntelligence** — Releases, scores, blast radius, DORA metrics, freeze windows
- **Workflow** — Aprovações com evidência e SLA
- **Promotion** — Promoção entre ambientes com gates configuráveis
- **RulesetGovernance** — Linting via Spectral

#### Entidades de Domínio Principais

**Release** (Aggregate Root)
- Campos: `ApiAssetId`, `ServiceName`, `Version` (semver), `Environment`, `PipelineSource`, `CommitSha`, `ChangeLevel` (None/Patch/Minor/Major/Breaking), `Status` (Pending/Running/Succeeded/Failed/RolledBack), `ChangeScore` (decimal 0.0–1.0, 4 casas), `ConfidenceStatus` (Unknown/Low/Medium/High), `TeamName`, `Domain`, `HasBreakingChanges`, `ExternalReleaseId`, `ExternalSystem`, `ReleaseName`, `ApprovalStatus`, `TenantId`, `RowVersion`
- Métodos: `NotifyDeployment()`, `UpdateDeploymentState()`, `MarkSucceeded()`, `MarkFailed()`, `InitiateRollback()`, `UpdateChangeScore()`, `SetBlastRadiusData()`
- Transições: Pending → Running → Succeeded|Failed → RolledBack

**ChangeIntelligenceScore** (Entity)
- Campos: `ReleaseId`, `BreakingChangeWeight`, `BlastRadiusWeight`, `EnvironmentWeight`, `Score` = média das 3 dimensões arredondada a 4 casas, `ComputedAt`

**BlastRadiusReport** (Entity)
- Campos: `ReleaseId`, `DirectDependencies` (int), `TransitiveDependencies` (int), `AffectedTeams` (JSONB), `AffectedContracts` (JSONB), `RiskLevel` (Low/Medium/High/Critical), `ComputedAt`

**FreezeWindow** (Aggregate Root)
- Campos: `Name`, `Reason`, `StartTime`, `EndTime`, `Scope` (Global/Environment/Team/Service), `ScopeValue`, `IsActive`, `CreatedBy`
- Regra: `EndTime > StartTime`; sem sobreposição de janelas ativas do mesmo scope

**WorkflowInstance** (Aggregate Root)
- Campos: `ReleaseId`, `TemplateId`, `Status` (Pending/InProgress/Approved/Rejected/Cancelled), `CurrentStageId`, `Stages` (navegação), `CreatedAt`, `CompletedAt`
- Transições: Pending → InProgress → Approved|Rejected|Cancelled

**WorkflowStage** (Entity)
- Campos: `WorkflowInstanceId`, `Name`, `Order`, `RequiredApprovers`, `ApprovalMode` (Any/All), `SlaHours`, `Status`, `DecidedAt`, `DecidedBy`

**PromotionRequest** (Aggregate Root)
- Campos: `ReleaseId`, `FromEnvironment`, `ToEnvironment`, `Status` (Pending/Approved/InProgress/Succeeded/Failed/Cancelled), `RequestedBy`, `RequestedAt`, `Gates` (navegação)
- Transições: Pending → Approved → InProgress → Succeeded|Failed

**PromotionGate** (Entity)
- Campos: `PromotionRequestId`, `Name`, `GateType` (ContractCompliance/ErrorBudget/FinOps/FourEyes/CAB/RAB), `IsBlocking`, `Order`, `Result` (Pending/Pass/Fail/Skipped), `EvaluatedAt`, `Details`

**Outros:** PostReleaseReview, ObservationWindow, RollbackAssessment, ReleaseBaseline, ExternalMarker, Commit, EvidencePack, ApprovalDecision, Ruleset, RulesetBinding, LintExecution

#### Features Principais (95+)

| Feature | Tipo | Descrição |
|---|---|---|
| `CreateRelease` | Command | Regista nova release com validação semver |
| `ComputeChangeScore` | Command | Calcula score de risco (0.0–1.0) com 3 pesos |
| `CalculateBlastRadius` | Command | Calcula impacto transitivo da mudança |
| `GetDoraMetrics` | Query | Lead Time, Deploy Frequency, MTTR, CFR num período |
| `CreateFreezeWindow` | Command | Define janela de congelamento de mudanças |
| `CheckFreezeConflict` | Query | Verifica se release conflitua com freeze window |
| `CreateWorkflowInstance` | Command | Inicia workflow de aprovação para release |
| `ProgressWorkflowStage` | Command | Aprova/rejeita estágio de workflow |
| `CreatePromotionRequest` | Command | Solicita promoção entre ambientes |
| `EvaluatePromotionGate` | Command | Avalia gate (compliance, budget, etc.) |
| `GetPromotionReadinessDelta` | Query | Delta de prontidão para promoção |
| `StartPostReleaseReview` | Command | Inicia período de observação pós-release |
| `RecordCanaryRollout` | Command | Regista rollout canário com métricas |
| `GetPreProductionComparison` | Query | Comparação métricas pré/pós produção |
| `IngestExternalRelease` | Command | Ingere release de sistema externo (idempotente) |
| `SyncJiraWorkItems` | Command | Sincroniza work items do Jira |
| `ExecuteRulesetLinting` | Command | Executa Spectral lint numa spec |
| `EvaluateReleaseTrain` | Query | Avalia estado de release train |

#### Endpoints HTTP (60+)

**ChangeIntelligence** — `/api/v1/change-intelligence`

| Método | Path | Auth | Descrição |
|---|---|---|---|
| `POST` | `/releases` | `change-intelligence:write` | Criar release |
| `GET` | `/releases` | `change-intelligence:read` | Listar (paginado, filtros) |
| `GET` | `/releases/{id}` | `change-intelligence:read` | Detalhe |
| `POST` | `/releases/{id}/score` | `change-intelligence:write` | Calcular change score |
| `POST` | `/releases/{id}/blast-radius` | `change-intelligence:write` | Calcular blast radius |
| `GET` | `/releases/{id}/blast-radius` | `change-intelligence:read` | Obter blast radius |
| `POST` | `/releases/{id}/deployment` | `change-intelligence:write` | Notificar deployment |
| `GET` | `/dora-metrics` | `change-intelligence:read` | DORA metrics |
| `POST` | `/freeze-windows` | `change-intelligence:write` | Criar freeze window |
| `GET` | `/freeze-windows` | `change-intelligence:read` | Listar freeze windows |
| `GET` | `/freeze-windows/check-conflict` | `change-intelligence:read` | Verificar conflito |
| `GET` | `/is-change-window-open` | `change-intelligence:read` | Janela aberta? |
| `POST` | `/release-windows` | `change-intelligence:write` | Registar release window |
| `GET` | `/release-windows/calendar` | `change-intelligence:read` | Calendário de releases |
| `POST` | `/commits/ingest` | `change-intelligence:write` | Ingerir commit |
| `POST` | `/external/release-ingest` | `change-intelligence:write` | Ingerir release externo |
| `POST` | `/reports/canary-record` | `change-intelligence:write` | Registo de canário |

**Workflow** — `/api/v1/workflows`

| Método | Path | Auth | Descrição |
|---|---|---|---|
| `POST` | `/templates` | `workflow:write` | Criar template de aprovação |
| `GET` | `/templates` | `workflow:read` | Listar templates |
| `GET` | `/{workflowId}` | `workflow:read` | Detalhe de instância |
| `POST` | `/{stageId}/decide` | `workflow:write` | Aprovar/rejeitar estágio |
| `GET` | `/approval-requests` | `workflow:read` | Listar pedidos de aprovação |
| `POST` | `/approval-requests/{id}/respond` | `workflow:write` | Responder pedido |

**Promotion** — `/api/v1/promotions`

| Método | Path | Auth | Descrição |
|---|---|---|---|
| `POST` | `/` | `promotion:write` | Criar pedido de promoção |
| `GET` | `/` | `promotion:read` | Listar promoções |
| `GET` | `/{id}` | `promotion:read` | Detalhe |
| `POST` | `/{gateId}/evaluate` | `promotion:write` | Avaliar gate |
| `GET` | `/{id}/readiness-delta` | `promotion:read` | Delta de prontidão |
| `GET` | `/by-environment/{env}` | `promotion:read` | Por ambiente |

#### Interface Pública

```csharp
public interface IChangeIntelligenceModule
{
    Task<ReleaseDto?> GetReleaseAsync(Guid releaseId, CancellationToken ct);
    Task<decimal?> GetChangeScoreAsync(Guid releaseId, CancellationToken ct);
    Task<BlastRadiusDto?> GetBlastRadiusAsync(Guid releaseId, CancellationToken ct);
    Task<IReadOnlyList<ReleaseSummaryDto>> GetReleasesInWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
```

#### Eventos de Integração
- `PromotionRegisteredEvent(promotionId, releaseId, fromEnv, toEnv, tenantId)`
- `WorkflowApprovedEvent(workflowId, releaseId, approvedAt, tenantId)`
- `WorkflowRejectedEvent(workflowId, releaseId, reason, tenantId)`
- `DeploymentEventReceivedEvent(releaseId, pipelineSource, receivedAt)`

#### Configuração EF Core (tabelas)
Prefixo `chg_` — 4 contextos: `ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext`

Tabela `chg_releases`: índices em `ApiAssetId`, `Status`, `ChangeLevel`, `CreatedAt`, `TenantId`; unique em `(ExternalReleaseId, ExternalSystem, TenantId)`; check constraints em `Status IN (0..4)`, `ChangeScore BETWEEN 0 AND 1`

---

### 4.3 Módulo: Operational Intelligence

**Responsabilidade:** Visibilidade operacional completa — incidentes, custos cloud, SLOs, métricas de runtime e automação de resposta.

#### Subdomínios
- **Incidents** — Ciclo de vida de incidentes com correlação automática, PIR e mitigação
- **Cost Intelligence** — FinOps, importação de billing cloud, forecasting, waste detection
- **Reliability** — SLO/SLI, error budget, burn rate, failure prediction
- **Runtime Intelligence** — Snapshots de métricas, profiling, chaos, drift
- **Automation** — Workflows automatizados de resposta

#### Entidades de Domínio Principais

**IncidentRecord** (Aggregate Root)
- Campos: `ExternalRef` (ex: INC-2026-0042, único), `Title` (500), `Description` (4000), `Type` (ServiceDegradation/AvailabilityIssue/DependencyFailure/ContractImpact/MessagingIssue/BackgroundProcessingIssue/OperationalRegression), `Severity` (Warning/Minor/Major/Critical), `Status` (Open/Investigating/Mitigating/Monitoring/Resolved/Closed), `ServiceId`, `OwnerTeam`, `Environment`, `HasCorrelation`, `CorrelationConfidence` (NotAssessed/Low/Medium/High), `TenantId`, `RowVersion`
- Campos JSONB: `TimelineJson`, `LinkedServicesJson`, `CorrelatedChangesJson`, `ImpactedContractsJson`, `RunbookLinksJson`, `MitigationActionsJson`
- Métodos: `SetCorrelation()`, `SetEvidence()`, `SetMitigation()`, `MarkResolved()`, `SetTenantContext()`

**MitigationWorkflowRecord** (Aggregate Root)
- Campos: `IncidentId`, `Status` (Draft/Approved/InProgress/Completed/Cancelled), `ActionType` (Rollback/Scale/Restart/HealthCheck), `RiskLevel`, `RequiresApproval`, `StepsJson`, `DecisionsJson`
- Métodos: `UpdateStatus()`, `SetApproval()`, `SetStarted()`, `SetCompleted()`

**PostIncidentReview** (Aggregate Root)
- Fases: `FactGathering → RootCauseAnalysis → PreventiveActions → FinalReview → Completed`
- Outcomes: `Pending/Successful/Inconclusive/CausedByExternal`

**CostRecord** (Aggregate Root)
- Campos: `BatchId`, `ServiceId`, `ServiceName`, `Team`, `Domain`, `Environment`, `Period` (ex: "2026-03"), `TotalCost` (≥0), `Currency`, `Source`, `ReleaseId` (nullable — correlação com mudança)
- Factory: `Create()` retorna `Result<CostRecord>`, valida custo não-negativo

**SloDefinition** (Aggregate Root)
- Campos: `ServiceId`, `Environment`, `Name`, `Type` (Availability/Latency/ErrorRate/DurationUptime), `TargetPercent` (0–100), `AlertThresholdPercent` (nullable), `WindowDays` (>0), `IsActive`, `RowVersion`
- Métodos: `Deactivate()`, `UpdateTarget()`

#### Features Principais (118+)

**Incidents (40 features)**

| Feature | Tipo | Descrição |
|---|---|---|
| `CreateIncident` | Command | Cria incidente com correlação automática inicial |
| `CorrelateIncidentWithChanges` | Command | Correlaciona com mudanças recentes |
| `RefreshIncidentCorrelation` | Command | Atualiza correlação com novos dados |
| `TriageIncident` | Query | Auto-triagem via IA |
| `GetRootCauseSuggestion` | Query | Sugestão de causa raiz (IA) |
| `StartPostIncidentReview` | Command | Inicia PIR formal |
| `ProgressPostIncidentReview` | Command | Avança fase do PIR |
| `ResolveIncident` | Command | Marca como resolvido |
| `SelectMitigationPlaybook` | Query | Seleciona playbook adequado automaticamente |
| `FindSimilarIncidents` | Query | Incidentes similares (90 dias lookback) |
| `GetOnCallIntelligence` | Query | Fadiga e distribuição de on-call |
| `AutoCreateIncidentFromBatchFailure` | Command | Auto-criação de incidente por falha de batch |
| `GetIncidentDetail` | Query | Detalhe consolidado (timeline, correlação, evidência, mitigação) |
| `GetUnifiedTimeline` | Query | Timeline unificada de incidentes e eventos |
| `GenerateIncidentNarrative` | Query | Narrativa contextual gerada por IA |

**Cost Intelligence (28 features)**

| Feature | Tipo | Descrição |
|---|---|---|
| `ImportCostBatch` | Command | Importa batch de custos cloud (Source, Period, Records[]) |
| `DetectCostAnomalies` | Command | Deteta anomalias vs orçamento |
| `DetectWasteSignals` | Command | Identifica recursos em desperdício |
| `GenerateEfficiencyRecommendations` | Command | Recomendações de otimização |
| `ForecastBudget` | Command | Previsão de orçamento por tendência linear |
| `GetCarbonScoreReport` | Query | Score de pegada de carbono |
| `GetFocusExport` | Query | Exporta dados em formato FOCUS (standard FinOps) |
| `CorrelateCloudCostWithChange` | Command | Correlaciona custo com release |
| `EvaluateCostAwareChangeGate` | Command | Gate de custo em promoção |
| `GetShowbackReport` | Query | Showback por equipa |

**Reliability (25 features)**

| Feature | Tipo | Descrição |
|---|---|---|
| `RegisterSloDefinition` | Command | Regista SLO com target, janela e threshold |
| `ComputeErrorBudget` | Command | Calcula budget restante no período |
| `ComputeBurnRate` | Command | Taxa de consumo do error budget |
| `GenerateHealingRecommendation` | Command | Recomendação de auto-healing |
| `PredictServiceFailure` | Command | Prevê falha com base em padrões históricos |
| `GetServiceReliabilityDetail` | Query | Detalhe completo de reliability |
| `GetSloBurnRateAlert` | Query | Alerta de burn rate crítico |

#### Endpoints HTTP (50+)

**Incidents** — `/api/v1/incidents` (rate limit: `operations`)

| Método | Path | Permissão |
|---|---|---|
| `POST` | `/` | `operations:incidents:write` |
| `GET` | `/` | `operations:incidents:read` |
| `GET` | `/summary` | `operations:incidents:read` |
| `GET` | `/timeline` | `operations:incidents:read` |
| `GET` | `/{id}` | `operations:incidents:read` |
| `GET` | `/{id}/correlation` | `operations:incidents:read` |
| `POST` | `/{id}/correlation/refresh` | `operations:incidents:write` |
| `GET` | `/{id}/evidence` | `operations:incidents:read` |
| `GET` | `/{id}/mitigation` | `operations:incidents:read` |
| `GET` | `/{id}/impact` | `operations:incidents:read` |
| `GET` | `/{id}/similar` | `operations:incidents:read` |
| `GET` | `/{id}/triage` | `operations:incidents:read` |
| `GET` | `/{id}/root-cause` | `operations:incidents:read` |
| `GET` | `/on-call-intelligence` | `operations:incidents:read` |

**Cost** — `/api/v1/cost`

| Método | Path | Permissão |
|---|---|---|
| `POST` | `/import` | `operations:cost:write` |
| `GET` | `/import` | `operations:cost:read` |
| `GET` | `/report` | `operations:cost:read` |
| `GET` | `/records` | `operations:cost:read` |
| `POST` | `/forecast` | `operations:cost:write` |
| `GET` | `/forecast` | `operations:cost:read` |
| `GET` | `/recommendations` | `operations:cost:read` |
| `GET` | `/carbon-score` | `operations:cost:read` |
| `GET` | `/focus-export` | `operations:cost:read` |
| `GET` | `/showback` | `operations:cost:read` |
| `GET` | `/waste` | `operations:cost:read` |

**Reliability** — `/api/v1/reliability`

| Método | Path | Permissão |
|---|---|---|
| `POST` | `/slos` | `operations:reliability:write` |
| `GET` | `/slos` | `operations:reliability:read` |
| `POST` | `/slas` | `operations:reliability:write` |
| `POST` | `/error-budget/compute` | `operations:reliability:write` |
| `POST` | `/burn-rate/compute` | `operations:reliability:write` |
| `GET` | `/services/{id}/detail` | `operations:reliability:read` |
| `GET` | `/services/{id}/trend` | `operations:reliability:read` |
| `GET` | `/domains/{id}/summary` | `operations:reliability:read` |
| `GET` | `/teams/{id}/summary` | `operations:reliability:read` |

#### Interfaces Públicas

```csharp
public interface IIncidentModule
{
    Task<int> CountOpenIncidentsAsync(CancellationToken ct);
    Task<int> CountResolvedInLastDaysAsync(int days, CancellationToken ct);
    Task<decimal> GetAverageResolutionHoursAsync(int days, CancellationToken ct);
    Task<IReadOnlyList<IncidentSummaryDto>> GetRecentIncidentsAsync(
        string tenantId, DateTimeOffset from, DateTimeOffset to, int maxCount, CancellationToken ct);
}

public interface ICostIntelligenceModule
{
    Task<decimal?> GetCurrentMonthlyCostAsync(string serviceName, string env, CancellationToken ct);
    Task<decimal?> GetCostTrendPercentageAsync(string serviceName, string env, CancellationToken ct);
    Task<BudgetForecastSummary?> GetLatestBudgetForecastAsync(string serviceId, string env, CancellationToken ct);
}

public interface IReliabilityModule
{
    Task<string?> GetCurrentReliabilityStatusAsync(string serviceName, string env, CancellationToken ct);
    Task<decimal?> GetRemainingErrorBudgetAsync(string serviceName, string env, CancellationToken ct);
    Task<decimal?> GetCurrentBurnRateAsync(string serviceName, string env, CancellationToken ct);
    Task<IReadOnlyList<SloSummary>> GetServiceSlosAsync(string serviceName, string env, CancellationToken ct);
}
```

#### Eventos de Integração
- `IncidentCreatedIntegrationEvent`, `IncidentEscalatedIntegrationEvent`, `IncidentResolvedIntegrationEvent`
- `BudgetExceededIntegrationEvent`, `AnomalyDetectedIntegrationEvent`
- `HealthDegradationIntegrationEvent`, `SyncFailedIntegrationEvent`

---

### 4.4 Módulo: Governance

**Responsabilidade:** Governança estratégica da plataforma — packs de regras, dashboards customizados, gestão de equipas, domínios, compliance gates e políticas como código.

#### Entidades de Domínio Principais

**GovernancePack** (Aggregate Root)
- Campos: `Name` (único, 100), `DisplayName` (200), `Category` (ApiStandards/SecurityCompliance/PerformanceReliability/FinOpsOptimization/DataGovernance/ChangeControl), `Status` (Draft/Published/Deprecated/Archived), `CurrentVersion` (nullable até publicar), `RowVersion`
- Métodos: `Publish(version)`, `Deprecate()`, `Archive()`

**Team** (Aggregate Root)
- Campos: `Name` (único, 100), `DisplayName` (200), `Status` (Active/Inactive/Archived), `ParentOrganizationUnit` (200, nullable), `RowVersion`
- Métodos: `Activate()`, `Deactivate()`, `Archive()`

**CustomDashboard** (Aggregate Root)
- Campos: `Name`, `Description`, `Layout`, `Persona`, `Widgets` (IReadOnlyList<DashboardWidget>), `SharingPolicy` (Private/Team/Organization/Public), `Variables` (tokens $service, $team, $env, $timeRange), `CurrentRevisionNumber` (auto-increment), `LifecycleStatus` (Draft/Published/Deprecated/Archived), `IsSystem`, `TeamId`, `TenantId`, `RowVersion`
- Métodos: `Clone()`, `Update()` (incrementa revisão), `SetSharingPolicy()`, `Publish()`, `Deprecate()`, `Archive()`, `CreateRevisionSnapshot()`

**DashboardWidget** (Value Object Record)
```csharp
record DashboardWidget(string WidgetId, string Type,
    WidgetPosition Position, WidgetConfig Config);
record WidgetConfig(string? ServiceId, string? TeamId,
    string? TimeRange, string? CustomTitle);
```

**Outros:** GovernanceRuleBinding, GovernanceWaiver, GovernanceDomain, SharingPolicy, DashboardRevision, DashboardVariable, GovernanceRolloutRecord, PolicyAsCodeDefinition, EvidencePackage, ServiceMaturityAssessment, ExecutiveBriefing, SamlSsoConfiguration

#### Features Principais (160+)

| Feature | Tipo | Descrição |
|---|---|---|
| `CreateGovernancePack` | Command | Cria pack de regras (inicia em Draft) |
| `PublishGovernancePack` | Command | Publica pack (Draft → Published) com versão |
| `ApplyGovernancePack` | Command | Aplica pack a scope (Organization/Domain/Team/Service) |
| `CreateTeam` | Command | Cria equipa com verificação de nome único |
| `GetTeamHealthSnapshot` | Query | Snapshot de saúde da equipa |
| `GetTeamFinOps` | Query | Dados financeiros da equipa |
| `CreateCustomDashboard` | Command | Cria dashboard (inicia em Draft) |
| `CloneDashboard` | Command | Clona dashboard com novo nome |
| `PublishDashboard` | Command | Draft → Published |
| `DeprecateDashboard` | Command | Published → Deprecated com nota |
| `GetDashboardHistory` | Query | Histórico de revisões |
| `GetDashboardRenderData` | Query | Dados para renderizar widgets |
| `ComposeAiDashboard` | Command | Compõe dashboard via IA |
| `EvaluateChangeAdvisoryBoard` | Command | Avaliação CAB para release |
| `EvaluateFourEyesPrinciple` | Command | Gate de aprovação dupla |
| `EvaluateFinOpsBudgetGate` | Command | Gate de orçamento em promoção |
| `RunComplianceChecks` | Command | Executa verificações de compliance |
| `CreateGovernanceWaiver` | Command | Cria waiver temporário de política |
| `ApproveGovernanceWaiver` | Command | Aprova waiver |

#### Endpoints HTTP

**Dashboards** — `/api/v1/dashboards`

| Método | Path | Descrição |
|---|---|---|
| `GET` | `/` | Listar dashboards |
| `POST` | `/` | Criar dashboard |
| `GET` | `/{id}` | Detalhe |
| `PATCH` | `/{id}` | Atualizar |
| `POST` | `/{id}/clone` | Clonar |
| `POST` | `/{id}/publish` | Publicar |
| `POST` | `/{id}/deprecate` | Depreciar |
| `DELETE` | `/{id}` | Eliminar |
| `GET` | `/{id}/history` | Histórico de revisões |
| `GET` | `/{id}/render` | Dados de renderização |
| `POST` | `/{id}/share` | Definir política de partilha |
| `GET` | `/templates` | Listar templates |
| `POST` | `/templates/{id}/instantiate` | Instanciar template |

**Teams** — `/api/v1/teams`

| Método | Path | Permissão |
|---|---|---|
| `GET` | `/` | `governance:teams:read` |
| `POST` | `/` | `governance:teams:write` |
| `GET` | `/{id}` | `governance:teams:read` |
| `PATCH` | `/{id}` | `governance:teams:write` |
| `GET` | `/{id}/health` | `governance:teams:read` |
| `GET` | `/{id}/finops` | `governance:teams:read` |

**Governance Packs** — `/api/v1/governance/packs`

| Método | Path | Permissão |
|---|---|---|
| `GET` | `/` | `governance:packs:read` |
| `POST` | `/` | `governance:packs:write` |
| `POST` | `/{id}/publish` | `governance:packs:write` |
| `POST` | `/{id}/apply` | `governance:packs:write` |
| `GET` | `/{id}/coverage` | `governance:packs:read` |

---

### 4.5 Módulo: Identity & Access

**Responsabilidade:** Autenticação multi-mecanismo, autorização granular, gestão de identidades, JIT access, Break Glass emergencial e multi-tenancy hierárquico.

#### Entidades de Domínio Principais

**User** (AggregateRoot)
- Campos: `Email` (ValueObject, 320), `FullName` (FirstName+LastName), `PasswordHash` (BCrypt, null para federados), `IsActive`, `FailedLoginAttempts`, `LockoutEnd` (15min), `MfaEnabled`, `MfaMethod` (TOTP/WebAuthn/SMS), `FederationProvider`, `ExternalId`, `RowVersion`
- Métodos: `CreateLocal()`, `CreateFederated()`, `RegisterSuccessfulLogin()`, `RegisterFailedLogin()` (bloqueia após 5), `EnableMfa()`, `SetPassword()`
- Eventos: `UserCreatedDomainEvent`, `UserLockedDomainEvent`

**Tenant** (AggregateRoot)
- Campos: `Name`, `Slug` (único, lowercase), `IsActive`, `ParentTenantId` (nullable), `TenantType` (Organization/Holding/Subsidiary/Department), `LegalName`, `TaxId`, `RowVersion`
- Regras: Organization/Holding sem parent; Subsidiary/Department requerem parent; max 3 níveis de hierarquia; nunca deletado, apenas desativado

**Session** (AggregateRoot)
- Campos: `UserId`, `RefreshToken` (hash SHA-256), `ExpiresAt`, `CreatedByIp`, `UserAgent`, `RevokedAt`
- Métodos: `Revoke()`, `Rotate()` (extende validade), `IsActive(now)`

**JitAccessRequest** (AggregateRoot)
- Campos: `PermissionCode`, `Scope`, `Justification` (obrigatório), `Status` (Pending/Approved/Rejected/Expired/Revoked), `ApprovalDeadline` (4h padrão), `GrantedUntil` (8h padrão)
- Regras: Não pode auto-aprovar; Justificativa obrigatória para rejeição

**BreakGlassRequest** (AggregateRoot)
- Campos: `Justification`, `Status` (Active/Expired/Revoked/PostMortemCompleted), `ExpiresAt` (2h padrão), `IpAddress`, `UserAgent`
- Regras: Acesso imediato sem aprovação; max 3 usos/trimestre; post-mortem obrigatório em 24h; limite trimestral antes de escalar

**Environment** (Entity)
- Campos: `Name`, `Slug` (único por tenant), `SortOrder`, `Profile` (Development/Staging/Production/DisasterRecovery), `Criticality` (Low/Medium/High/Critical), `Region`, `IsProductionLike`, `IsPrimaryProduction` (único ativo por tenant)

**Role** (Entity) — Papéis: PlatformAdmin, TechLead, Developer, Viewer, Auditor, SecurityReview, ApprovalOnly, AiUser

**SecurityEvent** (Entity) — `EventType`, `RiskScore` (0–100), `IpAddress`, `UserAgent`, `Metadata` (JSONB)

**Outros:** UserRoleAssignment, TenantMembership, Permission (código: `módulo:recurso:ação`), Delegation, PlatformApiToken, AccessReviewCampaign, AlertFiringRecord, AgentRegistration

#### Features (70+ Commands, 15+ Queries)

**Autenticação**
- `LocalLogin`, `FederatedLogin`, `RefreshToken`, `Logout`, `RevokeSession`
- `StartOidcLogin`, `OidcCallback`, `StartSamlLogin`, `SamlAcsCallback`
- `VerifyMfaChallenge`, `ForgotPassword`, `ResetPassword`
- `ActivateAccount`, `RequestAccountActivation`

**Gestão de Identidades**
- `CreateUser`, `ListTenantUsers`, `GetUser`, `UpdateUser`
- `CreateRole`, `AssignRoleToUser`, `RevokeRole`, `GetRolePermissions`
- `CreateTenant`, `ListMyTenants`, `GetTenant`, `ActivateTenant`, `DeactivateTenant`

**JIT Access**
- `RequestJitAccess`, `DecideJitAccess` (aprova/rejeita), `RevokeJitAccess`

**Break Glass**
- `RequestBreakGlass`, `RevokeBreakGlass`, `RecordBreakGlassPostMortem`

**Ambientes**
- `CreateEnvironment`, `UpdateEnvironment`, `DeactivateEnvironment`
- `SetPrimaryProductionEnvironment`

**Outros**
- `CreatePlatformApiToken`, `RevokeApiToken`
- `CreateDelegation`, `RevokeDelegation`
- `CreateAccessReviewCampaign`, `ReviewAccessItem`
- `ProvisionTenantLicense`, `StartOnboarding`

#### Endpoints HTTP (80+)

**Base:** `/api/v1/identity`

| Grupo | Endpoints Principais | Rate Limit |
|---|---|---|
| `/auth/login` | POST login local | `auth` (20/min) |
| `/auth/refresh` | POST refresh token | `auth` |
| `/auth/oidc/*` | OIDC flow | `auth-sensitive` (10/min) |
| `/auth/saml/*` | SAML flow | `auth-sensitive` |
| `/auth/mfa/*` | MFA verify/resend | — |
| `/users` | CRUD utilizadores | `identity:users:*` |
| `/roles` | CRUD papéis | `identity:roles:*` |
| `/tenants` | CRUD tenants | `identity:tenants:*` |
| `/jit/*` | JIT request/approve/revoke | `identity:jit:*` |
| `/break-glass/*` | Acesso emergencial | `identity:break-glass:*` |
| `/environments` | CRUD ambientes | `identity:environments:*` |
| `/delegations` | CRUD delegações | `identity:delegations:*` |
| `/api-tokens` | CRUD API tokens | `identity:tokens:*` |
| `/security-events` | Lista eventos segurança | `identity:security:read` |
| `/access-reviews/*` | Campanhas de revisão | `identity:access-review:*` |
| `/licensing/*` | Gestão de licença | `identity:licensing:*` |
| `/onboarding/*` | Wizard de onboarding | `identity:onboarding:*` |

#### Interface Pública

```csharp
public interface IIdentityModule
{
    Task<UserSummaryDto?> GetUserByIdAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        Guid userId, Guid tenantId, CancellationToken ct);
    Task<bool> ValidateTenantMembershipAsync(
        Guid userId, Guid tenantId, CancellationToken ct);
}
```

#### Configuração EF Core
Prefixo `iam_` — 67 tabelas. Destaques: `iam_users` (email unique), `iam_environments` (unique `(tenant_id, slug)`; índice parcial em `is_primary_production`), `iam_sessions` (unique `refresh_token_hash`)

---

### 4.6 Módulo: AI Knowledge

**Responsabilidade:** Orquestração de IA com governança total — roteamento de providers, guardrails de conteúdo, gestão de budgets de tokens, avaliação de qualidade, RAG e integração com IDEs.

#### Entidades de Domínio Principais

**AiAssistantConversation** (AuditableEntity)
- Campos: `Title`, `Persona`, `ClientType` (Web/VsCode/Api/Mobile/CLI), `DefaultContextScope`, `LastModelUsed`, `MessageCount`, `Tags`, `IsActive`, `ServiceId`, `ContractId`, `IncidentId`, `ChangeId`, `TeamId`
- Métodos: `Start()`, `RecordMessage()`, `UpdateMetadata()`, `Archive()`

**AiGuardrail** (AuditableEntity)
- Campos: `Name` (único), `Category` (InputValidation/OutputFiltering/ContentModeração), `GuardType` (PreInput/PostInput/PreOutput/PostOutput), `Pattern` (regex), `PatternType` (Regex/Classification/Custom), `Severity` (Info/Warning/Error/Critical), `Action` (Block/Sanitize/Alert/LogOnly/FlagForReview), `Priority` (menor executa primeiro), `AgentId`/`ModelId` (null = global)

**AiAgentExecution** (AuditableEntity)
- Campos: `AgentId`, `ExecutedBy`, `Status` (Running/Completed/Failed/Cancelled), `ModelIdUsed`, `ProviderUsed`, `InputJson`, `OutputJson`, `PromptTokens`, `CompletionTokens`, `DurationMs`, `CorrelationId`, `Steps` (JSONB intermediário), `ContextJson`
- Métodos: `Start()`, `Complete()`, `Fail()`, `Cancel()`

**AIBudget** (AuditableEntity)
- Campos: `Scope` (user/group/team/role), `ScopeValue`, `Period` (Daily/Weekly/Monthly), `MaxTokens`, `MaxRequests`, `CurrentTokensUsed`, `CurrentRequestCount`, `PeriodStartDate`
- Métodos: `RecordUsage()` → `Result<Unit>` (erro se quota excedida), `ResetPeriod()`, `Update()`

**AiProvider** (Entity) — `Name` (único), `Type` (Cloud/OnPremise/Hybrid), `ApiKeyEncrypted`, `Capabilities` (JSONB), `IsActive`

**AIRoutingDecision** (Entity) — `RecommendedAgent`, `RecommendedModel`, `Provider`, `Confidence` (float 0–1), `Reasoning`

**AiEvaluation** (Entity) — `ExecutionId`, `EvaluationType` (Correctness/Safety/Performance/UserSatisfaction), `Score` (0–1)

**AiFeedback** (Entity) — `ConversationId`, `MessageId`, `Rating` (1–5), feedback textual, tags

**AIKnowledgeSource** (Entity) — `SourceType` (URL/Document/Database/API), `Location`, `LastSyncedAt`

**AiTokenUsageLedger** (Entity, imutável) — `UserId`, `AgentId`, `ModelId`, `TokensConsumed`, `CostUsd`, `ConsumedAt`

**Outros:** DefaultModelCatalog, ModelRoutingPolicy, AIRoutingStrategy, PromptAsset, PromptVersion, PromptTemplate, OrganizationalMemoryNode, ExternalDataSource, AIIDEClientRegistration, OnboardingSession, WarRoomSession, SelfHealingAction, AiAgentTrajectoryFeedback

#### Features (55+)

| Feature | Tipo | Descrição |
|---|---|---|
| `StartAiConversation` | Command | Inicia conversa com persona e escopo |
| `RecordAiMessage` | Command | Regista mensagem e atualiza contadores |
| `ExecuteAiAgent` | Command | Executa agent com rastreamento completo |
| `CreateAiGuardrail` | Command | Cria guardrail com pattern e ação |
| `ApplyGuardrails` | Command | Aplica guardrails ativos a input/output |
| `CreateAiBudget` | Command | Cria budget de tokens por scope |
| `RecordTokenUsage` | Command | Regista consumo no ledger (imutável) |
| `GetBudgetStatus` | Query | Status atual de consumo vs limite |
| `DecideAiRouting` | Command | Decide provider+modelo ideal para request |
| `RegisterKnowledgeSource` | Command | Regista fonte para RAG |
| `SyncKnowledgeSource` | Command | Sincroniza e indexa no Qdrant |
| `EvaluateAiExecution` | Command | Avalia qualidade de execução (1–5) |
| `SubmitAiFeedback` | Command | Feedback de utilizador |
| `ConsultExternalAi` | Command | Consulta a provider externo (auditada) |
| `RegisterAiIdeClient` | Command | Regista cliente IDE (VS Code, JetBrains) |

#### Endpoints HTTP (50+)

Base `/api/v1/ai`:
- `/conversations` — CRUD conversas
- `/agents` — CRUD e execução de agents
- `/guardrails` — CRUD guardrails com activate/deactivate
- `/budgets` — CRUD budgets + `/usage/report`
- `/models` — Listar modelos e definir default
- `/providers` — CRUD providers
- `/routing/decide` — Decisão de routing
- `/evaluation/evaluate` — Avaliar execução
- `/feedback` — Submeter e analisar tendências
- `/knowledge/sources` — CRUD e sync de fontes
- `/ide/register` — Registar cliente IDE
- `/external/consult` — Consulta externa auditada

#### Interfaces Públicas

```csharp
public interface IExternalAiModule
{
    Task<IReadOnlyList<ProviderSummaryDto>> GetAvailableProvidersAsync(CancellationToken ct);
    Task<RoutingDecisionDto?> RouteRequestAsync(
        string capability, string? preferredProvider, string? env, CancellationToken ct);
}
```

#### Configuração EF Core
Prefixo `aik_` — 40+ tabelas: `aik_conversations`, `aik_guardrails`, `aik_agent_executions` (unique `correlation_id`), `aik_budgets`, `aik_providers` (unique `name`), `aik_token_usage_ledger` (imutável, índices em `user_id+consumed_at` e `tenant_id+consumed_at`), `aik_model_routing_policies`, `aik_agent_execution_plans`, `aik_model_prediction_samples`

---

### 4.7 Módulo: Audit & Compliance

**Responsabilidade:** Trilha de auditoria imutável com integridade garantida por blockchain-style hash chain, gestão de campanhas de auditoria, políticas de compliance e retenção de dados.

#### Entidades de Domínio

**AuditEvent** (AggregateRoot, imutável após criação)
- Campos: `SourceModule`, `ActionType`, `ResourceId`, `ResourceType`, `PerformedBy`, `OccurredAt`, `TenantId`, `Payload` (JSONB, encriptado AES-256-GCM), `CorrelationId`, `ChainLink`

**AuditChainLink** (Entity)
- `SequenceNumber` (long, único), `CurrentHash` (SHA-256), `PreviousHash`
- Hash calculado: `SHA256($"{seq}|{eventId}|{sourceModule}|{actionType}|{resourceId}|{performedBy}|{occurredAt:O}|{previousHash}")`
- Método `Verify()` re-calcula e compara

**AuditCampaign** (Entity) — `Status` (Planned/InProgress/Completed/Cancelled)

**CompliancePolicy** (Entity) — `Category`, `Severity` (Low/Medium/High/Critical), `EvaluationCriteria` (JSONB), `IsActive`

**ComplianceResult** (Entity) — `Outcome` (Compliant/NonCompliant/PartiallyCompliant/NotApplicable), `Evidence` (JSONB)

**RetentionPolicy** (Entity) — `RetentionDays` (ex: 2555 = 7 anos), `AppliesTo`

#### Features (26)

| Feature | Tipo | Descrição |
|---|---|---|
| `RecordAuditEvent` | Command | Regista evento e cria chain link; retorna hash e sequência |
| `GetAuditTrail` | Query | Trilha de um recurso específico |
| `SearchAuditLog` | Query | Pesquisa com filtros (módulo, tipo, período, paginado) |
| `VerifyChainIntegrity` | Query | Verifica todos os hashes da cadeia |
| `ExportAuditReport` | Query | Exporta trilha em PDF/JSON |
| `CreateCompliancePolicy` | Command | Cria política de compliance |
| `EvaluateContinuousCompliance` | Command | Avalia recurso contra todas as políticas ativas |
| `GetComplianceFrameworkSummary` | Query | Resumo por framework (SOC2/ISO27001/GDPR/PCI-DSS) |
| `ExportComplianceEvidences` | Query | Pack de evidências para auditores (ZIP/XLSX) |
| `GenerateAuditReadyReport` | Query | Relatório enterprise com assinatura digital SHA-256 |
| `ConfigureRetention` | Command | Configura política de retenção |
| `ApplyRetention` | Command | Executa limpeza baseada em políticas |

#### Endpoints HTTP (35+)

Base `/api/v1/audit`:

| Método | Path | Permissão |
|---|---|---|
| `POST` | `/events` | `audit:events:write` |
| `GET` | `/trail` | `audit:trail:read` |
| `GET` | `/search` | `audit:trail:read` |
| `GET` | `/verify-chain` | `audit:trail:read` |
| `GET` | `/report` | `audit:reports:read` |
| `POST` | `/compliance/policies` | `audit:compliance:write` |
| `GET` | `/compliance/policies` | `audit:compliance:read` |
| `POST` | `/compliance/evaluate` | `audit:compliance:write` |
| `GET` | `/compliance/framework/{fw}` | `audit:compliance:read` |
| `GET` | `/compliance/evidences/export` | `audit:compliance:read` |
| `GET` | `/compliance/report` | `audit:compliance:read` |
| `POST` | `/campaigns` | `audit:compliance:write` |
| `GET` | `/campaigns` | `audit:compliance:read` |
| `POST` | `/retention/policies` | `audit:compliance:write` |
| `POST` | `/retention/apply` | `audit:compliance:write` |

#### Interface Pública

```csharp
public interface IAuditModule
{
    Task RecordEventAsync(string sourceModule, string actionType, string resourceId,
        string resourceType, string performedBy, Guid tenantId,
        string? payload, CancellationToken ct, string? correlationId = null);
    Task<bool> VerifyChainIntegrityAsync(CancellationToken ct);
}
```

#### Jobs
- `AuditRetentionJob` — limpeza por RetentionPolicy
- `CompliancePolicyEvaluationJob` — avaliação periódica de recursos
- `ChainIntegrityVerificationJob` — verificação diária da cadeia de hash

---

### 4.8 Módulo: Integrations

**Responsabilidade:** Orquestração de integrações externas com Null Object Pattern — todos os providers são opcionais e o sistema funciona sem nenhum deles.

#### Providers (todos com `bool IsConfigured`)

| Provider | Tipo | Implementação Real | Null Default |
|---|---|---|---|
| `IKafkaEventProducer` | Event streaming | `ConfluentKafkaEventProducer` | `NullKafkaEventProducer` |
| `ICloudBillingProvider` | AWS/Azure/GCP billing | Configurável | `NullCloudBillingProvider` |
| `ICanaryProvider` | Canary deployments | Configurável | `NullCanaryProvider` |
| `IBackupProvider` | Backup externo | Configurável | `NullBackupProvider` |
| `ISamlProvider` | SAML 2.0 | Configurável | `NullSamlProvider` |
| `ICertificateProvider` | PKI/certificados | Configurável | `NullCertificateProvider` |
| `IChaosProvider` | Chaos engineering | Configurável | `NullChaosProvider` |
| `IRuntimeProvider` | Datadog/NewRelic/Prometheus | Configurável | `NullRuntimeProvider` |

#### Backends Opcionais
- **ClickHouse** (`ClickHouse.Client 7.9.0`) — store analítico de telemetria
- **Elasticsearch** (`NEST 7.17.5`) — busca full-text
- **Dapper** (`2.1.35`) — queries SQL de alta performance

#### Features (23)
Incluem: `RegisterExternalIntegration`, `TestIntegrationConnectivity`, `IngestLegacyTelemetry`, `ReceiveWebhook`, `ProduceKafkaEvent`, `ProduceKafkaBatch`, `TriggerBackup`, `IngestCloudBilling`, `ExecuteCanaryCheck`

#### Endpoints HTTP (25+)
Base `/api/v1/integrations`: providers, kafka, backup, telemetry, webhooks

---

### 4.9 Módulo: Notifications

**Responsabilidade:** Entrega de notificações multi-canal com templates, digest, escalação automática e preferências por utilizador.

#### Entidades de Domínio

**NotificationRecord** (AggregateRoot)
- Campos: `EventType`, `Category` (System/Security/Incident/Change/Cost/Compliance/Ai/User), `Severity` (Info/Warning/Error/Critical), `Title`, `Message`, `SourceModule`, `SourceEntityType`, `SourceEntityId`, `ActionUrl`, `RequiresAction`, `TenantId`, `EnvironmentId`, `ExpiresAt`, `PayloadJson`

**NotificationDeliveryLog** (Entity) — canal, status, tentativas, timestamps

**NotificationTemplate** (Entity) — templates por EventType e canal

**NotificationDigest** (Entity) — agrupamento de notificações

**NotificationEscalation** (Entity) — escalação automática por SLA

**NotificationPreference** (Entity) — preferências por utilizador e canal

#### Features (25+)

| Feature | Tipo | Descrição |
|---|---|---|
| `SubmitNotification` | Command | Submete notificação com roteamento automático |
| `GetUnreadCount` | Query | Contagem de não lidas por utilizador |
| `ListNotifications` | Query | Lista paginada com filtros |
| `MarkAsRead` | Command | Marca como lida (idempotente) |
| `MarkAllAsRead` | Command | Marca todas as lidas |
| `SetNotificationPreferences` | Command | Configura preferências por canal |
| `GetNotificationPreferences` | Query | Obtém preferências |
| `ProcessDigest` | Command | Agrupa notificações em digest |
| `EscalateNotification` | Command | Escalação automática |

#### Endpoints HTTP (20+)

Base `/api/v1/notifications`:
- `POST /submit`, `GET /`, `GET /unread-count`, `POST /{id}/mark-read`, `POST /mark-all-read`
- `GET /preferences`, `POST /preferences`

#### Interface Pública

```csharp
public interface INotificationModule
{
    Task<NotificationResult> SubmitAsync(NotificationRequest request, CancellationToken ct);
    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct);
}

public record NotificationRequest
{
    public required string EventType { get; init; }
    public required string Category { get; init; }
    public required string Severity { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string SourceModule { get; init; }
    public IReadOnlyList<Guid>? RecipientUserIds { get; init; }
    public IReadOnlyList<string>? RecipientRoles { get; init; }
    public IReadOnlyList<Guid>? RecipientTeamIds { get; init; }
    public bool RequiresAction { get; init; }
    // + 10 campos opcionais
}
```

#### Canais de Entrega
Email (SMTP), SMS, Slack, Microsoft Teams, In-app (WebSocket/SSE)

---

### 4.10 Módulo: Configuration

**Responsabilidade:** Gestão centralizada de configurações da plataforma — feature flags, alert rules, automation rules, templates, taxonomias, webhooks e preferências de utilizador.

#### Entidades Principais (18+)

- `FeatureFlagDefinition` — flag com regras de targeting por tenant/role/user
- `AlertRule` — regra de alerta com condição e ação
- `AutomationRule` — regra de automação com trigger e actions
- `ChangeChecklistTemplate` — templates de checklist de mudança
- `ContractCompliancePolicy` — política de compliance de contrato
- `ContractTemplate` — modelo de contrato reutilizável
- `EntityTag` — tags em entidades de qualquer módulo
- `SavedPrompt` — prompts guardados
- `ScheduledReport` — relatórios agendados
- `UserPreference` — preferências por utilizador
- `UserBookmark` — bookmarks
- `UserWatch` — watches em entidades
- `WebhookTemplate` — templates de webhook
- `TaxonomyDefinition` — taxonomias customizáveis

#### Features (50+)
CRUD completo de cada tipo de configuração + avaliação de feature flags, disparo de webhooks, avaliação de alert rules.

#### Endpoints HTTP (60+)
Base `/api/v1/configuration`: feature-flags, alert-rules, automation-rules, taxonomies, webhooks, preferences, bookmarks, watches, saved-prompts, scheduled-reports

---

### 4.11 Módulo: Knowledge

**Responsabilidade:** Base de conhecimento interna com artigos, FAQs, runbooks executáveis e controlo de versões.

#### Entidades Principais

- `KnowledgeArticle` — artigo com versão, categoria e status
- `KnowledgeCategory` — categorias hierárquicas
- `Runbook` — procedimento operacional com passos
- `RunbookStep` — passo individual com tipo (Manual/Automated/Approval)
- `RunbookApproval` — aprovação de passo crítico
- `Faq` / `FaqCategory` — FAQs categorizadas

#### Features (18)
`CreateArticle`, `PublishArticle`, `SearchArticles`, `CreateRunbook`, `ExecuteRunbook`, `ExecuteRunbookStep`, `GetRunbookExecution`, `CreateFaq`

#### Endpoints HTTP (20+)
Base `/api/v1/knowledge`: articles, runbooks, faqs

---

### 4.12 Módulo: Product Analytics

**Responsabilidade:** Analytics de produto — rastreia uso de features, jornadas de utilizador, adoção por módulo e identificação de pontos de fricção.

#### Entidades Principais

**AnalyticsEvent** (Entity, imutável)
- Campos: `TenantId`, `UserId`, `Persona`, `Module` (enum), `EventType` (enum), `Feature`, `EntityType`, `Outcome`, `Route`, `TeamId`, `DomainId`, `SessionId`, `ClientType`, `MetadataJson`
- Só inserts, nunca updates; índices em `(TenantId, OccurredAt)`, `(UserId, Module)`, `EventType`

**JourneyDefinition** (Entity) — definição de funil de utilizador com passos em JSONB

#### Features (15)

| Feature | Tipo | Descrição |
|---|---|---|
| `RecordAnalyticsEvent` | Command | Regista evento (fire-and-forget, async) |
| `GetAdoptionFunnel` | Query | Funil de adoção por etapas |
| `GetAnalyticsSummary` | Query | Resumo global de utilização |
| `GetCohortAnalysis` | Query | Comparação entre cohorts |
| `GetFeatureHeatmap` | Query | Heatmap de uso de features |
| `GetFrictionIndicators` | Query | Pontos de fricção na UX |
| `GetModuleAdoption` | Query | Taxa de adoção por módulo |
| `GetPersonaUsage` | Query | Padrões de uso por persona |
| `GetValueMilestones` | Query | Marcos de valor alcançados |
| `ExportAnalyticsData` | Command | Exporta dados em CSV/JSON |

#### Backends Suportados
- `AnalyticsEventRepository` (PostgreSQL padrão)
- `ClickHouseAnalyticsEventRepository` (OLAP opcional)
- `ElasticsearchAnalyticsEventRepository` (busca opcional)

---

## 5. Plataforma

### 5.1 ApiHost — Sequência de Inicialização

```
1. Assembly Integrity Check (SHA-256 dos assemblies)
2. Serilog configuration (Console, File, Loki, OTel)
3. Building Blocks (EventBus, DbContexts em 7 waves, Cache, Security)
4. Distributed Cache (Redis ou in-process fallback)
5. HttpClient (proxy corporativo, certificados, AirGap enforcement)
6. Preflight Checks (10 checks, bloqueia startup se obrigatórios falharem)
7. Platform Providers (HealthProvider, PendingMigrationsProvider)
8. Módulos (13 módulos via AddXxxModule())
9. Cross-Module Bridges (OtelRuntimeComparisonReader)
10. Configuration Validation (JWT Secret, Connection Strings)
11. JSON Serialization (enums como strings)
12. OpenAPI + Scalar
```

### 5.2 Middleware Pipeline (15 passos em ordem)

| # | Middleware | Função |
|---|---|---|
| 1 | `UseResponseCompression` | Compressão de respostas |
| 2 | `UseHttpsRedirection` | Redirecionamento HTTPS |
| 3 | `UseCors` | CORS (sem wildcards com credentials) |
| 4 | `UseRateLimiter` | 7 políticas de rate limiting |
| 5 | `UseSecurityHeaders` | Headers de segurança (CSP, HSTS, etc.) |
| 6 | `UseGlobalExceptionHandler` | RFC 7807 Problem Details |
| 7 | `UseWebSockets` | WebSocket support |
| 8 | `UseCookieSessionCsrfProtection` | CSRF (X-Csrf-Token) |
| 9 | `UseAuthentication` | JWT + API Key |
| 10 | `TenantResolutionMiddleware` | X-Tenant-Id → CurrentTenant |
| 11 | `EnvironmentResolutionMiddleware` | X-Environment-Id |
| 12 | `EnvironmentAuthorizationMiddleware` | Autorização por ambiente |
| 13 | `SessionInactivityMiddleware` | Timeout de sessão (30min) |
| 14 | `MaintenanceModeMiddleware` | Modo manutenção |
| 15 | `UseAuthorization` | Políticas de permissão |

### 5.3 Rate Limiting (7 políticas)

| Política | Limite | Janela | Uso |
|---|---|---|---|
| Global | 100 req | 1 min | Todos os pedidos |
| Global (IP não resolvido) | 20 req | 1 min | Fallback restritivo |
| Auth | 20 req | 1 min | login, refresh, OIDC |
| AuthSensitive | 10 req | 1 min | register, cookie session |
| AI | 30 req | 1 min | chat, geração, retrieval |
| DataIntensive | 50 req | 1 min | catálogo, analytics |
| Operations | 40 req | 1 min | incidentes, automação |

### 5.4 Preflight Checks (10 checks)

| Check | Obrigatório | Validação |
|---|---|---|
| PostgreSQL | ✓ | Versão ≥ 15, conectividade |
| JWT Secret | ✓ | ≥ 32 chars, não placeholder |
| Connection Strings | ✓ | Pelo menos uma válida |
| Disk Space | — | Aviso se < 5 GB |
| RAM | — | Aviso se < 4 GB |
| Ports | — | Aviso se 8080/8090 ocupadas |
| Ollama | — | Aviso se não acessível |
| SMTP | — | Aviso se não configurado |
| OTel Collector | — | Aviso se não acessível |
| CORS Origins | — | Aviso se não configurado em Prod |

Endpoint: `GET /preflight` (sem autenticação)

### 5.5 Health Checks

| Endpoint | Tags | Uso |
|---|---|---|
| `GET /health` | health | Liveness |
| `GET /ready` | ready | Readiness (Kubernetes) |
| `GET /live` | live | Liveliness |
| `GET /preflight` | — | Diagnóstico pré-arranque |
| `GET /api/v1/platform/database-health` | — | Diagnóstico PostgreSQL (auth) |

22 DbContext health checks registados (um por contexto).

### 5.6 Database Migrations — 27 DbContexts em 7 Waves

| Wave | DbContexts |
|---|---|
| 1 — Foundation | ConfigurationDbContext, IdentityDbContext |
| 2 — Catalog | CatalogGraphDbContext, DeveloperPortalDbContext, ContractsDbContext, DeveloperExperienceDbContext, LegacyAssetsDbContext, TemplatesDbContext, DependencyGovernanceDbContext |
| 3 — Change & Ops | ChangeIntelligenceDbContext, RulesetGovernanceDbContext, WorkflowDbContext, PromotionDbContext, IncidentDbContext, RuntimeIntelligenceDbContext, CostIntelligenceDbContext, ReliabilityDbContext, AutomationDbContext, TelemetryStoreDbContext |
| 4 — Audit & Gov | AuditDbContext, GovernanceDbContext |
| 5 — Integrations | IntegrationsDbContext, ProductAnalyticsDbContext |
| 6 — Notifications | NotificationsDbContext, KnowledgeDbContext |
| 7 — AI | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext |

Auto-migrations: habilitadas em Development; bloqueadas em Production mesmo com `NEXTRACE_AUTO_MIGRATE=true`.

---

### 5.7 Background Workers — 13 Jobs Quartz.NET

| Job | Intervalo | Função |
|---|---|---|
| `OutboxProcessorJob` | 5s | Processa OutboxMessages do módulo Identity (batch 50) |
| `ModuleOutboxProcessorJob<TContext>` | 5s | Genérico para qualquer DbContext; `pg_try_advisory_lock` para multi-instância; Dead Letter após 5 falhas |
| `PlatformHealthMonitorJob` | 5min | Monitora saúde (Outbox, disco, jobs travados, DB pool, taxa de erro); alertas com cooldown 15min |
| `LicenseRecalculationJob` | 15min | Recalcula `CurrentHostUnits` de TenantLicense; omite updates < 0.1 HU |
| `IdentityExpirationJob` | 60s | Orquestra 5 handlers: Delegation, BreakGlass, JitAccess, AccessReview, EnvironmentAccess (batch 100) |
| `DriftDetectionJob` | Config. | Deteta drift entre snapshots e baseline de serviços |
| `WasteDetectionJob` | 24h | Deteta IdleResources, Overprovisioned, OrphanedResources; notifica owners |
| `CloudBillingIngestionJob` | 24h | Importa billing cloud (AWS, Azure, GCP) |
| `ContractConsumerIngestionJob` | Config. | Consome eventos de contratos de fonte externa |
| `IncidentProbabilityRefreshJob` | Config. | Atualiza probabilidades de incidentes |
| `BackupCoordinatorJob` | Config. | Coordena backups da plataforma |
| `ElasticsearchIndexMaintenanceJob` | Config. | Manutenção de índices Elasticsearch |
| `CarbonScoreCalculationJob` | Config. | Calcula scores de pegada de carbono |

Todos os jobs têm `WorkerJobHealthRegistry` (rastreia last started, last success, last failure, reason).

---

## 6. Building Blocks

### 6.1 Core

- **Result<T>** — `IsSuccess`, `Value`, `Error`; implicit conversions; `Map<TOut>()`, `OnSuccess()`, `OnFailure()`
- **Error** — `Code`, `Message`, `ErrorType` (None/NotFound/Validation/Conflict/Unauthorized/Forbidden/Security/Business), factory methods estáticos
- **AggregateRoot<TId>** — Lista interna de `IDomainEvent`; `RaiseDomainEvent()`, `ClearDomainEvents()`
- **TypedIdBase** — `record` com `Guid Value`; compile-error se usar Guid raw
- **AuditableEntity** — `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` (preenchidos pelo AuditInterceptor)
- **[EncryptedField]** — Atributo que aciona `EncryptedStringConverter` (AES-256-GCM) no EF Core

### 6.2 Application

**Interfaces CQRS:**
```csharp
ICommand<TResponse> : IRequest<Result<TResponse>>
ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
IQuery<TResponse> : IRequest<Result<TResponse>>
IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
```

**Abstrações:**
- `IDateTimeProvider.UtcNow` — testável (não usa `DateTime.UtcNow` diretamente)
- `ICurrentUser` — UserId, Email, Name, Permissions (resolvidos de ClaimsPrincipal)
- `ICurrentTenant` — Id (resolvido de header + JWT)
- `IEventBus.PublishAsync<TEvent>()` — in-process ou Outbox
- `IErrorLocalizer` — traduz Error.Code para mensagem localizada
- `PagedList<T>` — Items, PageNumber, PageSize, TotalCount, TotalPages, HasPreviousPage, HasNextPage

**Contexto distribuído:**
- `DistributedExecutionContext` — CorrelationId, UserId, TenantId, EnvironmentId
- Headers propagados: `X-Correlation-Id`, `X-Tenant-Id`, `X-Environment-Id`

### 6.3 Infrastructure

**NexTraceDbContextBase** — automaticamente:
- Converte domain events de `AggregateRoot<T>` para `OutboxMessage` no `SaveChanges`
- Aplica filtro global soft-delete em `AuditableEntity<T>` (`IsDeleted == false`)
- Aplica `[EncryptedField]` via `EncryptedStringConverter`

**TenantRlsInterceptor** — antes de cada SQL: `SELECT set_config('app.current_tenant_id', @id, false)` (session-scoped, parâmetro — sem SQL injection)

**AuditInterceptor** — popula `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` automaticamente

**OutboxMessage** — `Id`, `EventType` (fully qualified), `Payload` (JSON), `CreatedAt`, `ProcessedAt`, `RetryCount` (max 5), `LastError`

**RepositoryBase<TAggregate, TId>** — `GetByIdAsync()`, `ListAsync()`, `Add()`, `Update()`, `Delete()`

**Cache:** Redis (quando `ConnectionStrings:Redis` configurado) ou `DistributedMemoryCache` como fallback

### 6.4 Security

**JWT:**
- Subject = UserId; claims: email, name, tenant_id, permissions (multi-valor)
- Access token: 60min; Refresh: 7 dias; ClockSkew: 1min
- Signing Key obrigatório ≥ 32 chars; não aceita placeholder em non-Development

**API Key:**
- Header `X-Api-Key`; comparação em tempo constante (previne timing attacks)
- Suporta hash SHA-256 ou plain-text (legacy)

**Authorization:**
- `PermissionRequirement(string permission)` → `IAuthorizationRequirement`
- Formato de permissão: `"modulo:recurso:acao"` (ex: `"catalog:assets:write"`)
- `PermissionPolicyProvider` resolve dinamicamente qualquer código de permissão

**Encryption:**
- `AesGcmEncryptor` — AES-256-GCM; output: IV (16B) + ciphertext + tag (16B)

**Assembly Integrity:**
- `AssemblyIntegrityChecker.VerifyOrThrow()` — SHA-256 de assemblies vs manifest
- Desativável via `NEXTRACE_SKIP_INTEGRITY=true` (dev/CI apenas)

### 6.5 Observability

**Serilog sinks:** Console, File (rotação diária), Grafana Loki, OpenTelemetry, PostgreSQL

**OpenTelemetry instrumentações:** AspNetCore (requests), Http (outbound calls), Runtime (GC, memory, thread pool)

**Enrichers:** EnvironmentName, Version, CorrelationId, UserId, TenantId

---

## 7. Tools

### 7.1 SDK (NexTrace.Sdk) — net6.0

```csharp
var client = new NexTraceSdkClient(new NexTraceSdkOptions
{
    BaseUrl = "https://api.nextraceone.com",
    ApiToken = "...",
    TimeoutSeconds = 30
});

// Sub-clientes:
client.Services    // ServiceCatalogClient
client.Contracts   // ContractClient
client.Changes     // ChangeClient
client.Compliance  // ComplianceClient
```

### 7.2 CLI (`nex`) — .NET 10

```bash
nex compliance check --service api-payments
nex contract validate --spec openapi.yaml
nex contract publish --service api-payments --version 2.1.0
nex catalog list-services --team platform-core
nex change score --release-id <guid>
nex report generate --type dora --period 30d
nex scaffold new-service --template rest-api
nex mcp start          # Model Context Protocol server
nex completion bash    # Shell completion (bash/zsh/powershell)
```

---

## 8. Testes

### 8.1 Distribuição

| Tipo | Localização | Foco |
|---|---|---|
| Unit | `tests/modules/<m>/` | Handlers, agregados, validators |
| Integration | `tests/platform/NexTraceOne.IntegrationTests/` | Fluxos end-to-end com PostgreSQL real |
| E2E | `tests/platform/NexTraceOne.E2E.Tests/` | WebApplicationFactory + HTTP |
| Selenium | `tests/platform/NexTraceOne.Selenium.Tests/` | Navegação browser |
| Load | `tests/load-testing/` | k6 TypeScript |

### 8.2 Integration Tests — ApiHostPostgreSqlFixture

Cria 11 databases reais via Testcontainers, sobe WebApplicationFactory com migrations reais, seed de dados, retorna `HttpClient` autenticado.

Databases: `rh6_it_catalog`, `rh6_it_change_governance`, `rh6_it_identity`, `rh6_it_incidents`, `rh6_it_runtime`, `rh6_it_cost`, `rh6_it_aiknowledge`, `rh6_it_external_ai`, `rh6_it_ai_orchestration`, `rh6_it_governance`, `rh6_it_audit`

### 8.3 Padrão de Teste Unitário

```csharp
public class CreateIncidentTests
{
    private readonly IIncidentStore _store = Substitute.For<IIncidentStore>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly TestDateTimeProvider _clock = new();

    [Fact]
    public async Task Handle_ValidCommand_CreatesIncidentWithReference()
    {
        var handler = new CreateIncident.Handler(_store, _uow, _clock);
        var cmd = new CreateIncident.Command("API Timeout", "...", IncidentType.ServiceDegradation,
            IncidentSeverity.Major, "svc-payments", "Payments API", "platform-team", null, "production");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reference.Should().StartWith("INC-");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
```

### 8.4 Selenium — Fluxos de Navegação

`AdminNavigationTests`, `DashboardNavigationTests`, `CatalogNavigationTests`, `GovernanceNavigationTests`, `ChangesNavigationTests`, `OperationsNavigationTests`, `ContractsNavigationTests`, `AiHubNavigationTests`, `AuthNavigationTests`, `KnowledgeNavigationTests`, `FullSmokeNavigationTests`

---

## 9. Segurança

### 9.1 Camadas de Segurança (5 camadas)

1. **JWT + API Key** — autenticação dupla, comparação em tempo constante
2. **PostgreSQL RLS** — `set_config('app.current_tenant_id')` antes de cada query
3. **Filtro de repositório** — `.Where(e => e.TenantId == currentTenant.Id)` em defense-in-depth
4. **TenantIsolationBehavior** — MediatR pipeline valida request vs tenant autenticado
5. **EnvironmentAuthorizationMiddleware** — isolamento por ambiente

### 9.2 Dados em Repouso

- Campos sensíveis marcados com `[EncryptedField]` — AES-256-GCM automático via EF Core
- Audit event payloads sempre encriptados
- API Keys armazenadas como SHA-256 hash

### 9.3 Segurança em Movimento

- HTTPS obrigatório em non-Development
- Headers de segurança: CSP, HSTS, X-Frame-Options, X-Content-Type-Options
- CSRF: `X-Csrf-Token` validation + Cookie `SameSite=Strict`
- Rate limiting por IP (7 políticas)

### 9.4 Acesso Privilegiado

- **JIT Access** — acesso temporário (8h) com aprovação (4h prazo) e justificativa
- **Break Glass** — acesso emergencial imediato (2h), máx. 3/trimestre, post-mortem obrigatório
- **Assembly Integrity** — SHA-256 dos assemblies antes do startup

---

## 10. Multi-Tenancy e SaaS

### 10.1 Modelo de Tenancy

- **Hierarquia:** Organization → Subsidiary → Department (máx. 3 níveis)
- **Resolução:** Header `X-Tenant-Id` validado contra JWT `tenant_id` claim
- **Isolamento:** RLS PostgreSQL + filtro de repositório em defense-in-depth

### 10.2 Licenciamento

Planos: `Starter`, `Professional`, `Enterprise`, `Trial`

```csharp
// Verificação de capability num handler
if (!currentTenant.HasCapability("contract_studio"))
    return Error.Forbidden("CapabilityRequired",
        "Esta funcionalidade requer o plano Contract Studio.");
```

Capabilities embutidas no JWT — sem roundtrip ao banco por request. Trial inclui Professional + 4 features Enterprise como teaser.

### 10.3 Environments por Tenant

Cada tenant tem N ambientes (Development, Staging, Production, DisasterRecovery). Cada ambiente tem:
- `Profile` e `Criticality` (Low/Medium/High/Critical)
- `IsProductionLike` — comportamento como produção
- `IsPrimaryProduction` — único activo por tenant

---

## 11. Configuração e Deploy

### 11.1 Variáveis de Ambiente Críticas

| Variável | Obrigatório | Descrição |
|---|---|---|
| `Jwt__Secret` | ✓ | ≥ 32 chars, não placeholder em Prod |
| `ConnectionStrings__NexTraceOne` | ✓ | PostgreSQL connection string |
| `ASPNETCORE_ENVIRONMENT` | ✓ | Development / Staging / Production |
| `ConnectionStrings__Redis` | — | Redis; fallback in-memory se ausente |
| `Kafka__Enabled` | — | true/false; NullProducer se false |
| `ClickHouse__Enabled` | — | true/false; desabilitado por padrão |
| `NEXTRACE_SKIP_INTEGRITY` | — | true em dev/CI para saltar hash check |
| `NEXTRACE_AUTO_MIGRATE` | — | true para auto-migrations (bloqueado em Prod) |
| `NEXTRACE_IGNORE_PENDING_MODEL_CHANGES` | — | Suprime aviso de migrations pendentes |
| `NEXTRACEONE_CONNECTION_STRING` | — | Override para EF design-time factories |

### 11.2 Dockerfiles Disponíveis

`Dockerfile.apihost`, `Dockerfile.workers`, `Dockerfile.ingestion`, `Dockerfile.frontend`, `Dockerfile.kubernetes`

`docker-compose.yml`, `docker-compose.production.yml`, `docker-compose.staging.yml`, `docker-compose.override.yml`

---

## 12. Estatísticas do Produto

| Métrica | Valor |
|---|---|
| Módulos DDD | 12 |
| Entidades de Domínio | 296+ |
| Features (Commands + Queries) | 400+ |
| Endpoints HTTP | 300+ |
| Repositórios (interfaces) | 200+ |
| DbContexts | 27 |
| Tabelas no banco | 350+ |
| Migrações EF Core | 154+ |
| Background Jobs | 13 |
| Testes totais | 2000+ |
| Building Block projects | 5 |
| Integration Events | 30+ |
| Rate Limiting policies | 7 |
| Preflight checks | 10 |
| Middleware pipeline steps | 15 |

---

## 13. Pontos Positivos

### Arquitetura

- **Modularidade genuína:** 12 módulos sem DbContext partilhado; comunicação exclusivamente via `IXxxModule` ou Integration Events pelo Outbox — acoplamento mínimo real, não apenas nominal.
- **CQRS consistente:** Feature como classe estática (`Command + Validator + Response + Handler` num único ficheiro) em 400+ features sem desvios.
- **Pipeline MediatR correto:** Ordem de behaviors garante que validação falha antes de atingir o handler, tenant está sempre verificado, e transações fecham automaticamente.
- **Outbox Pattern sólido:** Eventos de domínio persistidos atomicamente no `SaveChanges`; `pg_advisory_lock` para segurança multi-instância; Dead Letter Queue após 5 falhas.
- **Result<T> pattern:** Sem exceções para fluxo de negócio; mapeamento automático para HTTP; handlers legíveis.

### Segurança

- **5 camadas de isolamento de tenant** — nenhuma query passa sem o `tenant_id` correto em pelo menos 2 níveis.
- **AES-256-GCM transparente** via `[EncryptedField]` — zero código adicional nos handlers para encriptar campos.
- **Verificação de integridade de assembly** no startup — proteção contra substituição de DLLs em produção.
- **Rate limiting granular** por contexto (auth vs AI vs data vs operations) — não apenas rate limiting global.
- **JIT + Break Glass** com auditoria completa, pós-mortem obrigatório e limites trimestrais.

### Observabilidade

- OpenTelemetry end-to-end com traces, metrics e logs correlacionados pelo mesmo `CorrelationId`.
- 22 health checks de DbContext + Preflight com 10 verificações antes do startup aceitar tráfego.
- `WorkerJobHealthRegistry` em cada job — visibilidade de last success, last failure, reason.

### Extensibilidade

- **Null Object Pattern** correto em todos os providers opcionais — o sistema arranca e funciona sem Redis, Kafka, ClickHouse, SAML ou qualquer provider externo.
- **Capabilities por tenant** no JWT — extensão de planos sem roundtrip ao banco.
- **27 DbContexts em 7 waves** — dependências de migration explicitamente ordenadas.

### Qualidade

- 2000+ testes com Testcontainers (PostgreSQL real em integration tests), não apenas in-memory.
- `TestCurrentTenant` e `TestDateTimeProvider` como doubles partilhados.
- `GlobalUsings.cs` por projeto de teste evita imports repetidos.

---

## 14. Pontos Negativos

### Complexidade Operacional

- **27 DbContexts** e **350+ tabelas** numa única base de dados exigem DBA experiente; monitorar migrations e índices em escala é não-trivial.
- **13 jobs Quartz** precisam de monitoramento ativo — falha silenciosa no `OutboxProcessorJob` pode acumular eventos sem entrega.
- Stack opcional volumosa: Kafka, ClickHouse, Elasticsearch, Qdrant, Redis, Loki, OTel Collector — mesmo sendo opcional, operar todos em produção aumenta o surface de falha.

### Gaps Conhecidos

| Gap | Impacto | Esforço |
|---|---|---|
| **Email não enviado** — tokens de ativação/reset gerados mas SMTP não entrega por email | Bloqueante para onboarding SaaS | 6–8h |
| **Contract Pipeline inconsistente** — 3 features leem dados do request JSON em vez do banco | Inconsistência de estado possível | 4–6h |

### Acoplamento ao PostgreSQL

- `pg_try_advisory_lock`, `SET app.current_tenant_id`, `xmin` concurrency — migrar para outro SGBD exigiria refatoração significativa nos building blocks.

### Overhead de Camadas em Módulos Pequenos

- `Knowledge` (5 tabelas, 18 features) e `ProductAnalytics` (2 entidades, 15 features) têm a mesma estrutura de 5 projetos que `ChangeGovernance` (130+ features, 60 tabelas). Overhead considerável para módulos simples.

### Semantic Kernel em Versão 1.x

- `Microsoft.SemanticKernel 1.76.0` ainda tem API instável. Atualizações futuras podem exigir refatoração no módulo AI Knowledge.

### Frontend Ausente

- Produto é backend puro — `Dockerfile.frontend` existe mas não há UI no repositório. Qualquer adoção requer desenvolvimento de frontend separado ou integração com ferramentas como Backstage.

### Distinção Null Pattern Não Evidente no Código

- `NullXxxReader` (placeholder legítimo para fase seguinte) vs `NullXxxRepository` (seria um bug) — distinção documentada apenas no CLAUDE.md, não no código. Risco de confusão em equipas novas.

---

## 15. Roadmap Identificado

### Curto Prazo (< 2 semanas)
- Corrigir envio de email de ativação/reset (6–8h)
- Corrigir contract pipeline inconsistente (4–6h)

### Médio Prazo (6–12 meses)
- Kubernetes Helm Charts para deploy cloud-native
- CLI `nex` distribuível como binário independente
- Assinatura de assembly com certificados x.509
- Agentes de IA especializados (120–150h)

### Longo Prazo (12+ meses)
- ClickHouse como store analítico principal (desativado por padrão atualmente)
- NLP-based model routing (seleção inteligente de provider)
- Suporte a mainframe legado (CICS, COBOL, JCL)
- Dashboards 3D e visualizações interativas de grafos de serviços
- Frontend nativo (React/Vue)

---

*Análise gerada em 2026-05-17 a partir de exploração exaustiva do codebase por agentes especializados.*
