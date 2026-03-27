# Environment Management — Current State Inventory

> **Módulo:** 02 — Environment Management  
> **Data:** 2026-03-25  
> **Fase:** N4-R — Reexecução completa  
> **Estado:** ✅ FINALIZADO

---

## 1. Localização actual do módulo

Environment Management **NÃO** possui módulo backend dedicado.  
Todas as entidades, features, infraestrutura e persistência estão **dentro do módulo Identity & Access**.

| Camada | Localização actual | Localização esperada |
|--------|-------------------|---------------------|
| Domain entities | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Domain/` |
| Application (CQRS) | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Application/Features/` |
| API endpoints | `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/EnvironmentEndpoints.cs` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.API/Endpoints/` |
| Infrastructure | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Infrastructure/` |
| DbContext | `IdentityDbContext` (partilhado) | `EnvironmentDbContext` (dedicado) |
| Frontend page | `src/frontend/src/features/identity-access/pages/EnvironmentsPage.tsx` | `src/frontend/src/features/environment-management/` |
| Frontend context | `src/frontend/src/contexts/EnvironmentContext.tsx` | Permanece (shell concern) |

---

## 2. Entidades de domínio — 5 entidades no IdentityAccess

| # | Entidade | Ficheiro | DbSet | EF Config | Pertence a Env Mgmt? |
|---|----------|---------|-------|-----------|----------------------|
| 1 | `Environment` | `Domain/Entities/Environment.cs` (247 linhas) | ✅ `Environments` | ✅ `EnvironmentConfiguration.cs` | ✅ Aggregate root |
| 2 | `EnvironmentAccess` | `Domain/Entities/EnvironmentAccess.cs` (174 linhas) | ✅ `EnvironmentAccesses` | ✅ `EnvironmentAccessConfiguration.cs` | ⚠️ Partilhada — Identity necessita para auth |
| 3 | `EnvironmentPolicy` | `Domain/Entities/EnvironmentPolicy.cs` (124 linhas) | ❌ Sem DbSet | ❌ Sem mapping | ✅ Phase 2 — definida mas não persistida |
| 4 | `EnvironmentTelemetryPolicy` | `Domain/Entities/EnvironmentTelemetryPolicy.cs` (122 linhas) | ❌ Sem DbSet | ❌ Sem mapping | ✅ Phase 2 — definida mas não persistida |
| 5 | `EnvironmentIntegrationBinding` | `Domain/Entities/EnvironmentIntegrationBinding.cs` (90+ linhas) | ❌ Sem DbSet | ❌ Sem mapping | ✅ Phase 2 — definida mas não persistida |

### Propriedades da entidade `Environment` (aggregate root)

| Propriedade | Tipo | Obrigatória | Notas |
|-------------|------|-------------|-------|
| `Id` | `EnvironmentId` (GUID) | ✅ | Strongly-typed ID |
| `TenantId` | `TenantId` (GUID) | ✅ | FK lógica ao tenant |
| `Name` | `string` (max 100) | ✅ | Nome de exibição |
| `Slug` | `string` (max 50) | ✅ | URL-friendly, único por tenant |
| `SortOrder` | `int` | ✅ | 0 = menos restritivo |
| `IsActive` | `bool` | ✅ | Default true |
| `CreatedAt` | `DateTimeOffset` | ✅ | UTC |
| `Profile` | `EnvironmentProfile` (enum) | ✅ | Phase 9 |
| `Criticality` | `EnvironmentCriticality` (enum) | ✅ | Phase 9 |
| `Code` | `string?` (max 50) | ❌ | Ex: "DEV", "PROD-BR" |
| `Description` | `string?` | ❌ | Documentação |
| `Region` | `string?` (max 100) | ❌ | Ex: "eu-west-1" |
| `IsProductionLike` | `bool` | ✅ | Comporta-se como prod |
| `IsPrimaryProduction` | `bool` | ✅ | Unique filtrado por tenant |

### Métodos de domínio de `Environment`

- `Create()` — 2 factory methods (básico e completo)
- `Activate()` / `Deactivate()` — controlo de estado
- `DesignateAsPrimaryProduction()` / `RevokePrimaryProductionDesignation()`
- `UpdateProfile()` / `UpdateLocationInfo()` / `UpdateBasicInfo()` / `UpdateSortOrder()`

---

## 3. Enums

| Enum | Ficheiro | Valores |
|------|---------|---------|
| `EnvironmentProfile` | `Domain/Enums/EnvironmentProfile.cs` | Development(1), Validation(2), Staging(3), Production(4), Sandbox(5), DisasterRecovery(6), Training(7), UserAcceptanceTesting(8), PerformanceTesting(9) |
| `EnvironmentCriticality` | `Domain/Enums/EnvironmentCriticality.cs` | Low(1), Medium(2), High(3), Critical(4) |

---

## 4. Value Objects

| Value Object | Ficheiro | Propósito |
|-------------|---------|-----------|
| `TenantEnvironmentContext` | `Domain/ValueObjects/TenantEnvironmentContext.cs` | Encapsula contexto tenant+ambiente resolvido |
| `EnvironmentUiProfile` | `Domain/ValueObjects/EnvironmentUiProfile.cs` | Metadata de apresentação (badge color, protection warning, etc.) |

---

## 5. Features CQRS — 6 handlers

| # | Feature | Tipo | Ficheiro |
|---|---------|------|---------|
| 1 | `CreateEnvironment` | Command | `Application/Features/CreateEnvironment.cs` |
| 2 | `ListEnvironments` | Query | `Application/Features/ListEnvironments.cs` |
| 3 | `UpdateEnvironment` | Command | `Application/Features/UpdateEnvironment.cs` |
| 4 | `SetPrimaryProductionEnvironment` | Command | `Application/Features/SetPrimaryProductionEnvironment.cs` |
| 5 | `GetPrimaryProductionEnvironment` | Query | `Application/Features/GetPrimaryProductionEnvironment.cs` |
| 6 | `GrantEnvironmentAccess` | Command | `Application/Features/GrantEnvironmentAccess.cs` |

### Features ausentes confirmadas no código

- ❌ `DeleteEnvironment` (soft-delete)
- ❌ `DeactivateEnvironment`
- ❌ `CompareEnvironments`
- ❌ `DetectDrift`
- ❌ `ManagePromotionPath`
- ❌ `GetEnvironmentReadiness`
- ❌ `SetEnvironmentBaseline`
- ❌ `ListEnvironmentAccesses`
- ❌ `RevokeEnvironmentAccess`

---

## 6. Endpoints API — 6

| Método | Rota | Permissão | Feature |
|--------|------|-----------|---------|
| `GET` | `/api/v1/environments` | `identity:users:read` | ListEnvironments |
| `POST` | `/api/v1/environments` | `identity:users:write` | CreateEnvironment |
| `GET` | `/api/v1/environments/primary-production` | `identity:users:read` | GetPrimaryProductionEnvironment |
| `PUT` | `/api/v1/environments/{id}` | `identity:users:write` | UpdateEnvironment |
| `POST` | `/api/v1/environments/{id}/set-primary-production` | `identity:users:write` | SetPrimaryProductionEnvironment |
| `POST` | `/api/v1/environments/{id}/grant-access` | `identity:users:write` | GrantEnvironmentAccess |

**Fonte:** `NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/EnvironmentEndpoints.cs`

---

## 7. Serviços de infraestrutura — 7

| Serviço | Propósito |
|---------|-----------|
| `EnvironmentContextAccessor` | Accessor scoped do ambiente resolvido |
| `EnvironmentAccessValidator` | Valida acesso do utilizador ao ambiente |
| `EnvironmentProfileResolver` | Resolve perfil UI do ambiente |
| `TenantEnvironmentContextResolver` | Resolve contexto completo tenant+ambiente |
| `EnvironmentResolutionMiddleware` | Middleware ASP.NET para header `X-Environment-Id` |
| `EnvironmentRepository` | Data access |
| `CurrentEnvironmentAdapter` | Implementa `ICurrentEnvironment` do BuildingBlocks |

---

## 8. Tabelas no PostgreSQL — 2

| Tabela actual | Entidade | Prefixo actual | Prefixo alvo |
|--------------|----------|----------------|-------------|
| `identity_environments` | `Environment` | `identity_` | `env_` |
| `identity_environment_accesses` | `EnvironmentAccess` | `identity_` | `env_` |

### Índices existentes

1. **PK:** `id` em ambas as tabelas
2. **Unique:** `(TenantId, Slug)` — `IX_identity_environments_tenant_slug`
3. **Partial unique:** `(TenantId, IsPrimaryProduction)` WHERE `IsPrimaryProduction=true AND IsActive=true`

---

## 9. Frontend — componentes existentes

| Componente | Ficheiro | Feature | Linhas |
|-----------|---------|---------|--------|
| `EnvironmentsPage` | `features/identity-access/pages/EnvironmentsPage.tsx` | identity-access | ~434 |
| `EnvironmentComparisonPage` | `features/operations/pages/EnvironmentComparisonPage.tsx` | operations | ~623 |
| `EnvironmentContext` | `contexts/EnvironmentContext.tsx` | shell | ~261 |
| `EnvironmentBanner` | `components/shell/EnvironmentBanner.tsx` | shell | ~46 |

### Rotas frontend

| Rota | Componente | Permissão |
|------|-----------|-----------|
| `/environments` | `EnvironmentsPage` | `identity:users:read` |
| `/operations/runtime-comparison` | `EnvironmentComparisonPage` | `operations:runtime:read` |

### Sidebar entries

- ❌ `EnvironmentsPage` **não tem entrada no sidebar** — problema de descobribilidade
- ✅ `EnvironmentComparisonPage` tem entrada em Operations

---

## 10. Integração cross-module

| Módulo | Como usa ambiente | Interface |
|--------|-------------------|-----------|
| **BuildingBlocks** | `ICurrentEnvironment` — interface cross-cutting | `BuildingBlocks.Application.Abstractions` |
| **Change Governance** | `DeploymentEnvironment` — entidade separada para promotion flow | Domínio próprio, NÃO usa `Environment` do Identity |
| **Operational Intelligence** | `EnvironmentId` em `IncidentRecord` | `ICurrentEnvironment` |
| **AI & Knowledge** | `EnvironmentComparisonContext`, `PromotionRiskAnalysisContext` | `ICurrentEnvironment`, `IsPrimaryProduction` |
| **Configuration** | Sem referência directa a EnvironmentId | Potencial Phase 2 |

---

## 11. Migrations existentes

1. `20260321160222_InitialCreate` — Environment + EnvironmentAccess base
2. `20260323203306_AddIsPrimaryProductionToEnvironment` — Flag IsPrimaryProduction + partial unique index
3. Phase 9 (pendente): `AddEnvironmentProfileFields` — Profile, Criticality, Code, Description, Region, IsProductionLike

---

## 12. Sumário de gaps

| Categoria | Estado | Detalhes |
|-----------|--------|----------|
| Módulo backend dedicado | ❌ Ausente | Tudo em IdentityAccess |
| DbContext dedicado | ❌ Ausente | Usa `IdentityDbContext` |
| Prefixo `env_` | ❌ Não aplicado | Tabelas com `identity_` |
| Permissões dedicadas `env:*` | ❌ Ausente | Usa `identity:users:*` |
| Frontend feature dedicada | ❌ Ausente | Usa `identity-access` |
| Sidebar entry CRUD | ❌ Ausente | Página não descobrível |
| Página de detalhe | ❌ Ausente | — |
| Promotion path UI | ❌ Ausente | — |
| Drift detection | ❌ Ausente | — |
| Baseline management | ❌ Ausente | — |
| Readiness scoring | ❌ Ausente | — |
| Delete environment | ❌ Ausente | Sem soft-delete |
| Concurrency tokens | ❌ Ausente | Sem `xmin` |
| 3 entidades Phase 2 | ⚠️ Definidas | Sem EF mapping/DbSet |
