# 11 — Gap Backlog (Prioritized)

**Date:** 2026-03-22

---

## Severity Classification

| Level | Definition |
|-------|-----------|
| **Critical** | Blocks production deployment or creates security/data-integrity risk |
| **High** | Major functional gap that undermines enterprise readiness |
| **Medium** | Important improvement needed for production quality |
| **Low** | Enhancement for completeness, no blocking impact |

---

## Backlog Items

### GAP-001: Outbox Processor Only Covers IdentityDbContext
- **Module/Area:** Platform / BackgroundWorkers
- **Severity:** Critical
- **Type:** Architectural
- **Evidence:** `OutboxProcessorJob.cs` queries only `IdentityDbContext` for pending messages
- **Situation:** Domain events from Catalog, ChangeGovernance, AIKnowledge, Governance, OperationalIntelligence, AuditCompliance are never dispatched to consumers
- **Impact:** Cross-module event propagation is broken; any workflow that depends on domain events across modules silently fails
- **What's Missing:** OutboxProcessorJob must iterate over ALL DbContexts that produce domain events, or each module needs its own outbox processor
- **Risk of Not Fixing:** Event-driven flows between modules will not work. Audit trail may miss events. Correlation and enrichment pipelines incomplete.
- **Recommendation:** Extend `OutboxProcessorJob` to process outbox tables from all 16 DbContexts (or create per-database outbox processors)
- **Dependencies:** None
- **Complexity:** Medium
- **Priority:** P0
- **Acceptance Criteria:** All domain events across all modules are dispatched and can be consumed by subscribers

---

### GAP-002: 14 Route Prefixes Excluded from Production Scope
- **Module/Area:** Frontend / releaseScope.ts
- **Severity:** Critical
- **Type:** Functional
- **Evidence:** `releaseScope.ts` — `finalProductionExcludedRoutePrefixes` array with 14 entries
- **Situation:** `/portal`, `/governance/teams`, `/governance/packs`, `/integrations/executions`, `/analytics/value`, `/operations/runbooks`, `/operations/reliability`, `/operations/automation`, `/ai/models`, `/ai/policies`, `/ai/routing`, `/ai/ide`, `/ai/budgets`, `/ai/audit`
- **Impact:** ~40% of functional surface area hidden from users; backend exists for all these features
- **What's Missing:** Validation that each excluded feature is production-ready, then removal from exclusion list
- **Risk of Not Fixing:** Product delivered with massive functional gaps; enterprise value proposition severely weakened
- **Recommendation:** Systematically validate each excluded route, fix any issues, remove from exclusion list
- **Dependencies:** GAP-003 through GAP-014 (individual feature completion)
- **Complexity:** High (aggregate of many features)
- **Priority:** P0
- **Acceptance Criteria:** All 14 excluded routes validated and included in production scope

---

### GAP-003: FinOps Pages Use Demo Data (DemoBanner)
- **Module/Area:** Governance / Frontend
- **Severity:** High
- **Type:** Functional
- **Evidence:** 6 pages import and render `<DemoBanner />`: ExecutiveDrillDownPage, ServiceFinOpsPage, BenchmarkingPage, FinOpsPage, TeamFinOpsPage, DomainFinOpsPage
- **Situation:** FinOps pages display illustrative/mock data, not real persisted cost data
- **Impact:** FinOps pillar of the product is non-functional for enterprise customers
- **What's Missing:** Cost data ingestion pipeline, real data integration with CostIntelligence backend, removal of DemoBanner
- **Risk of Not Fixing:** A core enterprise capability (FinOps) exists only as a demo
- **Recommendation:** Connect FinOps pages to `CostIntelligenceEndpointModule` backend data, implement cost ingestion pipeline, remove DemoBanner
- **Dependencies:** CostIntelligence backend (exists), cost data source integration
- **Complexity:** High
- **Priority:** P1
- **Acceptance Criteria:** All 6 FinOps pages display real data from CostIntelligence backend; DemoBanner removed

---

### GAP-004: BuildingBlocks.Security Has 0 Tests
- **Module/Area:** BuildingBlocks / Security
- **Severity:** High
- **Type:** Testing
- **Evidence:** `BuildingBlocks.Security.Tests` project exists but contains 0 test files
- **Situation:** JWT validation, permission requirements, cookie session, encryption, multi-tenancy middleware completely untested at unit level
- **Impact:** Security-critical code has no regression protection
- **What's Missing:** Unit tests for JWT validation, permission requirement evaluation, cookie session handling, encryption utilities, tenant resolution
- **Risk of Not Fixing:** Security bugs could be introduced without detection
- **Recommendation:** Add comprehensive unit tests for all security building blocks
- **Dependencies:** None
- **Complexity:** Medium
- **Priority:** P1
- **Acceptance Criteria:** >80% code coverage for BuildingBlocks.Security

---

### GAP-005: AuditCompliance Module Has 0 Tests
- **Module/Area:** AuditCompliance
- **Severity:** High
- **Type:** Testing
- **Evidence:** `AuditCompliance.Tests` project exists but contains 0 test files
- **Situation:** Audit trail — a critical enterprise compliance feature — has no test coverage
- **Impact:** Audit functionality could break without detection; compliance requirements unverifiable
- **What's Missing:** Unit tests for RecordAuditEvent, QueryAuditTrail, ExportAuditReport
- **Risk of Not Fixing:** Enterprise compliance requirement cannot be verified
- **Recommendation:** Add unit tests for all 7 AuditCompliance features
- **Dependencies:** None
- **Complexity:** Low
- **Priority:** P1
- **Acceptance Criteria:** All 7 AuditCompliance features have unit test coverage

---

### GAP-006: AuditCompliance Minimal Domain Model
- **Module/Area:** AuditCompliance / Domain
- **Severity:** High
- **Type:** Functional
- **Evidence:** Only 1 entity (`AuditEvent`), 31 total .cs files in module
- **Situation:** A compliance module for enterprise needs more than event recording — needs policies, campaigns, results, scheduled reviews
- **Impact:** Compliance pillar is superficial; cannot support enterprise audit requirements
- **What's Missing:** CompliancePolicy, ComplianceResult, AuditCampaign, ScheduledReview entities and features
- **Risk of Not Fixing:** Enterprise customers cannot perform structured compliance audits
- **Recommendation:** Expand domain model to support compliance policies and structured audit campaigns
- **Dependencies:** None
- **Complexity:** Medium-High
- **Priority:** P2
- **Acceptance Criteria:** AuditCompliance module supports policy definition, campaign management, and result tracking

---

### GAP-007: CLI Tool Not Implemented
- **Module/Area:** Tools / NexTraceOne.CLI
- **Severity:** High
- **Type:** Functional
- **Evidence:** `Program.cs` — 7 TODO comments, 0 commands implemented, only ASCII banner display
- **Situation:** Developer CLI (contract validation, release management, promotion, impact analysis) is entirely a stub
- **Impact:** No developer-facing tooling for CI/CD integration or command-line operations
- **What's Missing:** Implementation of: `nex validate`, `nex release`, `nex promotion`, `nex approval`, `nex impact`, `nex tests`, `nex catalog`
- **Risk of Not Fixing:** Product lacks developer experience tooling; CI/CD integration limited to API-only
- **Recommendation:** Implement at minimum `nex validate` (contract validation) and `nex catalog` (service catalog query) as MVP CLI commands
- **Dependencies:** Module Contracts layers (already exist)
- **Complexity:** Medium
- **Priority:** P2
- **Acceptance Criteria:** At least `nex validate` and `nex catalog` commands functional

---

### GAP-008: Governance Application TODOs
- **Module/Area:** Governance / Application
- **Severity:** Medium
- **Type:** Functional
- **Evidence:** 4 TODO comments in feature handlers: GetGovernancePack (scopes), ListGovernancePacks (scope count), ListIngestionSources (LastProcessedAt), GetTeamDetail (cross-team enrichment)
- **Situation:** Governance pack features return incomplete data
- **Impact:** Governance packs lack scope counting and team detail lacks cross-team contract/dependency data
- **What's Missing:** Implementation of scope counting, LastProcessedAt field, cross-team enrichment
- **Risk of Not Fixing:** Governance module delivers incomplete information
- **Recommendation:** Implement all 4 TODOs
- **Dependencies:** None
- **Complexity:** Low-Medium
- **Priority:** P2
- **Acceptance Criteria:** All 4 TODO items resolved with working implementations

---

### GAP-009: No Rate Limiting on API Endpoints
- **Module/Area:** Platform / ApiHost
- **Severity:** Medium
- **Type:** Security
- **Evidence:** No rate limiting middleware configured in `Program.cs`
- **Situation:** All business API endpoints accept unlimited requests
- **Impact:** Vulnerable to DoS attacks, brute force on auth endpoints
- **What's Missing:** Rate limiting middleware (e.g., `Microsoft.AspNetCore.RateLimiting`)
- **Risk of Not Fixing:** Production API vulnerable to abuse
- **Recommendation:** Add rate limiting at minimum for auth endpoints, optionally for all endpoints
- **Dependencies:** None
- **Complexity:** Low
- **Priority:** P1
- **Acceptance Criteria:** Auth endpoints rate-limited; configurable policy for business endpoints

---

### GAP-010: AI TenantId Type Inconsistency
- **Module/Area:** AIKnowledge / Domain
- **Severity:** Medium
- **Type:** Architectural / Security
- **Evidence:** `AgentExecution` and `ToolInvocation` entities use `string` TenantId while all other entities use `Guid`. Manual filter in `AiRuntimeRepositories.cs:94`
- **Situation:** Global tenant query filter may not apply consistently to string-typed TenantId entities
- **Impact:** Potential cross-tenant data leakage in AI execution records
- **What's Missing:** Standardization to `Guid` TenantId; verification of global filter coverage
- **Risk of Not Fixing:** Security vulnerability — AI execution data could leak between tenants
- **Recommendation:** Standardize all TenantId fields to `Guid`; verify global filter applies; add migration
- **Dependencies:** Database migration required
- **Complexity:** Medium
- **Priority:** P1
- **Acceptance Criteria:** All AI entities use `Guid` TenantId; global filter verified; migration applied

---

### GAP-011: Automation Workflow Detail is Stub
- **Module/Area:** OperationalIntelligence / Frontend
- **Severity:** Medium
- **Type:** Functional
- **Evidence:** `AutomationWorkflowDetailPage.tsx:35` — "Workflow detail remains a preview stub"
- **Situation:** Automation workflow detail page is explicitly declared as a stub
- **Impact:** Automation module cannot show detailed execution state
- **What's Missing:** Real execution state display, audit data integration
- **Risk of Not Fixing:** Automation module incomplete
- **Recommendation:** Implement real execution state display using AutomationDbContext data
- **Dependencies:** GAP-002 (route must be included in scope)
- **Complexity:** Medium
- **Priority:** P2
- **Acceptance Criteria:** Workflow detail displays real execution state and audit trail

---

### GAP-012: Governance Tests Insufficient
- **Module/Area:** Governance / Tests
- **Severity:** Medium
- **Type:** Testing
- **Evidence:** Only 25 tests for 73 features and 175 .cs files
- **Situation:** ~34% feature coverage, far below acceptable threshold
- **Impact:** Governance module changes carry high regression risk
- **What's Missing:** Unit tests for pack management, team management, compliance checks, FinOps features
- **Risk of Not Fixing:** Governance bugs undetected
- **Recommendation:** Add tests for all key governance features, targeting >70% feature coverage
- **Dependencies:** None
- **Complexity:** Medium
- **Priority:** P2
- **Acceptance Criteria:** Governance test count >100, covering all endpoint-backed features

---

### GAP-013: No Production Deploy Pipeline
- **Module/Area:** Platform / CI/CD
- **Severity:** Medium
- **Type:** Operational
- **Evidence:** `staging.yml` exists but no `production.yml` workflow
- **Situation:** Staging delivery automated; production deployment manual
- **Impact:** Higher risk of production deployment errors; no automated rollback
- **What's Missing:** Production deployment workflow with approval gates, rollback automation
- **Risk of Not Fixing:** Production deployments are manual, error-prone, and unauditable
- **Recommendation:** Create production pipeline with manual approval gate, automated rollback capability
- **Dependencies:** Staging pipeline (exists)
- **Complexity:** Medium
- **Priority:** P2
- **Acceptance Criteria:** Production deploy workflow with approval gate and automated rollback exists

---

### GAP-014: Frontend Test Coverage (~54% of pages)
- **Module/Area:** Frontend / Tests
- **Severity:** Medium
- **Type:** Testing
- **Evidence:** 52 test files for 96 pages
- **Situation:** ~44 pages lack dedicated test files
- **Impact:** UI regressions may go undetected
- **What's Missing:** Tests for untested pages, especially governance, operations, AI hub pages
- **Risk of Not Fixing:** UI changes could break existing functionality
- **Recommendation:** Add tests for all production-scope pages
- **Dependencies:** None
- **Complexity:** Medium
- **Priority:** P3
- **Acceptance Criteria:** >80% page test coverage

---

### GAP-015: OIDC/Federated Auth Incomplete
- **Module/Area:** IdentityAccess / Auth
- **Severity:** Medium
- **Type:** Functional
- **Evidence:** Endpoints in `AuthEndpoints.cs` with AllowAnonymous for federated auth, but no provider configuration
- **Situation:** Enterprise SSO integration not functional
- **Impact:** Enterprise customers requiring SSO cannot use the platform
- **What's Missing:** OIDC provider configuration, Azure AD/Okta integration
- **Risk of Not Fixing:** Major enterprise adoption blocker
- **Recommendation:** Implement OIDC provider configuration with at least Azure AD support
- **Dependencies:** None
- **Complexity:** Medium-High
- **Priority:** P2
- **Acceptance Criteria:** OIDC login functional with at least one provider (Azure AD or Okta)

---

### GAP-016: Product Store Implementations Unverified
- **Module/Area:** BuildingBlocks / Observability
- **Severity:** Medium
- **Type:** Architectural
- **Evidence:** Interfaces defined (ITopologyWriter, IAnomalyWriter, etc.) but concrete implementations may be stubs
- **Situation:** Telemetry pipeline may not persist aggregated data
- **Impact:** Observability insights unavailable even if raw telemetry is collected
- **What's Missing:** Verification and completion of all IProductStore implementations
- **Risk of Not Fixing:** Observability pillar of the product non-functional
- **Recommendation:** Verify all IProductStore implementations are wired and functional
- **Dependencies:** None
- **Complexity:** Medium
- **Priority:** P2
- **Acceptance Criteria:** All IProductStore implementations verified with integration tests

---

### GAP-017: No Alerting Integration
- **Module/Area:** OperationalIntelligence / Observability
- **Severity:** Medium
- **Type:** Functional
- **Evidence:** No alerting gateway, webhook dispatcher, or notification system found
- **Situation:** Anomalies detected by drift detection cannot notify operators
- **Impact:** Operational issues not escalated automatically
- **What's Missing:** Alert gateway supporting PagerDuty, OpsGenie, Slack, webhooks, email
- **Risk of Not Fixing:** Operations team cannot be proactively notified of issues
- **Recommendation:** Implement alert gateway with at least webhook and email support
- **Dependencies:** None
- **Complexity:** Medium
- **Priority:** P3
- **Acceptance Criteria:** Alerts can be sent via at least 2 channels (webhook + email)

---

### GAP-018: VisualRestBuilder Hardcoded Placeholders
- **Module/Area:** Frontend / Contracts
- **Severity:** Low
- **Type:** i18n
- **Evidence:** `VisualRestBuilder.tsx:199,208,217,235,242,250` — hardcoded placeholder strings not using `t()`
- **Situation:** Minor i18n violation in contract builder
- **Impact:** Placeholders not translatable
- **What's Missing:** Extract hardcoded strings to i18n keys
- **Risk of Not Fixing:** Minor UX inconsistency for non-English users
- **Recommendation:** Replace hardcoded placeholders with `t()` calls
- **Dependencies:** None
- **Complexity:** Low
- **Priority:** P3
- **Acceptance Criteria:** All placeholder strings use `t()` function

---

### GAP-019: No Backup/Restore Strategy
- **Module/Area:** Platform / Operations
- **Severity:** Medium
- **Type:** Operational
- **Evidence:** No backup documentation or scripts found
- **Situation:** No documented or automated backup/restore for 4 PostgreSQL databases
- **Impact:** Data loss risk in production
- **What's Missing:** Backup strategy, scripts, documentation, restore runbook
- **Risk of Not Fixing:** Unrecoverable data loss on production failure
- **Recommendation:** Document backup strategy, create backup scripts, add restore runbook
- **Dependencies:** None
- **Complexity:** Low
- **Priority:** P2
- **Acceptance Criteria:** Backup strategy documented, scripts created, restore verified

---

### GAP-020: No End-User Documentation
- **Module/Area:** Documentation
- **Severity:** Low
- **Type:** Documentation
- **Evidence:** 188 markdown files are all technical/architectural; no user guides
- **Situation:** No documentation for end users (operators, engineers, admins)
- **Impact:** Users must discover product features through UI exploration
- **What's Missing:** User guides, feature documentation, getting started guide
- **Risk of Not Fixing:** Higher onboarding friction, support burden
- **Recommendation:** Create user-facing documentation for core workflows
- **Dependencies:** Functional completion of core features
- **Complexity:** Medium
- **Priority:** P3
- **Acceptance Criteria:** Getting started guide + core feature documentation exists

---

## Priority Summary

| Priority | Count | Items |
|----------|-------|-------|
| **P0 (Critical)** | 2 | GAP-001 (Outbox), GAP-002 (Excluded routes) |
| **P1 (High)** | 4 | GAP-003 (FinOps demo), GAP-004 (Security tests), GAP-005 (Audit tests), GAP-009 (Rate limiting), GAP-010 (TenantId inconsistency) |
| **P2 (Medium)** | 8 | GAP-006, GAP-007, GAP-008, GAP-011, GAP-012, GAP-013, GAP-015, GAP-016, GAP-019 |
| **P3 (Low)** | 4 | GAP-014, GAP-017, GAP-018, GAP-020 |
