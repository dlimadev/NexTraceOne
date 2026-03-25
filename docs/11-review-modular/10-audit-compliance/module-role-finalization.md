# Audit & Compliance — Module Role Finalization

> **Module:** 10 — Audit & Compliance  
> **Prefix:** `aud_`  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Maturity:** 53% (Backend 80%, Frontend 40%)

---

## 1. Official Role Definition

**Audit & Compliance is the transversal, foundational module responsible for providing an immutable, cryptographically verifiable audit trail across the entire NexTraceOne platform, combined with compliance policy management, audit campaigns, retention governance, and evidence-grade reporting.**

It materialises:

- **Immutable Audit Trail** — every auditable action from every module is recorded as an `AuditEvent` with full actor/tenant/resource context
- **Cryptographic Integrity** — SHA-256 hash chain (`AuditChainLink`) ensures tamper detection across the entire event log
- **Compliance Policies** — configurable policies with severity, category, and evaluation criteria, validated against resources
- **Audit Campaigns** — structured audit exercises (periodic, ad-hoc, regulatory) that group compliance evaluations
- **Retention Management** — configurable retention periods for audit data lifecycle
- **Evidence-Grade Reporting** — exportable audit reports and compliance reports with chain integrity verification

---

## 2. Why Audit & Compliance Is Transversal and Critical

| Aspect | Description |
|--------|-------------|
| **Regulatory Foundation** | Required for SOC 2, ISO 27001, GDPR, and enterprise compliance. Without it, NexTraceOne cannot position itself as an enterprise platform |
| **Cross-Module Scope** | Every module in NexTraceOne is a potential producer of audit events. Audit & Compliance is the single consumer and source of truth |
| **Tamper Detection** | The SHA-256 hash chain provides cryptographic proof that no events have been modified or deleted after recording |
| **Trust Anchor** | Change Governance decisions, Identity security events, and Operational incidents all derive their auditability from this module |
| **Evidence for Approvals** | Workflow approvals, gate overrides, and promotion decisions in Change Governance are only trustworthy if their audit trail is immutable |

---

## 3. What the Module Owns

### 3.1 Owned Entities (6)

| Entity | Type | Table | Description |
|--------|------|-------|-------------|
| `AuditEvent` | Aggregate Root | `aud_audit_events` | Core immutable event record with source module, action type, resource, actor, tenant, payload |
| `AuditChainLink` | Entity | `aud_audit_chain_links` | SHA-256 hash chain link with sequence number, current hash, previous hash |
| `CompliancePolicy` | Entity | `aud_compliance_policies` | Compliance policy definition with name, category, severity, evaluation criteria |
| `ComplianceResult` | Entity | `aud_compliance_results` | Compliance evaluation result linked to policy and optional campaign |
| `AuditCampaign` | Entity | `aud_campaigns` | Structured audit exercise with lifecycle (Planned → InProgress → Completed/Cancelled) |
| `RetentionPolicy` | Entity | `aud_retention_policies` | Retention period configuration for audit data |

### 3.2 Owned Capabilities

- Recording immutable audit events from any module via `IAuditModule` interface
- SHA-256 hash chain computation and linking for every audit event
- Hash chain integrity verification (full chain traversal and hash recomputation)
- Compliance policy CRUD (create, list, get, activate, deactivate)
- Compliance result recording with policy and campaign linkage
- Audit campaign lifecycle management (Planned → InProgress → Completed/Cancelled)
- Retention policy configuration (1–3650 days)
- Audit log search with filtering by module, action type, date range
- Audit trail retrieval by resource type and ID
- Audit report export for time periods
- Compliance report with chain integrity and module breakdown
- Domain events for integration (`AuditEventRecordedEvent`, `AuditIntegrityCheckpointCreatedEvent`)

### 3.3 Owned API Surface (15 endpoints)

- 6 audit trail endpoints (record, trail, search, verify chain, export report, compliance report)
- 3 compliance policy endpoints (create, list, get)
- 3 campaign endpoints (create, list, get)
- 2 compliance result endpoints (record, list)
- 1 retention configuration endpoint (placeholder)

### 3.4 Cross-Module Integration Contract

**File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Contracts/ServiceInterfaces/IAuditModule.cs`

```csharp
public interface IAuditModule
{
    Task RecordEventAsync(string sourceModule, string actionType,
        string resourceId, string resourceType, string performedBy,
        Guid tenantId, string? payload, CancellationToken cancellationToken);
    Task<bool> VerifyChainIntegrityAsync(CancellationToken cancellationToken);
}
```

This contract is the **primary interface** for all other modules to send audit events.

---

## 4. What the Module Does NOT Own

| Responsibility | Belongs To | Reason |
|---------------|------------|--------|
| Application logging (structured logs, traces, metrics) | **Platform / Infrastructure** | Audit events are business-level actions, not technical logs |
| Security event detection and risk scoring | **Identity & Access** | Identity owns `SecurityEvent` entities with risk scoring (0-100); Audit receives them via `SecurityAuditBridge` |
| Approval workflow decisions | **Change Governance** | Change Governance owns the approval lifecycle; Audit records the evidence |
| Incident lifecycle management | **Operational Intelligence** | OI owns incidents; Audit records incident-related actions |
| Executive dashboards and analytics | **Governance** | Governance owns reporting and dashboards; Audit provides the raw data |
| ClickHouse analytics pipeline | **Platform / Cross-cutting** | Audit stores in PostgreSQL; analytics projection to ClickHouse is platform-level |
| Notification delivery | **Notifications** | Audit may trigger compliance notifications but does not deliver them |

---

## 5. Why Audit & Compliance Must Not Be Confused With

### 5.1 Not Generic Logging

Application logging (Serilog, OpenTelemetry traces) is technical infrastructure for debugging and monitoring. Audit events are **business-level records** of who did what, when, and to which resource. They are immutable, hash-chained, and designed for regulatory evidence. Logging is ephemeral; audit is permanent.

### 5.2 Not Generic Reporting

The Governance module owns executive dashboards, product analytics, and cross-module reporting. Audit & Compliance provides **evidence-grade data** — the raw, tamper-proof record that feeds into governance reports. Audit owns the truth; Governance owns the presentation.

### 5.3 Not Security Events Alone

Identity & Access owns `SecurityEvent` entities with risk scoring, failed login tracking, and access anomaly detection. These are security-domain concepts. Audit & Compliance receives security events via `SecurityAuditBridge` as one of many event sources, but does not define or manage security policy.

### 5.4 Not Change Governance Evidence Packs

Change Governance generates `EvidencePack` entities for workflow approval decisions. These are workflow-specific artefacts. Audit & Compliance provides the **underlying immutable record** that confirms the evidence pack is trustworthy.

---

## 6. Summary

Audit & Compliance is a **transversal, foundational module** with 6 domain entities, 15 API endpoints, SHA-256 hash chain integrity, and a well-defined cross-module integration contract (`IAuditModule`). The backend is mature (80%) with real implementation, but the frontend is minimal (1 page out of 4+ needed). The module is the **trust anchor** for the entire NexTraceOne platform — without it, no other module's actions can be considered verifiable for compliance purposes.
