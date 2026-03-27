# Catalog Module — Frontend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Pages Inventory

### Catalog Pages (9 routed)

| # | Page | File | Route | Status |
|---|------|------|-------|--------|
| 1 | ServiceCatalogListPage | `catalog/pages/ServiceCatalogListPage.tsx` | `/services` | ✅ Routed |
| 2 | ServiceCatalogPage | `catalog/pages/ServiceCatalogPage.tsx` | `/services/graph` | ✅ Routed |
| 3 | ServiceDetailPage | `catalog/pages/ServiceDetailPage.tsx` | `/services/:serviceId` | ✅ Routed |
| 4 | SourceOfTruthExplorerPage | `catalog/pages/SourceOfTruthExplorerPage.tsx` | `/source-of-truth` | ✅ Routed |
| 5 | ServiceSourceOfTruthPage | `catalog/pages/ServiceSourceOfTruthPage.tsx` | `/source-of-truth/service/:serviceId` | ✅ Routed |
| 6 | ContractSourceOfTruthPage | `catalog/pages/ContractSourceOfTruthPage.tsx` | `/source-of-truth/contract/:contractId` | ✅ Routed |
| 7 | DeveloperPortalPage | `catalog/pages/DeveloperPortalPage.tsx` | `/portal` | ✅ Routed |
| 8 | GlobalSearchPage | `catalog/pages/GlobalSearchPage.tsx` | `/search` | ✅ Routed |
| 9 | CatalogContractsConfigurationPage | `catalog/pages/CatalogContractsConfigurationPage.tsx` | `/platform/configuration/catalog-contracts` | ✅ Routed |

### Orphaned Pages (in catalog feature — likely dead code)

| Page | File | Route | Status |
|------|------|-------|--------|
| ContractDetailPage | `catalog/pages/ContractDetailPage.tsx` | None | ❌ Orphaned |
| ContractListPage | `catalog/pages/ContractListPage.tsx` | None | ❌ Orphaned |
| ContractsPage | `catalog/pages/ContractsPage.tsx` | None | ❌ Orphaned |

**These 3 pages are likely superseded by the Contracts module frontend** (`features/contracts/`). They should be removed or formally deprecated.

---

## 2. Route Review

| Check | Status |
|-------|--------|
| All sidebar items have matching routes | ✅ |
| All routes have matching page components | ✅ |
| Route order prevents param catch-all conflicts | ✅ (specific routes before `/:serviceId`) |
| All routes wrapped in ProtectedRoute | ✅ |
| Redirect routes work correctly | ✅ |

---

## 3. Menu Review

**Sidebar items (AppSidebar.tsx, catalog section):**

| Label Key | Route | Permission | Has Matching Route |
|-----------|-------|-----------|-------------------|
| sidebar.serviceCatalog | /services | catalog:assets:read | ✅ |
| sidebar.serviceGraph | /services/graph | catalog:assets:read | ✅ |
| sidebar.sourceOfTruth | /source-of-truth | catalog:assets:read | ✅ |
| sidebar.developerPortal | /portal | developer-portal:read | ✅ |
| sidebar.globalSearch | /search | catalog:assets:read | ✅ |

---

## 4. Component Assessment

### Shared Components

| Component | File | Status |
|-----------|------|--------|
| CatalogSkeleton | `catalog/components/CatalogSkeleton.tsx` | ✅ Loading placeholder |
| CatalogTable | `catalog/components/CatalogTable.tsx` | ✅ Reusable data table |
| CatalogBadges | `catalog/components/CatalogBadges.tsx` | ✅ Status badges |
| ServiceCatalogOverviewTab | `catalog/components/ServiceCatalogOverviewTab.tsx` | ✅ Service stats |
| ServiceCatalogServicesTab | `catalog/components/ServiceCatalogServicesTab.tsx` | ✅ Service listing |
| ContractVersionDetailPanel | `catalog/components/ContractVersionDetailPanel.tsx` | ⚠️ May be orphaned |

### ServiceCatalogPage (1010 lines)

The graph visualization page is the most complex component in the Catalog frontend:
- D3.js-based interactive topology graph
- Node health overlays
- Snapshot time-travel controls
- Saved view management
- Impact propagation highlighting
- Filter and search capabilities

---

## 5. Loading/Error/Empty States

| State | ServiceCatalogListPage | ServiceCatalogPage | ServiceDetailPage |
|-------|----------------------|--------------------|--------------------|
| Loading | ✅ CatalogSkeleton | ✅ Spinner | ✅ Spinner |
| Error | ✅ Error message | ✅ Error message | ✅ Error message |
| Empty | ✅ Empty state | ✅ Empty graph state | N/A (404 if missing) |

| State | SourceOfTruthExplorerPage | DeveloperPortalPage | GlobalSearchPage |
|-------|--------------------------|--------------------|--------------------|
| Loading | ✅ Spinner | ✅ Spinner | ✅ Spinner |
| Error | ✅ Error message | ✅ Error message | ✅ Error message |
| Empty | ✅ Empty state | ✅ Empty state | ✅ No results state |

| State | CatalogContractsConfigurationPage |
|-------|----------------------------------|
| Loading | ⚠️ Needs verification |
| Error | ⚠️ Needs verification |
| Empty | ⚠️ Needs verification |

---

## 6. API Integration Review

### Service Catalog API (`catalog/api/serviceCatalog.ts`)

| Hook | Real API | Status |
|------|----------|--------|
| useServiceList | ✅ GET /api/v1/catalog/services | ✅ |
| useServiceDetail | ✅ GET /api/v1/catalog/services/{id} | ✅ |
| useServiceGraph | ✅ GET /api/v1/catalog/graph | ✅ |
| useApiDetail | ✅ GET /api/v1/catalog/apis/{id} | ✅ |
| useRegisterService | ✅ POST /api/v1/catalog/services | ✅ |
| useRegisterApi | ✅ POST /api/v1/catalog/apis | ✅ |
| useUpdateApi | ✅ PATCH /api/v1/catalog/apis/{id} | ✅ |
| useCreateSnapshot | ✅ POST /api/v1/catalog/snapshots | ✅ |
| useListSnapshots | ✅ GET /api/v1/catalog/snapshots | ✅ |
| useSaveGraphView | ✅ POST /api/v1/catalog/views | ✅ |
| useListSavedViews | ✅ GET /api/v1/catalog/views | ✅ |
| useNodeHealth | ✅ GET /api/v1/catalog/health/{nodeId} | ✅ |
| useImpactPropagation | ✅ GET /api/v1/catalog/impact/{serviceId} | ✅ |

### Developer Portal API (`catalog/api/developerPortal.ts`)

| Hook | Real API | Status |
|------|----------|--------|
| usePortalSearch | ✅ GET /api/v1/developerportal/catalog/search | ✅ |
| useMyApis | ✅ GET /api/v1/developerportal/catalog/my-apis | ✅ |
| useConsumingApis | ✅ GET /api/v1/developerportal/catalog/consuming | ✅ |
| useSubscriptions | ✅ GET /api/v1/developerportal/subscriptions | ✅ |
| useCreateSubscription | ✅ POST /api/v1/developerportal/subscriptions | ✅ |
| useDeleteSubscription | ✅ DELETE /api/v1/developerportal/subscriptions/{id} | ✅ |
| useGenerateCode | ✅ POST /api/v1/developerportal/codegen | ✅ |

### Source of Truth API (`catalog/api/sourceOfTruth.ts`)

| Hook | Real API | Status |
|------|----------|--------|
| useServiceSourceOfTruth | ✅ GET /api/v1/catalog/source-of-truth/services/{id} | ✅ |
| useContractSourceOfTruth | ✅ GET /api/v1/catalog/source-of-truth/contracts/{id} | ✅ |
| useSourceOfTruthSearch | ✅ POST /api/v1/catalog/source-of-truth/search | ✅ |
| useSourceOfTruthCoverage | ✅ GET /api/v1/catalog/source-of-truth/coverage | ✅ |

### Global Search API (`catalog/api/globalSearch.ts`)

| Hook | Real API | Status |
|------|----------|--------|
| useGlobalSearch | ✅ POST /api/v1/catalog/source-of-truth/global-search | ✅ |

---

## 7. i18n Review

| Namespace | en | pt-PT | pt-BR | es | Notes |
|-----------|-----|-------|-------|-----|-------|
| catalog.services | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | Core catalog i18n |
| catalog.graph | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | Graph visualization labels |
| catalog.sourceOfTruth | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | SoT explorer |
| catalog.portal | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | Developer portal |
| catalog.search | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | Global search |
| catalog.configuration | ✅ | ⚠️ Verify | ⚠️ Verify | ⚠️ Verify | Configuration page |

---

## 8. Corrections Backlog

### HIGH Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| FE-01 | Remove or formally deprecate 3 orphaned pages (ContractDetailPage, ContractListPage, ContractsPage) | `catalog/pages/` | 30min |
| FE-02 | Verify `ContractVersionDetailPanel` is not orphaned | `catalog/components/ContractVersionDetailPanel.tsx` | 15min |
| FE-03 | Verify CatalogContractsConfigurationPage loading/error/empty states | `CatalogContractsConfigurationPage.tsx` | 30min |

### MEDIUM Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| FE-04 | Verify i18n completeness for pt-BR and es on all catalog pages | `locales/*.json` | 2h |
| FE-05 | Add explicit lifecycle transition UI for service/API status changes | `ServiceDetailPage.tsx` | 3h |
| FE-06 | Remove duplicate contract API files from `catalog/api/` if they duplicate `contracts/api/` | `catalog/api/contracts.ts`, `catalog/api/contractStudio.ts` | 30min |

### LOW Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| FE-07 | Add breadcrumb consistency across all 9 catalog pages | Page components | 1h |
| FE-08 | Verify responsive behavior on ServiceCatalogPage (1010 lines, complex D3 graph) | `ServiceCatalogPage.tsx` | 1h |
| FE-09 | Add keyboard navigation support to graph visualization | `ServiceCatalogPage.tsx` | 2h |
| FE-10 | Verify accessibility (ARIA labels) on interactive graph elements | `ServiceCatalogPage.tsx` | 1h |
