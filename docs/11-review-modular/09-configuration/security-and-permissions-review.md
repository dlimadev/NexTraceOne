# Configuration Module — Security and Permissions Review

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. Permissions by Page

| Page | Route | Required Permission | Enforcement |
|------|-------|-------------------|-------------|
| ConfigurationAdminPage | `/platform/configuration` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| AdvancedConfigurationConsolePage | `/platform/configuration/advanced` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| NotificationConfigurationPage | `/platform/configuration/notifications` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| WorkflowConfigurationPage | `/platform/configuration/workflows` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| GovernanceConfigurationPage | `/platform/configuration/governance` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| CatalogContractsConfigurationPage | `/platform/configuration/catalog-contracts` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| OperationsFinOpsConfigurationPage | `/platform/configuration/operations-finops` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |
| AiIntegrationsConfigurationPage | `/platform/configuration/ai-integrations` | `platform:admin:read` | `<ProtectedRoute>` in App.tsx |

---

## 2. Permissions by Action

| Action | Backend Permission | Frontend Guard | Status |
|--------|-------------------|---------------|--------|
| List definitions | `configuration:read` | `platform:admin:read` | ✅ |
| List entries by scope | `configuration:read` | `platform:admin:read` | ✅ |
| Resolve effective settings | `configuration:read` | `platform:admin:read` | ✅ |
| View audit history | `configuration:read` | `platform:admin:read` | ✅ |
| Set configuration value | `configuration:write` | `platform:admin:read` | ⚠️ Gap |
| Remove override | `configuration:write` | `platform:admin:read` | ⚠️ Gap |
| Toggle configuration | `configuration:write` | `platform:admin:read` | ⚠️ Gap |

**Gap SEC-01:** Frontend uses `platform:admin:read` for all routes (including write operations), but backend requires `configuration:write` for mutations. The frontend relies on the backend to reject unauthorized writes rather than hiding write buttons for read-only users. This is acceptable as defense-in-depth but could improve UX.

---

## 3. Frontend Guards

| Guard | Implementation | Status |
|-------|---------------|--------|
| Route-level protection | `<ProtectedRoute permission="platform:admin:read">` | ✅ |
| Sidebar visibility | `permission: 'platform:admin:read'` on menu item | ✅ |
| Button-level write guard | None — edit/toggle/remove buttons shown to all authenticated admins | ⚠️ See SEC-01 |

---

## 4. Backend Enforcement

| Layer | Mechanism | Status |
|-------|----------|--------|
| Authentication | JWT / API Key / OIDC via global middleware | ✅ |
| Authorization | `RequirePermission("configuration:read/write")` per endpoint | ✅ |
| Multi-tenancy | PostgreSQL RLS via `TenantRlsInterceptor` | ✅ |
| Rate limiting | Global rate limiting (100 req/60s) | ✅ |
| Input validation | FluentValidation on commands | ✅ |
| Encryption | AES-256-GCM for sensitive values | ✅ |
| Audit trail | Every write creates ConfigurationAuditEntry | ✅ |

---

## 5. Sensitive Actions

| Action | Sensitivity | Protection | Status |
|--------|-----------|-----------|--------|
| Set sensitive value | HIGH | Encrypted at rest (AES-256-GCM), masked in responses | ✅ |
| View sensitive value | HIGH | Always masked (`••••••••`) in API responses and UI | ✅ |
| Export configuration | HIGH | Sensitive values masked in JSON export | ✅ |
| Import configuration | HIGH | Should validate and re-encrypt sensitive values | ⚠️ Verify |
| Toggle critical config | MEDIUM | Audit trail with change reason | ✅ |
| Remove override | MEDIUM | Audit trail with change reason | ✅ |
| View audit history | LOW | Read-only, sensitive values masked in audit | ✅ |

---

## 6. Tenant Scope

| Aspect | Implementation | Status |
|--------|---------------|--------|
| Data isolation | PostgreSQL RLS via `tenant_id` on all tables | ✅ |
| Scope resolution | Tenant is a scope level in hierarchy | ✅ |
| Cross-tenant access | Prevented by RLS policy | ✅ |
| System-scope access | Only for `ConfigurationScope.System` entries | ✅ |

---

## 7. Environment Scope

| Aspect | Implementation | Status |
|--------|---------------|--------|
| Environment-scoped values | `Scope = Environment`, `ScopeReferenceId = EnvironmentId` | ✅ |
| Environment isolation | Not at RLS level — via application-level scope filtering | ⚠️ |
| Cross-environment visibility | Admin can see all environment overrides | ✅ (intentional for admin UX) |

**Note:** Environment isolation is application-level (not RLS). An admin with `platform:admin:read` can view configuration across all environments. This is **intentional** for the admin configuration management use case.

---

## 8. Audit of Critical Actions

| Critical Action | Audit Entry Created | Includes User | Includes Timestamp | Includes Reason | Status |
|----------------|-------------------|---------------|-------------------|----------------|--------|
| Create value | ✅ Action="Created" | ✅ ChangedBy | ✅ ChangedAt | ✅ ChangeReason | ✅ |
| Update value | ✅ Action="Updated" | ✅ | ✅ | ✅ | ✅ |
| Activate | ✅ Action="Activated" | ✅ | ✅ | ✅ | ✅ |
| Deactivate | ✅ Action="Deactivated" | ✅ | ✅ | ✅ | ✅ |
| Remove override | ✅ Action="Removed" | ✅ | ✅ | ✅ | ✅ |
| Read definitions | N/A | — | — | — | ✅ |
| Read values | N/A | — | — | — | ✅ |

---

## 9. Security Gaps

| # | Gap | Severity | Description | Recommendation |
|---|-----|----------|-------------|----------------|
| SEC-01 | No write-specific frontend guard | LOW | Frontend uses `platform:admin:read` for all routes; write buttons visible to read-only users | Backend rejects unauthorized writes (defense-in-depth). Consider adding `platform:admin:write` guard on mutation buttons for better UX. |
| SEC-02 | Single permission for all domains | MEDIUM | `configuration:write` grants access to modify ALL configuration domains (AI, security, governance, etc.) | Evaluate domain-specific permissions (e.g., `configuration:ai:write`, `configuration:security:write`) for separation of duties. |
| SEC-03 | Import endpoint validation | MEDIUM | Configuration import (JSON upload) should validate imported values match expected types, do not exceed length limits, and re-encrypt sensitive fields | Verify import validation covers all security aspects. |
| SEC-04 | No change reason requirement | LOW | `changeReason` is optional on all write operations | Consider making `changeReason` mandatory for SensitiveOperational category configurations. |
| SEC-05 | No rate limiting per operation | LOW | Global rate limiting only; no specific throttling on configuration writes | Consider additional rate limiting on write endpoints to prevent configuration flooding. |

---

## 10. Security Posture Summary

| Security Aspect | Status | Notes |
|----------------|--------|-------|
| Authentication | ✅ STRONG | JWT/APIKey/OIDC |
| Authorization | ✅ ADEQUATE | 2 permissions (read/write), could be more granular |
| Multi-tenancy | ✅ STRONG | PostgreSQL RLS |
| Encryption at rest | ✅ STRONG | AES-256-GCM for sensitive values |
| Masking in responses | ✅ STRONG | Sensitive values always masked |
| Audit trail | ✅ STRONG | Every write audited with user/timestamp/reason |
| Input validation | ✅ ADEQUATE | FluentValidation, but value type not enforced |
| Rate limiting | ✅ BASIC | Global only |
| Defense in depth | ✅ GOOD | Backend rejects unauthorized even if frontend doesn't guard |

**Overall Security Assessment:** The Configuration module has a **solid security foundation**. The main improvement areas are permission granularity (SEC-02) and value type validation (related to B-05/B-06 in backend corrections).
