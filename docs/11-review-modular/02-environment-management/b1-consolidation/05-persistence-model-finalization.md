# Environment Management Module — Persistence Model Finalization

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Persistence State

### 1.1 DbContext

| Aspect | Current | Target |
|--------|---------|--------|
| Context | `IdentityDbContext` | `EnvironmentManagementDbContext` (new) |
| Project | `NexTraceOne.IdentityAccess.Infrastructure` | `NexTraceOne.EnvironmentManagement.Infrastructure` |
| Schema | `dbo` | `env` (or `env_` table prefix) |
| Connection | Shared with Identity tables | Same database, separate schema/prefix |

### 1.2 Current DbSets (in IdentityDbContext)

| DbSet | Entity | Table | Configuration Class |
|-------|--------|-------|-------------------|
| `Environments` | `Environment` | `dbo.Environments` | `EnvironmentConfiguration` |
| `EnvironmentAccesses` | `EnvironmentAccess` | `dbo.EnvironmentAccesses` | `EnvironmentAccessConfiguration` |

### 1.3 Entities WITHOUT DbSets (No Persistence)

| Entity | File Exists | DbSet | Table Config | Status |
|--------|------------|-------|-------------|--------|
| `EnvironmentPolicy` | ✅ | ❌ | ❌ | **Not persisted** — entity is dead code |
| `EnvironmentTelemetryPolicy` | ✅ | ❌ | ❌ | **Not persisted** — entity is dead code |
| `EnvironmentIntegrationBinding` | ✅ | ❌ | ❌ | **Not persisted** — entity is dead code |

**Issue:** Three entities exist in the domain layer but are never persisted. They have no table configuration, no DbSet, and no endpoints. This is either dead code or incomplete implementation.

---

## 2. Current Table Schema: dbo.Environments

**Configuration:** `EnvironmentConfiguration` in `IdentityAccess.Infrastructure/Persistence/Configurations/`

| Column | Type | Nullable | Default | Domain Adherence |
|--------|------|----------|---------|-----------------|
| `Id` | `uniqueidentifier` | NO | `newsequentialid()` | ✅ Strongly-typed `EnvironmentId` |
| `TenantId` | `uniqueidentifier` | NO | — | ✅ Multi-tenancy |
| `Name` | `nvarchar(200)` | NO | — | ✅ |
| `Slug` | `nvarchar(200)` | NO | — | ✅ |
| `SortOrder` | `int` | NO | `0` | ✅ |
| `IsActive` | `bit` | NO | `1` | ✅ |
| `Profile` | `int` | NO | — | ✅ Enum stored as int |
| `Code` | `nvarchar(50)` | YES | — | ✅ |
| `Description` | `nvarchar(500)` | YES | — | ✅ |
| `Criticality` | `int` | NO | — | ✅ Enum stored as int |
| `Region` | `nvarchar(100)` | YES | — | ✅ |
| `IsProductionLike` | `bit` | NO | `0` | ✅ |
| `IsPrimaryProduction` | `bit` | NO | `0` | ✅ |
| `CreatedAt` | `datetimeoffset` | NO | `SYSDATETIMEOFFSET()` | ⚠️ Incomplete — missing UpdatedAt |

### Indices (Current)

| Index | Columns | Type | Adequate? |
|-------|---------|------|----------|
| PK | `Id` | Clustered | ✅ |
| IX_TenantId | `TenantId` | Non-clustered | ✅ Multi-tenancy queries |
| IX_TenantId_Slug | `TenantId, Slug` | Unique | ✅ Slug uniqueness per tenant |
| IX_TenantId_IsPrimaryProduction | `TenantId, IsPrimaryProduction` | Filtered (WHERE IsPrimaryProduction = 1) | ✅ Fast primary prod lookup |

### Missing Indices

| Index | Columns | Type | Justification |
|-------|---------|------|--------------|
| IX_TenantId_Profile | `TenantId, Profile` | Non-clustered | Filter by profile (common query) |
| IX_TenantId_IsActive | `TenantId, IsActive` | Non-clustered | Filter active environments |
| IX_TenantId_Criticality | `TenantId, Criticality` | Non-clustered | Filter by criticality |

---

## 3. Current Table Schema: dbo.EnvironmentAccesses

**Configuration:** `EnvironmentAccessConfiguration` in `IdentityAccess.Infrastructure/Persistence/Configurations/`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | Primary key |
| `EnvironmentId` | `uniqueidentifier` | NO | FK to Environments |
| `UserId` | `uniqueidentifier` | NO | FK to Users |
| Additional columns | Various | — | Access level, grant metadata |

**Note:** This table stays in `IdentityDbContext` after extraction. It will reference `EnvironmentId` as a logical FK (no physical FK constraint across DbContexts if separate schemas).

---

## 4. Target Table Schema: env_environments

**New table name convention:** `env_` prefix for all Environment Management tables.

| Column | Type | Nullable | Default | Change from Current |
|--------|------|----------|---------|-------------------|
| `Id` | `uniqueidentifier` | NO | `newsequentialid()` | — (same) |
| `TenantId` | `uniqueidentifier` | NO | — | — (same) |
| `Name` | `nvarchar(200)` | NO | — | — (same) |
| `Slug` | `nvarchar(200)` | NO | — | — (same) |
| `SortOrder` | `int` | NO | `0` | — (same) |
| `IsActive` | `bit` | NO | `1` | — (same) |
| `Profile` | `int` | NO | — | — (same) |
| `Code` | `nvarchar(50)` | YES | — | — (same) |
| `Description` | `nvarchar(500)` | YES | — | — (same) |
| `Criticality` | `int` | NO | — | — (same) |
| `Region` | `nvarchar(100)` | YES | — | — (same) |
| `IsProductionLike` | `bit` | NO | `0` | — (same) |
| `IsPrimaryProduction` | `bit` | NO | `0` | — (same) |
| `CreatedAt` | `datetimeoffset` | NO | `SYSDATETIMEOFFSET()` | — (same) |
| `UpdatedAt` | `datetimeoffset` | YES | — | **NEW** — audit |
| `CreatedBy` | `nvarchar(200)` | NO | — | **NEW** — audit |
| `UpdatedBy` | `nvarchar(200)` | YES | — | **NEW** — audit |
| `DeactivatedAt` | `datetimeoffset` | YES | — | **NEW** — soft-delete |
| `DeactivatedBy` | `nvarchar(200)` | YES | — | **NEW** — soft-delete |
| `ParentEnvironmentId` | `uniqueidentifier` | YES | — | **NEW** — hierarchy |
| `xmin` | `xid` | NO | — | **NEW** — concurrency token |

---

## 5. New Tables

### 5.1 env_environment_policies

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | PK |
| `TenantId` | `uniqueidentifier` | NO | Multi-tenancy |
| `EnvironmentId` | `uniqueidentifier` | NO | FK to env_environments |
| `PolicyName` | `nvarchar(200)` | NO | Policy identifier |
| `PolicyType` | `nvarchar(100)` | NO | Category of policy |
| `Configuration` | `nvarchar(max)` | YES | JSON policy configuration |
| `IsActive` | `bit` | NO | Activation state |
| `CreatedAt` | `datetimeoffset` | NO | Audit |
| `CreatedBy` | `nvarchar(200)` | NO | Audit |
| `UpdatedAt` | `datetimeoffset` | YES | Audit |
| `xmin` | `xid` | NO | Concurrency token |

**Indices:** PK on Id, IX on (TenantId, EnvironmentId), UNIQUE on (EnvironmentId, PolicyName).

### 5.2 env_environment_telemetry_policies

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | PK |
| `TenantId` | `uniqueidentifier` | NO | Multi-tenancy |
| `EnvironmentId` | `uniqueidentifier` | NO | FK to env_environments |
| `CollectionLevel` | `int` | NO | Enum: None, Basic, Standard, Detailed |
| `RetentionDays` | `int` | NO | How long to retain telemetry |
| `SamplingRate` | `decimal(5,2)` | NO | Sampling percentage (0.00-100.00) |
| `IsActive` | `bit` | NO | Activation state |
| `CreatedAt` | `datetimeoffset` | NO | Audit |
| `UpdatedAt` | `datetimeoffset` | YES | Audit |
| `xmin` | `xid` | NO | Concurrency token |

**Indices:** PK on Id, IX on (TenantId, EnvironmentId), UNIQUE on (EnvironmentId) — one telemetry policy per environment.

### 5.3 env_environment_integration_bindings

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | PK |
| `TenantId` | `uniqueidentifier` | NO | Multi-tenancy |
| `EnvironmentId` | `uniqueidentifier` | NO | FK to env_environments |
| `IntegrationConnectorId` | `uniqueidentifier` | NO | Logical FK to Integrations module |
| `Configuration` | `nvarchar(max)` | YES | JSON binding-specific config |
| `IsActive` | `bit` | NO | Activation state |
| `CreatedAt` | `datetimeoffset` | NO | Audit |
| `UpdatedAt` | `datetimeoffset` | YES | Audit |
| `xmin` | `xid` | NO | Concurrency token |

**Indices:** PK on Id, IX on (TenantId, EnvironmentId), UNIQUE on (EnvironmentId, IntegrationConnectorId).

### 5.4 env_promotion_paths

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | PK |
| `TenantId` | `uniqueidentifier` | NO | Multi-tenancy |
| `Name` | `nvarchar(200)` | NO | Display name |
| `Description` | `nvarchar(500)` | YES | Free-text |
| `IsDefault` | `bit` | NO | Default path for tenant |
| `IsActive` | `bit` | NO | Activation state |
| `Steps` | `nvarchar(max)` | NO | JSON-serialized PromotionPathStep collection |
| `CreatedAt` | `datetimeoffset` | NO | Audit |
| `CreatedBy` | `nvarchar(200)` | NO | Audit |
| `UpdatedAt` | `datetimeoffset` | YES | Audit |
| `xmin` | `xid` | NO | Concurrency token |

**Indices:** PK on Id, IX on (TenantId), UNIQUE filtered on (TenantId) WHERE IsDefault = 1.

### 5.5 env_baselines

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | PK |
| `TenantId` | `uniqueidentifier` | NO | Multi-tenancy |
| `EnvironmentId` | `uniqueidentifier` | NO | FK to env_environments |
| `CapturedAt` | `datetimeoffset` | NO | When captured |
| `CapturedBy` | `nvarchar(200)` | NO | Who captured |
| `Label` | `nvarchar(200)` | YES | Human-readable label |
| `Snapshot` | `nvarchar(max)` | NO | JSON-serialized BaselineSnapshot |
| `IsActive` | `bit` | NO | Current active baseline |
| `SupersededAt` | `datetimeoffset` | YES | When replaced |
| `CreatedAt` | `datetimeoffset` | NO | Audit |
| `xmin` | `xid` | NO | Concurrency token |

**Indices:** PK on Id, IX on (TenantId, EnvironmentId), UNIQUE filtered on (EnvironmentId) WHERE IsActive = 1.

### 5.6 env_readiness_checks

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `Id` | `uniqueidentifier` | NO | PK |
| `TenantId` | `uniqueidentifier` | NO | Multi-tenancy |
| `EnvironmentId` | `uniqueidentifier` | NO | FK to env_environments |
| `CheckedAt` | `datetimeoffset` | NO | When checked |
| `CheckedBy` | `nvarchar(200)` | NO | Who triggered |
| `OverallScore` | `int` | NO | 0-100 |
| `Status` | `int` | NO | Enum: Ready, NotReady, Warning |
| `Findings` | `nvarchar(max)` | NO | JSON-serialized findings |
| `CreatedAt` | `datetimeoffset` | NO | Audit |

**Indices:** PK on Id, IX on (TenantId, EnvironmentId, CheckedAt DESC).

---

## 6. Migration Strategy

### 6.1 Phase 1 — Schema Migration (Non-Breaking)

1. **Create new EnvironmentManagementDbContext** with `env_` table prefix
2. **Create migration to rename existing tables:**
   - `dbo.Environments` → `env_environments`
   - Keep `dbo.EnvironmentAccesses` in Identity (unchanged)
3. **Add new columns** to `env_environments`: `UpdatedAt`, `CreatedBy`, `UpdatedBy`, `DeactivatedAt`, `DeactivatedBy`, `ParentEnvironmentId`
4. **Add `xmin` concurrency token** via `UseXminAsConcurrencyToken()`
5. **Create new tables:** `env_environment_policies`, `env_environment_telemetry_policies`, `env_environment_integration_bindings`, `env_promotion_paths`, `env_baselines`, `env_readiness_checks`

### 6.2 Phase 2 — Data Migration

1. **Backfill `CreatedBy`** with system user for existing records
2. **Verify FK integrity** between `dbo.EnvironmentAccesses.EnvironmentId` and `env_environments.Id`
3. **Update IdentityDbContext** to remove `Environments` DbSet and configuration
4. **Add cross-schema FK** or logical reference documentation

### 6.3 Migration Risks

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Table rename breaks existing queries | **HIGH** | Use views or synonyms as compatibility layer |
| `IdentityDbContext` still references old table | **HIGH** | Must update all EF configurations simultaneously |
| Cross-DbContext FK constraints | **MEDIUM** | Use logical FKs only (no physical constraint across contexts) |
| Data loss during migration | **LOW** | Use ALTER TABLE / sp_rename — no data copy |
| Downtime during migration | **LOW** | Schema changes are online-compatible |

---

## 7. Domain Adherence Checklist

| Criterion | Status | Notes |
|----------|--------|-------|
| Table names reflect domain | ⚠️ After rename | `env_environments`, `env_promotion_paths`, etc. |
| Column names reflect domain | ✅ | Clear, domain-aligned naming |
| Value objects mapped correctly | ❌ | `PromotionPathStep`, `BaselineSnapshot` need JSON serialization config |
| Enums mapped as integers | ✅ | `Profile`, `Criticality` stored as int |
| Strongly-typed IDs | ✅ | `EnvironmentId` configured as value converter |
| Soft-delete implementation | ❌ | Needs `DeactivatedAt` column and global query filter |
| Audit columns | ⚠️ Partial | Only `CreatedAt` — needs `UpdatedAt`, `CreatedBy`, `UpdatedBy` |
| Multi-tenancy (TenantId) | ✅ | Present on all tables |
| RLS configured | ❓ Unknown | Need to verify row-level security setup |
| Concurrency tokens | ❌ | Missing `xmin` on all entities |

---

## 8. Table Summary

| # | Table | Status | Entity | Indices |
|---|-------|--------|--------|---------|
| 1 | `env_environments` | 🔄 Rename from `dbo.Environments` | `Environment` | 4 existing + 3 new |
| 2 | `env_environment_policies` | 🆕 New | `EnvironmentPolicy` | 3 |
| 3 | `env_environment_telemetry_policies` | 🆕 New | `EnvironmentTelemetryPolicy` | 3 |
| 4 | `env_environment_integration_bindings` | 🆕 New | `EnvironmentIntegrationBinding` | 3 |
| 5 | `env_promotion_paths` | 🆕 New | `PromotionPath` | 3 |
| 6 | `env_baselines` | 🆕 New | `EnvironmentBaseline` | 3 |
| 7 | `env_readiness_checks` | 🆕 New | `EnvironmentReadinessCheck` | 2 |

**Total:** 7 tables (1 renamed, 6 new).  
**Remains in Identity:** `dbo.EnvironmentAccesses` (unchanged).
