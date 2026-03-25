# Change Governance — Security and Permissions Review

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Permission Matrix — Complete

### 1.1 ChangeIntelligence Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `change-intelligence:read` | GET releases, changes, intelligence summary, blast radius, advisory, decisions, freeze windows | Read | Core read permission for all change data |
| `change-intelligence:releases:read` | GET releases list, release history | Read | Specific to releases page |
| `change-intelligence:write` | POST deployments, markers, baselines, reviews, rollbacks, classifications, scores, blast radius, freeze windows, decisions | Write | All mutation operations |

### 1.2 Workflow Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `workflow:read` | GET instances, status, pending approvals, evidence | Read | View workflow data |
| `workflow:templates:read` | GET template by ID | Read | View specific template |
| `workflow:templates:write` | POST templates; ⚠️ also GET templates list (BUG) | Write | Template management |
| `workflow:write` | POST initiate, approve, reject, request-changes, generate evidence, export PDF | Write | Workflow lifecycle |

### 1.3 Promotion Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `promotion:read` | GET environments, requests, gates, status | Read | View promotion data |
| `promotion:write` | POST requests, evaluate gates, approve, block | Write | Promotion lifecycle |
| `promotion:admin:write` | POST gate override with justification | Admin Write | ⚠️ Sensitive — requires explicit admin permission |

### 1.4 RulesetGovernance Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `rulesets:read` | GET rulesets, findings, scores | Read | View ruleset data |
| `rulesets:write` | POST upload, bind, execute, archive | Write | Ruleset management |

### 1.5 Platform Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `platform:admin:read` | WorkflowConfigurationPage | Read | Admin configuration access |

---

## 2. Permission Enforcement — Backend

| Layer | Mechanism | Status |
|-------|-----------|--------|
| **Endpoint level** | `RequireAuthorization("permission:scope")` on endpoint registration | ✅ All endpoints protected |
| **Handler level** | MediatR pipeline with `IAuthorizationHandler` | ✅ Via pipeline behaviour |
| **Entity level** | `TenantId` filtering via `TenantRlsInterceptor` (PostgreSQL RLS) | ✅ Active on all 4 DbContexts |
| **Environment level** | `EnvironmentId` on Release, FreezeWindow, PromotionRequest | ⚠️ Not enforced at query level — application-side filtering |

---

## 3. Permission Enforcement — Frontend

| Layer | Mechanism | Status |
|-------|-----------|--------|
| **Route level** | `ProtectedRoute` with permission check in `App.tsx` | ✅ All 6 routes protected |
| **Sidebar visibility** | Permission-based menu filtering in `AppSidebar.tsx` | ✅ All 4 items filtered |
| **Action-level guards** | Conditional rendering of approve/reject/override buttons | ⚠️ Needs audit — some buttons may not check specific action permission |

---

## 4. Sensitive Actions Audit

| Action | Endpoint | Permission | Risk Level | Audit Trail |
|--------|----------|-----------|------------|-------------|
| Gate override with justification | `POST /promotion/gates/{id}/override` | `promotion:admin:write` | 🔴 High | ✅ `OverriddenBy` + `OverrideJustification` stored on `GateEvaluation` |
| Workflow approval | `POST /workflow/instances/{id}/approve` | `workflow:write` | 🟠 Medium | ✅ `ApprovedBy` + `Comment` + `ApprovedAt` stored on `ApprovalDecision` |
| Workflow rejection | `POST /workflow/instances/{id}/reject` | `workflow:write` | 🟠 Medium | ✅ Rejection reason stored |
| Record change decision | `POST /changes/{id}/record-decision` | `change-intelligence:write` | 🟠 Medium | ✅ Decision with actor stored |
| Register rollback | `POST /releases/{id}/rollback` | `change-intelligence:write` | 🟠 Medium | ✅ `RollbackAssessment` with `RollbackCommitSha` |
| Create freeze window | `POST /freeze-windows` | `change-intelligence:write` | 🟠 Medium | ✅ `CreatedBy` stored on `FreezeWindow` |
| Ruleset upload | `POST /rulesets/upload` | `rulesets:write` | 🟡 Low-Medium | ⚠️ No explicit audit of who uploaded |

---

## 5. Tenant Isolation

| Mechanism | Status | Details |
|-----------|--------|---------|
| `TenantId` on all entities | ✅ Present | All entities include `TenantId` via `AuditableEntity` |
| PostgreSQL RLS | ✅ Active | `TenantRlsInterceptor` applied via `NexTraceDbContextBase` on all 4 DbContexts |
| Application-level filtering | ✅ Active | Repository queries filter by tenant context |

---

## 6. Environment-Level Scoping

| Entity | `EnvironmentId` | Enforcement |
|--------|-----------------|-------------|
| Release | ✅ Present | Application-level filtering |
| FreezeWindow | ✅ Present | Scope enum (All, Environment, Service) |
| PromotionRequest | ✅ Present (SourceEnvironmentId, TargetEnvironmentId) | Application-level validation |
| WorkflowTemplate | ✅ Present (TargetEnvironment) | Template matching |

**Gap:** No RLS-level enforcement of `EnvironmentId` — scoping is purely application-side.

---

## 7. Security Gaps

| ID | Gap | Severity | Description |
|----|-----|----------|-------------|
| SEC-01 | Permission bug: GET templates requires `:write` | 🔴 High | Users need write permission to list templates (should be read) |
| SEC-02 | No rate limiting on analysis endpoints | 🟡 Medium | `POST /analysis/score` and `/blast-radius` could be abused for DoS |
| SEC-03 | Environment-level scoping is application-only | 🟡 Medium | No RLS for `EnvironmentId` — relies on correct application code |
| SEC-04 | Ruleset upload has no content validation | 🟡 Medium | JSON/YAML content not validated for malicious payloads |
| SEC-05 | No explicit audit event emission to Audit & Compliance module | 🟡 Medium | Outbox events exist but may not reach Audit module |
| SEC-06 | No minimum justification length on gate override | 🟡 Medium | Empty justification could bypass audit intent |
| SEC-07 | Frontend action-level permission guards need audit | 🟢 Low | Some action buttons may not check specific write permissions |

---

## 8. Security Correction Backlog

| ID | Item | Priority | Effort |
|----|------|----------|--------|
| SC-01 | Fix workflow templates GET permission bug | P0 | 1h |
| SC-02 | Add minimum length validation on gate override justification | P1 | 1h |
| SC-03 | Add content validation for ruleset uploads (JSON schema, YAML validation) | P1 | 4h |
| SC-04 | Add explicit audit event emission for sensitive actions | P1 | 8h |
| SC-05 | Audit frontend action-level permission guards | P2 | 4h |
| SC-06 | Consider rate limiting on analysis endpoints | P2 | 4h |
| SC-07 | Evaluate RLS-level enforcement for EnvironmentId | P3 | 8h |

**Total estimated effort:** ~30 hours
