# Configuration Module — DbContext and Mapping Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. DbContext Location

**File:** `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/ConfigurationDbContext.cs`  
**Lines:** 43  
**Base class:** `NexTraceDbContextBase`

```csharp
public sealed class ConfigurationDbContext(
    DbContextOptions<ConfigurationDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock), IUnitOfWork
{
    public DbSet<ConfigurationDefinition> Definitions => Set<ConfigurationDefinition>();
    public DbSet<ConfigurationEntry> Entries => Set<ConfigurationEntry>();
    public DbSet<ConfigurationAuditEntry> AuditEntries => Set<ConfigurationAuditEntry>();
}
```

**Assessment:** ✅ Clean. Correctly extends `NexTraceDbContextBase` which provides RLS, audit, encryption, and outbox interceptors.

---

## 2. EF Core Configuration Files

### 2.1 ConfigurationDefinitionConfiguration.cs (70 lines)

**File:** `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationDefinitionConfiguration.cs`

**Current mapping:**
- Table: `cfg_definitions` ✅
- PK: `Id` with typed ID conversion ✅
- Unique index on `Key` ✅
- Index on `Category` ✅
- Enum-to-string conversion for `Category` and `ValueType` ✅
- `AllowedScopes` stored as `text[]` with custom conversion ✅
- String length constraints applied ✅

**Issues found:**
| # | Issue | Severity | Correction |
|---|-------|----------|------------|
| C-01 | No `UseXminAsConcurrencyToken()` | HIGH | Add `.UseXminAsConcurrencyToken()` to entity builder |
| C-02 | No index on `sort_order` | LOW | Add `.HasIndex(e => e.SortOrder)` |
| C-03 | No check constraint for `Category` enum | MEDIUM | Add `.HasCheckConstraint("CK_cfg_definitions_category", ...)` |
| C-04 | No check constraint for `ValueType` enum | MEDIUM | Add `.HasCheckConstraint("CK_cfg_definitions_value_type", ...)` |

### 2.2 ConfigurationEntryConfiguration.cs (73 lines)

**File:** `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationEntryConfiguration.cs`

**Current mapping:**
- Table: `cfg_entries` ✅
- PK: `Id` with typed ID conversion ✅
- Index on `Key` ✅
- Index on `Scope` ✅
- Composite unique index on `(Key, Scope, ScopeReferenceId)` ✅
- Enum-to-string conversion for `Scope` ✅
- String length constraints applied ✅
- `DefinitionId` typed ID conversion ✅

**Issues found:**
| # | Issue | Severity | Correction |
|---|-------|----------|------------|
| C-05 | No `UseXminAsConcurrencyToken()` | HIGH | Add `.UseXminAsConcurrencyToken()` to entity builder |
| C-06 | No FK constraint on `DefinitionId` | MEDIUM | Add `.HasOne<ConfigurationDefinition>().WithMany().HasForeignKey(e => e.DefinitionId).OnDelete(DeleteBehavior.Restrict)` |
| C-07 | No index on `DefinitionId` | LOW | Add `.HasIndex(e => e.DefinitionId)` |
| C-08 | No check constraint for `Scope` enum | MEDIUM | Add `.HasCheckConstraint("CK_cfg_entries_scope", ...)` |
| C-09 | No check constraint for `Version >= 1` | LOW | Add `.HasCheckConstraint("CK_cfg_entries_version_positive", ...)` |
| C-10 | No index on `IsActive` | LOW | Add filtered index `.HasIndex(e => e.IsActive).HasFilter("\"is_active\" = true")` |

### 2.3 ConfigurationAuditEntryConfiguration.cs (65 lines)

**File:** `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Persistence/Configurations/ConfigurationAuditEntryConfiguration.cs`

**Current mapping:**
- Table: `cfg_audit_entries` ✅
- PK: `Id` with typed ID conversion ✅
- Index on `Key` ✅
- Index on `ChangedAt` ✅
- Index on `EntryId` ✅
- Enum-to-string conversion for `Scope` ✅
- String length constraints applied ✅

**Issues found:**
| # | Issue | Severity | Correction |
|---|-------|----------|------------|
| C-11 | No FK constraint on `EntryId` | MEDIUM | Add `.HasOne<ConfigurationEntry>().WithMany().HasForeignKey(e => e.EntryId).OnDelete(DeleteBehavior.Restrict)` |
| C-12 | `is_deleted` column exists on audit table | LOW | Consider constraining to always `false` or removing (audit entries should never be deleted) |
| C-13 | No index on `ChangedBy` | LOW | Add `.HasIndex(e => e.ChangedBy)` for user-based audit queries |

---

## 3. EnsureCreated Usage

**Search result:** `EnsureCreated` is **NOT found** in the current Configuration module codebase.

The previous audit reports flagged this as an issue, but the current code does not contain any calls to `Database.EnsureCreated()`. The module has **0 migrations** — the schema may have been created externally or via a mechanism not visible in the module code.

**Resolution:** The absence of both `EnsureCreated` and migrations means the schema must be bootstrapped externally. The baseline migration will resolve this definitively.

---

## 4. Informal Schema Creation

No informal schema creation mechanisms were found:
- No `Database.EnsureCreated()` ❌
- No `Database.Migrate()` calls outside standard EF tooling ❌
- No raw SQL DDL in code ❌
- No schema creation in `DependencyInjection.cs` ❌

The schema initialization method is unclear — it may rely on the shared `nextraceone_operations` database being created by another module's migrations, with ConfigurationDbContext's tables then being created implicitly or via an external process.

---

## 5. Table and Column Naming Review

| Aspect | Status | Notes |
|--------|--------|-------|
| Table prefix `cfg_` | ✅ Already applied | All 3 tables use `cfg_` prefix |
| Column naming `snake_case` | ✅ Correct | Via EF Core PostgreSQL naming convention |
| Enum storage as string | ✅ Correct | Category, ValueType, Scope stored as varchar |
| Array storage | ✅ Correct | `AllowedScopes` as PostgreSQL `text[]` |
| Timestamp type | ✅ Correct | `timestamptz` for all date columns |

---

## 6. Precision/Scale/Length Review

| Column | Current | Expected | Status |
|--------|---------|----------|--------|
| `cfg_definitions.key` | varchar(256) | varchar(256) | ✅ |
| `cfg_definitions.display_name` | varchar(200) | varchar(200) | ✅ |
| `cfg_definitions.description` | varchar(1000) | varchar(1000) | ✅ |
| `cfg_definitions.default_value` | varchar(4000) | varchar(4000) | ✅ |
| `cfg_definitions.validation_rules` | varchar(4000) | varchar(4000) | ✅ |
| `cfg_definitions.ui_editor_type` | varchar(100) | varchar(100) | ✅ |
| `cfg_entries.value` | varchar(4000) | varchar(4000) | ✅ |
| `cfg_entries.structured_value_json` | varchar(8000) | varchar(8000) | ✅ |
| `cfg_entries.scope_reference_id` | varchar(256) | varchar(256) | ✅ |
| `cfg_entries.change_reason` | varchar(500) | varchar(500) | ✅ |
| `cfg_audit_entries.action` | varchar(50) | varchar(50) | ✅ |
| `cfg_audit_entries.change_reason` | varchar(500) | varchar(500) | ✅ |

**All precision/scale/length settings are correct.**

---

## 7. Unmapped Entities

No unmapped entities found. All 3 domain entities have corresponding EF Core configurations and DbSet declarations.

---

## 8. Corrections Summary

### HIGH Priority

| # | Correction | File | Action |
|---|-----------|------|--------|
| C-01 | Add `xmin` concurrency token to Definition | `ConfigurationDefinitionConfiguration.cs` | Add `builder.UseXminAsConcurrencyToken()` |
| C-05 | Add `xmin` concurrency token to Entry | `ConfigurationEntryConfiguration.cs` | Add `builder.UseXminAsConcurrencyToken()` |

### MEDIUM Priority

| # | Correction | File | Action |
|---|-----------|------|--------|
| C-03 | Add check constraint for Category | `ConfigurationDefinitionConfiguration.cs` | Add `HasCheckConstraint` |
| C-04 | Add check constraint for ValueType | `ConfigurationDefinitionConfiguration.cs` | Add `HasCheckConstraint` |
| C-06 | Add FK constraint Definition → Entry | `ConfigurationEntryConfiguration.cs` | Add `HasOne/WithMany/HasForeignKey` |
| C-08 | Add check constraint for Scope | `ConfigurationEntryConfiguration.cs` | Add `HasCheckConstraint` |
| C-11 | Add FK constraint Entry → AuditEntry | `ConfigurationAuditEntryConfiguration.cs` | Add `HasOne/WithMany/HasForeignKey` |

### LOW Priority

| # | Correction | File | Action |
|---|-----------|------|--------|
| C-02 | Add sort_order index | `ConfigurationDefinitionConfiguration.cs` | Add `HasIndex(e => e.SortOrder)` |
| C-07 | Add DefinitionId index | `ConfigurationEntryConfiguration.cs` | Add `HasIndex(e => e.DefinitionId)` |
| C-09 | Add version >= 1 check | `ConfigurationEntryConfiguration.cs` | Add `HasCheckConstraint` |
| C-10 | Add filtered index on IsActive | `ConfigurationEntryConfiguration.cs` | Add `HasIndex(e => e.IsActive).HasFilter(...)` |
| C-12 | Constrain audit is_deleted | `ConfigurationAuditEntryConfiguration.cs` | Add constraint or remove column |
| C-13 | Add ChangedBy index | `ConfigurationAuditEntryConfiguration.cs` | Add `HasIndex(e => e.ChangedBy)` |

---

## 9. Pre-Migration Readiness

Before the baseline migration can be generated, these corrections should be applied to the EF Core configurations:

1. ✅ Table prefix `cfg_` — already in place
2. ⬜ Concurrency tokens (`xmin`) — needs code change
3. ⬜ FK constraints — needs code change
4. ⬜ Check constraints — needs code change
5. ⬜ Additional indexes — needs code change
6. ✅ Column types and lengths — already correct
7. ✅ Enum conversions — already correct
8. ✅ Strongly-typed ID conversions — already correct

Once corrections are applied, a single `InitialCreate` baseline migration can be generated that produces the complete, correct schema.
