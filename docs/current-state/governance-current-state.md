# Governance — Current State

**Maturity:** MOCK — 100% simulated by design; GovernanceDbContext exists but all 74 handlers return `IsSimulated: true`
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §Governance`, `docs/audit-forensic-2026-03/frontend-state-report.md §Governance`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| GovernanceDbContext | Confirmed (with snapshot) | READY* |

*Schema deployable; module intentionally returns mock data — no real persistence logic.
Table prefix: `gov_`

---

## Features (74 handlers, 100% mock)

| Area | Handler Count | Status | Notes |
|---|---|---|---|
| Teams & Domains | ~10 | MOCK | `IsSimulated: true`, `DataSource: "demo"`. Sole exception: `ICatalogGraphModule` called for real `ServiceCount` in Teams/Domains |
| Governance Packs / Evidence | ~12 | MOCK | All hardcoded |
| Policies / Compliance | ~10 | MOCK | No real persistence |
| Reports / Maturity Scorecards | ~10 | MOCK | Fabricated data |
| Waivers | ~5 | MOCK | Hardcoded |
| Risk / Benchmarking | ~8 | MOCK | Fabricated |
| Executive Views | ~5 | MOCK | Fabricated aggregates |
| FinOps (via Governance) | ~14 | MOCK | See also `finops-current-state.md` |

---

## Frontend Pages (25 pages — CONNECTED to mock backend)

All 25 pages call real API endpoints. Backend responds with `IsSimulated: true`. Frontend renders `DemoBanner` when `IsSimulated: true`.

| Area | Pages | Backend Status |
|---|---|---|
| Teams & Domains | TeamsOverviewPage, DomainsOverviewPage, TeamDetailPage, DomainDetailPage | MOCK |
| FinOps | FinOpsPage, TeamFinOpsPage, DomainFinOpsPage, ServiceFinOpsPage, ExecutiveFinOpsPage | MOCK |
| Executive | ExecutiveOverviewPage, ExecutiveDrillDownPage | MOCK |
| Compliance | CompliancePage, EnterpriseControlsPage, RiskHeatmapPage | MOCK |
| Governance Packs | GovernancePacksOverviewPage, GovernancePackDetailPage | MOCK |
| Policies / Waivers / Evidence | PolicyCatalogPage, WaiversPage, EvidencePackagesPage | MOCK / PARTIAL |
| Reports | ReportsPage, MaturityScorecardsPage, BenchmarkingPage | MOCK |

---

## Key Gaps

- All demo data is fabricated — no actual governance data is captured or stored
- `ICatalogGraphModule` is the only live cross-module call (real `ServiceCount`)
- `IChangeIntelligenceModule`, `IPromotionModule` not implemented — Governance cannot read real change data
- Module separation complete: Integrations and ProductAnalytics moved out in P8.x

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/capability-gap-matrix.md`*
