# P2.5 — Cross-Module References Alignment Report

**Data:** 2026-03-26  
**Fase:** P2.5 — Alinhamento de contracts, events e referências cross-module após P2.1–P2.4  
**Estado:** CONCLUÍDO — 0 erros de compilação, 412 Notifications + 163 Governance testes passam

---

## 1. Objetivo da Fase

Alinhar os contracts, integration events e referências cross-module do NexTraceOne após as extrações de P2.1 (IntegrationConnector), P2.2 (IngestionSource/IngestionExecution), P2.3 (AnalyticsEvent) e a limpeza de P2.4, garantindo que:

- os eventos de integração reflectem o bounded context correto;
- o módulo Integrations passa a ter o seu próprio `Contracts` project;
- a comunicação cross-module continua funcional e coerente;
- os resíduos de ownership errado são explicitamente marcados.

---

## 2. Inventário de Referências Cross-Module Analisadas

### 2.1 Integration Events auditados

| Evento | Localização Anterior | Avaliação |
|--------|---------------------|-----------|
| `ComplianceCheckFailedIntegrationEvent` | `Governance.Contracts` | ✅ Correcto — ownership Governance |
| `PolicyViolatedIntegrationEvent` | `Governance.Contracts` | ✅ Correcto — ownership Governance |
| `EvidenceExpiringIntegrationEvent` | `Governance.Contracts` | ✅ Correcto — ownership Governance |
| `BudgetThresholdReachedIntegrationEvent` | `Governance.Contracts` | ✅ Correcto — ownership Governance |
| `IncidentCreatedIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Correcto — ownership OI |
| `IncidentEscalatedIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Correcto — ownership OI |
| `BudgetExceededIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Correcto — ownership OI |
| `IntegrationFailedIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Mantido — evento OI genérico |
| `IncidentResolvedIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Correcto — ownership OI |
| `AnomalyDetectedIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Correcto — ownership OI |
| `HealthDegradationIntegrationEvent` | `OperationalIntelligence.Contracts` | ✅ Correcto — ownership OI |
| **`ConnectorAuthFailedIntegrationEvent`** | `OperationalIntelligence.Contracts` | ❌ Errado — ownership Integrations. Corrigido: movido para `Integrations.Contracts` |
| **`SyncFailedIntegrationEvent`** | `OperationalIntelligence.Contracts` | ❌ Errado — ownership Integrations. Corrigido: movido para `Integrations.Contracts` |

### 2.2 DTOs e Endpoint Routes auditados

| Item | Localização | Avaliação |
|------|------------|-----------|
| `integrationsApi` frontend | `/api/v1/integrations/*` + `/api/v1/ingestion/*` | ✅ Já apontam para path correcto |
| `productAnalyticsApi` frontend | `/api/v1/product-analytics/*` | ✅ Já aponta para path correcto |
| Handlers de Integrations em Governance.Application | Marcados como COMPATIBILIDADE TRANSITÓRIA (P2.4) | ✅ Correcto |
| Handlers de Analytics em Governance.Application | Marcados como COMPATIBILIDADE TRANSITÓRIA (P2.4) | ✅ Correcto |

### 2.3 Migration Waves auditadas

| DbContext | Presença em WebApplicationExtensions Antes | Depois |
|-----------|-------------------------------------------|--------|
| `IntegrationsDbContext` | ❌ Ausente | ✅ Wave 5 adicionado |
| `ProductAnalyticsDbContext` | ❌ Ausente | ✅ Wave 5 adicionado |

---

## 3. Contracts e Integration Events Ajustados

### 3.1 Novo projecto criado: `NexTraceOne.Integrations.Contracts`

**Localização:** `src/modules/integrations/NexTraceOne.Integrations.Contracts/`

**Eventos publicados:**
- `ConnectorAuthFailedIntegrationEvent` — source: `"Integrations"` (antes `"OperationalIntelligence"`)
- `SyncFailedIntegrationEvent` — source: `"Integrations"` (antes `"OperationalIntelligence"`)
- `ConnectorActivatedIntegrationEvent` — novo, placeholder para fase futura
- `ConnectorDeactivatedIntegrationEvent` — novo, placeholder para fase futura

**Adicionado ao `NexTraceOne.sln`** com GUID `{E5F6A7B8-C9D0-1234-5678-ABCDEF012345}`.

### 3.2 `OperationalIntelligence.Contracts` — eventos anotados como COMPATIBILIDADE TRANSITÓRIA

Os eventos `ConnectorAuthFailedIntegrationEvent` e `SyncFailedIntegrationEvent` permanecem neste ficheiro mas estão agora documentados como:
- cópias transitórias para compatibilidade com publicadores OI ainda não migrados
- definição canónica em `Integrations.Contracts`
- a remover quando todos os publicadores usarem `Integrations.Contracts`

---

## 4. Referências/DI/Imports Corrigidos

| Ficheiro | Alteração |
|----------|-----------|
| `Notifications.Infrastructure/EventHandlers/IntegrationFailureNotificationHandler.cs` | Handler migrado para usar `IntegrationsContracts.SyncFailedIntegrationEvent` e `IntegrationsContracts.ConnectorAuthFailedIntegrationEvent`. `SourceModule` corrigido de `"OperationalIntelligence"` para `"Integrations"`. |
| `Notifications.Infrastructure/DependencyInjection.cs` | DI registrations actualizados para usar `IntegrationsContracts.*`. Adicionado `using IntegrationsContracts = NexTraceOne.Integrations.Contracts.IntegrationEvents;` para desambiguação com `Governance.Contracts.IntegrationEvents`. |
| `Notifications.Infrastructure/NexTraceOne.Notifications.Infrastructure.csproj` | Adicionada referência a `NexTraceOne.Integrations.Contracts`. |
| `Notifications.Tests/NexTraceOne.Notifications.Tests.csproj` | Adicionada referência a `NexTraceOne.Integrations.Contracts`. |
| `Notifications.Tests/Engine/EventHandlers/IntegrationPhase5HandlerTests.cs` | Testes actualizados para usar `IntegrationsContracts.*`. Assertions de `SourceModule` corrigidas de `"OperationalIntelligence"` para `"Integrations"`. |
| `ApiHost/WebApplicationExtensions.cs` | Adicionados usings `NexTraceOne.Integrations.Infrastructure.Persistence` e `NexTraceOne.ProductAnalytics.Infrastructure.Persistence`. Adicionada Wave 5 com `IntegrationsDbContext` e `ProductAnalyticsDbContext`. |
| `OperationalIntelligence.Contracts/IntegrationEvents/OperationalIntegrationEvents.cs` | Anotações COMPATIBILIDADE TRANSITÓRIA (P2.5) adicionadas a `ConnectorAuthFailedIntegrationEvent` e `SyncFailedIntegrationEvent`. |

---

## 5. Endpoints Transitórios Mantidos por Compatibilidade

Nenhuma alteração foi necessária nos endpoints. O estado herdado de P2.4 é o correcto:

| Endpoint Module | Localização | Estado |
|----------------|------------|--------|
| `IntegrationHubEndpointModule` | Governance.API | ⚠️ COMPATIBILIDADE TRANSITÓRIA (P2.4) |
| `ProductAnalyticsEndpointModule` | Governance.API | ⚠️ COMPATIBILIDADE TRANSITÓRIA (P2.4) |

O frontend já aponta para `/api/v1/integrations/*` e `/api/v1/product-analytics/*` que são exactamente as rotas servidas por estes endpoint modules — nenhuma rota `/api/governance/integrations` ou `/api/governance/analytics` existe no código actual.

---

## 6. Validação Funcional/Compilação

| Verificação | Resultado |
|-------------|-----------|
| `dotnet build NexTraceOne.sln` | ✅ 0 erros |
| `dotnet test NexTraceOne.Notifications.Tests` | ✅ 412 testes passam |
| `dotnet test NexTraceOne.Governance.Tests` | ✅ 163 testes passam |

---

## 7. Ficheiros Alterados em P2.5

| Ficheiro | Tipo de Alteração |
|----------|-----------------|
| `src/modules/integrations/NexTraceOne.Integrations.Contracts/NexTraceOne.Integrations.Contracts.csproj` | Novo projecto |
| `src/modules/integrations/NexTraceOne.Integrations.Contracts/IntegrationEvents.cs` | Novo ficheiro — 4 integration events com ownership correcto |
| `NexTraceOne.sln` | Adicionado `NexTraceOne.Integrations.Contracts` (GUID, build configs, solution folder) |
| `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Contracts/IntegrationEvents/OperationalIntegrationEvents.cs` | Anotações COMPATIBILIDADE TRANSITÓRIA em 2 eventos |
| `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/EventHandlers/IntegrationFailureNotificationHandler.cs` | Handler migrado para `Integrations.Contracts`; `SourceModule` corrigido |
| `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/DependencyInjection.cs` | DI actualizados; using alias adicionado |
| `src/modules/notifications/NexTraceOne.Notifications.Infrastructure/NexTraceOne.Notifications.Infrastructure.csproj` | Referência a `Integrations.Contracts` adicionada |
| `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs` | Wave 5 adicionada; usings de Integrations/ProductAnalytics persistence adicionados |
| `tests/modules/notifications/NexTraceOne.Notifications.Tests/NexTraceOne.Notifications.Tests.csproj` | Referência a `Integrations.Contracts` adicionada |
| `tests/modules/notifications/NexTraceOne.Notifications.Tests/Engine/EventHandlers/IntegrationPhase5HandlerTests.cs` | Testes actualizados para novos tipos; assertions corrigidas |
