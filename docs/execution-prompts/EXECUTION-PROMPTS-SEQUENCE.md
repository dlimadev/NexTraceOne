# NexTraceOne — Execution Prompts Sequence

> **Source of Truth:** `docs/audit-forensic-2026-03/` (16 forensic audit reports, March 2026)
>
> **Project Verdict:** `STRATEGIC_BUT_INCOMPLETE`
>
> **Principle:** Close core flows and structural gaps before expanding surface.
>
> **Total Prompts:** 51 across 7 phases and 5 gates
>
> **Prompt Files:** `docs/execution-prompts/generated/` — each prompt is a standalone .md file with 15 sections

---

## Gate Map

| Gate | Theme | Unlocked When | Key Blockers |
|------|-------|---------------|--------------|
| **Gate 1** | Core flows stop being fake | Incidents real + AI Assistant real | P01.1, P01.3, P01.6, P01.8 |
| **Gate 2** | Cross-module integration exists | Priority interfaces + outbox + migrations | P02.1, P02.2, P02.3, P02.8a-g |
| **Gate 3** | Governance/FinOps/Knowledge real | Real data consumption replaces IsSimulated | P03.1, P03.2, P03.3, P03.5 |
| **Gate 4** | Quality and operations hardened | E2E blocks PRs + migration validation | P05.1, P05.5, P05.6 |
| **Gate 5** | Cleanup and consolidation | Historical docs archived + mocks removed | P06.1, P06.6 |

> See `GATES-AND-DEPENDENCIES.md` for detailed gate criteria and blocker analysis.

---

## Phase 00 — Foundation (3 prompts)

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 1 | P00.1 | [`P00.1-fix-doc-contradictions.md`](generated/00-foundation/P00.1-fix-doc-contradictions.md) | Fix IMPLEMENTATION-STATUS and ROADMAP contradictions | yes |
| 2 | P00.2 | [`P00.2-create-current-state-docs.md`](generated/00-foundation/P00.2-create-current-state-docs.md) | Create CURRENT-STATE.md per module | yes (after P00.1) |
| 3 | P00.3 | [`P00.3-align-core-flow-gaps.md`](generated/00-foundation/P00.3-align-core-flow-gaps.md) | Align CORE-FLOW-GAPS.md to audit | yes (after P00.1) |

---

## Phase 01 — Critical Core Flow Fixes (10 prompts) → Gate 1

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 4 | P01.1 | [`P01.1-incident-change-correlation-engine.md`](generated/01-critical-core-flows/P01.1-incident-change-correlation-engine.md) | Dynamic incident↔change correlation engine | yes (group-A) |
| 5 | P01.2 | [`P01.2-real-incidents-persistence.md`](generated/01-critical-core-flows/P01.2-real-incidents-persistence.md) | Fix all incident persistence handlers | yes (group-B) |
| 6 | P01.4 | [`P01.4-runbooks-persistence.md`](generated/01-critical-core-flows/P01.4-runbooks-persistence.md) | Replace 3 hardcoded runbooks with persistence | yes (group-A) |
| 7 | P01.5 | [`P01.5-mitigation-workflow-persistence.md`](generated/01-critical-core-flows/P01.5-mitigation-workflow-persistence.md) | Fix mitigation workflow to persist data | no (after P01.4) |
| 8 | P01.6 | [`P01.6-ai-assistant-real-llm.md`](generated/01-critical-core-flows/P01.6-ai-assistant-real-llm.md) | Connect AI Assistant to real LLM | yes (group-A) |
| 9 | P01.7 | [`P01.7-ai-conversation-persistence.md`](generated/01-critical-core-flows/P01.7-ai-conversation-persistence.md) | Persist AI conversations with audit | no (after P01.6) |
| 10 | P01.9 | [`P01.9-externalai-handlers.md`](generated/01-critical-core-flows/P01.9-externalai-handlers.md) | Implement essential ExternalAI handlers | no (after P01.6) |
| 10 | P01.10 | [`P01.10-grounding-quality.md`](generated/01-critical-core-flows/P01.10-grounding-quality.md) | Expand AI grounding to Catalog+Changes | yes (group-B) |
| 11 | P01.3 | [`P01.3-incidents-frontend-real-api.md`](generated/01-critical-core-flows/P01.3-incidents-frontend-real-api.md) | Connect incidents frontend to real API | no (after P01.1+P01.2) |
| 12 | P01.8 | [`P01.8-ai-assistant-frontend-real-api.md`](generated/01-critical-core-flows/P01.8-ai-assistant-frontend-real-api.md) | Connect AI Assistant frontend to real API+SSE | no (after P01.6+P01.7) |

**Gate 1 closure:** Incidents + AI Assistant both functional with real data. See `GATES-AND-DEPENDENCIES.md` for criteria.

---

## Phase 02 — Structural Foundations (14 prompts) → Gate 2

### Migrations (7 prompts — all parallelizable, group-C)

| Order | Prompt | File | DbContext | Prefix |
|-------|--------|------|-----------|--------|
| 13 | P02.8a | [`P02.8a-migration-runtimeintelligence.md`](generated/02-structural-foundations/P02.8a-migration-runtimeintelligence.md) | RuntimeIntelligenceDbContext | ops_ |
| 14 | P02.8b | [`P02.8b-migration-costintelligence.md`](generated/02-structural-foundations/P02.8b-migration-costintelligence.md) | CostIntelligenceDbContext | ops_ |
| 15 | P02.8c | [`P02.8c-migration-aiorchestration.md`](generated/02-structural-foundations/P02.8c-migration-aiorchestration.md) | AiOrchestrationDbContext | aik_ |
| 16 | P02.8d | [`P02.8d-migration-externalai.md`](generated/02-structural-foundations/P02.8d-migration-externalai.md) | ExternalAiDbContext | aik_ |
| 17 | P02.8e | [`P02.8e-migration-integrations.md`](generated/02-structural-foundations/P02.8e-migration-integrations.md) | IntegrationsDbContext | int_ |
| 18 | P02.8f | [`P02.8f-migration-knowledge.md`](generated/02-structural-foundations/P02.8f-migration-knowledge.md) | KnowledgeDbContext | knw_ |
| 19 | P02.8g | [`P02.8g-migration-productanalytics.md`](generated/02-structural-foundations/P02.8g-migration-productanalytics.md) | ProductAnalyticsDbContext | pan_ |

### Outbox + Cross-Module Interfaces (7 prompts)

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 20 | P02.1 | [`P02.1-outbox-processor-activation.md`](generated/02-structural-foundations/P02.1-outbox-processor-activation.md) | Activate outbox for Catalog+Changes+Incidents | yes |
| 21 | P02.2 | [`P02.2-icontracts-module.md`](generated/02-structural-foundations/P02.2-icontracts-module.md) | Implement IContractsModule interface | yes (group-D) |
| 22 | P02.3 | [`P02.3-ichangeintelligence-module.md`](generated/02-structural-foundations/P02.3-ichangeintelligence-module.md) | Implement IChangeIntelligenceModule interface | yes (group-D) |
| 23 | P02.4 | [`P02.4-icostintelligence-module.md`](generated/02-structural-foundations/P02.4-icostintelligence-module.md) | Implement ICostIntelligenceModule interface | no (after P02.8b) |
| 24 | P02.5 | [`P02.5-iruntimeintelligence-module.md`](generated/02-structural-foundations/P02.5-iruntimeintelligence-module.md) | Implement IRuntimeIntelligenceModule interface | no (after P02.8a) |
| 25 | P02.6 | [`P02.6-iaiorchestration-module.md`](generated/02-structural-foundations/P02.6-iaiorchestration-module.md) | Implement IAiOrchestrationModule interface | no (after P01.7+P02.8c) |
| 26 | P02.7 | [`P02.7-iexternalai-module.md`](generated/02-structural-foundations/P02.7-iexternalai-module.md) | Implement IExternalAiModule interface | no (after P01.9+P02.8d) |

**Gate 2 closure:** Cross-module interfaces implemented + outbox active + all migrations applied. See `GATES-AND-DEPENDENCIES.md`.

---

## Phase 03 — Governance / FinOps / Knowledge (6 prompts) → Gate 3

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 27 | P03.1 | [`P03.1-governance-teams-domains-real.md`](generated/03-governance-finops-knowledge/P03.1-governance-teams-domains-real.md) | Replace Teams/Domains mock with real data | no (after P02.2+P02.3) |
| 28 | P03.2 | [`P03.2-governance-handlers-real.md`](generated/03-governance-finops-knowledge/P03.2-governance-handlers-real.md) | Replace remaining CAN_REPLACE_NOW handlers | no (after P03.1) |
| 29 | P03.3 | [`P03.3-finops-costintelligence-real.md`](generated/03-governance-finops-knowledge/P03.3-finops-costintelligence-real.md) | Connect FinOps to real CostIntelligence data | no (after P02.4) |
| 30 | P03.5 | [`P03.5-knowledge-hub-persistence.md`](generated/03-governance-finops-knowledge/P03.5-knowledge-hub-persistence.md) | Knowledge Hub CRUD + FTS persistence | no (after P02.8f) |
| 31 | P03.6 | [`P03.6-runbooks-knowledge-link.md`](generated/03-governance-finops-knowledge/P03.6-runbooks-knowledge-link.md) | Link RunbookRecord to Knowledge Hub | no (after P01.4+P03.5) |
| 32 | P03.4 | [`P03.4-remove-issimulated-real.md`](generated/03-governance-finops-knowledge/P03.4-remove-issimulated-real.md) | Remove IsSimulated from confirmed-real handlers | no (after P03.2) |

**Gate 3 closure:** ≥20 Governance handlers return real data. FinOps shows real costs. Knowledge Hub persists. See `GATES-AND-DEPENDENCIES.md`.

---

## Phase 04 — Integrations & Observability (6 prompts)

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 33 | P04.1 | [`P04.1-otel-endpoint-per-environment.md`](generated/04-integrations-observability/P04.1-otel-endpoint-per-environment.md) | Make OTEL endpoint configurable per environment | yes |
| 34 | P04.2 | [`P04.2-validate-otel-pipeline.md`](generated/04-integrations-observability/P04.2-validate-otel-pipeline.md) | Validate OTEL→Collector→ClickHouse pipeline | no (after P04.1) |
| 35 | P04.3 | [`P04.3-ingestion-semantic-processing.md`](generated/04-integrations-observability/P04.3-ingestion-semantic-processing.md) | Ingestion API: semantic payload processing | yes |
| 36 | P04.4 | [`P04.4-github-actions-connector.md`](generated/04-integrations-observability/P04.4-github-actions-connector.md) | First real CI/CD connector: GitHub Actions | no (after P02.8e) |
| 37 | P04.5 | [`P04.5-canonical-deploy-event.md`](generated/04-integrations-observability/P04.5-canonical-deploy-event.md) | Canonical deploy event model + mappers | no (after P04.3 or P04.4) |
| 38 | P04.6 | [`P04.6-deploy-release-correlation.md`](generated/04-integrations-observability/P04.6-deploy-release-correlation.md) | Link deploy events to Change Intelligence releases | no (after P04.5+P01.1+P02.3) |

---

## Phase 05 — Quality & Hardening (6 prompts) → Gate 4

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 39 | P05.1 | [`P05.1-e2e-pr-gate.md`](generated/05-quality-hardening/P05.1-e2e-pr-gate.md) | E2E tests mandatory on PR merges | no (after P01.3+P01.8) |
| 40 | P05.2 | [`P05.2-code-coverage-gates.md`](generated/05-quality-hardening/P05.2-code-coverage-gates.md) | Code coverage tracking + thresholds | yes (group-E) |
| 41 | P05.3 | [`P05.3-nullable-warnings.md`](generated/05-quality-hardening/P05.3-nullable-warnings.md) | Reduce 516 CS8632 nullable warnings | yes (group-E) |
| 42 | P05.4 | [`P05.4-stricter-smoke-checks.md`](generated/05-quality-hardening/P05.4-stricter-smoke-checks.md) | Per-module status checks in staging | yes (group-E) |
| 43 | P05.5 | [`P05.5-migration-validation-pipeline.md`](generated/05-quality-hardening/P05.5-migration-validation-pipeline.md) | Validate all migrations in CI | no (after P02.8a-g) |
| 44 | P05.6 | [`P05.6-api-keys-secure-storage.md`](generated/05-quality-hardening/P05.6-api-keys-secure-storage.md) | API keys to encrypted database storage | yes (group-E) |

**Gate 4 closure:** E2E blocks PRs + migration validation in CI + API keys secured. See `GATES-AND-DEPENDENCIES.md`.

---

## Phase 06 — Cleanup & Consolidation (6 prompts) → Gate 5

| Order | Prompt | File | Objective | Parallel |
|-------|--------|------|-----------|----------|
| 45 | P06.1 | [`P06.1-archive-historical-docs.md`](generated/06-cleanup-consolidation/P06.1-archive-historical-docs.md) | Archive e14-e18, p0-p1, n-trail docs | yes (group-F) |
| 46 | P06.2 | [`P06.2-consolidate-duplicate-docs.md`](generated/06-cleanup-consolidation/P06.2-consolidate-duplicate-docs.md) | Merge overlapping status/roadmap docs | yes (group-F) |
| 47 | P06.3 | [`P06.3-remove-commercial-governance.md`](generated/06-cleanup-consolidation/P06.3-remove-commercial-governance.md) | Remove Commercial Governance residues | yes (group-F) |
| 48 | P06.4 | [`P06.4-remove-inmemory-incident-store.md`](generated/06-cleanup-consolidation/P06.4-remove-inmemory-incident-store.md) | Remove InMemoryIncidentStore residue | yes (group-F) |
| 49 | P06.5 | [`P06.5-consolidate-roadmap.md`](generated/06-cleanup-consolidation/P06.5-consolidate-roadmap.md) | Single authoritative ROADMAP.md | yes (group-F) |
| 50 | P06.6 | [`P06.6-cleanup-replaced-mocks.md`](generated/06-cleanup-consolidation/P06.6-cleanup-replaced-mocks.md) | Remove confirmed replaced mock code | no (after P03.4) |

**Gate 5 closure:** Docs archived, duplicates consolidated, dead code removed. See `GATES-AND-DEPENDENCIES.md`.

---

## Parallel Execution Groups

| Group | Prompts | Constraint |
|-------|---------|------------|
| **group-A** | P01.1, P01.4, P01.6 | Different modules, safe to run together |
| **group-B** | P01.2, P01.10 | Different handlers, safe to run together |
| **group-C** | P02.8a–P02.8g | Independent DbContext migrations, all safe to run together |
| **group-D** | P02.2, P02.3 | Different modules (Catalog, ChangeGovernance), safe to run together |
| **group-E** | P05.2, P05.3, P05.4, P05.6 | Independent quality tasks, safe to run together |
| **group-F** | P06.1, P06.2, P06.3, P06.4, P06.5 | Independent cleanup tasks, safe to run together |

---

## Sprint Mapping (Recommended)

| Sprint | Phases | Prompts | Gate |
|--------|--------|---------|------|
| Sprint 0 | Phase 00 | P00.1–P00.3 | — |
| Sprint 1 | Phase 01 | P01.1–P01.10 | Gate 1 |
| Sprint 2 | Phase 02 | P02.1–P02.8g | Gate 2 |
| Sprint 3 | Phase 03 | P03.1–P03.6 | Gate 3 |
| Sprint 4 | Phases 04+05 | P04.1–P05.6 | Gate 4 |
| Sprint 5 | Phase 06 | P06.1–P06.6 | Gate 5 |

---

## Anti-Collision Rules

1. **P01.3 and P01.8** — both touch frontend i18n locales. Do NOT run simultaneously.
2. **P03.1 → P03.2 → P03.4** — sequential chain on governance handlers. Must be in order.
3. **P02.8a-g** — all migrations can run in parallel but test on the same database. Run simultaneously only if using isolated test databases.
4. **P04.5 and P04.6** — strict sequence (canonical model before correlation).

---

## How to Use

1. **Start with Phase 00** — establish documentation truth baseline
2. **Execute prompts in order** within each phase (respect execution_order column)
3. **Check parallel groups** — prompts in the same group can run simultaneously
4. **Verify gate criteria** before advancing to the next phase
5. **Run build + tests** after each prompt completes
6. **Update PROMPT-INVENTORY.csv** status column after each prompt

> For detailed gate criteria, see `GATES-AND-DEPENDENCIES.md`.
> For prompt-to-problem traceability, see `PROBLEM-TO-PROMPT-MAPPING.csv`.
> For the full prompt inventory, see `PROMPT-INVENTORY.csv`.
