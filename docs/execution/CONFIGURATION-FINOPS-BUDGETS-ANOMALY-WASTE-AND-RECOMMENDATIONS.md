# Configuration — FinOps Budgets, Anomaly, Waste & Recommendations

## Budget Configuration

### Default Currency (`finops.budget.default_currency`)
- Default: USD (ISO 4217, validated 3-char)
- Scope: System, Tenant

### Budget by Tenant (`finops.budget.by_tenant`)
Monthly budget allocation per tenant with alert-on-exceed flag.

### Budget by Team (`finops.budget.by_team`)
Monthly budget allocation per team.

### Budget by Service (`finops.budget.by_service`)
Monthly budget allocation per service.

### Budget by Environment (`finops.budget.by_environment`)
- Per-environment budgets with hard limit flag
- Production = $8,000 (hard limit), Development = $1,000 (soft)

### Budget Alert Thresholds (`finops.budget.alert_thresholds`)
- Ordered threshold array: 80% → Low/Notify, 90% → Medium/Notify, 100% → High/NotifyAndBlock, 120% → Critical/Escalate
- Scope: System, Tenant, **Environment** — allows tighter thresholds in production

### Periodicity (`finops.budget.periodicity`)
- Options: Monthly, Quarterly, Yearly
- Validated enum selection

### Rollover (`finops.budget.rollover_enabled`)
- Default: disabled
- Whether unused budget rolls over to next period

## Anomaly Detection

### Detection Enabled (`finops.anomaly.detection_enabled`)
Toggle for cost anomaly detection.

### Thresholds (`finops.anomaly.thresholds`)
- Warning: 20% deviation from baseline
- High: 50% deviation
- Critical: 100% deviation

### Comparison Window (`finops.anomaly.comparison_window_days`)
- Default: 30 days
- Range: 7–90 days (validated)

### By Service Criticality (`finops.anomaly.by_criticality`)
- Critical services: 10% warning deviation, auto-escalate
- Standard services: 20% warning deviation, no auto-escalate

## Waste Detection

### Detection Enabled (`finops.waste.detection_enabled`)
Toggle for waste detection analysis.

### Thresholds (`finops.waste.thresholds`)
- Idle resource threshold: 90%
- Underutilization threshold: 20%
- Unused days threshold: 14

### Categories (`finops.waste.categories`)
Classification: IdleResources, Overprovisioned, UnattachedStorage, UnusedLicenses, OrphanedResources, OverlappingServices.

## Recommendation & Notification

### Financial Recommendation Policy (`finops.recommendation.policy`)
- Auto-recommend enabled
- Min savings threshold: $50
- Show in dashboard
- High savings notification threshold: $500

### Financial Notification Policy (`finops.notification.policy`)
- Notify on anomaly, budget breach, waste detection
- Digest frequency: Weekly
- Configurable per event type

## Multi-Tenant & Environment Behavior

All FinOps definitions support:
- System-level defaults
- Tenant-level overrides (different budgets, thresholds, policies per tenant)
- Environment-level overrides for budget alert thresholds
- Effective settings explorer shows resolved values with inheritance chain
