# Part 9 — Scoring, Thresholds & Automation Review

> **Module:** Operational Intelligence
> **Date:** 2025-07-14
> **Status:** Review Complete
> **Scope:** ReliabilitySnapshot scoring, RuntimeSnapshot health classification, automation workflow state machine, approval flow, audit traceability

---

## 1. How Scoring Works Today

### 1.1 ReliabilitySnapshot — Weighted Formula

**Source:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Reliability/Entities/ReliabilitySnapshot.cs`

The `ReliabilitySnapshot` aggregate root computes an `OverallScore` (0–100) using a weighted average of three sub-scores:

```
OverallScore = (RuntimeHealthScore × 0.50)
             + (IncidentImpactScore × 0.30)
             + (ObservabilityScore × 0.20)
```

| Sub-Score | Weight | Range | Source |
|---|---|---|---|
| `RuntimeHealthScore` | 50% | 0–100 | Derived from `RuntimeSnapshot` health metrics |
| `IncidentImpactScore` | 30% | 0–100 | Derived from open incident count and severity |
| `ObservabilityScore` | 20% | 0–100 | Coverage of monitoring/alerting for the service |

**Additional properties on `ReliabilitySnapshot`:**

- `OpenIncidentCount` (int) — number of unresolved incidents at snapshot time
- `RuntimeHealthStatus` (string) — Healthy / Degraded / Unavailable / NeedsAttention
- `TrendDirection` (enum) — Improving / Stable / Declining
- `ComputedAt` (DateTimeOffset) — timestamp of computation
- `TenantId`, `ServiceId`, `Environment` — scoping fields

### 1.2 RuntimeSnapshot — Health Classification

**Source:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Runtime/Entities/RuntimeSnapshot.cs` (lines 24–34, 161–174)

`RuntimeSnapshot` classifies service health into `HealthStatus` (enum: Healthy, Degraded, Unhealthy, Unknown) using two metrics:

| Metric | Degraded Threshold | Unhealthy Threshold |
|---|---|---|
| `ErrorRate` | ≥ 5% (`0.05m`) | ≥ 10% (`0.10m`) |
| `P99LatencyMs` | ≥ 1000 ms | ≥ 3000 ms |

**Classification logic (worst-of):**

```
if (ErrorRate ≥ 0.10m OR P99LatencyMs ≥ 3000m) → Unhealthy
else if (ErrorRate ≥ 0.05m OR P99LatencyMs ≥ 1000m) → Degraded
else → Healthy
```

### 1.3 Is Scoring Real or Partial?

**Verdict: Real formula, seed/mock data.**

- The weighted formula is implemented in domain code and is fully functional.
- The `ReliabilityDbContext` (`src/modules/operationalintelligence/.../Reliability/Persistence/ReliabilityDbContext.cs`) persists computed snapshots.
- Endpoints in `ReliabilityEndpointModule` serve scores, trends, coverage, and team-level views.
- **However**, in the current state, sub-scores (`RuntimeHealthScore`, `IncidentImpactScore`, `ObservabilityScore`) are populated from seed data or test fixtures — there is no production pipeline computing them from real telemetry.
- The `RuntimeSnapshot` health classification is real but depends on ingested runtime metrics, which currently come from seed data.

**Gap:** No automated computation pipeline transforms raw telemetry → sub-scores → `ReliabilitySnapshot`. This is a prerequisite for production use.

---

## 2. Threshold Review

### 2.1 ErrorRate Thresholds

| Level | Value | Constant |
|---|---|---|
| Degraded | 5% | `DegradedErrorRateThreshold = 0.05m` |
| Unhealthy | 10% | `UnhealthyErrorRateThreshold = 0.10m` |

**Assessment:** Reasonable defaults for general-purpose services. However:

- ❌ Thresholds are **hardcoded constants** — not configurable per service or environment.
- ❌ No support for per-service SLO-based thresholds (e.g., a payment service may need 1% / 3%).
- ❌ No hysteresis or cooldown to prevent flapping between Degraded ↔ Healthy.

### 2.2 P99 Latency Thresholds

| Level | Value | Constant |
|---|---|---|
| Degraded | 1000 ms | `DegradedLatencyThresholdMs = 1000m` |
| Unhealthy | 3000 ms | `UnhealthyLatencyThresholdMs = 3000m` |

**Assessment:** Acceptable as defaults but too coarse for latency-sensitive services.

- ❌ Same hardcoded-constant issue.
- ❌ No P50/P95 thresholds — only P99 is evaluated.
- ❌ No throughput-weighted evaluation (a service with 1 req/s at 3100ms P99 is not the same as 10k req/s).

### 2.3 Threshold Corrections Needed

| ID | Issue | Priority | Effort |
|---|---|---|---|
| TH-01 | Make thresholds configurable per service (DB-backed) | P1 | Medium |
| TH-02 | Add SLO-based threshold overrides | P2 | Medium |
| TH-03 | Add hysteresis / debounce for health transitions | P2 | Small |
| TH-04 | Consider multi-metric composite (P50+P99+ErrorRate) | P3 | Medium |
| TH-05 | Add throughput context to classification | P3 | Medium |

---

## 3. Automation Review

### 3.1 Automation Action Types (8 Total)

**Source:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Automation/Enums/AutomationActionType.cs`

| Action | Value | Sensitivity |
|---|---|---|
| `RestartControlled` | 0 | 🔴 **High** — service disruption risk |
| `ReprocessControlled` | 1 | 🟡 Medium — data reprocessing |
| `ExecuteRunbookStep` | 2 | 🟡 Medium — depends on runbook content |
| `RollbackReadinessReview` | 3 | 🔴 **High** — change reversal |
| `ObserveAndValidate` | 4 | 🟢 Low — read-only observation |
| `EscalateWithContext` | 5 | 🟢 Low — notification only |
| `VerifyDependencyState` | 6 | 🟢 Low — read-only check |
| `ValidatePostChangeState` | 7 | 🟡 Medium — post-change validation |

**Note:** The task specification mentions 8 sensitive actions (RestartControlled, ScaleOut, ScaleIn, ToggleFeatureFlag, DrainInstance, RollbackDeployment, PurgeQueue, RunDiagnostics). The current enum covers 8 actions but with different names. **ScaleOut, ScaleIn, ToggleFeatureFlag, DrainInstance, PurgeQueue, RunDiagnostics are not yet implemented as action types.**

### 3.2 Workflow State Machine

**Source:** `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/Automation/Enums/AutomationWorkflowStatus.cs`

```
Draft (0)
  ↓
PendingPreconditions (1)
  ↓
AwaitingApproval (2) ──→ Rejected (10)
  ↓
Approved (3)
  ↓
ReadyToExecute (4)
  ↓
Executing (5) ──→ Failed (8)
  ↓               ↑
AwaitingValidation (6)
  ↓
Completed (7)

(Any non-terminal state) ──→ Cancelled (9)
```

**Terminal states:** Completed (7), Failed (8), Cancelled (9), Rejected (10)

**Assessment:**

- ✅ Proper multi-step lifecycle with distinct pre-execution gates.
- ✅ `PendingPreconditions` allows pre-flight checks before approval.
- ✅ `AwaitingValidation` separates execution from confirmation.
- ❌ No explicit `Paused` or `Suspended` state for long-running workflows.
- ❌ No `RetryPending` state — failed workflows cannot be retried without creating a new workflow.

### 3.3 Approval Flow

**Source:** `AutomationApprovalStatus` enum

| Status | Value | Description |
|---|---|---|
| `NotRequired` | 0 | Low-risk actions skip approval |
| `Pending` | 1 | Awaiting approver action |
| `Approved` | 2 | Approved for execution |
| `Rejected` | 3 | Denied — workflow transitions to Rejected |
| `Escalated` | 4 | Escalated to higher authority |

**Approval permission:** `operations:automation:approve`
**Execution permission:** `operations:automation:execute`

**Assessment:**

- ✅ Separate approve and execute permissions — good separation of duties.
- ✅ `Escalated` state exists for multi-level approval.
- ❌ No approval quorum (e.g., require 2-of-3 approvers for Critical risk).
- ❌ No time-based auto-expiry for pending approvals.
- ❌ No explicit check that approver ≠ requester (four-eyes principle).

### 3.4 Sensitive Automation Identification

**High-sensitivity actions requiring mandatory approval:**

| Action | Risk | Justification |
|---|---|---|
| `RestartControlled` | Service downtime, user impact | Must require approval + audit |
| `RollbackReadinessReview` | Change reversal, potential data loss | Must require approval + audit |
| (Future) `ScaleOut` / `ScaleIn` | Cost impact, capacity risk | Should require approval for production |
| (Future) `DrainInstance` | Traffic redistribution risk | Should require approval |
| (Future) `RollbackDeployment` | Direct production change | Must require approval + step-up auth |

**Current enforcement:** The `AutomationEndpointModule` checks `operations:automation:approve` permission before status transitions but does not enforce **risk-level-based mandatory approval** — a Low-risk action follows the same flow as a Critical-risk action.

### 3.5 Recommendation vs Execution Distinction

**Current flow:**

```
Draft → PendingPreconditions → AwaitingApproval → Approved → ReadyToExecute → Executing
```

- ✅ The `Draft` state serves as a recommendation/proposal.
- ✅ `AwaitingApproval` gates execution behind human review.
- ✅ `Approved` → `ReadyToExecute` → `Executing` is a clear 3-step execution path.
- ❌ No AI-generated recommendation flag — a human-created Draft is indistinguishable from an AI-suggested one.
- ❌ No read-only "Suggestion" status that explicitly cannot transition to execution without human conversion to Draft.

### 3.6 Automation Traceability

**Source:** `AutomationAuditRecord` entity + `AutomationAuditAction` enum

**Tracked actions:**

| Audit Action | Tracked |
|---|---|
| Create | ✅ |
| Update | ✅ |
| RequestApproval | ✅ |
| Approve | ✅ |
| Reject | ✅ |
| Execute | ✅ |
| Complete | ✅ |
| Cancel | ✅ |
| Fail | ✅ |

**Audit record fields:** `WorkflowId`, `Action`, `Actor`, `Details`, `ServiceId`, `TeamId`, `OccurredAt`, `CreatedAt`

**Assessment:**

- ✅ Comprehensive lifecycle coverage — all state transitions are audited.
- ✅ Actor and timestamp recorded.
- ❌ No integration with central Audit & Compliance module — records stay in `AutomationDbContext` only.
- ❌ No IP address or session context captured.
- ❌ No before/after state diff in `Details` field.
- ❌ No tamper-evident mechanism (e.g., hash chain).

---

## 4. Gaps and Corrections Backlog

| ID | Category | Gap | Priority | Effort | Area |
|---|---|---|---|---|---|
| SC-01 | Scoring | No production computation pipeline for sub-scores | P1 | Large | Backend/Infra |
| SC-02 | Scoring | Sub-scores populated from seed data only | P1 | Medium | Backend |
| SC-03 | Scoring | No documentation of scoring formula for operators | P1 | Small | Docs |
| TH-01 | Thresholds | Hardcoded thresholds — not configurable per service | P1 | Medium | Backend |
| TH-02 | Thresholds | No SLO-based threshold overrides | P2 | Medium | Backend |
| TH-03 | Thresholds | No hysteresis for health state transitions | P2 | Small | Backend |
| AU-01 | Automation | Missing action types (ScaleOut, ScaleIn, etc.) | P2 | Medium | Backend |
| AU-02 | Automation | No risk-level-based mandatory approval enforcement | P1 | Medium | Backend |
| AU-03 | Automation | No four-eyes principle (approver ≠ requester) | P1 | Small | Backend |
| AU-04 | Automation | No approval quorum for Critical risk | P2 | Medium | Backend |
| AU-05 | Automation | No approval expiry / timeout | P3 | Small | Backend |
| AU-06 | Automation | No Retry/Paused states in workflow | P3 | Small | Backend |
| AU-07 | Automation | No AI-recommendation flag on Draft | P3 | Small | Backend |
| TR-01 | Traceability | No integration with central Audit & Compliance | P1 | Medium | Backend |
| TR-02 | Traceability | No IP/session context in audit records | P2 | Small | Backend |
| TR-03 | Traceability | No before/after state diff in Details | P3 | Medium | Backend |

---

## 5. References

| Artifact | Path |
|---|---|
| ReliabilitySnapshot entity | `src/modules/operationalintelligence/.../Reliability/Entities/ReliabilitySnapshot.cs` |
| RuntimeSnapshot entity | `src/modules/operationalintelligence/.../Runtime/Entities/RuntimeSnapshot.cs` |
| AutomationWorkflowStatus enum | `src/modules/operationalintelligence/.../Automation/Enums/AutomationWorkflowStatus.cs` |
| AutomationApprovalStatus enum | `src/modules/operationalintelligence/.../Automation/Enums/AutomationApprovalStatus.cs` |
| AutomationActionType enum | `src/modules/operationalintelligence/.../Automation/Enums/AutomationActionType.cs` |
| AutomationAuditRecord entity | `src/modules/operationalintelligence/.../Automation/Entities/AutomationAuditRecord.cs` |
| AutomationAuditAction enum | `src/modules/operationalintelligence/.../Automation/Enums/AutomationAuditAction.cs` |
| ReliabilityEndpointModule | `src/modules/operationalintelligence/.../API/Reliability/Endpoints/ReliabilityEndpointModule.cs` |
| AutomationEndpointModule | `src/modules/operationalintelligence/.../API/Automation/Endpoints/AutomationEndpointModule.cs` |
| ReliabilityDbContext | `src/modules/operationalintelligence/.../Reliability/Persistence/ReliabilityDbContext.cs` |
| AutomationDbContext | `src/modules/operationalintelligence/.../Automation/Persistence/AutomationDbContext.cs` |
