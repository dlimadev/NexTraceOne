# Governance Module — Scope Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Existing Functionality

### Fully Implemented (all have backend endpoints + frontend pages)

| # | Capability | Backend | Frontend | Status |
|---|-----------|---------|----------|--------|
| 1 | Teams CRUD | TeamEndpointModule (4 endpoints) | TeamsOverviewPage, TeamDetailPage | ✅ |
| 2 | Domains CRUD | DomainEndpointModule (4 endpoints) | DomainsOverviewPage, DomainDetailPage | ✅ |
| 3 | Governance Packs lifecycle | GovernancePacksEndpointModule (9 endpoints) | GovernancePacksOverviewPage, GovernancePackDetailPage | ✅ |
| 4 | Governance Waivers | GovernanceWaiversEndpointModule (5 endpoints) | WaiversPage | ✅ |
| 5 | Delegated Administration | DelegatedAdminEndpointModule (2 endpoints) | DelegatedAdminPage | ✅ |
| 6 | Executive Overview | ExecutiveOverviewEndpointModule (3 endpoints) | ExecutiveOverviewPage, ExecutiveDrillDownPage, ExecutiveFinOpsPage | ✅ |
| 7 | Compliance Summary | GovernanceComplianceEndpointModule (3 endpoints) | CompliancePage | ✅ |
| 8 | Risk Analysis | GovernanceRiskEndpointModule (2 endpoints) | RiskCenterPage, RiskHeatmapPage | ✅ |
| 9 | FinOps Governance | GovernanceFinOpsEndpointModule (5 endpoints) | FinOpsPage, ServiceFinOps, TeamFinOps, DomainFinOps | ✅ |
| 10 | Policy Catalog | PolicyCatalogEndpointModule (2 endpoints) | PolicyCatalogPage | ✅ |
| 11 | Enterprise Controls | EnterpriseControlsEndpointModule (1 endpoint) | EnterpriseControlsPage | ✅ |
| 12 | Evidence Packages | EvidencePackagesEndpointModule (2 endpoints) | EvidencePackagesPage | ✅ |
| 13 | Reports | GovernanceReportsEndpointModule (1 endpoint) | ReportsPage | ✅ |
| 14 | Maturity Scorecards | (via executive) | MaturityScorecardsPage | ✅ |
| 15 | Benchmarking | (via executive) | BenchmarkingPage | ✅ |
| 16 | Scoped Context | ScopedContextEndpointModule (1 endpoint) | (internal) | ✅ |
| 17 | Governance Configuration | (via Configuration module) | GovernanceConfigurationPage | ✅ |

---

## 2. Partially Implemented Functionality

| # | Capability | Gap | Priority |
|---|-----------|-----|----------|
| 1 | Compliance Checks execution | `RunComplianceChecks` handler exists but compliance rules are read-model/computed, not persisted entities | MEDIUM |
| 2 | GovernanceRuleBinding | Entity exists but no DbSet in GovernanceDbContext | MEDIUM |
| 3 | Policy CRUD | PolicyCatalog is read-only (2 GET endpoints) — no create/update/delete | HIGH |
| 4 | Evidence CRUD | Evidence is read-only (2 GET endpoints) — no create | MEDIUM |
| 5 | Controls CRUD | Controls is read-only (1 GET endpoint) — no create/update | MEDIUM |
| 6 | Pack rollout tracking | GovernanceRolloutRecord entity exists with DbSet but limited handler support | LOW |

---

## 3. Functionality That Must Leave (Not Governance)

| Capability | Currently In | Must Go To | Status |
|-----------|-------------|-----------|--------|
| Integration connector management | GovernanceDbContext + IntegrationHubEndpointModule | Integrations module | Pending extraction (OI-02) |
| Ingestion source/execution tracking | GovernanceDbContext + IntegrationHubEndpointModule | Integrations module | Pending extraction (OI-02) |
| Product analytics event recording | GovernanceDbContext + ProductAnalyticsEndpointModule | Product Analytics module | Pending extraction (OI-03) |
| Platform status/health monitoring | PlatformStatusEndpointModule | Operational Intelligence or Platform | Evaluate |
| Onboarding context | OnboardingEndpointModule | Platform or Identity | Evaluate |

---

## 4. Scope Assessment by Area

### 4.1 Policies and Governance Rules
- **Current:** Read-only policy catalog
- **Required:** Full CRUD for policies, enforcement modes, severity levels, categories
- **Gap:** Policy creation/update endpoints missing

### 4.2 Exceptions and Waivers
- **Current:** Full CRUD + approval workflow for waivers ✅
- **Required:** Complete
- **Gap:** None significant

### 4.3 Compliance Reports
- **Current:** Compliance summary endpoint + CompliancePage
- **Required:** Complete with evidence linking
- **Gap:** Minor — may need more reporting dimensions

### 4.4 Evidence
- **Current:** Read-only evidence packages
- **Required:** Evidence creation, linking to compliance checks
- **Gap:** Create evidence endpoint missing

### 4.5 Reviews and Approvals (Compliance)
- **Current:** Waiver approval/rejection workflow ✅
- **Required:** Complete
- **Gap:** None

### 4.6 Governance Dashboards
- **Current:** Executive overview, compliance, risk, FinOps dashboards ✅
- **Required:** Complete
- **Gap:** Validate data is real vs computed stubs

### 4.7 Risk/Compliance Matrices
- **Current:** Risk center + heatmap ✅
- **Required:** Complete
- **Gap:** None significant

### 4.8 Regulatory Artifacts
- **Current:** Evidence packages + compliance reports
- **Required:** Adequate for MVP
- **Gap:** Could expand in future with regulatory templates

### 4.9 Executive Status and Reports
- **Current:** Executive overview, drilldown, trends ✅
- **Required:** Complete
- **Gap:** Verify persona-based filtering (should restrict to Executive/Tech Lead)

### 4.10 What Is NOT Governance
- ❌ Integration connector management → Integrations
- ❌ Product usage analytics → Product Analytics
- ❌ Platform health monitoring → Operational Intelligence
- ❌ User onboarding → Platform

---

## 5. Minimum Complete Module Definition

### Must Have (blocks closure)

1. ✅ All 25 pages routed and accessible
2. ⬜ Policy CRUD endpoints (create, update, delete policies)
3. ⬜ Evidence creation endpoint
4. ⬜ Frontend permission granularity (replace generic `governance:read` with specific permissions)
5. ⬜ GovernanceRuleBinding added to DbSet
6. ⬜ RowVersion/xmin on Team, GovernanceDomain, GovernancePack, GovernanceWaiver
7. ⬜ Validate executive dashboards use real data
8. ⬜ Extract Integrations entities/endpoints (documented, ready for physical extraction)
9. ⬜ Extract Product Analytics entities/endpoints (documented, ready for physical extraction)

### Should Have

10. ⬜ Controls CRUD endpoints
11. ⬜ Promote Controls, Evidence, Maturity, Waivers, Benchmarking to sidebar
12. ⬜ Fix DelegatedAdmin POST permission (`platform:admin:write` instead of `platform:admin:read`)
13. ⬜ Fix Onboarding permission (own permission instead of `governance:teams:read`)
14. ⬜ Governance packs seed data
15. ⬜ Module README documentation

### Nice to Have

16. ⬜ Persona-based executive view filtering
17. ⬜ Filtered indexes (`WHERE is_deleted = false`)
18. ⬜ Check constraints for enums
