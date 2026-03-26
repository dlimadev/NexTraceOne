# P2.3 — AnalyticsEvent Extraction Report

**Data:** 2026-03-26  
**Fase:** P2.3 — Extração de AnalyticsEvent do GovernanceDbContext para o módulo Product Analytics  
**Estado:** CONCLUÍDO — Compilação verificada, 163 testes Governance + 68 testes Infrastructure passam

---

## 1. Objetivo da Tarefa

Extrair a entidade `AnalyticsEvent` do módulo Governance e movê-la para um novo módulo dedicado `ProductAnalytics`, completando a remoção do acoplamento de product analytics ao bounded context errado.

---

## 2. Localização Antiga da Entidade

| Artefacto | Localização Antiga |
|-----------|-------------------|
| `AnalyticsEvent` (entity + `AnalyticsEventId`) | `src/modules/governance/NexTraceOne.Governance.Domain/Entities/AnalyticsEvent.cs` |
| `ProductModule` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/ProductModule.cs` |
| `AnalyticsEventType` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/AnalyticsEventType.cs` |
| `IAnalyticsEventRepository` (interface) | `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs` |
| DTOs: `ModuleUsageRow`, `ModuleAdoptionRow`, `ModuleFeatureCountRow`, `SessionEventRow` | `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs` |
| `AnalyticsEventRepository` (impl) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs` |
| `AnalyticsEventConfiguration` (EF config) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Configurations/AnalyticsEventConfiguration.cs` |
| `AnalyticsEvents` DbSet | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs` |

---

## 3. Localização Nova da Entidade

| Artefacto | Localização Nova |
|-----------|----------------|
| `AnalyticsEvent` (entity + `AnalyticsEventId`) | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Entities/AnalyticsEvent.cs` |
| `ProductModule` (enum) | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/ProductModule.cs` |
| `AnalyticsEventType` (enum) | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/AnalyticsEventType.cs` |
| `IAnalyticsEventRepository` (interface) | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Abstractions/IAnalyticsEventRepository.cs` |
| DTOs: `ModuleUsageRow`, `ModuleAdoptionRow`, `ModuleFeatureCountRow`, `SessionEventRow` | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Abstractions/IAnalyticsEventRepository.cs` |
| `AnalyticsEventRepository` (impl) | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs` |
| `AnalyticsEventConfiguration` (EF config) | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Configurations/AnalyticsEventConfiguration.cs` |
| `AnalyticsEvents` DbSet | `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs` |

---

## 4. Novos Ficheiros Criados

### NexTraceOne.ProductAnalytics.Domain
```
NexTraceOne.ProductAnalytics.Domain.csproj
Entities/AnalyticsEvent.cs          (+ AnalyticsEventId)
Enums/ProductModule.cs
Enums/AnalyticsEventType.cs
```

### NexTraceOne.ProductAnalytics.Application
```
NexTraceOne.ProductAnalytics.Application.csproj
Abstractions/IAnalyticsEventRepository.cs  (interface + DTOs)
```

### NexTraceOne.ProductAnalytics.Infrastructure
```
NexTraceOne.ProductAnalytics.Infrastructure.csproj
Persistence/ProductAnalyticsDbContext.cs
Persistence/ProductAnalyticsDbContextDesignTimeFactory.cs
Persistence/Configurations/AnalyticsEventConfiguration.cs
Persistence/Repositories/AnalyticsEventRepository.cs
DependencyInjection.cs
```

---

## 5. Ficheiros Eliminados em Governance

| Ficheiro | Motivo |
|----------|--------|
| `Governance.Domain/Entities/AnalyticsEvent.cs` | Movido para ProductAnalytics.Domain |
| `Governance.Domain/Enums/ProductModule.cs` | Movido para ProductAnalytics.Domain |
| `Governance.Domain/Enums/AnalyticsEventType.cs` | Movido para ProductAnalytics.Domain |
| `Governance.Infrastructure/Persistence/Configurations/AnalyticsEventConfiguration.cs` | Movido para ProductAnalytics.Infrastructure |
| `Governance.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs` | Movido para ProductAnalytics.Infrastructure |

---

## 6. Ficheiros Actualizados em Governance

| Ficheiro | Alteração |
|----------|-----------|
| `Governance.Application/Abstractions/IGovernanceRepositories.cs` | Removidas `IAnalyticsEventRepository` e DTOs (`ModuleUsageRow`, `ModuleAdoptionRow`, `ModuleFeatureCountRow`, `SessionEventRow`). Adicionado comentário P2.3. |
| `Governance.Application/Features/RecordAnalyticsEvent/RecordAnalyticsEvent.cs` | Substituídas referências `Governance.Application.Abstractions`, `Governance.Domain.Entities`, `Governance.Domain.Enums` por `ProductAnalytics.Application.Abstractions`, `ProductAnalytics.Domain.Entities`, `ProductAnalytics.Domain.Enums` |
| `Governance.Application/Features/GetAnalyticsSummary/GetAnalyticsSummary.cs` | Substituída `Governance.Application.Abstractions` + `Governance.Domain.Enums` (analytics) por `ProductAnalytics.Application.Abstractions` + `ProductAnalytics.Domain.Enums`. Mantido `Governance.Domain.Enums` para `TrendDirection`. |
| `Governance.Application/Features/GetModuleAdoption/GetModuleAdoption.cs` | Idem |
| `Governance.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs` | Substituídas referências analytics por ProductAnalytics. Mantido `Governance.Domain.Enums` para `FrictionSignalType` e `TrendDirection`. |
| `Governance.Application/Features/GetPersonaUsage/GetPersonaUsage.cs` | Substituída referência `Governance.Domain.Enums` analytics por `ProductAnalytics.Domain.Enums`. Mantido `Governance.Domain.Enums` para `ValueMilestoneType`. |
| `Governance.Application/NexTraceOne.Governance.Application.csproj` | Adicionada referência a `ProductAnalytics.Application` |
| `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Removido `DbSet<AnalyticsEvent> AnalyticsEvents`. Actualizado comentário de escopo (P2.3 concluído). |
| `Governance.Infrastructure/DependencyInjection.cs` | Removido registo de `IAnalyticsEventRepository`. Adicionada chamada `AddProductAnalyticsInfrastructure`. Adicionado using `NexTraceOne.ProductAnalytics.Infrastructure`. |
| `Governance.Infrastructure/NexTraceOne.Governance.Infrastructure.csproj` | Adicionada referência a `ProductAnalytics.Infrastructure` |
| `Governance.Infrastructure/Migrations/GovernanceDbContextModelSnapshot.cs` | Removida entidade `AnalyticsEvent` (451 linhas removidas) |
| `Governance.Infrastructure/Migrations/20260325210705_InitialCreate.Designer.cs` | Idem (452 linhas removidas) |

---

## 7. Ficheiros Actualizados em Platform e Tests

| Ficheiro | Alteração |
|----------|-----------|
| `NexTraceOne.ApiHost/appsettings.json` | Adicionada `ProductAnalyticsDatabase` connection string |
| `NexTraceOne.BuildingBlocks.Infrastructure.Tests/Configuration/AppSettingsSecurityTests.cs` | Actualizado contador de connection strings de 21 para 22 |
| `NexTraceOne.Governance.Tests/Application/Features/AnalyticsFeatureTests.cs` | Substituídos usings `Governance.Application.Abstractions`, `Governance.Domain.Entities`, `Governance.Domain.Enums` por `ProductAnalytics.Application.Abstractions`, `ProductAnalytics.Domain.Entities`, `ProductAnalytics.Domain.Enums` |
| `NexTraceOne.Governance.Tests/NexTraceOne.Governance.Tests.csproj` | Adicionadas referências a `ProductAnalytics.Domain` e `ProductAnalytics.Application` |
| `NexTraceOne.sln` | Adicionados 3 novos projetos (`ProductAnalytics.Domain`, `ProductAnalytics.Application`, `ProductAnalytics.Infrastructure`) com entradas de configuração no GlobalSection |

---

## 8. Alterações em DbContext / Configuration / DI

### GovernanceDbContext — Removido
- `DbSet<AnalyticsEvent> AnalyticsEvents`

### ProductAnalyticsDbContext (NOVO) — Criado
- `DbSet<AnalyticsEvent> AnalyticsEvents`
- OutboxTableName: `pan_outbox_messages`
- Connection string key: `ProductAnalyticsDatabase`

### DI
- `Governance.Infrastructure.DependencyInjection.cs`: Removido `IAnalyticsEventRepository`. Adicionado `AddProductAnalyticsInfrastructure(configuration)`.
- `ProductAnalytics.Infrastructure.DependencyInjection.cs` (NOVO): Regista `IAnalyticsEventRepository → AnalyticsEventRepository` e `ProductAnalyticsDbContext`.

### GovernanceDbContext state após P2.3
`GovernanceDbContext` contém apenas 8 DbSets puros de Governance:
- `Teams`, `Domains`, `Packs`, `PackVersions`, `Waivers`, `DelegatedAdministrations`, `TeamDomainLinks`, `RolloutRecords`

---

## 9. Validação Funcional

| Teste | Resultado |
|-------|-----------|
| `dotnet build NexTraceOne.sln` | ✅ 0 erros |
| `dotnet test NexTraceOne.Governance.Tests` | ✅ 163 testes passam, 0 falhas |
| `dotnet test NexTraceOne.BuildingBlocks.Infrastructure.Tests` | ✅ 68 testes passam (inclui AppSettingsSecurityTests) |

---

## 10. Estado do Módulo Product Analytics Após P2.3

| Componente | Estado |
|------------|--------|
| `ProductAnalytics.Domain` | ✅ Completo — `AnalyticsEvent` + `AnalyticsEventId`, `ProductModule`, `AnalyticsEventType` |
| `ProductAnalytics.Application` | ✅ Completo — `IAnalyticsEventRepository` + 4 DTOs |
| `ProductAnalytics.Infrastructure` | ✅ Completo — `ProductAnalyticsDbContext`, EF config, repositório, DI |
| `ProductAnalytics.API` | ❌ Não criado (handlers ainda em Governance.Application, endpoints em Governance.API) |
| `ProductAnalytics` feature handlers | ⚠️ Ainda em Governance.Application (escopo P2.4) |
