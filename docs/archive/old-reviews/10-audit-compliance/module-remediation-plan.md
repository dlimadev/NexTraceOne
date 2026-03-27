# Audit & Compliance — Module Remediation Plan

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Maturity:** 53% → target ≥ 75%

---

## A. Quick Wins (< 2h each, no structural changes)

| ID | Item | Area | Priority | Effort | Files |
|----|------|------|----------|--------|-------|
| QW-01 | Create module README.md | Docs | P0 | 2h | `src/modules/auditcompliance/README.md` |
| QW-02 | Document hash chain specification | Docs | P0 | 2h | `docs/architecture/` or module README |
| QW-03 | Add date range filters to AuditPage | Frontend | P1 | 2h | `features/audit-compliance/pages/AuditPage.tsx` |
| QW-04 | Add source module filter to AuditPage | Frontend | P1 | 1h | `features/audit-compliance/pages/AuditPage.tsx` |
| QW-05 | Add export report button to AuditPage | Frontend | P2 | 1h | `features/audit-compliance/pages/AuditPage.tsx` |
| QW-06 | Add chain hash display per event row | Frontend | P2 | 1h | `features/audit-compliance/pages/AuditPage.tsx` |

**Quick wins total:** 6 items, ~9 hours

---

## B. Functional Corrections (Mandatory before production)

| ID | Item | Area | Priority | Effort | Dependencies |
|----|------|------|----------|--------|-------------|
| FC-01 | Fix ConfigureRetention handler to actually persist | Backend | P0 | 4h | RetentionPolicy entity |
| FC-02 | Add campaign lifecycle endpoints (Start, Complete, Cancel) | Backend | P1 | 4h | AuditCampaign domain methods |
| FC-03 | Add policy activate/deactivate endpoints | Backend | P1 | 2h | CompliancePolicy domain methods |
| FC-04 | Add policy update endpoint | Backend | P2 | 2h | CompliancePolicy domain methods |
| FC-05 | Add retention list endpoint | Backend | P1 | 2h | RetentionPolicy repository |
| FC-06 | Add `EnvironmentId` to RecordAuditEvent command and domain | Backend | P1 | 4h | AuditEvent entity |
| FC-07 | Wire Change Governance → IAuditModule for sensitive actions | Integration | P0 | 8h | Change Governance module |
| FC-08 | Wire Operational Intelligence → IAuditModule for incident events | Integration | P1 | 4h | OI module |
| FC-09 | Wire remaining modules → IAuditModule | Integration | P2 | 8h | All modules |
| FC-10 | Implement self-auditing for compliance policy changes | Security | P0 | 4h | RecordAuditEvent |
| FC-11 | Implement self-auditing for retention configuration changes | Security | P0 | 2h | RecordAuditEvent |
| FC-12 | Create CompliancePoliciesPage | Frontend | P1 | 16h | API client methods |
| FC-13 | Create ComplianceResultsPage | Frontend | P1 | 12h | API client methods |
| FC-14 | Create ComplianceReportPage | Frontend | P1 | 8h | API client methods |
| FC-15 | Create AuditCampaignsPage | Frontend | P2 | 12h | API client methods |
| FC-16 | Add API client methods for all 15+ backend endpoints | Frontend | P1 | 4h | Backend endpoints |
| FC-17 | Add sidebar items, routes, lazy imports for new pages | Frontend | P1 | 4h | App.tsx, AppSidebar.tsx |
| FC-18 | Add i18n keys for all new pages across 4 locales | Frontend | P2 | 8h | Locale files |

**Functional corrections total:** 18 items, ~108 hours

---

## C. Structural Adjustments (Architecture alignment)

| ID | Item | Area | Priority | Effort | Dependencies |
|----|------|------|----------|--------|-------------|
| SA-01 | Add `RowVersion` / `ConcurrencyToken` (xmin) to CompliancePolicy, AuditCampaign, RetentionPolicy | Persistence | P1 | 4h | Entity configurations |
| SA-02 | Add `TenantId` to RetentionPolicy | Domain | P1 | 2h | Entity + configuration |
| SA-03 | Add composite indexes (TenantId+OccurredAt, TenantId+SourceModule) | Persistence | P2 | 2h | Entity configurations |
| SA-04 | Implement retention purge background service | Backend | P1 | 8h | .NET BackgroundService |
| SA-05 | Add automated periodic chain verification job | Backend | P1 | 4h | .NET BackgroundService |
| SA-06 | Optimise VerifyChainIntegrity for large chains (streaming/batched) | Backend | P2 | 8h | Handler refactor |
| SA-07 | Define standard event taxonomy (SourceModule × ActionType) | Domain | P1 | 4h | Documentation + validation |
| SA-08 | Define standard Payload JSON schema | Domain | P1 | 2h | Documentation + validation |
| SA-09 | Consider DB-level immutability triggers for audit tables | Security | P3 | 4h | PostgreSQL triggers |

**Structural adjustments total:** 9 items, ~38 hours

---

## D. Pre-conditions for Recreating Migrations

Before dropping and recreating migrations for Audit & Compliance:

| # | Pre-condition | Depends On | Status |
|---|--------------|------------|--------|
| 1 | Domain model finalised (EnvironmentId on AuditEvent, TenantId on RetentionPolicy) | FC-06, SA-02 | ⏳ Pending |
| 2 | RowVersion added to mutable entities | SA-01 | ⏳ Pending |
| 3 | Composite indexes defined | SA-03 | ⏳ Pending |
| 4 | Retention enforcement design decided | SA-04 | ⏳ Pending |
| 5 | Evidence storage decision (Payload field vs. dedicated table) | Architecture | ⏳ Pending |
| 6 | `aud_` prefix confirmed (already in use — no change needed) | ✅ Done | ✅ Done |

---

## E. Criteria de Aceite do Módulo

### Mandatory (must be true for module to be considered production-ready)

| # | Criterion | Current Status |
|---|----------|---------------|
| 1 | All audit events recorded with hash chain | ✅ Done |
| 2 | Hash chain integrity verification functional | ✅ Done |
| 3 | Audit trail queryable by resource and filters | ✅ Done |
| 4 | At least Identity + Change Governance publishing events | ⚠️ Only Identity confirmed |
| 5 | Compliance policy CRUD functional (backend + frontend) | ⚠️ Backend only |
| 6 | Retention actually persisting and enforceable | ❌ Handler is placeholder |
| 7 | Module self-audits sensitive actions | ❌ Not implemented |
| 8 | Module README.md exists | ❌ Missing |
| 9 | EnvironmentId on audit events | ❌ Missing |
| 10 | No placeholder handlers | ⚠️ ConfigureRetention is placeholder |

### Complementary (should be true before GA)

| # | Criterion | Current Status |
|---|----------|---------------|
| 11 | All modules publishing audit events | ❌ Only Identity |
| 12 | Frontend for compliance policies, results, campaigns | ❌ Missing (5–6 pages) |
| 13 | Retention purge background service | ❌ Missing |
| 14 | Automated periodic chain verification | ❌ Missing |
| 15 | RowVersion on mutable entities | ❌ Missing |
| 16 | Hash chain documentation published | ❌ Missing |
| 17 | API documentation with examples | ❌ Missing |
| 18 | Integration guide for module producers | ❌ Missing |
| 19 | Standard event taxonomy defined | ❌ Missing |
| 20 | i18n validated across all 4 locales | ❌ Missing for new pages |

---

## F. Execution Waves

### Wave 1 — Quick Wins + Critical Fixes (1 week)
Items: QW-01 through QW-06, FC-01, FC-10, FC-11  
**Goal:** Fix placeholder handler, implement self-auditing, create README, enhance AuditPage

### Wave 2 — Backend Completeness + Integration (2 weeks)
Items: FC-02 through FC-09  
**Goal:** All endpoints functional, Change Governance and OI wired to Audit, EnvironmentId added

### Wave 3 — Frontend Build (2–3 weeks)
Items: FC-12 through FC-18  
**Goal:** All compliance, campaign, and retention pages built; full frontend parity

### Wave 4 — Structural Alignment (1–2 weeks)
Items: SA-01 through SA-09  
**Goal:** Persistence hardened, background services, event taxonomy standardised

### Wave 5 — Migration Recreation
**Pre-condition:** Waves 1–4 complete  
**Action:** Drop and recreate both migrations with final schema

---

## G. Summary

| Category | Items | Effort |
|----------|-------|--------|
| Quick Wins | 6 | ~9h |
| Functional Corrections | 18 | ~108h |
| Structural Adjustments | 9 | ~38h |
| Documentation | 10 | ~42h |
| **Total** | **43 items** | **~197h** |

The module is at **53% maturity** with a clear path to **75%+** through the remediation items above. The backend core (audit trail + hash chain) is solid and real. The two largest gaps are: (1) only Identity publishes events — all other modules need wiring, and (2) the frontend has only 1 page out of 6+ needed.
