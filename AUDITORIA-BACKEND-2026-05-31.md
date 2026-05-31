# Auditoria Detalhada do Backend — NexTraceOne

**Data:** 2026-05-31  
**Escopo:** Todo o backend .NET 10 (building-blocks, 12 modulos, 3 hosts, testes)  
**Referencia:** `CLAUDE.md` — premissas arquiteturais e padroes do projeto  
**Status do Build:** ❌ 1 erro + 2.457 avisos  

---

## Indice

1. [Resumo Executivo](#1-resumo-executivo)
2. [Bugs Criticos](#2-bugs-criticos)
3. [Bugs e Inconsistencias — Alto](#3-bugs-e-inconsistencias--alto)
4. [Funcionalidades Incompletas / Stubs](#4-funcionalidades-incompletas--stubs)
5. [Mudancas Cirurgicas Recomendadas](#5-mudancas-cirurgicas-recomendadas)
6. [Anexos por Area](#6-anexos-por-area)

---

## 1. Resumo Executivo

| Metrica | Valor |
|---------|-------|
| Modulos analisados | 12 |
| DbContexts | 28 (CLAUDE.md cita 24) |
| Endpoints REST/GraphQL | 1.118 |
| Handlers CQRS | 1.334 |
| Handlers "mortos" (sem endpoint) | **294** |
| Orphan endpoints (sem handler) | **1** |
| Null*Repository com impl. real (BUG) | **11** |
| DbContexts com pending model changes | **5** |
| Erros de build | **1** (testes) |
| Warnings de build | 2.457 (maioria CS8632 em testes) |

**Modulos mais saudaveis:** `auditcompliance`, `configuration` (0 handlers mortos, 0 HTTP issues).  
**Modulos com maior divida tecnica:** `catalog` (104 handlers mortos), `changegovernance` (65), `operationalintelligence` (58).

---

## 2. Bugs Criticos

### BC-01 — Pipeline MediatR em ordem incorreta
**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Application/DependencyInjection.cs`  
**Problema:** A ordem real de registro dos behaviors e `Validation -> ContextualLogging -> Logging -> Performance -> TenantIsolation -> Transaction`. O `CLAUDE.md` Parte 7 documenta: `Logging -> Performance -> TenantIsolation -> Validation -> Transaction`.  
**Impacto:** Requests invalidos nao geram logs de contexto nem metricas de performance. A validacao roda antes do tenant isolation, o que pode expor detalhes de validacao para requests sem tenant.  
**Acao:** Reordenar o registro no DI para alinhar com a documentacao.

### BC-02 — Null*Repository registrados no DI (violacao CLAUDE.md §21)
**Arquivos:**
- `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/DependencyInjection.cs:178`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/DependencyInjection.cs:159`

**Problema:** `NullFeatureFlagRepository` e `NullModelPredictionRepository` sao registrados como **Singleton** na camada Application, e depois a camada Infrastructure os sobrescreve com `EfXxxRepository` como **Scoped**.  
**Impacto:**
1. Em testes que chamam apenas `AddXxxApplication()`, o Null sera resolvido — dados silenciosamente descartados.
2. `IEnumerable<IFeatureFlagRepository>` retorna ambas as implementacoes.
3. Violacao explicita da regra de ouro do CLAUDE.md.
**Acao:** Remover os registros de Null*Repository do DI. Remover as classes se nao sao mais necessarias.

### AI-01 — OpenAILLMProvider e stub completo
**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Services/AIAgents/OpenAILLMProvider.cs`  
**Problema:** `GenerateAsync()` retorna string fake `"[STUB] Response from {_model}..."`.  
**Impacto:** Qualquer roteamento para OpenAI (fallback quando Ollama falha) retorna lixo ao usuario. A feature AI Knowledge perde o provedor de fallback.
**Acao:** Implementar chamada real a API OpenAI ou desabilitar o provider e ajustar o routing para nao cair nele.

### AI-02 — IntelligentRouter completamente vazio
**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Features/NLPRouting/Services/IntelligentRouter.cs`  
**Problema:** Classe vazia — apenas comentario `// TODO: implementar classificacao de intencao com embeddings leves`.  
**Impacto:** O sistema nao faz routing inteligente de queries NLP. Todo o AI Hub depende deste componente para classificacao de intencao.
**Acao:** Implementar ou remover a feature do pipeline.

### AI-03 — PromptRouter retorna valores hardcoded
**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Features/NLPRouting/PromptRouting/PromptRouter.cs:61`  
**Problema:** `ConfidenceScore: 0.85`, `SelectedProvider: "gpt-3.5-turbo"`, scores fixos. O TODO confirma: `"Implementar logica real de routing com ML.NET"`.  
**Impacto:** Decisao de roteamento e fake — nao analisa o prompt. Custo e qualidade das respostas sao imprevisiveis.
**Acao:** Implementar logica de routing real ou usar heuristica simples baseada em configuracao.

### CAT-01 — ContractPipeline gera stubs genericos que ignoram o contrato real
**Arquivos:**
- `src/modules/catalog/NexTraceOne.Catalog.Application/Portal/ContractPipeline/Features/GenerateServerFromContract/GenerateServerFromContract.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Application/Portal/ContractPipeline/Features/GenerateClientSdkFromContract/GenerateClientSdkFromContract.cs`

**Problema:** Ambos geram templates CRUD fixos (`/api/v1/resource`) com `object` como tipo de DTO, ignorando `paths`, `schemas`, `parameters` e `responses` do spec OpenAPI.  
**Impacto:** O Developer Portal e Contract Studio (capability Enterprise) geram codigo inutil. Usuario precisa reescrever 100% do codigo gerado.
**Acao:** Marcar como `PREVIEW` nao funcional ou implementar parsing real do OpenAPI spec.

### DB-01 — 5 DbContexts com pending model changes
**DbContexts afetados:** `CatalogGraphDbContext`, `ContractsDbContext`, `AiGovernanceDbContext`, `AiOrchestrationDbContext`, `ProductAnalyticsDbContext`  
**Problema:** O schema do banco pode divergir do modelo EF Core em runtime, causando `InvalidOperationException` ou queries que retornam dados incorretos.  
**Acao:** Gerar migrations pendentes para os 5 DbContexts.

### DB-02 — Connection string `TelemetryStoreDatabase` ausente
**Arquivos:** `src/platform/NexTraceOne.ApiHost/appsettings.json`, `appsettings.Development.json`  
**Problema:** `TelemetryStoreDbContext` busca `TelemetryStoreDatabase`, mas a connection string nao existe. O fallback `"NexTraceOne"` mascara o problema.  
**Impacto:** Em producao, se `TelemetryStoreDatabase` precisar de um PostgreSQL isolado, a configuracao nao esta preparada.
**Acao:** Adicionar `"TelemetryStoreDatabase"` aos `appsettings*.json`.

### API-01 — GET mutativo em Operational Intelligence
**Endpoint:** `GET /api/v1/operational-intelligence/profiles`  
**Problema:** GET dispara `CreateServiceCostProfile` (Command). Viola semantica REST e pode criar efeitos colaterais inesperados em caches e proxies.
**Acao:** Mudar para `POST` ou tornar o handler idempotente via `UpsertServiceCostProfile`.

### TEST-01 — Erro de compilacao em testes
**Arquivo:** `tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/Governance/Application/Features/SendAssistantMessageLlmTests.cs:147`  
**Problema:** `CS7036` — falta argumento `logger` no construtor de `SendAssistantMessage.Handler`.  
**Impacto:** Build da solucao falha quando testes sao incluidos. CI/CD pode quebrar.
**Acao:** Adicionar `Substitute.For<ILogger<SendAssistantMessage.Handler>>()` ao construtor do handler no teste.

---

## 3. Bugs e Inconsistencias — Alto

### BC-03 — `EncryptionInterceptor` nao existe
**Arquivo:** `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Persistence/NexTraceDbContextBase.cs`  
**Problema:** O XML doc menciona `EncryptionInterceptor`, mas a encriptacao e feita via `ApplyEncryptedFieldConvention` + `EncryptedStringConverter`.  
**Acao:** Atualizar XML doc para remover referencia ao interceptor inexistente.

### BC-04 — `NexTraceDbContextBase.WriteDomainEventsToOutbox()` usa reflection fragil
**Problema:** Usa `GetProperty("DomainEvents")` e `GetMethod("ClearDomainEvents")` por nome. Se renomear, quebra silenciosamente.  
**Acao:** Considerar interface `IHasDomainEvents` ou metodo virtual.

### BC-05 — `TransactionBehavior` nao protege contra duplo commit
**Problema:** Se um handler chamar `unitOfWork.CommitAsync()` manualmente (proibido mas nao enforceado), o behavior chama novamente.  
**Acao:** Adicionar flag `_commitCalled` no behavior ou no `NexTraceDbContextBase`.

### BC-06 — `TenantRlsInterceptor` nao trata `Guid.Empty` para background jobs
**Problema:** Em jobs sem tenant, nao executa `set_config`, mas a conexao pooled pode reter o tenant_id da sessao anterior.  
**Acao:** Executar `set_config('app.current_tenant_id', '', false)` explicitamente para resetar.

### BC-07 — `PerformanceBehavior` nao trata `OperationCanceledException`
**Problema:** Diferente de `LoggingBehavior`, que trata corretamente.  
**Acao:** Adicionar catch de `OperationCanceledException`.

### CAT-02 — 104 handlers mortos em Catalog
**Problema:** Handlers implementados mas sem endpoints expostos. Exemplos: `CreateComplianceGate`, `EvaluateContractCompliance`, `GetContractHealthScore`, `GetCriticalPathReport`, etc.  
**Acao:** Ou expor endpoints, ou remover handlers nao utilizados (codigo morto).

### CHG-01 — 65 handlers mortos em ChangeGovernance
**Exemplos:** `AttachSlsaProvenance`, `GetBlastRadiusDistributionReport`, `GetChangeLeadTimeReport`, `CreatePromotionGate`, etc.  
**Acao:** Mesmo que CAT-02.

### OPI-01 — 58 handlers mortos em OperationalIntelligence
**Exemplos:** `GetFinOpsInsights`, `GenerateHealingRecommendation`, `DetectEnvironmentDrift`, `ExecutePlaybook`, etc.  
**Acao:** Mesmo que CAT-02.

### GOV-01 — Orphan endpoint `GET /{dashboardId}/live`
**Arquivo:** `src/modules/governance/NexTraceOne.Governance.API/Endpoints/DashboardsAndDebtEndpointModule.cs`  
**Problema:** Usa metodos estaticos (`GenerateEventsAsync`, `ToSseFrame`) diretamente, sem padrao Command/Query+Handler.  
**Acao:** Refatorar para handler MediatR ou documentar excecao arquitetural.

### GOV-02 — Dashboard widgets placeholder
**Arquivo:** `src/modules/governance/NexTraceOne.Governance.Application/Features/GetDashboardBatchQuery/GetDashboardBatchQuery.cs:97`  
**Problema:** Para widgets que nao sao `"otel-metrics"`, retorna objeto placeholder generico.  
**Acao:** Implementar resolvers para os demais tipos de widget ou retornar `Error.NotFound`.

### AIK-01 — `QdrantVectorStoreRepository.DeleteAsync()` e stub
**Arquivo:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/VectorStore/QdrantVectorStoreRepository.cs:67`  
**Problema:** Apenas loga e retorna `Task.CompletedTask`. Vetores orfaos acumulam.  
**Acao:** Implementar delete real no Qdrant.

### INT-01 — `NullKafkaEventProducer` descarta eventos silenciosamente
**Arquivo:** `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Kafka/NullKafkaEventProducer.cs`  
**Problema:** Eventos sao descartados sem aviso quando Kafka nao esta configurado. Apenas log Debug.  
**Acao:** Adicionar log Warning quando evento e descartado e `Kafka:Enabled=false`.

### DB-03 — Migrations vazias anti-padrao
**Arquivos:**
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Migrations/FixPendingModelChanges.cs`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Cost/Persistence/Migrations/SyncModelSnapshot.cs`

**Problema:** Migrations vazias criadas para suprimir warnings. Poluem o historico.  
**Acao:** Remover migrations vazias e gerar migrations reais se houver pending changes.

---

## 4. Funcionalidades Incompletas / Stubs

### AI — NLP Routing
| Componente | Status |
|------------|--------|
| `IntelligentRouter` | ❌ Vazio — TODO |
| `PromptRouter` | ⚠️ Hardcoded — TODO ML.NET |
| `OpenAILLMProvider` | ❌ Stub — TODO SDK real |

### Catalog — Contract Pipeline
| Componente | Status |
|------------|--------|
| `GenerateServerFromContract` | ⚠️ PREVIEW — stubs genericos |
| `GenerateClientSdkFromContract` | ⚠️ PREVIEW — stubs genericos |
| `GenerateMockServer` | ⚠️ Respostas mock fixas |
| `GenerateMigrationPatch` | ⚠️ Gera TODOs no codigo de saida |

### Multi-Tenancy
| Componente | Status |
|------------|--------|
| `TenantSchemaManager` | ✅ Implementado, ❌ Nao utilizado (RLS e a estrategia real) |

### Kafka
| Componente | Status |
|------------|--------|
| `NullKafkaEventProducer` | Padrao — eventos descartados silenciosamente |
| `ConfluentKafkaEventProducer` | ✅ Implementado, mas requer `Kafka:Enabled=true` |

### Quartz.NET
| Componente | Status |
|------------|--------|
| Referenciado no csproj | ✅ |
| Jobs usando Quartz | ❌ Zero — todos usam `BackgroundService` + `PeriodicTimer` |

---

## 5. Mudancas Cirurgicas Recomendadas

Respeitando `CLAUDE.md` §1.3 (toque apenas o necessario), as correcoes devem ser feitas em PRs separados por dominio:

### PR #1 — Correcoes de Build & Testes
1. Corrigir `SendAssistantMessageLlmTests.cs` (adicionar `ILogger` mock).
2. Corrigir warnings CS8632 em testes (adicionar `#nullable enable` nos arquivos afetados ou remover anotacoes `?` em contextos sem nullable).

### PR #2 — Building Blocks (Pipeline + Infra)
1. Reordenar behaviors no DI para: `Logging -> Performance -> TenantIsolation -> Validation -> Transaction`.
2. Remover registros de `NullFeatureFlagRepository` e `NullModelPredictionRepository` dos DIs.
3. Corrigir XML doc de `NexTraceDbContextBase` (remover `EncryptionInterceptor`).
4. Adicionar `OperationCanceledException` em `PerformanceBehavior`.
5. Resetar RLS tenant em `TenantRlsInterceptor` quando `Guid.Empty`.

### PR #3 — Repositorios Nulos (Bugs)
1. Remover os 11 arquivos `Null*Repository` que possuem implementacao EF Core real.
2. Remover registros dessas classes nos DIs de Application.

### PR #4 — DbContexts & Migrations
1. Gerar migrations pendentes para os 5 DbContexts.
2. Adicionar `"TelemetryStoreDatabase"` aos `appsettings*.json`.
3. Remover migrations vazias de `IdentityDbContext` e `CostIntelligenceDbContext`.

### PR #5 — Background Workers
1. Adicionar `ModuleOutboxProcessorJob<DeveloperExperienceDbContext>` ao `BackgroundWorkers/Program.cs`.
2. Referenciar o projeto `DeveloperExperience` no csproj dos BackgroundWorkers.
3. Remover `OutboxProcessorJob.cs` (legado nao utilizado).
4. Avaliar remocao de `EventConsumerWorker.cs` (codigo morto).

### PR #6 — AI Knowledge (Stubs)
1. Implementar `OpenAILLMProvider` com chamada real HTTP a API OpenAI.
2. Implementar `IntelligentRouter` com classificacao simples (embeddings ou keyword matching).
3. Implementar `PromptRouter` com heuristica de configuracao (nao precisa ser ML.NET imediatamente).
4. Implementar `QdrantVectorStoreRepository.DeleteAsync()`.

### PR #7 — API Consistencia
1. Corrigir `GET /profiles` para `POST`.
2. Corrigir 14 endpoints que usam `POST` com `Query` (avaliar se e intencional).
3. Decidir o que fazer com os 294 handlers mortos (expor, mover para backlog, ou remover).

---

## 6. Anexos por Area

### 6.1 Building Blocks

| Componente | Status | Problema |
|------------|--------|----------|
| `LoggingBehavior` | ✅ OK | — |
| `PerformanceBehavior` | 🟡 Parcial | Falta `OperationCanceledException` |
| `TenantIsolationBehavior` | ✅ OK | — |
| `ValidationBehavior` | ✅ OK | — |
| `TransactionBehavior` | 🟡 Parcial | Nao protege duplo commit |
| `ContextualLoggingBehavior` | ✅ OK | Extra, nao documentado no CLAUDE.md |
| `Result<T>` | ✅ OK | — |
| `Error` / `ErrorType` | ✅ OK | — |
| `NexTraceDbContextBase` | 🟡 Parcial | Reflection fragil, XML doc incorreto |
| `AuditInterceptor` | ✅ OK | — |
| `TenantRlsInterceptor` | 🟡 Parcial | Nao reseta tenant em jobs |
| `ModuleOutboxProcessorJob` | ✅ OK | Completo com DLQ e advisory locks |
| `IDeadLetterRepository` | ✅ OK | Implementado e funcional |

### 6.2 Repositorios Nulos (Null*Repository) — BUGs

| # | Interface | Arquivo Null | Arquivo Real |
|---|-----------|-------------|--------------|
| 1 | `IAgentExecutionPlanRepository` | `aiknowledge/.../NullAgentExecutionPlanRepository.cs` | `EfAgentExecutionPlanRepository.cs` |
| 2 | `IAiAnalyticsRepository` | `aiknowledge/.../NullAiAnalyticsRepository.cs` | `ClickHouseAiAnalyticsRepository.cs` |
| 3 | `IAiSearchRepository` | `aiknowledge/.../NullAiSearchRepository.cs` | `ElasticSearchAiRepository.cs` |
| 4 | `IDataContractRepository` | `catalog/.../NullDataContractRepository.cs` | `EfDataContractRepository.cs` |
| 5 | `IDeprecationScheduleRepository` | `catalog/.../NullDeprecationScheduleRepository.cs` | `EfDeprecationScheduleRepository.cs` |
| 6 | `IEventConsumerDeadLetterRepository` | `integrations/.../NullEventConsumerDeadLetterRepository.cs` | `EfEventConsumerDeadLetterRepository.cs` |
| 7 | `IFeatureFlagRepository` | `catalog/.../NullFeatureFlagRepository.cs` | `EfFeatureFlagRepository.cs` |
| 8 | `IModelPredictionRepository` | `aiknowledge/.../NullModelPredictionRepository.cs` | `EfModelPredictionRepository.cs` |
| 9 | `IModelRoutingPolicyRepository` | `aiknowledge/.../NullModelRoutingPolicyRepository.cs` | `EfModelRoutingPolicyRepository.cs` |
| 10 | `ISbomRepository` | `catalog/.../NullSbomRepository.cs` | `EfSbomRepository.cs` |
| 11 | `IVectorStoreRepository` | `aiknowledge/.../NullVectorStoreRepository.cs` | `QdrantVectorStoreRepository.cs` |

> **Nota:** Os itens 7 e 8 sao registrados explicitamente no DI — sao os mais perigosos.

### 6.3 Endpoints vs Handlers

| Modulo | Endpoints | Handlers | Mortos | Orphan | HTTP Issues |
|--------|-----------|----------|--------|--------|-------------|
| aiknowledge | 192 | 204 | 18 | 0 | 3 |
| auditcompliance | 28 | 26 | 0 | 0 | 0 |
| catalog | 226 | 326 | **104** | 0 | 5 |
| changegovernance | 114 | 173 | **65** | 0 | 0 |
| configuration | 66 | 66 | 0 | 0 | 0 |
| governance | 207 | 190 | 20 | **1** | 3 |
| identityaccess | 77 | 86 | 13 | 0 | 1 |
| integrations | 24 | 26 | 2 | 0 | 0 |
| knowledge | 15 | 23 | 10 | 0 | 0 |
| notifications | 22 | 23 | 2 | 0 | 0 |
| operationalintelligence | 133 | 172 | **58** | 0 | **3** |
| productanalytics | 16 | 17 | 2 | 0 | 0 |
| **TOTAL** | **1.118** | **1.334** | **294** | **1** | **15** |

### 6.4 DbContexts & Migrations

| DbContext | Migrations | Pending Changes | Status |
|-----------|-----------|-----------------|--------|
| ExternalAiDbContext | ✅ | ✅ | 🟢 OK |
| AiGovernanceDbContext | ✅ | ❌ | 🚨 PENDING |
| AiOrchestrationDbContext | ✅ | ❌ | 🚨 PENDING |
| AuditDbContext | ✅ | ✅ | 🟢 OK |
| ContractsDbContext | ✅ | ❌ | 🚨 PENDING |
| DependencyGovernanceDbContext | ✅ | ✅ | 🟢 OK |
| DeveloperExperienceDbContext | ✅ | ✅ | 🟢 OK |
| CatalogGraphDbContext | ✅ | ❌ | 🚨 PENDING |
| LegacyAssetsDbContext | ✅ | ✅ | 🟢 OK |
| DeveloperPortalDbContext | ✅ | ✅ | 🟢 OK |
| TemplatesDbContext | ✅ | ✅ | 🟢 OK |
| ChangeIntelligenceDbContext | ✅ | ✅ | 🟢 OK |
| PromotionDbContext | ✅ | ✅ | 🟢 OK |
| RulesetGovernanceDbContext | ✅ | ✅ | 🟢 OK |
| WorkflowDbContext | ✅ | ✅ | 🟢 OK |
| ConfigurationDbContext | ✅ | ✅ | 🟢 OK |
| GovernanceDbContext | ✅ | ✅ | 🟢 OK |
| IdentityDbContext | ✅* | ✅ | 🟡 OK (migracao vazia) |
| IntegrationsDbContext | ✅ | ✅ | 🟢 OK |
| KnowledgeDbContext | ✅ | ✅ | 🟢 OK |
| NotificationsDbContext | ✅ | ✅ | 🟢 OK |
| AutomationDbContext | ✅ | ✅ | 🟢 OK |
| CostIntelligenceDbContext | ✅* | ✅ | 🟡 OK (migracao vazia) |
| IncidentDbContext | ✅ | ✅ | 🟢 OK |
| ReliabilityDbContext | ✅ | ✅ | 🟢 OK |
| RuntimeIntelligenceDbContext | ✅ | ✅ | 🟢 OK |
| TelemetryStoreDbContext | ✅ | ✅ | 🟡 OK (falta conn string) |
| ProductAnalyticsDbContext | ✅ | ❌ | 🚨 PENDING |

### 6.5 Background Workers & Jobs

| Job | Registrado | Health Check | Status |
|-----|-----------|--------------|--------|
| `LicenseRecalculationJob` | ✅ | ✅ | OK |
| `AlertEvaluationJob` | ✅ (indireto) | ❌ | OK, sem health check |
| `ModuleOutboxProcessorJob<T>` | ✅ (25/28) | ✅ (25/28) | Falta `DeveloperExperienceDbContext` |
| `BackupCoordinatorJob` | ✅ | ✅ | OK |
| `CarbonScoreCalculationJob` | ✅ | ✅ | OK |
| `CloudBillingIngestionJob` | ✅ | ✅ | OK |
| `ContractConsumerIngestionJob` | ✅ | ✅ | OK |
| `DriftDetectionJob` | ✅ | ✅ | OK |
| `ElasticsearchIndexMaintenanceJob` | ✅ | ✅ | OK |
| `IdentityExpirationJob` | ✅ | ✅ | OK |
| `IncidentProbabilityRefreshJob` | ✅ | ✅ | OK |
| `OtelCatalogBridgeJob` | ✅ | ✅ | OK |
| `PlatformHealthMonitorJob` | ✅ | ✅ | OK |
| `WasteDetectionJob` | ✅ | ✅ | OK |
| `EventConsumerWorker` | ❌ | ❌ | Codigo morto |
| `OutboxProcessorJob` (legado) | ❌ | ❌ | Codigo legado — substituido |

### 6.6 Testes

| Problema | Quantidade |
|----------|------------|
| Erros de compilacao | **1** (`SendAssistantMessageLlmTests.cs`) |
| Warnings CS8632 (nullable em testes sem `#nullable`) | ~40 |
| Projetos de teste existentes | 24+ |
| Modulos sem projeto de teste | 0 (todos têm) |

---

*Relatorio gerado automaticamente via analise de codebase em 2026-05-31.*
