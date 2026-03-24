# Configuration — Approvers, SLAs, Escalation and Gates

## Overview

This document describes the parameterization of approver policies, SLAs, deadlines, escalation, gates, checklists and auto-approval rules delivered in Phase 3.

## Approvers & Fallback

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.approvers.policy` | Json | `{"strategy":"ByOwnership","roles":["TechLead","Architect"]}` | System, Tenant, Environment | Approver resolution policy |
| `workflow.approvers.fallback` | Json | `{"enabled":true,"fallbackRoles":["PlatformAdmin"]}` | System, Tenant | Fallback approver policy |
| `workflow.approvers.self_approval_allowed` | Boolean | `false` | System, Tenant, Environment | Separation of duties control |

### Design Decisions

- **Self-approval is disabled by default** to enforce separation of duties — the requester should not approve their own workflow.
- **Fallback approvers** default to PlatformAdmin role, ensuring no workflow is ever left without a resolvable approver.
- Approver policy supports environment override to allow stricter policies in production.

## Escalation

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.escalation.enabled` | Boolean | `true` | System, Tenant | Escalation enabled |
| `workflow.escalation.delay_minutes` | Integer | `240` (4h) | System, Tenant, Environment | Escalation delay (15–10080 min) |
| `workflow.escalation.target_roles` | Json | `["PlatformAdmin","Architect"]` | System, Tenant | Escalation targets |
| `workflow.escalation.by_criticality` | Json | Critical: 60min, High: 120min, Medium: 240min | System, Tenant | Criticality-based escalation |

### Escalation by Criticality

```json
{
  "critical": { "delayMinutes": 60, "targets": ["PlatformAdmin"] },
  "high": { "delayMinutes": 120, "targets": ["TechLead"] },
  "medium": { "delayMinutes": 240, "targets": ["TechLead"] }
}
```

## SLAs, Deadlines & Timeout

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.sla.default_hours` | Integer | `48` | System, Tenant, Environment | Default SLA (1–720h) |
| `workflow.sla.by_type` | Json | Release: 24h, Promotion: 8h, Waiver: 48h, Review: 72h | System, Tenant | SLA by workflow type |
| `workflow.sla.by_environment` | Json | Production: 4h, PreProduction: 8h | System, Tenant | SLA by environment |
| `workflow.timeout.approval_hours` | Integer | `72` | System, Tenant, Environment | Individual approval timeout (1–720h) |
| `workflow.expiry.hours` | Integer | `168` (7 days) | System, Tenant, Environment | Full workflow expiry (1–2160h) |
| `workflow.expiry.action` | String | `Cancel` | System, Tenant | Expiry action (Cancel, Escalate, Notify) |
| `workflow.resubmission.allowed` | Boolean | `true` | System, Tenant | Re-submission after rejection |
| `workflow.resubmission.max_attempts` | Integer | `3` | System, Tenant | Max re-submission attempts (1–10) |

### SLA Hierarchy

SLA resolution order:
1. Environment-specific SLA (e.g., Production = 4h)
2. Workflow type-specific SLA (e.g., PromotionApproval = 8h)
3. Default SLA (48h)

## Gates & Checklists

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.gates.enabled` | Boolean | `true` | System, Tenant, Environment | Gates enforcement |
| `workflow.gates.by_environment` | Json | Production: full gates, Dev: none | System, Tenant | Gates by environment |
| `workflow.gates.by_criticality` | Json | Critical: all gates, Low: none | System, Tenant | Gates by criticality |
| `workflow.checklist.by_type` | Json | Per-type checklist items | System, Tenant | Checklists by workflow type |
| `workflow.checklist.by_environment` | Json | Production: readiness items | System, Tenant | Checklists by environment |
| `workflow.evidence_pack.required` | Boolean | `false` | System, Tenant, Environment | Evidence pack requirement |
| `workflow.rejection.require_reason` | Boolean | `true` | System, Tenant | Reason required on rejection |

### Production Gates (Default)

```json
["SecurityScan", "TestCoverage", "ApprovalComplete", "EvidencePack"]
```

## Auto-Approval

| Key | Type | Default | Scopes | Description |
|-----|------|---------|--------|-------------|
| `workflow.auto_approval.enabled` | Boolean | `false` | System, Tenant, Environment | Auto-approval (opt-in) |
| `workflow.auto_approval.conditions` | Json | Low-risk, non-production only | System, Tenant | Auto-approval conditions |
| `workflow.auto_approval.blocked_environments` | Json | `["Production"]` | System (non-inheritable) | Never auto-approve here |

### Safety Controls

- Auto-approval is **disabled by default** — opt-in only
- Production is **blocked from auto-approval** at system level and cannot be overridden by tenants
- Auto-approval conditions enforce `requireAllGatesPassed: true` by default
