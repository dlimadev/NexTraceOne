# Change Governance — Persistence Model Finalization

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Target prefix:** `chg_`

---

## 1. Current Database Architecture

### 1.1 DbContexts (4)

| DbContext | Subdomain | DbSets | Outbox Table | Migration |
|-----------|-----------|--------|--------------|-----------|
| `ChangeIntelligenceDbContext` | ChangeIntelligence | 11 | `ci_outbox_messages` | `20260321160240_InitialCreate` |
| `WorkflowDbContext` | Workflow | 6 | `wf_outbox_messages` | `20260321160543_InitialCreate` |
| `PromotionDbContext` | Promotion | 4 | `prm_outbox_messages` | `20260321160602_InitialCreate` |
| `RulesetGovernanceDbContext` | RulesetGovernance | 6 | `rg_outbox_messages` | `20260321160526_InitialCreate` |

All 4 DbContexts inherit from `NexTraceDbContextBase`, which provides:
- `TenantRlsInterceptor` — PostgreSQL RLS for tenant isolation
- `AuditInterceptor` — `CreatedAt`, `UpdatedAt`, `CreatedBy` columns
- `EncryptionInterceptor` — AES-256-GCM field encryption
- `OutboxInterceptor` — Outbox pattern for event-driven integration

### 1.2 Infrastructure Files

| File | Location |
|------|----------|
| ChangeIntelligenceDbContext | `Infrastructure/ChangeIntelligence/Persistence/ChangeIntelligenceDbContext.cs` |
| WorkflowDbContext | `Infrastructure/Workflow/Persistence/WorkflowDbContext.cs` |
| PromotionDbContext | `Infrastructure/Promotion/Persistence/PromotionDbContext.cs` |
| RulesetGovernanceDbContext | `Infrastructure/RulesetGovernance/Persistence/RulesetGovernanceDbContext.cs` |

---

## 2. Current Table Mappings

### 2.1 ChangeIntelligence Tables (prefix: `ci_`)

| Entity | Current Table | Configuration File |
|--------|--------------|-------------------|
| Release | `ci_releases` | `ReleaseConfiguration.cs` |
| ChangeEvent | `ci_change_events` | `ChangeEventConfiguration.cs` |
| BlastRadiusReport | `ci_blast_radius_reports` | `BlastRadiusReportConfiguration.cs` |
| ChangeIntelligenceScore | `ci_change_intelligence_scores` | `ChangeIntelligenceScoreConfiguration.cs` |
| FreezeWindow | `ci_freeze_windows` | `FreezeWindowConfiguration.cs` |
| ReleaseBaseline | `ci_release_baselines` | `ReleaseBaselineConfiguration.cs` |
| ObservationWindow | `ci_observation_windows` | `ObservationWindowConfiguration.cs` |
| PostReleaseReview | `ci_post_release_reviews` | `PostReleaseReviewConfiguration.cs` |
| RollbackAssessment | `ci_rollback_assessments` | `RollbackAssessmentConfiguration.cs` |
| ExternalMarker | `ci_external_markers` | `ExternalMarkerConfiguration.cs` |
| DeploymentState | `ci_deployment_states` | (within Release configuration) |

### 2.2 Workflow Tables (prefix: `wf_`)

| Entity | Current Table | Configuration File |
|--------|--------------|-------------------|
| WorkflowTemplate | `wf_workflow_templates` | `WorkflowTemplateConfiguration.cs` |
| WorkflowInstance | `wf_workflow_instances` | `WorkflowInstanceConfiguration.cs` |
| WorkflowStage | `wf_workflow_stages` | `WorkflowStageConfiguration.cs` |
| ApprovalDecision | `wf_approval_decisions` | `ApprovalDecisionConfiguration.cs` |
| EvidencePack | `wf_evidence_packs` | `EvidencePackConfiguration.cs` |
| SlaPolicy | `wf_sla_policies` | `SlaPolicyConfiguration.cs` |

### 2.3 Promotion Tables (prefix: `prm_`)

| Entity | Current Table | Configuration File |
|--------|--------------|-------------------|
| PromotionRequest | `prm_promotion_requests` | `PromotionRequestConfiguration.cs` |
| PromotionGate | `prm_promotion_gates` | `PromotionGateConfiguration.cs` |
| GateEvaluation | `prm_gate_evaluations` | `GateEvaluationConfiguration.cs` |
| DeploymentEnvironment | `prm_deployment_environments` | `DeploymentEnvironmentConfiguration.cs` |

### 2.4 RulesetGovernance Tables (prefix: `rg_`)

| Entity | Current Table | Configuration File |
|--------|--------------|-------------------|
| Ruleset | `rg_rulesets` | `RulesetConfiguration.cs` |
| RulesetBinding | `rg_ruleset_bindings` | `RulesetBindingConfiguration.cs` |
| LintResult | `rg_lint_results` | `LintResultConfiguration.cs` |
| LintExecution | `rg_lint_executions` | (within Ruleset configuration) |
| LintFinding | `rg_lint_findings` | (within LintExecution configuration) |
| RulesetScore | `rg_ruleset_scores` | (within Ruleset configuration) |

---

## 3. Target Table Names (with `chg_` prefix)

Per `docs/architecture/database-table-prefixes.md`, the official prefix for Change Governance is `chg_`. The current implementation uses **subdomain-specific prefixes** (`ci_`, `wf_`, `prm_`, `rg_`).

### Decision: Prefix Strategy

**Option A — Unified `chg_` prefix (recommended):**
All tables renamed to `chg_*`. Examples: `chg_releases`, `chg_workflow_templates`, `chg_promotion_requests`, `chg_rulesets`.

**Option B — Compound prefix `chg_ci_`, `chg_wf_`, etc.:**
Keeps subdomain visibility. Examples: `chg_ci_releases`, `chg_wf_templates`.

**Option C — Keep subdomain prefixes as-is:**
The current `ci_`, `wf_`, `prm_`, `rg_` already provide namespace isolation. The architectural standard (`chg_`) may be satisfied by the module-level grouping.

### ⚠️ Current Divergence

| Current Prefix | Target Prefix | Tables Affected |
|---------------|--------------|-----------------|
| `ci_` | `chg_` (or `chg_ci_`) | 11 tables + 1 outbox |
| `wf_` | `chg_` (or `chg_wf_`) | 6 tables + 1 outbox |
| `prm_` | `chg_` (or `chg_prm_`) | 4 tables + 1 outbox |
| `rg_` | `chg_` (or `chg_rg_`) | 6 tables + 1 outbox |

**Total tables to rename:** 27 data tables + 4 outbox tables = **31 tables**

---

## 4. Current Indexes

### ChangeIntelligence Indexes

| Table | Index Columns | Type |
|-------|--------------|------|
| `ci_releases` | `ApiAssetId` | Non-unique |
| `ci_releases` | `TenantId` | Non-unique |
| `ci_releases` | `TenantId`, `EnvironmentId` | Composite non-unique |
| `ci_freeze_windows` | `ApiAssetId` | Non-unique |
| `ci_freeze_windows` | `ApiAssetId`, `StartTime`, `EndTime` | Composite non-unique |

### Workflow Indexes

| Table | Index Columns | Type |
|-------|--------------|------|
| `wf_workflow_instances` | `WorkflowTemplateId` | Non-unique |
| `wf_workflow_instances` | `ReleaseId` | Non-unique |
| `wf_workflow_instances` | `Status` | Non-unique |

### Promotion Indexes

| Table | Index Columns | Type |
|-------|--------------|------|
| `prm_promotion_requests` | `ReleaseId` | Non-unique |
| `prm_promotion_requests` | `Status` | Non-unique |
| `prm_promotion_requests` | `TargetEnvironmentId` | Non-unique |
| `prm_promotion_requests` | `RequestedAt` | Non-unique |

### RulesetGovernance Indexes

| Table | Index Columns | Type |
|-------|--------------|------|
| `rg_rulesets` | `IsActive` | Non-unique |
| `rg_rulesets` | `RulesetType` | Non-unique |
| `rg_ruleset_bindings` | `RulesetId` | Non-unique |
| `rg_ruleset_bindings` | `AssetType` | Non-unique |

---

## 5. Missing Constraints

| Gap | Table(s) | Description | Priority |
|-----|----------|-------------|----------|
| PC-01 | All mutable tables | No `RowVersion` / `ConcurrencyToken` (xmin) | P1 |
| PC-02 | All tables | No FK constraints to parent aggregates | P1 |
| PC-03 | `ci_releases` | No CHECK constraint on `ChangeScore` (should be 0.0–1.0) | P2 |
| PC-04 | `ci_change_intelligence_scores` | No CHECK constraint on `Score` (0.0–1.0) | P2 |
| PC-05 | `ci_freeze_windows` | No CHECK constraint on `StartTime < EndTime` | P2 |
| PC-06 | `wf_workflow_instances` | No FK to `wf_workflow_templates.WorkflowTemplateId` | P1 |
| PC-07 | `wf_workflow_instances` | No FK to `ci_releases.ReleaseId` (cross-DbContext reference) | P2 |
| PC-08 | `prm_promotion_requests` | No FK to `ci_releases.ReleaseId` (cross-DbContext reference) | P2 |
| PC-09 | All tables | `TenantId` present but no explicit index in most tables | P2 |
| PC-10 | `ci_releases` | No unique constraint on `(ApiAssetId, Version, EnvironmentId)` | P2 |

---

## 6. Audit Columns

All entities inherit from `AuditableEntity` via `NexTraceDbContextBase`:

| Column | Type | Source |
|--------|------|--------|
| `CreatedAt` | `DateTimeOffset` | `AuditInterceptor` |
| `UpdatedAt` | `DateTimeOffset` | `AuditInterceptor` |
| `CreatedBy` | `string` | `AuditInterceptor` |

---

## 7. Outbox Tables (4)

| Outbox Table | DbContext | Pattern |
|-------------|-----------|---------|
| `ci_outbox_messages` | ChangeIntelligenceDbContext | `OutboxInterceptor` |
| `wf_outbox_messages` | WorkflowDbContext | `OutboxInterceptor` |
| `prm_outbox_messages` | PromotionDbContext | `OutboxInterceptor` |
| `rg_outbox_messages` | RulesetGovernanceDbContext | `OutboxInterceptor` |

---

## 8. Migration Strategy

### Current State
- 4 initial migrations (one per DbContext), all dated 2026-03-21
- No additional migrations beyond InitialCreate

### Pre-conditions for New Migrations
1. Freeze domain model changes
2. Decide on prefix strategy (unified `chg_` vs. subdomain prefixes)
3. Add `RowVersion` to all mutable aggregates
4. Define FK constraints within each DbContext
5. Add CHECK constraints for score ranges and date validations
6. Add missing indexes on `TenantId` columns
7. Consider consolidating 4 DbContexts into 1 `ChangeGovernanceDbContext` (architectural decision pending)

### ⚠️ DbContext Consolidation Decision

The architecture target mentions `ChangeGovernanceDbContext` as a single context. Current implementation has 4. Consolidation would:

- **Pros:** Simpler transaction management, cross-subdomain queries, unified migration history
- **Cons:** Larger migration surface, loss of subdomain isolation, more complex `OnModelCreating`

**Recommendation:** Keep 4 DbContexts for now; consolidation is a Phase 2 decision.
