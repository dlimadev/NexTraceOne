# Change Governance — Domain Model Finalization

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Aggregate Roots (5)

### 1.1 Release (ChangeIntelligence)

- **File:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Domain/ChangeIntelligence/Entities/Release.cs`
- **ID Type:** `ReleaseId` (strongly typed)
- **Properties:** `ApiAssetId`, `ServiceName`, `Version`, `Environment`, `PipelineSource`, `CommitSha`, `ChangeLevel` (enum), `Status` (enum), `ChangeScore`, `WorkItemReference`, `RolledBackFromReleaseId`, `CreatedAt`, `TenantId`, `EnvironmentId`
- **Owned entities:** ChangeEvent, BlastRadiusReport, ChangeIntelligenceScore, FreezeWindow (ref), ReleaseBaseline, ObservationWindow, PostReleaseReview, RollbackAssessment, ExternalMarker, DeploymentState
- **Status transitions:** Pending → Running → Succeeded/Failed → RolledBack

### 1.2 WorkflowTemplate (Workflow)

- **File:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Domain/Workflow/Entities/WorkflowTemplate.cs`
- **ID Type:** `WorkflowTemplateId` (strongly typed)
- **Properties:** `Name`, `Description`, `ChangeType`, `ApiCriticality`, `TargetEnvironment`, `MinimumApprovers`, `IsActive`, `CreatedAt`
- **Owned entities:** WorkflowStage[], SlaPolicy

### 1.3 WorkflowInstance (Workflow)

- **File:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Domain/Workflow/Entities/WorkflowInstance.cs`
- **ID Type:** `WorkflowInstanceId` (strongly typed)
- **Properties:** `WorkflowTemplateId`, `ReleaseId`, `SubmittedBy`, `Status` (enum), `CurrentStageIndex`, `SubmittedAt`, `CompletedAt`
- **Owned entities:** ApprovalDecision[], EvidencePack
- **Status transitions:** Draft → Pending → InReview → Approved/Rejected/Cancelled

### 1.4 PromotionRequest (Promotion)

- **File:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Domain/Promotion/Entities/PromotionRequest.cs`
- **ID Type:** `PromotionRequestId` (strongly typed)
- **Properties:** `ReleaseId`, `SourceEnvironmentId`, `TargetEnvironmentId`, `RequestedBy`, `Status` (enum), `Justification`, `RequestedAt`, `CompletedAt`
- **Owned entities:** PromotionGate[]
- **Status transitions:** Pending → InEvaluation → Approved/Rejected/Blocked/Cancelled

### 1.5 Ruleset (RulesetGovernance)

- **File:** `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Domain/RulesetGovernance/Entities/Ruleset.cs`
- **ID Type:** `RulesetId` (strongly typed)
- **Properties:** `Name`, `Description`, `Content` (JSON/YAML), `RulesetType` (enum), `IsActive`, `RulesetCreatedAt`
- **Owned entities:** RulesetBinding[], RulesetScore, LintExecution[]

---

## 2. Entities (22 Non-Root)

| Entity | Subdomain | Parent Aggregate | ID Type | Key Properties |
|--------|-----------|-----------------|---------|----------------|
| ChangeEvent | ChangeIntelligence | Release | `ChangeEventId` | EventType, Timestamp, Actor, Description |
| BlastRadiusReport | ChangeIntelligence | Release | `BlastRadiusReportId` | TotalAffectedConsumers, DirectConsumers[], TransitiveConsumers[], CalculatedAt |
| ChangeIntelligenceScore | ChangeIntelligence | Release | `ChangeIntelligenceScoreId` | Score(0.0-1.0), BreakingChangeWeight, BlastRadiusWeight, EnvironmentWeight, ComputedAt |
| FreezeWindow | ChangeIntelligence | (Root-level) | `FreezeWindowId` | ApiAssetId, StartTime, EndTime, Reason, Scope(enum), CreatedBy, Environment |
| ReleaseBaseline | ChangeIntelligence | Release | `ReleaseBaselineId` | RequestsPerMinute, ErrorRate, AvgLatencyMs, P95LatencyMs, P99LatencyMs, Throughput, CollectedFrom, CollectedTo |
| ObservationWindow | ChangeIntelligence | Release | `ObservationWindowId` | Phase(enum), StartTime, EndTime, MetricsCollected, Status |
| PostReleaseReview | ChangeIntelligence | Release | `PostReleaseReviewId` | CurrentPhase, Outcome(enum), ConfidenceScore, Summary, IsCompleted, StartedAt, CompletedAt |
| RollbackAssessment | ChangeIntelligence | Release | `RollbackAssessmentId` | IsViable, RiskLevel, ImpactAnalysis, AssessedAt, RollbackCommitSha |
| ExternalMarker | ChangeIntelligence | Release | `ExternalMarkerId` | MarkerType(enum), SourceSystem, ExternalId, OccurredAt, Metadata |
| DeploymentState | ChangeIntelligence | Release | `DeploymentStateId` | State(enum), Timestamp, Message |
| WorkflowStage | Workflow | WorkflowTemplate/Instance | `WorkflowStageId` | StageNumber, Name, Description, ApproversRequired, Status |
| ApprovalDecision | Workflow | WorkflowInstance | `ApprovalDecisionId` | StageId, ApprovedBy, ApprovalStatus(enum), Comment, ApprovedAt |
| EvidencePack | Workflow | WorkflowInstance | `EvidencePackId` | Content(JSON), Attachments[], GeneratedAt, ExportedAt, ExportFormat(enum) |
| SlaPolicy | Workflow | WorkflowTemplate | `SlaPolicyId` | StageId, MaxDurationHours, EscalationPolicy, NotifyAfterHours |
| PromotionGate | Promotion | PromotionRequest | `PromotionGateId` | GateName, GateType(enum), Status, Criteria[] |
| GateEvaluation | Promotion | PromotionGate | `GateEvaluationId` | EvaluationResult(enum), Details, EvaluatedAt, OverriddenBy, OverrideJustification |
| DeploymentEnvironment | Promotion | (Root-level) | `DeploymentEnvironmentId` | Name, EnvironmentType(enum), Criticality, IsCurrent |
| RulesetBinding | RulesetGovernance | Ruleset | `RulesetBindingId` | AssetType(enum), BoundAt |
| LintExecution | RulesetGovernance | Ruleset | `LintExecutionId` | ExecutedOn(ReleaseId/AssetId), StartedAt, CompletedAt, ExecutionStatus(enum) |
| LintFinding | RulesetGovernance | LintExecution | `LintFindingId` | Rule, Severity(enum), Message, Location, Suggestion |
| RulesetScore | RulesetGovernance | Ruleset | `RulesetScoreId` | TotalFindings, ErrorCount, WarningCount, InfoCount, ComputedAt |
| LintResult | RulesetGovernance | (Root-level) | `LintResultId` | ExecutionId, ReleaseId, Summary, ResultJson |

---

## 3. Enums (13+)

| Enum | Subdomain | Values | File Path |
|------|-----------|--------|-----------|
| ChangeType | ChangeIntelligence | Deployment, ConfigurationChange, ContractChange, SchemaChange, DependencyChange, PolicyChange, OperationalChange | `Domain/ChangeIntelligence/Enums/` |
| ChangeLevel | ChangeIntelligence | Operational(0), NonBreaking(1), Additive(2), Breaking(3), Publication(4) | `Domain/ChangeIntelligence/Enums/` |
| DeploymentStatus | ChangeIntelligence | Pending, Running, Succeeded, Failed, RolledBack | `Domain/ChangeIntelligence/Enums/` |
| ConfidenceStatus | ChangeIntelligence | NotAssessed, Validated, NeedsAttention, SuspectedRegression, CorrelatedWithIncident, Mitigated | `Domain/ChangeIntelligence/Enums/` |
| MarkerType | ChangeIntelligence | CiCd, APM, Incident, Manual | `Domain/ChangeIntelligence/Enums/` |
| FreezeScope | ChangeIntelligence | All, Environment, Service | `Domain/ChangeIntelligence/Enums/` |
| ObservationPhase | ChangeIntelligence | PreRelease, PostRelease, Validation | `Domain/ChangeIntelligence/Enums/` |
| ReviewOutcome | ChangeIntelligence | Success, Regression, Inconclusive, Pending | `Domain/ChangeIntelligence/Enums/` |
| ValidationStatus | ChangeIntelligence | Pending, Valid, Invalid, NeedsRework | `Domain/ChangeIntelligence/Enums/` |
| WorkflowStatus | Workflow | Draft, Pending, InReview, Approved, Rejected, Cancelled | `Domain/Workflow/Enums/` |
| ApprovalStatus | Workflow | Pending, Approved, Rejected, RequestedChanges | `Domain/Workflow/Enums/` |
| PromotionStatus | Promotion | Pending, InEvaluation, Approved, Rejected, Blocked, Cancelled | `Domain/Promotion/Enums/` |
| GateType | Promotion | Quality, Security, Performance, Compliance | `Domain/Promotion/Enums/` |

---

## 4. Cross-Module References

| Reference | From (Change Governance) | To (External Module) | Type |
|-----------|-------------------------|---------------------|------|
| `ApiAssetId` | Release, FreezeWindow | Service Catalog | Read-only reference |
| `EnvironmentId` | Release, FreezeWindow, PromotionRequest | Environment Management | Read-only reference |
| `TenantId` | All entities | Identity & Access | Security context |
| `ReleaseId` | WorkflowInstance, PromotionRequest, LintResult | Internal (ChangeIntelligence) | Internal cross-subdomain |

---

## 5. Domain Model Gaps

| Gap | Description | Impact | Priority |
|-----|-------------|--------|----------|
| G-01 | No `RowVersion` / `ConcurrencyToken` on any entity | Concurrent updates can silently overwrite data | P1 |
| G-02 | `BlastRadiusReport.TransitiveConsumers` is a JSON array, not normalised | Limits queryability and join potential | P2 |
| G-03 | `DeploymentEnvironment` in Promotion duplicates Environment Management data | Potential data drift between modules | P1 |
| G-04 | `FreezeWindow` is quasi-root-level in ChangeIntelligence but should be its own aggregate | Architectural clarity | P2 |
| G-05 | No domain events defined for cross-module integration (e.g., `ReleaseCreated`, `WorkflowApproved`) | Integration via Outbox is structural but domain events not explicit in entity code | P1 |
| G-06 | `SlaPolicy.EscalationPolicy` is a string — should be a value object or enum | Weak typing | P2 |
| G-07 | No value objects identified (e.g., Score could be a VO) | DDD best practice gap | P3 |

---

## 6. Final Domain Model Target

The domain model is **rich and well-structured** with 27 entities across 4 subdomains, 5 aggregate roots, 13+ enums, and strongly typed IDs. The main improvements needed are:

1. Add `RowVersion` / `ConcurrencyToken` to all mutable aggregates
2. Define explicit domain events for outbox integration
3. Resolve `DeploymentEnvironment` duplication with Environment Management
4. Consider value objects for `Score`, `EscalationPolicy`
5. Validate `ApiAssetId` references against Catalog on creation
