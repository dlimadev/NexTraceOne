# Catalog Module — Security and Permissions Review

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Permissions by Page

| Page | Route | Required Permission | Frontend Guard | Backend Enforcement |
|------|-------|-------------------|---------------|-------------------|
| ServiceCatalogListPage | `/services` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| ServiceCatalogPage | `/services/graph` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| ServiceDetailPage | `/services/:serviceId` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| SourceOfTruthExplorerPage | `/source-of-truth` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| ServiceSourceOfTruthPage | `/source-of-truth/service/:serviceId` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| ContractSourceOfTruthPage | `/source-of-truth/contract/:contractId` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| DeveloperPortalPage | `/portal` | `developer-portal:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| GlobalSearchPage | `/search` | `catalog:assets:read` | ProtectedRoute ✅ | Endpoint policy ✅ |
| CatalogContractsConfigurationPage | `/platform/configuration/catalog-contracts` | `platform:admin:read` | ProtectedRoute ✅ | Endpoint policy ✅ |

---

## 2. Permissions by Action

| Action | Permission | Frontend | Backend | Status |
|--------|-----------|----------|---------|--------|
| List services | `catalog:assets:read` | ✅ | ✅ | ✅ |
| View service detail | `catalog:assets:read` | ✅ | ✅ | ✅ |
| View service graph | `catalog:assets:read` | ✅ | ✅ | ✅ |
| View API detail | `catalog:assets:read` | ✅ | ✅ | ✅ |
| Search assets | `catalog:assets:read` | ✅ | ✅ | ✅ |
| View impact propagation | `catalog:assets:read` | ✅ | ✅ | ✅ |
| List graph snapshots | `catalog:assets:read` | ✅ | ✅ | ✅ |
| Compare snapshots | `catalog:assets:read` | ✅ | ✅ | ✅ |
| View node health | `catalog:assets:read` | ✅ | ✅ | ✅ |
| List saved views | `catalog:assets:read` | ✅ | ✅ | ✅ |
| Register service | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Register API | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Update API | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Add consumer relationship | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Register consumer | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Create snapshot | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Save graph view | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Record node health | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Configure discovery source | `catalog:assets:write` | ✅ | ✅ | ✅ |
| Portal: search catalog | `developer-portal:read` | ✅ | ✅ | ✅ |
| Portal: my APIs | `developer-portal:read` | ✅ | ✅ | ✅ |
| Portal: consuming APIs | `developer-portal:read` | ✅ | ✅ | ✅ |
| Portal: asset detail | `developer-portal:read` | ✅ | ✅ | ✅ |
| Portal: list subscriptions | `developer-portal:read` | ✅ | ✅ | ✅ |
| Portal: create subscription | `developer-portal:write` | ✅ | ✅ | ✅ |
| Portal: delete subscription | `developer-portal:write` | ✅ | ✅ | ✅ |
| Portal: generate code | `developer-portal:write` | ✅ | ✅ | ✅ |
| Portal: record analytics | `developer-portal:write` | ✅ | ✅ | ✅ |
| SoT: service source | `catalog:assets:read` | ✅ | ✅ | ✅ |
| SoT: contract source | `catalog:assets:read` | ✅ | ✅ | ✅ |
| SoT: search | `catalog:assets:read` | ✅ | ✅ | ✅ |
| SoT: coverage | `catalog:assets:read` | ✅ | ✅ | ✅ |
| SoT: global search | `catalog:assets:read` | ✅ | ✅ | ✅ |

---

## 3. Frontend Guards

| Guard Type | Implementation | Status |
|-----------|---------------|--------|
| Route-level | `<ProtectedRoute permission="..." redirectTo="/unauthorized">` | ✅ All 9 routes |
| Sidebar visibility | `permission` field in menu item config | ✅ All sidebar items |
| Button-level write guards | Hooks check permission before mutation | ✅ |
| Persona-based menu ordering | Menu items ordered by persona context | ✅ |

---

## 4. Backend Enforcement

| Layer | Mechanism | Status |
|-------|----------|--------|
| Authentication | JWT / API Key / OIDC | ✅ Global middleware |
| Authorization | `RequirePermission("catalog:assets:read/write")` per endpoint group | ✅ |
| Multi-tenancy | PostgreSQL RLS via `TenantRlsInterceptor` | ✅ |
| Rate limiting | Global policy (100 req/60s) | ✅ Appropriate for catalog |
| Input validation | FluentValidation on all commands | ✅ |
| Audit trail | `AuditInterceptor` (CreatedAt/By, UpdatedAt/By) | ✅ |
| Encryption | AES-256-GCM available via `EncryptionInterceptor` | ✅ |
| Soft delete | Global query filter | ✅ |

---

## 5. Sensitive Actions Review

| Action | Sensitivity | Protection | Status |
|--------|-----------|-----------|--------|
| Register service (new asset) | MEDIUM | Write permission + validation | ✅ |
| Register API (new asset) | MEDIUM | Write permission + validation | ✅ |
| Update API metadata | MEDIUM | Write permission + audit trail | ✅ |
| Configure discovery source | HIGH | Write permission + validated config | ✅ |
| Delete service/API (soft) | HIGH | Write permission + soft delete | ⚠️ No explicit endpoint |
| Lifecycle transition | HIGH | Not yet implemented | ❌ Missing handler |
| Bulk import | HIGH | Not yet implemented | ❌ Missing handler |
| Create snapshot (full graph) | LOW | Write permission | ✅ |
| Record health data | LOW | Write permission | ✅ |

---

## 6. Tenant Scope

| Aspect | Implementation | Status |
|--------|---------------|--------|
| Data isolation | PostgreSQL RLS via `tenant_id` | ✅ |
| Cross-tenant access | Prevented by RLS policy | ✅ |
| Service assets | Tenant-scoped | ✅ |
| API assets | Tenant-scoped | ✅ |
| Consumer relationships | Tenant-scoped | ✅ |
| Graph snapshots | Tenant-scoped | ✅ |
| Saved views | Tenant-scoped | ✅ |
| Health records | Tenant-scoped | ✅ |
| Portal entities | Tenant-scoped | ✅ |
| Linked references | Tenant-scoped | ✅ |

---

## 7. Environment Scope

Some Catalog entities ARE environment-scoped:

| Entity | Environment-Scoped | Rationale |
|--------|-------------------|-----------|
| ServiceAsset | YES (optional) | Services can exist per environment |
| ApiAsset | YES (optional) | APIs can exist per environment |
| NodeHealthRecord | YES (implicit via node) | Health is environment-specific |
| ConsumerRelationship | YES (implicit) | Consumption can vary by environment |
| GraphSnapshot | NO | Captures full graph across environments |
| SavedGraphView | NO | User preferences, not environment-specific |
| DiscoverySource | NO | Configuration, not environment-specific |
| Portal entities | NO | Portal aggregates across environments |
| LinkedReference | NO | External links are not environment-specific |

---

## 8. Audit of Critical Actions

| Action | Audit Record | Status |
|--------|-------------|--------|
| Register service | CreatedAt/By (interceptor) | ✅ |
| Register API | CreatedAt/By (interceptor) | ✅ |
| Update API | UpdatedAt/By (interceptor) | ✅ |
| Add consumer | CreatedAt/By (interceptor) | ✅ |
| Create snapshot | CreatedAt/By (interceptor) | ✅ |
| Record health | CreatedAt/By (interceptor) | ✅ |
| Lifecycle transition | Not implemented | ❌ Should have domain event + audit |
| Discovery source config | CreatedAt/By (interceptor) | ✅ |
| Soft delete | IsDeleted flag | ✅ |

---

## 9. Security Gaps

| # | Gap | Severity | Recommendation |
|---|-----|----------|---------------|
| SEC-01 | No concurrency control (RowVersion) | MEDIUM | Concurrent edits can silently overwrite — add xmin to ServiceAsset, ApiAsset |
| SEC-02 | No ownership validation on asset updates | MEDIUM | Verify only asset owner team can modify asset metadata |
| SEC-03 | Missing lifecycle transition handler means no audit trail for state changes | MEDIUM | Implement `TransitionAssetLifecycle` with full audit |
| SEC-04 | No explicit soft-delete endpoint | LOW | Soft delete works but is not discoverable via API |
| SEC-05 | No admin-only permission for discovery source configuration | LOW | Consider `catalog:admin` for discovery source management |
| SEC-06 | Portal analytics endpoint allows any user to record events | LOW | Consider rate limiting or validation that events match user context |

---

## 10. Security Posture Summary

| Aspect | Status |
|--------|--------|
| Authentication | ✅ STRONG |
| Authorization | ✅ STRONG (3 permission groups: catalog:assets, developer-portal, platform:admin) |
| Multi-tenancy | ✅ STRONG (RLS) |
| Audit trail | ✅ STRONG (auto-audit on all entities) |
| Input validation | ✅ STRONG (FluentValidation on all commands) |
| Rate limiting | ✅ ADEQUATE |
| Concurrency control | ❌ MISSING |
| Encryption | ✅ AVAILABLE |
| Environment isolation | ✅ ADEQUATE (optional EnvironmentId) |

**Overall:** The Catalog module has solid security foundations. The main gaps are concurrency control (RowVersion) and the missing lifecycle transition handler (which means state changes are not explicitly audited). Once xmin is added and the lifecycle handler is implemented, the security posture will be strong.
