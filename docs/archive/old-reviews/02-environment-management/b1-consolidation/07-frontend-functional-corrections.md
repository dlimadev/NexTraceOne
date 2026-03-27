# Environment Management Module — Frontend Functional Corrections

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Frontend State

### 1.1 Components Inventory

| # | Component | Location | Feature Module | LOC | Purpose |
|---|-----------|----------|---------------|-----|---------|
| 1 | `EnvironmentsPage` | `features/identity-access/pages/EnvironmentsPage.tsx` | identity-access | 434 | CRUD management of environments |
| 2 | `EnvironmentComparisonPage` | `features/operations/pages/EnvironmentComparisonPage.tsx` | operations | 623 | Runtime metrics comparison |
| 3 | `EnvironmentContext` | `contexts/EnvironmentContext.tsx` | shared context | 261 | Global environment state & hooks |
| 4 | `EnvironmentBanner` | `components/shell/EnvironmentBanner.tsx` | shell | 46 | Non-production warning banner |

### 1.2 Routes

| Route | Component | Permission | Feature |
|-------|-----------|-----------|---------|
| `/environments` | `EnvironmentsPage` | `identity:users:read` | identity-access |
| `/operations/runtime-comparison` | `EnvironmentComparisonPage` | `operations:runtime:read` | operations |

### 1.3 Sidebar Entries

| Entry | Route | Permission | Section |
|-------|-------|-----------|---------|
| `sidebar.environmentComparison` | `/operations/runtime-comparison` | `operations:runtime:read` | operations |

**Issue:** `EnvironmentsPage` has **no sidebar entry** — only accessible via direct URL.

### 1.4 API Services

| Service | File | Methods |
|---------|------|---------|
| Identity API | `features/identity-access/api/identity.ts` | `listEnvironments`, `createEnvironment`, `updateEnvironment`, `getPrimaryProductionEnvironment`, `setPrimaryProductionEnvironment` |
| Runtime Intelligence API | `features/operations/api/runtimeIntelligence.ts` | `compareReleaseRuntime`, `getDriftFindings`, `getReleaseHealthTimeline`, `getObservabilityScore` |

### 1.5 i18n Keys (~50+)

| Namespace | Count | Coverage |
|-----------|-------|----------|
| `environment.*` | 8 | Context banner & state |
| `environments.*` | 40+ | CRUD page |
| `environmentComparison.*` | 6 | Comparison page |
| `promotion.*` | 3 | Change governance |
| `sidebar.environmentComparison` | 1 | Navigation |

---

## 2. Structural Corrections

### 2.1 Create Dedicated Feature Module

**Action:** Create `src/frontend/src/features/environment-management/` as the dedicated feature module.

```
src/frontend/src/features/environment-management/
├── api/
│   └── environments.ts              ← NEW (extracted from identity.ts)
├── pages/
│   ├── EnvironmentsPage.tsx         ← MOVED from identity-access
│   ├── EnvironmentDetailPage.tsx    ← NEW
│   ├── PromotionPathsPage.tsx       ← NEW
│   ├── PromotionPathEditorPage.tsx  ← NEW
│   ├── EnvironmentDriftPage.tsx     ← NEW
│   ├── EnvironmentBaselinePage.tsx  ← NEW
│   └── EnvironmentReadinessPage.tsx ← NEW
├── components/
│   ├── EnvironmentCard.tsx          ← NEW
│   ├── EnvironmentStatusBadge.tsx   ← NEW
│   ├── PromotionPathVisualizer.tsx  ← NEW
│   ├── DriftFindingsTable.tsx       ← NEW
│   └── ReadinessScoreCard.tsx       ← NEW
├── hooks/
│   ├── useEnvironments.ts           ← NEW (React Query hooks)
│   ├── usePromotionPaths.ts         ← NEW
│   └── useBaselines.ts             ← NEW
└── index.ts                         ← barrel export
```

### 2.2 Move EnvironmentsPage

| Aspect | Current | Target |
|--------|---------|--------|
| File | `features/identity-access/pages/EnvironmentsPage.tsx` | `features/environment-management/pages/EnvironmentsPage.tsx` |
| Import in App.tsx | `features/identity-access` lazy import | `features/environment-management` lazy import |
| API calls | `features/identity-access/api/identity.ts` | `features/environment-management/api/environments.ts` |

### 2.3 Extract API Service

Move environment-related methods from `identity.ts` to a dedicated `environments.ts`:

**Methods to extract:**
- `listEnvironments()` → `GET /api/v1/environments`
- `getEnvironmentById(id)` → `GET /api/v1/environments/:id` (NEW)
- `createEnvironment(data)` → `POST /api/v1/environments`
- `updateEnvironment(id, data)` → `PUT /api/v1/environments/:id`
- `deactivateEnvironment(id)` → `DELETE /api/v1/environments/:id` (NEW)
- `getPrimaryProductionEnvironment()` → `GET /api/v1/environments/primary-production`
- `setPrimaryProductionEnvironment(id)` → `PUT /api/v1/environments/:id/primary-production`

**New methods:**
- `listPromotionPaths()` → `GET /api/v1/environments/promotion-paths`
- `createPromotionPath(data)` → `POST /api/v1/environments/promotion-paths`
- `updatePromotionPath(id, data)` → `PUT /api/v1/environments/promotion-paths/:id`
- `deletePromotionPath(id)` → `DELETE /api/v1/environments/promotion-paths/:id`
- `getBaseline(envId)` → `GET /api/v1/environments/:id/baseline`
- `setBaseline(envId, data)` → `POST /api/v1/environments/:id/baseline`
- `getBaselineHistory(envId)` → `GET /api/v1/environments/:id/baseline/history`
- `getDrift(envId)` → `GET /api/v1/environments/:id/drift`
- `compareEnvironments(env1Id, env2Id)` → `GET /api/v1/environments/compare`
- `getReadiness(envId)` → `GET /api/v1/environments/:id/readiness`
- `listPolicies(envId)` → `GET /api/v1/environments/:id/policies`
- `createPolicy(envId, data)` → `POST /api/v1/environments/:id/policies`
- `updatePolicy(envId, policyId, data)` → `PUT /api/v1/environments/:id/policies/:policyId`
- `deletePolicy(envId, policyId)` → `DELETE /api/v1/environments/:id/policies/:policyId`

### 2.4 EnvironmentComparisonPage Decision

`EnvironmentComparisonPage` stays in `features/operations/` because it compares **runtime metrics** (latency, error rates, throughput), which is an Operational Intelligence concern. It is NOT an environment definition comparison.

Environment Management will have its own **definition comparison** in `CompareEnvironments` — comparing config values, policies, integration bindings between two environments.

---

## 3. Navigation Corrections

### 3.1 Add Sidebar Entry for Environments

**File:** `src/frontend/src/components/shell/AppSidebar.tsx`

**Action:** Add a sidebar entry in a dedicated "Environments" or "Platform" section:

```typescript
{
  labelKey: 'sidebar.environments',
  to: '/environments',
  icon: <Server size={18} />,
  permission: 'env:environments:read',
  section: 'platform'  // or dedicated 'environments' section
}
```

### 3.2 Add Sub-Navigation for Environment Management

When the module grows, add sub-items:

| Label Key | Route | Permission | Priority |
|-----------|-------|-----------|----------|
| `sidebar.environments` | `/environments` | `env:environments:read` | P0 |
| `sidebar.promotionPaths` | `/environments/promotion-paths` | `env:promotion:read` | P1 |
| `sidebar.environmentReadiness` | `/environments/readiness` | `env:environments:read` | P2 |

### 3.3 Update Route Configuration

**File:** `src/frontend/src/App.tsx`

**Current:**
```typescript
{ path: '/environments', element: <EnvironmentsPage />, permission: 'identity:users:read' }
```

**Target:**
```typescript
// Environment Management routes
{ path: '/environments', element: <EnvironmentsPage />, permission: 'env:environments:read' },
{ path: '/environments/:id', element: <EnvironmentDetailPage />, permission: 'env:environments:read' },
{ path: '/environments/promotion-paths', element: <PromotionPathsPage />, permission: 'env:promotion:read' },
{ path: '/environments/promotion-paths/:id', element: <PromotionPathEditorPage />, permission: 'env:promotion:write' },
{ path: '/environments/:id/drift', element: <EnvironmentDriftPage />, permission: 'env:baseline:read' },
{ path: '/environments/:id/baseline', element: <EnvironmentBaselinePage />, permission: 'env:baseline:read' },
{ path: '/environments/readiness', element: <EnvironmentReadinessPage />, permission: 'env:environments:read' },
```

---

## 4. New Pages Required

### 4.1 EnvironmentDetailPage (P0)

**Route:** `/environments/:id`  
**Purpose:** Full environment detail view with tabs for policies, bindings, baseline, drift, readiness.

**Sections:**
- Header: Name, profile badge, criticality badge, status, region
- Overview tab: Properties, description, created/updated info
- Policies tab: List of environment policies with CRUD
- Integrations tab: Bound integrations with enable/disable
- Baseline tab: Current baseline info, set new baseline action
- Drift tab: Drift findings since last baseline
- Readiness tab: Latest readiness check result
- History tab: Audit trail of changes

**Persona relevance:**
- Engineer: Focus on policies, integrations, baseline
- Tech Lead: Focus on readiness, drift, promotion path position
- Architect: Focus on relationships, policies, topology
- Platform Admin: Full access to all tabs

### 4.2 PromotionPathsPage (P1)

**Route:** `/environments/promotion-paths`  
**Purpose:** List and manage promotion paths.

**Features:**
- Table listing all promotion paths with name, step count, default flag
- Visual preview of each path (environment badges in order)
- Create new path action
- Edit/delete actions per path
- Mark as default action

### 4.3 PromotionPathEditorPage (P1)

**Route:** `/environments/promotion-paths/:id`  
**Purpose:** Visual editor for a single promotion path.

**Features:**
- Drag-and-drop reordering of environments
- Add/remove environment from path
- Configure per-step settings (requires approval, soak time)
- Validation: min 2 steps, no duplicates, last step must be Production
- Save/cancel actions

### 4.4 EnvironmentDriftPage (P2)

**Route:** `/environments/:id/drift`  
**Purpose:** Show drift findings for an environment against its baseline.

**Features:**
- Summary: number of drift findings, severity breakdown
- Table of findings: category, what changed, baseline value, current value, severity
- Filter by category, severity
- Actions: acknowledge drift, set new baseline

### 4.5 EnvironmentBaselinePage (P2)

**Route:** `/environments/:id/baseline`  
**Purpose:** Manage baselines for an environment.

**Features:**
- Current active baseline info
- Set new baseline action with label
- Baseline history list with timestamps
- Compare two baselines

### 4.6 EnvironmentReadinessPage (P2)

**Route:** `/environments/readiness`  
**Purpose:** Dashboard showing readiness of all environments.

**Features:**
- Card per environment with readiness score (0-100)
- Color coding: green (ready), yellow (warning), red (not ready)
- Drill into individual environment readiness details
- Filter by promotion path, profile, criticality
- Trigger readiness check action

---

## 5. i18n Corrections

### 5.1 Key Namespace Standardization

**Issue:** Inconsistent key prefixes between `environment.*` (banner) and `environments.*` (page).

**Decision:** Keep both namespaces — they serve different purposes:
- `environment.*` — shell/context (banner, state, selection)
- `environments.*` — environment management CRUD page
- `environmentManagement.*` — new namespace for module-specific keys

### 5.2 New i18n Keys Required

| Key | English Value | Used By |
|-----|--------------|---------|
| `sidebar.environments` | "Environments" | Sidebar navigation |
| `sidebar.promotionPaths` | "Promotion Paths" | Sidebar navigation |
| `sidebar.environmentReadiness` | "Readiness" | Sidebar navigation |
| `environmentManagement.detail.title` | "Environment Details" | Detail page |
| `environmentManagement.detail.tabs.overview` | "Overview" | Detail page tab |
| `environmentManagement.detail.tabs.policies` | "Policies" | Detail page tab |
| `environmentManagement.detail.tabs.integrations` | "Integrations" | Detail page tab |
| `environmentManagement.detail.tabs.baseline` | "Baseline" | Detail page tab |
| `environmentManagement.detail.tabs.drift` | "Drift" | Detail page tab |
| `environmentManagement.detail.tabs.readiness` | "Readiness" | Detail page tab |
| `environmentManagement.detail.tabs.history` | "History" | Detail page tab |
| `environmentManagement.promotionPaths.title` | "Promotion Paths" | Promotion paths page |
| `environmentManagement.promotionPaths.create` | "Create Promotion Path" | Create action |
| `environmentManagement.promotionPaths.default` | "Default Path" | Default badge |
| `environmentManagement.promotionPaths.steps` | "Steps" | Step count |
| `environmentManagement.promotionPaths.requiresApproval` | "Requires Approval" | Step config |
| `environmentManagement.promotionPaths.soakTime` | "Minimum Soak Time" | Step config |
| `environmentManagement.drift.title` | "Drift Detection" | Drift page |
| `environmentManagement.drift.noDrift` | "No drift detected" | Empty state |
| `environmentManagement.drift.findings` | "Drift Findings" | Findings section |
| `environmentManagement.baseline.title` | "Baseline Management" | Baseline page |
| `environmentManagement.baseline.setCurrent` | "Set Current Baseline" | Action |
| `environmentManagement.baseline.history` | "Baseline History" | History section |
| `environmentManagement.readiness.title` | "Environment Readiness" | Readiness page |
| `environmentManagement.readiness.score` | "Readiness Score" | Score display |
| `environmentManagement.readiness.ready` | "Ready" | Status |
| `environmentManagement.readiness.notReady` | "Not Ready" | Status |
| `environmentManagement.readiness.warning` | "Warning" | Status |
| `environmentManagement.readiness.checkNow` | "Check Now" | Action |
| `environmentManagement.deactivate.confirm` | "Are you sure you want to deactivate this environment?" | Confirm dialog |
| `environmentManagement.deactivate.success` | "Environment deactivated successfully" | Success message |

### 5.3 Translation File Organization

Add new keys to `src/frontend/src/locales/en.json` (and all supported locales) under the `environmentManagement` namespace.

---

## 6. Permission Updates in Frontend

### 6.1 Route Guards

| Route | Current Guard | New Guard |
|-------|-------------|----------|
| `/environments` | `identity:users:read` | `env:environments:read` |
| `/environments/:id` | — (new) | `env:environments:read` |
| `/environments/promotion-paths` | — (new) | `env:promotion:read` |
| `/environments/promotion-paths/:id` | — (new) | `env:promotion:write` |
| `/environments/:id/drift` | — (new) | `env:baseline:read` |
| `/environments/:id/baseline` | — (new) | `env:baseline:read` |
| `/environments/readiness` | — (new) | `env:environments:read` |
| `/operations/runtime-comparison` | `operations:runtime:read` | `operations:runtime:read` (unchanged) |

### 6.2 Action Guards

| Action | Current Permission | New Permission |
|--------|-------------------|---------------|
| Create environment | `identity:users:write` | `env:environments:write` |
| Edit environment | `identity:users:write` | `env:environments:write` |
| Deactivate environment | — (new) | `env:environments:write` |
| Set primary production | `promotion:environments:write` | `env:promotion:write` |
| Create promotion path | — (new) | `env:promotion:write` |
| Set baseline | — (new) | `env:baseline:write` |

---

## 7. Shared Components (Stay in Place)

| Component | Location | Reason |
|-----------|----------|--------|
| `EnvironmentContext` | `contexts/EnvironmentContext.tsx` | Shared shell concern — used by all features |
| `EnvironmentBanner` | `components/shell/EnvironmentBanner.tsx` | Shell component — not module-specific |

These components provide cross-cutting environment awareness and should NOT move into the feature module.

---

## 8. Execution Priority

| Priority | Items | Effort |
|----------|-------|--------|
| **P0** | Create feature folder, move EnvironmentsPage, extract API service, add sidebar entry, update route permissions | Medium |
| **P1** | EnvironmentDetailPage, PromotionPathsPage, PromotionPathEditorPage | Large |
| **P2** | EnvironmentDriftPage, EnvironmentBaselinePage, EnvironmentReadinessPage | Large |
| **P3** | Reusable components (cards, badges, visualizer), React Query hooks | Medium |
