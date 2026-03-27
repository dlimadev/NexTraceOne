# Configuration — Promotion Paths, Release Windows and Freeze Policies

## Overview

This document describes the parameterization of promotion governance, environment progression rules, release windows and freeze policies delivered in Phase 3.

## Promotion Paths

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `promotion.paths.allowed` | Json | Sequential Dev→Test→QA→PreProd→Prod | System, Tenant | Allowed promotion paths |
| `promotion.production.extra_approvers_required` | Integer | `1` | System, Tenant | Extra approvers for production (0–10) |
| `promotion.production.extra_gates` | Json | `["SecurityScan","ComplianceCheck","PerformanceBaseline"]` | System, Tenant | Extra production gates |
| `promotion.restrictions.by_criticality` | Json | Critical/High require extra approvers + evidence | System, Tenant | Criticality-based restrictions |
| `promotion.rollback.recommendation_enabled` | Boolean | `true` | System, Tenant | Rollback recommendation |

### Default Promotion Path

```json
[
  {"source": "Development", "targets": ["Test"]},
  {"source": "Test", "targets": ["QA"]},
  {"source": "QA", "targets": ["PreProduction"]},
  {"source": "PreProduction", "targets": ["Production"]}
]
```

This enforces a sequential environment progression: Development → Test → QA → PreProduction → Production. Tenants can customize this path (e.g., adding parallel paths or skipping environments for specific use cases).

### Production Rules

- **Extra approvers**: Production promotion requires at least 1 additional approver by default
- **Extra gates**: SecurityScan, ComplianceCheck, and PerformanceBaseline are required for production
- **Criticality restrictions**: Critical services require 2 extra approvers + evidence pack; High services require 1 + evidence

### Criticality Restrictions Default

```json
{
  "critical": {"requireAdditionalApprovers": 2, "requireEvidencePack": true},
  "high": {"requireAdditionalApprovers": 1, "requireEvidencePack": true}
}
```

## Release Windows

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `promotion.release_window.enabled` | Boolean | `false` | System, Tenant, Environment | Release window enforcement (opt-in) |
| `promotion.release_window.schedule` | Json | Weekdays 06:00–18:00 UTC | System, Tenant, Environment | Release window schedule |

### Default Release Window

```json
{
  "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
  "startTimeUtc": "06:00",
  "endTimeUtc": "18:00"
}
```

Release windows are **disabled by default** and support environment-level override, allowing production to have stricter windows than development.

## Freeze Policies

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `promotion.freeze.enabled` | Boolean | `false` | System, Tenant, Environment | Freeze enforcement (opt-in) |
| `promotion.freeze.windows` | Json | `[]` | System, Tenant, Environment | Freeze window definitions |
| `promotion.freeze.override_allowed` | Boolean | `false` | System (non-inheritable) | Freeze override capability |
| `promotion.freeze.override_roles` | Json | `["PlatformAdmin"]` | System (non-inheritable) | Authorized override roles |

### Safety Controls

- Freeze windows are **disabled by default** — opt-in only
- Freeze override is **system-level only** and **non-inheritable** — tenants cannot grant freeze override to themselves
- Only `PlatformAdmin` role can override freeze windows by default
- Override requires justification (tracked in audit)

### Freeze Window Example

```json
[
  {
    "name": "Year-End Freeze",
    "start": "2026-12-20T00:00:00Z",
    "end": "2027-01-05T00:00:00Z",
    "reason": "Year-end code freeze"
  }
]
```

## Environment Awareness

All promotion governance settings leverage the environment model defined in Phase 1:
- `environment.classification` determines which promotion path applies
- `environment.is_production` triggers production-specific rules
- `environment.criticality` influences gate and approver requirements
- Release windows and freeze policies can be set per environment

## Effective Settings

The effective settings explorer shows:
- The complete promotion path resolution per tenant
- Production-specific rules (extra approvers, gates)
- Active release windows and freeze periods
- Whether freeze override is allowed and by whom
- Origin of each value (system default, tenant override, environment override)
