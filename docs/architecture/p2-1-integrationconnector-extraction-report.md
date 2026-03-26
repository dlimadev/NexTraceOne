# P2.1 — IntegrationConnector Extraction Report

**Data:** 2026-03-26  
**Fase:** P2.1 — Extração de IntegrationConnector do GovernanceDbContext  
**Estado:** CONCLUÍDO — Compilação verificada, 163 testes Governance passam

---

## 1. Objetivo da Tarefa

Extrair a entidade `IntegrationConnector` do módulo Governance e movê-la para o módulo Integrations, corrigindo o bounded context errado identificado na auditoria do estado actual do NexTraceOne.

---

## 2. Localização Antiga da Entidade

| Artefacto | Localização Antiga |
|-----------|-------------------|
| `IntegrationConnector` (entity) | `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IntegrationConnector.cs` |
| `IntegrationConnectorId` (typed ID) | Mesmo ficheiro acima |
| `ConnectorStatus` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/ConnectorStatus.cs` |
| `ConnectorHealth` (enum) | `src/modules/governance/NexTraceOne.Governance.Domain/Enums/ConnectorHealth.cs` |
| `IIntegrationConnectorRepository` (interface) | `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs` |
| `IntegrationConnectorRepository` (impl) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Repositories/GovernanceRepositories.cs` |
| `IntegrationConnectorConfiguration` (EF config) | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Configurations/IntegrationConnectorConfiguration.cs` |
| `IntegrationConnectors` DbSet | `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs` |

---

## 3. Localização Nova da Entidade

| Artefacto | Localização Nova |
|-----------|----------------|
| `IntegrationConnector` (entity) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Entities/IntegrationConnector.cs` |
| `IntegrationConnectorId` (typed ID) | Mesmo ficheiro acima |
| `ConnectorStatus` (enum) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/ConnectorStatus.cs` |
| `ConnectorHealth` (enum) | `src/modules/integrations/NexTraceOne.Integrations.Domain/Enums/ConnectorHealth.cs` |
| `IIntegrationConnectorRepository` (interface) | `src/modules/integrations/NexTraceOne.Integrations.Application/Abstractions/IIntegrationConnectorRepository.cs` |
| `IntegrationConnectorRepository` (impl) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Repositories/IntegrationConnectorRepository.cs` |
| `IntegrationConnectorConfiguration` (EF config) | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/Configurations/IntegrationConnectorConfiguration.cs` |
| `IntegrationConnectors` DbSet | `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs` |

---

## 4. Novos Ficheiros Criados

### NexTraceOne.Integrations.Domain
```
src/modules/integrations/NexTraceOne.Integrations.Domain/
├── NexTraceOne.Integrations.Domain.csproj
├── Entities/
│   └── IntegrationConnector.cs          (+ IntegrationConnectorId)
└── Enums/
    ├── ConnectorStatus.cs
    └── ConnectorHealth.cs
```

### NexTraceOne.Integrations.Application
```
src/modules/integrations/NexTraceOne.Integrations.Application/
├── NexTraceOne.Integrations.Application.csproj
└── Abstractions/
    └── IIntegrationConnectorRepository.cs
```

### NexTraceOne.Integrations.Infrastructure
```
src/modules/integrations/NexTraceOne.Integrations.Infrastructure/
├── NexTraceOne.Integrations.Infrastructure.csproj
├── DependencyInjection.cs
└── Persistence/
    ├── IntegrationsDbContext.cs
    ├── IntegrationsDbContextDesignTimeFactory.cs
    ├── Configurations/
    │   └── IntegrationConnectorConfiguration.cs
    └── Repositories/
        └── IntegrationConnectorRepository.cs
```

---

## 5. Ficheiros Alterados em Governance

### Ficheiros Eliminados
| Ficheiro | Motivo |
|----------|--------|
| `NexTraceOne.Governance.Domain/Entities/IntegrationConnector.cs` | Movido para Integrations.Domain |
| `NexTraceOne.Governance.Domain/Enums/ConnectorStatus.cs` | Movido para Integrations.Domain |
| `NexTraceOne.Governance.Domain/Enums/ConnectorHealth.cs` | Movido para Integrations.Domain |
| `NexTraceOne.Governance.Infrastructure/Persistence/Configurations/IntegrationConnectorConfiguration.cs` | Movido para Integrations.Infrastructure |

### Ficheiros Actualizados
| Ficheiro | Alteração |
|----------|-----------|
| `Governance.Domain/NexTraceOne.Governance.Domain.csproj` | Adicionada referência temporária a `Integrations.Domain` (para `IntegrationConnectorId` em `IngestionSource`/`IngestionExecution`). A ser removida em P2.2. |
| `Governance.Domain/Entities/IngestionSource.cs` | Adicionado `using NexTraceOne.Integrations.Domain.Entities` |
| `Governance.Domain/Entities/IngestionExecution.cs` | Adicionado `using NexTraceOne.Integrations.Domain.Entities` |
| `Governance.Application/Abstractions/IGovernanceRepositories.cs` | Removida `IIntegrationConnectorRepository` (movida para Integrations.Application). Adicionado `using` para `Integrations.Domain.Entities` |
| `Governance.Application/NexTraceOne.Governance.Application.csproj` | Adicionada referência a `Integrations.Application` |
| `Governance.Application/Features/RetryConnector/RetryConnector.cs` | Actualizados `using` para `Integrations.Application.Abstractions` e `Integrations.Domain.Entities` |
| `Governance.Application/Features/ListIntegrationConnectors/ListIntegrationConnectors.cs` | Actualizados `using` |
| `Governance.Application/Features/GetIntegrationConnector/GetIntegrationConnector.cs` | Actualizados `using` |
| `Governance.Application/Features/GetIngestionHealth/GetIngestionHealth.cs` | Actualizados `using` |
| `Governance.Application/Features/GetIngestionFreshness/GetIngestionFreshness.cs` | Actualizados `using` |
| `Governance.Application/Features/ListIngestionSources/ListIngestionSources.cs` | Actualizados `using` |
| `Governance.Application/Features/ListIngestionExecutions/ListIngestionExecutions.cs` | Actualizados `using` |
| `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Removido `DbSet<IntegrationConnector> IntegrationConnectors`. Actualizado comentário de escopo. |
| `Governance.Infrastructure/Persistence/Repositories/GovernanceRepositories.cs` | Removida classe `IntegrationConnectorRepository`. Adicionado `using` para `Integrations.Domain.Entities`. |
| `Governance.Infrastructure/Persistence/Configurations/IngestionSourceConfiguration.cs` | Removida navegação EF Core `HasOne<IntegrationConnector>()` (cross-DbContext). Adicionado `using`. |
| `Governance.Infrastructure/Persistence/Configurations/IngestionExecutionConfiguration.cs` | Removida navegação EF Core `HasOne<IntegrationConnector>()` (cross-DbContext). Adicionado `using`. |
| `Governance.Infrastructure/DependencyInjection.cs` | Removido registo de `IIntegrationConnectorRepository`. Adicionada chamada a `AddIntegrationsInfrastructure`. |
| `Governance.Infrastructure/NexTraceOne.Governance.Infrastructure.csproj` | Adicionada referência a `Integrations.Infrastructure` |
| `Governance.Infrastructure/Migrations/GovernanceDbContextModelSnapshot.cs` | Removida entidade `IntegrationConnector`. Removidas navegações FK para `IntegrationConnector` em `IngestionSource` e `IngestionExecution`. |
| `Governance.Infrastructure/Migrations/20260325210705_InitialCreate.Designer.cs` | Idem ao snapshot |

### Ficheiros Actualizados em Platform
| Ficheiro | Alteração |
|----------|-----------|
| `NexTraceOne.Ingestion.Api/Program.cs` | Actualizados `using`: adicionados `Integrations.Application.Abstractions`, `Integrations.Domain.Entities` e mantidos `Governance.Application.Abstractions`, `Governance.Domain.Entities` para `IngestionSource`/`IngestionExecution` |
| `NexTraceOne.ApiHost/appsettings.json` | Adicionada connection string `IntegrationsDatabase` |
| `NexTraceOne.ApiHost/appsettings.Development.json` | Idem |

### Ficheiros Actualizados em Tests
| Ficheiro | Alteração |
|----------|-----------|
| `NexTraceOne.Governance.Tests/NexTraceOne.Governance.Tests.csproj` | Adicionadas referências a `Integrations.Domain` e `Integrations.Application` |
| `NexTraceOne.Governance.Tests/Application/Features/IntegrationHubFeatureTests.cs` | Actualizados `using` |
| `NexTraceOne.Governance.Tests/Application/Features/Phase3GovernanceFeatureTests.cs` | Adicionado `using NexTraceOne.Integrations.Domain.Entities` |

---

## 6. Alterações em DbContext / Configuration / DI

### GovernanceDbContext
- **Removido:** `DbSet<IntegrationConnector> IntegrationConnectors`
- **Mantidos:** `DbSet<IngestionSource>`, `DbSet<IngestionExecution>`, `DbSet<AnalyticsEvent>` (a serem extraídos em P2.2 / OI-03)

### IntegrationsDbContext (NOVO)
- **Criado:** `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/Persistence/IntegrationsDbContext.cs`
- **DbSet:** `IntegrationConnectors`
- **Outbox table:** `int_outbox_messages`
- **UnitOfWork:** implementado via `CommitAsync`

### EF Configurations
- Navegação `HasOne<IntegrationConnector>()` removida de `IngestionSourceConfiguration` e `IngestionExecutionConfiguration` porque `IntegrationConnector` já não está no mesmo DbContext. A coluna `ConnectorId` permanece no modelo como referência cross-context.

### DI (Governance.Infrastructure)
- Adicionada chamada `services.AddIntegrationsInfrastructure(configuration)` em `AddGovernanceInfrastructure`

### DI (Integrations.Infrastructure)
- Criado `AddIntegrationsInfrastructure` que regista `IntegrationsDbContext` e `IntegrationConnectorRepository`

---

## 7. Migração de Base de Dados

### Estado actual
O baseline `InitialCreate` de `GovernanceDbContext` (E15) incluía a tabela `int_connectors`. Esta migração foi actualizada no snapshot (Designer.cs) para remover `IntegrationConnector`, mas a migração em si não foi modificada.

### Pendências de migração (documentadas para execução em ambiente real)
1. Gerar nova migração em `GovernanceDbContext` que remova `IntegrationConnector` do seu modelo EF (a tabela `int_connectors` permanece na BD, gerida por `IntegrationsDbContext`)
2. Gerar `InitialCreate` para `IntegrationsDbContext` que inclua `int_connectors`
3. Em BD existente: executar a migração Governance (no-op para a tabela) e registar o histórico de migração para Integrations apontando para o estado actual

> **Nota:** A tabela `int_connectors` já existe na BD (criada pelo baseline Governance E15). A migração do Integrations deve ser marcada como "já aplicada" ou criada com condição `IF NOT EXISTS` para evitar conflito.

---

## 8. Validação Funcional

| Teste | Resultado |
|-------|-----------|
| `dotnet build NexTraceOne.sln` | ✅ 0 erros, 723 warnings (pré-existentes) |
| `dotnet test NexTraceOne.Governance.Tests` | ✅ 163 testes passam, 0 falhas |
| `dotnet build NexTraceOne.Ingestion.Api` | ✅ 0 erros |

---

## 9. Referência Cross-Module Temporária

A dependência `Governance.Domain → Integrations.Domain` foi introduzida porque `IngestionSource` e `IngestionExecution` (ainda em Governance.Domain) referenciam `IntegrationConnectorId` (agora em Integrations.Domain). Esta dependência é:

- **Temporária** — será resolvida quando `IngestionSource` e `IngestionExecution` forem extraídos em P2.2
- **Documentada** — com comentários nos ficheiros `.csproj` e de entidade
- **Aceitável** — representa a direcção de dependência correcta (Governance referencia Integrations, não vice-versa)
