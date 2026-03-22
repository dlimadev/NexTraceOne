# 07 — Data, Migrations, and Tenancy Audit

**Date:** 2026-03-22

---

## Database Architecture

### Consolidation: 16 DbContexts → 4 Databases

| Database | Module/DbContexts | Table Prefix Strategy |
|----------|------------------|----------------------|
| `nextraceone_identity` | IdentityDbContext | `id_` |
| `nextraceone_catalog` | ContractsDbContext, CatalogGraphDbContext, DeveloperPortalDbContext | `ct_`, `cg_`, `dp_` |
| `nextraceone_operations` | ChangeIntelligenceDbContext, PromotionDbContext, RulesetGovernanceDbContext, WorkflowDbContext, IncidentDbContext, AutomationDbContext, CostIntelligenceDbContext, ReliabilityDbContext, RuntimeIntelligenceDbContext, GovernanceDbContext, AuditDbContext | `ci_`, `pm_`, `rg_`, `wf_`, `in_`, `at_`, `co_`, `rl_`, `ri_`, `gv_`, `aud_` |
| `nextraceone_ai` | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext | `aig_`, `eai_`, `aio_` |

**ADR:** Documented in `docs/architecture/adr/ADR-001-database-consolidation-plan.md` (status: Implemented)

**Assessment:** ✅ Clean consolidation strategy. Unique table prefixes eliminate naming conflicts. Maximum Pool Size=10 per connection string prevents pool exhaustion.

---

## Migration Status

### Total Migrations: 23

| Module/Context | Count | Dates | Status |
|---------------|-------|-------|--------|
| IdentityDbContext | 2 | 2026-03-21 (Initial + AddIsPrimaryProduction) | ✅ Clean |
| ContractsDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| CatalogGraphDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| DeveloperPortalDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| ChangeIntelligenceDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| PromotionDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| RulesetGovernanceDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| WorkflowDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| GovernanceDbContext | 2 | 2026-03-21 (Initial) + 2026-03-22 (Phase5Enrichment) | ✅ Clean |
| AiGovernanceDbContext | 4 | 2026-03-21 (Initial + 3 expansions) | ✅ Clean |
| ExternalAiDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| AiOrchestrationDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| IncidentDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| AutomationDbContext | 1 | 2026-03-22 (Initial) | ✅ Clean |
| CostIntelligenceDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| ReliabilityDbContext | 1 | 2026-03-22 (Initial) | ✅ Clean |
| RuntimeIntelligenceDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |
| AuditDbContext | 1 | 2026-03-21 (Initial) | ✅ Clean |

**Migration Health Assessment:**
- ✅ All 23 migrations are recent (2026-03-21/22), indicating a clean rebaseline
- ✅ Each context has ModelSnapshot.cs files consistent with migrations
- ✅ Design-time factories exist for all contexts (enables `dotnet ef migrations` tooling)
- ✅ No excessively long migration chains (max 4 for AiGovernance)
- ✅ Migration scripts (`scripts/db/apply-migrations.sh` + `.ps1`) support all 14 contexts across 4 databases
- ⚠️ No migration history table validation (should verify no drift after apply)

**Rebaseline Status:** The codebase appears to have been recently rebaselined. All migrations date from the same period (2026-03-21/22). This is healthy for a project in active development.

---

## Multi-Tenancy

### Implementation Pattern

The multi-tenancy strategy is built on two pillars:

1. **Global Query Filter** in `NexTraceDbContextBase.cs:137`:
   ```csharp
   modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
   ```
   Applied automatically to all entities implementing `ITenantEntity` (or having a `TenantId` property).

2. **Tenant Resolution Middleware** injects the current tenant into the request context.

### TenantId Usage by Module

| Module | Entities with TenantId | Filter Method | Status |
|--------|----------------------|---------------|--------|
| IdentityAccess | User, Tenant, Role, Environment, etc. | ✅ Global query filter | ✅ |
| Catalog | ServiceEntry, Contract, ContractVersion, DeveloperPortalAsset | ✅ Global query filter | ✅ |
| ChangeGovernance | Release, Deployment, ChangeAnalysis, Promotion, WorkflowInstance | ✅ Global query filter | ✅ |
| AIKnowledge | AiProvider, AiModel, AiAgent, AiPolicy, etc. | ✅ Global + ⚠️ Manual filter in `AiRuntimeRepositories.cs:94` | ⚠️ |
| Governance | GovernancePack, Team, Domain, AnalyticsEvent, IntegrationConnector | ✅ Global query filter | ✅ |
| OpIntelligence | Incident, Runbook, AutomationWorkflow, CostSnapshot, etc. | ✅ Global query filter | ✅ |
| AuditCompliance | AuditEvent (TenantId Guid, indexed) | ✅ Global query filter | ✅ |

### TenantId Type Inconsistency

| Module | TenantId Type | Notes |
|--------|--------------|-------|
| Most modules | `Guid` | Standard |
| AIKnowledge (AgentExecution, ToolInvocation) | `string` | Inconsistent with other modules |

**Risk Assessment:**
- The `string` vs `Guid` inconsistency in AIKnowledge could cause issues if tenant context uses `Guid` format but the entity stores `string`. The manual filter in `AiRuntimeRepositories.cs:94` (`.Where(e => e.TenantId == tenantId)`) may work around this but indicates the global filter may not apply to these entities.
- **Severity:** Medium — potential cross-tenant data leakage in AI execution records if the global filter doesn't cover string-typed TenantId.

### Seed Data

- **Location:** `src/platform/NexTraceOne.ApiHost/SeedData/`
- **Content:** Default roles, permissions, agent seeds (e.g., `SoapContractAuthorAgent` as `agent-010`)
- **Status:** ✅ Adequate for bootstrap

---

## Connection String Management

### Configuration Pattern

| Environment | Strategy | Status |
|-------------|----------|--------|
| Development | `appsettings.Development.json` with explicit credentials | ✅ Appropriate |
| Staging/Production | `appsettings.json` with empty passwords (must inject via env vars/secrets) | ✅ Secure pattern |
| Startup Validation | `StartupValidation.cs` — fails hard in non-dev if required config missing | ✅ Good |

### Connection Strings (17 keys → 4 databases)

All connection strings configured with `Maximum Pool Size=10`.
`ConfigurationExtensions.GetRequiredConnectionString()` method enforces presence of connection string at startup.

---

## Data Integrity Observations

| Check | Status | Evidence |
|-------|--------|----------|
| Primary keys defined | ✅ | All entities have strongly-typed IDs |
| Indexes present | ✅ | TenantId indexed in migrations |
| Foreign keys | ✅ | Relationships configured in EF configurations |
| Unique constraints | ✅ | Where business requires (e.g., partial unique index on IsPrimaryProduction) |
| Cascade behavior | ✅ | Configured per relationship |
| Soft delete | ✅ | Via `NexTraceDbContextBase` interceptors |
| Audit timestamps | ✅ | `CreatedAt`/`UpdatedAt` via interceptors |

---

## Gaps and Recommendations

| # | Gap | Severity | Recommendation |
|---|-----|----------|---------------|
| DG-01 | TenantId type inconsistency (string vs Guid) in AIKnowledge | Medium | Standardize to Guid across all entities |
| DG-02 | Manual tenant filter in AiRuntimeRepositories | Medium | Verify global filter covers AgentExecution/ToolInvocation entities |
| DG-03 | No migration validation after apply | Low | Add post-migration schema validation in CI |
| DG-04 | Infrastructure init-databases.sql only creates databases, no extensions | Low | Consider adding pg_trgm or other extensions if needed |
| DG-05 | AuditCompliance has minimal entity model | High | Expand domain to support compliance policies, results, campaigns |
