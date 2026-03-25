# Governance Module — Persistence Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Tables (12 in GovernanceDbContext)

| Table (current) | Entity | Belongs to Governance? | Target Table Name |
|-----------------|--------|----------------------|-------------------|
| `gov_teams` | Team | ✅ YES | `gov_teams` (keep) |
| `gov_team_domain_links` | TeamDomainLink | ✅ YES | `gov_team_domain_links` (keep) |
| `gov_domains` | GovernanceDomain | ✅ YES | `gov_domains` (keep) |
| `gov_packs` | GovernancePack | ✅ YES | `gov_packs` (keep) |
| `gov_pack_versions` | GovernancePackVersion | ✅ YES | `gov_pack_versions` (keep) |
| `gov_rollout_records` | GovernanceRolloutRecord | ✅ YES | `gov_rollout_records` (keep) |
| `gov_waivers` | GovernanceWaiver | ✅ YES | `gov_waivers` (keep) |
| `gov_delegated_admins` | DelegatedAdministration | ✅ YES | `gov_delegated_admins` (keep) |
| `gov_integration_connectors` | IntegrationConnector | ❌ Integrations | → `int_connectors` (extract) |
| `gov_ingestion_sources` | IngestionSource | ❌ Integrations | → `int_ingestion_sources` (extract) |
| `gov_ingestion_executions` | IngestionExecution | ❌ Integrations | → `int_ingestion_executions` (extract) |
| `gov_analytics_events` | AnalyticsEvent | ❌ Product Analytics | → `pan_analytics_events` (extract) |
| `gov_outbox_messages` | OutboxMessage | ✅ YES | `gov_outbox_messages` (keep) |

**After extraction: 9 tables + outbox remain in Governance.**

---

## 2. Final Table Definitions (Governance tables only)

### 2.1 `gov_teams`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, TeamId |
| `name` | `varchar(200)` | NOT NULL | Unique per tenant |
| `status` | `varchar(50)` | NOT NULL | TeamStatus enum |
| `description` | `varchar(2000)` | NULL | |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| `created_at` / `created_by` / `updated_at` / `updated_by` | — | — | Audit |
| `is_deleted` | `boolean` | NOT NULL | Soft delete |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:** `IX_gov_teams_name` (unique per tenant), `IX_gov_teams_status`

### 2.2 `gov_domains`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, GovernanceDomainId |
| `name` | `varchar(200)` | NOT NULL | Unique per tenant |
| `criticality` | `varchar(50)` | NOT NULL | DomainCriticality enum |
| `description` | `varchar(2000)` | NULL | |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit + soft delete columns | — | — | Standard |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:** `IX_gov_domains_name` (unique per tenant), `IX_gov_domains_criticality`

### 2.3 `gov_team_domain_links`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `team_id` | `uuid` | NOT NULL | FK → gov_teams |
| `domain_id` | `uuid` | NOT NULL | FK → gov_domains |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit columns | — | — | Standard |

**Indexes:** `IX_gov_team_domain_links_team_domain` (team_id, domain_id — unique)

### 2.4 `gov_packs`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK, GovernancePackId |
| `name` | `varchar(200)` | NOT NULL | Unique per tenant |
| `category` | `varchar(100)` | NOT NULL | GovernanceRuleCategory enum |
| `status` | `varchar(50)` | NOT NULL | GovernancePackStatus enum |
| `description` | `varchar(4000)` | NULL | |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit + soft delete columns | — | — | Standard |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:** `IX_gov_packs_name` (unique per tenant), `IX_gov_packs_category`, `IX_gov_packs_status`

### 2.5 `gov_pack_versions`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `pack_id` | `uuid` | NOT NULL | FK → gov_packs |
| `version` | `varchar(50)` | NOT NULL | Semantic version |
| `content` | `text` | NOT NULL | Version content/rules |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit columns | — | — | Standard |

**Indexes:** `IX_gov_pack_versions_pack_version` (pack_id, version — unique)

### 2.6 `gov_rollout_records`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `pack_id` | `uuid` | NOT NULL | FK → gov_packs |
| `version_id` | `uuid` | NOT NULL | FK → gov_pack_versions |
| `status` | `varchar(50)` | NOT NULL | RolloutStatus enum |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit columns | — | — | Standard |

**Indexes:** `IX_gov_rollout_records_pack_id`, `IX_gov_rollout_records_status`

### 2.7 `gov_rule_bindings` (NEW — needs table + EF configuration)

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `pack_id` | `uuid` | NOT NULL | FK → gov_packs |
| `scope_type` | `varchar(50)` | NOT NULL | GovernanceScopeType enum |
| `scope_id` | `uuid` | NULL | Reference to scoped entity |
| `is_active` | `boolean` | NOT NULL | |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit columns | — | — | Standard |

**Indexes:** `IX_gov_rule_bindings_pack_scope` (pack_id, scope_type, scope_id — unique)

### 2.8 `gov_waivers`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `pack_id` | `uuid` | NOT NULL | FK → gov_packs |
| `status` | `varchar(50)` | NOT NULL | WaiverStatus enum |
| `justification` | `varchar(4000)` | NOT NULL | |
| `requested_by` | `varchar(200)` | NOT NULL | |
| `approved_by` | `varchar(200)` | NULL | |
| `expires_at` | `timestamptz` | NULL | |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit + soft delete columns | — | — | Standard |
| `xmin` | `xid` | — | **NEW:** Concurrency token |

**Indexes:** `IX_gov_waivers_pack_id`, `IX_gov_waivers_status`

### 2.9 `gov_delegated_admins`

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK |
| `scope` | `varchar(50)` | NOT NULL | DelegationScope enum |
| `delegated_to` | `varchar(200)` | NOT NULL | UserId reference |
| `delegated_by` | `varchar(200)` | NOT NULL | UserId reference |
| `expires_at` | `timestamptz` | NULL | |
| `tenant_id` | `uuid` | NOT NULL | RLS |
| Audit columns | — | — | Standard |

**Indexes:** `IX_gov_delegated_admins_delegated_to`, `IX_gov_delegated_admins_scope`

---

## 3. RowVersion / Concurrency

| Entity | xmin Concurrency | Priority |
|--------|-----------------|----------|
| Team | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| GovernanceDomain | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| GovernancePack | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | HIGH |
| GovernanceWaiver | ⬜ **NEEDS** `UseXminAsConcurrencyToken()` | MEDIUM |

---

## 4. TenantId / EnvironmentId

| Column | Required | Notes |
|--------|----------|-------|
| `tenant_id` | YES (all tables) | Multi-tenancy via RLS |
| `environment_id` | NO | Governance is not environment-scoped (policies apply cross-environment) |

---

## 5. Divergences: Current vs Target

| # | Divergence | Current | Target | Priority |
|---|-----------|---------|--------|----------|
| 1 | Missing table | No `gov_rule_bindings` table | Add table + EF config | HIGH |
| 2 | No RowVersion | None | xmin on 4 aggregate roots | HIGH |
| 3 | Tables to extract | 4 tables belong to other modules | Document for extraction | HIGH |
| 4 | No check constraints | None | Enum check constraints | MEDIUM |
| 5 | No filtered indexes | None | `WHERE is_deleted = false` on key tables | LOW |
| 6 | Prefix already correct | `gov_` prefix | `gov_` — no change needed | ✅ |

---

## 6. Summary

The persistence model is well-structured with correct `gov_` prefix. Main gaps:
1. **1 unmapped entity** (GovernanceRuleBinding) needs table + EF config
2. **4 tables to extract** to Integrations and Product Analytics in future
3. **No concurrency tokens** — add xmin on 4 aggregate roots
4. **No check constraints** — add for enums

After extraction, Governance will have 9 tables + outbox, all correctly prefixed with `gov_`.
