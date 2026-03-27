# Phase 7 — Production Deployment Pipeline

## Overview

The production pipeline provides a controlled, auditable, and reversible path to production via `.github/workflows/production.yml`.

## Workflow: `production.yml`

### Trigger
- **Manual only** (`workflow_dispatch`)
- Inputs:
  - `image_tag` (required) — Docker image tag to deploy (e.g., staging SHA)
  - `rollback_tag` (optional) — Previous tag for automatic rollback
  - `skip_smoke` (boolean) — Skip post-deploy smoke checks

### Jobs

```
pre-deploy-validation → deploy-production → smoke-check → rollback (on failure)
                              ↑
                     [GitHub Environment
                      Approval Gate]
```

#### 1. `pre-deploy-validation`
- Validates inputs (image_tag required)
- Logs deployment intent with actor, timestamp, tags

#### 2. `deploy-production`
- **Environment: `production`** — triggers GitHub approval gate
- Runs database migrations via `scripts/db/apply-migrations.sh --env Production`
- Pulls and retags Docker images for production
- Records deployment metadata

#### 3. `smoke-check`
- Waits 30s for service stabilization
- Uses `scripts/deploy/smoke-check.sh` to verify:
  - `/live` endpoint
  - `/ready` endpoint  
  - Frontend availability
- Skippable via `skip_smoke` input

#### 4. `rollback`
- **Triggers only on**: smoke-check failure AND `rollback_tag` provided
- Uses `scripts/deploy/rollback.sh` to re-deploy previous version
- Verifies health after rollback

## Approval Gate

The `deploy-production` job uses GitHub's environment protection rules:
1. Create a `production` environment in repository Settings → Environments
2. Add required reviewers (e.g., tech leads, SRE team)
3. Optionally add wait timer and branch restrictions

## Scripts

### `scripts/deploy/smoke-check.sh`
```bash
bash scripts/deploy/smoke-check.sh \
  --api-url https://api.nextraceone.local \
  --frontend-url https://app.nextraceone.local \
  --timeout 30
```

### `scripts/deploy/rollback.sh`
```bash
bash scripts/deploy/rollback.sh \
  --tag abc123 \
  --registry ghcr.io/org/nextraceone
```

## Required Secrets

| Secret | Description |
|--------|-------------|
| `PRODUCTION_CONN_IDENTITY` | PostgreSQL connection — identity database |
| `PRODUCTION_CONN_CATALOG` | PostgreSQL connection — catalog database |
| `PRODUCTION_CONN_OPERATIONS` | PostgreSQL connection — operations database |
| `PRODUCTION_CONN_AI` | PostgreSQL connection — AI database |
| `PRODUCTION_APIHOST_URL` | Production API URL for smoke checks |
| `PRODUCTION_FRONTEND_URL` | Production frontend URL for smoke checks |
