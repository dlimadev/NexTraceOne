# ADR-001: Database Strategy — Schemas, Contexts and Physical Databases

**Status:** Accepted  
**Date:** 2026-03-21  
**Context:** NexTraceOne uses a modular monolith architecture with domain isolation.

## Decision

### Physical Databases (4)

| Database | Purpose | Schemas/Contexts |
|----------|---------|-----------------|
| `nextraceone_identity` | Identity, audit, sessions | IdentityDbContext, AuditDbContext |
| `nextraceone_catalog` | Service catalog, contracts, developer portal | CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext |
| `nextraceone_operations` | Change governance, operational intel, governance | ChangeIntelligenceDbContext, WorkflowDbContext, RulesetGovernanceDbContext, PromotionDbContext, IncidentDbContext, RuntimeIntelligenceDbContext, CostIntelligenceDbContext, GovernanceDbContext |
| `nextraceone_ai` | AI knowledge, orchestration, external AI | AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext |

### 16 DbContexts

Each module has its own DbContext with:
- Dedicated EF schema (to avoid table name collisions within shared databases)
- AuditInterceptor + TenantRlsInterceptor
- Connection string fallback: `{Module}Database` → `NexTraceOne` → `DefaultConnection`

### Connection Pool Sizing

- **Base config (appsettings.json):** `Maximum Pool Size=10` for all connections
- **Dev config (appsettings.Development.json):** 15–20 per connection
- Rationale: PostgreSQL default `max_connections=100`; 17 pools × 10 = 170 requires tuning in production

## Consequences

- Cross-database joins are not possible; modules communicate via events and contracts
- Each database can be scaled, backed up, and restored independently
- Schema isolation prevents migration conflicts between contexts sharing a database
