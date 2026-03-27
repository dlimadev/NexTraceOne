# Configuration Module — Persistence Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Tables

| Table (current) | Table (target with prefix) | Entity | DbSet |
|----------------|---------------------------|--------|-------|
| `cfg_definitions` | `cfg_definitions` | ConfigurationDefinition | `Definitions` |
| `cfg_entries` | `cfg_entries` | ConfigurationEntry | `Entries` |
| `cfg_audit_entries` | `cfg_audit_entries` | ConfigurationAuditEntry | `AuditEntries` |
| `cfg_outbox_messages` | `cfg_outbox_messages` | (OutboxMessage) | (via NexTraceDbContextBase) |

**Note:** The current EF Core configurations already use the `cfg_` prefix. No table renaming is required.

---

## 2. Entity → Table Mapping

### 2.1 `cfg_definitions`

| Column | Type | Nullable | Max Length | Default | Notes |
|--------|------|----------|-----------|---------|-------|
| `id` | `uuid` | NOT NULL | — | — | PK, strongly-typed `ConfigurationDefinitionId` |
| `key` | `varchar(256)` | NOT NULL | 256 | — | Unique, immutable |
| `display_name` | `varchar(200)` | NOT NULL | 200 | — | |
| `description` | `varchar(1000)` | NULL | 1000 | — | |
| `category` | `varchar(50)` | NOT NULL | 50 | — | Enum as string: Bootstrap, SensitiveOperational, Functional |
| `allowed_scopes` | `text[]` | NOT NULL | — | — | PostgreSQL array of scope enum strings |
| `default_value` | `varchar(4000)` | NULL | 4000 | — | |
| `value_type` | `varchar(50)` | NOT NULL | 50 | — | Enum as string: String, Integer, Decimal, Boolean, Json, StringList |
| `is_sensitive` | `boolean` | NOT NULL | — | `false` | |
| `is_editable` | `boolean` | NOT NULL | — | `true` | |
| `is_inheritable` | `boolean` | NOT NULL | — | `true` | |
| `validation_rules` | `varchar(4000)` | NULL | 4000 | — | JSON schema |
| `ui_editor_type` | `varchar(100)` | NULL | 100 | — | |
| `sort_order` | `integer` | NOT NULL | — | `0` | |
| `tenant_id` | `uuid` | NOT NULL | — | — | RLS column (from AuditableEntity) |
| `created_at` | `timestamptz` | NOT NULL | — | — | Audit column |
| `created_by` | `varchar(200)` | NOT NULL | 200 | — | Audit column |
| `updated_at` | `timestamptz` | NULL | — | — | Audit column |
| `updated_by` | `varchar(200)` | NULL | 200 | — | Audit column |
| `is_deleted` | `boolean` | NOT NULL | — | `false` | Soft delete |
| `xmin` | `xid` | — | — | — | **NEW:** PostgreSQL system column for concurrency |

### 2.2 `cfg_entries`

| Column | Type | Nullable | Max Length | Default | Notes |
|--------|------|----------|-----------|---------|-------|
| `id` | `uuid` | NOT NULL | — | — | PK, strongly-typed `ConfigurationEntryId` |
| `definition_id` | `uuid` | NOT NULL | — | — | FK to `cfg_definitions.id` |
| `key` | `varchar(256)` | NOT NULL | 256 | — | Denormalized from definition |
| `scope` | `varchar(50)` | NOT NULL | 50 | — | Enum as string |
| `scope_reference_id` | `varchar(256)` | NULL | 256 | — | TenantId, EnvironmentId, etc. |
| `value` | `varchar(4000)` | NULL | 4000 | — | Plain text value |
| `structured_value_json` | `varchar(8000)` | NULL | 8000 | — | JSON typed values |
| `is_encrypted` | `boolean` | NOT NULL | — | `false` | |
| `is_sensitive` | `boolean` | NOT NULL | — | `false` | |
| `is_active` | `boolean` | NOT NULL | — | `true` | Soft-deactivation |
| `version` | `integer` | NOT NULL | — | `1` | Auto-incremented |
| `effective_from` | `timestamptz` | NULL | — | — | Temporal window start |
| `effective_to` | `timestamptz` | NULL | — | — | Temporal window end |
| `change_reason` | `varchar(500)` | NULL | 500 | — | |
| `created_by` | `varchar(200)` | NOT NULL | 200 | — | |
| `updated_by` | `varchar(200)` | NULL | 200 | — | |
| `tenant_id` | `uuid` | NOT NULL | — | — | RLS column |
| `created_at` | `timestamptz` | NOT NULL | — | — | Audit column |
| `updated_at` | `timestamptz` | NULL | — | — | Audit column |
| `is_deleted` | `boolean` | NOT NULL | — | `false` | Soft delete |
| `xmin` | `xid` | — | — | — | **NEW:** PostgreSQL concurrency |

### 2.3 `cfg_audit_entries`

| Column | Type | Nullable | Max Length | Default | Notes |
|--------|------|----------|-----------|---------|-------|
| `id` | `uuid` | NOT NULL | — | — | PK, strongly-typed `ConfigurationAuditEntryId` |
| `entry_id` | `uuid` | NOT NULL | — | — | FK to `cfg_entries.id` |
| `key` | `varchar(256)` | NOT NULL | 256 | — | Denormalized |
| `scope` | `varchar(50)` | NOT NULL | 50 | — | Enum as string |
| `scope_reference_id` | `varchar(256)` | NULL | 256 | — | |
| `action` | `varchar(50)` | NOT NULL | 50 | — | Created, Updated, Activated, Deactivated, Removed |
| `previous_value` | `varchar(4000)` | NULL | 4000 | — | |
| `new_value` | `varchar(4000)` | NULL | 4000 | — | |
| `previous_version` | `integer` | NULL | — | — | |
| `new_version` | `integer` | NOT NULL | — | — | |
| `changed_by` | `varchar(200)` | NOT NULL | 200 | — | |
| `changed_at` | `timestamptz` | NOT NULL | — | — | |
| `change_reason` | `varchar(500)` | NULL | 500 | — | |
| `is_sensitive` | `boolean` | NOT NULL | — | `false` | |
| `tenant_id` | `uuid` | NOT NULL | — | — | RLS column |
| `created_at` | `timestamptz` | NOT NULL | — | — | Audit column |
| `created_by` | `varchar(200)` | NOT NULL | 200 | — | Audit column |
| `updated_at` | `timestamptz` | NULL | — | — | Audit column |
| `updated_by` | `varchar(200)` | NULL | 200 | — | Audit column |
| `is_deleted` | `boolean` | NOT NULL | — | `false` | Soft delete |

---

## 3. Primary Keys

| Table | PK Column | Type |
|-------|-----------|------|
| `cfg_definitions` | `id` | `uuid` (ConfigurationDefinitionId) |
| `cfg_entries` | `id` | `uuid` (ConfigurationEntryId) |
| `cfg_audit_entries` | `id` | `uuid` (ConfigurationAuditEntryId) |

---

## 4. Foreign Keys

| Table | Column | References | On Delete |
|-------|--------|-----------|-----------|
| `cfg_entries` | `definition_id` | `cfg_definitions.id` | RESTRICT |
| `cfg_audit_entries` | `entry_id` | `cfg_entries.id` | RESTRICT |

**Note:** Current EF Core configurations do not declare explicit FK navigation properties — relationships are via ID references. The migration should add FK constraints for referential integrity.

---

## 5. Indexes

### Current Indexes (from EF Core configurations)

| Table | Index | Columns | Unique | Notes |
|-------|-------|---------|--------|-------|
| `cfg_definitions` | `IX_cfg_definitions_key` | `key` | YES | Ensures unique keys |
| `cfg_definitions` | `IX_cfg_definitions_category` | `category` | NO | Filter by category |
| `cfg_entries` | `IX_cfg_entries_key` | `key` | NO | Lookup by key |
| `cfg_entries` | `IX_cfg_entries_scope` | `scope` | NO | Filter by scope |
| `cfg_entries` | `IX_cfg_entries_key_scope_scopeRefId` | `key, scope, scope_reference_id` | YES | Composite unique — one value per key/scope combo |
| `cfg_audit_entries` | `IX_cfg_audit_entries_key` | `key` | NO | History by key |
| `cfg_audit_entries` | `IX_cfg_audit_entries_changed_at` | `changed_at` | NO | Temporal queries |
| `cfg_audit_entries` | `IX_cfg_audit_entries_entry_id` | `entry_id` | NO | History by entry |

### Indexes to Add

| Table | Index | Columns | Unique | Rationale |
|-------|-------|---------|--------|-----------|
| `cfg_definitions` | `IX_cfg_definitions_sort_order` | `sort_order` | NO | Optimize ORDER BY sort_order queries |
| `cfg_entries` | `IX_cfg_entries_definition_id` | `definition_id` | NO | FK index for join performance |
| `cfg_entries` | `IX_cfg_entries_is_active` | `is_active` | NO | Filter active entries (filtered index candidate) |
| `cfg_audit_entries` | `IX_cfg_audit_entries_changed_by` | `changed_by` | NO | Audit queries by user |

---

## 6. Unique Constraints

| Table | Constraint | Columns | Notes |
|-------|-----------|---------|-------|
| `cfg_definitions` | `UQ_cfg_definitions_key` | `key` | Already exists as unique index |
| `cfg_entries` | `UQ_cfg_entries_key_scope_scopeRefId` | `key, scope, scope_reference_id` | Already exists as composite unique index |

---

## 7. Check Constraints (New)

| Table | Constraint | Expression | Rationale |
|-------|-----------|-----------|-----------|
| `cfg_definitions` | `CK_cfg_definitions_category` | `category IN ('Bootstrap', 'SensitiveOperational', 'Functional')` | Enforce enum at DB level |
| `cfg_definitions` | `CK_cfg_definitions_value_type` | `value_type IN ('String', 'Integer', 'Decimal', 'Boolean', 'Json', 'StringList')` | Enforce enum at DB level |
| `cfg_entries` | `CK_cfg_entries_scope` | `scope IN ('System', 'Tenant', 'Environment', 'Role', 'Team', 'User')` | Enforce enum at DB level |
| `cfg_entries` | `CK_cfg_entries_version_positive` | `version >= 1` | Version must be at least 1 |

---

## 8. Transversal Columns

All tables inherit these from `NexTraceDbContextBase` / `AuditableEntity`:

| Column | Type | Source | Notes |
|--------|------|--------|-------|
| `tenant_id` | `uuid` | RLS (`TenantRlsInterceptor`) | Required for multi-tenancy |
| `created_at` | `timestamptz` | `AuditInterceptor` | Auto-set on creation |
| `created_by` | `varchar(200)` | `AuditInterceptor` | Auto-set on creation |
| `updated_at` | `timestamptz` | `AuditInterceptor` | Auto-set on update |
| `updated_by` | `varchar(200)` | `AuditInterceptor` | Auto-set on update |
| `is_deleted` | `boolean` | Soft-delete | Global query filter excludes `is_deleted = true` |

---

## 9. Soft Delete Policy

All 3 tables support soft delete via the `is_deleted` column with global query filters.

- `ConfigurationDefinition`: Soft-deleted definitions should no longer appear in definition listings.
- `ConfigurationEntry`: Soft-deleted entries are excluded from scope resolution.
- `ConfigurationAuditEntry`: Audit entries should **never** be soft-deleted (immutable audit trail). Consider adding a constraint or removing the `is_deleted` column from this table.

**Recommendation:** For `cfg_audit_entries`, add `DEFAULT false` and a check constraint `CK_cfg_audit_entries_not_deletable` to enforce `is_deleted = false` always, or remove the column entirely in the baseline migration.

---

## 10. RowVersion / Concurrency

**Current state:** No concurrency control. Conflicts resolved via application-level `UpdatedAt` checks only.

**Target:** Add PostgreSQL `xmin` as concurrency token on:
- `ConfigurationDefinition` — admins may edit definitions concurrently
- `ConfigurationEntry` — multiple scopes may be edited concurrently

**Implementation:** Use `UseXminAsConcurrencyToken()` in EF Core configuration for both entities.

**Not needed on:** `ConfigurationAuditEntry` — immutable, append-only, no concurrent updates.

---

## 11. TenantId and EnvironmentId

| Column | Required | Source | Notes |
|--------|----------|--------|-------|
| `tenant_id` | YES (all tables) | `TenantRlsInterceptor` | Multi-tenancy isolation |
| `environment_id` | NO | Not stored on entities | Environment context is in `ScopeReferenceId` when `Scope = Environment` |

**Note:** The module does not store `EnvironmentId` as a dedicated column. Instead, when a `ConfigurationEntry` has `Scope = Environment`, the `ScopeReferenceId` contains the environment identifier. This is correct — the module does not need a dedicated environment column.

---

## 12. Audit Columns

All tables have standard audit columns via `AuditInterceptor`:
- `created_at` / `created_by` — set automatically on INSERT
- `updated_at` / `updated_by` — set automatically on UPDATE

Additionally, `ConfigurationEntry` has its own `CreatedBy` and `UpdatedBy` properties which are set by the domain logic (not by the interceptor). This creates a **duplication** — the interceptor-provided audit columns and the entity's own audit properties serve the same purpose.

**Recommendation:** In the baseline migration, evaluate whether to remove the entity-level `CreatedBy`/`UpdatedBy` in favor of the interceptor-provided ones, or vice versa. For now, both exist and provide correct data.

---

## 13. Divergences Between Current and Target Model

| # | Divergence | Current | Target | Priority |
|---|-----------|---------|--------|----------|
| 1 | No migrations | Schema via EnsureCreated (or manual) | Proper EF Core migration baseline | HIGH |
| 2 | No RowVersion | No concurrency control | `xmin` concurrency token on Definition + Entry | HIGH |
| 3 | No FK constraints in DB | Relationships via ID references only | Explicit FK constraints | MEDIUM |
| 4 | No check constraints | Enum validation app-level only | DB-level check constraints | MEDIUM |
| 5 | No sort_order index | Definition ordering in app layer | Index on `sort_order` | LOW |
| 6 | No definition_id FK index | Join performance relies on PK | Index on `cfg_entries.definition_id` | LOW |
| 7 | Audit entry soft-delete | `is_deleted` column exists on audit table | Remove or constrain to always false | LOW |
| 8 | Duplicate audit columns | Entity + Interceptor both track created/updated by | Evaluate consolidation | LOW |

---

## 14. Summary

The persistence model is **sound and well-structured**. The `cfg_` prefix is already applied. Table and column names follow conventions. The main gaps are:

1. **No formal migration** — must be created as baseline
2. **No concurrency tokens** — add `xmin`
3. **No FK constraints in database** — add during baseline
4. **No check constraints** — add for enum columns

The model is ready for the future baseline migration with these additions.
