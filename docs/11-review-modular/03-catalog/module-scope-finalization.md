# Catalog Module — Scope Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Existing Functionality

### Fully Implemented (30+ CQRS features + 9 frontend pages)

| Capability | Backend Features | Frontend Pages | Status |
|-----------|-----------------|---------------|--------|
| Service registration | RegisterService | ServiceCatalogListPage | ✅ Complete |
| API registration | RegisterApi | ServiceCatalogListPage | ✅ Complete |
| Service listing & filtering | ListServices, GetServiceDetail | ServiceCatalogListPage | ✅ Complete |
| API listing & detail | ListApis, GetApiDetail | ServiceDetailPage | ✅ Complete |
| Dependency graph visualization | GetServiceGraph | ServiceCatalogPage (1010 lines) | ✅ Complete |
| Consumer registration | RegisterConsumer | ServiceDetailPage | ✅ Complete |
| Consumer relationship mapping | AddConsumerRelationship | ServiceDetailPage | ✅ Complete |
| Impact propagation analysis | ComputeImpactPropagation | ServiceCatalogPage | ✅ Complete |
| Graph snapshots (time-travel) | CreateGraphSnapshot, ListGraphSnapshots | ServiceCatalogPage | ✅ Complete |
| Temporal diff between snapshots | CompareGraphSnapshots | ServiceCatalogPage | ✅ Complete |
| Health overlay | RecordNodeHealth, GetNodeHealth | ServiceCatalogPage | ✅ Complete |
| Saved graph views | SaveGraphView, ListSavedGraphViews | ServiceCatalogPage | ✅ Complete |
| Update API metadata | UpdateApiAsset | ServiceDetailPage | ✅ Complete |
| Discovery source configuration | ConfigureDiscoverySource | (via API) | ✅ Complete |
| Developer Portal search | SearchPortalCatalog | DeveloperPortalPage | ✅ Complete |
| My APIs view | GetMyApis | DeveloperPortalPage | ✅ Complete |
| Consuming APIs view | GetConsumingApis | DeveloperPortalPage | ✅ Complete |
| Portal asset detail | GetPortalAssetDetail | DeveloperPortalPage | ✅ Complete |
| API subscriptions | CreateSubscription, DeleteSubscription, ListSubscriptions | DeveloperPortalPage | ✅ Complete |
| Code generation | GenerateCode | DeveloperPortalPage | ✅ Complete |
| Playground sessions | GetPlaygroundHistory | DeveloperPortalPage | ✅ Complete |
| Portal analytics | RecordAnalyticsEvent, GetAnalytics | DeveloperPortalPage | ✅ Complete |
| Source of Truth: service view | GetServiceSourceOfTruth | SourceOfTruthExplorerPage | ✅ Complete |
| Source of Truth: contract view | GetContractSourceOfTruth | SourceOfTruthExplorerPage | ✅ Complete |
| Source of Truth: search | SearchSourceOfTruth | SourceOfTruthExplorerPage | ✅ Complete |
| Source of Truth: coverage | GetSourceOfTruthCoverage | SourceOfTruthExplorerPage | ✅ Complete |
| Global search | GlobalSearch | GlobalSearchPage | ✅ Complete |
| Configuration page | — | CatalogContractsConfigurationPage | ✅ Complete |

---

## 2. Partially Implemented Functionality

| Feature | Frontend | Backend | Gap |
|---------|----------|---------|-----|
| Asset lifecycle management (decommission flow) | ⚠️ Lifecycle badges exist | ⚠️ `LifecycleStatus` enum exists (Alpha→Retired) but no dedicated transition endpoint | **Need explicit DecommissionService/RetireApi handler** |
| Bulk import of assets | ❌ Not implemented | ❌ Not implemented | **No bulk registration endpoint** |
| Asset criticality scoring | ⚠️ `Criticality` enum exists | ⚠️ Set during registration only | **Need recalculation/update capability** |
| Discovery source sync | ⚠️ Entity exists | ⚠️ Configuration endpoint exists | **No automated sync execution** |

---

## 3. Missing but Mandatory Functionality

| Feature | Priority | Rationale |
|---------|----------|-----------|
| Asset lifecycle transition endpoint | HIGH | Must support Alpha → Beta → Stable → Deprecated → Retired transitions explicitly |
| RowVersion / xmin on ServiceAsset, ApiAsset | HIGH | No optimistic concurrency — concurrent edits silently overwrite |
| Check constraints for enums (ServiceType, ExposureType, LifecycleStatus, etc.) | MEDIUM | Database allows invalid enum values |
| Filtered indexes with `WHERE is_deleted = false` | MEDIUM | Soft-deleted rows degrade query performance |
| Bulk asset import endpoint | MEDIUM | Teams cannot onboard many services at once |
| Asset criticality recalculation | LOW | Criticality is static after creation |
| Discovery source execution (auto-sync) | LOW | Current flow is config-only, no automated execution |

---

## 4. Subdomain Scope Assessment

### Graph Subdomain

| Capability | Status | Notes |
|-----------|--------|-------|
| Service CRUD | ✅ Complete | RegisterService, GetServiceDetail, ListServices |
| API CRUD | ✅ Complete | RegisterApi, GetApiDetail, UpdateApiAsset |
| Consumer CRUD | ✅ Complete | RegisterConsumer, AddConsumerRelationship |
| Dependency graph | ✅ Complete | Full topology with visualization |
| Impact analysis | ✅ Complete | Blast radius via graph traversal |
| Snapshots | ✅ Complete | Time-travel, temporal diff |
| Health overlay | ✅ Complete | Per-node health records |
| Saved views | ✅ Complete | Persistent graph configurations |
| Discovery sources | ⚠️ Partial | Config exists, no auto-execution |
| Lifecycle transitions | ⚠️ Partial | Enum exists, no dedicated endpoint |

### Portal Subdomain

| Capability | Status | Notes |
|-----------|--------|-------|
| Catalog search | ✅ Complete | Full-text search with filters |
| Subscriptions | ✅ Complete | CRUD for change notifications |
| Code generation | ✅ Complete | Multi-language from contracts |
| Playground | ✅ Complete | Interactive API testing |
| Analytics | ✅ Complete | Usage tracking and reporting |
| My APIs / Consuming | ✅ Complete | Ownership and consumption views |

### SourceOfTruth Subdomain

| Capability | Status | Notes |
|-----------|--------|-------|
| Service source view | ✅ Complete | Linked external references |
| Contract source view | ✅ Complete | Linked external references |
| Search across sources | ✅ Complete | Unified search |
| Coverage metrics | ✅ Complete | Source-of-truth completeness |
| Global search | ✅ Complete | Cross-catalog search |

---

## 5. Scope Boundaries

### What Catalog Owns (Source of Truth for)

1. **Asset identity** — What services and APIs exist in the platform
2. **Asset metadata** — Name, description, type, protocol, exposure, criticality
3. **Ownership** — Which team owns which asset
4. **Topology** — How assets relate to each other (dependencies, consumers, producers)
5. **Health metadata** — Operational health status overlays on assets
6. **Discovery** — How assets are found and registered
7. **Portal** — Developer-facing view of the asset catalog
8. **External references** — Links to external systems (Jira, Backstage, Datadog)
9. **Search** — Global and scoped search across all catalog assets

### What Catalog Does NOT Own

| Excluded Capability | Correct Module |
|-------------------|---------------|
| Contract lifecycle management | Contracts (04) |
| Change tracking and validation | Change Governance (05) |
| Incident management | Operational Intelligence (06) |
| AI-assisted analysis | AI & Knowledge (07) |
| Compliance reporting | Governance (08) |
| Cost attribution | Governance / FinOps |
| User/team identity | Identity & Access (01) |
| Environment definitions | Environment Management (02) |

---

## 6. Minimum Complete Module Definition

### Must Have (blocks closure)

1. ✅ All 9 catalog pages routed and accessible
2. ✅ All 3 DbContexts operational with RLS, audit, soft-delete
3. ✅ Graph visualization with topology, health overlay, snapshots
4. ✅ Developer Portal with search, subscriptions, playground
5. ✅ Source of Truth with linking and coverage
6. ⬜ RowVersion/xmin on ServiceAsset and ApiAsset
7. ⬜ Asset lifecycle transition endpoint (explicit state machine)
8. ⬜ Table prefix migration from `eg_`/`dp_` to `cat_`

### Should Have (improves quality)

9. ⬜ Check constraints for enums at database level
10. ⬜ Filtered indexes with `WHERE is_deleted = false`
11. ⬜ Bulk asset import endpoint
12. ⬜ Remove orphaned legacy pages (ContractDetailPage, ContractListPage, ContractsPage)

### Nice to Have (polish)

13. ⬜ Asset criticality recalculation
14. ⬜ Discovery source auto-sync execution
15. ⬜ i18n verified for all locales on all pages
16. ⬜ Module documentation complete
