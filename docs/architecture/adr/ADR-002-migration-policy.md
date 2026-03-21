# ADR-002: Migration Policy & Baseline Strategy

**Status:** Implemented  
**Date:** 2026-03-21  
**Context:** Persistence Remediation (ANALISE-CRITICA-ARQUITETURAL-2026-03.md)

## Context

NexTraceOne has 16 DbContexts, each maintaining independent EF Core migration pipelines. Before this ADR, there were 24 migration files accumulated over 8 days of development (March 13-21, 2026), including incremental migrations that were trivial schema adjustments.

Since the project has no production database, this was the ideal time to establish a clean baseline.

## Decision

### Migration Naming Convention

All DbContexts use a single `InitialCreate` migration as baseline. Future migrations must follow:
```
YYYYMMDDHHMMSS_<DescriptiveAction>
```
Example: `20260401120000_AddServiceHealthMetrics`

### Auto-Migration Policy

| Environment | Behavior |
|------------|----------|
| Production | **Blocked** — throws `InvalidOperationException`. Use CI/CD pipeline with `dotnet ef database update`. |
| Staging/QA | Opt-in via `NEXTRACE_AUTO_MIGRATE=true` environment variable. Logs warning. |
| Development | Auto-migrates on startup (default). Only applies contexts with pending migrations. |

### Migration Generation Commands

Each DesignTimeFactory uses `NEXTRACEONE_CONNECTION_STRING` env var with fallback to localhost connection (empty password).

```powershell
# Pattern for generating a new migration
dotnet ef migrations add <MigrationName> `
    --context <DbContextName> `
    --project src\modules\<module>\NexTraceOne.<Module>.Infrastructure `
    --startup-project src\platform\NexTraceOne.ApiHost `
    --output-dir <Submodule>\Persistence\Migrations
```

### Database Reset (Development)

When resetting development databases after migration changes:
1. Drop databases: `nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai`
2. Run the application in Development mode — auto-migration will apply all `InitialCreate` migrations
3. Or use: `dotnet ef database update --context <ContextName> --project ... --startup-project ...`

### Connection String Resolution

DI registration follows a 3-level fallback with fail-fast on missing configuration:
```csharp
var connectionString = configuration.GetConnectionString("<ModuleKey>")
    ?? configuration.GetConnectionString("NexTraceOne")
    ?? configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string '<ModuleKey>' is not configured.");
```

## Consequences

### Positive
- Clean migration baseline (16 × InitialCreate = 48 files total)
- Deterministic schema creation from any empty database
- No migration history noise from incremental development
- Fail-fast on missing configuration (no silent fallback to hardcoded credentials)

### Negative
- Existing development databases need recreation (drop and re-migrate)
- `__EFMigrationsHistory` table needs manual cleanup if databases already exist with old migration names

### Rules Going Forward
- Never check in empty/trivial migrations
- Squash on every baseline opportunity (major version, sprint boundary)
- Production migrations only via CI/CD pipeline
- No hardcoded connection strings in any source file
