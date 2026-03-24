# Configuration — Integration Filters, Mappings, Failure Reaction & Governance

## Sync Filters & Mappings

### Sync Filter Policy (`integrations.sync.filter_policy`)
- Default: Exclude archived (true), exclude deleted (true), max age 720 hours
- Scope: System, Tenant

### Sync Mapping Policy (`integrations.sync.mapping_policy`)
- Auto-map by name: enabled
- Strict type validation: enabled
- Unmapped field action: Ignore

### Overwrite Behavior (`integrations.sync.overwrite_behavior`)
- Options: Overwrite, Merge, Skip
- Default: Merge

### Pre-Sync Validation (`integrations.sync.pre_validation_enabled`)
- Default: enabled

## Import/Export Policies

### Import Policy (`integrations.import.policy`)
- Allow overwrite: false (safe default)
- Require validation: true
- On conflict: Skip
- Max batch size: 1000

### Export Policy (`integrations.export.policy`)
- Include metadata: true
- Default format: JSON
- Max records: 10000
- Sanitize sensitive: true

## Freshness & Staleness

### Default Staleness Threshold (`integrations.freshness.staleness_threshold_hours`)
- Default: 24 hours (range 1-168)

### Freshness by Connector (`integrations.freshness.by_connector`)
Custom staleness thresholds:
- Prometheus/PagerDuty/Datadog: 1 hour
- ServiceNow: 6 hours
- AzureDevOps/GitHub: 12 hours
- Jira: 24 hours

## Failure Notification & Reaction

### Notification Policy (`integrations.failure.notification_policy`)
- Notify on first failure: true
- Notify after N consecutive failures: 3
- Notify on auth failure: true
- Notify on staleness: true
- Digest frequency: Hourly

### Failure Severity Mapping (`integrations.failure.severity_mapping`)
- Auth failure: Critical
- Sync failure: High
- Timeout failure: Medium
- Validation failure: Low
- Stale data: Medium

### Escalation Policy (`integrations.failure.escalation_policy`)
- Critical: escalate after 15 min → platform-admin
- High: escalate after 60 min → integration-owner
- Medium: escalate after 240 min
- Low: escalate after 1440 min

### Auth Failure Reaction (`integrations.failure.auth_reaction_policy`)
- Pause sync: true
- Notify owner: true
- Auto retry after: 60 minutes
- Max auth retries: 3

## Auto-Disable & Protection

### Auto-Disable on Failure (`integrations.failure.auto_disable_enabled`)
- Default: enabled

### Auto-Disable Threshold (`integrations.failure.auto_disable_threshold`)
- Default: 5 consecutive failures (range 2-50)

### Fallback Owner (`integrations.owner.fallback_recipient`)
- Default: platform-admin

### Blocked in Production (`integrations.governance.blocked_in_production`)
- System-only, non-inheritable
- Default blocked operations: bulkDelete, schemaOverwrite, forceReSync
