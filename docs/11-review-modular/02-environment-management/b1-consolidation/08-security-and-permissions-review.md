# Environment Management Module — Security and Permissions Review

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Permissions (3 in use)

| # | Permission | Type | Used By | Appropriate? |
|---|-----------|------|---------|-------------|
| 1 | `identity:users:read` | Read | List environments, Get primary production | ❌ **WRONG** — environment read ≠ user read |
| 2 | `identity:users:write` | Write | Create/update environment, Grant access | ❌ **WRONG** — environment write ≠ user write |
| 3 | `promotion:environments:write` | Write | Set primary production | ⚠️ Acceptable but should migrate to env namespace |

### Critical Security Issue

**Using `identity:users:read` for environment listing means that anyone with permission to list users can also list environments, and vice versa.** These are distinct operations with different security implications:

- Listing users reveals PII (names, emails)
- Listing environments reveals infrastructure topology

**Using `identity:users:write` for environment creation means that anyone who can create/edit users can also create/edit environments.** Creating an environment is a platform-level operation, not a user management task.

**Severity: CRITICAL** — Permission model conflates two unrelated domain concerns.

---

## 2. Target Permission Model

### 2.1 Permission Namespace: `env:`

| Permission | Scope | Description | Personas |
|-----------|-------|-------------|----------|
| `env:environments:read` | Read | List and view environment definitions | Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor |
| `env:environments:write` | Write | Create, update, deactivate environments | Tech Lead, Architect, Platform Admin |
| `env:promotion:read` | Read | View promotion paths and primary production | Engineer, Tech Lead, Architect, Product, Platform Admin |
| `env:promotion:write` | Write | Create/modify promotion paths, set primary production | Architect, Platform Admin |
| `env:baseline:read` | Read | View baselines, drift findings, readiness | Engineer, Tech Lead, Architect, Platform Admin |
| `env:baseline:write` | Write | Set baselines, trigger readiness checks | Tech Lead, Platform Admin |
| `env:policies:read` | Read | View environment policies | Engineer, Tech Lead, Architect, Platform Admin |
| `env:policies:write` | Write | Create/modify environment policies | Architect, Platform Admin |

### 2.2 Permissions That Stay in Identity

| Permission | Scope | Description | Notes |
|-----------|-------|-------------|-------|
| `identity:access:write` | Write | Grant/revoke environment access to users | Stays in Identity — user access is an identity concern |

---

## 3. Permissions by Page

| # | Page | Route | Frontend Guard | Required Backend Permission |
|---|------|-------|---------------|---------------------------|
| 1 | EnvironmentsPage | `/environments` | `env:environments:read` | `env:environments:read` |
| 2 | EnvironmentDetailPage | `/environments/:id` | `env:environments:read` | `env:environments:read` |
| 3 | PromotionPathsPage | `/environments/promotion-paths` | `env:promotion:read` | `env:promotion:read` |
| 4 | PromotionPathEditorPage | `/environments/promotion-paths/:id` | `env:promotion:write` | `env:promotion:write` |
| 5 | EnvironmentDriftPage | `/environments/:id/drift` | `env:baseline:read` | `env:baseline:read` |
| 6 | EnvironmentBaselinePage | `/environments/:id/baseline` | `env:baseline:read` | `env:baseline:read` |
| 7 | EnvironmentReadinessPage | `/environments/readiness` | `env:environments:read` | `env:environments:read` |
| 8 | EnvironmentComparisonPage | `/operations/runtime-comparison` | `operations:runtime:read` | `operations:runtime:read` |

**Alignment:** All frontend guards must match backend permissions 1:1. No generic fallback permissions.

---

## 4. Permissions by Action (Backend — Granular)

### 4.1 Read Permissions

| Permission | Endpoints |
|-----------|-----------|
| `env:environments:read` | `GET /environments`, `GET /environments/{id}`, `GET /environments/compare`, `GET /environments/{id}/readiness` |
| `env:promotion:read` | `GET /environments/promotion-paths`, `GET /environments/primary-production` |
| `env:baseline:read` | `GET /environments/{id}/baseline`, `GET /environments/{id}/baseline/history`, `GET /environments/{id}/drift` |
| `env:policies:read` | `GET /environments/{id}/policies` |

### 4.2 Write Permissions

| Permission | Endpoints |
|-----------|-----------|
| `env:environments:write` | `POST /environments`, `PUT /environments/{id}`, `DELETE /environments/{id}` |
| `env:promotion:write` | `POST /environments/promotion-paths`, `PUT /environments/promotion-paths/{id}`, `DELETE /environments/promotion-paths/{id}`, `PUT /environments/{id}/primary-production` |
| `env:baseline:write` | `POST /environments/{id}/baseline` |
| `env:policies:write` | `POST /environments/{id}/policies`, `PUT /environments/{id}/policies/{policyId}`, `DELETE /environments/{id}/policies/{policyId}` |

---

## 5. Environment-Level Restrictions

### 5.1 Production Environment Guards

| Restriction | Description | Implementation |
|------------|-------------|----------------|
| Write restriction | Production environments require elevated permissions | `env:promotion:write` required for any change to production-profile environments |
| Deactivation guard | Cannot deactivate production environment | Business rule in `DeactivateEnvironment` handler |
| Primary production guard | Only Production-profile environments can be designated primary | Validation in `SetPrimaryProductionEnvironment` handler |
| Inactive guard | Cannot designate inactive environment as primary production | Validation in handler |

### 5.2 Profile-Based Access Matrix

| Action | Development | Validation | Staging | Production | DR |
|--------|------------|------------|---------|-----------|-----|
| View | `env:environments:read` | `env:environments:read` | `env:environments:read` | `env:environments:read` | `env:environments:read` |
| Edit | `env:environments:write` | `env:environments:write` | `env:environments:write` | `env:environments:write` + `env:promotion:write` | `env:environments:write` + `env:promotion:write` |
| Deactivate | `env:environments:write` | `env:environments:write` | `env:environments:write` | ❌ Blocked | `env:environments:write` + `env:promotion:write` |
| Set Primary | N/A | N/A | N/A | `env:promotion:write` | N/A |
| Set Baseline | `env:baseline:write` | `env:baseline:write` | `env:baseline:write` | `env:baseline:write` | `env:baseline:write` |

### 5.3 Promotion Path Guards

| Restriction | Description |
|------------|-------------|
| Path must end in Production | Last step of any promotion path must be a Production-profile environment |
| No self-reference | An environment cannot appear twice in the same path |
| Active environments only | Only active environments can be added to a promotion path |
| Cannot remove if active promotions | Cannot remove an environment from a path if there are active promotions in progress |

---

## 6. Tenant-Level Restrictions

| # | Restriction | Implementation | Status |
|---|-----------|---------------|--------|
| 1 | All environment queries filter by TenantId | Query handler WHERE clause + global query filter | ✅ Exists (verify) |
| 2 | Cannot access environments of another tenant | TenantId from JWT claim vs entity TenantId | ✅ Exists (verify) |
| 3 | Cannot create environment for another tenant | TenantId injected from context, not from request body | ✅ Exists (verify) |
| 4 | Slug uniqueness is per-tenant | Unique index on (TenantId, Slug) | ✅ Index exists |
| 5 | Primary production is per-tenant | Filtered index on (TenantId) WHERE IsPrimaryProduction = 1 | ✅ Index exists |
| 6 | Promotion paths are per-tenant | TenantId on PromotionPath entity | ❌ New (implement) |

---

## 7. Data Sensitivity

### 7.1 Environment Data Classification

| Data | Sensitivity | Notes |
|------|-----------|-------|
| Environment names | Low | Generally known (Dev, Staging, Prod) |
| Environment slugs/codes | Low | URL-safe identifiers |
| Region information | Medium | Reveals infrastructure topology |
| Integration bindings | High | Reveals which integrations are active per env |
| Policy configurations | High | Reveals security/governance rules |
| Baseline snapshots | Medium | Point-in-time configuration state |
| Drift findings | Medium | Reveals configuration deviations |

### 7.2 Logging Rules

| Data | Log Level | Allowed in Logs? | Notes |
|------|----------|-----------------|-------|
| Environment ID | Info | ✅ | Always log for correlation |
| Environment name | Info | ✅ | Non-sensitive |
| Policy configuration JSON | — | ❌ | May contain sensitive config |
| Integration binding config | — | ❌ | May contain credentials |
| User who made change | Info | ✅ | Audit trail |
| Full baseline snapshot | — | ❌ | Too verbose, may contain sensitive data |

---

## 8. Migration Strategy for Permissions

### 8.1 Transition Period

During migration from `identity:users:*` to `env:*`:

1. **Register new `env:*` permissions** in the permission system
2. **Map old → new** in a compatibility layer:
   - `identity:users:read` → also grants `env:environments:read` + `env:promotion:read` + `env:baseline:read` + `env:policies:read`
   - `identity:users:write` → also grants `env:environments:write`
   - `promotion:environments:write` → also grants `env:promotion:write`
3. **Update frontend** to use new permission keys
4. **Update backend** to check new permission keys
5. **Notify administrators** to update role-permission assignments
6. **Remove compatibility mapping** after migration period (e.g., 2 sprints)

### 8.2 Role-Permission Mapping (Recommended Default)

| Role | Permissions |
|------|-----------|
| **Engineer** | `env:environments:read`, `env:promotion:read`, `env:baseline:read`, `env:policies:read` |
| **Tech Lead** | All Engineer + `env:environments:write`, `env:baseline:write` |
| **Architect** | All Tech Lead + `env:promotion:write`, `env:policies:write` |
| **Platform Admin** | All Architect (full access) |
| **Product** | `env:environments:read`, `env:promotion:read` |
| **Executive** | `env:environments:read` |
| **Auditor** | `env:environments:read`, `env:promotion:read`, `env:baseline:read`, `env:policies:read` (read-only to everything) |

---

## 9. Audit Requirements

### 9.1 Actions That Must Be Audited

| Action | Audit Event | Severity | Data Captured |
|--------|------------|----------|--------------|
| Create environment | `env.environment.created` | Info | envId, name, profile, criticality, createdBy |
| Update environment | `env.environment.updated` | Info | envId, changedFields, updatedBy |
| Deactivate environment | `env.environment.deactivated` | Warning | envId, deactivatedBy, reason |
| Set primary production | `env.primary_production.changed` | Warning | envId, previousPrimaryId, changedBy |
| Create promotion path | `env.promotion_path.created` | Info | pathId, name, stepCount, createdBy |
| Modify promotion path | `env.promotion_path.updated` | Info | pathId, changedFields, updatedBy |
| Set baseline | `env.baseline.set` | Info | baselineId, envId, capturedBy |
| Drift detected | `env.drift.detected` | Warning | envId, findingCount, severityBreakdown |
| Readiness check | `env.readiness.checked` | Info | envId, score, status, checkedBy |
| Policy created | `env.policy.created` | Info | policyId, envId, policyName, createdBy |
| Policy updated | `env.policy.updated` | Info | policyId, envId, changedFields, updatedBy |
| Policy deleted | `env.policy.deleted` | Warning | policyId, envId, deletedBy |

### 9.2 Failed Access Attempts

All authorization failures must be logged:
- Attempted action, required permission, user ID, tenant ID, environment ID
- Severity: Warning
- Never log request body content in failure logs

---

## 10. Security Gaps Summary

| # | Gap | Severity | Current Impact | Remediation |
|---|-----|----------|---------------|-------------|
| 1 | Permissions conflated with identity:users:* | **CRITICAL** | Users with user-read can see environments; users with user-write can create environments | Migrate to env:* namespace |
| 2 | No production environment write guard | **HIGH** | Any user with env write can modify production | Add profile-based guard for Production/DR environments |
| 3 | No concurrency control | **HIGH** | Concurrent updates can overwrite changes silently | Add xmin concurrency token |
| 4 | Missing audit trail | **MEDIUM** | No audit events emitted for environment changes | Implement domain events → audit integration |
| 5 | Integration binding config may contain secrets | **MEDIUM** | If bindings store credentials, they're in plain text | Encrypt sensitive fields, never log |
| 6 | No rate limiting on environment creation | **LOW** | Potential resource exhaustion via bulk creation | Add per-tenant rate limit |
| 7 | Profile enum mismatch (frontend vs backend) | **LOW** | Frontend accepts profiles that backend rejects | Align EnvironmentProfile enum values |
