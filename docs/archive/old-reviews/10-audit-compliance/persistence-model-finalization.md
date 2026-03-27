# Audit & Compliance — Persistence Model Finalization

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Target prefix:** `aud_`

---

## 1. Current Database Architecture

### 1.1 DbContext

| DbContext | DbSets | Outbox Table | Migrations |
|-----------|--------|--------------|-----------|
| `AuditDbContext` | 6 (AuditEvent, AuditChainLink, RetentionPolicy, CompliancePolicy, AuditCampaign, ComplianceResult) | `aud_outbox_messages` | 2 |

**File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure/Persistence/AuditDbContext.cs`

`AuditDbContext` inherits from `NexTraceDbContextBase`, which provides:
- `TenantRlsInterceptor` — PostgreSQL RLS for tenant isolation
- `AuditInterceptor` — `CreatedAt`, `UpdatedAt`, `CreatedBy` columns
- `EncryptionInterceptor` — AES-256-GCM field encryption
- `OutboxInterceptor` — Outbox pattern for event-driven integration

### 1.2 Migrations

| Migration | Date | Tables Created |
|-----------|------|---------------|
| `20260321160432_InitialCreate` | 2026-03-21 | aud_audit_events, aud_audit_chain_links, aud_retention_policies |
| `20260322160000_Phase3ComplianceDomain` | 2026-03-22 | aud_compliance_policies, aud_campaigns, aud_compliance_results |

---

## 2. Current Table Mappings

### 2.1 Tables (all with `aud_` prefix)

| Entity | Table | Configuration File |
|--------|-------|-------------------|
| AuditEvent | `aud_audit_events` | Entity configuration in `Persistence/Configurations/` |
| AuditChainLink | `aud_audit_chain_links` | Entity configuration in `Persistence/Configurations/` |
| CompliancePolicy | `aud_compliance_policies` | Entity configuration in `Persistence/Configurations/` |
| ComplianceResult | `aud_compliance_results` | Entity configuration in `Persistence/Configurations/` |
| AuditCampaign | `aud_campaigns` | Entity configuration in `Persistence/Configurations/` |
| RetentionPolicy | `aud_retention_policies` | Entity configuration in `Persistence/Configurations/` |

✅ **All tables already use the `aud_` prefix** — no prefix migration needed.

---

## 3. Current Indexes

| Table | Index Column(s) | Type |
|-------|----------------|------|
| `aud_audit_events` | `TenantId` | Non-unique |
| `aud_audit_events` | `OccurredAt` | Non-unique |
| `aud_audit_events` | `SourceModule` | Non-unique |
| `aud_audit_events` | `ActionType` | Non-unique |
| `aud_audit_events` | `PerformedBy` | Non-unique |
| `aud_audit_chain_links` | `SequenceNumber` | **UNIQUE** |
| `aud_audit_chain_links` | `CurrentHash` | **UNIQUE** |
| `aud_compliance_policies` | `TenantId` | Non-unique |
| `aud_compliance_policies` | `IsActive` | Non-unique |
| `aud_compliance_policies` | `Category` | Non-unique |
| `aud_compliance_policies` | `Severity` | Non-unique |
| `aud_compliance_results` | `TenantId` | Non-unique |
| `aud_compliance_results` | `PolicyId` | Non-unique |
| `aud_compliance_results` | `CampaignId` | Non-unique |
| `aud_compliance_results` | `Outcome` | Non-unique |
| `aud_compliance_results` | `EvaluatedAt` | Non-unique |
| `aud_campaigns` | `TenantId` | Non-unique |
| `aud_campaigns` | `Status` | Non-unique |
| `aud_campaigns` | `CampaignType` | Non-unique |
| `aud_retention_policies` | `IsActive` | Non-unique |

**Total: 20 indexes** — good coverage for common query patterns.

---

## 4. Foreign Key Constraints

| FK | From Table | To Table | Cascade |
|----|-----------|----------|---------|
| AuditEvent → AuditChainLink | `aud_audit_events` | `aud_audit_chain_links` | CASCADE DELETE (optional FK) |
| ComplianceResult → CompliancePolicy | `aud_compliance_results` | `aud_compliance_policies` | (FK exists) |
| ComplianceResult → AuditCampaign | `aud_compliance_results` | `aud_campaigns` | (optional FK) |

---

## 5. Audit Columns

All entities inherit from `AuditableEntity` via `NexTraceDbContextBase`:

| Column | Type | Source |
|--------|------|--------|
| `CreatedAt` | `DateTimeOffset` | `AuditInterceptor` |
| `UpdatedAt` | `DateTimeOffset` | `AuditInterceptor` |
| `CreatedBy` | `string` | `AuditInterceptor` |

---

## 6. Outbox Table

| Outbox Table | DbContext | Pattern |
|-------------|-----------|---------|
| `aud_outbox_messages` | AuditDbContext | `OutboxInterceptor` |

---

## 7. Hash Chain Persistence

| Aspect | Detail |
|--------|--------|
| Table | `aud_audit_chain_links` |
| Unique sequence | `SequenceNumber` (UNIQUE index) ensures no duplicate sequence |
| Unique hash | `CurrentHash` (UNIQUE index) ensures no duplicate hashes |
| Hash algorithm | SHA-256 |
| Hash input | `{sequence}|{eventId}|{sourceModule}|{actionType}|{resourceId}|{performedBy}|{occurredAt}|{previousHash}` |
| First link | `PreviousHash` = empty string for sequence 0 |
| Integrity | Verified by `VerifyChainIntegrity.Query` traversing all links and recomputing hashes |

---

## 8. Evidence Persistence

Currently, evidence is **not explicitly modelled** in Audit & Compliance. The module stores:
- Audit events with `Payload` (text field, JSON) — can contain evidence details
- Compliance results with `Details` (text field, JSON) — can contain evaluation evidence

There is **no dedicated evidence table** and **no link to Change Governance's `EvidencePack`**.

---

## 9. Retention Persistence

- `aud_retention_policies` table exists with `Name`, `RetentionDays`, `IsActive`
- **No enforcement mechanism** — no scheduled job to purge events older than retention period
- **No `TenantId`** on RetentionPolicy — policies are global, not tenant-scoped

---

## 10. Missing Constraints and Gaps

| Gap | Table(s) | Description | Priority |
|-----|----------|-------------|----------|
| PC-01 | CompliancePolicy, AuditCampaign, RetentionPolicy | No `RowVersion` / `ConcurrencyToken` (xmin) on mutable entities | P1 |
| PC-02 | `aud_audit_events` | No `EnvironmentId` column — cannot scope by environment | P1 |
| PC-03 | `aud_retention_policies` | No `TenantId` — retention is global, not per-tenant | P2 |
| PC-04 | `aud_audit_events` | No composite index on `(TenantId, OccurredAt)` for efficient tenant-scoped time queries | P2 |
| PC-05 | `aud_audit_events` | No composite index on `(TenantId, SourceModule)` for module-scoped queries | P2 |
| PC-06 | `aud_audit_events` | No immutability enforcement at DB level (e.g., DENY UPDATE trigger) | P2 |
| PC-07 | `aud_compliance_results` | No composite index on `(PolicyId, Outcome)` for policy compliance summary | P3 |
| PC-08 | `aud_campaigns` | No CHECK constraint on status transitions | P3 |

---

## 11. Pre-conditions for New Migrations

1. Add `EnvironmentId` to `AuditEvent` domain model
2. Add `TenantId` to `RetentionPolicy` domain model
3. Add `RowVersion` to mutable entities
4. Add composite indexes for common query patterns
5. Decide on DB-level immutability enforcement (triggers vs. application-only)
6. Decide on evidence table (dedicated `aud_evidence` or continue using Payload/Details fields)
7. Add `CorrelationId` to `AuditEvent` for cross-module event tracing

---

## 12. Divergences Between Current and Target

| Aspect | Current | Target |
|--------|---------|--------|
| Table prefix | ✅ `aud_` | ✅ `aud_` (no change needed) |
| EnvironmentId | ❌ Missing | Add to `aud_audit_events` |
| RowVersion | ❌ Missing | Add to mutable entities |
| Retention TenantId | ❌ Missing | Add to `aud_retention_policies` |
| Composite indexes | ❌ Missing | Add for common query patterns |
| Immutability enforcement | Application-only | Consider DB-level triggers |
| Evidence table | No dedicated table | Evaluate need for `aud_evidence` |
