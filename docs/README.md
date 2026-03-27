# NexTraceOne — Documentation Index

> **Source of Truth** for service governance, contract governance, change intelligence, operational reliability, and AI-assisted operations.

This file is the canonical entry point to the NexTraceOne documentation tree.
For the full DOCUMENTATION-INDEX see [DOCUMENTATION-INDEX.md](./DOCUMENTATION-INDEX.md).

---

## Product & Vision

| Document | Description |
|---|---|
| [PRODUCT-VISION.md](./PRODUCT-VISION.md) | Official product vision and positioning |
| [PRODUCT-SCOPE.md](./PRODUCT-SCOPE.md) | Product scope and boundaries |
| [PLATFORM-CAPABILITIES.md](./PLATFORM-CAPABILITIES.md) | Platform capability overview |
| [MODULES-AND-PAGES.md](./MODULES-AND-PAGES.md) | Module and page inventory |
| [DOMAIN-BOUNDARIES.md](./DOMAIN-BOUNDARIES.md) | DDD domain boundaries |

---

## Architecture

| Document | Description |
|---|---|
| [ARCHITECTURE-OVERVIEW.md](./ARCHITECTURE-OVERVIEW.md) | High-level system architecture |
| [DATA-ARCHITECTURE.md](./DATA-ARCHITECTURE.md) | Data architecture and persistence strategy |
| [DEPLOYMENT-ARCHITECTURE.md](./DEPLOYMENT-ARCHITECTURE.md) | Deployment and infrastructure architecture |
| [INTEGRATIONS-ARCHITECTURE.md](./INTEGRATIONS-ARCHITECTURE.md) | Integration patterns and adapters |
| [SOURCE-OF-TRUTH-STRATEGY.md](./SOURCE-OF-TRUTH-STRATEGY.md) | Source of truth principles |
| [architecture/ADR-001-database-strategy.md](./architecture/ADR-001-database-strategy.md) | ADR: Database strategy |
| [architecture/ADR-002-migration-policy.md](./architecture/ADR-002-migration-policy.md) | ADR: Migration policy |
| [architecture/ADR-003-event-bus-limitations.md](./architecture/ADR-003-event-bus-limitations.md) | ADR: Event bus limitations |
| [architecture/ADR-004-simulated-data-policy.md](./architecture/ADR-004-simulated-data-policy.md) | ADR: Simulated data policy |
| [architecture/ADR-005-ai-runtime-foundation.md](./architecture/ADR-005-ai-runtime-foundation.md) | ADR: AI runtime foundation |
| [architecture/ADR-006-agent-runtime-foundation.md](./architecture/ADR-006-agent-runtime-foundation.md) | ADR: Agent runtime foundation |
| [architecture/module-boundary-matrix.md](./architecture/module-boundary-matrix.md) | Module boundary matrix |
| [architecture/module-data-placement-matrix.md](./architecture/module-data-placement-matrix.md) | Module data placement decisions |
| [architecture/module-frontier-decisions.md](./architecture/module-frontier-decisions.md) | Module frontier decisions |
| [architecture/module-seed-strategy.md](./architecture/module-seed-strategy.md) | Module seed strategy |
| [architecture/database-table-prefixes.md](./architecture/database-table-prefixes.md) | Database table prefix conventions |
| [architecture/clickhouse-baseline-strategy.md](./architecture/clickhouse-baseline-strategy.md) | ClickHouse baseline strategy |

---

## AI

| Document | Description |
|---|---|
| [AI-ARCHITECTURE.md](./AI-ARCHITECTURE.md) | AI subsystem architecture |
| [AI-GOVERNANCE.md](./AI-GOVERNANCE.md) | AI governance policy |
| [AI-ASSISTED-OPERATIONS.md](./AI-ASSISTED-OPERATIONS.md) | AI-assisted operations capabilities |
| [AI-DEVELOPER-EXPERIENCE.md](./AI-DEVELOPER-EXPERIENCE.md) | AI developer experience and IDE integration |

---

## Contracts & Change Intelligence

| Document | Description |
|---|---|
| [CONTRACT-STUDIO-VISION.md](./CONTRACT-STUDIO-VISION.md) | Contract Studio vision and capabilities |
| [SERVICE-CONTRACT-GOVERNANCE.md](./SERVICE-CONTRACT-GOVERNANCE.md) | Service and contract governance |
| [CHANGE-CONFIDENCE.md](./CHANGE-CONFIDENCE.md) | Change confidence strategy |

---

## Security

| Document | Description |
|---|---|
| [SECURITY.md](./SECURITY.md) | Security reference |
| [SECURITY-ARCHITECTURE.md](./SECURITY-ARCHITECTURE.md) | Security architecture |
| [security/](./security/) | Security module docs |

---

## Frontend & UX

| Document | Description |
|---|---|
| [FRONTEND-ARCHITECTURE.md](./FRONTEND-ARCHITECTURE.md) | Frontend architecture |
| [DESIGN-SYSTEM.md](./DESIGN-SYSTEM.md) | Design system |
| [DESIGN.md](./DESIGN.md) | UX and visual design |
| [UX-PRINCIPLES.md](./UX-PRINCIPLES.md) | UX principles |
| [PERSONA-MATRIX.md](./PERSONA-MATRIX.md) | Persona definitions |
| [PERSONA-UX-MAPPING.md](./PERSONA-UX-MAPPING.md) | Persona-to-UX mapping |
| [BRAND-IDENTITY.md](./BRAND-IDENTITY.md) | Brand and visual identity |
| [I18N-STRATEGY.md](./I18N-STRATEGY.md) | Internationalization strategy |

---

## Engineering & Operations

| Document | Description |
|---|---|
| [BACKEND-MODULE-GUIDELINES.md](./BACKEND-MODULE-GUIDELINES.md) | Backend module and coding guidelines |
| [GUIDELINE.md](./GUIDELINE.md) | General development guidelines |
| [LOCAL-SETUP.md](./LOCAL-SETUP.md) | Local development setup |
| [ENVIRONMENT-VARIABLES.md](./ENVIRONMENT-VARIABLES.md) | Environment variable reference |
| [OBSERVABILITY-STRATEGY.md](./OBSERVABILITY-STRATEGY.md) | Observability strategy |
| [engineering/](./engineering/) | Engineering docs |
| [observability/](./observability/) | Observability docs |
| [telemetry/](./telemetry/) | Telemetry docs |
| [runbooks/](./runbooks/) | Operational runbooks |
| [deployment/](./deployment/) | Deployment docs |

---

## Quality & Testing

| Document | Description |
|---|---|
| [testing/](./testing/) | Testing docs |
| [quality/](./quality/) | Quality docs |
| [checklists/](./checklists/) | Checklists |
| [audits/](./audits/) | Audit reports |
| [assessment/](./assessment/) | Assessments |

---

## Reliability

| Document | Description |
|---|---|
| [reliability/RELIABILITY-DATA-MODEL.md](./reliability/RELIABILITY-DATA-MODEL.md) | Reliability data model |
| [reliability/RELIABILITY-SCORING-MODEL.md](./reliability/RELIABILITY-SCORING-MODEL.md) | Reliability scoring model |
| [reliability/RELIABILITY-FRONTEND-INTEGRATION.md](./reliability/RELIABILITY-FRONTEND-INTEGRATION.md) | Reliability frontend integration |

---

## User Guide & AI Knowledge

| Document | Description |
|---|---|
| [user-guide/](./user-guide/) | User guide |
| [aiknowledge/](./aiknowledge/) | AI knowledge base |

---

## Archive

Historical execution reports, phase reports, old roadmaps, and superseded planning docs are preserved in:

| Archive Subdirectory | Contents |
|---|---|
| [archive/old-reviews/](./archive/old-reviews/) | Old modular review/audit docs (298 files) |
| [archive/old-execution-plans/](./archive/old-execution-plans/) | Old wave/phase execution plans (103+ files) |
| [archive/old-phase-reports/](./archive/old-phase-reports/) | Architecture phase reports p0–p12 and e-trail/n-trail reports (143 files) |
| [archive/old-frontend-audit/](./archive/old-frontend-audit/) | Old frontend audit docs |
| [archive/old-rebaseline/](./archive/old-rebaseline/) | Old rebaseline plans |
| [archive/old-release/](./archive/old-release/) | Old release docs |
| [archive/old-reviews-ext/](./archive/old-reviews-ext/) | Old review docs |
| [archive/old-roadmaps/](./archive/old-roadmaps/) | Old roadmap and evolution plan docs |

---

## Documentation Cleanup Report

| Report | Description |
|---|---|
| [architecture/documentation-cleanup-execution-report.md](./architecture/documentation-cleanup-execution-report.md) | Summary of the 2025-07-17 cleanup |
| [architecture/documentation-cleanup-file-matrix.md](./architecture/documentation-cleanup-file-matrix.md) | Per-file operations matrix |
| [architecture/documentation-cleanup-post-gap-report.md](./architecture/documentation-cleanup-post-gap-report.md) | Post-cleanup gaps and next steps |
