# Legacy SQL Seed Files — Archived

> **Archived:** 2026-03-25 | E14 — Migration Removal Phase
> **Status:** LEGACY — DO NOT USE FOR PRODUCTION

---

## Purpose

These SQL files were used during development to populate initial data for the NexTraceOne development environment. They are **archived here as historical reference only**.

## Why Archived

These files use **old table names** that no longer match the current domain model:

| File | Old Table Names Used | New Prefix Target |
|------|--------------------|--------------------|
| `seed-identity.sql` | `identity_tenants`, `identity_users`, `identity_tenant_memberships` | `iam_` |
| `seed-catalog.sql` | `eg_service_assets`, `eg_api_assets`, `ct_contract_versions` | `cat_`, `ctr_` |
| `seed-incidents.sql` | `oi_incidents`, `oi_runbooks` | `ops_` |
| `seed-changegovernance.sql` | Old change governance tables | `chg_` |
| `seed-aiknowledge.sql` | Old AI tables | `aik_` |
| `seed-audit.sql` | Old audit tables | `aud_` |
| `seed-governance.sql` | Old governance tables | `gov_` |

## What Replaces These Files

These files will be replaced by **programmatic, idempotent seeders** as part of E15 and subsequent execution phases. Each module will have its own seeder class (like the existing `ConfigurationDefinitionSeeder`) that:

1. Uses the final table names via EF Core entities
2. Is idempotent (safe to run multiple times)
3. Is version-controlled alongside the module's code
4. Runs after the new baseline migration is applied

## Reference

- See `docs/architecture/module-seed-strategy.md` for the official seed strategy
- See `docs/architecture/new-postgresql-baseline-strategy.md` for the baseline approach
- See `docs/architecture/e14-legacy-migrations-removal-report.md` for the full removal report
