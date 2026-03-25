# E14 — Post-Removal Gap Report

> Generated: 2026-03-25 | Prompt E14 | Persistence Transition Phase
> This report documents what was resolved, what remains pending, and what blocks the new baseline.

---

## 1. What Was Resolved in E14

| Category | Items Resolved |
|----------|---------------|
| Legacy migrations removed | ALL 29 migrations across 18 DbContext Migrations directories (75 .cs files) |
| ModelSnapshots removed | ALL 18 ModelSnapshot.cs files (one per DbContext) |
| Designer files removed | ALL 25 migration Designer.cs files |
| EnsureCreated | Confirmed 0 occurrences — already clean |
| Legacy SQL seed files | 7 files archived to `docs/architecture/legacy-seeds/` |
| Database consolidation | 4 databases → 1 `nextraceone` database in all config files |
| init-databases.sql | Updated from 4 CREATE DATABASE to 1 |
| appsettings.json | All 20 connection strings unified to `nextraceone` |
| appsettings.Development.json | All 19 connection strings unified to `nextraceone` |
| docker-compose.yml | 4 env vars per service → 1 `CONNECTION_STRING_NEXTRACEONE` |
| docker-compose.override.yml | Dev override updated to single DB |
| WebApplicationExtensions.cs | Wave-ordered migration runner, E14/E15 documentation |
| Repository tag | `pre-migration-reset-v1` created for rollback safety |
| Build verification | 290 Identity tests pass, 0 build errors |

---

## 2. What Is Still Pending

### 2.1 Blocking for E15 (New Baseline Generation)

| ID | Gap | Impact | Priority |
|----|-----|--------|---------|
| OI-01 | Contracts extraction from Catalog | ContractsDbContext is still embedded in `NexTraceOne.Catalog.Infrastructure` | HIGH |
| OI-02 | Integrations extraction from Governance | `int_` tables still configured inside `GovernanceDbContext` | HIGH |
| OI-03 | Product Analytics extraction from Governance | `pan_` tables still configured inside `GovernanceDbContext` | HIGH |
| OI-04 | Environment Management extraction from Identity | `env_` tables still configured inside `IdentityDbContext` | MEDIUM |
| MI-01 | New baseline migrations not yet generated | All 20 DbContexts have 0 migrations | CRITICAL (E15 deliverable) |
| MI-02 | Notifications baseline missing | `NotificationsDbContext` never had migrations; baseline must be generated from scratch | HIGH |
| MI-03 | Configuration baseline missing | `ConfigurationDbContext` always had 0 migrations; `cfg_` schema not applied via EF | MEDIUM |
| SE-01 | New programmatic seeders not created | 12 modules need seeders to replace legacy SQL files | HIGH |
| SE-02 | Identity seed data (roles/permissions/tenant) not yet in a seeder class | Currently in `HasData()` configuration — must be extracted to a seeder | HIGH |

### 2.2 Functional Gaps Remaining After E14 (Not Blocking E15)

| ID | Gap | Module | Phase |
|----|-----|--------|-------|
| CF-01 | MFA enforcement in login flow | Identity | Future sprint |
| CF-04 | API Key entity + authentication | Identity | Future sprint |
| CF-07 | Background expiration worker for JIT/BreakGlass | Identity | Future sprint |
| AE-03 | Environment extraction as dedicated module | Environment | Future phase |
| OI-InMem | InMemoryIncidentStore replacement with real DB | Operational Intelligence | Future sprint |
| D-01 | DbUpdateConcurrencyException handling | All modules | Future sprint |
| D-02 | ClickHouse pipeline for analytics | OpIntel, ProductAnalytics | E16 |

### 2.3 Infrastructure Gaps

| ID | Gap | Notes |
|----|-----|-------|
| INF-01 | `.env.example` still references old `CONNECTION_STRING_IDENTITY`, `CONNECTION_STRING_CATALOG`, `CONNECTION_STRING_OPERATIONS`, `CONNECTION_STRING_AI` | Needs update to `CONNECTION_STRING_NEXTRACEONE` |
| INF-02 | CI/CD pipeline files (if any) may reference old env vars | Needs audit if pipelines exist |
| INF-03 | Helmchart / Kubernetes manifests (if any) may reference old databases | Needs audit if K8s manifests exist |

---

## 3. What Depends on E15

| Item | E15 Deliverable |
|------|----------------|
| New `iam_` baseline migration for Identity | `dotnet ef migrations add InitialCreate` for IdentityDbContext |
| New `cfg_` baseline migration for Configuration | `dotnet ef migrations add InitialCreate` for ConfigurationDbContext |
| New `cat_` baseline migration for Catalog | `dotnet ef migrations add InitialCreate` for CatalogGraphDbContext |
| New `chg_` baseline migration for Change Governance | 4 migrations for 4 DbContexts |
| New `ops_` baseline migration for Operational Intelligence | 5 migrations for 5 DbContexts |
| New `aud_` baseline migration for Audit & Compliance | `dotnet ef migrations add InitialCreate` for AuditDbContext |
| New `gov_` baseline migration for Governance | `dotnet ef migrations add InitialCreate` for GovernanceDbContext |
| New `ntf_` baseline migration for Notifications | `dotnet ef migrations add InitialCreate` for NotificationsDbContext |
| New `aik_` baseline migration for AI & Knowledge | 3 migrations for 3 DbContexts |
| Programmatic seeders for Identity roles/permissions | Replace HasData() |
| Smoke test: application boots with 0 pending migrations warning | Validate wave order |

---

## 4. What Depends on E16

| Item | E16 Deliverable |
|------|----------------|
| Product Analytics ClickHouse tables | ClickHouse DDL for `pan_*` analytical tables |
| Operational Intelligence ClickHouse tables | ClickHouse DDL for `ops_*` analytical data |
| ClickHouse ingestion pipeline | OTel Collector → ClickHouse write path |
| ClickHouse connection string in modules | Modules with ClickHouse must have dedicated client configuration |

---

## 5. What Still Blocks the New Baseline

### Critical Blockers (must be resolved before generating baselines for affected modules)

| Blocker | Affects | Resolution |
|---------|---------|-----------|
| OI-01: Contracts still inside Catalog | ContractsDbContext | Extract to `src/modules/contracts/` with own DbContext |
| OI-02: Integrations still inside Governance | GovernanceDbContext is polluted | Extract to `src/modules/integrations/` |
| OI-03: ProductAnalytics still inside Governance | GovernanceDbContext is polluted | Extract to `src/modules/productanalytics/` |
| OI-04: EnvironmentManagement inside Identity | IdentityDbContext is larger than expected | Extract to `src/modules/environmentmanagement/` |
| SE-01: Seeders missing | ALL modules | Create programmatic seeders before baseline |
| SE-02: Identity HasData seeds | IdentityDbContext | Convert HasData(roles/permissions/tenant) to seeder class |

### Non-Critical (can generate baseline without resolving, but should be noted)

| Item | Notes |
|------|-------|
| AI & Knowledge low maturity | Baseline can be generated but many features remain broken (agent tool calling) |
| Missing RowVersion on some entities | Not a migration blocker; can be in baseline migration |
| Missing TenantId on some entities | Not a blocker but recommended before baseline |

---

## 6. Modules Ready vs Not Ready for E15 Baseline

| Module | Ready for E15 Baseline? | Remaining Action |
|--------|------------------------|-----------------|
| Configuration | ✅ YES | Just run `dotnet ef migrations add InitialCreate` |
| Identity & Access | ⚠️ YES (with notes) | Run migration; note MFA fields need migration too; convert HasData seeds |
| Service Catalog | ⚠️ YES (with notes) | OI-01 extraction recommended first |
| Change Governance | ✅ YES | 4 DbContexts ready |
| Operational Intelligence | ✅ YES | 5 DbContexts ready (AutomationDbContext is missing from runner — add) |
| Audit & Compliance | ✅ YES | Run `dotnet ef migrations add InitialCreate` |
| Governance | ⚠️ YES (with notes) | OI-02/OI-03 recommended first for clean baseline |
| Notifications | ✅ YES | Run `dotnet ef migrations add InitialCreate` (never had migrations) |
| AI & Knowledge | ⚠️ YES (with notes) | Run 3 migrations; functional gaps remain |
| Environment Management | ❌ NO | OI-04 extraction required first |
| Contracts | ❌ NO | OI-01 extraction required first |
| Integrations | ❌ NO | OI-02 extraction required first |
| Product Analytics | ❌ NO | OI-03 extraction required first |

---

## 7. Recommended E15 Execution Order

1. Generate `InitialCreate` for **Configuration** (Wave 1, already ready)
2. Convert Identity `HasData()` seeds to `IdentitySeeder` class
3. Generate `InitialCreate` for **Identity** (Wave 1, iam_ prefix)
4. Generate `InitialCreate` for **Catalog** + **Contracts** (Wave 2)
5. Generate `InitialCreate` for **Change Governance** (4 DbContexts, Wave 3)
6. Generate `InitialCreate` for **Notifications** (Wave 3, first-ever migration)
7. Generate `InitialCreate` for **Operational Intelligence** (5 DbContexts, Wave 3)
8. Generate `InitialCreate` for **Audit & Compliance** (Wave 4)
9. Generate `InitialCreate` for **Governance** (Wave 4)
10. Generate `InitialCreate` for **AI & Knowledge** (3 DbContexts, Wave 6)
11. Validate smoke test: application boots, seeder runs, all DbContexts have applied migration

**Defer to separate phase:**
- Environment Management (requires OI-04 first)
- Integrations (requires OI-02 first)
- Product Analytics (requires OI-03 first)

---

## 8. Summary

| Metric | Before E14 | After E14 |
|--------|-----------|----------|
| Legacy migrations | 29 | 0 |
| Migration .cs files | 75 | 0 |
| PostgreSQL databases | 4 | 1 |
| Legacy SQL seed files (active) | 7 | 0 (archived) |
| EnsureCreated calls | 0 | 0 |
| Migration runner order (wave) | Unordered | Wave 1→6 |
| Repository tag for rollback | None | `pre-migration-reset-v1` |
| Build status | ✅ | ✅ |
| Identity test suite | 290 pass | 290 pass |
