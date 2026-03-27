# Governance Module — Security and Permissions Review

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Permissions by Page

| # | Page | Route | Frontend Guard | Required Backend Permission |
|---|------|-------|---------------|---------------------------|
| 1 | ExecutiveOverviewPage | `/governance/executive` | `governance:read` | `governance:compliance:read` |
| 2 | ExecutiveDrillDownPage | `/governance/executive/drill-down` | `governance:read` | `governance:compliance:read` |
| 3 | ExecutiveFinOpsPage | `/governance/executive/finops` | `governance:read` | `governance:compliance:read` |
| 4 | ReportsPage | `/governance/reports` | `governance:read` | `governance:reports:read` |
| 5 | CompliancePage | `/governance/compliance` | `governance:read` | `governance:compliance:read` |
| 6 | RiskCenterPage | `/governance/risk` | `governance:read` | `governance:risk:read` |
| 7 | RiskHeatmapPage | `/governance/risk/heatmap` | `governance:read` | `governance:risk:read` |
| 8 | FinOpsPage | `/governance/finops` | `governance:read` | `governance:finops:read` |
| 9 | ServiceFinOpsPage | `/governance/finops/service` | `governance:read` | `governance:finops:read` |
| 10 | TeamFinOpsPage | `/governance/finops/team` | `governance:read` | `governance:finops:read` |
| 11 | DomainFinOpsPage | `/governance/finops/domain` | `governance:read` | `governance:finops:read` |
| 12 | PolicyCatalogPage | `/governance/policies` | `governance:read` | `governance:policies:read` |
| 13 | EnterpriseControlsPage | `/governance/controls` | `governance:read` | `governance:controls:read` |
| 14 | EvidencePackagesPage | `/governance/evidence` | `governance:read` | `governance:evidence:read` |
| 15 | MaturityScorecardsPage | `/governance/maturity` | `governance:read` | `governance:compliance:read` |
| 16 | BenchmarkingPage | `/governance/benchmarking` | `governance:read` | `governance:compliance:read` |
| 17 | TeamsOverviewPage | `/governance/teams` | `governance:read` | `governance:teams:read` |
| 18 | TeamDetailPage | `/governance/teams/:id` | `governance:read` | `governance:teams:read` |
| 19 | DomainsOverviewPage | `/governance/domains` | `governance:read` | `governance:domains:read` |
| 20 | DomainDetailPage | `/governance/domains/:id` | `governance:read` | `governance:domains:read` |
| 21 | GovernancePacksOverviewPage | `/governance/packs` | `governance:read` | `governance:packs:read` |
| 22 | GovernancePackDetailPage | `/governance/packs/:id` | `governance:read` | `governance:packs:read` |
| 23 | WaiversPage | `/governance/waivers` | `governance:read` | `governance:waivers:read` |
| 24 | DelegatedAdminPage | `/governance/delegated-admin` | `platform:admin:read` | `governance:admin:read` |
| 25 | GovernanceConfigurationPage | `/governance/configuration` | `platform:admin:read` | `platform:admin:read` |

---

## 2. Permissions by Action (Backend — Granular)

### Read Permissions

| Permission | Scope | Used By |
|-----------|-------|---------|
| `governance:packs:read` | Pack listing and details | GovernancePacksEndpointModule, GovernancePacksVersionsEndpointModule |
| `governance:domains:read` | Domain listing and details | DomainEndpointModule |
| `governance:teams:read` | Team listing and details | TeamEndpointModule |
| `governance:waivers:read` | Waiver listing and details | GovernanceWaiversEndpointModule |
| `governance:admin:read` | Delegated admin listing | DelegatedAdminEndpointModule |
| `governance:compliance:read` | Compliance summary, executive views, scoped context | ComplianceChecks, Compliance, Executive, ScopedContext |
| `governance:analytics:read` | Product analytics data | ProductAnalyticsEndpointModule |
| `governance:evidence:read` | Evidence packages | EvidencePackagesEndpointModule |
| `governance:finops:read` | FinOps dashboards | GovernanceFinOpsEndpointModule |
| `governance:risk:read` | Risk analysis | GovernanceRiskEndpointModule |
| `governance:reports:read` | Governance reports | GovernanceReportsEndpointModule |
| `governance:controls:read` | Enterprise controls | EnterpriseControlsEndpointModule |
| `governance:policies:read` | Policy catalog | PolicyCatalogEndpointModule |
| `integrations:read` | Integration connectors, ingestion | IntegrationHubEndpointModule |
| `platform:admin:read` | Platform status, configuration | PlatformStatusEndpointModule |

### Write Permissions

| Permission | Scope | Used By |
|-----------|-------|---------|
| `governance:packs:write` | Create, update, delete, publish packs | GovernancePacksEndpointModule |
| `governance:domains:write` | Create, update, delete domains | DomainEndpointModule |
| `governance:teams:write` | Create, update, delete teams | TeamEndpointModule |
| `governance:waivers:write` | Create, update, approve, delete waivers | GovernanceWaiversEndpointModule |
| `governance:admin:write` | Create delegated admin entries | DelegatedAdminEndpointModule |
| `governance:compliance:write` | Run compliance checks | ComplianceChecksEndpointModule |
| `governance:analytics:write` | Record analytics events | ProductAnalyticsEndpointModule |
| `governance:policies:write` | **NOT YET USED** — no write endpoints exist | — |
| `integrations:write` | Create, update, delete connectors/sources | IntegrationHubEndpointModule |

---

## 3. Frontend Permission Guards

### Current Implementation

```
ProtectedRoute permission="governance:read"
  └── All 24 governance routes
  
ProtectedRoute permission="platform:admin:read"
  └── DelegatedAdminPage
  └── GovernanceConfigurationPage
```

### Analysis

| Aspect | Status | Notes |
|--------|--------|-------|
| Guard component | ✅ `ProtectedRoute` | Wraps all governance routes |
| Permission check | ⚠️ Too broad | Single `governance:read` for 24 diverse pages |
| Write action guards | ❌ Missing | No per-action permission checks on buttons/forms |
| Admin routes guarded | ✅ Present | DelegatedAdmin and Configuration use `platform:admin:read` |
| Role-based visibility | ❌ Missing | Menu items not filtered by user permissions |

---

## 4. Backend Permission Enforcement

### Current Implementation

| Aspect | Status | Notes |
|--------|--------|-------|
| Endpoint-level authorization | ✅ Present | Each endpoint module declares required permission |
| Handler-level authorization | ✅ Present | CQRS handlers enforce permissions via middleware |
| Granular permission model | ✅ Present | 12+ specific permission strings |
| Permission hierarchy | ⚠️ Not implemented | No `governance:*` super-permission for admins |
| Permission caching | ✅ Present | Token-based permissions cached per request |

---

## 5. Critical Gap: Frontend vs. Backend Permission Mismatch

### The Problem

The backend enforces **12+ granular permissions** per endpoint module. The frontend checks only **1 generic permission** (`governance:read`) for all 24 governance routes.

This means:
- A user with only `governance:teams:read` can navigate to the FinOps page (frontend allows it) but gets 403 from the backend
- A user without `governance:risk:read` can see the Risk Center in the sidebar and navigate to it, only to see an error
- The user experience is degraded by permission mismatches

### Required Frontend Alignment

| Page Group | Required Frontend Permission |
|-----------|---------------------------|
| Executive pages (3) | `governance:compliance:read` |
| Compliance page | `governance:compliance:read` |
| Risk pages (2) | `governance:risk:read` |
| FinOps pages (4) | `governance:finops:read` |
| Policy page | `governance:policies:read` |
| Controls page | `governance:controls:read` |
| Evidence page | `governance:evidence:read` |
| Reports page | `governance:reports:read` |
| Teams pages (2) | `governance:teams:read` |
| Domains pages (2) | `governance:domains:read` |
| Packs pages (2) | `governance:packs:read` |
| Waivers page | `governance:waivers:read` |
| Maturity/Benchmarking (2) | `governance:compliance:read` |
| DelegatedAdmin page | `governance:admin:read` |
| Configuration page | `platform:admin:read` |

### Impact of Not Fixing

- Users see pages they cannot access → 403 errors
- Sidebar shows items the user has no permission for
- No client-side write permission checks → forms render but submit fails
- Poor UX and perceived broken behavior

---

## 6. Overly Broad Permissions

| Permission | Issue | Recommendation |
|-----------|-------|---------------|
| `governance:read` (frontend) | Covers 24 pages with diverse data needs | Replace with granular per-page permissions matching backend |
| `governance:compliance:read` | Covers compliance, executive, scoped context, maturity, benchmarking | Acceptable — these are related read-model views |
| `platform:admin:read` | Used for DelegatedAdmin POST (write action!) | Fix: use `governance:admin:write` for POST |

---

## 7. Sensitive Actions Review

| # | Action | Current Permission | Risk Level | Recommendation |
|---|--------|-------------------|-----------|---------------|
| 1 | Publish governance pack | `governance:packs:write` | **HIGH** | Consider separate `governance:packs:publish` permission |
| 2 | Approve/reject waiver | `governance:waivers:write` | **HIGH** | Consider separate `governance:waivers:approve` permission |
| 3 | Create delegated admin | `platform:admin:read` ⚠️ | **CRITICAL** | Must use `governance:admin:write` |
| 4 | Run compliance checks | `governance:compliance:write` | **MEDIUM** | Appropriate for the action |
| 5 | Delete governance pack | `governance:packs:write` | **HIGH** | Consider soft-delete only for published packs |
| 6 | Delete team/domain | `governance:teams:write` / `governance:domains:write` | **MEDIUM** | Soft delete already implemented |
| 7 | Create/manage policies | `governance:policies:write` | **HIGH** | Permission exists but no endpoints yet |
| 8 | Pack status transitions | `governance:packs:write` | **HIGH** | Status transitions should be validated in entity |

---

## 8. Tenant Scope

| Aspect | Status | Notes |
|--------|--------|-------|
| Tenant isolation mechanism | ✅ `TenantRlsInterceptor` | Applied via EF Core interceptor |
| All entities have TenantId | ✅ Present | Every Governance entity includes `TenantId` |
| RLS on all queries | ✅ Present | Interceptor applies tenant filter to all queries |
| Cross-tenant access prevention | ✅ Present | TenantId cannot be overridden by user input |
| Tenant in audit fields | ✅ Present | CreatedBy/UpdatedBy include tenant context |

---

## 9. Environment Scope

| Aspect | Status | Notes |
|--------|--------|-------|
| Environment-specific data | ❌ Not applicable | Governance data is organizational, not environment-specific |
| Environment filtering | ❌ Not applicable | Governance packs/policies apply across environments |

Governance is organizational-scoped, not environment-scoped. No environment filtering is needed.

---

## 10. Audit of Critical Actions

| # | Action | Audit Trail | Notes |
|---|--------|-------------|-------|
| 1 | Pack created/updated/deleted | ✅ `CreatedAt/UpdatedAt/IsDeleted` timestamps | Basic audit via entity fields |
| 2 | Pack published/status changed | ⚠️ Partial | Status field updated but no dedicated audit event |
| 3 | Waiver approved/rejected | ⚠️ Partial | Status updated but no approval audit trail with approver details |
| 4 | Delegated admin created | ✅ `CreatedAt/CreatedBy` | Basic audit |
| 5 | Team/domain created/updated | ✅ `CreatedAt/UpdatedAt` | Basic audit |
| 6 | Compliance check run | ⚠️ Unknown | Depends on handler implementation |
| 7 | Policy changes | ❌ Not applicable | No write endpoints exist yet |

**Gap:** Critical governance actions (pack publish, waiver approval, delegation creation) need dedicated audit events beyond basic entity timestamps.

---

## 11. Security Corrections Backlog

### HIGH Priority

| # | Correction | Type | Risk | Rationale |
|---|-----------|------|------|-----------|
| 1 | Align frontend route permissions with backend granular permissions | Permission mismatch | **HIGH** | Users see pages they cannot access, leading to 403 errors and poor UX |
| 2 | Fix DelegatedAdmin POST permission from `platform:admin:read` to `governance:admin:write` | Wrong permission | **CRITICAL** | A read permission must never authorize a write/create operation |
| 3 | Fix OnboardingEndpointModule permission from `governance:teams:read` to appropriate scope | Wrong permission | **MEDIUM** | Onboarding is not a team-specific operation |

### MEDIUM Priority

| # | Correction | Type | Risk | Rationale |
|---|-----------|------|------|-----------|
| 4 | Add persona-based filtering to executive views | Data exposure | **MEDIUM** | Executives should see org-level data; team leads should see team-scoped data |
| 5 | Add frontend write-action permission guards | Missing guards | **MEDIUM** | Buttons for create/update/delete should check write permissions before rendering |
| 6 | Add sidebar item permission filtering | UX/Security | **MEDIUM** | Sidebar should hide items the user has no permission to view |
| 7 | Add dedicated audit events for pack publish and waiver approval | Audit gap | **MEDIUM** | Critical governance actions need full audit trail |

### LOW Priority

| # | Correction | Type | Risk | Rationale |
|---|-----------|------|------|-----------|
| 8 | Consider separate `governance:waivers:approve` permission | Permission granularity | **LOW** | Waiver approval is a high-value action — may warrant its own permission |
| 9 | Consider separate `governance:packs:publish` permission | Permission granularity | **LOW** | Pack publishing is a lifecycle milestone — may warrant its own permission |
| 10 | Add permission hierarchy support (`governance:*` for admin) | Admin UX | **LOW** | Currently no wildcard/super-permission for governance admins |

---

## Summary

The Governance module has a well-designed **backend permission model** with 12+ granular permissions covering all endpoint modules. However, the **frontend permission model is critically misaligned**, using a single generic `governance:read` permission for 24 diverse routes.

### Key Findings

| Area | Backend | Frontend |
|------|---------|----------|
| Permission granularity | ✅ 12+ specific permissions | ❌ 1 generic permission |
| Write permission enforcement | ✅ Per endpoint module | ❌ No client-side write checks |
| Tenant isolation | ✅ RLS interceptor | ✅ Token-based |
| Sensitive action protection | ⚠️ Mostly correct (except DelegatedAdmin) | ❌ No action-level guards |
| Audit trail | ⚠️ Basic entity timestamps only | N/A |

### Critical Fix Required

**DelegatedAdmin POST using `platform:admin:read`** is a security defect — a read permission authorizing a write operation. This must be fixed immediately regardless of other priorities.
