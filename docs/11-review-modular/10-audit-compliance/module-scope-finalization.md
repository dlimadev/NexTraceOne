# Audit & Compliance — Module Scope Finalization

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Functional Scope — Final Definition

### 1.1 Audit Trail

| Capability | Status | Implementation |
|------------|--------|----------------|
| Record audit events | ✅ Implemented | `RecordAuditEvent.Command` — creates `AuditEvent` + `AuditChainLink` in single transaction |
| Audit trail by resource | ✅ Implemented | `GetAuditTrail.Query` — retrieves all events for resource type + resource ID |
| Search audit log | ✅ Implemented | `SearchAuditLog.Query` — paginated search by source module, action type, date range |
| Hash chain integrity verification | ✅ Implemented | `VerifyChainIntegrity.Query` — full chain traversal with SHA-256 recomputation |
| Export audit report | ✅ Implemented | `ExportAuditReport.Query` — period-based report with all events |
| Cross-module event reception | ⚠️ Partial | `IAuditModule.RecordEventAsync()` available; only Identity publishes systematically via `SecurityAuditBridge` |

### 1.2 Compliance Policies

| Capability | Status | Implementation |
|------------|--------|----------------|
| Create compliance policy | ✅ Implemented | `CreateCompliancePolicy.Command` with name, category, severity, evaluation criteria |
| List compliance policies | ✅ Implemented | `ListCompliancePolicies.Query` with optional filters (isActive, category) |
| Get compliance policy detail | ✅ Implemented | `GetCompliancePolicy.Query` by ID |
| Activate/deactivate policy | ✅ Domain | Domain methods exist (`Activate()`, `Deactivate()`); no dedicated endpoint |
| Policy evaluation execution | ❌ Not implemented | `EvaluationCriteria` field exists but no evaluation engine |

### 1.3 Compliance Results

| Capability | Status | Implementation |
|------------|--------|----------------|
| Record compliance result | ✅ Implemented | `RecordComplianceResult.Command` linked to policy + optional campaign |
| List compliance results | ✅ Implemented | `ListComplianceResults.Query` by policy, campaign, outcome |
| Compliance report | ✅ Implemented | `GetComplianceReport.Query` — period breakdown by module with chain integrity |

### 1.4 Audit Campaigns

| Capability | Status | Implementation |
|------------|--------|----------------|
| Create campaign | ✅ Implemented | `CreateAuditCampaign.Command` with type (Periodic, AdHoc, Regulatory) |
| List campaigns | ✅ Implemented | `ListAuditCampaigns.Query` by status |
| Get campaign detail | ✅ Implemented | `GetAuditCampaign.Query` by ID |
| Campaign lifecycle | ✅ Domain | `Start()`, `Complete()`, `Cancel()` methods; no dedicated endpoints |

### 1.5 Retention

| Capability | Status | Implementation |
|------------|--------|----------------|
| Configure retention policy | ⚠️ Placeholder | `ConfigureRetention.Command` handler exists but does NOT persist |
| Enforce retention (purge old events) | ❌ Not implemented | No scheduled job or purge mechanism |
| Retention period validation | ✅ Domain | `RetentionPolicy.Create()` validates 1–3650 days |

### 1.6 Evidence & Export

| Capability | Status | Implementation |
|------------|--------|----------------|
| Audit report export | ✅ Implemented | `ExportAuditReport.Query` returns structured report data |
| Compliance report | ✅ Implemented | `GetComplianceReport.Query` with chain integrity check |
| Evidence linking to changes | ❌ Not implemented | No explicit link between `AuditEvent` and Change Governance `EvidencePack` |
| PDF/structured export | ❌ Not implemented | Frontend has `exportReport()` returning blob, but format unspecified |

---

## 2. What Is In Scope (Final)

1. **Immutable audit event recording** from all modules via `IAuditModule` contract
2. **SHA-256 hash chain** for tamper detection
3. **Audit trail query** by resource or by filters
4. **Hash chain integrity verification** (full chain)
5. **Compliance policy management** (CRUD + severity + category)
6. **Compliance result recording** linked to policies and campaigns
7. **Audit campaign management** (lifecycle: Planned → InProgress → Completed/Cancelled)
8. **Retention policy configuration** (1–3650 days)
9. **Audit report export** (period-based)
10. **Compliance reporting** with module breakdown and chain status

---

## 3. What Is Out of Scope (Final)

| Out of Scope | Reason | Owning Module |
|-------------|--------|---------------|
| Application logging and tracing | Technical infrastructure, not business audit | Platform / Infrastructure |
| Security event detection and risk scoring | Security domain concept | Identity & Access |
| Executive dashboards and analytics | Presentation layer | Governance |
| ClickHouse analytics pipeline | Cross-cutting analytics | Platform |
| Notification delivery for compliance alerts | Delivery mechanism | Notifications |
| Regulatory framework definitions (SOC 2, ISO, GDPR specifics) | External knowledge | Configuration / Manual |
| Change Governance evidence packs | Workflow-specific artefacts | Change Governance |

---

## 4. Scope Containment Rules

1. **Audit & Compliance MUST NOT** duplicate security event detection — it only receives and stores events
2. **Audit & Compliance MUST NOT** own reporting dashboards — it provides data for Governance reports
3. **Audit & Compliance MUST NOT** enforce compliance policies at runtime — it evaluates and records results
4. **Audit & Compliance MUST NOT** manage user identities — it records user actions by reference
5. **Audit & Compliance MAY** trigger compliance notifications via outbox events for the Notifications module

---

## 5. Minimum Complete Set for Production

| # | Capability | Priority | Status |
|---|-----------|----------|--------|
| 1 | Record audit events with hash chain | P0 | ✅ Done |
| 2 | Hash chain integrity verification | P0 | ✅ Done |
| 3 | Audit trail query by resource | P0 | ✅ Done |
| 4 | Audit log search with filters | P0 | ✅ Done |
| 5 | Compliance policy CRUD | P1 | ✅ Backend done; ❌ No frontend |
| 6 | Compliance result recording | P1 | ✅ Backend done; ❌ No frontend |
| 7 | Campaign management | P2 | ✅ Backend done; ❌ No frontend |
| 8 | Retention enforcement | P1 | ❌ Not implemented |
| 9 | All modules publishing events | P0 | ⚠️ Only Identity confirmed |
| 10 | Frontend for all features | P1 | ⚠️ Only 1 page (AuditPage) exists |

---

## 6. Frontend Scope Coverage

| Need | Page/Component | Status |
|------|---------------|--------|
| Audit log search and browse | AuditPage | ✅ Exists (event table, filters, pagination) |
| Integrity verification | AuditPage ("Verify Integrity" button) | ✅ Exists |
| Compliance policies management | — | ❌ Missing — no frontend page |
| Compliance results viewing | — | ❌ Missing — no frontend page |
| Campaign management | — | ❌ Missing — no frontend page |
| Retention configuration | — | ❌ Missing — no frontend page |
| Audit trail per resource (detail view) | — | ❌ Missing — no frontend page |
| Report export UI | AuditPage (partial) | ⚠️ `exportReport()` exists in API client but no UI trigger visible |

---

## 7. API Scope Coverage

- **15 endpoints** across audit trail, compliance policies, campaigns, and results
- **All CRUD operations** covered for core entities
- **No dead endpoints** — all mapped to handlers
- **1 placeholder handler** — `ConfigureRetention` returns success without persisting
- **Permission-protected** — 5 permission scopes across all endpoints
- **Gap:** No endpoints for campaign lifecycle transitions (Start, Complete, Cancel)
- **Gap:** No endpoint for policy activation/deactivation
