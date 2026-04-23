# ADR-002: Single PostgreSQL Database with Schema Isolation

## Status

Accepted

## Date

2026-01-15

## Context

With 12 bounded context modules and 28 DbContexts (27 at time of initial decision; grew as product evolved), we needed a data isolation strategy that balances:

- **Tenant isolation** for multi-tenant SaaS and self-hosted deployments.
- **Module isolation** to prevent one module from accessing another's data directly.
- **Operational simplicity** for backup, restore, and migration management.
- **Performance** for cross-module queries and reporting.

Options considered:

1. **Database-per-module**: 12+ PostgreSQL databases, one per module.
2. **Schema-per-module**: Single database, separate PostgreSQL schemas per module.
3. **Table-prefix isolation**: Single database, single schema, table prefixes per module.
4. **Previous approach**: 4 databases (identity, catalog, operations, ai) — abandoned.

## Decision

We chose **single PostgreSQL database (`nextraceone`)** with:

- **Table-prefix isolation** per module (e.g., `iam_users`, `cat_services`, `cg_changes`).
- **Row-Level Security (RLS)** policies on all tenant-scoped tables for defence-in-depth.
- **27 separate EF Core DbContexts** — each module only sees its own tables.
- **Automated RLS application** after migrations via `apply-rls.sql` script.
- **All connection strings** point to the same physical database with different pool configurations.

## Consequences

### Positive

- Single backup/restore operation covers all data.
- Cross-module joins possible for reporting (when needed via raw SQL/views).
- Simpler connection management and pooling.
- RLS provides tenant isolation at the database level even if application code has bugs.

### Negative

- All modules share the same database performance envelope.
- Schema migrations must be coordinated (mitigated by wave-based migration ordering).
- RLS policies must be kept in sync with table schema changes.

### Mitigations

- Migration waves (7 waves) ordered by dependency to prevent conflicts.
- RLS script is idempotent and re-applied automatically after migrations.
- Connection pool size per module configured independently via connection string.
