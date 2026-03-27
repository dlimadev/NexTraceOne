# Operational Intelligence Module — Domain Model Finalization

> **Status:** DRAFT  
> **Date:** 2026-03-24  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation

---

## 1. Aggregate Roots (5)

| Aggregate Root | Subdomain | File | Responsibility |
|---|---|---|---|
| `IncidentRecord` | Incidents | `Domain/Incidents/Entities/IncidentRecord.cs` | Operational incident lifecycle — detection, correlation, evidence, mitigation coordination, closure |
| `AutomationWorkflowRecord` | Automation | `Domain/Automation/Entities/AutomationWorkflowRecord.cs` | Controlled automation execution — preconditions, approval, steps, validation, audit |
| `ReliabilitySnapshot` | Reliability | `Domain/Reliability/Entities/ReliabilitySnapshot.cs` | Composite reliability scoring — `OverallScore = Runtime×0.50 + Incident×0.30 + Observability×0.20` |
| `RuntimeSnapshot` | Runtime | `Domain/Runtime/Entities/RuntimeSnapshot.cs` | Runtime metrics capture with automatic health classification (Healthy/Degraded/Unhealthy) |
| `CostSnapshot` | Cost | `Domain/Cost/Entities/CostSnapshot.cs` | Service cost capture with share decomposition (CPU, Memory, Network, Storage) and anomaly detection |

### Additional Aggregate-Level Entities

The following entities function as independent aggregates (standalone lifecycle, own TypedId, direct DbSet mapping) even though they are not the primary aggregate roots of their subdomains:

| Entity | Subdomain | File | Rationale |
|---|---|---|---|
| `DriftFinding` | Runtime | `Domain/Runtime/Entities/DriftFinding.cs` | Independent lifecycle (Detect → Acknowledge → Resolve), own severity classification, optional release correlation |
| `ObservabilityProfile` | Runtime | `Domain/Runtime/Entities/ObservabilityProfile.cs` | Independent assessment lifecycle per service/environment, own scoring formula |
| `ServiceCostProfile` | Cost | `Domain/Cost/Entities/ServiceCostProfile.cs` | Independent budget management per service, own alert threshold logic |
| `CostImportBatch` | Cost | `Domain/Cost/Entities/CostImportBatch.cs` | Independent batch lifecycle (Pending → Completed / Failed), own status management |

---

## 2. Entities (19 total)

| # | Entity | Type | Subdomain | Parent Aggregate | DbSet Mapped | Table | File |
|---|---|---|---|---|---|---|---|
| 1 | `IncidentRecord` | Aggregate Root | Incidents | Self | ✅ | `ops_incident_records` | `Incidents/Entities/IncidentRecord.cs` |
| 2 | `MitigationWorkflowRecord` | Entity | Incidents | IncidentRecord (via `IncidentId`) | ✅ | `ops_mitigation_workflows` | `Incidents/Entities/MitigationWorkflowRecord.cs` |
| 3 | `MitigationWorkflowActionLog` | Entity | Incidents | MitigationWorkflowRecord (via `WorkflowId`) | ✅ | `ops_mitigation_workflow_actions` | `Incidents/Entities/MitigationWorkflowActionLog.cs` |
| 4 | `MitigationValidationLog` | Entity | Incidents | MitigationWorkflowRecord (via `WorkflowId`) | ✅ | `ops_mitigation_validations` | `Incidents/Entities/MitigationValidationLog.cs` |
| 5 | `RunbookRecord` | Entity | Incidents | Standalone | ✅ | `ops_runbooks` | `Incidents/Entities/RunbookRecord.cs` |
| 6 | `AutomationWorkflowRecord` | Aggregate Root | Automation | Self | ✅ | `ops_automation_workflow_records` | `Automation/Entities/AutomationWorkflowRecord.cs` |
| 7 | `AutomationValidationRecord` | Entity | Automation | AutomationWorkflowRecord (via `WorkflowId`) | ✅ | `ops_automation_validation_records` | `Automation/Entities/AutomationValidationRecord.cs` |
| 8 | `AutomationAuditRecord` | Entity | Automation | AutomationWorkflowRecord (via `WorkflowId`) | ✅ | `ops_automation_audit_records` | `Automation/Entities/AutomationAuditRecord.cs` |
| 9 | `ReliabilitySnapshot` | Aggregate Root | Reliability | Self | ✅ | `ops_reliability_snapshots` | `Reliability/Entities/ReliabilitySnapshot.cs` |
| 10 | `RuntimeSnapshot` | Aggregate Root | Runtime | Self | ✅ | `ops_runtime_snapshots` | `Runtime/Entities/RuntimeSnapshot.cs` |
| 11 | `RuntimeBaseline` | Entity | Runtime | Standalone (per service/environment) | ✅ | `ops_runtime_baselines` | `Runtime/Entities/RuntimeBaseline.cs` |
| 12 | `DriftFinding` | Aggregate-Level | Runtime | Standalone | ✅ | `ops_drift_findings` | `Runtime/Entities/DriftFinding.cs` |
| 13 | `ObservabilityProfile` | Aggregate-Level | Runtime | Standalone | ✅ | `ops_observability_profiles` | `Runtime/Entities/ObservabilityProfile.cs` |
| 14 | `CostSnapshot` | Aggregate Root | Cost | Self | ✅ | `ops_cost_snapshots` | `Cost/Entities/CostSnapshot.cs` |
| 15 | `CostRecord` | Entity | Cost | CostImportBatch (via `BatchId`) | ✅ | `ops_cost_records` | `Cost/Entities/CostRecord.cs` |
| 16 | `CostAttribution` | Entity | Cost | Standalone | ✅ | `ops_cost_attributions` | `Cost/Entities/CostAttribution.cs` |
| 17 | `CostTrend` | Entity | Cost | Standalone | ✅ | `ops_cost_trends` | `Cost/Entities/CostTrend.cs` |
| 18 | `ServiceCostProfile` | Aggregate-Level | Cost | Standalone | ✅ | `ops_service_cost_profiles` | `Cost/Entities/ServiceCostProfile.cs` |
| 19 | `CostImportBatch` | Aggregate-Level | Cost | Standalone | ✅ | `ops_cost_import_batches` | `Cost/Entities/CostImportBatch.cs` |

**All 19 entities are mapped in their respective DbContexts** — no DbSet gaps.

---

## 3. Strongly Typed IDs (19)

All entities use sealed record TypedIds for type-safe identity:

| TypedId | Entity | Definition |
|---|---|---|
| `IncidentRecordId(Guid Value)` | IncidentRecord | `Domain/Incidents/Entities/IncidentRecord.cs` |
| `MitigationWorkflowRecordId(Guid Value)` | MitigationWorkflowRecord | `Domain/Incidents/Entities/MitigationWorkflowRecord.cs` |
| `MitigationWorkflowActionLogId(Guid Value)` | MitigationWorkflowActionLog | `Domain/Incidents/Entities/MitigationWorkflowActionLog.cs` |
| `MitigationValidationLogId(Guid Value)` | MitigationValidationLog | `Domain/Incidents/Entities/MitigationValidationLog.cs` |
| `RunbookRecordId(Guid Value)` | RunbookRecord | `Domain/Incidents/Entities/RunbookRecord.cs` |
| `AutomationWorkflowRecordId(Guid Value)` | AutomationWorkflowRecord | `Domain/Automation/Entities/AutomationWorkflowRecord.cs` |
| `AutomationValidationRecordId(Guid Value)` | AutomationValidationRecord | `Domain/Automation/Entities/AutomationValidationRecord.cs` |
| `AutomationAuditRecordId(Guid Value)` | AutomationAuditRecord | `Domain/Automation/Entities/AutomationAuditRecord.cs` |
| `ReliabilitySnapshotId(Guid Value)` | ReliabilitySnapshot | `Domain/Reliability/Entities/ReliabilitySnapshot.cs` |
| `RuntimeSnapshotId(Guid Value)` | RuntimeSnapshot | `Domain/Runtime/Entities/RuntimeSnapshot.cs` |
| `RuntimeBaselineId(Guid Value)` | RuntimeBaseline | `Domain/Runtime/Entities/RuntimeBaseline.cs` |
| `DriftFindingId(Guid Value)` | DriftFinding | `Domain/Runtime/Entities/DriftFinding.cs` |
| `ObservabilityProfileId(Guid Value)` | ObservabilityProfile | `Domain/Runtime/Entities/ObservabilityProfile.cs` |
| `CostSnapshotId(Guid Value)` | CostSnapshot | `Domain/Cost/Entities/CostSnapshot.cs` |
| `CostRecordId(Guid Value)` | CostRecord | `Domain/Cost/Entities/CostRecord.cs` |
| `CostAttributionId(Guid Value)` | CostAttribution | `Domain/Cost/Entities/CostAttribution.cs` |
| `CostTrendId(Guid Value)` | CostTrend | `Domain/Cost/Entities/CostTrend.cs` |
| `ServiceCostProfileId(Guid Value)` | ServiceCostProfile | `Domain/Cost/Entities/ServiceCostProfile.cs` |
| `CostImportBatchId(Guid Value)` | CostImportBatch | `Domain/Cost/Entities/CostImportBatch.cs` |

---

## 4. Enums (23 total, all persisted as strings)

### 4.1 Incidents Enums (11)

| Enum | Values | Used By | File |
|---|---|---|---|
| `IncidentType` | ServiceDegradation, AvailabilityIssue, DependencyFailure, ContractImpact, MessagingIssue, BackgroundProcessingIssue, OperationalRegression | IncidentRecord | `Incidents/Enums/IncidentType.cs` |
| `IncidentSeverity` | Warning, Minor, Major, Critical | IncidentRecord | `Incidents/Enums/IncidentSeverity.cs` |
| `IncidentStatus` | Open, Investigating, Mitigating, Monitoring, Resolved, Closed | IncidentRecord | `Incidents/Enums/IncidentStatus.cs` |
| `MitigationStatus` | NotStarted, InProgress, Applied, Verified, Failed | IncidentRecord | `Incidents/Enums/MitigationStatus.cs` |
| `MitigationWorkflowStatus` | Draft, Recommended, AwaitingApproval, Approved, InProgress, AwaitingValidation, Completed, Rejected, Cancelled | MitigationWorkflowRecord | `Incidents/Enums/MitigationWorkflowStatus.cs` |
| `MitigationActionType` | Investigate, ValidateChange, RollbackCandidate, RestartControlled, Reprocess, VerifyDependency, Escalate, ExecuteRunbook, ObserveAndValidate, ContractImpactReview | MitigationWorkflowRecord | `Incidents/Enums/MitigationActionType.cs` |
| `MitigationOutcome` | (custom outcomes for mitigation results) | MitigationWorkflowRecord | `Incidents/Enums/MitigationOutcome.cs` |
| `RiskLevel` | Low, Medium, High, Critical | MitigationWorkflowRecord | `Incidents/Enums/RiskLevel.cs` |
| `ValidationStatus` | Pending, InProgress, Passed, Failed, Skipped | MitigationValidationLog | `Incidents/Enums/ValidationStatus.cs` |
| `CorrelationConfidence` | NotAssessed, Low, Medium, High, Confirmed | IncidentRecord | `Incidents/Enums/CorrelationConfidence.cs` |
| `MitigationDecisionType` | (decision types for workflow decisions) | MitigationWorkflowRecord | `Incidents/Enums/MitigationDecisionType.cs` |

### 4.2 Automation Enums (6)

| Enum | Values | Used By | File |
|---|---|---|---|
| `AutomationWorkflowStatus` | Draft, PendingPreconditions, AwaitingApproval, Approved, ReadyToExecute, Executing, AwaitingValidation, Completed, Failed, Cancelled, Rejected | AutomationWorkflowRecord | `Automation/Enums/AutomationWorkflowStatus.cs` |
| `AutomationApprovalStatus` | Pending, Approved, Rejected | AutomationWorkflowRecord | `Automation/Enums/AutomationApprovalStatus.cs` |
| `AutomationOutcome` | Successful, PartiallySuccessful, Failed, Inconclusive, Cancelled, RolledBack | AutomationValidationRecord | `Automation/Enums/AutomationOutcome.cs` |
| `AutomationActionType` | (automation action types for step classification) | AutomationWorkflowRecord | `Automation/Enums/AutomationActionType.cs` |
| `AutomationAuditAction` | WorkflowCreated, PreconditionsEvaluated, ApprovalRequested, ApprovalGranted, ExecutionStarted, StepCompleted, (others) | AutomationAuditRecord | `Automation/Enums/AutomationAuditAction.cs` |
| `PreconditionType` | (precondition types for workflow readiness checks) | AutomationWorkflowRecord | `Automation/Enums/PreconditionType.cs` |

### 4.3 Runtime Enums (2)

| Enum | Values | Used By | File |
|---|---|---|---|
| `HealthStatus` | Healthy, Degraded, Unhealthy, Unknown | RuntimeSnapshot | `Runtime/Enums/HealthStatus.cs` |
| `DriftSeverity` | Low (<10%), Medium (10–25%), High (25–50%), Critical (>50%) | DriftFinding | `Runtime/Enums/DriftSeverity.cs` |

### 4.4 Reliability Enums (3)

| Enum | Values | Used By | File |
|---|---|---|---|
| `ReliabilityStatus` | Healthy, Degraded, Unavailable, NeedsAttention | ReliabilitySnapshot | `Reliability/Enums/ReliabilityStatus.cs` |
| `TrendDirection` | (Trend direction for reliability) | ReliabilitySnapshot | `Reliability/Enums/TrendDirection.cs` |
| `OperationalFlag` | (Operational flags for reliability context) | ReliabilitySnapshot | `Reliability/Enums/OperationalFlag.cs` |

### 4.5 Cost Enums (1)

| Enum | Values | Used By | File |
|---|---|---|---|
| `TrendDirection` | Rising, Stable, Declining | CostTrend | `Cost/Enums/TrendDirection.cs` |

---

## 5. Domain Events (3)

| Event | Subdomain | File | Payload | Expected Consumers |
|---|---|---|---|---|
| `RuntimeAnomalyDetectedEvent` | Runtime | `Runtime/Events/RuntimeAnomalyDetectedEvent.cs` | `AnomalyId`, `ServiceName`, `AnomalyType`, `Severity`, `DetectedAt` | Change Governance (blast radius), Notifications, Audit |
| `RuntimeSignalReceivedEvent` | Runtime | `Runtime/Events/RuntimeSignalReceivedEvent.cs` | `SignalId`, `SourceSystem`, `SignalType`, `ReceivedAt` | Change Governance (correlation), Audit |
| `CostAnomalyDetectedEvent` | Cost | `Cost/Events/CostAnomalyDetectedEvent.cs` | `AnomalyId`, `ServiceName`, `ExpectedCost`, `ActualCost`, `DetectedAt` | Change Governance (release correlation), Notifications, Audit |

All events extend `IntegrationEventBase("OperationalIntelligence")` and use the outbox pattern for eventual consistency.

---

## 6. Ports / Interfaces (2)

| Port | Subdomain | File | Purpose |
|---|---|---|---|
| `IRuntimeSignalIngestionPort` | Runtime | `Runtime/Ports/IRuntimeSignalIngestionPort.cs` | Contract for receiving runtime data from external monitoring systems (Prometheus, Datadog, CloudWatch). Prepared for future agent extraction. |
| `IRuntimeCorrelationPort` | Runtime | `Runtime/Ports/IRuntimeCorrelationPort.cs` | Contract for correlating runtime signals with releases from Change Governance. Prepared for future stream processor. |

---

## 7. Internal Entity Relationships

```
INCIDENTS SUBDOMAIN
━━━━━━━━━━━━━━━━━━
IncidentRecord (Aggregate Root)
  ├── 1:N → MitigationWorkflowRecord (via IncidentId)
  │          ├── 1:N → MitigationWorkflowActionLog (via WorkflowId)
  │          └── 1:N → MitigationValidationLog (via WorkflowId)
  └── N:M → RunbookRecord (via MitigationRecommendedRunbooksJson + LinkedRunbookId)

RunbookRecord (Standalone)
  └── Referenced by MitigationWorkflowRecord.LinkedRunbookId

AUTOMATION SUBDOMAIN
━━━━━━━━━━━━━━━━━━━━
AutomationWorkflowRecord (Aggregate Root)
  ├── 1:N → AutomationValidationRecord (via WorkflowId)
  └── 1:N → AutomationAuditRecord (via WorkflowId)

RELIABILITY SUBDOMAIN
━━━━━━━━━━━━━━━━━━━━━
ReliabilitySnapshot (Aggregate Root)
  └── Composed from: RuntimeSnapshot (health), IncidentRecord (impact), ObservabilityProfile (maturity)

RUNTIME SUBDOMAIN
━━━━━━━━━━━━━━━━━
RuntimeSnapshot (Aggregate Root)
  └── Compared against → RuntimeBaseline (via ServiceName + Environment)

DriftFinding (Standalone Aggregate)
  └── Optional → ReleaseId (correlation with Change Governance)

ObservabilityProfile (Standalone Aggregate)
  └── Per ServiceName + Environment

COST SUBDOMAIN
━━━━━━━━━━━━━━
CostSnapshot (Aggregate Root)
  └── Per ServiceName + Environment + CapturedAt

CostImportBatch (Standalone Aggregate)
  └── 1:N → CostRecord (via BatchId)

CostAttribution (Standalone)
  └── Per ApiAssetId + PeriodStart/PeriodEnd

CostTrend (Standalone)
  └── Per ServiceName + Environment + PeriodStart/PeriodEnd

ServiceCostProfile (Standalone Aggregate)
  └── Per ServiceName + Environment (budget + alert config)
```

---

## 8. Cross-Module Relationships

| This Module Entity/Field | References | Other Module | Relationship Type |
|---|---|---|---|
| `IncidentRecord.ServiceId` | → | Service Catalog (03) `ServiceAsset.Id` | Guid reference (no FK) |
| `IncidentRecord.CorrelatedChangesJson` | → | Change Governance (05) Change IDs | JSON array (loose coupling) |
| `IncidentRecord.ImpactedContractsJson` | → | Contracts (04) Contract IDs | JSON array (loose coupling) |
| `IncidentRecord.TenantId` | → | Identity & Access (01) Tenant | Guid reference (RLS context) |
| `IncidentRecord.EnvironmentId` | → | Environment Management (02) Environment | Guid reference |
| `AutomationWorkflowRecord.ServiceId` | → | Service Catalog (03) ServiceAsset | String reference |
| `AutomationWorkflowRecord.IncidentId` | → | Self (Incidents subdomain) IncidentRecord | String reference |
| `AutomationWorkflowRecord.ChangeId` | → | Change Governance (05) Change | String reference |
| `CostAttribution.ApiAssetId` | → | Service Catalog (03) `ApiAsset.Id` | Guid reference (no FK) |
| `DriftFinding.ReleaseId` | → | Change Governance (05) Release | Guid reference (optional) |
| `RuntimeAnomalyDetectedEvent` | → published to | Notifications (11), Audit & Compliance (10) | Integration event (outbox) |
| `CostAnomalyDetectedEvent` | → published to | Notifications (11), Audit & Compliance (10) | Integration event (outbox) |
| `RuntimeSignalReceivedEvent` | → published to | Change Governance (05), Audit & Compliance (10) | Integration event (outbox) |
| `IRuntimeCorrelationPort` | ← implemented by | Change Governance (05) (expected) | Port/Adapter |

---

## 9. Anemic Entity Assessment

| Entity | Assessment | Notes |
|---|---|---|
| `IncidentRecord` | **Rich** | Factory method `Create()`, state setters with guards (`SetCorrelation`, `SetEvidence`, `SetMitigation`, `SetDetail`, `SetTenantContext`), `UpdateCorrelationAssessment` |
| `MitigationWorkflowRecord` | **Rich** | Factory `Create()`, `UpdateStatus()`, `SetApproval()`, `SetStarted()`, `SetCompleted()`, `SetDecisions()` |
| `MitigationWorkflowActionLog` | **Thin — acceptable** | Immutable action log entry — no behaviour expected beyond creation |
| `MitigationValidationLog` | **Thin — acceptable** | Immutable validation record — no behaviour expected beyond creation |
| `RunbookRecord` | **Thin — needs enrichment** | Only static data (title, steps, prerequisites). Missing: `Publish()`, `Archive()`, `Review()` lifecycle methods |
| `AutomationWorkflowRecord` | **Rich** | Factory `Create()`, `UpdateStatus()`, `Approve()`, `Reject()` with guard clauses |
| `AutomationValidationRecord` | **Thin — acceptable** | Immutable post-execution record — creation via factory |
| `AutomationAuditRecord` | **Thin — acceptable** | Immutable audit entry — creation via factory |
| `ReliabilitySnapshot` | **Rich** | Factory `Create()` with scoring formula computation, clamping 0–100 |
| `RuntimeSnapshot` | **Rich** | Factory `Create()` with auto health classification, `CalculateDeviationsFrom(baseline)`, computed properties (`IsHealthy`, `IsDegraded`, `IsUnhealthy`) |
| `RuntimeBaseline` | **Rich** | Factory `Establish()`, `IsWithinTolerance()`, `Refresh()`, computed `IsConfident` |
| `DriftFinding` | **Rich** | Factory `Detect()` with auto-severity, `Acknowledge()`, `Resolve(comment)`, `CorrelateWithRelease()` — all return `Result<Unit>` |
| `ObservabilityProfile` | **Rich** | Factory `Assess()` with weighted scoring, `UpdateCapabilities()` with recalculation, computed `HasAdequateObservability` |
| `CostSnapshot` | **Rich** | Factory `Create()` with share validation, `IsAnomaly()`, `CalculateDeviationPercent()`, computed `UnattributedCost` |
| `CostRecord` | **Thin — acceptable** | Data record from batch import — no behaviour beyond creation |
| `CostAttribution` | **Moderate** | Factory `Attribute()` with period validation, `UpdateCosts()` with auto cost-per-request recalculation |
| `CostTrend` | **Moderate** | Factory `Create()` with auto direction classification (Rising/Stable/Declining), computed `IsSignificant` |
| `ServiceCostProfile` | **Rich** | Factory `Create()`, `SetBudget()`, `UpdateCurrentCost()`, `CheckBudgetAlert()`, `UpdateAlertThreshold()`, `ResetMonthlyCycle()` — all return `Result<Unit>` |
| `CostImportBatch` | **Moderate** | Factory `Create()`, `Complete(recordCount)`, `Fail(error)` — clear state machine |

### Summary

| Classification | Count | Entities |
|---|---|---|
| **Rich** (significant domain logic) | 9 | IncidentRecord, MitigationWorkflowRecord, AutomationWorkflowRecord, ReliabilitySnapshot, RuntimeSnapshot, RuntimeBaseline, DriftFinding, ObservabilityProfile, CostSnapshot |
| **Moderate** (factory + limited logic) | 3 | CostAttribution, CostTrend, CostImportBatch, ServiceCostProfile |
| **Thin — acceptable** (immutable records) | 5 | MitigationWorkflowActionLog, MitigationValidationLog, AutomationValidationRecord, AutomationAuditRecord, CostRecord |
| **Thin — needs enrichment** | 1 | RunbookRecord |

---

## 10. Misplaced Business Rules

| Rule | Current Location | Issue | Correct Location |
|---|---|---|---|
| Reliability scoring weights (0.50, 0.30, 0.20) | `ReliabilitySnapshot.Create()` (hardcoded constants) | ⚠️ Not configurable per tenant | Should be configurable via `ReliabilityScoringConfiguration` value object or module configuration |
| Health classification thresholds (ErrorRate 5%/10%, P99 1000ms/3000ms) | `RuntimeSnapshot.ClassifyHealth()` (hardcoded constants) | ⚠️ Not configurable per service | Should be configurable via `HealthThresholdConfiguration` or per-service override |
| Drift severity thresholds (10%, 25%, 50%) | `DriftFinding.DeriveServerity()` (hardcoded constants) | ⚠️ Not configurable | Should be configurable via module settings |
| Cost trend stability threshold (±5%) | `CostTrend.DeriveDirection()` (hardcoded constant) | ⚠️ Not configurable | Should be configurable via module settings |
| Observability scoring weights (0.25, 0.25, 0.20, 0.15, 0.15) | `ObservabilityProfile.DeriveScore()` (hardcoded constants) | ⚠️ Not configurable | Should be configurable or at least documented as product-level default |
| `CostImportBatch` status strings ("Pending", "Completed", "Failed") | Constants in entity | ⚠️ Should be enum | Replace with `CostImportBatchStatus` enum for type safety |

---

## 11. Missing Fields Assessment

| Entity | Missing Field | Rationale | Priority |
|---|---|---|---|
| `IncidentRecord` | `RowVersion` (uint/xmin) | Optimistic concurrency for concurrent incident updates | HIGH |
| `AutomationWorkflowRecord` | `RowVersion` (uint/xmin) | Optimistic concurrency for concurrent workflow actions | HIGH |
| `IncidentRecord` | `ResolvedAt` (DateTimeOffset?) | Track when incident was resolved (separate from `LastUpdatedAt`) | MEDIUM |
| `IncidentRecord` | `ResolvedBy` (string?) | Track who resolved the incident | MEDIUM |
| `RunbookRecord` | `IsPublished` (bool) | Distinguish draft runbooks from published ones | MEDIUM |
| `RunbookRecord` | `Version` (int) | Versioning for runbook updates | MEDIUM |
| `RuntimeSnapshot` | `TenantId` (Guid) | Multi-tenancy consistency (present on IncidentRecord but missing here) | MEDIUM |
| `CostSnapshot` | `TenantId` (Guid) | Multi-tenancy consistency | MEDIUM |
| `DriftFinding` | `TenantId` (Guid) | Multi-tenancy consistency | MEDIUM |
| `ReliabilitySnapshot` | `RowVersion` (uint/xmin) | Concurrency protection (less critical for snapshots) | LOW |

---

## 12. Unnecessary / Redundant Fields Assessment

| Entity | Field | Issue | Recommendation |
|---|---|---|---|
| `IncidentRecord` | `ServiceName` + `ServiceId` | Dual identification — `ServiceId` is the canonical reference, `ServiceName` can be resolved from Catalog | ⚠️ Keep both for denormalisation performance, but document that `ServiceId` is the source of truth |
| `AutomationWorkflowRecord` | `IncidentId` + `ChangeId` + `ServiceId` as strings | String references instead of strongly typed | ⚠️ Consider migrating to Guid references for `IncidentId` and `ChangeId` |
| `CostImportBatch` | `Status` as string constants | Not type-safe | Replace with `CostImportBatchStatus` enum |

---

## 13. Final Domain Model

The current domain model is **approved as structurally sound** with these required improvements:

### Must Add to Entities (HIGH priority)

1. `RowVersion` (xmin) on `IncidentRecord` and `AutomationWorkflowRecord` for optimistic concurrency
2. `CostImportBatchStatus` enum to replace string constants on `CostImportBatch`
3. `TenantId` on `RuntimeSnapshot`, `CostSnapshot`, and `DriftFinding` for multi-tenancy consistency

### Must Add to EF Core Configurations (HIGH priority)

4. `UseXminAsConcurrencyToken()` on `IncidentRecordConfiguration` and `AutomationWorkflowRecordConfiguration`
5. Check constraints for all enums (23 enums × appropriate database constraints)

### Business Rule Improvements (MEDIUM priority)

6. Make reliability scoring weights configurable via `ReliabilityScoringConfiguration`
7. Make health classification thresholds configurable per service/environment
8. Add lifecycle methods to `RunbookRecord`: `Publish()`, `Archive()`, `Review()`
9. Replace `CostImportBatch.Status` string constants with proper enum

### Entity Enrichment (LOW priority)

10. Add `ResolvedAt`/`ResolvedBy` to `IncidentRecord` for explicit resolution tracking
11. Add `IsPublished`/`Version` to `RunbookRecord` for publication lifecycle
12. Consider migrating string IDs on `AutomationWorkflowRecord` (`IncidentId`, `ChangeId`) to Guid for type safety

### Domain Model Classification

| Subdomain | Entities | Domain Richness | Assessment |
|---|---|---|---|
| **Incidents** | 5 entities, 11 enums | Rich aggregate + thin logs | ✅ **COERENTE** — appropriate mix of rich behaviour and immutable records |
| **Automation** | 3 entities, 6 enums | Rich aggregate + thin records | ✅ **COERENTE** — well-structured approval/execution lifecycle |
| **Reliability** | 1 entity, 3 enums | Rich scoring aggregate | ✅ **COERENTE** — clear responsibility, scoring formula in domain |
| **Runtime** | 4 entities, 2 enums | Rich aggregates with ports | ✅ **COERENTE** — health, baseline, drift, observability well-separated |
| **Cost** | 6 entities, 1 enum | Mix of rich and data entities | ✅ **COERENTE** — appropriate for financial data domain |

**The domain model is ready for the migration baseline once concurrency tokens, multi-tenancy gaps, and enum type safety are addressed.**
