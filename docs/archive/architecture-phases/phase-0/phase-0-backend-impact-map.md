# Phase 0 — Backend Impact Map

**Data:** 2026-03-20  
**Scope:** Inventário de impacto da refatoração TenantId + EnvironmentId no backend

Legenda:  
- ✅ Já usa corretamente  
- ⚠️ Usa mas de forma incompleta ou frágil  
- ❌ Não usa / ausente  
- 🔴 Impacto crítico  
- 🟠 Impacto alto  
- 🟡 Impacto médio  
- 🟢 Impacto baixo

---

## 1. BuildingBlocks

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `ICurrentTenant` | BuildingBlocks.Application.Abstractions | ✅ | ❌ | Baixo | Adicionar `ICurrentEnvironment` interface | 2 |
| `TenantResolutionMiddleware` | BuildingBlocks.Security.MultiTenancy | ✅ | ❌ | Médio | Criar `EnvironmentResolutionMiddleware` paralelo | 2 |
| `TenantIsolationBehavior` | BuildingBlocks.Application.Behaviors | ✅ | ❌ | Baixo | Criar `EnvironmentContextBehavior` opcional | 2 |
| `TenantRlsInterceptor` | BuildingBlocks.Infrastructure.Interceptors | ✅ | ❌ | Baixo | Avaliar se RLS por ambiente é necessário | 2 |
| `AuditInterceptor` | BuildingBlocks.Infrastructure.Interceptors | ⚠️ | ❌ | Médio | Adicionar EnvironmentId ao audit trail | 4 |
| Observability Models (`AnomalySnapshot`, `InvestigationContext`, etc.) | BuildingBlocks.Observability.Telemetry.Models | ⚠️ `Guid?` | ⚠️ `string` | 🟠 Alto | Tornar `TenantId` required; trocar `Environment: string` por `EnvironmentId: Guid` | 5 |
| `IProductStore` (IObservedTopologyReader, etc.) | BuildingBlocks.Observability.Telemetry.Abstractions | ⚠️ Ausente na assinatura | ⚠️ `string environment` | 🟠 Alto | Adicionar `tenantId` obrigatório e `EnvironmentId` tipado às assinaturas | 5 |
| `IMetricsStore` | BuildingBlocks.Observability.Telemetry.Abstractions | ❌ | ⚠️ `string environment` | 🟠 Alto | Adicionar `tenantId` e trocar `string` por `EnvironmentId` | 5 |

---

## 2. IdentityAccess Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `Environment` entity | IdentityAccess.Domain.Entities | ✅ `TenantId` | ✅ `EnvironmentId` (é o Id) | Baixo | Possivelmente tornar-se o modelo canônico unificado | 1 |
| `EnvironmentAccess` entity | IdentityAccess.Domain.Entities | ✅ | ✅ | Baixo | Sem impacto imediato | — |
| `IEnvironmentRepository` | IdentityAccess.Application.Abstractions | ✅ `ListByTenantAsync` | ✅ | Baixo | Adicionar método `GetBySlugAsync(TenantId, string slug)` | 2 |
| `GrantEnvironmentAccess` handler | IdentityAccess.Application.Features | ✅ | ✅ | Baixo | Sem impacto imediato | — |
| `GetCurrentUser` handler | IdentityAccess.Application.Features | ✅ | ❌ | Médio | Incluir lista de ambientes do usuário na resposta | 4 |
| All tenant/membership handlers | IdentityAccess.Application.Features | ✅ | ✅/❌ | Baixo | Minimal impact | — |

---

## 3. Catalog Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `ApiAsset` aggregate | Catalog.Domain.Graph.Entities | ❌ | ❌ | 🔴 Crítico | Adicionar `TenantId` — breaking change em persistência | 1 |
| `ServiceAsset` entity | Catalog.Domain.Graph.Entities | ❌ | ❌ | 🔴 Crítico | Adicionar `TenantId` — core do catálogo global | 1 |
| `ConsumerRelationship` entity | Catalog.Domain.Graph.Entities | ❌ | ⚠️ `consumerEnvironment: string` | 🟠 Alto | Adicionar `TenantId`; trocar string por `EnvironmentId` | 1 |
| `NodeHealthRecord` entity | Catalog.Domain.Graph.Entities | ❌ | ❌ | 🟡 Médio | Adicionar `TenantId` + `EnvironmentId` | 1 |
| `GraphSnapshot` entity | Catalog.Domain.Graph.Entities | ❌ | ❌ | 🟡 Médio | Adicionar `TenantId` | 1 |
| `ContractVersion` aggregate | Catalog.Domain.Contracts.Entities | ❌ | ❌ | 🔴 Crítico | Adicionar `TenantId` — contrato pertence ao tenant | 1 |
| `ContractDiff`, `ContractArtifact`, etc. | Catalog.Domain.Contracts.Entities | ❌ | ❌ | 🟡 Médio | Herdam scope via ContractVersion | 1 |
| `PlaygroundSession` aggregate | Catalog.Domain.Portal.Entities | ❌ | ⚠️ `environment?: string` | 🟡 Médio | Adicionar `TenantId` + `EnvironmentId` tipado | 1 |
| `Subscription` aggregate | Catalog.Domain.Portal.Entities | ❌ | ❌ | 🟡 Médio | Adicionar `TenantId` | 1 |
| `LinkedReference` entity | Catalog.Domain.SourceOfTruth.Entities | ❌ | ❌ | 🟡 Médio | Adicionar `TenantId` | 1 |
| Catalog repositories (Graph, Contracts, Portal) | Catalog.Infrastructure | ❌ | ❌ | 🔴 Crítico | Atualizar queries para filtrar por `TenantId` | 3 |
| Catalog application handlers | Catalog.Application | ❌ | ❌ | 🔴 Crítico | Extrair `TenantId` de `ICurrentTenant` em todos os handlers | 4 |
| Catalog API endpoints | Catalog.API | ❌ | ❌ | 🟠 Alto | Remover `tenantId` do body; usar contexto autenticado | 4 |

---

## 4. ChangeGovernance Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `Release` aggregate | ChangeGovernance.Domain.ChangeIntelligence.Entities | ❌ | ⚠️ `Environment: string` | 🔴 Crítico | Adicionar `TenantId`; substituir `string` por `EnvironmentId` | 1 |
| `ReleaseBaseline` entity | ChangeGovernance.Domain.ChangeIntelligence.Entities | ❌ | ⚠️ Provável via Release | 🟠 Alto | Verificar e adicionar `TenantId` | 1 |
| `DeploymentEnvironment` aggregate | ChangeGovernance.Domain.Promotion.Entities | ❌ | ✅ (é o Id) | 🔴 Crítico | Adicionar `TenantId` — ambiente de pipeline é tenant-scoped | 1 |
| `PromotionRequest` aggregate | ChangeGovernance.Domain.Promotion.Entities | ❌ | ✅ `SourceEnvironmentId`, `TargetEnvironmentId` | 🔴 Crítico | Adicionar `TenantId`; validar que ambos environments pertencem ao mesmo tenant | 1 |
| `PromotionGate` entity | ChangeGovernance.Domain.Promotion.Entities | ❌ | ✅ via `DeploymentEnvironmentId` | 🟠 Alto | Validar tenant consistency via DeploymentEnvironment | 1 |
| `CreatePromotionRequest` handler | ChangeGovernance.Application.Promotion.Features | ❌ | ✅ usa IDs | 🔴 Crítico | Adicionar validação de tenant-ownership dos environments | 4 |
| `ConfigureEnvironment` handler | ChangeGovernance.Application.Promotion.Features | ❌ | ✅ | 🟠 Alto | Adicionar `TenantId` ao criar DeploymentEnvironment | 4 |
| `EvaluatePromotionGates` handler | ChangeGovernance.Application.Promotion.Features | ❌ | ✅ | 🟡 Médio | Validar que gates pertencem ao tenant ativo | 4 |
| `ListChanges` query | ChangeGovernance.Application.ChangeIntelligence.Features | ❌ | ⚠️ `string? environment` | 🔴 Crítico | Filtrar por `TenantId` + `EnvironmentId` | 4 |
| `ListReleases` query | ChangeGovernance.Application.ChangeIntelligence.Features | ❌ | ❌ | 🔴 Crítico | Filtrar por `TenantId` | 4 |
| Release repositories | ChangeGovernance.Infrastructure | ❌ | ⚠️ | 🔴 Crítico | Adicionar filtro por `TenantId` em todas as queries | 3 |
| Promotion repositories | ChangeGovernance.Infrastructure | ❌ | ✅ | 🟠 Alto | Adicionar filtro por `TenantId` | 3 |
| Change Intelligence API endpoints | ChangeGovernance.API | ❌ | ⚠️ `string? environment` | 🔴 Crítico | Usar `ICurrentTenant` + `ICurrentEnvironment` | 4 |
| Promotion API endpoints | ChangeGovernance.API | ❌ | ✅ IDs | 🟠 Alto | Adicionar validação de tenant ownership | 4 |

---

## 5. OperationalIntelligence Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `IncidentRecord` aggregate | OI.Domain.Incidents.Entities | ❌ | ⚠️ `Environment: string` | 🔴 Crítico | Adicionar `TenantId`; trocar `string` por `EnvironmentId` | 1 |
| `MitigationWorkflowRecord` entity | OI.Domain.Incidents.Entities | ❌ | ❌ | 🟠 Alto | Adicionar `TenantId` (herdado de IncidentRecord) | 1 |
| `RunbookRecord` entity | OI.Domain.Incidents.Entities | ❌ | ❌ | 🟠 Alto | Adicionar `TenantId` + `EnvironmentId` (runbooks são ambiente-específicos) | 1 |
| `ServiceCostProfile` entity | OI.Domain.Cost.Entities | ❌ | ❌ | 🟠 Alto | Adicionar `TenantId` — custo é tenant-scoped | 1 |
| `CostAttribution` entity | OI.Domain.Cost.Entities | ❌ | ❌ | 🟠 Alto | Adicionar `TenantId` | 1 |
| `AutomationActionCatalog` | OI.Application.Automation | ❌ | 🔴 Hardcoded `"Staging"`, `"Production"`, `"Development"` | 🔴 Crítico | Substituir por perfil operacional por tenant/ambiente | 4 |
| `CreateIncident` handler | OI.Application.Incidents.Features | ❌ | ⚠️ | 🔴 Crítico | Extrair `TenantId` de `ICurrentTenant`; aceitar `EnvironmentId` | 4 |
| `ListIncidents` query | OI.Application.Incidents.Features | ❌ | ⚠️ `string? environment` | 🔴 Crítico | Filtrar por `TenantId` + `EnvironmentId` | 4 |
| Incident correlation service | OI.Application.Incidents.Services | ❌ | ⚠️ | 🟠 Alto | Correlação deve ser scoped por tenant | 4 |
| `InMemoryIncidentStore` | OI.Infrastructure.Incidents | ❌ | 🔴 Hardcoded `"Production"`, `"Staging"` | 🔴 Crítico | Substituir seed data hardcoded | 3 |
| `IncidentSeedData` | OI.Infrastructure.Incidents.Persistence | ❌ | 🔴 Hardcoded `"Production"` | 🟠 Alto | Seed data deve usar ambientes do tenant | 3 |
| OI repositories | OI.Infrastructure | ❌ | ⚠️ | 🔴 Crítico | Filtrar por `TenantId` em todas as queries | 3 |

---

## 6. AIKnowledge Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `AiTokenUsageLedger` entity | AIKnowledge.Domain.Governance.Entities | ⚠️ `string TenantId` | ❌ | 🟡 Médio | Trocar `string` por `TenantId` (strongly typed) | 1 |
| `AiExternalInferenceRecord` entity | AIKnowledge.Domain.Governance.Entities | ⚠️ `string TenantId` | ❌ | 🟡 Médio | Trocar `string` por `TenantId` (strongly typed) | 1 |
| `AiAssistantConversation` entity | AIKnowledge.Domain.Governance | ⚠️ Provável string | ❌ | 🟡 Médio | Verificar e tipificar TenantId | 1 |
| `/api/v1/ai/chat` endpoint | AIKnowledge.API.Runtime.Endpoints | ⚠️ `body.TenantId` opcional | ❌ | 🔴 Crítico | Remover do body; extrair de `ICurrentTenant` + adicionar `EnvironmentId` | 4 |
| `ExecuteAiChat` command | AIKnowledge.Application.Runtime.Features | ⚠️ Sem TenantId | ❌ | 🔴 Crítico | Injetar `TenantId` + `EnvironmentId` do contexto | 4 |
| `SearchData` feature | AIKnowledge.Application.Runtime.Features | ⚠️ `string? TenantId` opcional | ❌ | 🟠 Alto | Tornar obrigatório via contexto; adicionar `EnvironmentId` | 4 |
| `GetTokenUsage` feature | AIKnowledge.Application.Runtime.Features | ⚠️ `string? TenantId` opcional | ❌ | 🟡 Médio | Sempre extrair de `ICurrentTenant` | 4 |
| `AiTokenQuotaService` | AIKnowledge.Infrastructure.Runtime.Services | ⚠️ `string tenantId` | ❌ | 🟡 Médio | Usar strongly typed + adicionar filtro de ambiente | 4 |
| ExternalAI features | AIKnowledge.Application.ExternalAI.Features | ❌ TODO stubs | ❌ | 🟡 Médio | Implementar com TenantId + EnvironmentId desde o início | 7 |
| Orchestration features | AIKnowledge.Application.Orchestration.Features | ❌ TODO stubs | ❌ | 🟡 Médio | Implementar com TenantId + EnvironmentId desde o início | 7 |

---

## 7. Governance Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `GetPlatformReadiness` handler | Governance.Application.Features | ❌ | 🔴 `?? "Production"` hardcode | 🔴 Crítico | Remover hardcode; usar ambiente real do tenant | 4 |
| `GetPlatformConfig` handler | Governance.Application.Features | ❌ | 🔴 `?? "Production"` hardcode | 🔴 Crítico | Remover hardcode; usar ambiente real do tenant | 4 |
| `ListIntegrationConnectors` handler | Governance.Application.Features | ❌ | 🔴 `Environment: "Production"` hardcode | 🔴 Crítico | Remover TODO hardcode | 4 |
| `GetIntegrationConnector` handler | Governance.Application.Features | ❌ | 🔴 `Environment: "Production"` hardcode | 🔴 Crítico | Remover TODO hardcode | 4 |
| `GetPackApplicability` handler | Governance.Application.Features | ❌ | 🔴 `GovernanceScopeType.Environment, "Production"` hardcode | 🔴 Crítico | Substituir por EnvironmentId dinâmico | 4 |
| Governance Infrastructure | Governance.Infrastructure | ❌ | ❌ | 🔴 Crítico | Infrastructure completamente vazia — implementar persistence com TenantId | 3 |
| Governance domain entities | Governance.Domain.Entities | ❌ | ❌ | 🟠 Alto | 9 entidades sem TenantId, sem persistência real | 1 |

---

## 8. AuditCompliance Module

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `AuditEvent` entity | AuditCompliance.Domain.Entities | ✅ `Guid TenantId` | ❌ | 🟡 Médio | Adicionar `EnvironmentId` para audit por ambiente | 1 |
| `RecordAuditEvent` handler | AuditCompliance.Application.Features | ✅ | ❌ | 🟡 Médio | Adicionar `EnvironmentId` ao comando | 4 |
| `IAuditModule` contract | AuditCompliance.Contracts | ✅ | ❌ | 🟡 Médio | Adicionar `EnvironmentId` ao contrato de integração | 4 |

---

## 9. Platform — Ingestion API

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| Ingestion endpoints (deployment events, promotions) | NexTraceOne.Ingestion.Api | ⚠️ Via ApiKey `TenantId` | ⚠️ `environment: string` no body | 🔴 Crítico | Resolver tenant via ApiKey claim; aceitar `EnvironmentId` nos payloads | 4 |
| ApiKey claim `tenant_id` | Ingestion.Api.Program | ⚠️ Validado como Guid mas string | ❌ | 🟠 Alto | Validar que TenantId pertence a tenant ativo | 4 |

---

## 10. Platform — BackgroundWorkers

| Item | Localização | TenantId? | EnvironmentId? | Risco Atual | Impacto Refactor | Fase |
|------|-------------|-----------|----------------|-------------|-----------------|------|
| `EnvironmentAccessExpirationHandler` | BackgroundWorkers.Jobs.ExpirationHandlers | ✅ `access.TenantId` | ✅ `EnvironmentId` via EnvironmentAccess | Baixo | Sem impacto imediato | — |
| `BreakGlassExpirationHandler` | BackgroundWorkers.Jobs.ExpirationHandlers | ✅ `request.TenantId` | ❌ | 🟡 Médio | Sem impacto imediato | — |
| `DelegationExpirationHandler` | BackgroundWorkers.Jobs.ExpirationHandlers | ✅ `delegation.TenantId` | ❌ | 🟢 Baixo | Sem impacto imediato | — |
| `JitAccessExpirationHandler` | BackgroundWorkers.Jobs.ExpirationHandlers | ✅ `request.TenantId` | ❌ | 🟢 Baixo | Sem impacto imediato | — |
| `AccessReviewExpirationHandler` | BackgroundWorkers.Jobs.ExpirationHandlers | ✅ `campaign.TenantId` | ❌ | 🟢 Baixo | Sem impacto imediato | — |
