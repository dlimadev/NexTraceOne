# Governance Module — Backend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Full Endpoint Inventory (19 Modules)

| # | Endpoint Module | Base Route | HTTP Methods | Belongs to | Permission |
|---|----------------|-----------|-------------|-----------|-----------|
| 1 | GovernancePacksEndpointModule | `/api/v1/governance/packs` | GET, POST, PUT, DELETE, PATCH | ✅ Governance | `governance:packs:read/write` |
| 2 | GovernancePacksVersionsEndpointModule | `/api/v1/governance/packs/{id}/versions` | GET, POST | ✅ Governance | `governance:packs:read/write` |
| 3 | DomainEndpointModule | `/api/v1/governance/domains` | GET, POST, PUT, DELETE | ✅ Governance | `governance:domains:read/write` |
| 4 | TeamEndpointModule | `/api/v1/governance/teams` | GET, POST, PUT, DELETE | ✅ Governance | `governance:teams:read/write` |
| 5 | GovernanceWaiversEndpointModule | `/api/v1/governance/waivers` | GET, POST, PUT, DELETE, PATCH | ✅ Governance | `governance:waivers:read/write` |
| 6 | DelegatedAdminEndpointModule | `/api/v1/governance/delegated-admin` | GET, POST | ✅ Governance | `governance:admin:read/write` |
| 7 | ComplianceChecksEndpointModule | `/api/v1/governance/compliance` | GET, POST | ✅ Governance | `governance:compliance:read/write` |
| 8 | GovernanceComplianceEndpointModule | `/api/v1/governance/compliance-summary` | GET | ✅ Governance | `governance:compliance:read` |
| 9 | EvidencePackagesEndpointModule | `/api/v1/governance/evidence` | GET | ✅ Governance | `governance:evidence:read` |
| 10 | GovernanceRiskEndpointModule | `/api/v1/governance/risk` | GET | ✅ Governance | `governance:risk:read` |
| 11 | GovernanceReportsEndpointModule | `/api/v1/governance/reports` | GET | ✅ Governance | `governance:reports:read` |
| 12 | EnterpriseControlsEndpointModule | `/api/v1/governance/controls` | GET | ✅ Governance | `governance:controls:read` |
| 13 | ExecutiveOverviewEndpointModule | `/api/v1/governance/executive` | GET | ✅ Governance | `governance:compliance:read` |
| 14 | GovernanceFinOpsEndpointModule | `/api/v1/governance/finops` | GET | ✅ Governance | `governance:finops:read` |
| 15 | PolicyCatalogEndpointModule | `/api/v1/governance/policies` | GET | ✅ Governance | `governance:policies:read` |
| 16 | ScopedContextEndpointModule | `/api/v1/governance/context` | GET | ✅ Governance | `governance:compliance:read` |
| 17 | **IntegrationHubEndpointModule** | `/api/v1/integrations`, `/api/v1/ingestion` | GET, POST, PUT, DELETE | ❌ **Integrations** | `integrations:read/write` |
| 18 | **ProductAnalyticsEndpointModule** | `/api/v1/product-analytics` | GET, POST | ❌ **Product Analytics** | `governance:analytics:read/write` |
| 19 | OnboardingEndpointModule | `/api/v1/governance/onboarding` | GET | ⚠️ EVALUATE | `governance:teams:read` ⚠️ |
| 20 | PlatformStatusEndpointModule | `/api/v1/governance/platform` | GET | ⚠️ EVALUATE | `platform:admin:read` |

**Totals:** 16 Governance + 1 Integrations + 1 Product Analytics + 2 to evaluate = 20 endpoint entries across 19 modules.

---

## 2. Endpoint-to-Use-Case Mapping

| Endpoint Module | Use Cases Served |
|----------------|-----------------|
| GovernancePacksEndpointModule | List packs, create pack, update pack, delete pack, change pack status, publish, rollout |
| GovernancePacksVersionsEndpointModule | List versions, create version |
| DomainEndpointModule | List domains, create domain, update domain, delete domain |
| TeamEndpointModule | List teams, create team, update team, delete team |
| GovernanceWaiversEndpointModule | List waivers, request waiver, update waiver, approve/reject waiver, delete waiver |
| DelegatedAdminEndpointModule | List delegated admins, create delegation |
| ComplianceChecksEndpointModule | List compliance checks, run compliance check |
| GovernanceComplianceEndpointModule | Get compliance summary, compliance trends |
| EvidencePackagesEndpointModule | List evidence packages, get evidence package detail |
| GovernanceRiskEndpointModule | Get risk summary, risk heatmap data |
| GovernanceReportsEndpointModule | Get governance reports |
| EnterpriseControlsEndpointModule | List enterprise controls |
| ExecutiveOverviewEndpointModule | Executive summary, executive drill-down, executive finops |
| GovernanceFinOpsEndpointModule | FinOps overview, service finops, team finops, domain finops, cost trends |
| PolicyCatalogEndpointModule | List policies, get policy detail |
| ScopedContextEndpointModule | Get scoped governance context |
| IntegrationHubEndpointModule | CRUD connectors, manage ingestion sources, track executions |
| ProductAnalyticsEndpointModule | Record analytics events, query analytics data |
| OnboardingEndpointModule | Get onboarding status for governance |
| PlatformStatusEndpointModule | Get platform status overview |

---

## 3. Dead Endpoints

**None found.** All 19 endpoint modules are referenced in the module registration and have at least one active handler.

---

## 4. Incomplete Endpoints

| # | Endpoint Module | Gap | Impact | Priority |
|---|----------------|-----|--------|----------|
| 1 | PolicyCatalogEndpointModule | Read-only — no POST, PUT, DELETE for policy management | Users cannot create or manage policies | **HIGH** |
| 2 | EvidencePackagesEndpointModule | Read-only — no POST for evidence creation | Users cannot submit evidence packages | **HIGH** |
| 3 | EnterpriseControlsEndpointModule | Read-only — no POST, PUT, DELETE for controls management | Admins cannot manage enterprise controls | **MEDIUM** |
| 4 | GovernancePacksVersionsEndpointModule | Limited — only GET list and POST create, no publish/deprecate | Pack version lifecycle is partially managed via pack module | **LOW** |

---

## 5. Endpoints Belonging to Other Modules

### 5.1 IntegrationHubEndpointModule → Integrations

| Route | Method | Handler | Notes |
|-------|--------|---------|-------|
| `/api/v1/integrations/connectors` | GET | ListIntegrationConnectors | Uses `integrations:read` |
| `/api/v1/integrations/connectors` | POST | CreateIntegrationConnector | Uses `integrations:write` |
| `/api/v1/integrations/connectors/{id}` | PUT | UpdateIntegrationConnector | Uses `integrations:write` |
| `/api/v1/integrations/connectors/{id}` | DELETE | DeleteIntegrationConnector | Uses `integrations:write` |
| `/api/v1/ingestion/sources` | GET | ListIngestionSources | Uses `integrations:read` |
| `/api/v1/ingestion/sources` | POST | CreateIngestionSource | Uses `integrations:write` |
| `/api/v1/ingestion/executions` | GET | ListIngestionExecutions | Uses `integrations:read` |
| `/api/v1/ingestion/executions` | POST | TriggerIngestionExecution | Uses `integrations:write` |

**Action:** Extract to Integrations module when that module is created.

### 5.2 ProductAnalyticsEndpointModule → Product Analytics

| Route | Method | Handler | Notes |
|-------|--------|---------|-------|
| `/api/v1/product-analytics/events` | GET | ListAnalyticsEvents | Uses `governance:analytics:read` |
| `/api/v1/product-analytics/events` | POST | RecordAnalyticsEvent | Uses `governance:analytics:write` |
| `/api/v1/product-analytics/summary` | GET | GetAnalyticsSummary | Uses `governance:analytics:read` |
| `/api/v1/product-analytics/trends` | GET | GetAnalyticsTrends | Uses `governance:analytics:read` |
| `/api/v1/product-analytics/reports` | GET | GetAnalyticsReports | Uses `governance:analytics:read` |
| `/api/v1/product-analytics/export` | GET | ExportAnalyticsData | Uses `governance:analytics:read` |
| `/api/v1/product-analytics/dashboard` | GET | GetAnalyticsDashboard | Uses `governance:analytics:read` |

**Action:** Extract to Product Analytics module when that module is created.

### 5.3 PlatformStatusEndpointModule — Evaluate

Currently serves platform health data under the governance prefix. This may belong to:
- **Operational Intelligence** — if it reports on platform component health
- **Platform/Foundation** — if it reports on system infrastructure

**Recommendation:** Keep in Governance temporarily until Operational Intelligence module is defined. Mark for re-evaluation at B2.

### 5.4 OnboardingEndpointModule — Evaluate

Provides governance onboarding context. Uses `governance:teams:read` which is **incorrect** — onboarding is not a team-read action.

**Recommendation:** Keep endpoint but fix permission to `governance:compliance:read` or a dedicated `governance:onboarding:read`. Evaluate at B2 whether onboarding belongs to Platform/Identity.

---

## 6. Validation Review

| Area | Status | Notes |
|------|--------|-------|
| Request validation | ✅ Present | FluentValidation used on command/query DTOs |
| Required fields enforcement | ✅ Present | Validators enforce required fields |
| String length limits | ✅ Present | Name, description fields have max length validators |
| Enum validation | ✅ Present | Enum values validated on input |
| ID validation | ✅ Present | Strongly-typed IDs with non-empty checks |
| Cross-field validation | ⚠️ Partial | Some complex rules (e.g., waiver date ranges) may need strengthening |
| Business rule validation | ⚠️ Partial | Pack status transitions not validated in entity — done in handlers |

---

## 7. Error Handling Review

| Area | Status | Notes |
|------|--------|-------|
| Not found handling | ✅ Present | Returns 404 with domain-specific error codes |
| Validation errors | ✅ Present | Returns 400 with validation problem details |
| Authorization errors | ✅ Present | Returns 403 via middleware |
| Concurrency conflicts | ❌ Missing | No `DbUpdateConcurrencyException` handling — no xmin tokens yet |
| Domain exceptions | ✅ Present | Custom exceptions mapped to HTTP status codes |
| Unhandled exceptions | ✅ Present | Global exception handler returns 500 |

---

## 8. Audit Trail Review

| Area | Status | Notes |
|------|--------|-------|
| Created/updated timestamps | ✅ Present | `CreatedAt`, `UpdatedAt` on all entities |
| Created/updated by | ✅ Present | `CreatedBy`, `UpdatedBy` on all entities |
| Soft delete | ✅ Present | `IsDeleted` flag on all entities |
| Tenant isolation | ✅ Present | `TenantId` on all entities with RLS interceptor |
| Domain events | ❌ Missing | No domain events raised for governance actions |
| Audit log integration | ⚠️ Partial | Depends on global audit middleware — no governance-specific audit events |

---

## 9. Permission Review

### Backend Permissions (Granular)

| Permission | Used By | Type |
|-----------|---------|------|
| `governance:packs:read` | GovernancePacksEndpointModule (GET) | Read |
| `governance:packs:write` | GovernancePacksEndpointModule (POST/PUT/DELETE/PATCH) | Write |
| `governance:domains:read` | DomainEndpointModule (GET) | Read |
| `governance:domains:write` | DomainEndpointModule (POST/PUT/DELETE) | Write |
| `governance:teams:read` | TeamEndpointModule (GET) | Read |
| `governance:teams:write` | TeamEndpointModule (POST/PUT/DELETE) | Write |
| `governance:waivers:read` | GovernanceWaiversEndpointModule (GET) | Read |
| `governance:waivers:write` | GovernanceWaiversEndpointModule (POST/PUT/DELETE/PATCH) | Write |
| `governance:admin:read` | DelegatedAdminEndpointModule (GET) | Read |
| `governance:admin:write` | DelegatedAdminEndpointModule (POST) | Write |
| `governance:compliance:read` | ComplianceChecks, ComplianceSummary, Executive, ScopedContext (GET) | Read |
| `governance:compliance:write` | ComplianceChecksEndpointModule (POST) | Write |
| `governance:analytics:read` | ProductAnalyticsEndpointModule (GET) | Read |
| `governance:analytics:write` | ProductAnalyticsEndpointModule (POST) | Write |
| `governance:evidence:read` | EvidencePackagesEndpointModule (GET) | Read |
| `governance:finops:read` | GovernanceFinOpsEndpointModule (GET) | Read |
| `governance:risk:read` | GovernanceRiskEndpointModule (GET) | Read |
| `governance:reports:read` | GovernanceReportsEndpointModule (GET) | Read |
| `governance:controls:read` | EnterpriseControlsEndpointModule (GET) | Read |
| `governance:policies:read` | PolicyCatalogEndpointModule (GET) | Read |
| `governance:policies:write` | ❌ **Not yet used** — no write endpoints exist | Write |
| `integrations:read` | IntegrationHubEndpointModule (GET) | Read |
| `integrations:write` | IntegrationHubEndpointModule (POST/PUT/DELETE) | Write |
| `platform:admin:read` | PlatformStatusEndpointModule, DelegatedAdminEndpointModule (POST) ⚠️ | Read |

### Permission Issues Found

| # | Issue | Severity | Details |
|---|-------|----------|---------|
| 1 | DelegatedAdmin POST uses `platform:admin:read` | **HIGH** | Should use `governance:admin:write` — a POST is a write action, and `read` permission should not authorize creation |
| 2 | OnboardingEndpointModule uses `governance:teams:read` | **MEDIUM** | Onboarding is not a teams-specific action — should use a broader or dedicated permission |
| 3 | Frontend uses single `governance:read` for 24 routes | **HIGH** | Backend has 12+ granular permissions — frontend must align (see security-and-permissions-review.md) |

---

## 10. Corrections Backlog

### HIGH Priority

| # | Correction | Type | Rationale |
|---|-----------|------|-----------|
| 1 | Add CRUD endpoints for PolicyCatalog (POST, PUT, DELETE) | Feature gap | Policy management is a core governance capability — read-only is insufficient |
| 2 | Add evidence creation endpoint (POST) | Feature gap | Users must be able to submit evidence packages for compliance |
| 3 | Add GovernanceRuleBinding DbSet + EF configuration | Persistence gap | Entity exists but is not persisted — cannot be queried or stored |
| 4 | Add `xmin` concurrency token to all Governance entities | Data safety | No optimistic concurrency protection — concurrent writes silently overwrite |
| 5 | Handle `DbUpdateConcurrencyException` in all write handlers | Data safety | Without handling, concurrency conflicts result in unhandled 500 errors |
| 6 | Fix DelegatedAdmin POST permission from `platform:admin:read` to `governance:admin:write` | Security | Read permission should never authorize a write operation |

### MEDIUM Priority

| # | Correction | Type | Rationale |
|---|-----------|------|-----------|
| 7 | Add CRUD endpoints for EnterpriseControls (POST, PUT, DELETE) | Feature gap | Admins need to manage enterprise controls, not just view them |
| 8 | Fix OnboardingEndpointModule permission from `governance:teams:read` to appropriate scope | Security | Wrong permission scope for the operation |
| 9 | Add pack status transition validation in GovernancePack entity | Domain logic | Status transitions are validated in handlers, not the entity — violates DDD aggregate invariants |
| 10 | Add `governance:policies:write` permission enforcement | Security | Permission constant exists but no endpoint uses it yet |

### LOW Priority

| # | Correction | Type | Rationale |
|---|-----------|------|-----------|
| 11 | Add domain events for governance actions (pack created, waiver approved, etc.) | Architecture | Enables event-driven integration with other modules |
| 12 | Add governance-specific audit events | Observability | Governance actions are high-value — dedicated audit events improve traceability |
| 13 | Evaluate PlatformStatus/Onboarding placement at B2 | Module boundary | May belong to other modules — defer evaluation to next phase |

---

## Summary

The Governance backend has a solid foundation with 16 well-structured endpoint modules and granular permissions. The primary gaps are:

1. **Read-only endpoints** for policy, evidence, and controls that need write operations
2. **Concurrency protection** is entirely missing (no xmin, no DbUpdateConcurrencyException handling)
3. **Permission mismatches** on DelegatedAdmin and Onboarding endpoints
4. **GovernanceRuleBinding** entity without persistence mapping
5. **Domain events** not yet implemented for governance actions

All corrections are cataloged and prioritized for implementation in subsequent phases.
