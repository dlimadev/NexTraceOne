# Part 11 — Module Dependency Map

> **Module:** Operational Intelligence
> **Date:** 2025-07-14
> **Status:** Review Complete
> **Scope:** Cross-module dependencies, integration gaps, exposed/consumed contracts

---

## 1. Dependency Overview

```
┌─────────────────────────────────────────────────────────────┐
│                 OPERATIONAL INTELLIGENCE                      │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────────┐  │
│  │Incidents │ │Automation│ │Reliability│ │Runtime/Cost  │  │
│  └────┬─────┘ └────┬─────┘ └─────┬─────┘ └──────┬───────┘  │
└───────┼─────────────┼─────────────┼──────────────┼──────────┘
        │             │             │              │
   ┌────▼────┐   ┌────▼────┐  ┌────▼────┐   ┌────▼────┐
   │Identity │   │ Change  │  │ Service │   │Environ- │
   │& Access │   │Governan.│  │ Catalog │   │  ment   │
   │  ✅ INT │   │  ✅ INT │  │  ✅ REF │   │  ⚠️ REF │
   └─────────┘   └─────────┘  └─────────┘   └─────────┘

   ┌─────────┐   ┌─────────┐  ┌─────────┐   ┌─────────┐
   │Notific- │   │Audit &  │  │AI &     │   │Integra- │
   │ ations  │   │Complian.│  │Knowledge│   │  tions   │
   │  ❌ GAP │   │  ❌ GAP │  │  ❌ FUT │   │  ❌ FUT │
   └─────────┘   └─────────┘  └─────────┘   └─────────┘
```

---

## 2. Integrated Dependencies

### 2.1 Identity & Access — ✅ Integrated

**Integration type:** JWT claims + PostgreSQL RLS

| Integration Point | Mechanism | Source |
|---|---|---|
| Authentication | JWT bearer token on all endpoints | API host middleware |
| Permission enforcement | `.RequirePermission()` on all 44+ endpoints | Each `*EndpointModule.cs` |
| Tenant isolation | `TenantRlsInterceptor` on all 5 DbContexts | `TenantRlsInterceptor.cs` |
| User context | `ICurrentTenant`, `ICurrentUser` injected into handlers | Building blocks |

**Status:** Fully integrated. No gaps identified for current scope.

### 2.2 Service Catalog — ✅ Referenced (Loose Coupling)

**Integration type:** String-based `ServiceId` / `ServiceName` references (no direct API calls)

| Entity | Field | Type | Notes |
|---|---|---|---|
| `IncidentRecord` | `ServiceId`, `ServiceName` | `string` | Denormalized name for display |
| `AutomationWorkflowRecord` | `ServiceId` | `string?` | Optional target service |
| `CostRecord` | `ServiceId`, `ServiceName` | `string` | Denormalized name |
| `ReliabilitySnapshot` | `ServiceId` | `Guid` | Typed ID reference |
| `RuntimeSnapshot` | `ServiceName` | `string` | Name only, no ID |

**Gaps:**
- ❌ No API integration — service metadata (owner, team, criticality) is not fetched at query time.
- ❌ Inconsistent reference type — `Guid` in ReliabilitySnapshot vs `string` in others.
- ❌ No integration event consumption for service renames/deletions.
- ❌ Denormalized `ServiceName` may become stale.

**Recommendation:** Consume `ServiceRenamed` / `ServiceDeleted` integration events to keep denormalized data consistent.

### 2.3 Change Governance — ✅ Referenced (Incident Correlation)

**Integration type:** Correlation data on `IncidentRecord`

| Entity | Field | Type | Notes |
|---|---|---|---|
| `IncidentRecord` | `HasCorrelation` | `bool` | Flag indicating change correlation |
| `IncidentRecord` | `CorrelationConfidence` | `enum` | High / Medium / Low / None |
| `IncidentRecord` | `CorrelatedChanges` | JSON | Array of correlated change references |
| `AutomationWorkflowRecord` | `ChangeId` | `string?` | Optional linked change |

**Service:** `IncidentCorrelationService` performs correlation logic.

**Gaps:**
- ❌ No real-time event-driven correlation — correlation is computed at incident creation time only.
- ❌ No API call to Change Governance to validate/enrich correlation data.
- ❌ `CorrelatedChanges` is a JSON blob — no typed contract for change references.

### 2.4 Environment Management — ⚠️ Referenced (Inconsistent)

**Integration type:** `EnvironmentId` / `Environment` fields on entities (no API integration)

| Entity | Field | Type | Notes |
|---|---|---|---|
| `IncidentRecord` | `EnvironmentId` | `Guid?` | Phase 4 addition, nullable |
| `RuntimeSnapshot` | `Environment` | `string` | Name-based |
| `ReliabilitySnapshot` | `Environment` | `string` | Name-based |
| `AutomationWorkflowRecord` | `TargetEnvironment` | `string?` | Name-based, nullable |
| `CostRecord` | `Environment` | `string?` | Name-based, nullable |

**Gaps:**
- ❌ **Inconsistent typing** — `Guid?` on IncidentRecord vs `string` on all others.
- ❌ No validation against Environment Management module.
- ❌ No integration events consumed for environment lifecycle changes.
- ❌ No environment-scoped queries at API level.

---

## 3. NOT Integrated Dependencies (Critical Gaps)

### 3.1 Notifications — ❌ NOT Integrated (Critical Gap)

**Impact:** No alerting for operational events.

| Missing Integration | Priority | Impact |
|---|---|---|
| Incident created/escalated notification | P1 | On-call team not alerted |
| Automation approval requested notification | P1 | Approvers not notified |
| Automation execution completed/failed notification | P1 | Operators not informed |
| Reliability score degradation alert | P2 | SLO breach undetected |
| Cost anomaly notification | P2 | Budget overrun undetected |

**Required:** Publish integration events that the Notifications module can consume:
- `IncidentCreatedEvent`, `IncidentEscalatedEvent`
- `AutomationApprovalRequestedEvent`, `AutomationExecutionCompletedEvent`
- `ReliabilityScoreDegradedEvent`
- `CostAnomalyDetectedEvent`

### 3.2 Audit & Compliance — ❌ NOT Integrated (Critical Gap)

**Impact:** No central audit trail for operational actions.

| Missing Integration | Priority | Impact |
|---|---|---|
| Automation audit → central audit log | P1 | Compliance gap |
| Incident lifecycle → central audit log | P1 | No traceability |
| Cost import → central audit log | P2 | No import tracking |
| Mitigation actions → central audit log | P1 | No accountability |

**Current state:** `AutomationAuditRecord` exists in `AutomationDbContext` but is not forwarded to the central Audit & Compliance module. All other subdomains (Incidents, Cost, Runtime) have **no audit records at all**.

**Required:** Publish `AuditableActionOccurredEvent` for all state-changing operations.

### 3.3 AI & Knowledge — ❌ NOT Integrated (Future)

**Impact:** No AI-assisted recommendations.

| Future Integration | Priority | Use Case |
|---|---|---|
| AI-suggested incident correlation | P3 | Improve correlation confidence |
| AI-generated mitigation recommendations | P3 | Accelerate MTTR |
| AI-driven automation suggestions | P3 | Propose runbook steps |
| Knowledge base enrichment | P3 | Link incidents to known issues |

**Status:** Planned for future phases. No blocking dependency.

### 3.4 Integrations Module — ❌ NOT Integrated (Future)

**Impact:** No external tool connectivity.

| Future Integration | Priority | Use Case |
|---|---|---|
| PagerDuty / OpsGenie sync | P2 | Bidirectional incident sync |
| Jira / ServiceNow ticketing | P2 | Auto-create tickets from incidents |
| Prometheus / Datadog metrics | P2 | Real-time RuntimeSnapshot data |
| AWS CUR / Azure Cost API | P2 | Automated cost import |

---

## 4. What This Module Exposes to Others

### 4.1 Data Contracts (Available for Consumption)

| Data | Consumer Modules | Mechanism |
|---|---|---|
| Operational state (incident status, count) | Executive dashboards, Home | API / future events |
| Reliability scores (OverallScore, trend) | Service Catalog, Home | API / future events |
| Incident data (severity, status, correlation) | Change Governance (blast radius) | API / future events |
| Runtime health (HealthStatus) | Service Catalog (service health) | API / future events |
| Cost data (per-service, per-team) | FinOps dashboards | API |
| Automation status | Platform admin views | API |

### 4.2 Integration Events (Should Publish)

| Event | Status | Consumer |
|---|---|---|
| `IncidentCreatedEvent` | ❌ Not published | Notifications, Change Governance |
| `IncidentResolvedEvent` | ❌ Not published | Notifications, Audit |
| `ReliabilityScoreChangedEvent` | ❌ Not published | Service Catalog, Home |
| `AutomationExecutedEvent` | ❌ Not published | Audit, Notifications |
| `CostAnomalyDetectedEvent` | ❌ Not published | Notifications, FinOps |

---

## 5. What This Module Consumes from Others

| Data | Source Module | Mechanism | Status |
|---|---|---|---|
| Service info (name, owner, team) | Service Catalog | String reference (denormalized) | ⚠️ Stale risk |
| Change data (for correlation) | Change Governance | JSON blob on IncidentRecord | ⚠️ No live enrichment |
| Environment config (name, ID) | Environment Management | Field reference (inconsistent) | ⚠️ Inconsistent typing |
| User identity + permissions | Identity & Access | JWT + RLS | ✅ Integrated |
| Tenant context | Identity & Access | `ICurrentTenant` + RLS | ✅ Integrated |

---

## 6. Dependency Corrections Backlog

| ID | Gap | Priority | Effort | Area |
|---|---|---|---|---|
| DEP-01 | Integrate with Notifications (publish events) | P1 | Medium | Backend |
| DEP-02 | Integrate with Audit & Compliance (publish audit events) | P1 | Medium | Backend |
| DEP-03 | Standardize EnvironmentId typing (all Guid) | P2 | Medium | Backend |
| DEP-04 | Standardize ServiceId typing (all strongly-typed) | P2 | Medium | Backend |
| DEP-05 | Consume ServiceRenamed/Deleted events | P2 | Small | Backend |
| DEP-06 | Consume EnvironmentLifecycle events | P3 | Small | Backend |
| DEP-07 | Publish IncidentCreated/Resolved events | P1 | Small | Backend |
| DEP-08 | Publish ReliabilityScoreChanged events | P2 | Small | Backend |
| DEP-09 | Enrich change correlation via API or events | P2 | Medium | Backend |
| DEP-10 | Plan Integrations module connectivity (PagerDuty, etc.) | P3 | Large | Backend/Infra |

---

## 7. References

| Artifact | Path |
|---|---|
| IncidentRecord entity | `src/modules/operationalintelligence/.../Incidents/Entities/IncidentRecord.cs` |
| AutomationWorkflowRecord entity | `src/modules/operationalintelligence/.../Automation/Entities/AutomationWorkflowRecord.cs` |
| CostRecord entity | `src/modules/operationalintelligence/.../Cost/Entities/CostRecord.cs` |
| ReliabilitySnapshot entity | `src/modules/operationalintelligence/.../Reliability/Entities/ReliabilitySnapshot.cs` |
| RuntimeSnapshot entity | `src/modules/operationalintelligence/.../Runtime/Entities/RuntimeSnapshot.cs` |
| TenantRlsInterceptor | `src/building-blocks/.../Interceptors/TenantRlsInterceptor.cs` |
| Module scope finalization | `docs/11-review-modular/06-operational-intelligence/module-scope-finalization.md` |
| Module consolidated review | `docs/11-review-modular/06-operational-intelligence/module-consolidated-review.md` |
