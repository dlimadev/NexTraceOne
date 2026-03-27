# Module Role Finalization — Service Catalog (Catalog)

> **Module:** 03 — Service Catalog (Catalog)
> **Phase:** B1 — Consolidation
> **Date:** 2026-03-24
> **Status:** DRAFT

---

## 1. Official Role Definition

The **Service Catalog** module is the **foundational Source of Truth** for all assets within NexTraceOne. It owns the canonical representation of every service, API, event, producer, consumer, system, domain, owner, and classification known to the platform.

### What the Catalog IS

| Responsibility | Description |
|---|---|
| **Asset Registry** | Canonical source for all service and API assets — registration, versioning, lifecycle |
| **Ownership Authority** | Defines who owns each asset (team, domain, individual) |
| **Topology Provider** | Models dependencies, consumers, producers, and inter-service relationships |
| **Classification Engine** | Taxonomy, criticality, exposure type, service type, lifecycle status |
| **Inventory & Search** | Searchable catalog of all known assets with filtering, saved views |
| **Operational Metadata Hub** | Health status, discovery source, environment presence |
| **Asset History** | Change log of asset mutations (creation, reclassification, ownership transfer) |
| **Foundation for Other Modules** | Provides the base entity graph that Contracts, Changes, Operations, and Governance depend on |

### What the Catalog IS NOT

| Anti-pattern | Correct Owner |
|---|---|
| Contract lifecycle management (drafts, reviews, diffs, approval) | **Contracts** module |
| Incident management or operational runbooks | **Operations** module |
| Change tracking and blast radius analysis | **Change Governance** module |
| Cost attribution and FinOps | **Governance / FinOps** module |
| AI model registry or AI governance | **AI & Knowledge** module |
| Generic observability dashboard (metrics, traces, logs) | **Operational Intelligence** module |
| User/team identity or authentication | **Identity** module |
| Organization hierarchy or environment definitions | **Foundation** module |

---

## 2. Why Catalog Is Foundational

The Catalog module is the **most depended-upon module** in NexTraceOne. At least 5 other modules reference Catalog assets:

| Dependent Module | What It Uses from Catalog |
|---|---|
| **Contracts** | Links contract versions to `ApiAsset` and `ServiceAsset` |
| **Change Governance** | Reads topology graph to compute blast radius |
| **Operational Intelligence** | Uses topology and health data for operational dashboards |
| **AI & Knowledge** | Will use asset metadata for AI-assisted analysis |
| **Governance** | Uses service context (ownership, criticality) for compliance reports |

### Implication

Any breaking change to Catalog entities, endpoints, or events has **cascading impact** across the platform. The Catalog must be treated as a **stable, versioned foundation**.

---

## 3. Protecting Catalog from Scope Expansion

### Scope Protection Rules

1. **No contract lifecycle logic inside Catalog.** Contract entities physically reside here today but logically belong to the Contracts bounded context. This is a known structural debt (see `catalog-vs-contracts-boundary-deep-dive.md`).

2. **No operational intelligence logic.** Catalog provides health status as metadata but does NOT own metrics collection, alerting, or dashboards.

3. **No change tracking logic.** Catalog records asset history but does NOT own change validation, blast radius computation, or deployment correlation.

4. **No AI features embedded directly.** AI-assisted operations should consume Catalog via well-defined queries/events, not be embedded in Catalog handlers.

5. **No FinOps or cost logic.** Cost attribution uses Catalog context but the computation belongs to Governance.

### Gatekeeping Checklist

Before adding any feature to Catalog, answer:

| Question | Expected Answer |
|---|---|
| Does this feature define or describe an asset? | YES → Catalog |
| Does this feature manage a contract lifecycle? | NO → Contracts |
| Does this feature analyze changes or deployments? | NO → Change Governance |
| Does this feature manage incidents or runbooks? | NO → Operations |
| Does this feature compute costs or budgets? | NO → Governance/FinOps |
| Does this feature provide AI reasoning? | NO → AI & Knowledge |

---

## 4. Catalog Subdomains

The Catalog module is internally organized into 4 subdomains:

| Subdomain | Responsibility | Key Entities |
|---|---|---|
| **Graph** | Asset registry, topology, dependencies, health | `ApiAsset`, `ServiceAsset`, `ConsumerAsset`, `ConsumerRelationship`, `DiscoverySource`, `GraphSnapshot`, `NodeHealthRecord`, `SavedGraphView` |
| **Contracts** | Contract lifecycle (physically here, logically separate) | `ContractVersion`, `ContractDraft`, `ContractDiff`, `ContractReview`, `ContractExample`, etc. |
| **Portal** | Developer portal, subscriptions, playground | `Subscription`, `PlaygroundSession`, `CodeGenerationRecord`, `PortalAnalyticsEvent`, `SavedSearch` |
| **SourceOfTruth** | Cross-referencing assets with external systems | `LinkedReference` |

### Structural Note

The **Contracts** subdomain is a candidate for physical extraction into its own project. The frontend already treats them as separate modules (`features/catalog/` vs `features/contracts/`). See `catalog-vs-contracts-boundary-deep-dive.md` for the full analysis.

---

## 5. Key Files

| Area | Path |
|---|---|
| Backend project | `src/modules/catalog/` |
| Frontend catalog | `src/frontend/features/catalog/` |
| Frontend contracts | `src/frontend/features/contracts/` |
| Graph DbContext | `src/modules/catalog/Infrastructure/Persistence/CatalogGraphDbContext.cs` |
| Contracts DbContext | `src/modules/catalog/Infrastructure/Persistence/ContractsDbContext.cs` |
| Portal DbContext | `src/modules/catalog/Infrastructure/Persistence/DeveloperPortalDbContext.cs` |

---

## 6. Open Questions

| # | Question | Impact |
|---|---|---|
| 1 | Should Contracts be physically extracted from the Catalog project? | HIGH — affects project structure, build, migrations |
| 2 | Should SourceOfTruth subdomain grow or remain minimal? | MEDIUM — currently only `LinkedReference` |
| 3 | Should Portal subdomain be extracted into its own module? | LOW — small footprint, low coupling |
| 4 | How should Catalog expose events to downstream modules? | HIGH — integration contract definition needed |
