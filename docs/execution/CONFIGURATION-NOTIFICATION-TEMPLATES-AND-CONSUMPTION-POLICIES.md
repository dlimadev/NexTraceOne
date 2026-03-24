# Configuration — Notification Templates and Consumption Policies

## Templates

### Internal Notification Templates

**Key:** `notifications.templates.internal`
**Type:** JSON
**Scopes:** System, Tenant

Each entry maps a notification type to a template object:
```json
{
  "IncidentCreated": {
    "title": "Incident created — {ServiceName}",
    "message": "A new incident with severity {IncidentSeverity} has been created for service {ServiceName}.",
    "placeholders": ["ServiceName", "IncidentSeverity"]
  }
}
```

The template resolver uses these parameterized templates to generate notification content. Templates support:
- **Title** — Short summary with placeholder substitution
- **Message** — Detailed body with placeholder substitution
- **Placeholders** — List of supported variables for validation

### Email Templates

**Key:** `notifications.templates.email`
**Type:** JSON
**Scopes:** System, Tenant

```json
{
  "default": {
    "subject": "[NexTraceOne] {Title}",
    "bodyHtml": "<h2>{Title}</h2><p>{Message}</p><p><a href='{ActionUrl}'>View details</a></p>",
    "placeholders": ["Title", "Message", "ActionUrl"]
  }
}
```

### Microsoft Teams Templates

**Key:** `notifications.templates.teams`
**Type:** JSON
**Scopes:** System, Tenant

```json
{
  "default": {
    "cardTitle": "NexTraceOne — {Title}",
    "cardBody": "{Message}",
    "actionUrl": "{ActionUrl}",
    "placeholders": ["Title", "Message", "ActionUrl"]
  }
}
```

### Template Governance

- Templates can be overridden per tenant for customization
- System-level templates serve as defaults
- Placeholders must be validated against the template engine
- Future: i18n support via locale-keyed template variants

## Consumption Policies

### Default Preferences

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.preferences.default_by_tenant` | `{"emailEnabled":true,"teamsEnabled":true,"digestEnabled":false}` | System, Tenant | Default preferences for all users in a tenant |
| `notifications.preferences.default_by_role` | `{}` | System, Tenant, Role | Role-specific default preferences |

### Quiet Hours

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.quiet_hours.enabled` | `true` | System, Tenant, User | Master switch for quiet hours |
| `notifications.quiet_hours.start` | `22:00` | System, Tenant, User | Start time (HH:mm) |
| `notifications.quiet_hours.end` | `08:00` | System, Tenant, User | End time (HH:mm) |
| `notifications.quiet_hours.bypass_categories` | `["Incident","Security"]` | System | Categories that bypass quiet hours (non-inheritable) |

**Safety rule:** Categories in `bypass_categories` are never deferred, ensuring critical incident and security notifications always reach recipients.

### Digest

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.digest.enabled` | `false` | System, Tenant, User | Master switch for digest summaries |
| `notifications.digest.period_hours` | `24` | System, Tenant, User | Digest generation interval (1-168 hours) |
| `notifications.digest.eligible_categories` | `["Informational","Change","Integration","Platform"]` | System, Tenant | Categories eligible for digest aggregation |

**Safety rule:** Critical categories (Incident, Security, Compliance) should NOT be included in digest-eligible categories.

### Suppression

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.suppress.enabled` | `true` | System, Tenant | Master switch for suppression rules |
| `notifications.suppress.acknowledged_window_minutes` | `30` | System, Tenant | Window for acknowledged-entity suppression (5-1440 min) |

**Safety rules:**
- Critical severity notifications are never suppressed
- Mandatory notification types are never suppressed
- Suppression only applies after explicit acknowledgment

### Acknowledgment

| Key | Default | Scopes | Description |
|-----|---------|--------|-------------|
| `notifications.acknowledge.required_categories` | `["Incident","Security","Compliance"]` | System, Tenant | Categories requiring explicit acknowledgment |

## Effective Settings Explorer

The notification configuration admin page shows effective values with:
- **Default** badge — using system default value
- **Inherited** badge — value resolved from parent scope
- **Override** badge — value explicitly set at current scope
- **Source indicator** — which scope the value resolved from
- **Audit history** — expandable panel per configuration key

This allows administrators to understand the final behavior of the notification platform without ambiguity.
