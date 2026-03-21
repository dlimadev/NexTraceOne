# NexTraceOne — Implementation Status Taxonomy

This document defines the official taxonomy for classifying implementation maturity
across all modules, handlers, interfaces, and features.

## Status Levels

| Status | Code | Meaning |
|--------|------|---------|
| **Implemented** | `IMPL` | Real data from database, validated pipeline, full integration |
| **Partially Implemented** | `PARTIAL` | Mix of real and deferred/placeholder fields |
| **Simulated** | `SIM` | Handler exists, returns hardcoded/fabricated data with `IsSimulated: true` |
| **Planned** | `PLAN` | Interface/contract defined, no implementation exists |
| **Deferred** | `DEF` | Specific fields within a real handler that return 0/placeholder |
| **Preview** | `PREV` | Module visible in UI but not functional; gated by feature flag |

## Backend Markers

- **Simulated handlers**: Response DTOs include `IsSimulated = true` and `DataSource = "demo"`
- **Deferred fields**: Response DTOs include `DeferredFields` property listing field names
- **Planned interfaces**: Source files include `// IMPLEMENTATION STATUS: Planned — no implementation exists.`
- **Ingestion metadata-only**: Endpoints return `processingStatus: "metadata_recorded"`

## Frontend Markers

- **DemoBanner component**: Renders on all pages consuming simulated handlers
- **Preview badge**: Module-level gating for not-yet-functional features

---

## Module Status Matrix

### Foundation

| Feature | Status | Notes |
|---------|--------|-------|
| Identity (Users, Roles, Tenants) | IMPL | Full CRUD, events, audit |
| Organization structure | IMPL | Teams, domains, links |
| Environments | PARTIAL | Entities exist, no IsProductionLike logic |

### Services (Catalog)

| Feature | Status | Notes |
|---------|--------|-------|
| Service Catalog | IMPL | Full CRUD, metadata, graph |
| API Assets | IMPL | Full lifecycle |
| Consumer tracking | IMPL | DB-backed |
| Dependencies / Topology | PLAN | IObservedTopologyWriter/Reader — no implementation |

### Contracts

| Feature | Status | Notes |
|---------|--------|-------|
| Contract versions | IMPL | Full CRUD, diff, scoring |
| Contract Studio | IMPL | Visual builder + source editor |
| Approval workflow | IMPL | WorkflowInstance, EvidencePack |
| IContractsModule cross-module | PLAN | Interface defined, no implementation |

### Changes

| Feature | Status | Notes |
|---------|--------|-------|
| Release tracking | IMPL | Events, state machine |
| Blast radius analysis | IMPL | Consumer graph computation |
| Ruleset governance (Spectral) | IMPL | Lint + scoring |
| Change Intelligence cross-module | PLAN | IChangeIntelligenceModule — no implementation |
| Promotion module | PLAN | IPromotionModule — no implementation |

### Operations (OperationalIntelligence)

| Feature | Status | Notes |
|---------|--------|-------|
| Runtime snapshot ingestion | IMPL | Real DB pipeline |
| Drift detection | IMPL | Baseline comparison |
| Observability scoring | IMPL | Profile computation |
| Cost intelligence | IMPL | CostSnapshot, reports, trends |
| Service reliability list | SIM | Hardcoded 8 services, `IsSimulated: true` |
| Service reliability detail | SIM | Switch on 3 serviceIds, `IsSimulated: true` |
| Team reliability summary | SIM | Switch on 5 teamIds, `IsSimulated: true` |
| Team reliability trend | SIM | Hardcoded 5 data points, `IsSimulated: true` |
| Service reliability trend | SIM | Switch on 2 serviceIds, `IsSimulated: true` |
| Service reliability coverage | SIM | Switch on 6 serviceIds, `IsSimulated: true` |
| Domain reliability summary | SIM | Switch on 6 domainIds, `IsSimulated: true` |
| Automation workflows | PREV | Returns `PreviewOnly` error |
| Automation audit trail | SIM | Hardcoded 8 entries |
| Incidents | SIM | InMemoryIncidentStore — volatile, not persisted |
| IRuntimeIntelligenceModule | PLAN | Interface defined, no implementation |
| ICostIntelligenceModule | PLAN | Interface defined, no implementation |

### Governance

| Feature | Status | Notes |
|---------|--------|-------|
| Teams list | PARTIAL | ServiceCount real, ContractCount/MemberCount/MaturityLevel deferred |
| Domains list | PARTIAL | ServiceCount/TeamCount real, ContractCount/MaturityLevel deferred |
| Team detail | PARTIAL | ServiceCount real, 5 fields deferred |
| Domain detail | PARTIAL | ServiceCount/TeamCount real, 4 fields deferred |
| Benchmarking | SIM | Hardcoded 5 comparisons, `IsSimulated: true` |
| FinOps (domain/team/service) | SIM | All hardcoded, `IsSimulated: true` |
| Waste signals | SIM | 7 hardcoded signals, `IsSimulated: true` |
| Efficiency indicators | SIM | 3 hardcoded profiles, `IsSimulated: true` |
| Executive drill-down | SIM | All fabricated, `IsSimulated: true` |

### AI

| Feature | Status | Notes |
|---------|--------|-------|
| AI Knowledge sources | IMPL | Context builders, surfaces |
| AI Chat (local) | IMPL | Ollama integration, conversation history |
| AI model registry | IMPL | CRUD, budget tracking |
| AI access policies | IMPL | Per-user, per-group |
| IAiOrchestrationModule | PLAN | Empty interface, no methods |
| IExternalAiModule | PLAN | Empty interface, no methods |

### Knowledge

| Feature | Status | Notes |
|---------|--------|-------|
| Operational notes | IMPL | CRUD, linked to entities |
| Changelog | IMPL | Via domain events |

### Ingestion

| Feature | Status | Notes |
|---------|--------|-------|
| 5 ingestion endpoints | PARTIAL | Metadata recorded, payload not processed |

### Infrastructure

| Feature | Status | Notes |
|---------|--------|-------|
| Outbox pattern | PARTIAL | Only IdentityDbContext processed; 15 other contexts unprocessed |
| IdempotencyKey | IMPL | Deterministic key based on content hash |

---

## Cross-Module Contract Health

| Contract Interface | Status | Consumer |
|---|---|---|
| ICatalogGraphModule | IMPL | Governance module |
| IAuditModule | IMPL | IdentityAccess module |
| IIdentityModule | IMPL | No external consumer |
| IDeveloperPortalModule | IMPL | No external consumer |
| IWorkflowModule | IMPL | No external consumer |
| IContractsModule | PLAN | Referenced in governance comments only |
| IChangeIntelligenceModule | PLAN | None |
| IPromotionModule | PLAN | None |
| IRulesetGovernanceModule | PLAN | None |
| ICostIntelligenceModule | PLAN | None |
| IRuntimeIntelligenceModule | PLAN | None |
| IAiOrchestrationModule | PLAN | None |
| IExternalAiModule | PLAN | None |

## Integration Events

| Event | Status |
|---|---|
| UserCreatedIntegrationEvent | PLAN — no consumers |
| UserRoleChangedIntegrationEvent | PLAN — no consumers |
| RiskReportGenerated | PLAN — no consumers |
| ComplianceGapsDetected | PLAN — no consumers |

---

## Update Protocol

When implementing a feature:
1. Update this matrix from SIM/PLAN → PARTIAL or IMPL
2. Remove `IsSimulated`/`DeferredFields` flags from the affected DTOs
3. Remove `DemoBanner` from the affected frontend pages
4. Add/update tests for the real implementation
