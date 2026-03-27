# NexTraceOne — Phase A Open Items

> **Status:** ACTIVE  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation  
> **Purpose:** Items not yet reflected in code that block the next phase

---

## Open Items Registry

### OI-01 — Contracts backend physically inside Catalog

| Attribute | Value |
|-----------|-------|
| **Item** | Contracts domain, application, infrastructure, and API layers reside inside `src/modules/catalog/` |
| **Why it blocks** | Cannot establish Contracts as an independent bounded context with its own DbContext and migrations while it shares the Catalog project. Table prefix `ctr_` cannot be applied independently. |
| **Modules affected** | Contracts, Service Catalog |
| **Priority** | **HIGH** |
| **Action** | Extract all Contracts-related code from `src/modules/catalog/NexTraceOne.Catalog.*/Contracts/` into a new `src/modules/contracts/` project with Domain, Application, Infrastructure, API, and Contracts layers. Maintain `ServiceId` as cross-module reference. |
| **Evidence** | `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/`, `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/`, `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/`, `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/` |

---

### OI-02 — Integrations backend physically inside Governance

| Attribute | Value |
|-----------|-------|
| **Item** | Integrations entities (`IntegrationConnector`, `IngestionSource`), endpoints (`IntegrationHubEndpointModule`), and features reside inside `src/modules/governance/` |
| **Why it blocks** | Cannot establish Integrations as an independent module with its own `IntegrationsDbContext`. Table prefix `int_` cannot be applied independently. Governance `GovernanceDbContext` remains overloaded. |
| **Modules affected** | Integrations, Governance |
| **Priority** | **HIGH** |
| **Action** | Extract all Integration-related entities, endpoints, features, and contracts from `src/modules/governance/` into a new `src/modules/integrations/` project. Create `IntegrationsDbContext`. |
| **Evidence** | `src/modules/governance/NexTraceOne.Governance.Domain/Entities/IntegrationConnector.cs`, `src/modules/governance/NexTraceOne.Governance.API/Endpoints/IntegrationHubEndpointModule.cs`, `src/modules/governance/NexTraceOne.Governance.Application/Features/ListIntegrationConnectors/` |

---

### OI-03 — Product Analytics backend physically inside Governance

| Attribute | Value |
|-----------|-------|
| **Item** | Product Analytics entities (`AnalyticsEvent`), endpoints (`ProductAnalyticsEndpointModule`), and features reside inside `src/modules/governance/` |
| **Why it blocks** | Cannot establish Product Analytics as an independent module with its own `ProductAnalyticsDbContext`. Table prefix `pan_` cannot be applied independently. ClickHouse integration cannot be scoped to this module. |
| **Modules affected** | Product Analytics, Governance |
| **Priority** | **HIGH** |
| **Action** | Extract all analytics-related entities, endpoints, features, and repositories from `src/modules/governance/` into a new `src/modules/productanalytics/` project. Create `ProductAnalyticsDbContext`. |
| **Evidence** | `src/modules/governance/NexTraceOne.Governance.Domain/Entities/AnalyticsEvent.cs`, `src/modules/governance/NexTraceOne.Governance.API/Endpoints/ProductAnalyticsEndpointModule.cs`, `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs` |

---

### OI-04 — Environment Management has no dedicated backend module

| Attribute | Value |
|-----------|-------|
| **Item** | Environment Management functionality is dispersed inside `src/modules/identityaccess/` with no dedicated module, DbContext, or migration set |
| **Why it blocks** | Cannot apply table prefix `env_` independently. Cannot manage environment lifecycle without coupling to Identity. Other modules (Change Governance, Configuration) depend on environment data but cannot reference an independent module. |
| **Modules affected** | Environment Management, Identity & Access, Configuration, Change Governance, Operational Intelligence |
| **Priority** | **MEDIUM** |
| **Action** | Create `src/modules/environmentmanagement/` with Domain, Application, Infrastructure, and API layers. Extract environment-related entities (`Environment`, `EnvironmentPolicy`, `EnvironmentProfile`) from `IdentityDbContext` into a new `EnvironmentDbContext`. |
| **Evidence** | No `src/modules/environmentmanagement/` directory exists. Environment entities in `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/Entities/`. |

---

### OI-05 — ~~Licensing residues still present in code~~ ✅ CLOSED (P12.1)

| Attribute | Value |
|-----------|-------|
| **Item** | Licensing module was removed from scope; all active code references cleaned in P12.1 |
| **Status** | ✅ **RESOLVED** — P12.1 removed all active licensing residues from `RolePermissionCatalog.cs`, `PermissionConfiguration.cs`, `CreateDelegation.cs`, `Breadcrumbs.tsx`, `navigation.ts`, `en.json`, `DeveloperPortalPage.tsx`, and `MfaPolicy.cs`. |
| **Modules affected** | Identity & Access, Frontend (i18n, Breadcrumbs, DeveloperPortalPage) |
| **Priority** | **CLOSED** |
| **Resolution** | All 7 LIC items from `licensing-residue-final-audit.md` addressed. See `p12-1-licensing-residue-removal-report.md`. |

---

### OI-06 — 3 broken Contracts frontend routes

| Attribute | Value |
|-----------|-------|
| **Item** | Three Contracts pages exist in the codebase but their routes are not registered in `App.tsx`: `/contracts/governance` (ContractGovernancePage), `/contracts/spectral` (SpectralRulesetManagerPage), `/contracts/canonical` (CanonicalEntityCatalogPage) |
| **Why it blocks** | Sidebar links to these pages result in 404 errors. Contracts module appears incomplete to users. 3 Spectral/Canonical features are inaccessible. |
| **Modules affected** | Contracts (frontend) |
| **Priority** | **HIGH** |
| **Action** | Add lazy imports and `ProtectedRoute` entries in `App.tsx` for the 3 missing routes. The page components and hooks already exist. |
| **Evidence** | `src/frontend/src/App.tsx` (missing routes), `src/frontend/src/components/shell/AppSidebar.tsx` (sidebar links exist) |

---

### OI-07 — Configuration and Notifications have 0 migrations

| Attribute | Value |
|-----------|-------|
| **Item** | `ConfigurationDbContext` and `NotificationsDbContext` have 0 EF Core migrations. Code search confirms `EnsureCreated` is **not present** in the current codebase — the schema initialization mechanism is unclear (possibly external or manual). Regardless, both modules lack proper migration files. |
| **Why it blocks** | Without migrations, schema cannot evolve incrementally. Cannot apply table prefixes, add concurrency tokens, FK constraints, or check constraints. Blocks migration baseline reset. |
| **Modules affected** | Configuration, Notifications |
| **Priority** | **MEDIUM** |
| **Action** | Create initial baseline migrations for both modules once their data models are finalized. Configuration model is finalized (see `docs/11-review-modular/09-configuration/persistence-model-finalization.md`). Notifications model needs review. |
| **Evidence** | `docs/11-review-modular/00-governance/database-structural-audit.md` reports 0 migrations for both. Codebase search for `EnsureCreated` returns no matches in `src/`. |

---

### OI-08 — 4 logical databases must converge to 1 physical PostgreSQL database

| Attribute | Value |
|-----------|-------|
| **Item** | Current architecture uses 4 logical databases (`nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`). Target is 1 physical PostgreSQL database with table prefixes. |
| **Why it blocks** | Table prefix convention assumes a shared database. Cross-module referential integrity is impossible across separate databases. Connection management is more complex with 4 databases. |
| **Modules affected** | All modules |
| **Priority** | **MEDIUM** |
| **Action** | Plan convergence as part of the migration baseline reset phase. All DbContexts must be reconfigured to point to the same physical database. Connection strings must be unified. |
| **Evidence** | `docker-compose.yml`, `docker-compose.override.yml`, module `appsettings.json` files reference different database names. |

---

### OI-09 — Governance module is a catch-all with 15+ subdomains

| Attribute | Value |
|-----------|-------|
| **Item** | Governance module contains 25 pages, 18 endpoint modules, and 15+ subdomains including functionality that should belong to Integrations, Product Analytics, and other dedicated modules |
| **Why it blocks** | After extracting Integrations and Product Analytics (OI-02, OI-03), Governance must be reviewed to ensure remaining responsibilities are coherent. Current single permission `governance:read` is too broad for 25 pages. |
| **Modules affected** | Governance |
| **Priority** | **MEDIUM** |
| **Action** | After OI-02 and OI-03 extractions, audit remaining Governance subdomains. Define sub-permissions for different Governance areas (compliance, risk, FinOps, teams, domains). |
| **Evidence** | `src/modules/governance/NexTraceOne.Governance.API/Endpoints/` (18 endpoint modules), `src/frontend/src/features/governance/` (25 pages) |

---

### OI-10 — AI & Knowledge backend at 25% maturity with 70% frontend

| Attribute | Value |
|-----------|-------|
| **Item** | AI & Knowledge has the largest perception gap: frontend is 70% mature (11 pages, rich UI) but backend is only 25% mature (tools declared but not runtime-connected, streaming not implemented, retrieval possibly stubs) |
| **Why it blocks** | UI suggests capabilities that do not exist in the backend. Users will experience broken or non-functional AI features. AI tool runtime, streaming, and RAG must be implemented before this module can be considered production-ready. |
| **Modules affected** | AI & Knowledge |
| **Priority** | **MEDIUM** |
| **Action** | Calibrate expectations in documentation. Implement AI tool runtime connections, streaming support, and basic RAG in a future phase. Do not present unimplemented features as available. |
| **Evidence** | `docs/11-review-modular/07-ai-knowledge/module-consolidated-review.md`, `src/modules/aiknowledge/` (278 files, ~10% test coverage) |

---

### OI-11 — No RowVersion or ConcurrencyToken on entities

| Attribute | Value |
|-----------|-------|
| **Item** | None of the 382 entities across 20 DbContexts implement `RowVersion` or `ConcurrencyToken` for optimistic concurrency control |
| **Why it blocks** | Concurrent updates to the same entity can silently overwrite each other. Critical for approval workflows, contract versioning, and incident management. |
| **Modules affected** | All modules (especially Change Governance, Contracts, Operational Intelligence) |
| **Priority** | **LOW** |
| **Action** | Add `RowVersion` (or `xmin` concurrency token for PostgreSQL) to all aggregate roots. Implement in the migration baseline reset phase. |
| **Evidence** | `docs/11-review-modular/00-governance/database-structural-audit.md` (explicitly notes absence) |

---

## Summary

| ID | Item | Priority | Modules |
|----|------|----------|---------|
| OI-01 | Contracts backend in Catalog | HIGH | Contracts, Catalog |
| OI-02 | Integrations backend in Governance | HIGH | Integrations, Governance |
| OI-03 | Product Analytics backend in Governance | HIGH | Product Analytics, Governance |
| OI-04 | No Environment Management module | MEDIUM | Environment Mgmt, Identity |
| OI-05 | Licensing residues in code | ~~LOW~~ ✅ CLOSED (P12.1) | Identity, Frontend |
| OI-06 | 3 broken Contracts frontend routes | HIGH | Contracts (frontend) |
| OI-07 | Configuration/Notifications migration gap | MEDIUM | Configuration, Notifications |
| OI-08 | 4 databases → 1 convergence | MEDIUM | All modules |
| OI-09 | Governance catch-all problem | MEDIUM | Governance |
| OI-10 | AI & Knowledge maturity gap | MEDIUM | AI & Knowledge |
| OI-11 | No concurrency tokens | LOW | All modules |

### Recommended Execution Order for Next Phase

1. **OI-06** — Fix broken routes (fast, high impact, ~2 hours)
2. **OI-01** — Extract Contracts from Catalog (enables independent module)
3. **OI-02** — Extract Integrations from Governance
4. **OI-03** — Extract Product Analytics from Governance
5. **OI-07** — Resolve Configuration/Notifications migration state
6. **OI-04** — Create Environment Management module
7. **OI-05** — ~~Clean Licensing residues~~ ✅ CLOSED (P12.1)
8. **OI-09** — Refine Governance after extractions
9. **OI-08** — Converge databases (part of baseline reset)
10. **OI-10** — Address AI backend maturity
11. **OI-11** — Add concurrency tokens (part of baseline reset)
