# Identity Access — Current State

**Maturity:** READY (100% real)
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §IdentityAccess`, `docs/audit-forensic-2026-03/frontend-state-report.md §IdentityAccess`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| IdentityDbContext | Confirmed (1+ migrations) | READY |

Table prefix: `iam_`
Note: Only DbContext with active outbox processing.

---

## Features (35 total, 100% real)

| Area | Features | Status |
|---|---|---|
| Auth & RBAC | JWT auth, multi-tenancy with RLS, role-permission enforcement | READY |
| Sessions / Cookies | Cookie session, refresh token | READY |
| JIT Access | Just-In-Time privileged access, auto-expiry (60s periodic, batch 100) | READY |
| Break Glass | Emergency access with expiry and audit trail | READY |
| Access Reviews | Periodic review workflows | READY |
| Delegations | Delegated access with expiry and revocation | READY |
| Users / Roles / Tenants | Full CRUD, tenant isolation | READY |
| Security Events | Audit trail for all security events via `ISecurityEventTracker` | READY |
| Environments | Environment CRUD, `IsProductionLike` logic (partial) | PARTIAL |

---

## Frontend Pages (9 pages — FULLY CONNECTED)

| Page | Status |
|---|---|
| LoginPage | READY |
| UserManagementPage | READY |
| RolePermissionsPage | READY |
| TenantSelectionPage | READY |
| AccessReviewPage | READY |
| BreakGlassPage | READY |
| JitAccessPage | READY |
| DelegatedAdminPage | READY |
| DelegationPage | READY |

---

## Key Gaps

- `IsProductionLike` environment logic is partial
- Outbox: active only for IdentityDbContext — 23 other DbContexts have outbox tables but no active processing
- Self-action prevention enforced at domain + handler layer (not a gap — by design)

---

## Background Workers

- `IdentityExpirationJob`: 60s periodic, batch 100 — handles BreakGlass, JIT, Delegation expiry
- `SecurityEventAuditBehavior`: routes security events to central Audit module

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/database-state-report.md`*
