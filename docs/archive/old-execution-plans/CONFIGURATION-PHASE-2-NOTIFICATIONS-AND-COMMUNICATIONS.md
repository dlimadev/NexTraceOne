# Configuration Phase 2 — Notifications and Communications Parameterization

## Objective

Phase 2 delivers the functional parameterization of the NexTraceOne notification and communication platform. All functional behavior that was previously hardcoded, implicit, or embedded in code is now governed by the configuration capability built in Phases 0 and 1.

## Scope Delivered

### Configuration Definitions Added (38 new definitions)

| Block | Definitions | Keys |
|-------|------------|------|
| Types, Categories & Severities | 6 | `notifications.types.enabled`, `notifications.categories.enabled`, `notifications.severity.default`, `notifications.severity.minimum_for_external`, `notifications.mandatory.types`, `notifications.mandatory.severities` |
| Channels Allowed & Mandatory | 5 | `notifications.channels.inapp.enabled`, `notifications.channels.allowed_by_type`, `notifications.channels.mandatory_by_severity`, `notifications.channels.mandatory_by_type`, `notifications.channels.disabled_in_environment` |
| Templates | 3 | `notifications.templates.internal`, `notifications.templates.email`, `notifications.templates.teams` |
| Routing & Fallback | 4 | `notifications.routing.default_policy`, `notifications.routing.fallback_recipients`, `notifications.routing.by_category`, `notifications.routing.by_severity` |
| Preferences, Quiet Hours, Digest & Suppression | 10 | `notifications.preferences.default_by_tenant`, `notifications.preferences.default_by_role`, `notifications.quiet_hours.enabled`, `notifications.quiet_hours.bypass_categories`, `notifications.digest.enabled`, `notifications.digest.period_hours`, `notifications.digest.eligible_categories`, `notifications.suppress.enabled`, `notifications.suppress.acknowledged_window_minutes`, `notifications.acknowledge.required_categories` |
| Escalation, Dedup & Incident Linkage | 10 | `notifications.dedup.enabled`, `notifications.dedup.window_minutes`, `notifications.dedup.window_by_category`, `notifications.escalation.enabled`, `notifications.escalation.critical_threshold_minutes`, `notifications.escalation.action_required_threshold_minutes`, `notifications.escalation.channels`, `notifications.incident_linkage.enabled`, `notifications.incident_linkage.auto_create_enabled`, `notifications.incident_linkage.eligible_types`, `notifications.incident_linkage.correlation_window_minutes`, `notifications.grouping.window_minutes` |

### Combined with Phase 0/1 notification definitions (5 existing):
- `notifications.enabled`
- `notifications.email.enabled`
- `notifications.teams.enabled`
- `notifications.quiet_hours.start`
- `notifications.quiet_hours.end`

**Total notification configuration definitions: 43**

## Key Design Decisions

### 1. All definitions are Functional category
Notification configuration is product behavior, not bootstrap or sensitive operational data. Channel credentials remain in `SensitiveOperational` or external configuration.

### 2. Mandatory types and severities are System-only and non-inheritable
`notifications.mandatory.types` and `notifications.mandatory.severities` can only be set at the System level and cannot be overridden by tenant or environment scopes. This ensures critical notifications cannot be silenced.

### 3. Incident linkage is disabled by default
Both `notifications.incident_linkage.enabled` and `notifications.incident_linkage.auto_create_enabled` default to `false` because automatic incident creation requires careful governance.

### 4. JSON type for complex policies
Channel mapping, routing rules, template definitions, and category-specific configurations use `ConfigurationValueType.Json` with `json-editor` UI type.

### 5. Environment-scoped channel disabling
`notifications.channels.disabled_in_environment` allows development environments to disable Email and Teams channels without affecting production.

## Frontend Delivery

- **NotificationConfigurationPage** at `/platform/configuration/notifications`
- 6 sections: Types/Severities, Channels, Templates, Routing/Fallback, Quiet Hours/Digest/Suppress, Escalation/Dedup/Incidents
- Effective settings explorer with inheritance/override/default badges
- i18n support for en, pt-BR, pt-PT, es

## Testing

- 20 new backend tests for Phase 2 definitions
- 84 total configuration tests pass
- 412 notification tests pass (no regression)

## Impact on Next Phases

Phase 3 (Workflows, Approvals & Promotion Governance) can now build upon:
- The configuration capability with 43 notification definitions
- The effective settings explorer pattern
- The multi-tenant, multi-scope inheritance model
- The administrative UI pattern established in this phase
