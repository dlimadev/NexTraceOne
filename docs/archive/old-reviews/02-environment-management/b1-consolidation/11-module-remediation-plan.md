# Environment Management Module — Remediation Plan

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## Overview

This document consolidates all corrections, adjustments, and improvements identified during the B1 module consolidation review into a structured remediation plan. Items are grouped by effort level and sequenced by dependency.

**References:**
- `01-current-state-inventory.md` — Full inventory of existing code
- `02-module-boundary-finalization.md` — What belongs where
- `03-module-scope-finalization.md` — Feature scope and priorities
- `04-domain-model-finalization.md` — Entity model and new entities
- `05-persistence-model-finalization.md` — Database schema and migration
- `06-backend-functional-corrections.md` — Backend corrections and new features
- `07-frontend-functional-corrections.md` — Frontend corrections and new pages
- `08-security-and-permissions-review.md` — Permission model redesign
- `09-module-dependency-map.md` — Cross-module dependencies
- `10-documentation-and-onboarding-upgrade.md` — Documentation gaps

---

## A. Quick Wins (Low Effort, High Impact)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| A1 | Add EnvironmentsPage to sidebar | Frontend/Navigation | **CRITICAL** | Small | Add sidebar entry with `sidebar.environments` label key, `Server` icon, `env:environments:read` permission. Currently the page is only accessible via direct URL. See `07-frontend-functional-corrections.md` §3.1. |
| A2 | Add i18n key for sidebar entry | Frontend/i18n | **CRITICAL** | Small | Add `sidebar.environments: "Environments"` to `src/frontend/src/locales/en.json` (and all locales). |
| A3 | Align EnvironmentProfile enum | Domain/Backend | **HIGH** | Small | Add `Sandbox`, `Training`, `UserAcceptanceTesting`, `PerformanceTesting` values to backend `EnvironmentProfile` enum to match frontend. See `04-domain-model-finalization.md` §4.1. |
| A4 | Add GetEnvironmentById endpoint | Backend/API | **HIGH** | Small | Add `GET /api/v1/environments/{id}` endpoint returning full environment details. Currently only list and primary-production queries exist. See `06-backend-functional-corrections.md` §4.1. |
| A5 | Add audit columns to Environment | Domain/Backend | **HIGH** | Small | Add `UpdatedAt`, `CreatedBy`, `UpdatedBy` properties to `Environment` entity and `EnvironmentConfiguration`. See `04-domain-model-finalization.md` §1.3. |
| A6 | Add xmin concurrency token | Persistence | **HIGH** | Small | Add `.UseXminAsConcurrencyToken()` to `EnvironmentConfiguration`. See `05-persistence-model-finalization.md` §4. |

### Quick Win Execution Order

```
A6 → A5 → A3 → A4 → A1 → A2
(persistence safety first, then domain alignment, then navigation)
```

---

## B. Functional Corrections (Feature Gaps and Safety)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| B1 | Register new env:* permissions | Security | **CRITICAL** | Medium | Create and register: `env:environments:read`, `env:environments:write`, `env:promotion:read`, `env:promotion:write`, `env:baseline:read`, `env:baseline:write`, `env:policies:read`, `env:policies:write`. See `08-security-and-permissions-review.md` §2. |
| B2 | Update endpoint permissions | Security | **CRITICAL** | Medium | Replace `identity:users:read/write` with `env:environments:read/write` on all 6 existing endpoints. Add backward compatibility mapping during transition. See `08-security-and-permissions-review.md` §8.1. |
| B3 | Add soft-delete endpoint | Backend/API | **HIGH** | Medium | Add `DELETE /api/v1/environments/{id}` with guards: cannot deactivate production env, cannot deactivate if services bound, emit `EnvironmentDeactivated` event. See `06-backend-functional-corrections.md` §4.1. |
| B4 | Add missing validations | Backend/Validation | **HIGH** | Medium | Add: duplicate slug check, profile enum validation, concurrency check on update, cannot designate inactive env as primary, cannot designate non-Production profile as primary. See `06-backend-functional-corrections.md` §3.2. |
| B5 | Handle DbUpdateConcurrencyException | Backend/Safety | **HIGH** | Medium | Add try/catch for `DbUpdateConcurrencyException` in all write handlers. Return 409 Conflict with error code `ENVIRONMENT_CONFLICT` and `messageKey`. See `06-backend-functional-corrections.md` §3.3. |
| B6 | Persist EnvironmentPolicy entity | Persistence | **HIGH** | Medium | Add `EnvironmentPolicies` DbSet, create `EnvironmentPolicyConfiguration` with table `env_environment_policies`. Entity exists but is dead code without persistence. See `05-persistence-model-finalization.md` §5.1. |
| B7 | Persist EnvironmentTelemetryPolicy | Persistence | **MEDIUM** | Medium | Add `EnvironmentTelemetryPolicies` DbSet, create configuration with table `env_environment_telemetry_policies`. See `05-persistence-model-finalization.md` §5.2. |
| B8 | Persist EnvironmentIntegrationBinding | Persistence | **MEDIUM** | Medium | Add `EnvironmentIntegrationBindings` DbSet, create configuration with table `env_environment_integration_bindings`. See `05-persistence-model-finalization.md` §5.3. |

### Functional Corrections Execution Order

```
B1 → B2 → B4 → B5 → B3 → B6 → B7 → B8
(security first, then validation, then persistence gaps)
```

---

## C. Structural Adjustments (Module Extraction)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| C1 | Create module project structure | Architecture | **HIGH** | Large | Create `src/modules/environmentmanagement/` with 4 projects: Domain, Application, API, Infrastructure. Follow same structure as other modules. See `06-backend-functional-corrections.md` §2.1. |
| C2 | Move entities to new module | Architecture | **HIGH** | Large | Move `Environment`, `EnvironmentPolicy`, `EnvironmentTelemetryPolicy`, `EnvironmentIntegrationBinding` from IdentityAccess.Domain to EnvironmentManagement.Domain. Move enums and value objects. See `02-module-boundary-finalization.md` §10. |
| C3 | Create EnvironmentManagementDbContext | Persistence | **HIGH** | Large | Create new DbContext with `env_` table prefix. Register DbSets for all entities. Apply `UseXminAsConcurrencyToken()` on all entities. See `05-persistence-model-finalization.md` §4. |
| C4 | Create database migration | Persistence | **HIGH** | Large | Rename `dbo.Environments` → `env_environments`. Create 6 new tables. Add new columns to environments table. See `05-persistence-model-finalization.md` §6. |
| C5 | Move CQRS features to new module | Architecture | **HIGH** | Large | Move 5 features (ListEnvironments, CreateEnvironment, UpdateEnvironment, GetPrimaryProductionEnvironment, SetPrimaryProductionEnvironment) to EnvironmentManagement.Application. Keep GrantEnvironmentAccess in Identity. See `06-backend-functional-corrections.md` §2.2. |
| C6 | Move endpoint module | Architecture | **HIGH** | Medium | Move environment endpoints from IdentityAccess.API to EnvironmentManagement.API. Update route registration. |
| C7 | Move EnvironmentRepository | Architecture | **HIGH** | Medium | Move from IdentityAccess.Infrastructure to EnvironmentManagement.Infrastructure. Update DI registration. |
| C8 | Register module in host | Architecture | **HIGH** | Small | Register EnvironmentManagement module in API host startup. Add DI registrations for DbContext, repository, handlers. |
| C9 | Create frontend feature module | Frontend | **HIGH** | Medium | Create `src/frontend/src/features/environment-management/` with api/, pages/, components/, hooks/ directories. Move EnvironmentsPage. Extract API service. See `07-frontend-functional-corrections.md` §2.1. |
| C10 | Update routes and imports | Frontend | **HIGH** | Medium | Update App.tsx imports to new feature module. Update permission guards to `env:*`. Add new routes for future pages. See `07-frontend-functional-corrections.md` §3.3. |
| C11 | Remove environment code from IdentityAccess | Architecture | **HIGH** | Medium | Remove moved entities, features, configurations from Identity module. Update IdentityDbContext to remove Environments DbSet. Verify Identity module still compiles. See `06-backend-functional-corrections.md` §2.2. |

### Module Extraction Execution Order

```
C1 → C2 → C3 → C4 → C5 → C6 → C7 → C8 → C9 → C10 → C11
(sequential — each step depends on the previous)
```

---

## D. New Features (Post-Extraction)

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| D1 | Promotion path CRUD | Backend | **HIGH** | Large | Create `PromotionPath` entity, 4 endpoints (list, create, update, delete), validation (min 2 steps, no cycles, last step production). See `03-module-scope-finalization.md` §3.2. |
| D2 | Promotion path UI | Frontend | **HIGH** | Large | Create `PromotionPathsPage` and `PromotionPathEditorPage` with visual path editor. See `07-frontend-functional-corrections.md` §4.2, §4.3. |
| D3 | Environment detail page | Frontend | **HIGH** | Large | Create `EnvironmentDetailPage` with tabs: overview, policies, integrations, baseline, drift, readiness, history. See `07-frontend-functional-corrections.md` §4.1. |
| D4 | Baseline management | Backend | **MEDIUM** | Medium | Create `EnvironmentBaseline` entity, 3 endpoints (set, get, history). See `03-module-scope-finalization.md` §3.3. |
| D5 | Drift detection | Backend | **MEDIUM** | Large | Create drift detection logic comparing current state against baseline. 2 endpoints (detect, compare envs). See `03-module-scope-finalization.md` §3.3. |
| D6 | Readiness scoring | Backend | **MEDIUM** | Medium | Create `EnvironmentReadinessCheck` entity, 1 endpoint (check readiness). See `03-module-scope-finalization.md` §3.4. |
| D7 | Policy CRUD endpoints | Backend | **MEDIUM** | Medium | Add 4 endpoints for environment policy management. See `06-backend-functional-corrections.md` §4.5. |
| D8 | Drift and baseline UI | Frontend | **MEDIUM** | Large | Create `EnvironmentDriftPage`, `EnvironmentBaselinePage`. See `07-frontend-functional-corrections.md` §4.4, §4.5. |
| D9 | Readiness dashboard | Frontend | **LOW** | Medium | Create `EnvironmentReadinessPage` with scores per environment. See `07-frontend-functional-corrections.md` §4.6. |
| D10 | Domain events integration | Backend | **MEDIUM** | Medium | Emit domain events on all write operations. Integrate with Audit & Compliance and Notifications modules. See `04-domain-model-finalization.md` §6. |
| D11 | Environment grouping | Backend + Frontend | **LOW** | Large | Create grouping model, CRUD, and UI. See `03-module-scope-finalization.md` §3.5. |

---

## E. Documentation Tasks

| # | Item | Category | Priority | Effort | Details |
|---|------|----------|----------|--------|---------|
| E1 | Fill module README | Documentation | **CRITICAL** | Small | Complete README.md with real module data. See `10-documentation-and-onboarding-upgrade.md` §6.1. |
| E2 | Fill module overview | Documentation | **HIGH** | Small | Complete module-overview.md with entities, flows, risks. |
| E3 | Document all endpoints | Documentation | **HIGH** | Medium | Fill endpoints.md with request/response schemas, permissions, error codes for all 6 (then 21) endpoints. |
| E4 | Document schema | Documentation | **HIGH** | Medium | Fill schema-review.md with current and target table definitions. |
| E5 | Document permissions | Documentation | **HIGH** | Small | Fill authorization-rules.md with new env:* permission model. |
| E6 | Add XML doc comments | Documentation | **MEDIUM** | Medium | Add XML doc comments to all entities, enums, value objects. |
| E7 | Create test scenarios | Documentation | **MEDIUM** | Medium | Fill test-scenarios.md with happy path, edge cases, error scenarios. |
| E8 | Catalogue technical debt | Documentation | **MEDIUM** | Small | Fill technical-debt.md with identified items from this review. |
| E9 | Create integration guide | Documentation | **MEDIUM** | Medium | Document how other modules consume Environment Management. |

---

## Pre-Conditions for Module Extraction

Before starting Phase C (structural adjustments), the following must be true:

| # | Pre-Condition | Why | Status |
|---|-------------|-----|--------|
| 1 | All Quick Wins (A1-A6) completed | Safety and alignment before structural change | ⬜ |
| 2 | All Functional Corrections (B1-B5) completed | Security and validation before extraction | ⬜ |
| 3 | Module README documented (E1) | Team alignment on scope | ⬜ |
| 4 | Permission model agreed (E5) | Cannot extract with wrong permissions | ⬜ |
| 5 | Database migration strategy reviewed by DBA | Cannot rename tables without review | ⬜ |
| 6 | Feature flag for new module available | Need to run old and new in parallel during transition | ⬜ |
| 7 | All consuming modules identified and notified | Avoid breaking changes | ⬜ |
| 8 | Rollback plan documented | If extraction fails, how to revert | ⬜ |

---

## Acceptance Criteria per Phase

### Phase A — Quick Wins ✅

- [ ] EnvironmentsPage appears in sidebar navigation
- [ ] `sidebar.environments` i18n key exists in all locales
- [ ] Backend `EnvironmentProfile` enum includes all frontend-supported profiles
- [ ] `GET /api/v1/environments/{id}` returns full environment details
- [ ] `Environment` entity has `UpdatedAt`, `CreatedBy`, `UpdatedBy` properties
- [ ] `xmin` concurrency token configured on `Environment`

### Phase B — Functional Corrections ✅

- [ ] 8 new `env:*` permissions registered in the system
- [ ] All 6 endpoints use `env:*` permissions (with backward compat)
- [ ] `DELETE /api/v1/environments/{id}` endpoint exists with guards
- [ ] All validations from §B4 are implemented and tested
- [ ] `DbUpdateConcurrencyException` handled in all write handlers
- [ ] `EnvironmentPolicy`, `EnvironmentTelemetryPolicy`, `EnvironmentIntegrationBinding` are persisted

### Phase C — Module Extraction ✅

- [ ] `src/modules/environmentmanagement/` exists with 4 projects
- [ ] All environment entities live in EnvironmentManagement.Domain
- [ ] `EnvironmentManagementDbContext` registered with `env_` table prefix
- [ ] Database migration applied: table renamed, new tables created, new columns added
- [ ] All 5 CQRS features moved (GrantEnvironmentAccess stays in Identity)
- [ ] Endpoint module serves from EnvironmentManagement.API
- [ ] Frontend `features/environment-management/` folder exists
- [ ] `EnvironmentsPage` moved and working from new feature module
- [ ] All Identity module environment code removed
- [ ] Both old and new modules tested end-to-end

### Phase D — New Features ✅ (Incremental)

- [ ] Promotion path CRUD working (4 endpoints + UI)
- [ ] Environment detail page with tabs working
- [ ] Baseline set/get working
- [ ] Drift detection returning findings
- [ ] Readiness scoring returning 0-100 score
- [ ] Policy CRUD endpoints working
- [ ] Domain events emitted and consumed by Audit

### Phase E — Documentation ✅

- [ ] README complete and accurate
- [ ] All endpoints documented with schemas
- [ ] Permission model documented
- [ ] Schema documented
- [ ] Test scenarios documented
- [ ] New developer can onboard in < 5 hours

---

## Timeline Estimate

| Phase | Description | Estimated Effort | Dependencies |
|-------|------------|-----------------|-------------|
| **A** | Quick Wins | 2-3 days | None |
| **B** | Functional Corrections | 1 week | A completed |
| **E1-E5** | Critical Documentation | 2-3 days | Can parallel with A/B |
| **C** | Module Extraction | 2-3 weeks | A, B, E1-E5 completed |
| **D1-D3** | Priority Features | 2-3 weeks | C completed |
| **D4-D7** | Core Features | 2-3 weeks | D1-D3 completed |
| **D8-D11** | Advanced Features | 2-3 weeks | D4-D7 completed |
| **E6-E9** | Remaining Documentation | 1 week | Ongoing |

**Total estimated time: 10-14 weeks** (for full module extraction and feature implementation)

---

## Risk Summary

| # | Risk | Severity | Mitigation | Phase |
|---|------|----------|-----------|-------|
| 1 | Breaking Identity module during extraction | **HIGH** | Feature flag, parallel operation, comprehensive tests | C |
| 2 | Permission migration breaks existing users | **HIGH** | Backward compatibility mapping during transition | B |
| 3 | Database migration causes downtime | **MEDIUM** | Online-compatible schema changes, blue-green deployment | C |
| 4 | Cross-module references break | **MEDIUM** | Use strongly-typed IDs, no navigation properties | C |
| 5 | Frontend route changes break bookmarks | **LOW** | Routes stay the same (only feature module changes) | C |
| 6 | Scope creep during extraction | **MEDIUM** | Strict adherence to this plan, no feature additions during extraction | C |
