# Audit & Compliance — Integrity, Retention, and Evidence Review

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Hash Chain Integrity — Current Implementation

### 1.1 How It Works

| Aspect | Detail |
|--------|--------|
| **Algorithm** | SHA-256 |
| **Entity** | `AuditChainLink` with `SequenceNumber`, `CurrentHash`, `PreviousHash`, `CreatedAt` |
| **Hash Input** | `{sequence}|{eventId}|{sourceModule}|{actionType}|{resourceId}|{performedBy}|{occurredAt}|{previousHash}` |
| **First Link** | `PreviousHash` = empty string for sequence 0 |
| **Creation** | `AuditChainLink.Create()` factory computes hash and assigns sequence |
| **Binding** | `AuditEvent.LinkToChain(AuditChainLink)` — one-to-one relationship |
| **Transaction** | Event + chain link persisted in single `IUnitOfWork.CommitAsync()` call |
| **Verification** | `VerifyChainIntegrity.Query` iterates all links, recomputes hash, compares with stored |
| **Violation Detection** | Returns `ChainViolation(sequenceNumber, reason)` for each mismatch |

### 1.2 Is the Integrity Real?

**Assessment: ✅ The hash chain is REAL — with caveats.**

| Check | Status | Details |
|-------|--------|---------|
| SHA-256 computation exists | ✅ Real | `AuditChainLink.ComputeHash()` implements SHA-256 |
| Hash includes event data | ✅ Real | Hash covers eventId, sourceModule, actionType, resourceId, performedBy, occurredAt |
| Sequential linking | ✅ Real | Each link stores `PreviousHash` from prior link |
| UNIQUE constraint on SequenceNumber | ✅ Real | Database prevents duplicate sequences |
| UNIQUE constraint on CurrentHash | ✅ Real | Database prevents hash collisions |
| Verification endpoint | ✅ Real | Full chain traversal with hash recomputation |
| Frontend verification | ✅ Real | "Verify Integrity" button on AuditPage |

**Caveats:**

| Caveat | Impact | Priority |
|--------|--------|----------|
| No DB-level immutability enforcement (triggers/policies) | Admin with DB access could modify events or hashes | P2 |
| Verification loads ALL links into memory | Will not scale for millions of events | P2 |
| No periodic automated verification (only manual via API) | Tampering could go undetected between manual checks | P1 |
| No checkpoint mechanism for partial verification | Full chain must be verified every time | P2 |
| Hash does not include `TenantId` in computation | Two tenants with identical events would have different chain positions but hash doesn't prove tenant isolation | P3 |

---

## 2. Retention — Current Implementation

### 2.1 Current State

| Aspect | Status | Details |
|--------|--------|---------|
| `RetentionPolicy` entity | ✅ Exists | `Name`, `RetentionDays` (1–3650), `IsActive` |
| Domain validation | ✅ Exists | `Create()` validates RetentionDays > 0 |
| `aud_retention_policies` table | ✅ Exists | With `IsActive` index |
| Configuration handler | ⚠️ Placeholder | `ConfigureRetention.Handler` returns success but does NOT persist |
| Purge/cleanup job | ❌ Missing | No scheduled job to delete events older than retention period |
| Retention enforcement | ❌ Missing | Events accumulate indefinitely |
| Tenant-scoped retention | ❌ Missing | No `TenantId` on RetentionPolicy |

### 2.2 Is Retention Real?

**Assessment: ❌ Retention is NOT functional — it is modelled but not enforced.**

The entity and table exist, but:
1. The handler is a placeholder that doesn't persist
2. No mechanism exists to actually delete old events
3. Retention policies are global, not tenant-scoped
4. No UI to configure retention

### 2.3 Minimum Viable Retention

| # | Requirement | Priority | Effort |
|---|-----------|----------|--------|
| 1 | Fix `ConfigureRetention` handler to actually persist | P0 | 4h |
| 2 | Add retention list endpoint | P1 | 2h |
| 3 | Implement scheduled purge job (background service) | P1 | 8h |
| 4 | Add `TenantId` to RetentionPolicy | P2 | 2h |
| 5 | Add retention configuration UI | P2 | 8h |
| 6 | Ensure purge respects hash chain (archive before delete, or mark as expired) | P1 | 4h |

---

## 3. Evidence — Current Implementation

### 3.1 Current State

| Aspect | Status | Details |
|--------|--------|---------|
| AuditEvent `Payload` field | ✅ Exists | Text field (JSON) can store arbitrary evidence data |
| ComplianceResult `Details` field | ✅ Exists | Text field (JSON) can store evaluation evidence |
| Dedicated evidence entity | ❌ Missing | No `aud_evidence` table or entity |
| Link to Change Governance EvidencePack | ❌ Missing | No FK or reference to `wf_evidence_packs` |
| Evidence export | ⚠️ Partial | `ExportAuditReport.Query` exports events, but not structured evidence |
| Evidence search/filter | ❌ Missing | Cannot search within Payload content |

### 3.2 How Evidence Is Linked to Actions Today

| Action Source | Evidence Mechanism | Status |
|--------------|-------------------|--------|
| Identity security events | `SecurityAuditBridge` records event with `MetadataJson` as payload | ✅ Working |
| Change Governance approvals | ❌ Not linked | Audit module does not receive approval events |
| Change Governance gate overrides | ❌ Not linked | Override justifications not forwarded to Audit |
| Operational Intelligence incidents | ❌ Not linked | Incident actions not forwarded to Audit |
| Other module actions | ❌ Not linked | Only Identity publishes events |

### 3.3 Is Evidence Real?

**Assessment: ⚠️ Evidence is STRUCTURALLY present but PRACTICALLY limited.**

The `Payload` field on `AuditEvent` can store evidence, but:
1. Only Identity module actually sends evidence via the `Payload` field
2. There is no structured evidence schema — it's free-form JSON
3. There is no way to search within evidence content
4. There is no link between audit events and Change Governance evidence packs
5. There is no frontend to view evidence details

### 3.4 Minimum Viable Evidence

| # | Requirement | Priority | Effort |
|---|-----------|----------|--------|
| 1 | Define evidence JSON schema (standardise Payload structure) | P1 | 4h |
| 2 | Ensure all sensitive actions include evidence in Payload | P1 | 8h (across modules) |
| 3 | Add evidence detail view on AuditTrailDetailPage | P2 | 4h |
| 4 | Consider `CorrelationId` field on AuditEvent for cross-event linking | P2 | 2h |
| 5 | Consider dedicated `aud_evidence` table for binary/structured evidence | P3 | 8h |

---

## 4. Frontend Exposure of Integrity, Retention, and Evidence

| Feature | Frontend Status | Details |
|---------|----------------|---------|
| Chain integrity verification | ✅ Working | "Verify Integrity" button on AuditPage |
| Chain hash per event | ❌ Not shown | Hash value available in API response but not displayed per row |
| Retention policy management | ❌ No UI | No page to configure retention |
| Retention status indicator | ❌ Not shown | No indication of event retention status |
| Evidence detail view | ❌ No UI | Payload content not viewable in frontend |
| Compliance policy UI | ❌ No UI | No page for policy management |

---

## 5. Summary of Gaps

| Area | Status | Key Gap |
|------|--------|---------|
| **Integrity** | ✅ Real (SHA-256 hash chain) | Scalability, automated verification, DB-level immutability |
| **Retention** | ❌ Not functional | Handler is placeholder; no purge mechanism |
| **Evidence** | ⚠️ Structural only | Only Identity sends evidence; no structured schema; no UI |

---

## 6. Recommendations

| # | Recommendation | Priority | Effort |
|---|---------------|----------|--------|
| 1 | Fix retention handler to persist (immediate) | P0 | 4h |
| 2 | Add automated periodic chain verification job | P1 | 4h |
| 3 | Wire Change Governance sensitive actions to Audit | P0 | 8h |
| 4 | Define standard Payload schema for evidence | P1 | 4h |
| 5 | Implement retention purge background service | P1 | 8h |
| 6 | Add chain hash display per event row on AuditPage | P2 | 2h |
| 7 | Consider checkpoint-based verification for large chains | P2 | 8h |
| 8 | Add DB-level immutability triggers | P3 | 4h |
