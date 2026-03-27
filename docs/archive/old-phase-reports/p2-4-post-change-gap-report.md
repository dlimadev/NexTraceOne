# P2.4 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P2.4 — Limpeza residual do módulo Governance  
**Estado:** CONCLUÍDO COM FAÇADES TRANSITÓRIAS CONTROLADAS

---

## 1. O Que Foi Resolvido em P2.4

| Item | Estado |
|------|--------|
| GovernanceDbContext confirmado limpo (8 DbSets de Governance puro) | ✅ |
| Todos os 8 handlers de Integrations marcados como COMPATIBILIDADE TRANSITÓRIA | ✅ |
| Todos os 7 handlers de Product Analytics marcados como COMPATIBILIDADE TRANSITÓRIA | ✅ |
| `IntegrationHubEndpointModule` marcado como facade de compatibilidade | ✅ |
| `ProductAnalyticsEndpointModule` marcado como facade + permissões residuais documentadas | ✅ |
| DI de Governance.Infrastructure clarificado com notas de compatibilidade transitória | ✅ |
| `GovernanceDbContext` comentário actualizado com estado P2.4 e responsabilidades temporárias | ✅ |
| 3 enums analytics residuais (`JourneyStatus`, `ValueMilestoneType`, `FrictionSignalType`) marcados como pertencentes a ProductAnalytics | ✅ |
| Compilação: 0 erros | ✅ |
| 163 testes Governance passam | ✅ |

---

## 2. Evolução das Extrações da Fase 2 (Estado Completo)

| Extracção | Fase | DbContext | Handlers | Endpoints |
|-----------|------|-----------|----------|-----------|
| `IntegrationConnector` | P2.1 | ✅ IntegrationsDbContext | ⚠️ Governance.Application (façade) | ⚠️ Governance.API (façade) |
| `IngestionSource` | P2.2 | ✅ IntegrationsDbContext | ⚠️ Governance.Application (façade) | ⚠️ Governance.API (façade) |
| `IngestionExecution` | P2.2 | ✅ IntegrationsDbContext | ⚠️ Governance.Application (façade) | ⚠️ Governance.API (façade) |
| `AnalyticsEvent` | P2.3 | ✅ ProductAnalyticsDbContext | ⚠️ Governance.Application (façade) | ⚠️ Governance.API (façade) |
| Limpeza residual | P2.4 | ✅ Concluída | ⚠️ Façades documentadas | ⚠️ Façades documentadas |

---

## 3. O Que Ficou Pendente (Façades Transitórias Controladas)

### 3.1 Handlers de Integrations ainda em Governance.Application

| Resíduo | Localização Actual | Ownership Correto | Fase Alvo |
|---------|-------------------|-------------------|-----------|
| `GetIngestionFreshness` | Governance.Application | Integrations.Application | Fase futura |
| `GetIngestionHealth` | Governance.Application | Integrations.Application | Fase futura |
| `GetIntegrationConnector` | Governance.Application | Integrations.Application | Fase futura |
| `ListIngestionExecutions` | Governance.Application | Integrations.Application | Fase futura |
| `ListIngestionSources` | Governance.Application | Integrations.Application | Fase futura |
| `ListIntegrationConnectors` | Governance.Application | Integrations.Application | Fase futura |
| `RetryConnector` | Governance.Application | Integrations.Application | Fase futura |
| `ReprocessExecution` | Governance.Application | Integrations.Application | Fase futura |

### 3.2 Handlers de Product Analytics ainda em Governance.Application

| Resíduo | Localização Actual | Ownership Correto | Fase Alvo |
|---------|-------------------|-------------------|-----------|
| `RecordAnalyticsEvent` | Governance.Application | ProductAnalytics.Application | Fase futura |
| `GetAnalyticsSummary` | Governance.Application | ProductAnalytics.Application | Fase futura |
| `GetModuleAdoption` | Governance.Application | ProductAnalytics.Application | Fase futura |
| `GetPersonaUsage` | Governance.Application | ProductAnalytics.Application | Fase futura |
| `GetFrictionIndicators` | Governance.Application | ProductAnalytics.Application | Fase futura |
| `GetJourneys` | Governance.Application | ProductAnalytics.Application | Fase futura |
| `GetValueMilestones` | Governance.Application | ProductAnalytics.Application | Fase futura |

### 3.3 Endpoint Modules em Governance.API

| Resíduo | Rota | Ownership Correto | Fase Alvo |
|---------|------|-------------------|-----------|
| `IntegrationHubEndpointModule` | `/api/v1/integrations`, `/api/v1/ingestion` | Integrations.API | Fase futura |
| `ProductAnalyticsEndpointModule` | `/api/v1/product-analytics` | ProductAnalytics.API | Fase futura |

### 3.4 Enums analytics em Governance.Domain.Enums

| Resíduo | Ownership Correto | Fase Alvo |
|---------|-------------------|-----------|
| `JourneyStatus` | ProductAnalytics.Domain.Enums | Junto com handlers |
| `ValueMilestoneType` | ProductAnalytics.Domain.Enums | Junto com handlers |
| `FrictionSignalType` | ProductAnalytics.Domain.Enums | Junto com handlers |

### 3.5 Permissões residuais

| Resíduo | Estado | Fase Alvo |
|---------|--------|-----------|
| `governance:analytics:read` | Ainda usada em `ProductAnalyticsEndpointModule` e `RolePermissionCatalog` | Renomear para `analytics:read` junto com migração de endpoint |
| `governance:analytics:write` | Idem | Renomear para `analytics:write` |

### 3.6 DI wiring transitório

| Resíduo | Motivo | Fase Alvo |
|---------|--------|-----------|
| `Governance.Infrastructure` chama `AddIntegrationsInfrastructure` | Necessário enquanto handlers de Integrations estiverem em Governance.Application | Remover ao migrar handlers |
| `Governance.Infrastructure` chama `AddProductAnalyticsInfrastructure` | Necessário enquanto handlers de Analytics estiverem em Governance.Application | Remover ao migrar handlers |

---

## 4. O Que Fica para Fase Futura (P2.5 ou fase posterior)

### Para o módulo Integrations
1. Criar projecto `NexTraceOne.Integrations.Application` (já existe? verificar) — mover os 8 handlers
2. Criar projecto `NexTraceOne.Integrations.API` — mover `IntegrationHubEndpointModule`
3. Registar `Integrations.API` no `ApiHost` de forma autónoma
4. Remover os 8 handlers de `Governance.Application`
5. Remover `AddIntegrationsInfrastructure` do wiring de Governance

### Para o módulo Product Analytics
1. Mover os 7 handlers para `NexTraceOne.ProductAnalytics.Application`
2. Criar projecto `NexTraceOne.ProductAnalytics.API` — mover `ProductAnalyticsEndpointModule`
3. Migrar `JourneyStatus`, `ValueMilestoneType`, `FrictionSignalType` para `ProductAnalytics.Domain.Enums`
4. Renomear permissões `governance:analytics:*` para `analytics:*` em `RolePermissionCatalog` e endpoints
5. Registar `ProductAnalytics.API` no `ApiHost` de forma autónoma
6. Remover os 7 handlers de `Governance.Application`
7. Remover `AddProductAnalyticsInfrastructure` do wiring de Governance

---

## 5. Limitações Residuais

| Limitação | Impacto | Criticidade |
|-----------|---------|-------------|
| Handlers de Integrations e ProductAnalytics ainda em Governance.Application | Dependência de namespace cruzado, mas semanticamente corretos | Baixa |
| Permissões `governance:analytics:*` dão falsa impressão de ownership | Cosmético, não funcional | Baixa |
| DI wiring transitório de Governance → Integrations/ProductAnalytics | Acoplamento de bootstrap, não acoplamento de domínio | Baixa |
| 3 enums de analytics ainda em Governance.Domain | Dívida técnica explicitamente documentada | Baixa |

---

## 6. Estado Final do Módulo Governance Após P2.1–P2.4

```
GovernanceDbContext
├── Teams (✅ Governance)
├── Domains (✅ Governance)
├── Packs (✅ Governance)
├── PackVersions (✅ Governance)
├── Waivers (✅ Governance)
├── DelegatedAdministrations (✅ Governance)
├── TeamDomainLinks (✅ Governance)
└── RolloutRecords (✅ Governance)

Governance.Application (handlers legítimos)
├── CreateTeam, CreateDomain, CreateGovernancePack, CreatePackVersion
├── CreateGovernanceWaiver, ApproveGovernanceWaiver
├── GetGovernancePack, GetPackCoverage, GetPackApplicability
├── GetComplianceSummary, GetComplianceGaps
├── GetFinOpsSummary, GetFinOpsTrends, GetDomainFinOps
├── GetExecutiveOverview, GetExecutiveDrillDown, GetExecutiveTrends
├── GetBenchmarking, GetMaturityScorecards
├── ApplyGovernancePack, CreateDelegatedAdministration
└── ... (handlers de Governance puro)

Governance.Application (façades transitórias)
├── [8 handlers de Integrations — COMPATIBILIDADE TRANSITÓRIA P2.4]
└── [7 handlers de ProductAnalytics — COMPATIBILIDADE TRANSITÓRIA P2.4]

Governance.API (façades transitórias)
├── IntegrationHubEndpointModule — COMPATIBILIDADE TRANSITÓRIA P2.4
└── ProductAnalyticsEndpointModule — COMPATIBILIDADE TRANSITÓRIA P2.4
```
