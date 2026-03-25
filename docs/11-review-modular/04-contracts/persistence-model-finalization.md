# Contracts Module — Persistence Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Tables (from InitialCreate migration)

| Table (current) | Table (target with prefix) | Entity | In DbContext |
|-----------------|---------------------------|--------|-------------|
| `ct_contract_versions` | `ctr_contract_versions` | ContractVersion | ✅ |
| `ct_contract_drafts` | `ctr_contract_drafts` | ContractDraft | ✅ |
| `ct_contract_diffs` | `ctr_contract_diffs` | ContractDiff | ✅ |
| `ct_contract_reviews` | `ctr_contract_reviews` | ContractReview | ✅ |
| `ct_contract_examples` | `ctr_contract_examples` | ContractExample | ✅ |
| `ct_contract_artifacts` | `ctr_contract_artifacts` | ContractArtifact | ✅ |
| `ct_contract_rule_violations` | `ctr_contract_rule_violations` | ContractRuleViolation | ✅ |
| `ct_outbox_messages` | `ctr_outbox_messages` | OutboxMessage | ✅ |
| — | `ctr_spectral_rulesets` | SpectralRuleset | ❌ MISSING |
| — | `ctr_canonical_entities` | CanonicalEntity | ❌ MISSING |
| — | `ctr_contract_locks` | ContractLock | ❌ MISSING |
| — | `ctr_contract_scorecards` | ContractScorecard | ❌ MISSING |
| — | `ctr_contract_evidence_packs` | ContractEvidencePack | ❌ MISSING |

**Critical prefix issue:** Current tables use `ct_` prefix. Official prefix per `docs/architecture/database-table-prefixes.md` is `ctr_`. This will be corrected in the future baseline migration.

---

## 2. Entity → Table Mapping (Final)

### 2.1 `ctr_contract_versions` (Aggregate Root)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, strongly-typed ContractVersionId |
| `api_asset_id` | `uuid` | NOT NULL | FK reference to Catalog.ApiAsset (no nav property) |
| `sem_ver` | `varchar(50)` | NOT NULL | Semantic version string |
| `protocol` | `varchar(50)` | NOT NULL | Enum as string |
| `spec_content` | `text` | NOT NULL | Full spec (OpenAPI YAML/JSON, WSDL, AsyncAPI) |
| `lifecycle_state` | `varchar(50)` | NOT NULL | Enum as string |
| `deprecation_date` | `timestamptz` | NULL | When deprecated |
| `sunset_date` | `timestamptz` | NULL | When sunset |
| `signature_algorithm` | `varchar(100)` | NULL | Owned: Signature.Algorithm |
| `signature_value` | `text` | NULL | Owned: Signature.Value |
| `signature_signed_by` | `varchar(200)` | NULL | Owned: Signature.SignedBy |
| `signature_signed_at` | `timestamptz` | NULL | Owned: Signature.SignedAt |
| `provenance_source_url` | `varchar(2000)` | NULL | Owned: Provenance.SourceUrl |
| `provenance_imported_at` | `timestamptz` | NULL | Owned: Provenance.ImportedAt |
| `provenance_imported_by` | `varchar(200)` | NULL | Owned: Provenance.ImportedBy |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `created_at` | `timestamptz` | NOT NULL | Audit |
| `created_by` | `varchar(200)` | NOT NULL | Audit |
| `updated_at` | `timestamptz` | NULL | Audit |
| `updated_by` | `varchar(200)` | NULL | Audit |
| `is_deleted` | `boolean` | NOT NULL | Soft delete |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:**
- `IX_ctr_contract_versions_api_asset_sem_ver` (api_asset_id, sem_ver) — UNIQUE
- `IX_ctr_contract_versions_protocol` (protocol)
- `IX_ctr_contract_versions_lifecycle_state` (lifecycle_state)

### 2.2 `ctr_contract_drafts`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `title` | `varchar(200)` | NOT NULL | |
| `protocol` | `varchar(50)` | NOT NULL | Enum |
| `spec_content` | `text` | NULL | |
| `status` | `varchar(50)` | NOT NULL | DraftStatus enum |
| `author` | `varchar(200)` | NOT NULL | |
| `service_id` | `uuid` | NULL | Optional link to service |
| `is_ai_generated` | `boolean` | NOT NULL | AI tracking |
| `ai_generation_prompt` | `text` | NULL | AI tracking |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `created_at` / `created_by` / `updated_at` / `updated_by` | — | — | Audit |
| `is_deleted` | `boolean` | NOT NULL | Soft delete |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:** `IX_ctr_contract_drafts_author`, `IX_ctr_contract_drafts_protocol`

### 2.3 `ctr_contract_diffs`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `contract_version_id` | `uuid` | NOT NULL | FK to ctr_contract_versions |
| `previous_version_id` | `uuid` | NULL | Reference version |
| `change_level` | `varchar(50)` | NOT NULL | Enum |
| `protocol` | `varchar(50)` | NOT NULL | Enum |
| `breaking_changes` | `jsonb` | NULL | JSON array of changes |
| `non_breaking_changes` | `jsonb` | NULL | JSON array |
| `additive_changes` | `jsonb` | NULL | JSON array |
| `suggested_sem_ver` | `varchar(50)` | NULL | |
| `confidence` | `decimal(5,2)` | NULL | |
| Standard audit + tenant + soft delete columns | | | |

**Indexes:** `IX_ctr_contract_diffs_contract_version_id`, `IX_ctr_contract_diffs_protocol`

### 2.4-2.7 (Other existing tables follow similar patterns)

Tables `ctr_contract_reviews`, `ctr_contract_examples`, `ctr_contract_artifacts`, `ctr_contract_rule_violations` follow the same convention with appropriate columns, FKs, and indexes as already defined in the InitialCreate migration.

### 2.8 `ctr_spectral_rulesets` (NEW — needs EF configuration)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `name` | `varchar(200)` | NOT NULL | |
| `description` | `varchar(2000)` | NULL | |
| `origin` | `varchar(50)` | NOT NULL | SpectralRulesetOrigin enum |
| `execution_mode` | `varchar(50)` | NOT NULL | SpectralExecutionMode enum |
| `enforcement_behavior` | `varchar(50)` | NOT NULL | SpectralEnforcementBehavior enum |
| `rules_content` | `text` | NOT NULL | Spectral rules definition |
| `is_active` | `boolean` | NOT NULL | |
| Standard audit + tenant + soft delete columns | | | |
| `xmin` | `xid` | — | Concurrency token |

**Indexes:** `IX_ctr_spectral_rulesets_name` (unique per tenant), `IX_ctr_spectral_rulesets_origin`

### 2.9 `ctr_canonical_entities` (NEW — needs EF configuration)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `name` | `varchar(200)` | NOT NULL | |
| `description` | `varchar(2000)` | NULL | |
| `state` | `varchar(50)` | NOT NULL | CanonicalEntityState enum |
| `schema_content` | `text` | NOT NULL | JSON Schema or similar |
| `version` | `varchar(50)` | NULL | |
| Standard audit + tenant + soft delete columns | | | |

**Indexes:** `IX_ctr_canonical_entities_name` (unique per tenant), `IX_ctr_canonical_entities_state`

---

## 3. Primary Keys

All tables use `uuid` PKs with strongly-typed ID value converters.

---

## 4. Foreign Keys

| Table | Column | References | On Delete |
|-------|--------|-----------|-----------|
| `ctr_contract_diffs` | `contract_version_id` | `ctr_contract_versions.id` | CASCADE |
| `ctr_contract_reviews` | `draft_id` | `ctr_contract_drafts.id` | CASCADE |
| `ctr_contract_examples` | `draft_id` | `ctr_contract_drafts.id` | CASCADE |
| `ctr_contract_examples` | `contract_version_id` | `ctr_contract_versions.id` | CASCADE |
| `ctr_contract_artifacts` | `contract_version_id` | `ctr_contract_versions.id` | CASCADE |
| `ctr_contract_rule_violations` | `contract_version_id` | `ctr_contract_versions.id` | CASCADE |
| `ctr_contract_rule_violations` | `ruleset_id` | `ctr_spectral_rulesets.id` | RESTRICT |

**Cross-module:** `ctr_contract_versions.api_asset_id` references Catalog's `ApiAsset.Id` but is NOT an FK constraint (cross-module reference by convention).

---

## 5. RowVersion / Concurrency

| Entity | xmin Concurrency | Priority |
|--------|-----------------|----------|
| ContractVersion | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| ContractDraft | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| SpectralRuleset | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | MEDIUM |
| All other entities | Not needed (child entities updated within aggregate) | N/A |

---

## 6. TenantId / EnvironmentId

| Column | Required | Source |
|--------|----------|--------|
| `tenant_id` | YES (all tables) | `TenantRlsInterceptor` |
| `environment_id` | NO | Not applicable to Contracts — contracts are not environment-scoped |

---

## 7. Audit Columns

All tables have standard audit columns via `AuditInterceptor`:
- `created_at`, `created_by`, `updated_at`, `updated_by`

---

## 8. Divergences: Current vs Target

| # | Divergence | Current | Target | Priority |
|---|-----------|---------|--------|----------|
| 1 | Wrong table prefix | `ct_` | `ctr_` | HIGH (fix in baseline) |
| 2 | Missing DbSets | 7 DbSets | 12 DbSets (add 5) | HIGH |
| 3 | Missing EF configurations | 7 configs | 12 configs (add 5) | HIGH |
| 4 | No RowVersion | None | xmin on Version, Draft, Ruleset | HIGH |
| 5 | No check constraints | None | Enum check constraints | MEDIUM |
| 6 | Missing filtered indexes | None | `WHERE is_deleted = false` on key tables | MEDIUM |
| 7 | Outbox table prefix | `ct_outbox_messages` | `ctr_outbox_messages` | LOW (fix in baseline) |

---

## 9. Summary

The persistence model for Contracts is well-structured with 7 mapped tables and 3 existing migrations. The main gaps are:
1. **Wrong prefix** (`ct_` → `ctr_`) — fix in future baseline
2. **5 unmapped entities** — add DbSets and EF configurations
3. **No concurrency tokens** — add xmin
4. **No check constraints** — add for enums

The model is ready for the future baseline migration once these gaps are addressed in the EF configurations.
