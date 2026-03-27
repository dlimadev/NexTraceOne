# Catalog Module — Backend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Endpoints Inventory

### ServiceCatalogEndpointModule (~21 endpoints)

**File:** `src/modules/catalog/NexTraceOne.Catalog.API/Graph/Endpoints/Endpoints/ServiceCatalogEndpointModule.cs`

| # | Endpoint | Method | Permission | Handler | Status |
|---|----------|--------|-----------|---------|--------|
| 1 | `/api/v1/catalog/services` | POST | catalog:assets:write | RegisterService | ✅ |
| 2 | `/api/v1/catalog/apis` | POST | catalog:assets:write | RegisterApi | ✅ |
| 3 | `/api/v1/catalog/apis/{id}/consumers` | POST | catalog:assets:write | AddConsumerRelationship | ✅ |
| 4 | `/api/v1/catalog/graph` | GET | catalog:assets:read | GetServiceGraph | ✅ |
| 5 | `/api/v1/catalog/apis/{id}` | GET | catalog:assets:read | GetApiDetail | ✅ |
| 6 | `/api/v1/catalog/services/{id}` | GET | catalog:assets:read | GetServiceDetail | ✅ |
| 7 | `/api/v1/catalog/services` | GET | catalog:assets:read | ListServices | ✅ |
| 8 | `/api/v1/catalog/apis/{id}` | PATCH | catalog:assets:write | UpdateApiAsset | ✅ |
| 9 | `/api/v1/catalog/snapshots` | POST | catalog:assets:write | CreateGraphSnapshot | ✅ |
| 10 | `/api/v1/catalog/snapshots` | GET | catalog:assets:read | ListGraphSnapshots | ✅ |
| 11 | `/api/v1/catalog/snapshots/compare` | POST | catalog:assets:read | CompareGraphSnapshots | ✅ |
| 12 | `/api/v1/catalog/views` | POST | catalog:assets:write | SaveGraphView | ✅ |
| 13 | `/api/v1/catalog/views` | GET | catalog:assets:read | ListSavedGraphViews | ✅ |
| 14 | `/api/v1/catalog/health` | POST | catalog:assets:write | RecordNodeHealth | ✅ |
| 15 | `/api/v1/catalog/health/{nodeId}` | GET | catalog:assets:read | GetNodeHealth | ✅ |
| 16 | `/api/v1/catalog/impact/{serviceId}` | GET | catalog:assets:read | ComputeImpactPropagation | ✅ |
| 17 | `/api/v1/catalog/discovery-sources` | POST | catalog:assets:write | ConfigureDiscoverySource | ✅ |
| 18 | `/api/v1/catalog/consumers` | POST | catalog:assets:write | RegisterConsumer | ✅ |
| 19 | `/api/v1/catalog/search` | POST | catalog:assets:read | SearchAssets | ✅ |
| 20 | `/api/v1/catalog/services/{id}/apis` | GET | catalog:assets:read | ListServiceApis | ✅ |
| 21 | `/api/v1/catalog/services/{id}/dependencies` | GET | catalog:assets:read | GetServiceDependencies | ✅ |

### Contracts Endpoints (35 endpoints — TO BE EXTRACTED)

**Files:**
- `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/Endpoints/ContractsEndpointModule.cs` (24 endpoints)
- `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/Endpoints/ContractStudioEndpointModule.cs` (11 endpoints)

These 35 endpoints belong to the **Contracts module** and will move to `src/modules/contracts/` during extraction (OI-01). They are documented in `docs/11-review-modular/04-contracts/backend-functional-corrections.md`.

**Action:** No corrections needed here — only extraction planning.

### SourceOfTruthEndpointModule (5 endpoints)

**File:** `src/modules/catalog/NexTraceOne.Catalog.API/SourceOfTruth/Endpoints/Endpoints/SourceOfTruthEndpointModule.cs`

| # | Endpoint | Method | Permission | Handler | Status |
|---|----------|--------|-----------|---------|--------|
| 22 | `/api/v1/catalog/source-of-truth/services/{id}` | GET | catalog:assets:read | GetServiceSourceOfTruth | ✅ |
| 23 | `/api/v1/catalog/source-of-truth/contracts/{id}` | GET | catalog:assets:read | GetContractSourceOfTruth | ✅ |
| 24 | `/api/v1/catalog/source-of-truth/search` | POST | catalog:assets:read | SearchSourceOfTruth | ✅ |
| 25 | `/api/v1/catalog/source-of-truth/coverage` | GET | catalog:assets:read | GetSourceOfTruthCoverage | ✅ |
| 26 | `/api/v1/catalog/source-of-truth/global-search` | POST | catalog:assets:read | GlobalSearch | ✅ |

### DeveloperPortalEndpointModule (~15 endpoints)

**File:** `src/modules/catalog/NexTraceOne.Catalog.API/Portal/Endpoints/Endpoints/DeveloperPortalEndpointModule.cs`

| # | Endpoint | Method | Permission | Handler | Status |
|---|----------|--------|-----------|---------|--------|
| 27 | `/api/v1/developerportal/catalog/search` | GET | developer-portal:read | SearchPortalCatalog | ✅ |
| 28 | `/api/v1/developerportal/catalog/my-apis` | GET | developer-portal:read | GetMyApis | ✅ |
| 29 | `/api/v1/developerportal/catalog/consuming` | GET | developer-portal:read | GetConsumingApis | ✅ |
| 30 | `/api/v1/developerportal/catalog/{id}` | GET | developer-portal:read | GetPortalAssetDetail | ✅ |
| 31 | `/api/v1/developerportal/subscriptions` | GET | developer-portal:read | ListSubscriptions | ✅ |
| 32 | `/api/v1/developerportal/subscriptions` | POST | developer-portal:write | CreateSubscription | ✅ |
| 33 | `/api/v1/developerportal/subscriptions/{id}` | DELETE | developer-portal:write | DeleteSubscription | ✅ |
| 34 | `/api/v1/developerportal/codegen` | POST | developer-portal:write | GenerateCode | ✅ |
| 35 | `/api/v1/developerportal/playground/history` | GET | developer-portal:read | GetPlaygroundHistory | ✅ |
| 36 | `/api/v1/developerportal/analytics` | POST | developer-portal:write | RecordAnalyticsEvent | ✅ |
| 37 | `/api/v1/developerportal/analytics` | GET | developer-portal:read | GetAnalytics | ✅ |

**Total Catalog-owned endpoints: ~37** (excluding 35 Contracts endpoints pending extraction)

---

## 2. Endpoint → Use Case Mapping

All 37 Catalog-owned endpoints map 1:1 to CQRS handlers. No dead endpoints found.

---

## 3. Dead Endpoints

**None identified.** All endpoints have corresponding CQRS handlers.

---

## 4. Incomplete Endpoints / Missing Backend Features

| # | Gap | Priority | Details |
|---|-----|----------|---------|
| BE-01 | **No asset lifecycle transition endpoint** | HIGH | `LifecycleStatus` enum exists but no explicit `TransitionAssetLifecycle` handler to move services/APIs through Alpha → Beta → Stable → Deprecated → Retired |
| BE-02 | **No bulk import endpoint** | MEDIUM | Teams cannot register multiple services/APIs in one call |
| BE-03 | **No UpdateService endpoint** | MEDIUM | `UpdateApiAsset` exists but no equivalent `UpdateServiceAsset` for metadata changes |
| BE-04 | **No DecommissionService/RetireApi endpoint** | MEDIUM | No explicit decommissioning flow |
| BE-05 | **No DeleteService/DeleteApi soft-delete endpoint** | LOW | Soft delete works via filter but no explicit endpoint |
| BE-06 | **Discovery source sync execution** | LOW | Configuration endpoint exists but no auto-sync execution endpoint |

---

## 5. Validation Review

| Handler | Validation | Status |
|---------|-----------|--------|
| RegisterService | FluentValidation on name, type, criticality | ✅ |
| RegisterApi | Validates serviceId, name, protocol, exposure | ✅ |
| AddConsumerRelationship | Validates apiAssetId, consumerAssetId | ✅ |
| UpdateApiAsset | Validates apiAssetId, changed fields | ✅ |
| CreateGraphSnapshot | Validates snapshot data | ✅ |
| SaveGraphView | Validates name, filter config | ✅ |
| RecordNodeHealth | Validates nodeId, health status | ✅ |
| All other handlers | FluentValidation validators present | ✅ |

---

## 6. Error Handling Review

| Aspect | Status |
|--------|--------|
| Domain error catalog | ✅ `CatalogErrors.cs` / `GraphErrors.cs` with i18n codes |
| Result pattern | ✅ Handlers return `Result<T>` |
| Validation pipeline | ✅ FluentValidation with MediatR pipeline |
| 404 handling | ✅ Entity not found returns proper error |
| Concurrency conflict | ❌ No `DbUpdateConcurrencyException` handling (no RowVersion yet) |

---

## 7. Audit Trail Review

| Operation | Audit | Status |
|-----------|-------|--------|
| Register service | CreatedAt/By via interceptor | ✅ |
| Register API | CreatedAt/By via interceptor | ✅ |
| Update API | UpdatedAt/By via interceptor | ✅ |
| Add consumer relationship | CreatedAt/By via interceptor | ✅ |
| Create snapshot | CreatedAt/By via interceptor | ✅ |
| Record health | CreatedAt/By via interceptor | ✅ |
| Asset lifecycle transition | Not implemented | ❌ Should have domain event |
| Soft delete | IsDeleted flag | ✅ |

---

## 8. Corrections Backlog

### HIGH Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| BE-01 | Create `TransitionAssetLifecycle` CQRS handler (service + API) | New files in `Application/Graph/Features/` | 4h |
| BE-02 | Add `UseXminAsConcurrencyToken()` to ServiceAsset, ApiAsset | 2 EF config files | 30min |
| BE-03 | Handle `DbUpdateConcurrencyException` in write handlers | All write handlers | 1h |

### MEDIUM Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| BE-04 | Create `UpdateServiceAsset` handler | New file in `Application/Graph/Features/` | 2h |
| BE-05 | Create `BulkImportAssets` handler | New file in `Application/Graph/Features/` | 4h |
| BE-06 | Create `DecommissionAsset` handler (service + API) | New file in `Application/Graph/Features/` | 2h |
| BE-07 | Add domain events for asset lifecycle transitions | `ServiceAsset.cs`, `ApiAsset.cs` | 1h |

### LOW Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| BE-08 | Create explicit soft-delete endpoints for service/API | New endpoint entries | 1h |
| BE-09 | Add discovery source sync execution handler | New file in `Application/Graph/Features/` | 3h |
| BE-10 | Verify all `ICatalogGraphModule` and `IDeveloperPortalModule` methods are implemented | Infrastructure service files | 1h |
| BE-11 | Add integration event publishing for asset registration/update | Write handlers | 2h |
