# P2.5 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P2.5 — Alinhamento de contracts, events e referências cross-module  
**Referências anteriores:** P2.1, P2.2, P2.3, P2.4

---

## 1. O que foi resolvido em P2.5

| Item | Estado |
|------|--------|
| Criação de `NexTraceOne.Integrations.Contracts` com eventos de ownership correcto | ✅ Resolvido |
| `ConnectorAuthFailedIntegrationEvent` movido para `Integrations.Contracts` | ✅ Resolvido |
| `SyncFailedIntegrationEvent` movido para `Integrations.Contracts` | ✅ Resolvido |
| `SourceModule` em `IntegrationFailureNotificationHandler` corrigido de `"OperationalIntelligence"` para `"Integrations"` | ✅ Resolvido |
| Testes de handler actualizados para usar `Integrations.Contracts` | ✅ Resolvido |
| `IntegrationsDbContext` adicionado às migration waves (Wave 5) | ✅ Resolvido |
| `ProductAnalyticsDbContext` adicionado às migration waves (Wave 5) | ✅ Resolvido |
| Frontend já aponta para paths correctos (`/api/v1/integrations/*`, `/api/v1/product-analytics/*`) | ✅ Confirmado |
| `Governance.Contracts` não contém referências a entidades extraídas | ✅ Confirmado |
| Compilação 0 erros | ✅ Resolvido |
| 412 Notifications tests + 163 Governance tests passam | ✅ Resolvido |

---

## 2. O que ainda ficou pendente

### 2.1 Handlers de Integrations e Analytics ainda em Governance.Application (COMPATIBILIDADE TRANSITÓRIA)

Os 15 handlers (8 Integrations + 7 Analytics) marcados em P2.4 como COMPATIBILIDADE TRANSITÓRIA continuam em `Governance.Application`:

**Integrations handlers ainda em Governance.Application:**
- `ListConnectorsQueryHandler`
- `GetConnectorQueryHandler`
- `ListIngestionSourcesQueryHandler`
- `ListIngestionExecutionsQueryHandler`
- `GetIngestionHealthQueryHandler`
- `GetIngestionFreshnessQueryHandler`
- `RetryConnectorCommandHandler`
- `ReprocessExecutionCommandHandler`

**Analytics handlers ainda em Governance.Application:**
- `GetAnalyticsSummaryQueryHandler`
- `GetModuleAdoptionQueryHandler`
- `GetPersonaUsageQueryHandler`
- `GetJourneysQueryHandler`
- `GetValueMilestonesQueryHandler`
- `GetFrictionQueryHandler`
- `RecordAnalyticsEventCommandHandler`

**Motivo:** Estes handlers requerem que `Integrations.Application/API` e `ProductAnalytics.Application/API` sejam completados com endpoint modules próprios — fora do escopo de P2.5.

### 2.2 `IntegrationHubEndpointModule` ainda em Governance.API (COMPATIBILIDADE TRANSITÓRIA)

O endpoint module `IntegrationHubEndpointModule` serve as rotas `/api/v1/integrations/*` e `/api/v1/ingestion/*` a partir do projecto Governance.API. É necessário criar `NexTraceOne.Integrations.API` para migrar estes endpoints.

### 2.3 `ProductAnalyticsEndpointModule` ainda em Governance.API (COMPATIBILIDADE TRANSITÓRIA)

O endpoint module `ProductAnalyticsEndpointModule` serve as rotas `/api/v1/product-analytics/*` a partir do projecto Governance.API. É necessário criar `NexTraceOne.ProductAnalytics.API` para migrar estes endpoints.

### 2.4 Enums residuais no domínio Governance (COMPATIBILIDADE TRANSITÓRIA)

Os enums `JourneyStatus`, `ValueMilestoneType` e `FrictionSignalType` ainda estão em `Governance.Domain`. Pertencem semanticamente a `ProductAnalytics.Domain`.

### 2.5 `ConnectorAuthFailedIntegrationEvent` e `SyncFailedIntegrationEvent` ainda em `OperationalIntelligence.Contracts`

Permanecem como cópias transitórias documentadas. A remoção depende de confirmar que nenhum publicador OI activo usa estas cópias. A migração definitiva requer auditoria dos publicadores.

### 2.6 Migrations de `IntegrationsDbContext` e `ProductAnalyticsDbContext` ainda não geradas

As migration waves foram adicionadas em P2.5, mas nenhuma baseline InitialCreate foi gerada para estes dois DbContexts. Será necessário executar `dotnet ef migrations add InitialCreate` para cada um.

### 2.7 `NexTraceOne.Integrations.API` e `NexTraceOne.ProductAnalytics.API` não existem

Estes projectos de API dedicados ainda não foram criados. Enquanto não existirem, os endpoint modules permanecem em `Governance.API` como facades transitórias.

---

## 3. Resíduos ainda existentes por Compatibilidade

| Residual | Módulo | Motivo de permanência | Marcação |
|----------|--------|-----------------------|----------|
| `IntegrationHubEndpointModule` | Governance.API | Aguarda criação de Integrations.API | COMPATIBILIDADE TRANSITÓRIA |
| `ProductAnalyticsEndpointModule` | Governance.API | Aguarda criação de ProductAnalytics.API | COMPATIBILIDADE TRANSITÓRIA |
| 8 handlers de Integrations | Governance.Application | Aguarda Integrations.Application handlers completos | COMPATIBILIDADE TRANSITÓRIA |
| 7 handlers de Analytics | Governance.Application | Aguarda ProductAnalytics.Application handlers completos | COMPATIBILIDADE TRANSITÓRIA |
| `ConnectorAuthFailedIntegrationEvent` (cópia) | OI.Contracts | Aguarda auditoria de publicadores OI | COMPATIBILIDADE TRANSITÓRIA |
| `SyncFailedIntegrationEvent` (cópia) | OI.Contracts | Aguarda auditoria de publicadores OI | COMPATIBILIDADE TRANSITÓRIA |
| `JourneyStatus`, `ValueMilestoneType`, `FrictionSignalType` | Governance.Domain | Aguarda extracção para ProductAnalytics.Domain | RESIDUAL MARCADO |

---

## 4. O que fica explicitamente para a próxima macrofase

### Fase 3 recomendada: Criar APIs dedicadas para Integrations e ProductAnalytics

| Tarefa | Descrição |
|--------|-----------|
| Criar `NexTraceOne.Integrations.API` | Com `IntegrationHubEndpointModule` próprio e routing limpo |
| Criar `NexTraceOne.ProductAnalytics.API` | Com `ProductAnalyticsEndpointModule` próprio e routing limpo |
| Migrar handlers de Integrations para `Integrations.Application` | 8 handlers já documentados como transitórios |
| Migrar handlers de Analytics para `ProductAnalytics.Application` | 7 handlers já documentados como transitórios |
| Remover facades de Governance.Application e Governance.API | Após validação dos novos módulos |
| Gerar migrations `InitialCreate` para `IntegrationsDbContext` e `ProductAnalyticsDbContext` | Usando `dotnet ef migrations add InitialCreate` |
| Extrair `JourneyStatus`, `ValueMilestoneType`, `FrictionSignalType` para `ProductAnalytics.Domain` | Após criação de ProductAnalytics.API |
| Remover cópias transitórias de `ConnectorAuthFailedIntegrationEvent` e `SyncFailedIntegrationEvent` de `OI.Contracts` | Após auditoria de publicadores |

---

## 5. Classificação do estado pós P2.5

| Área | Estado |
|------|--------|
| GovernanceDbContext | ✅ LIMPO (P2.4) |
| Governance.Contracts | ✅ LIMPO — apenas eventos de Governance |
| Integrations.Contracts | ✅ CRIADO — ownership correcto |
| Integrations.Domain/Application/Infrastructure | ✅ CORRECTO |
| ProductAnalytics.Domain/Application/Infrastructure | ✅ CORRECTO |
| Cross-module event ownership | ✅ CORRIGIDO (com cópias transitórias documentadas) |
| Migration waves | ✅ COMPLETAS (Wave 5 adicionada) |
| Frontend API paths | ✅ CORRECTOS |
| Governance.Application (handlers transitórios) | ⚠️ COMPATIBILIDADE TRANSITÓRIA — aguarda P3 |
| Governance.API (endpoints transitórios) | ⚠️ COMPATIBILIDADE TRANSITÓRIA — aguarda P3 |
| Migrations InitialCreate para Integrations/ProductAnalytics | ⚠️ PENDENTE |
