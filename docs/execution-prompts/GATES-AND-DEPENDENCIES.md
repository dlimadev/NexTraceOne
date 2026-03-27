# NexTraceOne — Gates and Dependencies

> Complete gate definitions, blocker analysis, and dependency map for the execution prompts package.

---

## Gate Definitions

### Gate 1 — Core Flows Stop Being Fake

**Theme:** The two broken central value flows (Incidents, AI Assistant) become functional with real data.

**Required Prompts:**
| Prompt | Description | Blocker? |
|--------|-------------|----------|
| P01.1 | Incident↔change correlation engine | Yes |
| P01.2 | Real incidents persistence | Yes |
| P01.3 | Incidents frontend → real API | Yes |
| P01.4 | Runbooks persistence | No (enhances) |
| P01.5 | Mitigation workflow persistence | No (enhances) |
| P01.6 | AI Assistant → real LLM | Yes |
| P01.7 | AI conversation persistence | Yes |
| P01.8 | AI Assistant frontend → real API | Yes |
| P01.9 | ExternalAI essential handlers | No (enhances) |
| P01.10 | Grounding quality | No (enhances) |

**Gate Closure Criteria:**
- [ ] `GET /api/v1/incidents` returns real persisted data (no mockIncidents)
- [ ] `GET /api/v1/incidents/{id}/correlated-changes` returns dynamic correlations
- [ ] `POST /api/v1/ai/chat` returns real LLM response (no hardcoded stub)
- [ ] `GET /api/v1/ai/conversations` returns persisted conversations
- [ ] IncidentsPage.tsx has zero references to `mockIncidents`
- [ ] AiAssistantPage.tsx has zero references to `mockConversations`
- [ ] All existing tests pass (1,447 backend + 264 frontend)

**When this gate closes:** The product has 4 functional core flows instead of 2.

---

### Gate 2 — Cross-Module Integration Exists

**Theme:** Modules can communicate via defined interfaces; database schemas are deployable; events propagate.

**Required Prompts:**
| Prompt | Description | Blocker? |
|--------|-------------|----------|
| P02.1 | Outbox processor for priority DbContexts | Yes |
| P02.2 | IContractsModule implementation | Yes |
| P02.3 | IChangeIntelligenceModule implementation | Yes |
| P02.4 | ICostIntelligenceModule implementation | Yes |
| P02.5 | IRuntimeIntelligenceModule implementation | No (enhances) |
| P02.6 | IAiOrchestrationModule implementation | No (enhances) |
| P02.7 | IExternalAiModule implementation | No (enhances) |
| P02.8a-g | 7 DbContext migrations | Yes (all) |

**Gate Closure Criteria:**
- [ ] IContractsModule has working implementation with unit tests
- [ ] IChangeIntelligenceModule has working implementation with unit tests
- [ ] ICostIntelligenceModule has working implementation with unit tests
- [ ] IRuntimeIntelligenceModule has working implementation with unit tests
- [ ] Outbox events consumed for Catalog, ChangeGovernance, OperationalIntelligence
- [ ] All 7 new migrations apply cleanly to fresh database
- [ ] All existing tests pass

**When this gate closes:** The product can flow data between modules instead of being isolated silos.

---

### Gate 3 — Governance/FinOps/Knowledge Stop Being Simulated

**Theme:** Executive-facing features show real data instead of `IsSimulated: true`.

**Required Prompts:**
| Prompt | Description | Blocker? |
|--------|-------------|----------|
| P03.1 | Replace mock Teams/Domains handlers | Yes |
| P03.2 | Replace remaining Governance mock handlers | Yes |
| P03.3 | Connect FinOps to CostIntelligence real data | Yes |
| P03.4 | Remove IsSimulated where real exists | Yes |
| P03.5 | Knowledge Hub real persistence | Yes |
| P03.6 | Link runbooks to knowledge storage | No (enhances) |

**Gate Closure Criteria:**
- [ ] ≥20 Governance handlers return real data (not IsSimulated)
- [ ] FinOps pages show real cost data from CostIntelligenceDbContext
- [ ] Knowledge Hub persists and retrieves documents from database
- [ ] Runbooks are persisted via RunbookRecord (not hardcoded)
- [ ] DemoBanner only shown where data is still genuinely simulated
- [ ] All existing tests pass

**When this gate closes:** Product, Executive, and Auditor personas see real operational data.

---

### Gate 4 — Quality and Operations Hardened

**Theme:** CI/CD pipeline prevents regressions; configs are environment-aware; security gaps closed.

**Required Prompts:**
| Prompt | Description | Blocker? |
|--------|-------------|----------|
| P05.1 | E2E tests as PR gate | Yes |
| P05.2 | Code coverage gates | No (enhances) |
| P05.3 | Reduce CS8632 warnings | No (enhances) |
| P05.4 | Stricter smoke checks | No (enhances) |
| P05.5 | Migration validation in pipeline | Yes |
| P05.6 | API Keys to secure storage | Yes |

**Gate Closure Criteria:**
- [ ] E2E tests run on every PR and block merge on failure
- [ ] OTEL endpoint configurable per environment (not hardcoded localhost)
- [ ] At least 1 CI/CD connector processes real deploy events
- [ ] API Keys stored in encrypted database (not appsettings.json)
- [ ] All migrations validate cleanly in CI
- [ ] All existing tests pass

**When this gate closes:** The product is deployable with confidence in non-development environments.

---

### Gate 5 — Cleanup and Consolidation

**Theme:** Technical debt reduced; documentation reflects reality; no orphaned mocks.

**Required Prompts:**
| Prompt | Description | Blocker? |
|--------|-------------|----------|
| P06.1 | Archive historical execution docs | No |
| P06.2 | Consolidate duplicate docs | No |
| P06.3 | Remove Commercial Governance residues | No |
| P06.4 | Remove InMemoryIncidentStore residue | No |
| P06.5 | Consolidate roadmap and status | No |
| P06.6 | Cleanup replaced mocks | Yes (depends on Gate 3) |

**Gate Closure Criteria:**
- [ ] Historical docs moved to `docs/archive/`
- [ ] No duplicate roadmap/status documents in active docs
- [ ] Zero references to Commercial Governance in active code
- [ ] Zero references to InMemoryIncidentStore in active code
- [ ] All replaced mocks removed from codebase
- [ ] Documentation reflects actual project state

**When this gate closes:** The repository is clean, honest, and navigable.

---

## Dependency Chain (Critical Path)

```
P00.1 → P00.2, P00.3
                      ↘
P01.1 ──────→ P01.3 ──→ P05.1 (E2E gate)
P01.2 ──────→ P01.3
P01.4 → P01.5
P01.6 → P01.7 → P01.8 → P05.1 (E2E gate)
P01.6 → P01.9 → P02.7
                      ↘
P02.8a → P02.5       GATE 2
P02.8b → P02.4 → P03.3
P02.8c → P02.6
P02.8d → P02.7
P02.8e → P04.4
P02.8f → P03.5 → P03.6
P02.2 → P03.1
P02.3 → P03.2 → P03.4 → P06.6 → GATE 5
                      ↘
P03.3 ──────→ GATE 3
P04.1 → P04.2
P04.3 → P04.5 → P04.6
```

---

## Parallel Execution Groups

### Group A (can run simultaneously)
- P01.1 (OperationalIntelligence backend)
- P01.4 (OperationalIntelligence backend — different area)
- P01.6 (AIKnowledge backend)

### Group B (can run simultaneously — after Group A)
- P01.2 (OperationalIntelligence persistence fix)
- P01.7 (AIKnowledge conversation persistence)

### Group C (all independent migrations)
- P02.8a, P02.8b, P02.8c, P02.8d, P02.8e, P02.8f, P02.8g

### Group D (independent interface implementations)
- P02.2 (Catalog module)
- P02.3 (ChangeGovernance module)

### Group E (independent quality tasks)
- P05.2, P05.3, P05.4, P05.6

### Group F (independent cleanup tasks)
- P06.1, P06.2, P06.3, P06.4

---

## Blocker Analysis

### Critical blockers (if not done, entire phases stall)
1. **P01.1** — Without correlation engine, P01.3 (frontend) is impossible
2. **P01.6** — Without real LLM, P01.7 and P01.8 are impossible
3. **P02.8a-g** — Without migrations, interface implementations can't persist data
4. **P02.2 + P02.3** — Without these interfaces, Governance can't consume real data (P03.*)

### Non-blockers (improve quality but don't block other work)
- P00.2 (CURRENT-STATE docs)
- P01.10 (grounding quality)
- P05.2 (coverage gates)
- P05.3 (nullable warnings)
