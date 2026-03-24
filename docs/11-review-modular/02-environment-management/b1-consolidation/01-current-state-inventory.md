# Environment Management Module — Current State Inventory

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Module Location

Environment Management does **NOT** have its own backend module.  
All environment entities, features, infrastructure, and persistence live inside the **Identity & Access** module.

| Layer | Current Location | Expected Location |
|-------|-----------------|-------------------|
| Domain entities | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Domain/Entities/` |
| Application (CQRS) | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Features/Environments/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Application/Features/` |
| API endpoints | `src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.API/Endpoints/` |
| Infrastructure | `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/` | `src/modules/environmentmanagement/NexTraceOne.EnvironmentManagement.Infrastructure/` |
| DbContext | `IdentityDbContext` | `EnvironmentManagementDbContext` |
| Frontend page | `src/frontend/src/features/identity-access/pages/EnvironmentsPage.tsx` | `src/frontend/src/features/environment-management/pages/` |
| Frontend context | `src/frontend/src/contexts/EnvironmentContext.tsx` | Stays (shared shell concern) |

---

## 2. Entities Currently in IdentityAccess (Environment-Related) — 5

| # | Entity | File | DbSet | Belongs to Env Management? |
|---|--------|------|-------|---------------------------|
| 1 | `Environment` | `Domain/Entities/Environment.cs` | ✅ `Environments` | ✅ YES — aggregate root |
| 2 | `EnvironmentAccess` | `Domain/Entities/EnvironmentAccess.cs` | ✅ `EnvironmentAccesses` | ⚠️ SHARED — access control, referenced by Identity for auth |
| 3 | `EnvironmentPolicy` | `Domain/Entities/EnvironmentPolicy.cs` | — (no DbSet) | ✅ YES — policy enforcement per environment |
| 4 | `EnvironmentTelemetryPolicy` | `Domain/Entities/EnvironmentTelemetryPolicy.cs` | — (no DbSet) | ✅ YES — telemetry settings per environment |
| 5 | `EnvironmentIntegrationBinding` | `Domain/Entities/EnvironmentIntegrationBinding.cs` | — (no DbSet) | ✅ YES — integration bindings per environment |

**Summary:** 5 entities. All belong to Environment Management. `EnvironmentAccess` is shared with Identity for authorization evaluation.

---

## 3. Enums

| # | Enum | File | Values |
|---|------|------|--------|
| 1 | `EnvironmentCriticality` | `Domain/Enums/EnvironmentCriticality.cs` | Low, Medium, High, Critical |
| 2 | `EnvironmentProfile` | `Domain/Enums/EnvironmentProfile.cs` | Development, Validation, Staging, Production, DisasterRecovery |

**Note:** Frontend supports additional profiles not in backend enum: Sandbox, Training, UserAcceptanceTesting, PerformanceTesting.

---

## 4. Value Objects

| # | Value Object | File | Purpose |
|---|-------------|------|---------|
| 1 | `TenantEnvironmentContext` | `Domain/ValueObjects/TenantEnvironmentContext.cs` | Encapsulates tenant + environment context for cross-cutting resolution |
| 2 | `EnvironmentUiProfile` | `Domain/ValueObjects/EnvironmentUiProfile.cs` | UI presentation metadata for an environment |

---

## 5. CQRS Features (Application Layer) — 6

| # | Feature | Type | File | Handler |
|---|---------|------|------|---------|
| 1 | `ListEnvironments` | Query | `Features/Environments/ListEnvironments.cs` | Returns all environments for a tenant |
| 2 | `CreateEnvironment` | Command | `Features/Environments/CreateEnvironment.cs` | Creates new environment with profile/criticality |
| 3 | `UpdateEnvironment` | Command | `Features/Environments/UpdateEnvironment.cs` | Updates environment properties |
| 4 | `GrantEnvironmentAccess` | Command | `Features/Environments/GrantEnvironmentAccess.cs` | Grants user access to an environment |
| 5 | `GetPrimaryProductionEnvironment` | Query | `Features/Environments/GetPrimaryProductionEnvironment.cs` | Returns the primary production environment |
| 6 | `SetPrimaryProductionEnvironment` | Command | `Features/Environments/SetPrimaryProductionEnvironment.cs` | Designates an environment as primary production |

**Missing features:** DeleteEnvironment, CompareEnvironments, DetectDrift, ManagePromotionPath, GetEnvironmentReadiness, SetEnvironmentBaseline.

---

## 6. API Endpoints — 6

| # | Method | Route | Permission | Purpose |
|---|--------|-------|-----------|---------|
| 1 | `GET` | `/api/v1/environments` | `identity:users:read` | List all environments |
| 2 | `POST` | `/api/v1/environments` | `identity:users:write` | Create environment |
| 3 | `GET` | `/api/v1/environments/primary-production` | `identity:users:read` | Get primary production environment |
| 4 | `PUT` | `/api/v1/environments/{id}` | `identity:users:write` | Update environment |
| 5 | `POST` | `/api/v1/environments/{id}/access` | `identity:users:write` | Grant environment access |
| 6 | `PUT` | `/api/v1/environments/{id}/primary-production` | `promotion:environments:write` | Set primary production |

**Issues:**
- Routes under `/api/v1/environments` but served by Identity module
- Permissions use `identity:users:*` — too generic for environment management
- Only 6 endpoints — significant functional gaps

---

## 7. Infrastructure Services — 7

| # | Service | File | Purpose |
|---|---------|------|---------|
| 1 | `EnvironmentContextAccessor` | `Infrastructure/Services/EnvironmentContextAccessor.cs` | Resolves current environment context |
| 2 | `EnvironmentAccessValidator` | `Infrastructure/Services/EnvironmentAccessValidator.cs` | Validates user access to environments |
| 3 | `EnvironmentProfileResolver` | `Infrastructure/Services/EnvironmentProfileResolver.cs` | Resolves environment UI profile |
| 4 | `TenantEnvironmentContextResolver` | `Infrastructure/Services/TenantEnvironmentContextResolver.cs` | Resolves tenant-environment context |
| 5 | `EnvironmentResolutionMiddleware` | `Infrastructure/Middleware/EnvironmentResolutionMiddleware.cs` | ASP.NET middleware for environment context |
| 6 | `EnvironmentRepository` | `Infrastructure/Repositories/EnvironmentRepository.cs` | Data access for environments |
| 7 | `EnvironmentAccessAuthorizationHandler` | `Infrastructure/Authorization/EnvironmentAccessAuthorizationHandler.cs` | Authorization handler for environment access |

**Note:** Services 1, 2, 4, 5, 7 are cross-cutting concerns used by the entire platform. They may remain in a shared location or Identity module even after extraction.

---

## 8. Database Tables — 2

| # | Table | Schema | Entity | Configuration |
|---|-------|--------|--------|--------------|
| 1 | `dbo.Environments` | `dbo` | `Environment` | `EnvironmentConfiguration` |
| 2 | `dbo.EnvironmentAccesses` | `dbo` | `EnvironmentAccess` | `EnvironmentAccessConfiguration` |

**Issues:**
- Uses `dbo` schema — should use `env_` prefix for module isolation
- Only 2 tables configured — `EnvironmentPolicy`, `EnvironmentTelemetryPolicy`, `EnvironmentIntegrationBinding` have no DbSet/table configuration
- Tables live in `IdentityDbContext` — should have dedicated `EnvironmentManagementDbContext`

---

## 9. Frontend Components — 4

| # | Component | File | Feature Module | LOC | Purpose |
|---|-----------|------|---------------|-----|---------|
| 1 | `EnvironmentsPage` | `features/identity-access/pages/EnvironmentsPage.tsx` | identity-access | 434 | CRUD management of environments |
| 2 | `EnvironmentComparisonPage` | `features/operations/pages/EnvironmentComparisonPage.tsx` | operations | 623 | Runtime comparison between environments |
| 3 | `EnvironmentContext` | `contexts/EnvironmentContext.tsx` | shared context | 261 | Global environment state & hooks |
| 4 | `EnvironmentBanner` | `components/shell/EnvironmentBanner.tsx` | shell | 46 | Non-production warning banner |

**Issues:**
- `EnvironmentsPage` lives in identity-access feature, not a dedicated environment-management feature
- `EnvironmentComparisonPage` lives in operations feature — unclear ownership
- No `EnvironmentDetailPage`, no promotion path UI, no drift view, no baseline management UI

---

## 10. Frontend Routes — 2

| # | Route | Component | Permission | Feature |
|---|-------|-----------|-----------|---------|
| 1 | `/environments` | `EnvironmentsPage` | `identity:users:read` | identity-access |
| 2 | `/operations/runtime-comparison` | `EnvironmentComparisonPage` | `operations:runtime:read` | operations |

---

## 11. Frontend API Services

| # | Service | File | Methods |
|---|---------|------|---------|
| 1 | Identity API | `features/identity-access/api/identity.ts` | `listEnvironments`, `createEnvironment`, `updateEnvironment`, `getPrimaryProductionEnvironment`, `setPrimaryProductionEnvironment` |
| 2 | Runtime Intelligence API | `features/operations/api/runtimeIntelligence.ts` | `compareReleaseRuntime`, `getDriftFindings`, `getReleaseHealthTimeline`, `getObservabilityScore` |

---

## 12. i18n Keys — 50+

| Namespace | Count | Coverage |
|-----------|-------|----------|
| `environment.*` | 8 | Context banner & state |
| `environments.*` | 40+ | Environments CRUD page |
| `environmentComparison.*` | 6 | Comparison page |
| `promotion.*` | 3 | Change governance context |
| `sidebar.environmentComparison` | 1 | Navigation |

**Issue:** No i18n key for EnvironmentsPage sidebar entry (because it has no sidebar entry).

---

## 13. Sidebar Entries — 1

| # | Label Key | Route | Permission | Section |
|---|-----------|-------|-----------|---------|
| 1 | `sidebar.environmentComparison` | `/operations/runtime-comparison` | `operations:runtime:read` | operations |

**Issue:** `EnvironmentsPage` has **no sidebar entry** — discoverability problem.

---

## 14. Permissions Used — 3

| # | Permission | Used By | Appropriate? |
|---|-----------|---------|-------------|
| 1 | `identity:users:read` | List envs, get primary production | ❌ Too generic |
| 2 | `identity:users:write` | Create/update env, grant access | ❌ Too generic |
| 3 | `promotion:environments:write` | Set primary production | ⚠️ Acceptable but misplaced module |

---

## 15. Summary of Gaps

| Category | Exists | Missing/Incomplete |
|----------|--------|-------------------|
| Dedicated backend module | ❌ | Entire module structure |
| Dedicated DbContext | ❌ | `EnvironmentManagementDbContext` |
| Table prefix | ❌ | `env_` prefix for all tables |
| Dedicated permissions | ❌ | `env:*` permission namespace |
| Dedicated frontend feature | ❌ | `features/environment-management/` |
| Sidebar entry for CRUD | ❌ | EnvironmentsPage not in sidebar |
| Environment detail page | ❌ | Individual environment view |
| Promotion path management | ❌ | Define Dev→Staging→Prod paths |
| Drift detection | ❌ | Compare environment configurations |
| Baseline management | ❌ | Set and track environment baselines |
| Readiness scoring | ❌ | Assess environment readiness for promotion |
| Environment grouping | ❌ | Group related environments |
| Delete environment | ❌ | Soft-delete with guards |
| Concurrency tokens | ❌ | `xmin` on all entities |
