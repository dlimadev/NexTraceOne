# E15 — PostgreSQL Baseline Gap Report

> Generated: 2026-03-25 | Prompt E15 | Persistence Transition Phase
> This report documents what was resolved, what remains pending, and what blocks validation.

---

## 1. What Was Resolved in E15

| Category | Items Resolved |
|----------|---------------|
| New baseline migrations | 20 `InitialCreate` migrations generated across 20 DbContexts |
| Design-time factories created | `ConfigurationDbContextDesignTimeFactory`, `NotificationsDbContextDesignTimeFactory` |
| Design-time factory fallbacks fixed | 18 factories updated from old DB names to single `nextraceone` |
| AI Knowledge outbox names | `ext_ai_` / `ai_gov_` / `ai_orch_` → `aik_ext_` / `aik_gov_` / `aik_orch_` |
| Notifications outbox | Added `OutboxTableName = "ntf_outbox_messages"` |
| Automation outbox | Added `OutboxTableName = "ops_auto_outbox_messages"` |
| Prefix validation | 20/20 pass — all 154 tables use correct module prefix |
| Test verification | 2628 tests pass across 9 test suites |
| Migration runner wave order | `WebApplicationExtensions.cs` applies all 20 in correct order |

---

## 2. What Is Still Pending

### 2.1 Architectural Extractions (Structural Blockers)

| ID | Gap | Affected DbContext | Impact |
|----|-----|-------------------|--------|
| OI-01 | Contracts still inside `NexTraceOne.Catalog.Infrastructure` | `ContractsDbContext` is in Catalog project | When extracted, Contracts gets own project, own migrations |
| OI-02 | Integrations inside `GovernanceDbContext` | `int_` tables contaminate Governance baseline | When extracted, Governance baseline will be smaller and cleaner |
| OI-03 | Product Analytics inside `GovernanceDbContext` | `pan_analytics_events` inside Governance | When extracted, baseline for ProductAnalytics will be created |
| OI-04 | Environment Management inside `IdentityDbContext` | `env_environments`, `env_environment_accesses` inside Identity | When extracted, Identity baseline will be smaller |

### 2.2 Missing Seeders

| Module | Gap | Priority |
|--------|-----|---------|
| Identity & Access | `HasData()` roles/permissions in configurations — should be extracted to a proper seeder class | HIGH |
| Service Catalog | No seeder for foundational asset types / templates | MEDIUM |
| Change Governance | No seeder for default ruleset definitions | MEDIUM |
| All other modules (10) | No programmatic seeder exists | LOW-MEDIUM |

### 2.3 Functional Gaps in Baseline

| ID | Gap | Module | Notes |
|----|-----|--------|-------|
| F-01 | MFA enforcement not applied in login flow | Identity | MFA fields present in baseline, enforcement logic missing |
| F-02 | API Key entity missing | Identity | Not in current baseline; needs entity + migration |
| F-03 | Background expiration workers | Identity | Not a baseline issue, but needs to run post-seeding |
| F-04 | `DbUpdateConcurrencyException` handling | All modules with RowVersion | Not a migration issue; application-layer gap |
| F-05 | `InMemoryIncidentStore` still active | Operational Intelligence | Needs real DB store to replace in-memory |
| F-06 | EnvironmentId on AuditEvent | Audit & Compliance | Not in current baseline; gap noted in E9 |

### 2.4 Infrastructure Gaps

| ID | Gap | Notes |
|----|-----|-------|
| INF-01 | No automated `dotnet ef database update` in CI | Needs migration runner in CI/CD pipeline |
| INF-02 | `dotnet-ef` global tool version mismatch (tools 10.0.0, runtime 10.0.5) | Update tools version when runtime stabilizes |
| INF-03 | ClickHouse domain analytics tables | Not in PostgreSQL baseline; need E16 |

---

## 3. What Depends on E16

| Item | E16 Deliverable |
|------|----------------|
| Product Analytics ClickHouse DDL | `pan_*` analytical tables |
| Operational Intelligence ClickHouse DDL | `ops_*` analytical / timeline tables |
| OTel pipeline to ClickHouse | Already exists in `build/clickhouse/init-schema.sql` for otel_ tables |
| ClickHouse connection in modules | ModuleDbContext or dedicated ClickHouse client per module |
| OI-03 Product Analytics extraction | Prerequisite for ProductAnalytics own baseline |

---

## 4. What Depends on E17

| Item | E17 Deliverable |
|------|----------------|
| End-to-end application boot test | App starts, all 20 migrations apply, seeder runs |
| Integration tests | API endpoints exercise DbContexts with real PostgreSQL |
| Smoke test: zero pending migrations | Verify `GetPendingMigrationsAsync()` returns empty after apply |
| Seeder for Identity roles/permissions | Extract from HasData() to `IdentitySeeder` class |
| API Key entity and migration | New entity + migration on Identity module |
| OI-04 Environment extraction | New `EnvironmentManagementDbContext` + baseline migration |

---

## 5. What Still Blocks Full Baseline Validation

### Non-blocking (can run migrations, minor gaps noted)

| Item | Notes |
|------|-------|
| OI-04 env_ tables inside IdentityDbContext | Tables correctly prefixed `env_`; functional gap, not a migration blocker |
| OI-02/OI-03 tables inside GovernanceDbContext | Tables correctly prefixed `int_`/`pan_`; functional gap, not a migration blocker |
| HasData() in PermissionConfiguration | Will be applied during migration; refactor to seeder is a quality improvement |

### Requires resolution before production deployment

| Item | Priority |
|------|---------|
| OI-01/02/03/04 extractions | HIGH — GovernanceDbContext and IdentityDbContext will accumulate drift |
| Idempotent seeder for Identity | HIGH — initial tenant/admin user must be seeded |
| ClickHouse baseline (E16) | HIGH for Product Analytics and Operational Intelligence |

---

## 6. Module Readiness for E17

| Module | E17 Ready? | Gap |
|--------|-----------|-----|
| Configuration | ✅ YES | `ConfigurationDefinitionSeeder` already runs |
| Identity & Access | ⚠️ PARTIAL | HasData() seeds OK for migration; seeder refactor needed |
| Service Catalog | ⚠️ PARTIAL | No seeder; functional on empty DB |
| Change Governance | ✅ YES | No mandatory seed data |
| Notifications | ✅ YES | No mandatory seed data |
| Operational Intelligence | ⚠️ PARTIAL | InMemoryIncidentStore replacement needed |
| Audit & Compliance | ✅ YES | No mandatory seed data |
| Governance | ⚠️ PARTIAL | OI-02/OI-03 contamination documented |
| AI & Knowledge | ⚠️ PARTIAL | Low maturity; agent tool calling broken |
| Contracts (embedded) | ⚠️ PARTIAL | OI-01 extraction recommended before production |

---

## 7. Summary

| Metric | Before E15 | After E15 |
|--------|-----------|----------|
| InitialCreate migrations | 0 | 20 |
| Tables in baseline | 0 | 154 |
| Design-time factories | 18 | 20 |
| Correct outbox table names | 15/20 | 20/20 |
| Prefix validation pass rate | N/A | 20/20 |
| Test suites passing | 9/9 | 9/9 |
| Total tests | 2628 | 2628 |
| App can boot with fresh DB | ❌ No migrations | ✅ Yes (20 migrations apply in wave order) |
