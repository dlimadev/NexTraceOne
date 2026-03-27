# NexTraceOne — Execution Prompts Package

> **Source of Truth:** `docs/audit-forensic-2026-03/` (16 forensic audit reports, March 2026)
> **Project Verdict:** `STRATEGIC_BUT_INCOMPLETE`
> **Total Prompts:** 51 across 7 phases and 5 gates

---

## What is this?

This directory contains a **complete, sequenced, dependency-aware set of execution prompts** designed for the Copilot Agent (or any AI coding agent) to evolve the NexTraceOne platform from its current state to enterprise-ready.

Each prompt is:
- **Self-contained** — one file per task, one objective per prompt
- **Detailed** — includes problem context, steps, acceptance criteria, and validations
- **Sequenced** — explicit dependencies and execution order
- **Scoped** — limited file count, bounded context, clear limits

---

## Directory Structure

```
docs/execution-prompts/
├── README.md                          ← You are here
├── EXECUTION-PROMPTS-SEQUENCE.md      ← Gate map + macro sequence + dependency graph
├── GATES-AND-DEPENDENCIES.md          ← Gate definitions + blocker analysis
├── PROMPT-INVENTORY.csv               ← Full inventory of all prompts (machine-readable)
├── PROBLEM-TO-PROMPT-MAPPING.csv      ← Maps problems → prompts (traceability)
└── generated/                         ← Individual prompt files
    ├── 00-foundation/                 ← Phase 00: Documentation truth baseline
    ├── 01-critical-core-flows/        ← Phase 01: Fix broken core flows (Gate 1)
    ├── 02-structural-foundations/     ← Phase 02: Cross-module interfaces + migrations (Gate 2)
    ├── 03-governance-finops-knowledge/← Phase 03: Real data for Governance/FinOps/Knowledge (Gate 3)
    ├── 04-integrations-observability/ ← Phase 04: CI/CD connectors + telemetry pipeline
    ├── 05-quality-hardening/          ← Phase 05: E2E gates + security + coverage (Gate 4)
    └── 06-cleanup-consolidation/      ← Phase 06: Archive + cleanup + consolidation (Gate 5)
```

---

## How to Execute

### Step 1: Follow the sequence
Execute prompts in the order specified in `EXECUTION-PROMPTS-SEQUENCE.md`. The execution order column in each prompt file is authoritative.

### Step 2: Respect dependencies
Each prompt file lists:
- `depends_on`: Which prompts must be completed before starting
- `unblocks`: Which prompts become available after completion
- `can_run_in_parallel`: Whether this prompt can run alongside others

### Step 3: Check gates before advancing phases
Before moving to a new phase, verify the gate criteria in `GATES-AND-DEPENDENCIES.md`. A gate is closed when all its required prompts pass acceptance criteria.

### Step 4: Avoid collisions
Two prompts that modify the same files should NOT run simultaneously. When overlap is unavoidable, prompts are marked as sequential continuations.

### Step 5: Update state after each prompt
After executing a prompt:
1. Verify all acceptance criteria pass
2. Run build + tests
3. Mark the prompt as DONE in `PROMPT-INVENTORY.csv`
4. Check if any blocked prompts are now unblocked

---

## Execution Order Summary

| Sprint | Phase | Gate | Prompts | Focus |
|--------|-------|------|---------|-------|
| Sprint 0 | Phase 00 | — | P00.1–P00.3 | Fix doc contradictions |
| Sprint 1 | Phase 01 | Gate 1 | P01.1–P01.10 | Incidents real + AI real |
| Sprint 2 | Phase 02 | Gate 2 | P02.1–P02.8g | Migrations + interfaces + outbox |
| Sprint 3 | Phase 03 | Gate 3 | P03.1–P03.6 | Governance/FinOps/Knowledge real |
| Sprint 4 | Phase 04 | — | P04.1–P04.6 | Integrations + observability |
| Sprint 4 | Phase 05 | Gate 4 | P05.1–P05.6 | Quality gates + hardening |
| Sprint 5 | Phase 06 | Gate 5 | P06.1–P06.6 | Cleanup + consolidation |

---

## Parallelism Rules

### Safe to parallelize
- P01.1 and P01.6 (different modules: OperationalIntelligence vs AIKnowledge)
- P01.4 and P01.6 (different modules)
- P02.8a through P02.8g (independent DbContext migrations)
- P02.2 and P02.3 (different modules: Catalog vs ChangeGovernance)
- P05.2 through P05.6 (independent concerns)

### Must be sequential
- P01.1 → P01.3 (backend before frontend)
- P01.6 → P01.7 → P01.8 (LLM → persistence → frontend)
- P03.1 → P03.2 → P03.4 (Teams/Domains → broader Governance → IsSimulated removal)
- P01.3 and P01.8 should NOT run in parallel (both touch frontend, risk i18n conflicts)

---

## Key Principles

1. **Close core flows before expanding surface** — Fix incidents and AI before touching FinOps
2. **Backend before frontend** — Never connect frontend to an API that doesn't exist yet
3. **Real before cleanup** — Never remove a mock before its real replacement is confirmed
4. **Independence** — Each prompt should be executable without hidden context
5. **Verifiability** — Every prompt has acceptance criteria that can be objectively checked
