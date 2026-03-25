# Audit & Compliance — Backend Functional Corrections

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Endpoint Inventory (15 Endpoints)

### 1.1 Audit Trail Endpoints (6)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 1 | POST | `/api/v1/audit/events` | RecordAuditEvent.Command | `audit:events:write` | ✅ Working |
| 2 | GET | `/api/v1/audit/trail` | GetAuditTrail.Query | `audit:trail:read` | ✅ Working |
| 3 | GET | `/api/v1/audit/search` | SearchAuditLog.Query | `audit:trail:read` | ✅ Working |
| 4 | GET | `/api/v1/audit/verify-chain` | VerifyChainIntegrity.Query | `audit:trail:read` | ✅ Working |
| 5 | GET | `/api/v1/audit/report` | ExportAuditReport.Query | `audit:reports:read` | ✅ Working |
| 6 | GET | `/api/v1/audit/compliance` | GetComplianceReport.Query | `audit:compliance:read` | ✅ Working |

### 1.2 Compliance Policy Endpoints (3)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 7 | POST | `/api/v1/audit/compliance/policies` | CreateCompliancePolicy.Command | `audit:compliance:write` | ✅ Working |
| 8 | GET | `/api/v1/audit/compliance/policies` | ListCompliancePolicies.Query | `audit:compliance:read` | ✅ Working |
| 9 | GET | `/api/v1/audit/compliance/policies/{policyId}` | GetCompliancePolicy.Query | `audit:compliance:read` | ✅ Working |

### 1.3 Campaign Endpoints (3)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 10 | POST | `/api/v1/audit/campaigns` | CreateAuditCampaign.Command | `audit:compliance:write` | ✅ Working |
| 11 | GET | `/api/v1/audit/campaigns` | ListAuditCampaigns.Query | `audit:compliance:read` | ✅ Working |
| 12 | GET | `/api/v1/audit/campaigns/{campaignId}` | GetAuditCampaign.Query | `audit:compliance:read` | ✅ Working |

### 1.4 Compliance Result Endpoints (2)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 13 | POST | `/api/v1/audit/compliance/results` | RecordComplianceResult.Command | `audit:compliance:write` | ✅ Working |
| 14 | GET | `/api/v1/audit/compliance/results` | ListComplianceResults.Query | `audit:compliance:read` | ✅ Working |

### 1.5 Retention (1 — Placeholder)

| # | Method | Route | Handler | Permission | Status |
|---|--------|-------|---------|-----------|--------|
| 15 | — | (internal) | ConfigureRetention.Command | `audit:compliance:write` | ⚠️ Placeholder — returns success but does NOT persist |

---

## 2. Missing Endpoints

| ID | Endpoint Needed | Domain Method | Priority |
|----|----------------|---------------|----------|
| ME-01 | `POST /api/v1/audit/campaigns/{id}/start` | `AuditCampaign.Start()` | P1 |
| ME-02 | `POST /api/v1/audit/campaigns/{id}/complete` | `AuditCampaign.Complete()` | P1 |
| ME-03 | `POST /api/v1/audit/campaigns/{id}/cancel` | `AuditCampaign.Cancel()` | P1 |
| ME-04 | `POST /api/v1/audit/compliance/policies/{id}/activate` | `CompliancePolicy.Activate()` | P1 |
| ME-05 | `POST /api/v1/audit/compliance/policies/{id}/deactivate` | `CompliancePolicy.Deactivate()` | P1 |
| ME-06 | `PUT /api/v1/audit/compliance/policies/{id}` | `CompliancePolicy.Update()` | P2 |
| ME-07 | `POST /api/v1/audit/retention` | ConfigureRetention (real) | P1 |
| ME-08 | `GET /api/v1/audit/retention` | ListRetentionPolicies | P1 |

---

## 3. Identified Issues

| ID | Description | Handler/File | Severity |
|----|-------------|-------------|----------|
| B-01 | `ConfigureRetention.Handler` is a placeholder — returns success without persisting | `Features/ConfigureRetention/ConfigureRetention.cs` | 🔴 High |
| B-02 | `VerifyChainIntegrity` loads ALL chain links into memory — will not scale | `Features/VerifyChainIntegrity/VerifyChainIntegrity.cs` | 🟡 Medium |
| B-03 | `RecordAuditEvent` does not validate `SourceModule` against known module list | `Features/RecordAuditEvent/RecordAuditEvent.cs` | 🟢 Low |
| B-04 | `ExportAuditReport` has no pagination — returns all events for date range | `Features/ExportAuditReport/ExportAuditReport.cs` | 🟡 Medium |
| B-05 | No `EnvironmentId` parameter on `RecordAuditEvent.Command` | `Features/RecordAuditEvent/RecordAuditEvent.cs` | 🟠 Medium-High |

---

## 4. Validation Coverage

| Handler | Validation | Status |
|---------|-----------|--------|
| RecordAuditEvent | SourceModule, ActionType, ResourceId, ResourceType, PerformedBy: NotEmpty + MaxLength; TenantId: NotEmpty | ✅ Good |
| GetAuditTrail | ResourceType, ResourceId: NotEmpty + MaxLength | ✅ Good |
| SearchAuditLog | Page >= 1, PageSize 1–100 | ✅ Good |
| ExportAuditReport | From < To | ✅ Good |
| CreateCompliancePolicy | Name, DisplayName, Category: NotEmpty + MaxLength; Severity: IsInEnum; TenantId: NotEmpty | ✅ Good |
| RecordComplianceResult | PolicyId, ResourceType, ResourceId, EvaluatedBy: NotEmpty; Outcome: IsInEnum | ✅ Good |
| CreateAuditCampaign | Name, CampaignType, CreatedBy: NotEmpty + MaxLength; TenantId: NotEmpty | ✅ Good |
| ConfigureRetention | PolicyName: NotEmpty; RetentionDays: > 0, ≤ 3650 | ✅ Good (but handler doesn't persist) |

---

## 5. Cross-Module Integration

### Integration Contract

**File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Contracts/ServiceInterfaces/IAuditModule.cs`

| Method | Status | Consumers |
|--------|--------|-----------|
| `RecordEventAsync()` | ✅ Implemented | Identity via `SecurityAuditBridge` |
| `VerifyChainIntegrityAsync()` | ✅ Implemented | — |

### Implementation

**File:** `src/modules/auditcompliance/NexTraceOne.AuditCompliance.Infrastructure/Services/AuditModuleService.cs`

Delegates to `RecordAuditEvent.Command` via MediatR. Best-effort — failures logged but do not block caller.

### Confirmed Consumers

| Module | Integration | Status |
|--------|------------|--------|
| Identity & Access | `SecurityAuditBridge` → `IAuditModule.RecordEventAsync()` | ✅ Confirmed |
| Change Governance | No integration | ❌ Not wired |
| Operational Intelligence | No integration | ❌ Not wired |
| Catalog | No integration | ❌ Not wired |
| Configuration | No integration | ❌ Not wired |
| Governance | No integration | ❌ Not wired |
| Notifications | No integration | ❌ Not wired |
| AI & Knowledge | No integration | ❌ Not wired |

---

## 6. Backend Correction Backlog

| ID | Item | Area | Priority | Effort |
|----|------|------|----------|--------|
| BC-01 | Implement real ConfigureRetention handler (persist to DB) | Retention | P0 | 4h |
| BC-02 | Add campaign lifecycle endpoints (Start, Complete, Cancel) | Campaign | P1 | 4h |
| BC-03 | Add policy activate/deactivate endpoints | Compliance | P1 | 2h |
| BC-04 | Add policy update endpoint | Compliance | P2 | 2h |
| BC-05 | Add retention list endpoint | Retention | P1 | 2h |
| BC-06 | Add `EnvironmentId` to RecordAuditEvent command | Audit Trail | P1 | 4h |
| BC-07 | Add pagination to ExportAuditReport | Audit Trail | P2 | 2h |
| BC-08 | Optimise VerifyChainIntegrity for large chains (streaming/batched) | Performance | P2 | 8h |
| BC-09 | Wire Change Governance → IAuditModule for approval/override events | Integration | P0 | 8h |
| BC-10 | Wire Operational Intelligence → IAuditModule for incident events | Integration | P1 | 4h |
| BC-11 | Wire remaining modules → IAuditModule | Integration | P2 | 8h |
| BC-12 | Add RowVersion to mutable entities | Persistence | P1 | 4h |
| BC-13 | Create module README.md | Documentation | P2 | 2h |

**Total estimated effort:** ~54 hours
