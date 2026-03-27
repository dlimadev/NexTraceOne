# Environment Management Module — Backend Functional Corrections

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Backend State

### 1.1 Module Structure

Environment Management has **no dedicated backend module**. All environment logic resides in the Identity & Access module:

```
src/modules/identityaccess/
├── NexTraceOne.IdentityAccess.Domain/
│   ├── Entities/
│   │   ├── Environment.cs                    ← aggregate root
│   │   ├── EnvironmentAccess.cs              ← stays in Identity
│   │   ├── EnvironmentPolicy.cs              ← unpersisted entity
│   │   ├── EnvironmentTelemetryPolicy.cs     ← unpersisted entity
│   │   └── EnvironmentIntegrationBinding.cs  ← unpersisted entity
│   ├── Enums/
│   │   ├── EnvironmentCriticality.cs
│   │   └── EnvironmentProfile.cs
│   └── ValueObjects/
│       ├── TenantEnvironmentContext.cs
│       └── EnvironmentUiProfile.cs
├── NexTraceOne.IdentityAccess.Application/
│   └── Features/Environments/
│       ├── ListEnvironments.cs
│       ├── CreateEnvironment.cs
│       ├── UpdateEnvironment.cs
│       ├── GrantEnvironmentAccess.cs
│       ├── GetPrimaryProductionEnvironment.cs
│       └── SetPrimaryProductionEnvironment.cs
├── NexTraceOne.IdentityAccess.API/
│   └── Endpoints/ (EnvironmentsEndpointModule or similar)
└── NexTraceOne.IdentityAccess.Infrastructure/
    ├── Persistence/
    │   ├── IdentityDbContext.cs (DbSets: Environments, EnvironmentAccesses)
    │   └── Configurations/
    │       ├── EnvironmentConfiguration.cs
    │       └── EnvironmentAccessConfiguration.cs
    ├── Repositories/
    │   └── EnvironmentRepository.cs
    ├── Services/
    │   ├── EnvironmentContextAccessor.cs
    │   ├── EnvironmentAccessValidator.cs
    │   ├── EnvironmentProfileResolver.cs
    │   └── TenantEnvironmentContextResolver.cs
    ├── Middleware/
    │   └── EnvironmentResolutionMiddleware.cs
    └── Authorization/
        └── EnvironmentAccessAuthorizationHandler.cs
```

### 1.2 Endpoints (6 total)

| # | Method | Route | Permission | Handler |
|---|--------|-------|-----------|---------|
| 1 | `GET` | `/api/v1/environments` | `identity:users:read` | `ListEnvironments` |
| 2 | `POST` | `/api/v1/environments` | `identity:users:write` | `CreateEnvironment` |
| 3 | `GET` | `/api/v1/environments/primary-production` | `identity:users:read` | `GetPrimaryProductionEnvironment` |
| 4 | `PUT` | `/api/v1/environments/{id}` | `identity:users:write` | `UpdateEnvironment` |
| 5 | `POST` | `/api/v1/environments/{id}/access` | `identity:users:write` | `GrantEnvironmentAccess` |
| 6 | `PUT` | `/api/v1/environments/{id}/primary-production` | `promotion:environments:write` | `SetPrimaryProductionEnvironment` |

---

## 2. Structural Corrections (Module Extraction)

### 2.1 Create Dedicated Module

**Action:** Create `src/modules/environmentmanagement/` with standard vertical slice structure.

```
src/modules/environmentmanagement/
├── NexTraceOne.EnvironmentManagement.Domain/
│   ├── Entities/
│   │   ├── Environment.cs
│   │   ├── EnvironmentPolicy.cs
│   │   ├── EnvironmentTelemetryPolicy.cs
│   │   ├── EnvironmentIntegrationBinding.cs
│   │   ├── PromotionPath.cs                    ← NEW
│   │   ├── EnvironmentBaseline.cs              ← NEW
│   │   └── EnvironmentReadinessCheck.cs        ← NEW
│   ├── Enums/
│   │   ├── EnvironmentCriticality.cs
│   │   ├── EnvironmentProfile.cs
│   │   ├── ReadinessStatus.cs                  ← NEW
│   │   └── ReadinessCheckStatus.cs             ← NEW
│   ├── ValueObjects/
│   │   ├── EnvironmentUiProfile.cs
│   │   ├── PromotionPathStep.cs                ← NEW
│   │   ├── BaselineSnapshot.cs                 ← NEW
│   │   └── ReadinessFinding.cs                 ← NEW
│   └── Events/
│       ├── EnvironmentCreated.cs               ← NEW
│       ├── EnvironmentUpdated.cs               ← NEW
│       ├── EnvironmentDeactivated.cs           ← NEW
│       ├── PrimaryProductionChanged.cs         ← NEW
│       ├── PromotionPathCreated.cs             ← NEW
│       ├── BaselineSet.cs                      ← NEW
│       └── DriftDetected.cs                    ← NEW
├── NexTraceOne.EnvironmentManagement.Application/
│   └── Features/
│       ├── Environments/
│       │   ├── ListEnvironments.cs
│       │   ├── GetEnvironmentById.cs           ← NEW
│       │   ├── CreateEnvironment.cs
│       │   ├── UpdateEnvironment.cs
│       │   ├── DeactivateEnvironment.cs        ← NEW
│       │   ├── GetPrimaryProductionEnvironment.cs
│       │   └── SetPrimaryProductionEnvironment.cs
│       ├── PromotionPaths/
│       │   ├── ListPromotionPaths.cs           ← NEW
│       │   ├── CreatePromotionPath.cs          ← NEW
│       │   ├── UpdatePromotionPath.cs          ← NEW
│       │   └── DeletePromotionPath.cs          ← NEW
│       ├── Baselines/
│       │   ├── SetBaseline.cs                  ← NEW
│       │   ├── GetCurrentBaseline.cs           ← NEW
│       │   ├── GetBaselineHistory.cs           ← NEW
│       │   └── DetectDrift.cs                  ← NEW
│       ├── Readiness/
│       │   └── CheckReadiness.cs               ← NEW
│       ├── Policies/
│       │   ├── ListEnvironmentPolicies.cs      ← NEW
│       │   ├── CreateEnvironmentPolicy.cs      ← NEW
│       │   ├── UpdateEnvironmentPolicy.cs      ← NEW
│       │   └── DeleteEnvironmentPolicy.cs      ← NEW
│       └── Compare/
│           └── CompareEnvironments.cs          ← NEW
├── NexTraceOne.EnvironmentManagement.API/
│   └── Endpoints/
│       ├── EnvironmentsEndpointModule.cs
│       ├── PromotionPathsEndpointModule.cs     ← NEW
│       ├── BaselinesEndpointModule.cs          ← NEW
│       ├── ReadinessEndpointModule.cs          ← NEW
│       └── PoliciesEndpointModule.cs           ← NEW
└── NexTraceOne.EnvironmentManagement.Infrastructure/
    ├── Persistence/
    │   ├── EnvironmentManagementDbContext.cs    ← NEW
    │   └── Configurations/
    │       ├── EnvironmentConfiguration.cs
    │       ├── EnvironmentPolicyConfiguration.cs        ← NEW
    │       ├── EnvironmentTelemetryPolicyConfiguration.cs ← NEW
    │       ├── EnvironmentIntegrationBindingConfiguration.cs ← NEW
    │       ├── PromotionPathConfiguration.cs            ← NEW
    │       ├── EnvironmentBaselineConfiguration.cs      ← NEW
    │       └── EnvironmentReadinessCheckConfiguration.cs ← NEW
    └── Repositories/
        └── EnvironmentRepository.cs
```

### 2.2 Remove from IdentityAccess

After extraction, remove from Identity & Access:

| Item to Remove | File | Notes |
|---------------|------|-------|
| `Environment` entity | `IdentityAccess.Domain/Entities/Environment.cs` | Moved to EnvironmentManagement |
| `EnvironmentPolicy` entity | `IdentityAccess.Domain/Entities/EnvironmentPolicy.cs` | Moved |
| `EnvironmentTelemetryPolicy` entity | `IdentityAccess.Domain/Entities/EnvironmentTelemetryPolicy.cs` | Moved |
| `EnvironmentIntegrationBinding` entity | `IdentityAccess.Domain/Entities/EnvironmentIntegrationBinding.cs` | Moved |
| `EnvironmentCriticality` enum | `IdentityAccess.Domain/Enums/EnvironmentCriticality.cs` | Moved |
| `EnvironmentProfile` enum | `IdentityAccess.Domain/Enums/EnvironmentProfile.cs` | Moved |
| `EnvironmentUiProfile` value object | `IdentityAccess.Domain/ValueObjects/EnvironmentUiProfile.cs` | Moved |
| `Environments` DbSet | `IdentityDbContext.cs` | Removed (table stays, accessed via new context) |
| `EnvironmentConfiguration` | Config file | Moved to new Infrastructure |
| All 6 CQRS features | `Application/Features/Environments/*` | Moved |
| Environment endpoints | API endpoint module | Moved |
| `EnvironmentRepository` | Infrastructure/Repositories/ | Moved |
| `EnvironmentProfileResolver` | Infrastructure/Services/ | Moved |

### 2.3 Stays in IdentityAccess

| Item | File | Reason |
|------|------|--------|
| `EnvironmentAccess` entity | `Domain/Entities/EnvironmentAccess.cs` | User access control — Identity domain |
| `EnvironmentAccesses` DbSet | `IdentityDbContext.cs` | Identity persistence |
| `EnvironmentAccessConfiguration` | Config file | Identity persistence |
| `GrantEnvironmentAccess` feature | `Application/Features/Environments/GrantEnvironmentAccess.cs` | Identity operation |
| `EnvironmentContextAccessor` | Infrastructure/Services/ | Cross-cutting auth concern |
| `EnvironmentAccessValidator` | Infrastructure/Services/ | Auth validation |
| `TenantEnvironmentContextResolver` | Infrastructure/Services/ | Cross-cutting |
| `EnvironmentResolutionMiddleware` | Infrastructure/Middleware/ | Cross-cutting |
| `EnvironmentAccessAuthorizationHandler` | Infrastructure/Authorization/ | Auth handler |

---

## 3. Functional Corrections (Existing Endpoints)

### 3.1 Permission Alignment

| Endpoint | Current Permission | Correct Permission | Priority |
|----------|-------------------|-------------------|----------|
| `GET /environments` | `identity:users:read` | `env:environments:read` | **CRITICAL** |
| `POST /environments` | `identity:users:write` | `env:environments:write` | **CRITICAL** |
| `GET /environments/primary-production` | `identity:users:read` | `env:environments:read` | **CRITICAL** |
| `PUT /environments/{id}` | `identity:users:write` | `env:environments:write` | **CRITICAL** |
| `POST /environments/{id}/access` | `identity:users:write` | `identity:access:write` | **HIGH** (stays in Identity) |
| `PUT /environments/{id}/primary-production` | `promotion:environments:write` | `env:promotion:write` | **MEDIUM** |

### 3.2 Missing Validations

| Endpoint | Missing Validation | Priority |
|----------|-------------------|----------|
| `POST /environments` | Duplicate slug check per tenant | **HIGH** |
| `POST /environments` | Profile enum validation (reject unknown values) | **MEDIUM** |
| `PUT /environments/{id}` | Concurrency check (no `xmin`/`ETag`) | **HIGH** |
| `PUT /environments/{id}` | Cannot change profile if services are bound | **MEDIUM** |
| `PUT /primary-production` | Cannot designate inactive environment | **HIGH** |
| `PUT /primary-production` | Cannot designate non-Production profile as primary | **HIGH** |

### 3.3 Missing Error Handling

| Endpoint | Missing Handler | Priority |
|----------|----------------|----------|
| All write endpoints | `DbUpdateConcurrencyException` → 409 Conflict | **HIGH** |
| All endpoints | Standardized error response with `code`, `messageKey`, `correlationId` | **MEDIUM** |
| `PUT /environments/{id}` | 404 Not Found when environment doesn't exist | **MEDIUM** |

---

## 4. New Endpoint Requirements

### 4.1 Core CRUD Additions

| # | Method | Route | Permission | Handler | Priority |
|---|--------|-------|-----------|---------|----------|
| 1 | `GET` | `/api/v1/environments/{id}` | `env:environments:read` | `GetEnvironmentById` | **HIGH** |
| 2 | `DELETE` | `/api/v1/environments/{id}` | `env:environments:write` | `DeactivateEnvironment` | **HIGH** |

### 4.2 Promotion Paths

| # | Method | Route | Permission | Handler | Priority |
|---|--------|-------|-----------|---------|----------|
| 3 | `GET` | `/api/v1/environments/promotion-paths` | `env:promotion:read` | `ListPromotionPaths` | **HIGH** |
| 4 | `POST` | `/api/v1/environments/promotion-paths` | `env:promotion:write` | `CreatePromotionPath` | **HIGH** |
| 5 | `PUT` | `/api/v1/environments/promotion-paths/{id}` | `env:promotion:write` | `UpdatePromotionPath` | **MEDIUM** |
| 6 | `DELETE` | `/api/v1/environments/promotion-paths/{id}` | `env:promotion:write` | `DeletePromotionPath` | **LOW** |

### 4.3 Baselines & Drift

| # | Method | Route | Permission | Handler | Priority |
|---|--------|-------|-----------|---------|----------|
| 7 | `POST` | `/api/v1/environments/{id}/baseline` | `env:baseline:write` | `SetBaseline` | **MEDIUM** |
| 8 | `GET` | `/api/v1/environments/{id}/baseline` | `env:baseline:read` | `GetCurrentBaseline` | **MEDIUM** |
| 9 | `GET` | `/api/v1/environments/{id}/baseline/history` | `env:baseline:read` | `GetBaselineHistory` | **LOW** |
| 10 | `GET` | `/api/v1/environments/{id}/drift` | `env:baseline:read` | `DetectDrift` | **MEDIUM** |
| 11 | `GET` | `/api/v1/environments/compare` | `env:environments:read` | `CompareEnvironments` | **MEDIUM** |

### 4.4 Readiness

| # | Method | Route | Permission | Handler | Priority |
|---|--------|-------|-----------|---------|----------|
| 12 | `GET` | `/api/v1/environments/{id}/readiness` | `env:environments:read` | `CheckReadiness` | **MEDIUM** |

### 4.5 Policies

| # | Method | Route | Permission | Handler | Priority |
|---|--------|-------|-----------|---------|----------|
| 13 | `GET` | `/api/v1/environments/{id}/policies` | `env:policies:read` | `ListEnvironmentPolicies` | **MEDIUM** |
| 14 | `POST` | `/api/v1/environments/{id}/policies` | `env:policies:write` | `CreateEnvironmentPolicy` | **MEDIUM** |
| 15 | `PUT` | `/api/v1/environments/{id}/policies/{policyId}` | `env:policies:write` | `UpdateEnvironmentPolicy` | **LOW** |
| 16 | `DELETE` | `/api/v1/environments/{id}/policies/{policyId}` | `env:policies:write` | `DeleteEnvironmentPolicy` | **LOW** |

---

## 5. Cross-Cutting Concerns

### 5.1 CancellationToken

All async handlers must accept and propagate `CancellationToken`. Verify all existing handlers comply.

### 5.2 Result Pattern

All command handlers should return `Result<T>` for controlled failures instead of throwing exceptions.

### 5.3 Audit Events

Every write operation must emit an audit event:

| Operation | Event | Key Data |
|----------|-------|----------|
| Create environment | `environment.created` | environmentId, name, profile, criticality |
| Update environment | `environment.updated` | environmentId, changedFields |
| Deactivate environment | `environment.deactivated` | environmentId, reason |
| Set primary production | `environment.primary_production_changed` | environmentId, previousPrimaryId |
| Create promotion path | `promotion_path.created` | pathId, stepCount |
| Set baseline | `baseline.set` | baselineId, environmentId |

### 5.4 Multi-Tenancy

All queries must filter by `TenantId`. Verify all existing queries enforce tenant isolation.

---

## 6. Execution Order

```
Phase 1: Module Foundation
  1. Create module project structure (4 projects)
  2. Move entities, enums, value objects from IdentityAccess
  3. Create EnvironmentManagementDbContext with env_ prefix
  4. Move existing 5 CQRS features (keep GrantEnvironmentAccess in Identity)
  5. Move endpoint module
  6. Update permissions to env:* namespace
  7. Add xmin concurrency tokens
  8. Add GetEnvironmentById endpoint
  9. Add DeactivateEnvironment endpoint
  10. Register module in DI and API host

Phase 2: Core Features
  11. Add promotion path CRUD (4 endpoints)
  12. Add baseline management (3 endpoints)
  13. Add drift detection endpoint
  14. Add environment comparison endpoint
  15. Add readiness check endpoint

Phase 3: Policies & Advanced
  16. Add policy CRUD (4 endpoints)
  17. Add domain events emission
  18. Add audit event integration
```

---

## 7. Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Breaking Identity module during extraction | **HIGH** | Feature flag new module, run both in parallel during transition |
| Route collision during migration | **MEDIUM** | New module uses same routes — swap at deployment |
| Permission rename breaks existing tokens | **HIGH** | Support both old and new permission names during transition period |
| Database migration downtime | **LOW** | Table rename is an online operation |
| Cross-module reference breaks | **MEDIUM** | Use strongly-typed IDs, no navigation properties across modules |
