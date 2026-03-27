# Contracts Module — Security and Permissions Review

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Permissions by Page

| Page | Route | Required Permission | Frontend Guard | Backend Enforcement |
|------|-------|-------------------|---------------|-------------------|
| ContractCatalogPage | `/contracts` | `contracts:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| CreateServicePage | `/contracts/new` | `contracts:write` | ProtectedRoute ✅ | Endpoint policy ✅ |
| DraftStudioPage | `/contracts/studio/:draftId` | `contracts:write` | ProtectedRoute ✅ | Endpoint policy ✅ |
| ContractWorkspacePage | `/contracts/:id` | `contracts:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| ContractGovernancePage | `/contracts/governance` | `contracts:read` | ProtectedRoute ✅ | N/A (frontend-only view) |
| SpectralRulesetManagerPage | `/contracts/spectral` | `contracts:write` | ProtectedRoute ✅ | ⚠️ No backend endpoints yet |
| CanonicalEntityCatalogPage | `/contracts/canonical` | `contracts:read` | ProtectedRoute ✅ | ⚠️ No backend endpoints yet |
| ContractPortalPage | `/contracts/portal/:id` | `developer-portal:read` | ProtectedRoute ✅ | ⚠️ No dedicated endpoint |

---

## 2. Permissions by Action

| Action | Permission | Frontend | Backend | Status |
|--------|-----------|----------|---------|--------|
| List contracts | `contracts:read` | ✅ | ✅ | ✅ |
| View contract detail | `contracts:read` | ✅ | ✅ | ✅ |
| View version history | `contracts:read` | ✅ | ✅ | ✅ |
| View violations | `contracts:read` | ✅ | ✅ | ✅ |
| Search contracts | `contracts:read` | ✅ | ✅ | ✅ |
| Export contract | `contracts:read` | ✅ | ✅ | ✅ |
| Validate contract | `contracts:read` | ✅ | ✅ | ✅ |
| Generate scorecard | `contracts:read` | ✅ | ✅ | ✅ |
| Import contract | `contracts:write` | ✅ | ✅ | ✅ |
| Create version | `contracts:write` | ✅ | ✅ | ✅ |
| Transition lifecycle | `contracts:write` | ✅ | ✅ | ✅ |
| Deprecate version | `contracts:write` | ✅ | ✅ | ✅ |
| Lock version | `contracts:write` | ✅ | ✅ | ✅ |
| Sign version | `contracts:write` | ✅ | ✅ | ✅ |
| Create draft | `contracts:write` | ✅ | ✅ | ✅ |
| Edit draft | `contracts:write` | ✅ | ✅ | ✅ |
| Submit for review | `contracts:write` | ✅ | ✅ | ✅ |
| Approve draft | `contracts:write` | ✅ | ✅ | ✅ |
| Reject draft | `contracts:write` | ✅ | ✅ | ✅ |
| Publish draft | `contracts:write` | ✅ | ✅ | ✅ |
| AI generation | `contracts:write` | ✅ | ✅ | ✅ |
| Manage spectral rulesets | `contracts:write` | ✅ | ❌ No endpoint | ⚠️ Gap |
| Manage canonical entities | `contracts:read/write` | ✅ | ❌ No endpoint | ⚠️ Gap |

---

## 3. Frontend Guards

| Guard Type | Implementation | Status |
|-----------|---------------|--------|
| Route-level | `<ProtectedRoute permission="..." redirectTo="/unauthorized">` | ✅ All 8 routes |
| Sidebar visibility | `permission` field in menu item config | ✅ All 6 items |
| Button-level write guards | Hooks check permission before mutation | ✅ |

---

## 4. Backend Enforcement

| Layer | Mechanism | Status |
|-------|----------|--------|
| Authentication | JWT / API Key / OIDC | ✅ Global middleware |
| Authorization | `RequirePermission("contracts:read/write")` per endpoint group | ✅ |
| Multi-tenancy | PostgreSQL RLS via `TenantRlsInterceptor` | ✅ |
| Rate limiting | Global policy (100 req/60s) | ⚠️ Import should use `data-intensive` |
| Input validation | FluentValidation on all commands | ✅ |
| Audit trail | `AuditInterceptor` (CreatedAt/By, UpdatedAt/By) | ✅ |
| Encryption | AES-256-GCM available via `EncryptionInterceptor` | ✅ |
| Soft delete | Global query filter | ✅ |

---

## 5. Sensitive Actions Review

| Action | Sensitivity | Protection | Status |
|--------|-----------|-----------|--------|
| Import contract (external content) | HIGH | Rate limited, validated, permissioned | ⚠️ Rate limit too permissive |
| Modify contract (lifecycle change) | HIGH | Permission + audit trail | ✅ |
| Sign contract | HIGH | Permission + signature embedded | ✅ |
| Approve/reject draft | HIGH | Permission + review record | ✅ |
| Publish draft | HIGH | Permission + must be approved first | ✅ |
| AI generation | MEDIUM | Permission + AI audit tracking | ✅ |
| Export contract | LOW | Read permission only | ✅ |
| Delete contract (soft) | HIGH | Write permission + soft delete | ✅ |

---

## 6. Tenant Scope

| Aspect | Implementation | Status |
|--------|---------------|--------|
| Data isolation | PostgreSQL RLS via `tenant_id` | ✅ |
| Cross-tenant access | Prevented by RLS policy | ✅ |
| Contract versions | Tenant-scoped | ✅ |
| Drafts | Tenant-scoped | ✅ |
| Reviews | Tenant-scoped | ✅ |
| Spectral rulesets | Should be tenant-scoped | ⚠️ Not yet in DB |
| Canonical entities | Should be tenant-scoped | ⚠️ Not yet in DB |

---

## 7. Environment Scope

Contracts are NOT environment-scoped. Contracts define the specification of an API regardless of which environment it's deployed to. This is correct — contracts are organizational/governance artifacts, not runtime artifacts.

---

## 8. Audit of Critical Actions

| Action | Audit Record | Status |
|--------|-------------|--------|
| Create contract version | CreatedAt/By (interceptor) | ✅ |
| Update contract version | UpdatedAt/By (interceptor) | ✅ |
| Lifecycle transition | UpdatedAt/By (interceptor) | ⚠️ Should also have domain event |
| Sign version | Signature embedded with SignedBy/SignedAt | ✅ |
| Create draft | CreatedAt/By (interceptor) | ✅ |
| Submit for review | Status change + audit | ✅ |
| Approve/reject | ContractReview record with ReviewedBy/At | ✅ |
| Publish draft | Status change + version created | ✅ |
| Delete (soft) | IsDeleted flag | ✅ |

---

## 9. Security Gaps

| # | Gap | Severity | Recommendation |
|---|-----|----------|---------------|
| SEC-01 | No permission granularity for approve vs reject | LOW | Consider `contracts:approve` permission for separation of duties |
| SEC-02 | Import rate limiting too permissive | LOW | Change to `data-intensive` policy (50 req/60s) |
| SEC-03 | No ownership validation on lifecycle transitions | MEDIUM | Verify only contract owner or authorized reviewer can transition |
| SEC-04 | Spectral ruleset management has no backend security | HIGH (temporary) | Will be resolved when backend CRUD is added (BE-01) |
| SEC-05 | Canonical entity management has no backend security | HIGH (temporary) | Will be resolved when backend CRUD is added (BE-02) |
| SEC-06 | No concurrency control (RowVersion) | MEDIUM | Concurrent edits can silently overwrite — add xmin |

---

## 10. Security Posture Summary

| Aspect | Status |
|--------|--------|
| Authentication | ✅ STRONG |
| Authorization | ✅ ADEQUATE (2 permissions: read/write) |
| Multi-tenancy | ✅ STRONG (RLS) |
| Audit trail | ✅ STRONG |
| Input validation | ✅ STRONG |
| Rate limiting | ⚠️ NEEDS ADJUSTMENT (import endpoint) |
| Concurrency control | ❌ MISSING |
| Encryption | ✅ AVAILABLE |

**Overall:** The Contracts module has solid security foundations. The main gaps are concurrency control (RowVersion), rate limiting adjustment, and the temporary absence of backend endpoints for Spectral/Canonical (which means those features can't be secured server-side yet).
