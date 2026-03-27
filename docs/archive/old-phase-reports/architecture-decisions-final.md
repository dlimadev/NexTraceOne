# NexTraceOne — Final Architectural Decisions

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation  
> **Sources:** `docs/11-review-modular/modular-review-master.md`, module consolidated reviews, governance reports

---

## 1. Product Direction

NexTraceOne is no longer in MVP phase. The product is the **single source of truth for services, contracts, changes, and operational knowledge**.

All mocks, stubs, simulated data, and cosmetic features are being progressively replaced by real implementations. No new mocks or stubs shall be introduced.

---

## 2. Licensing Module — Removed

The Licensing module is **officially removed** from the product scope.

- All dedicated Licensing documentation has been deleted.
- Residual code references (permissions, configurations, breadcrumbs, i18n keys) shall be cleaned in a future dedicated task.
- Existing database migrations that reference Licensing entities **must not be altered** — they are historical records.

---

## 3. Official Module List

The product comprises **13 official modules**:

| # | Module |
|---|--------|
| 01 | Identity & Access |
| 02 | Environment Management |
| 03 | Service Catalog |
| 04 | Contracts |
| 05 | Change Governance |
| 06 | Operational Intelligence |
| 07 | AI & Knowledge |
| 08 | Governance |
| 09 | Configuration |
| 10 | Audit & Compliance |
| 11 | Notifications |
| 12 | Integrations |
| 13 | Product Analytics |

No new modules shall be created outside this list during this phase.

---

## 4. Module Separation Decisions

### 4.1 Contracts — Separate from Catalog

Contracts is an **independent module** with its own bounded context, even though the backend currently resides inside the Catalog project. The frontend already treats Contracts as a separate feature. Physical separation of the backend is a pending refactor.

### 4.2 Integrations — Separate from Governance

Integrations is an **independent module** responsible for connector management and data ingestion. It must not remain inside the Governance module. Physical separation of the backend is a pending refactor.

### 4.3 Product Analytics — Separate from Governance

Product Analytics is an **independent module** responsible for product usage analytics, adoption tracking, and value measurement. It must not remain inside the Governance module. Physical separation of the backend is a pending refactor.

### 4.4 Environment Management — Own Bounded Context

Environment Management must be treated as an **independent bounded context** responsible for environment lifecycle, policies, criticality, and drift detection. It currently shares code and data with Identity & Access. Physical separation is a pending refactor.

### 4.5 AI Core and Agents — Inside AI & Knowledge

AI Core and Agents remain as **internal subdomains** of the AI & Knowledge module. They do not justify independent modules at this stage. The three subdomains are:

- **AI Core** — Model registry, providers, policies, routing, budgets, token governance
- **Agents** — Agent definitions, orchestration sessions, agent executions, tools
- **Knowledge** — Knowledge capture, retrieval, operational context

---

## 5. Persistence Strategy

### 5.1 PostgreSQL — Transactional Database

PostgreSQL is the **single physical transactional database** for the product.

- All domain data, configuration, audit trails, and operational records are stored in PostgreSQL.
- The current logical database separation (nextraceone_identity, nextraceone_catalog, nextraceone_operations, nextraceone_ai) will converge into **one physical PostgreSQL database** with module-level table prefixes for isolation.

### 5.2 ClickHouse — Analytical Database

ClickHouse is the **complementary analytical database**.

- Used for high-volume analytical queries, time-series data, and aggregated metrics.
- It does **not** replace PostgreSQL for any transactional or domain data.
- Modules that benefit from ClickHouse: Product Analytics, Operational Intelligence, Integrations, analytical parts of Governance, and AI usage analytics.

### 5.3 One DbContext Per Module

Each module maintains its **own DbContext** class, extending `NexTraceDbContextBase`.

- DbContexts provide module-level isolation within the shared PostgreSQL database.
- Cross-module data access is done through events, contracts, or explicit integration points — never through shared DbContexts.

### 5.4 Migrations Per Module

Each module maintains its **own set of migrations**.

- Migrations are scoped to the module's DbContext.
- No new migrations shall be created until the final data model is approved per module.
- Existing migrations are preserved as historical records.

### 5.5 Table Prefix Convention

All PostgreSQL tables **must** use a module-specific prefix.

- Prefixes are defined in `docs/architecture/database-table-prefixes.md`.
- The prefix convention ensures no table name collisions in the shared database.
- Existing tables will be renamed to conform to the convention during the migration baseline reset.

---

## 6. Prohibitions

| Rule | Rationale |
|------|-----------|
| No new migrations before final model approval | Prevents accumulation of interim schema changes |
| No use of `EnsureCreated` | Incompatible with migration-based schema management |
| No removal of existing migrations in this phase | Historical records must be preserved until baseline reset |
| No new modules outside the official 13 | Scope control during consolidation |
| No removal of existing functionality (except Licensing) | Preserve working features during restructuring |
| No cross-module direct database access | Modules communicate via events and contracts |
| No ClickHouse as primary domain store | ClickHouse is analytical only |

---

## 7. Architecture Principles Retained

The following architecture patterns are confirmed and must be preserved:

- **Clean Architecture + DDD** — Domain layer has no infrastructure dependencies
- **CQRS via MediatR** — Command/Query separation with pipeline behaviors
- **Multi-tenancy via PostgreSQL RLS** — `TenantRlsInterceptor` on every DbContext
- **Automatic Audit** — `AuditInterceptor` provides CreatedAt/By, UpdatedAt/By on all entities
- **Field Encryption** — AES-256-GCM via `[EncryptedField]` attribute and `EncryptionInterceptor`
- **Outbox Pattern** — Eventual consistency for cross-module events via `OutboxInterceptor`
- **Soft Delete** — `IsDeleted` flag with global query filters
- **Strongly-typed IDs** — All entity identifiers use strongly-typed wrappers
- **Persona-aware UX** — Frontend segmented by 7 personas (Engineer, Tech Lead, Architect, Product, Executive, Platform Admin, Auditor)
- **i18n mandatory** — All user-facing text from i18n resources (4 locales: en, pt-PT, pt-BR, es)

---

## 8. Reference Documents

| Document | Path |
|----------|------|
| Module Boundary Matrix | `docs/architecture/module-boundary-matrix.md` |
| Module Frontier Decisions | `docs/architecture/module-frontier-decisions.md` |
| Persistence Strategy | `docs/architecture/persistence-strategy-final.md` |
| Table Prefixes | `docs/architecture/database-table-prefixes.md` |
| Data Placement Matrix | `docs/architecture/module-data-placement-matrix.md` |
| Phase A Open Items | `docs/architecture/phase-a-open-items.md` |
| Modular Review Master | `docs/11-review-modular/modular-review-master.md` |
