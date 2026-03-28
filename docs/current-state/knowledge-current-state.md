# Knowledge — Current State

**Maturity:** INCOMPLETE — Module created (P10.1–P10.3), DbContext exists, endpoints functional, no EF migrations generated
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §Knowledge`, `docs/audit-forensic-2026-03/database-state-report.md`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| KnowledgeDbContext | No confirmed migrations | INCOMPLETE |

Table prefix: `knw_`
Tables (defined in DbContext, not yet deployed): `knw_documents`, `knw_operational_notes`, `knw_relations`, `knw_outbox_messages`

---

## Entities

| Entity | Table | Status |
|---|---|---|
| KnowledgeDocument | `knw_documents` | Created (P10.1) |
| OperationalNote | `knw_operational_notes` | Created (P10.1); includes `NoteType`, `Origin` metadata, `UpdateContext` (P10.2) |
| KnowledgeRelation | `knw_relations` | Created (P10.1); enum source type, optional context, query by target type+entity (P10.2) |

---

## Features

| Area | Status | Notes |
|---|---|---|
| Create KnowledgeDocument | PARTIAL | Endpoint exists (P10.3); no migration = not deployable |
| Create OperationalNote | PARTIAL | Endpoint exists (P10.3); no migration = not deployable |
| Create KnowledgeRelation | PARTIAL | Endpoint exists (P10.3); no migration = not deployable |
| Query relations by target | PARTIAL | `GetKnowledgeByRelationTarget` handler (P10.3) |
| Query relations by source | PARTIAL | `GetKnowledgeByRelationSource` handler (P10.3) |
| Full-text search (FTS) | NOT WIRED | PostgreSQL FTS switched in catalog (P10.2); Knowledge FTS not yet wired |
| Cross-module relations (Service/Contract/Change/Incident) | PARTIAL | `KnowledgeRelation` supports Service/Contract/Change/Incident linkage; cross-module interfaces not implemented |
| Status endpoint | READY | `/api/v1/knowledge/status` (P10.1) |

---

## Frontend Pages

Knowledge Hub frontend pages: not audited as part of March 2026 audit — assumed PARTIAL/INCOMPLETE given no migrations.

---

## Key Gaps

- **No EF migration** — `KnowledgeDbContext` schema not deployable until `dotnet ef migrations add` is run
- **FTS not wired** — Full-text search infrastructure exists in catalog (PostgreSQL FTS via P10.2) but not connected to Knowledge module
- **Cross-module interfaces** — `IKnowledgeModule` defined; zero cross-module callers
- **Outbox** — `knw_outbox_messages` table defined but outbox not processed

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/database-state-report.md §Gap 2`*
