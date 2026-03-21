# ADR-002: Migration Policy and Baseline Strategy

**Status:** Accepted  
**Date:** 2026-03-21  
**Context:** 16 DbContexts require a clear, safe migration strategy.

## Decision

### Baseline

All 16 DbContexts have a single `InitialCreate` migration representing the current schema.
All previous incremental migrations were deleted and regenerated from the current model state.

### Auto-Migration Policy

| Environment | Behavior |
|-------------|----------|
| **Production** | Auto-migrate **BLOCKED unconditionally** — startup throws if `NEXTRACE_AUTO_MIGRATE=true` |
| **Staging / QA** | Auto-migrate only if `NEXTRACE_AUTO_MIGRATE=true` |
| **Development** | Auto-migrate runs automatically on startup |

Migrations are only applied when `GetPendingMigrationsAsync()` returns pending items.

### Creating New Migrations

```bash
# From the solution root:
dotnet ef migrations add <MigrationName> \
  --project src/modules/<module>/<Module>.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context <DbContextName>
```

Each Infrastructure project contains a `DesignTimeFactory` that provides
the `DbContext` with the correct connection string for tooling.

### Model Snapshots

Each DbContext has a `ModelSnapshot.cs` file co-located with its migrations.
These must be committed alongside migration files.

## Consequences

- Clean baseline: no historical migration baggage
- `dotnet ef database update` from zero creates the full schema in one step
- Production safety: no accidental schema changes on deploy
