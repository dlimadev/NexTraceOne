# Change Governance — End-to-End Flow Validation

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Core Flow: Release → Score → Blast Radius → Review → Decision → Promotion

### Step 1 — Release Creation / Deployment Notification

| Aspect | Status | Details |
|--------|--------|---------|
| **Endpoint** | ✅ Working | `POST /api/v1/deployments/notify` → `NotifyDeployment.Command` |
| **Trigger** | ✅ Real | CI/CD pipeline calls the endpoint with release metadata |
| **Entity** | ✅ Real | Creates `Release` aggregate root with `ApiAssetId`, `ServiceName`, `Version`, `Environment`, `PipelineSource`, `CommitSha` |
| **State** | ✅ Real | Initial status: `Pending`, transitions to `Running` |
| **Handler** | ✅ `NotifyDeployment.cs` in `ChangeIntelligence/Features/NotifyDeployment/` |
| **Validation** | ⚠️ Partial | Basic FluentValidation present; no validation against Catalog to verify `ApiAssetId` exists |

**Verdict:** ✅ Functional — release creation is real and working.

### Step 2 — Change Level Classification (Enrichment)

| Aspect | Status | Details |
|--------|--------|---------|
| **Endpoint** | ✅ Working | `POST /api/v1/analysis/classify` → `ClassifyChangeLevel.Command` |
| **Logic** | ✅ Real | Classifies change as Operational(0) → NonBreaking(1) → Additive(2) → Breaking(3) → Publication(4) |
| **AI-Assisted** | ⚠️ Partial | Handler exists but AI classification integration is structural, not fully wired to AI Hub |
| **Handler** | ✅ `ClassifyChangeLevel.cs` in `ChangeIntelligence/Features/ClassifyChangeLevel/` |

**Verdict:** ✅ Functional — classification works with rule-based logic. AI enhancement is partial.

### Step 3 — Change Score Computation

| Aspect | Status | Details |
|--------|--------|---------|
| **Endpoint** | ✅ Working | `POST /api/v1/analysis/score` → `ComputeChangeScore.Command` |
| **Formula** | ✅ Real | Composite score (0.0–1.0) = f(BreakingChangeWeight, BlastRadiusWeight, EnvironmentWeight) |
| **Entity** | ✅ Real | Creates `ChangeIntelligenceScore` with `Score`, weights, `ComputedAt` |
| **Handler** | ✅ `ComputeChangeScore.cs` in `ChangeIntelligence/Features/ComputeChangeScore/` |
| **Persistence** | ✅ Real | Saved to `ci_change_intelligence_scores` table |

**Verdict:** ✅ Functional — score computation is real with composite weights.

### Step 4 — Blast Radius Calculation

| Aspect | Status | Details |
|--------|--------|---------|
| **Endpoint** | ✅ Working | `POST /api/v1/analysis/blast-radius/{releaseId}` → `CalculateBlastRadius.Command` |
| **Direct consumers** | ✅ Real | Calculates direct consumers from dependency data |
| **Transitive consumers** | ⚠️ Partial | Transitive resolution depends on Catalog Graph — currently structural, not fully operational |
| **Entity** | ✅ Real | Creates `BlastRadiusReport` with `TotalAffectedConsumers`, `DirectConsumers[]`, `TransitiveConsumers[]` |
| **Handler** | ✅ `CalculateBlastRadius.cs` in `ChangeIntelligence/Features/CalculateBlastRadius/` |

**Verdict:** ⚠️ Partially functional — direct blast radius works; transitive depth needs Catalog Graph integration.

### Step 5 — Workflow / Review Initiation

| Aspect | Status | Details |
|--------|--------|---------|
| **Endpoint** | ✅ Working | `POST /api/v1/workflow/instances` → `InitiateWorkflow.Command` |
| **Template selection** | ✅ Real | Links to `WorkflowTemplate` with matching `ChangeType`, `ApiCriticality`, `TargetEnvironment` |
| **Instance creation** | ✅ Real | Creates `WorkflowInstance` with status `Draft` → `Pending` |
| **Stage progression** | ✅ Real | Sequential stage advancement with `CurrentStageIndex` |
| **Handler** | ✅ `InitiateWorkflow.cs` in `Workflow/Features/InitiateWorkflow/` |

**Verdict:** ✅ Functional — workflow initiation is real and working.

### Step 6 — Approval / Rejection Decision

| Aspect | Status | Details |
|--------|--------|---------|
| **Approve** | ✅ Working | `POST /api/v1/workflow/instances/{id}/approve` → `ApproveStage.Command` |
| **Reject** | ✅ Working | `POST /api/v1/workflow/instances/{id}/reject` → `RejectWorkflow.Command` |
| **Request Changes** | ✅ Working | `POST /api/v1/workflow/instances/{id}/request-changes` → `RequestChanges.Command` |
| **Entity** | ✅ Real | Creates `ApprovalDecision` with `ApprovalStatus`, `Comment`, `ApprovedBy`, `ApprovedAt` |
| **SLA** | ✅ Real | `SlaPolicy` enforced with `MaxDurationHours` and `EscalateSlaViolation` command |

**Verdict:** ✅ Functional — full approval lifecycle is real and working.

### Step 7 — Evidence Pack Generation

| Aspect | Status | Details |
|--------|--------|---------|
| **Generate** | ✅ Working | `POST /api/v1/workflow/{instanceId}/evidence` → `GenerateEvidencePack.Command` |
| **Export PDF** | ✅ Working | `POST /api/v1/workflow/{instanceId}/evidence/export-pdf` → `ExportEvidencePackPdf.Command` |
| **Entity** | ✅ Real | `EvidencePack` with `Content(JSON)`, `Attachments[]`, `ExportFormat(PDF/JSON)` |

**Verdict:** ✅ Functional — evidence generation is real.

### Step 8 — Promotion / Gate Evaluation

| Aspect | Status | Details |
|--------|--------|---------|
| **Create request** | ✅ Working | `POST /api/v1/promotion/requests` → `CreatePromotionRequest.Command` |
| **Evaluate gates** | ✅ Working | `POST /api/v1/promotion/requests/{id}/evaluate-gates` → `EvaluatePromotionGates.Command` |
| **Gate types** | ✅ Real | Quality, Security, Performance, Compliance |
| **Override** | ✅ Working | `POST /api/v1/promotion/gates/{id}/override` → `OverrideGateWithJustification.Command` (requires `promotion:admin:write`) |
| **Approve** | ✅ Working | `POST /api/v1/promotion/requests/{id}/approve` → `ApprovePromotion.Command` |

**Verdict:** ✅ Functional — promotion gates are real and working.

### Step 9 — Post-Release Review

| Aspect | Status | Details |
|--------|--------|---------|
| **Start review** | ✅ Working | `POST /api/v1/releases/{releaseId}/review/start` → `StartPostReleaseReview.Command` |
| **Progress** | ✅ Working | `POST /api/v1/releases/{releaseId}/review/progress` → `ProgressPostReleaseReview.Command` |
| **Outcome** | ✅ Real | `ReviewOutcome`: Success, Regression, Inconclusive, Pending |
| **Confidence** | ✅ Real | `ConfidenceScore` field on `PostReleaseReview` |
| **Correlation** | ⚠️ Partial | Incident correlation depends on real-time OI integration, not fully operational |

**Verdict:** ⚠️ Partially functional — review lifecycle works; incident correlation is structural.

### Step 10 — Rollback Assessment

| Aspect | Status | Details |
|--------|--------|---------|
| **Assess** | ✅ Working | `POST /api/v1/releases/{releaseId}/rollback-assessment` → `AssessRollbackViability.Query` |
| **Register** | ✅ Working | `POST /api/v1/releases/{releaseId}/rollback` → `RegisterRollback.Command` |
| **Entity** | ✅ Real | `RollbackAssessment` with `IsViable`, `RiskLevel`, `ImpactAnalysis`, `RollbackCommitSha` |

**Verdict:** ✅ Functional — rollback assessment is real and working.

---

## 2. Summary of End-to-End Flow

| Step | Description | Status | Notes |
|------|-------------|--------|-------|
| 1 | Release creation | ✅ Functional | No Catalog validation of ApiAssetId |
| 2 | Change classification | ✅ Functional | AI enhancement partial |
| 3 | Score computation | ✅ Functional | Composite formula real |
| 4 | Blast radius | ⚠️ Partial | Direct OK, transitive incomplete |
| 5 | Workflow initiation | ✅ Functional | Template matching real |
| 6 | Approval/rejection | ✅ Functional | Full lifecycle |
| 7 | Evidence pack | ✅ Functional | JSON + PDF |
| 8 | Promotion gates | ✅ Functional | 4 gate types |
| 9 | Post-release review | ⚠️ Partial | Incident correlation incomplete |
| 10 | Rollback assessment | ✅ Functional | Full lifecycle |

**Overall flow assessment:** **80% functional** — the core happy path works end-to-end. The two partial steps (blast radius transitive, incident correlation) are enhancements to an already working foundation.

---

## 3. What Is Cosmetic vs. Real

| Element | Assessment |
|---------|------------|
| Change score (0.0–1.0) | ✅ **Real** — computed from weighted formula, stored in DB |
| Blast radius report | ⚠️ **Partially real** — direct consumers calculated, transitive depth is placeholder |
| Approval workflow | ✅ **Real** — multi-stage with SLA, stored decisions, evidence packs |
| Gate evaluation | ✅ **Real** — 4 gate types with pass/fail/warning results |
| Freeze window | ✅ **Real** — conflict detection operational |
| AI advisory | ⚠️ **Structural** — AssistantPanel exists on ChangeDetailPage, but agent capabilities not fully defined |
| Jira sync | ✅ **Real** — bidirectional sync commands exist |
| PDF export | ✅ **Real** — `ExportEvidencePackPdf` command present |

---

## 4. What Is Missing from the End-to-End Flow

| Gap | Impact | Priority | Effort |
|-----|--------|----------|--------|
| Transitive blast radius via Catalog Graph | Score accuracy reduced for transitive dependencies | P1 | 1–2 weeks |
| Incident-change correlation in post-release review | Cannot automatically flag regressions | P2 | 2–3 weeks |
| Catalog validation on release creation | Orphan releases possible with invalid ApiAssetId | P1 | 2 days |
| Automatic workflow triggering on release creation | Currently manual initiation required | P2 | 1 week |
| Gate evaluation automation (vs manual trigger) | Promotion gates require explicit call | P2 | 1 week |
| Notification integration (workflow stage transitions) | No email/Slack notification on approval needed | P2 | 1 week |
