# Governance Module — Current State Inventory

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Entities Currently in the Module (13)

| # | Entity | File | DbSet | Belongs to Governance? |
|---|--------|------|-------|----------------------|
| 1 | `Team` | `Domain/Entities/Team.cs` | ✅ `Teams` | ✅ YES (organizational unit) |
| 2 | `TeamDomainLink` | `Domain/Entities/TeamDomainLink.cs` | ✅ `TeamDomainLinks` | ✅ YES (team-domain association) |
| 3 | `GovernanceDomain` | `Domain/Entities/GovernanceDomain.cs` | ✅ `Domains` | ✅ YES (governance domain) |
| 4 | `GovernancePack` | `Domain/Entities/GovernancePack.cs` | ✅ `Packs` | ✅ YES (governance rule packs) |
| 5 | `GovernancePackVersion` | `Domain/Entities/GovernancePackVersion.cs` | ✅ `PackVersions` | ✅ YES (pack versioning) |
| 6 | `GovernanceRolloutRecord` | `Domain/Entities/GovernanceRolloutRecord.cs` | ✅ `RolloutRecords` | ✅ YES (pack rollout tracking) |
| 7 | `GovernanceRuleBinding` | `Domain/Entities/GovernanceRuleBinding.cs` | — (no DbSet) | ✅ YES (rule-to-scope binding) |
| 8 | `GovernanceWaiver` | `Domain/Entities/GovernanceWaiver.cs` | ✅ `Waivers` | ✅ YES (compliance waivers) |
| 9 | `DelegatedAdministration` | `Domain/Entities/DelegatedAdministration.cs` | ✅ `DelegatedAdministrations` | ✅ YES (delegation management) |
| 10 | **`IntegrationConnector`** | `Domain/Entities/IntegrationConnector.cs` | ✅ `IntegrationConnectors` | ❌ **BELONGS TO INTEGRATIONS** |
| 11 | **`IngestionSource`** | `Domain/Entities/IngestionSource.cs` | ✅ `IngestionSources` | ❌ **BELONGS TO INTEGRATIONS** |
| 12 | **`IngestionExecution`** | `Domain/Entities/IngestionExecution.cs` | ✅ `IngestionExecutions` | ❌ **BELONGS TO INTEGRATIONS** |
| 13 | **`AnalyticsEvent`** | `Domain/Entities/AnalyticsEvent.cs` | ✅ `AnalyticsEvents` | ❌ **BELONGS TO PRODUCT ANALYTICS** |

**Summary:** 9 entities belong to Governance, 3 to Integrations, 1 to Product Analytics.

---

## 2. Endpoints Currently in the Module (19 endpoint modules)

| # | Endpoint Module | Base Route | Belongs to Governance? |
|---|----------------|-----------|----------------------|
| 1 | GovernancePacksEndpointModule | `/api/v1/governance/packs` | ✅ YES |
| 2 | GovernancePacksVersionsEndpointModule | (part of packs) | ✅ YES |
| 3 | DomainEndpointModule | `/api/v1/governance/domains` | ✅ YES |
| 4 | TeamEndpointModule | `/api/v1/governance/teams` | ✅ YES |
| 5 | GovernanceWaiversEndpointModule | `/api/v1/governance/waivers` | ✅ YES |
| 6 | DelegatedAdminEndpointModule | `/api/v1/governance/delegated-admin` | ✅ YES |
| 7 | ComplianceChecksEndpointModule | `/api/v1/governance/compliance` | ✅ YES |
| 8 | GovernanceComplianceEndpointModule | `/api/v1/governance/compliance-summary` | ✅ YES |
| 9 | EvidencePackagesEndpointModule | `/api/v1/governance/evidence` | ✅ YES |
| 10 | GovernanceRiskEndpointModule | `/api/v1/governance/risk` | ✅ YES |
| 11 | GovernanceReportsEndpointModule | `/api/v1/governance/reports` | ✅ YES |
| 12 | EnterpriseControlsEndpointModule | `/api/v1/governance/controls` | ✅ YES |
| 13 | ExecutiveOverviewEndpointModule | `/api/v1/governance/executive` | ✅ YES |
| 14 | GovernanceFinOpsEndpointModule | `/api/v1/governance/finops` | ✅ YES |
| 15 | PolicyCatalogEndpointModule | `/api/v1/governance/policies` | ✅ YES |
| 16 | ScopedContextEndpointModule | `/api/v1/governance/context` | ✅ YES |
| 17 | **IntegrationHubEndpointModule** | `/api/v1/integrations`, `/api/v1/ingestion` | ❌ **BELONGS TO INTEGRATIONS** |
| 18 | **ProductAnalyticsEndpointModule** | `/api/v1/product-analytics` | ❌ **BELONGS TO PRODUCT ANALYTICS** |
| 19 | OnboardingEndpointModule | `/api/v1/governance/onboarding` | ⚠️ EVALUATE |
| 20 | PlatformStatusEndpointModule | `/api/v1/governance/platform` | ⚠️ EVALUATE |

**Summary:** 16 endpoint modules belong to Governance, 2 to other modules, 2 need evaluation.

---

## 3. Frontend Pages (25)

| # | Page | Route | Belongs to Governance? |
|---|------|-------|----------------------|
| 1 | ExecutiveOverviewPage | `/governance/executive` | ✅ YES |
| 2 | ExecutiveDrillDownPage | `/governance/executive/drilldown` | ✅ YES |
| 3 | ExecutiveFinOpsPage | `/governance/executive/finops` | ✅ YES |
| 4 | ReportsPage | `/governance/reports` | ✅ YES |
| 5 | CompliancePage | `/governance/compliance` | ✅ YES |
| 6 | RiskCenterPage | `/governance/risk` | ✅ YES |
| 7 | RiskHeatmapPage | `/governance/risk/heatmap` | ✅ YES |
| 8 | FinOpsPage | `/governance/finops` | ✅ YES |
| 9 | ServiceFinOpsPage | `/governance/finops/service/:id` | ✅ YES |
| 10 | TeamFinOpsPage | `/governance/finops/team/:id` | ✅ YES |
| 11 | DomainFinOpsPage | `/governance/finops/domain/:id` | ✅ YES |
| 12 | PolicyCatalogPage | `/governance/policies` | ✅ YES |
| 13 | EnterpriseControlsPage | `/governance/controls` | ✅ YES |
| 14 | EvidencePackagesPage | `/governance/evidence` | ✅ YES |
| 15 | MaturityScorecardsPage | `/governance/maturity` | ✅ YES |
| 16 | BenchmarkingPage | `/governance/benchmarking` | ✅ YES |
| 17 | TeamsOverviewPage | `/governance/teams` | ✅ YES |
| 18 | TeamDetailPage | `/governance/teams/:teamId` | ✅ YES |
| 19 | DomainsOverviewPage | `/governance/domains` | ✅ YES |
| 20 | DomainDetailPage | `/governance/domains/:domainId` | ✅ YES |
| 21 | GovernancePacksOverviewPage | `/governance/packs` | ✅ YES |
| 22 | GovernancePackDetailPage | `/governance/packs/:packId` | ✅ YES |
| 23 | WaiversPage | `/governance/waivers` | ✅ YES |
| 24 | DelegatedAdminPage | `/governance/delegated-admin` | ✅ YES |
| 25 | GovernanceConfigurationPage | `/platform/configuration/governance` | ✅ YES |

**All 25 frontend pages are correctly scoped to Governance.** The Integrations and Product Analytics frontend already have their own separate feature folders (`features/integrations/`, `features/product-analytics/`).

---

## 4. Current Permissions

| Permission | Used In | Scope |
|-----------|---------|-------|
| `governance:read` | ALL 24 governance routes | ⚠️ Too broad |
| `governance:packs:read` | Backend packs endpoints | ✅ Granular |
| `governance:packs:write` | Backend packs endpoints | ✅ Granular |
| `governance:domains:read/write` | Backend domain endpoints | ✅ Granular |
| `governance:teams:read/write` | Backend team endpoints | ✅ Granular |
| `governance:waivers:read/write` | Backend waiver endpoints | ✅ Granular |
| `governance:admin:read/write` | Backend delegation endpoints | ✅ Granular |
| `governance:compliance:read/write` | Backend compliance endpoints | ✅ Granular |
| `governance:analytics:read/write` | Backend analytics endpoints | ❌ Belongs to Product Analytics |
| `governance:evidence:read` | Backend evidence endpoints | ✅ Granular |
| `governance:finops:read` | Backend finops endpoints | ✅ Granular |
| `governance:risk:read` | Backend risk endpoints | ✅ Granular |
| `governance:reports:read` | Backend reports endpoints | ✅ Granular |
| `governance:controls:read` | Backend controls endpoints | ✅ Granular |
| `governance:policies:read/write` | Backend policy endpoints | ✅ Granular |
| `integrations:read/write` | Backend integration endpoints | ❌ Belongs to Integrations |
| `platform:admin:read` | GovernanceConfigurationPage | ✅ Platform-level |

**Critical gap:** Backend has 12+ granular permissions but frontend uses only `governance:read` for all 24 pages.

---

## 5. What Belongs to Integrations (currently inside Governance)

### Backend
| Component | File | Notes |
|-----------|------|-------|
| `IntegrationConnector` entity | `Domain/Entities/IntegrationConnector.cs` | Connector management |
| `IngestionSource` entity | `Domain/Entities/IngestionSource.cs` | Data source tracking |
| `IngestionExecution` entity | `Domain/Entities/IngestionExecution.cs` | Execution history |
| `IntegrationHubEndpointModule` | `API/Endpoints/IntegrationHubEndpointModule.cs` | `/api/v1/integrations/*` |
| 8 CQRS handlers | `Application/Features/` | ListConnectors, GetConnector, ListSources, ListExecutions, GetHealth, GetFreshness, RetryConnector, ReprocessExecution |
| 3 repository interfaces | `Application/Abstractions/` | IIntegrationConnectorRepository, IIngestionSourceRepository, IIngestionExecutionRepository |
| 3 EF configurations | `Infrastructure/Persistence/Configurations/` | IntegrationConnectorConfiguration, IngestionSourceConfiguration, IngestionExecutionConfiguration |
| 6 enums | `Domain/Enums/` | ConnectorStatus, ConnectorHealth, SourceStatus, SourceTrustLevel, FreshnessStatus, ExecutionResult |
| 3 DbSets | `GovernanceDbContext.cs` | IntegrationConnectors, IngestionSources, IngestionExecutions |
| 3 tables | Migrations | `gov_integration_connectors`, `gov_ingestion_sources`, `gov_ingestion_executions` |

### Frontend
Already separated: `src/frontend/src/features/integrations/` (4 pages, own routes, own API client, `integrations:read` permission)

---

## 6. What Belongs to Product Analytics (currently inside Governance)

### Backend
| Component | File | Notes |
|-----------|------|-------|
| `AnalyticsEvent` entity | `Domain/Entities/AnalyticsEvent.cs` | Usage event recording |
| `ProductAnalyticsEndpointModule` | `API/Endpoints/ProductAnalyticsEndpointModule.cs` | `/api/v1/product-analytics/*` |
| 7 CQRS handlers | `Application/Features/` | RecordAnalyticsEvent, GetAnalyticsSummary, GetModuleAdoption, GetPersonaUsage, GetJourneys, GetValueMilestones, GetFrictionIndicators |
| 1 repository interface | `Application/Abstractions/` | IAnalyticsEventRepository |
| 1 EF configuration | `Infrastructure/Persistence/Configurations/` | AnalyticsEventConfiguration |
| 6 enums | `Domain/Enums/` | AnalyticsEventType, ProductModule, WasteSignalType, FrictionSignalType, ValueMilestoneType, JourneyStatus |
| 1 DbSet | `GovernanceDbContext.cs` | AnalyticsEvents |
| 1 table | Migrations | `gov_analytics_events` |

### Frontend
Already separated: `src/frontend/src/features/product-analytics/` (5 pages, own routes, own API client, `analytics:read` permission)

---

## 7. What Clearly Belongs to Governance

### Core Governance Entities (9)
- Team, TeamDomainLink, GovernanceDomain — Organizational structure
- GovernancePack, GovernancePackVersion, GovernanceRolloutRecord — Governance rule packs
- GovernanceRuleBinding — Rule-to-scope binding
- GovernanceWaiver — Compliance exceptions
- DelegatedAdministration — Administrative delegation

### Core Governance Capabilities
- Policy management (create, read, enforce)
- Compliance assessment and reporting
- Risk analysis and heatmaps
- FinOps governance (cost governance, efficiency)
- Evidence packages (compliance proof)
- Enterprise controls management
- Executive overview dashboards
- Maturity scorecards and benchmarking
- Governance pack lifecycle (create, version, rollout, waiver)
- Team and domain management
- Reports generation

---

## 8. What Is in the Module Only for Technical Convenience

| Component | Reason It's Here | Where It Should Be |
|-----------|-----------------|-------------------|
| IntegrationHub endpoints + entities | Built alongside Governance before module extraction was decided | `src/modules/integrations/` |
| ProductAnalytics endpoints + entities | Built alongside Governance before module extraction was decided | `src/modules/productanalytics/` |
| PlatformStatusEndpointModule | Platform health monitoring — not governance | Platform or Operational Intelligence |
| OnboardingEndpointModule | User onboarding context — uses `governance:teams:read` | Evaluate: keep or move to platform |

---

## 9. Enums Summary (45 total)

| Category | Count | Belongs to Governance? |
|----------|-------|----------------------|
| Governance Pack enums | 4 | ✅ YES |
| Governance Rule enums | 3 | ✅ YES |
| Waiver/Compliance enums | 9 | ✅ YES |
| Risk/Control enums | 4 | ✅ YES |
| Delegation/Ownership enums | 2 | ✅ YES |
| Deployment/Rollout enums | 3 | ✅ YES |
| Maturity/Quality enums | 2 | ✅ YES |
| Team/Platform enums | 7 | ✅ YES |
| Connector/Integration enums | 2 | ❌ INTEGRATIONS |
| Ingestion enums | 3 | ❌ INTEGRATIONS |
| Analytics/Signal enums | 6 | ❌ PRODUCT ANALYTICS |

---

## 10. Database Tables (12 current)

| Table | Belongs to | Target Prefix |
|-------|-----------|---------------|
| `gov_teams` | ✅ Governance | `gov_` |
| `gov_team_domain_links` | ✅ Governance | `gov_` |
| `gov_domains` | ✅ Governance | `gov_` |
| `gov_packs` | ✅ Governance | `gov_` |
| `gov_pack_versions` | ✅ Governance | `gov_` |
| `gov_rollout_records` | ✅ Governance | `gov_` |
| `gov_waivers` | ✅ Governance | `gov_` |
| `gov_delegated_admins` | ✅ Governance | `gov_` |
| `gov_integration_connectors` | ❌ Integrations | → `int_integration_connectors` |
| `gov_ingestion_sources` | ❌ Integrations | → `int_ingestion_sources` |
| `gov_ingestion_executions` | ❌ Integrations | → `int_ingestion_executions` |
| `gov_analytics_events` | ❌ Product Analytics | → `pan_analytics_events` |
| `gov_outbox_messages` | ✅ Governance | `gov_` |
