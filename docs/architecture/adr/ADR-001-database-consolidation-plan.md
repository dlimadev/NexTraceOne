# ADR-001: Database Consolidation Plan (16 → 4)

**Status:** Implemented  
**Date:** 2026-03-21  
**Updated:** 2026-03-21 (Persistence Remediation)  
**Context:** Architectural Analysis (ANALISE-CRITICA-ARQUITETURAL-2026-03.md)

## Context

NexTraceOne uses 16 DbContexts, consolidated to 4 physical PostgreSQL databases. Each DbContext inherits `NexTraceDbContextBase` which provides: AuditInterceptor, TenantRlsInterceptor, EncryptionInterceptor, Outbox pattern (IdempotencyKey), and soft-delete global filters.

**Problems addressed:**
- 17 connection strings (16 modules + 1 default fallback) with no pool limits → connection exhaustion risk
- Hardcoded `postgres:postgres` credentials in DI fallback across all 16 modules
- Stale database names in 15 DesignTimeFactory files (pointed to legacy per-module databases)
- 24 migration files with incremental noise (now squashed to 16 clean InitialCreate)
- Stale namespace references (`NexTraceOne.Identity` vs `NexTraceOne.IdentityAccess`) in BackgroundWorkers

## Decision

### Database Consolidation (4 physical databases)

| Database | Connection String Keys | DbContexts | Max Pool |
|----------|----------------------|-------------|----------|
| `nextraceone_identity` | `IdentityDatabase`, `AuditDatabase` | IdentityDbContext, AuditDbContext | 20 |
| `nextraceone_catalog` | `CatalogDatabase`, `ContractsDatabase`, `DeveloperPortalDatabase` | CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext | 30 |
| `nextraceone_operations` | `ChangeIntelligenceDatabase`, `RulesetGovernanceDatabase`, `WorkflowDatabase`, `PromotionDatabase`, `IncidentDatabase`, `RuntimeIntelligenceDatabase`, `CostIntelligenceDatabase`, `GovernanceDatabase` | ChangeIntelligenceDbContext, RulesetGovernanceDbContext, WorkflowDbContext, PromotionDbContext, IncidentDbContext, RuntimeIntelligenceDbContext, CostIntelligenceDbContext, GovernanceDbContext | 80 |
| `nextraceone_ai` | `AiGovernanceDatabase`, `ExternalAiDatabase`, `AiOrchestrationDatabase` | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext | 30 |

### Table Isolation Strategy

Tables are isolated via **module-specific name prefixes** in `IEntityTypeConfiguration<T>`:
- `identity_`, `aud_`, `ct_`, `eg_`, `dp_`, `ci_`, `rg_`, `wf_`, `prm_`, `oi_`, `gov_`, `ai_gov_`, `ext_ai_`, `ai_orch_`

The shared `outbox_messages` table uses `CREATE TABLE IF NOT EXISTS` in the base `NexTraceDbContextBase.OnModelCreating`, allowing multiple DbContexts per database without conflicts.

**PostgreSQL schemas (`HasDefaultSchema`) are deferred** to a future PR. Current prefix approach works correctly and avoids complexity with the shared outbox table pattern.

## Implementation (Done)

### Phase 1 — Pool Size ✅
All 17 connection strings set to `Maximum Pool Size=10`. Max per-database: operations=80, identity=20, catalog=30, ai=30.

### Phase 2 — Connection String Security ✅
- Removed hardcoded `postgres:postgres` fallback from all 16 DI registration files
- DI now uses: `ModuleKey → NexTraceOne → DefaultConnection → throw InvalidOperationException`
- 15 DesignTimeFactory files: removed `Password=ouro18`, aligned database names with consolidated strategy
- Created missing `AiGovernanceDbContextDesignTimeFactory`

### Phase 3 — Migration Reset ✅
- Deleted all 24 incremental migration files across 16 DbContexts
- Regenerated clean `InitialCreate` per context (16 total, 48 files: migration + Designer + Snapshot)
- All migrations generated via `dotnet ef migrations add InitialCreate`

### Phase 4 — Auto-Migration Hardening ✅
- Production: blocked unconditionally (throws `InvalidOperationException`)
- Development: auto-migrates
- Staging/QA: opt-in via `NEXTRACE_AUTO_MIGRATE=true` env var
- `MigrateContextAsync`: only applies when pending migrations exist, includes count in logs

## Consequences

### Positive
- 4 physical databases with connection pools capped at 10 per context
- No hardcoded credentials anywhere in source code
- Clean migration baseline: 1 migration per context (16 total)
- DesignTimeFactory database names aligned with runtime configuration
- Missing AiGovernanceDbContextDesignTimeFactory created

### Negative
- Existing databases need `__EFMigrationsHistory` cleanup (delete old rows, insert new InitialCreate entry)
- E2E tests need connection string configuration (no longer fall back to hardcoded values)

### Deferred
- PostgreSQL `HasDefaultSchema` (module-level schemas) — deferred to avoid outbox table complexity
