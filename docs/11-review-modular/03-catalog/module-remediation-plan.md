# Catalog Module — Remediation Plan

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## A. Quick Wins

Small, high-value, low-effort items.

| # | Item | File(s) | Effort | Impact |
|---|------|---------|--------|--------|
| QW-01 | Add XML doc comments to `ServiceAsset` and `ApiAsset` aggregate roots | `Domain/Graph/Entities/ServiceAsset.cs`, `ApiAsset.cs` | 1h | Documentation baseline |
| QW-02 | Add XML doc comments to `ICatalogGraphModule` and `IDeveloperPortalModule` | `Contracts/Graph/ServiceInterfaces/ICatalogGraphModule.cs`, `Portal/ServiceInterfaces/IDeveloperPortalModule.cs` | 1h | Cross-module clarity |
| QW-03 | Remove or deprecate 3 orphaned pages (ContractDetailPage, ContractListPage, ContractsPage) | `catalog/pages/ContractDetailPage.tsx`, `ContractListPage.tsx`, `ContractsPage.tsx` | 30min | Dead code removed |
| QW-04 | Remove duplicate contract API files from catalog/api/ if they duplicate contracts/api/ | `catalog/api/contracts.ts`, `catalog/api/contractStudio.ts` | 30min | Reduce confusion |
| QW-05 | Create minimal backend README | `src/modules/catalog/README.md` | 2h | Onboarding possible |
| QW-06 | Create minimal frontend README | `src/frontend/src/features/catalog/README.md` | 2h | Onboarding possible |

**Quick Wins total: ~7 hours**

---

## B. Functional Corrections (Mandatory)

Items the module needs to be considered functionally complete.

| # | Item | File(s) | Effort | Priority |
|---|------|---------|--------|----------|
| FC-01 | **Add `UseXminAsConcurrencyToken()` to ServiceAsset and ApiAsset** | `ServiceAssetConfiguration.cs`, `ApiAssetConfiguration.cs` | 30min | HIGH |
| FC-02 | **Handle `DbUpdateConcurrencyException` in write handlers** | All Graph write handlers | 1h | HIGH |
| FC-03 | **Create `TransitionAssetLifecycle` CQRS handler** (service + API lifecycle state machine) | New files in `Application/Graph/Features/` + endpoint entry | 4h | HIGH |
| FC-04 | **Create `UpdateServiceAsset` CQRS handler** (missing — only UpdateApiAsset exists) | New file in `Application/Graph/Features/` + endpoint entry | 2h | MEDIUM |
| FC-05 | **Verify CatalogContractsConfigurationPage loading/error/empty states** | `CatalogContractsConfigurationPage.tsx` | 30min | MEDIUM |
| FC-06 | **Verify i18n completeness for pt-BR and es** on all 9 catalog pages | `locales/*.json` | 2h | MEDIUM |
| FC-07 | **Add check constraints for enums** (ServiceType, ExposureType, LifecycleStatus, Criticality, HealthStatus, NodeType, EdgeType, RelationshipSemantic) | EF configurations | 2h | MEDIUM |
| FC-08 | **Add filtered indexes** `WHERE is_deleted = false` on cat_service_assets, cat_api_assets | EF configurations | 1h | MEDIUM |
| FC-09 | **Create `DecommissionAsset` handler** (explicit retirement flow) | New file in `Application/Graph/Features/` + endpoint entry | 2h | MEDIUM |
| FC-10 | **Create bulk import endpoint** (`BulkImportAssets`) | New file in `Application/Graph/Features/` + endpoint entry | 4h | MEDIUM |

**Functional Corrections total: ~19 hours**

---

## C. Structural Adjustments

Items related to the new boundary and persistence patterns.

| # | Item | File(s) | Effort | Priority |
|---|------|---------|--------|----------|
| SA-01 | **Plan table prefix change** from `eg_`/`dp_` to `cat_` (applies in future baseline migration) | All EF configurations | 3h | HIGH |
| SA-02 | **Move lifecycle transition validation into `ServiceAsset`** entity (add `TransitionTo()` method) | `ServiceAsset.cs` | 2h | MEDIUM |
| SA-03 | **Move lifecycle transition validation into `ApiAsset`** entity (add `TransitionTo()` method) | `ApiAsset.cs` | 2h | MEDIUM |
| SA-04 | **Add domain events for asset lifecycle transitions** (ServiceLifecycleChanged, ApiLifecycleChanged) | `ServiceAsset.cs`, `ApiAsset.cs` | 1h | MEDIUM |
| SA-05 | **Add integration event publishing** for asset registration, update, and lifecycle changes | Write handlers | 2h | MEDIUM |
| SA-06 | **Evaluate consolidating CatalogGraphDbContext and DeveloperPortalDbContext** into single CatalogDbContext | DbContext files, EF configs | 4h | LOW |
| SA-07 | **Remove orphaned `ContractVersionDetailPanel`** if confirmed dead code | `catalog/components/ContractVersionDetailPanel.tsx` | 15min | LOW |
| SA-08 | **Add breadcrumb consistency** across all 9 catalog pages | Page components | 1h | LOW |
| SA-09 | **Verify `ICatalogGraphModule` and `IDeveloperPortalModule` coverage** — all methods implemented | Infrastructure service files | 1h | MEDIUM |
| SA-10 | **Document integration event schemas** for downstream module consumers | New documentation | 2h | MEDIUM |

**Structural Adjustments total: ~18 hours**

---

## D. Pre-conditions for Recreating Migrations

Items that must be completed before the baseline migration can be generated.

| # | Pre-condition | Dependencies | Status |
|---|-------------|-------------|--------|
| D-01 | Domain model finalized | `domain-model-finalization.md` | ✅ Done |
| D-02 | Persistence model finalized | `persistence-model-finalization.md` | ✅ Done |
| D-03 | Table prefix changed to `cat_` in EF configurations | SA-01 | ⬜ Pending |
| D-04 | All 14 entities have EF configurations with `cat_` prefix | SA-01 | ⬜ Pending |
| D-05 | `UseXminAsConcurrencyToken()` on ServiceAsset, ApiAsset | FC-01 | ⬜ Pending |
| D-06 | Check constraints for all 8 enums | FC-07 | ⬜ Pending |
| D-07 | Filtered indexes added | FC-08 | ⬜ Pending |
| D-08 | Outbox table prefix changed from `eg_outbox_messages`/`dp_outbox_messages` to `cat_outbox_messages` | SA-01 | ⬜ Pending |
| D-09 | All FK constraints properly defined | D-04 | ⬜ Pending |
| D-10 | Existing migrations preserved (not deleted in this phase) | Rule | ✅ Maintained |
| D-11 | DbContext consolidation decision made (keep 2 or merge to 1) | SA-06 | ⬜ Pending |

**Once all D items are complete, a single baseline migration per DbContext can replace existing migrations.**

---

## E. Module Closure Criteria

| # | Criterion | Status | Dependency |
|---|----------|--------|------------|
| E-01 | Catalog vs Contracts boundary documented | ✅ | `catalog-vs-contracts-boundary-deep-dive.md` |
| E-02 | Module scope finalized | ✅ | `module-scope-finalization.md` |
| E-03 | Domain model finalized | ✅ | `domain-model-finalization.md` |
| E-04 | Persistence model finalized | ✅ | `persistence-model-finalization.md` |
| E-05 | Backend corrections identified and documented | ✅ | `backend-functional-corrections.md` |
| E-06 | Frontend corrections identified and documented | ✅ | `frontend-functional-corrections.md` |
| E-07 | Security and permissions mapped | ✅ | `security-and-permissions-review.md` |
| E-08 | Module dependency map documented | ✅ | `module-dependency-map.md` |
| E-09 | Documentation plan defined | ✅ | `documentation-and-onboarding-upgrade.md` |
| E-10 | All 9 catalog pages routed and accessible | ✅ | Already done |
| E-11 | Orphaned pages removed | ⬜ | QW-03 |
| E-12 | Concurrency tokens (xmin) added | ⬜ | FC-01 |
| E-13 | Lifecycle transition handler created | ⬜ | FC-03 |
| E-14 | UpdateServiceAsset handler created | ⬜ | FC-04 |
| E-15 | Table prefix corrected to `cat_` | ⬜ | SA-01 |
| E-16 | Module documentation minimum created | ⬜ | QW-05, QW-06 |
| E-17 | Baseline migration ready (not yet generated) | ⬜ | All D items |
| E-18 | Module maturity ≥85% | ⬜ | All above |

---

## Execution Priority

### Phase 1 — Quick Wins (Day 1)

Execute QW-01 through QW-06.

### Phase 2 — Core Gap Closure (Days 2-4)

Execute FC-01 through FC-04. These fill the most critical gaps (concurrency, lifecycle transitions, service updates).

### Phase 3 — Structural Adjustments (Days 5-7)

Execute SA-01 through SA-05. Prepare the module for baseline migration and extraction.

### Phase 4 — Polish & Documentation (Days 8-9)

Execute FC-05 through FC-10, remaining structural items, and documentation tasks.

### Total Estimated Effort

| Phase | Effort | Items |
|-------|--------|-------|
| Quick Wins | 7h | 6 items |
| Functional Corrections | 19h | 10 items |
| Structural Adjustments | 18h | 10 items |
| Documentation | 8h | 7 items |
| **Total** | **~52h** (~7 days) | **33 items** |

---

## Reference Documents

| Document | Path |
|----------|------|
| Boundary Deep Dive | `docs/11-review-modular/03-catalog/catalog-vs-contracts-boundary-deep-dive.md` |
| Scope Finalization | `docs/11-review-modular/03-catalog/module-scope-finalization.md` |
| Domain Model | `docs/11-review-modular/03-catalog/domain-model-finalization.md` |
| Persistence Model | `docs/11-review-modular/03-catalog/persistence-model-finalization.md` |
| Backend Corrections | `docs/11-review-modular/03-catalog/backend-functional-corrections.md` |
| Frontend Corrections | `docs/11-review-modular/03-catalog/frontend-functional-corrections.md` |
| Security Review | `docs/11-review-modular/03-catalog/security-and-permissions-review.md` |
| Dependency Map | `docs/11-review-modular/03-catalog/module-dependency-map.md` |
| Documentation Plan | `docs/11-review-modular/03-catalog/documentation-and-onboarding-upgrade.md` |
| Module Role | `docs/11-review-modular/03-catalog/module-role-finalization.md` |
| Architecture Decisions | `docs/architecture/architecture-decisions-final.md` |
| Module Boundary Matrix | `docs/architecture/module-boundary-matrix.md` |
| Table Prefixes | `docs/architecture/database-table-prefixes.md` |
| Phase A Open Items | `docs/architecture/phase-a-open-items.md` |
