# Change Governance — Module Remediation Plan

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Maturity:** 81% → target ≥ 90%

---

## A. Quick Wins (< 2h each, no structural changes)

| ID | Item | Area | Priority | Effort | Files |
|----|------|------|----------|--------|-------|
| QW-01 | Create module README.md | Docs | P0 | 2h | `src/modules/changegovernance/README.md` |
| QW-02 | Fix workflow templates GET permission bug (`:write` → `:read`) | Security | P0 | 1h | `Infrastructure/Workflow/Endpoints/TemplateEndpoints.cs` |
| QW-03 | Add FreezeWindow `StartTime < EndTime` validation | Validation | P1 | 1h | `Application/ChangeIntelligence/Features/CreateFreezeWindow/` |
| QW-04 | Add `SourceEnv != TargetEnv` validation in promotion | Validation | P1 | 1h | `Application/Promotion/Features/CreatePromotionRequest/` |
| QW-05 | Add minimum justification length on gate override | Security | P1 | 1h | `Application/Promotion/Features/OverrideGateWithJustification/` |
| QW-06 | Add domain-level score range validation (0.0–1.0) | Domain | P1 | 1h | `Domain/ChangeIntelligence/Entities/ChangeIntelligenceScore.cs` |

**Quick wins total:** 6 items, ~7 hours

---

## B. Functional Corrections (Mandatory before production)

| ID | Item | Area | Priority | Effort | Dependencies |
|----|------|------|----------|--------|-------------|
| FC-01 | Add ApiAssetId validation against Catalog on release creation | Backend | P1 | 4h | Service Catalog API |
| FC-02 | Add ReleaseId existence check in InitiateWorkflow | Backend | P1 | 2h | Cross-DbContext query |
| FC-03 | Add 404 handling for missing entities across all endpoints | Backend | P1 | 4h | All endpoint modules |
| FC-04 | Add explicit domain events for outbox integration (ReleaseCreated, WorkflowApproved, etc.) | Domain | P1 | 8h | All 4 subdomains |
| FC-05 | Wire score into promotion gate evaluation (score gate type) | Backend | P1 | 8h | Promotion + ChangeIntelligence |
| FC-06 | Add explicit audit event emission for sensitive actions | Backend | P1 | 8h | Audit & Compliance integration |
| FC-07 | Standardise error response format across all endpoints | Backend | P2 | 4h | All endpoint modules |
| FC-08 | Add content validation for ruleset uploads (JSON/YAML schema) | Backend | P1 | 4h | RulesetGovernance |
| FC-09 | Add incident correlation panel to ChangeDetailPage | Frontend | P2 | 8h | Operational Intelligence API |
| FC-10 | Add gate override audit trail to PromotionPage | Frontend | P2 | 4h | Promotion read queries |
| FC-11 | Validate i18n keys across all 4 locales | Frontend | P2 | 4h | i18n files |

**Functional corrections total:** 11 items, ~58 hours

---

## C. Structural Adjustments (Architecture alignment)

| ID | Item | Area | Priority | Effort | Dependencies |
|----|------|------|----------|--------|-------------|
| SA-01 | Add `RowVersion` / `ConcurrencyToken` (xmin) to all mutable aggregates | Persistence | P1 | 8h | All 4 DbContexts |
| SA-02 | Add FK constraints within each DbContext | Persistence | P1 | 4h | All entity configurations |
| SA-03 | Add CHECK constraints (score ranges, date validations) | Persistence | P2 | 4h | All entity configurations |
| SA-04 | Add missing `TenantId` indexes on all tables | Persistence | P2 | 2h | All entity configurations |
| SA-05 | Decide table prefix strategy (unified `chg_` vs. subdomain prefixes) | Architecture | P1 | 2h | Architecture decision |
| SA-06 | Resolve `DeploymentEnvironment` duplication with Environment Management | Domain | P2 | 8h | Env Mgmt module |
| SA-07 | Improve Catalog Graph integration for transitive blast radius | Backend | P1 | 2 weeks | Service Catalog module |
| SA-08 | Add unique constraint on `(ApiAssetId, Version, EnvironmentId)` for releases | Persistence | P2 | 1h | ChangeIntelligence DbContext |

**Structural adjustments total:** 8 items, ~41 hours (excluding SA-07 which is 2 weeks)

---

## D. Pre-conditions for Recreating Migrations

Before dropping and recreating migrations for Change Governance, the following must be completed:

| # | Pre-condition | Depends On | Status |
|---|--------------|------------|--------|
| 1 | Domain model finalized (all entities, enums, VOs confirmed) | domain-model-finalization.md | ✅ Done |
| 2 | Table prefix strategy decided (`chg_` unified or subdomain) | SA-05 | ⏳ Pending |
| 3 | RowVersion added to all mutable aggregates | SA-01 | ⏳ Pending |
| 4 | FK constraints defined within each DbContext | SA-02 | ⏳ Pending |
| 5 | CHECK constraints defined | SA-03 | ⏳ Pending |
| 6 | Missing indexes added (TenantId, unique constraints) | SA-04, SA-08 | ⏳ Pending |
| 7 | `DeploymentEnvironment` duplication resolved | SA-06 | ⏳ Pending |
| 8 | DbContext consolidation decision (1 vs. 4 DbContexts) made | Architecture decision | ⏳ Pending |

**Migration recreation is blocked until items 1–8 are resolved.**

---

## E. Criteria de Aceite do Módulo

### Mandatory (must be true for module to be considered production-ready)

| # | Criterion | Current Status |
|---|----------|---------------|
| 1 | All 46+ endpoints functional with correct permissions | ⚠️ 1 permission bug (QW-02) |
| 2 | Change score computed from real data with valid range | ⚠️ No domain validation (QW-06) |
| 3 | Direct blast radius functional | ✅ Done |
| 4 | Workflow approval lifecycle complete | ✅ Done |
| 5 | Promotion gates functional with override audit trail | ✅ Done |
| 6 | Evidence pack generation functional | ✅ Done |
| 7 | Freeze window conflict detection functional | ✅ Done |
| 8 | Tenant isolation via RLS active on all DbContexts | ✅ Done |
| 9 | Module README.md exists | ❌ Missing (QW-01) |
| 10 | No permission bugs | ⚠️ 1 bug (QW-02) |

### Complementary (should be true before GA)

| # | Criterion | Current Status |
|---|----------|---------------|
| 11 | Transitive blast radius via Catalog Graph | ⚠️ Partial |
| 12 | Score feeds into promotion gates | ❌ Not wired (FC-05) |
| 13 | RowVersion on all mutable aggregates | ❌ Missing (SA-01) |
| 14 | FK constraints within DbContexts | ❌ Missing (SA-02) |
| 15 | API documentation with examples | ❌ Missing (D-04) |
| 16 | End-to-end flow diagrams | ❌ Missing (D-05) |
| 17 | Incident-change correlation in post-release review | ⚠️ Structural only |
| 18 | Notifications integration for workflow events | ❌ Not wired |
| 19 | AI agent capabilities defined for AssistantPanel | ⚠️ Structural only |
| 20 | i18n validated across all 4 locales | ⚠️ Needs validation |

---

## F. Execution Waves

### Wave 1 — Quick Wins (1 week)
Items: QW-01 through QW-06  
**Goal:** Fix all permission bugs, add critical validations, create README

### Wave 2 — Functional Corrections (2–3 weeks)
Items: FC-01 through FC-11  
**Goal:** All endpoints correctly validated, domain events defined, error handling standardised

### Wave 3 — Structural Alignment (2–3 weeks)
Items: SA-01 through SA-08  
**Goal:** Persistence model production-ready, architecture alignment confirmed

### Wave 4 — Documentation & Polish (1–2 weeks)
Items: D-01 through D-10  
**Goal:** Complete documentation, onboarding guide, API docs

### Wave 5 — Migration Recreation
**Pre-condition:** Waves 1–3 complete  
**Action:** Drop and recreate all 4 migrations with final schema

---

## G. Summary

| Category | Items | Effort |
|----------|-------|--------|
| Quick Wins | 6 | ~7h |
| Functional Corrections | 11 | ~58h |
| Structural Adjustments | 8 | ~41h + 2 weeks |
| Documentation | 10 | ~62h |
| **Total** | **35 items** | **~168h + 2 weeks** |

The module is at **81% maturity** with a clear path to **90%+** through the remediation items above. The core flows are functional; the gaps are in validation, persistence hardening, documentation, and cross-module integration wiring.
