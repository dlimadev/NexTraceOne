# 04 — Hidden, Removed, and Incomplete Features

**Date:** 2026-03-22

---

## Methodology

Features were identified as hidden/incomplete by cross-referencing:
1. `releaseScope.ts` excluded route prefixes
2. `DemoBanner` component usage
3. `preview` badges in sidebar navigation
4. TODO/FIXME comments in backend code
5. Backend endpoints with no frontend consumption
6. Frontend pages with no real backend data

---

## Features Excluded from Production Scope (releaseScope.ts)

These features have **full backend + frontend implementations** but are explicitly excluded from the production build scope:

### HF-01: Developer Portal
- **Route:** `/portal`
- **Backend:** `DeveloperPortalEndpointModule` (Catalog module), `DeveloperPortalDbContext`
- **Frontend:** `DeveloperPortalPage.tsx`, `ContractPortalPage.tsx`
- **Entities:** DeveloperPortalAsset
- **State:** Backend and frontend exist. Excluded from production.
- **Recommendation:** Complete and include. Portal is core to contract governance value proposition.
- **Severity:** High

### HF-02: Governance Teams
- **Route:** `/governance/teams`
- **Backend:** `TeamEndpointModule` with full CRUD
- **Frontend:** `TeamsOverviewPage.tsx`, `TeamDetailPage.tsx`
- **Entities:** Team entity in Governance module
- **State:** Backend and frontend exist. Excluded from production.
- **Recommendation:** Complete and include. Team management is foundational for ownership.
- **Severity:** High

### HF-03: Governance Packs
- **Route:** `/governance/packs`
- **Backend:** `GovernancePacksEndpointModule` with 8 endpoints
- **Frontend:** `GovernancePacksOverviewPage.tsx`, `GovernancePackDetailPage.tsx`
- **Entities:** GovernancePack entity
- **Application TODOs:** `GetGovernancePack.cs:57` — "TODO: implementar Scopes quando tiver escopo completo no domínio". `ListGovernancePacks.cs:54` — "TODO: enriquecer com contagem real de scopes"
- **State:** Backend mostly complete (2 TODOs), frontend exists. Excluded from production.
- **Recommendation:** Close TODOs and include.
- **Severity:** High

### HF-04: AI Model Registry
- **Route:** `/ai/models`
- **Backend:** AiGovernance endpoints, AiModel entity with full CRUD
- **Frontend:** `ModelRegistryPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production. Core to AI governance pillar.
- **Severity:** High

### HF-05: AI Policies
- **Route:** `/ai/policies`
- **Backend:** AiPolicy entity, governance endpoints
- **Frontend:** `AiPoliciesPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** High

### HF-06: AI Routing
- **Route:** `/ai/routing`
- **Backend:** AiRoutingRule entity, governance endpoints
- **Frontend:** `AiRoutingPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** Medium

### HF-07: AI IDE Integrations
- **Route:** `/ai/ide`
- **Backend:** AiIdeEndpointModule
- **Frontend:** `IdeIntegrationsPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** Medium

### HF-08: AI Token Budget
- **Route:** `/ai/budgets`
- **Backend:** AiTokenBudget entity, governance endpoints
- **Frontend:** `TokenBudgetPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** Medium

### HF-09: AI Audit
- **Route:** `/ai/audit`
- **Backend:** AiAuditEntry entity, governance endpoints
- **Frontend:** `AiAuditPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production. Critical for AI governance compliance.
- **Severity:** High

### HF-10: Operations — Runbooks
- **Route:** `/operations/runbooks`
- **Backend:** `RunbookEndpointModule`, Runbook entity
- **Frontend:** `RunbooksPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production. Essential for operational readiness.
- **Severity:** High

### HF-11: Operations — Team Reliability
- **Route:** `/operations/reliability`
- **Backend:** `ReliabilityEndpointModule`, ReliabilityScore entity, ReliabilityDbContext
- **Frontend:** `TeamReliabilityPage.tsx`, `ServiceReliabilityDetailPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** High

### HF-12: Operations — Automation
- **Route:** `/operations/automation`
- **Backend:** `AutomationEndpointModule`, AutomationWorkflow entity, AutomationDbContext
- **Frontend:** `AutomationWorkflowsPage.tsx`, `AutomationWorkflowDetailPage.tsx`, `AutomationAdminPage.tsx`
- **Note:** `AutomationWorkflowDetailPage.tsx:35` states: "Workflow detail remains a preview stub until the execution model is backed by real automation state and audit data."
- **State:** Backend exists, frontend partially stub. Excluded.
- **Recommendation:** Complete execution model and include.
- **Severity:** High

### HF-13: Ingestion Executions
- **Route:** `/integrations/executions`
- **Backend:** Executions tracked via IngestionExecution entity
- **Frontend:** `IngestionExecutionsPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** Medium

### HF-14: Analytics — Value Tracking
- **Route:** `/analytics/value`
- **Backend:** ProductAnalytics endpoints
- **Frontend:** `ValueTrackingPage.tsx`
- **State:** Implemented. Excluded.
- **Recommendation:** Include in production.
- **Severity:** Low

---

## Features with DemoBanner (Illustrative/Non-Persisted Data)

These pages display a `DemoBanner` component indicating the data shown is illustrative:

| # | Page | File | Module |
|---|------|------|--------|
| DB-01 | Executive Drill-Down | `governance/pages/ExecutiveDrillDownPage.tsx:76` | Governance |
| DB-02 | Service FinOps | `governance/pages/ServiceFinOpsPage.tsx:61` | Governance |
| DB-03 | Benchmarking | `governance/pages/BenchmarkingPage.tsx:66` | Governance |
| DB-04 | FinOps Overview | `governance/pages/FinOpsPage.tsx:97` | Governance |
| DB-05 | Team FinOps | `governance/pages/TeamFinOpsPage.tsx:70` | Governance |
| DB-06 | Domain FinOps | `governance/pages/DomainFinOpsPage.tsx:70` | Governance |

**Impact:** All FinOps and benchmarking pages show mock/demo data. These pages exist with backend endpoints but the data pipeline is not connected to real cost/performance data.

**Recommendation:** Implement real data integration via cost ingestion pipeline and remove DemoBanner.

---

## Backend TODOs

| # | File | Line | TODO Content | Severity |
|---|------|------|-------------|----------|
| T-01 | `Governance/GetGovernancePack.cs` | 57 | "implementar Scopes quando tiver escopo completo no domínio" | Medium |
| T-02 | `Governance/ListGovernancePacks.cs` | 54 | "enriquecer com contagem real de scopes (future work)" | Medium |
| T-03 | `Governance/ListIngestionSources.cs` | 83 | "add LastProcessedAt field" | Low |
| T-04 | `Governance/GetTeamDetail.cs` | 34 | "enriquecer com dados reais de contratos e dependências cross-team" | Medium |
| T-05 | `CLI/Program.cs` | 14-20 | 7 TODO commands: validate, release, promotion, approval, impact, tests, catalog | High |

---

## Stub / Preview Features

| # | Feature | Evidence | Status |
|---|---------|----------|--------|
| S-01 | Automation Workflow Detail | `AutomationWorkflowDetailPage.tsx:35` — stated as "preview stub" | Stub |
| S-02 | CLI Tool | `Program.cs` — only banner display, 0 commands | Stub |
| S-03 | SOAP Contract Studio | `SoapDesign`/`SoapContractDraft` enums exist but no visual builder | Structure only |
| S-04 | Kafka/Event Contract Builder | Type defined but no dedicated visual builder | Structure only |

---

## Summary

| Category | Count |
|----------|-------|
| Features excluded from production scope | 14 route prefixes |
| Pages with DemoBanner (demo data) | 6 pages |
| Backend TODOs in application code | 5 |
| Stub/preview features | 4 |
| **Total hidden/incomplete features** | **29 items** |

All items listed above have backend and/or frontend code that exists in the repository. None should be deleted. All should be completed and included in the final production scope.
