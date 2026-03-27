# Change Governance — Backend Functional Corrections

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Endpoint Inventory (46+ Endpoints)

### 1.1 ChangeIntelligence Endpoints (25)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 1 | GET | `/api/v1/releases/{releaseId}` | GetRelease.Query | `change-intelligence:read` | ✅ Working |
| 2 | GET | `/api/v1/releases/` | ListReleases.Query | `change-intelligence:read` | ✅ Working |
| 3 | GET | `/api/v1/releases/{apiAssetId}/history` | GetReleaseHistory.Query | `change-intelligence:read` | ✅ Working |
| 4 | POST | `/api/v1/releases/{releaseId}/markers` | RegisterExternalMarker.Command | `change-intelligence:write` | ✅ Working |
| 5 | GET | `/api/v1/releases/{releaseId}/intelligence` | GetChangeIntelligenceSummary.Query | `change-intelligence:read` | ✅ Working |
| 6 | POST | `/api/v1/releases/{releaseId}/baseline` | RecordReleaseBaseline.Command | `change-intelligence:write` | ✅ Working |
| 7 | POST | `/api/v1/releases/{releaseId}/review/start` | StartPostReleaseReview.Command | `change-intelligence:write` | ✅ Working |
| 8 | POST | `/api/v1/releases/{releaseId}/review/progress` | ProgressPostReleaseReview.Command | `change-intelligence:write` | ✅ Working |
| 9 | POST | `/api/v1/releases/{releaseId}/rollback-assessment` | AssessRollbackViability.Query | `change-intelligence:read` | ✅ Working |
| 10 | POST | `/api/v1/releases/{releaseId}/rollback` | RegisterRollback.Command | `change-intelligence:write` | ✅ Working |
| 11 | GET | `/api/v1/changes/` | ListChanges.Query | `change-intelligence:read` | ✅ Working |
| 12 | GET | `/api/v1/changes/summary` | GetChangesSummary.Query | `change-intelligence:read` | ✅ Working |
| 13 | GET | `/api/v1/changes/{changeId}/detail` | GetChangeIntelligenceSummary.Query | `change-intelligence:read` | ✅ Working |
| 14 | GET | `/api/v1/changes/{changeId}/advisory` | GetChangeAdvisory.Query | `change-intelligence:read` | ✅ Working |
| 15 | GET | `/api/v1/changes/{changeId}/decisions` | GetChangeDecisionHistory.Query | `change-intelligence:read` | ✅ Working |
| 16 | POST | `/api/v1/changes/{changeId}/record-decision` | RecordChangeDecision.Command | `change-intelligence:write` | ✅ Working |
| 17 | POST | `/api/v1/analysis/classify` | ClassifyChangeLevel.Command | `change-intelligence:write` | ✅ Working |
| 18 | POST | `/api/v1/analysis/score` | ComputeChangeScore.Command | `change-intelligence:write` | ✅ Working |
| 19 | POST | `/api/v1/analysis/blast-radius/{releaseId}` | CalculateBlastRadius.Command | `change-intelligence:write` | ✅ Working |
| 20 | GET | `/api/v1/analysis/blast-radius/{releaseId}` | GetBlastRadiusReport.Query | `change-intelligence:read` | ✅ Working |
| 21 | POST | `/api/v1/deployments/notify` | NotifyDeployment.Command | `change-intelligence:write` | ✅ Working |
| 22 | POST | `/api/v1/deployments/{releaseId}/state` | UpdateDeploymentState.Command | `change-intelligence:write` | ✅ Working |
| 23 | GET | `/api/v1/freeze-windows/check` | CheckFreezeConflict.Query | `change-intelligence:read` | ✅ Working |
| 24 | POST | `/api/v1/freeze-windows` | CreateFreezeWindow.Command | `change-intelligence:write` | ✅ Working |
| 25 | GET | `/api/v1/freeze-windows/active` | ListActiveFreezeWindows.Query | `change-intelligence:read` | ✅ Working |

### 1.2 Workflow Endpoints (13)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 26 | POST | `/api/v1/workflow/templates` | CreateWorkflowTemplate.Command | `workflow:templates:write` | ✅ Working |
| 27 | GET | `/api/v1/workflow/templates` | ListWorkflowTemplates.Query | ⚠️ `workflow:templates:write` | 🐛 Bug: requires `:write` not `:read` |
| 28 | GET | `/api/v1/workflow/templates/{id}` | GetWorkflowTemplate.Query | `workflow:templates:read` | ✅ Working |
| 29 | POST | `/api/v1/workflow/instances` | InitiateWorkflow.Command | `workflow:write` | ✅ Working |
| 30 | GET | `/api/v1/workflow/instances/{id}` | GetWorkflowInstance.Query | `workflow:read` | ✅ Working |
| 31 | POST | `/api/v1/workflow/instances/{id}/approve` | ApproveStage.Command | `workflow:write` | ✅ Working |
| 32 | POST | `/api/v1/workflow/instances/{id}/reject` | RejectWorkflow.Command | `workflow:write` | ✅ Working |
| 33 | POST | `/api/v1/workflow/instances/{id}/request-changes` | RequestChanges.Command | `workflow:write` | ✅ Working |
| 34 | GET | `/api/v1/workflow/approvals/pending` | ListPendingApprovals.Query | `workflow:read` | ✅ Working |
| 35 | GET | `/api/v1/workflow/{instanceId}/status` | GetWorkflowStatus.Query | `workflow:read` | ✅ Working |
| 36 | POST | `/api/v1/workflow/{instanceId}/evidence` | GenerateEvidencePack.Command | `workflow:write` | ✅ Working |
| 37 | GET | `/api/v1/workflow/{instanceId}/evidence` | GetEvidencePack.Query | `workflow:read` | ✅ Working |
| 38 | POST | `/api/v1/workflow/{instanceId}/evidence/export-pdf` | ExportEvidencePackPdf.Command | `workflow:write` | ✅ Working |

### 1.3 Promotion Endpoints (9)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 39 | GET | `/api/v1/promotion/environments` | ListDeploymentEnvironments.Query | `promotion:read` | ✅ Working |
| 40 | POST | `/api/v1/promotion/requests` | CreatePromotionRequest.Command | `promotion:write` | ✅ Working |
| 41 | GET | `/api/v1/promotion/requests` | ListPromotionRequests.Query | `promotion:read` | ✅ Working |
| 42 | GET | `/api/v1/promotion/requests/{id}` | GetPromotionStatus.Query | `promotion:read` | ✅ Working |
| 43 | POST | `/api/v1/promotion/requests/{id}/evaluate-gates` | EvaluatePromotionGates.Command | `promotion:write` | ✅ Working |
| 44 | GET | `/api/v1/promotion/requests/{id}/gates` | GetGateEvaluation.Query | `promotion:read` | ✅ Working |
| 45 | POST | `/api/v1/promotion/requests/{id}/approve` | ApprovePromotion.Command | `promotion:write` | ✅ Working |
| 46 | POST | `/api/v1/promotion/requests/{id}/block` | BlockPromotion.Command | `promotion:write` | ✅ Working |
| 47 | POST | `/api/v1/promotion/gates/{id}/override` | OverrideGateWithJustification.Command | `promotion:admin:write` | ✅ Working |

### 1.4 RulesetGovernance Endpoints (7)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 48 | POST | `/api/v1/rulesets/upload` | UploadRuleset.Command | `rulesets:write` | ✅ Working |
| 49 | GET | `/api/v1/rulesets` | ListRulesets.Query | `rulesets:read` | ✅ Working |
| 50 | POST | `/api/v1/rulesets/{id}/bind` | BindRulesetToAssetType.Command | `rulesets:write` | ✅ Working |
| 51 | POST | `/api/v1/rulesets/{id}/execute` | ExecuteLintForRelease.Command | `rulesets:write` | ✅ Working |
| 52 | GET | `/api/v1/rulesets/{id}/findings` | GetRulesetFindings.Query | `rulesets:read` | ✅ Working |
| 53 | GET | `/api/v1/rulesets/{id}/score` | GetRulesetScore.Query | `rulesets:read` | ✅ Working |
| 54 | POST | `/api/v1/rulesets/{id}/archive` | ArchiveRuleset.Command | `rulesets:write` | ✅ Working |

---

## 2. Identified Bugs

| ID | Description | File | Severity | Fix |
|----|-------------|------|----------|-----|
| B-01 | `GET /api/v1/workflow/templates` requires `workflow:templates:write` instead of `workflow:templates:read` | `Workflow/Endpoints/TemplateEndpoints.cs` | 🔴 High | Change permission to `:read` |
| B-02 | `POST /api/v1/releases/{releaseId}/rollback-assessment` uses POST for a query (should be GET) | `ChangeIntelligence/Endpoints/IntelligenceEndpoints.cs` | 🟡 Medium | Change to GET (breaking API change) or document as-is |

---

## 3. Validation Gaps

| ID | Description | Handler | Priority |
|----|-------------|---------|----------|
| V-01 | `NotifyDeployment` does not validate `ApiAssetId` against Catalog | `ChangeIntelligence/Features/NotifyDeployment/` | P1 |
| V-02 | `ComputeChangeScore` does not validate score range (0.0–1.0) at domain level | `ChangeIntelligence/Features/ComputeChangeScore/` | P2 |
| V-03 | `CreateFreezeWindow` does not validate `StartTime < EndTime` | `ChangeIntelligence/Features/CreateFreezeWindow/` | P1 |
| V-04 | `InitiateWorkflow` does not check if `ReleaseId` exists | `Workflow/Features/InitiateWorkflow/` | P1 |
| V-05 | `CreatePromotionRequest` does not validate `SourceEnvironmentId != TargetEnvironmentId` | `Promotion/Features/CreatePromotionRequest/` | P1 |
| V-06 | `OverrideGateWithJustification` does not enforce minimum justification length | `Promotion/Features/OverrideGateWithJustification/` | P2 |

---

## 4. Error Handling Gaps

| ID | Description | Priority |
|----|-------------|----------|
| E-01 | No standardised error response format across endpoints | P2 |
| E-02 | Domain errors not mapped to HTTP status codes consistently | P2 |
| E-03 | No `404 Not Found` for missing releases in intelligence endpoints | P1 |

---

## 5. Backend Correction Backlog

| ID | Item | Area | Priority | Effort |
|----|------|------|----------|--------|
| BC-01 | Fix workflow templates GET permission bug | Workflow Endpoints | P0 | 1h |
| BC-02 | Add ApiAssetId validation against Catalog on release creation | ChangeIntelligence | P1 | 4h |
| BC-03 | Add FreezeWindow StartTime < EndTime validation | ChangeIntelligence | P1 | 1h |
| BC-04 | Add ReleaseId existence check in InitiateWorkflow | Workflow | P1 | 2h |
| BC-05 | Add SourceEnv != TargetEnv validation in promotion | Promotion | P1 | 1h |
| BC-06 | Add RowVersion/ConcurrencyToken to all aggregates | All DbContexts | P1 | 8h |
| BC-07 | Add FK constraints within each DbContext | All DbContexts | P1 | 4h |
| BC-08 | Add CHECK constraints (score ranges, date validations) | All DbContexts | P2 | 4h |
| BC-09 | Define explicit domain events for outbox integration | Domain | P1 | 8h |
| BC-10 | Add missing TenantId indexes | All DbContexts | P2 | 2h |
| BC-11 | Standardise error response format | API Layer | P2 | 4h |
| BC-12 | Add 404 handling for missing entities | API Layer | P1 | 2h |
| BC-13 | Create module README.md | Documentation | P2 | 2h |

**Total estimated effort:** ~43 hours
