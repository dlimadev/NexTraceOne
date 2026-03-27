# Catalog Module — Catalog vs Contracts Boundary Deep Dive

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Physical vs Logical Boundaries

The Contracts code is **physically inside** the Catalog module but **logically belongs** to a separate bounded context:

| Layer | Physical Location (current) | Logical Owner | Target Location (after extraction) |
|-------|---------------------------|---------------|-------------------------------------|
| Domain Entities | `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Domain/Entities/` |
| Domain Enums | `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Enums/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Domain/Enums/` |
| Domain Value Objects | `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/ValueObjects/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Domain/ValueObjects/` |
| Domain Errors | `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Errors/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Domain/Errors/` |
| Application Features | `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/` (36+) | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Application/Features/` |
| Application Abstractions | `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Abstractions/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Application/Abstractions/` |
| Infrastructure Persistence | `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Infrastructure/Persistence/` |
| Infrastructure Services | `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Services/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Infrastructure/Services/` |
| API Endpoints | `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/Endpoints/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.API/Endpoints/` |
| Public Contracts (DTOs) | `src/modules/catalog/NexTraceOne.Catalog.Contracts/Contracts/` | Contracts | `src/modules/contracts/NexTraceOne.Contracts.Contracts/` |
| DbContext | `ContractsDbContext.cs` (ct_ prefix) | Contracts | Own DbContext with `ctr_` prefix |

**The frontend already treats them as separate:** `features/catalog/` vs `features/contracts/` (69 files).

---

## 2. Entity Ownership — What Catalog Owns

All Graph, Portal, and SourceOfTruth entities belong to Catalog:

| Entity | Subdomain | DbContext | Table Prefix | Status |
|--------|-----------|-----------|-------------|--------|
| `ServiceAsset` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `ApiAsset` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `ConsumerRelationship` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `ConsumerAsset` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `DiscoverySource` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `GraphSnapshot` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `NodeHealthRecord` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `SavedGraphView` | Graph | CatalogGraphDbContext | `eg_` → `cat_` | ✅ Catalog |
| `Subscription` | Portal | DeveloperPortalDbContext | `dp_` → `cat_` | ✅ Catalog |
| `PlaygroundSession` | Portal | DeveloperPortalDbContext | `dp_` → `cat_` | ✅ Catalog |
| `CodeGenerationRecord` | Portal | DeveloperPortalDbContext | `dp_` → `cat_` | ✅ Catalog |
| `PortalAnalyticsEvent` | Portal | DeveloperPortalDbContext | `dp_` → `cat_` | ✅ Catalog |
| `SavedSearch` | Portal | DeveloperPortalDbContext | `dp_` → `cat_` | ✅ Catalog |
| `LinkedReference` | SourceOfTruth | DeveloperPortalDbContext | `dp_` → `cat_` | ✅ Catalog |

---

## 3. Entity Ownership — What Contracts Owns

All entities under `Domain/Contracts/` belong to the Contracts bounded context:

| Entity | Type | DbContext (current) | Table Prefix | Status |
|--------|------|---------------------|-------------|--------|
| `ContractVersion` | Aggregate Root | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractDraft` | Aggregate Root | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractDiff` | Entity | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractReview` | Entity | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractExample` | Entity | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractArtifact` | Entity | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractRuleViolation` | Entity | ContractsDbContext | `ct_` → `ctr_` | ❌ Extract |
| `ContractLock` | Entity | ContractsDbContext (missing) | — | ❌ Extract |
| `ContractScorecard` | Entity | ContractsDbContext (missing) | — | ❌ Extract |
| `ContractEvidencePack` | Entity | ContractsDbContext (missing) | — | ❌ Extract |
| `OpenApiSchema` | Entity | ContractsDbContext (missing) | — | ❌ Extract |
| `SpectralRuleset` | Aggregate Root | ContractsDbContext (missing) | — | ❌ Extract |
| `CanonicalEntity` | Aggregate Root | ContractsDbContext (missing) | — | ❌ Extract |

---

## 4. Cross-Module Reference Point

The **only cross-module reference** between Catalog and Contracts is:

```
ContractVersion.ApiAssetId → Catalog.ApiAsset.Id (Guid FK, no navigation property)
```

| Aspect | Implementation | Status |
|--------|---------------|--------|
| FK type | Guid reference (no EF navigation) | ✅ Correct |
| Database constraint | No FK constraint across modules | ✅ Correct (by convention) |
| Cross-module query | `IContractsModule` interface | ✅ Correct |
| Integration events | Outbox pattern from ContractsDbContext | ✅ Correct |
| Portal → Contracts | `IContractsModule.HasContractVersionAsync(apiAssetId)` | ✅ Correct |

---

## 5. What Belongs to Asset (Catalog)

| Capability | Rationale |
|-----------|-----------|
| Service registration and metadata | Defines what a service IS |
| API registration and classification | Defines what an API IS |
| Service/API lifecycle (Alpha, Beta, Stable, Deprecated, Retired) | Asset lifecycle, not contract lifecycle |
| Dependency mapping and topology graph | Structural relationships between assets |
| Consumer/producer relationships | Who calls whom |
| Health status overlays | Operational metadata about assets |
| Graph snapshots and temporal diff | Historical topology views |
| Impact propagation analysis | Blast radius based on topology |
| Saved graph views | User personalization of catalog views |
| Discovery source configuration | How assets are discovered |
| Developer Portal (search, playground, subscriptions) | Consumer-facing view of assets |
| Source of Truth linking | External system cross-references |

---

## 6. What Belongs to Contract (Contracts)

| Capability | Rationale |
|-----------|-----------|
| Contract specification content (OpenAPI, WSDL, AsyncAPI) | The spec defines how an API is consumed, not what it IS |
| Contract versions and semantic versioning | Version history is governance, not registration |
| Semantic diff and breaking change analysis | Contract-level analysis |
| Contract review and approval workflow | Governance workflow |
| Draft management (Contract Studio) | Editing lifecycle |
| Spectral linting and rule evaluation | Contract quality assurance |
| Digital signatures and verification | Contract integrity |
| Contract export and format conversion | Contract utility |
| Compliance scoring and evidence packs | Contract governance |
| Canonical entity management | Shared schema governance |
| Contract provenance tracking | Import/origin tracking |

---

## 7. Concrete Interaction Examples

### Example 1: "Register a new REST API"
- **Catalog:** Creates `ApiAsset(name="Orders API", protocol=REST, serviceId=...)`
- **Contracts:** NOT involved

### Example 2: "Import an OpenAPI spec for Orders API"
- **Catalog:** Provides `ApiAssetId` reference
- **Contracts:** Creates `ContractVersion(apiAssetId, specContent, protocol=OpenAPI, semVer=1.0.0)`

### Example 3: "View all contracts for a service in the developer portal"
- **Catalog (Portal):** Queries `IContractsModule.HasContractVersionAsync(apiAssetId)` to enrich asset view
- **Contracts:** Returns contract metadata via the interface

### Example 4: "Check breaking changes between contract versions"
- **Catalog:** NOT involved
- **Contracts:** `ComputeSemanticDiff` → `ContractDiff` with `ChangeLevel`

### Example 5: "Compute blast radius for a service dependency change"
- **Catalog:** Provides dependency graph via `GET /api/v1/catalog/graph`
- **Contracts:** NOT involved (blast radius is about topology, not contract content)

---

## 8. What Must NEVER Be in Catalog

| Anti-pattern | Correct Module |
|-------------|---------------|
| Contract specification content or schemas | Contracts |
| Contract lifecycle state management | Contracts |
| Contract review/approval workflow logic | Contracts |
| Spectral validation rules or results | Contracts |
| Contract diff/compatibility analysis | Contracts |
| Contract digital signatures | Contracts |
| Contract Studio draft management | Contracts |
| Canonical entity definitions | Contracts |

---

## 9. What Must NEVER Be in Contracts

| Anti-pattern | Correct Module |
|-------------|---------------|
| Service registration or metadata management | Catalog |
| Service dependency graph or topology | Catalog |
| Service health monitoring overlays | Catalog |
| Team/ownership assignment at service level | Catalog |
| Service discovery or consumer registry | Catalog |
| Developer Portal features | Catalog |
| Source of Truth linking | Catalog |

---

## 10. Boundary Relationship Diagram

```
┌──────────────────────────────┐        ┌──────────────────────────────┐
│         CATALOG              │        │         CONTRACTS            │
│                              │        │                              │
│  ServiceAsset                │  1:N   │  ContractVersion             │
│    └─ ApiAsset ─────────────────────►│    ├─ SpecContent             │
│       (Name, Type, Protocol) │        │    ├─ SemVer                 │
│                              │        │    ├─ LifecycleState         │
│  ConsumerRelationship        │        │    ├─ Signature              │
│  ConsumerAsset               │        │    └─ Provenance             │
│  DiscoverySource             │        │                              │
│  GraphSnapshot               │        │  ContractDraft               │
│  NodeHealthRecord            │        │  ContractDiff                │
│  SavedGraphView              │        │  ContractReview              │
│                              │        │  ContractArtifact            │
│  Subscription (Portal)       │        │  ContractRuleViolation       │
│  PlaygroundSession (Portal)  │        │  SpectralRuleset             │
│  CodeGenerationRecord        │        │  CanonicalEntity             │
│  PortalAnalyticsEvent        │        │  ContractLock                │
│  SavedSearch                 │        │  ContractScorecard           │
│  LinkedReference (SoT)       │        │  ContractEvidencePack        │
│                              │        │  OpenApiSchema               │
│  DeveloperPortal ◄───query───┤        │                              │
│                              │        │                              │
└──────────────────────────────┘        └──────────────────────────────┘
         14 entities                           13 entities
    CatalogGraphDbContext (eg_)            ContractsDbContext (ct_)
    DeveloperPortalDbContext (dp_)
```

---

## 11. Extraction Readiness Assessment

| Criterion | Status | Notes |
|-----------|--------|-------|
| Code organized by subdomain | ✅ | All `/Contracts/` subdirs within each layer |
| No cross-subdomain navigation properties | ✅ | Only `ApiAssetId` (Guid) |
| Separate DbContext | ✅ | `ContractsDbContext` already isolated |
| Cross-module interface defined | ✅ | `IContractsModule` in `Catalog.Contracts` project |
| Frontend already separated | ✅ | `features/contracts/` (69 files) |
| Integration events via outbox | ✅ | Outbox table in ContractsDbContext |
| No shared repositories | ✅ | Each subdomain has own repositories |

**Verdict:** The boundary is conceptually clear and the code is structurally ready for extraction. The physical extraction (OI-01) is NOT in scope for this phase but is fully prepared.

---

## 12. Backlog

| # | Item | Priority | Effort |
|---|------|----------|--------|
| BD-01 | Document the boundary rules in a shared architecture doc | HIGH | 1h |
| BD-02 | Add XML comment on `ContractVersion.ApiAssetId` referencing Catalog boundary | MEDIUM | 15min |
| BD-03 | Ensure `IContractsModule` interface covers all cross-module queries needed by Portal | MEDIUM | 2h |
| BD-04 | Plan physical extraction to `src/modules/contracts/` (OI-01 scope) | HIGH | 4h |
| BD-05 | Verify no accidental coupling between Graph handlers and Contract handlers | LOW | 1h |
