# Contracts Module — Frontend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Pages Inventory

| # | Page | File | Route | Status |
|---|------|------|-------|--------|
| 1 | ContractCatalogPage | `contracts/catalog/ContractCatalogPage.tsx` | `/contracts` | ✅ Routed |
| 2 | CreateServicePage | `contracts/create/CreateServicePage.tsx` | `/contracts/new` | ✅ Routed |
| 3 | DraftStudioPage | `contracts/studio/DraftStudioPage.tsx` | `/contracts/studio/:draftId` | ✅ Routed |
| 4 | ContractWorkspacePage | `contracts/workspace/ContractWorkspacePage.tsx` | `/contracts/:contractVersionId` | ✅ Routed |
| 5 | ContractGovernancePage | `contracts/governance/ContractGovernancePage.tsx` | `/contracts/governance` | ✅ **FIXED** |
| 6 | SpectralRulesetManagerPage | `contracts/spectral/SpectralRulesetManagerPage.tsx` | `/contracts/spectral` | ✅ **FIXED** |
| 7 | CanonicalEntityCatalogPage | `contracts/canonical/CanonicalEntityCatalogPage.tsx` | `/contracts/canonical` | ✅ **FIXED** |
| 8 | ContractPortalPage | `contracts/portal/ContractPortalPage.tsx` | `/contracts/portal/:contractVersionId` | ✅ **FIXED** |

### Legacy Pages (in catalog feature — likely dead code)

| Page | File | Route | Status |
|------|------|-------|--------|
| ContractDetailPage | `catalog/pages/ContractDetailPage.tsx` | None | ❌ Orphaned |
| ContractListPage | `catalog/pages/ContractListPage.tsx` | None | ❌ Orphaned |
| ContractsPage | `catalog/pages/ContractsPage.tsx` | None | ❌ Orphaned |
| ContractSourceOfTruthPage | `catalog/pages/ContractSourceOfTruthPage.tsx` | None | ❌ Orphaned |

---

## 2. Route Review

| Check | Status |
|-------|--------|
| All sidebar items have matching routes | ✅ (after P0 fix) |
| All routes have matching page components | ✅ |
| Route order prevents param catch-all conflicts | ✅ (specific routes before `/:contractVersionId`) |
| All routes wrapped in ProtectedRoute | ✅ |
| Redirect routes work correctly | ✅ (`/contracts/studio` → `/contracts`, `/contracts/legacy` → `/contracts`) |

---

## 3. Menu Review

**Sidebar items (AppSidebar.tsx, contracts section):**

| Label Key | Route | Permission | Has Matching Route |
|-----------|-------|-----------|-------------------|
| sidebar.contractCatalog | /contracts | contracts:read | ✅ |
| sidebar.createContract | /contracts/new | contracts:write | ✅ |
| sidebar.contractStudio | /contracts/studio | contracts:read | ✅ (redirects) |
| sidebar.contractGovernance | /contracts/governance | contracts:read | ✅ (FIXED) |
| sidebar.spectralRulesets | /contracts/spectral | contracts:write | ✅ (FIXED) |
| sidebar.canonicalEntities | /contracts/canonical | contracts:read | ✅ (FIXED) |

---

## 4. Component Assessment

### Shared Components (8)
- `ProtocolBadge.tsx` ✅
- `LifecycleBadge.tsx` ✅
- `ComplianceScoreCard.tsx` ✅
- `ContractHeader.tsx` ✅
- `ContractQuickActions.tsx` ✅
- `StateIndicators.tsx` ✅
- `ServiceTypeBadge.tsx` ✅
- `constants.ts` ✅

### Workspace Sections (15)
All properly rendering within ContractWorkspacePage:
- SummarySection, DefinitionSection, ContractSection, OperationsSection, SchemasSection, SecuritySection, ValidationSection, VersioningSection, ChangelogSection, ApprovalsSection, ComplianceSection, ConsumersSection, DependenciesSection, AiAgentsSection, StudioRail ✅

### Visual Builders (4)
- VisualRestBuilder, VisualSoapBuilder, VisualEventBuilder, VisualWorkserviceBuilder ✅

---

## 5. Loading/Error/Empty States

| State | ContractCatalogPage | CreateServicePage | DraftStudioPage | ContractWorkspacePage |
|-------|-------------------|-------------------|-----------------|----------------------|
| Loading | ✅ CatalogSkeleton | ✅ Spinner | ✅ Spinner | ✅ Spinner |
| Error | ✅ Error message | ✅ Error message | ✅ Error message | ✅ Error message |
| Empty | ✅ Empty state | N/A | N/A | N/A |

| State | GovernancePage | SpectralPage | CanonicalPage | PortalPage |
|-------|---------------|-------------|---------------|-----------|
| Loading | ⚠️ Needs verification | ⚠️ Needs verification | ⚠️ Needs verification | ⚠️ Needs verification |
| Error | ⚠️ Needs verification | ⚠️ Needs verification | ⚠️ Needs verification | ⚠️ Needs verification |
| Empty | ⚠️ Needs verification | ⚠️ Needs verification | ⚠️ Needs verification | ⚠️ Needs verification |

---

## 6. API Integration Review

| Hook | Real API | Status |
|------|----------|--------|
| useContractList | ✅ GET /api/v1/contracts/list | ✅ |
| useContractDetail | ✅ GET /api/v1/contracts/{id} | ✅ |
| useContractHistory | ✅ GET /api/v1/contracts/{id}/history | ✅ |
| useContractViolations | ✅ GET /api/v1/contracts/{id}/violations | ✅ |
| useContractTransition | ✅ POST /api/v1/contracts/lifecycle-transition | ✅ |
| useContractExport | ✅ POST /api/v1/contracts/export | ✅ |
| useContractDiff | ✅ POST /api/v1/contracts/diff | ✅ |
| useCreateDraft | ✅ POST /api/v1/contracts/drafts | ✅ |
| useSubmitForReview | ✅ POST /api/v1/contracts/drafts/{id}/submit-review | ✅ |
| usePublishDraft | ✅ POST /api/v1/contracts/drafts/{id}/publish | ✅ |
| useValidationSummary | ✅ POST /api/v1/contracts/validate | ✅ |
| useSpectralRulesets | ❌ **No backend endpoint** | ⚠️ Will fail |
| useCanonicalEntities | ❌ **No backend endpoint** | ⚠️ Will fail |

---

## 7. i18n Review

| Namespace | en | pt-PT | pt-BR | es | Notes |
|-----------|-----|-------|-------|-----|-------|
| contracts.catalog | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | |
| contracts.create | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | |
| contracts.workspace | ✅ | ✅ | ⚠️ Verify | ⚠️ Verify | |
| contracts.governance | ✅ | ⚠️ Verify | ⚠️ Verify | ⚠️ Verify | Newly routed page |
| contracts.spectral | ✅ | ⚠️ Verify | ⚠️ Verify | ⚠️ Verify | Newly routed page |
| contracts.canonical | ✅ | ⚠️ Verify | ⚠️ Verify | ⚠️ Verify | Newly routed page |
| contractGov | ✅ | ⚠️ Verify | ⚠️ Verify | ⚠️ Verify | |

---

## 8. Corrections Backlog

### HIGH Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| FE-01 | Verify loading/error/empty states on 4 newly routed pages | 4 page files | 1h |
| FE-02 | Verify SpectralRulesetManagerPage gracefully handles missing backend | SpectralRulesetManagerPage.tsx | 30min |
| FE-03 | Verify CanonicalEntityCatalogPage gracefully handles missing backend | CanonicalEntityCatalogPage.tsx | 30min |

### MEDIUM Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| FE-04 | Verify i18n completeness for pt-BR and es on newly routed pages | locales/*.json | 1h |
| FE-05 | Remove or mark as deprecated 4 legacy pages in catalog/pages/ | catalog/pages/*.tsx | 30min |
| FE-06 | Verify ContractPortalPage works standalone (no sidebar entry) | ContractPortalPage.tsx | 30min |

### LOW Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| FE-07 | Clarify `/contracts/studio` redirect behavior | App.tsx | 15min |
| FE-08 | Add breadcrumb consistency across all 8 pages | Page components | 1h |
| FE-09 | Remove duplicate API files in catalog/api/ (contracts.ts, contractStudio.ts) | catalog/api/ | 30min |
