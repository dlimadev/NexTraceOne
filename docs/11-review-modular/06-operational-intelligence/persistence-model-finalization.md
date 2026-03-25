# Operational Intelligence — Persistence Model Finalization (Part 5)

> **Status:** DRAFT  
> **Date:** 2026-03-25  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation  
> **Source:** Code analysis of `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/`

---

## 1. DbContext Inventory (5)

| # | DbContext | Interface | Outbox Table | Subdomain | Configuration Namespace |
|---|---|---|---|---|---|
| 1 | `IncidentDbContext` | `IUnitOfWork` | `oi_inc_outbox_messages` | Incidents | `…Infrastructure.Incidents.Persistence.Configurations` |
| 2 | `AutomationDbContext` | `IAutomationUnitOfWork` | _(default: `outbox_messages`)_ | Automation | `…Infrastructure.Automation.Persistence.Configurations` |
| 3 | `ReliabilityDbContext` | `IUnitOfWork` | `oi_rel_outbox_messages` | Reliability | `…Infrastructure.Reliability.Persistence.Configurations` |
| 4 | `RuntimeIntelligenceDbContext` | `IUnitOfWork` | `oi_rt_outbox_messages` | Runtime | `…Infrastructure.Runtime.Persistence.Configurations` |
| 5 | `CostIntelligenceDbContext` | `IUnitOfWork` | `oi_cost_outbox_messages` | Cost | `…Infrastructure.Cost.Persistence.Configurations` |

**Base class:** All inherit `NexTraceDbContextBase` which provides:

- **RLS** — `TenantRlsInterceptor` (tenant isolation via `ICurrentTenant`)
- **Audit** — `AuditInterceptor` (auto-populates `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` on `AuditableEntity<>` types)
- **Encryption** — `EncryptionInterceptor` with AES-256-GCM via `[EncryptedField]` attribute
- **Outbox** — Domain events written to outbox table in `SaveChangesAsync()` override (lines 80–106 of `NexTraceDbContextBase.cs`)
- **Soft Delete** — Global query filter on `IsDeleted = false` for all `AuditableEntity<>` types

---

## 2. Table Inventory (19 Tables)

### 2.1 Current Table Names in Code (oi_ prefix)

All table names are defined via `builder.ToTable("…")` in each `IEntityTypeConfiguration<T>` implementation.

| # | Entity | Type | Subdomain | Current Table (code) | Target Table (ops_ prefix) | Configuration File |
|---|---|---|---|---|---|---|
| 1 | `IncidentRecord` | AggregateRoot | Incidents | `oi_incidents` | `ops_incident_records` | `IncidentRecordConfiguration.cs` |
| 2 | `MitigationWorkflowRecord` | Entity | Incidents | `oi_mitigation_workflows` | `ops_mitigation_workflows` | `MitigationWorkflowRecordConfiguration.cs` |
| 3 | `MitigationWorkflowActionLog` | Entity | Incidents | `oi_mitigation_workflow_actions` | `ops_mitigation_workflow_actions` | `MitigationWorkflowActionLogConfiguration.cs` |
| 4 | `MitigationValidationLog` | Entity | Incidents | `oi_mitigation_validations` | `ops_mitigation_validations` | `MitigationValidationLogConfiguration.cs` |
| 5 | `RunbookRecord` | Entity | Incidents | `oi_runbooks` | `ops_runbooks` | `RunbookRecordConfiguration.cs` |
| 6 | `AutomationWorkflowRecord` | AggregateRoot | Automation | `oi_automation_workflows` | `ops_automation_workflow_records` | `AutomationWorkflowRecordConfiguration.cs` |
| 7 | `AutomationValidationRecord` | Entity | Automation | `oi_automation_validations` | `ops_automation_validation_records` | `AutomationValidationRecordConfiguration.cs` |
| 8 | `AutomationAuditRecord` | Entity | Automation | `oi_automation_audit_records` | `ops_automation_audit_records` | `AutomationAuditRecordConfiguration.cs` |
| 9 | `ReliabilitySnapshot` | AggregateRoot | Reliability | `oi_reliability_snapshots` | `ops_reliability_snapshots` | `ReliabilitySnapshotConfiguration.cs` |
| 10 | `RuntimeSnapshot` | AggregateRoot | Runtime | `oi_runtime_snapshots` | `ops_runtime_snapshots` | `RuntimeSnapshotConfiguration.cs` |
| 11 | `RuntimeBaseline` | Entity | Runtime | `oi_runtime_baselines` | `ops_runtime_baselines` | `RuntimeBaselineConfiguration.cs` |
| 12 | `DriftFinding` | Aggregate-Level | Runtime | `oi_drift_findings` | `ops_drift_findings` | `DriftFindingConfiguration.cs` |
| 13 | `ObservabilityProfile` | Aggregate-Level | Runtime | `oi_observability_profiles` | `ops_observability_profiles` | `ObservabilityProfileConfiguration.cs` |
| 14 | `CostSnapshot` | AggregateRoot | Cost | `oi_cost_snapshots` | `ops_cost_snapshots` | `CostSnapshotConfiguration.cs` |
| 15 | `CostAttribution` | Entity | Cost | `oi_cost_attributions` | `ops_cost_attributions` | `CostAttributionConfiguration.cs` |
| 16 | `CostTrend` | Entity | Cost | `oi_cost_trends` | `ops_cost_trends` | `CostTrendConfiguration.cs` |
| 17 | `ServiceCostProfile` | Aggregate-Level | Cost | `oi_service_cost_profiles` | `ops_service_cost_profiles` | `ServiceCostProfileConfiguration.cs` |
| 18 | `CostImportBatch` | Aggregate-Level | Cost | `oi_cost_import_batches` | `ops_cost_import_batches` | `CostImportBatchConfiguration.cs` |
| 19 | `CostRecord` | Entity | Cost | `oi_cost_records` | `ops_cost_records` | `CostRecordConfiguration.cs` |

### 2.2 Outbox Tables (4 + 1 default)

| # | Outbox Table | DbContext | Override |
|---|---|---|---|
| 1 | `oi_inc_outbox_messages` | IncidentDbContext | ✅ Explicit |
| 2 | `oi_rel_outbox_messages` | ReliabilityDbContext | ✅ Explicit |
| 3 | `oi_rt_outbox_messages` | RuntimeIntelligenceDbContext | ✅ Explicit |
| 4 | `oi_cost_outbox_messages` | CostIntelligenceDbContext | ✅ Explicit |
| 5 | `outbox_messages` | AutomationDbContext | ❌ Uses base default |

---

## 3. Primary Keys

All entities use **strongly-typed GUID IDs** defined as sealed record types.

| Entity | TypedId | PK Column | Type |
|---|---|---|---|
| `IncidentRecord` | `IncidentRecordId(Guid Value)` | `id` | UUID |
| `MitigationWorkflowRecord` | `MitigationWorkflowRecordId(Guid Value)` | `id` | UUID |
| `MitigationWorkflowActionLog` | `MitigationWorkflowActionLogId(Guid Value)` | `id` | UUID |
| `MitigationValidationLog` | `MitigationValidationLogId(Guid Value)` | `id` | UUID |
| `RunbookRecord` | `RunbookRecordId(Guid Value)` | `id` | UUID |
| `AutomationWorkflowRecord` | `AutomationWorkflowRecordId(Guid Value)` | `id` | UUID |
| `AutomationValidationRecord` | `AutomationValidationRecordId(Guid Value)` | `id` | UUID |
| `AutomationAuditRecord` | `AutomationAuditRecordId(Guid Value)` | `id` | UUID |
| `ReliabilitySnapshot` | `ReliabilitySnapshotId(Guid Value)` | `id` | UUID |
| `RuntimeSnapshot` | `RuntimeSnapshotId(Guid Value)` | `id` | UUID |
| `RuntimeBaseline` | `RuntimeBaselineId(Guid Value)` | `id` | UUID |
| `DriftFinding` | `DriftFindingId(Guid Value)` | `id` | UUID |
| `ObservabilityProfile` | `ObservabilityProfileId(Guid Value)` | `id` | UUID |
| `CostSnapshot` | `CostSnapshotId(Guid Value)` | `id` | UUID |
| `CostAttribution` | `CostAttributionId(Guid Value)` | `id` | UUID |
| `CostTrend` | `CostTrendId(Guid Value)` | `id` | UUID |
| `ServiceCostProfile` | `ServiceCostProfileId(Guid Value)` | `id` | UUID |
| `CostImportBatch` | `CostImportBatchId(Guid Value)` | `id` | UUID |
| `CostRecord` | `CostRecordId(Guid Value)` | `id` | UUID |

---

## 4. Foreign Keys

| Child Entity | FK Column | Parent Entity | Cascade | Configuration |
|---|---|---|---|---|
| `MitigationWorkflowRecord` | `incident_id` | `IncidentRecord` | _(logical — VARCHAR 200, no EF FK)_ | `MitigationWorkflowRecordConfiguration` |
| `MitigationWorkflowActionLog` | `workflow_id` | `MitigationWorkflowRecord` | _(logical — Guid, no navigation)_ | `MitigationWorkflowActionLogConfiguration` |
| `MitigationValidationLog` | `workflow_id` | `MitigationWorkflowRecord` | _(logical — Guid, no navigation)_ | `MitigationValidationLogConfiguration` |
| `AutomationValidationRecord` | `workflow_id` | `AutomationWorkflowRecord` | ✅ Cascade Delete | `AutomationValidationRecordConfiguration` |
| `AutomationAuditRecord` | `workflow_id` | `AutomationWorkflowRecord` | ✅ Cascade Delete | `AutomationAuditRecordConfiguration` |
| `CostRecord` | `batch_id` | `CostImportBatch` | _(logical — Guid, indexed)_ | `CostRecordConfiguration` |
| `CostAttribution` | `api_asset_id` | _(external — Contract module)_ | _(cross-module reference)_ | `CostAttributionConfiguration` |
| `DriftFinding` | `release_id` | _(external — Change Governance)_ | _(nullable, cross-module reference)_ | `DriftFindingConfiguration` |

**Observations:**
- ⚠️ Mitigation entities use **logical FKs** (no EF navigation property / no `HasOne().WithMany()`) — IncidentId is stored as VARCHAR, not typed Guid
- ✅ Automation entities use **proper EF FKs** with `HasOne().WithMany().HasForeignKey().OnDelete(Cascade)`
- ⚠️ CostRecord → CostImportBatch is **logical only** (indexed but no EF relationship)

---

## 5. Indexes

### 5.1 Incidents Subdomain

| Table | Index | Columns | Type |
|---|---|---|---|
| `oi_incidents` | `IX_oi_incidents_ExternalRef` | `ExternalRef` | UNIQUE |
| `oi_incidents` | `IX_oi_incidents_ServiceId` | `ServiceId` | Non-unique |
| `oi_incidents` | `IX_oi_incidents_Status` | `Status` | Non-unique |
| `oi_incidents` | `IX_oi_incidents_Severity` | `Severity` | Non-unique |
| `oi_incidents` | `IX_oi_incidents_DetectedAt` | `DetectedAt` | Non-unique |
| `oi_incidents` | `ix_oi_incidents_tenant_id` | `TenantId` | Non-unique (named) |
| `oi_incidents` | `ix_oi_incidents_tenant_environment` | `TenantId, EnvironmentId` | Composite (named) |
| `oi_mitigation_workflows` | `IX_…_IncidentId` | `IncidentId` | Non-unique |
| `oi_mitigation_workflows` | `IX_…_Status` | `Status` | Non-unique |
| `oi_mitigation_workflow_actions` | `IX_…_WorkflowId` | `WorkflowId` | Non-unique |
| `oi_mitigation_workflow_actions` | `IX_…_IncidentId` | `IncidentId` | Non-unique |
| `oi_mitigation_validations` | `IX_…_IncidentId` | `IncidentId` | Non-unique |
| `oi_mitigation_validations` | `IX_…_WorkflowId` | `WorkflowId` | Non-unique |
| `oi_runbooks` | `IX_…_LinkedService` | `LinkedService` | Non-unique |

### 5.2 Automation Subdomain

| Table | Index | Columns | Type |
|---|---|---|---|
| `oi_automation_workflows` | `IX_…_ActionId` | `ActionId` | Non-unique |
| `oi_automation_workflows` | `IX_…_ServiceId` | `ServiceId` | Non-unique |
| `oi_automation_workflows` | `IX_…_IncidentId` | `IncidentId` | Non-unique |
| `oi_automation_workflows` | `IX_…_Status` | `Status` | Non-unique |
| `oi_automation_workflows` | `IX_…_RequestedBy` | `RequestedBy` | Non-unique |
| `oi_automation_workflows` | `IX_…_CreatedAt` | `CreatedAt` | Non-unique |
| `oi_automation_validations` | `IX_…_WorkflowId` | `WorkflowId` | UNIQUE |
| `oi_automation_validations` | `IX_…_Outcome` | `Outcome` | Non-unique |
| `oi_automation_audit_records` | `IX_…_WorkflowId` | `WorkflowId` | Non-unique |
| `oi_automation_audit_records` | `IX_…_ServiceId` | `ServiceId` | Non-unique |
| `oi_automation_audit_records` | `IX_…_TeamId` | `TeamId` | Non-unique |
| `oi_automation_audit_records` | `IX_…_OccurredAt` | `OccurredAt` | Non-unique |
| `oi_automation_audit_records` | `IX_…_Action` | `Action` | Non-unique |

### 5.3 Reliability Subdomain

| Table | Index | Columns | Type |
|---|---|---|---|
| `oi_reliability_snapshots` | Composite | `TenantId, ServiceId, ComputedAt` | Composite |
| `oi_reliability_snapshots` | Composite | `TenantId, ComputedAt` | Composite |

### 5.4 Runtime Subdomain

| Table | Index | Columns | Type |
|---|---|---|---|
| `oi_runtime_snapshots` | Composite | `ServiceName, Environment, CapturedAt` | Composite |
| `oi_runtime_snapshots` | Single | `HealthStatus` | Non-unique |
| `oi_runtime_baselines` | Composite | `ServiceName, Environment` | UNIQUE |
| `oi_runtime_baselines` | Single | `EstablishedAt` | Non-unique |
| `oi_drift_findings` | Composite | `ServiceName, Environment, DetectedAt` | Composite |
| `oi_drift_findings` | Single | `Severity` | Non-unique |
| `oi_drift_findings` | Single | `IsResolved` | Non-unique |
| `oi_observability_profiles` | Composite | `ServiceName, Environment` | UNIQUE |
| `oi_observability_profiles` | Single | `LastAssessedAt` | Non-unique |

### 5.5 Cost Subdomain

| Table | Index | Columns | Type |
|---|---|---|---|
| `oi_cost_snapshots` | Composite | `ServiceName, Environment, CapturedAt` | Composite |
| `oi_cost_snapshots` | Single | `Period` | Non-unique |
| `oi_cost_attributions` | Composite | `ApiAssetId, Environment, PeriodStart, PeriodEnd` | UNIQUE |
| `oi_cost_attributions` | Single | `ServiceName` | Non-unique |
| `oi_cost_trends` | Composite | `ServiceName, Environment, PeriodStart, PeriodEnd` | UNIQUE |
| `oi_cost_trends` | Single | `TrendDirection` | Non-unique |
| `oi_service_cost_profiles` | Composite | `ServiceName, Environment` | UNIQUE |
| `oi_cost_import_batches` | Composite | `Source, Period` | Composite |
| `oi_cost_import_batches` | Single | `Status` | Non-unique |
| `oi_cost_records` | Single | `BatchId` | Non-unique |
| `oi_cost_records` | Composite | `ServiceId, Period` | Composite |
| `oi_cost_records` | Single | `Period` | Non-unique |
| `oi_cost_records` | Single | `Team` | Non-unique |
| `oi_cost_records` | Single | `Domain` | Non-unique |

---

## 6. Uniqueness Constraints

| Table | Columns | Purpose |
|---|---|---|
| `oi_incidents` | `ExternalRef` | Prevent duplicate external incident references |
| `oi_automation_validations` | `WorkflowId` | One validation per workflow |
| `oi_runtime_baselines` | `ServiceName, Environment` | One baseline per service/environment pair |
| `oi_observability_profiles` | `ServiceName, Environment` | One profile per service/environment pair |
| `oi_cost_attributions` | `ApiAssetId, Environment, PeriodStart, PeriodEnd` | One attribution per API asset/period |
| `oi_cost_trends` | `ServiceName, Environment, PeriodStart, PeriodEnd` | One trend per service/period |
| `oi_service_cost_profiles` | `ServiceName, Environment` | One cost profile per service/environment pair |

---

## 7. Audit Columns

All entities inheriting `AuditableEntity<TId>` receive automatic audit columns via `AuditInterceptor`:

| Column | Type | Source | Applied To |
|---|---|---|---|
| `CreatedAt` | `DateTimeOffset` | `IDateTimeProvider.UtcNow` | All AuditableEntity types |
| `CreatedBy` | `string` | `ICurrentUser.UserId` | All AuditableEntity types |
| `UpdatedAt` | `DateTimeOffset?` | `IDateTimeProvider.UtcNow` | All AuditableEntity types (on update) |
| `UpdatedBy` | `string?` | `ICurrentUser.UserId` | All AuditableEntity types (on update) |
| `IsDeleted` | `bool` | Soft delete flag | All AuditableEntity types (global filter) |

**Coverage by entity:**

| Entity | AuditableEntity | Audit Columns |
|---|---|---|
| `IncidentRecord` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `MitigationWorkflowRecord` | ⚠️ Entity (not AuditableEntity) | Manual `CreatedByUser`, `CompletedBy` fields |
| `MitigationWorkflowActionLog` | ⚠️ Entity | Manual `PerformedBy`, `PerformedAt` |
| `MitigationValidationLog` | ⚠️ Entity | Manual `ValidatedBy`, `ValidatedAt` |
| `RunbookRecord` | ⚠️ Entity | Manual `PublishedAt`, `LastReviewedAt`, `MaintainedBy` |
| `AutomationWorkflowRecord` | ⚠️ Entity | Manual `CreatedAt`, `UpdatedAt`, `RequestedBy` |
| `AutomationValidationRecord` | ⚠️ Entity | Manual `ValidatedBy`, `ValidatedAt`, `CreatedAt` |
| `AutomationAuditRecord` | ⚠️ Entity (immutable) | Manual `Actor`, `OccurredAt`, `CreatedAt` |
| `ReliabilitySnapshot` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `RuntimeSnapshot` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `RuntimeBaseline` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `DriftFinding` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `ObservabilityProfile` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `CostSnapshot` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `CostAttribution` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `CostTrend` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `ServiceCostProfile` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `CostImportBatch` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |
| `CostRecord` | ✅ Yes | CreatedAt, CreatedBy, UpdatedAt, UpdatedBy |

---

## 8. TenantId & EnvironmentId

### 8.1 TenantId

Applied via `TenantRlsInterceptor` on all entities that support multi-tenancy.

| Entity | TenantId | Mechanism |
|---|---|---|
| `IncidentRecord` | ✅ Via RLS interceptor | Indexed: `ix_oi_incidents_tenant_id` |
| `ReliabilitySnapshot` | ✅ Explicit column | Composite index: `(TenantId, ServiceId, ComputedAt)` |
| All others | ✅ Via RLS interceptor | Applied globally by `NexTraceDbContextBase` |

### 8.2 EnvironmentId / Environment

| Entity | Field | Type | Notes |
|---|---|---|---|
| `IncidentRecord` | `EnvironmentId` | Guid? | Composite index: `(TenantId, EnvironmentId)` |
| `RuntimeSnapshot` | `Environment` | VARCHAR 100 | Part of composite index: `(ServiceName, Environment, CapturedAt)` |
| `RuntimeBaseline` | `Environment` | VARCHAR 100 | Part of unique: `(ServiceName, Environment)` |
| `DriftFinding` | `Environment` | VARCHAR 100 | Part of composite index |
| `ObservabilityProfile` | `Environment` | VARCHAR 100 | Part of unique: `(ServiceName, Environment)` |
| `ReliabilitySnapshot` | `Environment` | VARCHAR 100 | Present on entity |
| `CostSnapshot` | `Environment` | VARCHAR | Part of composite index |
| `CostAttribution` | `Environment` | VARCHAR | Part of unique constraint |
| `CostTrend` | `Environment` | VARCHAR | Part of unique constraint |
| `ServiceCostProfile` | `Environment` | VARCHAR | Part of unique constraint |
| `CostRecord` | `Environment` | VARCHAR 100 | Nullable |
| `AutomationWorkflowRecord` | `TargetEnvironment` | VARCHAR | Present on entity |

---

## 9. Concurrency Control — RowVersion

> **Status:** ❌ MISSING on ALL 19 entities

No entity in the Operational Intelligence module implements `RowVersion` or `ConcurrencyToken`. This is a **required addition** for entities with mutable state:

| Entity | Concurrency Risk | Priority |
|---|---|---|
| `IncidentRecord` | 🔴 High — concurrent status updates, correlation refresh | P1 |
| `MitigationWorkflowRecord` | 🔴 High — concurrent workflow state transitions | P1 |
| `AutomationWorkflowRecord` | 🔴 High — concurrent approval/execution transitions | P1 |
| `DriftFinding` | 🟠 Medium — acknowledge/resolve operations | P2 |
| `ServiceCostProfile` | 🟠 Medium — budget updates | P2 |
| `CostImportBatch` | 🟡 Low — status transitions (single-writer expected) | P3 |
| `RuntimeBaseline` | 🟡 Low — upsert pattern (single-writer expected) | P3 |
| `ObservabilityProfile` | 🟡 Low — upsert pattern | P3 |
| All snapshot/log entities | 🟢 None — append-only | — |

---

## 10. Check Constraints

> **Status:** ❌ NONE defined on any entity

No `HasCheckConstraint()` calls found in any configuration file. Recommended additions:

| Table | Constraint | Expression | Priority |
|---|---|---|---|
| `oi_reliability_snapshots` | `chk_overall_score_range` | `overall_score >= 0 AND overall_score <= 100` | P2 |
| `oi_runtime_snapshots` | `chk_error_rate_range` | `error_rate >= 0 AND error_rate <= 100` | P2 |
| `oi_cost_snapshots` | `chk_total_cost_positive` | `total_cost >= 0` | P2 |
| `oi_cost_attributions` | `chk_cost_per_request_positive` | `cost_per_request >= 0` | P3 |
| `oi_drift_findings` | `chk_deviation_positive` | `deviation_percent >= 0` | P3 |

---

## 11. State Persistence — Operational State Machines

### 11.1 Incident Lifecycle

```
Open → Investigating → Mitigating → Monitoring → Resolved → Closed
```

- **State column:** `oi_incidents.status` (INTEGER enum → `IncidentStatus`)
- **Mitigation state:** `oi_incidents.mitigation_status` (INTEGER enum → `MitigationStatus`)
- **Persisted as:** Integer enum value

### 11.2 Mitigation Workflow Lifecycle

```
Draft → Recommended → AwaitingApproval → Approved → InProgress → AwaitingValidation → Completed
                                       ↘ Rejected
                          (any) → Cancelled
```

- **State column:** `oi_mitigation_workflows.status` (INTEGER enum → `MitigationWorkflowStatus`)
- **Action log:** Each transition recorded in `oi_mitigation_workflow_actions`
- **Validation:** Post-mitigation outcome recorded in `oi_mitigation_validations`

### 11.3 Automation Workflow Lifecycle

```
Draft → PendingApproval → Approved → Executing → Completed
                        ↘ Rejected
              (any) → Cancelled → Failed
```

- **State column:** `oi_automation_workflows.status` (STRING enum, default `Draft`)
- **Approval state:** `oi_automation_workflows.approval_status` (STRING enum, default `Pending`)
- **Audit trail:** Every transition recorded in `oi_automation_audit_records`
- **Validation:** Post-execution validation in `oi_automation_validations`

### 11.4 Drift Finding Lifecycle

```
Detected → Acknowledged → Resolved
```

- **State columns:** `is_acknowledged` (BOOLEAN), `is_resolved` (BOOLEAN)
- **Resolution:** `resolution_comment`, `resolved_at`

### 11.5 Cost Import Batch Lifecycle

```
Pending → Completed | Failed
```

- **State column:** `oi_cost_import_batches.status` (VARCHAR 20: `Pending`, `Completed`, `Failed`)
- **Error tracking:** `error` (VARCHAR 4000)

---

## 12. JSON Columns (Embedded Data)

Several entities use JSON columns for flexible/nested data:

| Table | JSON Column | Content |
|---|---|---|
| `oi_incidents` | `timeline_json` | Incident timeline events |
| `oi_incidents` | `linked_services_json` | Array of linked service references |
| `oi_incidents` | `correlated_changes_json` | Correlated change events |
| `oi_incidents` | `correlated_services_json` | Correlated service identifiers |
| `oi_incidents` | `correlated_dependencies_json` | Correlated dependency graph |
| `oi_incidents` | `impacted_contracts_json` | Impacted contract references |
| `oi_incidents` | `evidence_observations_json` | Evidence and signal observations |
| `oi_incidents` | `related_contracts_json` | Related contract references |
| `oi_incidents` | `runbook_links_json` | Linked runbook identifiers |
| `oi_incidents` | `mitigation_actions_json` | Active mitigation actions |
| `oi_incidents` | `mitigation_recommendations_json` | Recommended mitigations |
| `oi_incidents` | `mitigation_recommended_runbooks_json` | Recommended runbooks for mitigation |
| `oi_mitigation_workflows` | `steps_json` | Workflow step definitions |
| `oi_mitigation_workflows` | `decisions_json` | Workflow decisions log |
| `oi_mitigation_validations` | `checks_json` | Validation check results |
| `oi_runbooks` | `steps_json` | Runbook procedure steps |
| `oi_runbooks` | `prerequisites_json` | Runbook prerequisites |

---

## 13. Divergences — Current State vs Target Model

| # | Divergence | Current | Target | Priority | Effort |
|---|---|---|---|---|---|
| 1 | **Table prefix** | `oi_` prefix | `ops_` prefix per domain-model-finalization.md | 🟠 P2 | Migration + rename |
| 2 | **RowVersion** | ❌ Missing on all entities | Required on mutable entities (Incident, Workflows, DriftFinding, ServiceCostProfile) | 🔴 P1 | 2–3h |
| 3 | **Check constraints** | ❌ None | Required for score/rate/cost value ranges | 🟡 P3 | 1–2h |
| 4 | **AutomationDbContext outbox** | Uses default `outbox_messages` table name | Should use `oi_auto_outbox_messages` (or `ops_auto_outbox_messages`) for consistency | 🟠 P2 | 1h |
| 5 | **Mitigation FK typing** | `IncidentId` stored as VARCHAR 200 (logical FK) | Should be typed Guid FK with EF navigation | 🟡 P3 | 3–4h |
| 6 | **CostRecord → CostImportBatch** | Logical FK only (indexed, no EF relationship) | Should be proper EF FK with cascade | 🟡 P3 | 1h |
| 7 | **Incident sub-entities audit** | Manual audit fields (PerformedBy, etc.) | Consider promotion to `AuditableEntity` for consistency | 🟡 P3 | 2–3h |
| 8 | **Automation enum storage** | STRING enums (default values in config) | Should match other subdomains (INTEGER enums) | 🟡 P3 | 2h |

---

## 14. Action Backlog

| # | Action | Esforço | Prioridade |
|---|---|---|---|
| 1 | Add `RowVersion` (`xmin` or explicit `uint` column) to `IncidentRecord`, `MitigationWorkflowRecord`, `AutomationWorkflowRecord` | 2–3h | 🔴 P1 |
| 2 | Add `RowVersion` to `DriftFinding`, `ServiceCostProfile`, `CostImportBatch` | 1–2h | 🟠 P2 |
| 3 | Rename table prefix from `oi_` to `ops_` across all 19 entity configurations + migration | 3–4h | 🟠 P2 |
| 4 | Override `OutboxTableName` in `AutomationDbContext` | 30min | 🟠 P2 |
| 5 | Add check constraints for score/rate/cost ranges | 1–2h | 🟡 P3 |
| 6 | Convert Mitigation `IncidentId` from VARCHAR to typed Guid FK | 3–4h | 🟡 P3 |
| 7 | Add EF FK relationship for `CostRecord.BatchId → CostImportBatch` | 1h | 🟡 P3 |
| 8 | Standardize Automation enum storage to INTEGER (aligning with other subdomains) | 2h | 🟡 P3 |
