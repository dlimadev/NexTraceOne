# Operational Intelligence — Module Remediation Plan

> **Module:** Operational Intelligence  
> **Prefix:** `ops_`  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Maturity:** 74 % (target ≥ 80 %)

---

## A. Quick Wins

| ID | Item | Area | Priority | Effort | Files |
|----|------|------|----------|--------|-------|
| QW-01 | Create module README.md | Docs | P1 | 2 h | `src/modules/operationalintelligence/README.md` |
| QW-02 | Document reliability scoring formula (weights 50/30/20) | Docs | P1 | 2 h | `ReliabilitySnapshot.cs` |
| QW-03 | Document health-classification thresholds (ErrorRate 5 %/10 %, P99 1 s/3 s) | Docs | P1 | 1 h | `RuntimeSnapshot.cs` |
| QW-04 | Document automation workflow state machine diagram | Docs | P1 | 2 h | `AutomationWorkflowRecord.cs` |
| QW-05 | Add XML doc comments to all public Application contracts | Docs | P2 | 3 h | `Application/Features/**/*.cs` |
| QW-06 | Fix embryonic `operational-intelligence/` frontend folder — decide merge or delete | Frontend | P2 | 1 h | `src/frontend/src/features/operational-intelligence/` |
| QW-07 | Add missing `operations:runbooks:write` permission if runbook CRUD is planned | Backend | P2 | 1 h | `RolePermissionCatalog.cs` |

**Total Quick Wins: ~12 h**

---

## B. Mandatory Functional Corrections

| ID | Item | Area | Priority | Effort | Notes |
|----|------|------|----------|--------|-------|
| FC-01 | Integrate with **Notifications** module — publish domain events that trigger notifications on critical incidents and automation outcomes | Backend | P1 | 8 h | No notification integration exists today |
| FC-02 | Integrate with **Audit & Compliance** module — forward sensitive actions (automation approve/execute, incident status changes) as audit events | Backend | P1 | 6 h | No audit forwarding exists |
| FC-03 | Add **Cost Intelligence** frontend pages (CostDashboardPage, CostByServicePage) — 9 backend endpoints have zero UI | Frontend | P1 | 16 h | `Cost/Endpoints/` has 9 endpoints, 0 pages |
| FC-04 | Add **RowVersion** (`xmin` concurrency token) to all mutable aggregate roots | Backend/Infra | P1 | 4 h | IncidentRecord, AutomationWorkflowRecord, CostSnapshot, RuntimeBaseline, ObservabilityProfile |
| FC-05 | Add FluentValidation rules for all command request DTOs that lack them | Backend | P2 | 4 h | Verify all 20+ commands |
| FC-06 | Harden automation execute endpoint — reject execution if ApprovalStatus ≠ Approved | Backend | P1 | 2 h | `UpdateAutomationWorkflowAction.cs` |
| FC-07 | Add advanced filters to IncidentsPage (severity, type, environment, date range) — backend supports them, frontend does not expose all | Frontend | P2 | 4 h | `IncidentsPage.tsx` |
| FC-08 | Validate IncidentCorrelationService returns real data (not seed/mock) | Backend | P2 | 3 h | `IncidentCorrelationService.cs` |

**Total Functional Corrections: ~47 h**

---

## C. Structural Adjustments

| ID | Item | Area | Priority | Effort | Notes |
|----|------|------|----------|--------|-------|
| SA-01 | Confirm all 19 tables already use `ops_` prefix — verify outbox tables use `ops_` not `oi_` | Infra | P1 | 2 h | Outbox tables currently `oi_inc_`, `oi_rel_`, `oi_rt_`, `oi_cost_` — must become `ops_*_outbox_messages` |
| SA-02 | Make reliability scoring thresholds **configurable** via Configuration module instead of hardcoded constants | Backend | P2 | 6 h | Currently hardcoded in `RuntimeSnapshot.cs` |
| SA-03 | Make automation precondition checks **extensible** — support custom precondition plugins | Backend | P3 | 8 h | `EvaluatePreconditions.cs` |
| SA-04 | Define ClickHouse schema for runtime metrics time-series (5 proposed tables in clickhouse-data-placement-review.md) | Infra | P3 | 4 h | No implementation yet — design only |
| SA-05 | Add **EnvironmentId** consistently to all entities that reference runtime/operational context | Backend | P2 | 4 h | Missing on some Cost and Automation entities |
| SA-06 | Implement Cost import pipeline end-to-end (ImportCostBatch handler exists but is partial) | Backend | P2 | 8 h | `ImportCostBatch.cs`, `CostImportBatch.cs` |
| SA-07 | Separate CostIntelligence into its own frontend feature folder if it grows beyond 3 pages | Frontend | P3 | 4 h | Currently part of `features/operations/` |
| SA-08 | Add domain events for all state transitions (incident status change, automation status change, drift detected) | Backend | P2 | 6 h | Only RuntimeAnomalyDetectedEvent and CostAnomalyDetectedEvent exist |

**Total Structural Adjustments: ~42 h**

---

## D. Pre-conditions for Migration Recreation

Before deleting existing migrations and generating the new baseline:

| # | Pre-condition | Status |
|---|--------------|--------|
| 1 | Domain model finalized (all 19 entities reviewed and confirmed) | ✅ Done (domain-model-finalization.md) |
| 2 | All table names confirmed with `ops_` prefix | ⚠️ Outbox tables need rename |
| 3 | RowVersion (`xmin`) added to all mutable aggregates | ❌ Not yet |
| 4 | EnvironmentId consistently applied | ⚠️ Partial |
| 5 | All FKs and indexes defined in entity configurations | ✅ Done (16 configurations exist) |
| 6 | Check constraints for enum columns defined | ❌ Not yet |
| 7 | Outbox table naming aligned to `ops_*_outbox_messages` | ❌ Not yet |
| 8 | Seed data strategy defined (dev-only vs production seeds) | ❌ Not yet |

---

## E. Module Acceptance Criteria

| # | Criterion | Status |
|---|----------|--------|
| 1 | Module role clearly defined | ✅ module-role-finalization.md |
| 2 | Functional scope finalized | ✅ module-scope-finalization.md |
| 3 | End-to-end operational flows mapped and validated | ✅ end-to-end-operational-flow-validation.md |
| 4 | Domain model closed | ✅ domain-model-finalization.md |
| 5 | Persistence with `ops_` prefix designed | ✅ persistence-model-finalization.md |
| 6 | PostgreSQL vs ClickHouse defined | ✅ clickhouse-data-placement-review.md |
| 7 | Backend and frontend correction backlogs exist | ✅ backend-functional-corrections.md + frontend-functional-corrections.md |
| 8 | Scoring, thresholds, automations reviewed | ✅ scoring-thresholds-automation-review.md |
| 9 | Permissions and security mapped | ✅ security-and-permissions-review.md |
| 10 | Module dependencies clear | ✅ module-dependency-map.md |
| 11 | Minimum documentation defined | ✅ documentation-and-onboarding-upgrade.md |
| 12 | Remediation plan exists | ✅ This document |

---

## Effort Summary

| Category | Items | Effort |
|----------|-------|--------|
| Quick Wins | 7 | ~12 h |
| Functional Corrections | 8 | ~47 h |
| Structural Adjustments | 8 | ~42 h |
| **Total** | **23** | **~101 h** |

---

## Execution Priority

1. **Wave 1 (P1 — immediate):** QW-01–04, FC-01, FC-02, FC-04, FC-06, SA-01 → ~28 h
2. **Wave 2 (P2 — next sprint):** FC-03, FC-05, FC-07, FC-08, SA-02, SA-05, SA-06, SA-08 → ~51 h
3. **Wave 3 (P3 — backlog):** QW-05–07, SA-03, SA-04, SA-07 → ~22 h
