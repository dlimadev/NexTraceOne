# Phase 0 — Excluded Production Surface Map

**Date:** 2026-03-22
**Source:** `src/frontend/src/releaseScope.ts` — `finalProductionExcludedRoutePrefixes`

---

## 1. Exclusion Mechanism

The release scope is controlled by two arrays in `releaseScope.ts`:
- `finalProductionIncludedRoutePrefixes` — 33 route prefixes allowed in production
- `finalProductionExcludedRoutePrefixes` — 14 route prefixes explicitly excluded

A route is available in production only if it matches an included prefix **AND** does not match an excluded prefix. This is a well-designed whitelist + blacklist pattern.

---

## 2. Excluded Surface Matrix

| # | Route/Prefix | Area/Module | Backend | Frontend | Persistence | Real State | Probable Exclusion Reason | Recovery Phase |
|---|-------------|-------------|---------|----------|-------------|------------|--------------------------|----------------|
| 1 | `/portal` | Developer Portal (Catalog) | ✅ `DeveloperPortalEndpointModule` | ✅ `DeveloperPortalPage` | ✅ `DeveloperPortalDbContext` | Partial | Portal features incomplete, search/discovery not connected | Phase 3 |
| 2 | `/governance/teams` | Teams (Governance) | ✅ `TeamEndpointModule` | ✅ `TeamsOverviewPage`, `TeamDetailPage` | ✅ `GovernanceDbContext` | Partial | Team ownership/responsibility features incomplete | Phase 2 |
| 3 | `/governance/packs` | Governance Packs | ✅ `GovernancePacksEndpointModule` | ✅ `GovernancePacksOverviewPage`, `GovernancePackDetailPage` | ✅ `GovernanceDbContext` | Partial | Pack definition/assignment workflow incomplete | Phase 3 |
| 4 | `/integrations/executions` | Ingestion Executions | ✅ `IntegrationHubEndpointModule` | ✅ `IngestionExecutionsPage` | ✅ Various | Partial | Execution tracking not fully integrated | Phase 3 |
| 5 | `/analytics/value` | Value Tracking (Analytics) | ✅ `ProductAnalyticsEndpointModule` | ✅ `ValueTrackingPage` | ✅ Various | Demo | Uses simulated/demo data | Phase 4 |
| 6 | `/operations/runbooks` | Runbooks (OI) | ✅ `RunbookEndpointModule` | ✅ `RunbooksPage` | ✅ Various | Partial | Runbook execution engine incomplete | Phase 3 |
| 7 | `/operations/reliability` | Service Reliability (OI) | ✅ `ReliabilityEndpointModule` | ✅ `TeamReliabilityPage`, `ServiceReliabilityDetailPage` | ✅ `ReliabilityDbContext` | Partial | SLI/SLO calculation not fully connected | Phase 3 |
| 8 | `/operations/automation` | Automation (OI) | ✅ `AutomationEndpointModule` | ✅ `AutomationWorkflowsPage`, `AutomationAdminPage`, `AutomationWorkflowDetailPage` | ✅ `AutomationDbContext` | Partial | Automation engine not connected to real triggers | Phase 4 |
| 9 | `/ai/models` | AI Model Registry | ✅ `AiGovernanceEndpointModule` | ✅ `ModelRegistryPage` | ✅ `AiGovernanceDbContext` | Partial | Model registry CRUD exists but integration with runtime routing incomplete | Phase 3 |
| 10 | `/ai/policies` | AI Policies | ✅ `AiGovernanceEndpointModule` | ✅ `AiPoliciesPage` | ✅ `AiGovernanceDbContext` | Partial | Policy engine exists but enforcement not fully wired | Phase 3 |
| 11 | `/ai/routing` | AI Routing | ✅ `AiGovernanceEndpointModule` | ✅ `AiRoutingPage` | ✅ `AiGovernanceDbContext` | Stub | Routing configuration UI exists, backend routing logic incomplete | Phase 4 |
| 12 | `/ai/ide` | IDE Integrations | ✅ `AiIdeEndpointModule` | ✅ `IdeIntegrationsPage` | ✅ Various | Stub | IDE extension management UI exists, no real IDE integration | Phase 5 |
| 13 | `/ai/budgets` | AI Token Budgets | ✅ `AiGovernanceEndpointModule` | ✅ `TokenBudgetPage` | ✅ `AiGovernanceDbContext` | Partial | Budget tracking exists but enforcement not connected to runtime | Phase 4 |
| 14 | `/ai/audit` | AI Audit | ✅ `AiGovernanceEndpointModule` | ✅ `AiAuditPage` | ✅ Various | Partial | Audit trail collection partial, query/reporting incomplete | Phase 3 |

---

## 3. Summary by State

| State | Count | Routes |
|-------|-------|--------|
| **Partial** | 11 | Most routes — backend+frontend exist but business logic incomplete |
| **Demo** | 1 | `/analytics/value` — uses simulated data |
| **Stub** | 2 | `/ai/routing`, `/ai/ide` — UI shell exists, backend logic minimal |

---

## 4. Summary by Recovery Phase

| Phase | Routes | Rationale |
|-------|--------|-----------|
| Phase 2 | `/governance/teams` | Team ownership is foundational for governance |
| Phase 3 | `/portal`, `/governance/packs`, `/integrations/executions`, `/operations/runbooks`, `/operations/reliability`, `/ai/models`, `/ai/policies`, `/ai/audit` | Core product features with partially complete backends |
| Phase 4 | `/analytics/value`, `/operations/automation`, `/ai/routing`, `/ai/budgets` | Features requiring more infrastructure or demo data replacement |
| Phase 5 | `/ai/ide` | External integration dependency (IDE extensions) |

---

## 5. Key Findings

1. **All 14 excluded routes have complete frontend pages** — no UI work needed for re-activation
2. **All 14 have corresponding backend API endpoints** — no new endpoints needed, but business logic may need completion
3. **All 14 have database persistence** — models and migrations exist
4. **No route is excluded without reason** — each exclusion correlates with incomplete backend logic or demo-quality data
5. **The exclusion mechanism is clean and reversible** — removing a prefix from `finalProductionExcludedRoutePrefixes` is the only frontend change needed
6. **Backend readiness varies** — some routes need only outbox fix + minor backend hardening, others need significant logic completion

---

## 6. Dependencies for Recovery

| Dependency | Affected Routes | Must Be Resolved Before Recovery |
|------------|----------------|----------------------------------|
| Outbox cross-module (GAP-001) | All 14 routes | Yes — event propagation needed for most features |
| Test coverage for Governance | `/governance/teams`, `/governance/packs` | Yes — currently only 27 tests for major module |
| AI model runtime integration | `/ai/models`, `/ai/routing`, `/ai/budgets` | Yes — model registry ↔ runtime flow |
| Automation trigger engine | `/operations/automation` | Yes — workflow execution engine |
| SLI/SLO data pipeline | `/operations/reliability` | Yes — needs real telemetry data |
