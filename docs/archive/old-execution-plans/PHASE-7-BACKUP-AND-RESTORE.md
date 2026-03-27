# Phase 7 — Backup and Restore Strategy

## Database Layout

| Database | Contexts | Data |
|----------|----------|------|
| `nextraceone_identity` | Identity, Audit | Users, roles, sessions, audit logs |
| `nextraceone_catalog` | CatalogGraph, Contracts, DeveloperPortal | Services, APIs, contracts, subscriptions |
| `nextraceone_operations` | 11 contexts | Changes, incidents, governance, workflows, runtime |
| `nextraceone_ai` | AiGovernance, ExternalAi, AiOrchestration | AI models, policies, usage ledger |

## Strategy

### Frequency
| Environment | Full Backup | Retention |
|-------------|-------------|-----------|
| Production | Daily (scheduled) | 30 days |
| Staging | Weekly | 7 days |
| Development | On-demand | 3 days |

### Backup Format
- Tool: `pg_dump` (custom or plain SQL)
- Compression: gzip (`.sql.gz`)
- Naming: `{database}_{environment}_{YYYYMMDD_HHMMSS}.sql.gz`

## Scripts

### Backup — `scripts/db/backup.sh`

```bash
# Backup all databases
bash scripts/db/backup.sh --env production --output-dir /backups

# Backup specific database
bash scripts/db/backup.sh --env production --databases nextraceone_identity --output-dir /backups

# Options
--env <environment>         Environment label (default: local)
--output-dir <dir>          Backup output directory (default: ./backups)
--databases <db1,db2,...>   Comma-separated database list (default: all 4)
--help                      Show usage
```

**Environment variables:**
- `PGHOST` (default: localhost), `PGPORT` (default: 5432)
- `PGUSER` (default: nextraceone), `PGPASSWORD`

### Restore — `scripts/db/restore.sh`

```bash
# Restore latest backup
bash scripts/db/restore.sh --env production --database nextraceone_identity

# Restore specific file
bash scripts/db/restore.sh --database nextraceone_catalog --file /backups/nextraceone_catalog_production_20260322.sql.gz

# Skip confirmation
bash scripts/db/restore.sh --database nextraceone_identity --force

# Options
--env <environment>      Environment label
--database <name>        Database to restore (required)
--file <path>            Specific backup file (default: latest in --input-dir)
--input-dir <dir>        Backup directory (default: ./backups)
--force                  Skip confirmation prompt
--help                   Show usage
```

### Verify Restore — `scripts/db/verify-restore.sh`

```bash
bash scripts/db/verify-restore.sh --database nextraceone_identity

# Checks performed:
# - Database exists and is connectable
# - Table count
# - Row counts for key tables
# - EF Core migrations table exists
# - Latest migration timestamp
# - Schema presence (public, telemetry)
```

## Restore Runbook

### Prerequisites
- PostgreSQL client tools (`pg_dump`, `psql`, `gunzip`)
- Network access to PostgreSQL server
- Database credentials with restore privileges

### Procedure
1. **Stop application services** to prevent writes during restore
2. **Identify backup** — latest or specific timestamp
3. **Run restore**: `bash scripts/db/restore.sh --database <name> --file <backup>`
4. **Verify**: `bash scripts/db/verify-restore.sh --database <name>`
5. **Run pending migrations**: `bash scripts/db/apply-migrations.sh --env <env>`
6. **Restart application services**
7. **Verify health**: `bash scripts/deploy/smoke-check.sh --api-url <url>`

### Recovery Time Objective
- Single database restore: < 15 minutes (depends on size)
- Full platform restore (all 4 databases): < 45 minutes
