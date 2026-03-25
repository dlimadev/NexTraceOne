# Audit & Compliance — Module Role Finalization

> **Module:** Audit & Compliance  
> **Prefix:** `aud_`  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Role Definition

The **Audit & Compliance** module is the **transversal foundation of traceability, integrity and compliance** for the entire NexTraceOne platform.

It is responsible for:

- Receiving, persisting and making available **every auditable event** produced by any module in the platform
- Maintaining a **cryptographic hash chain** (SHA-256) that guarantees event immutability and non-repudiation
- Providing **compliance policies, audit campaigns and compliance evaluations** to support regulatory and internal governance requirements
- Offering **retention policies** for data lifecycle management
- Serving as the **single source of truth** for "who did what, when, and with what outcome" across all modules

### Positioning

Audit & Compliance is not an optional add-on. It is a **core infrastructure module** that every other module depends on for:

- Regulatory compliance evidence
- Operational audit trail
- Change traceability
- Security event logging
- Incident evidence preservation

---

## 2. Ownership Confirmation

| Responsibility | Owner? | Notes |
|---|---|---|
| Audit event storage and retrieval | ✅ YES | `AuditEvent` aggregate root, `RecordAuditEvent` command |
| Immutable audit trail | ✅ YES | Events are append-only; `AuditEvent.Record()` factory enforces immutability |
| Hash chain integrity | ✅ YES | `AuditChainLink` entity with SHA-256 chain; `VerifyChainIntegrity` query |
| Evidence linked to changes/approvals | ✅ YES | Via `ResourceType`/`ResourceId` correlation to Change Governance entities |
| Verifiable history | ✅ YES | `GetAuditTrail`, `SearchAuditLog`, `ExportAuditReport` queries |
| Compliance policies | ✅ YES | `CompliancePolicy` entity with CRUD |
| Audit campaigns | ✅ YES | `AuditCampaign` entity with lifecycle (Planned→InProgress→Completed/Cancelled) |
| Compliance evaluation results | ✅ YES | `ComplianceResult` entity linked to policies and campaigns |
| Retention management | ✅ YES | `RetentionPolicy` entity (partial — `ConfigureRetention` is placeholder) |
| Compliance reporting | ✅ YES | `GetComplianceReport` query with module breakdown |

---

## 3. What the Module Must NOT Own

| Responsibility | Correct Owner | Reason |
|---|---|---|
| Generating auditable actions | Each source module | Audit is a consumer, not a producer of business actions |
| Authentication / authorization | Identity & Access | Audit trusts the identity context provided by IAM |
| Security event classification | Identity & Access | `SecurityEvent` entity lives in Identity module |
| Change lifecycle management | Change Governance | Audit records change events but does not manage change state |
| Incident management | Operational Intelligence | Audit records incident events but does not own incident lifecycle |
| Notification routing | Notifications | Audit may trigger events that Notifications consumes |
| Service catalog data | Catalog | Audit references services by ID but does not own service definitions |
| Configuration management | Configuration | Audit records config changes but does not own configuration state |

---

## 4. Modules That Feed Audit & Compliance

| Source Module | Events Produced | Integration Status |
|---|---|---|
| **Identity & Access** | Login, permission changes, role assignments, delegation, break-glass, JIT access | ⚠️ SecurityAuditBridge exists but systematic validation pending |
| **Change Governance** | Change requests, approvals, deployments, rollbacks | ❌ Not confirmed — audit event publication not validated |
| **Operational Intelligence** | Incident creation, automation execution, automation approval | ❌ Not integrated |
| **Catalog** | Service creation/update, dependency changes | ❌ Not confirmed |
| **Configuration** | Configuration value changes | ❌ Not confirmed |
| **Governance** | Report generation, risk assessments | ❌ Not confirmed |
| **Notifications** | Notification delivery events | ❌ Not confirmed |

**Critical gap:** Only Identity & Access has a known bridge (`SecurityAuditBridge`). All other modules need validation of audit event publication.

---

## 5. Why the Module Is Transversal and Critical

1. **Regulatory compliance**: Enterprise customers require verifiable audit trails for SOC 2, ISO 27001, GDPR and similar frameworks
2. **Non-repudiation**: The SHA-256 hash chain provides cryptographic proof that events have not been tampered with
3. **Operational trust**: Every sensitive action in the platform should be traceable
4. **Cross-module evidence**: A single incident may involve changes from Change Governance, actions from Operational Intelligence, and approvals from Identity — Audit correlates all of them
5. **Legal defensibility**: Exported audit reports serve as evidence in compliance audits

---

## 6. Key Code References

| Component | File |
|---|---|
| AuditEvent aggregate root | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/AuditEvent.cs` |
| AuditChainLink entity | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/AuditChainLink.cs` |
| Hash chain verification | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Application/Features/VerifyChainIntegrity/VerifyChainIntegrity.cs` |
| RecordAuditEvent command | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Application/Features/RecordAuditEvent/RecordAuditEvent.cs` |
| AuditDbContext | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure/Persistence/AuditDbContext.cs` |
| API endpoints | `src/modules/auditcompliance/NexTraceOne.AuditCompliance.API/Endpoints/Endpoints/AuditEndpointModule.cs` |
| Frontend page | `src/frontend/src/features/audit-compliance/pages/AuditPage.tsx` |
| Frontend API client | `src/frontend/src/features/audit-compliance/api/audit.ts` |
