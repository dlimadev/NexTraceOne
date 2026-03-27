# Documentation Cleanup — File Operations Matrix

**Date:** 2025-07-17
**Scope:** Full documentation cleanup — NexTraceOne

---

## Directory-Level Operations

| Path | Action | Destination | Reason |
|---|---|---|---|
| `docs/11-review-modular/` (298 files) | Archived | `docs/archive/old-reviews/` | Old modular review/audit docs from prior phases |
| `docs/execution/` (103 files) | Archived | `docs/archive/old-execution-plans/` | Old wave/phase execution plans |
| `docs/frontend-audit/` (8 files) | Archived | `docs/archive/old-frontend-audit/` | Old frontend audit reports |
| `docs/rebaseline/` (2 files) | Archived | `docs/archive/old-rebaseline/` | Old rebaseline plans |
| `docs/release/` (9 files) | Archived | `docs/archive/old-release/` | Old release docs and notes |
| `docs/reviews/` (3 files) | Archived | `docs/archive/old-reviews-ext/` | Old review reports |
| `docs/roadmap/` (1 file) | Archived | `docs/archive/old-roadmaps/` | Old roadmap docs |
| `docs/planos/` (1 file) | Archived + dir removed | `docs/archive/old-roadmaps/` | Old evolution plan |
| `docs/frontend/` (3 files) | Archived + dir removed | `docs/archive/old-frontend-audit/` | Old frontend refactoring docs |
| `docs/governance/` (1 file) | Archived + dir removed | `docs/archive/old-execution-plans/` | Old phase governance doc |

---

## Architecture Phase Reports (docs/architecture/ → archive)

### p-series (107 files)

| Pattern | Count | Destination | Reason |
|---|---|---|---|
| `p0-*.md` | ~6 | `docs/archive/old-phase-reports/` | Phase 0 execution reports |
| `p1-*.md` | ~10 | `docs/archive/old-phase-reports/` | Phase 1 execution reports |
| `p2-*.md` | ~6 | `docs/archive/old-phase-reports/` | Phase 2 execution reports |
| `p3-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 3 execution reports |
| `p4-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 4 execution reports |
| `p5-*.md` | ~6 | `docs/archive/old-phase-reports/` | Phase 5 execution reports |
| `p6-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 6 execution reports |
| `p7-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 7 execution reports |
| `p8-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 8 execution reports |
| `p9-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 9 execution reports |
| `p10-*.md` | ~8 | `docs/archive/old-phase-reports/` | Phase 10 execution reports |
| `p11-*.md` | ~6 | `docs/archive/old-phase-reports/` | Phase 11 execution reports |
| `p12-*.md` | ~6 | `docs/archive/old-phase-reports/` | Phase 12 execution reports |

### Obsolete e-trail / n-trail / migration reports (36 files)

| File | Action | Reason |
|---|---|---|
| `e-trail-final-closure.md` | Archived | Execution trail closure, phase complete |
| `e14-legacy-migrations-removal-report.md` | Archived | Migration phase complete |
| `e14-post-removal-gap-report.md` | Archived | Gap report for completed phase |
| `e15-postgresql-baseline-gap-report.md` | Archived | Gap report for completed phase |
| `e15-postgresql-baseline-generation-report.md` | Archived | Baseline generation complete |
| `e16-clickhouse-gap-report.md` | Archived | Gap report for completed phase |
| `e16-clickhouse-structure-implementation-report.md` | Archived | Implementation report for completed phase |
| `e17-end-to-end-gap-report.md` | Archived | Gap report for completed phase |
| `e17-end-to-end-validation-report.md` | Archived | Validation report for completed phase |
| `e18-final-cleanup-gap-report.md` | Archived | Final cleanup gap report, phase done |
| `e18-final-technical-closure-report.md` | Archived | Technical closure report, phase done |
| `execution-phase-readiness-report.md` | Archived | Phase readiness report |
| `legacy-migrations-removal-strategy.md` | Archived | Removal strategy for completed migration |
| `licensing-residue-final-audit.md` | Archived | Audit from completed phase |
| `migration-readiness-by-module.md` | Archived | Migration readiness from completed phase |
| `migration-removal-prerequisites.md` | Archived | Prerequisites for completed migration |
| `migration-transition-risks-and-mitigations.md` | Archived | Transition risks from completed phase |
| `mock-inventory-report.md` | Archived | Mock inventory from completed phase |
| `n-phase-final-validation-and-closure.md` | Archived | N-phase closure |
| `n-trail-final-execution-audit.md` | Archived | N-trail execution audit |
| `n-trail-final-pending-items.md` | Archived | N-trail pending items |
| `n-trail-final-summary-matrix.md` | Archived | N-trail summary |
| `new-baseline-validation-strategy.md` | Archived | Baseline strategy superseded |
| `new-postgresql-baseline-strategy.md` | Archived | PostgreSQL strategy superseded by ADRs |
| `out-of-scope-residue-report.md` | Archived | Residue from completed cleanup |
| `persistence-strategy-final.md` | Archived | Superseded by ADRs and active arch docs |
| `persistence-transition-master-plan.md` | Archived | Transition complete |
| `phase-a-open-items.md` | Archived | Old phase open items, closed |
| `placeholder-and-cosmetic-ui-report.md` | Archived | Old UI audit report |
| `postgresql-baseline-execution-order.md` | Archived | Baseline execution complete |
| `stub-inventory-report.md` | Archived | Old stub inventory |
| `architecture-decisions-final.md` | Archived | Superseded by formal ADRs |
| `final-data-placement-matrix.md` | Archived | Superseded by `module-data-placement-matrix.md` |
| `final-structural-cleanup-backlog.md` | Archived | Cleanup complete |
| `final-structural-readiness-assessment.md` | Archived | Phase readiness assessment, done |
| `docs-vs-code-consistency-report.md` | Archived | One-time consistency audit |

---

## Root docs/ Deleted Files

| File | Action | Reason |
|---|---|---|
| `docs/WAVE-1-CONSOLIDATED-VALIDATION.md` | Deleted | Execution-phase validation tracker, complete |
| `docs/WAVE-1-VALIDATION-TRACKER.md` | Deleted | Execution-phase tracker, complete |
| `docs/EXECUTION-BASELINE-PR1-PR16.md` | Deleted | Execution baseline from completed phase |
| `docs/GO-NO-GO-GATES.md` | Deleted | Gate docs from completed execution |
| `docs/CORE-FLOW-GAPS.md` | Deleted | Gap analysis from past phase |
| `docs/SOLUTION-GAP-ANALYSIS.md` | Deleted | Gap analysis from past phase |
| `docs/ANALISE-CRITICA-ARQUITETURAL.md` | Deleted | Portuguese critical analysis, superseded by ADRs |
| `docs/POST-PR16-EVOLUTION-ROADMAP.md` | Deleted | Superseded by PRODUCT-VISION.md |
| `docs/PRODUCT-REFOUNDATION-PLAN.md` | Deleted | Superseded by PRODUCT-VISION.md |
| `docs/IMPLEMENTATION-STATUS.md` | Deleted | Old execution-phase implementation status |
| `docs/REBASELINE.md` | Deleted | Rebaseline from completed phase |
| `docs/NexTraceOne_Avaliacao_Atual_e_Plano_de_Testes.md` | Deleted | Portuguese duplicate, past phase |
| `docs/NexTraceOne_Plano_Operacional_Finalizacao.md` | Deleted | Portuguese operational plan, past phase |
| `docs/estado-atual-projeto-e-plano-testes.md` | Deleted | Portuguese state snapshot, past phase |
| `docs/ROADMAP.md` | Deleted | Superseded by PRODUCT-VISION.md and PLATFORM-CAPABILITIES.md |

---

## Reliability Directory

| File | Action | Reason |
|---|---|---|
| `docs/reliability/PHASE-3-RELIABILITY-COMPLETION.md` | Archived → `old-phase-reports/` | Phase completion report, done |
| `docs/reliability/RELIABILITY-DATA-MODEL.md` | Kept | Active architecture doc |
| `docs/reliability/RELIABILITY-FRONTEND-INTEGRATION.md` | Kept | Active architecture doc |
| `docs/reliability/RELIABILITY-SCORING-MODEL.md` | Kept | Active architecture doc |

---

## Files Kept Active (docs/ root)

| File | Category |
|---|---|
| `AI-ARCHITECTURE.md` | Architecture |
| `AI-ASSISTED-OPERATIONS.md` | AI Ops |
| `AI-DEVELOPER-EXPERIENCE.md` | AI |
| `AI-GOVERNANCE.md` | Governance |
| `ARCHITECTURE-OVERVIEW.md` | Architecture |
| `BACKEND-MODULE-GUIDELINES.md` | Engineering |
| `BRAND-IDENTITY.md` | Design |
| `CHANGE-CONFIDENCE.md` | Change Intelligence |
| `CONTRACT-STUDIO-VISION.md` | Contracts |
| `DATA-ARCHITECTURE.md` | Architecture |
| `DEPLOYMENT-ARCHITECTURE.md` | Architecture |
| `DESIGN-SYSTEM.md` | Frontend |
| `DESIGN.md` | Design |
| `DOCUMENTATION-INDEX.md` | Meta |
| `DOMAIN-BOUNDARIES.md` | DDD |
| `ENVIRONMENT-VARIABLES.md` | Operations |
| `FRONTEND-ARCHITECTURE.md` | Frontend |
| `GUIDELINE.md` | Engineering |
| `I18N-STRATEGY.md` | Frontend |
| `INTEGRATIONS-ARCHITECTURE.md` | Architecture |
| `LOCAL-SETUP.md` | Operations |
| `MODULES-AND-PAGES.md` | Product |
| `OBSERVABILITY-STRATEGY.md` | Observability |
| `PERSONA-MATRIX.md` | Product |
| `PERSONA-UX-MAPPING.md` | UX |
| `PLATFORM-CAPABILITIES.md` | Product |
| `PRODUCT-SCOPE.md` | Product |
| `PRODUCT-VISION.md` | Product |
| `SECURITY-ARCHITECTURE.md` | Security |
| `SECURITY.md` | Security |
| `SERVICE-CONTRACT-GOVERNANCE.md` | Contracts |
| `SOURCE-OF-TRUTH-STRATEGY.md` | Strategy |
| `UX-PRINCIPLES.md` | UX |
