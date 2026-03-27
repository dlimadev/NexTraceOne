# Phase 4 — Módulos Refatorados

## Resumo

### BuildingBlocks.Application
- **Novo**: `ICurrentEnvironment` — abstração de acesso ao ambiente ativo da requisição

### IdentityAccess.Infrastructure
- **Novo**: `CurrentEnvironmentAdapter` — implementa `ICurrentEnvironment` sobre `EnvironmentContextAccessor`
- **Atualizado**: `DependencyInjection` — registra `ICurrentEnvironment`

### OperationalIntelligence.Domain
- **Atualizado**: `IncidentRecord` — novos campos `TenantId?`, `EnvironmentId?`, método `SetTenantContext`

### OperationalIntelligence.Application
- **Atualizado**: `CreateIncidentInput` — novos parâmetros opcionais `TenantId`, `EnvironmentId`
- **Atualizado**: `CreateIncident.Handler` — injeta `ICurrentTenant` e `ICurrentEnvironment`
- **Novo**: `IIncidentContextSurface` — interface de superfície de AI-readiness

### OperationalIntelligence.Infrastructure
- **Atualizado**: `EfIncidentStore.CreateIncident` — chama `SetTenantContext`
- **Atualizado**: `IncidentRecordConfiguration` — configura colunas e índices de tenant/ambiente
- **Novo**: `IncidentContextSurface` — implementação stub da superfície de AI
- **Novo**: Migração `20260320220000_AddTenantContextToIncidents`
- **Atualizado**: `IncidentDbContextModelSnapshot`
- **Atualizado**: `DependencyInjection` — registra `IIncidentContextSurface`

### ChangeGovernance.Domain
- **Atualizado**: `Release` — novos campos `TenantId?`, `EnvironmentId?`, método `SetTenantContext`

### ChangeGovernance.Application
- **Novo**: `IReleaseContextSurface` — interface de superfície de AI-readiness

### ChangeGovernance.Infrastructure
- **Atualizado**: `ReleaseConfiguration` — configura colunas e índices de tenant/ambiente
- **Novo**: `ReleaseContextSurface` — implementação stub da superfície de AI
- **Novo**: Migração `20260320220001_AddTenantContextToReleases`
- **Atualizado**: `ChangeIntelligenceDbContextModelSnapshot`
- **Atualizado**: `DependencyInjection` — registra `IReleaseContextSurface`
