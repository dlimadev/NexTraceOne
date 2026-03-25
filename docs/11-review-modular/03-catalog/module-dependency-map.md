# Catalog Module — Module Dependency Map

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Dependency Direction Summary

The Catalog module is the **most depended-upon module** in NexTraceOne. It consumes nothing directly from other business modules and serves as the foundational asset registry.

```
                    ┌─────────────────────────┐
                    │   04 — CONTRACTS        │
                    │   ContractVersion       │
                    │   .ApiAssetId ──────────┼──┐
                    └─────────────────────────┘  │
                                                  │
                    ┌─────────────────────────┐  │
                    │   05 — CHANGE GOV       │  │
                    │   reads topology graph  │──┤
                    └─────────────────────────┘  │
                                                  │    ┌──────────────────────────┐
                    ┌─────────────────────────┐  ├───►│   03 — CATALOG           │
                    │   06 — OPS INTEL        │  │    │   ServiceAsset           │
                    │   reads health/topology │──┤    │   ApiAsset               │
                    └─────────────────────────┘  │    │   ConsumerRelationship   │
                                                  │    │   GraphSnapshot          │
                    ┌─────────────────────────┐  │    │   NodeHealthRecord       │
                    │   07 — AI & KNOWLEDGE   │  │    │   ICatalogGraphModule    │
                    │   reads asset metadata  │──┤    │   IDeveloperPortalModule │
                    └─────────────────────────┘  │    └──────────────────────────┘
                                                  │
                    ┌─────────────────────────┐  │
                    │   08 — GOVERNANCE       │  │
                    │   reads ownership/crit. │──┘
                    └─────────────────────────┘
```

---

## 2. Incoming Dependencies (Who Depends on Catalog)

| Module | What It Uses | How It References | Communication |
|--------|-------------|-------------------|--------------|
| **04 — Contracts** | `ApiAssetId` to link contracts to APIs | Guid FK (no navigation) in `ContractVersion.ApiAssetId` | Direct Guid reference |
| **04 — Contracts** | Asset metadata for portal enrichment | `IContractsModule` ← DeveloperPortal queries this | Cross-module interface |
| **05 — Change Governance** | Service topology for blast radius computation | Service names/IDs via integration events | Outbox events + query |
| **05 — Change Governance** | Dependency graph for impact analysis | `ICatalogGraphModule.GetServiceGraph()` | Cross-module interface |
| **06 — Operational Intelligence** | Service health data and topology | `ICatalogGraphModule` for topology overlays | Cross-module interface |
| **06 — Operational Intelligence** | Health records for operational dashboards | `NodeHealthRecord` data via query | Cross-module interface |
| **07 — AI & Knowledge** | Asset metadata for AI-assisted analysis | Service/API context for reasoning | Cross-module interface (planned) |
| **08 — Governance** | Ownership, criticality for compliance | Service classification data | Cross-module interface |
| **08 — Governance** | Service inventory for FinOps attribution | Team ownership for cost allocation | Cross-module interface |

---

## 3. Outgoing Dependencies (What Catalog Consumes)

| Module | What Catalog Uses | Required? | Notes |
|--------|------------------|-----------|-------|
| **01 — Identity & Access** | User identity, team membership | YES (infrastructure) | Via JWT claims / middleware — not a module dependency |
| **02 — Environment Management** | Environment definitions for `EnvironmentId` | YES (optional field) | References by Guid only, no direct module call |
| **Foundation** (shared) | TenantId, audit columns, outbox pattern | YES (infrastructure) | Shared kernel, not module dependency |

**Catalog has ZERO outgoing business module dependencies.** It only depends on infrastructure/foundation concerns that are shared across all modules.

---

## 4. Cross-Module Interfaces

### Provided by Catalog

| Interface | File | Methods | Consumers |
|-----------|------|---------|-----------|
| `ICatalogGraphModule` | `NexTraceOne.Catalog.Contracts/Graph/ServiceInterfaces/ICatalogGraphModule.cs` | `GetServiceByIdAsync`, `GetApiAssetByIdAsync`, `GetServiceGraphAsync`, `GetServiceDependenciesAsync` | Change Governance, Operational Intelligence, AI & Knowledge |
| `IDeveloperPortalModule` | `NexTraceOne.Catalog.Contracts/Portal/ServiceInterfaces/IDeveloperPortalModule.cs` | `SearchCatalogAsync`, `GetAssetDetailAsync` | Developer Portal consumers |

### Shared DTOs

| DTO | File | Purpose |
|-----|------|---------|
| `TeamServiceInfo` | `NexTraceOne.Catalog.Contracts/Graph/ServiceInterfaces/` | Service data for cross-module queries |
| `TeamContractInfo` | `NexTraceOne.Catalog.Contracts/Graph/ServiceInterfaces/` | Contract reference for cross-module queries |
| `CrossTeamDependencyInfo` | `NexTraceOne.Catalog.Contracts/Graph/ServiceInterfaces/` | Dependency data for topology consumers |

---

## 5. Integration Events

### Events Published by Catalog

| Event | Trigger | Consumers | Outbox |
|-------|---------|-----------|--------|
| `ServiceRegistered` | New service created | Change Governance, Governance | CatalogGraphDbContext outbox |
| `ApiRegistered` | New API created | Contracts (enrichment), Change Governance | CatalogGraphDbContext outbox |
| `ServiceUpdated` | Service metadata changed | Change Governance, Operational Intelligence | CatalogGraphDbContext outbox |
| `DependencyAdded` | New consumer relationship | Change Governance (blast radius) | CatalogGraphDbContext outbox |
| `HealthRecorded` | New health data point | Operational Intelligence | CatalogGraphDbContext outbox |

### Events Consumed by Catalog

| Event | Source | Purpose |
|-------|--------|---------|
| None | — | Catalog does not consume events from other business modules |

---

## 6. Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                        CATALOG MODULE                            │
│                                                                  │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐ │
│  │  Graph       │   │  Portal      │   │  SourceOfTruth       │ │
│  │  Subdomain   │   │  Subdomain   │   │  Subdomain           │ │
│  │              │   │              │   │                      │ │
│  │ ServiceAsset │   │ Subscription │   │ LinkedReference      │ │
│  │ ApiAsset     │   │ Playground   │   │                      │ │
│  │ Consumer*    │   │ CodeGen      │   └──────────────────────┘ │
│  │ Snapshot     │   │ Analytics    │                            │
│  │ Health       │   │ SavedSearch  │                            │
│  │ SavedView    │   │              │                            │
│  │ Discovery    │   └──────┬───────┘                            │
│  └──────┬───────┘          │                                    │
│         │                  │ queries IContractsModule            │
│         │                  │ for contract enrichment             │
│         │                  ▼                                     │
│  ┌──────┴──────────────────────────────────────────────────────┐ │
│  │  ICatalogGraphModule (provided to other modules)            │ │
│  │  IDeveloperPortalModule (provided to other modules)         │ │
│  └─────────────────────────────────────────────────────────────┘ │
│         │                                                        │
│         │  Integration Events (outbox)                           │
│         ▼                                                        │
└──────────────────────────────────────────────────────────────────┘
          │
          ├──► Change Governance (topology, blast radius)
          ├──► Operational Intelligence (health, topology)
          ├──► AI & Knowledge (asset metadata)
          ├──► Governance (ownership, criticality, compliance)
          └──► Contracts (ApiAssetId reference)
```

---

## 7. Circular Dependency Assessment

| Check | Result |
|-------|--------|
| Catalog → Contracts | ❌ NO (Catalog does NOT depend on Contracts) |
| Contracts → Catalog | ✅ YES (via ApiAssetId Guid reference) |
| Catalog → Change Governance | ❌ NO |
| Change Governance → Catalog | ✅ YES (via topology query) |
| Catalog → Operational Intel | ❌ NO |
| Operational Intel → Catalog | ✅ YES (via health/topology) |

**No circular dependencies.** Catalog is a pure provider — it provides data to other modules but does not consume from them.

---

## 8. Coupling Assessment

| Metric | Value | Assessment |
|--------|-------|-----------|
| Incoming dependencies | 5 modules | HIGH (expected — Catalog is foundational) |
| Outgoing business dependencies | 0 modules | ✅ IDEAL |
| Cross-module interfaces provided | 2 | ✅ Well-bounded |
| Shared DTOs | 3 | ✅ Minimal surface |
| Integration events published | 5 | ✅ Appropriate |
| Integration events consumed | 0 | ✅ No upstream coupling |
| FK constraints across modules | 0 | ✅ By convention (Guid only) |

---

## 9. Stability Impact

Because Catalog is the most depended-upon module:

| Concern | Mitigation |
|---------|-----------|
| Breaking entity changes cascade to 5+ modules | Treat ServiceAsset, ApiAsset as **stable contracts** — version carefully |
| Interface changes break consumers | `ICatalogGraphModule` and `IDeveloperPortalModule` must follow backward-compatible evolution |
| Event schema changes break subscribers | Integration events must be versioned |
| Table prefix changes require coordination | Prefix migration (`eg_`/`dp_` → `cat_`) affects only internal persistence — no external impact |
| New fields are additive and safe | ✅ Adding RowVersion, check constraints are non-breaking |

---

## 10. Backlog

| # | Item | Priority | Effort |
|---|------|----------|--------|
| DEP-01 | Verify `ICatalogGraphModule` covers all cross-module query needs | HIGH | 2h |
| DEP-02 | Document integration event schemas for consumers | HIGH | 2h |
| DEP-03 | Add versioning strategy for cross-module interfaces | MEDIUM | 1h |
| DEP-04 | Verify Change Governance has proper fallback when Catalog is unavailable | MEDIUM | 1h |
| DEP-05 | Ensure Operational Intelligence can degrade gracefully without health data | LOW | 1h |
| DEP-06 | Plan `ICatalogGraphModule` backward compatibility strategy for extraction phase | HIGH | 2h |
