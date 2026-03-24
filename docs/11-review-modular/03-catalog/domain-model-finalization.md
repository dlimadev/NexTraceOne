# Catalog Module — Domain Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Aggregates

| Aggregate Root | File | Responsibility |
|---------------|------|---------------|
| `ServiceAsset` | `Domain/Graph/Entities/ServiceAsset.cs` | Canonical service representation — registration, classification, ownership |
| `ApiAsset` | `Domain/Graph/Entities/ApiAsset.cs` | API specifications with protocol, exposure type, lifecycle status |

---

## 2. Entities (14 total)

| Entity | Type | Subdomain | Parent Aggregate | DbSet Mapped | File |
|--------|------|-----------|-----------------|-------------|------|
| `ServiceAsset` | Aggregate Root | Graph | Self | ✅ `ServiceAssets` | `Graph/Entities/ServiceAsset.cs` |
| `ApiAsset` | Aggregate Root | Graph | Self | ✅ `ApiAssets` | `Graph/Entities/ApiAsset.cs` |
| `ConsumerRelationship` | Entity | Graph | ApiAsset | ✅ `ConsumerRelationships` | `Graph/Entities/ConsumerRelationship.cs` |
| `ConsumerAsset` | Entity | Graph | ServiceAsset | ✅ `ConsumerAssets` | `Graph/Entities/ConsumerAsset.cs` |
| `DiscoverySource` | Entity | Graph | Standalone | ✅ `DiscoverySources` | `Graph/Entities/DiscoverySource.cs` |
| `GraphSnapshot` | Entity | Graph | Standalone | ✅ `GraphSnapshots` | `Graph/Entities/GraphSnapshot.cs` |
| `NodeHealthRecord` | Entity | Graph | Standalone | ✅ `NodeHealthRecords` | `Graph/Entities/NodeHealthRecord.cs` |
| `SavedGraphView` | Entity | Graph | Standalone | ✅ `SavedGraphViews` | `Graph/Entities/SavedGraphView.cs` |
| `Subscription` | Entity | Portal | Standalone | ✅ `Subscriptions` | `Portal/Entities/Subscription.cs` |
| `PlaygroundSession` | Entity | Portal | Standalone | ✅ `PlaygroundSessions` | `Portal/Entities/PlaygroundSession.cs` |
| `CodeGenerationRecord` | Entity | Portal | Standalone | ✅ `CodeGenerationRecords` | `Portal/Entities/CodeGenerationRecord.cs` |
| `PortalAnalyticsEvent` | Entity | Portal | Standalone | ✅ `PortalAnalyticsEvents` | `Portal/Entities/PortalAnalyticsEvent.cs` |
| `SavedSearch` | Entity | Portal | Standalone | ✅ `SavedSearches` | `Portal/Entities/SavedSearch.cs` |
| `LinkedReference` | Entity | SourceOfTruth | Standalone | ✅ `LinkedReferences` | `SourceOfTruth/Entities/LinkedReference.cs` |

**All 14 entities are mapped in their respective DbContexts** — no DbSet gaps.

---

## 3. Enums (8 total, all persisted as strings)

| Enum | Values | Used By | File |
|------|--------|---------|------|
| `ServiceType` | Microservice, LegacySystem, ThirdParty, Gateway | ServiceAsset | `Graph/Enums/ServiceType.cs` |
| `ExposureType` | Internal, Partner, Public | ApiAsset | `Graph/Enums/ExposureType.cs` |
| `LifecycleStatus` | Alpha, Beta, Stable, Deprecated, Retired | ServiceAsset, ApiAsset | `Graph/Enums/LifecycleStatus.cs` |
| `Criticality` | Critical, High, Medium, Low | ServiceAsset | `Graph/Enums/Criticality.cs` |
| `NodeType` | API, Service, Consumer, ExternalSystem | GraphSnapshot, NodeHealthRecord | `Graph/Enums/NodeType.cs` |
| `EdgeType` | Dependency, Producer, Consumer, Integration | GraphSnapshot | `Graph/Enums/EdgeType.cs` |
| `HealthStatus` | Healthy, Degraded, Unhealthy, Unknown | NodeHealthRecord | `Graph/Enums/HealthStatus.cs` |
| `RelationshipSemantic` | Calls, Publishes, Consumes, Depends | ConsumerRelationship | `Graph/Enums/RelationshipSemantic.cs` |

---

## 4. Internal Entity Relationships

```
ServiceAsset (Aggregate Root)
  ├── 1:N → ApiAsset (APIs belonging to this service)
  ├── 1:N → ConsumerAsset (known consumers of this service)
  └── N:M → ConsumerRelationship (via ApiAsset)

ApiAsset (Aggregate Root)
  ├── N:1 → ServiceAsset (parent service)
  └── 1:N → ConsumerRelationship (consumers of this API)

ConsumerRelationship
  ├── N:1 → ApiAsset (the API being consumed)
  └── N:1 → ConsumerAsset (the consumer)

GraphSnapshot (Standalone)
  └── Contains serialized graph topology at a point in time

NodeHealthRecord (Standalone)
  └── References ServiceAsset or ApiAsset by NodeId (Guid)

SavedGraphView (Standalone)
  └── Contains serialized filter/overlay configuration

DiscoverySource (Standalone)
  └── Configuration for asset discovery integration

LinkedReference (SourceOfTruth, Standalone)
  └── Links to external systems by entity type + entity ID
```

---

## 5. Cross-Module Relationships

| This Module | References | Other Module | Type |
|------------|-----------|-------------|------|
| `ApiAsset.Id` | ← referenced by | Contracts (`ContractVersion.ApiAssetId`) | Guid FK (no nav property) |
| `ServiceAsset.Id` | ← referenced by | Change Governance (by ServiceName/Id) | Integration event / query |
| `ICatalogGraphModule` | ← queried by | Operational Intelligence, AI & Knowledge | Cross-module interface |
| `IDeveloperPortalModule` | ← queried by | Contracts (portal enrichment) | Cross-module interface |
| Integration events | → consumed by | Change Governance, Operational Intelligence | Outbox pattern |

---

## 6. Anemic Entity Assessment

| Entity | Assessment | Notes |
|--------|-----------|-------|
| ServiceAsset | **Needs enrichment** | Should have lifecycle transition validation as domain behavior |
| ApiAsset | **Needs enrichment** | Should have lifecycle transition and exposure validation |
| ConsumerRelationship | **Adequate** | Data holder with semantic classification |
| ConsumerAsset | **Adequate** | Consumer reference entity |
| DiscoverySource | **Adequate** | Configuration entity |
| GraphSnapshot | **Adequate** | Immutable snapshot — no behavior expected |
| NodeHealthRecord | **Adequate** | Temporal health data point |
| SavedGraphView | **Thin** | User personalization — acceptable |
| Subscription | **Thin** | Simple subscription state — acceptable |
| PlaygroundSession | **Thin** | Session state — acceptable |
| CodeGenerationRecord | **Thin** | Generated record — acceptable |
| PortalAnalyticsEvent | **Thin** | Event data point — acceptable |
| SavedSearch | **Thin** | User personalization — acceptable |
| LinkedReference | **Thin** | External link — acceptable |

---

## 7. Business Rules Location Assessment

| Rule | Current Location | Correct? |
|------|-----------------|----------|
| Service lifecycle transitions (Alpha → Stable → Deprecated → Retired) | Not explicitly enforced | ❌ Should be in ServiceAsset entity |
| API exposure validation (Internal cannot be Public) | Not explicitly enforced | ❌ Should be in ApiAsset entity |
| Consumer relationship uniqueness | Database constraint | ✅ Correct |
| Snapshot immutability | By convention (no update) | ⚠️ Should be enforced in entity |
| Health record staleness | Not enforced | ⚠️ Consider domain logic for stale detection |
| Impact propagation traversal | Application handler (ComputeImpactPropagation) | ✅ Correct (domain service) |
| Graph temporal diff | Application handler (CompareGraphSnapshots) | ✅ Correct (domain service) |

---

## 8. Missing Fields

| Entity | Missing Field | Rationale | Priority |
|--------|-------------|-----------|----------|
| ServiceAsset | `RowVersion` (uint/xmin) | Optimistic concurrency for concurrent edits | HIGH |
| ApiAsset | `RowVersion` (uint/xmin) | Optimistic concurrency for concurrent edits | HIGH |
| ServiceAsset | `DecommissionedAt` | Track when service was retired | MEDIUM |
| ServiceAsset | `DecommissionedBy` | Track who retired the service | MEDIUM |
| ApiAsset | `DecommissionedAt` | Track when API was retired | MEDIUM |
| NodeHealthRecord | `StalenessThreshold` | Define when health data is considered stale | LOW |

---

## 9. Unnecessary Fields

No unnecessary fields identified. All current fields serve clear purposes within the asset registry and topology domains.

---

## 10. Final Domain Model

The current domain model is **approved as final** with these required additions:

### Must Add to Entities

1. `RowVersion` (xmin) on `ServiceAsset` and `ApiAsset`
2. Lifecycle state transition validation methods on `ServiceAsset` and `ApiAsset`

### Must Add to EF Core Configurations

1. `UseXminAsConcurrencyToken()` on `ServiceAssetConfiguration`
2. `UseXminAsConcurrencyToken()` on `ApiAssetConfiguration`
3. Check constraints for all enums (`ServiceType`, `ExposureType`, `LifecycleStatus`, `Criticality`, etc.)

### Business Rule Improvements (recommended)

1. Move lifecycle state transition validation into `ServiceAsset` entity
2. Move lifecycle state transition validation into `ApiAsset` entity
3. Add `Decommission(string by, DateTimeOffset at)` method to both aggregate roots
4. Add `IsValidTransition(LifecycleStatus target)` guard on aggregate roots

### Portal / SourceOfTruth Entities

No changes needed — all entities are correctly mapped and serve their purpose.

**The domain model is ready for the migration baseline once RowVersion and check constraint gaps are filled.**
