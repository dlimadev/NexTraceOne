# P2.4 — Governance Residual Cleanup Report

**Data:** 2026-03-26  
**Fase:** P2.4 — Limpeza residual do módulo Governance após extrações P2.1–P2.3  
**Estado:** CONCLUÍDO — 0 erros de compilação, 163 testes Governance passam

---

## 1. Objetivo da Fase

Após as extrações P2.1 (IntegrationConnector), P2.2 (IngestionSource + IngestionExecution) e P2.3 (AnalyticsEvent), esta fase consolida a limpeza residual do módulo Governance, marcando explicitamente as responsabilidades ainda temporariamente alojadas nele, e garantindo que o GovernanceDbContext e os seus componentes deixem de aparentar ownership das entidades extraídas.

---

## 2. Estado do GovernanceDbContext Antes da P2.4

O GovernanceDbContext já estava limpo após P2.3:

| DbSet | Entidade | Estado |
|-------|---------|--------|
| `Teams` | `Team` | ✅ Governance |
| `Domains` | `GovernanceDomain` | ✅ Governance |
| `Packs` | `GovernancePack` | ✅ Governance |
| `PackVersions` | `GovernancePackVersion` | ✅ Governance |
| `Waivers` | `GovernanceWaiver` | ✅ Governance |
| `DelegatedAdministrations` | `DelegatedAdministration` | ✅ Governance |
| `TeamDomainLinks` | `TeamDomainLink` | ✅ Governance |
| `RolloutRecords` | `GovernanceRolloutRecord` | ✅ Governance |

**Não havia entidades extraídas no DbContext a remover nesta fase.**

---

## 3. Pontos Residuais Encontrados Após P2.1–P2.3

### 3.1 Handlers residuais de Integrations em Governance.Application (8 handlers)
| Handler | Estado Antes P2.4 |
|---------|------------------|
| `GetIngestionFreshness` | Em Governance.Application. Usa `Integrations.Application.Abstractions` e `Integrations.Domain.*`. |
| `GetIngestionHealth` | Idem |
| `GetIntegrationConnector` | Idem |
| `ListIngestionExecutions` | Idem |
| `ListIngestionSources` | Idem |
| `ListIntegrationConnectors` | Idem |
| `RetryConnector` | Idem |
| `ReprocessExecution` | Idem |

### 3.2 Handlers residuais de Product Analytics em Governance.Application (7 handlers)
| Handler | Estado Antes P2.4 |
|---------|------------------|
| `RecordAnalyticsEvent` | Em Governance.Application. Usa `ProductAnalytics.Application.Abstractions` e `ProductAnalytics.Domain.*`. |
| `GetAnalyticsSummary` | Idem |
| `GetModuleAdoption` | Idem |
| `GetPersonaUsage` | Idem |
| `GetFrictionIndicators` | Idem |
| `GetJourneys` | Em Governance.Application. Usa `Governance.Domain.Enums.JourneyStatus`. |
| `GetValueMilestones` | Em Governance.Application. Usa `Governance.Domain.Enums.ValueMilestoneType`. |

### 3.3 Endpoint modules residuais em Governance.API
| Endpoint Module | Estado Antes P2.4 |
|----------------|------------------|
| `IntegrationHubEndpointModule` | Em Governance.API. Rotas `/api/v1/integrations` e `/api/v1/ingestion`. Ownership real: Integrations. |
| `ProductAnalyticsEndpointModule` | Em Governance.API. Rota `/api/v1/product-analytics`. Permissões `governance:analytics:*`. Ownership real: ProductAnalytics. |

### 3.4 Enums residuais de analytics em Governance.Domain.Enums
| Enum | Estado Antes P2.4 |
|------|------------------|
| `JourneyStatus` | Pertence semanticamente a ProductAnalytics. Usado por `GetJourneys`. |
| `ValueMilestoneType` | Pertence semanticamente a ProductAnalytics. Usado por `GetValueMilestones`. |
| `FrictionSignalType` | Pertence semanticamente a ProductAnalytics. Usado por `GetFrictionIndicators`. |

### 3.5 DI residual em Governance.Infrastructure
- `AddIntegrationsInfrastructure` chamado de `Governance.Infrastructure.DependencyInjection` — necessário enquanto os handlers de Integrations estiverem em Governance.Application.
- `AddProductAnalyticsInfrastructure` chamado de `Governance.Infrastructure.DependencyInjection` — necessário enquanto os handlers de ProductAnalytics estiverem em Governance.Application.

### 3.6 Permissões residuais de analytics
- `governance:analytics:read` e `governance:analytics:write` ainda usadas em `ProductAnalyticsEndpointModule`. Deverão ser renomeadas para `analytics:*` em fase futura.

---

## 4. Limpeza Realizada no GovernanceDbContext

Nenhuma remoção adicional de DbSet foi necessária — o contexto já estava limpo após P2.3.

Actualização do comentário de classe para registar:
- P2.4 completado
- Responsabilidades temporárias ainda alojadas explicitamente documentadas

---

## 5. Referências/DI/Imports Corrigidos

| Ficheiro | Alteração |
|----------|-----------|
| `Governance.Infrastructure/DependencyInjection.cs` | Comentários expandidos: marcadas explicitamente as chamadas `AddIntegrationsInfrastructure` e `AddProductAnalyticsInfrastructure` como COMPATIBILIDADE TRANSITÓRIA. Clarificado que `IGovernanceAnalyticsRepository` é legítimo (queries Waivers/Packs/RolloutRecords). |
| `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Comentário de classe actualizado: registado P2.4 e listadas responsabilidades temporárias restantes. |

---

## 6. Endpoints/Handlers Temporariamente Mantidos por Compatibilidade

Todos os seguintes foram explicitamente marcados com `COMPATIBILIDADE TRANSITÓRIA (P2.4)` na sua documentação XML:

### Endpoint Modules (Governance.API)
| Ficheiro | Marcação |
|----------|---------|
| `IntegrationHubEndpointModule.cs` | Ownership real: Integrations. Migração prevista em fase futura. |
| `ProductAnalyticsEndpointModule.cs` | Ownership real: ProductAnalytics. Permissões `governance:analytics:*` residuais. Migração prevista em fase futura. |

### Handlers de Integrations (Governance.Application) — 8 handlers
| Handler | Marcação |
|---------|---------|
| `GetIngestionFreshness`, `GetIngestionHealth`, `GetIntegrationConnector` | COMPATIBILIDADE TRANSITÓRIA (P2.4) — migração para Integrations.Application prevista em fase futura |
| `ListIngestionExecutions`, `ListIngestionSources`, `ListIntegrationConnectors` | Idem |
| `RetryConnector`, `ReprocessExecution` | Idem |

### Handlers de Product Analytics (Governance.Application) — 7 handlers
| Handler | Marcação |
|---------|---------|
| `RecordAnalyticsEvent`, `GetAnalyticsSummary`, `GetModuleAdoption`, `GetPersonaUsage` | COMPATIBILIDADE TRANSITÓRIA (P2.4) — migração para ProductAnalytics.Application prevista em fase futura |
| `GetFrictionIndicators`, `GetJourneys`, `GetValueMilestones` | Idem |

### Enums Residuais (Governance.Domain.Enums) — 3 enums
| Enum | Marcação |
|------|---------|
| `JourneyStatus` | COMPATIBILIDADE TRANSITÓRIA (P2.4) — extração para ProductAnalytics.Domain.Enums prevista |
| `ValueMilestoneType` | Idem |
| `FrictionSignalType` | Idem |

---

## 7. Ficheiros Alterados em P2.4

| Ficheiro | Tipo de Alteração |
|----------|-----------------|
| `Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Actualizado comentário da classe (P2.4 + responsabilidades temporárias) |
| `Governance.Infrastructure/DependencyInjection.cs` | Comentários de DI expandidos para marcar compatibilidade transitória |
| `Governance.API/Endpoints/IntegrationHubEndpointModule.cs` | Comentário da classe: marcado como facade de compatibilidade transitória |
| `Governance.API/Endpoints/ProductAnalyticsEndpointModule.cs` | Idem + permissões residuais documentadas |
| `Governance.Application/Features/GetIngestionFreshness/GetIngestionFreshness.cs` | Marcado COMPATIBILIDADE TRANSITÓRIA |
| `Governance.Application/Features/GetIngestionHealth/GetIngestionHealth.cs` | Idem |
| `Governance.Application/Features/GetIntegrationConnector/GetIntegrationConnector.cs` | Idem |
| `Governance.Application/Features/ListIngestionExecutions/ListIngestionExecutions.cs` | Idem |
| `Governance.Application/Features/ListIngestionSources/ListIngestionSources.cs` | Idem |
| `Governance.Application/Features/ListIntegrationConnectors/ListIntegrationConnectors.cs` | Idem |
| `Governance.Application/Features/RetryConnector/RetryConnector.cs` | Idem |
| `Governance.Application/Features/ReprocessExecution/ReprocessExecution.cs` | Idem |
| `Governance.Application/Features/GetAnalyticsSummary/GetAnalyticsSummary.cs` | Marcado COMPATIBILIDADE TRANSITÓRIA |
| `Governance.Application/Features/GetModuleAdoption/GetModuleAdoption.cs` | Idem |
| `Governance.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs` | Idem |
| `Governance.Application/Features/GetPersonaUsage/GetPersonaUsage.cs` | Idem |
| `Governance.Application/Features/RecordAnalyticsEvent/RecordAnalyticsEvent.cs` | Idem |
| `Governance.Application/Features/GetJourneys/GetJourneys.cs` | Idem + nota sobre JourneyStatus enum |
| `Governance.Application/Features/GetValueMilestones/GetValueMilestones.cs` | Idem + nota sobre ValueMilestoneType enum |
| `Governance.Domain/Enums/JourneyStatus.cs` | Marcado COMPATIBILIDADE TRANSITÓRIA: pertence a ProductAnalytics |
| `Governance.Domain/Enums/ValueMilestoneType.cs` | Idem |
| `Governance.Domain/Enums/FrictionSignalType.cs` | Idem |

---

## 8. Validação Funcional

| Verificação | Resultado |
|-------------|-----------|
| `dotnet build NexTraceOne.sln` | ✅ 0 erros |
| `dotnet test NexTraceOne.Governance.Tests` | ✅ 163 testes passam |

---

## 9. Estado do Módulo Governance Após P2.4

| Componente | Estado |
|------------|--------|
| `GovernanceDbContext` | ✅ Limpo — 8 DbSets de Governance puro |
| Handlers de Governance puro | ✅ Corretos — Teams, Domains, Packs, Waivers, etc. |
| `IGovernanceAnalyticsRepository` | ✅ Legítimo — queries Governance entities para executive trends |
| Handlers de Integrations | ⚠️ Façades transitórias marcadas — migração para Integrations.Application pendente |
| Handlers de ProductAnalytics | ⚠️ Façades transitórias marcadas — migração para ProductAnalytics.Application pendente |
| `IntegrationHubEndpointModule` | ⚠️ Façade transitória marcada — migração para Integrations.API pendente |
| `ProductAnalyticsEndpointModule` | ⚠️ Façade transitória marcada — migração para ProductAnalytics.API pendente |
| Enums analytics em Governance.Domain | ⚠️ Documentados como residuais — extração para ProductAnalytics.Domain pendente |
| Permissões `governance:analytics:*` | ⚠️ Residuais — renomear para `analytics:*` em fase futura |
