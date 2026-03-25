# Governance Module — Remediation Plan

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## Overview

This document consolidates all corrections, adjustments, and improvements identified during the B1 module consolidation review into a structured remediation plan. Items are grouped by effort level and sequenced by dependency.

---

## A. Quick Wins (Low Effort, High Impact)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| A1 | Promote 5 pages to sidebar | Frontend/Navigation | **HIGH** | Small | Add EnterpriseControlsPage, EvidencePackagesPage, MaturityScorecardsPage, WaiversPage, and BenchmarkingPage to the governance sidebar menu. These pages are currently only accessible via deep links. |
| A2 | Fix DelegatedAdmin POST permission | Security | **CRITICAL** | Small | Change DelegatedAdminEndpointModule POST from `platform:admin:read` to `governance:admin:write`. A read permission must never authorize a write/create operation. |
| A3 | Fix Onboarding permission | Security | **MEDIUM** | Small | Change OnboardingEndpointModule permission from `governance:teams:read` to `governance:compliance:read` or a dedicated `governance:onboarding:read`. Onboarding is not a team-specific action. |
| A4 | Create module README | Documentation | **HIGH** | Small | Create `README.md` in the Governance module root with module overview, structure, subdomains, key concepts, and developer setup instructions. |
| A5 | Align frontend route permissions with backend | Security | **HIGH** | Medium | Replace generic `governance:read` on all 24 routes with the matching backend permission per page group (see security-and-permissions-review.md §5 for the mapping). |
| A6 | Document subdomain boundaries | Documentation | **HIGH** | Small | Create subdomain overview document listing the 10 governance subdomains (Teams & Org, Packs & Rules, Compliance & Waivers, Risk, FinOps, Executive, Policies, Controls, Evidence, Delegated Admin) with their responsibilities and entities. |

### Quick Win Execution Order

```
A2 → A3 → A5 → A1 → A4 → A6
(security fixes first, then navigation, then documentation)
```

---

## B. Functional Corrections (Feature Gaps and Data Safety)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| B1 | Add Policy CRUD endpoints | Feature gap | **HIGH** | Medium | Add POST, PUT, DELETE to PolicyCatalogEndpointModule. Requires: command handlers, validators, repository methods. Permission: `governance:policies:write`. |
| B2 | Add Evidence creation endpoint | Feature gap | **HIGH** | Medium | Add POST to EvidencePackagesEndpointModule. Requires: command handler, validator, repository method. Permission: `governance:evidence:write` (new permission). |
| B3 | Add Controls CRUD endpoints | Feature gap | **MEDIUM** | Medium | Add POST, PUT, DELETE to EnterpriseControlsEndpointModule. Requires: command handlers, validators, repository methods. Permission: `governance:controls:write` (new permission). |
| B4 | Map GovernanceRuleBinding in DbContext | Persistence gap | **HIGH** | Small | Add DbSet `RuleBindings` to GovernanceDbContext, create `GovernanceRuleBindingConfiguration` with table `gov_rule_bindings`, add to migration. |
| B5 | Add `xmin` concurrency token to all entities | Data safety | **HIGH** | Medium | Add `.UseXminAsConcurrencyToken()` to all 9 entity EF configurations. Requires migration recreation. |
| B6 | Handle `DbUpdateConcurrencyException` | Data safety | **HIGH** | Medium | Add try/catch for `DbUpdateConcurrencyException` in all write handlers (create, update, delete, status change). Return 409 Conflict with appropriate error message. |

### Functional Corrections Execution Order

```
B4 → B5 → B6 → B1 → B2 → B3
(persistence fixes first to ensure safe foundation, then feature additions)
```

### Frontend Follow-Up Required After B1–B3

| Backend Item | Frontend Follow-Up |
|-------------|-------------------|
| B1 (Policy CRUD) | Add Create/Edit/Delete buttons to PolicyCatalogPage |
| B2 (Evidence create) | Add "Submit Evidence" form to EvidencePackagesPage |
| B3 (Controls CRUD) | Add Create/Edit/Delete buttons to EnterpriseControlsPage |

---

## C. Structural Adjustments (Architecture and Module Boundaries)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| C1 | Document Integrations extraction plan | Module boundary | **HIGH** | Medium | Formalize extraction of 3 entities (IntegrationConnector, IngestionSource, IngestionExecution), 1 endpoint module, 8 CQRS handlers, 3 repositories, 3 EF configs, 6 enums, 3 DbSets, 3 tables to the Integrations module. See integrations-and-product-analytics-dependency-map.md for full inventory. |
| C2 | Document Product Analytics extraction plan | Module boundary | **HIGH** | Medium | Formalize extraction of 1 entity (AnalyticsEvent), 1 endpoint module, 7 CQRS handlers, 1 repository, 1 EF config, 6 enums, 1 DbSet, 1 table to the Product Analytics module. See integrations-and-product-analytics-dependency-map.md for full inventory. |
| C3 | Evaluate PlatformStatus and Onboarding placement | Module boundary | **MEDIUM** | Small | Determine whether PlatformStatusEndpointModule belongs to Operational Intelligence or Platform. Determine whether OnboardingEndpointModule belongs to Platform or Identity. Document decision. Defer physical move to B2. |
| C4 | Verify executive dashboards use real data | Data integrity | **MEDIUM** | Medium | Confirm ExecutiveOverviewPage, CompliancePage, RiskCenterPage, FinOpsPage display real aggregated data (not hardcoded/mocked). Verify MaturityScorecardsPage and BenchmarkingPage have appropriate data sources. |
| C5 | Add pack status transition validation in entity | Domain logic | **HIGH** | Small | Move pack status transition rules (Draft→Published→Deprecated→Archived) from handlers into the GovernancePack entity as a domain method with guard clauses. Enforce invariants at the aggregate level. |

### Structural Adjustments Execution Order

```
C5 → C4 → C3 → C1 → C2
(domain logic first, then data verification, then boundary documentation)
```

### Extraction Execution (Separate Tracks)

The actual extraction of Integrations (C1) and Product Analytics (C2) entities/endpoints is a **separate work stream** that occurs after B1 consolidation is complete. The B1 deliverable is the **documented extraction plan**, not the extraction itself.

---

## D. Pre-conditions for Migration Recreation

Before the GovernanceDbContext migrations can be recreated (clean single migration replacing the current 7 files), the following must be finalized:

| # | Pre-condition | Status | Depends On | Details |
|---|-------------- |--------|-----------|---------|
| D1 | Domain model finalized | ✅ Documented | domain-model-finalization.md | 5 aggregate roots, 9 entities, relationships defined |
| D2 | Persistence model finalized | ✅ Documented | persistence-model-finalization.md | Table definitions, columns, types, indexes, constraints defined |
| D3 | GovernanceRuleBinding mapped | 🔲 Pending | B4 | DbSet + EF configuration must be added before migration recreation |
| D4 | RowVersion (`xmin`) added to all entities | 🔲 Pending | B5 | `.UseXminAsConcurrencyToken()` must be in all configurations |
| D5 | Extraction documented and boundaries clear | 🔲 Pending | C1, C2 | Must know exactly which entities/tables remain in GovernanceDbContext |
| D6 | Check constraints defined | ✅ Documented | persistence-model-finalization.md | Enum check constraints, string length constraints, nullable constraints defined |

### Migration Recreation Steps

Once all pre-conditions are met:

```
1. Delete existing 7 migration files from Governance/Infrastructure/Persistence/Migrations/
2. Run: dotnet ef migrations add InitialCreate --context GovernanceDbContext
3. Verify generated migration matches persistence-model-finalization.md
4. Manually add:
   a. Check constraints for enum columns
   b. Index definitions (unique, filtered, composite)
   c. xmin concurrency tokens
   d. Seed data (if any)
5. Run: dotnet ef database update --context GovernanceDbContext
6. Verify all tables, columns, indexes, and constraints are correct
7. Run existing tests to verify no regressions
```

### Tables in Recreated Migration (After Extraction)

| # | Table | Entity | Notes |
|---|-------|--------|-------|
| 1 | `gov_teams` | Team | |
| 2 | `gov_team_domain_links` | TeamDomainLink | |
| 3 | `gov_domains` | GovernanceDomain | |
| 4 | `gov_packs` | GovernancePack | |
| 5 | `gov_pack_versions` | GovernancePackVersion | |
| 6 | `gov_rollout_records` | GovernanceRolloutRecord | |
| 7 | `gov_rule_bindings` | GovernanceRuleBinding | **NEW** — added via B4 |
| 8 | `gov_waivers` | GovernanceWaiver | |
| 9 | `gov_delegated_admins` | DelegatedAdministration | |
| 10 | `gov_outbox_messages` | OutboxMessage | Shared outbox pattern |

**No Integrations or Product Analytics tables** — those will be in their own DbContexts.

---

## E. Module Closure Criteria

The Governance B1 consolidation is complete when ALL of the following are satisfied:

### E1. Documentation Complete

| # | Criterion | Status |
|---|-----------|--------|
| 1 | current-state-inventory.md approved | ✅ |
| 2 | module-scope-finalization.md approved | ✅ |
| 3 | module-boundary-finalization.md approved | ✅ |
| 4 | domain-model-finalization.md approved | ✅ |
| 5 | persistence-model-finalization.md approved | ✅ |
| 6 | backend-functional-corrections.md approved | ✅ |
| 7 | frontend-functional-corrections.md approved | ✅ |
| 8 | security-and-permissions-review.md approved | ✅ |
| 9 | integrations-and-product-analytics-dependency-map.md approved | ✅ |
| 10 | documentation-and-onboarding-upgrade.md approved | ✅ |
| 11 | module-remediation-plan.md approved (this document) | ✅ |

### E2. Quick Wins Executed

| # | Criterion | Status |
|---|-----------|--------|
| 1 | DelegatedAdmin POST permission fixed (A2) | 🔲 Pending |
| 2 | Onboarding permission fixed (A3) | 🔲 Pending |
| 3 | Frontend permissions aligned with backend (A5) | 🔲 Pending |
| 4 | 5 pages promoted to sidebar (A1) | 🔲 Pending |
| 5 | Module README created (A4) | 🔲 Pending |
| 6 | Subdomain boundaries documented (A6) | 🔲 Pending |

### E3. Functional Corrections Planned

| # | Criterion | Status |
|---|-----------|--------|
| 1 | All corrections identified and documented in backlog | ✅ |
| 2 | Priorities assigned | ✅ |
| 3 | Execution order defined | ✅ |
| 4 | Dependencies mapped | ✅ |

### E4. Migration Recreation Ready

| # | Criterion | Status |
|---|-----------|--------|
| 1 | All pre-conditions documented | ✅ |
| 2 | Pending pre-conditions have clear owners and timeline | 🔲 Pending |
| 3 | Migration recreation steps defined | ✅ |
| 4 | Post-recreation verification plan defined | ✅ |

### E5. Module Boundary Clean

| # | Criterion | Status |
|---|-----------|--------|
| 1 | All 9 governance entities correctly identified | ✅ |
| 2 | All 4 foreign entities identified for extraction | ✅ |
| 3 | Extraction plan documented (C1, C2) | ✅ |
| 4 | PlatformStatus/Onboarding evaluation documented (C3) | 🔲 Pending |
| 5 | No new foreign entities introduced | ✅ |

---

## Timeline Summary

```
Phase 1: Quick Wins (A1–A6)
├── Week 1: Security fixes (A2, A3, A5)
├── Week 1: Navigation fix (A1)
└── Week 2: Documentation (A4, A6)

Phase 2: Functional Corrections (B1–B6)
├── Week 2–3: Persistence fixes (B4, B5, B6)
├── Week 3–4: Policy CRUD (B1)
├── Week 4: Evidence create (B2)
└── Week 4–5: Controls CRUD (B3)

Phase 3: Structural Adjustments (C1–C5)
├── Week 3: Pack status validation (C5)
├── Week 4: Dashboard data verification (C4)
├── Week 4: PlatformStatus/Onboarding evaluation (C3)
└── Week 5: Extraction plan documentation (C1, C2)

Phase 4: Migration Recreation
├── Week 5: Verify all pre-conditions (D1–D6)
└── Week 6: Execute migration recreation

Phase 5: Module Closure
└── Week 6: Verify all closure criteria (E1–E5)
```

---

## Summary

The Governance module remediation plan contains **6 quick wins**, **6 functional corrections**, **5 structural adjustments**, **6 migration pre-conditions**, and **5 closure criteria groups**. The execution follows a dependency-aware sequence: security fixes → persistence fixes → feature additions → boundary documentation → migration recreation. All items are tracked with priority, effort, and status to ensure visibility and accountability throughout the B1 consolidation process.
