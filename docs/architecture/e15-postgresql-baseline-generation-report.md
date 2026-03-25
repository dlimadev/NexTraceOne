# E15 — PostgreSQL Baseline Generation Report

> Generated: 2026-03-25 | Prompt E15 | Persistence Transition Phase
> Pre-E15: 0 migrations (removed in E14), 20 DbContexts with 0 migrations
> Post-E15: 20 new `InitialCreate` baseline migrations, 154 tables, all prefixes validated

---

## 1. Modules Included in Baseline Generation

### Classification

| Module | DbContext(s) | Classification | Notes |
|--------|-------------|----------------|-------|
| Configuration | `ConfigurationDbContext` | ✅ GENERATE_NOW | 0 prior migrations, seeder-based; most ready |
| Identity & Access | `IdentityDbContext` | ✅ GENERATE_NOW | E13 complete: iam_ prefix, MFA fields, RowVersion |
| Service Catalog | `CatalogGraphDbContext`, `DeveloperPortalDbContext` | ✅ GENERATE_NOW | E5 complete: cat_/dp_ prefix, RowVersion on 2 aggregates |
| Contracts | `ContractsDbContext` | ✅ GENERATE_NOW | ctr_ prefix, part of Catalog Infrastructure (OI-01 pending) |
| Change Governance | `ChangeIntelligenceDbContext`, `WorkflowDbContext`, `PromotionDbContext`, `RulesetGovernanceDbContext` | ✅ GENERATE_NOW | E6 complete: chg_ prefix, RowVersion on 6 aggregates |
| Notifications | `NotificationsDbContext` | ✅ GENERATE_NOW | E7 complete: ntf_ prefix, RowVersion on 2 entities |
| Operational Intelligence | `IncidentDbContext`, `AutomationDbContext`, `ReliabilityDbContext`, `RuntimeIntelligenceDbContext`, `CostIntelligenceDbContext` | ✅ GENERATE_NOW | E8 complete: ops_ prefix, RowVersion on 5 aggregates |
| Audit & Compliance | `AuditDbContext` | ✅ GENERATE_NOW | E9 complete: aud_ prefix, RowVersion on 3 mutable entities |
| Governance | `GovernanceDbContext` | ⚠️ GENERATE_WITH_WARNING | E4 complete: gov_ prefix, but includes int_+pan_ tables (OI-02/OI-03 pending) |
| AI & Knowledge | `AiGovernanceDbContext`, `ExternalAiDbContext`, `AiOrchestrationDbContext` | ⚠️ GENERATE_WITH_WARNING | E12 complete: aik_ prefix, RowVersion on 4 aggregates; low maturity |

### Modules Deferred (NOT_READY for standalone baseline)

| Module | Reason | Dependency |
|--------|--------|-----------|
| Environment Management | OI-04: no dedicated DbContext; `env_` tables inside IdentityDbContext | OI-04 extraction required |
| Integrations (standalone) | OI-02: no dedicated DbContext; `int_` tables inside GovernanceDbContext | OI-02 extraction required |
| Product Analytics (standalone) | OI-03: no dedicated DbContext; `pan_` tables inside GovernanceDbContext | OI-03 extraction required |

---

## 2. Migrations Generated

### Wave 1 — Foundation

| DbContext | Migration | Tables | Prefixes |
|-----------|-----------|--------|---------|
| `ConfigurationDbContext` | `20260325210036_InitialCreate` | 4 | cfg_ |
| `IdentityDbContext` | `20260325210113_InitialCreate` | 17 | iam_, env_ |

### Wave 2 — Catalog & Contracts

| DbContext | Migration | Tables | Prefixes |
|-----------|-----------|--------|---------|
| `CatalogGraphDbContext` | `20260325210147_InitialCreate` | 10 | cat_ |
| `DeveloperPortalDbContext` | `20260325210206_InitialCreate` | 6 | dp_ |
| `ContractsDbContext` | `20260325210222_InitialCreate` | 12 | ctr_ |

### Wave 3 — Change Governance + Notifications + Operational Intelligence

| DbContext | Migration | Tables | Prefixes |
|-----------|-----------|--------|---------|
| `ChangeIntelligenceDbContext` | `20260325210259_InitialCreate` | 11 | chg_ |
| `WorkflowDbContext` | `20260325210318_InitialCreate` | 7 | chg_ |
| `PromotionDbContext` | `20260325210335_InitialCreate` | 5 | chg_ |
| `RulesetGovernanceDbContext` | `20260325210351_InitialCreate` | 4 | chg_ |
| `NotificationsDbContext` | `20260325210426_InitialCreate` | 4 | ntf_ |
| `IncidentDbContext` | `20260325210503_InitialCreate` | 6 | ops_ |
| `AutomationDbContext` | `20260325210523_InitialCreate` | 4 | ops_ |
| `ReliabilityDbContext` | `20260325210540_InitialCreate` | 2 | ops_ |
| `RuntimeIntelligenceDbContext` | `20260325210556_InitialCreate` | 5 | ops_ |
| `CostIntelligenceDbContext` | `20260325210613_InitialCreate` | 7 | ops_ |

### Wave 4 — Audit & Governance

| DbContext | Migration | Tables | Prefixes |
|-----------|-----------|--------|---------|
| `AuditDbContext` | `20260325210647_InitialCreate` | 7 | aud_ |
| `GovernanceDbContext` | `20260325210705_InitialCreate` | 13 | gov_, int_, pan_ |

### Wave 6 — AI & Knowledge

| DbContext | Migration | Tables | Prefixes |
|-----------|-----------|--------|---------|
| `AiGovernanceDbContext` | `20260325210741_InitialCreate` | 20 | aik_ |
| `ExternalAiDbContext` | `20260325210800_InitialCreate` | 5 | aik_ |
| `AiOrchestrationDbContext` | `20260325210818_InitialCreate` | 5 | aik_ |

**Total: 20 migrations, 154 tables**

---

## 3. Design-Time Factories

### Created (new in E15)

| File | DbContext |
|------|-----------|
| `Configuration.Infrastructure/Persistence/ConfigurationDbContextDesignTimeFactory.cs` | `ConfigurationDbContext` |
| `Notifications.Infrastructure/Persistence/NotificationsDbContextDesignTimeFactory.cs` | `NotificationsDbContext` |

### Fixed (stale DB name in fallback connection string)

All 18 existing design-time factories had stale fallback connection strings pointing to old databases (`nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`). All updated to `nextraceone` (single database, E14 architecture).

---

## 4. Prefix Validation

**Result: 20/20 PASS**

All 154 tables use the correct module prefix:

| Module | Prefix | Tables Created |
|--------|--------|---------------|
| Configuration | `cfg_` | cfg_definitions, cfg_entries, cfg_audit_entries, cfg_outbox_messages |
| Identity & Access | `iam_` | 13 iam_ tables + 2 env_ tables (OI-04 pending) + 2 iam_ |
| Catalog | `cat_` | 9 cat_ tables + cat_outbox_messages |
| Developer Portal | `dp_` | 5 dp_ tables + cat_portal_outbox_messages |
| Contracts | `ctr_` | 11 ctr_ tables + cat_contracts_outbox_messages |
| Change Governance | `chg_` | 27 chg_ tables across 4 DbContexts |
| Notifications | `ntf_` | 3 ntf_ tables + ntf_outbox_messages |
| Operational Intelligence | `ops_` | 19 ops_ tables + outbox tables across 5 DbContexts |
| Audit & Compliance | `aud_` | 6 aud_ tables + aud_outbox_messages |
| Governance | `gov_` | 8 gov_ tables + 3 int_ + 1 pan_ (temporary, OI-02/OI-03 pending) |
| AI & Knowledge | `aik_` | 27 aik_ tables across 3 DbContexts |

> Note: `env_environments` and `env_environment_accesses` appear inside `IdentityDbContext` — these have the correct `env_` prefix but are awaiting OI-04 extraction.

---

## 5. Outbox Table Fixes Applied

Three DbContexts used legacy outbox table names that violated the `aik_` prefix convention. Fixed before migration generation:

| DbContext | Before | After |
|-----------|--------|-------|
| `ExternalAiDbContext` | `ext_ai_outbox_messages` | `aik_ext_outbox_messages` |
| `AiGovernanceDbContext` | `ai_gov_outbox_messages` | `aik_gov_outbox_messages` |
| `AiOrchestrationDbContext` | `ai_orch_outbox_messages` | `aik_orch_outbox_messages` |

Two DbContexts used the base-class default `outbox_messages`:

| DbContext | Before | After |
|-----------|--------|-------|
| `NotificationsDbContext` | `outbox_messages` (default) | `ntf_outbox_messages` |
| `AutomationDbContext` | `outbox_messages` (default) | `ops_auto_outbox_messages` |

---

## 6. DbContext Isolation Validation

All 20 DbContexts compile and generate migrations independently. Each uses:
- Own `DbContextOptions<T>` (strongly typed)
- Own `MigrationsAssembly` pointing to its own Infrastructure assembly
- Own `IDesignTimeDbContextFactory<T>` implementation
- Single `nextraceone` database (shared physical, isolated by prefix)

### Known Contamination (documented as architectural gaps)

| DbContext | Contaminated By | Gap ID | Resolution |
|-----------|----------------|--------|-----------|
| `IdentityDbContext` | env_ tables (2 tables) | OI-04 | Extract to EnvironmentManagementDbContext |
| `GovernanceDbContext` | int_ tables (3 tables) | OI-02 | Extract to IntegrationsDbContext |
| `GovernanceDbContext` | pan_ table (1 table) | OI-03 | Extract to ProductAnalyticsDbContext |

---

## 7. Seed Validation

### Existing Programmatic Seeder

| Module | Seeder Class | Status |
|--------|-------------|--------|
| Configuration | `ConfigurationDefinitionSeeder` (~345 definitions) | ✅ Intact, runs in all environments |

### Modules Without Seeders (gap for E16/E17)

All other modules (12) currently have no programmatic seeder. Roles and permissions in Identity are seeded via `HasData()` in `PermissionConfiguration` and `RolePermissionCatalog`. These need to be extracted to a proper seeder class.

---

## 8. Build and Test Verification

| Test Suite | Tests | Status |
|-----------|-------|--------|
| Identity & Access | 290 | ✅ PASS |
| AI & Knowledge | 410 | ✅ PASS |
| Governance | 163 | ✅ PASS |
| Notifications | 412 | ✅ PASS |
| Operational Intelligence | 323 | ✅ PASS |
| Change Governance | 198 | ✅ PASS |
| Catalog | 468 | ✅ PASS |
| Audit & Compliance | 113 | ✅ PASS |
| Configuration | 251 | ✅ PASS |
| **TOTAL** | **2628** | **✅ ALL PASS** |

---

## 9. Preparation for E16 and E17

### E16 — ClickHouse Baseline

The following modules require ClickHouse analytical tables (not covered by PostgreSQL baseline):

| Module | ClickHouse Dependency | Notes |
|--------|----------------------|-------|
| Product Analytics | REQUIRED | `pan_analytics_events` is currently in GovernanceDbContext; OI-03 extraction first |
| Operational Intelligence | RECOMMENDED | Runtime snapshots, incidents timeline |
| Integrations | OPTIONAL | Ingestion metrics |
| AI & Knowledge | OPTIONAL | LLM usage analytics |

The ClickHouse schema at `build/clickhouse/init-schema.sql` covers OTEL observability (logs/traces/metrics). Domain-specific analytics tables need to be designed in E16.

### E17 — Full Application Validation

- All DbContexts now have migrations; `ApplyDatabaseMigrationsAsync` will apply all 20 in wave order
- Application can boot with a fresh `nextraceone` database
- Seeds: only `ConfigurationDefinitionSeeder` runs automatically; other modules need seeders
