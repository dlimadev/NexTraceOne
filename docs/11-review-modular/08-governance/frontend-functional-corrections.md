# Governance Module — Frontend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Full Page Inventory (25 Pages)

| # | Page | Route | Sidebar | Permission (current) | Status |
|---|------|-------|---------|---------------------|--------|
| 1 | ExecutiveOverviewPage | `/governance/executive` | ✅ executiveOverview | `governance:read` | ✅ Active |
| 2 | ExecutiveDrillDownPage | `/governance/executive/drill-down` | — (child of executive) | `governance:read` | ✅ Active |
| 3 | ExecutiveFinOpsPage | `/governance/executive/finops` | — (child of executive) | `governance:read` | ✅ Active |
| 4 | ReportsPage | `/governance/reports` | ✅ reports | `governance:read` | ✅ Active |
| 5 | CompliancePage | `/governance/compliance` | ✅ compliance | `governance:read` | ✅ Active |
| 6 | RiskCenterPage | `/governance/risk` | ✅ riskCenter | `governance:read` | ✅ Active |
| 7 | RiskHeatmapPage | `/governance/risk/heatmap` | — (child of risk) | `governance:read` | ✅ Active |
| 8 | FinOpsPage | `/governance/finops` | ✅ finops | `governance:read` | ✅ Active |
| 9 | ServiceFinOpsPage | `/governance/finops/service` | — (child of finops) | `governance:read` | ✅ Active |
| 10 | TeamFinOpsPage | `/governance/finops/team` | — (child of finops) | `governance:read` | ✅ Active |
| 11 | DomainFinOpsPage | `/governance/finops/domain` | — (child of finops) | `governance:read` | ✅ Active |
| 12 | PolicyCatalogPage | `/governance/policies` | ✅ policies | `governance:read` | ✅ Active |
| 13 | EnterpriseControlsPage | `/governance/controls` | ❌ Not in sidebar | `governance:read` | ✅ Active |
| 14 | EvidencePackagesPage | `/governance/evidence` | ❌ Not in sidebar | `governance:read` | ✅ Active |
| 15 | MaturityScorecardsPage | `/governance/maturity` | ❌ Not in sidebar | `governance:read` | ✅ Active |
| 16 | BenchmarkingPage | `/governance/benchmarking` | ❌ Not in sidebar | `governance:read` | ✅ Active |
| 17 | TeamsOverviewPage | `/governance/teams` | ✅ teams (org section) | `governance:read` | ✅ Active |
| 18 | TeamDetailPage | `/governance/teams/:id` | — (child of teams) | `governance:read` | ✅ Active |
| 19 | DomainsOverviewPage | `/governance/domains` | ✅ domains (org section) | `governance:read` | ✅ Active |
| 20 | DomainDetailPage | `/governance/domains/:id` | — (child of domains) | `governance:read` | ✅ Active |
| 21 | GovernancePacksOverviewPage | `/governance/packs` | ✅ packs | `governance:read` | ✅ Active |
| 22 | GovernancePackDetailPage | `/governance/packs/:id` | — (child of packs) | `governance:read` | ✅ Active |
| 23 | WaiversPage | `/governance/waivers` | ❌ Not in sidebar | `governance:read` | ✅ Active |
| 24 | DelegatedAdminPage | `/governance/delegated-admin` | ❌ Not in sidebar (but accessible from config) | `platform:admin:read` | ✅ Active |
| 25 | GovernanceConfigurationPage | `/governance/configuration` | — (settings area) | `platform:admin:read` | ✅ Active |

**Summary:** 25 pages, all correctly routed under `/governance/`. All pages are active. 24 use `governance:read`, 1 uses `platform:admin:read`.

---

## 2. Route Review

| Check | Status | Notes |
|-------|--------|-------|
| All routes under `/governance/` prefix | ✅ Pass | Consistent module-level routing |
| No route conflicts | ✅ Pass | No overlapping or ambiguous routes |
| Detail pages use `:id` param | ✅ Pass | TeamDetail, DomainDetail, PackDetail |
| Nested routes follow hierarchy | ✅ Pass | Executive → DrillDown/FinOps, Risk → Heatmap, FinOps → Service/Team/Domain |
| Lazy-loaded module | ✅ Pass | Governance feature uses lazy loading |
| Route guards present | ✅ Pass | `ProtectedRoute` wraps all routes |

---

## 3. Menu / Sidebar Review

### Governance Section (7 items)

| # | Sidebar Key | Route | Icon | Status |
|---|------------|-------|------|--------|
| 1 | `executiveOverview` | `/governance/executive` | ✅ | ✅ Visible |
| 2 | `reports` | `/governance/reports` | ✅ | ✅ Visible |
| 3 | `compliance` | `/governance/compliance` | ✅ | ✅ Visible |
| 4 | `riskCenter` | `/governance/risk` | ✅ | ✅ Visible |
| 5 | `finops` | `/governance/finops` | ✅ | ✅ Visible |
| 6 | `policies` | `/governance/policies` | ✅ | ✅ Visible |
| 7 | `packs` | `/governance/packs` | ✅ | ✅ Visible |

### Organization Section (2 items)

| # | Sidebar Key | Route | Icon | Status |
|---|------------|-------|------|--------|
| 8 | `teams` | `/governance/teams` | ✅ | ✅ Visible |
| 9 | `domains` | `/governance/domains` | ✅ | ✅ Visible |

### Pages NOT in Sidebar (6 pages)

| # | Page | Route | How Accessed | Recommendation |
|---|------|-------|-------------|---------------|
| 1 | EnterpriseControlsPage | `/governance/controls` | Linked from compliance | ⚠️ **Promote to sidebar** |
| 2 | EvidencePackagesPage | `/governance/evidence` | Linked from compliance | ⚠️ **Promote to sidebar** |
| 3 | MaturityScorecardsPage | `/governance/maturity` | Linked from executive | ⚠️ **Promote to sidebar** |
| 4 | WaiversPage | `/governance/waivers` | Linked from packs/compliance | ⚠️ **Promote to sidebar** |
| 5 | BenchmarkingPage | `/governance/benchmarking` | Linked from executive | ⚠️ **Promote to sidebar** |
| 6 | DelegatedAdminPage | `/governance/delegated-admin` | Linked from configuration | ⚠️ Keep as config sub-page (admin-only) |

**Note:** DelegatedAdminPage may remain outside the sidebar since it is admin-only and accessible from the configuration area. The other 5 pages should be promoted.

---

## 4. Dashboard Review

| Dashboard | Page | Data Source | Status |
|-----------|------|-----------|--------|
| Executive Overview | ExecutiveOverviewPage | ExecutiveOverviewEndpointModule | ✅ Dedicated page with API |
| Executive Drill-Down | ExecutiveDrillDownPage | ExecutiveOverviewEndpointModule | ✅ Dedicated page with API |
| Executive FinOps | ExecutiveFinOpsPage | ExecutiveOverviewEndpointModule | ✅ Dedicated page with API |
| Compliance | CompliancePage | GovernanceComplianceEndpointModule | ✅ Dedicated page with API |
| Risk Center | RiskCenterPage | GovernanceRiskEndpointModule | ✅ Dedicated page with API |
| Risk Heatmap | RiskHeatmapPage | GovernanceRiskEndpointModule | ✅ Dedicated page with API |
| FinOps Overview | FinOpsPage | GovernanceFinOpsEndpointModule | ✅ Dedicated page with API |
| Maturity Scorecards | MaturityScorecardsPage | (via executive endpoints) | ⚠️ Uses executive API — verify data source |
| Benchmarking | BenchmarkingPage | (via executive endpoints) | ⚠️ Uses executive API — verify data source |

---

## 5. Integration with API

### API Client Files

| # | File | Lines | Endpoints Covered | Notes |
|---|------|-------|------------------|-------|
| 1 | `organizationGovernance.ts` | 126 | Teams, Domains, Packs, Waivers, DelegatedAdmin, Compliance, Policies, Controls | Main API client |
| 2 | `executive.ts` | 14 | Executive overview, drill-down, finops | Executive dashboard API |
| 3 | `finOps.ts` | 26 | FinOps overview, service, team, domain, trends | FinOps dashboard API |
| 4 | `evidence.ts` | 11 | Evidence list, evidence detail | Evidence packages API |

**Coverage:** All 25 pages have corresponding API client functions. No orphaned API calls found.

---

## 6. i18n Review

| Area | Status | Notes |
|------|--------|-------|
| Namespace `governance` exists | ✅ Present | In `en` locale |
| Page titles | ✅ Present | All 25 pages use i18n keys |
| Menu labels | ✅ Present | All 9 sidebar items use i18n keys |
| Form labels | ✅ Present | Teams, Domains, Packs, Waivers forms |
| Table headers | ✅ Present | All data tables use i18n keys |
| Empty states | ⚠️ Partial | Some pages may use hardcoded empty state text — verify |
| Error messages | ⚠️ Partial | API error messages may not all be i18n-mapped |
| Tooltips | ⚠️ Partial | Dashboard tooltips need verification |
| Other locales | ❌ Missing | Only `en` locale present — `pt` not yet added |

---

## 7. Buttons Without Action

| # | Page | Button/Action | Issue | Priority |
|---|------|--------------|-------|----------|
| 1 | PolicyCatalogPage | No "Create Policy" button | Backend has no POST endpoint | **HIGH** |
| 2 | PolicyCatalogPage | No "Edit" action on policy rows | Backend has no PUT endpoint | **HIGH** |
| 3 | PolicyCatalogPage | No "Delete" action on policy rows | Backend has no DELETE endpoint | **HIGH** |
| 4 | EvidencePackagesPage | No "Submit Evidence" button | Backend has no POST endpoint | **MEDIUM** |
| 5 | EnterpriseControlsPage | No "Create Control" button | Backend has no POST endpoint | **MEDIUM** |

**Note:** These are not broken buttons — they are missing UI actions because the backend endpoints do not exist yet. Once backend CRUD is implemented, corresponding frontend actions must be added.

---

## 8. Pages Belonging to Other Modules

### Integrations Pages

**None in governance feature folder.** Frontend already has a separate `features/integrations/` folder with:
- 4 pages
- Own route configuration
- Uses `integrations:read` permission

✅ No extraction needed on frontend.

### Product Analytics Pages

**None in governance feature folder.** Frontend already has a separate `features/product-analytics/` folder with:
- 5 pages
- Own route configuration
- Uses `analytics:read` permission

✅ No extraction needed on frontend.

---

## 9. Legacy Content

**None found.** The governance feature folder contains only governance-related pages, components, and utilities. No legacy or deprecated content detected.

---

## 10. Corrections Backlog

### HIGH Priority

| # | Correction | Type | Rationale |
|---|-----------|------|-----------|
| 1 | Align frontend permissions with backend granularity | Security | Frontend uses single `governance:read` for 24 routes; backend has 12+ specific permissions — frontend must check matching permissions per page/action |
| 2 | Promote 5 pages to sidebar | Navigation | Controls, Evidence, Maturity, Waivers, and Benchmarking pages are not discoverable without direct links |
| 3 | Add write actions to PolicyCatalogPage | Feature gap | Once backend policy CRUD is implemented, add Create/Edit/Delete buttons |

### MEDIUM Priority

| # | Correction | Type | Rationale |
|---|-----------|------|-----------|
| 4 | Add evidence submission UI to EvidencePackagesPage | Feature gap | Once backend evidence POST is implemented, add submission form |
| 5 | Add controls management UI to EnterpriseControlsPage | Feature gap | Once backend controls CRUD is implemented, add management actions |
| 6 | Verify all 25 pages have loading, error, and empty states | UX consistency | Ensure consistent user experience across all governance pages |
| 7 | Verify MaturityScorecards and Benchmarking use dedicated data (not just executive reuse) | Data integrity | These pages may need dedicated API endpoints for complete data |

### LOW Priority

| # | Correction | Type | Rationale |
|---|-----------|------|-----------|
| 8 | Document all 25 pages with component descriptions | Documentation | No page-level documentation exists |
| 9 | Add `pt` locale translations for governance namespace | i18n | Only English locale is currently present |
| 10 | Review empty state text for i18n compliance | i18n | Some empty states may use hardcoded text |
| 11 | Review tooltip text for i18n compliance | i18n | Dashboard tooltips need verification |

---

## Summary

The Governance frontend is well-structured with 25 pages properly organized under the governance feature folder. Frontend has already correctly separated Integrations and Product Analytics into their own feature folders. The primary issues are:

1. **Permission model mismatch** — frontend uses a single generic `governance:read` permission while backend enforces granular permissions per endpoint
2. **5 pages missing from sidebar** — limits discoverability for users
3. **Read-only pages** for policies, evidence, and controls — need write actions once backend supports them
4. **i18n completeness** — English keys present but completeness and other locales need attention

All corrections are documented and prioritized for implementation in subsequent phases.
