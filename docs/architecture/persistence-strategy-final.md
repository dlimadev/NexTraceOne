# NexTraceOne — Official Persistence Strategy

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation  
> **Sources:** `docs/11-review-modular/modular-review-master.md`, governance reports, database structural audit

---

## 1. Overview

NexTraceOne uses a **dual-database strategy**:

- **PostgreSQL** — Single physical transactional database for all domain data
- **ClickHouse** — Complementary analytical database for high-volume metrics and time-series data

These two databases serve fundamentally different purposes and must never be mixed in responsibility.

---

## 2. PostgreSQL — Transactional Database

### 2.1 Why PostgreSQL is the primary transactional database

- **ACID compliance** — All domain operations require transactional integrity (contract versioning, approval workflows, audit trails, incident state management).
- **Row-Level Security (RLS)** — Multi-tenant isolation is enforced at the database level via `TenantRlsInterceptor`, requiring PostgreSQL's native RLS support.
- **Mature ecosystem** — EF Core with PostgreSQL (Npgsql) provides mature migration support, strongly-typed configurations, and interceptor pipelines.
- **Referential integrity** — Foreign key relationships between entities within and across modules require relational constraints.
- **Encryption** — AES-256-GCM field-level encryption via `EncryptionInterceptor` operates within PostgreSQL transactions.

### 2.2 What data belongs in PostgreSQL

- All domain entities (services, contracts, releases, incidents, configurations, policies, etc.)
- All audit trail data (immutable, with cryptographic hash chain)
- All identity and access data (users, tenants, roles, permissions, sessions)
- All configuration data (definitions, entries, audit log)
- All workflow state (approval decisions, promotion requests, orchestration sessions)
- All notification records (deliveries, preferences)
- All governance data (teams, domains, policies, compliance reports, evidence)

### 2.3 What must NOT go in PostgreSQL

- High-volume telemetry and metrics streams (belongs in ClickHouse)
- Real-time analytics aggregations on millions of events (ClickHouse is optimized for this)
- Raw ingestion execution logs at scale (if volume exceeds transactional DB capacity)
- Product usage event streams at high frequency (ClickHouse handles this better)

### 2.4 Single physical database, multiple DbContexts

**Decision:** All modules share **one physical PostgreSQL database**.

The current architecture uses 4 logical databases (`nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`). The target is **one physical database** with module-level isolation achieved through:

1. **Table prefixes** — Each module's tables use a unique 3-character prefix (e.g., `iam_`, `cat_`, `ctr_`). See `docs/architecture/database-table-prefixes.md`.
2. **Dedicated DbContexts** — Each module has its own `DbContext` extending `NexTraceDbContextBase`, scoped to its prefixed tables.
3. **PostgreSQL RLS** — Tenant isolation continues via `TenantRlsInterceptor` on every DbContext.

This approach provides:

- Module-level isolation without physical database overhead
- Simplified operational management (backup, monitoring, connection pooling)
- Clear ownership boundaries via prefix conventions
- Ability to split into separate databases later if needed (prefix convention enables this)

### 2.5 Migrations per module

Each module maintains its **own migration set**, scoped to its DbContext.

**Current state (20 DbContexts, 29 active migrations, 19 snapshots):**

| Module | DbContexts | Migrations | Status |
|--------|-----------|-----------|--------|
| Identity & Access | 1 (IdentityDbContext) | 2 | Active |
| Audit & Compliance | 1 (AuditDbContext) | 2 | Active |
| Service Catalog | 2 (CatalogGraph, DeveloperPortal) | 2 | Active |
| Contracts | 1 (ContractsDbContext) | 1 | Active (in Catalog project) |
| Change Governance | 4 (ChangeIntelligence, Workflow, Promotion, RulesetGovernance) | 4 | Active |
| Operational Intelligence | 5 (Incident, Automation, Reliability, RuntimeIntelligence, CostIntelligence) | 6 | Active |
| AI & Knowledge | 3 (AiGovernance, AiOrchestration, ExternalAi) | 9 | Active (AiGovernance has 7 — technical debt) |
| Governance | 1 (GovernanceDbContext) | 3 | Active |
| Configuration | 1 (ConfigurationDbContext) | **0** | ⚠️ Uses EnsureCreated |
| Notifications | 1 (NotificationsDbContext) | **0** | ⚠️ Uses EnsureCreated |

**Rules for this phase:**

- No new migrations shall be created.
- No existing migrations shall be deleted.
- Configuration and Notifications must be migrated from `EnsureCreated` to proper migrations in a future phase.
- When the baseline reset occurs, all existing migrations will be replaced by a single baseline migration per module.

### 2.6 Table prefix policy

All tables must conform to the prefix convention defined in `docs/architecture/database-table-prefixes.md`.

- Prefix format: `{3-char-prefix}_` (e.g., `iam_`, `cat_`, `ctr_`)
- Table names after prefix: `snake_case`
- No two modules share a prefix
- Prefix is mandatory for all new tables
- Existing tables will be renamed during the baseline reset

---

## 3. ClickHouse — Analytical Database

### 3.1 Why ClickHouse will be used

- **Columnar storage** — Optimized for analytical queries over large volumes of data.
- **Time-series performance** — Excellent for metrics, telemetry, and event streams with time-based partitioning.
- **Aggregation speed** — Orders of magnitude faster than PostgreSQL for `GROUP BY`, `COUNT`, `SUM`, `AVG` over millions of rows.
- **Cost efficiency** — Handles large data volumes at lower cost than PostgreSQL for read-heavy analytical workloads.
- **Separation of concerns** — Keeps analytical workloads off the transactional database, preserving PostgreSQL performance for domain operations.

### 3.2 Modules that should send data to ClickHouse

| Module | ClickHouse Level | Use Case |
|--------|-----------------|----------|
| **Product Analytics** | REQUIRED | Usage event streams, adoption metrics, persona usage, journey funnels, value tracking |
| **Operational Intelligence** | RECOMMENDED | Runtime metrics time-series, cost analytics, SLA compliance, telemetry |
| **Integrations** | RECOMMENDED | Ingestion execution analytics, connector performance, data freshness |
| **Governance** | RECOMMENDED | Compliance trend analytics, risk scoring time-series, FinOps aggregated reporting |
| AI & Knowledge | OPTIONAL_LATER | Token usage analytics, model performance (when volume justifies) |
| Service Catalog | OPTIONAL_LATER | Service health trend analytics (future) |
| Change Governance | OPTIONAL_LATER | Change frequency analytics (future) |
| Audit & Compliance | OPTIONAL_LATER | Long-term audit analytics over large volumes (future) |

### 3.3 What analytical data goes in ClickHouse

- Event streams (product usage events, integration execution events)
- Time-series metrics (runtime performance, cost tracking, SLA compliance)
- Aggregated analytics (adoption rates, persona usage patterns, funnel conversion)
- Trend analysis data (compliance trends, risk evolution, change frequency)
- High-volume logs that require fast analytical queries

### 3.4 What must NOT go in ClickHouse

- Domain entities that require ACID transactions
- Data that requires UPDATE or DELETE operations (ClickHouse is append-optimized)
- Data protected by PostgreSQL RLS (tenant isolation)
- Configuration data, audit trails, or workflow state
- Any data that serves as source of truth for domain operations
- Referential integrity dependent data (ClickHouse does not enforce foreign keys)

### 3.5 How ClickHouse relates to PostgreSQL

```
PostgreSQL (transactional)          ClickHouse (analytical)
┌─────────────────────┐           ┌──────────────────────┐
│ Domain entities     │           │ Event streams        │
│ Audit trails        │──events──▶│ Time-series metrics  │
│ Workflow state      │           │ Aggregated analytics │
│ Configuration       │           │ Trend data           │
│ Identity/Access     │           │ High-volume logs     │
└─────────────────────┘           └──────────────────────┘
         │                                  │
         │   Source of Truth                │   Derived Analytics
         │   ACID, RLS, FK                  │   Append-only, fast reads
         │   Transactional writes           │   Analytical queries
```

**Data flow:**

1. Transactional operations write to PostgreSQL (source of truth).
2. Events are published via the Outbox pattern.
3. An ingestion pipeline (or background worker) reads events and writes to ClickHouse.
4. Analytical dashboards and reports query ClickHouse directly.
5. ClickHouse never writes back to PostgreSQL.

---

## 4. Mandatory Rules

| Rule | Rationale |
|------|-----------|
| Transactional database ≠ analytical database | Different workloads require different storage engines |
| ClickHouse is never the primary domain store | It lacks ACID, FK constraints, and RLS |
| No mixing of transactional and analytical data in the same responsibility | A table/entity serves one purpose |
| No new migrations in this phase | Model must be finalized before migration baseline reset |
| No `EnsureCreated` usage | Incompatible with migration-based schema evolution |
| Table prefixes are mandatory | Ensures module isolation in shared database |
| Cross-module access via events, not shared DbContexts | Preserves bounded context integrity |

---

## 5. Migration Baseline Reset Plan (Future Phase)

When the data model is finalized per module:

1. Remove all existing migration files.
2. Generate a single baseline migration per module that creates the final schema.
3. Apply table prefix convention to all table names.
4. Replace `EnsureCreated` in Configuration and Notifications with proper migrations.
5. Consolidate multiple DbContexts per module where appropriate (e.g., Change Governance's 4 contexts → 1, Operational Intelligence's 5 contexts → 1-2).
6. Converge the 4 logical databases into 1 physical PostgreSQL database.
7. Validate RLS, audit interceptors, and encryption on all tables.

This reset is a **future phase activity** and must not be started until all module data models are approved.

---

## 6. Reference Documents

| Document | Path |
|----------|------|
| Table Prefix Convention | `docs/architecture/database-table-prefixes.md` |
| Data Placement Matrix | `docs/architecture/module-data-placement-matrix.md` |
| Module Boundary Matrix | `docs/architecture/module-boundary-matrix.md` |
| Database Structural Audit | `docs/11-review-modular/00-governance/database-structural-audit.md` |
| DbContexts Inventory | `docs/11-review-modular/00-governance/dbcontexts-and-persistence-inventory.md` |
