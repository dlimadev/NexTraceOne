# Audit & Compliance — Security and Permissions Review

> **Module:** 10 — Audit & Compliance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Permission Matrix — Complete

### 1.1 Audit Trail Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `audit:events:write` | POST /api/v1/audit/events | Write | Record new audit events |
| `audit:trail:read` | GET /audit/trail, /audit/search, /audit/verify-chain | Read | Query and verify audit trail |
| `audit:reports:read` | GET /audit/report | Read | Export audit reports |

### 1.2 Compliance Permissions

| Permission | Endpoints | Type | Notes |
|-----------|-----------|------|-------|
| `audit:compliance:read` | GET policies, results, campaigns, compliance report | Read | View all compliance data |
| `audit:compliance:write` | POST policies, results, campaigns, retention | Write | Create and manage compliance data |

### Total: 5 permission scopes

---

## 2. Permission Enforcement — Backend

| Layer | Mechanism | Status |
|-------|-----------|--------|
| **Endpoint level** | `RequireAuthorization("permission:scope")` on endpoint registration | ✅ All 15 endpoints protected |
| **Handler level** | MediatR pipeline with `IAuthorizationHandler` | ✅ Via pipeline behaviour |
| **Entity level** | `TenantId` filtering via `TenantRlsInterceptor` (PostgreSQL RLS) | ✅ Active on AuditDbContext |
| **Cross-module contract** | `IAuditModule.RecordEventAsync()` — receives TenantId as parameter | ✅ Tenant context preserved |

---

## 3. Permission Enforcement — Frontend

| Layer | Mechanism | Status |
|-------|-----------|--------|
| **Route level** | `ProtectedRoute` with `permission="audit:read"` in App.tsx | ✅ AuditPage protected |
| **Sidebar visibility** | Permission-based filtering in AppSidebar.tsx | ✅ `audit:read` filter |
| **Action-level guards** | "Verify Integrity" button — no specific write permission check | ⚠️ Uses read permission for verification action |

---

## 4. Sensitive Actions Audit

| Action | Endpoint | Permission | Risk Level | Self-Audit |
|--------|----------|-----------|------------|------------|
| Record audit event | POST /audit/events | `audit:events:write` | 🟡 Medium | ⚠️ The audit module does not audit its own event recordings |
| Verify chain integrity | GET /audit/verify-chain | `audit:trail:read` | 🟢 Low | ⚠️ Verification action not audited |
| Create compliance policy | POST /audit/compliance/policies | `audit:compliance:write` | 🟡 Medium | ⚠️ Policy creation not self-audited |
| Record compliance result | POST /audit/compliance/results | `audit:compliance:write` | 🟡 Medium | ⚠️ Result recording not self-audited |
| Create campaign | POST /audit/campaigns | `audit:compliance:write` | 🟡 Medium | ⚠️ Campaign creation not self-audited |
| Configure retention | (internal) | `audit:compliance:write` | 🔴 High | ⚠️ Retention changes not audited — most sensitive action |

**Critical observation:** The Audit module does not audit its own sensitive actions. Changes to compliance policies, campaigns, and retention should be self-audited.

---

## 5. Tenant Isolation

| Mechanism | Status | Details |
|-----------|--------|---------|
| `TenantId` on AuditEvent | ✅ Present | All events scoped by tenant |
| `TenantId` on CompliancePolicy | ✅ Present | Policies scoped by tenant |
| `TenantId` on ComplianceResult | ✅ Present | Results scoped by tenant |
| `TenantId` on AuditCampaign | ✅ Present | Campaigns scoped by tenant |
| `TenantId` on RetentionPolicy | ❌ Missing | Retention policies are global |
| PostgreSQL RLS | ✅ Active | `TenantRlsInterceptor` applied via `NexTraceDbContextBase` |
| `IAuditModule` TenantId param | ✅ Present | Cross-module contract includes TenantId |

---

## 6. Environment-Level Scoping

| Entity | `EnvironmentId` | Status |
|--------|-----------------|--------|
| AuditEvent | ❌ Missing | Cannot filter audit trail by environment |
| CompliancePolicy | ❌ Missing | Policies are environment-agnostic |
| ComplianceResult | ❌ Missing | Results not scoped by environment |
| AuditCampaign | ❌ Missing | Campaigns not scoped by environment |

**Gap:** No environment-level scoping exists in Audit & Compliance. This means:
- Cannot audit only production actions separately from staging
- Cannot apply different compliance policies per environment
- Cannot scope campaigns to specific environments

---

## 7. Security Gaps

| ID | Gap | Severity | Description |
|----|-----|----------|-------------|
| SEC-01 | Module does not self-audit | 🔴 High | Compliance policy changes, campaign lifecycle transitions, and retention configuration are not recorded as audit events |
| SEC-02 | No EnvironmentId on audit events | 🟠 Medium-High | Cannot scope audit trail by environment |
| SEC-03 | No DB-level immutability enforcement | 🟡 Medium | Admin with DB access could modify events or chain links |
| SEC-04 | RetentionPolicy has no TenantId | 🟡 Medium | Retention is global, not tenant-scoped |
| SEC-05 | VerifyChainIntegrity not scoped by tenant | 🟡 Medium | Verification is global; should be tenant-scoped |
| SEC-06 | No rate limiting on event recording endpoint | 🟡 Medium | Could be abused for DoS |
| SEC-07 | Audit events from IAuditModule are best-effort | 🟡 Medium | Failed propagation is logged but event is lost |
| SEC-08 | No audit of who accessed audit data | 🟢 Low | Read operations on audit trail not tracked |

---

## 8. Security Correction Backlog

| ID | Item | Priority | Effort |
|----|------|----------|--------|
| SC-01 | Implement self-auditing for compliance policy changes | P0 | 4h |
| SC-02 | Implement self-auditing for campaign lifecycle transitions | P1 | 2h |
| SC-03 | Implement self-auditing for retention configuration changes | P0 | 2h |
| SC-04 | Add EnvironmentId to AuditEvent and recording flow | P1 | 4h |
| SC-05 | Add TenantId to RetentionPolicy | P1 | 2h |
| SC-06 | Scope chain verification by tenant | P2 | 4h |
| SC-07 | Consider guaranteed delivery for IAuditModule (vs best-effort) | P2 | 8h |
| SC-08 | Add DB-level immutability triggers for audit tables | P3 | 4h |

**Total estimated effort:** ~30 hours
