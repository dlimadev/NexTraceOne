# Configuration — Environment Model

## Overview

Environments in NexTraceOne are governed entities with formal classification, criticality levels, and structural policies. Phase 1 transforms environments from implicit labels into administrable, auditable configuration objects.

## Environment Definitions

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `environment.classification` | String | — | Development, Test, QA, PreProduction, Production, Lab |
| `environment.is_production` | Boolean | false | Formal production designation |
| `environment.criticality` | String | medium | low, medium, high, critical |
| `environment.lifecycle_order` | Integer | 0 | Position in deployment pipeline (0-100) |
| `environment.description` | String | — | Purpose description |
| `environment.active` | Boolean | true | Whether environment is active |

## Production Environment Designation

### Rules
- `environment.is_production` is a **non-inheritable** Boolean flag
- Each environment must be explicitly marked (not inferred from name)
- Production designation triggers stricter governance policies
- The flag is auditable — every change is tracked with reason

### Governance Implications
When `environment.is_production = true`:
- Change approval requirements may be enforced
- Automation restrictions may apply
- Change freeze policies become more critical
- Drift analysis is typically mandatory
- Sensitive feature restrictions are recommended

## Environment Policies

Structural policies control behavior per environment:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `policy.environment.allow_automation` | Boolean | true | Allow automated operations |
| `policy.environment.allow_promotion_target` | Boolean | true | Can receive promotions |
| `policy.environment.allow_promotion_source` | Boolean | true | Can be promotion source |
| `policy.environment.require_approval_for_changes` | Boolean | false | Changes need approval |
| `policy.environment.allow_drift_analysis` | Boolean | true | Drift analysis active |
| `policy.environment.restrict_sensitive_features` | Boolean | false | Restrict sensitive features |
| `policy.environment.change_freeze.enabled` | Boolean | false | Change freeze active |
| `policy.environment.change_freeze.reason` | String | — | Freeze reason |

## Validation Rules
- Environment classification must be one of: Development, Test, QA, PreProduction, Production, Lab
- Criticality must be one of: low, medium, high, critical
- Lifecycle order must be between 0 and 100
- Production designation is independent of classification (explicit governance decision)
