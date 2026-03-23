# Operations & Incidents

## Incident Management

Navigate to **Operations** → **Incidents** to view and manage incidents.

### Viewing Incidents

- **Incident List** — All incidents with severity, status, affected services, and timeline
- **Incident Detail** — Full incident view with timeline, affected services, related changes, and mitigation steps

### Incident Workflow

1. An incident is detected (manually created or auto-correlated from monitoring).
2. Triage the incident — assign severity, affected services, and ownership.
3. Track mitigation progress through the incident timeline.
4. Resolve and document root cause and corrective actions.

## Runbooks

Navigate to **Operations** → **Runbooks** to access operational runbooks.

Runbooks provide step-by-step procedures for common operational tasks:
- Incident response procedures
- Deployment rollback steps
- Health check procedures
- Escalation paths

## Service Reliability

### Team Reliability Dashboard

The **Team Reliability** page shows reliability metrics aggregated by team:
- SLO compliance across team services
- Incident frequency and MTTR (Mean Time to Resolution)
- Reliability trends over time

### Service Reliability Detail

Each service has a dedicated reliability view showing:
- Current SLO status
- Error rates and latency percentiles
- Incident history specific to the service

## Environment Comparison

The **Environment Comparison** tool helps identify configuration drift between environments (Development, Staging, Production), ensuring consistency across deployment stages.

## Automation Workflows

Navigate to **Operations** → **Automation** to view and manage operational automation:
- View available automation workflows
- Inspect workflow execution history
- Review approval requirements and preconditions
- Track audit trails for automated operations
