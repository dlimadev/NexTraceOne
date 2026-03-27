# Configuration — Integrations Connectors, Schedules, Retries & Timeouts

## Connector Enablement

### Enabled Connectors (`integrations.connectors.enabled`)
- Default: AzureDevOps, GitHub, Jira, ServiceNow, PagerDuty, Datadog, Prometheus
- Scope: System, Tenant

### Connectors by Environment (`integrations.connectors.enabled_by_environment`)
Per-environment connector enablement overrides:
- Production: AzureDevOps, GitHub, ServiceNow, PagerDuty, Datadog, Prometheus
- Development: AzureDevOps, GitHub, Jira

## Sync Schedules

### Default Schedule (`integrations.schedule.default`)
- Default: `0 */6 * * *` (every 6 hours)
- Scope: System, Tenant

### Schedule by Connector (`integrations.schedule.by_connector`)
Custom cron schedules per connector:
- Prometheus: every 5 minutes
- PagerDuty: every 30 minutes
- AzureDevOps/GitHub: every 4 hours
- ServiceNow: every 2 hours

## Retry Configuration

### Max Attempts (`integrations.retry.max_attempts`)
- Default: 3 (range 0-10)

### Backoff Seconds (`integrations.retry.backoff_seconds`)
- Default: 30 (range 5-600)

### Exponential Backoff (`integrations.retry.exponential_backoff`)
- Default: enabled

## Timeout Configuration

### Default Timeout (`integrations.timeout.default_seconds`)
- Default: 120 seconds (range 10-3600)

### Timeout by Connector (`integrations.timeout.by_connector`)
Custom timeouts per connector type:
- AzureDevOps/ServiceNow: 180s
- GitHub/Jira: 120s
- Datadog: 90s
- PagerDuty/Prometheus: 60s

## Execution Limits

### Max Concurrent Executions (`integrations.execution.max_concurrent`)
- Default: 5 (range 1-20)

## Safeguards

- Retry max attempts capped at 10
- Backoff minimum 5 seconds
- Timeout minimum 10 seconds, maximum 1 hour
- Concurrent executions capped at 20
- All changes auditable through configuration audit trail
