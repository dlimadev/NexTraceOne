# FinOps — Current State

**Maturity:** MOCK — CostIntelligenceDbContext exists; all data fabricated; ICostIntelligenceModule = PLAN; frontend shows simulated data with DemoBanner
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §OperationalIntelligence`, `docs/audit-forensic-2026-03/capability-gap-matrix.md §FinOps`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| CostIntelligenceDbContext | Snapshot only — migration not confirmed | PARTIAL |
| GovernanceDbContext | Confirmed (with snapshot) | READY* |

*GovernanceDbContext deployable; FinOps data it serves is 100% fabricated (`IsSimulated: true`).
Table prefix: `ops_` (CostIntelligence), `gov_` (Governance FinOps views)

---

## Features

| Area | Status | Notes |
|---|---|---|
| Cost data ingestion | MOCK | No real cost data pipeline; `CostIntelligenceDbContext` schema exists but no migration deployed |
| Cost by service | MOCK | Fabricated values (`IsSimulated: true`) |
| Cost by team / domain | MOCK | Fabricated values |
| Cost by environment | MOCK | Fabricated values |
| FinOps trend analysis | MOCK | Hardcoded trends |
| Cost↔change correlation | MOCK | `ICostIntelligenceModule` = PLAN (empty interface); no cross-module wiring |
| ClickHouse analytics pipeline | INCOMPLETE | Schema defined in `build/clickhouse/`; no confirmed end-to-end pipeline from PostgreSQL to ClickHouse |

---

## Frontend Pages (5 pages — CONNECTED to mock backend)

| Page | Status |
|---|---|
| FinOpsPage | CONNECTED — backend returns `IsSimulated: true` |
| TeamFinOpsPage | CONNECTED — backend returns `IsSimulated: true` |
| DomainFinOpsPage | CONNECTED — backend returns `IsSimulated: true` |
| ServiceFinOpsPage | CONNECTED — backend returns `IsSimulated: true` |
| ExecutiveFinOpsPage | CONNECTED — backend returns `IsSimulated: true` |

All 5 pages display `DemoBanner` component when receiving `IsSimulated: true` from backend.

---

## Key Gaps (Critical)

- **All FinOps data is fabricated** — every value shown in UI is hardcoded/simulated
- **`ICostIntelligenceModule`** — empty interface; no cross-module calls possible
- **`CostIntelligenceDbContext`** — no confirmed deployable EF migration
- **ClickHouse pipeline** — schema defined; ingest pipeline from service telemetry not operational
- **No cost↔service correlation** — cannot attribute cost to real service/change/team data
- **Governance as FinOps proxy** — Governance module serves FinOps pages but has no real data source

---

## Related Module

Commercial Governance (licensing/entitlements) was removed in PR-17. No replacement implemented.

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/capability-gap-matrix.md`, `docs/audit-forensic-2026-03/database-state-report.md §ClickHouse`*
