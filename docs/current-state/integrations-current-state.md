# Integrations — Current State

**Maturity:** INCOMPLETE — DbContext exists, module separated (P8.1/P8.2), connectors are stubs, no real E2E ingestion
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §Integrations`, `docs/audit-forensic-2026-03/integrations-state-report.md`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| IntegrationsDbContext | Snapshot exists — migration not confirmed | PARTIAL |

Table prefix: `int_`
Entities: `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions` (3 entities)

---

## Features

| Area | Count | Status | Notes |
|---|---|---|---|
| Integration Connectors | — | STUB | Connector handlers exist; no real connector executes E2E |
| Ingestion Sources | — | PARTIAL | 5 ingestion endpoints exist in `NexTraceOne.Ingestion.Api`; payload recorded as metadata only (`processingStatus: "metadata_recorded"`) |
| Ingestion Executions | — | STUB | Execution tracking exists; no real data processed |
| CRUD Endpoints | 8 | PARTIAL | `IntegrationHubEndpointModule` with 8 endpoints (P8.1) |

---

## Module Separation Status (P8.1/P8.2)

- `NexTraceOne.Integrations.API` project created (P8.1) — `IntegrationHubEndpointModule` (8 endpoints)
- `NexTraceOne.Integrations.Tests` project created (P8.2) — 17 tests pass
- Module fully separated from Governance: Domain / Application / Infrastructure / Contracts / API / Tests all present
- `AddIntegrationsModule()` registered in `Program.cs`

---

## Frontend Pages (4 pages — PARTIAL)

| Page | Status |
|---|---|
| ConnectorDetailPage | PARTIAL — connected; backend returns stubs |
| IngestionExecutionsPage | PARTIAL — connected; backend returns metadata-only |
| Other integrations pages | PARTIAL — connected; backend stubs |

---

## Key Gaps

- No confirmed deployable EF migration for `IntegrationsDbContext`
- No real connector executes data end-to-end (GitLab, Jenkins, GitHub, Azure DevOps all stub)
- Ingestion API records metadata but does not process payload
- Event publishing not implemented (outbox present but unprocessed)
- No seeder for integration configuration

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/integrations-state-report.md`*
