# Configuration — Notification Escalation, Dedup, and Incident Linkage

## Deduplication

### Configuration Keys

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.dedup.enabled` | `true` | System, Tenant | Master switch for deduplication |
| `notifications.dedup.window_minutes` | `5` | System, Tenant | Default dedup window (1-1440 min) |
| `notifications.dedup.window_by_category` | `{"Incident":10,"Security":10,"Integration":15}` | System, Tenant | Category-specific dedup windows |

### Behavior

When deduplication is enabled, the notification engine checks for existing notifications matching the same tenant + recipient + event type + source entity within the configured window. If a match is found, the notification is suppressed.

Category-specific windows override the default window for listed categories.

### Current Implementation

The `NotificationDeduplicationService` uses a default 5-minute window (hardcoded). With Phase 2 parameterization, this value is now configurable and can be varied per category.

## Escalation

### Configuration Keys

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.escalation.enabled` | `true` | System, Tenant | Master switch for escalation |
| `notifications.escalation.critical_threshold_minutes` | `30` | System, Tenant | Minutes before Critical escalation (5-1440) |
| `notifications.escalation.action_required_threshold_minutes` | `120` | System, Tenant | Minutes before ActionRequired escalation (15-2880) |
| `notifications.escalation.channels` | `["InApp","Email","MicrosoftTeams"]` | System, Tenant | Channels used for escalation delivery |

### Behavior

When escalation is enabled, unacknowledged notifications that exceed their severity-specific threshold are marked as escalated. The escalation service:

1. Checks if the notification is eligible (not already escalated, acknowledged, archived, dismissed, or snoozed)
2. Compares the notification age against the configured threshold
3. Marks the notification as escalated
4. Triggers delivery through escalation channels

### Current Implementation

The `NotificationEscalationService` uses hardcoded thresholds (Critical: 30 min, ActionRequired: 2 hours). These values are now configurable definitions that can be tuned per tenant.

## Notification Grouping

### Configuration Keys

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.grouping.window_minutes` | `60` | System, Tenant | Window for grouping correlated notifications (5-1440 min) |

### Behavior

The grouping service generates deterministic correlation keys and resolves notification groups within the configured window. Related notifications are grouped under the same group ID.

## Incident Linkage

### Configuration Keys

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.incident_linkage.enabled` | `false` | System, Tenant | Master switch for incident linkage |
| `notifications.incident_linkage.auto_create_enabled` | `false` | System, Tenant | Auto-create incidents from critical notifications |
| `notifications.incident_linkage.eligible_types` | `["IncidentCreated","IncidentEscalated","HealthDegradation","AnomalyDetected"]` | System, Tenant | Types eligible for incident linkage |
| `notifications.incident_linkage.correlation_window_minutes` | `60` | System, Tenant | Window for correlating with existing incidents (5-1440 min) |

### Behavior

When incident linkage is enabled:
1. Eligible notification types are checked against existing incidents using the correlation key
2. If a matching incident exists within the correlation window, the notification is linked to it
3. If auto-creation is enabled and no matching incident exists, a new incident is created

### Safety Rules

1. **Disabled by default** — Both `enabled` and `auto_create_enabled` default to `false`
2. **Explicit opt-in** — Auto-creation must be explicitly enabled per tenant
3. **Type restriction** — Only eligible types trigger incident linkage
4. **Correlation window** — Prevents spurious incident creation from stale events
5. **Audit trail** — All linkage and auto-creation events are audited

## Safeguards Summary

| Safeguard | Implementation |
|-----------|---------------|
| Critical notifications never suppressed | `MandatoryNotificationPolicy` + `notifications.mandatory.severities` |
| Mandatory types never suppressed | `MandatoryNotificationPolicy` + `notifications.mandatory.types` |
| Incident auto-creation off by default | `notifications.incident_linkage.auto_create_enabled = false` |
| Escalation thresholds validated | Min/max validation rules on threshold definitions |
| Dedup windows bounded | Min 1, max 1440 minutes |
| Correlation windows bounded | Min 5, max 1440 minutes |
| All changes audited | Configuration audit trail on every write |
