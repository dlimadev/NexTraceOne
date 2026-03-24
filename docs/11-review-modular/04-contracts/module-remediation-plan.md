# Contracts Module — Remediation Plan

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## A. Quick Wins

Small, high-value, low-effort items.

| # | Item | File(s) | Effort | Impact |
|---|------|---------|--------|--------|
| QW-01 | ✅ **DONE** — Fix 3 broken frontend routes (governance, spectral, canonical) | `App.tsx` | 30min | 🔴 P0 eliminated |
| QW-02 | ✅ **DONE** — Add ContractPortalPage route | `App.tsx` | 15min | Portal accessible |
| QW-03 | Change import endpoint rate limit to `data-intensive` | `ContractsEndpointModule.cs` | 15min | Better security |
| QW-04 | Update module-review.md to reflect P0 fix | `module-review.md` | 1h | Documentation current |
| QW-05 | Create minimal frontend README | `features/contracts/README.md` | 2h | Onboarding possible |
| QW-06 | Verify loading/error/empty states on 4 newly routed pages | 4 page files | 1h | UX completeness |

**Quick Wins total: ~5 hours (2 already done)**

---

## B. Functional Corrections (Mandatory)

Items the module needs to be considered functionally complete.

| # | Item | File(s) | Effort | Priority |
|---|------|---------|--------|----------|
| FC-01 | **Create SpectralRuleset backend CRUD** (Create, List, Get, Update, Delete, Toggle) | New CQRS handlers + endpoint module | 6h | HIGH |
| FC-02 | **Create CanonicalEntity backend CRUD** (Create, List, Get, Update, Promote) | New CQRS handlers + endpoint module | 6h | HIGH |
| FC-03 | **Add 5 missing DbSets** (SpectralRuleset, CanonicalEntity, ContractLock, ContractScorecard, ContractEvidencePack) | `ContractsDbContext.cs` | 1h | HIGH |
| FC-04 | **Create 5 missing EF Core configurations** | New files in `Persistence/Configurations/` | 4h | HIGH |
| FC-05 | Add `UseXminAsConcurrencyToken()` to ContractVersion, ContractDraft, SpectralRuleset | 3 EF config files | 30min | HIGH |
| FC-06 | Handle `DbUpdateConcurrencyException` in all write handlers | All write handlers | 2h | HIGH |
| FC-07 | Create Contract Portal read endpoint | New endpoint | 2h | MEDIUM |
| FC-08 | Verify i18n for pt-BR and es on newly routed pages | locales/*.json | 1h | MEDIUM |
| FC-09 | Verify SpectralRulesetManagerPage graceful error handling | SpectralRulesetManagerPage.tsx | 30min | MEDIUM |
| FC-10 | Verify CanonicalEntityCatalogPage graceful error handling | CanonicalEntityCatalogPage.tsx | 30min | MEDIUM |

**Functional Corrections total: ~24 hours**

---

## C. Structural Adjustments

Items related to the new boundary and persistence patterns.

| # | Item | File(s) | Effort | Priority |
|---|------|---------|--------|----------|
| SA-01 | **Plan table prefix change** from `ct_` to `ctr_` (applies in future baseline migration) | EF configurations | 2h | HIGH |
| SA-02 | Move lifecycle transition validation into `ContractVersion` entity | `ContractVersion.cs` | 2h | MEDIUM |
| SA-03 | Move draft status transition validation into `ContractDraft` entity | `ContractDraft.cs` | 2h | MEDIUM |
| SA-04 | Add domain events for lifecycle transitions | `ContractVersion.cs` | 1h | MEDIUM |
| SA-05 | Add integration event publishing for key actions | Write handlers | 2h | MEDIUM |
| SA-06 | Add check constraints for all enums (ContractProtocol, LifecycleState, DraftStatus, etc.) | EF configurations | 2h | MEDIUM |
| SA-07 | Add filtered indexes `WHERE is_deleted = false` on key tables | EF configurations | 1h | LOW |
| SA-08 | Remove legacy contract pages from catalog/pages/ | 4 files in catalog/pages/ | 30min | LOW |
| SA-09 | Remove duplicate API files from catalog/api/ | 2 files in catalog/api/ | 30min | LOW |
| SA-10 | Verify IContractsModule methods are all implemented | Infrastructure service | 1h | MEDIUM |

**Structural Adjustments total: ~14 hours**

---

## D. Pre-conditions for Recreating Migrations

Items that must be completed before the baseline migration can be generated.

| # | Pre-condition | Dependencies | Status |
|---|-------------|-------------|--------|
| D-01 | Domain model finalized | Part 4 document | ✅ Done |
| D-02 | Persistence model finalized | Part 5 document | ✅ Done |
| D-03 | Table prefix changed to `ctr_` in EF configurations | SA-01 | ⬜ Pending |
| D-04 | All 12 entities have EF configurations | FC-03, FC-04 | ⬜ Pending |
| D-05 | `UseXminAsConcurrencyToken()` on Version, Draft, Ruleset | FC-05 | ⬜ Pending |
| D-06 | Check constraints for all enums | SA-06 | ⬜ Pending |
| D-07 | Filtered indexes added | SA-07 | ⬜ Pending |
| D-08 | Outbox table prefix changed to `ctr_outbox_messages` | SA-01 | ⬜ Pending |
| D-09 | All FK constraints properly defined | FC-04 | ⬜ Pending |
| D-10 | Existing 3 migrations preserved (not deleted in this phase) | Rule | ✅ Maintained |

**Once all D items are complete, a single baseline migration can replace the existing 3.**

---

## E. Module Closure Criteria

| # | Criterion | Status | Dependency |
|---|----------|--------|------------|
| E-01 | Catalog vs Contracts boundary clearly documented | ✅ | Part 1 |
| E-02 | P0 frontend blocker eliminated | ✅ | Part 2 (DONE) |
| E-03 | Module scope finalized | ✅ | Part 3 |
| E-04 | Domain model finalized | ✅ | Part 4 |
| E-05 | Persistence model finalized | ✅ | Part 5 |
| E-06 | Backend corrections identified and documented | ✅ | Part 6 |
| E-07 | Frontend corrections identified and documented | ✅ | Part 7 |
| E-08 | Security and permissions mapped | ✅ | Part 8 |
| E-09 | Documentation plan defined | ✅ | Part 9 |
| E-10 | All 8 pages routed and accessible | ✅ | QW-01, QW-02 |
| E-11 | SpectralRuleset backend CRUD created | ⬜ | FC-01 |
| E-12 | CanonicalEntity backend CRUD created | ⬜ | FC-02 |
| E-13 | All 12 entities mapped in ContractsDbContext | ⬜ | FC-03, FC-04 |
| E-14 | Concurrency tokens (xmin) added | ⬜ | FC-05 |
| E-15 | Table prefix corrected to `ctr_` | ⬜ | SA-01 |
| E-16 | Module documentation minimum created | ⬜ | QW-05 |
| E-17 | Baseline migration ready (not yet generated) | ⬜ | All D items |
| E-18 | Module maturity ≥85% | ⬜ | All above |

---

## Execution Priority

### Phase 1 — Quick Wins (Day 1)

Execute QW-01 through QW-06. **QW-01 and QW-02 are already done.**

### Phase 2 — Backend Gap Closure (Days 2-4)

Execute FC-01 through FC-06. These fill the most critical gaps (unmapped entities, missing CRUD, concurrency).

### Phase 3 — Structural Adjustments (Days 5-6)

Execute SA-01 through SA-10. Prepare the module for extraction and baseline migration.

### Phase 4 — Polish & Documentation (Days 7-8)

Execute FC-07 through FC-10, remaining quick wins, and documentation tasks.

### Total Estimated Effort

| Phase | Effort | Items |
|-------|--------|-------|
| Quick Wins | 5h (2 done) | 6 items |
| Functional Corrections | 24h | 10 items |
| Structural Adjustments | 14h | 10 items |
| Documentation | 8h | 5 items |
| **Total** | **~51h** (~7 days) | **31 items** |

---

## Reference Documents

| Document | Path |
|----------|------|
| Boundary Deep Dive | `docs/11-review-modular/04-contracts/catalog-vs-contracts-boundary-deep-dive.md` |
| P0 Correction Report | `docs/11-review-modular/04-contracts/frontend-p0-correction-report.md` |
| Scope Finalization | `docs/11-review-modular/04-contracts/module-scope-finalization.md` |
| Domain Model | `docs/11-review-modular/04-contracts/domain-model-finalization.md` |
| Persistence Model | `docs/11-review-modular/04-contracts/persistence-model-finalization.md` |
| Backend Corrections | `docs/11-review-modular/04-contracts/backend-functional-corrections.md` |
| Frontend Corrections | `docs/11-review-modular/04-contracts/frontend-functional-corrections.md` |
| Security Review | `docs/11-review-modular/04-contracts/security-and-permissions-review.md` |
| Documentation Plan | `docs/11-review-modular/04-contracts/documentation-and-onboarding-upgrade.md` |
| Architecture Decisions | `docs/architecture/architecture-decisions-final.md` |
| Module Boundary Matrix | `docs/architecture/module-boundary-matrix.md` |
| Frontier Decisions | `docs/architecture/module-frontier-decisions.md` |
| Table Prefixes | `docs/architecture/database-table-prefixes.md` |
| Phase A Open Items | `docs/architecture/phase-a-open-items.md` |
