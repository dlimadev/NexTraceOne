# Operational Intelligence Module — Scope Finalization

> **Status:** DRAFT  
> **Date:** 2026-03-24  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation

---

## 1. Functional Area Assessment (12 Areas)

| # | Functional Area | Status | Mandatory | Notes |
|---|---|---|---|---|
| 1 | Signal ingestion | ✅ Implemented | YES | `IngestRuntimeSnapshot`, `IngestCostSnapshot`, `IRuntimeSignalIngestionPort` |
| 2 | Operational scoring | ✅ Implemented | YES | `ReliabilitySnapshot.Create()` with weighted formula, `ObservabilityProfile.Assess()` |
| 3 | Thresholds & rules | ✅ Implemented | YES | `RuntimeSnapshot.ClassifyHealth()`, `DriftFinding.Detect()`, `CostSnapshot.IsAnomaly()` |
| 4 | Incidents & alerts | ✅ Implemented | YES | Full lifecycle via `IncidentRecord`, mitigation workflows, validation |
| 5 | Controlled automations | ✅ Implemented | YES | `AutomationWorkflowRecord` with approval gates, preconditions, audit |
| 6 | Runbooks | ✅ Implemented | YES | `RunbookRecord` with steps, prerequisites, linked to mitigation |
| 7 | Operational state visualisation | ✅ Implemented | YES | 10 frontend pages: incidents, reliability, runtime comparison, automation |
| 8 | Change/release linkage | ⚠️ Partial | YES | `CorrelatedChangesJson`, `IRuntimeCorrelationPort`, `GetCostByRelease` — but correlation not validated end-to-end |
| 9 | Environment linkage | ✅ Implemented | YES | `Environment` field on all snapshots and incidents, `EnvironmentId` on tenant context |
| 10 | Notification linkage | ❌ Missing | YES | Domain events exist but no Notifications module subscription |
| 11 | Operational traceability | ⚠️ Partial | YES | `AutomationAuditRecord` is local; no cross-module audit forwarding |
| 12 | Analytics data (ClickHouse) | ❌ Missing | OPTIONAL | ClickHouse recommended but not yet integrated |

---

## 2. Existing Functionality — Fully Implemented

### 2.1 Incidents Subdomain (35+ CQRS features)

| Capability | Backend Features | Frontend | Status |
|---|---|---|---|
| Incident creation | `CreateIncident.Command` | IncidentsPage | ✅ Complete |
| Incident listing with filters | `ListIncidents.Query` (team, service, environment, severity, status, type, search, date range) | IncidentsPage | ✅ Complete |
| Incident detail | `GetIncidentDetail.Query` | IncidentDetailPage | ✅ Complete |
| Incident summary | `GetIncidentSummary.Query` | IncidentsPage (header) | ✅ Complete |
| Correlation analysis | `GetIncidentCorrelation.Query`, `RefreshIncidentCorrelation.Command` | IncidentDetailPage | ✅ Complete |
| Evidence gathering | `GetIncidentEvidence.Query` | IncidentDetailPage | ✅ Complete |
| Mitigation view | `GetIncidentMitigation.Query` | IncidentDetailPage | ✅ Complete |
| Mitigation recommendations | `GetMitigationRecommendations.Query` | IncidentDetailPage | ✅ Complete |
| Mitigation workflows | `CreateMitigationWorkflow.Command`, `UpdateMitigationWorkflowAction.Command` | IncidentDetailPage | ✅ Complete |
| Mitigation validation | `RecordMitigationValidation.Command`, `GetMitigationValidation.Query` | IncidentDetailPage | ✅ Complete |
| Mitigation history | `GetMitigationHistory.Query` | IncidentDetailPage | ✅ Complete |
| Incidents by service | `ListIncidentsByService.Query` | (via API) | ✅ Complete |
| Incidents by team | `ListIncidentsByTeam.Query` | (via API) | ✅ Complete |
| Runbook listing | `ListRunbooks.Query` | RunbooksPage | ✅ Complete |
| Runbook detail | `GetRunbookDetail.Query` | RunbooksPage | ✅ Complete |

### 2.2 Automation Subdomain (10+ CQRS features)

| Capability | Backend Features | Frontend | Status |
|---|---|---|---|
| Action catalog | `ListAutomationActions.Query`, `GetAutomationAction.Query` | AutomationAdminPage | ✅ Complete |
| Workflow creation | `CreateAutomationWorkflow.Command` | AutomationWorkflowsPage | ✅ Complete |
| Workflow listing | `ListAutomationWorkflows.Query` | AutomationWorkflowsPage | ✅ Complete |
| Workflow detail | `GetAutomationWorkflow.Query` | AutomationWorkflowDetailPage | ✅ Complete |
| Approval flow | `RequestApproval`, `Approve`, `Reject` (via `UpdateAutomationWorkflowAction`) | AutomationWorkflowDetailPage | ✅ Complete |
| Execution | `Execute`, `CompleteStep` (via `UpdateAutomationWorkflowAction`) | AutomationWorkflowDetailPage | ✅ Complete |
| Precondition evaluation | `EvaluatePreconditions.Command` | AutomationWorkflowDetailPage | ✅ Complete |
| Validation recording | `RecordAutomationValidation.Command`, `GetAutomationValidation.Query` | AutomationWorkflowDetailPage | ✅ Complete |
| Audit trail | `GetAutomationAuditTrail.Query` | AutomationWorkflowDetailPage | ✅ Complete |

### 2.3 Reliability Subdomain (7 CQRS features)

| Capability | Backend Features | Frontend | Status |
|---|---|---|---|
| Service reliability list | `ListServiceReliability.Query` (filters: team, service, domain, environment, status, criticality) | TeamReliabilityPage | ✅ Complete |
| Service reliability detail | `GetServiceReliabilityDetail.Query` | ServiceReliabilityDetailPage | ✅ Complete |
| Service reliability trend | `GetServiceReliabilityTrend.Query` | ServiceReliabilityDetailPage | ✅ Complete |
| Service reliability coverage | `GetServiceReliabilityCoverage.Query` | ServiceReliabilityDetailPage | ✅ Complete |
| Team reliability summary | `GetTeamReliabilitySummary.Query` | TeamReliabilityPage | ✅ Complete |
| Team reliability trend | `GetTeamReliabilityTrend.Query` | TeamReliabilityPage | ✅ Complete |
| Domain reliability summary | `GetDomainReliabilitySummary.Query` | (via API) | ✅ Complete |

### 2.4 Runtime Subdomain (8 CQRS features)

| Capability | Backend Features | Frontend | Status |
|---|---|---|---|
| Metrics ingestion | `IngestRuntimeSnapshot.Command` | (via API/agents) | ✅ Complete |
| Health classification | `GetRuntimeHealth.Query` | EnvironmentComparisonPage | ✅ Complete |
| Observability scoring | `GetObservabilityScore.Query` | (via API) | ✅ Complete |
| Observability debt assessment | `ComputeObservabilityDebt.Command` | (via API) | ✅ Complete |
| Drift detection | `DetectRuntimeDrift.Command` | EnvironmentComparisonPage | ✅ Complete |
| Drift findings listing | `GetDriftFindings.Query` | EnvironmentComparisonPage | ✅ Complete |
| Release health timeline | `GetReleaseHealthTimeline.Query` | EnvironmentComparisonPage | ✅ Complete |
| Release runtime comparison | `CompareReleaseRuntime.Query` | EnvironmentComparisonPage | ✅ Complete |

### 2.5 Cost Subdomain (9 CQRS features)

| Capability | Backend Features | Frontend | Status |
|---|---|---|---|
| Cost ingestion | `IngestCostSnapshot.Command` | (via API/import) | ✅ Complete |
| Cost report | `GetCostReport.Query` | (via API) | ✅ Complete |
| Cost by release | `GetCostByRelease.Query` | (via API) | ✅ Complete |
| Cost by route | `GetCostByRoute.Query` | (via API) | ✅ Complete |
| Cost delta | `GetCostDelta.Query` | (via API) | ✅ Complete |
| Cost attribution | `AttributeCostToService.Command` | (via API) | ✅ Complete |
| Cost trend computation | `ComputeCostTrend.Command` | (via API) | ✅ Complete |
| Batch import | `ImportCostBatch.Command` | (via API) | ✅ Complete |
| Anomaly detection | `AlertCostAnomaly.Command` | (via API) | ✅ Complete |

---

## 3. Partially Implemented Functionality

| Feature | Frontend | Backend | Gap |
|---|---|---|---|
| Change/release correlation | ⚠️ `CorrelatedChangesJson` displayed in IncidentDetailPage | ⚠️ `IRuntimeCorrelationPort` defined, `CorrelateWithReleaseAsync` declared | **No validated end-to-end flow** — Change Governance does not yet publish events consumed by Operational Intelligence; correlation data may be seeded/mocked |
| Operational traceability | ⚠️ `AutomationAuditRecord` displayed in AutomationWorkflowDetailPage | ⚠️ Local audit via `GetAutomationAuditTrail.Query` | **Local only** — audit records not forwarded to Audit & Compliance (10); incident timeline not exported as audit evidence |
| FinOps configuration | ⚠️ `OperationsFinOpsConfigurationPage.tsx` exists (615 lines) | ⚠️ Uses Configuration module hooks, not OpIntel endpoints | **Embryonic** — page exists under `features/operational-intelligence/` (separate from `features/operations/`), configuration definitions may not be complete |
| Cost frontend pages | ❌ No dedicated Cost dashboard page | ✅ 9 Cost CQRS features fully implemented | **Backend ahead of frontend** — all cost endpoints work but no cost-specific UI page exists |
| Telemetry real-time ingestion | ⚠️ `IRuntimeSignalIngestionPort` defined | ⚠️ Port interface only, no real ingestion adapter | **Port contract only** — no Prometheus, Datadog, or CloudWatch adapter implemented; dashboard data may rely on seed data |

---

## 4. Missing but Mandatory Functionality

| Feature | Priority | Rationale |
|---|---|---|
| Notifications integration | HIGH | Critical incidents and cost anomalies must trigger notifications; `RuntimeAnomalyDetectedEvent` and `CostAnomalyDetectedEvent` are published but no subscriber exists |
| Audit & Compliance integration | HIGH | Automation decisions, incident lifecycle transitions, and mitigation validations must be forwarded as audit evidence |
| Cost dashboard frontend page | HIGH | 9 backend endpoints have no dedicated UI — cost data is invisible to users |
| End-to-end change correlation validation | HIGH | `IRuntimeCorrelationPort` is a contract without validated implementation; Change Governance events not consumed |
| Real telemetry ingestion adapter | MEDIUM | `IRuntimeSignalIngestionPort` needs at least one adapter (Prometheus/OpenTelemetry) for production readiness |
| ClickHouse integration for analytics | MEDIUM | Runtime metrics and cost time-series data will outgrow PostgreSQL at scale |
| Module README | LOW | Largest module (211 C# files) has no onboarding documentation |

---

## 5. Missing but Optional Functionality

| Feature | Priority | Rationale |
|---|---|---|
| SLA compliance tracking | LOW | Composable from existing `ReliabilitySnapshot` and `RuntimeSnapshot` data but no dedicated SLA entity exists |
| Incident auto-detection from runtime anomalies | LOW | `RuntimeAnomalyDetectedEvent` is published but does not auto-create incidents |
| Cost forecasting | LOW | `CostTrend` provides direction but no predictive model exists |
| Runbook execution tracking | LOW | Runbooks are reference documents; no step-by-step execution state tracking |
| Multi-environment cost comparison | LOW | `CostSnapshot` has `Environment` field but no cross-environment comparison endpoint |
| Observability profile auto-discovery | LOW | `ObservabilityProfile` is manually assessed; no integration with monitoring tools for auto-population |

---

## 6. Subdomain Scope Assessment

### 6.1 Incidents Subdomain

| Capability | Status | Notes |
|---|---|---|
| Incident CRUD | ✅ Complete | Full lifecycle with 6 statuses |
| Mitigation workflows | ✅ Complete | Multi-step with approval, 9 workflow statuses, 10 action types |
| Mitigation validation | ✅ Complete | Post-mitigation checks with Pass/Fail/Skip outcomes |
| Runbooks | ✅ Complete | Linked to services and incident types, JSON steps |
| Correlation with changes | ⚠️ Partial | Data structure exists, end-to-end flow not validated |
| Notification on critical incidents | ❌ Missing | No Notifications integration |

### 6.2 Automation Subdomain

| Capability | Status | Notes |
|---|---|---|
| Action catalog | ✅ Complete | Browsable catalog of automation actions |
| Workflow lifecycle | ✅ Complete | 11 statuses from Draft to Cancelled/Rejected |
| Approval gates | ✅ Complete | `AutomationApprovalStatus`, `Approve()`, `Reject()` methods |
| Precondition evaluation | ✅ Complete | Dedicated `EvaluatePreconditions` command |
| Execution steps | ✅ Complete | Step-by-step execution with `CompleteStep` |
| Post-execution validation | ✅ Complete | `AutomationValidationRecord` with 6 outcomes |
| Local audit trail | ✅ Complete | `AutomationAuditRecord` with `AutomationAuditAction` enum |
| Cross-module audit forwarding | ❌ Missing | Records not published to Audit & Compliance |

### 6.3 Reliability Subdomain

| Capability | Status | Notes |
|---|---|---|
| Reliability scoring | ✅ Complete | Composite formula: `0.50×Runtime + 0.30×Incident + 0.20×Observability` |
| Service reliability views | ✅ Complete | List, detail, trend, coverage |
| Team reliability summary | ✅ Complete | Aggregated team-level view |
| Domain reliability summary | ✅ Complete | Aggregated domain-level view |
| SLA compliance tracking | ❌ Missing | No dedicated SLA entity or threshold |

### 6.4 Runtime Subdomain

| Capability | Status | Notes |
|---|---|---|
| Metrics ingestion | ✅ Complete | `IngestRuntimeSnapshot` with auto health classification |
| Health classification | ✅ Complete | `ErrorRate≥10%` or `P99≥3000ms` → Unhealthy; `≥5%`/`≥1000ms` → Degraded |
| Baseline management | ✅ Complete | `RuntimeBaseline.Establish()`, `IsWithinTolerance()`, `Refresh()` |
| Drift detection | ✅ Complete | Auto-severity: 50%→Critical, 25%→High, 10%→Medium |
| Observability profiling | ✅ Complete | 5 capabilities weighted, 0.60+ = adequate |
| Release health timeline | ✅ Complete | Time-windowed health views |
| Real telemetry adapter | ❌ Missing | `IRuntimeSignalIngestionPort` defined, no adapter implemented |

### 6.5 Cost Subdomain

| Capability | Status | Notes |
|---|---|---|
| Cost ingestion | ✅ Complete | `IngestCostSnapshot` with share validation |
| Cost attribution | ✅ Complete | Per-API, cost-per-request auto-calculation |
| Trend analysis | ✅ Complete | Auto-classification: >±5% = significant |
| Budget management | ✅ Complete | `ServiceCostProfile` with alerts at threshold |
| Batch import | ✅ Complete | `CostImportBatch` with Pending/Completed/Failed states |
| Anomaly detection | ✅ Complete | Threshold-based deviation percentage |
| Cost frontend page | ❌ Missing | No dedicated UI page for cost data |
| FinOps configuration | ⚠️ Embryonic | `OperationsFinOpsConfigurationPage.tsx` exists but uses generic Configuration hooks |

---

## 7. Scope Boundaries

### What Operational Intelligence Owns (Source of Truth for)

1. **Operational state** — Latest health, reliability, and cost posture of each service/environment
2. **Operational incidents** — Full lifecycle from detection to closure with evidence and mitigation
3. **Controlled automations** — Approved, audited, validated automation executions
4. **Operational runbooks** — Documented response procedures linked to services and incident types
5. **Reliability scoring** — Composite health assessment per service, team, and domain
6. **Runtime health** — Metrics-based health classification with baseline and drift detection
7. **Operational cost** — Service-level cost tracking, attribution, trends, and anomaly detection
8. **Observability maturity** — Per-service assessment of tracing, metrics, logging, alerting, dashboards

### What Operational Intelligence Does NOT Own

| Excluded Capability | Correct Module |
|---|---|
| Service and API definitions (assets, topology, ownership) | Service Catalog (03) |
| Contract lifecycle and versioning | Contracts (04) |
| Change tracking, deployment validation, blast radius | Change Governance (05) |
| Notification routing and delivery | Notifications (11) |
| Compliance evidence storage and regulatory reporting | Audit & Compliance (10) |
| User authentication and team management | Identity & Access (01) |
| Environment definitions and promotion rules | Environment Management (02) |
| AI model governance and token budgets | AI & Knowledge (07) |
| Executive compliance dashboards | Governance (08) |
| Raw billing data integration | Integrations (12) |

---

## 8. Minimum Complete Module Definition

### Must Have (blocks module closure)

1. ✅ All 10 operational pages routed and accessible
2. ✅ All 5 DbContexts operational with RLS, audit, soft-delete
3. ✅ Incident full lifecycle (create → investigate → mitigate → validate → close)
4. ✅ Automation full lifecycle (create → approve → execute → validate → audit)
5. ✅ Reliability scoring with trend and coverage views
6. ✅ Runtime health classification, drift detection, baseline management
7. ✅ Cost ingestion, attribution, trends, budgets, anomaly detection
8. ⬜ Notifications integration — domain events consumed by Notifications module
9. ⬜ Audit & Compliance integration — operational events forwarded as audit evidence
10. ⬜ Cost dashboard frontend page — dedicated UI for cost data
11. ⬜ End-to-end change correlation validation with Change Governance

### Should Have (improves quality)

12. ⬜ At least one real telemetry adapter for `IRuntimeSignalIngestionPort`
13. ⬜ ClickHouse integration for runtime metrics and cost analytics
14. ⬜ Merge embryonic `operational-intelligence/` frontend into `operations/`
15. ⬜ Module README with onboarding documentation
16. ⬜ `RowVersion`/xmin on aggregate roots for optimistic concurrency

### Nice to Have (polish)

17. ⬜ SLA compliance tracking entity and views
18. ⬜ Incident auto-creation from runtime anomaly events
19. ⬜ Cost forecasting capability
20. ⬜ Observability profile auto-discovery from monitoring tools
21. ⬜ i18n verification for all locales across all 11 pages
