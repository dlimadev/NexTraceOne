# Change Governance & Releases

## Understanding Change Intelligence

NexTraceOne tracks every production change and provides intelligence about its impact:

- **Change Catalog** — View all changes across services and environments
- **Change Detail** — Inspect individual changes with blast radius, validation status, and risk assessment
- **Change-to-Incident Correlation** — Automatically links changes to incidents that occur in related timeframes

## Reviewing Changes

Navigate to **Changes** in the sidebar to access the change catalog.

Each change entry shows:
- Service affected
- Change type and description
- Validation status (Passed, Failed, Pending)
- Blast radius assessment
- Related incidents (if any)

## Releases & Promotion

### Release Management

The **Releases** page shows release bundles across your services:

- Track which changes are included in each release
- Monitor release validation status
- View promotion history across environments

### Promotion Workflows

Use the **Promotion** page to move changes through environments:

1. Select the change or release bundle to promote.
2. Review the blast radius and validation results.
3. Approve or request additional validation.
4. Track the promotion through Development → Staging → Production.

### Workflow Configuration

The **Workflow** page allows Tech Leads and Architects to define approval gates and validation requirements for different types of changes.

## Best Practices

- Always review the **blast radius** before approving a production change.
- Use **change-to-incident correlation** to identify patterns in deployment issues.
- Set up **approval gates** appropriate to the criticality of each service.
