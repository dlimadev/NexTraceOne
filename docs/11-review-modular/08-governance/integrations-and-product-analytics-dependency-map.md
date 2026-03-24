# Governance Module — Integrations & Product Analytics Dependency Map

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Overview

The Governance module currently hosts entities, endpoints, CQRS handlers, and persistence configurations that belong to two other modules: **Integrations** and **Product Analytics**. This document maps every dependency, what must be extracted, and what Governance should expose/consume after extraction.

---

## 2. Integrations Dependency (Currently Inside Governance)

### 2.1 Entities (3)

| Entity | File | DbSet | Table |
|--------|------|-------|-------|
| `IntegrationConnector` | `Domain/Entities/IntegrationConnector.cs` | `IntegrationConnectors` | `gov_integration_connectors` |
| `IngestionSource` | `Domain/Entities/IngestionSource.cs` | `IngestionSources` | `gov_ingestion_sources` |
| `IngestionExecution` | `Domain/Entities/IngestionExecution.cs` | `IngestionExecutions` | `gov_ingestion_executions` |

### 2.2 Endpoint Module (1)

| Module | Routes | Permission |
|--------|--------|-----------|
| `IntegrationHubEndpointModule` | `/api/v1/integrations/connectors`, `/api/v1/ingestion/sources`, `/api/v1/ingestion/executions` | `integrations:read/write` |

### 2.3 CQRS Handlers (8)

| # | Handler | Type | Entity |
|---|---------|------|--------|
| 1 | ListIntegrationConnectorsHandler | Query | IntegrationConnector |
| 2 | CreateIntegrationConnectorHandler | Command | IntegrationConnector |
| 3 | UpdateIntegrationConnectorHandler | Command | IntegrationConnector |
| 4 | DeleteIntegrationConnectorHandler | Command | IntegrationConnector |
| 5 | ListIngestionSourcesHandler | Query | IngestionSource |
| 6 | CreateIngestionSourceHandler | Command | IngestionSource |
| 7 | ListIngestionExecutionsHandler | Query | IngestionExecution |
| 8 | TriggerIngestionExecutionHandler | Command | IngestionExecution |

### 2.4 Repositories (3)

| Repository | Interface | Entity |
|-----------|-----------|--------|
| `IntegrationConnectorRepository` | `IIntegrationConnectorRepository` | IntegrationConnector |
| `IngestionSourceRepository` | `IIngestionSourceRepository` | IngestionSource |
| `IngestionExecutionRepository` | `IIngestionExecutionRepository` | IngestionExecution |

### 2.5 EF Configurations (3)

| Configuration | Table | Prefix |
|--------------|-------|--------|
| `IntegrationConnectorConfiguration` | `gov_integration_connectors` | `gov_` (wrong — should be `int_`) |
| `IngestionSourceConfiguration` | `gov_ingestion_sources` | `gov_` (wrong — should be `int_`) |
| `IngestionExecutionConfiguration` | `gov_ingestion_executions` | `gov_` (wrong — should be `int_`) |

### 2.6 Enums (6)

| Enum | Used By |
|------|---------|
| `ConnectorType` | IntegrationConnector |
| `ConnectorStatus` | IntegrationConnector |
| `ConnectorAuthType` | IntegrationConnector |
| `IngestionSourceType` | IngestionSource |
| `IngestionSourceStatus` | IngestionSource |
| `IngestionExecutionStatus` | IngestionExecution |

### 2.7 DbSets (3)

| DbSet Name | Entity | In GovernanceDbContext |
|-----------|--------|----------------------|
| `IntegrationConnectors` | IntegrationConnector | ✅ (should be in IntegrationsDbContext) |
| `IngestionSources` | IngestionSource | ✅ (should be in IntegrationsDbContext) |
| `IngestionExecutions` | IngestionExecution | ✅ (should be in IntegrationsDbContext) |

### 2.8 Tables (3)

| Current Table | Target Table | Target Prefix |
|--------------|-------------|--------------|
| `gov_integration_connectors` | `int_connectors` | `int_` |
| `gov_ingestion_sources` | `int_ingestion_sources` | `int_` |
| `gov_ingestion_executions` | `int_ingestion_executions` | `int_` |

---

## 3. Product Analytics Dependency (Currently Inside Governance)

### 3.1 Entity (1)

| Entity | File | DbSet | Table |
|--------|------|-------|-------|
| `AnalyticsEvent` | `Domain/Entities/AnalyticsEvent.cs` | `AnalyticsEvents` | `gov_analytics_events` |

### 3.2 Endpoint Module (1)

| Module | Routes | Permission |
|--------|--------|-----------|
| `ProductAnalyticsEndpointModule` | `/api/v1/product-analytics/events`, `/api/v1/product-analytics/summary`, `/api/v1/product-analytics/trends`, `/api/v1/product-analytics/reports`, `/api/v1/product-analytics/export`, `/api/v1/product-analytics/dashboard` | `governance:analytics:read/write` |

### 3.3 CQRS Handlers (7)

| # | Handler | Type | Entity |
|---|---------|------|--------|
| 1 | ListAnalyticsEventsHandler | Query | AnalyticsEvent |
| 2 | RecordAnalyticsEventHandler | Command | AnalyticsEvent |
| 3 | GetAnalyticsSummaryHandler | Query | AnalyticsEvent |
| 4 | GetAnalyticsTrendsHandler | Query | AnalyticsEvent |
| 5 | GetAnalyticsReportsHandler | Query | AnalyticsEvent |
| 6 | ExportAnalyticsDataHandler | Query | AnalyticsEvent |
| 7 | GetAnalyticsDashboardHandler | Query | AnalyticsEvent |

### 3.4 Repository (1)

| Repository | Interface | Entity |
|-----------|-----------|--------|
| `AnalyticsEventRepository` | `IAnalyticsEventRepository` | AnalyticsEvent |

### 3.5 EF Configuration (1)

| Configuration | Table | Prefix |
|--------------|-------|--------|
| `AnalyticsEventConfiguration` | `gov_analytics_events` | `gov_` (wrong — should be `pan_`) |

### 3.6 Enums (6)

| Enum | Used By |
|------|---------|
| `AnalyticsEventType` | AnalyticsEvent |
| `AnalyticsEventCategory` | AnalyticsEvent |
| `AnalyticsEventSeverity` | AnalyticsEvent |
| `AnalyticsEventSource` | AnalyticsEvent |
| `AnalyticsEventStatus` | AnalyticsEvent |
| `AnalyticsPeriod` | Query filters |

### 3.7 DbSet (1)

| DbSet Name | Entity | In GovernanceDbContext |
|-----------|--------|----------------------|
| `AnalyticsEvents` | AnalyticsEvent | ✅ (should be in ProductAnalyticsDbContext) |

### 3.8 Table (1)

| Current Table | Target Table | Target Prefix |
|--------------|-------------|--------------|
| `gov_analytics_events` | `pan_analytics_events` | `pan_` |

---

## 4. Shared Entities Analysis

### Entities Improperly in GovernanceDbContext

| Entity | Current DbContext | Target DbContext | Reason |
|--------|------------------|-----------------|--------|
| IntegrationConnector | GovernanceDbContext | IntegrationsDbContext | Integration management is not governance |
| IngestionSource | GovernanceDbContext | IntegrationsDbContext | Data ingestion tracking is not governance |
| IngestionExecution | GovernanceDbContext | IntegrationsDbContext | Execution history is not governance |
| AnalyticsEvent | GovernanceDbContext | ProductAnalyticsDbContext | Usage analytics is not governance |

### Entities That Correctly Remain in Governance

All 9 governance entities (Team, TeamDomainLink, GovernanceDomain, GovernancePack, GovernancePackVersion, GovernanceRolloutRecord, GovernanceRuleBinding, GovernanceWaiver, DelegatedAdministration) correctly belong to GovernanceDbContext.

---

## 5. Shared Pages Analysis

**No shared pages exist.** The frontend has already been properly separated:

| Module | Feature Folder | Pages | Routes | Permission |
|--------|---------------|-------|--------|-----------|
| Governance | `features/governance/` | 25 pages | `/governance/*` | `governance:read` |
| Integrations | `features/integrations/` | 4 pages | `/integrations/*` | `integrations:read` |
| Product Analytics | `features/product-analytics/` | 5 pages | `/product-analytics/*` | `analytics:read` |

✅ Frontend extraction is **already complete** for both Integrations and Product Analytics.

---

## 6. Shared Endpoints Analysis

| Endpoint Module | Currently In | Routes Already Separated? | Permission |
|----------------|-------------|--------------------------|-----------|
| IntegrationHubEndpointModule | Governance backend project | ✅ Routes use `/api/v1/integrations/` and `/api/v1/ingestion/` (not `/governance/`) | `integrations:read/write` |
| ProductAnalyticsEndpointModule | Governance backend project | ✅ Routes use `/api/v1/product-analytics/` (not `/governance/`) | `governance:analytics:read/write` ⚠️ |

**Note on Product Analytics permission:** Currently uses `governance:analytics:read/write` — after extraction should use `analytics:read/write` to match the frontend.

---

## 7. What Governance Should Expose (After Extraction)

After Integration and Product Analytics entities are extracted, Governance should still **expose** to other modules:

| Exposed Data | Consumer | Mechanism | Use Case |
|-------------|----------|-----------|----------|
| Governance policies referencing integration health | Integrations | API contract / event | Policies that require "integration must be healthy" |
| Compliance status per service/team | All modules | API / read-model | Other modules query compliance posture |
| Risk assessment data | Executive dashboards | API | Cross-module risk aggregation |
| Team and domain ownership data | All modules | API | Service-to-team, service-to-domain mapping |
| Governance pack rules | Service Catalog, Change Intelligence | API / event | Rule validation during changes |

---

## 8. What Governance Should Consume (After Extraction)

After extraction, Governance should **consume** from other modules:

| Consumed Data | Provider | Mechanism | Use Case |
|-------------- |----------|-----------|----------|
| Integration connector status | Integrations | Event / API query | Risk reports factor in integration health |
| Integration connector count/health | Integrations | API query | Executive overview shows integration posture |
| Analytics event data | Product Analytics | API query | Executive views include usage analytics |
| Service catalog data | Services | API query / event | Compliance assessed per service |
| Change history | Change Intelligence | Event | Change-to-compliance correlation |
| Incident data | Operations | Event | Incident-to-risk correlation |

---

## 9. What Must Be Separated (Extraction Plan)

### 9.1 Integrations Extraction (OI-02)

| # | Component | Current Location | Target Location |
|---|-----------|-----------------|----------------|
| 1 | IntegrationConnector entity | `Governance/Domain/Entities/` | `Integrations/Domain/Entities/` |
| 2 | IngestionSource entity | `Governance/Domain/Entities/` | `Integrations/Domain/Entities/` |
| 3 | IngestionExecution entity | `Governance/Domain/Entities/` | `Integrations/Domain/Entities/` |
| 4 | IntegrationHubEndpointModule | `Governance/Api/Endpoints/` | `Integrations/Api/Endpoints/` |
| 5 | 8 CQRS handlers | `Governance/Application/` | `Integrations/Application/` |
| 6 | 3 repositories | `Governance/Infrastructure/` | `Integrations/Infrastructure/` |
| 7 | 3 EF configurations | `Governance/Infrastructure/` | `Integrations/Infrastructure/` |
| 8 | 6 enums | `Governance/Domain/Enums/` | `Integrations/Domain/Enums/` |
| 9 | 3 DbSets | `GovernanceDbContext` | `IntegrationsDbContext` |
| 10 | 3 tables (rename prefix) | `gov_*` | `int_*` |

### 9.2 Product Analytics Extraction (OI-03)

| # | Component | Current Location | Target Location |
|---|-----------|-----------------|----------------|
| 1 | AnalyticsEvent entity | `Governance/Domain/Entities/` | `ProductAnalytics/Domain/Entities/` |
| 2 | ProductAnalyticsEndpointModule | `Governance/Api/Endpoints/` | `ProductAnalytics/Api/Endpoints/` |
| 3 | 7 CQRS handlers | `Governance/Application/` | `ProductAnalytics/Application/` |
| 4 | 1 repository | `Governance/Infrastructure/` | `ProductAnalytics/Infrastructure/` |
| 5 | 1 EF configuration | `Governance/Infrastructure/` | `ProductAnalytics/Infrastructure/` |
| 6 | 6 enums | `Governance/Domain/Enums/` | `ProductAnalytics/Domain/Enums/` |
| 7 | 1 DbSet | `GovernanceDbContext` | `ProductAnalyticsDbContext` |
| 8 | 1 table (rename prefix) | `gov_analytics_events` | `pan_analytics_events` |
| 9 | Permission update | `governance:analytics:*` | `analytics:*` |

---

## 10. Extraction Plan Summary

### Pre-conditions

1. Integrations module project structure must exist
2. Product Analytics module project structure must exist
3. Target DbContexts must be created (IntegrationsDbContext, ProductAnalyticsDbContext)
4. Migration strategy must be defined (data migration for renamed tables)

### Extraction Sequence

```
Phase 1: Create target module projects
  ├── Create Integrations module (Domain, Application, Infrastructure, Api layers)
  └── Create ProductAnalytics module (Domain, Application, Infrastructure, Api layers)

Phase 2: Move entities and enums
  ├── Move 3 entities + 6 enums to Integrations
  └── Move 1 entity + 6 enums to ProductAnalytics

Phase 3: Move handlers and repositories  
  ├── Move 8 handlers + 3 repos to Integrations
  └── Move 7 handlers + 1 repo to ProductAnalytics

Phase 4: Move endpoint modules
  ├── Move IntegrationHubEndpointModule to Integrations
  └── Move ProductAnalyticsEndpointModule to ProductAnalytics

Phase 5: Move EF configurations and create target DbContexts
  ├── Move 3 configs to IntegrationsDbContext
  └── Move 1 config to ProductAnalyticsDbContext

Phase 6: Remove from GovernanceDbContext
  ├── Remove 3 DbSets (Integrations)
  ├── Remove 1 DbSet (ProductAnalytics)
  └── Update GovernanceDbContext to 9 entities only

Phase 7: Data migration
  ├── Rename gov_integration_connectors → int_connectors
  ├── Rename gov_ingestion_sources → int_ingestion_sources
  ├── Rename gov_ingestion_executions → int_ingestion_executions
  └── Rename gov_analytics_events → pan_analytics_events

Phase 8: Update permissions
  └── Change governance:analytics:* → analytics:*
```

### Post-extraction Governance Module State

| Metric | Before | After |
|--------|--------|-------|
| Entities | 13 | 9 |
| DbSets | 12 | 9 (8 entity + 1 outbox) |
| EF Configurations | 12 | 9 |
| Tables | 13 | 10 (9 entity + 1 outbox) |
| Endpoint modules | 19 | 16 (+ 2 to evaluate) |
| CQRS handlers | 71 | 56 |
| Enums | 45 | 33 |

---

## Summary

The Governance module currently hosts **4 entities, 2 endpoint modules, 15 CQRS handlers, 4 repositories, 4 EF configurations, 12 enums, 4 DbSets, and 4 tables** that belong to Integrations (3 entities) and Product Analytics (1 entity).

The frontend has already been properly separated. The backend extraction is the remaining work, following a phased approach that ensures no data loss and maintains API compatibility during the transition.
