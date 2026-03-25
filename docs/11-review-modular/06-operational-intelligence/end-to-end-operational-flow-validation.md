# Operational Intelligence — End-to-End Operational Flow Validation

> **Status:** DRAFT  
> **Date:** 2026-03-24  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation

---

## 1. Purpose

This document maps the **real operational flows** through the Operational Intelligence module code, tracing each path from signal entry to final outcome. For each flow, it identifies steps with real backend implementation, steps without implementation, cosmetic-only steps, missing state persistence, and missing cross-module integrations.

---

## 2. Master Flow Overview

```
Signal Entry ──► Evaluation / Rules ──► Scoring ──► Decision ──► Automation / Recommendation ──► Notification ──► Audit
     │                  │                  │            │                    │                        │              │
     ▼                  ▼                  ▼            ▼                    ▼                        ▼              ▼
IngestRuntime     ClassifyHealth     ReliabilityScore  DriftDetect     MitigationWorkflow     [NOT INTEGRATED]  [LOCAL ONLY]
IngestCost        IsAnomaly          ObservScore       CostAnomaly     AutomationWorkflow
CreateIncident    ThresholdCheck     TrendDirection    HealthStatus    RunbookLink
```

---

## 3. Flow 1 — Runtime Health Monitoring

### Signal → Health → Baseline → Drift → Recommendation

| Step | Operation | Code Path | State Persisted | Status |
|---|---|---|---|---|
| 1. Signal ingestion | External system sends runtime metrics | `POST /api/v1/runtime/snapshots` → `IngestRuntimeSnapshot.Command` | `ops_runtime_snapshots` | ✅ Real |
| 2. Health classification | Auto-classify from ErrorRate and P99Latency | `RuntimeSnapshot.ClassifyHealth()` — `ErrorRate≥10%` or `P99≥3000ms` → Unhealthy | `HealthStatus` field in snapshot | ✅ Real |
| 3. Baseline comparison | Check deviation from established baseline | `RuntimeBaseline.IsWithinTolerance(snapshot, tolerance)` — `IsMetricWithinTolerance()` helper | Baseline in `ops_runtime_baselines` | ✅ Real |
| 4. Drift detection | Detect metric deviations exceeding thresholds | `POST /api/v1/runtime/drift/detect` → `DetectRuntimeDrift.Command` → `DriftFinding.Detect()` | `ops_drift_findings` with auto-severity | ✅ Real |
| 5. Anomaly event | Publish domain event on anomaly | `RuntimeAnomalyDetectedEvent(AnomalyId, ServiceName, AnomalyType, Severity, DetectedAt)` | Outbox (IntegrationEventBase) | ✅ Real |
| 6. Observability assessment | Evaluate service observability maturity | `POST /api/v1/runtime/observability/assess` → `ComputeObservabilityDebt.Command` → `ObservabilityProfile.Assess()` | `ops_observability_profiles` | ✅ Real |
| 7. Health timeline | View health evolution over time window | `GET /api/v1/runtime/timeline` → `GetReleaseHealthTimeline.Query` | Read from `ops_runtime_snapshots` | ✅ Real |
| 8. Release comparison | Compare before/after metrics around a release | `GET /api/v1/runtime/compare` → `CompareReleaseRuntime.Query` | Read from `ops_runtime_snapshots` | ✅ Real |
| 9. **Notification** | Alert team about unhealthy service | — | — | ❌ **NOT INTEGRATED** |
| 10. **Audit** | Record anomaly detection in audit trail | — | — | ❌ **NOT INTEGRATED** |

### Gaps Identified

| Gap | Severity | Detail |
|---|---|---|
| **No Notifications subscriber** | HIGH | `RuntimeAnomalyDetectedEvent` is published but Notifications module does not subscribe |
| **No Audit forwarding** | MEDIUM | Anomaly events mention Audit as consumer but no cross-module integration exists |
| **No auto-incident creation** | LOW | Runtime anomaly does not auto-create an `IncidentRecord`; requires manual incident creation |
| **No real telemetry adapter** | MEDIUM | `IRuntimeSignalIngestionPort` is defined but no Prometheus/Datadog/OpenTelemetry adapter implements it |

---

## 4. Flow 2 — Incident Lifecycle

### Detection → Investigation → Correlation → Evidence → Mitigation → Validation → Closure

| Step | Operation | Code Path | State Persisted | Status |
|---|---|---|---|---|
| 1. Incident creation | Manual or system-created incident | `POST /api/v1/incidents` → `CreateIncident.Command` → `IncidentRecord.Create()` | `ops_incident_records` | ✅ Real |
| 2. Correlation analysis | Link incident to changes, services, dependencies | `GET /api/v1/incidents/{id}/correlation` → `GetIncidentCorrelation.Query` | `CorrelatedChangesJson`, `CorrelatedServicesJson`, `CorrelatedDependenciesJson` on `IncidentRecord` | ⚠️ **Partial** |
| 3. Correlation refresh | Re-compute correlations | `POST /api/v1/incidents/{id}/correlation/refresh` → `RefreshIncidentCorrelation.Command` → `IncidentRecord.UpdateCorrelationAssessment()` | Updated JSON fields on `IncidentRecord` | ⚠️ **Partial** |
| 4. Evidence gathering | Collect telemetry, business impact, analysis | `GET /api/v1/incidents/{id}/evidence` → `GetIncidentEvidence.Query` | `EvidenceTelemetrySummary`, `EvidenceBusinessImpact`, `EvidenceAnalysis`, `EvidenceTemporalContext` on `IncidentRecord` | ✅ Real |
| 5. Mitigation recommendations | AI/rule-based suggestions with runbook links | `GET /api/v1/incidents/{id}/mitigation` → `GetIncidentMitigation.Query` | `MitigationRecommendationsJson`, `MitigationRecommendedRunbooksJson` on `IncidentRecord` | ✅ Real |
| 6. Mitigation workflow creation | Create multi-step mitigation plan | `POST /api/v1/mitigation/workflows` → `CreateMitigationWorkflow.Command` → `MitigationWorkflowRecord.Create()` | `ops_mitigation_workflows` | ✅ Real |
| 7. Workflow action execution | Execute mitigation steps with status transitions | `POST /api/v1/mitigation/workflows/{id}/action` → `UpdateMitigationWorkflowAction.Command` → `MitigationWorkflowRecord.UpdateStatus()` | `ops_mitigation_workflow_actions` (action log) | ✅ Real |
| 8. Post-mitigation validation | Verify mitigation effectiveness | `POST /api/v1/mitigation/workflows/{id}/validation` → `RecordMitigationValidation.Command` → `MitigationValidationLog` | `ops_mitigation_validations` | ✅ Real |
| 9. Incident status transition | Move through Open → Investigating → Mitigating → Monitoring → Resolved → Closed | `IncidentRecord.SetDetail()` / status update | `Status` field on `IncidentRecord` | ✅ Real |
| 10. **Notification** | Alert stakeholders on severity escalation | — | — | ❌ **NOT INTEGRATED** |
| 11. **Audit** | Record incident timeline as audit evidence | — | — | ❌ **NOT INTEGRATED** |

### Gaps Identified

| Gap | Severity | Detail |
|---|---|---|
| **Correlation not validated end-to-end** | HIGH | `CorrelatedChangesJson` stores change IDs but Change Governance (05) does not publish events consumed by Operational Intelligence; correlation may be seed/mock data |
| **No Notifications on severity escalation** | HIGH | Critical/Major incidents do not trigger team notifications |
| **No Audit trail forwarding** | MEDIUM | Incident lifecycle transitions are not exported to Audit & Compliance (10) |
| **No auto-detection from runtime anomalies** | LOW | `RuntimeAnomalyDetectedEvent` does not auto-create incidents; incidents are manually created |

### Correlation Detail — What Exists vs What Is Missing

| Correlation Type | JSON Field | Data Source | Validated? |
|---|---|---|---|
| Change correlation | `CorrelatedChangesJson` | Expected from Change Governance | ⚠️ **Structure exists, source unvalidated** |
| Service correlation | `CorrelatedServicesJson` | Service Catalog via `ServiceId` | ✅ `ServiceId` is set on creation |
| Dependency correlation | `CorrelatedDependenciesJson` | Topology from Catalog | ⚠️ **Structure exists, auto-population unclear** |
| Contract impact | `ImpactedContractsJson` | Contracts module | ⚠️ **Structure exists, source unvalidated** |

---

## 5. Flow 3 — Controlled Automation

### Request → Preconditions → Approval → Execution → Validation → Audit

| Step | Operation | Code Path | State Persisted | Status |
|---|---|---|---|---|
| 1. Workflow creation | Request automation execution | `POST /api/v1/automation/workflows` → `CreateAutomationWorkflow.Command` → `AutomationWorkflowRecord.Create()` | `ops_automation_workflow_records` (status: Draft) | ✅ Real |
| 2. Precondition evaluation | Check readiness conditions | `POST /api/v1/automation/workflows/{id}/evaluate-preconditions` → `EvaluatePreconditions.Command` | Status → PendingPreconditions / ReadyToExecute | ✅ Real |
| 3. Approval request | Request human approval for risky actions | `POST /api/v1/automation/workflows/{id}/request-approval` | Status → AwaitingApproval | ✅ Real |
| 4. Approval decision | Approve or reject | `POST /api/v1/automation/workflows/{id}/approve` or `/reject` → `AutomationWorkflowRecord.Approve()` / `Reject()` | `ApprovalStatus`, `ApprovedBy`, `ApprovedAt` | ✅ Real |
| 5. Execution | Execute automation steps | `POST /api/v1/automation/workflows/{id}/execute` | Status → Executing | ✅ Real |
| 6. Step completion | Complete individual execution steps | `POST /api/v1/automation/workflows/{id}/complete-step` | Status updates per step | ✅ Real |
| 7. Post-execution validation | Verify execution outcome | `POST /api/v1/automation/workflows/{id}/validation` → `RecordAutomationValidation.Command` → `AutomationValidationRecord` | `ops_automation_validation_records` with 6 outcomes (Successful, PartiallySuccessful, Failed, Inconclusive, Cancelled, RolledBack) | ✅ Real |
| 8. Local audit recording | Log each workflow action | `AutomationAuditRecord` created per state transition | `ops_automation_audit_records` with `AutomationAuditAction` enum | ✅ Real |
| 9. Cancellation | Cancel workflow at any point | `POST /api/v1/automation/workflows/{id}/cancel` | Status → Cancelled | ✅ Real |
| 10. **Notification on approval request** | Notify approvers | — | — | ❌ **NOT INTEGRATED** |
| 11. **Cross-module audit forwarding** | Forward audit records to Audit & Compliance | — | — | ❌ **NOT INTEGRATED** |

### Gaps Identified

| Gap | Severity | Detail |
|---|---|---|
| **No approval notifications** | HIGH | When `AwaitingApproval`, no notification is sent to approvers; they must poll the UI |
| **Local audit only** | MEDIUM | `AutomationAuditRecord` is stored in `ops_automation_audit_records` but not forwarded to Audit & Compliance (10) |
| **No execution integration** | LOW | Automation steps track status but do not invoke real infrastructure actions (expected — requires integration adapters) |

### Automation State Machine

```
Draft ──► PendingPreconditions ──► AwaitingApproval ──► Approved ──► ReadyToExecute ──► Executing
  │                                       │                                                    │
  │                                       ▼                                                    ▼
  │                                   Rejected                                         AwaitingValidation
  │                                                                                           │
  ▼                                                                                           ▼
Cancelled                                                                                 Completed
                                                                                              │
                                                                                              ▼
                                                                                           Failed
```

---

## 6. Flow 4 — Reliability Scoring

### Ingestion → Computation → Aggregation → Trending

| Step | Operation | Code Path | State Persisted | Status |
|---|---|---|---|---|
| 1. Runtime health input | Latest `RuntimeSnapshot` provides health score | `RuntimeSnapshot.HealthStatus` + computed health score | `ops_runtime_snapshots` | ✅ Real |
| 2. Incident impact input | Count of open incidents affects score | `IncidentRecord` count where `Status ∈ {Open, Investigating, Mitigating}` | `ops_incident_records` | ✅ Real |
| 3. Observability input | `ObservabilityProfile.ObservabilityScore` (0–1 × 100) | `ObservabilityProfile.Assess()` | `ops_observability_profiles` | ✅ Real |
| 4. Score computation | `OverallScore = RuntimeHealthScore×0.50 + IncidentImpactScore×0.30 + ObservabilityScore×0.20` | `ReliabilitySnapshot.Create()` | `ops_reliability_snapshots` | ✅ Real |
| 5. Service-level view | Reliability per service with status classification | `GET /api/v1/reliability/services` → `ListServiceReliability.Query` | Read-only | ✅ Real |
| 6. Trend analysis | Reliability trend over time | `GET /api/v1/reliability/services/{id}/trend` → `GetServiceReliabilityTrend.Query` | Read from snapshots | ✅ Real |
| 7. Team aggregation | Reliability summary per team | `GET /api/v1/reliability/teams/{id}/summary` → `GetTeamReliabilitySummary.Query` | Read-only aggregation | ✅ Real |
| 8. Domain aggregation | Reliability summary per domain | `GET /api/v1/reliability/domains/{id}/summary` → `GetDomainReliabilitySummary.Query` | Read-only aggregation | ✅ Real |
| 9. Coverage assessment | Which services have adequate observability | `GET /api/v1/reliability/services/{id}/coverage` → `GetServiceReliabilityCoverage.Query` | Read-only | ✅ Real |
| 10. **Alert on degraded reliability** | Notify when score drops below threshold | — | — | ❌ **NOT INTEGRATED** |

### Gaps Identified

| Gap | Severity | Detail |
|---|---|---|
| **No automated score computation trigger** | MEDIUM | `ReliabilitySnapshot.Create()` must be called explicitly; no background worker auto-computes on new data |
| **No alert on reliability degradation** | MEDIUM | No threshold-based alerting when `OverallScore` drops below a configurable minimum |
| **Scoring weights are hardcoded** | LOW | Weights (0.50, 0.30, 0.20) are constants in `ReliabilitySnapshot.cs` — not configurable per tenant |

---

## 7. Flow 5 — Cost Intelligence

### Ingestion → Attribution → Trending → Budgeting → Anomaly Detection

| Step | Operation | Code Path | State Persisted | Status |
|---|---|---|---|---|
| 1. Cost ingestion | Receive pre-aggregated cost data | `POST /api/v1/cost/snapshots` → `IngestCostSnapshot.Command` → `CostSnapshot.Create()` | `ops_cost_snapshots` | ✅ Real |
| 2. Batch import | Import bulk cost data from cloud providers | `POST /api/v1/cost/import` → `ImportCostBatch.Command` → `CostImportBatch.Create()` | `ops_cost_import_batches` + `ops_cost_records` | ✅ Real |
| 3. Cost attribution | Attribute cost to specific API routes | `POST /api/v1/cost/attributions` → `AttributeCostToService.Command` → `CostAttribution.Attribute()` | `ops_cost_attributions` with auto cost-per-request | ✅ Real |
| 4. Trend computation | Analyse cost direction over time | `POST /api/v1/cost/trends` → `ComputeCostTrend.Command` → `CostTrend.Create()` | `ops_cost_trends` with auto Rising/Stable/Declining | ✅ Real |
| 5. Budget management | Set and monitor service budgets | `ServiceCostProfile.SetBudget()`, `UpdateCurrentCost()`, `CheckBudgetAlert()` | `ops_service_cost_profiles` | ✅ Real |
| 6. Anomaly detection | Compare actual vs expected cost | `POST /api/v1/cost/anomaly-check` → `AlertCostAnomaly.Command` → `CostSnapshot.IsAnomaly()` | Domain event: `CostAnomalyDetectedEvent` | ✅ Real |
| 7. Cost report | View cost breakdown per service/environment | `GET /api/v1/cost/report` → `GetCostReport.Query` | Read-only | ✅ Real |
| 8. Cost by release | View cost impact of a specific release | `GET /api/v1/cost/by-release/{releaseId}` → `GetCostByRelease.Query` | Read-only | ✅ Real |
| 9. Cost delta | Compare costs between periods | `GET /api/v1/cost/delta` → `GetCostDelta.Query` | Read-only | ✅ Real |
| 10. **Notification on anomaly** | Alert team on cost anomaly | — | — | ❌ **NOT INTEGRATED** |
| 11. **Notification on budget threshold** | Alert when budget usage exceeds threshold | — | — | ❌ **NOT INTEGRATED** |
| 12. **Cost dashboard page** | Dedicated UI for cost data | — | — | ❌ **MISSING FRONTEND** |

### Gaps Identified

| Gap | Severity | Detail |
|---|---|---|
| **No cost frontend page** | HIGH | All 9 cost endpoints are implemented but no dedicated UI page exists for cost data |
| **No Notifications subscriber for cost anomalies** | HIGH | `CostAnomalyDetectedEvent` is published but Notifications module does not subscribe |
| **No budget alert notifications** | HIGH | `ServiceCostProfile.CheckBudgetAlert()` returns an error result but does not trigger notifications |
| **`UnattributedCost` not surfaced** | LOW | `CostSnapshot.UnattributedCost` computed property highlights missing cost shares but no alert or view uses it |

---

## 8. Cross-Cutting Gaps Summary

### 8.1 Missing Notifications Integration

| Event | Published By | Expected Consumer | Status |
|---|---|---|---|
| `RuntimeAnomalyDetectedEvent` | Runtime subdomain (outbox) | Notifications (11) | ❌ No subscriber |
| `CostAnomalyDetectedEvent` | Cost subdomain (outbox) | Notifications (11) | ❌ No subscriber |
| Critical incident creation | Incidents subdomain | Notifications (11) | ❌ No event published |
| Budget threshold breach | Cost subdomain (`CheckBudgetAlert`) | Notifications (11) | ❌ No event published |
| Automation approval request | Automation subdomain | Notifications (11) | ❌ No event published |

**Correction needed:** Define integration contract between Operational Intelligence and Notifications. At minimum, 5 notification triggers should be implemented.

### 8.2 Missing Audit & Compliance Integration

| Auditable Action | Current State | Expected State |
|---|---|---|
| Automation workflow state transitions | `AutomationAuditRecord` in `ops_automation_audit_records` (local) | Forward via domain event to Audit & Compliance (10) |
| Incident lifecycle transitions | `TimelineJson` on `IncidentRecord` (embedded JSON) | Publish transition events for audit trail |
| Mitigation approval decisions | `ApprovedBy`/`ApprovedAt` on `MitigationWorkflowRecord` | Publish approval events for compliance evidence |
| Cost anomaly detections | `CostAnomalyDetectedEvent` published (outbox) | Audit & Compliance should subscribe |
| Runtime anomaly detections | `RuntimeAnomalyDetectedEvent` published (outbox) | Audit & Compliance should subscribe |

**Correction needed:** At minimum, ensure existing domain events are consumed by Audit & Compliance. Add new events for incident transitions and mitigation approvals.

### 8.3 Missing State Persistence

| Data | Current State | Expected State |
|---|---|---|
| Incident timeline | `TimelineJson` (serialised string on entity) | ✅ Persisted, but embedded — consider normalising for queryability |
| Correlation data | `CorrelatedChangesJson`, `CorrelatedServicesJson` (serialised strings) | ✅ Persisted, but source not validated end-to-end |
| Mitigation decisions | `DecisionsJson` on `MitigationWorkflowRecord` | ✅ Persisted as JSON |
| Notification delivery status | Not tracked | ❌ Must be tracked by Notifications module, not here |

### 8.4 Missing Traceability

| Trace | Current State | Gap |
|---|---|---|
| Incident → Change → Service → Impact | `CorrelatedChangesJson` → `ServiceId` → `ImpactedContractsJson` | ⚠️ Data structures exist but Change Governance does not provide source events |
| Automation → Incident → Service | `IncidentId` + `ServiceId` on `AutomationWorkflowRecord` | ✅ Linked |
| Cost → Service → Release | `ServiceName` on `CostSnapshot`, `releaseId` on `GetCostByRelease` | ✅ Linked |
| Drift → Release → Baseline | `ReleaseId` on `DriftFinding`, comparison via `RuntimeBaseline` | ✅ Linked |
| Mitigation → Runbook → Incident | `LinkedRunbookId` on `MitigationWorkflowRecord`, `IncidentId` | ✅ Linked |

---

## 9. Corrections Needed — Prioritised

| # | Correction | Priority | Effort | Subdomain |
|---|---|---|---|---|
| 1 | Implement Notifications subscription for `RuntimeAnomalyDetectedEvent` and `CostAnomalyDetectedEvent` | HIGH | 3h | Cross-module |
| 2 | Publish domain events for critical incident creation and severity escalation | HIGH | 2h | Incidents |
| 3 | Publish domain events for automation approval requests | HIGH | 1h | Automation |
| 4 | Create dedicated Cost dashboard frontend page | HIGH | 8h | Cost |
| 5 | Validate end-to-end Change Governance → Incident correlation flow | HIGH | 4h | Incidents |
| 6 | Ensure Audit & Compliance subscribes to all operational domain events | MEDIUM | 3h | Cross-module |
| 7 | Publish domain events for incident lifecycle transitions (for audit) | MEDIUM | 2h | Incidents |
| 8 | Publish domain event for budget threshold breach | MEDIUM | 1h | Cost |
| 9 | Implement at least one telemetry adapter for `IRuntimeSignalIngestionPort` | MEDIUM | 8h | Runtime |
| 10 | Add background worker for automated `ReliabilitySnapshot` computation | MEDIUM | 4h | Reliability |
| 11 | Merge embryonic `operational-intelligence/` frontend into `operations/` | LOW | 2h | Frontend |
| 12 | Make reliability scoring weights configurable per tenant | LOW | 2h | Reliability |

**Total estimated effort:** ~40 hours
