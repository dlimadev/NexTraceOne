# ADR-001: Database Consolidation Plan (16 → 4)

**Status:** Implemented  
**Date:** 2026-03-21  
**Implemented:** 2026-03-22  
**Context:** Architectural Analysis (ANALISE-CRITICA-ARQUITETURAL-2026-03.md)

## Context

NexTraceOne currently uses 16 separate DbContexts, each with its own connection string and database. In the current modular monolith deployment, all 16 run in the same process against the same PostgreSQL instance.

**Problems:**
- 16 connection pools × default 100 connections = 1600 potential connections (PostgreSQL default max is 100)
- 16 databases to backup, monitor, and maintain
- No cross-module transactions possible
- 49+ migration files distributed across 16 independent pipelines

## Decision

Consolidate 16 databases into 4, using PostgreSQL schemas for module isolation within each database.

| Database | Schemas | Modules |
|----------|---------|---------|
| `nextraceone_identity` | `identity`, `audit` | IdentityAccess, AuditCompliance |
| `nextraceone_catalog` | `catalog`, `contracts`, `portal` | Catalog (Graph, Contracts, Portal) |
| `nextraceone_operations` | `changes`, `incidents`, `cost`, `runtime`, `workflow`, `promotion`, `ruleset` | ChangeGovernance, OperationalIntelligence |
| `nextraceone_ai` | `governance`, `external`, `orchestration` | AIKnowledge |

## Approach

1. **Phase 1 — Pool Size (Done):** Add `Maximum Pool Size=10` to all connection strings (160 max total)
2. **Phase 2 — Schema prefixes:** Add PostgreSQL schema configuration to each DbContext's `OnModelCreating`
3. **Phase 3 — Connection string consolidation (Done):** Point related contexts to the same database. All 17 connection string keys now map to 4 physical databases. All tables use unique module prefixes (`identity_`, `ci_`, `oi_`, `gov_`, `aud_`, `eg_`, `ct_`, `dp_`, `wf_`, `rg_`, `prm_`, `ai_gov_`, `ext_ai_`, `ai_orch_`) so no conflicts exist. The `outbox_messages` table uses `CREATE TABLE IF NOT EXISTS` in all migrations.
4. **Phase 4 — Migration consolidation:** Merge migration histories into 4 pipelines

## Consequences

### Positive
- 4 connection pools instead of 16 (max 40 connections total)
- Cross-module transactions within the same database
- Simpler backup/restore/monitoring
- Fewer migration pipelines to maintain

### Negative
- Migration effort to consolidate existing data
- Risk of schema naming conflicts (mitigated by explicit schema prefixes)
- Slightly more complex DbContext configuration

### Neutral
- Each module still maintains its own DbContext (no code-level coupling)
- Existing repository patterns remain unchanged
- Can be done incrementally, one database group at a time

## Migration Strategy

Consolidation should be done in Development first, validated with integration tests, then applied to other environments. Each phase should be a separate PR with rollback plan.

**Priority:** Important — schedule for next sprint after current critical fixes.
