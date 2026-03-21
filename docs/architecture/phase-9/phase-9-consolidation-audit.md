# Phase 9 — Consolidation & Adherence Audit

**Produto:** NexTraceOne  
**Fase:** 9 — Consolidation, Adherence Audit & 100% Validation  
**Data:** 2026-03-21  
**Auditor:** Principal Software Architect (AI-assisted)  
**Repositório:** `/home/runner/work/NexTraceOne/NexTraceOne`

---

## Metodologia

- Leitura directa de todos os ficheiros fonte relevantes nas 8 áreas auditadas.
- Verificação de presença e conteúdo de ficheiros críticos.
- Execução de `dotnet build` e `dotnet test` para apurar estado real de build e testes.
- Tentativa de execução de `npx vitest run` no frontend (falhou por dependências não instaladas no CI runner).
- Cada área é evidenciada com caminhos de ficheiro exactos.
- Não se assume conformidade sem prova directa no código.

---

## Estado do Build e Testes

| Artefacto | Resultado | Detalhes |
|---|---|---|
| `dotnet build NexTraceOne.sln` | ✅ **Build com sucesso** | 849 warnings, **0 errors** |
| `dotnet test NexTraceOne.sln` | ⚠️ **Falhas parciais** | Integration Tests: 8 falhas (PostgreSQL/DB). E2E Tests: 8 falhas (HTTP 500 em endpoint de Incidents). Unit tests: todos passaram. |
| `npx vitest run` (frontend) | ❌ **Não executou** | `vitest` não está instalado no runner. Dependências ausentes. |

---

## Área 1 — Domain Model

### Scope
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| `Environment` entity pertence a Tenant | `IdentityAccess.Domain/Entities/Environment.cs` | ✅ Conforme | `public TenantId TenantId { get; private set; }` — strongly typed, guard clause presente |
| `EnvironmentProfile` enum (não-fixo) | `IdentityAccess.Domain/Enums/EnvironmentProfile.cs` | ✅ Conforme | 9 valores: Development, Validation, Staging, Production, Sandbox, DisasterRecovery, Training, UserAcceptanceTesting, PerformanceTesting |
| `TenantEnvironmentContext` VO | `IdentityAccess.Domain/ValueObjects/TenantEnvironmentContext.cs` | ✅ Conforme | Encapsula TenantId + EnvironmentId + Profile + Criticality + IsProductionLike. Métodos `AllowsDeepAiAnalysis()`, `IsPreProductionCandidate()` |
| `AiExecutionContext` | `AIKnowledge.Domain/Orchestration/Context/AiExecutionContext.cs` | ✅ Conforme | ValueObject com TenantId, EnvironmentId, EnvironmentProfile, IsProductionLikeEnvironment, AllowedDataScopes |
| `PromotionRiskAnalysisContext` | `AIKnowledge.Domain/Orchestration/Context/PromotionRiskAnalysisContext.cs` | ✅ Conforme | Contém SourceEnvironmentId, TargetEnvironmentId, SourceProfile, TargetProfile, ServiceName |
| `ReadinessAssessment` | `AIKnowledge.Domain/Orchestration/Context/ReadinessAssessment.cs` | ✅ Conforme | AssessmentId, TenantId, SourceEnvironmentId, TargetEnvironmentId, ServiceName, Version |
| `EnvironmentComparisonContext` | `AIKnowledge.Domain/Orchestration/Context/EnvironmentComparisonContext.cs` | ✅ Conforme | Contexto adicional para comparação entre ambientes |
| `RegressionSignal`, `RiskFinding` | `AIKnowledge.Domain/Orchestration/Context/` | ✅ Conforme | Value Objects de suporte à análise |
| Ausência de hardcodes enum fixos no Domain | Todo domain | ✅ Conforme | Sem referências a "DEV", "PRE", "PROD" como constantes fixas no código de domínio |
| **Profile fields persistidos na BD** | `IdentityAccess.Infrastructure/Persistence/Configurations/EnvironmentConfiguration.cs` | ❌ **GAP CRÍTICO** | `builder.Ignore(x => x.Profile)`, `builder.Ignore(x => x.Code)`, `builder.Ignore(x => x.Description)`, `builder.Ignore(x => x.Criticality)`, `builder.Ignore(x => x.Region)`, `builder.Ignore(x => x.IsProductionLike)` — campos do domain **não estão mapeados para a BD**. Migração `AddEnvironmentProfileFields` **não existe**. |

### Notas
O domínio está arquitecturalmente correcto. O gap crítico é que os campos `Profile`, `Criticality`, `Code`, `Description`, `Region`, `IsProductionLike` existem na entidade domain mas estão explicitamente ignorados no EF Core config com um comentário `// Phase 1 fields — deferred to migration AddEnvironmentProfileFields (Phase 2)`. A migração referenciada **nunca foi criada**.

---

## Área 2 — Context & Authorization

### Scope
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Context/`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Context/`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Security/Authorization/`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| `EnvironmentResolutionMiddleware` lê `X-Environment-Id` | `IdentityAccess.Infrastructure/Context/EnvironmentResolutionMiddleware.cs` | ✅ Conforme | `public const string EnvironmentIdHeader = "X-Environment-Id"`. Lê header e query string. Valida ambiente pertence ao tenant activo. |
| `EnvironmentContextAccessor` | `IdentityAccess.Infrastructure/Context/EnvironmentContextAccessor.cs` | ✅ Conforme | Ficheiro existe e implementado |
| `TenantEnvironmentContextResolver` | `IdentityAccess.Infrastructure/Context/TenantEnvironmentContextResolver.cs` | ✅ Conforme | Ficheiro existe |
| `IOperationalExecutionContext` | `IdentityAccess.Application/Abstractions/IOperationalExecutionContext.cs` | ✅ Conforme | Interface encontrada |
| `EnvironmentAccessValidator` | `IdentityAccess.Infrastructure/Context/EnvironmentAccessValidator.cs` | ✅ Conforme | Ficheiro existe |
| `EnvironmentAccessRequirement` + `OperationalContextRequirement` | `BuildingBlocks.Security/Authorization/EnvironmentAccessRequirement.cs` | ✅ Conforme | `OperationalContextRequirement.Instance` — exige TenantId+EnvironmentId+User |
| `OperationalContextAuthorizationHandler` | `IdentityAccess.Infrastructure/Authorization/OperationalContextAuthorizationHandler.cs` | ✅ Conforme | Handler de autorização específico para contexto operacional |
| `GET /api/v1/identity/context/runtime` | `IdentityAccess.API/Endpoints/Endpoints/RuntimeContextEndpoints.cs` | ✅ Conforme | Endpoint existe, retorna RuntimeUserDto + RuntimeTenantDto + RuntimeEnvironmentDto com perfil e IsProductionLike |

### Notas
A camada de contexto e autorização está completa e correcta. Middleware, accessors, resolvers e handlers de autorização estão todos implementados. O endpoint `/context/runtime` expõe o contexto completo ao frontend.

---

## Área 3 — Data & Persistence

### Scope
- `src/modules/identityaccess/.../Persistence/Migrations/`
- `src/modules/operationalintelligence/.../Incidents/`
- `src/modules/changegovernance/.../`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| Migração `AddEnvironmentProfileFields` | `IdentityAccess.Infrastructure/Persistence/Migrations/` | ❌ **AUSENTE** | Apenas 2 migrações existem: `20260313210303_InitialIdentitySchema` e `20260320131347_AddIncidentPermissionsSeed`. Profile, Criticality, Code, Region, IsProductionLike **não estão na BD**. |
| `IncidentRecord` tem `TenantId?` | `OperationalIntelligence.Domain/Incidents/Entities/IncidentRecord.cs` | ✅ Parcial | `public Guid? TenantId { get; private set; }` (linha 141). Migração inicial inclui coluna `TenantId`. |
| `IncidentRecord` tem `EnvironmentId?` | Mesmo ficheiro | ⚠️ **GAP** | `public Guid? EnvironmentId { get; private set; }` (linha 147) existe no domain. Mas migração `20260317161138_InitialIncidentsSchema` não foi verificada como contendo `environment_id`. A coluna `TenantId` aparece (linha 181 da migração) mas `EnvironmentId` não foi encontrada na migração. |
| `Release` tem `TenantId?` e `EnvironmentId?` | `ChangeGovernance.Infrastructure/.../Migrations/20260320220001_AddTenantContextToReleases.cs` | ✅ Conforme | Migração adiciona colunas `tenant_id` e `environment_id` na tabela `ci_releases`, com índices compostos. |
| Release domain entity tem `TenantId/EnvironmentId` | `ChangeGovernance.Domain/.../Entities/Release.cs` | ⚠️ **GAP Parcial** | Entidade domain tem `Environment` como string (linha: `public string Environment { get; private set; }`). TenantId/EnvironmentId existem como nullable Guid via migração mas a entidade domain usa string para `Environment`. Inconsistência domain ↔ BD. |
| FK constraints para TenantId → Environments | Migrations | ⚠️ **Ausente** | Não foi encontrada FK de `ci_releases.tenant_id` para a tabela de tenants. Colunas adicionadas sem FK formal. |
| Queries respeitam escopo tenant+environment | Repositories | ⚠️ **Parcial** | Repositories do ChangeGovernance foram encontrados mas não verificada presença de filtro composto tenant+environment em todas as queries. |

### Notas
O gap mais crítico desta área é a ausência da migração `AddEnvironmentProfileFields`. Os campos de perfil do Environment estão ignorados no EF config e não chegam à BD. Todas as funcionalidades de IA que dependem de `IsProductionLike` ou `Profile` lidas da BD estão a funcionar sem dados persistidos.

---

## Área 4 — Backend Modular (AI Orchestration)

### Scope
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.API/Orchestration/`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| Endpoint `POST /api/v1/aiorchestration/analysis/non-prod` | `AiOrchestrationEndpointModule.cs` | ✅ Conforme | Mapeado com `RequirePermission("ai:runtime:write")` |
| Endpoint `POST /api/v1/aiorchestration/analysis/compare-environments` | Mesmo ficheiro | ✅ Conforme | Mapeado e autenticado |
| Endpoint `POST /api/v1/aiorchestration/analysis/promotion-readiness` | Mesmo ficheiro | ✅ Conforme | Mapeado e autenticado |
| `AnalyzeNonProdEnvironment` handler com TenantId+EnvironmentId | `Orchestration/Features/AnalyzeNonProdEnvironment/AnalyzeNonProdEnvironment.cs` | ✅ Conforme | `Guard.Against.NullOrWhiteSpace(request.TenantId)`, `Guard.Against.NullOrWhiteSpace(request.EnvironmentId)`. FluentValidation presente. |
| `CompareEnvironments` handler valida mesmo tenant | `Orchestration/Features/CompareEnvironments/CompareEnvironments.cs` | ✅ Conforme | `Guard.Against.NullOrWhiteSpace(request.TenantId)`. Validator: `SubjectEnvironmentId != ReferenceEnvironmentId`. Grounding inclui "Both environments belong to the same tenant." |
| `AssessPromotionReadiness` handler valida Source≠Target | `Orchestration/Features/AssessPromotionReadiness/AssessPromotionReadiness.cs` | ✅ Conforme | Validator: `SourceEnvironmentId != TargetEnvironmentId` |
| Sem nomes de ambiente hardcoded nos handlers | Todos os handlers | ✅ Conforme | Handlers recebem names/profiles como parâmetros, nunca comparam com strings fixas |
| **Validação server-side: AnalyzeNonProd rejeita ambiente de produção** | `AnalyzeNonProdEnvironment.cs` | ❌ **GAP CRÍTICO** | Handler não valida que `EnvironmentProfile` passado corresponde a um ambiente não-produtivo. Aceita qualquer string incluindo "production". A validação é operacional e não estrutural. |
| **Validação server-side: AssessPromotionReadiness source=non-prod** | `AssessPromotionReadiness.cs` | ❌ **GAP** | Handler não valida que source é non-prod e target é prod-like. Aceita qualquer combinação de ambientes. |
| **DB-level tenant validation para CompareEnvironments** | Handler | ⚠️ **Parcial** | Tenant isolation é garantida apenas pelo contexto passado na command. Não há lookup na BD para confirmar que os dois EnvironmentIds pertencem ao TenantId indicado. |

---

## Área 5 — Distributed Context (Telemetry & Integrations)

### Scope
- `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Context/`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Correlation/`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Integrations/`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Observability/Telemetry/`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| `ContextPropagationHeaders` (X-Tenant-Id, X-Environment-Id) | `Application/Context/ContextPropagationHeaders.cs` | ✅ Conforme | `TenantId = "X-Tenant-Id"`, `EnvironmentId = "X-Environment-Id"`. Array `PropagatedHeaders` inclui ambos. |
| `DistributedExecutionContext` | `Application/Context/DistributedExecutionContext.cs` | ✅ Conforme | Record imutável com TenantId?, EnvironmentId?, CorrelationId, UserId. Factory method `From(ICurrentTenant, ICurrentEnvironment)`. |
| `ContextualLoggingBehavior` (MediatR pipeline) | `Application/Behaviors/ContextualLoggingBehavior.cs` | ✅ Conforme | Ficheiro existe no pipeline MediatR |
| `IIntegrationContextResolver` | `Application/Integrations/IIntegrationContextResolver.cs` | ✅ Conforme | Interface presente com NullImplementation |
| `IDistributedSignalCorrelationService` | `Application/Correlation/IDistributedSignalCorrelationService.cs` | ✅ Conforme | Interface presente com NullImplementation |
| `TelemetryContextEnricher` com tags `nexttrace.*` | `Observability/Telemetry/TelemetryContextEnricher.cs` | ✅ Conforme | Tags: `nexttrace.tenant_id`, `nexttrace.environment_id`, `nexttrace.environment.is_production_like`, `nexttrace.correlation_id`, `nexttrace.service_origin`, `nexttrace.user_id` |
| `IntegrationEventBase` tem `TenantId?` e `EnvironmentId?` | `BuildingBlocks.Core/Events/IntegrationEventBase.cs` | ✅ Conforme | Propriedades nullable `TenantId` e `EnvironmentId` presentes com documentação de uso |
| Headers nexttrace.* vs X-* | — | ℹ️ **Nota** | Spec menciona `nexttrace.tenant-id` como header HTTP mas a implementação usa `X-Tenant-Id`. Os `nexttrace.*` são usados como atributos OpenTelemetry (tags de spans), não como headers HTTP. Isto é arquitecturalmente correcto. |

---

## Área 6 — Frontend Context

### Scope
- `src/frontend/src/contexts/EnvironmentContext.tsx`
- `src/frontend/src/utils/tokenStorage.ts`
- `src/frontend/src/components/shell/EnvironmentBanner.tsx`
- `src/frontend/src/components/shell/WorkspaceSwitcher.tsx`
- `src/frontend/src/api/client.ts`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| `EnvironmentContext.tsx` | `src/contexts/EnvironmentContext.tsx` | ✅ Conforme | Context com `EnvironmentProfile` type, `activeEnvironment`, `availableEnvironments`, `selectEnvironment` |
| `tokenStorage.ts` com EnvironmentId | `src/utils/tokenStorage.ts` | ✅ Conforme | `storeEnvironmentId`, `getEnvironmentId`, key `nxt_eid` em sessionStorage |
| `EnvironmentBanner.tsx` | `src/components/shell/EnvironmentBanner.tsx` | ✅ Conforme | Exibe apenas para `!activeEnvironment.isProductionLike`. i18n aplicado. `role="status"` |
| `WorkspaceSwitcher.tsx` | `src/components/shell/WorkspaceSwitcher.tsx` | ✅ Conforme | Usa `useEnvironment()`, `getProfileBadgeClass` com 6 perfis dinâmicos (não hardcoded enum fixo) |
| API client injecta `X-Environment-Id` | `src/api/client.ts` | ✅ Conforme | `config.headers['X-Environment-Id'] = environmentId` no interceptor de request |
| Sem hardcoded "DEV", "PRE", "PROD" em ReleasesPage | `features/change-governance/pages/ReleasesPage.tsx` | ✅ Conforme | Grep não encontrou strings hardcoded de ambiente |
| **Mock em vez de API real para ambientes** | `src/contexts/EnvironmentContext.tsx` | ❌ **GAP** | Função `loadEnvironmentsForTenant` é um mock com comentário `// TODO Phase 7: Replace with real API call to GET /api/v1/identity/environments?tenantId=X`. Frontend não consulta a API real. |
| **Ambientes mock com estrutura semi-fixa** | Mesmo ficheiro | ⚠️ **Parcial** | Mock retorna sempre Production+Staging+QA+Development para qualquer tenant. Não reflecte ambientes reais do backend. |
| **AiAnalysisPage valida isProductionLike** | `features/ai-hub/pages/AiAnalysisPage.tsx` | ✅ Conforme | Verifica `isProductionLike` e exibe mensagem se ambiente for produção |

---

## Área 7 — AI Module (CRÍTICO)

### Scope
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Context/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| `AIContextBuilder` com TenantId+EnvironmentId | `Infrastructure/Context/AIContextBuilder.cs` | ✅ Conforme | Resolve TenantId, EnvironmentId, Profile via `ICurrentTenant`, `IEnvironmentContextAccessor`, `ITenantEnvironmentContextResolver`. Determina `AllowedDataScopes` com base no perfil. |
| `PromotionRiskContextBuilder` | `Infrastructure/Context/PromotionRiskContextBuilder.cs` | ✅ Conforme | Implementa `IPromotionRiskContextBuilder` |
| `AnalyzeNonProdEnvironment` handler existe | `Application/Orchestration/Features/AnalyzeNonProdEnvironment/AnalyzeNonProdEnvironment.cs` | ✅ Conforme | Handler completo com Command, Validator, Handler, Response |
| `CompareEnvironments` valida mesmo tenant | `Application/Orchestration/Features/CompareEnvironments/CompareEnvironments.cs` | ✅ Parcial | TenantId obrigatório e incluído em grounding. Mas sem DB lookup para validar environments pertencem ao tenant. |
| `AssessPromotionReadiness` valida source≠target | `Application/Orchestration/Features/AssessPromotionReadiness/AssessPromotionReadiness.cs` | ✅ Parcial | Validator garante `SourceEnvironmentId != TargetEnvironmentId` |
| **AnalyzeNonProd valida que ambiente é não-produtivo** | `AnalyzeNonProdEnvironment.cs` | ❌ **GAP CRÍTICO** | Handler NÃO valida que `EnvironmentProfile` passado é de um ambiente não produtivo. Sem lista de profiles proibidos (ex: `Production`, `DisasterRecovery`). Risco: análise pode ser feita em ambiente produtivo sem bloqueio. |
| **AssessPromotionReadiness valida source=non-prod, target=prod-like** | `AssessPromotionReadiness.cs` | ❌ **GAP** | Handler não valida que source é non-prod e target é prod-like. Validator só verifica `SourceEnvironmentId != TargetEnvironmentId`. |
| **Isolamento cross-tenant via DB** | Todos os handlers | ⚠️ **Parcial** | Isolamento garantido via `TenantId` no grounding context e na response, mas não há lookup de BD para confirmar que `EnvironmentId` pertence ao `TenantId`. |
| **Fail-safe em provider indisponível** | Todos os handlers | ✅ Conforme | try/catch com `logger.LogWarning` e `return Error.Business("AIKnowledge.Provider.Unavailable", ...)` |
| **CorrelationId único por execução** | Todos os handlers | ✅ Conforme | `var correlationId = Guid.NewGuid().ToString()` no início de cada `Handle` |
| **TenantId e EnvironmentId na response** | Todos os handlers | ✅ Conforme | Incluídos explicitamente em todas as Response records |

---

## Área 8 — Tests

### Scope
- `tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/Orchestration/Features/`
- `src/frontend/src/__tests__/pages/`

### Findings

| Componente | Ficheiro | Status | Evidência |
|---|---|---|---|
| `AiAnalysisContextIsolationTests.cs` | `Orchestration/Features/AiAnalysisContextIsolationTests.cs` | ✅ Conforme | **17 testes** cobrindo: isolamento de tenant no grounding, correlationId único, propagação de EnvironmentId, same-tenant isolation em CompareEnvironments, fail-safe com provider indisponível |
| `AiAnalysisNonProdScenarioTests.cs` | `Orchestration/Features/AiAnalysisNonProdScenarioTests.cs` | ✅ Conforme | **8 testes** cobrindo: QA risk analysis com contract drift, UAT vs PROD comparison, promoção bloqueada, assessment de readiness |
| `AnalyzeNonProdEnvironmentTests.cs` | `Orchestration/Features/AnalyzeNonProdEnvironmentTests.cs` | ✅ Conforme | Testes adicionais específicos para o handler |
| `CompareEnvironmentsTests.cs` | `Orchestration/Features/CompareEnvironmentsTests.cs` | ✅ Conforme | Testes para comparação de ambientes |
| `AssessPromotionReadinessTests.cs` | `Orchestration/Features/AssessPromotionReadinessTests.cs` | ✅ Conforme | Testes de readiness |
| `AiAnalysisPageHardening.test.tsx` | `src/frontend/src/__tests__/pages/AiAnalysisPageHardening.test.tsx` | ✅ Conforme | Ficheiro existe com **12 testes** de hardening. Verifica bloqueio em ambiente produção, propagação de TenantId/EnvironmentId |
| **Teste: AnalyzeNonProd rejeita ambiente de produção** | — | ❌ **AUSENTE** | Nenhum teste verifica que o handler rejeita quando `EnvironmentProfile = "production"` |
| **Teste: AssessPromotionReadiness valida source=non-prod** | — | ❌ **AUSENTE** | Nenhum teste verifica que handler rejeita quando source é prod-like |
| **Testes de integração: falhas** | `IntegrationTests.dll` | ❌ **8 falhas** | PostgreSQL/DB persistence issues em `CriticalFlowsPostgreSqlTests` |
| **Testes E2E: falhas** | `E2E.Tests.dll` | ❌ **8 falhas** | `Incidents_GetById_With_Unknown_Id_Should_Return_404` recebe HTTP 500 em vez de 404/400/403 |
| **Frontend tests** | `__tests__/` | ❌ **Não executaram** | `vitest` não instalado no runner CI. 42 ficheiros de teste existem mas não foram executados. |

---

## Sumário Estatístico

| Área | Conformes | Parciais | Gaps | Status |
|---|---|---|---|---|
| 1. Domain Model | 8 | 0 | 1 (crítico) | ⚠️ Parcial |
| 2. Context & Authorization | 8 | 0 | 0 | ✅ Conforme |
| 3. Data & Persistence | 2 | 2 | 2 (1 crítico) | ❌ Gap |
| 4. Backend Modular | 5 | 1 | 2 (1 crítico) | ⚠️ Parcial |
| 5. Distributed Context | 8 | 0 | 0 | ✅ Conforme |
| 6. Frontend Context | 6 | 1 | 1 | ⚠️ Parcial |
| 7. AI Module | 5 | 2 | 2 (1 crítico) | ⚠️ Parcial |
| 8. Tests | 6 | 0 | 4 | ⚠️ Parcial |
| **Total** | **48** | **6** | **12** | **⚠️ Aprovado com ressalvas** |

### Gaps Críticos (bloqueadores de 100%)
1. Migração `AddEnvironmentProfileFields` ausente → Profile/Criticality/IsProductionLike não persistidos na BD
2. `AnalyzeNonProdEnvironment` handler não valida server-side que o ambiente é não-produtivo
3. Frontend usa mock de ambientes em vez de API real

### Gaps Importantes (não-bloqueadores imediatos)
4. `AssessPromotionReadiness` não valida source=non-prod/target=prod-like
5. `IncidentRecord.EnvironmentId` em DB não confirmado
6. Release domain entity usa `Environment` como string (sem FK strongly typed)
7. Cross-tenant validation apenas em grounding, sem DB lookup
8. 8 falhas em Integration Tests (DB)
9. 8 falhas em E2E Tests (HTTP 500 em Incidents)
10. Frontend tests não executaram
