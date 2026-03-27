# Audit & Compliance — Module Dependency Map

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Dependencies Overview

```
    ┌──────────────────┐
    │ Identity & Access │──────────────────────┐
    │  (SecurityEvent   │                      │
    │   SecurityAudit   │                      ▼
    │   Bridge)         │           ┌──────────────────────┐
    └──────────────────┘           │  AUDIT & COMPLIANCE   │
                                   │                       │
    ┌──────────────────┐           │  - Audit Trail        │
    │ Change Governance │──(❌)──▶ │  - Hash Chain         │
    │  (approvals,      │          │  - Compliance         │
    │   overrides,      │          │  - Campaigns          │
    │   gate decisions) │          │  - Retention          │
    └──────────────────┘           │  - Evidence           │
                                   └───────────┬───────────┘
    ┌──────────────────┐                       │
    │ Operational Intel │──(❌)──▶              │ exposes
    │  (incidents,      │                      ▼
    │   mitigations)    │          ┌──────────────────────┐
    └──────────────────┘           │  Governance           │
                                   │  (compliance reports, │
    ┌──────────────────┐           │   executive views)    │
    │ Catalog           │──(❌)──▶ └──────────────────────┘
    │  (API changes)    │
    └──────────────────┘

    (❌) = Integration NOT yet wired
    (✅) = Integration confirmed
```

---

## 2. Inbound Dependencies (What Audit & Compliance Consumes)

### 2.1 Identity & Access → Audit & Compliance ✅ CONFIRMED

| Dependency | Type | Usage | Criticality |
|-----------|------|-------|-------------|
| SecurityEvents | Event bridge | `SecurityAuditBridge.PropagateAsync()` translates `SecurityEvent` to audit event via `IAuditModule.RecordEventAsync()` | 🔴 High |
| Pipeline behaviour | MediatR | `SecurityEventAuditBehavior` propagates events post-handler-success | 🔴 High |
| User identity | Context | `PerformedBy` field populated from identity context | 🔴 High |
| TenantId | Context | Tenant context passed through for all events | 🔴 High |

**Files:**
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/Services/SecurityAuditBridge.cs`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Application/Behaviors/SecurityEventAuditBehavior.cs`

**Strategy:** Best-effort propagation — failures logged but do not block primary operation.

### 2.2 Change Governance → Audit & Compliance ❌ NOT WIRED

| Expected Events | Source | Status |
|----------------|--------|--------|
| Workflow approval decisions | `ApproveStage.Command` | ❌ Not publishing to Audit |
| Workflow rejection decisions | `RejectWorkflow.Command` | ❌ Not publishing to Audit |
| Gate override with justification | `OverrideGateWithJustification.Command` | ❌ Not publishing to Audit |
| Promotion approvals | `ApprovePromotion.Command` | ❌ Not publishing to Audit |
| Release creation/state changes | `NotifyDeployment.Command` | ❌ Not publishing to Audit |

**Impact:** Approval decisions and gate overrides — the most sensitive actions in Change Governance — are not recorded in the central audit trail.

### 2.3 Operational Intelligence → Audit & Compliance ❌ NOT WIRED

| Expected Events | Source | Status |
|----------------|--------|--------|
| Incident creation | Incident handlers | ❌ Not publishing to Audit |
| Incident resolution | Resolution handlers | ❌ Not publishing to Audit |
| Automation execution | Workflow handlers | ❌ Not publishing to Audit |

### 2.4 Other Modules → Audit & Compliance ❌ NOT WIRED

| Module | Expected Events | Status |
|--------|----------------|--------|
| **Catalog** | Service registration, API asset changes | ❌ Not publishing |
| **Contracts** | Contract version publication, schema changes | ❌ Not publishing |
| **Configuration** | Configuration value changes | ❌ Not publishing |
| **Governance** | Report generation, policy changes | ❌ Not publishing |
| **Notifications** | Notification delivery, channel changes | ❌ Not publishing |
| **AI & Knowledge** | Agent execution, model changes | ❌ Not publishing |
| **Environment Management** | Environment changes, criticality updates | ❌ Not publishing |

---

## 3. Outbound Dependencies (What Audit & Compliance Exposes)

### 3.1 Audit & Compliance → Governance

| Exposed Data | Type | Usage | Criticality |
|-------------|------|-------|-------------|
| Compliance reports | Query | Governance uses compliance data for executive reports | 🟡 Medium |
| Audit statistics | Query | Module breakdown, event counts for dashboards | 🟡 Medium |

### 3.2 Audit & Compliance → All Modules (via IAuditModule)

| Exposed Capability | Type | Usage | Criticality |
|-------------------|------|-------|-------------|
| `RecordEventAsync()` | Service contract | All modules can record events | 🔴 High |
| `VerifyChainIntegrityAsync()` | Service contract | Any module can verify chain | 🟡 Medium |

### 3.3 Integration Events (via Outbox)

| Event | Consumers | Status |
|-------|-----------|--------|
| `AuditEventRecordedEvent` | Governance dashboards, external integrations | ✅ Defined |
| `AuditIntegrityCheckpointCreatedEvent` | Compliance monitoring | ✅ Defined |

---

## 4. What the Module Exposes

### API Surface (15 endpoints)
All under `/api/v1/audit/` with sub-routes for events, trail, search, verify-chain, report, compliance, policies, campaigns, results.

### Service Contract
`IAuditModule` — the primary integration interface for all modules.

---

## 5. What the Module Consumes

| Source | Mechanism | Data |
|--------|-----------|------|
| Identity & Access | `SecurityAuditBridge` → `IAuditModule` | Security events (login, role change, delegation, etc.) |
| (All other modules) | Not yet wired | — |

---

## 6. Gaps in Event Production by Source Modules

This is the **most critical gap** in the entire Audit & Compliance module. The following modules need to publish events:

| Module | Priority | Effort to Wire | Key Events Needed |
|--------|----------|----------------|-------------------|
| **Change Governance** | P0 | 8h | Approvals, rejections, gate overrides, promotions |
| **Operational Intelligence** | P1 | 4h | Incident creation/resolution, automation execution |
| **Catalog** | P1 | 4h | Service registration, API asset changes |
| **Contracts** | P2 | 4h | Contract publication, schema changes |
| **Configuration** | P2 | 2h | Configuration value changes |
| **Governance** | P2 | 2h | Policy changes, report generation |
| **Notifications** | P3 | 2h | Notification delivery |
| **AI & Knowledge** | P3 | 2h | Agent execution |
| **Environment Management** | P2 | 2h | Environment changes |

**Total wiring effort:** ~30 hours across all modules.

---

## 7. Circular Dependency Analysis

| Pair | Direction | Risk | Mitigation |
|------|-----------|------|------------|
| Audit ← Identity | One-way: Identity → Audit | ✅ No circular dependency | — |
| Audit ← Change Gov | One-way: Change Gov → Audit (when wired) | ✅ No circular dependency | — |
| Audit ↔ Governance | Audit provides data; Governance reads it | ✅ No circular dependency (read-only) | — |

**Conclusion:** No circular dependencies exist or are expected. Audit & Compliance is a **pure sink** — it receives events from other modules but does not call back into them.
