# Environment Management Module — Domain Model Finalization

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Aggregate Root: Environment

The `Environment` entity is the single aggregate root for this module. All other entities are direct children or associated entities within the aggregate boundary.

### 1.1 Current State

**File:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/Environment.cs`

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| `Id` | `EnvironmentId` (strongly-typed) | ✅ | Primary key |
| `TenantId` | `TenantId` (strongly-typed) | ✅ | Multi-tenancy isolation |
| `Name` | `string` | ✅ | Display name (max length enforced) |
| `Slug` | `string` | ✅ | URL-safe unique identifier |
| `SortOrder` | `int` | ✅ | Display ordering |
| `IsActive` | `bool` | ✅ | Activation state |
| `Profile` | `EnvironmentProfile` | ✅ | Type classification (enum) |
| `Code` | `string` | ❌ | Short code identifier |
| `Description` | `string` | ❌ | Free-text description |
| `Criticality` | `EnvironmentCriticality` | ✅ | Business impact level |
| `Region` | `string` | ❌ | Geographic region |
| `IsProductionLike` | `bool` | ✅ | Mirrors production characteristics |
| `IsPrimaryProduction` | `bool` | ✅ | Designated primary production |
| `CreatedAt` | `DateTimeOffset` | ✅ | Audit timestamp |

### 1.2 Assessment

| Aspect | Status | Notes |
|--------|--------|-------|
| Strongly-typed ID | ✅ Good | `EnvironmentId` used correctly |
| Multi-tenancy | ✅ Good | `TenantId` present on aggregate root |
| Audit fields | ⚠️ Incomplete | Only `CreatedAt` — missing `UpdatedAt`, `CreatedBy`, `UpdatedBy` |
| Concurrency | ❌ Missing | No `xmin` concurrency token configured |
| Soft-delete | ❌ Missing | No `DeactivatedAt` / `IsDeleted` field |
| Domain events | ❌ Missing | No domain event emission on state changes |
| Guard clauses | ⚠️ Unknown | Need to verify constructor and method guards |
| Invariants | ⚠️ Unknown | Need to verify business rule enforcement |

### 1.3 Recommended Additions to Environment

| Property | Type | Purpose |
|----------|------|---------|
| `UpdatedAt` | `DateTimeOffset?` | Audit — last modification timestamp |
| `CreatedBy` | `string` | Audit — who created |
| `UpdatedBy` | `string?` | Audit — who last modified |
| `DeactivatedAt` | `DateTimeOffset?` | Soft-delete support |
| `DeactivatedBy` | `string?` | Who deactivated |
| `Tags` | `IReadOnlyList<string>` | Flexible categorization |
| `ParentEnvironmentId` | `EnvironmentId?` | Hierarchical relationships |

---

## 2. Existing Child Entities

### 2.1 EnvironmentAccess

**File:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentAccess.cs`

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| `Id` | ID type | ✅ | Primary key |
| `EnvironmentId` | `EnvironmentId` | ✅ | FK to Environment |
| `UserId` | ID type | ✅ | FK to User |
| Additional access control fields | Various | — | Access level, granted by, etc. |

**Boundary Decision:** Stays in Identity & Access module. References `EnvironmentId` by value only (no navigation property across bounded contexts).

### 2.2 EnvironmentPolicy

**File:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentPolicy.cs`

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| `Id` | ID type | ✅ | Primary key |
| `EnvironmentId` | `EnvironmentId` | ✅ | FK to Environment |
| Policy definition fields | Various | — | Policy name, type, configuration |

**Status:** Entity exists but has **no DbSet**, **no table configuration**, **no endpoints**. Needs full implementation.

### 2.3 EnvironmentTelemetryPolicy

**File:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentTelemetryPolicy.cs`

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| `Id` | ID type | ✅ | Primary key |
| `EnvironmentId` | `EnvironmentId` | ✅ | FK to Environment |
| Telemetry settings fields | Various | — | Collection level, retention, sampling |

**Status:** Entity exists but has **no DbSet**, **no table configuration**, **no endpoints**. Needs full implementation.

### 2.4 EnvironmentIntegrationBinding

**File:** `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/EnvironmentIntegrationBinding.cs`

| Property | Type | Required | Notes |
|----------|------|----------|-------|
| `Id` | ID type | ✅ | Primary key |
| `EnvironmentId` | `EnvironmentId` | ✅ | FK to Environment |
| Integration binding fields | Various | — | Integration ID, config, enabled state |

**Status:** Entity exists but has **no DbSet**, **no table configuration**, **no endpoints**. Needs full implementation.

---

## 3. New Entities Required

### 3.1 PromotionPath

Represents an ordered sequence of environments through which changes must flow.

| Property | Type | Required | Purpose |
|----------|------|----------|---------|
| `Id` | `PromotionPathId` | ✅ | Strongly-typed primary key |
| `TenantId` | `TenantId` | ✅ | Multi-tenancy |
| `Name` | `string` | ✅ | Display name (e.g., "Standard Promotion") |
| `Description` | `string` | ❌ | Free-text description |
| `IsDefault` | `bool` | ✅ | Whether this is the default path |
| `IsActive` | `bool` | ✅ | Activation state |
| `Steps` | `IReadOnlyList<PromotionPathStep>` | ✅ | Ordered list of environment steps |
| `CreatedAt` | `DateTimeOffset` | ✅ | Audit |
| `CreatedBy` | `string` | ✅ | Audit |
| `UpdatedAt` | `DateTimeOffset?` | ❌ | Audit |

**PromotionPathStep (Value Object):**

| Property | Type | Purpose |
|----------|------|---------|
| `Order` | `int` | Position in the path (1-based) |
| `EnvironmentId` | `EnvironmentId` | Reference to environment |
| `RequiresApproval` | `bool` | Whether promotion to this step needs approval |
| `MinimumSoakTimeHours` | `int?` | Minimum time in previous env before promotion |

**Invariants:**
- A path must have at least 2 steps
- No duplicate environments in a path
- No circular references
- Last step must be a Production-profile environment
- Only one default path per tenant

### 3.2 EnvironmentBaseline

Represents a captured snapshot of an environment's known-good state.

| Property | Type | Required | Purpose |
|----------|------|----------|---------|
| `Id` | `EnvironmentBaselineId` | ✅ | Strongly-typed primary key |
| `TenantId` | `TenantId` | ✅ | Multi-tenancy |
| `EnvironmentId` | `EnvironmentId` | ✅ | FK to Environment |
| `CapturedAt` | `DateTimeOffset` | ✅ | When baseline was captured |
| `CapturedBy` | `string` | ✅ | Who captured it |
| `Label` | `string` | ❌ | Human-readable label (e.g., "Pre-release v2.1") |
| `Snapshot` | `BaselineSnapshot` | ✅ | The actual baseline data (JSON-serialized) |
| `IsActive` | `bool` | ✅ | Whether this is the current active baseline |
| `SupersededAt` | `DateTimeOffset?` | ❌ | When this baseline was replaced |

**BaselineSnapshot (Value Object):**

| Property | Type | Purpose |
|----------|------|---------|
| `PolicyCount` | `int` | Number of policies at capture time |
| `IntegrationBindingCount` | `int` | Number of integration bindings |
| `TelemetryPolicyHash` | `string` | Hash of telemetry policy config |
| `ConfigurationHash` | `string` | Hash of environment configuration (from Config module) |
| `Metadata` | `Dictionary<string, string>` | Additional key-value metadata |

**Invariants:**
- Only one active baseline per environment at a time
- Setting a new baseline supersedes the previous one

### 3.3 EnvironmentReadinessCheck

Represents a readiness assessment result for an environment.

| Property | Type | Required | Purpose |
|----------|------|----------|---------|
| `Id` | `EnvironmentReadinessCheckId` | ✅ | Strongly-typed primary key |
| `TenantId` | `TenantId` | ✅ | Multi-tenancy |
| `EnvironmentId` | `EnvironmentId` | ✅ | FK to Environment |
| `CheckedAt` | `DateTimeOffset` | ✅ | When the check was performed |
| `CheckedBy` | `string` | ✅ | Who triggered the check |
| `OverallScore` | `int` | ✅ | 0-100 readiness score |
| `Status` | `ReadinessStatus` | ✅ | Ready, NotReady, Warning |
| `Findings` | `IReadOnlyList<ReadinessFinding>` | ✅ | Individual check results |

**ReadinessFinding (Value Object):**

| Property | Type | Purpose |
|----------|------|---------|
| `Category` | `string` | Check category (e.g., "Policies", "Integrations", "Baseline") |
| `Name` | `string` | Check name |
| `Status` | `ReadinessCheckStatus` | Pass, Fail, Warning, Skipped |
| `Message` | `string` | Human-readable result message |
| `Score` | `int` | 0-100 score for this check |

---

## 4. Enums

### 4.1 Existing

| Enum | Values | Status |
|------|--------|--------|
| `EnvironmentCriticality` | Low, Medium, High, Critical | ✅ Complete |
| `EnvironmentProfile` | Development, Validation, Staging, Production, DisasterRecovery | ⚠️ Frontend has more values |

**Issue:** Frontend `EnvironmentsPage.tsx` supports additional profiles: `Sandbox`, `Training`, `UserAcceptanceTesting`, `PerformanceTesting`. These must be added to the backend enum.

### 4.2 New

| Enum | Values | Purpose |
|------|--------|---------|
| `ReadinessStatus` | Ready, NotReady, Warning | Overall readiness assessment |
| `ReadinessCheckStatus` | Pass, Fail, Warning, Skipped | Individual check result |

---

## 5. Value Objects

### 5.1 Existing

| Value Object | File | Status |
|-------------|------|--------|
| `TenantEnvironmentContext` | `Domain/ValueObjects/TenantEnvironmentContext.cs` | ✅ — move to shared kernel |
| `EnvironmentUiProfile` | `Domain/ValueObjects/EnvironmentUiProfile.cs` | ✅ — move to Environment Management |

### 5.2 New

| Value Object | Purpose |
|-------------|---------|
| `PromotionPathStep` | Step in a promotion path (order, env reference, approval requirements) |
| `BaselineSnapshot` | Captured state data for drift comparison |
| `ReadinessFinding` | Individual readiness check result |

---

## 6. Domain Events (New)

| Event | Trigger | Consumers |
|-------|---------|-----------|
| `EnvironmentCreated` | New environment created | Audit, Notifications |
| `EnvironmentUpdated` | Environment properties changed | Audit, Configuration |
| `EnvironmentDeactivated` | Environment soft-deleted | Audit, Change Governance, Operations |
| `PrimaryProductionChanged` | Primary production designation changed | Audit, Change Governance, Operations, Notifications |
| `PromotionPathCreated` | New promotion path defined | Change Governance |
| `PromotionPathUpdated` | Promotion path modified | Change Governance |
| `BaselineSet` | New baseline captured | Audit |
| `BaselineSuperseded` | Baseline replaced by newer one | Audit |
| `DriftDetected` | Environment drifted from baseline | Notifications, Operations |
| `ReadinessAssessed` | Readiness check completed | Change Governance, Notifications |

---

## 7. Aggregate Boundary Summary

```
Environment (Aggregate Root)
├── EnvironmentPolicy (child entity)
├── EnvironmentTelemetryPolicy (child entity)
├── EnvironmentIntegrationBinding (child entity)
├── EnvironmentBaseline (child entity — one active at a time)
└── EnvironmentReadinessCheck (child entity — latest result)

PromotionPath (Separate Aggregate)
└── PromotionPathStep (value object collection)

EnvironmentAccess (Separate Aggregate — stays in Identity module)
```

**Design rationale:** `PromotionPath` is a separate aggregate because it spans multiple environments and has its own lifecycle. `EnvironmentBaseline` and `EnvironmentReadinessCheck` are children of Environment because they are always scoped to a single environment.

---

## 8. Cross-Module References

| This Module References | Referenced Module | Reference Type |
|-----------------------|-------------------|---------------|
| `TenantId` | Identity & Access | Strongly-typed ID (no navigation property) |
| `UserId` (in CapturedBy, CheckedBy) | Identity & Access | String (user identifier) |
| Integration IDs (in bindings) | Integrations | Strongly-typed ID (no navigation property) |

| Other Module References This | Referencing Module | Reference Type |
|-----------------------------|-------------------|---------------|
| `EnvironmentId` | Identity & Access (EnvironmentAccess) | Strongly-typed ID |
| `EnvironmentId` | Configuration (per-env config) | Strongly-typed ID |
| `EnvironmentId` | Change Governance (promotion gates) | Strongly-typed ID |
| `EnvironmentId` | Operational Intelligence (runtime metrics) | Strongly-typed ID |
| `EnvironmentId` | Audit & Compliance (audit entries) | Strongly-typed ID |
