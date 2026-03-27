# Catalog Module — Persistence Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 03 — Service Catalog (Catalog)  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Tables (from InitialCreate migrations)

### CatalogGraphDbContext Tables

| Table (current) | Table (target with prefix) | Entity | In DbContext |
|-----------------|---------------------------|--------|-------------|
| `eg_service_assets` | `cat_service_assets` | ServiceAsset | ✅ |
| `eg_api_assets` | `cat_api_assets` | ApiAsset | ✅ |
| `eg_consumer_relationships` | `cat_consumer_relationships` | ConsumerRelationship | ✅ |
| `eg_consumer_assets` | `cat_consumer_assets` | ConsumerAsset | ✅ |
| `eg_discovery_sources` | `cat_discovery_sources` | DiscoverySource | ✅ |
| `eg_graph_snapshots` | `cat_graph_snapshots` | GraphSnapshot | ✅ |
| `eg_node_health_records` | `cat_node_health_records` | NodeHealthRecord | ✅ |
| `eg_saved_graph_views` | `cat_saved_graph_views` | SavedGraphView | ✅ |
| `eg_outbox_messages` | `cat_outbox_messages` | OutboxMessage | ✅ |

### DeveloperPortalDbContext Tables

| Table (current) | Table (target with prefix) | Entity | In DbContext |
|-----------------|---------------------------|--------|-------------|
| `dp_subscriptions` | `cat_subscriptions` | Subscription | ✅ |
| `dp_playground_sessions` | `cat_playground_sessions` | PlaygroundSession | ✅ |
| `dp_code_generation_records` | `cat_code_generation_records` | CodeGenerationRecord | ✅ |
| `dp_portal_analytics_events` | `cat_portal_analytics_events` | PortalAnalyticsEvent | ✅ |
| `dp_saved_searches` | `cat_saved_searches` | SavedSearch | ✅ |
| `dp_linked_references` | `cat_linked_references` | LinkedReference | ✅ |
| `dp_outbox_messages` | `cat_outbox_messages` | OutboxMessage | ✅ |

**Critical prefix issue:** Current tables use `eg_` (graph) and `dp_` (portal) prefixes. Official prefix per `docs/architecture/database-table-prefixes.md` is `cat_`. This will be corrected in the future baseline migration.

---

## 2. Entity → Table Mapping (Final)

### 2.1 `cat_service_assets` (Aggregate Root)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, strongly-typed ServiceAssetId |
| `name` | `varchar(200)` | NOT NULL | Service name |
| `description` | `varchar(2000)` | NULL | Service description |
| `service_type` | `varchar(50)` | NOT NULL | Enum as string (Microservice, LegacySystem, etc.) |
| `lifecycle_status` | `varchar(50)` | NOT NULL | Enum as string (Alpha, Beta, Stable, etc.) |
| `criticality` | `varchar(50)` | NOT NULL | Enum as string (Critical, High, Medium, Low) |
| `owner_team` | `varchar(200)` | NOT NULL | Owning team identifier |
| `domain` | `varchar(200)` | NULL | Business domain |
| `tags` | `jsonb` | NULL | Tags array |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `environment_id` | `uuid` | NULL | Environment scope (where deployed) |
| `created_at` | `timestamptz` | NOT NULL | Audit |
| `created_by` | `varchar(200)` | NOT NULL | Audit |
| `updated_at` | `timestamptz` | NULL | Audit |
| `updated_by` | `varchar(200)` | NULL | Audit |
| `is_deleted` | `boolean` | NOT NULL | Soft delete |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:**
- `IX_cat_service_assets_name_tenant` (name, tenant_id) — UNIQUE
- `IX_cat_service_assets_service_type` (service_type)
- `IX_cat_service_assets_lifecycle_status` (lifecycle_status)
- `IX_cat_service_assets_owner_team` (owner_team)
- `IX_cat_service_assets_criticality` (criticality)

### 2.2 `cat_api_assets`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, strongly-typed ApiAssetId |
| `service_id` | `uuid` | NOT NULL | FK to cat_service_assets |
| `name` | `varchar(200)` | NOT NULL | API name |
| `description` | `varchar(2000)` | NULL | API description |
| `protocol` | `varchar(50)` | NOT NULL | REST, SOAP, gRPC, etc. |
| `exposure_type` | `varchar(50)` | NOT NULL | Internal, Partner, Public |
| `lifecycle_status` | `varchar(50)` | NOT NULL | Alpha, Beta, Stable, etc. |
| `base_url` | `varchar(2000)` | NULL | Base endpoint URL |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `environment_id` | `uuid` | NULL | Environment scope |
| `created_at` / `created_by` / `updated_at` / `updated_by` | — | — | Audit |
| `is_deleted` | `boolean` | NOT NULL | Soft delete |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:**
- `IX_cat_api_assets_service_id` (service_id)
- `IX_cat_api_assets_name_service_tenant` (name, service_id, tenant_id) — UNIQUE
- `IX_cat_api_assets_protocol` (protocol)
- `IX_cat_api_assets_exposure_type` (exposure_type)

### 2.3 `cat_consumer_relationships`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `api_asset_id` | `uuid` | NOT NULL | FK to cat_api_assets |
| `consumer_asset_id` | `uuid` | NOT NULL | FK to cat_consumer_assets |
| `semantic` | `varchar(50)` | NOT NULL | RelationshipSemantic enum |
| `description` | `varchar(2000)` | NULL | |
| Standard audit + tenant + soft delete columns | | | |

**Indexes:** `IX_cat_consumer_relationships_api_asset_id`, `IX_cat_consumer_relationships_consumer_asset_id`, UNIQUE (api_asset_id, consumer_asset_id)

### 2.4 `cat_consumer_assets`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `name` | `varchar(200)` | NOT NULL | Consumer name |
| `description` | `varchar(2000)` | NULL | |
| `external_id` | `varchar(500)` | NULL | External system reference |
| Standard audit + tenant + soft delete columns | | | |

### 2.5 `cat_discovery_sources`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `name` | `varchar(200)` | NOT NULL | Source name |
| `source_type` | `varchar(50)` | NOT NULL | Backstage, Kong, OpenTelemetry, etc. |
| `configuration` | `jsonb` | NOT NULL | Connection/sync configuration |
| `is_active` | `boolean` | NOT NULL | |
| `last_sync_at` | `timestamptz` | NULL | |
| Standard audit + tenant + soft delete columns | | | |

### 2.6 `cat_graph_snapshots`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `label` | `varchar(200)` | NULL | Snapshot description |
| `snapshot_data` | `jsonb` | NOT NULL | Serialized graph topology |
| `captured_at` | `timestamptz` | NOT NULL | When the snapshot was taken |
| Standard audit + tenant columns | | | |

### 2.7 `cat_node_health_records`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `node_id` | `uuid` | NOT NULL | Reference to ServiceAsset or ApiAsset |
| `node_type` | `varchar(50)` | NOT NULL | NodeType enum |
| `health_status` | `varchar(50)` | NOT NULL | HealthStatus enum |
| `latency_ms` | `decimal(10,2)` | NULL | |
| `error_rate` | `decimal(5,4)` | NULL | |
| `availability` | `decimal(5,4)` | NULL | |
| `recorded_at` | `timestamptz` | NOT NULL | |
| Standard audit + tenant columns | | | |

**Indexes:** `IX_cat_node_health_records_node_id_recorded_at` (node_id, recorded_at DESC)

### 2.8 `cat_saved_graph_views`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `name` | `varchar(200)` | NOT NULL | View name |
| `filter_config` | `jsonb` | NOT NULL | Filters, overlays, layout settings |
| `owner` | `varchar(200)` | NOT NULL | User who created the view |
| `is_shared` | `boolean` | NOT NULL | Visible to team |
| Standard audit + tenant + soft delete columns | | | |

### 2.9-2.14 Portal & SourceOfTruth Tables

Tables `cat_subscriptions`, `cat_playground_sessions`, `cat_code_generation_records`, `cat_portal_analytics_events`, `cat_saved_searches`, and `cat_linked_references` follow the same convention with appropriate columns, FKs, and indexes as already defined in the DeveloperPortalDbContext migrations.

---

## 3. Primary Keys

All tables use `uuid` PKs with strongly-typed ID value converters where applicable.

---

## 4. Foreign Keys

| Table | Column | References | On Delete |
|-------|--------|-----------|-----------|
| `cat_api_assets` | `service_id` | `cat_service_assets.id` | CASCADE |
| `cat_consumer_relationships` | `api_asset_id` | `cat_api_assets.id` | CASCADE |
| `cat_consumer_relationships` | `consumer_asset_id` | `cat_consumer_assets.id` | RESTRICT |

**Cross-module:** `Contracts.ContractVersion.api_asset_id` references `cat_api_assets.id` but is NOT an FK constraint (cross-module reference by convention).

---

## 5. RowVersion / Concurrency

| Entity | xmin Concurrency | Priority |
|--------|-----------------|----------|
| ServiceAsset | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| ApiAsset | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| All other entities | Not needed (standalone records or child entities) | N/A |

---

## 6. TenantId / EnvironmentId

| Column | Required | Source | Notes |
|--------|----------|--------|-------|
| `tenant_id` | YES (all tables) | `TenantRlsInterceptor` | RLS policy enforced |
| `environment_id` | OPTIONAL (Graph tables) | Application logic | Services/APIs exist per environment; Portal/SoT entities are not environment-scoped |

---

## 7. Audit Columns

All tables have standard audit columns via `AuditInterceptor`:
- `created_at`, `created_by`, `updated_at`, `updated_by`

---

## 8. Divergences: Current vs Target

| # | Divergence | Current | Target | Priority |
|---|-----------|---------|--------|----------|
| 1 | Wrong table prefix (Graph) | `eg_` | `cat_` | HIGH (fix in baseline) |
| 2 | Wrong table prefix (Portal) | `dp_` | `cat_` | HIGH (fix in baseline) |
| 3 | No RowVersion | None | xmin on ServiceAsset, ApiAsset | HIGH |
| 4 | No check constraints | None | Enum check constraints for ServiceType, ExposureType, LifecycleStatus, Criticality, NodeType, EdgeType, HealthStatus, RelationshipSemantic | MEDIUM |
| 5 | Missing filtered indexes | None | `WHERE is_deleted = false` on cat_service_assets, cat_api_assets | MEDIUM |
| 6 | Outbox table prefixes | `eg_outbox_messages`, `dp_outbox_messages` | `cat_outbox_messages` (consolidated or per-context) | LOW |
| 7 | Two DbContexts → consider consolidation | CatalogGraphDbContext + DeveloperPortalDbContext | Consider merging into single `CatalogDbContext` | LOW (evaluate) |

---

## 9. Summary

The persistence model for Catalog is well-structured with 15 mapped tables across 2 DbContexts and existing migrations. The main gaps are:

1. **Wrong prefixes** (`eg_`, `dp_` → `cat_`) — fix in future baseline
2. **No concurrency tokens** — add xmin to ServiceAsset, ApiAsset
3. **No check constraints** — add for 8 enums
4. **No filtered indexes** — add for soft-deleted queries

The model is ready for the future baseline migration once these gaps are addressed in the EF configurations.
