# Operational Intelligence — Backend Functional Corrections (Part 7)

> **Status:** DRAFT  
> **Date:** 2026-03-25  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation  
> **Source:** Code analysis of `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.API/` and `…Application/`

---

## 1. Endpoint Inventory (56 Endpoints across 7 EndpointModules)

### 1.1 IncidentEndpointModule (10 endpoints)

**File:** `…API/Incidents/Endpoints/Endpoints/IncidentEndpointModule.cs`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 1 | POST | `/api/v1/incidents` | `operations:incidents:write` | `CreateIncident.Command` | Create operational incident with initial data |
| 2 | GET | `/api/v1/incidents` | `operations:incidents:read` | `ListIncidents.Query` | List incidents with filters (team, service, environment, severity, status, type, search, date range) |
| 3 | GET | `/api/v1/incidents/summary` | `operations:incidents:read` | `GetIncidentSummary.Query` | Aggregated summary (total open, critical, with correlation, with mitigation) |
| 4 | GET | `/api/v1/incidents/{incidentId}` | `operations:incidents:read` | `GetIncidentDetail.Query` | Full incident detail with timeline, linked services, correlation |
| 5 | GET | `/api/v1/incidents/{incidentId}/correlation` | `operations:incidents:read` | `GetIncidentCorrelation.Query` | Correlation with changes, services, dependencies |
| 6 | POST | `/api/v1/incidents/{incidentId}/correlation/refresh` | `operations:incidents:write` | `RefreshIncidentCorrelation.Command` | Recompute incident correlation |
| 7 | GET | `/api/v1/incidents/{incidentId}/evidence` | `operations:incidents:read` | `GetIncidentEvidence.Query` | Evidence and signal observations |
| 8 | GET | `/api/v1/incidents/{incidentId}/mitigation` | `operations:incidents:read` | `GetIncidentMitigation.Query` | Mitigation actions and runbook links |
| 9 | GET | `/api/v1/services/{serviceId}/incidents` | `operations:incidents:read` | `ListIncidentsByService.Query` | Incidents scoped to a specific service |
| 10 | GET | `/api/v1/teams/{teamId}/incidents` | `operations:incidents:read` | `ListIncidentsByTeam.Query` | Incidents scoped to a specific team |

### 1.2 MitigationEndpointModule (7 endpoints)

**File:** `…API/Incidents/Endpoints/Endpoints/MitigationEndpointModule.cs`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 11 | GET | `/api/v1/incidents/{incidentId}/mitigation/recommendations` | `operations:mitigation:read` | `GetMitigationRecommendations.Query` | AI/rule-based mitigation recommendations |
| 12 | GET | `/api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}` | `operations:mitigation:read` | `GetMitigationWorkflow.Query` | Workflow detail with steps, decisions |
| 13 | POST | `/api/v1/incidents/{incidentId}/mitigation/workflows` | `operations:mitigation:write` | `CreateMitigationWorkflow.Command` | Create mitigation workflow (title, actionType, riskLevel, requiresApproval, steps) |
| 14 | PATCH | `/api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}/actions` | `operations:mitigation:write` | `UpdateMitigationWorkflowAction.Command` | Execute workflow action (action, performedBy, reason, notes) |
| 15 | GET | `/api/v1/incidents/{incidentId}/mitigation/history` | `operations:mitigation:read` | `GetMitigationHistory.Query` | Mitigation history for incident |
| 16 | GET | `/api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}/validation` | `operations:mitigation:read` | `GetMitigationValidation.Query` | Post-mitigation validation status |
| 17 | POST | `/api/v1/incidents/{incidentId}/mitigation/workflows/{workflowId}/validation` | `operations:mitigation:write` | `RecordMitigationValidation.Command` | Record post-mitigation validation (status, observedOutcome, checks) |

### 1.3 RunbookEndpointModule (2 endpoints)

**File:** `…API/Incidents/Endpoints/Endpoints/RunbookEndpointModule.cs`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 18 | GET | `/api/v1/runbooks` | `operations:runbooks:read` | `ListRunbooks.Query` | List runbooks (filters: serviceId, incidentType, search) |
| 19 | GET | `/api/v1/runbooks/{runbookId}` | `operations:runbooks:read` | `GetRunbookDetail.Query` | Runbook detail with steps and prerequisites |

### 1.4 AutomationEndpointModule (15 endpoints)

**File:** `…API/Automation/Endpoints/Endpoints/AutomationEndpointModule.cs`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 20 | GET | `/api/v1/automation/actions` | `operations:automation:read` | `ListAutomationActions.Query` | List available automation actions |
| 21 | GET | `/api/v1/automation/actions/{actionId}` | `operations:automation:read` | `GetAutomationAction.Query` | Action detail with safety controls |
| 22 | POST | `/api/v1/automation/workflows` | `operations:automation:write` | `CreateAutomationWorkflow.Command` | Create workflow (actionId, serviceId, incidentId, changeId, rationale, scope, environment) |
| 23 | GET | `/api/v1/automation/workflows` | `operations:automation:read` | `ListAutomationWorkflows.Query` | List workflows (filters: serviceId, status, pagination) |
| 24 | GET | `/api/v1/automation/workflows/{workflowId}` | `operations:automation:read` | `GetAutomationWorkflow.Query` | Workflow detail with steps, preconditions, validation |
| 25 | POST | `/api/v1/automation/workflows/{workflowId}/request-approval` | `operations:automation:write` | `UpdateAutomationWorkflowAction.Command` | Request approval for workflow |
| 26 | POST | `/api/v1/automation/workflows/{workflowId}/approve` | `operations:automation:approve` | `UpdateAutomationWorkflowAction.Command` | Approve workflow execution |
| 27 | POST | `/api/v1/automation/workflows/{workflowId}/reject` | `operations:automation:approve` | `UpdateAutomationWorkflowAction.Command` | Reject workflow |
| 28 | POST | `/api/v1/automation/workflows/{workflowId}/execute` | `operations:automation:execute` | `UpdateAutomationWorkflowAction.Command` | Begin workflow execution |
| 29 | POST | `/api/v1/automation/workflows/{workflowId}/cancel` | `operations:automation:write` | `UpdateAutomationWorkflowAction.Command` | Cancel workflow |
| 30 | POST | `/api/v1/automation/workflows/{workflowId}/complete-step` | `operations:automation:execute` | `UpdateAutomationWorkflowAction.Command` | Complete a workflow step |
| 31 | POST | `/api/v1/automation/workflows/{workflowId}/evaluate-preconditions` | `operations:automation:write` | `EvaluatePreconditions.Command` | Evaluate preconditions before execution |
| 32 | GET | `/api/v1/automation/workflows/{workflowId}/validation` | `operations:automation:read` | `GetAutomationValidation.Query` | Post-execution validation status |
| 33 | POST | `/api/v1/automation/workflows/{workflowId}/validation` | `operations:automation:write` | `RecordAutomationValidation.Command` | Record post-execution validation result |
| 34 | GET | `/api/v1/automation/audit` | `operations:automation:read` | `GetAutomationAuditTrail.Query` | Audit trail (filters: workflowId, serviceId, teamId) |

### 1.5 ReliabilityEndpointModule (7 endpoints)

**File:** `…API/Reliability/Endpoints/Endpoints/ReliabilityEndpointModule.cs`  
**Rate Limit Group:** `operations`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 35 | GET | `/api/v1/reliability/services` | `operations:reliability:read` | `ListServiceReliability.Query` | List services with reliability summary (filters: teamId, serviceId, domain, environment, status, serviceType, criticality, search, pagination) |
| 36 | GET | `/api/v1/reliability/services/{serviceId}` | `operations:reliability:read` | `GetServiceReliabilityDetail.Query` | Service reliability detail with dependencies, contracts, runbooks |
| 37 | GET | `/api/v1/reliability/services/{serviceId}/trend` | `operations:reliability:read` | `GetServiceReliabilityTrend.Query` | Service reliability score trend over time |
| 38 | GET | `/api/v1/reliability/services/{serviceId}/coverage` | `operations:reliability:read` | `GetServiceReliabilityCoverage.Query` | Operational coverage assessment |
| 39 | GET | `/api/v1/reliability/teams/{teamId}/summary` | `operations:reliability:read` | `GetTeamReliabilitySummary.Query` | Team-level reliability summary |
| 40 | GET | `/api/v1/reliability/teams/{teamId}/trend` | `operations:reliability:read` | `GetTeamReliabilityTrend.Query` | Team reliability trend over time |
| 41 | GET | `/api/v1/reliability/domains/{domainId}/summary` | `operations:reliability:read` | `GetDomainReliabilitySummary.Query` | Domain-level reliability summary |

### 1.6 RuntimeIntelligenceEndpointModule (8 endpoints)

**File:** `…API/Runtime/Endpoints/Endpoints/RuntimeIntelligenceEndpointModule.cs`  
**Rate Limit Group:** `operations`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 42 | POST | `/api/v1/runtime/snapshots` | `operations:runtime:write` | `IngestRuntimeSnapshot.Command` | Ingest runtime metrics (latency, error rate, throughput, CPU, memory) |
| 43 | GET | `/api/v1/runtime/health` | `operations:runtime:read` | `GetRuntimeHealth.Query` | Current service health status |
| 44 | GET | `/api/v1/runtime/observability` | `operations:runtime:read` | `GetObservabilityScore.Query` | Observability assessment score and breakdown |
| 45 | POST | `/api/v1/runtime/observability/assess` | `operations:runtime:write` | `ComputeObservabilityDebt.Command` | Assess observability debt for a service |
| 46 | POST | `/api/v1/runtime/drift/detect` | `operations:runtime:write` | `DetectRuntimeDrift.Command` | Detect runtime drift against baseline |
| 47 | GET | `/api/v1/runtime/drift` | `operations:runtime:read` | `GetDriftFindings.Query` | List drift findings (filters: serviceName, environment, unacknowledgedOnly, pagination) |
| 48 | GET | `/api/v1/runtime/timeline` | `operations:runtime:read` | `GetReleaseHealthTimeline.Query` | Health timeline within a time window |
| 49 | GET | `/api/v1/runtime/compare` | `operations:runtime:read` | `CompareReleaseRuntime.Query` | Compare metrics between two time periods (before/after) |

### 1.7 CostIntelligenceEndpointModule (9 endpoints)

**File:** `…API/Cost/Endpoints/Endpoints/CostIntelligenceEndpointModule.cs`

| # | Method | Route | Permission | Handler | Use Case |
|---|---|---|---|---|---|
| 50 | POST | `/api/v1/cost/snapshots` | `operations:cost:write` | `IngestCostSnapshot.Command` | Ingest cost snapshot with share decomposition |
| 51 | GET | `/api/v1/cost/report` | `operations:cost:read` | `GetCostReport.Query` | Cost report by service/environment (pagination) |
| 52 | GET | `/api/v1/cost/by-release/{releaseId}` | `operations:cost:read` | `GetCostByRelease.Query` | Cost analysis scoped to a release |
| 53 | GET | `/api/v1/cost/by-route` | `operations:cost:read` | `GetCostByRoute.Query` | Cost by route/service (pagination) |
| 54 | GET | `/api/v1/cost/delta` | `operations:cost:read` | `GetCostDelta.Query` | Cost delta between two time periods |
| 55 | POST | `/api/v1/cost/attributions` | `operations:cost:write` | `AttributeCostToService.Command` | Attribute cost to a specific API/service |
| 56 | POST | `/api/v1/cost/trends` | `operations:cost:write` | `ComputeCostTrend.Command` | Compute cost trends from snapshot data |
| 57 | POST | `/api/v1/cost/import` | `operations:cost:write` | `ImportCostBatch.Command` | Import batch of cost records from external source |
| 58 | POST | `/api/v1/cost/anomaly-check` | `operations:cost:write` | `AlertCostAnomaly.Command` | Check for cost anomalies against budget |

**Total: 10 + 7 + 2 + 15 + 7 + 8 + 9 = 58 endpoints**

---

## 2. Endpoint → Use Case Mapping

### 2.1 Signal Ingestion (3 endpoints)

| # | Endpoint | Flow |
|---|---|---|
| 42 | `POST /runtime/snapshots` | External source → `IngestRuntimeSnapshot` → `RuntimeSnapshot.Create()` → health classification → persist → outbox event |
| 50 | `POST /cost/snapshots` | External source → `IngestCostSnapshot` → `CostSnapshot.Create()` → share validation → persist → outbox event |
| 57 | `POST /cost/import` | External source → `ImportCostBatch` → `CostImportBatch.Create()` → N×`CostRecord` → persist |

### 2.2 Scoring & Assessment (3 endpoints)

| # | Endpoint | Flow |
|---|---|---|
| 45 | `POST /runtime/observability/assess` | Service + environment → `ComputeObservabilityDebt` → check tracing/metrics/logging/alerting/dashboard → score → persist `ObservabilityProfile` |
| 46 | `POST /runtime/drift/detect` | Service + environment → `DetectRuntimeDrift` → compare current vs baseline → create `DriftFinding` per metric |
| 58 | `POST /cost/anomaly-check` | Service + environment → `AlertCostAnomaly` → compare current cost vs budget threshold → alert if exceeded |

### 2.3 Rule Evaluation & Correlation (2 endpoints)

| # | Endpoint | Flow |
|---|---|---|
| 6 | `POST /incidents/{id}/correlation/refresh` | Incident → `RefreshIncidentCorrelation` → correlate with changes, services, dependencies → update JSON columns |
| 31 | `POST /automation/workflows/{id}/evaluate-preconditions` | Workflow → `EvaluatePreconditions` → evaluate safety preconditions → pass/fail result |

### 2.4 Automation State Machine (6 endpoints)

| # | Endpoint | Flow |
|---|---|---|
| 25 | `POST …/request-approval` | Draft → PendingApproval + audit record |
| 26 | `POST …/approve` | PendingApproval → Approved + audit record |
| 27 | `POST …/reject` | PendingApproval → Rejected + audit record |
| 28 | `POST …/execute` | Approved → Executing + audit record |
| 29 | `POST …/cancel` | (any active) → Cancelled + audit record |
| 30 | `POST …/complete-step` | Executing → step completed + audit record |

### 2.5 State Queries (28 endpoints)

All GET endpoints serve current state queries — listing, detail, summary, and filtered views across all 5 subdomains.

### 2.6 History & Audit Queries (3 endpoints)

| # | Endpoint | Scope |
|---|---|---|
| 15 | `GET …/mitigation/history` | Mitigation workflow history for an incident |
| 34 | `GET /automation/audit` | Automation audit trail (filters: workflowId, serviceId, teamId) |
| 16 | `GET …/mitigation/…/validation` | Post-mitigation validation status |

---

## 3. Dead & Incomplete Endpoints

### 3.1 Dead Endpoints

> **Result:** ❌ No dead endpoints found. All 58 endpoints have corresponding handlers, validators, and are registered in endpoint modules.

### 3.2 Incomplete / Placeholder Endpoints

| # | Area | Status | Details |
|---|---|---|---|
| 1 | **Retention configuration** | ❌ Missing | No `ConfigureRetention` endpoint exists. No retention policies are defined for any subdomain. Runtime snapshots, cost records, and drift findings grow without bounds. |
| 2 | **Incident update/close** | ⚠️ Implicit | No explicit `PUT /incidents/{id}` or `PATCH /incidents/{id}/status`. Status changes appear to happen via correlation refresh and mitigation workflows only. |
| 3 | **Runbook CRUD** | ⚠️ Read-only | Only `GET` endpoints for runbooks. No `POST/PUT/DELETE` for creating, updating, or deleting runbooks. |
| 4 | **Automation action CRUD** | ⚠️ Read-only | Action catalog is read-only. No endpoints to register, update, or deactivate automation actions. |
| 5 | **Cost profile management** | ⚠️ Missing | `ServiceCostProfile` entity exists but no endpoint to create/update budget/threshold configuration. |
| 6 | **Baseline management** | ⚠️ Missing | `RuntimeBaseline` entity exists but no endpoint to manually set or adjust baselines. |

---

## 4. Validation Review

### 4.1 FluentValidation Coverage

All 53 handlers have corresponding `Validator` classes defined as nested types inside each feature file (VSA pattern).

| Subdomain | Validators | Registration |
|---|---|---|
| Incidents | 12 validators | `Incidents/DependencyInjection.cs` |
| Mitigation | 7 validators | `Incidents/DependencyInjection.cs` |
| Automation | 10 validators | `Automation/DependencyInjection.cs` |
| Cost | 9 validators | `Cost/DependencyInjection.cs` |
| Runtime | 8 validators | `Runtime/DependencyInjection.cs` |
| Reliability | 7 validators | `Reliability/DependencyInjection.cs` |
| **Total** | **53** | |

**Assessment:** ✅ Full coverage — every command and query has a validator.

### 4.2 Validation Patterns

| Pattern | Status |
|---|---|
| Required field validation | ✅ Present on all commands |
| String length constraints | ✅ Matches configuration (VARCHAR limits) |
| Enum range validation | ✅ On status, severity, type fields |
| Pagination validation | ✅ Page > 0, PageSize in range |
| Date range validation | ✅ Start before End on period queries |
| GUID format validation | ✅ On all ID parameters |

---

## 5. Error Handling Review

### 5.1 Result Pattern

All handlers return `Result<TResponse>` using the shared Result pattern:

- **Success:** `Result.Success(response)` → serialized via `.ToHttpResult(localizer)` or `.ToCreatedResult(path, localizer)`
- **Failure:** `Result.Failure(error)` → error type determines HTTP status code

### 5.2 Error Catalogs (4 domain-specific)

| Catalog | File | Error Count | Error Types |
|---|---|---|---|
| `IncidentErrors` | `Domain/Incidents/Errors/IncidentErrors.cs` | 6 | NotFound (Incident, Service, Team, Workflow, Runbook), Validation (InvalidAction) |
| `AutomationErrors` | `Domain/Automation/Errors/AutomationErrors.cs` | 8 | NotFound (Action, Workflow), Validation (InvalidTransition, PreconditionsNotMet, ApprovalRequired, InvalidAction), Forbidden (Unauthorized), Conflict (AlreadyCompleted) |
| `RuntimeIntelligenceErrors` | `Domain/Runtime/Errors/RuntimeIntelligenceErrors.cs` | 8 | NotFound (Snapshot, Baseline, DriftFinding, Profile), Validation (InvalidValue, InvalidTolerance), Conflict (Duplicate Baseline, AlreadyAcknowledged) |
| `CostIntelligenceErrors` | `Domain/Cost/Errors/CostIntelligenceErrors.cs` | 11 | NotFound (Snapshot, Profile, Attribution, Batch), Validation (InvalidCostShares, InvalidPeriod, NegativeCost, EmptyBatch), Business (BudgetExceeded), Conflict (DuplicateSnapshot, DuplicateBatch) |

**Assessment:** ✅ Comprehensive error catalogs with typed error codes, i18n-ready message templates, and parameter interpolation.

### 5.3 Error Handling Gaps

| # | Gap | Severity |
|---|---|---|
| 1 | No `ReliabilityErrors` catalog — reliability queries may return generic errors | 🟡 Medium |
| 2 | Error codes use module-specific prefixes (`Incidents.`, `CostIntelligence.`, `RuntimeIntelligence.`, `Automation.`) — no unified `OperationalIntelligence.` prefix | 🟢 Low |

---

## 6. Audit of Module Actions

### 6.1 Local Audit

| Subdomain | Audit Mechanism | Scope |
|---|---|---|
| Automation | `AutomationAuditRecord` entity with dedicated table `oi_automation_audit_records` | ✅ Complete — every workflow action logged with actor, action, details, timestamps |
| Mitigation | `MitigationWorkflowActionLog` entity with dedicated table `oi_mitigation_workflow_actions` | ✅ Complete — every workflow state transition logged |
| Incidents | `AuditableEntity` base provides CreatedAt/CreatedBy/UpdatedAt/UpdatedBy | ⚠️ Basic — no explicit action log for incident status changes |
| Runtime | `AuditableEntity` base only | ⚠️ Basic |
| Cost | `AuditableEntity` base only | ⚠️ Basic |

### 6.2 Cross-Module Audit Integration

> **Status:** ❌ MISSING

The Operational Intelligence module does **not** forward audit events to the Audit & Compliance module (`src/modules/auditcompliance/`). Findings:

| Gap | Impact |
|---|---|
| No domain events published to Audit & Compliance | Automation approvals, incident state changes, cost imports are not tracked in the centralized audit trail |
| No audit chain link creation | Actions within this module are not part of the cryptographic audit chain |
| No compliance evidence generation | Automation approval flows cannot be used as compliance evidence |

**Recommendation:** Implement outbox-based event forwarding from Operational Intelligence domain events to the Audit & Compliance module's event consumer.

---

## 7. Permissions Review

### 7.1 Permission Matrix (16 unique permissions)

| # | Permission | Used By | Endpoints |
|---|---|---|---|
| 1 | `operations:incidents:read` | IncidentEndpointModule | #2, #3, #4, #5, #7, #8, #9, #10 |
| 2 | `operations:incidents:write` | IncidentEndpointModule | #1, #6 |
| 3 | `operations:mitigation:read` | MitigationEndpointModule | #11, #12, #15, #16 |
| 4 | `operations:mitigation:write` | MitigationEndpointModule | #13, #14, #17 |
| 5 | `operations:runbooks:read` | RunbookEndpointModule | #18, #19 |
| 6 | `operations:automation:read` | AutomationEndpointModule | #20, #21, #23, #24, #32, #34 |
| 7 | `operations:automation:write` | AutomationEndpointModule | #22, #25, #29, #31, #33 |
| 8 | `operations:automation:approve` | AutomationEndpointModule | #26, #27 |
| 9 | `operations:automation:execute` | AutomationEndpointModule | #28, #30 |
| 10 | `operations:reliability:read` | ReliabilityEndpointModule | #35–#41 |
| 11 | `operations:runtime:read` | RuntimeIntelligenceEndpointModule | #43, #44, #47, #48, #49 |
| 12 | `operations:runtime:write` | RuntimeIntelligenceEndpointModule | #42, #45, #46 |
| 13 | `operations:cost:read` | CostIntelligenceEndpointModule | #51–#54 |
| 14 | `operations:cost:write` | CostIntelligenceEndpointModule | #50, #55–#58 |
| 15 | `platform:admin:read` | PlatformOperationsPage (frontend) | Platform operations route |
| 16 | `operations:runbooks:read` (route) | RunbooksPage uses `operations:incidents:read` in route | ⚠️ Mismatch — see §7.2 |

### 7.2 Permission Gaps

| # | Gap | Details | Severity |
|---|---|---|---|
| 1 | **Runbooks route permission mismatch** | `RunbooksPage` route in `App.tsx` requires `operations:incidents:read`, but `RunbookEndpointModule` requires `operations:runbooks:read` | 🟠 Medium |
| 2 | **No `operations:runbooks:write`** | Runbooks are read-only; no write permission defined for future CRUD | 🟡 Low |
| 3 | **No `operations:reliability:write`** | ReliabilitySnapshot creation has no endpoint; when added, will need write permission | 🟡 Low |
| 4 | **No `operations:cost:admin`** | Budget configuration (ServiceCostProfile) may need separate admin permission | 🟡 Low |
| 5 | **Permissions are string literals** | Not centralized in constants/enum; risk of typos across endpoint modules and frontend | 🟠 Medium |

---

## 8. Critical Flow Review

### 8.1 Signal Ingestion Flow

```
External Source → POST /runtime/snapshots → IngestRuntimeSnapshot.Command
    → Validator (required fields, numeric ranges)
    → Handler: RuntimeSnapshot.Create() → ClassifyHealth()
    → Persist to oi_runtime_snapshots
    → Domain event → Outbox (oi_rt_outbox_messages)
```

**Assessment:** ✅ Functional. Validation covers required fields and ranges. Health classification is automatic via domain logic.

**Gaps:**
- ❌ No idempotency control (duplicate snapshots with same service/environment/timestamp not prevented at API level)
- ❌ No rate limiting per service (single rate limit group `operations` for all endpoints)

### 8.2 Scoring Flow (Reliability)

```
ReliabilitySnapshot.Create(runtimeHealth, incidentImpact, observability) →
    OverallScore = Runtime × 0.50 + Incident × 0.30 + Observability × 0.20
    → Persist to oi_reliability_snapshots
```

**Assessment:** ✅ Deterministic weighted scoring in domain logic. Formula is explicit and testable.

**Gaps:**
- ⚠️ No endpoint to trigger reliability recomputation (computed externally or by background service)
- ⚠️ Weights are hardcoded in domain logic — not configurable per tenant

### 8.3 Rule Evaluation (Drift Detection)

```
POST /runtime/drift/detect → DetectRuntimeDrift.Command
    → Fetch current RuntimeSnapshot for service/environment
    → Fetch RuntimeBaseline for service/environment
    → Compare each metric: deviation = |actual - expected| / expected × 100
    → If deviation > tolerance → Create DriftFinding (severity based on deviation %)
    → Persist findings
```

**Assessment:** ✅ Complete comparison logic with configurable tolerance.

**Gaps:**
- ⚠️ No automatic drift detection (must be triggered via API call)
- ⚠️ No scheduled job for periodic drift detection

### 8.4 Automation State Machine

```
CreateAutomationWorkflow → Draft
    → RequestApproval → PendingApproval (audit: WorkflowCreated + ApprovalRequested)
    → Approve → Approved (audit: Approved, requires operations:automation:approve)
    → Execute → Executing (audit: ExecutionStarted, requires operations:automation:execute)
    → CompleteStep → (step completed, audit: StepCompleted)
    → (all steps done) → Completed (audit: Completed)

Alternative paths:
    → Reject → Rejected (audit: Rejected)
    → Cancel → Cancelled (audit: Cancelled)
```

**Assessment:** ✅ Well-structured state machine with audit trail at every transition. Permission separation between read/write/approve/execute is correct.

**Gaps:**
- ⚠️ No timeout handling for workflows stuck in PendingApproval or Executing
- ⚠️ No automatic rollback on failed execution

### 8.5 State Query Flow

All GET endpoints follow consistent patterns:
1. Validate query parameters (FluentValidation)
2. Execute query against respective DbContext
3. Map domain entities to response DTOs
4. Return `Result<TResponse>`

**Assessment:** ✅ Consistent and functional.

---

## 9. Request/Response Review

### 9.1 Request Patterns

| Pattern | Implementation | Status |
|---|---|---|
| Commands with body | All POST/PATCH commands accept typed request bodies | ✅ |
| Queries with filters | All GET queries accept query string parameters | ✅ |
| Pagination | `page` + `pageSize` parameters on list endpoints | ✅ |
| Date range filtering | `periodStart/periodEnd`, `windowStart/windowEnd`, `beforeStart/afterEnd` | ✅ |

### 9.2 Response Patterns

| Pattern | Implementation | Status |
|---|---|---|
| Typed DTOs | All responses are typed DTO records | ✅ |
| Pagination metadata | List responses include page/pageSize/total | ✅ |
| Error responses | `Result<T>` → HTTP status codes with error body | ✅ |
| i18n error messages | Error templates support parameter interpolation | ✅ |

---

## 10. Backend Correction Backlog

| # | Correction | Severity | Effort | Category |
|---|---|---|---|---|
| 1 | Add `RowVersion` to mutable entities (IncidentRecord, MitigationWorkflowRecord, AutomationWorkflowRecord, DriftFinding, ServiceCostProfile) | 🔴 P1 | 2–3h | Data Integrity |
| 2 | Implement cross-module audit forwarding to Audit & Compliance module via outbox events | 🔴 P1 | 1–2 days | Compliance |
| 3 | Fix Runbooks route permission mismatch (App.tsx: `operations:incidents:read` → should be `operations:runbooks:read`) | 🟠 P2 | 30min | Security |
| 4 | Add idempotency control on signal ingestion endpoints (`POST /runtime/snapshots`, `POST /cost/snapshots`) | 🟠 P2 | 2–3h | Reliability |
| 5 | Add Runbook CRUD endpoints (POST, PUT, DELETE) with `operations:runbooks:write` permission | 🟠 P2 | 1 day | Completeness |
| 6 | Add Automation Action CRUD endpoints (POST, PUT, DELETE) for action catalog management | 🟠 P2 | 1 day | Completeness |
| 7 | Add ServiceCostProfile management endpoint (budget, threshold configuration) | 🟠 P2 | 4h | Completeness |
| 8 | Add RuntimeBaseline management endpoint (manual baseline adjustment) | 🟠 P2 | 4h | Completeness |
| 9 | Add explicit Incident status update endpoint (`PATCH /incidents/{id}/status`) | 🟠 P2 | 4h | Completeness |
| 10 | Add retention configuration and cleanup endpoints/background job | 🟠 P2 | 1 day | Operations |
| 11 | Centralize permission strings into constants/enum class | 🟡 P3 | 2h | Maintainability |
| 12 | Add `ReliabilityErrors` catalog for reliability query failures | 🟡 P3 | 1h | Error Handling |
| 13 | Add scheduled job for periodic drift detection (instead of API-triggered only) | 🟡 P3 | 4h | Automation |
| 14 | Add workflow timeout handling for stuck PendingApproval/Executing states | 🟡 P3 | 4h | Reliability |
| 15 | Add per-service rate limiting on ingestion endpoints | 🟡 P3 | 2h | Performance |
| 16 | Unify Automation enum storage from STRING to INTEGER (matching other subdomains) | 🟡 P3 | 2h | Consistency |
