# Configuration — Waivers and Governance Pack Bindings

## Overview

This document describes the parameterization of waiver lifecycle rules and governance pack bindings delivered in Phase 4.

## Waiver Rules

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.waiver.eligible_policies` | Json | ApiVersioning, DocumentationCoverage, TestCoverage | System, Tenant | Policies eligible for waiver |
| `governance.waiver.blocked_severities` | Json | ["Critical"] | System (non-inheritable) | Severities that cannot be waived |
| `governance.waiver.validity_days_default` | Integer | 30 | System, Tenant | Default waiver validity (1-365 days) |
| `governance.waiver.validity_days_max` | Integer | 90 | System, Tenant | Maximum waiver validity (1-365 days) |
| `governance.waiver.require_approval` | Boolean | true | System, Tenant | Approval required |
| `governance.waiver.require_evidence` | Boolean | true | System, Tenant | Evidence/justification required |
| `governance.waiver.allowed_environments` | Json | Dev, Test, QA | System, Tenant | Environments where waivers are permitted |
| `governance.waiver.blocked_environments` | Json | ["Production"] | System (non-inheritable) | Environments where waivers are never allowed |
| `governance.waiver.renewal.allowed` | Boolean | true | System, Tenant | Whether renewal is allowed |
| `governance.waiver.renewal.max_count` | Integer | 2 | System, Tenant | Maximum renewals (0-10) |

### Safety Controls

- **Critical policies cannot be waived** — `governance.waiver.blocked_severities` is system-only and non-inheritable
- **Production is blocked** — `governance.waiver.blocked_environments` is system-only and non-inheritable
- **Approval is required by default** — separation of duties enforced
- **Evidence is required by default** — auditability enforced
- **Maximum validity is 90 days** — prevents indefinite waivers

### Waiver Lifecycle

1. Request: User requests waiver for eligible policy
2. Validation: System checks eligibility (severity, environment, limits)
3. Approval: Required if `governance.waiver.require_approval` is true
4. Evidence: Required if `governance.waiver.require_evidence` is true
5. Active: Waiver granted for configured validity period
6. Expiry: Waiver expires after validity period
7. Renewal: Optional renewal up to `governance.waiver.renewal.max_count` times

## Governance Packs

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `governance.packs.enabled` | Json | CoreGovernance, ApiGovernance, SecurityHardening | System, Tenant | Enabled packs |
| `governance.packs.active_version` | Integer | 1 | System, Tenant | Active version number (1-9999) |
| `governance.packs.binding_policy` | Json | Bind by tenant, environment, systemType | System, Tenant | Binding rules |
| `governance.packs.by_environment` | Json | Environment→Pack bindings | System, Tenant | Packs per environment |
| `governance.packs.by_system_type` | Json | SystemType→Pack bindings | System, Tenant | Packs per system type |
| `governance.packs.overlap_resolution` | String | MostRestrictive | System, Tenant | Overlap strategy |

### Default Pack Bindings

#### By Environment
| Environment | Packs |
|-------------|-------|
| Production | CoreGovernance, SecurityHardening |
| PreProduction | CoreGovernance |
| Development | CoreGovernance |

#### By System Type
| System Type | Packs |
|-------------|-------|
| REST | ApiGovernance, CoreGovernance |
| SOAP | ApiGovernance, CoreGovernance |
| Event | CoreGovernance |
| Background | CoreGovernance |

### Overlap Resolution Strategies

- **MostRestrictive** (default): When packs overlap, the most restrictive policy wins
- **MostSpecific**: The most specifically targeted pack takes precedence
- **Merge**: Policies from all applicable packs are combined

### Pack Versioning

- `governance.packs.active_version` controls the active version
- Version changes are audited
- Binding policy determines how packs are assigned to targets

## Effective Settings

The effective settings explorer shows:
- Active packs and their bindings per tenant
- Waiver eligibility per policy
- Waiver blocked environments and severities
- Pack overlap resolution strategy
- Origin of each value (system default, tenant override)
