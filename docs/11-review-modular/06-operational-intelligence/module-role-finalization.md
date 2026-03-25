# Module Role Finalization — Operational Intelligence

> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Consolidation  
> **Date:** 2026-03-24  
> **Status:** DRAFT

---

## 1. Official Role Definition

The **Operational Intelligence** module is the **unified operational nervous system** of NexTraceOne. It owns the aggregated operational state of all services, computes reliability and health scores, manages operational incidents with guided mitigation, orchestrates controlled automations, and provides contextualised operational recommendations — always linked to services, teams, environments, and changes.

### What Operational Intelligence IS

| Responsibility | Description |
|---|---|
| **Operational Signal Aggregation** | Receives and normalises runtime metrics, cost data, and health signals from monitored services (`IRuntimeSignalIngestionPort`, `IngestRuntimeSnapshot`, `IngestCostSnapshot`) |
| **Aggregated Operational State** | Maintains the latest runtime health, reliability score, and cost posture per service/environment (`RuntimeSnapshot`, `ReliabilitySnapshot`, `CostSnapshot`) |
| **Operational Scoring** | Computes composite reliability scores: `OverallScore = RuntimeHealthScore×0.50 + IncidentImpactScore×0.30 + ObservabilityScore×0.20` (see `ReliabilitySnapshot.cs`) |
| **Operational Incidents** | Full lifecycle of incidents — detection, investigation, correlation with changes, evidence gathering, guided mitigation, and closure (`IncidentRecord` aggregate root) |
| **Controlled Automations** | Approval-gated automation workflows with preconditions, execution steps, validation, and audit trail (`AutomationWorkflowRecord` aggregate root) |
| **Operational Recommendations** | AI-assisted and rule-based mitigation recommendations linked to runbooks (`MitigationRecommendations`, `RunbookRecord`) |
| **Readiness & Health Assessment** | Runtime health classification (Healthy/Degraded/Unhealthy), drift detection against baselines, and observability maturity scoring (`RuntimeSnapshot.ClassifyHealth()`, `DriftFinding.Detect()`, `ObservabilityProfile.Assess()`) |
| **Guided Operational Response** | Multi-step mitigation workflows with approval gates, action logging, and post-mitigation validation (`MitigationWorkflowRecord`, `MitigationValidationLog`) |
| **FinOps Operational** | Service-level cost tracking, attribution per API route, trend analysis, budget alerts, anomaly detection, and batch import (`CostSnapshot`, `CostAttribution`, `ServiceCostProfile`, `CostImportBatch`) |

### What Operational Intelligence IS NOT

| Anti-pattern | Correct Owner |
|---|---|
| Authentication, authorisation, or user management | **Identity & Access** (01) |
| Service or API registration, ownership, topology | **Service Catalog** (03) |
| Contract lifecycle — drafts, reviews, diffs, approval | **Contracts** (04) |
| Change tracking, blast radius computation, deployment validation | **Change Governance** (05) |
| Notification routing, channel configuration, delivery tracking | **Notifications** (11) |
| Audit trail storage, compliance evidence, regulatory reporting | **Audit & Compliance** (10) |
| Environment definitions, promotion rules | **Environment Management** (02) |
| AI model registry, AI access policies, token governance | **AI & Knowledge** (07) |
| Compliance reports, executive risk dashboards | **Governance** (08) |
| Generic observability dashboards without service/change context | **Not in NexTraceOne scope** |

---

## 2. Why Operational Intelligence Is Central

Operational Intelligence sits at the intersection of all operational modules. It **consumes** foundational data (services, environments, changes) and **produces** operational state that other modules depend on.

| Consumer Module | What It Gets from Operational Intelligence |
|---|---|
| **Governance** (08) | Reliability scores, incident trends, SLA compliance data for executive reports |
| **Change Governance** (05) | Post-deployment health signals, runtime drift correlated with releases |
| **AI & Knowledge** (07) | Operational context for AI-assisted analysis (incidents, health, runbooks) |
| **Notifications** (11) | Events requiring notification: anomalies, budget alerts, critical incidents (via domain events) |
| **Audit & Compliance** (10) | Automation audit trail, incident timeline, mitigation decisions (via domain events) |

### Dependency Direction

```
Identity & Access (01) ──JWT/RLS/Permissions──► Operational Intelligence (06)
Environment Management (02) ──EnvironmentId──► Operational Intelligence (06)
Service Catalog (03) ──ServiceId/ServiceName──► Operational Intelligence (06)
Change Governance (05) ──ChangeId/Correlation──► Operational Intelligence (06)

Operational Intelligence (06) ──Domain Events──► Notifications (11) [NOT YET INTEGRATED]
Operational Intelligence (06) ──Domain Events──► Audit & Compliance (10) [NOT YET INTEGRATED]
Operational Intelligence (06) ──Scores/State──► Governance (08) [VIA QUERY]
```

---

## 3. Protecting Operational Intelligence from Scope Expansion

### Scope Protection Rules

1. **No asset registry logic inside Operational Intelligence.** Services are referenced by `ServiceId`/`ServiceName` from the Catalog. This module never creates or modifies service definitions.

2. **No change lifecycle logic.** `CorrelatedChangesJson` in `IncidentRecord` references change identifiers but does NOT own the change tracking lifecycle.

3. **No notification delivery logic.** Domain events (`RuntimeAnomalyDetectedEvent`, `CostAnomalyDetectedEvent`) signal the need for notification, but routing and delivery belong to Notifications.

4. **No audit trail persistence beyond local scope.** `AutomationAuditRecord` is a local audit trail for automation workflows. Cross-module audit persistence belongs to Audit & Compliance.

5. **No generic observability.** Every metric, dashboard, and alert must be contextualised by service, team, environment, or change. Generic metric viewers are out of scope.

6. **No cost data sourcing.** `CostImportBatch` imports pre-aggregated cost data from external sources (AWS CUR, Azure Cost Management). Raw billing integration belongs to Integrations.

### Gatekeeping Checklist

Before adding any feature to Operational Intelligence, answer:

| Question | Expected Answer |
|---|---|
| Does this feature aggregate or assess operational state of a service? | YES → Operational Intelligence |
| Does this feature manage an operational incident lifecycle? | YES → Operational Intelligence |
| Does this feature control an automation workflow with approval? | YES → Operational Intelligence |
| Does this feature register or modify a service definition? | NO → Service Catalog (03) |
| Does this feature manage a change or deployment lifecycle? | NO → Change Governance (05) |
| Does this feature route or deliver notifications? | NO → Notifications (11) |
| Does this feature store compliance evidence? | NO → Audit & Compliance (10) |
| Does this feature define AI models or govern AI access? | NO → AI & Knowledge (07) |
| Does this feature define environments or promotion rules? | NO → Environment Management (02) |

---

## 4. Operational Intelligence Subdomains

The module is internally organised into 5 bounded subdomains:

| Subdomain | Responsibility | Aggregate Roots | DbContext |
|---|---|---|---|
| **Incidents** | Incident lifecycle, mitigation workflows, action logging, validation, runbooks | `IncidentRecord` | `IncidentDbContext` (5 DbSets) |
| **Automation** | Controlled automation workflows, preconditions, approval, execution, validation, audit | `AutomationWorkflowRecord` | `AutomationDbContext` (3 DbSets) |
| **Reliability** | Composite reliability scoring per service/environment with trending | `ReliabilitySnapshot` | `ReliabilityDbContext` (1 DbSet) |
| **Runtime** | Runtime health metrics, baseline management, drift detection, observability profiling | `RuntimeSnapshot` | `RuntimeIntelligenceDbContext` (4 DbSets) |
| **Cost** | Cost tracking, attribution, trends, budgets, anomaly detection, batch import | `CostSnapshot` | `CostIntelligenceDbContext` (6 DbSets) |

### Structural Note

The 5 DbContexts share the `ops_` table prefix and target the same PostgreSQL database. A consolidation into a single `OperationalIntelligenceDbContext` is under evaluation (see `module-boundary-matrix.md`). ClickHouse is **recommended** for runtime metrics time-series and cost analytics aggregations (see `module-data-placement-matrix.md`).

---

## 5. Support for Other Modules

### 5.1 Support for Change Governance

| Mechanism | Detail |
|---|---|
| **Correlation** | `IncidentRecord.CorrelatedChangesJson` links incidents to changes from Change Governance |
| **Post-deployment health** | `RuntimeSnapshot` health after deployment, `DriftFinding` for regression detection |
| **Release-health timeline** | `GetReleaseHealthTimeline` and `CompareReleaseRuntime` endpoints serve Change Governance dashboards |
| **Cost-by-release** | `GetCostByRelease` links cost impact to specific releases |
| **Port** | `IRuntimeCorrelationPort.CorrelateWithReleaseAsync()` prepared for cross-module correlation |

### 5.2 Support for Notifications (NOT YET INTEGRATED)

| Mechanism | Detail |
|---|---|
| **Domain events** | `RuntimeAnomalyDetectedEvent` and `CostAnomalyDetectedEvent` are published via outbox pattern |
| **Expected consumers** | Notifications module should subscribe to these events for alerting |
| **Gap** | No `INotificationService` interface imported; no direct Notifications dependency exists in code |
| **Action needed** | Define integration contract; Notifications subscribes to `OperationalIntelligence` integration events |

### 5.3 Support for Audit & Compliance (NOT YET INTEGRATED)

| Mechanism | Detail |
|---|---|
| **Local audit** | `AutomationAuditRecord` tracks workflow actions locally with `AutomationAuditAction` enum |
| **Domain events** | `RuntimeSignalReceivedEvent` and anomaly events declare Audit as a typical consumer |
| **Gap** | No cross-module audit integration; local audit records are not forwarded to Audit & Compliance |
| **Action needed** | Publish audit-relevant events via outbox; Audit & Compliance subscribes to `OperationalIntelligence` events |

---

## 6. Key Files

| Area | Path |
|---|---|
| Backend project | `src/modules/operationalintelligence/` |
| Frontend (main) | `src/frontend/src/features/operations/` (10 pages) |
| Frontend (embryonic) | `src/frontend/src/features/operational-intelligence/` (1 file) |
| IncidentDbContext | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Incidents/Persistence/IncidentDbContext.cs` |
| AutomationDbContext | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Automation/Persistence/AutomationDbContext.cs` |
| ReliabilityDbContext | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Reliability/Persistence/ReliabilityDbContext.cs` |
| RuntimeIntelligenceDbContext | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Runtime/Persistence/RuntimeIntelligenceDbContext.cs` |
| CostIntelligenceDbContext | `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Cost/Persistence/CostIntelligenceDbContext.cs` |
| Domain events | `Domain/Runtime/Events/RuntimeAnomalyDetectedEvent.cs`, `RuntimeSignalReceivedEvent.cs`, `Domain/Cost/Events/CostAnomalyDetectedEvent.cs` |
| Ports | `Domain/Runtime/Ports/IRuntimeSignalIngestionPort.cs`, `IRuntimeCorrelationPort.cs` |

---

## 7. Open Questions

| # | Question | Impact |
|---|---|---|
| 1 | Should 5 DbContexts be consolidated into a single `OperationalIntelligenceDbContext`? | HIGH — affects migrations, query complexity, transaction boundaries |
| 2 | When should ClickHouse integration be implemented for runtime metrics and cost analytics? | HIGH — affects performance at scale |
| 3 | How should Notifications integration be formalised — direct event subscription or dedicated integration adapter? | MEDIUM — defines integration pattern |
| 4 | Should `AutomationAuditRecord` be migrated to Audit & Compliance or remain local with event forwarding? | MEDIUM — affects audit completeness |
| 5 | Should the embryonic `operational-intelligence/` frontend folder be merged into `operations/`? | LOW — naming consistency |
| 6 | Should `DriftFinding` and `ObservabilityProfile` remain as aggregate roots or become entities under `RuntimeSnapshot`? | MEDIUM — DDD correctness |
