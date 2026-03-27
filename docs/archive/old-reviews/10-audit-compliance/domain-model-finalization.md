# Audit & Compliance — Domain Model Finalization

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Aggregate Root (1)

### AuditEvent

- **File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/AuditEvent.cs`
- **ID Type:** `AuditEventId` (strongly typed)
- **Properties:**
  - `SourceModule` (string, max 200) — module that produced the event (e.g., "identity", "change-governance")
  - `ActionType` (string, max 200) — action performed (e.g., "login", "approval", "deployment")
  - `ResourceId` (string, max 500) — affected resource identifier
  - `ResourceType` (string, max 200) — type of affected resource (e.g., "user", "release", "workflow")
  - `PerformedBy` (string, max 500) — actor who performed the action
  - `OccurredAt` (DateTimeOffset, UTC) — event timestamp
  - `TenantId` (Guid) — tenant context
  - `Payload` (string?, text) — serialised additional details (JSON)
- **Relationships:**
  - `ChainLink` (one-to-one, optional) → `AuditChainLink`
- **Factory:** `Record()` — creates immutable event with guard clauses
- **Methods:** `LinkToChain(AuditChainLink)` — binds event to hash chain element
- **Immutability:** No setter methods after creation (application-level enforcement)

---

## 2. Entities (5)

### 2.1 AuditChainLink

- **File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/AuditChainLink.cs`
- **ID Type:** `AuditChainLinkId` (strongly typed)
- **Properties:**
  - `SequenceNumber` (long) — sequential number in chain
  - `CurrentHash` (string, max 128) — SHA-256 hash of this link
  - `PreviousHash` (string, max 128) — SHA-256 hash of previous link
  - `CreatedAt` (DateTimeOffset, UTC) — creation timestamp
- **Factory:** `Create()` — computes hash from event data + sequence + previous hash
- **Methods:** `Verify(AuditEvent, previousHash)` — validates hash integrity
- **Hash Algorithm:** SHA-256
- **Hash Input Format:** `{sequence}|{eventId}|{sourceModule}|{actionType}|{resourceId}|{performedBy}|{occurredAt}|{previousHash}`

### 2.2 CompliancePolicy

- **File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/CompliancePolicy.cs`
- **ID Type:** `CompliancePolicyId` (strongly typed)
- **Properties:**
  - `Name` (string, max 200), `DisplayName` (string, max 300), `Description` (string?, text)
  - `Category` (string, max 100) — "Security", "DataProtection", "Operational", "Governance"
  - `Severity` (ComplianceSeverity enum) — Low, Medium, High, Critical
  - `IsActive` (bool, default: true)
  - `EvaluationCriteria` (string?, text) — JSON evaluation rules
  - `TenantId` (Guid), `CreatedAt`, `UpdatedAt`
- **Factory:** `Create()` — creates active policy
- **Methods:** `Update()`, `Activate()`, `Deactivate()`

### 2.3 ComplianceResult

- **File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/ComplianceResult.cs`
- **ID Type:** `ComplianceResultId` (strongly typed)
- **Properties:**
  - `PolicyId` (CompliancePolicyId) — evaluated policy
  - `CampaignId` (AuditCampaignId?, nullable) — optional campaign context
  - `ResourceType` (string, max 200), `ResourceId` (string, max 500)
  - `Outcome` (ComplianceOutcome enum) — Compliant, NonCompliant, PartiallyCompliant, NotApplicable
  - `Details` (string?, text) — evaluation details (JSON)
  - `EvaluatedBy` (string, max 200), `EvaluatedAt` (DateTimeOffset), `TenantId` (Guid)
- **Relationships:** FK to CompliancePolicy, optional FK to AuditCampaign

### 2.4 AuditCampaign

- **File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/AuditCampaign.cs`
- **ID Type:** `AuditCampaignId` (strongly typed)
- **Properties:**
  - `Name` (string, max 200), `Description` (string?, text)
  - `CampaignType` (string, max 100) — "Periodic", "AdHoc", "Regulatory"
  - `Status` (CampaignStatus enum) — Planned, InProgress, Completed, Cancelled
  - `ScheduledStartAt`, `StartedAt`, `CompletedAt` (DateTimeOffset?, UTC)
  - `TenantId` (Guid), `CreatedBy` (string, max 200), `CreatedAt`
- **State Machine:** Planned → InProgress → Completed (or Cancelled from Planned|InProgress)
- **Methods:** `Start()`, `Complete()`, `Cancel()`

### 2.5 RetentionPolicy

- **File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Domain/Entities/RetentionPolicy.cs`
- **ID Type:** `RetentionPolicyId` (strongly typed)
- **Properties:**
  - `Name` (string, max 200)
  - `RetentionDays` (int, range 1–3650)
  - `IsActive` (bool, default: true)
- **Factory:** `Create()` — validates RetentionDays > 0
- **Methods:** `UpdateRetention()`, `Deactivate()`

---

## 3. Enums (3)

| Enum | Values | File |
|------|--------|------|
| ComplianceSeverity | Low, Medium, High, Critical | `Domain/Enums/ComplianceEnums.cs` |
| CampaignStatus | Planned, InProgress, Completed, Cancelled | `Domain/Enums/ComplianceEnums.cs` |
| ComplianceOutcome | Compliant, NonCompliant, PartiallyCompliant, NotApplicable | `Domain/Enums/ComplianceEnums.cs` |

---

## 4. Domain Events (2)

| Event | Properties | Purpose |
|-------|-----------|---------|
| `AuditEventRecordedEvent` | AuditEventId, EventType, Actor, RecordedAt | Published when audit event is recorded |
| `AuditIntegrityCheckpointCreatedEvent` | CheckpointId, PeriodFrom, PeriodTo, Hash, CreatedAt | Published when hash chain checkpoint is created |

---

## 5. Domain Errors (5)

| Error | Code | Context |
|-------|------|---------|
| `EventNotFound(eventId)` | `Audit.Event.NotFound` | Query for non-existent event |
| `ChainIntegrityViolation(seq)` | `Audit.Chain.IntegrityViolation` | Hash mismatch detected during verification |
| `RetentionPolicyNotFound(id)` | `Audit.RetentionPolicy.NotFound` | Retention policy lookup |
| `CompliancePolicyNotFound(id)` | `Audit.CompliancePolicy.NotFound` | Policy lookup |
| `CampaignNotFound(id)` | `Audit.Campaign.NotFound` | Campaign lookup |

---

## 6. Cross-Module References

| Reference | From (Audit) | To (External Module) | Type |
|-----------|-------------|---------------------|------|
| `TenantId` | All entities | Identity & Access | Security context |
| `SourceModule` (string) | AuditEvent | Any module | Loose coupling — module name as string |
| `PerformedBy` (string) | AuditEvent | Identity & Access | Loose coupling — user identifier as string |
| `ResourceId` (string) | AuditEvent | Any module | Loose coupling — resource ID as string |

**Note:** Audit & Compliance uses **loose coupling by design** — all cross-module references are strings, not strongly typed IDs. This avoids compile-time dependencies on other modules.

---

## 7. Domain Model Gaps

| Gap | Description | Impact | Priority |
|-----|-------------|--------|----------|
| G-01 | No `RowVersion` / `ConcurrencyToken` on mutable entities (CompliancePolicy, AuditCampaign, RetentionPolicy) | Concurrent updates can silently overwrite data | P1 |
| G-02 | `CampaignType` is a string, not an enum | Weak typing allows arbitrary campaign types | P2 |
| G-03 | `EvaluationCriteria` is a raw string with no validation | No enforcement of evaluation rule structure | P2 |
| G-04 | No `EnvironmentId` on `AuditEvent` | Cannot scope audit trail by environment | P1 |
| G-05 | No explicit link to originating outbox event ID | Cannot trace audit event back to the outbox message that triggered it | P2 |
| G-06 | `RetentionPolicy` has no `TenantId` | Retention policies are global, not tenant-scoped | P2 |
| G-07 | No value objects (e.g., AuditPayload, HashValue could be VOs) | DDD best practice gap | P3 |
| G-08 | `AuditEvent` has no `EnvironmentId` field | Cannot filter audit trail by environment | P1 |

---

## 8. Final Domain Model Target

The domain model is **well-structured** for a transversal module with 6 entities, real hash chain implementation, and proper factory patterns. Key improvements needed:

1. Add `EnvironmentId` to `AuditEvent` (P1) — critical for environment-scoped audit
2. Add `RowVersion` to mutable entities (P1) — concurrency safety
3. Add `TenantId` to `RetentionPolicy` (P2) — tenant-scoped retention
4. Convert `CampaignType` to enum (P2) — type safety
5. Add `CorrelationId` field to `AuditEvent` (P2) — trace back to originating event
