# Governance Module — Documentation and Onboarding Upgrade

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Existing Documentation Status

| Document | Location | Lines | Status | Notes |
|----------|----------|-------|--------|-------|
| `module-review.md` | `docs/11-review-modular/08-governance/` | 188 | ✅ Exists | Initial module review — first-pass analysis |
| `module-consolidated-review.md` | `docs/11-review-modular/08-governance/` | 393 | ✅ Exists | Consolidated review with corrections and findings |
| `current-state-inventory.md` | `docs/11-review-modular/08-governance/` | — | ✅ Exists | Full inventory of entities, endpoints, pages |
| `module-scope-finalization.md` | `docs/11-review-modular/08-governance/` | — | ✅ Exists | Scope boundaries and capability mapping |
| `module-boundary-finalization.md` | `docs/11-review-modular/08-governance/` | — | ✅ Exists | What stays in Governance, what leaves |
| `domain-model-finalization.md` | `docs/11-review-modular/08-governance/` | — | ✅ Exists | Aggregates, entities, value objects |
| `persistence-model-finalization.md` | `docs/11-review-modular/08-governance/` | — | ✅ Exists | Tables, columns, indexes, constraints |
| Module README | `src/.../Governance/README.md` | — | ❌ **Missing** | No module-level README |

### Assessment

The B1 review documentation is **comprehensive** — 7 analysis documents covering inventory, scope, boundaries, domain model, and persistence model. However, the module itself has **no operational documentation** (README, subdomain docs, developer guides).

---

## 2. Missing Documentation

### 2.1 Module-Level Documentation (Must Create)

| # | Document | Location | Priority | Purpose |
|---|----------|----------|----------|---------|
| 1 | **Module README** | `src/.../Governance/README.md` | **HIGH** | Entry point for developers — module overview, structure, how to run, how to extend |
| 2 | **Subdomain Overview** | `src/.../Governance/docs/subdomains.md` | **HIGH** | Documents the governance subdomains: Teams & Org, Packs & Rules, Compliance & Risk, FinOps, Executive |
| 3 | **API Reference** | `src/.../Governance/docs/api-reference.md` | **MEDIUM** | All 16+ endpoint modules with routes, methods, permissions, request/response examples |
| 4 | **Permission Matrix** | `src/.../Governance/docs/permissions.md` | **HIGH** | Complete permission mapping — backend granular permissions, frontend guards, persona mapping |
| 5 | **Entity Relationship Diagram** | `src/.../Governance/docs/erd.md` | **MEDIUM** | Visual or textual ERD for the 9 governance entities |

### 2.2 Architecture Decision Records (Should Create)

| # | ADR | Priority | Topic |
|---|-----|----------|-------|
| 1 | ADR: Why governance is a single module | **MEDIUM** | Justifies keeping governance as one module vs. splitting into compliance, risk, finops |
| 2 | ADR: Read-model approach for compliance/risk/finops | **MEDIUM** | Documents why some dashboards use computed read-models vs. persisted entities |
| 3 | ADR: Extraction plan for Integrations and Product Analytics | **HIGH** | Documents the decision and plan to extract non-governance entities |

### 2.3 Process Documentation (Nice to Have)

| # | Document | Priority | Purpose |
|---|----------|----------|---------|
| 1 | Pack lifecycle documentation | **LOW** | Documents Draft → Published → Deprecated → Archived status transitions |
| 2 | Waiver workflow documentation | **LOW** | Documents waiver request → review → approve/reject flow |
| 3 | Compliance check execution documentation | **LOW** | Documents how compliance checks are triggered and evaluated |

---

## 3. Code Areas Needing Documentation

### 3.1 Domain Layer

| Area | Current Doc Status | What's Needed |
|------|-------------------|---------------|
| Aggregate roots (5) | ❌ No XML docs | Summary XML docs on each aggregate class |
| Entity relationships | ❌ No documentation | Document parent-child relationships (Pack → Version, Pack → RolloutRecord) |
| Business rules | ❌ Not documented | Document pack status transitions, waiver approval rules, team constraints |
| Value object candidates | ❌ Not documented | Document why ComplianceScore, RiskRating, MaturityScore should be value objects |

### 3.2 Application Layer

| Area | Current Doc Status | What's Needed |
|------|-------------------|---------------|
| CQRS handlers (56 after extraction) | ❌ No XML docs | Summary XML docs on each handler |
| Command/query DTOs | ❌ No XML docs | Property-level XML docs on all DTOs |
| Validators | ❌ No XML docs | Document validation rules per command |
| Error codes | ❌ Not documented | Catalog of domain-specific error codes |

### 3.3 Infrastructure Layer

| Area | Current Doc Status | What's Needed |
|------|-------------------|---------------|
| EF configurations | ❌ No XML docs | Document column mappings, index rationale |
| GovernanceDbContext | ❌ No XML docs | Document table prefix convention, tenant isolation, outbox pattern |
| Repositories | ❌ No XML docs | Document query patterns, filtering conventions |
| Migration history | ❌ Not documented | Document what each migration adds/changes |

### 3.4 API Layer

| Area | Current Doc Status | What's Needed |
|------|-------------------|---------------|
| Endpoint modules (16) | ❌ No XML docs | Document routes, permissions, request/response types |
| Permission requirements | ❌ Not inline | Add `/// <remarks>Requires: governance:packs:read</remarks>` to endpoints |
| Error responses | ❌ Not documented | Document 400/403/404/409/500 responses per endpoint |

---

## 4. XML Documentation Requirements

### Priority 1: Aggregate Roots and Key Entities

```
Required on:
- Team.cs
- GovernanceDomain.cs
- GovernancePack.cs
- GovernancePackVersion.cs
- GovernanceRolloutRecord.cs
- GovernanceRuleBinding.cs
- GovernanceWaiver.cs
- DelegatedAdministration.cs
- TeamDomainLink.cs
```

Minimum XML docs per entity:
- `<summary>` on class
- `<summary>` on public properties (especially business-critical ones)
- `<remarks>` for business rules and constraints

### Priority 2: CQRS Handlers

Minimum XML docs per handler:
- `<summary>` on class describing the use case
- `<param>` on Handle method parameters
- `<returns>` describing the result
- `<exception>` for known failure cases

### Priority 3: Endpoint Modules

Minimum XML docs per endpoint module:
- `<summary>` on class describing the API area
- `<remarks>` listing routes and required permissions

---

## 5. Minimum Mandatory Documentation Plan

### Phase 1: Immediate (During B1 Completion)

| # | Deliverable | Owner | Status |
|---|------------|-------|--------|
| 1 | Create module README with structure overview | Dev team | 🔲 Pending |
| 2 | Create permission matrix document | Dev team | 🔲 Pending |
| 3 | Create subdomain overview document | Dev team | 🔲 Pending |

### Phase 2: During Functional Corrections

| # | Deliverable | Owner | Status |
|---|------------|-------|--------|
| 4 | Add XML docs to all 9 entity classes | Dev team | 🔲 Pending |
| 5 | Add XML docs to all aggregate root methods | Dev team | 🔲 Pending |
| 6 | Document pack lifecycle state machine | Dev team | 🔲 Pending |
| 7 | Document waiver approval workflow | Dev team | 🔲 Pending |

### Phase 3: During Migration Recreation

| # | Deliverable | Owner | Status |
|---|------------|-------|--------|
| 8 | Document migration history | Dev team | 🔲 Pending |
| 9 | Create entity relationship diagram | Dev team | 🔲 Pending |
| 10 | Add XML docs to all CQRS handlers | Dev team | 🔲 Pending |

### Phase 4: Post-Migration

| # | Deliverable | Owner | Status |
|---|------------|-------|--------|
| 11 | Create API reference document | Dev team | 🔲 Pending |
| 12 | Add XML docs to all endpoint modules | Dev team | 🔲 Pending |
| 13 | Write ADRs (governance as single module, read-model approach, extraction plan) | Dev team | 🔲 Pending |

---

## 6. Onboarding Notes for New Developers

### Understanding the Governance Module

The Governance module is the **broadest module** in NexTraceOne, covering organizational governance, compliance, risk assessment, FinOps, executive reporting, and policy management. Key points for new developers:

### 6.1 Module Structure

```
Governance/
├── Domain/
│   ├── Entities/         # 9 entities (after extraction of 4 foreign entities)
│   ├── Enums/            # ~33 enums (after extraction of 12 foreign enums)
│   └── Exceptions/       # Domain-specific exceptions
├── Application/
│   ├── Commands/         # Write operations (create, update, delete, status changes)
│   ├── Queries/          # Read operations (list, get, summaries, dashboards)
│   ├── Validators/       # FluentValidation validators per command
│   └── DTOs/             # Request/response data transfer objects
├── Infrastructure/
│   ├── Persistence/
│   │   ├── GovernanceDbContext.cs    # Module DbContext with gov_ prefix
│   │   ├── Configurations/          # EF type configurations
│   │   ├── Repositories/            # Repository implementations
│   │   └── Migrations/              # 7 migration files (3 logical migrations)
│   └── ...
└── Api/
    └── Endpoints/        # 16 endpoint modules (after extraction)
```

### 6.2 Key Concepts

| Concept | Description |
|---------|------------|
| **Governance Pack** | A versioned collection of governance rules that teams must comply with |
| **Pack Version** | A specific version of a governance pack with defined rules |
| **Rule Binding** | Association of a governance rule with a scope (team, service, domain) |
| **Rollout Record** | Tracks the deployment of a pack version to a scope |
| **Waiver** | A formal exception to a governance rule, with justification and expiration |
| **Delegated Admin** | Grants admin rights for specific governance areas to non-admin users |
| **Governance Domain** | An organizational grouping for governance purposes (e.g., "Payments", "Identity") |

### 6.3 Subdomain Map

```
Governance Module
├── Teams & Organization    → Team, TeamDomainLink, GovernanceDomain
├── Packs & Rules           → GovernancePack, GovernancePackVersion, GovernanceRolloutRecord, GovernanceRuleBinding
├── Compliance & Waivers    → GovernanceWaiver, ComplianceChecks (read-model)
├── Risk Assessment         → Risk (read-model/computed)
├── FinOps                  → FinOps (read-model/computed)
├── Executive Reporting     → Executive dashboards (read-model/computed)
├── Policy Catalog          → Policies (read-model — CRUD pending)
├── Enterprise Controls     → Controls (read-model — CRUD pending)
├── Evidence Management     → Evidence (read-model — create pending)
└── Delegated Admin         → DelegatedAdministration
```

### 6.4 Common Pitfalls

1. **GovernanceRuleBinding has no DbSet** — the entity exists but is not persisted. Do not try to query it via GovernanceDbContext.
2. **Read-model subdomains** — Compliance, Risk, FinOps, and Executive views use computed/aggregated data, not dedicated entities. Changes to these areas affect queries, not the domain model.
3. **Integration and Analytics entities are temporary** — IntegrationConnector, IngestionSource, IngestionExecution, and AnalyticsEvent are scheduled for extraction. Do not add new features to these entities in the Governance module.
4. **Frontend uses single permission** — All 24 governance routes use `governance:read`. Backend has granular permissions. This mismatch is a known issue being addressed.
5. **No concurrency tokens** — Entities do not have `xmin` concurrency tokens yet. Be aware of potential lost-update scenarios until this is fixed.

### 6.5 Getting Started Checklist

- [ ] Read this document and the module README (once created)
- [ ] Review `current-state-inventory.md` for full entity/endpoint/page inventory
- [ ] Review `module-boundary-finalization.md` to understand what belongs in Governance
- [ ] Review `domain-model-finalization.md` for aggregate root and entity details
- [ ] Review `persistence-model-finalization.md` for table structures and indexes
- [ ] Review `security-and-permissions-review.md` for permission model details
- [ ] Identify which subdomain your work falls into
- [ ] Check if your changes affect entities scheduled for extraction

---

## Summary

The Governance module has **comprehensive B1 analysis documentation** (7 documents) but lacks **operational documentation** that developers need day-to-day. The minimum mandatory plan creates a README, permission matrix, and subdomain overview in Phase 1, followed by XML docs and workflow documentation in subsequent phases. The onboarding notes above serve as an interim guide until the full documentation suite is complete.
