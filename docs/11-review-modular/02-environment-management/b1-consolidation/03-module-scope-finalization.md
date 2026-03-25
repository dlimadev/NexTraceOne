# Environment Management Module — Module Scope Finalization

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Scope Definition

Environment Management is the **authoritative module** for environment lifecycle, classification, promotion paths, baselines, drift detection, and readiness assessment.

It provides:
- The canonical definition of every environment in the platform
- The classification system (profile, criticality, production-like, primary production)
- The promotion path between environments
- The baseline and drift management for environment governance
- The readiness assessment before promotion

---

## 2. Functional Scope — Existing Capabilities

### 2.1 Fully Implemented (6 features)

| # | Capability | Backend | Frontend | Status |
|---|-----------|---------|----------|--------|
| 1 | List all environments | `GET /api/v1/environments` — `ListEnvironments` query | `EnvironmentsPage.tsx` table listing | ✅ Complete |
| 2 | Create environment | `POST /api/v1/environments` — `CreateEnvironment` command | `EnvironmentsPage.tsx` create dialog | ✅ Complete |
| 3 | Update environment | `PUT /api/v1/environments/{id}` — `UpdateEnvironment` command | `EnvironmentsPage.tsx` edit dialog | ✅ Complete |
| 4 | Get primary production | `GET /api/v1/environments/primary-production` — `GetPrimaryProductionEnvironment` query | `EnvironmentContext.tsx` hook | ✅ Complete |
| 5 | Set primary production | `PUT /api/v1/environments/{id}/primary-production` — `SetPrimaryProductionEnvironment` command | `EnvironmentsPage.tsx` action | ✅ Complete |
| 6 | Grant environment access | `POST /api/v1/environments/{id}/access` — `GrantEnvironmentAccess` command | No dedicated UI (admin action) | ⚠️ Backend only |

### 2.2 Partially Implemented (1 feature)

| # | Capability | Backend | Frontend | Status |
|---|-----------|---------|----------|--------|
| 1 | Environment comparison | Runtime Intelligence API (in Operational Intelligence module) | `EnvironmentComparisonPage.tsx` (in operations feature) | ⚠️ Partial — compares runtime metrics, not environment definitions |

**Detail:** The comparison page (`/operations/runtime-comparison`) compares **runtime behavior** (metrics, latency, error rates) between environments. It does NOT compare **environment definitions** (config values, integration bindings, policies). The runtime comparison belongs to Operational Intelligence. Environment Management needs its own definition-level comparison.

---

## 3. Functional Scope — Missing Capabilities

### 3.1 Core CRUD Gaps

| # | Capability | Priority | Effort | Description |
|---|-----------|----------|--------|-------------|
| 1 | Delete environment (soft-delete) | **HIGH** | Small | Deactivate environment with guards (no active services, no pending promotions). Emit `EnvironmentDeactivated` event. |
| 2 | Get environment by ID (detail) | **HIGH** | Small | Return full environment details including policies, bindings, and relationships. |
| 3 | Environment detail page (frontend) | **HIGH** | Medium | Dedicated page showing full environment info, policies, bindings, promotion path position, baseline status. |

### 3.2 Promotion Path Management

| # | Capability | Priority | Effort | Description |
|---|-----------|----------|--------|-------------|
| 4 | Define promotion path | **HIGH** | Medium | Create ordered sequence of environments (e.g., Dev → QA → Staging → Prod). Per-tenant, supports multiple paths. |
| 5 | List promotion paths | **HIGH** | Small | Query all promotion paths for a tenant. |
| 6 | Update promotion path | **MEDIUM** | Medium | Modify existing promotion path. Validate no circular references. |
| 7 | Delete promotion path | **LOW** | Small | Remove a promotion path (with guards). |
| 8 | Promotion path UI | **HIGH** | Medium | Visual editor for promotion paths with drag-and-drop environment ordering. |

### 3.3 Baseline & Drift Detection

| # | Capability | Priority | Effort | Description |
|---|-----------|----------|--------|-------------|
| 9 | Set environment baseline | **MEDIUM** | Medium | Capture current state of an environment (config values, integration bindings, policies) as a baseline snapshot. |
| 10 | Compare against baseline | **MEDIUM** | Medium | Detect drift: what changed since the baseline was set. |
| 11 | Compare two environments | **MEDIUM** | Medium | Side-by-side comparison of two environment definitions (not runtime — that's Operations). |
| 12 | Drift findings view (frontend) | **MEDIUM** | Medium | Display drift findings with severity, category, and recommended actions. |
| 13 | Baseline history | **LOW** | Small | List previous baselines for an environment with timestamps. |

### 3.4 Readiness Scoring

| # | Capability | Priority | Effort | Description |
|---|-----------|----------|--------|-------------|
| 14 | Environment readiness check | **MEDIUM** | Medium | Evaluate whether an environment is ready for promotion: all integrations bound, policies met, no critical drift, baseline set. |
| 15 | Readiness score endpoint | **MEDIUM** | Small | Return readiness score (0-100) with breakdown by category. |
| 16 | Readiness dashboard (frontend) | **LOW** | Medium | Visual dashboard showing readiness of all environments in a promotion path. |

### 3.5 Environment Grouping & Relationships

| # | Capability | Priority | Effort | Description |
|---|-----------|----------|--------|-------------|
| 17 | Environment grouping | **LOW** | Medium | Group related environments (e.g., "EU Region Envs", "DR Pair"). |
| 18 | Environment relationships | **LOW** | Medium | Define parent/child, DR pairs, mirror relationships. |
| 19 | Group/relationship view (frontend) | **LOW** | Medium | Visual representation of environment topology. |

### 3.6 Policy & Binding Management

| # | Capability | Priority | Effort | Description |
|---|-----------|----------|--------|-------------|
| 20 | CRUD environment policies | **MEDIUM** | Medium | Full lifecycle management for environment-specific policies (currently entity exists but no endpoints). |
| 21 | CRUD telemetry policies | **MEDIUM** | Medium | Manage telemetry collection settings per environment. |
| 22 | CRUD integration bindings | **MEDIUM** | Medium | Manage which integrations are active per environment. |
| 23 | Policy/binding management UI | **MEDIUM** | Medium | UI for managing policies and bindings within environment detail page. |

---

## 4. Scope Boundary with Other Modules

### 4.1 Environment Management Owns

```
Environment definitions (CRUD, lifecycle)
├── Profile classification
├── Criticality assignment
├── Primary production designation
├── Production-like flag
├── Region assignment
├── Sort order
├── Activation/deactivation
│
Promotion paths
├── Path definition (ordered sequence)
├── Path validation (no cycles)
├── Path lifecycle
│
Baselines & drift
├── Baseline capture
├── Drift detection (definition-level)
├── Baseline history
│
Readiness
├── Readiness checks
├── Readiness scoring
│
Environment policies
├── Policy CRUD
├── Telemetry policies
├── Integration bindings
│
Environment grouping & relationships
├── Logical groups
├── Structural relationships (parent/child, DR pairs)
```

### 4.2 Environment Management Does NOT Own

| Capability | Owned By | Interaction Pattern |
|-----------|----------|-------------------|
| User access to environments | Identity & Access | Env Mgmt provides env IDs; Identity grants access |
| Per-environment config values | Configuration | Configuration reads env definitions |
| Runtime metric comparison | Operational Intelligence | Ops reads env definitions for context |
| Change promotion approval | Change Governance | Change Gov reads promotion paths |
| Audit trail of env changes | Audit & Compliance | Env Mgmt emits events; Audit captures |
| Environment-scoped notifications | Notifications | Notifications reads env context |

---

## 5. Feature Priority Matrix

| Priority | Features | Target Phase |
|----------|----------|-------------|
| **P0 — Must Have** | Module extraction, dedicated permissions, sidebar entry, env detail page | Phase 1 |
| **P1 — Should Have** | Promotion path management (CRUD + UI), soft-delete, env comparison (definitions) | Phase 1 |
| **P2 — Important** | Baseline management, drift detection, readiness scoring, policy CRUD | Phase 2 |
| **P3 — Nice to Have** | Environment grouping, relationships, topology view, baseline history | Phase 3 |

---

## 6. Endpoint Inventory — Target State

### Existing (to be moved to new module)

| # | Method | Route | Permission (New) |
|---|--------|-------|-----------------|
| 1 | `GET` | `/api/v1/environments` | `env:environments:read` |
| 2 | `POST` | `/api/v1/environments` | `env:environments:write` |
| 3 | `GET` | `/api/v1/environments/{id}` | `env:environments:read` |
| 4 | `PUT` | `/api/v1/environments/{id}` | `env:environments:write` |
| 5 | `DELETE` | `/api/v1/environments/{id}` | `env:environments:write` |
| 6 | `GET` | `/api/v1/environments/primary-production` | `env:environments:read` |
| 7 | `PUT` | `/api/v1/environments/{id}/primary-production` | `env:promotion:write` |

### New — Promotion Paths

| # | Method | Route | Permission |
|---|--------|-------|-----------|
| 8 | `GET` | `/api/v1/environments/promotion-paths` | `env:promotion:read` |
| 9 | `POST` | `/api/v1/environments/promotion-paths` | `env:promotion:write` |
| 10 | `PUT` | `/api/v1/environments/promotion-paths/{id}` | `env:promotion:write` |
| 11 | `DELETE` | `/api/v1/environments/promotion-paths/{id}` | `env:promotion:write` |

### New — Baselines & Drift

| # | Method | Route | Permission |
|---|--------|-------|-----------|
| 12 | `POST` | `/api/v1/environments/{id}/baseline` | `env:baseline:write` |
| 13 | `GET` | `/api/v1/environments/{id}/baseline` | `env:baseline:read` |
| 14 | `GET` | `/api/v1/environments/{id}/baseline/history` | `env:baseline:read` |
| 15 | `GET` | `/api/v1/environments/{id}/drift` | `env:baseline:read` |
| 16 | `GET` | `/api/v1/environments/compare` | `env:environments:read` |

### New — Readiness

| # | Method | Route | Permission |
|---|--------|-------|-----------|
| 17 | `GET` | `/api/v1/environments/{id}/readiness` | `env:environments:read` |

### New — Policies & Bindings

| # | Method | Route | Permission |
|---|--------|-------|-----------|
| 18 | `GET` | `/api/v1/environments/{id}/policies` | `env:policies:read` |
| 19 | `POST` | `/api/v1/environments/{id}/policies` | `env:policies:write` |
| 20 | `PUT` | `/api/v1/environments/{id}/policies/{policyId}` | `env:policies:write` |
| 21 | `DELETE` | `/api/v1/environments/{id}/policies/{policyId}` | `env:policies:write` |

**Total target: 21 endpoints** (from current 6).

---

## 7. Frontend Page Inventory — Target State

| # | Page | Route | Priority | Status |
|---|------|-------|----------|--------|
| 1 | `EnvironmentsPage` | `/environments` | P0 | ✅ Exists (needs move to env-management feature) |
| 2 | `EnvironmentDetailPage` | `/environments/{id}` | P0 | ❌ New |
| 3 | `PromotionPathsPage` | `/environments/promotion-paths` | P1 | ❌ New |
| 4 | `PromotionPathEditorPage` | `/environments/promotion-paths/{id}` | P1 | ❌ New |
| 5 | `EnvironmentDriftPage` | `/environments/{id}/drift` | P2 | ❌ New |
| 6 | `EnvironmentBaselinePage` | `/environments/{id}/baseline` | P2 | ❌ New |
| 7 | `EnvironmentReadinessPage` | `/environments/readiness` | P2 | ❌ New |

**Note:** `EnvironmentComparisonPage` stays in operations feature — it compares runtime metrics, not environment definitions.

---

## 8. Acceptance Criteria for Scope Completion

### Phase 1 (Module Foundation)
- [ ] Dedicated `src/modules/environmentmanagement/` backend module created
- [ ] All 5 environment entities moved from IdentityAccess
- [ ] `EnvironmentManagementDbContext` with `env_` table prefix
- [ ] Dedicated `env:*` permissions registered
- [ ] All 6 existing endpoints migrated to new module
- [ ] Environment detail endpoint (GET by ID) added
- [ ] Soft-delete endpoint added
- [ ] Frontend `environment-management` feature folder created
- [ ] `EnvironmentsPage` moved to new feature
- [ ] `EnvironmentDetailPage` created
- [ ] Sidebar entry for Environments added
- [ ] Migration path documented for database schema change

### Phase 2 (Core Features)
- [ ] Promotion path CRUD endpoints (4)
- [ ] Promotion path UI
- [ ] Baseline management endpoints (3)
- [ ] Drift detection endpoint
- [ ] Drift view UI
- [ ] Readiness scoring endpoint
- [ ] Policy CRUD endpoints (4)

### Phase 3 (Advanced Features)
- [ ] Environment grouping
- [ ] Environment relationships
- [ ] Baseline history
- [ ] Readiness dashboard
- [ ] Environment topology view
