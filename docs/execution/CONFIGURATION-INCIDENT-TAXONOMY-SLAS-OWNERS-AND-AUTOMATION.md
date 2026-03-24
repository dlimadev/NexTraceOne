# Configuration — Incident Taxonomy, SLAs, Owners & Automation

## Incident Taxonomy

### Categories (`incidents.taxonomy.categories`)
- Default: Infrastructure, Application, Security, Data, Network, ThirdParty
- Scope: System, Tenant
- Editor: JSON

### Types (`incidents.taxonomy.types`)
- Default: Outage, Degradation, Latency, ErrorSpike, SecurityBreach, DataLoss, ConfigDrift
- Scope: System, Tenant
- Editor: JSON

## Severity & Criticality

### Severity by Type (`incidents.severity.defaults_by_type`)
Maps incident types to default severities (Critical, High, Medium, Low).

### Severity by Category (`incidents.severity.defaults_by_category`)
Maps incident categories to default severities.

### Criticality Defaults (`incidents.criticality.defaults`)
Combined type+category → criticality mapping for priority determination.

### Severity Mapping (`incidents.severity.mapping`)
Labels, colors, and numeric weights for each severity level.

## SLA Configuration

### SLA by Severity (`incidents.sla.by_severity`)
- Per-severity targets: acknowledgement, first response, resolution (in minutes)
- Scope: System, Tenant, **Environment** — Production stricter than Dev
- Default: Critical = 5 min ack / 15 min first response / 240 min resolution

### SLA by Environment (`incidents.sla.by_environment`)
- Multiplier per environment (Production = 1.0x, Development = 5.0x)
- Allows relaxed SLAs in non-production environments

### Production Behavior (`incidents.sla.production_behavior`)
- Auto-escalation, on-call paging, post-mortem requirements by severity

## Owners & Responsibility

### Default Owner by Category (`incidents.owner.default_by_category`)
Team/role assignment per category (e.g., Security → security-team).

### Fallback Owner (`incidents.owner.fallback`)
Always `platform-admin` unless overridden.

## Classification & Correlation

### Auto-Classification (`incidents.classification.auto_enabled`)
Enables automatic categorization from alert context.

### Correlation Policy (`incidents.correlation.policy`)
Rules for grouping alerts into incidents: by service, environment, severity, correlation key fields.

### Auto-Incident Creation (`incidents.auto_creation.enabled` / `.policy`)
- Min severity for auto-create (default: High)
- Max auto-incidents per hour (default: 10)
- Blocked environments (System-only, non-inheritable)

### Enrichment (`incidents.enrichment.enabled`)
Adds new correlated alerts to existing open incidents.

## Playbooks & Runbooks

### Playbook Defaults (`operations.playbook.defaults_by_type`)
Default playbook per incident type (e.g., Outage → playbook-outage-standard).

### Runbook Defaults (`operations.runbook.defaults_by_category`)
Default runbook per incident category.

### Requirements
- By environment: Production requires playbook
- By criticality: Critical incidents require playbook

## Operational Automation

### By Environment (`operations.automation.enabled_by_environment`)
Granular control: auto-restart, auto-scale, auto-remediate per environment.

### Blocked in Production (`operations.automation.blocked_in_production`)
System-only, non-inheritable list of permanently blocked automations.

### By Severity (`operations.automation.by_severity`)
Allowed automation actions per severity level.

### Post-Incident Template (`operations.postincident.template_enabled`)
Whether post-incident review templates are automatically applied.

## Effective Settings

All definitions support effective settings explorer with:
- System → Tenant → Environment inheritance chain
- Override indicators
- Resolved scope display
- Mandatory rule indicators for non-inheritable settings
