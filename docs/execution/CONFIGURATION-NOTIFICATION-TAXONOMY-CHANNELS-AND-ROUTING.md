# Configuration — Notification Taxonomy, Channels, and Routing

## Notification Taxonomy

### Types

All 29 notification types are configurable via `notifications.types.enabled`. This JSON array controls which event types are processed by the notification engine.

**Default enabled types:**
- Operations/Incidents: IncidentCreated, IncidentEscalated, IncidentResolved, AnomalyDetected, HealthDegradation
- Workflow/Approval: ApprovalPending, ApprovalApproved, ApprovalRejected, ApprovalExpiring
- Catalog/Contracts: ContractPublished, BreakingChangeDetected, ContractValidationFailed
- Security/Access: BreakGlassActivated, JitAccessPending, JitAccessGranted, UserRoleChanged, AccessReviewPending
- Compliance/Governance: ComplianceCheckFailed, PolicyViolated, EvidenceExpiring
- FinOps/Budget: BudgetExceeded, BudgetThresholdReached
- Integrations: IntegrationFailed, SyncFailed, ConnectorAuthFailed
- AI/Platform: AiProviderUnavailable, TokenBudgetExceeded, AiGenerationFailed, AiActionBlockedByPolicy

**Scopes:** System, Tenant, Environment

### Categories

All 11 categories configurable via `notifications.categories.enabled`:
Incident, Approval, Change, Contract, Security, Compliance, FinOps, AI, Integration, Platform, Informational

**Scopes:** System, Tenant

### Severities

- `notifications.severity.default`: Default severity when not specified (default: Info)
- `notifications.severity.minimum_for_external`: Minimum severity for Email/Teams delivery (default: Warning)

**Allowed values:** Info, ActionRequired, Warning, Critical

### Mandatory Policies

- `notifications.mandatory.types`: Types that cannot be disabled by users (System-only, non-inheritable)
  - Default: BreakGlassActivated, IncidentCreated, IncidentEscalated, ApprovalPending, ComplianceCheckFailed
- `notifications.mandatory.severities`: Severities always mandatory (System-only, non-inheritable)
  - Default: Critical

## Channels

### Channel Enable/Disable

| Key | Default | Scopes |
|-----|---------|--------|
| `notifications.enabled` | true | System, Tenant |
| `notifications.channels.inapp.enabled` | true | System, Tenant |
| `notifications.email.enabled` | true | System, Tenant |
| `notifications.teams.enabled` | true | System, Tenant |

### Channels by Type

`notifications.channels.allowed_by_type` — JSON object mapping types to allowed channels.
Default: `{}` (all channels allowed for all types)

### Mandatory Channels by Severity

`notifications.channels.mandatory_by_severity` — System-only, non-inheritable.
Default:
```json
{
  "Critical": ["InApp", "Email", "MicrosoftTeams"],
  "Warning": ["InApp", "Email"]
}
```

### Mandatory Channels by Type

`notifications.channels.mandatory_by_type` — System-only, non-inheritable.
Default:
```json
{
  "BreakGlassActivated": ["InApp", "Email", "MicrosoftTeams"],
  "ApprovalPending": ["InApp", "Email"],
  "ComplianceCheckFailed": ["InApp", "Email"]
}
```

### Environment-Level Channel Disabling

`notifications.channels.disabled_in_environment` — Environment-scoped, non-inheritable.
Allows disabling external channels in non-production environments.
Default: `[]`

## Routing and Fallback

### Default Routing Policy

`notifications.routing.default_policy`:
```json
{
  "ownerFirst": true,
  "adminFallback": true,
  "approverRouting": false
}
```

### Category-Based Routing

`notifications.routing.by_category`:
```json
{
  "Incident": {"recipientType": "owner", "fallbackToAdmin": true},
  "Approval": {"recipientType": "approver", "fallbackToAdmin": true},
  "Security": {"recipientType": "admin", "fallbackToAdmin": false}
}
```

### Severity-Based Routing

`notifications.routing.by_severity`:
```json
{
  "Critical": {"notifyAdmins": true, "broadcastToTeam": true},
  "Warning": {"notifyAdmins": false, "broadcastToTeam": false}
}
```

### Fallback Recipients

`notifications.routing.fallback_recipients` — JSON array of fallback recipient identifiers.
Default: `[]` (must be configured per tenant)

## Governance Safeguards

1. **Mandatory types cannot be overridden** — System-only scope prevents tenant/user override
2. **Channel consistency** — Mandatory channels by severity ensure Critical notifications always reach all channels
3. **Environment isolation** — Channel disabling per environment prevents dev noise
4. **Validation rules** — All severity enums validated against allowed values
5. **Audit trail** — All changes logged with user, timestamp, and reason
