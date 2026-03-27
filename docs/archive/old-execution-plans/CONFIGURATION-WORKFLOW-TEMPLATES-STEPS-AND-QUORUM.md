# Configuration — Workflow Templates, Steps and Quorum

## Overview

This document describes the parameterization of workflow templates, stages, sequencing and quorum rules delivered in Phase 3 of the NexTraceOne configuration platform.

## Workflow Types & Templates

### Configuration Keys

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.types.enabled` | Json | `["ReleaseApproval","PromotionApproval","WaiverApproval","GovernanceReview"]` | System, Tenant | Enabled workflow types |
| `workflow.templates.default` | Json | Standard single-stage template | System, Tenant, Environment | Default workflow template definition |
| `workflow.templates.by_change_level` | Json | `{"1":"Standard","2":"Enhanced","3":"FullGovernance"}` | System, Tenant | Map of change level to template name |
| `workflow.templates.active_version` | Integer | `1` | System, Tenant | Active template version number |

### Template Structure

The default template JSON contains:
```json
{
  "name": "Standard Approval",
  "stages": [
    {
      "name": "Review",
      "order": 1,
      "requiredApprovals": 1,
      "approvalRule": "SingleApprover"
    }
  ]
}
```

Templates are versionable — `workflow.templates.active_version` controls which version is active.

## Stages & Sequencing

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.stages.max_count` | Integer | `10` | System, Tenant | Maximum stages per workflow (1–50) |
| `workflow.stages.allow_parallel` | Boolean | `false` | System, Tenant | Whether parallel stages are permitted |
| `workflow.stages.allow_optional` | Boolean | `true` | System, Tenant | Whether optional stages are permitted |

### Design Decisions

- **Parallel stages** are disabled by default because the current workflow engine processes stages sequentially. This can be enabled when the engine supports true parallelism.
- **Maximum stages** defaults to 10, validated between 1 and 50 to prevent excessively complex workflows.

## Quorum

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.quorum.default_rule` | String | `SingleApprover` | System, Tenant, Environment | Quorum rule (SingleApprover, Majority, Unanimous) |
| `workflow.quorum.minimum_approvers` | Integer | `1` | System, Tenant, Environment | Minimum approvers per stage (1–20) |

### Quorum Rules

- **SingleApprover**: One approval is sufficient
- **Majority**: More than half of assigned approvers must approve
- **Unanimous**: All assigned approvers must approve

The quorum rule supports **environment-level override**, enabling stricter quorum for production environments.

## Effective Settings

The effective settings explorer shows:
- Which template is active (resolved from System → Tenant → Environment)
- Whether parallel/optional stages are enabled
- The effective quorum rule per environment
- The origin of each value (default, inherited, or overridden)
