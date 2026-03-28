# Change Governance — Current State

**Maturity:** READY (100% real — most mature module)
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §ChangeGovernance`, `docs/audit-forensic-2026-03/frontend-state-report.md §ChangeGovernance`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| ChangeIntelligenceDbContext | Confirmed (with snapshot) | READY |
| WorkflowDbContext | Confirmed (with snapshot) | READY |
| PromotionDbContext | Confirmed (with snapshot) | READY |
| RulesetGovernanceDbContext | Confirmed (with snapshot) | READY |

Table prefix: `chg_`

---

## Features (50+ total, 100% real)

| Area | Features | Status |
|---|---|---|
| Change Intelligence | Releases, BlastRadius, ChangeScores, FreezeWindows, RollbackAssessments | READY |
| Workflow | Templates, instances, stages, approval decisions, evidence packs, SLA policies | READY |
| Promotion | Environments, promotion requests, gates, gate evaluations | READY |
| Ruleset Governance | Rulesets, bindings, lint results (Spectral) | READY |
| Audit Trail | Decision trail, change timeline, correlation events | READY |
| Post-change Gates | Gate evaluations real; RecordMitigationValidation partial | PARTIAL |

---

## Frontend Pages (6 pages — FULLY CONNECTED)

| Page | Status |
|---|---|
| ChangeCatalogPage | READY — API real |
| ChangeDetailPage | READY — API real |
| WorkflowPage | READY — API real |
| WorkflowConfigurationPage | READY — API real |
| ReleasesPage | READY — API real |
| PromotionPage | READY — API real |

---

## Key Gaps

- `IChangeIntelligenceModule` cross-module interface: defined, not implemented — blocks Governance from reading real change data
- `IPromotionModule` cross-module interface: defined, not implemented
- `IRulesetGovernanceModule` cross-module interface: defined, not implemented
- CI/CD integration: stub (no real pipeline events consumed)
- Release Calendar UI: not audited for completeness
- `RecordMitigationValidation`: partial — post-change verification not fully wired

---

## Cross-Module Interface Status

| Interface | Status |
|---|---|
| `IChangeIntelligenceModule` | PLAN — defined, not implemented |
| `IPromotionModule` | PLAN — defined, not implemented |
| `IRulesetGovernanceModule` | PLAN — defined, not implemented |

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/capability-gap-matrix.md`*
