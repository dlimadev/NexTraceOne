# Configuration Module — Seeds and Defaults Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Seeds

### 1.1 Primary Seeder

**File:** `src/modules/configuration/NexTraceOne.Configuration.Infrastructure/Seed/ConfigurationDefinitionSeeder.cs`  
**Lines:** 4,030  
**Method:** `SeedDefaultDefinitionsAsync(ConfigurationDbContext dbContext, CancellationToken ct)`

The seeder creates ~345 `ConfigurationDefinition` records across 8 phases using a C# programmatic approach.

### 1.2 Seed Phases

| Phase | Domain | Count | Key Prefix | SortOrder |
|-------|--------|-------|------------|-----------|
| 0 | Foundation (instance) | ~5 | `instance.*` | 1–10 |
| 1 | Foundation (features, policies) | ~10 | `instance.*`, `policies.*`, `feature.*`, `security.*` | 10–50 |
| 2 | Notifications | 38 | `notifications.*` | 150–201 |
| 3 | Workflow & Promotion | 45 | `workflow.*`, `promotion.*` | 2000–2650 |
| 4 | Governance & Compliance | 44 | `governance.*` | 3000–3540 |
| 5 | Catalog, Contracts & Change | 49 | `catalog.*`, `change.*` | 4000–4690 |
| 6 | Operations, Incidents, FinOps | 53 | `incidents.*`, `operations.*`, `finops.*`, `benchmarking.*` | 5000–5620 |
| 7 | AI & Integrations | 55 | `ai.*`, `integrations.*` | 6000–6670 |

### 1.3 Seed Mechanism

- **Pattern:** `BuildDefaultDefinitions()` constructs a list of `ConfigurationDefinition` entities via `ConfigurationDefinition.Create(...)` factory method.
- **Idempotence:** The `SeedDefaultDefinitionsAsync` method checks existing keys before inserting, making it safe for repeated execution.
- **Validation:** Each definition includes key, display name, description, category, allowed scopes, default value, value type, sensitivity flag, editability, inheritability, validation rules, UI editor type, and sort order.

### 1.4 Seed Execution

**Current:** Seeder is executed via `DevelopmentSeedDataExtensions` — **Development environment only**.

**File:** `src/platform/NexTraceOne.ApiHost/` (startup/seed orchestration)

**Problem:** The 345+ definitions are essential for the platform to function in any environment. Without them, no configuration can be resolved.

---

## 2. Required Base Data

The following data must exist for the module to function:

### 2.1 Configuration Definitions (~345)

**Mandatory.** Without definitions, no configuration entries can be created or resolved. The definitions serve as the schema for the entire platform's configuration.

**All 345 definitions are required in ALL environments** (Development, Staging, Production).

### 2.2 Default Values

Each definition has a `DefaultValue` property that serves as the fallback when no explicit `ConfigurationEntry` exists at any scope. These defaults are part of the definition records and are seeded together.

**Examples of critical defaults:**
- `instance.default_language` → `"en"` (Bootstrap)
- `instance.default_timezone` → `"UTC"` (Bootstrap)
- `notifications.enabled` → `"true"` (Functional)
- `notifications.quiet_hours.start` → `"22:00"` (Functional)
- `security.session_timeout_minutes` → `"480"` (SensitiveOperational)
- `ai.provider.openai.enabled` → `"false"` (Functional)

### 2.3 No Pre-seeded Entries

`ConfigurationEntry` records are **not pre-seeded**. Entries are created by platform administrators at runtime when they need to override default values at specific scopes.

### 2.4 No Pre-seeded Audit Entries

`ConfigurationAuditEntry` records are **not pre-seeded**. They are created automatically when configuration values are changed.

---

## 3. Missing Seeds

| # | Missing Seed | Impact | Priority |
|---|-------------|--------|----------|
| S-01 | **Production seeding not enabled** — seeder only runs in Development | Platform non-functional in Staging/Production without definitions | **CRITICAL** |
| S-02 | No seed verification/reconciliation — if a definition is added in a new phase, existing environments don't get it | Environments fall behind on available configurations | MEDIUM |
| S-03 | No seed versioning — no way to know which phase's seeds have been applied | Difficult to track seed state across environments | LOW |

---

## 4. Redundant Seeds

**None identified.** The seeder has been validated by 251 backend tests that confirm:
- All keys are unique
- All sort orders are unique within their ranges
- All categories are valid
- All value types are valid
- All allowed scopes are non-empty
- All mandatory properties are set

---

## 5. Final Seed Plan

### 5.1 Seed Content

The **345 definitions** organized in 8 phases are the complete and final seed set for the Configuration module. No additional seed data is needed.

### 5.2 Idempotence

The seeder **must be idempotent** in all environments:
- Check if key already exists before inserting
- Do not update existing definitions during seeding (updates are admin operations)
- Safe for repeated execution

The current `SeedDefaultDefinitionsAsync` already implements this pattern correctly.

### 5.3 Versioning

Each seed phase should be treated as a **version of the configuration schema**:

| Phase | Version | Description |
|-------|---------|-------------|
| Phase 0-1 | v1.0 | Foundation: instance, features, policies |
| Phase 2 | v2.0 | Notifications configuration |
| Phase 3 | v3.0 | Workflow & promotion configuration |
| Phase 4 | v4.0 | Governance & compliance configuration |
| Phase 5 | v5.0 | Catalog, contracts & change configuration |
| Phase 6 | v6.0 | Operations, incidents, FinOps configuration |
| Phase 7 | v7.0 | AI & integrations configuration |

Future phases (Phase 8+) will add more definitions as the platform evolves.

### 5.4 Environment Strategy

| Environment | Seed Execution | Mechanism |
|-------------|---------------|-----------|
| Development | ✅ On startup | `DevelopmentSeedDataExtensions` (current) |
| Staging | ✅ On startup | **NEW:** Add staging seed execution |
| Production | ✅ On startup (first run) | **NEW:** Add production seed execution with safety checks |

**Recommendation:** Create a `ConfigurationSeedOrchestrator` that:
1. Runs on application startup in all environments
2. Calls `SeedDefaultDefinitionsAsync` idempotently
3. Logs which definitions were added (new) vs already existed (skipped)
4. Does NOT update existing definitions (admin-only operation)
5. Does NOT delete definitions (backward compatibility)

### 5.5 Seed Testing

The current 251 tests provide excellent coverage of seed data correctness. Additional tests recommended:

| # | Test | Purpose | Priority |
|---|------|---------|----------|
| T-01 | Test idempotence — run seeder twice, verify no duplicates | Confirm safe re-execution | HIGH |
| T-02 | Test phase completeness — verify all key prefixes have definitions | Catch missing definitions | MEDIUM |
| T-03 | Test default value types — verify defaults match declared ValueType | Catch type mismatches | MEDIUM |

---

## 6. Summary

| Aspect | Status | Action |
|--------|--------|--------|
| Seed content (~345 definitions) | ✅ Complete | No changes |
| Seed idempotence | ✅ Implemented | No changes |
| Seed validation (tests) | ✅ 251 tests | Add 3 additional tests |
| Production seeding | ❌ Missing | **CRITICAL:** Enable for all environments |
| Seed versioning | ❌ Missing | LOW: Track phase versions |
| Seed reconciliation | ❌ Missing | MEDIUM: Detect missing definitions in existing environments |
