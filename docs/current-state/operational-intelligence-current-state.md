# Operational Intelligence — Current State

**Maturity:** LOW — Incidents backend partial; Automation and Reliability 100% mock; correlation engine absent
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §OperationalIntelligence`, `docs/audit-forensic-2026-03/frontend-state-report.md §Operations`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| IncidentDbContext | Confirmed (with snapshot) | READY |
| AutomationDbContext | Confirmed (with snapshot) | READY |
| ReliabilityDbContext | Confirmed (with snapshot) | READY |
| RuntimeIntelligenceDbContext | Snapshot only — migration not confirmed | PARTIAL |
| CostIntelligenceDbContext | Snapshot only — migration not confirmed | PARTIAL |

Table prefix: `ops_`

---

## Features by Area

| Area | Count | Status | Notes |
|---|---|---|---|
| Incidents | 17 | PARTIAL | EfIncidentStore (678 lines) registered in DI; `InMemoryIncidentStore` is test-only (deprecated). Dynamic incident↔change correlation = 0%. Seed data is static SQL, not dynamic engine. |
| Automation | 10 | MOCK | Static catalog, workflows not persisted; handlers return `PreviewOnly` |
| Reliability | 7 | MOCK | 8 services hardcoded in handler; `ReliabilityDbContext` exists but handlers do not use it |
| Runtime Intelligence | 8+ | PARTIAL | `RuntimeIntelligenceDbContext` + EF Core repos exist; `IRuntimeIntelligenceModule` = PLAN (empty interface) |
| Cost Intelligence | 8+ | PARTIAL | `CostIntelligenceDbContext` exists; `ICostIntelligenceModule` = PLAN (empty interface); data 100% mock |

---

## Frontend Pages (9 pages — MOCK/BROKEN)

| Page | Status |
|---|---|
| IncidentsPage | MOCK — uses `mockIncidents` hardcoded inline |
| IncidentDetailPage | MOCK — static data |
| Mitigation / Runbooks / Post-action pages | STUB — no real API calls |

Comment in source: *"Dados simulados — em produção, virão da API /api/v1/incidents"*

---

## Key Gaps (Critical)

- **Correlation engine absent** — incident↔change dynamic correlation is 0%; only static seed JSON
- **Frontend not connected** — `IncidentsPage.tsx` uses `mockIncidents`, not the real incidents API
- **Runbooks hardcoded** — 3 runbooks hardcoded in code; `RunbookRecord` entity exists but unused by handlers
- **CreateMitigationWorkflow** — handler exists but does NOT persist mitigation records
- **GetMitigationHistory** — returns hardcoded static data
- **RuntimeIntelligenceDbContext / CostIntelligenceDbContext** — no confirmed deployable migrations

---

## Cross-Module Interface Status

| Interface | Status |
|---|---|
| `IRuntimeIntelligenceModule` | PLAN — empty interface |
| `ICostIntelligenceModule` | PLAN — empty interface |

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/final-project-state-assessment.md §Fluxo 3`*
