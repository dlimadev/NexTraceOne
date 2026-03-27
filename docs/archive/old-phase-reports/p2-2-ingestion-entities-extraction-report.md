# P2.2 — IngestionSource e IngestionExecution Extraction Report

**Data:** 2026-03-26  
**Fase:** P2.2 — Extração de IngestionSource e IngestionExecution do GovernanceDbContext  
**Estado:** CONCLUÍDO — Compilação verificada, 163 testes Governance passam

---

## 1. Objetivo da Tarefa

Extrair as entidades `IngestionSource` e `IngestionExecution` do módulo Governance e movê-las para o módulo Integrations, completando a remoção do acoplamento de integrações ao bounded context errado identificado no P2.1.

---

## 2. Localização Antiga das Entidades

| Artefacto | Localização Antiga |
|-----------|-------------------|
| `IngestionSource` (entity + `IngestionSourceId`) | `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IngestionSource.cs` |
| `IngestionExecution` (entity + `IngestionExecutionId`) | `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IngestionExecution.cs` |
| `SourceStatus` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/SourceStatus.cs` |
| `FreshnessStatus` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/FreshnessStatus.cs` |
| `SourceTrustLevel` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/SourceTrustLevel.cs` |
| `ExecutionResult` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/ExecutionResult.cs` |
| `IIngestionSourceRepository` | `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs` |
| `IIngestionExecutionRepository` | `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs` |
| `IngestionSourceRepository` (impl) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Repositories/GovernanceRepositories.cs` |
| `IngestionExecutionRepository` (impl) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Repositories/GovernanceRepositories.cs` |
| `IngestionSourceConfiguration` (EF config) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Configurations/IngestionSourceConfiguration.cs` |
| `IngestionExecutionConfiguration` (EF config) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Configurations/IngestionExecutionConfiguration.cs` |
| `IngestionSources` DbSet | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs` |
| `IngestionExecutions` DbSet | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs` |

---

## 3. Localização Nova das Entidades

| Artefacto | Localização Nova |
|-----------|----------------|
| `IngestionSource` (entity + `IngestionSourceId`) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Entities/IngestionSource.cs` |
| `IngestionExecution` (entity + `IngestionExecutionId`) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Entities/IngestionExecution.cs` |
| `SourceStatus` (enum) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/SourceStatus.cs` |
| `FreshnessStatus` (enum) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/FreshnessStatus.cs` |
| `SourceTrustLevel` (enum) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/SourceTrustLevel.cs` |
| `ExecutionResult` (enum) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/ExecutionResult.cs` |
| `IIngestionSourceRepository` | `src/modules/integrations/NexTraceOne.Integrations.Application/Abstractions/IIngestionSourceRepository.cs` |
| `IIngestionExecutionRepository` | `src/modules/integrations/NexTraceOne.Integrations.Application/Abstractions/IIngestionExecutionRepository.cs` |
| `IngestionSourceRepository` (impl) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Repositories/IngestionRepositories.cs` |
| `IngestionExecutionRepository` (impl) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Repositories/IngestionRepositories.cs` |
| `IngestionSourceConfiguration` (EF config) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Configurations/IngestionSourceConfiguration.cs` |
| `IngestionExecutionConfiguration` (EF config) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Configurations/IngestionExecutionConfiguration.cs` |
| `IngestionSources` DbSet | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs` |
| `IngestionExecutions` DbSet | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs` |

---

## 4. Novos Ficheiros Criados

### NexTraceOne.Integrations.Domain
```
Entities/IngestionSource.cs          (+ IngestionSourceId)
Entities/IngestionExecution.cs       (+ IngestionExecutionId)
Enums/SourceStatus.cs
Enums/FreshnessStatus.cs
Enums/SourceTrustLevel.cs
Enums/ExecutionResult.cs
```

### NexTraceOne.Integrations.Application
```
Abstractions/IIngestionSourceRepository.cs
Abstractions/IIngestionExecutionRepository.cs
```

### NexTraceOne.Integrations.Infrastructure
```
Persistence/Configurations/IngestionSourceConfiguration.cs
Persistence/Configurations/IngestionExecutionConfiguration.cs
Persistence/Repositories/IngestionRepositories.cs    (IngestionSourceRepository + IngestionExecutionRepository)
```

---

## 5. Ficheiros Eliminados em Governance

| Ficheiro | Motivo |
|----------|--------|
| `Governance.Domain/Entities/IngestionSource.cs` | Movido para Integrations.Domain |
| `Governance.Domain/Entities/IngestionExecution.cs` | Movido para Integrations.Domain |
| `Governance.Domain/Enums/SourceStatus.cs` | Movido para Integrations.Domain |
| `Governance.Domain/Enums/FreshnessStatus.cs` | Movido para Integrations.Domain |
| `Governance.Domain/Enums/SourceTrustLevel.cs` | Movido para Integrations.Domain |
| `Governance.Domain/Enums/ExecutionResult.cs` | Movido para Integrations.Domain |
| `Governance.Infrastructure/Persistence/Configurations/IngestionSourceConfiguration.cs` | Movido para Integrations.Infrastructure |
| `Governance.Infrastructure/Persistence/Configurations/IngestionExecutionConfiguration.cs` | Movido para Integrations.Infrastructure |

---

## 6. Ficheiros Actualizados em Governance

| Ficheiro | Alteração |
|----------|-----------|
| `Governance.Domain/NexTraceOne.Governance.Domain.csproj` | **Removida** referência temporária a `Integrations.Domain` (adicionada em P2.1). Não é mais necessária pois `IngestionSource` e `IngestionExecution` saíram de Governance.Domain. |
| `Governance.Application/Abstractions/IGovernanceRepositories.cs` | Removidas `IIngestionSourceRepository` e `IIngestionExecutionRepository`. Removido `using NexTraceOne.Integrations.Domain.Entities`. |
| `Governance.Application/Features/GetIngestionFreshness/GetIngestionFreshness.cs` | Removido `using NexTraceOne.Governance.Domain.Entities` (IngestionSource agora em Integrations.Domain) |
| `Governance.Application/Features/GetIngestionHealth/GetIngestionHealth.cs` | Removido `using NexTraceOne.Governance.Domain.Enums` (FreshnessStatus agora em Integrations.Domain.Enums) |
| `Governance.Application/Features/ListIngestionSources/ListIngestionSources.cs` | Removidos `using NexTraceOne.Governance.Domain.Entities` e `using NexTraceOne.Governance.Domain.Enums` (tipos agora em Integrations.Domain) |
| `Governance.Application/Features/ListIngestionExecutions/ListIngestionExecutions.cs` | Idem |
| `Governance.Application/Features/GetIntegrationConnector/GetIntegrationConnector.cs` | Removido `using NexTraceOne.Governance.Domain.Entities`. Corrigida referência `NexTraceOne.Governance.Domain.Enums.ExecutionResult.Failed` → `NexTraceOne.Integrations.Domain.Enums.ExecutionResult.Failed` |
| `Governance.Application/Features/RetryConnector/RetryConnector.cs` | Removidos `using NexTraceOne.Governance.Domain.Entities` e `using NexTraceOne.Governance.Domain.Enums` |
| `Governance.Application/Features/ReprocessExecution/ReprocessExecution.cs` | Substituído `using NexTraceOne.Governance.Application.Abstractions` + `NexTraceOne.Governance.Domain.Entities` por `using NexTraceOne.Integrations.Application.Abstractions` + `NexTraceOne.Integrations.Domain.Entities` |
| `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Removidos `DbSet<IngestionSource>` e `DbSet<IngestionExecution>`. Atualizado comentário de escopo. |
| `Governance.Infrastructure/Persistence/Repositories/GovernanceRepositories.cs` | Removidas classes `IngestionSourceRepository` e `IngestionExecutionRepository`. Removido `using NexTraceOne.Integrations.Domain.Entities`. |
| `Governance.Infrastructure/DependencyInjection.cs` | Removidos registos de `IIngestionSourceRepository` e `IIngestionExecutionRepository` (agora em `AddIntegrationsInfrastructure`) |
| `Governance.Infrastructure/Migrations/GovernanceDbContextModelSnapshot.cs` | Removidas entidades `IngestionExecution` e `IngestionSource` e suas relações FK |
| `Governance.Infrastructure/Migrations/20260325210705_InitialCreate.Designer.cs` | Idem ao snapshot |

---

## 7. Ficheiros Actualizados em Integrations

| Ficheiro | Alteração |
|----------|-----------|
| `Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs` | Adicionados `DbSet<IngestionSource>` e `DbSet<IngestionExecution>`. Atualizado comentário de escopo. |
| `Integrations.Infrastructure/DependencyInjection.cs` | Adicionados registos `IIngestionSourceRepository` e `IIngestionExecutionRepository` |

---

## 8. Ficheiros Actualizados em Platform e Tests

| Ficheiro | Alteração |
|----------|-----------|
| `NexTraceOne.Ingestion.Api/Program.cs` | Removidos `using NexTraceOne.Governance.Application.Abstractions` e `using NexTraceOne.Governance.Domain.Entities` (tipos agora em Integrations) |
| `NexTraceOne.Governance.Tests/Application/Features/IntegrationHubFeatureTests.cs` | Removidos `using NexTraceOne.Governance.Domain.Entities` e `using NexTraceOne.Governance.Domain.Enums` (tipos agora em Integrations.Domain) |

---

## 9. Alterações em DbContext / Configuration / DI

### GovernanceDbContext — Removido
- `DbSet<IngestionSource> IngestionSources`
- `DbSet<IngestionExecution> IngestionExecutions`

### IntegrationsDbContext — Adicionado
- `DbSet<IngestionSource> IngestionSources`
- `DbSet<IngestionExecution> IngestionExecutions`

### EF Configurations Restauradas
As navegações EF Core entre `IngestionSource`/`IngestionExecution` e `IntegrationConnector` foram restauradas em `IngestionSourceConfiguration` e `IngestionExecutionConfiguration` porque todas as três entidades agora pertencem ao mesmo `IntegrationsDbContext`. Em P2.1 essas navegações foram removidas por estarem em DbContexts diferentes — essa limitação já não existe.

### DI
- `Governance.Infrastructure.DependencyInjection.cs`: Removidos `IIngestionSourceRepository` e `IIngestionExecutionRepository`
- `Integrations.Infrastructure.DependencyInjection.cs`: Adicionados `IIngestionSourceRepository` e `IIngestionExecutionRepository`

### Governance.Domain.csproj
Removida a referência temporária `Governance.Domain → Integrations.Domain` adicionada em P2.1. O motivo era que `IngestionSource` e `IngestionExecution` (em Governance.Domain) referenciavam `IntegrationConnectorId` (em Integrations.Domain). Agora que essas entidades saíram de Governance.Domain, a dependência não é mais necessária. `Governance.Domain` volta a ter apenas `BuildingBlocks.Core` como dependência.

---

## 10. Validação Funcional

| Teste | Resultado |
|-------|-----------|
| `dotnet build NexTraceOne.sln` | ✅ 0 erros |
| `dotnet test NexTraceOne.Governance.Tests` | ✅ 163 testes passam, 0 falhas |
| `dotnet build NexTraceOne.Integrations.Infrastructure` | ✅ 0 erros |

---

## 11. Resumo do Estado do Módulo Integrations Após P2.2

| Componente | Estado |
|------------|--------|
| `Integrations.Domain` | ✅ Completo — `IntegrationConnector`, `IngestionSource`, `IngestionExecution` + todos os 6 enums |
| `Integrations.Application` | ✅ Completo — `IIntegrationConnectorRepository`, `IIngestionSourceRepository`, `IIngestionExecutionRepository` |
| `Integrations.Infrastructure` | ✅ Completo — `IntegrationsDbContext` com 3 DbSets, 3 configs, 3 repositórios, DI |
| `Integrations.API` | ❌ Não criado (escopo P2.3) |
| `AnalyticsEvent` | ⚠️ Ainda em Governance (escopo OI-03) |
