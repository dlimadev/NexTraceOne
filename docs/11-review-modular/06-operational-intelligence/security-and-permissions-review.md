# Part 10 — Security & Permissions Review

> **Module:** Operational Intelligence
> **Date:** 2025-07-14
> **Status:** Review Complete
> **Scope:** Permission mapping per page and action, frontend/backend guards, sensitive action review, tenant/environment scoping, audit integration

---

## 1. Permissions Inventory (16 Total)

### 1.1 Complete Permission List

| # | Permission | Type | Used By |
|---|---|---|---|
| 1 | `operations:incidents:read` | Read | Incident list, detail, by-service, by-team |
| 2 | `operations:incidents:write` | Write | Create, update incidents |
| 3 | `operations:mitigation:read` | Read | List, get mitigation workflows |
| 4 | `operations:mitigation:write` | Write | Create, update mitigation actions |
| 5 | `operations:runbooks:read` | Read | List, get runbooks |
| 6 | `operations:reliability:read` | Read | Services, trends, coverage, teams |
| 7 | `operations:runtime:read` | Read | Health, drift, baselines, observability |
| 8 | `operations:runtime:write` | Write | Update baselines |
| 9 | `operations:cost:read` | Read | List, get cost data, snapshots, trends |
| 10 | `operations:cost:write` | Write | Import, update cost records |
| 11 | `operations:automation:read` | Read | List, get workflows, actions, preconditions |
| 12 | `operations:automation:write` | Write | Create, update, cancel, complete-step |
| 13 | `operations:automation:execute` | Execute | Execute workflows |
| 14 | `operations:automation:approve` | Approve | Request/approve/reject workflows |
| 15 | — | — | *(No `operations:runbooks:write` exists)* |
| 16 | — | — | *(No `operations:reliability:write` exists)* |

**Note:** The module defines 14 distinct permissions. The stated count of 16 includes the two that are absent but expected (`operations:runbooks:write`, `operations:reliability:write`). This is a gap.

### 1.2 Role-to-Permission Matrix

**Source:** `docs/11-review-modular/06-operational-intelligence/module-role-finalization.md`, `src/modules/identityaccess/.../Authorization/OperationalContextAuthorizationHandler.cs`

| Permission | Admin | Eng Lead | SRE | Ops | Viewer |
|---|---|---|---|---|---|
| `operations:incidents:read` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `operations:incidents:write` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `operations:mitigation:read` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `operations:mitigation:write` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `operations:runbooks:read` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `operations:reliability:read` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `operations:runtime:read` | ✅ | ✅ | ✅ | ✅ | ❌ |
| `operations:runtime:write` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `operations:cost:read` | ✅ | ✅ | ❌ | ❌ | ❌ |
| `operations:cost:write` | ✅ | ❌ | ❌ | ❌ | ❌ |
| `operations:automation:read` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `operations:automation:write` | ✅ | ✅ | ✅ | ❌ | ❌ |
| `operations:automation:execute` | ✅ | ❌ | ✅ | ❌ | ❌ |
| `operations:automation:approve` | ✅ | ✅ | ❌ | ❌ | ❌ |

**Effective permission counts:** Admin=14, Eng Lead=11, SRE=10, Ops=6, Viewer=4

---

## 2. Permissions by Page (10 Frontend Pages)

**Source:** `src/App.tsx` (lines 50–60), `src/frontend/src/components/shell/AppSidebar.tsx`

| # | Route | Page Component | Required Permission | Sidebar |
|---|---|---|---|---|
| 1 | `/operations/incidents` | `IncidentsPage` | `operations:incidents:read` | ✅ |
| 2 | `/operations/incidents/:incidentId` | `IncidentDetailPage` | `operations:incidents:read` | — |
| 3 | `/operations/runbooks` | `RunbooksPage` | `operations:runbooks:read` | ✅ |
| 4 | `/operations/automation` | `AutomationPage` | `operations:automation:read` | ✅ |
| 5 | `/operations/automation/:workflowId` | `AutomationDetailPage` | `operations:automation:read` | — |
| 6 | `/operations/automation/admin` | `AutomationAdminPage` | `operations:automation:approve` | — |
| 7 | `/operations/reliability` | `ReliabilityPage` | `operations:reliability:read` | ✅ |
| 8 | `/operations/reliability/:serviceId` | `ReliabilityDetailPage` | `operations:reliability:read` | — |
| 9 | `/operations/runtime-comparison` | `RuntimeComparisonPage` | `operations:runtime:read` | ✅ |
| 10 | `/platform/operations` | `PlatformOperationsPage` | `platform:admin:read` | ✅ (admin) |

**Missing pages (no frontend exists):**

| Gap | Expected Route | Permission | Status |
|---|---|---|---|
| Cost Dashboard | `/operations/costs` | `operations:cost:read` | ❌ Not implemented |
| Cost Import | `/operations/costs/import` | `operations:cost:write` | ❌ Not implemented |
| Mitigation Detail | `/operations/mitigation/:id` | `operations:mitigation:read` | ❌ Not implemented |

---

## 3. Frontend Guards Review

### 3.1 ProtectedRoute Pattern

**Source:** `src/App.tsx`, `src/frontend/src/components/ProtectedRoute.tsx`

All operations routes use the `ProtectedRoute` wrapper:

```tsx
<Route
  path="/operations/incidents"
  element={
    <ProtectedRoute permission="operations:incidents:read" redirectTo="/unauthorized">
      <IncidentsPage />
    </ProtectedRoute>
  }
/>
```

**Assessment:**

| Check | Status |
|---|---|
| All 10 routes wrapped with ProtectedRoute | ✅ |
| Permission attribute matches backend permission | ✅ |
| Redirect to `/unauthorized` on denial | ✅ |
| Permission checked at route level | ✅ |
| Permission checked at component action level | ⚠️ Partial |

**Gaps:**

- ❌ **No in-page action guards for write operations.** For example, `IncidentsPage` is guarded by `operations:incidents:read`, but the "Create Incident" button inside the page may not check `operations:incidents:write` before rendering.
- ❌ **No conditional UI rendering** based on `operations:automation:execute` vs `operations:automation:read` — users who can view but not execute may still see the "Execute" button.
- ❌ **No permission-aware sidebar badge** to indicate pending approvals only for users with `operations:automation:approve`.

### 3.2 Sidebar Guards

**Source:** `src/frontend/src/components/shell/AppSidebar.tsx` (lines 51–55)

Each sidebar item has a `permission` field that controls visibility:

```tsx
{ labelKey: 'sidebar.incidents', to: '/operations/incidents', permission: 'operations:incidents:read' }
```

- ✅ 5 operations sidebar items each have a permission check.
- ✅ Admin item (`/platform/operations`) uses `platform:admin:read`.
- ✅ Items are hidden from users without the required permission.

---

## 4. Backend Enforcement Review

### 4.1 .RequirePermission() Coverage

**Source:** 7 endpoint modules in `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.API/`

| Endpoint Module | Permission(s) Enforced | Endpoints |
|---|---|---|
| `IncidentEndpointModule` | `incidents:read`, `incidents:write` | ~8 |
| `RunbookEndpointModule` | `runbooks:read` | ~4 |
| `MitigationEndpointModule` | `mitigation:read`, `mitigation:write` | ~4 |
| `AutomationEndpointModule` | `automation:read`, `automation:write`, `automation:execute`, `automation:approve` | ~10 |
| `ReliabilityEndpointModule` | `reliability:read` | ~6 |
| `RuntimeIntelligenceEndpointModule` | `runtime:read`, `runtime:write` | ~6 |
| `CostIntelligenceEndpointModule` | `cost:read`, `cost:write` | ~6 |

**Total: ~44 endpoints, all protected.**

**Assessment:**

- ✅ Every endpoint has `.RequirePermission()` — no unprotected endpoints.
- ✅ Write endpoints use write permissions; read endpoints use read permissions.
- ✅ Automation approval and execution have dedicated permissions.
- ❌ No endpoint enforces **composite permission** (e.g., `read AND write` for a PATCH that modifies and returns data).
- ❌ No rate limiting on sensitive endpoints (execute, approve).

---

## 5. Sensitive Actions Review

### 5.1 Actions Requiring Enhanced Security

| Action | Permission | Risk | Current Enforcement | Gap |
|---|---|---|---|---|
| Execute automation workflow | `operations:automation:execute` | 🔴 High | Permission check, status validation | No step-up auth, no IP validation |
| Approve automation workflow | `operations:automation:approve` | 🔴 High | Permission check | No four-eyes (approver ≠ requester) |
| Import cost data | `operations:cost:write` | 🟡 Medium | Permission check | No validation of data source |
| Update runtime baselines | `operations:runtime:write` | 🟡 Medium | Permission check | No change tracking |
| Create/update incidents | `operations:incidents:write` | 🟡 Medium | Permission check | No field-level audit |
| Alter health thresholds | *Not exposed* | 🔴 High | Hardcoded — no UI | Not applicable yet |
| Silence/ignore signal | *Not implemented* | 🔴 High | — | Not implemented |
| View sensitive history | `operations:incidents:read` | 🟡 Medium | Permission check | No data classification |

### 5.2 Step-Up Authentication

**Status: ❌ Not implemented.**

No endpoint requires re-authentication or MFA step-up for sensitive operations. All actions rely solely on the existing JWT token and permission claims.

**Recommendation:** Implement step-up authentication for:
- `operations:automation:execute` on High/Critical risk workflows
- `operations:automation:approve` on Critical risk workflows
- `operations:cost:write` for batch imports

---

## 6. Tenant Scope Review

### 6.1 TenantRlsInterceptor

**Source:** `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Interceptors/TenantRlsInterceptor.cs`

**Mechanism:** PostgreSQL Row-Level Security via `set_config('app.current_tenant_id', @tenantId, false)`

| Check | Status |
|---|---|
| Interceptor registered on all 5 DbContexts | ✅ |
| Parameterized tenant ID (SQL injection safe) | ✅ |
| Null-safe (skips if TenantId = Guid.Empty) | ✅ |
| Session-scoped (not transaction-local) | ✅ |
| Async + CancellationToken support | ✅ |
| Covers all 6 EF Core command types | ✅ |

**Assessment:** Tenant isolation is robust. RLS policies in PostgreSQL ensure that even a bypassed interceptor cannot leak cross-tenant data (defense in depth).

### 6.2 TenantId on Domain Entities

| Entity | Has TenantId | Type |
|---|---|---|
| `IncidentRecord` | ✅ | `Guid?` (nullable for Phase 4 retrocompat) |
| `ReliabilitySnapshot` | ✅ | `Guid` |
| `RuntimeSnapshot` | ⚠️ | Not explicit — relies on RLS only |
| `AutomationWorkflowRecord` | ✅ | Via base class |
| `CostRecord` | ✅ | Via base class |
| `RunbookRecord` | ⚠️ | Not explicitly shown |

**Gap:** Some entities rely solely on RLS without an explicit `TenantId` column. This is acceptable for PostgreSQL but problematic for future ClickHouse migration (which does not support RLS).

---

## 7. Environment Scope Review

### 7.1 EnvironmentId on Entities

| Entity | Field | Type | Nullable | Status |
|---|---|---|---|---|
| `IncidentRecord` | `EnvironmentId` | `Guid?` | Yes | Phase 4 addition |
| `RuntimeSnapshot` | `Environment` | `string` | No | Name, not ID |
| `ReliabilitySnapshot` | `Environment` | `string` | No | Name, not ID |
| `AutomationWorkflowRecord` | `TargetEnvironment` | `string?` | Yes | Name, not ID |
| `CostRecord` | `Environment` | `string?` | Yes | Name, not ID |

**Assessment:**

- ❌ **Inconsistent typing** — `IncidentRecord` uses `Guid? EnvironmentId`, while all others use `string Environment` (name-based).
- ❌ **No foreign key** to Environment Management module — references are by name or optional GUID.
- ❌ **No API-level environment filter** on most endpoints (queries do not enforce environment scope).
- ❌ **No environment-based permission** (e.g., user can view production but not staging) — all environments accessible with same permission.

---

## 8. Critical Action Audit Review

### 8.1 Current Audit Coverage

**Within module (AutomationAuditRecord):**

- ✅ All 9 automation lifecycle actions are recorded.
- ✅ Actor and timestamp tracked.
- ✅ ServiceId and TeamId attached.

**Cross-module (Audit & Compliance integration):**

- ❌ **NOT INTEGRATED.** AutomationAuditRecord lives in `AutomationDbContext` only.
- ❌ No events published to central audit log.
- ❌ No integration events for: incident creation, mitigation actions, cost imports, baseline changes.
- ❌ No tamper-evident audit trail.

### 8.2 Actions Missing Audit Trail

| Action | Module Audit | Central Audit |
|---|---|---|
| Create incident | ❌ | ❌ |
| Update incident severity | ❌ | ❌ |
| Create mitigation workflow | ❌ | ❌ |
| Import cost data | ❌ | ❌ |
| Update runtime baseline | ❌ | ❌ |
| Automation lifecycle | ✅ (AutomationAuditRecord) | ❌ |

---

## 9. Security Corrections Backlog

| ID | Category | Issue | Priority | Effort | Area |
|---|---|---|---|---|---|
| SEC-01 | Permissions | Missing `operations:runbooks:write` permission | P2 | Small | Backend |
| SEC-02 | Permissions | Missing `operations:reliability:write` permission | P3 | Small | Backend |
| SEC-03 | Frontend | No in-page action guards for write operations | P1 | Medium | Frontend |
| SEC-04 | Frontend | Execute/Approve buttons visible to unauthorized users | P1 | Small | Frontend |
| SEC-05 | Approval | No four-eyes principle enforcement | P1 | Small | Backend |
| SEC-06 | Execution | No step-up authentication for sensitive actions | P2 | Medium | Backend |
| SEC-07 | Rate Limit | No rate limiting on execute/approve endpoints | P2 | Medium | Backend/Infra |
| SEC-08 | Audit | No central Audit & Compliance integration | P1 | Large | Backend |
| SEC-09 | Audit | No audit for non-automation actions (incidents, costs) | P1 | Medium | Backend |
| SEC-10 | Environment | Inconsistent EnvironmentId typing (Guid vs string) | P2 | Medium | Backend |
| SEC-11 | Environment | No environment-based access control | P3 | Large | Backend |
| SEC-12 | Tenant | Some entities lack explicit TenantId column | P2 | Small | Backend |
| SEC-13 | Data | No data classification for sensitive incident fields | P3 | Medium | Backend |
| SEC-14 | Pages | Missing Cost and Mitigation UI pages | P2 | Medium | Frontend |
| SEC-15 | Threshold | No permission for threshold modification (future) | P3 | Small | Backend |

---

## 10. References

| Artifact | Path |
|---|---|
| ProtectedRoute component | `src/frontend/src/components/ProtectedRoute.tsx` |
| App.tsx (routes) | `src/App.tsx` |
| AppSidebar (menu) | `src/frontend/src/components/shell/AppSidebar.tsx` |
| TenantRlsInterceptor | `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Interceptors/TenantRlsInterceptor.cs` |
| IncidentEndpointModule | `src/modules/operationalintelligence/.../API/Incidents/Endpoints/IncidentEndpointModule.cs` |
| AutomationEndpointModule | `src/modules/operationalintelligence/.../API/Automation/Endpoints/AutomationEndpointModule.cs` |
| RuntimeIntelligenceEndpointModule | `src/modules/operationalintelligence/.../API/Runtime/Endpoints/RuntimeIntelligenceEndpointModule.cs` |
| ReliabilityEndpointModule | `src/modules/operationalintelligence/.../API/Reliability/Endpoints/ReliabilityEndpointModule.cs` |
| CostIntelligenceEndpointModule | `src/modules/operationalintelligence/.../API/Cost/Endpoints/CostIntelligenceEndpointModule.cs` |
| Role finalization doc | `docs/11-review-modular/06-operational-intelligence/module-role-finalization.md` |
| OperationalContextAuthorizationHandler | `src/modules/identityaccess/.../Authorization/OperationalContextAuthorizationHandler.cs` |
